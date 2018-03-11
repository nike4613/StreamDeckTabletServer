using Backend.Networking;
using Backend.Networking.DataBlocks;
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
            var toserial = new ButtonSizeUpdateBlock
            {
                buttonX = 0,
                buttonY = 0,
                newSizeH = 2,
                newSizeW = 3,
            };

            var json = NetDataBlocks.SerializeObjectDefs();

            var data = NetDataBlocks.Serialize(new NetDataBlock[] { toserial });

            var deserial = NetDataBlocks.Deserialize(data);
        }
    }
}
