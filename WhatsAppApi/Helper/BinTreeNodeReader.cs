using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatsAppApi.Helper
{
    public class BinTreeNodeReader
    {
        public KeyStream Key;
        private List<byte> buffer;

        public BinTreeNodeReader()
        {
        }

        public void SetKey(byte[] key, byte[] mac)
        {
            this.Key = new KeyStream(key, mac);
        }

        public ProtocolTreeNode NextTree(byte[] pInput = null, bool useDecrypt = true)
        {
            if (pInput != null && pInput.Length > 0)
            {
                this.buffer = new List<byte>();
                this.buffer.AddRange(pInput);
            }

            int firstByte = this.PeekInt8();
            int stanzaFlag = (firstByte & 0xF0) >> 4;
            int stanzaSize = this.PeekInt16(1) | ((firstByte & 0x0F) << 16);

            int flags = stanzaFlag;
            int size = stanzaSize;

            this.ReadInt24();

            bool isEncrypted = (stanzaFlag & 8) != 0;

            if (isEncrypted)
            {
                if (this.Key != null)
                {
                    var realStanzaSize = stanzaSize - 4;
                    var macOffset = stanzaSize - 4;
                    var treeData = this.buffer.ToArray();
                    try
                    {
                        this.Key.DecodeMessage(treeData, macOffset, 0, realStanzaSize);
                    }
                    catch (Exception e)
                    {
                        Helper.DebugAdapter.Instance.FireOnPrintDebug(e);
                    }
                    this.buffer.Clear();
                    this.buffer.AddRange(treeData.Take(realStanzaSize).ToArray());
                }
                else
                {
                    throw new Exception("Received encrypted message, encryption key not set");
                }
            }

            if (stanzaSize > 0)
            {
                ProtocolTreeNode node = this.NextTreeInternal();
                if (node != null)
                    this.DebugPrint(node.NodeString("rx "));
                return node;
            }

            return null;
        }

        protected string GetToken(int token)
        {
            string tokenString = null;
            int num = -1;
            new TokenDictionary().GetToken(token, ref num, ref tokenString);
            if (tokenString == null)
            {
                token = ReadInt8();
                new TokenDictionary().GetToken(token, ref num, ref tokenString);
            }
            return tokenString;
        }

        protected byte[] ReadBytes(int token)
        {
            byte[] ret = new byte[0];
            if (token == -1)
            {
                throw new Exception("BinTreeNodeReader->readString: Invalid token " + token);
            }
            if ((token > 2) && (token < 245))
            {
                ret = WhatsApp.SysEncoding.GetBytes(this.GetToken(token));
            }
            else if (token == 0)
            {
                ret = new byte[0];
            }
            else if (token == 252)
            {
                int size = this.ReadInt8();
                ret = this.FillArray(size);
            }
            else if (token == 253)
            {
                int size = this.ReadInt24();
                ret = this.FillArray(size);
            }
            else if (token == 254)
            {
                int tmpToken = this.ReadInt8();
                ret = WhatsApp.SysEncoding.GetBytes(this.GetToken(tmpToken + 0xf5));
            }
            else if (token == 250)
            {
                string user = WhatsApp.SysEncoding.GetString(this.ReadBytes(this.ReadInt8()));
                string server = WhatsApp.SysEncoding.GetString(this.ReadBytes(this.ReadInt8()));
                if ((user.Length > 0) && (server.Length > 0))
                {
                    ret = WhatsApp.SysEncoding.GetBytes(user + "@" + server);
                }
                else if (server.Length > 0)
                {
                    ret = WhatsApp.SysEncoding.GetBytes(server);
                }
            }
            else if (token == 255)
            {
                ret = WhatsApp.SysEncoding.GetBytes(ReadNibble());
            }
            return ret;
        }

        protected string ReadNibble()
        {
            var nextByte = ReadInt8();

            var ignoreLastNibble = (nextByte & 0x80) != 0;
            var size = (nextByte & 0x7f);
            var nrOfNibbles = size * 2 - (ignoreLastNibble ? 1 : 0);

            var data = FillArray(size);
            var chars = new List<char>();

            for (int i = 0; i < nrOfNibbles; i++)
            {
                nextByte = data[(int)Math.Floor(i / 2.0)];

                var shift = 4 * (1 - i % 2);
                byte dec = (byte)((nextByte & (15 << shift)) >> shift);

                switch (dec)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        chars.Add(dec.ToString()[0]);
                        break;

                    case 10:
                        chars.Add('-');
                        break;

                    case 11:
                        chars.Add('.');
                        break;

                    default:
                        throw new Exception("Bad nibble: " + dec);
                }
            }
            return new string(chars.ToArray());
        }

        protected IEnumerable<KeyValue> ReadAttributes(int size)
        {
            var attributes = new List<KeyValue>();
            int attribCount = (size - 2 + size % 2) / 2;
            for (int i = 0; i < attribCount; i++)
            {
                byte[] keyB = this.ReadBytes(this.ReadInt8());
                byte[] valueB = this.ReadBytes(this.ReadInt8());
                string key = WhatsApp.SysEncoding.GetString(keyB);
                string value = WhatsApp.SysEncoding.GetString(valueB);
                attributes.Add(new KeyValue(key, value));
            }
            return attributes;
        }

        protected ProtocolTreeNode NextTreeInternal()
        {
            int token1 = this.ReadInt8();
            int size = this.ReadListSize(token1);
            int token2 = this.ReadInt8();
            if (token2 == 1)
            {
                var attributes = this.ReadAttributes(size);
                return new ProtocolTreeNode("start", attributes);
            }
            if (token2 == 2)
            {
                return null;
            }
            string tag = WhatsApp.SysEncoding.GetString(this.ReadBytes(token2));
            var tmpAttributes = this.ReadAttributes(size);

            if ((size % 2) == 1)
            {
                return new ProtocolTreeNode(tag, tmpAttributes);
            }
            int token3 = this.ReadInt8();
            if (this.IsListTag(token3))
            {
                return new ProtocolTreeNode(tag, tmpAttributes, this.ReadList(token3));
            }

            return new ProtocolTreeNode(tag, tmpAttributes, null, this.ReadBytes(token3));
        }

        protected bool IsListTag(int token)
        {
            return ((token == 248) || (token == 0) || (token == 249));
        }

        protected List<ProtocolTreeNode> ReadList(int token)
        {
            int size = this.ReadListSize(token);
            var ret = new List<ProtocolTreeNode>();
            for (int i = 0; i < size; i++)
            {
                ret.Add(this.NextTreeInternal());
            }
            return ret;
        }

        protected int ReadListSize(int token)
        {
            int size = 0;
            if (token == 0)
            {
                size = 0;
            }
            else if (token == 0xf8)
            {
                size = this.ReadInt8();
            }
            else if (token == 0xf9)
            {
                size = this.ReadInt16();
            }
            else
            {
                throw new Exception("BinTreeNodeReader->readListSize: Invalid token " + token);
            }
            return size;
        }

        protected int PeekInt8(int offset = 0)
        {
            int ret = 0;

            if (this.buffer.Count >= offset + 1)
                ret = this.buffer[offset];

            return ret;
        }

        protected int PeekInt24(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= 3 + offset)
            {
                ret = (this.buffer[0 + offset] << 16) + (this.buffer[1 + offset] << 8) + this.buffer[2 + offset];
            }
            return ret;
        }

        protected int ReadInt24()
        {
            int ret = 0;
            if (this.buffer.Count >= 3)
            {
                ret = this.buffer[0] << 16;
                ret |= this.buffer[1] << 8;
                ret |= this.buffer[2] << 0;
                this.buffer.RemoveRange(0, 3);
            }
            return ret;
        }

        protected int PeekInt16(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= offset + 2)
            {
                ret = (int)this.buffer[0 + offset] << 8;
                ret |= (int)this.buffer[1 + offset] << 0;
            }
            return ret;
        }

        protected int ReadInt16()
        {
            int ret = 0;
            if (this.buffer.Count >= 2)
            {
                ret = (int)this.buffer[0] << 8;
                ret |= (int)this.buffer[1] << 0;
                this.buffer.RemoveRange(0, 2);
            }
            return ret;
        }

        protected int ReadInt8()
        {
            int ret = 0;
            if (this.buffer.Count >= 1)
            {
                ret = (int)this.buffer[0];
                this.buffer.RemoveAt(0);
            }
            return ret;
        }

        protected byte[] FillArray(int len)
        {
            byte[] ret = new byte[len];
            if (this.buffer.Count >= len)
            {
                Buffer.BlockCopy(this.buffer.ToArray(), 0, ret, 0, len);
                this.buffer.RemoveRange(0, len);
            }
            else
            {
                throw new Exception();
            }
            return ret;
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