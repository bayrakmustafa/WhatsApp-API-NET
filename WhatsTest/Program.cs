using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;
using WhatsAppApi.Parser;
using WhatsAppApi.Response;
using WhatsAppPw;

namespace WhatsTest
{
    internal class Program
    {
        private static WhatsApp _WhatsAppApi = null;

        private static void Main(string[] args)
        {
            //Turkish Encoding
            System.Console.OutputEncoding = Encoding.GetEncoding(857);
            System.Console.InputEncoding = Encoding.GetEncoding(857);

            //UTF-8 Encoding
            //System.Console.OutputEncoding = Encoding.UTF8;
            //System.Console.InputEncoding = Encoding.UTF8;

            string _Nickname = "";
            string _Sender = ""; //Mobile Number with Country Code (but without + or 00)
            string _Password = ""; //v2 password
            string _Target = ""; // Mobile Number to Send the Message to

            _WhatsAppApi = new WhatsApp(_Sender, _Password, _Nickname, true);

            //Event Bindings
            _WhatsAppApi.OnLoginSuccess += OnLoginSuccess;
            _WhatsAppApi.OnLoginFailed += OnLoginFailed;
            _WhatsAppApi.OnGetMessage += OnGetMessage;
            _WhatsAppApi.OnGetMessageReadedClient += OnGetMessageReadedClient;
            _WhatsAppApi.OnGetMessageReceivedClient += OnGetMessageReceivedClient;
            _WhatsAppApi.OnGetMessageReceivedServer += OnGetMessageReceivedServer;
            _WhatsAppApi.OnNotificationPicture += OnNotificationPicture;
            _WhatsAppApi.OnGetPresence += OnGetPresence;
            _WhatsAppApi.OnGetGroupParticipants += OnGetGroupParticipants;
            _WhatsAppApi.OnGetLastSeen += OnGetLastSeen;
            _WhatsAppApi.OnGetTyping += OnGetTyping;
            _WhatsAppApi.OnGetPaused += OnGetPaused;
            _WhatsAppApi.OnGetMessageImage += OnGetMessageImage;
            _WhatsAppApi.OnGetMessageAudio += OnGetMessageAudio;
            _WhatsAppApi.OnGetMessageVideo += OnGetMessageVideo;
            _WhatsAppApi.OnGetMessageLocation += OnGetMessageLocation;
            _WhatsAppApi.OnGetMessageVcard += OnGetMessageVcard;
            _WhatsAppApi.OnGetPhoto += OnGetPhoto;
            _WhatsAppApi.OnGetPhotoPreview += OnGetPhotoPreview;
            _WhatsAppApi.OnGetGroups += OnGetGroups;
            _WhatsAppApi.OnGetSyncResult += OnGetSyncResult;
            _WhatsAppApi.OnGetStatus += OnGetStatus;
            _WhatsAppApi.OnGetPrivacySettings += OnGetPrivacySettings;
            _WhatsAppApi.OnGetBroadcastLists += OnGetBroadcastLists;

            /*Debug Code*/
            DebugAdapter.Instance.OnPrintDebug += Instance_OnPrintDebug;

            _WhatsAppApi.SendGetServerProperties();

            // Error Notification ErrorAxolotl
            _WhatsAppApi.OnErrorAxolotl += OnErrorAxolotl;

            _WhatsAppApi.Connect();

            string datFile = GetDatFileName(_Sender);
            byte[] nextChallenge = null;
            if (File.Exists(datFile))
            {
                try
                {
                    String foo = File.ReadAllText(datFile);
                    nextChallenge = Convert.FromBase64String(foo);
                }
                catch (Exception)
                {

                };
            }

            _WhatsAppApi.Login(nextChallenge);

            ProcessChat(_WhatsAppApi, _Target);
            Console.ReadKey();
        }

        private static void OnGetBroadcastLists(string phoneNumber, List<WaBroadcast> broadcastLists)
        {
            foreach (WaBroadcast broadcast in broadcastLists)
            {
                Console.WriteLine("Broadcast Lists");
                Console.WriteLine("Name : " + broadcast.Name);
                foreach (string recipient in broadcast.Recipients)
                {
                    Console.WriteLine("Recipient : " + recipient);
                }
            }
        }

        private static void OnGetMessageReadedClient(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} read by client", id, from);
        }

        private static void Instance_OnPrintDebug(object value)
        {
            Console.WriteLine("Debug Message : " + value);
        }

        private static void OnGetPrivacySettings(Dictionary<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting> settings)
        {
            if (settings != null)
            {
                foreach (KeyValuePair<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting> visibilitySetting in settings)
                {
                    Console.WriteLine("Visibility Category : " + visibilitySetting.Key);
                    Console.WriteLine("Visibility Setting : " + visibilitySetting.Value);
                }
            }
        }

        private static void OnGetStatus(string from, string type, string name, string status)
        {
            Console.WriteLine(String.Format("Got Status From {0}: {1}", from, status));
        }

        private static string GetDatFileName(string pn)
        {
            string filename = string.Format("{0}.next.dat", pn);
            return Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        private static void OnGetSyncResult(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers)
        {
            Console.WriteLine("Sync result for {0}:", sid);
            foreach (KeyValuePair<string, string> item in existingUsers)
            {
                Console.WriteLine("Existing: {0} (username {1})", item.Key, item.Value);
            }
            foreach (string item in failedNumbers)
            {
                Console.WriteLine("Non-Existing: {0}", item);
            }
        }

        private static void OnGetGroups(WaGroupInfo[] groups)
        {
            Console.WriteLine("Got groups:");
            foreach (WaGroupInfo info in groups)
            {
                Console.WriteLine("\t{0} {1}", info.subject, info.id);
            }
        }

        private static void OnGetPhotoPreview(string from, string id, byte[] data)
        {
            Console.WriteLine("Got preview photo for {0}", from);
            File.WriteAllBytes(string.Format("preview_{0}.jpg", from), data);
        }

        private static void OnGetPhoto(string from, string id, byte[] data)
        {
            Console.WriteLine("Got full photo for {0}", from);
            File.WriteAllBytes(string.Format("{0}.jpg", from), data);
        }

        private static void OnGetMessageVcard(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data)
        {
            Console.WriteLine("Got vcard \"{0}\" from {1}", name, from);
            File.WriteAllBytes(string.Format("{0}.vcf", name), data);
        }

        private static void OnGetMessageLocation(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string User)
        {
            Console.WriteLine("Got location from {0} ({1}, {2})", from, lat, lon);
            if (!string.IsNullOrEmpty(name))
            {
                Console.WriteLine("\t{0}", name);
            }
            File.WriteAllBytes(string.Format("{0}{1}.jpg", lat, lon), preview);
        }

        private static void OnGetMessageVideo(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got video from {0}", from, fileName);
            OnGetMedia(fileName, url, preview);
        }

        private static void OnGetMessageAudio(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got audio from {0}", from, fileName);
            OnGetMedia(fileName, url, preview);
        }

        private static void OnGetMessageImage(ProtocolTreeNode mediaNode, string from, string id, string fileName, int size, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got image from {0}", from, fileName);
            OnGetMedia(fileName, url, preview);
        }

        private static void OnGetMedia(string file, string url, byte[] data)
        {
            File.WriteAllBytes(string.Format("Preview_{0}.jpg", file), data);
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileAsync(new Uri(url), file, null);
            }
        }

        private static void OnGetPaused(string from)
        {
            Console.WriteLine("{0} stopped typing", from);
        }

        private static void OnGetTyping(string from)
        {
            Console.WriteLine("{0} is typing...", from);
        }

        private static void OnGetLastSeen(string from, DateTime lastSeen)
        {
            Console.WriteLine("{0} last seen on {1}", from, lastSeen.ToString(CultureInfo.InvariantCulture));
        }

        private static void OnGetMessageReceivedServer(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} received by server", id, from);
        }

        private static void OnGetMessageReceivedClient(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} received by client", id, from);
        }

        private static void OnGetGroupParticipants(string gjid, string[] jids)
        {
            Console.WriteLine("Got participants from {0}:", gjid);
            foreach (string jid in jids)
            {
                Console.WriteLine("\t{0}", jid);
            }
        }

        private static void OnGetPresence(string from, string type)
        {
            Console.WriteLine("Presence from {0}: {1}", from, type);
        }

        private static void OnNotificationPicture(string type, string jid, string id)
        {
            throw new NotImplementedException();
        }

        private static void OnGetMessage(ProtocolTreeNode node, string from, string id, string name, string message, bool receipt_sent)
        {
            Console.WriteLine("Message From {0} {1}: {2}", name, from, message);
        }

        private static void OnLoginFailed(string data)
        {
            Console.WriteLine("Login failed. Reason: {0}", data);
        }

        private static void OnLoginSuccess(string phoneNumber, byte[] data)
        {
            Console.WriteLine("Login success. Next password:");
            string sdata = Convert.ToBase64String(data);
            Console.WriteLine(sdata);
            try
            {
                File.WriteAllText(GetDatFileName(phoneNumber), sdata);
            }
            catch (Exception)
            {

            }
        }

        private static void ProcessChat(WhatsApp _WhatsAppApi, string _Dest)
        {
            Thread thRecv = new Thread(t =>
                                        {
                                            try
                                            {
                                                while (_WhatsAppApi != null)
                                                {
                                                    _WhatsAppApi.PollMessages();
                                                    Thread.Sleep(100);
                                                    continue;
                                                }
                                            }
                                            catch (ThreadAbortException)
                                            {
                                            }
                                        })
            {
                IsBackground = true
            };
            thRecv.Start();

            WhatsUserManager usrMan = new WhatsUserManager();
            WhatsUser tmpUser = usrMan.CreateUser(_Dest, "User");

            while (true)
            {
                String line = Console.ReadLine();
                if (String.IsNullOrEmpty(line))
                    continue;

                string command = line.Trim();
                switch (command)
                {
                    case "/query":
                        Console.WriteLine("[] Interactive conversation with {0}:", tmpUser);
                        break;

                    case "/accountinfo":
                        Console.WriteLine("[] Account Info: {0}", _WhatsAppApi.GetAccountInfo().ToString());
                        break;

                    case "/lastseen":
                        Console.WriteLine("[] Request last seen {0}", tmpUser);
                        _WhatsAppApi.SendQueryLastOnline(tmpUser.GetFullJid());
                        break;

                    case "/exit":
                        _WhatsAppApi = null;
                        thRecv.Abort();
                        return;

                    case "/start":
                        _WhatsAppApi.SendComposing(tmpUser.GetFullJid());
                        break;

                    case "/pause":
                        _WhatsAppApi.SendPaused(tmpUser.GetFullJid());
                        break;

                    default:
                        Console.WriteLine("[] Send message to {0}: {1}", tmpUser, line);
                        _WhatsAppApi.SendMessage(tmpUser.GetFullJid(), line);
                        break;
                }
            }
        }

        /// <summary>
        /// Recieve All Error Messgaes From the Axolotl Orocess to Record
        /// </summary>
        /// <param name="ErrorMessage"></param>
        private static void OnErrorAxolotl(string ErrorMessage)
        {
        }
    }
}