using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Tr.Com.Eimza.LibAxolotl;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Groups;
using Tr.Com.Eimza.LibAxolotl.Groups.State;
using Tr.Com.Eimza.LibAxolotl.State;

namespace WhatsAppApi.Store
{
    public class MemoryAxolotlStore : IAxolotStore
    {
        public IDictionary<string, IdentitiesObject> IdentitiesObjectDic = new Dictionary<string, IdentitiesObject>();
        public IDictionary<string, SessionsObject> SessionsObjectDic = new Dictionary<string, SessionsObject>();

        public IDictionary<uint, PreKeysObject> PreKeysObjectDic = new Dictionary<uint, PreKeysObject>();
        public IDictionary<uint, SignedPreKeysObject> SignedPreKeysObjectDic = new Dictionary<uint, SignedPreKeysObject>();

        public IDictionary<SenderKeyName, SenderKeysObject> SenderKeysObjectsDic = new Dictionary<SenderKeyName, SenderKeysObject>();

        #region Database Binding For IIdentityKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="identityKey"></param>
        public bool SaveIdentity(string recipientId, IdentityKey identityKey)
        {
            if (IdentitiesObjectDic.ContainsKey(recipientId))
                IdentitiesObjectDic.Remove(recipientId);

            IdentitiesObjectDic.Add(recipientId, new IdentitiesObject()
            {
                RecipientId = recipientId,
                PublicKey = identityKey.GetPublicKey().Serialize()
            });

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="identityKey"></param>
        /// <returns></returns>
        public bool IsTrustedIdentity(string recipientId, IdentityKey identityKey)
        {
            IdentitiesObject trusted;
            IdentitiesObjectDic.TryGetValue(recipientId, out trusted);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public uint GetLocalRegistrationId()
        {
            IdentitiesObject identity;
            IdentitiesObjectDic.TryGetValue("-1", out identity);
            return (identity == null) ? 000000 : uint.Parse(identity.RegistrationId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IdentityKeyPair GetIdentityKeyPair()
        {
            IdentityKeyPair result = null;
            IdentitiesObject identity;
            IdentitiesObjectDic.TryGetValue("-1", out identity);
            if (identity != null)
            {
                result = new IdentityKeyPair(new IdentityKey(new DjbECPublicKey(identity.PublicKey)), new DjbECPrivateKey(identity.PrivateKey));
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="identityKey"></param>
        public void StoreLocalData(uint registrationId, IdentityKeyPair identityKey)
        {
            if (IdentitiesObjectDic.ContainsKey("-1"))
                IdentitiesObjectDic.Remove("-1");

            IdentitiesObjectDic.Add("-1", new IdentitiesObject()
            {
                RecipientId = "-1",
                RegistrationId = registrationId.ToString(),
                PublicKey = identityKey.GetPublicKey().Serialize(),
                PrivateKey = identityKey.GetPrivateKey().Serialize()
            });
        }

        #endregion Database Binding For IIdentityKeyStore

        #region Database Binding For ISignedPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        public void RemoveSignedPreKey(uint preKeyId)
        {
            if (SignedPreKeysObjectDic.ContainsKey(preKeyId))
                SignedPreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        public bool ContainsSignedPreKey(uint preKeyId)
        {
            SignedPreKeysObject prekey;
            SignedPreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<SignedPreKeyRecord> LoadSignedPreKeys()
        {
            List<SignedPreKeyRecord> result = new List<SignedPreKeyRecord> { };
            foreach (SignedPreKeysObject key in SignedPreKeysObjectDic.Values)
                result.Add(new SignedPreKeyRecord(key.Record));

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        public SignedPreKeyRecord LoadSignedPreKey(uint preKeyId)
        {
            SignedPreKeysObject prekey;
            SignedPreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return prekey != null ? new SignedPreKeyRecord(prekey.Record) : null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="signedPreKeyId"></param>
        /// <param name="signedPreKeyRecord"></param>
        public void StoreSignedPreKey(uint signedPreKeyId, SignedPreKeyRecord signedPreKeyRecord)
        {
            if (SignedPreKeysObjectDic.ContainsKey(signedPreKeyId))
                SignedPreKeysObjectDic.Remove(signedPreKeyId);

            SignedPreKeysObjectDic.Add(signedPreKeyId, new SignedPreKeysObject()
            {
                PreKeyId = signedPreKeyId,
                Record = signedPreKeyRecord.Serialize()
            });
        }

        #endregion Database Binding For ISignedPreKeyStore

        #region Database Binding For SenderKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="senderKeyName"></param>
        public void RemoveSenderKey(SenderKeyName senderKeyName)
        {
            if (SenderKeysObjectsDic.ContainsKey(senderKeyName))
                SenderKeysObjectsDic.Remove(senderKeyName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="senderKeyName"></param>
        /// <returns></returns>
        public bool ContainsSenderKey(SenderKeyName senderKeyName)
        {
            SenderKeysObject prekey;
            SenderKeysObjectsDic.TryGetValue(senderKeyName, out prekey);
            return (prekey != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<SenderKeyRecord> LoadSenderKeys()
        {
            List<SenderKeyRecord> result = new List<SenderKeyRecord> { };
            foreach (SenderKeysObject key in SenderKeysObjectsDic.Values)
                result.Add(new SenderKeyRecord(key.Record));

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="senderKeyName"></param>
        /// <returns></returns>
        public SenderKeyRecord LoadSenderKey(SenderKeyName senderKeyName)
        {
            SenderKeysObject prekey;
            SenderKeysObjectsDic.TryGetValue(senderKeyName, out prekey);
            return prekey != null ? new SenderKeyRecord(prekey.Record) : new SenderKeyRecord();

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="senderKeyName"></param>
        /// <param name="record"></param>
        public void StoreSenderKey(SenderKeyName senderKeyName, SenderKeyRecord record)
        {
            if (SenderKeysObjectsDic.ContainsKey(senderKeyName))
                SenderKeysObjectsDic.Remove(senderKeyName);

            SenderKeysObjectsDic.Add(senderKeyName, new SenderKeysObject()
            {
                SenderKeyKeyName = senderKeyName,
                Record = record.Serialize()
            });
        }

        #endregion Database Binding For SenderKeyStore

        #region Database Binding For IPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        public void RemovePreKey(uint preKeyId)
        {
            if (PreKeysObjectDic.ContainsKey(preKeyId))
                PreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        public void RemoveAllPreKeys()
        {
            PreKeysObjectDic.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        public bool ContainsPreKey(uint preKeyId)
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
        public PreKeyRecord LoadPreKey(uint preKeyId)
        {
            PreKeysObject prekey;
            PreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return prekey != null ? new PreKeyRecord(prekey.Record) : null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<PreKeyRecord> LoadPreKeys()
        {
            List<PreKeyRecord> result = new List<PreKeyRecord> { };
            foreach (PreKeysObject key in PreKeysObjectDic.Values)
                result.Add(new PreKeyRecord(key.Record));

            if (result.Count == 0)
                return null;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prekeyId"></param>
        /// <param name="preKeyRecord"></param>
        public void StorePreKey(uint prekeyId, PreKeyRecord preKeyRecord)
        {
            if (PreKeysObjectDic.ContainsKey(prekeyId))
                PreKeysObjectDic.Remove(prekeyId);

            PreKeysObjectDic.Add(prekeyId, new PreKeysObject()
            {
                PreKeyId = prekeyId.ToString(),
                Record = preKeyRecord.Serialize()
            });
        }

        #endregion Database Binding For IPreKeyStore

        #region Database Binding For ISessionStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        public void DeleteSession(AxolotlAddress address)
        {
            String recipientId = address.GetName();
            if (SessionsObjectDic.ContainsKey(recipientId))
                SessionsObjectDic.Remove(recipientId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        public void DeleteAllSessions(string name)
        {
            if (SessionsObjectDic.ContainsKey(name))
                SessionsObjectDic.Remove(name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool ContainsSession(AxolotlAddress address)
        {
            SessionsObject session;
            SessionsObjectDic.TryGetValue(address.GetName(), out session);
            return (session != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <returns></returns>
        public List<uint> GetSubDeviceSessions(string recipientId)
        {
            List<uint> result = new List<uint> { };
            foreach (SessionsObject key in SessionsObjectDic.Values)
                result.Add(key.DeviceId);

            return result;
        }

        ///  <summary>
        /// 
        ///  </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public SessionRecord LoadSession(AxolotlAddress address)
        {
            SessionsObject session;
            SessionsObjectDic.TryGetValue(address.GetName(), out session);
            return session != null ? new SessionRecord(session.Record) : new SessionRecord();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <param name="record"></param>
        public void StoreSession(AxolotlAddress address, SessionRecord record)
        {
            String recipientId = address.GetName();
            uint deviceId = address.GetDeviceId();
            if (SessionsObjectDic.ContainsKey(recipientId))
                SessionsObjectDic.Remove(recipientId);

            SessionsObjectDic.Add(recipientId, new SessionsObject()
            {
                DeviceId = deviceId,
                RecipientId = recipientId,
                Record = record.Serialize()
            });
        }

        #endregion Database Binding For ISessionStore

        public void Clear()
        {
            IdentitiesObjectDic = new Dictionary<string, IdentitiesObject>();
            PreKeysObjectDic = new Dictionary<uint, PreKeysObject>();
            SenderKeysObjectsDic = new Dictionary<SenderKeyName, SenderKeysObject>();
            SessionsObjectDic = new Dictionary<string, SessionsObject>();
            SignedPreKeysObjectDic = new Dictionary<uint, SignedPreKeysObject>();
        }

        public void ClearRecipient(String recipientId)
        {
            IdentitiesObjectDic.Remove(recipientId);
            SessionsObjectDic.Remove(recipientId);
        }
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
        public SenderKeyName SenderKeyKeyName
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
