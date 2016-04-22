using System;
using System.Windows.Forms;

namespace WhatsAppPw
{
    public static class Program
    {
        public static void Main()
        {
            String phoneNumber = "";
            String password = PwExtractor.ExtractPassword(phoneNumber, "pw");
            Console.WriteLine("Password : " + password);
            Console.ReadKey();
        }
    }
}