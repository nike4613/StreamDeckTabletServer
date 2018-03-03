﻿using Nito.KitchenSink.CRC;
using Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Networking
{
    public static class NetDataBlocks
    {
        internal class NetDataBlockDef
        {
            public Type type;
            public Guid guid;
            public Func<NetDataBlock> createFunc;
            public HashSet<string> fields;
        }

        internal static Dictionary<Guid, NetDataBlockDef> dataBlocks = new Dictionary<Guid, NetDataBlockDef>();

        internal static void AddBlockDef(NetDataBlockDef def)
        {
            if (!dataBlocks.ContainsKey(def.guid))
                dataBlocks.Add(def.guid, def);
        }

        public static NetDataBlock Construct(Guid guid)
        {
            return dataBlocks[guid].createFunc();
        }

        public static byte[] Serialize(NetDataBlock[] blocks)
        {
            var stream = new MemoryStream();

            Serialize(blocks, stream);

            return stream.ToArray();
        }

        public static void Serialize(NetDataBlock[] blocks, Stream ostream)
        {
            var bwrite = new BinaryWriter(ostream);

            foreach (var blk in blocks)
            {
                var bin = blk.Serialize();
                bwrite.Write(bin.LongLength);
                bwrite.Write(bin);
            }
        }

        public static NetDataBlock[] Deserialize(byte[] data)
        {
            return Deserialize(new MemoryStream(data));
        }

        public static NetDataBlock[] Deserialize(Stream istream)
        {
            List<NetDataBlock> blks = new List<NetDataBlock>();
            
            var bread = new BinaryReader(istream);

            while (istream.Position < istream.Length)
            {
                long len = bread.ReadInt64();
                if (len < NetDataBlock.serializedHeader.Length + 16)
                    throw new DeserializationException("Invalid block length");

                var spos = istream.Position;
                istream.Seek(NetDataBlock.serializedHeader.Length, SeekOrigin.Current);

                byte[] baGuid = new byte[16];
                istream.Read(baGuid, 0, baGuid.Length);

                byte[] deserialdata = new byte[len];
                istream.Seek(spos, SeekOrigin.Begin);
                istream.Read(deserialdata, 0, (int)len);

                var guid = new Guid(baGuid);
                var block = Construct(guid);

                block.Deserialize(deserialdata);

                blks.Add(block);
            }

            return blks.ToArray();
        }
    }

    public abstract class NetDataBlock
    {
        private Guid TypeGUID;
        private Dictionary<UInt32, PropertyCallbacks> CRCSerializer = new Dictionary<UInt32, PropertyCallbacks>();
        protected internal static byte[] serializedHeader = Encoding.ASCII.GetBytes("NET-dObj");

        public NetDataBlock(Guid guid)
        {
            TypeGUID = guid;

            NetDataBlocks.NetDataBlockDef blockdef = new NetDataBlocks.NetDataBlockDef
            {
                type = GetType(),
                guid = guid,
                createFunc = GetCreationFunc(),
                fields = new HashSet<string>()
            };

            var props = GetProperties();
            foreach (var e in props) {
                CRC32 crc = new CRC32();
                crc.ComputeHash(Encoding.ASCII.GetBytes(e.Key));
                CRCSerializer.Add(BitConverter.ToUInt32(crc.Hash, 0), e.Value);

                blockdef.fields.Add(e.Key);
            }

            if (!NetDataBlocks.dataBlocks.ContainsKey(guid))
                NetDataBlocks.AddBlockDef(blockdef);
        }

        protected abstract Func<NetDataBlock> GetCreationFunc();

        protected sealed class PropertyCallbacks
        {
            public Func<byte[]> Serializer { get; set; }
            public Action<byte[]> Deserializer { get; set; }
        }

        /// <summary>
        /// Gets properties and functions to serialize. 
        /// Must be able to be called when default constructor is called.
        /// </summary>
        /// <returns>a mapping of property to serializer</returns>
        protected abstract Dictionary<string, PropertyCallbacks> GetProperties(); 


        // serializes to little-endian
        public byte[] Serialize()
        {
            var stream = new MemoryStream();

            Serialize(stream);

            return stream.ToArray();
        }

        public void Serialize(Stream s)
        {
            var binwrite = new BinaryWriter(s);

            binwrite.Write(serializedHeader); // write header
            binwrite.Write(TypeGUID.ToByteArray()); // write guid
            foreach (var prop in CRCSerializer)
            {
                binwrite.Write(prop.Key); // write CRC
                byte[] serialized = prop.Value.Serializer(); // get data
                binwrite.Write(serialized.Length); // write data length
                binwrite.Write(serialized); // write data;
            }
        }

        public void Deserialize(byte[] data)
        {
            Deserialize(new MemoryStream(data)); 
        }

        public void Deserialize(Stream istream)
        {
            var binread = new BinaryReader(istream);

            byte[] head = new byte[serializedHeader.Length];
            binread.Read(head, 0, head.Length);
            if (!Util.ByteArrayCompare(head, serializedHeader))
            {
                throw new DeserializationException("Block header does not match");
            }

            byte[] guid = new byte[16];
            binread.Read(guid, 0, guid.Length);
            if (new Guid(guid) != TypeGUID)
            {
                throw new DeserializationException("GUID does not match");
            }

            while (istream.Position < istream.Length)
            {
                UInt32 crc = binread.ReadUInt32();
                int dataLen = binread.ReadInt32();
                var data = binread.ReadBytes(dataLen);

                CRCSerializer[crc].Deserializer(data);
            }
        }
    }

    public class DeserializationException : Exception
    {
        public DeserializationException(string message) : base(message)
        {
        }
    }
}
