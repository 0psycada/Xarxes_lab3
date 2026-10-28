using UnityEngine;

namespace Deliverable3
{
    public class WaitingRoomUI : MonoBehaviour
    {
        string m_chatInput = "";

        void OnGUI()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LobbyGUI.Begin("WAITING ROOM  (" + session.protocol + ")   " + client.RoomInfo);
            GUILayout.BeginHorizontal();
            GUILayout.Label("You are " + client.userName + (session.IsHost ? " (host)" : "") +
                            "    ping " + (client.PingMs < 0 ? "-" : client.PingMs + " ms"), GUILayout.Width(420));
            int level = LobbyGUI.IntField("Your level", session.playerLevel, 1, 100);
            if (level != session.playerLevel)
            {
                session.playerLevel = level;
                client.UpdateData(session.MakePlayerData());
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawPlayers(session, client);
            GUILayout.Space(15);
            m_chatInput = LobbyGUI.Chat(client, m_chatInput, Screen.height - 200);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (session.IsHost && GUILayout.Button("Start game", GUILayout.Width(180), GUILayout.Height(40)))
                session.Server.StartGame();
            if (GUILayout.Button(session.IsHost ? "Close room" : "Leave", GUILayout.Width(160), GUILayout.Height(40)))
                client.Leave();
            GUILayout.EndHorizontal();

            LobbyGUI.End();
        }

        static void DrawPlayers(LobbySession session, LobbyClient client)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(460), GUILayout.Height(Screen.height - 200));
            GUILayout.Label("Players (" + client.Players.Count +
                            (session.IsHost ? "/" + session.Server.maxPlayers : "") + ")");
            foreach (string name in client.Players)
            {
                GUILayout.BeginHorizontal();
                string line = name + (name == client.userName ? " (you)" : "");
                LobbyServer.Player hosted = session.IsHost ? FindHosted(session, name) : null;
                if (hosted != null) line += "   " + (hosted.pingMs < 0 ? "-" : hosted.pingMs + " ms");
                GUILayout.Label(line, GUILayout.Width(300));
                bool kick = hosted != null && name != client.userName && GUILayout.Button("Kick", GUILayout.Width(70));
                GUILayout.EndHorizontal();

                PlayerDTO data;
                GUILayout.Label("    " + (client.PlayerData.TryGetValue(name, out data) ? Messages.Describe(data) : "..."));

                if (kick)
                {
                    session.Server.Kick(hosted);
                    break;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        static LobbyServer.Player FindHosted(LobbySession session, string name)
        {
            foreach (LobbyServer.Player p in session.Server.Players) if (p.name == name) return p;
            return null;
        }
    }
}
