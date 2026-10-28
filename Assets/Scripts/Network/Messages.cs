using System;
using System.IO;

namespace Deliverable3
{
    public static class Messages
    {
        public static byte[] Text(string text) { return Wrap(MsgType.Text, StringBody(text)); }
        public static byte[] Player(PlayerDTO dto) { return Wrap(MsgType.Player, Serializer.Serialize(dto)); }
        public static byte[] GameJoin(string name) { return Wrap(MsgType.GameJoin, StringBody(name)); }
        public static byte[] Input(InputDTO dto) { return Wrap(MsgType.Input, Serializer.Serialize(dto)); }
        public static byte[] State(StateDTO dto) { return Wrap(MsgType.State, Serializer.Serialize(dto)); }

        public static MsgType Type(byte[] data)
        {
            if (data == null || data.Length < 1) throw new InvalidDataException("empty message");
            return (MsgType)data[0];
        }

        public static byte[] Body(byte[] data)
        {
            byte[] body = new byte[data.Length - 1];
            Array.Copy(data, 1, body, 0, body.Length);
            return body;
        }

        public static string ReadString(byte[] data)
        {
            return new BinaryReader(new MemoryStream(data, 1, data.Length - 1)).ReadString();
        }

        public static MsgType ReadLobby(byte[] data, out string text, out PlayerDTO player)
        {
            text = null;
            player = new PlayerDTO();
            MsgType type = Type(data);
            switch (type)
            {
                case MsgType.Text: text = ReadString(data); break;
                case MsgType.Player: player = Serializer.DeserializePlayerDTO(Body(data)); break;
                default: throw new InvalidDataException("not a lobby message: type " + data[0]);
            }
            return type;
        }

        public static string Describe(PlayerDTO dto)
        {
            return "level " + dto.level;
        }

        static byte[] Wrap(MsgType type, byte[] body)
        {
            byte[] message = new byte[1 + body.Length];
            message[0] = (byte)type;
            body.CopyTo(message, 1);
            return message;
        }

        static byte[] StringBody(string text)
        {
            MemoryStream stream = new MemoryStream();
            new BinaryWriter(stream).Write(text);
            return stream.ToArray();
        }
    }
}
