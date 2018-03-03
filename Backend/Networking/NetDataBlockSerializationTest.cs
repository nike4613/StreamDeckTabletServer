﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Networking
{
    public class NetDataBlockSerializationTest : NetDataBlock
    {
        static Guid guid = Guid.NewGuid();

        public NetDataBlockSerializationTest() : base(guid)
        {

        }

        protected override Dictionary<string, PropertyCallbacks> GetProperties()
        {
            return new Dictionary<string, PropertyCallbacks>()
            {
                { "f_str",
                    new PropertyCallbacks() {
                        Serializer = () => Encoding.ASCII.GetBytes(str),
                        Deserializer = (b) => str = Encoding.ASCII.GetString(b),
                    }
                },
                { "f_int",
                    new PropertyCallbacks() {
                        Serializer = () => BitConverter.GetBytes(data),
                        Deserializer = (b) => data = BitConverter.ToInt32(b,0),
                    }
                },
            };
        }

        protected override Func<NetDataBlock> GetCreationFunc()
        {
            return () => new NetDataBlockSerializationTest();
        }

        protected override string GetWellKnownName()
        {
            return "SerializationTest";
        }

        public int data = 4135789;
        public string str = "I mean, if it works.";

    }
}
