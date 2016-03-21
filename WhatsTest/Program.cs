using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;
using WhatsAppApi.Response;
using WhatsAppPasswordExtractor;

namespace WhatsTest
{
    internal class Program
    {
        // DEMO STORE SHOULD BE DATABASE OR PERMANENT MEDIA IN REAL CASE
        private static IDictionary<string, IdentitiesObject> IdentitiesObjectDic = new Dictionary<string, IdentitiesObject>();

        private static IDictionary<uint, PreKeysObject> PreKeysObjectDic = new Dictionary<uint, PreKeysObject>();
        private static IDictionary<uint, SenderKeysObject> SenderKeysObjectDic = new Dictionary<uint, SenderKeysObject>();
        private static IDictionary<string, SessionsObject> SessionsObjectDic = new Dictionary<string, SessionsObject>();
        private static IDictionary<uint, SignedPreKeysObject> SignedPreKeysObjectDic = new Dictionary<uint, SignedPreKeysObject>();

        private static WhatsApp wa = null;

        private static void Main(string[] args)
        {
            var tmpEncoding = Encoding.UTF8;
            System.Console.OutputEncoding = Encoding.Default;
            System.Console.InputEncoding = Encoding.Default;
            string nickname = "";
            string sender = ""; // Mobile number with country code (but without + or 00)
            string password = PwExtractor.ExtractPassword(sender);//v2 password
            string target = "";// Mobile number to send the message to

            wa = new WhatsApp(sender, password, nickname, true);

            //Event Bindings
            wa.OnLoginSuccess += OnLoginSuccess;
            wa.OnLoginFailed += OnLoginFailed;
            wa.OnGetMessage += OnGetMessage;
            wa.OnGetMessageReadedClient += OnGetMessageReadedClient;
            wa.OnGetMessageReceivedClient += OnGetMessageReceivedClient;
            wa.OnGetMessageReceivedServer += OnGetMessageReceivedServer;
            wa.OnNotificationPicture += OnNotificationPicture;
            wa.OnGetPresence += OnGetPresence;
            wa.OnGetGroupParticipants += OnGetGroupParticipants;
            wa.OnGetLastSeen += OnGetLastSeen;
            wa.OnGetTyping += OnGetTyping;
            wa.OnGetPaused += OnGetPaused;
            wa.OnGetMessageImage += OnGetMessageImage;
            wa.OnGetMessageAudio += OnGetMessageAudio;
            wa.OnGetMessageVideo += OnGetMessageVideo;
            wa.OnGetMessageLocation += OnGetMessageLocation;
            wa.OnGetMessageVcard += OnGetMessageVcard;
            wa.OnGetPhoto += OnGetPhoto;
            wa.OnGetPhotoPreview += OnGetPhotoPreview;
            wa.OnGetGroups += OnGetGroups;
            wa.OnGetSyncResult += OnGetSyncResult;
            wa.OnGetStatus += OnGetStatus;
            wa.OnGetPrivacySettings += OnGetPrivacySettings;
            DebugAdapter.Instance.OnPrintDebug += Instance_OnPrintDebug;
            wa.SendGetServerProperties();
            //ISessionStore AxolotlStore
            wa.OnstoreSession += OnstoreSession;
            wa.OnloadSession += OnloadSession;
            wa.OngetSubDeviceSessions += OngetSubDeviceSessions;
            wa.OncontainsSession += OncontainsSession;
            wa.OndeleteSession += OndeleteSession;
            // IPreKeyStore AxolotlStore
            wa.OnstorePreKey += OnstorePreKey;
            wa.OnloadPreKey += OnloadPreKey;
            wa.OnloadPreKeys += OnloadPreKeys;
            wa.OncontainsPreKey += OncontainsPreKey;
            wa.OnremovePreKey += OnremovePreKey;
            // ISignedPreKeyStore AxolotlStore
            wa.OnstoreSignedPreKey += OnstoreSignedPreKey;
            wa.OnloadSignedPreKey += OnloadSignedPreKey;
            wa.OnloadSignedPreKeys += OnloadSignedPreKeys;
            wa.OncontainsSignedPreKey += OncontainsSignedPreKey;
            wa.OnremoveSignedPreKey += OnremoveSignedPreKey;
            // IIdentityKeyStore AxolotlStore
            wa.OngetIdentityKeyPair += OngetIdentityKeyPair;
            wa.OngetLocalRegistrationId += OngetLocalRegistrationId;
            wa.OnisTrustedIdentity += OnisTrustedIdentity;
            wa.OnsaveIdentity += OnsaveIdentity;
            wa.OnstoreLocalData += OnstoreLocalData;
            // Error Notification ErrorAxolotl
            wa.OnErrorAxolotl += OnErrorAxolotl;

            wa.Connect();

            string datFile = GetDatFileName(sender);
            byte[] nextChallenge = null;
            if (File.Exists(datFile))
            {
                try
                {
                    string foo = File.ReadAllText(datFile);
                    nextChallenge = Convert.FromBase64String(foo);
                }
                catch (Exception) { };
            }

            wa.Login(nextChallenge);
            wa.SendGetPrivacyList();
            wa.SendGetClientConfig();

            if (wa.LoadPreKeys() == null)
                wa.SendSetPreKeys(true);

            ProcessChat(wa, target);
            Console.ReadKey();
        }

        private static void OnGetMessageReadedClient(string from, string id)
        {
            Console.WriteLine("Message {0} to {1} read by client", id, from);
        }

        private static void Instance_OnPrintDebug(object value)
        {
            Console.WriteLine(value);
        }

        private static void OnGetPrivacySettings(Dictionary<ApiBase.VisibilityCategory, ApiBase.VisibilitySetting> settings)
        {
            throw new NotImplementedException();
        }

        private static void OnGetStatus(string from, string type, string name, string status)
        {
            Console.WriteLine(String.Format("Got status from {0}: {1}", from, status));
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

        // string User new
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

        // string name new
        private static void OnGetMessageAudio(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got audio from {0}", from, fileName);
            OnGetMedia(fileName, url, preview);
        }

        // string name new
        private static void OnGetMessageImage(ProtocolTreeNode mediaNode, string from, string id, string fileName, int size, string url, byte[] preview, string name)
        {
            Console.WriteLine("Got image from {0}", from, fileName);
            OnGetMedia(fileName, url, preview);
        }

        private static void OnGetMedia(string file, string url, byte[] data)
        {
            //save preview
            File.WriteAllBytes(string.Format("preview_{0}.jpg", file), data);
            //download
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
            Console.WriteLine("{0} last seen on {1}", from, lastSeen.ToString());
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
            //TODO
            //throw new NotImplementedException();
        }

        private static void OnGetMessage(ProtocolTreeNode node, string from, string id, string name, string message, bool receipt_sent)
        {
            Console.WriteLine("Message from {0} {1}: {2}", name, from, message);
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
            catch (Exception) { }
        }

        private static void ProcessChat(WhatsApp wa, string dst)
        {
            var thRecv = new Thread(t =>
                                        {
                                            try
                                            {
                                                while (wa != null)
                                                {
                                                    wa.PollMessages();
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
            var tmpUser = usrMan.CreateUser(dst, "User");

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null && line.Length == 0)
                    continue;

                string command = line.Trim();
                switch (command)
                {
                    case "/query":
                        //var dst = dst//trim(strstr($line, ' ', FALSE));
                        Console.WriteLine("[] Interactive conversation with {0}:", tmpUser);
                        break;

                    case "/accountinfo":
                        Console.WriteLine("[] Account Info: {0}", wa.GetAccountInfo().ToString());
                        break;

                    case "/lastseen":
                        Console.WriteLine("[] Request last seen {0}", tmpUser);
                        wa.SendQueryLastOnline(tmpUser.GetFullJid());
                        break;

                    case "/exit":
                        wa = null;
                        thRecv.Abort();
                        return;

                    case "/start":
                        wa.SendComposing(tmpUser.GetFullJid());
                        break;

                    case "/pause":
                        wa.SendPaused(tmpUser.GetFullJid());
                        break;

                    default:
                        Console.WriteLine("[] Send message to {0}: {1}", tmpUser, line);
                        wa.SendMessage(tmpUser.GetFullJid(), line);
                        break;
                }
            }
        }

        // ALL NE REQUIRED INTERFACES FOR AXOLOTL ARE BELOW
        /// <summary>
        /// recieve all errormessgaes from the Axolotl process to record
        /// </summary>
        /// <param name="ErrorMessage"></param>
        private static void OnErrorAxolotl(string ErrorMessage)
        {
        }

        #region DATABASE BINDING FOR IIdentityKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="identityKey"></param>
        private static bool OnsaveIdentity(string recipientId, byte[] identityKey)
        {
            if (IdentitiesObjectDic.ContainsKey(recipientId))
                IdentitiesObjectDic.Remove(recipientId);

            IdentitiesObjectDic.Add(recipientId, new IdentitiesObject()
            {
                RecipientId = recipientId,
                PublicKey = identityKey
            });

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="identityKey"></param>
        /// <returns></returns>
        private static bool OnisTrustedIdentity(string recipientId, byte[] identityKey)
        {
            IdentitiesObject trusted;
            IdentitiesObjectDic.TryGetValue(recipientId, out trusted);
            return true; // (trusted == null || trusted.public_key.Equals(identityKey));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static uint OngetLocalRegistrationId()
        {
            IdentitiesObject identity;
            IdentitiesObjectDic.TryGetValue("-1", out identity);
            return (identity == null) ? 000000 : uint.Parse(identity.RegistrationId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static List<byte[]> OngetIdentityKeyPair()
        {
            List<byte[]> result = new List<byte[]> { };
            IdentitiesObject identity;
            IdentitiesObjectDic.TryGetValue("-1", out identity);
            if (identity != null)
            {
                result.Add(identity.PublicKey);
                result.Add(identity.PrivateKey);
            }

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="publickey"></param>
        /// <param name="privatekey"></param>
        private static void OnstoreLocalData(uint registrationId, byte[] publickey, byte[] privatekey)
        {
            if (IdentitiesObjectDic.ContainsKey("-1"))
                IdentitiesObjectDic.Remove("-1");

            IdentitiesObjectDic.Add("-1", new IdentitiesObject()
            {
                RecipientId = "-1",
                RegistrationId = registrationId.ToString(),
                PublicKey = publickey,
                PrivateKey = privatekey
            });
        }

        #endregion DATABASE BINDING FOR IIdentityKeyStore

        #region DATABASE BINDING FOR ISignedPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        private static void OnremoveSignedPreKey(uint preKeyId)
        {
            if (SignedPreKeysObjectDic.ContainsKey(preKeyId))
                SignedPreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        private static bool OncontainsSignedPreKey(uint preKeyId)
        {
            SignedPreKeysObject prekey;
            SignedPreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static List<byte[]> OnloadSignedPreKeys()
        {
            List<byte[]> result = new List<byte[]> { };
            foreach (SignedPreKeysObject key in SignedPreKeysObjectDic.Values)
                result.Add(key.Record);

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        private static byte[] OnloadSignedPreKey(uint preKeyId)
        {
            SignedPreKeysObject prekey;
            SignedPreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey == null) ? new byte[] { } : prekey.Record;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="signedPreKeyId"></param>
        /// <param name="signedPreKeyRecord"></param>
        private static void OnstoreSignedPreKey(uint signedPreKeyId, byte[] signedPreKeyRecord)
        {
            if (SignedPreKeysObjectDic.ContainsKey(signedPreKeyId))
                SignedPreKeysObjectDic.Remove(signedPreKeyId);

            SignedPreKeysObjectDic.Add(signedPreKeyId, new SignedPreKeysObject()
            {
                PreKeyId = signedPreKeyId,
                Record = signedPreKeyRecord
            });
        }

        #endregion DATABASE BINDING FOR ISignedPreKeyStore

        #region DATABASE BINDING FOR IPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        private static void OnremovePreKey(uint preKeyId)
        {
            if (PreKeysObjectDic.ContainsKey(preKeyId))
                PreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        private static bool OncontainsPreKey(uint preKeyId)
        {
            PreKeysObject prekey;
            PreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        private static byte[] OnloadPreKey(uint preKeyId)
        {
            PreKeysObject prekey;
            PreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey == null) ? new byte[] { } : prekey.Record;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static List<byte[]> OnloadPreKeys()
        {
            List<byte[]> result = new List<byte[]> { };
            foreach (PreKeysObject key in PreKeysObjectDic.Values)
                result.Add(key.Record);

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prekeyId"></param>
        /// <param name="preKeyRecord"></param>
        private static void OnstorePreKey(uint prekeyId, byte[] preKeyRecord)
        {
            if (PreKeysObjectDic.ContainsKey(prekeyId))
                PreKeysObjectDic.Remove(prekeyId);

            PreKeysObjectDic.Add(prekeyId, new PreKeysObject()
            {
                PreKeyId = prekeyId.ToString(),
                Record = preKeyRecord
            });
        }

        #endregion DATABASE BINDING FOR IPreKeyStore

        #region DATABASE BINDING FOR ISessionStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="deviceId"></param>
        private static void OndeleteSession(string recipientId, uint deviceId)
        {
            if (SessionsObjectDic.ContainsKey(recipientId))
                SessionsObjectDic.Remove(recipientId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private static bool OncontainsSession(string recipientId, uint deviceId)
        {
            SessionsObject session;
            SessionsObjectDic.TryGetValue(recipientId, out session);
            return (session != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <returns></returns>
        private static List<uint> OngetSubDeviceSessions(string recipientId)
        {
            List<uint> result = new List<uint> { };
            foreach (SessionsObject key in SessionsObjectDic.Values)
                result.Add(key.DeviceId);

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private static byte[] OnloadSession(string recipientId, uint deviceId)
        {
            SessionsObject session;
            SessionsObjectDic.TryGetValue(recipientId, out session);
            return (session == null) ? new byte[] { } : session.Record;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="deviceId"></param>
        /// <param name="sessionRecord"></param>
        private static void OnstoreSession(string recipientId, uint deviceId, byte[] sessionRecord)
        {
            if (SessionsObjectDic.ContainsKey(recipientId))
                SessionsObjectDic.Remove(recipientId);

            SessionsObjectDic.Add(recipientId, new SessionsObject()
            {
                DeviceId = deviceId,
                RecipientId = recipientId,
                Record = sessionRecord
            });
        }

        #endregion DATABASE BINDING FOR ISessionStore
    }

    public class IdentitiesObject
    {
        public string RecipientId
        {
            get; set;
        }

        public string RegistrationId
        {
            get; set;
        }

        public byte[] PublicKey
        {
            get; set;
        }

        public byte[] PrivateKey
        {
            get; set;
        }
    }

    public class PreKeysObject
    {
        public string PreKeyId
        {
            get; set;
        }

        public byte[] Record
        {
            get; set;
        }
    }

    public class SenderKeysObject
    {
        public uint SenderKeyId
        {
            get; set;
        }

        public byte[] Record
        {
            get; set;
        }
    }

    public class SessionsObject
    {
        public string RecipientId
        {
            get; set;
        }

        public uint DeviceId
        {
            get; set;
        }

        public byte[] Record
        {
            get; set;
        }
    }

    public class SignedPreKeysObject
    {
        public uint PreKeyId
        {
            get; set;
        }

        public byte[] Record
        {
            get; set;
        }
    }
}