// http://www.darkfader.net/toolbox/convert/ Test Units
using System;
using System.Collections.Generic;

// For ExtraFunctions
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Tr.Com.Eimza.LibAxolotl;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Protocol;
using Tr.Com.Eimza.LibAxolotl.State;

// Temporary only For Testing

namespace WhatsAppApi.Helper
{
    public class AxolotlManager : WhatsAppBase, AxolotlStore
    {
        private Dictionary<string, List<ProtocolTreeNode>> pending_nodes = new Dictionary<string, List<ProtocolTreeNode>>();
        private Dictionary<string, ProtocolTreeNode> retryNodes = new Dictionary<string, ProtocolTreeNode>();
        private Dictionary<string, int> retryCounters = new Dictionary<string, int>();
        private Dictionary<string, SessionCipher> sessionCiphers = new Dictionary<string, SessionCipher>();
        private List<string> cipherKeys = new List<string>();
        private List<string> v2Jids = new List<string>();
        private bool replaceKey = false;

        /// <summary>
        ///
        /// </summary>
        public AxolotlManager()
        {
        }

        /// <summary>
        /// intercept iq and precess the keys
        /// </summary>
        /// <param name="node"></param>
        public ProtocolTreeNode[] ProcessIqTreeNode(ProtocolTreeNode node)
        {
            try
            {
                if (cipherKeys.Contains(node.GetAttribute("id")))
                {
                    cipherKeys.Remove(node.GetAttribute("id"));
                    foreach (var child in node.children)
                    {
                        string jid = child.GetChild("user").GetAttribute("jid");
                        uint registrationId = DeAdjustID(child.GetChild("registration").GetData());
                        IdentityKey identityKey = new IdentityKey(new DjbECPublicKey(child.GetChild("identity").GetData()));
                        uint signedPreKeyId = DeAdjustID(child.GetChild("skey").GetChild("id").GetData());
                        DjbECPublicKey signedPreKeyPub = new DjbECPublicKey(child.GetChild("skey").GetChild("value").GetData());
                        byte[] signedPreKeySig = child.GetChild("skey").GetChild("signature").GetData();
                        uint preKeyId = DeAdjustID(child.GetChild("key").GetChild("id").GetData());
                        DjbECPublicKey preKeyPublic = new DjbECPublicKey(child.GetChild("key").GetChild("value").GetData());
                        PreKeyBundle preKeyBundle = new PreKeyBundle(registrationId, 1, preKeyId, preKeyPublic, signedPreKeyId, signedPreKeyPub, signedPreKeySig, identityKey);
                        SessionBuilder sessionBuilder = new SessionBuilder(this, this, this, this, new AxolotlAddress(ExtractNumber(jid), 1));

                        // Now Do the Work Return Nodelist
                        sessionBuilder.Process(preKeyBundle);

                        if (pending_nodes.ContainsKey(ExtractNumber(jid)))
                        {
                            var pendingNodes = pending_nodes[ExtractNumber(jid)].ToArray();
                            pending_nodes.Remove(ExtractNumber(jid));
                            return pendingNodes;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
            }
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ProtocolTreeNode ProcessEncryptedNode(ProtocolTreeNode node)
        {
            string from = node.GetAttribute("from");
            string author = string.Empty;
            string version = string.Empty;
            string encType = string.Empty;
            byte[] encMsg = null;
            ProtocolTreeNode rtnNode = null;

            if (from.IndexOf("s.whatsapp.net", 0, StringComparison.Ordinal) > -1)
            {
                author = ExtractNumber(node.GetAttribute("from"));
                version = node.GetAttribute("v");
                encType = node.GetChild("enc").GetAttribute("type");
                encMsg = node.GetChild("enc").GetData();

                if (!ContainsSession(new AxolotlAddress(author, 1)))
                {
                    //we don't have the session to decrypt, save it in pending and process it later
                    AddPendingNode(node);
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("info : Requesting cipher keys from " + author);
                    SendGetCipherKeysFromUser(author);
                }
                else
                {
                    //decrypt the message with the session
                    if (node.GetChild("enc").GetAttribute("count") == "")
                        SetRetryCounter(node.GetAttribute("id"), 1);
                    if (version == "2")
                    {
                        if (!v2Jids.Contains(author))
                            v2Jids.Add(author);
                    }

                    object plaintext = DecryptMessage(from, encMsg, encType, node.GetAttribute("id"), node.GetAttribute("t"));

                    if (plaintext is bool && false == (bool)plaintext)
                    {
                        SendRetry(node, from, node.GetAttribute("id"), node.GetAttribute("t"));
                        Helper.DebugAdapter.Instance.FireOnPrintDebug("info : " + string.Format("Couldn't decrypt message id {0} from {1}. Retrying.", node.GetAttribute("id"), author));
                        return node; // could not decrypt
                    }

                    // success now lets clear all setting and return node
                    if (retryCounters.ContainsKey(node.GetAttribute("id")))
                        retryCounters.Remove(node.GetAttribute("id"));
                    if (retryNodes.ContainsKey(node.GetAttribute("id")))
                        retryNodes.Remove(node.GetAttribute("id"));

                    switch (node.GetAttribute("type"))
                    {
                        case "text":
                            //Convert to List.
                            List<ProtocolTreeNode> children = node.children.ToList();
                            List<KeyValue> attributeHash = node.attributeHash.ToList();
                            children.Add(new ProtocolTreeNode("body", null, null, (byte[])plaintext));
                            rtnNode = new ProtocolTreeNode(node.tag, attributeHash.ToArray(), children.ToArray(), node.data);
                            break;

                        case "media":
                            break;
                    }

                    Helper.DebugAdapter.Instance.FireOnPrintDebug("info : " + string.Format("Decrypted message with {0} id from {1}", node.GetAttribute("id"), author));
                    return rtnNode;
                }
            }
            return node;
        }

        /// <summary>
        /// decrypt an incomming message
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ciphertext"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="retry_from"></param>
        /// <param name="skip_unpad"></param>
        public object DecryptMessage(string from, byte[] ciphertext, string type, string id,
                                    string t, string retry_from = null, bool skip_unpad = false)
        {
            //string _Version = "2";

            #region pkmsg routine

            if (type == "pkmsg")
            {
                if (v2Jids.Contains(ExtractNumber(from)))
                    //_Version = "2";
                    try
                    {
                        PreKeyWhisperMessage preKeyWhisperMessage = new PreKeyWhisperMessage(ciphertext);
                        SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(from));
                        return sessionCipher.Decrypt(preKeyWhisperMessage);
                        // if (version == "2" && !skip_unpad)
                        //  return UnpadV2Plaintext(plaintext.ToString());
                    }
                    catch (Exception)
                    {
                        //ErrorAxolotl(e.Message);
                        return false;
                    }
            }

            #endregion pkmsg routine

            #region WhisperMessage routine

            if (type == "msg")
            {
                if (v2Jids.Contains(ExtractNumber(from)))
                    //_Version = "2";
                    try
                    {
                        PreKeyWhisperMessage preKeyWhisperMessage = new PreKeyWhisperMessage(ciphertext);
                        SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(from));
                        return sessionCipher.Decrypt(preKeyWhisperMessage);
                        //if (version == "2" && !skip_unpad)
                        //    return UnpadV2Plaintext(plaintext.ToString());
                    }
                    catch (Exception)
                    {
                    }
            }

            #endregion WhisperMessage routine

            #region Group message Cipher routine

            if (type == "skmsg")
            {
                throw new NotImplementedException();
            }

            #endregion Group message Cipher routine

            return false;
        }

        /// <summary>
        /// Send a request to get cipher keys from an user
        /// </summary>
        /// <param name="number">Phone number of the user you want to get the cipher keys</param>
        /// <param name="replaceKeyIn"></param>
        public void SendGetCipherKeysFromUser(string number, bool replaceKeyIn = false)
        {
            replaceKey = replaceKeyIn;
            var msgId = TicketManager.GenerateId();
            cipherKeys.Add(msgId);

            ProtocolTreeNode user = new ProtocolTreeNode("user", new[] {
                    new KeyValue("jid", ApiBase.GetJID(number)),
                    }, null, null);

            ProtocolTreeNode keyNode = new ProtocolTreeNode("key", null, new ProtocolTreeNode[] { user }, null);
            ProtocolTreeNode Node = new ProtocolTreeNode("iq", new[] {
                    new KeyValue("id", msgId),
                    new KeyValue("xmlns", "encrypt"),
                    new KeyValue("type", "get"),
                    new KeyValue("to", "s.whatsapp.net")
                   }, new ProtocolTreeNode[] { keyNode }, null);

            this.SendNode(Node);
        }

        /// <summary>
        /// return the stored session cypher for this number
        /// </summary>
        public SessionCipher GetSessionCipher(string number)
        {
            if (sessionCiphers.ContainsKey(number))
            {
                return sessionCiphers[number];
            }
            else
            {
                AxolotlAddress address = new AxolotlAddress(number, 1);
                sessionCiphers.Add(number, new SessionCipher(this, this, this, this, address));
                return sessionCiphers[number];
            }
        }

        /// <summary>
        /// Generate the keysets for ourself
        /// </summary>
        /// <returns></returns>
        public bool SendSetPreKeys(bool isnew = false)
        {
            uint registrationId = 0;
            if (!isnew)
                registrationId = (uint)this.GetLocalRegistrationId();
            else
                registrationId = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateRegistrationId(true);
            Random random = new Random();
            uint randomid = (uint)Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GetRandomSequence(5000);//65536
            IdentityKeyPair identityKeyPair = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateIdentityKeyPair();
            byte[] privateKey = identityKeyPair.GetPrivateKey().Serialize();
            byte[] publicKey = identityKeyPair.GetPublicKey().GetPublicKey().Serialize();
            IList<PreKeyRecord> preKeys = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GeneratePreKeys((uint)random.Next(), 200);
            SignedPreKeyRecord signedPreKey = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateSignedPreKey(identityKeyPair, randomid);
            PreKeyRecord lastResortKey = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateLastResortPreKey();

            this.StorePreKeys(preKeys);
            this.StoreLocalData(registrationId, identityKeyPair.GetPublicKey().Serialize(), identityKeyPair.GetPrivateKey().Serialize());
            this.StoreSignedPreKey(signedPreKey.GetId(), signedPreKey);
            // FOR INTERNAL TESTING ONLY
            //this.InMemoryTestSetup(identityKeyPair, registrationId);

            ProtocolTreeNode[] preKeyNodes = new ProtocolTreeNode[200];
            for (int i = 0; i < 200; i++)
            {
                byte[] prekeyId = AdjustID(preKeys[i].GetId().ToString()).Skip(1).ToArray();
                byte[] prekey = preKeys[i].GetKeyPair().GetPublicKey().Serialize().Skip(1).ToArray();
                ProtocolTreeNode NodeId = new ProtocolTreeNode("id", null, null, prekeyId);
                ProtocolTreeNode NodeValue = new ProtocolTreeNode("value", null, null, prekey);
                preKeyNodes[i] = new ProtocolTreeNode("key", null, new ProtocolTreeNode[] { NodeId, NodeValue }, null);
            }

            ProtocolTreeNode registration = new ProtocolTreeNode("registration", null, null, AdjustID(registrationId.ToString()));
            ProtocolTreeNode type = new ProtocolTreeNode("type", null, null, new byte[] { Curve.DJB_TYPE });
            ProtocolTreeNode list = new ProtocolTreeNode("list", null, preKeyNodes, null);
            ProtocolTreeNode sid = new ProtocolTreeNode("id", null, null, AdjustID(signedPreKey.GetId().ToString(), true));
            ProtocolTreeNode identity = new ProtocolTreeNode("identity", null, null, identityKeyPair.GetPublicKey().GetPublicKey().Serialize().Skip(1).ToArray());
            ProtocolTreeNode value = new ProtocolTreeNode("value", null, null, signedPreKey.GetKeyPair().GetPublicKey().Serialize().Skip(1).ToArray());
            ProtocolTreeNode signature = new ProtocolTreeNode("signature", null, null, signedPreKey.GetSignature());
            ProtocolTreeNode secretKey = new ProtocolTreeNode("skey", null, new ProtocolTreeNode[] { sid, value, signature }, null);

            String id = TicketManager.GenerateId();
            Helper.DebugAdapter.Instance.FireOnPrintDebug(string.Format("axolotl id = {0}", id));

            ProtocolTreeNode Node = new ProtocolTreeNode("iq", new KeyValue[] {
                    new KeyValue("id", id),
                    new KeyValue("to", "s.whatsapp.net"),
                    new KeyValue("type", "set"),
                    new KeyValue("xmlns", "encrypt")
                   }, new ProtocolTreeNode[] { identity, registration, type, list, secretKey }, null);

            this.SendNode(Node);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        public void ResetEncryption()
        {
        }

        /// <summary>
        /// jid from address
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public string ExtractNumber(string from)
        {
            return new StringBuilder(from).Replace("@s.whatsapp.net", "").Replace("@g.us", "").ToString();
        }

        /// <summary>
        /// add a node to the queue for latter processing
        /// due to missing cyper keys
        /// </summary>
        /// <param name="node"></param>
        public void AddPendingNode(ProtocolTreeNode node)
        {
            string number = string.Empty;
            string from = node.GetAttribute("from");

            if (from.IndexOf("s.whatsapp.net", 0) > -1)
                number = ExtractNumber(node.GetAttribute("from"));
            else
                number = ExtractNumber(node.GetAttribute("participant"));

            if (pending_nodes.ContainsKey(number))
            {
                pending_nodes[number].Add(node);
            }
            else
            {
                pending_nodes.Add(number, new List<ProtocolTreeNode> { node });
            }
        }

        /// <summary>
        /// increment the retry counters base
        /// </summary>
        /// <param name="id"></param>
        /// <param name="counter"></param>
        public void SetRetryCounter(string id, int counter)
        {
            //   retryCounters[$id] = $counter;
        }

        /// <summary>
        /// send a retry to reget this node
        /// </summary>
        /// <param name="nod"></param>
        /// <param name="to"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="participant"></param>
        public void SendRetry(ProtocolTreeNode node, string to, string id, string t, string participant = null)
        {
            ProtocolTreeNode returnNode = null;

            #region update retry counters

            if (!retryCounters.ContainsKey(id))
            {
                retryCounters.Add(id, 1);
            }
            else
            {
                if (retryNodes.ContainsKey(id))
                {
                    retryNodes[id] = node;
                }
                else
                {
                    retryNodes.Add(id, node);
                }
            }

            #endregion update retry counters

            if (retryCounters[id] > 2)
                ResetEncryption();

            retryCounters[id]++;
            ProtocolTreeNode retryNode = new ProtocolTreeNode("retry", new[] {
                        new KeyValue("v", "1"),
                        new KeyValue("count", retryCounters[id].ToString()),
                        new KeyValue("id", id),
                        new KeyValue("t", t)
                       }, null, null);

            byte[] regid = AdjustID(GetLocalRegistrationId().ToString());
            ProtocolTreeNode registrationNode = new ProtocolTreeNode("registration", null, null, regid);

            //if (participant != null) //isgroups group retry
            //    attrGrp.Add(new KeyValue("participant", participant));

            returnNode = new ProtocolTreeNode("receipt", new[] {
                                            new KeyValue("id", id),
                                            new KeyValue("to", to),
                                            new KeyValue("type", "retry"),
                                            new KeyValue("t", t)
                                            },
                                         new ProtocolTreeNode[] { retryNode, registrationNode }, null);
            this.SendNode(returnNode);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public uint DeAdjustID(byte[] val)
        {
            byte[] newval = null;
            byte[] bytebase = new byte[] { (byte)0x00 };
            byte[] rv = bytebase.Concat(val).ToArray();

            if (val.Length < 4)
                newval = rv;
            else
                newval = val;

            byte[] reversed = newval.Reverse().ToArray();
            return BitConverter.ToUInt32(reversed, 0);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <param name="limitsix"></param>
        public byte[] AdjustID(String id, bool limitsix = false)
        {
            uint idx = uint.Parse(id);
            byte[] bytes = BitConverter.GetBytes(idx);
            uint atomSize = BitConverter.ToUInt32(bytes, 0);
            Array.Reverse(bytes, 0, bytes.Length);

            string data = Bin2Hex(bytes);

            if (!string.IsNullOrEmpty(data) && limitsix)
                data = (data.Length <= 6) ? data : data.Substring(data.Length - 6);

            while (data.Length < 6)
            {
                data = "0" + data;
            }
            return Hex2Bin(data);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public byte[] Hex2Bin(String hex)
        {
            // return (from i in Enumerable.Range(0, hex.Length / 2)
            //          select Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private static string Bin2Hex(byte[] bin)
        {
            return BitConverter.ToString(bin).Replace("-", string.Empty).ToLower();

            #region old code

            /*
            StringBuilder sb = new StringBuilder(bin.Length *2);
            foreach (byte b in bin)
            {
                sb.Append(b.ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
            */

            #endregion old code
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="strBin"></param>
        /// <returns></returns>
        public string Bin2HexXX(string strBin)
        {
            int decNumber = Convert.ToInt16(strBin, 2);
            return decNumber.ToString("X");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v2plaintext"></param>
        /// <returns></returns>
        public string UnpadV2Plaintext(string v2plaintext)
        {
            if (v2plaintext.Length < 128)
                return v2plaintext.Substring(2, -1);
            else
                return v2plaintext.Substring(3, -1);
        }

        #region raise a delegates error event to the main aplication

        public event OnErrorAxolotlDelegate OnErrorAxolotl;

        public void ErrorAxolotl(String ErrorMessage)
        {
            if (this.OnErrorAxolotl != null)
            {
                this.OnErrorAxolotl(ErrorMessage);
            }
        }

        public delegate void OnErrorAxolotlDelegate(string ErrorMessage);

        #endregion raise a delegates error event to the main aplication

        #region Public Interfaces for AxolotlStore

        #region session event and delegates ISessionStore

        public event OnstoreSessionDataDelegate OnstoreSession;

        public void StoreSession(AxolotlAddress address, SessionRecord record)
        {
            if (this.OnstoreSession != null)
            {
                this.OnstoreSession(address.GetName(), address.GetDeviceId(), record.Serialize());
            }
        }

        public event OnloadSessionDelegate OnloadSession;

        public SessionRecord LoadSession(AxolotlAddress address)
        {
            if (this.OnloadSession != null)
            {
                byte[] session = this.OnloadSession(address.GetName(), address.GetDeviceId());
                if (session == null)
                    return new SessionRecord();
                else
                    return new SessionRecord(session);
            }
            return null;
        }

        public event OngetSubDeviceSessionsDelegate OngetSubDeviceSessions;

        public List<UInt32> GetSubDeviceSessions(string recipientId)
        {
            if (this.OngetSubDeviceSessions != null)
            {
                return this.OngetSubDeviceSessions(recipientId);
            }
            return null;
        }

        public event OncontainsSessionDelegate OncontainsSession;

        public bool ContainsSession(AxolotlAddress address)
        {
            if (this.OncontainsSession != null)
            {
                return this.OncontainsSession(address.GetName(), address.GetDeviceId());
            }
            return false;
        }

        public event OndeleteSessionDelegate OndeleteSession;

        public void DeleteSession(AxolotlAddress address)
        {
            if (this.OndeleteSession != null)
            {
                this.OndeleteSession(address.GetName(), address.GetDeviceId());
            }
        }

        public event OndeleteAllSessionsDelegate OndeleteAllSessions;

        public void DeleteAllSessions(string name)
        {
            if (this.OndeleteAllSessions != null)
            {
                this.OndeleteAllSessions(name);
            }
        }

        //Event Delegates
        public delegate void OnstoreSessionDataDelegate(string recipientId, uint deviceId, byte[] sessionRecord);

        public delegate byte[] OnloadSessionDelegate(string recipientId, uint deviceId);

        public delegate List<UInt32> OngetSubDeviceSessionsDelegate(string recipientId);

        public delegate bool OncontainsSessionDelegate(string recipientId, uint deviceId);

        public delegate void OndeleteAllSessionsDelegate(string recipientId);

        public delegate void OndeleteSessionDelegate(string recipientId, uint deviceId);

        #endregion session event and delegates ISessionStore

        #region PreKeys event and delegates IPreKeyStore

        // Internat Store All Generatet Keys
        public void StorePreKeys(IList<PreKeyRecord> keys)
        {
            foreach (PreKeyRecord key in keys)
                StorePreKey((uint)key.GetId(), key);
        }

        public event OnstorePreKeyDelegate OnstorePreKey;

        public void StorePreKey(uint preKeyId, PreKeyRecord record)
        {
            if (this.OnstorePreKey != null)
            {
                this.OnstorePreKey(preKeyId, record.Serialize());
            }
        }

        public event OnloadPreKeyDelegate OnloadPreKey;

        public PreKeyRecord LoadPreKey(uint preKeyId)
        {
            if (this.OnloadPreKey != null)
            {
                return new PreKeyRecord(this.OnloadPreKey(preKeyId));
            }
            return null;
        }

        public event OncontainsPreKeyDelegate OncontainsPreKey;

        public bool ContainsPreKey(uint preKeyId)
        {
            if (this.OncontainsPreKey != null)
            {
                return this.OncontainsPreKey(preKeyId);
            }
            return false;
        }

        public event OnremovePreKeyDelegate OnremovePreKey;

        public void RemovePreKey(uint preKeyId)
        {
            if (this.OnremovePreKey != null)
            {
                this.OnremovePreKey(preKeyId);
            }
        }

        public event OnloadPreKeysDelegate OnloadPreKeys;

        public List<byte[]> LoadPreKeys()
        {
            if (this.OnloadPreKeys != null)
            {
                return this.OnloadPreKeys();
            }
            return null;
        }

        public event OnremoveAllPreKeysDelegate OnremoveAllPreKeys;

        protected void RemoveAllPreKeys()
        {
            if (this.OnremoveAllPreKeys != null)
            {
                this.OnremoveAllPreKeys();
            }
        }

        //Event Delegates
        public delegate void OnstorePreKeyDelegate(uint prekeyId, byte[] preKeyRecord);

        public delegate byte[] OnloadPreKeyDelegate(uint preKeyId);

        public delegate List<byte[]> OnloadPreKeysDelegate();

        public delegate bool OncontainsPreKeyDelegate(uint preKeyId);

        public delegate void OnremovePreKeyDelegate(uint preKeyId);

        public delegate void OnremoveAllPreKeysDelegate();

        #endregion PreKeys event and delegates IPreKeyStore

        #region SignedPreKey Event and Delegates ISignedPreKeyStore

        public event OnstoreSignedPreKeyDelegate OnstoreSignedPreKey;

        public void StoreSignedPreKey(UInt32 signedPreKeyId, SignedPreKeyRecord record)
        {
            if (this.OnstoreSignedPreKey != null)
            {
                this.OnstoreSignedPreKey(signedPreKeyId, record.Serialize());
            }
        }

        public event OnloadSignedPreKeyDelegate OnloadSignedPreKey;

        public SignedPreKeyRecord LoadSignedPreKey(UInt32 signedPreKeyId)
        {
            if (this.OnloadSignedPreKey != null)
            {
                return new SignedPreKeyRecord(this.OnloadSignedPreKey(signedPreKeyId));
            }
            return null;
        }

        public event OnloadSignedPreKeysDelegate OnloadSignedPreKeys;

        public List<SignedPreKeyRecord> LoadSignedPreKeys()
        {
            if (this.OnloadSignedPreKeys != null)
            {
                List<byte[]> inputList = this.OnloadSignedPreKeys();
                return inputList.Select(x => new SignedPreKeyRecord(x)).ToList();
            }
            return null;
        }

        public event OncontainsSignedPreKeyDelegate OncontainsSignedPreKey;

        public bool ContainsSignedPreKey(UInt32 signedPreKeyId)
        {
            if (this.OncontainsSignedPreKey != null)
            {
                return this.OncontainsSignedPreKey(signedPreKeyId);
            }
            return false;
        }

        public event OnremoveSignedPreKeyDelegate OnremoveSignedPreKey;

        public void RemoveSignedPreKey(UInt32 signedPreKeyId)
        {
            if (this.OnremoveSignedPreKey != null)
            {
                this.OnremoveSignedPreKey(signedPreKeyId);
            }
        }

        //Event Delegates
        public delegate void OnstoreSignedPreKeyDelegate(UInt32 signedPreKeyId, byte[] signedPreKeyRecord);

        public delegate byte[] OnloadSignedPreKeyDelegate(UInt32 preKeyId);

        public delegate List<byte[]> OnloadSignedPreKeysDelegate();

        public delegate void OnremoveSignedPreKeyDelegate(UInt32 preKeyId);

        public delegate bool OncontainsSignedPreKeyDelegate(UInt32 preKeyId);

        #endregion SignedPreKey Event and Delegates ISignedPreKeyStore

        #region identity event and delegates IIdentityKeyStore

        public event OngetIdentityKeyPairDelegate OngetIdentityKeyPair;

        public IdentityKeyPair GetIdentityKeyPair()
        {
            if (this.OngetIdentityKeyPair != null)
            {
                List<byte[]> pair = this.OngetIdentityKeyPair();
                return new IdentityKeyPair(new IdentityKey(new DjbECPublicKey(pair[0])), new DjbECPrivateKey(pair[1]));
            }
            return null;
        }

        public event OngetLocalRegistrationIdDelegate OngetLocalRegistrationId;

        public UInt32 GetLocalRegistrationId()
        {
            if (this.OngetLocalRegistrationId != null)
            {
                return this.OngetLocalRegistrationId();
            }
            return 0; // FIXME: this isn't correct workaround only
        }

        /// <summary>
        ///  Determine whether a remote client's identity is trusted.  Convention is
        ///  that the TextSecure protocol is 'trust on first use.'  This means that
        ///  an identity key is considered 'trusted' if there is no entry for the recipient
        ///  in the local store, or if it matches the saved key for a recipient in the local
        ///  store.Only if it mismatches an entry in the local store is it considered
        ///  'untrusted.'
        /// </summary>
        public event OnisTrustedIdentityDelegate OnisTrustedIdentity;

        public bool IsTrustedIdentity(string name, IdentityKey identityKey)
        {
            if (this.OnisTrustedIdentity != null)
            {
                return this.OnisTrustedIdentity(name, identityKey.Serialize());
            }
            return false; // FIXME: this isn't correct workaround only
        }

        public event OnsaveIdentityDelegate OnsaveIdentity;

        public bool SaveIdentity(string name, IdentityKey identityKey)
        {
            if (this.OnsaveIdentity != null)
            {
                return this.OnsaveIdentity(name, identityKey.Serialize());
            }
            return false;
        }

        public event OnstoreLocalDataDelegate OnstoreLocalData;

        public void StoreLocalData(uint registrationId, byte[] publickey, byte[] privatekey)
        {
            if (this.OnstoreLocalData != null)
            {
                this.OnstoreLocalData(registrationId, publickey, privatekey);
            }
        }

        //event delegates
        public delegate void OnstoreLocalDataDelegate(uint registrationId, byte[] publickey, byte[] privatekey);

        public delegate List<byte[]> OngetIdentityKeyPairDelegate();

        public delegate UInt32 OngetLocalRegistrationIdDelegate();

        public delegate bool OnisTrustedIdentityDelegate(string recipientId, byte[] identityKey);

        public delegate bool OnsaveIdentityDelegate(string recipientId, byte[] identityKey);

        #endregion identity event and delegates IIdentityKeyStore

        #region sender_keys event and delegates

        public event OnstoreSenderKeyDelegate OnstoreSenderKey;

        protected void FireStoreSenderKey(int senderKeyId, byte[] senderKeyRecord)
        {
            if (this.OnstoreSenderKey != null)
            {
                this.OnstoreSenderKey(senderKeyId, senderKeyRecord);
            }
        }

        public event OnloadSenderKeyDelegate OnloadSenderKey;

        protected byte[] FireLoadSenderKey(int senderKeyId)
        {
            if (this.OnloadSenderKey != null)
            {
                return this.OnloadSenderKey(senderKeyId);
            }
            return null;
        }

        public event OnremoveSenderKeyDelegate OnremoveSenderKey;

        protected void FireRemoveSenderKey(int senderKeyId)
        {
            if (this.OnremoveSenderKey != null)
            {
                this.OnremoveSenderKey(senderKeyId);
            }
        }

        public event OncontainsSenderKeyDelegate OncontainsSenderKey;

        protected bool FireContainsSenderKey(int senderKeyId)
        {
            if (this.OncontainsSenderKey != null)
            {
                return this.OncontainsSenderKey(senderKeyId);
            }

            return false;
        }

        //event delegates
        public delegate void OnstoreSenderKeyDelegate(int senderKeyId, byte[] senderKeyRecord);

        public delegate byte[] OnloadSenderKeyDelegate(int senderKeyId);

        public delegate void OnremoveSenderKeyDelegate(int senderKeyId);

        public delegate bool OncontainsSenderKeyDelegate(int senderKeyId);

        #endregion sender_keys event and delegates

        #endregion Public Interfaces for AxolotlStore

        #region TESTING IN MEMORY STORE

        /*
        private  InMemoryPreKeyStore preKeyStore             = new InMemoryPreKeyStore();
        private  InMemorySessionStore sessionStore           = new InMemorySessionStore();
        private  InMemorySignedPreKeyStore signedPreKeyStore = new InMemorySignedPreKeyStore();
        private  InMemoryIdentityKeyStore identityKeyStore;

        public List<byte[]> LoadPreKeys() { return null; }
        public void RemoveAllPreKeys() { }
        public void StorePreKeys(IList<PreKeyRecord> keys) { }
        public void StoreLocalData(uint registrationId, byte[] publickey, byte[] privatekey) { }
        public void InMemoryTestSetup(IdentityKeyPair identityKeyPair, uint registrationId)
        {
            this.identityKeyStore = new InMemoryIdentityKeyStore(identityKeyPair, registrationId);
        }

        public IdentityKeyPair GetIdentityKeyPair()
        {
            return identityKeyStore.GetIdentityKeyPair();
        }
        public uint GetLocalRegistrationId()
        {
            return identityKeyStore.GetLocalRegistrationId();
        }
        public bool SaveIdentity(String name, IdentityKey identityKey)
        {
            identityKeyStore.SaveIdentity(name, identityKey);
            return true;
        }
        public bool IsTrustedIdentity(String name, IdentityKey identityKey)
        {
            return identityKeyStore.IsTrustedIdentity(name, identityKey);
        }
        public PreKeyRecord LoadPreKey(uint preKeyId)
        {
            return preKeyStore.LoadPreKey(preKeyId);
        }
        public void StorePreKey(uint preKeyId, PreKeyRecord record)
        {
            preKeyStore.StorePreKey(preKeyId, record);
        }
        public bool ContainsPreKey(uint preKeyId)
        {
            return preKeyStore.ContainsPreKey(preKeyId);
        }
        public void RemovePreKey(uint preKeyId)
        {
            preKeyStore.RemovePreKey(preKeyId);
        }
        public SessionRecord LoadSession(AxolotlAddress address)
        {
            return sessionStore.LoadSession(address);
        }
        public List<uint> GetSubDeviceSessions(String name)
        {
            return sessionStore.GetSubDeviceSessions(name);
        }
        public void StoreSession(AxolotlAddress address, SessionRecord record)
        {
            sessionStore.StoreSession(address, record);
        }
        public bool ContainsSession(AxolotlAddress address)
        {
            return sessionStore.ContainsSession(address);
        }
        public void DeleteSession(AxolotlAddress address)
        {
            sessionStore.DeleteSession(address);
        }
        public void DeleteAllSessions(String name)
        {
            sessionStore.DeleteAllSessions(name);
        }
        public SignedPreKeyRecord LoadSignedPreKey(uint signedPreKeyId)
        {
            return signedPreKeyStore.LoadSignedPreKey(signedPreKeyId);
        }
        public List<SignedPreKeyRecord> LoadSignedPreKeys()
        {
            return signedPreKeyStore.LoadSignedPreKeys();
        }
        public void StoreSignedPreKey(uint signedPreKeyId, SignedPreKeyRecord record)
        {
            signedPreKeyStore.StoreSignedPreKey(signedPreKeyId, record);
        }
        public bool ContainsSignedPreKey(uint signedPreKeyId)
        {
            return signedPreKeyStore.ContainsSignedPreKey(signedPreKeyId);
        }
        public void RemoveSignedPreKey(uint signedPreKeyId)
        {
            signedPreKeyStore.RemoveSignedPreKey(signedPreKeyId);
        }
        */

        #endregion TESTING IN MEMORY STORE
    }

    /// <summary>
    /// NOT USED YET
    /// </summary>
    public class ExtraFunctions
    {
        public static byte[] SerializeToBytes<T>(T item)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }

        public static object DeserializeFromBytes(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
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
            // for (int i = 0; i >> offset) & 0xFF);
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