using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppApi.Store
{
    public class MemoryAxolotlStore
    {
        // Demo Store Should be Database or Permanent Media in Real Case
        public IDictionary<string, IdentitiesObject> IdentitiesObjectDic = new Dictionary<string, IdentitiesObject>();

        public IDictionary<uint, PreKeysObject> PreKeysObjectDic = new Dictionary<uint, PreKeysObject>();
        public IDictionary<uint, SenderKeysObject> SenderKeysObjectDic = new Dictionary<uint, SenderKeysObject>();
        public IDictionary<string, SessionsObject> SessionsObjectDic = new Dictionary<string, SessionsObject>();
        public IDictionary<uint, SignedPreKeysObject> SignedPreKeysObjectDic = new Dictionary<uint, SignedPreKeysObject>();

        #region Database Binding For IIdentityKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="identityKey"></param>
        public bool OnsaveIdentity(string recipientId, byte[] identityKey)
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
        public bool OnisTrustedIdentity(string recipientId, byte[] identityKey)
        {
            IdentitiesObject trusted;
            IdentitiesObjectDic.TryGetValue(recipientId, out trusted);
            return true; // (trusted == null || trusted.public_key.Equals(identityKey));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public uint OngetLocalRegistrationId()
        {
            IdentitiesObject identity;
            IdentitiesObjectDic.TryGetValue("-1", out identity);
            return (identity == null) ? 000000 : uint.Parse(identity.RegistrationId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<byte[]> OngetIdentityKeyPair()
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
        public void OnstoreLocalData(uint registrationId, byte[] publickey, byte[] privatekey)
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

        #endregion Database Binding For IIdentityKeyStore

        #region Database Binding For ISignedPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        public void OnremoveSignedPreKey(uint preKeyId)
        {
            if (SignedPreKeysObjectDic.ContainsKey(preKeyId))
                SignedPreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        public bool OncontainsSignedPreKey(uint preKeyId)
        {
            SignedPreKeysObject prekey;
            SignedPreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey != null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<byte[]> OnloadSignedPreKeys()
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
        public byte[] OnloadSignedPreKey(uint preKeyId)
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
        public void OnstoreSignedPreKey(uint signedPreKeyId, byte[] signedPreKeyRecord)
        {
            if (SignedPreKeysObjectDic.ContainsKey(signedPreKeyId))
                SignedPreKeysObjectDic.Remove(signedPreKeyId);

            SignedPreKeysObjectDic.Add(signedPreKeyId, new SignedPreKeysObject()
            {
                PreKeyId = signedPreKeyId,
                Record = signedPreKeyRecord
            });
        }

        #endregion Database Binding For ISignedPreKeyStore

        #region Database Binding For IPreKeyStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        public void OnremovePreKey(uint preKeyId)
        {
            if (PreKeysObjectDic.ContainsKey(preKeyId))
                PreKeysObjectDic.Remove(preKeyId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preKeyId"></param>
        /// <returns></returns>
        public bool OncontainsPreKey(uint preKeyId)
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
        public byte[] OnloadPreKey(uint preKeyId)
        {
            PreKeysObject prekey;
            PreKeysObjectDic.TryGetValue(preKeyId, out prekey);
            return (prekey == null) ? new byte[] { } : prekey.Record;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<byte[]> OnloadPreKeys()
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
        public void OnstorePreKey(uint prekeyId, byte[] preKeyRecord)
        {
            if (PreKeysObjectDic.ContainsKey(prekeyId))
                PreKeysObjectDic.Remove(prekeyId);

            PreKeysObjectDic.Add(prekeyId, new PreKeysObject()
            {
                PreKeyId = prekeyId.ToString(),
                Record = preKeyRecord
            });
        }

        #endregion Database Binding For IPreKeyStore

        #region Database Binding For ISessionStore

        /// <summary>
        ///
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="deviceId"></param>
        public void OndeleteSession(string recipientId, uint deviceId)
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
        public bool OncontainsSession(string recipientId, uint deviceId)
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
        public List<uint> OngetSubDeviceSessions(string recipientId)
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
        public byte[] OnloadSession(string recipientId, uint deviceId)
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
        public void OnstoreSession(string recipientId, uint deviceId, byte[] sessionRecord)
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

        #endregion Database Binding For ISessionStore

        public void Clear()
        {
            IdentitiesObjectDic = new Dictionary<string, IdentitiesObject>();
            PreKeysObjectDic = new Dictionary<uint, PreKeysObject>();
            SenderKeysObjectDic = new Dictionary<uint, SenderKeysObject>();
            SessionsObjectDic = new Dictionary<string, SessionsObject>();
            SignedPreKeysObjectDic = new Dictionary<uint, SignedPreKeysObject>();
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
