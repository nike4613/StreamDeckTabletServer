using System;
using System.Collections.Generic;
using Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Networking.DataBlocks
{
    public static class ButtonBlockManager
    {
        private static bool hasRegistered = false;

        public static void RegisterUpdateTypes()
        {
            if (!hasRegistered) {
                hasRegistered = true;

                NetDataBlock.RegisterBlock(typeof(ButtonUpdateBlock), Guid.NewGuid());
                NetDataBlock.RegisterBlock(typeof(ButtonSizeUpdateBlock), Guid.NewGuid());
            }
        }
    }

    [NetworkDataBlock("ButtonUpdate")]
    public class ButtonUpdateBlock : NetDataBlock
    {
        static ButtonUpdateBlock()
        {
            ButtonBlockManager.RegisterUpdateTypes();
        }

        public int buttonX;
        public int buttonY;

        public static Dictionary<string, PropertyCallbacks> GetProperties()
        {
            return new Dictionary<string, PropertyCallbacks>()
            {
                {"x_pos",
                    new PropertyCallbacks<ButtonUpdateBlock>() {
                        Serializer = self => BitConverter.GetBytes(self.buttonX),
                        Deserializer = (self, bytes) => self.buttonX = BitConverter.ToInt32(bytes, 0),
                    }
                },
                {"y_pos",
                    new PropertyCallbacks<ButtonUpdateBlock>() {
                        Serializer = self => BitConverter.GetBytes(self.buttonY),
                        Deserializer = (self, bytes) => self.buttonY = BitConverter.ToInt32(bytes, 0),
                    }
                },
            };
        }

        public static ButtonUpdateBlock Create()
        {
            return new ButtonUpdateBlock();
        }
    }

    [NetworkDataBlock("ButtonSizeUpdate")]
    public class ButtonSizeUpdateBlock : ButtonUpdateBlock
    {
        static ButtonSizeUpdateBlock()
        {
            ButtonBlockManager.RegisterUpdateTypes();
        }

        public int newSizeW;
        public int newSizeH;

        public new static Dictionary<string, PropertyCallbacks> GetProperties()
        {
            return ButtonUpdateBlock.GetProperties().MergeLeft(new Dictionary<string, PropertyCallbacks>()
            {
                {"new_width",
                    new PropertyCallbacks<ButtonSizeUpdateBlock>() {
                        Serializer = self => BitConverter.GetBytes(self.newSizeW),
                        Deserializer = (self, bytes) => self.newSizeW = BitConverter.ToInt32(bytes, 0),
                    }
                },
                {"new_height",
                    new PropertyCallbacks<ButtonSizeUpdateBlock>() {
                        Serializer = self => BitConverter.GetBytes(self.newSizeH),
                        Deserializer = (self, bytes) => self.newSizeH = BitConverter.ToInt32(bytes, 0),
                    }
                },
            });
        }

        public new static ButtonSizeUpdateBlock Create()
        {
            return new ButtonSizeUpdateBlock();
        }
    }
}
