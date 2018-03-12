using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Networking
{
    [NetworkDataBlock("SerializationTest")]
    public class NetDataBlockSerializationTest : NetDataBlock
    {
        static Guid guid = Guid.NewGuid();

        static NetDataBlockSerializationTest()
        {
            RegisterBlock(typeof(NetDataBlockSerializationTest), guid);
        }

        protected static Dictionary<string, PropertyCallbacks> GetProperties()
        {
            return new Dictionary<string, PropertyCallbacks>()
            {
                { "str",
                    new PropertyCallbacks<NetDataBlockSerializationTest>() {
                        Serializer = self => Encoding.ASCII.GetBytes(self.str),
                        Deserializer = (self, b) => self.str = Encoding.ASCII.GetString(b),
                    }
                },
                { "int",
                    new PropertyCallbacks<NetDataBlockSerializationTest>() {
                        Serializer = self => BitConverter.GetBytes(self.data),
                        Deserializer = (self, b) => self.data = BitConverter.ToInt32(b,0),
                    }
                },
            };
        }

        public static NetDataBlockSerializationTest Create()
        {
            return new NetDataBlockSerializationTest();
        }

        public int data = 4135789;
        public string str = "I mean, if it works.";

    }
}
