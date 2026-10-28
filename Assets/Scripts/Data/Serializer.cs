using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Deliverable3
{
    // Llegir en el mateix ordre i amb els mateixos tipus que s'ha escrit: els bytes no diuen què són.
    public static class Serializer
    {
        public static byte[] Serialize(PlayerDTO dto)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(dto.playerName);
            writer.Write(dto.level);

            return stream.ToArray();
        }

        public static PlayerDTO DeserializePlayerDTO(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            PlayerDTO dto = new PlayerDTO();

            dto.playerName = reader.ReadString();
            dto.level = reader.ReadInt32();

            return dto;
        }

        public static byte[] Serialize(InputDTO dto)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(dto.sequence);
            writer.Write(dto.moveX);
            writer.Write(dto.moveZ);
            writer.Write(dto.aimYaw);
            writer.Write(dto.buttons);

            return stream.ToArray();
        }

        public static InputDTO DeserializeInputDTO(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            InputDTO dto = new InputDTO();

            dto.sequence = reader.ReadUInt32();
            dto.moveX = reader.ReadSingle();
            dto.moveZ = reader.ReadSingle();
            dto.aimYaw = reader.ReadSingle();
            dto.buttons = reader.ReadByte();

            return dto;
        }

        public static byte[] Serialize(StateDTO dto)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(dto.tick);
            writer.Write((byte)dto.phase);
            writer.Write(dto.winner ?? "");
            writer.Write(dto.restartIn);
            writer.Write((byte)dto.players.Count);
            foreach (PlayerStateDTO p in dto.players)
            {
                writer.Write(p.name);
                writer.Write(p.position.x);
                writer.Write(p.position.y);
                writer.Write(p.position.z);
                writer.Write(p.yaw);
                writer.Write(p.flags);
                writer.Write(p.health);
                writer.Write(p.score);
                writer.Write(p.shots);
            }

            return stream.ToArray();
        }

        public static StateDTO DeserializeStateDTO(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            StateDTO dto = new StateDTO();
            dto.players = new List<PlayerStateDTO>();

            dto.tick = reader.ReadUInt32();
            dto.phase = (GamePhase)reader.ReadByte();
            dto.winner = reader.ReadString();
            dto.restartIn = reader.ReadSingle();
            int count = reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                PlayerStateDTO p = new PlayerStateDTO();
                p.name = reader.ReadString();
                p.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                p.yaw = reader.ReadSingle();
                p.flags = reader.ReadByte();
                p.health = reader.ReadByte();
                p.score = reader.ReadByte();
                p.shots = reader.ReadByte();
                dto.players.Add(p);
            }

            return dto;
        }
    }
}
