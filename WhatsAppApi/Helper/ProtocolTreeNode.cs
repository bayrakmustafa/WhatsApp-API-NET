using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Util;

namespace WhatsAppApi.Helper
{
    public class ProtocolTreeNode
    {
        public string _Tag;
        public IEnumerable<KeyValue> _AttributeHash;
        public IEnumerable<ProtocolTreeNode> _Children;
        public byte[] _Data;

        public ProtocolTreeNode(string tag, IEnumerable<KeyValue> attributeHash, IEnumerable<ProtocolTreeNode> children = null, byte[] data = null)
        {
            this._Tag = tag ?? "";
            this._AttributeHash = attributeHash ?? new KeyValue[0];
            this._Children = children ?? new ProtocolTreeNode[0];
            this._Data = new byte[0];
            if (data != null)
                this._Data = data;
        }

        public ProtocolTreeNode(string tag, IEnumerable<KeyValue> attributeHash, ProtocolTreeNode children = null)
        {
            this._Tag = tag ?? "";
            this._AttributeHash = attributeHash ?? new KeyValue[0];
            this._Children = children != null ? new ProtocolTreeNode[] { children } : new ProtocolTreeNode[0];
            this._Data = new byte[0];
        }

        public ProtocolTreeNode(string tag, IEnumerable<KeyValue> attributeHash, byte[] data = null)
            : this(tag, attributeHash, new ProtocolTreeNode[0], data)
        {
        }

        public ProtocolTreeNode(string tag, IEnumerable<KeyValue> attributeHash)
            : this(tag, attributeHash, new ProtocolTreeNode[0], null)
        {
        }

        public string NodeString(string indent = "")
        {
            string ret = "\n" + indent + "<" + this._Tag;
            if (this._AttributeHash != null)
            {
                foreach (KeyValue item in this._AttributeHash)
                {
                    ret += string.Format(" {0}=\"{1}\"", item.Key, item.Value);
                }
            }
            ret += ">";
            if (this._Data.Length > 0)
            {
                if (this._Data.Length <= 1024)
                {
                    ret += WhatsApp.SysEncoding.GetString(this._Data);
                }
                else
                {
                    ret += string.Format("--{0} byte--", this._Data.Length);
                }
            }

            if (this._Children != null && this._Children.Count() > 0)
            {
                foreach (ProtocolTreeNode item in this._Children)
                {
                    ret += item.NodeString(indent + "  ");
                }
                ret += "\n" + indent;
            }
            ret += "</" + this._Tag + ">";
            return ret;
        }

        public string GetAttribute(string attribute)
        {
            KeyValue ret = this._AttributeHash.FirstOrDefault(x => x.Key.Equals(attribute));
            return (ret == null) ? null : ret.Value;
        }

        public bool HashChild(string tag)
        {
            return GetChild(tag) != null;
        }

        public ProtocolTreeNode GetChild(string tag)
        {
            if (this._Children != null && this._Children.Any())
            {
                foreach (ProtocolTreeNode item in this._Children)
                {
                    if (ProtocolTreeNode.TagEquals(item, tag))
                    {
                        return item;
                    }
                    ProtocolTreeNode ret = item.GetChild(tag);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }

        public ProtocolTreeNode GetChild(int index)
        {
            if (this._Children != null && this._Children.Any())
            {
                if (_Children.Count() >= index)
                {
                    return _Children.ElementAt(index);
                }
            }
            return null;
        }

        public ProtocolTreeNode GetChild(string tag, Dictionary<String, String> attrDic)
        {
            if (this._Children != null && this._Children.Any())
            {
                foreach (ProtocolTreeNode item in this._Children)
                {
                    if (ProtocolTreeNode.TagEquals(item, tag))
                    {
                        return item;
                    }
                    ProtocolTreeNode ret = item.GetChild(tag);
                    if (ret != null)
                    {
                        Boolean found = true;
                        foreach (KeyValuePair<String, String> pair in attrDic)
                        {
                            if (!item.GetAttribute(pair.Key).Equals(pair.Value))
                            {
                                found = false;
                            }
                        }
                        if (found)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }

        public IEnumerable<ProtocolTreeNode> GetAllChildren(string tag)
        {
            List<ProtocolTreeNode> tmpReturn = new List<ProtocolTreeNode>();
            if (this._Children != null && this._Children.Any())
            {
                foreach (ProtocolTreeNode item in this._Children)
                {
                    if (tag.Equals(item._Tag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        tmpReturn.Add(item);
                    }
                    tmpReturn.AddRange(item.GetAllChildren(tag));
                }
            }
            return tmpReturn.ToArray();
        }

        public void RefreshTimes(int offset = 0)
        {
            Dictionary<string, string> retVal = this._AttributeHash.ToDictionary(x => x.Key, x => x.Value);
            if (retVal.ContainsKey("id"))
            {
                String id = retVal.Keys.FirstOrDefault();
                String[] parts = id.Split('-');
                parts[0] = Func.GetNowUnixTimestamp().ToString() + offset.ToString();

                id = String.Join("-", parts);
                
                //Re-Write ID
                retVal.Remove("id");
                retVal.Add("id", id);
            }
            if (retVal.ContainsKey("t"))
            {
                //Re-Write T
                retVal.Remove("t");
                retVal.Add("t",Func.GetNowUnixTimestamp().ToString());
            }
        }

        public IEnumerable<ProtocolTreeNode> GetAllChildren()
        {
            return this._Children.ToArray();
        }

        public void SetChildren(IEnumerable<ProtocolTreeNode> children)
        {
            this._Children = children;
        }

        public byte[] GetData()
        {
            return this._Data;
        }

        public static bool TagEquals(ProtocolTreeNode node, string _string)
        {
            return (((node != null) && (node._Tag != null)) && node._Tag.Equals(_string, StringComparison.OrdinalIgnoreCase));
        }
    }
}