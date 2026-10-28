using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Deliverable3
{
    // Servidor autoritatiu: els clients envien input i el servidor envia l'estat sencer de la partida.
    public class GameServer : MonoBehaviour
    {
        public const float TickRate = 30f;
        public const float SnapshotRate = 20f;

        const float ArenaHalfSize = 10f;
        const float SpawnRadius = 7f;
        const float WalkSpeed = 5f;
        const float CrouchSpeed = 2f;
        const float JumpSpeed = 6.5f;
        const float Gravity = 18f;
        const float BodyRadius = 0.5f;
        const float StandHeight = 2f;
        const float CrouchHeight = 1f;
        const float FireCooldown = 0.35f;
        const float ShotRange = 25f;
        const int ShotDamage = 25;
        const int MaxHealth = 100;
        const float RespawnDelay = 2f;
        const int KillsToWin = 5;
        const float RestartDelay = 5f;

        class Player
        {
            public string name;
            public EndPoint endpoint;
            public InputDTO input;
            public uint lastSequence;
            public Vector3 position;
            public float verticalSpeed;
            public float yaw;
            public bool crouching;
            public int health;
            public int score;
            public byte shots;
            public float nextFireTime;
            public float respawnTime;

            public bool Alive { get { return health > 0; } }
            public float Height { get { return crouching ? CrouchHeight : StandHeight; } }
            public float ShotHeight { get { return crouching ? 0.6f : 1.4f; } }
        }

        readonly UdpChannel m_channel = new UdpChannel();
        readonly Dictionary<string, Player> m_players = new Dictionary<string, Player>();
        LobbyServer m_lobby;
        uint m_tick;
        float m_time;
        float m_tickTimer;
        float m_sendTimer;
        GamePhase m_phase;
        string m_winner = "";
        float m_restartTime;

        public bool Begin(LobbyServer lobby, int port)
        {
            m_lobby = lobby;
            if (!m_channel.Open(port)) return false;
            SyncWithLobby();
            ResetRound();
            Debug.Log("[GAME SERVER] UDP " + port + ", players: " + string.Join(", ", SortedNames()));
            return true;
        }

        void OnDestroy() { m_channel.Close(); }

        void Update()
        {
            if (m_lobby == null) return;

            UdpChannel.Packet packet;
            while (m_channel.TryReceive(out packet)) Handle(packet);

            SyncWithLobby();

            float step = 1f / TickRate;
            // Passos fixos, sigui quin sigui el framerate.
            m_tickTimer = Mathf.Min(m_tickTimer + Time.deltaTime, 0.25f);
            while (m_tickTimer >= step)
            {
                m_tickTimer -= step;
                Simulate(step);
                m_tick++;
            }

            m_sendTimer += Time.deltaTime;
            if (m_sendTimer >= 1f / SnapshotRate)
            {
                m_sendTimer = 0f;
                SendState();
            }
        }

        void Handle(UdpChannel.Packet packet)
        {
            try
            {
                switch (Messages.Type(packet.data))
                {
                    case MsgType.GameJoin: OnJoin(packet.from, Messages.ReadString(packet.data)); break;
                    case MsgType.Input: OnInput(packet.from, Serializer.DeserializeInputDTO(Messages.Body(packet.data))); break;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[GAME SERVER] Dropped a bad packet from " + packet.from + ": " + e.Message);
            }
        }

        void OnJoin(EndPoint from, string name)
        {
            Player player;
            if (!m_players.TryGetValue(name, out player) || from.Equals(player.endpoint)) return;
            player.endpoint = from;
            player.lastSequence = 0;
            Debug.Log("[GAME SERVER] " + name + " is at " + from);
        }

        void OnInput(EndPoint from, InputDTO input)
        {
            Player player = FindByEndpoint(from);
            // UDP pot desordenar: un input més vell que l'últim que tenim no serveix.
            if (player == null || input.sequence <= player.lastSequence) return;

            player.lastSequence = input.sequence;
            input.moveX = Clean(input.moveX, -1f, 1f);
            input.moveZ = Clean(input.moveZ, -1f, 1f);
            input.aimYaw = Clean(input.aimYaw, -360f, 360f);
            player.input = input;
        }

        static float Clean(float value, float min, float max)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp(value, min, max);
        }

        void SendState()
        {
            StateDTO state = new StateDTO
            {
                tick = m_tick,
                phase = m_phase,
                winner = m_winner,
                restartIn = m_phase == GamePhase.GameOver ? Mathf.Max(0f, m_restartTime - m_time) : 0f,
                players = new List<PlayerStateDTO>(),
            };
            foreach (string name in SortedNames())
            {
                Player p = m_players[name];
                byte flags = 0;
                if (p.crouching) flags |= PlayerStateDTO.Crouching;
                if (p.Alive) flags |= PlayerStateDTO.Alive;
                state.players.Add(new PlayerStateDTO
                {
                    name = p.name,
                    position = p.position,
                    yaw = p.yaw,
                    flags = flags,
                    health = (byte)p.health,
                    score = (byte)Mathf.Min(p.score, 255),
                    shots = p.shots,
                });
            }

            byte[] message = Messages.State(state);
            foreach (Player p in m_players.Values)
                if (p.endpoint != null) m_channel.Send(message, p.endpoint);
        }

        void Simulate(float dt)
        {
            m_time += dt;

            if (m_phase == GamePhase.GameOver)
            {
                if (m_time >= m_restartTime) ResetRound();
                return;
            }

            foreach (Player p in m_players.Values)
            {
                if (!p.Alive) { if (m_time >= p.respawnTime) Respawn(p); continue; }
                Move(p, dt);
            }

            foreach (Player p in m_players.Values)
            {
                if (m_phase != GamePhase.Playing) break;
                if (p.Alive && p.input.Has(InputDTO.Fire) && m_time >= p.nextFireTime) Fire(p);
            }
        }

        void Move(Player p, float dt)
        {
            bool grounded = p.position.y <= 0f;
            p.crouching = grounded && p.input.Has(InputDTO.Crouch);

            Vector3 move = Vector3.ClampMagnitude(new Vector3(p.input.moveX, 0f, p.input.moveZ), 1f);
            p.position += move * (p.crouching ? CrouchSpeed : WalkSpeed) * dt;

            if (grounded && !p.crouching && p.input.Has(InputDTO.Jump)) p.verticalSpeed = JumpSpeed;
            p.verticalSpeed -= Gravity * dt;
            p.position.y += p.verticalSpeed * dt;
            if (p.position.y <= 0f) { p.position.y = 0f; p.verticalSpeed = 0f; }

            p.position.x = Mathf.Clamp(p.position.x, -ArenaHalfSize, ArenaHalfSize);
            p.position.z = Mathf.Clamp(p.position.z, -ArenaHalfSize, ArenaHalfSize);
            p.yaw = p.input.aimYaw;
        }

        void Fire(Player shooter)
        {
            shooter.nextFireTime = m_time + FireCooldown;
            shooter.shots++;

            Vector3 origin = shooter.position + Vector3.up * shooter.ShotHeight;
            Vector3 direction = Quaternion.Euler(0f, shooter.yaw, 0f) * Vector3.forward;

            Player hit = null;
            float nearest = ShotRange;
            foreach (Player target in m_players.Values)
            {
                if (target == shooter || !target.Alive) continue;
                // Un tret dret passa per sobre d'un jugador ajupit: ajupir-se serveix per esquivar.
                if (origin.y < target.position.y || origin.y > target.position.y + target.Height) continue;

                Vector3 toTarget = target.position - origin;
                toTarget.y = 0f;
                float along = Vector3.Dot(toTarget, direction);
                if (along < 0f || along > nearest) continue;
                if ((toTarget - direction * along).magnitude > BodyRadius) continue;

                hit = target;
                nearest = along;
            }
            if (hit != null) Damage(shooter, hit);
        }

        void Damage(Player shooter, Player target)
        {
            target.health = Mathf.Max(0, target.health - ShotDamage);
            if (target.Alive) return;

            target.respawnTime = m_time + RespawnDelay;
            shooter.score++;
            Debug.Log("[GAME SERVER] " + shooter.name + " killed " + target.name + " (" + shooter.score + "/" + KillsToWin + ")");

            if (shooter.score >= KillsToWin)
            {
                m_phase = GamePhase.GameOver;
                m_winner = shooter.name;
                m_restartTime = m_time + RestartDelay;
                Debug.Log("[GAME SERVER] " + shooter.name + " wins");
            }
        }

        void ResetRound()
        {
            m_phase = GamePhase.Playing;
            m_winner = "";
            List<string> names = SortedNames();
            for (int i = 0; i < names.Count; i++)
            {
                Player p = m_players[names[i]];
                p.score = 0;
                Respawn(p, SpawnPoint(i, names.Count));
            }
        }

        void Respawn(Player p) { Respawn(p, SpawnPoint(UnityEngine.Random.Range(0, 8), 8)); }

        void Respawn(Player p, Vector3 position)
        {
            p.position = position;
            p.verticalSpeed = 0f;
            p.crouching = false;
            p.health = MaxHealth;
            p.yaw = Mathf.Atan2(-position.x, -position.z) * Mathf.Rad2Deg;
        }

        static Vector3 SpawnPoint(int index, int count)
        {
            float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
            return new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)) * SpawnRadius;
        }

        void SyncWithLobby()
        {
            HashSet<string> inLobby = new HashSet<string>();
            foreach (LobbyServer.Player lp in m_lobby.Players) inLobby.Add(lp.name);

            foreach (string name in new List<string>(m_players.Keys))
                if (!inLobby.Contains(name)) { m_players.Remove(name); Debug.Log("[GAME SERVER] " + name + " left"); }

            foreach (string name in inLobby)
                if (!m_players.ContainsKey(name))
                {
                    Player p = new Player { name = name };
                    Respawn(p);
                    m_players.Add(name, p);
                }
        }

        Player FindByEndpoint(EndPoint endpoint)
        {
            foreach (Player p in m_players.Values) if (endpoint.Equals(p.endpoint)) return p;
            return null;
        }

        List<string> SortedNames()
        {
            List<string> names = new List<string>(m_players.Keys);
            names.Sort(StringComparer.Ordinal);
            return names;
        }
    }
}
