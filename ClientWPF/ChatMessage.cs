using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWPF
{
    public enum TypeMessage
    {
        Login,
        Logout,
        Message
    }
    public class ChatMessage
    {
        public TypeMessage MessageType;
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public int ImageSize { get; set; }
        public byte[] Image { get; set; }
        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using(BinaryWriter bw = new BinaryWriter(m))
                {
                    bw.Write((int)MessageType);
                    bw.Write(UserId);
                    bw.Write(UserName);
                    bw.Write(Text);
                    bw.Write(ImageSize);
                    bw.Write(Image);
                }
                return m.ToArray();
            }
        }
        public static ChatMessage Deserialize(byte[] data)
        {
            ChatMessage message = new ChatMessage();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(m))
                {
                    message.MessageType = (TypeMessage)br.ReadInt32();
                    message.UserId = br.ReadString();
                    message.UserName = br.ReadString();
                    message.Text = br.ReadString();
                    message.ImageSize = br.ReadInt32();
                    message.Image = br.ReadBytes(message.ImageSize);
                }
                
            }
            return message;
        }
    }
}
