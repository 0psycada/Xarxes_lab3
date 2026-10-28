using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Deliverable3
{
    public abstract class LobbyClient : MonoBehaviour
    {
        public string serverIp = "127.0.0.1";
        public int port = 9050;
        public PlayerDTO myData;
        public float pingSeconds = 1f;

        public string userName { get { return myData.playerName; } }

        public readonly List<string> Players = new List<string>();
        public readonly Dictionary<string, PlayerDTO> PlayerData = new Dictionary<string, PlayerDTO>();
        public readonly List<string> Chat = new List<string>();
        public string RoomInfo = "";
        public int PingMs = -1;
        public bool InRoom { get; private set; }
        public bool InGame { get; private set; }

        public event Action Joined;
        public event Action GameStarted;
        public event Action<string> Closed;

        protected readonly ConcurrentQueue<byte[]> m_inbox = new ConcurrentQueue<byte[]>();
        protected volatile string m_endReason = "Connection to the server lost";
        protected float m_lastHeard;
        float m_lastPing;
        bool m_closed;

        public abstract void Connect();
        protected abstract void SendBytes(byte[] message);
        protected abstract void CloseSocket();

        protected void SendText(string text) { SendBytes(Messages.Text(text)); }

        protected byte[] JoinMessage() { return Messages.Player(myData); }

        public void SendChat(string line)
        {
            line = line.Trim();
            if (line.Length > 0) SendText("CHAT:" + line);
        }

        public void UpdateData(PlayerDTO data)
        {
            data.playerName = userName;
            myData = data;
            SendBytes(JoinMessage());
        }

        public void Leave()
        {
            SendText("LEAVE:");
            Close("You left the room");
        }

        protected virtual void Update()
        {
            byte[] data;
            while (!m_closed && m_inbox.TryDequeue(out data))
            {
                if (data == null) { Close(m_endReason); break; }
                m_lastHeard = Time.realtimeSinceStartup;

                string text;
                PlayerDTO dto;
                MsgType type;
                try { type = Messages.ReadLobby(data, out text, out dto); }
                catch (Exception e) { Log("[CLIENT " + userName + "] Dropped a bad message: " + e.Message); continue; }

                if (type == MsgType.Player) OnPlayerData(dto);
                else OnText(text);
            }

            if (!m_closed && InRoom && Time.realtimeSinceStartup - m_lastPing >= pingSeconds)
            {
                m_lastPing = Time.realtimeSinceStartup;
                SendText("PING:" + Environment.TickCount + "," + PingMs);
            }
        }

        void OnPlayerData(PlayerDTO dto)
        {
            PlayerDTO old;
            bool changed = !PlayerData.TryGetValue(dto.playerName, out old) || Messages.Describe(old) != Messages.Describe(dto);
            PlayerData[dto.playerName] = dto;
            if (changed) Log("[CLIENT " + userName + "] Data of " + dto.playerName + ": " + Messages.Describe(dto));
        }

        void OnText(string text)
        {
            int colon = text.IndexOf(':');
            string type = colon < 0 ? text : text.Substring(0, colon);
            string content = colon < 0 ? "" : text.Substring(colon + 1);

            switch (type)
            {
                case "PLAYERS":
                    Players.Clear();
                    if (content.Length > 0) Players.AddRange(content.Split(','));
                    foreach (string name in new List<string>(PlayerData.Keys))
                        if (!Players.Contains(name)) PlayerData.Remove(name);
                    Log("[CLIENT " + userName + "] Players (" + Players.Count + "): " + string.Join(", ", Players));
                    if (!InRoom) { InRoom = true; if (Joined != null) Joined(); }
                    break;

                case "ROOM":
                    RoomInfo = content;
                    break;

                case "CHAT":
                    Chat.Add(content);
                    if (Chat.Count > 50) Chat.RemoveAt(0);
                    Log("[CLIENT " + userName + "] Chat: " + content);
                    break;

                case "PONG":
                    int sent;
                    if (int.TryParse(content, out sent)) PingMs = Environment.TickCount - sent;
                    break;

                case "START":
                    // El servidor repeteix el START: amb cada PONG: només canviem d'escena el primer cop.
                    if (InGame) break;
                    InGame = true;
                    Log("[CLIENT " + userName + "] The host started the game");
                    if (GameStarted != null) GameStarted();
                    break;

                case "KICKED":
                    Close("The host kicked you out of the room");
                    break;

                case "ERROR":
                    Close(content);
                    break;
            }
        }

        protected void Close(string reason)
        {
            if (m_closed) return;
            m_closed = true;
            InRoom = false;
            CloseSocket();
            Log("[CLIENT " + userName + "] Closed: " + reason);
            if (Closed != null) Closed(reason);
        }

        void OnDestroy() { if (!m_closed) { m_closed = true; CloseSocket(); } }

        protected void Log(string message) { LobbySession.Log(message); }
    }
}
