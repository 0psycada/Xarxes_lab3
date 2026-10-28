using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Deliverable3
{
    public abstract class LobbyServer : MonoBehaviour
    {
        public string serverName = "MyRoom";
        public int port = 9050;
        public int maxPlayers = 4;

        public class Player
        {
            public object connection;
            public PlayerDTO data;
            public int pingMs = -1;
            public float lastSeen;
            public string name { get { return data.playerName; } }
        }

        protected struct NetEvent { public object from; public byte[] data; }
        protected readonly ConcurrentQueue<NetEvent> m_inbox = new ConcurrentQueue<NetEvent>();

        readonly List<Player> m_players = new List<Player>();

        public IList<Player> Players { get { return m_players.AsReadOnly(); } }
        public bool Started { get; private set; }
        public abstract bool IsRunning { get; }

        public abstract bool StartServer();
        public abstract void StopServer();
        protected abstract void Send(object connection, byte[] message);
        protected abstract void Drop(object connection);
        protected virtual string Describe(object connection) { return connection.ToString(); }

        protected void SendText(object connection, string text) { Send(connection, Messages.Text(text)); }

        protected virtual void Update()
        {
            NetEvent e;
            while (m_inbox.TryDequeue(out e))
            {
                if (e.data == null) { OnConnectionLost(e.from); continue; }

                string text;
                PlayerDTO dto;
                MsgType type;
                try { type = Messages.ReadLobby(e.data, out text, out dto); }
                catch (Exception ex)
                {
                    Log("[SERVER] Dropped a bad message from " + Describe(e.from) + ": " + ex.Message);
                    continue;
                }

                Player player = Find(e.from);
                if (player != null) player.lastSeen = Time.realtimeSinceStartup;

                if (type == MsgType.Player) OnPlayerData(e.from, player, dto);
                else OnText(e.from, player, text);
            }
        }

        void OnText(object from, Player player, string text)
        {
            int colon = text.IndexOf(':');
            string type = colon < 0 ? text : text.Substring(0, colon);
            string content = colon < 0 ? "" : text.Substring(colon + 1);

            switch (type)
            {
                case "CHAT":
                    if (player == null) break;
                    Log("[CHAT] " + player.name + ": " + content);
                    Broadcast("CHAT:" + player.name + ": " + content);
                    break;

                case "PING":
                    if (player == null) break;
                    string[] parts = content.Split(',');
                    SendText(from, "PONG:" + parts[0]);
                    // Si el START: s'ha perdut (UDP), es repara al següent PING.
                    if (Started) SendText(from, "START:");
                    int ping;
                    if (parts.Length > 1 && int.TryParse(parts[1], out ping)) player.pingMs = ping;
                    break;

                case "LEAVE":
                    if (player != null) RemovePlayer(player, "left");
                    Drop(from);
                    break;
            }
        }

        void OnPlayerData(object from, Player player, PlayerDTO dto)
        {
            string name = (dto.playerName ?? "").Replace(",", "").Replace(":", "").Trim();
            if (name.Length == 0) name = "Player";
            dto.playerName = name;

            if (player != null)
            {
                if (dto.playerName != player.name) { SendRoomStateTo(from); return; }
                if (Messages.Describe(dto) == Messages.Describe(player.data)) { SendRoomStateTo(from); return; }
                player.data = dto;
                Log("[SERVER] " + name + " updated: " + Messages.Describe(dto));
                SendRoomState();
                return;
            }

            if (Started)                       { Refuse(from, "The game has already started"); return; }
            if (m_players.Count >= maxPlayers) { Refuse(from, "The room is full (" + maxPlayers + "/" + maxPlayers + ")"); return; }
            if (FindByName(name) != null)      { Refuse(from, "The name " + name + " is already taken"); return; }

            m_players.Add(new Player { connection = from, data = dto, lastSeen = Time.realtimeSinceStartup });
            Log("[SERVER] " + name + " joined (" + Describe(from) + "): " + Messages.Describe(dto));
            BroadcastSystem(name + " joined");
            SendRoomState();
        }

        void Refuse(object to, string reason)
        {
            Log("[SERVER] Refused " + Describe(to) + ": " + reason);
            SendText(to, "ERROR:" + reason);
            Drop(to);
        }

        void OnConnectionLost(object connection)
        {
            Player player = Find(connection);
            if (player != null) RemovePlayer(player, "disconnected");
        }

        protected void RemovePlayer(Player player, string reason)
        {
            if (!m_players.Remove(player)) return;
            Log("[SERVER] " + player.name + " " + reason);
            BroadcastSystem(player.name + " " + reason);
            SendRoomState();
        }

        public void Kick(Player player)
        {
            object connection = player.connection;
            SendText(connection, "KICKED:");
            RemovePlayer(player, "was kicked");
            Drop(connection);
        }

        public void StartGame()
        {
            if (Started) return;
            Started = true;
            Log("[SERVER] Game started with " + Names(", "));
            Broadcast("START:");
        }

        void SendRoomState()
        {
            Log("[SERVER] Players (" + m_players.Count + "/" + maxPlayers + "): " + Names(", "));
            foreach (Player p in m_players.ToArray()) SendRoomStateTo(p.connection);
        }

        void SendRoomStateTo(object connection)
        {
            foreach (Player p in m_players.ToArray()) Send(connection, Messages.Player(p.data));
            SendText(connection, "PLAYERS:" + Names(","));
            SendText(connection, "ROOM:" + serverName + "   " + m_players.Count + "/" + maxPlayers + " players");
        }

        void BroadcastSystem(string text) { Broadcast("CHAT:* " + text); }

        protected void Broadcast(string text)
        {
            byte[] message = Messages.Text(text);
            foreach (Player p in m_players.ToArray()) Send(p.connection, message);
        }

        public string Names(string separator)
        {
            List<string> names = new List<string>();
            foreach (Player p in m_players) names.Add(p.name);
            return string.Join(separator, names);
        }

        protected Player Find(object connection)
        {
            foreach (Player p in m_players) if (p.connection.Equals(connection)) return p;
            return null;
        }

        Player FindByName(string name)
        {
            foreach (Player p in m_players) if (p.name == name) return p;
            return null;
        }

        protected void ClearPlayers() { m_players.Clear(); Started = false; }

        protected void Log(string message) { LobbySession.Log(message); }
    }
}
