using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading;
using Tr.Com.Eimza.LibAxolotl.Util;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    public abstract class WhatsSendBase : AxolotlManager // WhatsAppBase
    {
        protected bool m_usePoolMessages = false;

        public void Login(byte[] nextChallenge = null)
        {
            //reset stuff
            this.reader.Key = null;
            this.BinWriter.Key = null;
            this._ChallengeBytes = null;

            if (nextChallenge != null)
            {
                this._ChallengeBytes = nextChallenge;
            }

            string resource = string.Format(@"{0}-{1}-{2}", WhatsConstants.Device, WhatsConstants.WhatsAppVer, WhatsConstants.WhatsPort);
            byte[] data = this.BinWriter.StartStream(WhatsConstants.WhatsAppServer, resource);
            ProtocolTreeNode feat = this.AddFeatures();
            ProtocolTreeNode auth = this.AddAuth();
            this.SendData(data);
            this.SendData(this.BinWriter.Write(feat, false));
            this.SendData(this.BinWriter.Write(auth, false));

            this.PollMessage();//Stream Start
            this.PollMessage();//Features
            this.PollMessage();//Challenge or Success

            if (this.loginStatus != CONNECTION_STATUS.LOGGEDIN)
            {
                //OneShot Failed
                ProtocolTreeNode authResp = this.AddAuthResponse();
                this.SendData(this.BinWriter.Write(authResp, false));
                this.PollMessage();
            }

            Helper.DebugAdapter.Instance.FireOnPrintDebug(String.Format("{0} Successfully Logged In", this.phoneNumber));

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TicketCounter.SetLoginTime(unixTimestamp.ToString());
            this.SendAvailableForChat(this.name, this.hidden);
        }

        public void PollMessages(bool autoReceipt = true)
        {
            m_usePoolMessages = true;
            while (PollMessage(autoReceipt))
            {
                Thread.Sleep(100);
            }
            m_usePoolMessages = false;
        }

        public bool PollMessage(bool autoReceipt = true)
        {
            if (this.loginStatus == CONNECTION_STATUS.CONNECTED || this.loginStatus == CONNECTION_STATUS.LOGGEDIN)
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

        protected ProtocolTreeNode AddFeatures()
        {
            ProtocolTreeNode readReceipts = new ProtocolTreeNode("readreceipts", null, null, null);
            ProtocolTreeNode groups_v2 = new ProtocolTreeNode("groups_v2", null, null, null);
            ProtocolTreeNode privacy = new ProtocolTreeNode("privacy", null, null, null);
            ProtocolTreeNode presencev2 = new ProtocolTreeNode("presence", null, null, null);
            return new ProtocolTreeNode("stream:features", null, new ProtocolTreeNode[] { readReceipts, groups_v2, privacy, presencev2 }, null);
        }

        protected ProtocolTreeNode AddAuth()
        {
            List<KeyValue> attr = new List<KeyValue>(new KeyValue[] {
                new KeyValue("mechanism", Helper.KeyStream.AuthMethod),
                new KeyValue("user", this.phoneNumber)});
            if (this.hidden)
            {
                attr.Add(new KeyValue("passive", "true"));
            }
            ProtocolTreeNode node = new ProtocolTreeNode("auth", attr.ToArray(), null, this.GetAuthBlob());
            return node;
        }

        protected byte[] GetAuthBlob()
        {
            byte[] data = null;
            if (this._ChallengeBytes != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.EncryptPassword(), this._ChallengeBytes);

                this.reader.Key = new KeyStream(keys[2], keys[3]);
                this.outputKey = new KeyStream(keys[0], keys[1]);

                PhoneNumber pn = new PhoneNumber(this.phoneNumber);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SysEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._ChallengeBytes);
                b.AddRange(WhatsApp.SysEncoding.GetBytes(Helper.Func.GetNowUnixTimestamp().ToString()));
                data = b.ToArray();

                this._ChallengeBytes = null;

                this.outputKey.EncodeMessage(data, 0, 4, data.Length - 4);

                this.BinWriter.Key = this.outputKey;
            }

            return data;
        }

        protected ProtocolTreeNode AddAuthResponse()
        {
            if (this._ChallengeBytes != null)
            {
                byte[][] keys = KeyStream.GenerateKeys(this.EncryptPassword(), this._ChallengeBytes);

                this.reader.Key = new KeyStream(keys[2], keys[3]);
                this.BinWriter.Key = new KeyStream(keys[0], keys[1]);

                List<byte> b = new List<byte>();
                b.AddRange(new byte[] { 0, 0, 0, 0 });
                b.AddRange(WhatsApp.SysEncoding.GetBytes(this.phoneNumber));
                b.AddRange(this._ChallengeBytes);

                byte[] data = b.ToArray();
                this.BinWriter.Key.EncodeMessage(data, 0, 4, data.Length - 4);
                ProtocolTreeNode node = new ProtocolTreeNode("response", null, null, data);

                return node;
            }
            throw new Exception("Auth Response Error");
        }

        protected void ProcessChallenge(ProtocolTreeNode node)
        {
            _ChallengeBytes = node.data;
        }

        protected bool ProcessInboundData(byte[] msgdata, bool autoReceipt = true)
        {
            ProtocolTreeNode node = this.reader.NextTree(msgdata);

            if (node != null)
            {
                if (ProtocolTreeNode.TagEquals(node, "challenge"))
                {
                    this.ProcessChallenge(node);
                }
                else if (ProtocolTreeNode.TagEquals(node, "success"))
                {
                    this.loginStatus = CONNECTION_STATUS.LOGGEDIN;
                    this.accountinfo = new AccountInfo(node.GetAttribute("status"),
                                                        node.GetAttribute("kind"),
                                                        node.GetAttribute("creation"),
                                                        node.GetAttribute("expiration"));
                    this.FireOnLoginSuccess(this.phoneNumber, node.GetData());
                }
                else if (ProtocolTreeNode.TagEquals(node, "failure"))
                {
                    this.loginStatus = CONNECTION_STATUS.UNAUTHORIZED;
                    this.FireOnLoginFailed(node.children.FirstOrDefault().tag);
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
                            //todo
                            break;

                        case "played":
                            //Played by Target
                            //todo
                            break;
                    }

                    ProtocolTreeNode list = node.GetChild("list");
                    if (list != null)
                        foreach (ProtocolTreeNode receipt in list.GetAllChildren())
                        {
                            this.FireOnGetMessageReceivedClient(from, receipt.GetAttribute("id"));
                        }

                    //Send Ack
                    SendNotificationAck(node, type);
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
                        string content = WhatsApp.SysEncoding.GetString(textNode.GetData());
                        Helper.DebugAdapter.Instance.FireOnPrintDebug("Error : " + content);
                    }
                    this.Disconnect();
                }

                if (ProtocolTreeNode.TagEquals(node, "presence"))
                {
                    //Presence Node
                    this.FireOnGetPresence(node.GetAttribute("from"), node.GetAttribute("type"));
                }

                if (node.tag == "ib")
                {
                    foreach (ProtocolTreeNode child in node.children)
                    {
                        switch (child.tag)
                        {
                            case "dirty":
                                this.SendClearDirty(child.GetAttribute("type"));
                                break;

                            case "offline":
                                //this.SendQrSync(null);
                                break;

                            default:
                                throw new NotImplementedException(node.NodeString());
                        }
                    }
                }

                if (node.tag == "chatstate")
                {
                    ProtocolTreeNode protocolTreeNode = node.children.FirstOrDefault();
                    if (protocolTreeNode != null)
                    {
                        string state = protocolTreeNode.tag;
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

                if (node.tag == "ack")
                {
                    string cls = node.GetAttribute("class");
                    if (cls == "message")
                    {
                        //Server Receipt
                        this.FireOnGetMessageReceivedServer(node.GetAttribute("from"), node.GetAttribute("id"));
                    }
                }

                if (node.tag == "notification")
                {
                    this.HandleNotification(node);
                }

                return true;
            }

            return false;
        }

        public void HandleMessage(ProtocolTreeNode node, bool autoReceipt)
        {
            if (!string.IsNullOrEmpty(node.GetAttribute("notify")))
            {
                string notifyName = node.GetAttribute("notify");
                this.FireOnGetContactName(node.GetAttribute("from"), notifyName);
            }
            if (node.GetAttribute("type") == "error")
            {
                throw new NotImplementedException(node.NodeString());
            }

            if (node.GetChild("body") != null || node.GetChild("enc") != null)
            {
                // Text Message
                // Encrypted Messages Have No Body Bode. Instead, the Encrypted Cipher Text is Provided within the Enc Node
                Pair<Boolean, ProtocolTreeNode> ret = null;
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
                        this.AddMessage(node);
                        if (autoReceipt)
                        {
                            this.SendMessageReceived(node);
                        }
                    }
                    else if (ret == null)
                    {
                        this.FireOnGetMessage(node, node.GetAttribute("from"), node.GetAttribute("id"), node.GetAttribute("notify"), WhatsApp.SysEncoding.GetString(contentNode.GetData()), autoReceipt);
                        this.AddMessage(node);
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
                        double lon = double.Parse(media.GetAttribute("longitude"), System.Globalization.CultureInfo.InvariantCulture);
                        double lat = double.Parse(media.GetAttribute("latitude"), System.Globalization.CultureInfo.InvariantCulture);
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

        protected void HandleIq(ProtocolTreeNode node)
        {
            #region Error IQ

            if (node.GetAttribute("type") == "error")
            {
                this.FireOnError(node.GetAttribute("id"), node.GetAttribute("from"), Int32.Parse(node.GetChild("error").GetAttribute("code")), node.GetChild("error").GetAttribute("text"));
            }

            #endregion Error IQ

            #region Sync IQ

            if (node.GetChild("sync") != null)
            {
                //sync result
                ProtocolTreeNode sync = node.GetChild("sync");
                ProtocolTreeNode existing = sync.GetChild("in");
                ProtocolTreeNode nonexisting = sync.GetChild("out");
                //process existing first
                Dictionary<string, string> existingUsers = new Dictionary<string, string>();
                if (existing != null)
                {
                    foreach (ProtocolTreeNode child in existing.GetAllChildren())
                    {
                        existingUsers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()), child.GetAttribute("jid"));
                    }
                }
                //now process failed numbers
                List<string> failedNumbers = new List<string>();
                if (nonexisting != null)
                {
                    foreach (ProtocolTreeNode child in nonexisting.GetAllChildren())
                    {
                        failedNumbers.Add(System.Text.Encoding.UTF8.GetString(child.GetData()));
                    }
                }
                int index = 0;
                Int32.TryParse(sync.GetAttribute("index"), out index);
                this.FireOnGetSyncResult(index, sync.GetAttribute("sid"), existingUsers, failedNumbers.ToArray());
            }

            #endregion Sync IQ

            #region Type IQ

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("query") != null)
            {
                //last seen
                DateTime lastSeen = DateTime.Now.AddSeconds(double.Parse(node.children.FirstOrDefault().GetAttribute("seconds")) * -1);
                this.FireOnGetLastSeen(node.GetAttribute("from"), lastSeen);
            }
            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && (node.GetChild("media") != null || node.GetChild("duplicate") != null))
            {
                //media upload
                this.uploadResponse = node;
            }

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("picture") != null)
            {
                //profile picture
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

            if (node.GetAttribute("type").Equals("get", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("ping") != null)
            {
                this.SendPong(node.GetAttribute("id"));
            }

            #endregion Ping IQ

            #region Group Result IQ

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("group") != null)
            {
                //Group(s) info
                List<WaGroupInfo> groups = new List<WaGroupInfo>();
                foreach (ProtocolTreeNode group in node.children)
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

            if (node.GetAttribute("type").Equals("result", StringComparison.OrdinalIgnoreCase)
                && node.GetChild("participant") != null)
            {
                //Group Participants
                List<string> participants = new List<string>();
                foreach (ProtocolTreeNode part in node.GetAllChildren())
                {
                    if (part.tag == "participant" && !string.IsNullOrEmpty(part.GetAttribute("jid")))
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
                    this.FireOnGetStatus(status.GetAttribute("jid"),
                        "result",
                        null,
                        WhatsApp.SysEncoding.GetString(status.GetData()));
                }
            }

            #endregion Status Result IQ

            #region Privacy Result IQ

            if (node.GetAttribute("type") == "result" && node.GetChild("privacy") != null)
            {
                Dictionary<VisibilityCategory, VisibilitySetting> settings = new Dictionary<VisibilityCategory, VisibilitySetting>();
                foreach (ProtocolTreeNode child in node.GetChild("privacy").GetAllChildren("category"))
                {
                    settings.Add(this.ParsePrivacyCategory(
                        child.GetAttribute("name")),
                        this.ParsePrivacySetting(child.GetAttribute("value"))
                    );
                }
                this.FireOnGetPrivacySettings(settings);
            }

            #endregion Privacy Result IQ

            #region CipherKeys && Message IQ

            ProtocolTreeNode[] pnodes = ProcessIqTreeNode(node);
            if (pnodes != null)
                foreach (ProtocolTreeNode pnode in pnodes)
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
                    if (encrytrid.All(char.IsDigit) && encrytrid.Length > 0)
                    {
                        RemoveAllPreKeys();
                        SendSetPreKeys(true);
                    }

                    break;

                case "picture":
                    ProtocolTreeNode pChild = node.children.FirstOrDefault();
                    this.FireOnNotificationPicture(pChild.tag,
                        pChild.GetAttribute("jid"),
                        pChild.GetAttribute("id"));
                    break;

                case "status":
                    ProtocolTreeNode sChild = node.children.FirstOrDefault();
                    this.FireOnGetStatus(node.GetAttribute("from"),
                        sChild.tag,
                        node.GetAttribute("notify"),
                        System.Text.Encoding.UTF8.GetString(sChild.GetData()));
                    break;

                case "subject":
                    //Fire Username Notify
                    this.FireOnGetContactName(node.GetAttribute("participant"),
                        node.GetAttribute("notify"));
                    //Fire Subject Notify
                    this.FireOnGetGroupSubject(node.GetAttribute("from"),
                        node.GetAttribute("participant"),
                        node.GetAttribute("notify"),
                        System.Text.Encoding.UTF8.GetString(node.GetChild("body").GetData()),
                        GetDateTimeFromTimestamp(node.GetAttribute("t")));
                    break;

                case "contacts":
                    //TODO
                    break;

                case "participant":
                    string gjid = node.GetAttribute("from");
                    string t = node.GetAttribute("t");
                    foreach (ProtocolTreeNode xChild in node.GetAllChildren())
                    {
                        if (xChild.tag == "add")
                        {
                            this.FireOnGetParticipantAdded(gjid,
                                xChild.GetAttribute("jid"),
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (xChild.tag == "remove")
                        {
                            this.FireOnGetParticipantRemoved(gjid,
                                xChild.GetAttribute("jid"),
                                xChild.GetAttribute("author"),
                                GetDateTimeFromTimestamp(t));
                        }
                        else if (xChild.tag == "modify")
                        {
                            this.FireOnGetParticipantRenamed(gjid,
                                xChild.GetAttribute("remove"),
                                xChild.GetAttribute("add"),
                                GetDateTimeFromTimestamp(t));
                        }
                    }
                    break;
            }
            this.SendNotificationAck(node);
        }

        private void SendNotificationAck(ProtocolTreeNode node, string type = null)
        {
            string from = node.GetAttribute("from");
            string to = node.GetAttribute("to");
            string participant = node.GetAttribute("participant");
            string id = node.GetAttribute("id");

            List<KeyValue> attributes = new List<KeyValue>();
            if (!string.IsNullOrEmpty(to))
            {
                attributes.Add(new KeyValue("from", to));
            }
            if (!string.IsNullOrEmpty(participant))
            {
                attributes.Add(new KeyValue("participant", participant));
            }

            if (!string.IsNullOrEmpty(type))
            {
                attributes.Add(new KeyValue("type", type));
            }
            attributes.AddRange(new[] {
                new KeyValue("to", from),
                new KeyValue("class", node.tag),
                new KeyValue("id", id)
             });

            ProtocolTreeNode sendNode = new ProtocolTreeNode("ack", attributes.ToArray());
            this.SendNode(sendNode);
        }

        protected void SendMessageReceived(ProtocolTreeNode msg, string type = "read")
        {
            FMessage tmpMessage = new FMessage(new FMessage.FMessageIdentifierKey(msg.GetAttribute("from"), true, msg.GetAttribute("id")));
            this.SendMessageReceived(tmpMessage, type);
        }

        public void SendAvailableForChat(string nickName = null, bool isHidden = false)
        {
            ProtocolTreeNode node = new ProtocolTreeNode("presence", new[] { new KeyValue("name", (!String.IsNullOrEmpty(nickName) ? nickName : this.name)) });
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
                                                    new KeyValue("to", "s.whatsapp.net"),
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