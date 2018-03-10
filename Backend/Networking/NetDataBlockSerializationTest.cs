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
                { "f_str",
                    new PropertyCallbacks() {
                        Serializer = self => Encoding.ASCII.GetBytes(((NetDataBlockSerializationTest)self).str),
                        Deserializer = (self, b) => ((NetDataBlockSerializationTest)self).str = Encoding.ASCII.GetString(b),
                    }
                },
                { "f_int",
                    new PropertyCallbacks() {
                        Serializer = self => BitConverter.GetBytes(((NetDataBlockSerializationTest)self).data),
                        Deserializer = (self, b) => ((NetDataBlockSerializationTest)self).data = BitConverter.ToInt32(b,0),
                    }
                },
            };
        }

        protected static NetDataBlock Create()
        {
            return new NetDataBlockSerializationTest();
        }

        public int data = 4135789;
        public string str = "I mean, if it works.";

    }
}
