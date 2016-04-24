using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Tr.Com.Eimza.LibAxolotl;
using Tr.Com.Eimza.LibAxolotl.exceptions;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Groups;
using Tr.Com.Eimza.LibAxolotl.Protocol;
using Tr.Com.Eimza.LibAxolotl.State;
using Tr.Com.Eimza.LibAxolotl.Util;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppApi.Settings;
using WhatsAppApi.Store;

namespace WhatsAppApi
{
    /// <summary>
    /// Main API Interface
    /// </summary>
    public class WhatsApp : WhatsAppBase
    {
        public List<String> _CipherKeys = new List<String>();

        public String _GetGroupsId = String.Empty;
        public String _GetGroupv2InfoId = String.Empty;
        public String _GetListsId = String.Empty;
        public String _GetStatusesId = String.Empty;
        public String _GroupCreateId = String.Empty;
        public String _GroupId = String.Empty;
        public String _LeaveGroupId = String.Empty;

        public Dictionary<String, GroupCipher> _GroupCiphers = new Dictionary<String, GroupCipher>();
        public Dictionary<String, List<ProtocolTreeNode>> _PendingNodes = new Dictionary<String, List<ProtocolTreeNode>>();

        public String _PrivacyId = String.Empty;
        public String _PrivacySettingsId = String.Empty;

        public bool _ReplaceKey = false;
        public Dictionary<String, int> _RetryCounters = new Dictionary<String, int>();
        public Dictionary<String, ProtocolTreeNode> _RetryNodes = new Dictionary<String, ProtocolTreeNode>();
        public List<String> _SendCipherKeys = new List<String>();
        public Dictionary<String, SessionCipher> _SessionCiphers = new Dictionary<String, SessionCipher>();
        public List<String> _V2Jids = new List<String>();

        protected bool m_usePoolMessages = false;

        private String _LastServerReceivedID = null;
        private long pTimeout = 0;

        public WhatsApp(string phoneNum, string imei, string nick, bool debug = false, bool hidden = false)
            : base(phoneNum, imei, nick, debug, hidden)
        {
            //ISessionStore AxolotlStore
            base.OnStoreSession += _AxolotKeyStore.StoreSession;
            base.OnLoadSession += _AxolotKeyStore.LoadSession;
            base.OnGetSubDeviceSessions += _AxolotKeyStore.GetSubDeviceSessions;
            base.OnContainsSession += _AxolotKeyStore.ContainsSession;
            base.OnDeleteSession += _AxolotKeyStore.DeleteSession;
            base.OnDeleteAllSessions += _AxolotKeyStore.DeleteAllSessions;

            // IPreKeyStore AxolotlStore
            base.OnStorePreKey += _AxolotKeyStore.StorePreKey;
            base.OnLoadPreKey += _AxolotKeyStore.LoadPreKey;
            base.OnLoadPreKeys += _AxolotKeyStore.LoadPreKeys;
            base.OnContainsPreKey += _AxolotKeyStore.ContainsPreKey;
            base.OnRemovePreKey += _AxolotKeyStore.RemovePreKey;
            base.OnRemoveAllPreKeys += _AxolotKeyStore.RemoveAllPreKeys;

            // ISignedPreKeyStore AxolotlStore
            base.OnStoreSignedPreKey += _AxolotKeyStore.StoreSignedPreKey;
            base.OnLoadSignedPreKey += _AxolotKeyStore.LoadSignedPreKey;
            base.OnLoadSignedPreKeys += _AxolotKeyStore.LoadSignedPreKeys;
            base.OnContainsSignedPreKey += _AxolotKeyStore.ContainsSignedPreKey;
            base.OnRemoveSignedPreKey += _AxolotKeyStore.RemoveSignedPreKey;

            // IIdentityKeyStore AxolotlStore
            base.OnGetIdentityKeyPair += _AxolotKeyStore.GetIdentityKeyPair;
            base.OnGetLocalRegistrationId += _AxolotKeyStore.GetLocalRegistrationId;
            base.OnIsTrustedIdentity += _AxolotKeyStore.IsTrustedIdentity;
            base.OnSaveIdentity += _AxolotKeyStore.SaveIdentity;
            base.OnStoreLocalData += _AxolotKeyStore.StoreLocalData;

            // SenderKeyStore AxolotlStore
            base.OnStoreSenderKey += _AxolotKeyStore.StoreSenderKey;
            base.OnLoadSenderKey += _AxolotKeyStore.LoadSenderKey;
            base.OnLoadSenderKeys += _AxolotKeyStore.LoadSenderKeys;
            base.OnContainsSenderKey += _AxolotKeyStore.ContainsSenderKey;
            base.OnRemoveSenderKey += _AxolotKeyStore.RemoveSenderKey;
        }

        public WhatsAppApi.Store.IAxolotStore _AxolotKeyStore
        {
            get;
            set;
        } = new SqliteAxolotlStore();

        public string LastId { get; set; } = String.Empty;


        public static String FixPadding(String result)
        {
            /* From Chat-API Php Code */
            Char lastChar = result[result.Length - 1];
            String unpadded = result.TrimEnd(lastChar);
            /* From Chat-API Php Code */

            return unpadded;
        }

        public static string SubStr(String value, int startIndex, int length = 0)
        {
            if (length == 0)
                return value.Substring(startIndex);

            if (length < 0)
                length = value.Length - 1 + length;

            return value.Substring(startIndex, length);
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

            number = ExtractNumber(@from.Contains(WhatsConstants.WhatsAppServer) ? node.GetAttribute("from") : node.GetAttribute("participant"));

            if (_PendingNodes.ContainsKey(number))
            {
                _PendingNodes[number].Add(node);
            }
            else
            {
                _PendingNodes.Add(number, new List<ProtocolTreeNode> { node });
            }
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
        /// <param name="val"></param>
        /// <returns></returns>
        public uint DeAdjustID(byte[] val)
        {
            byte[] newval = null;
            byte[] bytebase = new byte[] { (byte)0x00 };
            byte[] rv = bytebase.Concat(val).ToArray();

            newval = val.Length < 4 ? rv : val;

            byte[] reversed = newval.Reverse().ToArray();
            return BitConverter.ToUInt32(reversed, 0);
        }

        /// <summary>
        /// Decrypt an Incomming Group Message
        /// </summary>
        /// <param name="node"></param>
        /// <param name="groupNumber"></param>
        /// <param name="author"></param>
        /// <param name="ciphertext"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="retryFrom"></param>
        /// <param name="skip_unpad"></param>
        public object DecryptGroupMessage(ProtocolTreeNode node, string groupNumber, string author, byte[] ciphertext, string type, string id, string t, string retryFrom = null, bool skip_unpad = false)
        {
            string _Version = "1";

            #region Pkmsg Routine

            if (type == "pkmsg")
            {
                if (_V2Jids.Contains(ExtractNumber(author)))
                    _Version = "2";
                try
                {
                    PreKeyWhisperMessage preKeyWhisperMessage = new PreKeyWhisperMessage(ciphertext);
                    SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(author));
                    byte[] plaintext = sessionCipher.Decrypt(preKeyWhisperMessage);
                    String text = WhatsApp.SysEncoding.GetString(plaintext);

                    if (_Version == "2" && !skip_unpad)
                    {
                        String ret = UnPadv2Plaintext(text);
                        return WhatsApp.SysEncoding.GetBytes(ret);
                    }
                }
                catch (Exception e)
                {
                    if (e is UntrustedIdentityException)
                    {
                        _AxolotKeyStore.ClearRecipient(ExtractNumber(author));
                    }
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + e.Message);
                    ErrorAxolotl(e.Message);
                    return false;
                }
            }

            #endregion Pkmsg Routine

            #region WhisperMessage Routine

            if (type == "msg")
            {
                if (_V2Jids.Contains(ExtractNumber(author)))
                    _Version = "2";
                try
                {
                    WhisperMessage preKeyWhisperMessage = new WhisperMessage(ciphertext);
                    SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(author));
                    byte[] plaintext = sessionCipher.Decrypt(preKeyWhisperMessage);
                    String text = WhatsApp.SysEncoding.GetString(plaintext);

                    if (_Version == "2" && !skip_unpad)
                    {
                        String ret = UnPadv2Plaintext(text);
                        return WhatsApp.SysEncoding.GetBytes(ret);
                    }
                }
                catch (Exception e)
                {
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + e.Message);
                    ErrorAxolotl(e.Message);
                    return false;
                }
            }

            #endregion WhisperMessage Routine

            #region Group Message Cipher Routine

            if (type == "skmsg")
            {
                if (_V2Jids.Contains(ExtractNumber(author)))
                    _Version = "2";
                try
                {
                    GroupCipher sessionCipher = GetGroupCipher(groupNumber, ExtractNumber(author));
                    byte[] plaintext = sessionCipher.Decrypt(ciphertext);
                    String text = WhatsApp.SysEncoding.GetString(plaintext);

                    if (_Version == "2" && !skip_unpad)
                    {
                        String ret = UnPadv2Plaintext(text);
                        return WhatsApp.SysEncoding.GetBytes(ret);
                    }
                }
                catch (Exception e)
                {
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + e.Message);
                    ErrorAxolotl(e.Message);

                    if (retryFrom != null)
                    {
                        author = retryFrom;
                    }
                    this.SendRetry(node, GetJID(author), id, t);
                    return false;
                }
            }

            #endregion Group Message Cipher Routine

            return false;
        }

        /// <summary>
        /// Decrypt an Incomming Message
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ciphertext"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="retry_from"></param>
        /// <param name="skip_unpad"></param>
        public object DecryptMessage(string from, byte[] ciphertext, string type, string id, string t, string retry_from = null, bool skip_unpad = false)
        {
            string _Version = "1";

            #region Pkmsg Routine

            if (type == "pkmsg")
            {
                if (_V2Jids.Contains(ExtractNumber(from)))
                    _Version = "2";
                try
                {
                    PreKeyWhisperMessage preKeyWhisperMessage = new PreKeyWhisperMessage(ciphertext);
                    SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(from));
                    byte[] plaintext = sessionCipher.Decrypt(preKeyWhisperMessage);
                    String text = WhatsApp.SysEncoding.GetString(plaintext);

                    if (_Version == "2" && !skip_unpad)
                    {
                        String ret = UnPadv2Plaintext(text);
                        return WhatsApp.SysEncoding.GetBytes(ret);
                    }
                }
                catch (Exception e)
                {
                    if (e is UntrustedIdentityException)
                    {
                        _AxolotKeyStore.ClearRecipient(ExtractNumber(from));
                    }
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + e.Message);
                    ErrorAxolotl(e.Message);
                    return false;
                }
            }

            #endregion Pkmsg Routine

            #region WhisperMessage Routine

            if (type == "msg")
            {
                if (_V2Jids.Contains(ExtractNumber(from)))
                    _Version = "2";
                try
                {
                    WhisperMessage preKeyWhisperMessage = new WhisperMessage(ciphertext);
                    SessionCipher sessionCipher = GetSessionCipher(ExtractNumber(from));
                    byte[] plaintext = sessionCipher.Decrypt(preKeyWhisperMessage);
                    String text = WhatsApp.SysEncoding.GetString(plaintext);

                    if (_Version == "2" && !skip_unpad)
                    {
                        String ret = UnPadv2Plaintext(text);
                        return WhatsApp.SysEncoding.GetBytes(ret);
                    }
                }
                catch (Exception e)
                {
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + e.Message);
                    ErrorAxolotl(e.Message);
                    return false;
                }
            }

            #endregion WhisperMessage Routine

            return false;
        }

        public String EncodeInt7Bit(int value)
        {
            int v = value;
            String ret = "";
            while (v >= 0x80)
            {
                ret += Convert.ToChar((v | 0x80) % 256);
                v >>= 7;
            }
            ret += Convert.ToChar(v % 256);

            return ret;
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
        /// Return the stored group cypher for this number
        /// </summary>
        public GroupCipher GetGroupCipher(String groupId, String sender)
        {
            if (_GroupCiphers.ContainsKey(groupId))
            {
                return _GroupCiphers[groupId];
            }
            else
            {
                AxolotlAddress address = new AxolotlAddress(sender, 1);
                SenderKeyName senderKeyId = new SenderKeyName(groupId, address);
                _GroupCiphers.Add(groupId, new GroupCipher(_AxolotKeyStore, senderKeyId));
                return _GroupCiphers[groupId];
            }
        }

        /// <summary>
        /// Return the stored session cypher for this number
        /// </summary>
        public SessionCipher GetSessionCipher(string number)
        {
            if (_SessionCiphers.ContainsKey(number))
            {
                return _SessionCiphers[number];
            }
            else
            {
                AxolotlAddress address = new AxolotlAddress(number, 1);
                _SessionCiphers.Add(number, new SessionCipher(_AxolotKeyStore, _AxolotKeyStore, _AxolotKeyStore, _AxolotKeyStore, address));
                return _SessionCiphers[number];
            }
        }

        public void HandleGroupV2InfoResponse(ProtocolTreeNode groupNode, bool FromGetGroups = false)
        {
            String creator = groupNode.GetAttribute("creator");
            String creation = groupNode.GetAttribute("creation");
            String subject = groupNode.GetAttribute("subject");
            String groupID = groupNode.GetAttribute("id");

            List<String> participants = new List<string>();
            List<String> admins = new List<string>();

            if (groupNode.GetChild(0) != null)
            {
                foreach (ProtocolTreeNode child in groupNode.GetAllChildren())
                {
                    participants.Add(child.GetAttribute("jid"));
                    if (child.GetAttribute("type").Equals("admin"))
                    {
                        admins.Add(child.GetAttribute("jid"));
                    }
                }
            }

            /*
            $this->parent->eventManager()->fire('onGetGroupV2Info', [
                $this->phoneNumber,
                $groupID,
                $creator,
                $creation,
                $subject,
                $participants,
                $admins,
                $fromGetGroups,
                    ]
            );
            */
        }

        public void HandleMessage(ProtocolTreeNode node, bool autoReceipt)
        {
            this.PushMessageToQueue(node);

            //Php
            if (node.HashChild("x") && !String.IsNullOrEmpty(LastId) && LastId.Equals(node.GetAttribute("id")))
            {
                this.SendNextMessage();
            }

            if (!String.IsNullOrEmpty(node.GetAttribute("notify")))
            {
                string notifyName = node.GetAttribute("notify");
                this.FireOnGetContactName(node.GetAttribute("from"), notifyName);
            }
            if (node.GetAttribute("type") == "error")
            {
                throw new NotImplementedException(node.NodeString());
            }


            //Process Message Node
            if (node.GetChild("body") != null || node.GetChild("enc") != null)
            {
                // Text Message
                // Encrypted Messages Have No Body Bode. Instead, the Encrypted Cipher Text is Provided within the Enc Node
                Pair<bool, ProtocolTreeNode> ret = null;
                if (node.GetChild("enc") != null)
                {
                    ret = ProcessEncryptedNode(node);
                    node = ret.Second();
                }

                ProtocolTreeNode contentNode = node.GetChild("body") ?? node.GetChild("enc");
                if (contentNode != null)
                {
                    //Encrypted Messages
                    if (ret != null && ret.First())
                    {
                        this.FireOnGetMessage(node, node.GetAttribute("from"), node.GetAttribute("id"), node.GetAttribute("notify"), WhatsApp.SysEncoding.GetString(contentNode.GetData()), autoReceipt);
                        if (autoReceipt)
                        {
                            this.SendMessageReceived(node);
                        }
                    }
                    else if (ret == null)
                    {
                        this.FireOnGetMessage(node, node.GetAttribute("from"), node.GetAttribute("id"), node.GetAttribute("notify"), WhatsApp.SysEncoding.GetString(contentNode.GetData()), autoReceipt);
                        if (autoReceipt)
                        {
                            this.SendMessageReceived(node);
                        }
                    }
                }
            }

            if (node.GetChild("media") != null)
            {
                //Media message
                ProtocolTreeNode media = node.GetChild("media");

                //Define Variables in Switch
                string file, url;
                int size;
                byte[] preview, dat;
                string _ID = node.GetAttribute("id");
                string _From = node.GetAttribute("from");
                string _UserName = node.GetAttribute("notify");
                switch (media.GetAttribute("type"))
                {
                    case "image":
                        url = media.GetAttribute("url");
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        preview = media.GetData();
                        this.FireOnGetMessageImage(node, _From, _ID, file, size, url, preview, _UserName);
                        break;

                    case "audio":
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        url = media.GetAttribute("url");
                        preview = media.GetData();
                        this.FireOnGetMessageAudio(node, _From, _ID, file, size, url, preview, _UserName);
                        break;

                    case "video":
                        file = media.GetAttribute("file");
                        size = Int32.Parse(media.GetAttribute("size"));
                        url = media.GetAttribute("url");
                        preview = media.GetData();
                        this.FireOnGetMessageVideo(node, _From, _ID, file, size, url, preview, _UserName);
                        break;

                    case "location":
                        double lon = Double.Parse(media.GetAttribute("longitude"), CultureInfo.InvariantCulture);
                        double lat = Double.Parse(media.GetAttribute("latitude"), CultureInfo.InvariantCulture);
                        preview = media.GetData();
                        name = media.GetAttribute("name");
                        url = media.GetAttribute("url");
                        this.FireOnGetMessageLocation(node, _From, _ID, lon, lat, url, name, preview, _UserName);
                        break;

                    case "vcard":
                        ProtocolTreeNode vcard = media.GetChild("vcard");
                        name = vcard.GetAttribute("name");
                        dat = vcard.GetData();
                        this.FireOnGetMessageVcard(node, _From, _ID, name, dat);
                        break;
                }
                this.SendMessageReceived(node);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public byte[] Hex2Bin(String hex)
        {
            //return (from i in Enumerable.Range(0, hex.Length / 2)
            //        select Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public void Login(byte[] nextChallenge = null)
        {
            //Reset Stuff
            this.BinReader.Key = null;
            this.BinWriter.Key = null;
            this._ChallengeData = null;

            if (nextChallenge != null)
            {
                this._ChallengeData = nextChallenge;
            }

            string resource = String.Format(@"{0}-{1}-{2}", WhatsConstants.Platform, WhatsConstants.WhatsAppVer, WhatsConstants.WhatsPort);
            byte[] data = this.BinWriter.StartStream(WhatsConstants.WhatsAppServer, resource);
            ProtocolTreeNode feat = this.CreateFeaturesNode();
            ProtocolTreeNode auth = this.AddAuth();
            this.SendData(data);
            this.SendData(this.BinWriter.Write(feat, false));
            this.SendData(this.BinWriter.Write(auth, false));

            this.PollMessage();//Stream Start
            this.PollMessage();//Features
            this.PollMessage();//Challenge or Success

            if (this.loginStatus != ApiBase.CONNECTION_STATUS.LOGGEDIN)
            {
                //OneShot Failed
                ProtocolTreeNode authResp = this.CreateAuthResponseNode();
                this.SendData(this.BinWriter.Write(authResp, false));
                this.PollMessage();
            }

            DebugAdapter.Instance.FireOnPrintDebug(String.Format("{0} Successfully Logged In", this.phoneNumber));

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TicketCounter.SetLoginTime(unixTimestamp.ToString());

            this.SendAvailableForChat(this.name, this.hidden);
            this.SendGetPrivacyList();
            this.SendGetClientConfig();

            if (this.LoadPreKeys() == null)
                this.SendSetPreKeys(true);
        }

        public void LoginWithPassword(String password, byte[] nextChallenge = null)
        {
            base.password = password;

            //Reset Stuff
            this.BinReader.Key = null;
            this.BinWriter.Key = null;
            this._ChallengeData = null;

            if (nextChallenge != null)
            {
                this._ChallengeData = nextChallenge;
            }

            string resource = String.Format(@"{0}-{1}-{2}", WhatsConstants.Platform, WhatsConstants.WhatsAppVer, WhatsConstants.WhatsPort);
            byte[] data = this.BinWriter.StartStream(WhatsConstants.WhatsAppServer, resource);
            ProtocolTreeNode feat = this.CreateFeaturesNode();
            ProtocolTreeNode auth = this.AddAuth();
            this.SendData(data);
            this.SendData(this.BinWriter.Write(feat, false));
            this.SendData(this.BinWriter.Write(auth, false));

            this.PollMessage();//Stream Start
            this.PollMessage();//Features
            this.PollMessage();//Challenge or Success

            if (this.loginStatus != ApiBase.CONNECTION_STATUS.LOGGEDIN)
            {
                //OneShot Failed
                ProtocolTreeNode authResp = this.CreateAuthResponseNode();
                this.SendData(this.BinWriter.Write(authResp, false));
                this.PollMessage();
            }

            DebugAdapter.Instance.FireOnPrintDebug(String.Format("{0} Successfully Logged In", this.phoneNumber));

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TicketCounter.SetLoginTime(unixTimestamp.ToString());

            this.SendAvailableForChat(this.name, this.hidden);
            this.SendGetPrivacyList();
            this.SendGetClientConfig();

            if (this.LoadPreKeys() == null)
                this.SendSetPreKeys(true);
        }

        public String PadMessage(String message)
        {
            String padding = String.Empty;
            padding += Convert.ToChar(10);
            padding += EncodeInt7Bit(message.Length);
            padding += message;
            padding += Convert.ToChar(1);
            return padding;
        }

        public bool PollMessage(bool autoReceipt = true)
        {
            if (this.loginStatus == ApiBase.CONNECTION_STATUS.CONNECTED || this.loginStatus == ApiBase.CONNECTION_STATUS.LOGGEDIN)
            {
                byte[] nodeData;
                try
                {
                    nodeData = this.whatsNetwork.ReadNextNode();
                    if (nodeData != null)
                    {
                        return this.ProcessInboundData(nodeData, autoReceipt);
                    }
                }
                catch (ConnectionException)
                {
                    this.Disconnect();
                }
            }
            return false;
        }

        public void PollMessages(bool autoReceipt = true)
        {
            m_usePoolMessages = true;
            while (PollMessage(autoReceipt))
            {
                ;
            }
            m_usePoolMessages = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Pair<Boolean, ProtocolTreeNode> ProcessEncryptedNode(ProtocolTreeNode node)
        {
            String from = node.GetAttribute("from");
            String author = String.Empty;
            String groupNumber = String.Empty;
            String version = String.Empty;
            String encType = String.Empty;
            byte[] encMsg = null;

            ProtocolTreeNode rtnNode = null;

            Pair<Boolean, ProtocolTreeNode> ret = new Pair<bool, ProtocolTreeNode>(false, node);

            if (from.Contains(WhatsConstants.WhatsAppServer))
            {
                author = ExtractNumber(node.GetAttribute("from"));
                version = node.GetChild("enc").GetAttribute("v");
                encType = node.GetChild("enc").GetAttribute("type");
                encMsg = node.GetChild("enc").GetData();

                if (!_AxolotKeyStore.ContainsSession(new AxolotlAddress(author, 1)))
                {
                    //We don't Have the Session to Decrypt, Save It in Pending and Process it Later
                    this.AddPendingNode(node);
                    Helper.DebugAdapter.Instance.FireOnPrintDebug("Info : Requesting Cipher Keys From " + author);
                    this.SendGetCipherKeysFromUser(author);
                }
                else
                {
                    //Decrypt the Message with The Session
                    if (node.GetChild("enc").GetAttribute("count") == "")
                        SetRetryCounter(node.GetAttribute("id"), 1);
                    if (version == "2")
                    {
                        if (!_V2Jids.Contains(author))
                            _V2Jids.Add(author);
                    }

                    object plaintext = DecryptMessage(from, encMsg, encType, node.GetAttribute("id"), node.GetAttribute("t"));

                    if (plaintext is bool && false == (bool)plaintext)
                    {
                        SendRetry(node, from, node.GetAttribute("id"), node.GetAttribute("t"));
                        Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + String.Format("Couldn't Decrypt Message ID {0} From {1}. Retrying.", node.GetAttribute("id"), author));
                        ret = new Pair<bool, ProtocolTreeNode>(false, node);
                        return ret; // Could Not Decrypt
                    }

                    // Success Now Lets Clear All Setting and Return Node
                    if (_RetryCounters.ContainsKey(node.GetAttribute("id")))
                        _RetryCounters.Remove(node.GetAttribute("id"));
                    if (_RetryNodes.ContainsKey(node.GetAttribute("id")))
                        _RetryNodes.Remove(node.GetAttribute("id"));

                    switch (node.GetAttribute("type"))
                    {
                        case "text":
                            {
                                //Convert to List.
                                List<ProtocolTreeNode> children = node._Children.ToList();
                                List<KeyValue> attributeHash = node._AttributeHash.ToList();
                                children.Add(new ProtocolTreeNode("body", null, null, (byte[])plaintext));
                                rtnNode = new ProtocolTreeNode(node._Tag, attributeHash.ToArray(), children.ToArray(), node._Data);
                            }
                            break;
                        case "media":
                            {
                                //Convert to list.
                                List<ProtocolTreeNode> children = (from q in node._Children where !string.Equals(q._Tag, "enc") select q).ToList();
                                List<KeyValue> attributeHash = node._AttributeHash.ToList();
                                byte[] data = null;

                                String text = plaintext as string;
                                if (text != null)
                                {
                                    data = System.Text.Encoding.UTF8.GetBytes(text);
                                }

                                children.Add(new ProtocolTreeNode("media", null, null, data));
                                rtnNode = new ProtocolTreeNode(node._Tag, attributeHash.ToArray(), children.ToArray(), node._Data);
                            }
                            break;
                    }

                    Helper.DebugAdapter.Instance.FireOnPrintDebug("info : " + string.Format("Decrypted Message with {0} Id From {1}", node.GetAttribute("id"), author));
                    ret = new Pair<bool, ProtocolTreeNode>(true, rtnNode);
                    return ret;
                }
            }
            //Is Group
            /*
            else
            {
                author = ExtractNumber(node.GetAttribute("participant"));
                groupNumber = ExtractNumber(node.GetAttribute("from"));

                foreach (ProtocolTreeNode child in node.GetAllChildren())
                {
                    encType = child.GetAttribute("type");
                    encMsg = child.GetData();
                    from = node.GetAttribute("participant");
                    version = child.GetAttribute("v");

                    if (encType.Equals("pkmsg") || encType.Equals("msg"))
                    {
                        if (!_AxolotKeyStore.ContainsSession(new AxolotlAddress(author, 1)))
                        {
                            this.AddPendingNode(node);
                            Helper.DebugAdapter.Instance.FireOnPrintDebug("Info : Requesting Cipher Keys From " + author);
                            this.SendGetCipherKeysFromUser(author);
                            break;
                        }
                        else
                        {
                            if (node.GetChild("enc").GetAttribute("count") == "")
                                SetRetryCounter(node.GetAttribute("id"), 1);

                            if (version == "2")
                            {
                                if (!_V2Jids.Contains(author))
                                    _V2Jids.Add(author);
                            }

                            bool skipUnpad = node.GetChild("enc").GetAttribute("type") != "skmsg";
                            object senderKeyBytes = DecryptMessage(from, encMsg, encType, node.GetAttribute("id"), node.GetAttribute("t"), node.GetAttribute("from"), skipUnpad);

                            if (senderKeyBytes is bool && false == (bool)senderKeyBytes)
                            {
                                SendRetry(node, from, node.GetAttribute("id"), node.GetAttribute("t"));
                                Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + String.Format("Couldn't Decrypt Message ID {0} From {1}. Retrying.", node.GetAttribute("id"), author));
                                ret = new Pair<bool, ProtocolTreeNode>(false, node);
                                return ret; // Could Not Decrypt
                            }
                            else
                            {
                                byte[] data = (byte[])senderKeyBytes;
                                byte[] message = null;
                                SenderKeyGroupMessage senderKeyGroupMessage = null;

                                if (!skipUnpad)
                                {
                                    senderKeyGroupMessage = new SenderKeyGroupMessage(data);
                                }
                                else
                                {
                                    SenderKeyGroupData senderKeyGroupData = null;
                                    try
                                    {
                                        senderKeyGroupData = new SenderKeyGroupData(data);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            String text = WhatsApp.SysEncoding.GetString(data);
                                            text = text.Substring(0, text.Length - 2);
                                            byte[] dataEdit = WhatsApp.SysEncoding.GetBytes(text);
                                            senderKeyGroupData = new SenderKeyGroupData(dataEdit);
                                        }
                                        catch (Exception)
                                        {
                                            ret = new Pair<bool, ProtocolTreeNode>(false, node);
                                            return ret;
                                        }
                                    }

                                    message = senderKeyGroupData.GetMessage();
                                    senderKeyGroupMessage = senderKeyGroupData.GetSenderKey();
                                }

                                SenderKeyDistributionMessage senderKey = new SenderKeyDistributionMessage(senderKeyGroupMessage.Serialize());
                                GroupSessionBuilder builder = new GroupSessionBuilder(_AxolotKeyStore);
                                AxolotlAddress senderAddress = new AxolotlAddress(author, 1);
                                SenderKeyName senderKeyName = new SenderKeyName(groupNumber, senderAddress);

                                builder.Process(senderKeyName, senderKey);

                                if (message != null)
                                {
                                    this.SendReceipt(node, "receipt", ApiBase.GetJID(this.phoneNumber));

                                    List<ProtocolTreeNode> childsNodes = node._Children.ToList();
                                    childsNodes.Add(new ProtocolTreeNode("body", null, null, message));
                                    node.SetChildren(childsNodes);

                                    ret = new Pair<bool, ProtocolTreeNode>(false, node);
                                    return ret;
                                }
                            }
                        }
                    }
                    else if (encType.Equals("skmsg"))
                    {
                        version = child.GetAttribute("v");

                        if (version == "2")
                        {
                            if (!_V2Jids.Contains(author))
                                _V2Jids.Add(author);
                        }

                        object plaintext = DecryptGroupMessage(node, groupNumber, author, encMsg, encType, node.GetAttribute("type"), node.GetAttribute("id"), node.GetAttribute("t"));

                        if (plaintext is bool && false == (bool)plaintext)
                        {
                            SendRetry(node, from, node.GetAttribute("id"), node.GetAttribute("t"), node.GetAttribute("participant"));
                            Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + String.Format("Couldn't Decrypt Message ID {0} From {1}. Retrying.", node.GetAttribute("id"), from));
                            ret = new Pair<bool, ProtocolTreeNode>(false, node);
                            return ret; // Could Not Decrypt
                        }

                        // Success Now Lets Clear All Setting and Return Node
                        if (_RetryCounters.ContainsKey(node.GetAttribute("id")))
                            _RetryCounters.Remove(node.GetAttribute("id"));
                        if (_RetryNodes.ContainsKey(node.GetAttribute("id")))
                            _RetryNodes.Remove(node.GetAttribute("id"));

                        this.SendReceipt(node, "receipt", ApiBase.GetJID(this.phoneNumber));

                        List<ProtocolTreeNode> childsNodes = node._Children.ToList();
                        childsNodes.Add(new ProtocolTreeNode("body", null, null, (byte[])plaintext));
                        node.SetChildren(childsNodes);

                        ret = new Pair<bool, ProtocolTreeNode>(false, node);
                        return ret;
                    }
                }
            }*/

            return ret;
        }

        /// <summary>
        /// Intercept IQ and Precess the Keys
        /// </summary>
        /// <param name="node"></param>
        public ProtocolTreeNode[] ProcessIqTreeNode(ProtocolTreeNode node)
        {
            try
            {
                if (_CipherKeys.Contains(node.GetAttribute("id")))
                {
                    _CipherKeys.Remove(node.GetAttribute("id"));

                    foreach (ProtocolTreeNode child in node._Children)
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
                        SessionBuilder sessionBuilder = new SessionBuilder(_AxolotKeyStore, _AxolotKeyStore, _AxolotKeyStore, _AxolotKeyStore, new AxolotlAddress(ExtractNumber(jid), 1));

                        // Now Do the Work Return Nodelist
                        sessionBuilder.Process(preKeyBundle);

                        if (_PendingNodes.ContainsKey(ExtractNumber(jid)))
                        {
                            ProtocolTreeNode[] pendingNodes = _PendingNodes[ExtractNumber(jid)].ToArray();
                            _PendingNodes.Remove(ExtractNumber(jid));
                            return pendingNodes;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public void ResetEncryption()
        {
            if (this._AxolotKeyStore != null)
            {
                _AxolotKeyStore.Clear();
            }
            this._RetryCounters = new Dictionary<String, int>();

            this.SendSetPreKeys();
            this.PollMessage();
            this.PollMessage();

            this.Disconnect();
            this.Connect();

            this.Login();
            this.SendGetPrivacyList();
            this.SendGetClientConfig();
            foreach (KeyValuePair<string, ProtocolTreeNode> protocolTreeNode in _RetryNodes)
            {
                this.ProcessInboundData(protocolTreeNode.Value.GetData());
            }
        }

        public void SendActiveStatus()
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "active") });
            this.SendNode(node);
        }

        public void SendAddParticipants(string gjid, IEnumerable<string> participants)
        {
            string id = TicketCounter.MakeId();
            this.SendVerbParticipants(gjid, participants, id, "add");
        }

        public void SendAvailableForChat(string nickName = null, bool isHidden = false)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", (!String.IsNullOrEmpty(nickName) ? nickName : this.name)) });
            this.SendNode(node);
        }

        public void SendChangeNumber(String number, String identity)
        {
            string id = TicketCounter.MakeId();

            ProtocolTreeNode usernameNode = new ProtocolTreeNode("username", null, System.Text.Encoding.UTF8.GetBytes(number));
            ProtocolTreeNode passwordNode = new ProtocolTreeNode("password", null, System.Text.Encoding.UTF8.GetBytes(identity));

            ProtocolTreeNode modifyNode = new ProtocolTreeNode("modify", null, new[] { usernameNode, passwordNode }, null);

            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "urn:xmpp:whatsapp:account"), new KeyValue("to", WhatsConstants.WhatsAppServer) },
                new[] { modifyNode }, null);
            this.SendNode(node);
        }

        public void SendClientConfig()
        {
            string v = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("platform", WhatsConstants.Platform), new KeyValue("version", WhatsConstants.WhatsAppVer)});
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendClientConfig(string platform, string lg, string lc, Uri pushUri, bool preview, bool defaultSetting, bool groupsSetting, IEnumerable<GroupSetting> groups, Action onCompleted, Action<int> onError)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                        new[]
                                        {
                                            new KeyValue("id", id), new KeyValue("type", "set"),
                                            new KeyValue("to", "") //this.Login.Domain)
                                        },
                                        new ProtocolTreeNode[]
                                        {
                                            new ProtocolTreeNode("config",
                                            new[]
                                                {
                                                    new KeyValue("xmlns","urn:xmpp:whatsapp:push"),
                                                    new KeyValue("platform", platform),
                                                    new KeyValue("lg", lg),
                                                    new KeyValue("lc", lc),
                                                    new KeyValue("clear", "0"),
                                                    new KeyValue("id", pushUri.ToString()),
                                                    new KeyValue("preview",preview ? "1" : "0"),
                                                    new KeyValue("default",defaultSetting ? "1" : "0"),
                                                    new KeyValue("groups",groupsSetting ? "1" : "0")
                                                },
                                            this.ProcessGroupSettings(groups))
                                        });
            this.SendNode(node);
        }

        public void SendClose()
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.SendNode(node);
        }

        public void SendComposing(string to)
        {
            this.SendChatState(to, "composing");
        }

        public String SendCreateGroupChat(string subject, IEnumerable<string> participants)
        {
            _GroupCreateId = TicketCounter.MakeId();
            IEnumerable<ProtocolTreeNode> participant = from jid in participants select new ProtocolTreeNode("participant", new[] { new KeyValue("jid", GetJID(jid)) });
            ProtocolTreeNode child = new ProtocolTreeNode("create", new[] { new KeyValue("subject", subject) }, new ProtocolTreeNode[] { (ProtocolTreeNode)participant });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[]
            { new KeyValue("id", _GroupCreateId), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", "g.us") }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
            this.WaitForServer(_GroupCreateId);
            return this._GroupId;
        }

        public void SendDeleteAccount()
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                    new KeyValue[]
                                        {
                                            new KeyValue("id", id),
                                            new KeyValue("type", "get"),
                                            new KeyValue("to", WhatsConstants.WhatsAppServer),
                                            new KeyValue("xmlns", "urn:xmpp:whatsapp:account")
                                        },
                                    new ProtocolTreeNode[]
                                        {
                                            new ProtocolTreeNode("remove", null)
                                        });
            this.SendNode(node);
            this.WaitForServer(id);
        }

        public void SendDeleteFromRoster(string jid)
        {
            string v = TicketCounter.MakeId();
            ProtocolTreeNode innerChild = new ProtocolTreeNode("item", new[] { new KeyValue("jid", jid), new KeyValue("subscription", "remove") });
            ProtocolTreeNode child = new ProtocolTreeNode("query", new[] { new KeyValue("xmlns", "jabber:iq:roster") }, new ProtocolTreeNode[] { innerChild });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "set"), new KeyValue("id", v) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendEndGroupChat(string gjid)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("group", new[] { new KeyValue("action", "delete") });
            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", gjid) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendExtendAccount()
        {
            string id = TicketCounter.MakeId();

            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                    new KeyValue[]
                                        {
                                            new KeyValue("id", id),
                                            new KeyValue("xmlns", "urn:xmpp:whatsapp:account"),
                                            new KeyValue("type", "set"),
                                            new KeyValue("to", WhatsConstants.WhatsAppServer)
                                        },
                                    new ProtocolTreeNode[]
                                        {
                                            new ProtocolTreeNode("extend", null)
                                        });
            this.SendNode(node);
        }

        public void SendGetBroadcastLists()
        {
            _GetListsId = TicketManager.GenerateId();

            ProtocolTreeNode listsNode = new ProtocolTreeNode("lists", null, null, null);
            ProtocolTreeNode node = new ProtocolTreeNode("id", new[]
            {
                new KeyValue("id", _GetListsId), new KeyValue("xmlns", "w:b"), new KeyValue("type", "get"), new KeyValue("to", WhatsConstants.WhatsAppServer)
            }, new ProtocolTreeNode[] { listsNode }, null);
            this.SendNode(node);
        }

        /// <summary>
        /// Send a request to Get Cipher Keys From an User
        /// </summary>
        /// <param name="number">Phone number of the user you want to get the cipher keys</param>
        /// <param name="replaceKeyIn"></param>
        public void SendGetCipherKeysFromUser(string number, bool replaceKeyIn = false)
        {
            _ReplaceKey = replaceKeyIn;
            String msgId = TicketManager.GenerateId();

            _CipherKeys.Add(msgId);

            ProtocolTreeNode user = new ProtocolTreeNode("user", new[] {
                    new KeyValue("jid", ApiBase.GetJID(number)),
                    }, null, null);

            ProtocolTreeNode keyNode = new ProtocolTreeNode("key", null, new ProtocolTreeNode[] { user }, null);
            ProtocolTreeNode Node = new ProtocolTreeNode("iq", new[] {
                    new KeyValue("id", msgId),
                    new KeyValue("xmlns", "encrypt"),
                    new KeyValue("type", "get"),
                    new KeyValue("to", WhatsConstants.WhatsAppServer)
                   }, new ProtocolTreeNode[] { keyNode }, null);

            this.SendNode(Node);
            this.WaitForServer(msgId);
        }

        public void SendGetClientConfig()
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("config", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:push") });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetDirty()
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("status", new[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty") });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", WhatsConstants.WhatsAppServer) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetGroupInfo(string gjid)
        {
            _GetGroupv2InfoId = TicketCounter.MakeId();

            ProtocolTreeNode child = new ProtocolTreeNode("query", null);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[]
            {
                new KeyValue("id", _GetGroupv2InfoId), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(gjid))
            }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetGroups()
        {
            _GetGroupsId = TicketCounter.MakeId();
            this.SendGetGroups(_GetGroupsId, "participating");
        }

        public void SendGetGroups(string id, string type)
        {
            ProtocolTreeNode child = new ProtocolTreeNode(type, null);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", "g.us") }, child);
            this.SendNode(node);
        }

        public void SendGetGroupV2Info(string gjid)
        {
            _GetGroupv2InfoId = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("query", new List<KeyValue> { new KeyValue("request", "interactive") });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[]
            {
                new KeyValue("id", _GetGroupv2InfoId), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(gjid))
            }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }

        public void SendGetNormalizedJid(String countryCode, String number)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode ccNode = new ProtocolTreeNode("cc", null, null, Encoding.UTF8.GetBytes(countryCode));
            ProtocolTreeNode inNode = new ProtocolTreeNode("in", null, null, Encoding.UTF8.GetBytes(number));
            ProtocolTreeNode normalizeNode = new ProtocolTreeNode("normalize", null, new List<ProtocolTreeNode> { ccNode, inNode }, null);

            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                    new KeyValue[]
                                        {
                                            new KeyValue("id", id),
                                            new KeyValue("xmlns", "urn:xmpp:whatsapp:account"),
                                            new KeyValue("type", "get"),
                                            new KeyValue("to", WhatsConstants.WhatsAppServer)
                                        }, normalizeNode);
            this.SendNode(node);
        }
        /**
         * Removes an account from WhatsApp.
         *
         * @param string lg       Language
         * @param string lc       Country
         * @param string feedback User Feedback
         */
        public void SendGetOwningGroups()
        {
            string id = TicketCounter.MakeId();
            this.SendGetGroups(id, "owning");
        }

        public void SendGetParticipants(string gjid)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("list", null);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", WhatsApp.GetJID(gjid)) }, child);
            this.SendNode(node);
        }

        public string SendGetPhoto(string jid, string expectedPhotoId, bool largeFormat)
        {
            string id = TicketCounter.MakeId();
            List<KeyValue> attrList = new List<KeyValue>();
            if (!largeFormat)
            {
                attrList.Add(new KeyValue("type", "preview"));
            }
            if (expectedPhotoId != null)
            {
                attrList.Add(new KeyValue("id", expectedPhotoId));
            }
            ProtocolTreeNode child = new ProtocolTreeNode("picture", attrList.ToArray());
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w:profile:picture"), new KeyValue("to", WhatsAppApi.WhatsApp.GetJID(jid)) }, child);
            this.SendNode(node);
            return id;
        }

        public void SendGetPhotoIds(IEnumerable<string> jids)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", GetJID(this.phoneNumber)) },
                new ProtocolTreeNode("list", new[] { new KeyValue("xmlns", "w:profile:picture") },
                    (from jid in jids select new ProtocolTreeNode("user", new[] { new KeyValue("jid", jid) })).ToArray<ProtocolTreeNode>()));
            this.SendNode(node);
        }

        public void SendGetPrivacyList()
        {
            _PrivacyId = TicketCounter.MakeId();
            ProtocolTreeNode innerChild = new ProtocolTreeNode("list", new[] { new KeyValue("name", "default") });
            ProtocolTreeNode child = new ProtocolTreeNode("query", null, innerChild);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", _PrivacyId), new KeyValue("type", "get"), new KeyValue("xmlns", "jabber:iq:privacy") }, child);
            this.SendNode(node);
        }

        public void SendGetPrivacySettings()
        {
            _PrivacySettingsId = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] {
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("id", _PrivacySettingsId),
                new KeyValue("type", "get"),
                new KeyValue("xmlns", "privacy")
            }, new ProtocolTreeNode[] {
                new ProtocolTreeNode("privacy", null)
            });
            this.SendNode(node);
        }

        public void SendGetServerProperties()
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                new[] { new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("xmlns", "w"), new KeyValue("to", WhatsConstants.WhatsAppServer) },
                new ProtocolTreeNode("props", null));
            this.SendNode(node);
        }

        public void SendGetStatuses(string[] jids)
        {
            List<ProtocolTreeNode> targets = new List<ProtocolTreeNode>();
            foreach (string jid in jids)
            {
                targets.Add(new ProtocolTreeNode("user", new[] { new KeyValue("jid", GetJID(jid)) }, null, null));
            }

            _GetStatusesId = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] {
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("type", "get"),
                new KeyValue("xmlns", "status"),
                new KeyValue("id",_GetStatusesId)
            }, new[] {
                new ProtocolTreeNode("status", null, targets.ToArray(), null)
            }, null);

            this.SendNode(node);
        }

        public void SendInactive()
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "inactive") });
            this.SendNode(node);
        }

        public void SendLeaveGroup(string gjid)
        {
            this.SendLeaveGroups(new string[] { gjid });
        }

        public void SendLeaveGroups(IEnumerable<string> gjids)
        {
            string id = TicketCounter.MakeId();
            IEnumerable<ProtocolTreeNode> innerChilds = from gjid in gjids select new ProtocolTreeNode("group", new[] { new KeyValue("id", gjid) });
            ProtocolTreeNode child = new ProtocolTreeNode("leave", null, innerChilds);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", "g.us") }, child);
            this.SendNode(node);
        }

        public string SendMessage(string to, string txt, bool force_plain = true)
        {
            String toNumber = ExtractNumber(to);

            if (!force_plain)
            {
                /* TODO:Send Encrypted Message
                ProtocolTreeNode msgNode = null;
                if (!to.Contains("-"))
                {
                    if (!_AxolotKeyStore.ContainsSession(new AxolotlAddress(toNumber, 1)))
                    {
                        SendGetCipherKeysFromUser(to);
                    }

                    String _Version = String.Empty;
                    String _AlteredText = String.Empty;

                    SessionCipher sessionCipher = GetSessionCipher(toNumber);
                    if (_V2Jids.Contains(toNumber))
                    {
                        _Version = "2";
                        _AlteredText = PadMessage(txt);
                    }
                    else
                    {
                        _Version = "1";
                        _AlteredText = txt;
                    }

                    String type = String.Empty;
                    CipherTextMessage cipherText = sessionCipher.Encrypt(Encoding.UTF8.GetBytes(_AlteredText));
                    if (cipherText is WhisperMessage)
                        type = "msg";
                    else
                        type = "pkmsg";

                    byte[] message = cipherText.Serialize();
                    msgNode = new ProtocolTreeNode("enc", new KeyValue[] { new KeyValue("v", _Version), new KeyValue("type", type) }, null, message);
                }
                else
                {
                    FMessage tmpMessage = new FMessage(GetJID(to), true)
                    {
                        data = txt
                    };
                    this.SendMessage(tmpMessage, this.hidden);
                    return tmpMessage.identifier_key.ToString();
                }

                ProtocolTreeNode plaintextNode = new ProtocolTreeNode("body", null, null, Encoding.UTF8.GetBytes(txt));
                return SendMessageNode(to, msgNode, null, plaintextNode);
                */

                FMessage tmpMessage = new FMessage(GetJID(to), true)
                {
                    data = txt
                };
                this.SendMessage(tmpMessage, this.hidden);
                return tmpMessage.identifier_key.ToString();
            }
            else
            {
                FMessage tmpMessage = new FMessage(GetJID(to), true)
                {
                    data = txt
                };
                this.SendMessage(tmpMessage, this.hidden);
                return tmpMessage.identifier_key.ToString();
            }
        }

        public void SendMessage(FMessage message, bool hidden = false)
        {
            if (message.media_wa_type != FMessage.Type.Undefined)
            {
                this.SendMessageWithMedia(message);
            }
            else
            {
                this.SendMessageWithBody(message, hidden);
            }
        }

        public void SendMessageAudio(string to, byte[] audioData, ApiBase.AudioType audtype)
        {
            FMessage msg = this.GetFmessageAudio(to, audioData, audtype);
            if (msg != null)
            {
                this.SendMessage(msg);
            }
        }

        public string SendMessageBroadcast(string[] to, string message)
        {
            FMessage tmpMessage = new FMessage(string.Empty, true) { data = message, media_wa_type = FMessage.Type.Undefined };
            this.SendMessageBroadcast(to, tmpMessage);
            return tmpMessage.identifier_key.ToString();
        }

        public void SendMessageBroadcast(string[] to, FMessage message)
        {
            if (to != null && to.Length > 0 && !String.IsNullOrEmpty(message?.data))
            {
                ProtocolTreeNode child;
                if (message.media_wa_type == FMessage.Type.Undefined)
                {
                    //Text Broadcast
                    child = new ProtocolTreeNode("body", null, null, WhatsApp.SysEncoding.GetBytes(message.data));
                }
                else
                {
                    throw new NotImplementedException();
                }

                List<ProtocolTreeNode> toNodes = new List<ProtocolTreeNode>();
                foreach (string target in to)
                {
                    toNodes.Add(new ProtocolTreeNode("to", new KeyValue[] { new KeyValue("jid", WhatsAppApi.WhatsApp.GetJID(target)) }));
                }

                ProtocolTreeNode broadcastNode = new ProtocolTreeNode("broadcast", null, toNodes);
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                ProtocolTreeNode messageNode = new ProtocolTreeNode("message", new KeyValue[] {
                    new KeyValue("to", unixTimestamp.ToString() + "@broadcast"),
                    new KeyValue("type", message.media_wa_type == FMessage.Type.Undefined ? "text" : "media"),
                    new KeyValue("id", message.identifier_key.id)
                }, new ProtocolTreeNode[] {
                    broadcastNode,
                    child
                });
                this.SendNode(messageNode);
                this.WaitForServer(message.identifier_key.id);
            }
        }

        public string SendMessageBroadcastAudio(string[] recipients, byte[] audioData, ApiBase.AudioType audtype)
        {
            List<string> foo = new List<string>();
            foreach (string s in recipients)
            {
                foo.Add(GetJID(s));
            }
            string to = string.Join(",", foo.ToArray());
            FMessage msg = this.GetFmessageAudio(to, audioData, audtype);
            if (msg != null)
            {
                this.SendMessage(msg);
                return msg.identifier_key.ToString();
            }

            return "";
        }

        public string SendMessageBroadcastImage(string[] recipients, byte[] imageData, ApiBase.ImageType imgtype)
        {
            List<string> foo = new List<string>();
            foreach (string s in recipients)
            {
                foo.Add(GetJID(s));
            }
            string to = string.Join(",", foo.ToArray());
            FMessage msg = this.GetFmessageImage(to, imageData, imgtype);
            if (msg != null)
            {
                this.SendMessage(msg);
                return msg.identifier_key.ToString();
            }

            return "";
        }

        public string SendMessageBroadcastVideo(string[] recipients, byte[] videoData, ApiBase.VideoType vidtype)
        {
            List<string> foo = new List<string>();
            foreach (string s in recipients)
            {
                foo.Add(GetJID(s));
            }
            string to = string.Join(",", foo.ToArray());
            FMessage msg = this.GetFmessageVideo(to, videoData, vidtype);
            if (msg != null)
            {
                this.SendMessage(msg);
                return msg.identifier_key.ToString();
            }

            return "";
        }

        public string SendMessageImage(string to, byte[] ImageData, ApiBase.ImageType imgtype)
        {
            FMessage msg = this.GetFmessageImage(to, ImageData, imgtype);
            if (msg != null)
            {
                this.SendMessage(msg);
                return msg.identifier_key.ToString();
            }
            return "";
        }

        public string SendMessageLocation(string to, string name, double lat, double lon)
        {
            FMessage tmpMessage = new FMessage(GetJID(to), true) { location_details = name, media_wa_type = FMessage.Type.Location, latitude = lat, longitude = lon };
            this.SendMessage(tmpMessage, this.hidden);
            return tmpMessage.identifier_key.ToString();
        }

        public void SendMessageVcard(string to, string name, string vcard_data)
        {
            FMessage tmpMessage = new FMessage(GetJID(to), true) { data = vcard_data, media_wa_type = FMessage.Type.Contact, media_name = name };
            this.SendMessage(tmpMessage, this.hidden);
        }

        public string SendMessageVideo(string to, byte[] videoData, ApiBase.VideoType vidtype)
        {
            FMessage msg = this.GetFmessageVideo(to, videoData, vidtype);
            if (msg != null)
            {
                this.SendMessage(msg);
                return msg.identifier_key.ToString();
            }
            return "";
        }

        public void SendNop()
        {
            this.SendNode(null);
        }

        public void SendPaused(string to)
        {
            this.SendChatState(to, "paused");
        }

        public void SendPing()
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("ping", null);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[]
            { new KeyValue("id", id), new KeyValue("xmlns", "w:p"), new KeyValue("type", "get"), new KeyValue("to", WhatsConstants.WhatsAppServer) }, child);
            this.SendNode(node);
        }

        public void SendPresenceSubscriptionRequest(string to)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "subscribe"), new KeyValue("to", GetJID(to)) });
            this.SendNode(node);
        }

        public void SendQueryLastOnline(string jid)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("query", null);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[]
            {
                new KeyValue("id", id), new KeyValue("type", "get"), new KeyValue("to", GetJID(jid)), new KeyValue("xmlns", "jabber:iq:last")
            }, child);
            this.SendNode(node);
        }

        public void SendReceipt(ProtocolTreeNode node, String type = "read", String participant = null, String callId = null)
        {
            List<KeyValue> messageHash = new List<KeyValue>();
            if (type.Equals("read"))
            {
                messageHash.Add(new KeyValue("type", type));
            }
            if (participant != null)
            {
                messageHash.Add(new KeyValue("participant", participant));
            }
            messageHash.Add(new KeyValue("to", node.GetAttribute("from")));
            messageHash.Add(new KeyValue("id", node.GetAttribute("id")));
            messageHash.Add(new KeyValue("t", node.GetAttribute("t")));

            ProtocolTreeNode messageNode = null;
            if (callId != null)
            {
                ProtocolTreeNode offerNode = new ProtocolTreeNode("offer", new[] { new KeyValue("call-id", callId) }, null, null);
                messageNode = new ProtocolTreeNode("receipt", messageHash, new[] { offerNode }, null);
            }
            else
            {
                messageNode = new ProtocolTreeNode("receipt", messageHash, null, null);
            }
            this.SendNode(messageNode);
            /*
            $this->eventManager()->fire('onSendMessageReceived', [
                $this->phoneNumber,
                $node->getAttribute('id'),
                $node->getAttribute('from'),
                $type,
            ]);
             */
        }

        public void SendRemoveAccount(String lg = null, String lc = null, String feedback = null)
        {
            string id = TicketCounter.MakeId();

            List<ProtocolTreeNode> childNode = new List<ProtocolTreeNode>();
            if (!String.IsNullOrEmpty(feedback))
            {
                if (lg == null)
                {
                    lg = "";
                }

                if (lc == null)
                {
                    lc = "";
                }

                ProtocolTreeNode child = new ProtocolTreeNode("body", new[] { new KeyValue("lg", lg), new KeyValue("lc", lc) }, null, Encoding.UTF8.GetBytes(feedback));
                childNode.Add(child);
            }
            else
            {
                childNode = null;
            }

            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                    new KeyValue[]
                                        {
                                            new KeyValue("id", id),
                                            new KeyValue("type", "get"),
                                            new KeyValue("to", WhatsConstants.WhatsAppServer),
                                            new KeyValue("xmlns", "urn:xmpp:whatsapp:account")
                                        },
                                    new ProtocolTreeNode[]
                                        {
                                            new ProtocolTreeNode("remove", null, childNode, null)
                                        });
            this.SendNode(node);
            this.WaitForServer(id);
        }
        public void SendRemoveParticipants(string gjid, List<string> participants)
        {
            string id = TicketCounter.MakeId();
            this.SendVerbParticipants(gjid, participants, id, "remove");
        }

        /// <summary>
        /// send a retry to reget this node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="to"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="participant"></param>
        public void SendRetry(ProtocolTreeNode node, string to, string id, string t, string participant = null)
        {
            ProtocolTreeNode returnNode = null;

            #region Update Retry Counters

            if (!_RetryCounters.ContainsKey(id))
            {
                _RetryCounters.Add(id, 1);
            }
            else
            {
                if (_RetryNodes.ContainsKey(id))
                {
                    _RetryNodes[id] = node;
                }
                else
                {
                    _RetryNodes.Add(id, node);
                }
                if (_RetryCounters[id] > 2)
                    this.ResetEncryption();
            }

            #endregion Update Retry Counters

            //this._RetryCounters[id]++;
            ProtocolTreeNode retryNode = new ProtocolTreeNode("retry", new[] {
                        new KeyValue("v", "1"),
                        new KeyValue("count", "1" /*_RetryCounters[id].ToString()*/),
                        new KeyValue("id", id),
                        new KeyValue("t", t)
                       }, null, null);

            byte[] regid = AdjustID(GetLocalRegistrationId().ToString());
            ProtocolTreeNode registrationNode = new ProtocolTreeNode("registration", null, null, regid);

            if (participant != null) //IsGroups Retry
            {
                returnNode = new ProtocolTreeNode("receipt", new[] {
                                            new KeyValue("id", id),
                                            new KeyValue("to", to),
                                            new KeyValue("participant", participant),
                                            new KeyValue("type", "retry"),
                                            new KeyValue("t", t)
                                            },
                                         new ProtocolTreeNode[] { retryNode, registrationNode }, null);
            }
            else
            {
                returnNode = new ProtocolTreeNode("receipt", new[] {
                                            new KeyValue("id", id),
                                            new KeyValue("to", to),
                                            new KeyValue("type", "retry"),
                                            new KeyValue("t", t)
                                            },
                                     new ProtocolTreeNode[] { retryNode, registrationNode }, null);

                if (!this._RetryCounters.ContainsKey(id))
                {
                    this._RetryCounters[id] = 0;
                }
                this._RetryCounters[id]++;
            }

            this.SendNode(returnNode);
            this.WaitForServer(id);
        }

        /// <summary>
        /// send a retry to reget this node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="to"></param>
        /// <param name="id"></param>
        /// <param name="t"></param>
        /// <param name="participant"></param>
        public void SendRetry_Ex(ProtocolTreeNode node, string to, string id, string t, string participant = null)
        {
            ProtocolTreeNode returnNode = null;

            #region Update Retry Counters
            if (!_RetryCounters.ContainsKey(id))
            {
                _RetryCounters.Add(id, 1);
            }
            else
            {
                if (_RetryNodes.ContainsKey(id))
                {
                    _RetryNodes[id] = node;
                }
                else
                {
                    _RetryNodes.Add(id, node);
                }
            }
            #endregion

            if (_RetryCounters[id] > 2)
                this.ResetEncryption();

            //_RetryCounters[id]++;
            ProtocolTreeNode retryNode = new ProtocolTreeNode("retry", new[] {
                        new KeyValue("v", "1"),
                        new KeyValue("count", "1"),
                        new KeyValue("id", id),
                        new KeyValue("t", t)
                       }, null, null);

            byte[] regid = AdjustID(GetLocalRegistrationId().ToString());
            ProtocolTreeNode registrationNode = new ProtocolTreeNode("registration", null, null, regid);

            returnNode = new ProtocolTreeNode("receipt", new[] {
                                            new KeyValue("id", id),
                                            new KeyValue("to", to),
                                            new KeyValue("type", "retry"),
                                            new KeyValue("t", t)
                                            },
                                         new ProtocolTreeNode[] { retryNode, registrationNode }, null);
            this.SendNode(returnNode);
        }

        public void SendSetGcm(string gcm = null)
        {
            if (gcm == null)
            {
                gcm = Func.GetRandomGcm();
            }
            string v = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("config", new[] { new KeyValue("platform", "gcm"), new KeyValue("id", gcm) });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", v), new KeyValue("type", "set"), new KeyValue("xmlns", "urn:xmpp:whatsapp:push"), new KeyValue("to", WhatsConstants.WhatsAppRealm) }, new ProtocolTreeNode[] { child });
            this.SendNode(node);
        }
        public void SendSetGroupSubject(string gjid, string subject)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode child = new ProtocolTreeNode("subject", new[] { new KeyValue("value", subject) });
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", gjid) }, child);
            this.SendNode(node);
        }

        public void SendSetPhoto(string jid, byte[] bytes, byte[] thumbnailBytes = null)
        {
            string id = TicketCounter.MakeId();

            bytes = this.ProcessProfilePicture(bytes);

            List<ProtocolTreeNode> list = new List<ProtocolTreeNode> { new ProtocolTreeNode("picture", null, null, bytes) };

            if (thumbnailBytes == null)
            {
                //Auto Generate
                thumbnailBytes = this.CreateThumbnail(bytes);
            }

            //Debug
            System.IO.File.WriteAllBytes("pic.jpg", bytes);
            System.IO.File.WriteAllBytes("picthumb.jpg", thumbnailBytes);

            if (thumbnailBytes != null)
            {
                list.Add(new ProtocolTreeNode("picture", new[] { new KeyValue("type", "preview") }, null, thumbnailBytes));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:profile:picture"), new KeyValue("to", GetJID(jid)) }, list.ToArray());
            this.SendNode(node);
        }

        /// <summary>
        /// Generate the Keysets for Ourself
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
            uint randomid = (uint)Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GetRandomSequence(65536); //5000

            IdentityKeyPair identityKeyPair = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateIdentityKeyPair();
            byte[] privateKey = identityKeyPair.GetPrivateKey().Serialize();
            byte[] publicKey = identityKeyPair.GetPublicKey().Serialize();

            IList<PreKeyRecord> preKeys = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GeneratePreKeys((uint)random.Next(), 200);
            SignedPreKeyRecord signedPreKey = Tr.Com.Eimza.LibAxolotl.Util.KeyHelper.GenerateSignedPreKey(identityKeyPair, randomid);

            this.StorePreKeys(preKeys);
            this.StoreLocalData(registrationId, identityKeyPair);
            this.StoreSignedPreKey(signedPreKey.GetId(), signedPreKey);

            // For Internal Testing Only
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

            ProtocolTreeNode identity = new ProtocolTreeNode("identity", null, null, publicKey.Skip(1).ToArray());
            ProtocolTreeNode value = new ProtocolTreeNode("value", null, null, signedPreKey.GetKeyPair().GetPublicKey().Serialize().Skip(1).ToArray());

            ProtocolTreeNode signature = new ProtocolTreeNode("signature", null, null, signedPreKey.GetSignature());
            ProtocolTreeNode secretKey = new ProtocolTreeNode("skey", null, new ProtocolTreeNode[] { sid, value, signature }, null);

            String id = TicketManager.GenerateId();
            _SendCipherKeys.Add(id);

            Helper.DebugAdapter.Instance.FireOnPrintDebug(String.Format("Axolotl ID = {0}", id));

            ProtocolTreeNode _Node = new ProtocolTreeNode("iq", new KeyValue[] {
                    new KeyValue("id", id),
                    new KeyValue("to", WhatsConstants.WhatsAppServer),
                    new KeyValue("type", "set"),
                    new KeyValue("xmlns", "encrypt")
                   }, new ProtocolTreeNode[] { identity, registration, type, list, secretKey }, null);

            this.SendNode(_Node);
            this.WaitForServer(id);
            return true;
        }

        public void SendSetPrivacyBlockedList(IEnumerable<string> jidSet)
        {
            string id = TicketCounter.MakeId();
            ProtocolTreeNode[] nodeArray = Enumerable.Select<string, ProtocolTreeNode>(jidSet, (Func<string, int, ProtocolTreeNode>)((jid, index) => new ProtocolTreeNode("item", new KeyValue[] { new KeyValue("type", "jid"), new KeyValue("value", jid), new KeyValue("action", "deny"), new KeyValue("order", index.ToString(CultureInfo.InvariantCulture)) }))).ToArray<ProtocolTreeNode>();
            ProtocolTreeNode child = new ProtocolTreeNode("list", new KeyValue[] { new KeyValue("name", "default") }, (nodeArray.Length == 0) ? null : nodeArray);
            ProtocolTreeNode query = new ProtocolTreeNode("query", null, child);
            ProtocolTreeNode iq = new ProtocolTreeNode("iq", new KeyValue[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "jabber:iq:privacy") }, query);
            this.SendNode(iq);
        }

        public void SendSetPrivacySetting(ApiBase.VisibilityCategory category, ApiBase.VisibilitySetting setting)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] {
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("id", TicketCounter.MakeId()),
                new KeyValue("type", "set"),
                new KeyValue("xmlns", "privacy")
            }, new ProtocolTreeNode[] {
                new ProtocolTreeNode("privacy", null, new ProtocolTreeNode[] {
                    new ProtocolTreeNode("category", new [] {
                    new KeyValue("name", this.PrivacyCategoryToString(category)),
                    new KeyValue("value", this.PrivacySettingToString(setting))
                    })
            })
            });

            this.SendNode(node);
        }

        public void SendStatusUpdate(string status)
        {
            string id = TicketCounter.MakeId();

            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] {
                new KeyValue("to", WhatsConstants.WhatsAppServer),
                new KeyValue("type", "set"),
                new KeyValue("id", id),
                new KeyValue("xmlns", "status")
            },
            new[] {
                new ProtocolTreeNode("status", null, System.Text.Encoding.UTF8.GetBytes(status))
            });

            this.SendNode(node);
        }

        public void SendSubjectReceived(string to, string id)
        {
            ProtocolTreeNode child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            ProtocolTreeNode node = GetSubjectMessage(to, id, child);
            this.SendNode(node);
        }

        public void SendSync(string[] numbers, ApiBase.SyncMode mode = ApiBase.SyncMode.Delta, ApiBase.SyncContext context = ApiBase.SyncContext.Background, int index = 0, bool last = true)
        {
            List<ProtocolTreeNode> users = new List<ProtocolTreeNode>();
            foreach (string number in numbers)
            {
                string _number = number;
                if (!_number.StartsWith("+", StringComparison.InvariantCulture))
                    _number = string.Format("+{0}", number);
                users.Add(new ProtocolTreeNode("user", null, System.Text.Encoding.UTF8.GetBytes(_number)));
            }

            String id = TicketCounter.MakeId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[]
            {
                new KeyValue("to", GetJID(this.phoneNumber)),
                new KeyValue("type", "get"),
                new KeyValue("id", id),
                new KeyValue("xmlns", "urn:xmpp:whatsapp:sync")
            }, new ProtocolTreeNode("sync", new KeyValue[]
                {
                    new KeyValue("mode", mode.ToString().ToLowerInvariant()),
                    new KeyValue("context", context.ToString().ToLowerInvariant()),
                    new KeyValue("sid", DateTime.Now.ToFileTimeUtc().ToString()),
                    new KeyValue("index", index.ToString()),
                    new KeyValue("last", last.ToString())
                },
                users.ToArray()
                )
            );
            this.SendNode(node);
            this.WaitForServer(id);
        }
        public void SendUnavailable()
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unavailable") });
            this.SendNode(node);
        }

        public void SendUnsubscribeHim(string jid)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribed"), new KeyValue("to", jid) });
            this.SendNode(node);
        }

        public void SendUnsubscribeMe(string jid)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("type", "unsubscribe"), new KeyValue("to", jid) });
            this.SendNode(node);
        }

        /// <summary>
        /// increment the retry counters base
        /// </summary>
        /// <param name="id"></param>
        /// <param name="counter"></param>
        public void SetRetryCounter(string id, int counter)
        {
            this._RetryCounters.Add(id, counter);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string TrimNonAscii(String data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in data.Select(b => (char)b))
            {
                if ((c > '\u0020' && c < '\u007F') || c == 32)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v2plaintext"></param>
        /// <returns></returns>
        public string UnPadv2Plaintext(String v2plaintext)
        {
            String ret = SubStr(v2plaintext, 2, -1);
            return FixPadding(ret);
        }

        public void WaitForServer(String id, int timeout = 5)
        {
            long time = Func.GetNowUnixTimestamp();
            this._LastServerReceivedID = null;
            do
            {
                this.PollMessage();
            }
            while (this._LastServerReceivedID != id && Func.GetNowUnixTimestamp() - time < timeout);
        }

        protected static ProtocolTreeNode GetMessageNode(FMessage message, ProtocolTreeNode pNode, bool hidden = false)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return new ProtocolTreeNode("message", new[] {
                new KeyValue("to", message.identifier_key.remote_jid),
                new KeyValue("type", message.media_wa_type == FMessage.Type.Undefined ? "text" : "media"),
                new KeyValue("id", message.identifier_key.id),
                new KeyValue("t",unixTimestamp.ToString())
            },
            new ProtocolTreeNode[] {
                pNode
            });
        }

        protected static ProtocolTreeNode GetSubjectMessage(string to, string id, ProtocolTreeNode child)
        {
            return new ProtocolTreeNode("message", new[] { new KeyValue("to", to), new KeyValue("type", "subject"), new KeyValue("id", id) }, child);
        }

        protected ProtocolTreeNode AddAuth()
        {
            List<KeyValue> attr = new List<KeyValue>(new KeyValue[] {
                new KeyValue("mechanism", KeyStream.AuthMethod),
                new KeyValue("user", this.phoneNumber)});
            if (this.hidden)
            {
                attr.Add(new KeyValue("passive", "true"));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("auth", attr.ToArray(), null, this.CreateAuthBlob());
            return node;
        }

        protected byte[] CreateAuthBlob()
        {
            byte[] data = null;
            if (this._ChallengeData != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.EncryptPassword(), this._ChallengeData);

                this.BinReader.Key = new KeyStream(keys[2], keys[3]);
                this.outputKey = new KeyStream(keys[0], keys[1]);

                PhoneNumber pn = new PhoneNumber(this.phoneNumber);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SysEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._ChallengeData);
                b.AddRange(WhatsApp.SysEncoding.GetBytes(Func.GetNowUnixTimestamp().ToString()));
                data = b.ToArray();

                this._ChallengeData = null;

                this.outputKey.EncodeMessage(data, 0, 4, data.Length - 4);

                this.BinWriter.Key = this.outputKey;
            }

            return data;
        }

        protected ProtocolTreeNode CreateAuthResponseNode()
        {
            if (this._ChallengeData != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.EncryptPassword(), this._ChallengeData);

                this.BinReader.Key = new KeyStream(keys[2], keys[3]);
                this.BinWriter.Key = new KeyStream(keys[0], keys[1]);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SysEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._ChallengeData);

                //b.AddRange(WhatsApp.SysEncoding.GetBytes(Func.GetNowUnixTimestamp().ToString()));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes(""));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes("000"));
                //b.AddRange(Hex2Bin("00"));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes("000"));
                //b.AddRange(Hex2Bin("00"));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes(WhatsConstants.OS_Version));
                //b.AddRange(Hex2Bin("00"));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes(WhatsConstants.Manufacturer));
                //b.AddRange(Hex2Bin("00"));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes(WhatsConstants.Device));
                //b.AddRange(Hex2Bin("00"));
                //b.AddRange(WhatsApp.SysEncoding.GetBytes(WhatsConstants.Build_Version));

                byte[] data = b.ToArray();
                this.BinWriter.Key.EncodeMessage(data, 0, 4, data.Length - 4);
                ProtocolTreeNode node = new ProtocolTreeNode("response", null, null, data);

                return node;
            }

            throw new Exception("Auth Response Error");
        }

        protected ProtocolTreeNode CreateFeaturesNode()
        {
            ProtocolTreeNode readReceipts = new ProtocolTreeNode("readreceipts", null, null, null);
            ProtocolTreeNode groups_v2 = new ProtocolTreeNode("groups_v2", null, null, null);
            ProtocolTreeNode privacy = new ProtocolTreeNode("privacy", null, null, null);
            ProtocolTreeNode presencev2 = new ProtocolTreeNode("presence", null, null, null);
            return new ProtocolTreeNode("stream:features", null, new ProtocolTreeNode[] { readReceipts, groups_v2, privacy, presencev2 }, null);
        }
        protected FMessage GetFmessageAudio(string to, byte[] audioData, ApiBase.AudioType audtype)
        {
            to = GetJID(to);
            string type = string.Empty;
            string extension = string.Empty;
            switch (audtype)
            {
                case ApiBase.AudioType.WAV:
                    type = "audio/wav";
                    extension = "wav";
                    break;

                case ApiBase.AudioType.OGG:
                    type = "audio/ogg";
                    extension = "ogg";
                    break;

                default:
                    type = "audio/mpeg";
                    extension = "mp3";
                    break;
            }

            //Create Hash
            string filehash = string.Empty;
            using (HashAlgorithm sha = HashAlgorithm.Create("sha256"))
            {
                byte[] raw = sha.ComputeHash(audioData);
                filehash = Convert.ToBase64String(raw);
            }

            //Request Upload
            WaUploadResponse response = this.UploadFile(filehash, "audio", audioData.Length, audioData, to, type, extension);

            if (!String.IsNullOrEmpty(response?.url))
            {
                //Send Message
                FMessage msg = new FMessage(to, true)
                {
                    media_wa_type = FMessage.Type.Audio,
                    media_mime_type = response.mimetype,
                    media_name = response.url.Split('/').Last(),
                    media_size = response.size,
                    media_url = response.url,
                    media_duration_seconds = response.duration
                };
                return msg;
            }
            return null;
        }

        protected FMessage GetFmessageImage(string to, byte[] ImageData, ApiBase.ImageType imgtype)
        {
            to = GetJID(to);
            string type = string.Empty;
            string extension = string.Empty;
            switch (imgtype)
            {
                case ApiBase.ImageType.PNG:
                    type = "image/png";
                    extension = "png";
                    break;

                case ApiBase.ImageType.GIF:
                    type = "image/gif";
                    extension = "gif";
                    break;

                default:
                    type = "image/jpeg";
                    extension = "jpg";
                    break;
            }

            //Create Hash
            string filehash = string.Empty;
            using (HashAlgorithm sha = HashAlgorithm.Create("sha256"))
            {
                byte[] raw = sha.ComputeHash(ImageData);
                filehash = Convert.ToBase64String(raw);
            }

            //Request Upload
            WaUploadResponse response = this.UploadFile(filehash, "image", ImageData.Length, ImageData, to, type, extension);

            if (response != null && !String.IsNullOrEmpty(response.url))
            {
                //Send Message
                FMessage msg = new FMessage(to, true)
                {
                    media_wa_type = FMessage.Type.Image,
                    media_mime_type = response.mimetype,
                    media_name = response.url.Split('/').Last(),
                    media_size = response.size,
                    media_url = response.url,
                    binary_data = this.CreateThumbnail(ImageData)
                };
                return msg;
            }
            return null;
        }
        protected FMessage GetFmessageVideo(string to, byte[] videoData, ApiBase.VideoType vidtype)
        {
            to = GetJID(to);
            string type = string.Empty;
            string extension = string.Empty;
            switch (vidtype)
            {
                case ApiBase.VideoType.MOV:
                    type = "video/quicktime";
                    extension = "mov";
                    break;

                case ApiBase.VideoType.AVI:
                    type = "video/x-msvideo";
                    extension = "avi";
                    break;

                default:
                    type = "video/mp4";
                    extension = "mp4";
                    break;
            }

            //create hash
            string filehash = string.Empty;
            using (HashAlgorithm sha = HashAlgorithm.Create("sha256"))
            {
                byte[] raw = sha.ComputeHash(videoData);
                filehash = Convert.ToBase64String(raw);
            }

            //request upload
            WaUploadResponse response = this.UploadFile(filehash, "video", videoData.Length, videoData, to, type, extension);

            if (!String.IsNullOrEmpty(response?.url))
            {
                //send message
                FMessage msg = new FMessage(to, true)
                {
                    media_wa_type = FMessage.Type.Video,
                    media_mime_type = response.mimetype,
                    media_name = response.url.Split('/').Last(),
                    media_size = response.size,
                    media_url = response.url,
                    media_duration_seconds = response.duration
                };
                return msg;
            }
            return null;
        }
        protected void HandleIq(ProtocolTreeNode node)
        {
            #region Error IQ

            if (node.GetAttribute("type") == "error" && node.GetChild("error") != null)
            {
                #region SendCipherKeys IQ

                if (_SendCipherKeys.Contains(node.GetAttribute("id")) && node.GetChild("error").GetAttribute("code").Equals("406"))
                    SendSetPreKeys();
                else if (node.GetAttribute("id").Equals("2"))
                    SendSetGcm();

                #endregion SendCipherKeys IQ

                this.FireOnError(node.GetAttribute("id"), node.GetAttribute("from"), Int32.Parse(node.GetChild("error").GetAttribute("code")), node.GetChild("error").GetAttribute("text"));
            }

            #endregion Error IQ

            #region Sync IQ

            if (node.GetChild("sync") != null)
            {
                //Sync Result
                ProtocolTreeNode sync = node.GetChild("sync");
                ProtocolTreeNode existing = sync.GetChild("in");
                ProtocolTreeNode nonexisting = sync.GetChild("out");

                //Process Existing First
                Dictionary<string, string> existingUsers = new Dictionary<string, string>();
                if (existing != null)
                {
                    foreach (ProtocolTreeNode child in existing.GetAllChildren())
                    {
                        existingUsers.Add(Encoding.UTF8.GetString(child.GetData()), child.GetAttribute("jid"));
                    }
                }

                //Now Process Failed Numbers
                List<string> failedNumbers = new List<string>();
                if (nonexisting != null)
                {
                    foreach (ProtocolTreeNode child in nonexisting.GetAllChildren())
                    {
                        failedNumbers.Add(Encoding.UTF8.GetString(child.GetData()));
                    }
                }
                int index = 0;
                Int32.TryParse(sync.GetAttribute("index"), out index);
                this.FireOnGetSyncResult(index, sync.GetAttribute("sid"), existingUsers, failedNumbers.ToArray());
            }

            #endregion Sync IQ

            #region Type IQ

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase) && node.GetChild("query") != null)
            {
                //Last Seen
                DateTime lastSeen = DateTime.Now.AddSeconds(Double.Parse(node._Children.FirstOrDefault().GetAttribute("seconds")) * -1);
                this.FireOnGetLastSeen(node.GetAttribute("from"), lastSeen);
            }
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase) && (node.GetChild("media") != null || node.GetChild("duplicate") != null))
            {
                //Media Upload
                this.uploadResponse = node;
            }

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase) && node.GetChild("picture") != null)
            {
                //Profile Picture
                string from = node.GetAttribute("from");
                string id = node.GetChild("picture").GetAttribute("id");
                byte[] dat = node.GetChild("picture").GetData();
                string type = node.GetChild("picture").GetAttribute("type");
                if (type == "preview")
                {
                    this.FireOnGetPhotoPreview(from, id, dat);
                }
                else
                {
                    this.FireOnGetPhoto(from, id, dat);
                }
            }

            #endregion Type IQ

            #region Ping IQ

            if (node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase) && node.GetChild("ping") != null)
            {
                this.SendPong(node.GetAttribute("id"));
            }

            #endregion Ping IQ

            #region Group Result IQ

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase) && node.GetChild("group") != null)
            {
                //Group(s) info
                List<WaGroupInfo> groups = new List<WaGroupInfo>();
                foreach (ProtocolTreeNode group in node._Children)
                {
                    groups.Add(new WaGroupInfo(
                        group.GetAttribute("id"),
                        group.GetAttribute("owner"),
                        group.GetAttribute("creation"),
                        group.GetAttribute("subject"),
                        group.GetAttribute("s_t"),
                        group.GetAttribute("s_o")
                        ));
                }
                this.FireOnGetGroups(groups.ToArray());
            }

            #endregion Group Result IQ

            #region Participant Result IQ

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase) && node.GetChild("participant") != null)
            {
                //Group Participants
                List<string> participants = new List<string>();
                foreach (ProtocolTreeNode part in node.GetAllChildren())
                {
                    if (part._Tag == "participant" && !String.IsNullOrEmpty(part.GetAttribute("jid")))
                    {
                        participants.Add(part.GetAttribute("jid"));
                    }
                }
                this.FireOnGetGroupParticipants(node.GetAttribute("from"), participants.ToArray());
            }

            #endregion Participant Result IQ

            #region Status Result IQ

            if (node.GetAttribute("type") == "result" && node.GetChild("status") != null)
            {
                foreach (ProtocolTreeNode status in node.GetChild("status").GetAllChildren())
                {
                    this.FireOnGetStatus(status.GetAttribute("jid"), "result", null, WhatsApp.SysEncoding.GetString(status.GetData()));
                }
            }

            #endregion Status Result IQ

            #region Privacy Result IQ

            if (node.GetAttribute("type") == "result" && node.GetChild("privacy") != null)
            {
                Dictionary<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting> settings = new Dictionary<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting>();
                foreach (ProtocolTreeNode child in node.GetChild("privacy").GetAllChildren("category"))
                {
                    settings.Add(this.ParsePrivacyCategory(child.GetAttribute("name")), this.ParsePrivacySetting(child.GetAttribute("value")));
                }
                this.FireOnGetPrivacySettings(settings);
            }

            #endregion Privacy Result IQ

            #region BroadCastLists Result IQ

            if (String.IsNullOrEmpty(_GetListsId) && _GetListsId.Equals(node.GetAttribute("id")))
            {
                List<WaBroadcast> broadcasts = new List<WaBroadcast>();

                foreach (ProtocolTreeNode child in node.GetAllChildren())
                {
                    foreach (ProtocolTreeNode subChild in child.GetAllChildren())
                    {
                        String id = subChild.GetAttribute("id");
                        String name = subChild.GetAttribute("name");
                        List<string> recipients = new List<string>();

                        foreach (ProtocolTreeNode recipient in subChild.GetAllChildren())
                        {
                            recipients.Add(recipient.GetAttribute("jid"));
                        }

                        broadcasts.Add(new WaBroadcast(id, name, recipients));
                    }
                }

                if (broadcasts.Count > 0)
                {
                    FireOnGetBroadcastLists(this.phoneNumber, broadcasts);
                }
            }

            #endregion BroadCastLists Result IQ

            #region Php Result IQ

            if (node.GetChild("query") != null)
            {
                if (!String.IsNullOrEmpty(_PrivacyId) && _PrivacyId.Equals(node.GetAttribute("id")))
                {
                    ProtocolTreeNode listChild = node.GetChild(0).GetChild(0);
                    List<String> blockedJids = new List<string>();

                    foreach (ProtocolTreeNode child in listChild.GetAllChildren())
                    {
                        blockedJids.Add(child.GetAttribute("value"));
                    }

                    /*
                    $this->parent->eventManager()->fire('onGetPrivacyBlockedList',
                    [
                        $this->phoneNumber,
                        $blockedJids,
                    ]);
                    */

                    return;
                }
            }

            if (node.GetAttribute("type").Equals("get") && node.GetAttribute("xmlns").Equals("urn:xmpp:ping"))
            {
                /*
                $this->parent->eventManager()->fire('onPing', [
                    $this->phoneNumber,
                    $this->node->getAttribute('id'),
                ]);
                */
                this.SendPong(node.GetAttribute("id"));
            }

            if (node.GetChild("props") != null)
            {
                Dictionary<String, String> props = new Dictionary<string, string>();
                foreach (ProtocolTreeNode child in node.GetChild(0).GetAllChildren())
                {
                    props.Add(child.GetAttribute("name"), child.GetAttribute("value"));
                }

                /*
                $this->parent->eventManager()->fire('onGetServerProperties', [
                    $this->phoneNumber,
                    $this->node->getChild(0)->getAttribute('version'),
                    $props,
                ]);
                */
            }

            if (node.GetChild("picture") != null)
            {
                /*
                $this->parent->eventManager()->fire('onGetProfilePicture', [
                    $this->phoneNumber,
                    $this->node->getAttribute('from'),
                    $this->node->getChild('picture')->getAttribute('type'),
                    $this->node->getChild('picture')->getData(),
                ]);
                */
            }

            if (node.GetAttribute("from").Contains(WhatsConstants.WhatsGroupChat))
            {
                List<KeyValue> groupList = new List<KeyValue>();
                List<ProtocolTreeNode> groupNodes = null;

                if (node.GetChild(0) != null && node.GetChild(0).GetAllChildren() != null)
                {
                    foreach (ProtocolTreeNode child in node.GetChild(0).GetAllChildren())
                    {
                        groupList = child._AttributeHash.ToList();
                        groupNodes.Add(child);
                    }
                }

                if (!String.IsNullOrEmpty(_GroupCreateId) && _GroupCreateId.Equals(node.GetAttribute("id")))
                {
                    _GroupId = node.GetChild(0).GetAttribute("id");

                    /*
                    $this->parent->eventManager()->fire('onGroupsChatCreate', [
                        $this->phoneNumber,
                        $this->node->getChild(0)->getAttribute('id'),
                    ]);
                    */
                }

                if (!String.IsNullOrEmpty(_LeaveGroupId) && _LeaveGroupId.Equals(node.GetAttribute("id")))
                {
                    _GroupId = node.GetChild(0).GetChild(0).GetAttribute("id");

                    /*
                    $this->parent->eventManager()->fire('onGroupsChatEnd', [
                        $this->phoneNumber,
                        $this->node->getChild(0)->getChild(0)->getAttribute('id'),
                    ]);
                    */
                }

                if (!String.IsNullOrEmpty(_GetGroupsId) && _GetGroupsId.Equals(node.GetAttribute("id")))
                {
                    /*
                    $this->parent->eventManager()->fire('onGetGroups', [
                        $this->phoneNumber,
                        $groupList,
                    ]);
                    */

                    foreach (ProtocolTreeNode groupNode in groupNodes)
                    {
                        this.HandleGroupV2InfoResponse(groupNode, true);
                    }
                }

                if (!String.IsNullOrEmpty(_GetGroupv2InfoId) && _GetGroupv2InfoId.Equals(node.GetAttribute("id")))
                {
                    ProtocolTreeNode groupChild = node.GetChild(0);
                    if (groupChild != null)
                    {
                        this.HandleGroupV2InfoResponse(groupChild);
                    }
                }
            }

            if (node.GetChild("pricing") != null)
            {
                /*
                $this->parent->eventManager()->fire('onGetServicePricing', [
                    $this->phoneNumber,
                    $this->node->getChild(0)->getAttribute('price'),
                    $this->node->getChild(0)->getAttribute('cost'),
                    $this->node->getChild(0)->getAttribute('currency'),
                    $this->node->getChild(0)->getAttribute('expiration'),
                ]); 
                */
            }

            if (node.GetChild("extend") != null)
            {
                /*
                $this->parent->eventManager()->fire('onGetExtendAccount', [
                    $this->phoneNumber,
                    $this->node->getChild('account')->getAttribute('kind'),
                    $this->node->getChild('account')->getAttribute('status'),
                    $this->node->getChild('account')->getAttribute('creation'),
                    $this->node->getChild('account')->getAttribute('expiration'),
                ]);
                */
            }

            if (node.GetChild("normalize") != null)
            {
                /*
                $this->parent->eventManager()->fire('onGetNormalizedJid', [
                    $this->phoneNumber,
                    $this->node->getChild(0)->getAttribute('result'),
                ]);
                */
            }

            #endregion

            #region CipherKeys && Message IQ

            ProtocolTreeNode[] pendingNodes = ProcessIqTreeNode(node);
            if (pendingNodes != null)
                foreach (ProtocolTreeNode pnode in pendingNodes)
                    this.HandleMessage(pnode, true);

            #endregion CipherKeys && Message IQ
        }
        protected void HandleNotification(ProtocolTreeNode node)
        {
            if (!String.IsNullOrEmpty(node.GetAttribute("notify")))
            {
                this.FireOnGetContactName(node.GetAttribute("from"), node.GetAttribute("notify"));
            }
            string type = node.GetAttribute("type");
            switch (type)
            {
                case "encrypt":
                    string encrytrid = node.GetAttribute("value");
                    if (encrytrid.All(Char.IsDigit) && encrytrid.Length > 0)
                    {
                        RemoveAllPreKeys();
                        SendSetPreKeys(true);
                    }
                    break;

                case "picture":
                    ProtocolTreeNode pChild = node._Children.FirstOrDefault();
                    this.FireOnNotificationPicture(pChild._Tag,
                        pChild.GetAttribute("jid"),
                        pChild.GetAttribute("id"));
                    break;

                case "status":
                    ProtocolTreeNode sChild = node._Children.FirstOrDefault();
                    this.FireOnGetStatus(node.GetAttribute("from"),
                        sChild._Tag,
                        node.GetAttribute("notify"),
                        Encoding.UTF8.GetString(sChild.GetData()));
                    break;

                case "subject":
                    //Fire Username Notify
                    this.FireOnGetContactName(node.GetAttribute("participant"), node.GetAttribute("notify"));

                    //Fire Subject Notify
                    this.FireOnGetGroupSubject(node.GetAttribute("from"), node.GetAttribute("participant"), node.GetAttribute("notify"),
                        Encoding.UTF8.GetString(node.GetChild("body").GetData()), GetDateTimeFromTimestamp(node.GetAttribute("t")));
                    break;

                case "contacts":
                    //TODO
                    break;

                case "participant":
                    string gjid = node.GetAttribute("from");
                    string t = node.GetAttribute("t");
                    foreach (ProtocolTreeNode xChild in node.GetAllChildren())
                    {
                        if (xChild._Tag == "add")
                        {
                            this.FireOnGetParticipantAdded(gjid,
                                xChild.GetAttribute("jid"),
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (xChild._Tag == "remove")
                        {
                            this.FireOnGetParticipantRemoved(gjid,
                                xChild.GetAttribute("jid"),
                                xChild.GetAttribute("author"),
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (xChild._Tag == "modify")
                        {
                            this.FireOnGetParticipantRenamed(gjid,
                                xChild.GetAttribute("remove"),
                                xChild.GetAttribute("add"),
                                GetDateTimeFromTimestamp(t));
                        }
                    }
                    break;
            }
            this.SendAck(node);
        }

        protected void ProcessChallenge(ProtocolTreeNode node)
        {
            _ChallengeData = node.GetData();
        }

        protected IEnumerable<ProtocolTreeNode> ProcessGroupSettings(IEnumerable<GroupSetting> groups)
        {
            ProtocolTreeNode[] nodeArray = null;
            IEnumerable<GroupSetting> groupSettings = groups as GroupSetting[] ?? groups.ToArray();
            if ((groups != null) && groupSettings.Any<GroupSetting>())
            {
                DateTime now = DateTime.Now;
                nodeArray = (from @group in groupSettings
                             select new ProtocolTreeNode("item", new[]
                { new KeyValue("jid", @group.Jid),
                    new KeyValue("notify", @group.Enabled ? "1" : "0"),
                    new KeyValue("mute", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", new object[] { (!@group.MuteExpiry.HasValue || (@group.MuteExpiry.Value <= now)) ? 0 : ((int) (@group.MuteExpiry.Value - now).TotalSeconds) })) })).ToArray<ProtocolTreeNode>();
            }
            return nodeArray;
        }

        protected bool ProcessInboundData(byte[] msgdata, bool autoReceipt = true)
        {
            ProtocolTreeNode node = this.BinReader.NextTree(msgdata);

            if (node != null)
            {
                //Php
                this.pTimeout = Func.GetNowUnixTimestamp();
                this._LastServerReceivedID = node.GetAttribute("id");

                if (ProtocolTreeNode.TagEquals(node, "challenge"))
                {
                    this.ProcessChallenge(node);
                }
                else if (ProtocolTreeNode.TagEquals(node, "success"))
                {
                    this.loginStatus = ApiBase.CONNECTION_STATUS.LOGGEDIN;
                    this.accountinfo = new AccountInfo(node.GetAttribute("status"), node.GetAttribute("kind"), node.GetAttribute("creation"), node.GetAttribute("expiration"));
                    this.FireOnLoginSuccess(this.phoneNumber, node.GetData());
                }
                else if (ProtocolTreeNode.TagEquals(node, "failure"))
                {
                    this.loginStatus = ApiBase.CONNECTION_STATUS.UNAUTHORIZED;
                    this.FireOnLoginFailed(node._Children.FirstOrDefault()._Tag);
                }

                if (ProtocolTreeNode.TagEquals(node, "receipt"))
                {
                    string from = node.GetAttribute("from");
                    string id = node.GetAttribute("id");
                    string type = node.GetAttribute("type") ?? "delivery";
                    switch (type)
                    {
                        case "delivery":
                            //Delivered to Target
                            this.FireOnGetMessageReceivedClient(from, id);
                            break;

                        case "read":
                            this.FireOnGetMessageReadedClient(from, id);
                            //Read by Target
                            //TODO
                            break;

                        case "played":
                            //Played by Target
                            //TODO
                            break;
                    }

                    ProtocolTreeNode list = node.GetChild("list");
                    if (list != null)
                    {
                        foreach (ProtocolTreeNode receipt in list.GetAllChildren())
                        {
                            this.FireOnGetMessageReceivedClient(from, receipt.GetAttribute("id"));
                        }
                    }

                    //Send Ack
                    SendAck(node, type);
                }

                if (ProtocolTreeNode.TagEquals(node, "retry"))
                {
                    SendGetCipherKeysFromUser(ExtractNumber(node.GetAttribute("from")), true);
                }

                if (ProtocolTreeNode.TagEquals(node, "message"))
                {
                    this.HandleMessage(node, autoReceipt);
                }

                if (ProtocolTreeNode.TagEquals(node, "iq"))
                {
                    this.HandleIq(node);
                }

                if (ProtocolTreeNode.TagEquals(node, "stream:error"))
                {
                    ProtocolTreeNode textNode = node.GetChild("text");
                    if (textNode != null)
                    {
                        String content = WhatsApp.SysEncoding.GetString(textNode.GetData());
                        DebugAdapter.Instance.FireOnPrintDebug("Stream Error : " + content);
                    }
                    this.Disconnect();
                }

                if (ProtocolTreeNode.TagEquals(node, "presence"))
                {
                    //Presence Node
                    this.FireOnGetPresence(node.GetAttribute("from"), node.GetAttribute("type"));
                }

                if (node._Tag == "ib")
                {
                    foreach (ProtocolTreeNode child in node._Children)
                    {
                        switch (child._Tag)
                        {
                            case "dirty":
                                this.SendClearDirty(child.GetAttribute("type"));
                                break;

                            case "offline":
                                break;

                            default:
                                throw new NotImplementedException(node.NodeString());
                        }
                    }
                }

                if (node._Tag == "chatstate")
                {
                    ProtocolTreeNode protocolTreeNode = node._Children.FirstOrDefault();
                    if (protocolTreeNode != null)
                    {
                        string state = protocolTreeNode._Tag;
                        switch (state)
                        {
                            case "composing":
                                this.FireOnGetTyping(node.GetAttribute("from"));
                                break;

                            case "paused":
                                this.FireOnGetPaused(node.GetAttribute("from"));
                                break;

                            default:
                                throw new NotImplementedException(node.NodeString());
                        }
                    }
                }

                if (node._Tag == "ack")
                {
                    string cls = node.GetAttribute("class");
                    if (cls == "message")
                    {
                        //Server Receipt
                        this.FireOnGetMessageReceivedServer(node.GetAttribute("from"), node.GetAttribute("id"));
                    }
                }

                if (node._Tag == "notification")
                {
                    this.HandleNotification(node);
                }

                return true;
            }

            return false;
        }

        protected void SendChatState(string to, string type)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("chatstate", new[] { new KeyValue("to", WhatsApp.GetJID(to)) }, new[] {
                new ProtocolTreeNode(type, null)
            });
            this.SendNode(node);
        }

        protected void SendClearDirty(IEnumerable<string> categoryNames)
        {
            string id = TicketCounter.MakeId();
            List<ProtocolTreeNode> children = new List<ProtocolTreeNode>();
            foreach (string category in categoryNames)
            {
                ProtocolTreeNode cat = new ProtocolTreeNode("clean", new[] { new KeyValue("type", category) });
                children.Add(cat);
            }
            ProtocolTreeNode node = new ProtocolTreeNode("iq",
                                            new[]
                                                {
                                                    new KeyValue("id", id),
                                                    new KeyValue("type", "set"),
                                                    new KeyValue("to", WhatsConstants.WhatsAppServer),
                                                    new KeyValue("xmlns", "urn:xmpp:whatsapp:dirty")
                                                }, children);
            this.SendNode(node);
        }

        protected void SendClearDirty(string category)
        {
            this.SendClearDirty(new string[] { category });
        }

        protected void SendDeliveredReceiptAck(string to, string id)
        {
            this.SendReceiptAck(to, id, "delivered");
        }

        protected String SendMessageNode(String to, ProtocolTreeNode node, String id = null, ProtocolTreeNode plaintextNode = null)
        {
            String messageId = TicketCounter.MakeId();
            String toID = GetJID(to);
            String type = String.Empty;

            if (ProtocolTreeNode.TagEquals(node, "body") || ProtocolTreeNode.TagEquals(node, "enc"))
                type = "text";
            else
                type = "media";

            long unixTimestamp = Func.GetNowUnixTimestamp();
            ProtocolTreeNode messageNode = new ProtocolTreeNode("message", new[] {
                new KeyValue("to",toID),
                new KeyValue("type",type),
                new KeyValue("id",messageId),
                new KeyValue("t",unixTimestamp.ToString()),
                new KeyValue("notify",this.name)
            },
            new ProtocolTreeNode[] {
                node
            });

            this.SendNode(messageNode);
            return messageId;
        }

        protected void SendMessageReceived(ProtocolTreeNode msg, string type = "read")
        {
            FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.SendMessageReceived(tmpMessage, type);
        }

        protected void SendMessageReceived(FMessage message, string type = "read")
        {
            KeyValue toAttrib = new KeyValue("to", message.identifier_key.remote_jid);
            KeyValue idAttrib = new KeyValue("id", message.identifier_key.id);

            List<KeyValue> attribs = new List<KeyValue>();
            attribs.Add(toAttrib);
            attribs.Add(idAttrib);
            if (type.Equals("read"))
            {
                KeyValue typeAttrib = new KeyValue("type", type);
                attribs.Add(typeAttrib);
            }

            ProtocolTreeNode node = new ProtocolTreeNode("receipt", attribs.ToArray());

            this.SendNode(node);
        }
        protected void SendMessageWithBody(FMessage message, bool hidden = false)
        {
            ProtocolTreeNode child = new ProtocolTreeNode("body", null, null, WhatsApp.SysEncoding.GetBytes(message.data));
            this.SendNode(GetMessageNode(message, child, hidden));
        }

        protected void SendMessageWithMedia(FMessage message)
        {
            ProtocolTreeNode node;
            if (FMessage.Type.System == message.media_wa_type)
            {
                throw new SystemException("Cannot send system message over the network");
            }

            List<KeyValue> list = new List<KeyValue>(new KeyValue[] { new KeyValue("xmlns", "urn:xmpp:whatsapp:mms"), new KeyValue("type", FMessage.GetMessage_WA_Type_StrValue(message.media_wa_type)) });
            if (FMessage.Type.Location == message.media_wa_type)
            {
                list.AddRange(new KeyValue[] { new KeyValue("latitude", message.latitude.ToString(CultureInfo.InvariantCulture)), new KeyValue("longitude", message.longitude.ToString(CultureInfo.InvariantCulture)) });
                if (message.location_details != null)
                {
                    list.Add(new KeyValue("name", message.location_details));
                }
                if (message.location_url != null)
                {
                    list.Add(new KeyValue("url", message.location_url));
                }
            }
            else if (((FMessage.Type.Contact != message.media_wa_type) && (message.media_name != null)) && ((message.media_url != null) && (message.media_size > 0L)))
            {
                list.AddRange(new KeyValue[] { new KeyValue("file", message.media_name), new KeyValue("size", message.media_size.ToString(CultureInfo.InvariantCulture)), new KeyValue("url", message.media_url) });
                if (message.media_duration_seconds > 0)
                {
                    list.Add(new KeyValue("seconds", message.media_duration_seconds.ToString(CultureInfo.InvariantCulture)));
                }
            }
            if ((FMessage.Type.Contact == message.media_wa_type) && (message.media_name != null))
            {
                node = new ProtocolTreeNode("media", list.ToArray(), new ProtocolTreeNode("vcard", new KeyValue[] { new KeyValue("name", message.media_name) }, WhatsApp.SysEncoding.GetBytes(message.data)));
            }
            else
            {
                byte[] data = message.binary_data;
                if ((data == null) && !string.IsNullOrEmpty(message.data))
                {
                    try
                    {
                        data = Convert.FromBase64String(message.data);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (data != null)
                {
                    list.Add(new KeyValue("encoding", "raw"));
                }
                node = new ProtocolTreeNode("media", list.ToArray(), null, data);
            }
            this.SendNode(GetMessageNode(message, node));
        }

        /**
         * Send the Next message.
         */
        protected void SendNextMessage()
        {
            if (base.outMessageQueue.Count > 0)
            {
                ProtocolTreeNode msgnode = this.outMessageQueue.FirstOrDefault();
                msgnode.RefreshTimes();
                this.LastId = msgnode.GetAttribute("id");
                this.SendNode(msgnode);
            }
            else
            {
                this.LastId = String.Empty;
            }
        }

        protected void SendNotificationReceived(string jid, string id)
        {
            ProtocolTreeNode child = new ProtocolTreeNode("received", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            ProtocolTreeNode node = new ProtocolTreeNode("message", new[] { new KeyValue("to", jid), new KeyValue("type", "notification"), new KeyValue("id", id) }, child);
            this.SendNode(node);
        }

        protected void SendPong(string id)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("type", "result"), new KeyValue("to", WhatsConstants.WhatsAppRealm), new KeyValue("id", id) });
            this.SendNode(node);
        }

        protected void SendQrSync(byte[] qrkey, byte[] token = null)
        {
            string id = TicketCounter.MakeId();
            List<ProtocolTreeNode> children = new List<ProtocolTreeNode>();
            children.Add(new ProtocolTreeNode("sync", null, qrkey));
            if (token != null)
            {
                children.Add(new ProtocolTreeNode("code", null, token));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] {
                new KeyValue("type", "set"),
                new KeyValue("id", id),
                new KeyValue("xmlns", "w:web")
            }, children.ToArray());
            this.SendNode(node);
        }

        protected void SendVerbParticipants(string gjid, IEnumerable<string> participants, string id, string inner_tag)
        {
            IEnumerable<ProtocolTreeNode> source = from jid in participants select new ProtocolTreeNode("participant", new[] { new KeyValue("jid", GetJID(jid)) });
            ProtocolTreeNode child = new ProtocolTreeNode(inner_tag, null, source);
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new[] { new KeyValue("id", id), new KeyValue("type", "set"), new KeyValue("xmlns", "w:g2"), new KeyValue("to", GetJID(gjid)) }, child);
            this.SendNode(node);
        }
        protected WaUploadResponse UploadFile(string b64hash, string type, long size, byte[] fileData, string to, string contenttype, string extension)
        {
            ProtocolTreeNode media = new ProtocolTreeNode("media", new KeyValue[] {
                new KeyValue("hash", b64hash),
                new KeyValue("type", type),
                new KeyValue("size", size.ToString())
            });
            string id = TicketManager.GenerateId();
            ProtocolTreeNode node = new ProtocolTreeNode("iq", new KeyValue[] {
                new KeyValue("id", id),
                new KeyValue("to", to),
                new KeyValue("type", "set"),
                new KeyValue("xmlns", "w:m")
            }, media);
            this.uploadResponse = null;
            this.SendNode(node);

            int i = 0;
            while (this.uploadResponse == null && i <= 100)
            {
                if (m_usePoolMessages)
                    System.Threading.Thread.Sleep(500);
                else
                    this.PollMessage();
                i++;
            }
            if (this.uploadResponse != null && this.uploadResponse.GetChild("duplicate") != null)
            {
                WaUploadResponse res = new WaUploadResponse(this.uploadResponse);
                this.uploadResponse = null;
                return res;
            }
            else
            {
                try
                {
                    string uploadUrl = this.uploadResponse.GetChild("media").GetAttribute("url");
                    this.uploadResponse = null;

                    Uri uri = new Uri(uploadUrl);

                    string hashname = string.Empty;
                    byte[] buff = MD5.Create().ComputeHash(System.Text.Encoding.Default.GetBytes(b64hash));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in buff)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    hashname = String.Format("{0}.{1}", sb.ToString(), extension);

                    string boundary = "zzXXzzYYzzXXzzQQ";

                    sb = new StringBuilder();

                    sb.AppendFormat("--{0}\r\n", boundary);
                    sb.Append("Content-Disposition: form-data; name=\"to\"\r\n\r\n");
                    sb.AppendFormat("{0}\r\n", to);
                    sb.AppendFormat("--{0}\r\n", boundary);
                    sb.Append("Content-Disposition: form-data; name=\"from\"\r\n\r\n");
                    sb.AppendFormat("{0}\r\n", this.phoneNumber);
                    sb.AppendFormat("--{0}\r\n", boundary);
                    sb.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", hashname);
                    sb.AppendFormat("Content-Type: {0}\r\n\r\n", contenttype);
                    string header = sb.ToString();

                    sb = new StringBuilder();
                    sb.AppendFormat("\r\n--{0}--\r\n", boundary);
                    string footer = sb.ToString();

                    long clength = size + header.Length + footer.Length;

                    sb = new StringBuilder();
                    sb.AppendFormat("POST {0}\r\n", uploadUrl);
                    sb.AppendFormat("Content-Type: multipart/form-data; boundary={0}\r\n", boundary);
                    sb.AppendFormat("Host: {0}\r\n", uri.Host);
                    sb.AppendFormat("User-Agent: {0}\r\n", WhatsConstants.UserAgent);
                    sb.AppendFormat("Content-Length: {0}\r\n\r\n", clength);
                    string post = sb.ToString();

                    TcpClient tc = new TcpClient(uri.Host, 443);
                    SslStream ssl = new SslStream(tc.GetStream());
                    try
                    {
                        ssl.AuthenticateAsClient(uri.Host);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    List<byte> buf = new List<byte>();
                    buf.AddRange(Encoding.UTF8.GetBytes(post));
                    buf.AddRange(Encoding.UTF8.GetBytes(header));
                    buf.AddRange(fileData);
                    buf.AddRange(Encoding.UTF8.GetBytes(footer));

                    ssl.Write(buf.ToArray(), 0, buf.ToArray().Length);

                    //Moment of Truth...
                    buff = new byte[1024];
                    ssl.Read(buff, 0, 1024);

                    string result = Encoding.UTF8.GetString(buff);
                    foreach (string line in result.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.StartsWith("{"))
                        {
                            string fooo = line.TrimEnd(new char[] { (char)0 });
                            JavaScriptSerializer jss = new JavaScriptSerializer();
                            WaUploadResponse resp = jss.Deserialize<WaUploadResponse>(fooo);
                            if (!String.IsNullOrEmpty(resp.url))
                            {
                                return resp;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
        private static string Bin2Hex(byte[] bin)
        {
            return BitConverter.ToString(bin).Replace("-", string.Empty).ToLower();

            #region Old Code

            /*
            StringBuilder sb = new StringBuilder(bin.Length *2);
            foreach (byte b in bin)
            {
                sb.Append(b.ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
            */

            #endregion Old Code
        }

        private void SendAck(ProtocolTreeNode node, string type = null)
        {
            string from = node.GetAttribute("from");
            string to = node.GetAttribute("to");
            string participant = node.GetAttribute("participant");
            string id = node.GetAttribute("id");

            List<KeyValue> attributes = new List<KeyValue>();
            if (!String.IsNullOrEmpty(to))
            {
                attributes.Add(new KeyValue("from", to));
            }
            if (!String.IsNullOrEmpty(participant))
            {
                attributes.Add(new KeyValue("participant", participant));
            }

            if (!String.IsNullOrEmpty(type))
            {
                attributes.Add(new KeyValue("type", type));
            }
            attributes.AddRange(new[] {
                new KeyValue("to", from),
                new KeyValue("class", node._Tag),
                new KeyValue("id", id)
             });

            ProtocolTreeNode sendNode = new ProtocolTreeNode("ack", attributes.ToArray());
            this.SendNode(sendNode);
        }
        private void SendReceiptAck(string to, string id, string receiptType)
        {
            ProtocolTreeNode tmpChild = new ProtocolTreeNode("ack", new[] { new KeyValue("xmlns", "urn:xmpp:receipts") });
            ProtocolTreeNode resultNode = new ProtocolTreeNode("message", new[]
                                                             {
                                                                 new KeyValue("to", to),
                                                                 new KeyValue("type", "chat"),
                                                                 new KeyValue("id", id)
                                                             }, tmpChild);
            this.SendNode(resultNode);
        }
    }
}
