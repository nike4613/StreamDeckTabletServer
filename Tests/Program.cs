using Backend.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var toserial = new NetDataBlockSerializationTest
            {
                str = "HBell;"
            };



            var data = NetDataBlocks.Serialize(new NetDataBlock[] { toserial, toserial });

            var deserial = NetDataBlocks.Deserialize(data);
        }
    }
}
