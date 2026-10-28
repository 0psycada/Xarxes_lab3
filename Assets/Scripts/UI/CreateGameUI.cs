using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deliverable3
{
    public class CreateGameUI : MonoBehaviour
    {
        string m_roomName = "MyRoom";
        string m_port = "9050";
        int m_maxPlayers = 4;
        bool m_creating;

        void OnGUI()
        {
            LobbySession session = LobbySession.Instance;
            LobbyGUI.Begin("CREATE GAME  (" + session.protocol + ")");

            GUI.enabled = !m_creating;
            m_roomName = LobbyGUI.Field("Room name", m_roomName);
            m_port = LobbyGUI.Field("Port", m_port, 100);
            m_maxPlayers = LobbyGUI.IntField("Max players", m_maxPlayers, 2, 16);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            int port;
            GUI.enabled = !m_creating && int.TryParse(m_port, out port) && m_roomName.Trim().Length > 0;
            if (GUILayout.Button(m_creating ? "Creating..." : "Create", GUILayout.Width(160), GUILayout.Height(40)))
                m_creating = session.Host(m_roomName.Trim(), int.Parse(m_port), m_maxPlayers);
            GUI.enabled = true;
            if (GUILayout.Button("Back", GUILayout.Width(120), GUILayout.Height(40)))
            {
                session.EndSession();
                SceneManager.LoadScene(LobbySession.MenuScene);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            int lobbyPort;
            string gamePort = int.TryParse(m_port, out lobbyPort) ? (lobbyPort + 1).ToString() : "?";
            GUILayout.Label("Others join with your IP (ipconfig) and port " + m_port +
                            ". The game uses UDP port " + gamePort + ": allow the app in the firewall.");
            LobbyGUI.Message(session.message);
            LobbyGUI.End();
        }
    }
}
