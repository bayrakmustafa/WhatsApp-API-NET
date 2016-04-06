using System;
using System.Collections.Generic;
using WhatsAppApi.Account;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;

namespace WhatsAppPort
{
    internal class WhatsMessageHandler
    {
        private Dictionary<string, List<FMessage>> messageHistory;
        private Dictionary<string, WhatsEventHandler.MessageRecievedHandler> userMessageDict;

        private static WhatsMessageHandler _WhatsMessageHandler = null;

        public static WhatsMessageHandler Instance
        {
            get
            {
                if (_WhatsMessageHandler == null)
                {
                    _WhatsMessageHandler = new WhatsMessageHandler();
                }
                return _WhatsMessageHandler;
            }
        }

        private WhatsMessageHandler()
        {
            this.messageHistory = new Dictionary<string, List<FMessage>>();
            this.userMessageDict = new Dictionary<string, WhatsEventHandler.MessageRecievedHandler>();

            WhatsEventHandler.MessageRecievedEvent += WhatsEventHandlerOnMessageRecievedEvent;
        }

        public void WhatsEventHandlerOnMessageRecievedEvent(FMessage mess)
        {
            if (mess == null || mess.identifier_key.remote_jid == null || mess.identifier_key.remote_jid.Length == 0)
                return;

            if (!this.messageHistory.ContainsKey(mess.identifier_key.remote_jid))
                this.messageHistory.Add(mess.identifier_key.remote_jid, new List<FMessage>());

            this.messageHistory[mess.identifier_key.remote_jid].Add(mess);
            this.CheckIfUserRegisteredAndCreate(mess);
        }

        public void CheckIfUserRegisteredAndCreate(FMessage mess)
        {
            if (this.messageHistory.ContainsKey(mess.identifier_key.remote_jid))
                return;

            String[] jidSplit = mess.identifier_key.remote_jid.Split('@');
            WhatsUser tmpWhatsUser = new WhatsUser(jidSplit[0], jidSplit[1], mess.identifier_key.serverNickname);
            User tmpUser = new User(jidSplit[0], jidSplit[1]);
            tmpUser.SetUser(tmpWhatsUser);

            this.messageHistory.Add(mess.identifier_key.remote_jid, new List<FMessage>());
            this.messageHistory[mess.identifier_key.remote_jid].Add(mess);
        }

        public void RegisterUser(User user, WhatsEventHandler.MessageRecievedHandler messHandler)
        {
        }
    }
}