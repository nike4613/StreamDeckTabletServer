using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidTabletButtonPadServer.Networking
{
    public class NetDataBlockSerializationTest : NetDataBlock
    {
        static Guid guid = Guid.NewGuid();

        public NetDataBlockSerializationTest() : base(guid)
        {

        }

        public override Dictionary<string, Func<byte[]>> GetProperties()
        {
            return new Dictionary<string, Func<byte[]>>()
            {
                { "string", ()=>{ return Encoding.ASCII.GetBytes(str); } },
                { "int", ()=>{ return BitConverter.GetBytes(data); } },
            };
        }

        public int data = 4135789;
        public string str = "I mean, if it works.";

    }
}
