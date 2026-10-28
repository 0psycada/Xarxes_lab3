using UnityEngine;

namespace Deliverable3
{
    [RequireComponent(typeof(GameView), typeof(GameUI))]
    public class GameManager : MonoBehaviour
    {
        void Start()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient lobby = session.Client;
            int gamePort = lobby.port + 1;
            string error = "";

            if (session.IsHost && !gameObject.AddComponent<GameServer>().Begin(session.Server, gamePort))
                error = "Could not open the game server: UDP port " + gamePort + " is already in use";

            GameClient client = gameObject.AddComponent<GameClient>();
            if (!client.Begin(lobby.serverIp, gamePort, lobby.userName))
                error = "Could not open the game client socket";

            GetComponent<GameView>().Begin(client);
            GetComponent<GameUI>().Begin(client, error);
        }
    }
}
