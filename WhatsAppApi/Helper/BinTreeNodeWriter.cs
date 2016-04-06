using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatsAppApi.Helper
{
    public class BinTreeNodeWriter
    {
        private List<byte> buffer;
        public KeyStream Key;

        public BinTreeNodeWriter()
        {
            buffer = new List<byte>();
        }

        public byte[] StartStream(string domain, string resource)
        {
            List<KeyValue> attributes = new List<KeyValue>();
            this.buffer = new List<byte>();

            attributes.Add(new KeyValue("to", domain));
            attributes.Add(new KeyValue("resource", resource));
            this.WriteListStart(attributes.Count * 2 + 1);

            this.buffer.Add(1);
            this.WriteAttributes(attributes.ToArray());

            byte[] ret = this.FlushBuffer();
            this.buffer.Add((byte)'W');
            this.buffer.Add((byte)'A');
            this.buffer.Add(0x1);
            this.buffer.Add(0x5);
            this.buffer.AddRange(ret);
            ret = buffer.ToArray();
            this.buffer = new List<byte>();
            return ret;
        }

        public byte[] Write(ProtocolTreeNode node, bool encrypt = true)
        {
            if (node == null)
            {
                this.buffer.Add((byte)'\x00');
            }
            else
            {
                if (WhatsApp.Debug && WhatsApp.DebugOutBound)
                    this.DebugPrint(node.NodeString("tx "));
                this.WriteInternal(node);
            }
            return this.FlushBuffer(encrypt);
        }

        protected byte[] FlushBuffer(bool encrypt = true)
        {
            byte[] data = this.buffer.ToArray();
            byte[] data2 = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, data2, 0, data.Length);

            byte[] size = this.GetInt24(data.Length);
            if (encrypt && this.Key != null)
            {
                byte[] paddedData = new byte[data.Length + 4];
                Array.Copy(data, paddedData, data.Length);
                this.Key.EncodeMessage(paddedData, paddedData.Length - 4, 0, paddedData.Length - 4);
                data = paddedData;

                //Add Encryption Signature
                uint encryptedBit = 0u;
                encryptedBit |= 8u;
                long dataLength = data.Length;
                size[0] = (byte)((ulong)((ulong)encryptedBit << 4) | (ulong)((dataLength & 16711680L) >> 16));
                size[1] = (byte)((dataLength & 65280L) >> 8);
                size[2] = (byte)(dataLength & 255L);
            }
            byte[] ret = new byte[data.Length + 3];
            Buffer.BlockCopy(size, 0, ret, 0, 3);
            Buffer.BlockCopy(data, 0, ret, 3, data.Length);
            this.buffer = new List<byte>();
            return ret;
        }

        protected void WriteAttributes(IEnumerable<KeyValue> attributes)
        {
            if (attributes != null)
            {
                foreach (KeyValue item in attributes)
                {
                    this.WriteString(item.Key);
                    this.WriteString(item.Value);
                }
            }
        }

        private byte[] GetInt16(int len)
        {
            byte[] ret = new byte[2];
            ret[0] = (byte)((len & 0xff00) >> 8);
            ret[1] = (byte)(len & 0x00ff);
            return ret;
        }

        private byte[] GetInt24(int len)
        {
            byte[] ret = new byte[3];
            ret[0] = (byte)((len & 0xf0000) >> 16);
            ret[1] = (byte)((len & 0xff00) >> 8);
            ret[2] = (byte)(len & 0xff);
            return ret;
        }

        protected void WriteBytes(string bytes)
        {
            WriteBytes(WhatsApp.SysEncoding.GetBytes(bytes));
        }

        protected void WriteBytes(byte[] bytes)
        {
            int len = bytes.Length;
            if (len >= 0x100)
            {
                this.buffer.Add(0xfd);
                this.WriteInt24(len);
            }
            else
            {
                this.buffer.Add(0xfc);
                this.WriteInt8(len);
            }
            this.buffer.AddRange(bytes);
        }

        protected void WriteInt16(int v)
        {
            this.buffer.Add((byte)((v & 0xff00) >> 8));
            this.buffer.Add((byte)(v & 0x00ff));
        }

        protected void WriteInt24(int v)
        {
            this.buffer.Add((byte)((v & 0xff0000) >> 16));
            this.buffer.Add((byte)((v & 0x00ff00) >> 8));
            this.buffer.Add((byte)(v & 0x0000ff));
        }

        protected void WriteInt8(int v)
        {
            this.buffer.Add((byte)(v & 0xff));
        }

        protected void WriteInternal(ProtocolTreeNode node)
        {
            int len = 1;
            if (node.attributeHash != null)
            {
                len += node.attributeHash.Count() * 2;
            }
            if (node.children.Any())
            {
                len += 1;
            }
            if (node.data.Length > 0)
            {
                len += 1;
            }
            this.WriteListStart(len);
            this.WriteString(node.tag);
            this.WriteAttributes(node.attributeHash);
            if (node.data.Length > 0)
            {
                this.WriteBytes(node.data);
            }
            if (node.children != null && node.children.Any())
            {
                this.WriteListStart(node.children.Count());
                foreach (ProtocolTreeNode item in node.children)
                {
                    this.WriteInternal(item);
                }
            }
        }

        protected void WriteJid(string user, string server)
        {
            this.buffer.Add(0xfa);
            if (user.Length > 0)
            {
                this.WriteString(user);
            }
            else
            {
                this.WriteToken(0);
            }
            this.WriteString(server);
        }

        protected void WriteListStart(int len)
        {
            if (len == 0)
            {
                this.buffer.Add(0x00);
            }
            else if (len < 256)
            {
                this.buffer.Add(0xf8);
                this.WriteInt8(len);
            }
            else
            {
                this.buffer.Add(0xf9);
                this.WriteInt16(len);
            }
        }

        protected void WriteString(string tag)
        {
            int intValue = -1;
            int num = -1;
            if (new TokenDictionary().TryGetToken(tag, ref num, ref intValue))
            {
                if (num >= 0)
                {
                    this.WriteToken(num);
                }
                this.WriteToken(intValue);
                return;
            }
            int num2 = tag.IndexOf('@');
            if (num2 < 1)
            {
                this.WriteBytes(tag);
                return;
            }
            string server = tag.Substring(num2 + 1);
            string user = tag.Substring(0, num2);
            this.WriteJid(user, server);
        }

        protected void WriteToken(int token)
        {
            if (token < 0xf5)
            {
                this.buffer.Add((byte)token);
            }
            else if (token <= 0x1f4)
            {
                this.buffer.Add(0xfe);
                this.buffer.Add((byte)(token - 0xf5));
            }
        }

        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.Debug && debugMsg.Length > 0)
            {
                Helper.DebugAdapter.Instance.FireOnPrintDebug(debugMsg);
            }
        }
    }
}