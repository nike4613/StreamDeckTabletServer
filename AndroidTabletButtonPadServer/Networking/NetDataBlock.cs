using Nito.KitchenSink.CRC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidTabletButtonPadServer.Networking
{
    public abstract class NetDataBlock
    {
        private Guid TypeGUID;
        private Dictionary<byte[], Func<byte[]>> CRCSerializer = new Dictionary<byte[], Func<byte[]>>();
        private byte[] serializedHeader = Encoding.ASCII.GetBytes("NET-dObj");

        public NetDataBlock(Guid guid)
        {
            TypeGUID = guid;

            var props = GetProperties();
            foreach (var e in props) {
                CRC32 crc = new CRC32();
                crc.ComputeHash(Encoding.ASCII.GetBytes(e.Key));
                CRCSerializer.Add(crc.Hash, e.Value);
            }
        }

        /// <summary>
        /// Gets properties and functions to serialize. 
        /// Must be able to be called when default constructor is called.
        /// </summary>
        /// <returns>a mapping of property to serializer</returns>
        public abstract Dictionary<string, Func<byte[]>> GetProperties(); 

        public byte[] Serialize()
        {
            var stream = new MemoryStream();
            var binwrite = new BinaryWriter(stream);

            binwrite.Write(serializedHeader); // write header
            binwrite.Write(TypeGUID.ToByteArray()); // write guid
            foreach(var prop in CRCSerializer)
            {
                binwrite.Write(prop.Key); // write CRC
                byte[] serialized = prop.Value(); // get data
                binwrite.Write((ulong)serialized.LongLength); // write data length
                binwrite.Write(serialized); // write data;
            }

            return stream.ToArray();
        }
    }
}
