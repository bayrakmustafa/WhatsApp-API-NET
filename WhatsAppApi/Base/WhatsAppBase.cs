using System;
using System.Collections.Generic;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Settings;

namespace WhatsAppApi
{
    public class WhatsAppBase : WhatsEventBase
    {
        public static readonly Encoding SysEncoding = Encoding.UTF8;

        public static bool Debug;
        public static bool DebugOutBound;
        public long m_LastSentInfo = 0;

        protected static object messageLock = new object();

        protected byte[] _ChallengeData;
        protected AccountInfo accountinfo;

        protected BinTreeNodeWriter BinWriter;
        protected BinTreeNodeReader BinReader;

        protected bool hidden;
        protected CONNECTION_STATUS loginStatus;
        protected List<ProtocolTreeNode> messageQueue;
        protected List<ProtocolTreeNode> outMessageQueue;
        protected string name;
        protected KeyStream outputKey;
        protected string password;
        protected string phoneNumber;
        protected int timeout = 300000;
        protected ProtocolTreeNode uploadResponse;
        protected WhatsNetwork whatsNetwork;

        public CONNECTION_STATUS ConnectionStatus
        {
            get
            {
                return this.loginStatus;
            }
        }

        public WhatsAppBase(string phoneNum, string password, string nick, bool debug, bool hidden)
        {
            this.messageQueue = new List<ProtocolTreeNode>();
            this.phoneNumber = phoneNum;
            this.password = password;
            this.name = nick;
            this.hidden = hidden;
            WhatsApp.Debug = debug;
            this.BinReader = new BinTreeNodeReader();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.BinWriter = new BinTreeNodeWriter();
            this.whatsNetwork = new WhatsNetwork(WhatsConstants.WhatsAppHost, WhatsConstants.WhatsPort, this.timeout);
        }

        public void Connect()
        {
            try
            {
                this.whatsNetwork.Connect();
                this.loginStatus = CONNECTION_STATUS.CONNECTED;

                //Success
                this.FireOnConnectSuccess();
            }
            catch (Exception e)
            {
                this.FireOnConnectFailed(e);
            }
        }

        public void Disconnect(Exception ex = null)
        {
            this.whatsNetwork.Disconenct();
            this.loginStatus = CONNECTION_STATUS.DISCONNECTED;
            this.FireOnDisconnect(ex);
        }

        public AccountInfo GetAccountInfo()
        {
            return this.accountinfo;
        }

        public ProtocolTreeNode[] GetAllMessages()
        {
            ProtocolTreeNode[] tmpReturn = null;
            lock (messageLock)
            {
                tmpReturn = this.messageQueue.ToArray();
                this.messageQueue.Clear();
            }
            return tmpReturn;
        }

        public bool HasMessages()
        {
            if (this.messageQueue == null)
                return false;
            return this.messageQueue.Count > 0;
        }

        public void SendNode(ProtocolTreeNode node)
        {
            m_LastSentInfo = DateTime.UtcNow.ToFileTime();
            this.SendData(this.BinWriter.Write(node));
        }

        protected void AddOutMessage(ProtocolTreeNode node)
        {
            lock (messageLock)
            {
                this.outMessageQueue.Add(node);
            }
        }

        protected void PushMessageToQueue(ProtocolTreeNode node)
        {
            lock (messageLock)
            {
                this.messageQueue.Add(node);
            }
        }

        protected byte[] EncryptPassword()
        {
            return Convert.FromBase64String(this.password);
        }

        protected void SendData(byte[] data)
        {
            try
            {
                this.whatsNetwork.SendData(data);
            }
            catch (ConnectionException)
            {
                this.Disconnect();
            }
        }
    }
}