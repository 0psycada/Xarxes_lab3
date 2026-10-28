using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deliverable3
{
    public class MenuUI : MonoBehaviour
    {
        void Start()
        {
            LobbySession.Instance.EndSession();
        }

        void OnGUI()
        {
            LobbySession session = LobbySession.Instance;
            LobbyGUI.Begin("XJO  -  Deliverable 3");

            session.playerName = LobbyGUI.Field("Your name", session.playerName);
            session.playerLevel = LobbyGUI.IntField("Level", session.playerLevel, 1, 100);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Protocol", GUILayout.Width(140));
            session.protocol = (LobbyProtocol)GUILayout.Toolbar((int)session.protocol, new[] { "TCP", "UDP" },
                                                                 GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUI.enabled = session.playerName.Trim().Length > 0;
            if (GUILayout.Button("Create game", GUILayout.Width(220), GUILayout.Height(40)))
                SceneManager.LoadScene(LobbySession.CreateScene);
            if (GUILayout.Button("Join game", GUILayout.Width(220), GUILayout.Height(40)))
                SceneManager.LoadScene(LobbySession.JoinScene);
            GUI.enabled = true;

            LobbyGUI.Message(session.message);
            LobbyGUI.End();
        }
    }
}
