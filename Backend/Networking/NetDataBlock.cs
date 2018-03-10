using Nito.KitchenSink.CRC;
using Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;

namespace Backend.Networking
{
    public static class NetDataBlocks
    {
        [JsonObject(MemberSerialization.OptIn)]
        internal class NetDataBlockDef
        {
            public string name;
            [JsonProperty]
            public Guid guid;
            public Func<NetDataBlock> createFunc;
            [JsonProperty]
            public HashSet<string> fields;
        }

        internal static Dictionary<Guid, NetDataBlockDef> dataBlocks = new Dictionary<Guid, NetDataBlockDef>();

        internal static void AddBlockDef(NetDataBlockDef def)
        {
            if (!dataBlocks.ContainsKey(def.guid))
            {
                dataBlocks.Add(def.guid, def);

                serializationDictOutdated = true;
            }
        }

        internal static bool serializationDictOutdated = false;
        internal static Dictionary<string, NetDataBlockDef> serializationDict = null;
        public static string SerializeObjectDefs()
        {
            if (serializationDict == null || serializationDictOutdated)
            {
                serializationDict = new Dictionary<string, NetDataBlockDef>();

                foreach (var kvp in dataBlocks)
                    serializationDict.Add(kvp.Value.name, kvp.Value);

                serializationDictOutdated = false;
            }

            return JsonConvert.SerializeObject(serializationDict, Formatting.None);
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

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NetworkDataBlockAttribute : Attribute
    {
        internal string name;

        public NetworkDataBlockAttribute(string name)
        {
            this.name = name;
        }
    }

    public abstract class NetDataBlock
    {
        private Guid TypeGUID;
        private Dictionary<UInt32, PropertyCallbacks> CRCSerializer = new Dictionary<UInt32, PropertyCallbacks>();
        protected internal static byte[] serializedHeader = Encoding.ASCII.GetBytes("NET-dObj");

        private static Dictionary<Type, Guid> guidLookup = new Dictionary<Type, Guid>();

        public static void RegisterBlock(Type type, Guid guid)
        {
            var attr = (NetworkDataBlockAttribute)type.GetCustomAttributes(typeof(NetworkDataBlockAttribute), false).First();

            var m = type.GetMethod("Create", BindingFlags.Static, null, new Type[] { }, new ParameterModifier[] { });
            if (!type.IsAssignableFrom(m.ReturnType))
            {
                throw new ArgumentException("Static method 'Create' on type " + type.ToString() + " does not return correct type!");
            }

            NetDataBlocks.NetDataBlockDef blockdef = new NetDataBlocks.NetDataBlockDef
            {
                name = attr.name,
                guid = guid,
                createFunc = (Func<NetDataBlock>) Delegate.CreateDelegate(typeof(Func<NetDataBlock>), m),
                fields = new HashSet<string>()
            };

            var props = GetPropertiesFor(type);
            foreach (var e in props)
                blockdef.fields.Add(e.Key);

            if (!NetDataBlocks.dataBlocks.ContainsKey(guid))
                NetDataBlocks.AddBlockDef(blockdef);

            guidLookup.Add(type, guid);
        }

        private static Dictionary<string, PropertyCallbacks> GetPropertiesFor(Type t)
        {
            var m = t.GetMethod("GetProperties", BindingFlags.Static, null, new Type[] { }, new ParameterModifier[] { });
            if (typeof(Dictionary<string, PropertyCallbacks>).IsAssignableFrom(m.ReturnType))
            {
                return (Dictionary<string, PropertyCallbacks>) m.Invoke(null, new object[] { });
            }
            else
            {
                throw new ArgumentException("Static method 'GetProperties' on type " + t.ToString() + " does not return correct type!");
            }
        }

        public NetDataBlock()
        {
            var guid = guidLookup[GetType()];

            TypeGUID = guid;

            var props = GetPropertiesFor(GetType());
            foreach (var e in props)
            {
                CRC32 crc = new CRC32();
                crc.ComputeHash(Encoding.ASCII.GetBytes(e.Key));
                CRCSerializer.Add(BitConverter.ToUInt32(crc.Hash, 0), e.Value);
            }
        }

        public sealed class PropertyCallbacks
        {
            public Func<NetDataBlock, byte[]> Serializer { get; set; }
            public Action<NetDataBlock, byte[]> Deserializer { get; set; }
        }

        /// <summary>
        /// Gets properties and functions to serialize. 
        /// Must be able to be called when default constructor is called.
        /// </summary>
        /// <returns>a mapping of property to serializer</returns>
        //protected abstract Dictionary<string, PropertyCallbacks> GetProperties(); 


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
