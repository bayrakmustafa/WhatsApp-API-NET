/** 
 * Copyright (C) 2015 smndtrl
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Util
{
    public class ByteUtil
    {

        public static byte[] Combine(params byte[][] elements)
        {
            try
            {
                MemoryStream baos = new MemoryStream();

                foreach (byte[] element in elements)
                {
                    baos.Write(element, 0, element.Length);
                }

                return baos.ToArray();
            }
            catch (IOException e)
            {
                throw new Exception(e.Message);
            }
        }

        public static byte[][] Split(byte[] input, int firstLength, int secondLength)
        {
            byte[][] parts = new byte[2][];

            parts[0] = new byte[firstLength];
            Buffer.BlockCopy(input, 0, parts[0], 0, firstLength);

            parts[1] = new byte[secondLength];
            Buffer.BlockCopy(input, firstLength, parts[1], 0, secondLength);

            return parts;
        }

        public static byte[][] Split(byte[] input, int firstLength, int secondLength, int thirdLength)
        {
            if (input == null || firstLength < 0 || secondLength < 0 || thirdLength < 0 ||
                input.Length < firstLength + secondLength + thirdLength)
            {
                throw new Exception("Input too small: " + (input == null ? null : string.Join(",", input)));
            }

            byte[][] parts = new byte[3][];

            parts[0] = new byte[firstLength];
            Buffer.BlockCopy(input, 0, parts[0], 0, firstLength);

            parts[1] = new byte[secondLength];
            Buffer.BlockCopy(input, firstLength, parts[1], 0, secondLength);

            parts[2] = new byte[thirdLength];
            Buffer.BlockCopy(input, firstLength + secondLength, parts[2], 0, thirdLength);

            return parts;
        }

        public static byte[] Trim(byte[] input, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(input, 0, result, 0, result.Length);

            return result;
        }

        public static byte[] CopyFrom(byte[] input)
        {
            byte[] output = new byte[input.Length];
            Buffer.BlockCopy(input, 0, output, 0, output.Length);

            return output;
        }

        public static byte IntsToByteHighAndLow(int highValue, int lowValue)
        {
            return (byte)((highValue << 4 | lowValue) & 0xFF);
        }

        public static int HighBitsToInt(byte value)
        {
            return (value & 0xFF) >> 4;
        }

        public static int LowBitsToInt(byte value)
        {
            return (value & 0xF);
        }

        public static int HighBitsToMedium(int value)
        {
            return (value >> 12);
        }

        public static int LowBitsToMedium(int value)
        {
            return (value & 0xFFF);
        }

        public static byte[] ShortToByteArray(int value)
        {
            byte[] bytes = new byte[2];
            ShortToByteArray(bytes, 0, value);
            return bytes;
        }

        public static int ShortToByteArray(byte[] bytes, int offset, int value)
        {
            bytes[offset + 1] = (byte)value;
            bytes[offset] = (byte)(value >> 8);
            return 2;
        }

        public static int ShortToLittleEndianByteArray(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            return 2;
        }

        public static byte[] MediumToByteArray(int value)
        {
            byte[] bytes = new byte[3];
            MediumToByteArray(bytes, 0, value);
            return bytes;
        }

        public static int MediumToByteArray(byte[] bytes, int offset, int value)
        {
            bytes[offset + 2] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset] = (byte)(value >> 16);
            return 3;
        }

        public static byte[] IntToByteArray(int value)
        {
            byte[] bytes = new byte[4];
            IntToByteArray(bytes, 0, value);
            return bytes;
        }

        public static int IntToByteArray(byte[] bytes, int offset, int value)
        {
            bytes[offset + 3] = (byte)value;
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset] = (byte)(value >> 24);
            return 4;
        }

        public static int IntToLittleEndianByteArray(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
            return 4;
        }

        public static byte[] LongToByteArray(long l)
        {
            byte[] bytes = new byte[8];
            LongToByteArray(bytes, 0, l);
            return bytes;
        }

        public static int LongToByteArray(byte[] bytes, int offset, long value)
        {
            bytes[offset + 7] = (byte)value;
            bytes[offset + 6] = (byte)(value >> 8);
            bytes[offset + 5] = (byte)(value >> 16);
            bytes[offset + 4] = (byte)(value >> 24);
            bytes[offset + 3] = (byte)(value >> 32);
            bytes[offset + 2] = (byte)(value >> 40);
            bytes[offset + 1] = (byte)(value >> 48);
            bytes[offset] = (byte)(value >> 56);
            return 8;
        }

        public static int LongTo4ByteArray(byte[] bytes, int offset, long value)
        {
            bytes[offset + 3] = (byte)value;
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 0] = (byte)(value >> 24);
            return 4;
        }

        public static int ByteArrayToShort(byte[] bytes)
        {
            return ByteArrayToShort(bytes, 0);
        }

        public static int ByteArrayToShort(byte[] bytes, int offset)
        {
            return
                (bytes[offset] & 0xff) << 8 | (bytes[offset + 1] & 0xff);
        }

        // The SSL patented 3-byte Value.
        public static int ByteArrayToMedium(byte[] bytes, int offset)
        {
            return
                (bytes[offset] & 0xff) << 16 |
                    (bytes[offset + 1] & 0xff) << 8 |
                    (bytes[offset + 2] & 0xff);
        }

        public static int ByteArrayToInt(byte[] bytes)
        {
            return ByteArrayToInt(bytes, 0);
        }

        public static int ByteArrayToInt(byte[] bytes, int offset)
        {
            return
                (bytes[offset] & 0xff) << 24 |
                    (bytes[offset + 1] & 0xff) << 16 |
                    (bytes[offset + 2] & 0xff) << 8 |
                    (bytes[offset + 3] & 0xff);
        }

        public static int ByteArrayToIntLittleEndian(byte[] bytes, int offset)
        {
            return
                (bytes[offset + 3] & 0xff) << 24 |
                    (bytes[offset + 2] & 0xff) << 16 |
                    (bytes[offset + 1] & 0xff) << 8 |
                    (bytes[offset] & 0xff);
        }

        public static long ByteArrayToLong(byte[] bytes)
        {
            return ByteArrayToLong(bytes, 0);
        }

        public static long ByteArray4ToLong(byte[] bytes, int offset)
        {
            return
                ((bytes[offset + 0] & 0xffL) << 24) |
                    ((bytes[offset + 1] & 0xffL) << 16) |
                    ((bytes[offset + 2] & 0xffL) << 8) |
                    ((bytes[offset + 3] & 0xffL));
        }

        public static long ByteArrayToLong(byte[] bytes, int offset)
        {
            return
                ((bytes[offset] & 0xffL) << 56) |
                    ((bytes[offset + 1] & 0xffL) << 48) |
                    ((bytes[offset + 2] & 0xffL) << 40) |
                    ((bytes[offset + 3] & 0xffL) << 32) |
                    ((bytes[offset + 4] & 0xffL) << 24) |
                    ((bytes[offset + 5] & 0xffL) << 16) |
                    ((bytes[offset + 6] & 0xffL) << 8) |
                    ((bytes[offset + 7] & 0xffL));
        }

    }
}
