using UnityEngine;

namespace Deliverable3
{
    public class GameUI : MonoBehaviour
    {
        GameClient m_client;
        string m_error = "";
        string m_chatInput = "";
        GUIStyle m_bigStyle;

        public void Begin(GameClient client, string error)
        {
            m_client = client;
            m_error = error;
        }

        void OnGUI()
        {
            LobbySession session = LobbySession.Instance;
            LobbyClient lobby = session.Client;
            if (lobby == null || m_client == null) return;

            // Un clic fora del xat li treu el teclat; si el clic és al xat, el camp el recupera.
            if (Event.current.type == EventType.MouseDown) GUIUtility.keyboardControl = 0;

            LobbyGUI.Begin("GAME  (lobby " + session.protocol + ", game UDP)   " + lobby.RoomInfo +
                           "    ping " + (lobby.PingMs < 0 ? "-" : lobby.PingMs + " ms"));
            LobbyGUI.Message(m_error);

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(330));
            if (!m_client.HasState) GUILayout.Label("Waiting for the game server...");
            else DrawScores(m_client.State);
            GUILayout.EndVertical();

            GUILayout.Label("WASD move, Space jump, C crouch, mouse aim, click shoot. First to 5 kills wins.");
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(460));
            m_chatInput = LobbyGUI.Chat(lobby, m_chatInput, 190);
            if (GUILayout.Button(session.IsHost ? "End game" : "Leave", GUILayout.Width(160), GUILayout.Height(36)))
                lobby.Leave();
            GUILayout.EndVertical();
            LobbyGUI.End();

            if (m_client.HasState && m_client.State.phase == GamePhase.GameOver) DrawWinner(m_client.State);
        }

        void DrawScores(StateDTO state)
        {
            foreach (PlayerStateDTO p in state.players)
            {
                string line = p.name + (p.name == m_client.PlayerName ? " (you)" : "") +
                              "   kills " + p.score + "   " +
                              (p.Has(PlayerStateDTO.Alive) ? "hp " + p.health : "respawning");
                GUILayout.Label(line);
            }
        }

        void DrawWinner(StateDTO state)
        {
            if (m_bigStyle == null)
                m_bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
            string text = state.winner + " wins!\nNew round in " + Mathf.CeilToInt(state.restartIn) + " s";
            GUI.Label(new Rect(0f, Screen.height / 2f - 80f, Screen.width, 160f), text, m_bigStyle);
        }
    }
}
