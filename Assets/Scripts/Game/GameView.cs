using System.Collections.Generic;
using UnityEngine;

namespace Deliverable3
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] PlayerAvatar playerPrefab;
        [SerializeField] GameObject tracerPrefab;

        const float SmoothRate = 20f;
        const float SnapDistance = 3f;
        const float TracerLength = 25f;
        const float TracerSeconds = 0.08f;

        readonly Dictionary<string, PlayerAvatar> m_avatars = new Dictionary<string, PlayerAvatar>();
        GameClient m_client;
        float m_lastYaw;
        GUIStyle m_labelStyle;

        public void Begin(GameClient client)
        {
            m_client = client;
            if (viewCamera == null) viewCamera = Camera.main;
        }

        void Update()
        {
            if (m_client == null) return;
            ReadInput();
            if (m_client.HasState) ShowState(m_client.State);
        }

        void ReadInput()
        {
            InputDTO input = new InputDTO { aimYaw = AimYaw() };

            // Mentre s'escriu al xat, les tecles no mouen el jugador.
            if (GUIUtility.keyboardControl == 0)
            {
                input.moveX = PlayerInput.MoveX;
                input.moveZ = PlayerInput.MoveZ;
                if (PlayerInput.Jump) input.buttons |= InputDTO.Jump;
                if (PlayerInput.Crouch) input.buttons |= InputDTO.Crouch;
                if (PlayerInput.Fire) input.buttons |= InputDTO.Fire;
            }
            m_client.input = input;
        }

        float AimYaw()
        {
            PlayerAvatar me;
            if (viewCamera == null || !m_avatars.TryGetValue(m_client.PlayerName, out me)) return m_lastYaw;

            Ray ray = viewCamera.ScreenPointToRay(PlayerInput.MousePosition);
            if (ray.direction.y >= -0.01f) return m_lastYaw;
            Vector3 onFloor = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);

            Vector3 toMouse = onFloor - me.transform.position;
            toMouse.y = 0f;
            if (toMouse.sqrMagnitude > 0.04f) m_lastYaw = Mathf.Atan2(toMouse.x, toMouse.z) * Mathf.Rad2Deg;
            return m_lastYaw;
        }

        void ShowState(StateDTO state)
        {
            HashSet<string> present = new HashSet<string>();
            float blend = 1f - Mathf.Exp(-SmoothRate * Time.deltaTime);

            foreach (PlayerStateDTO p in state.players)
            {
                present.Add(p.name);
                PlayerAvatar avatar;
                bool isNew = !m_avatars.TryGetValue(p.name, out avatar);
                if (isNew) { avatar = CreateAvatar(p.name); avatar.LastShots = p.shots; m_avatars.Add(p.name, avatar); }

                bool alive = p.Has(PlayerStateDTO.Alive);
                avatar.gameObject.SetActive(alive);
                if (!alive) continue;

                Transform t = avatar.transform;
                Quaternion rotation = Quaternion.Euler(0f, p.yaw, 0f);
                if (isNew || Vector3.Distance(t.position, p.position) > SnapDistance)
                {
                    t.position = p.position;
                    t.rotation = rotation;
                }
                else
                {
                    t.position = Vector3.Lerp(t.position, p.position, blend);
                    t.rotation = Quaternion.Slerp(t.rotation, rotation, blend);
                }
                avatar.SetCrouching(p.Has(PlayerStateDTO.Crouching));

                if (p.shots != avatar.LastShots)
                {
                    avatar.LastShots = p.shots;
                    ShowTracer(avatar);
                }
            }

            foreach (string name in new List<string>(m_avatars.Keys))
                if (!present.Contains(name)) { Destroy(m_avatars[name].gameObject); m_avatars.Remove(name); }
        }

        PlayerAvatar CreateAvatar(string name)
        {
            PlayerAvatar avatar = Instantiate(playerPrefab);
            avatar.name = "Player " + name;
            avatar.SetColors(ColorOf(name), name == m_client.PlayerName ? Color.white : Color.black);
            return avatar;
        }

        void ShowTracer(PlayerAvatar avatar)
        {
            if (tracerPrefab == null) return;
            Vector3 forward = avatar.transform.forward;
            GameObject tracer = Instantiate(tracerPrefab, avatar.GunPosition + forward * (TracerLength / 2f), avatar.transform.rotation);
            Vector3 scale = tracer.transform.localScale;
            tracer.transform.localScale = new Vector3(scale.x, scale.y, TracerLength);
            Destroy(tracer, TracerSeconds);
        }

        // El mateix nom dona el mateix color a tots els PCs.
        static Color ColorOf(string name)
        {
            int hash = 17;
            foreach (char c in name) hash = hash * 31 + c;
            return Color.HSVToRGB((hash & 0xFFFF) / 65535f, 0.7f, 0.9f);
        }

        void OnGUI()
        {
            if (m_client == null || !m_client.HasState || viewCamera == null) return;
            if (m_labelStyle == null)
                m_labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };

            foreach (PlayerStateDTO p in m_client.State.players)
            {
                PlayerAvatar avatar;
                if (!p.Has(PlayerStateDTO.Alive) || !m_avatars.TryGetValue(p.name, out avatar)) continue;
                Vector3 screen = viewCamera.WorldToScreenPoint(avatar.transform.position + Vector3.up * 2.4f);
                if (screen.z <= 0f) continue;
                string text = p.name + (p.name == m_client.PlayerName ? " (you)" : "") + "  " + p.health;
                GUI.Label(new Rect(screen.x - 80f, Screen.height - screen.y - 12f, 160f, 24f), text, m_labelStyle);
            }
        }
    }
}
