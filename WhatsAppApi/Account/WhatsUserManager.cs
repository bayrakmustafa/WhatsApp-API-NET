using System.Collections.Generic;
using WhatsAppApi.Settings;

namespace WhatsAppApi.Account
{
    public class WhatsUserManager
    {
        private Dictionary<string, WhatsUser> userList;

        public WhatsUserManager()
        {
            this.userList = new Dictionary<string, WhatsUser>();
        }

        public WhatsUser CreateUser(string jid, string nickname = "")
        {
            if (this.userList.ContainsKey(jid))
                return this.userList[jid];

            string server = WhatsConstants.WhatsAppServer;
            if (jid.Contains("-"))
                server = WhatsConstants.WhatsGroupChat;

            var tmpUser = new WhatsUser(jid, server, nickname);
            this.userList.Add(jid, tmpUser);
            return tmpUser;
        }
    }
}