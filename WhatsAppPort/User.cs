using WhatsAppApi.Account;

namespace WhatsAppPort
{
    public class User
    {
        public string PhoneNumber
        {
            get; private set;
        }

        public string UserName
        {
            get; private set;
        }

        public WhatsUser WhatsUser
        {
            get; private set;
        }

        public User(string phone, string name)
        {
            this.PhoneNumber = phone;
            this.UserName = name;
        }

        public static User UserExists(string phoneNum, string nickName)
        {
            WhatsUserManager man = new WhatsUserManager();
            WhatsUser whatsUser = man.CreateUser(phoneNum, phoneNum);
            User tmpUser = new User(phoneNum, nickName);
            tmpUser.SetUser(whatsUser);
            return tmpUser;
        }

        public void SetUser(WhatsUser user)
        {
            if (this.WhatsUser != null)
                return;

            this.WhatsUser = user;
        }
    }
}