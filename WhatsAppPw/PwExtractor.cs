using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Managed.Adb;
using OpenSSL.Crypto;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using WhatsAppPasswordExtractor.Properties;

namespace WhatsAppPasswordExtractor
{
    public class PwExtractor
    {
        private static String PasswordFile = "pw";

        public static String ExtractPassword(String phoneNumber)
        {

            byte[] usernameKey = Encoding.UTF8.GetBytes(phoneNumber);
            byte[] key = Hex.Decode(Encoding.UTF8.GetBytes("c2991ec29b1d0cc2b8c3b7556458c298c29203c28b45c2973e78c386c395"));

            MemoryStream PbkdfFileData = new MemoryStream();
            PbkdfFileData.Write(key, 0, key.Length);
            PbkdfFileData.Write(usernameKey, 0, usernameKey.Length);
            PbkdfFileData.Flush();
            PbkdfFileData.Close();

            AndroidDebugBridge _AndroidDebugDevice = null;
            try
            {
                _AndroidDebugDevice = AndroidDebugBridge.CreateBridge("Tools\\adb.exe", false);
                _AndroidDebugDevice.Start();

                //Get First Device
                Device _Device = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress).First();

                if (_Device != null)
                {
                    //Is Root Device
                    if (_Device.CanSU())
                    {
                        Object[] args = new Object[2];
                        args[0] = "/data/data/com.whatsapp/files/pw";
                        args[1] = "/sdcard";
                        _Device.ExecuteRootShellCommand("cp /data/data/com.whatsapp/files/pw /sdcard", new ConsoleOutputReceiver(), args);

                        using (SyncService service = new SyncService(_Device))
                        {
                            SyncResult syncResult = service.PullFile("/sdcard/pw", PasswordFile, new NullSyncProgressMonitor());
                            if (syncResult.Code == 0)
                            {
                                byte[] pw = File.ReadAllBytes(PasswordFile);

                                byte[] pw_key = new byte[20];
                                Buffer.BlockCopy(pw, 49, pw_key, 0, 20);
                                //File.WriteAllBytes("pw_key", pw_key);

                                byte[] pw_salt = new byte[4];
                                Buffer.BlockCopy(pw, 29, pw_salt, 0, 4);
                                //File.WriteAllBytes("pw_salt", pw_salt);

                                byte[] pw_iv = new byte[16];
                                Buffer.BlockCopy(pw, 33, pw_iv, 0, 16);
                                //File.WriteAllBytes("pw_iv", pw_iv);                                

                                byte[] pbkdf2_pass_bin = PbkdfFileData.ToArray();

                                Pkcs5S2ParametersGenerator bcKeyDer = new Pkcs5S2ParametersGenerator();
                                bcKeyDer.Init(pbkdf2_pass_bin, pw_salt, 16);
                                KeyParameter keyParameter = (KeyParameter)bcKeyDer.GenerateDerivedParameters("AES128", 128);
                                byte[] pbkdf2_key_bin = keyParameter.GetKey();

                                Cipher cipher = Cipher.AES_128_OFB;
                                CipherContext cipherContext = new CipherContext(cipher);
                                byte[] passwordData = cipherContext.Decrypt(pw_key, pbkdf2_key_bin, pw_iv);

                                String password = Convert.ToBase64String(passwordData);
                                return password;

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (_AndroidDebugDevice != null)
                {
                    _AndroidDebugDevice.Stop();
                }

                CleanUp();
            }

            return null;
        }

        public static void CleanUp()
        {
            File.Delete(PasswordFile);
        }

    }
}
