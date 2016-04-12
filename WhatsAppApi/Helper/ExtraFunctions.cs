using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppApi.Helper
{
    /// <summary>
    /// Not Used Yet
    /// </summary>
    public class ExtraFunctions
    {
        public static byte[] SerializeToBytes<T>(T item)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }

        public static object DeserializeFromBytes(byte[] bytes)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                return formatter.Deserialize(stream);
            }
        }

        public static byte[] GetBigEndianBytes(UInt32 val, bool isLittleEndian)
        {
            UInt32 bigEndian = val;
            if (isLittleEndian)
            {
                bigEndian = (val & 0x000000FFU) << 24 | (val & 0x0000FF00U) << 8 |
                     (val & 0x00FF0000U) >> 8 | (val & 0xFF000000U) >> 24;
            }
            return BitConverter.GetBytes(bigEndian);
        }

        public static byte[] IntToByteArray(int value)
        {
            byte[] b = new byte[4];
            //for (int i = 0; i >> offset) & 0xFF);
            return b;
        }

        public static void WritetoFile(string pathtofile, string data)
        {
            using (StreamWriter writer = new StreamWriter(pathtofile, true))
            {
                writer.WriteLine(data);
            }
        }
    }
}
