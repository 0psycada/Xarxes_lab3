using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deliverable3
{
    public class LobbySession : MonoBehaviour
    {
        public const string MenuScene = "D3_Menu";
        public const string CreateScene = "D3_CreateGame";
        public const string JoinScene = "D3_JoinGame";
        public const string RoomScene = "D3_WaitingRoom";
        public const string GameScene = "D3_Game";

        public LobbyProtocol protocol = LobbyProtocol.TCP;
        public string playerName = "Player";
        public int playerLevel = 12;
        public string message = "";

        public LobbyServer Server { get; private set; }
        public LobbyClient Client { get; private set; }
        public bool IsHost { get { return Server != null; } }

        static LobbySession s_instance;

        public static LobbySession Instance
        {
            get
            {
                if (s_instance == null)
                {
                    GameObject go = new GameObject("LobbySession");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<LobbySession>();
                }
                return s_instance;
            }
        }

        public static void Log(string text) { Debug.Log(text); }

        void Awake()
        {
            // Sense això, la finestra sense focus deixa de fer PING i el servidor ens expulsa.
            Application.runInBackground = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Log("[SCENE " + playerName + "] " + scene.name); }

        public bool Host(string serverName, int port, int maxPlayers)
        {
            EndSession();
            Server = protocol == LobbyProtocol.TCP
                ? (LobbyServer)gameObject.AddComponent<TCPLobbyServer>()
                : gameObject.AddComponent<UDPLobbyServer>();
            Server.serverName = serverName;
            Server.port = port;
            Server.maxPlayers = maxPlayers;

            if (!Server.StartServer())
            {
                Destroy(Server); Server = null;
                message = "Could not create the game: port " + port + " is already in use";
                return false;
            }

            Join("127.0.0.1", port);
            return true;
        }

        public void Join(string ip, int port)
        {
            if (Client != null) { Destroy(Client); Client = null; }
            message = "";

            Client = protocol == LobbyProtocol.TCP
                ? (LobbyClient)gameObject.AddComponent<TCPLobbyClient>()
                : gameObject.AddComponent<UDPLobbyClient>();
            Client.serverIp = ip.Trim();
            Client.port = port;
            Client.myData = MakePlayerData();

            Client.Joined += delegate { SceneManager.LoadScene(RoomScene); };
            Client.GameStarted += delegate { SceneManager.LoadScene(GameScene); };
            Client.Closed += OnClientClosed;
            Client.Connect();
        }

        public PlayerDTO MakePlayerData()
        {
            return new PlayerDTO { playerName = playerName.Trim(), level = playerLevel };
        }

        void OnClientClosed(string reason)
        {
            message = reason;
            EndSession();
            SceneManager.LoadScene(MenuScene);
        }

        public void EndSession()
        {
            if (Client != null) { Destroy(Client); Client = null; }
            if (Server != null) { Server.StopServer(); Destroy(Server); Server = null; }
        }
    }
}
