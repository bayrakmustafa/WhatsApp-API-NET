using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;
using Tr.Com.Eimza.LibAxolotl;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Groups;
using Tr.Com.Eimza.LibAxolotl.State;
using WhatsAppApi.Database;
using WhatsAppApi.Database.Orm;
using WhatsAppApi.Database.Repo;

namespace WhatsAppApi.Store
{
    public class SqliteAxolotlStore : IAxolotStore
    {
        public SqliteAxolotlStore()
        {
            DatabaseUtil.InitializeDatabase();
        }

        public PreKeyRecord LoadPreKey(uint preKeyId)
        {
            PreKeysRepository preKeysRepository = new PreKeysRepository();
            List<PreKeys> preKeys = preKeysRepository.GetPreKeys(Convert.ToString(preKeyId));
            if (preKeys != null && preKeys.Count > 0)
            {
                PreKeyRecord preKeyRecord = new PreKeyRecord(preKeys.First().Record);
                return preKeyRecord;
            }

            return null;
        }

        public void StorePreKey(uint preKeyId, PreKeyRecord record)
        {
            if (ContainsPreKey(preKeyId))
                RemovePreKey(preKeyId);

            PreKeysRepository preKeysRepository = new PreKeysRepository();
            PreKeys preKey = new PreKeys()
            {
                PreKeyId = Convert.ToString(preKeyId),
                Record = record.Serialize()
            };
            bool result = preKeysRepository.Save(preKey);
        }

        public bool ContainsPreKey(uint preKeyId)
        {
            PreKeysRepository preKeysRepository = new PreKeysRepository();
            return preKeysRepository.Contains(Convert.ToString(preKeyId));
        }

        public void RemovePreKey(uint preKeyId)
        {
            PreKeysRepository preKeysRepository = new PreKeysRepository();
            List<PreKeys> preKeys = preKeysRepository.GetPreKeys(Convert.ToString(preKeyId));
            if (preKeys != null && preKeys.Count > 0)
            {
                PreKeys preKey = preKeys.First();
                preKeysRepository.Delete(preKey);
            }
        }

        public void RemoveAllPreKeys()
        {
            PreKeysRepository preKeysRepository = new PreKeysRepository();
            preKeysRepository.DeleteAll();
        }

        public List<PreKeyRecord> LoadPreKeys()
        {
            List<PreKeyRecord> retVal = new List<PreKeyRecord>();

            PreKeysRepository preKeysRepository = new PreKeysRepository();
            List<PreKeys> preKeys = (List<PreKeys>)preKeysRepository.GetAll();
            foreach (PreKeys preKeyse in preKeys)
            {
                retVal.Add(new PreKeyRecord(preKeyse.Record));
            }

            return retVal.Count > 0 ? retVal : null;
        }

        public SignedPreKeyRecord LoadSignedPreKey(uint signedPreKeyId)
        {
            SignedPreKeysRepository preKeysRepository = new SignedPreKeysRepository();
            List<SignedPreKeys> signedPreKeys = preKeysRepository.GetSignedPreKeys(Convert.ToString(signedPreKeyId));
            if (signedPreKeys != null && signedPreKeys.Count > 0)
            {
                SignedPreKeyRecord signedPreKeyRecord = new SignedPreKeyRecord(signedPreKeys.First().Record);
                return signedPreKeyRecord;
            }

            return null;
        }

        public List<SignedPreKeyRecord> LoadSignedPreKeys()
        {
            List<SignedPreKeyRecord> retVal = new List<SignedPreKeyRecord>();

            SignedPreKeysRepository preKeysRepository = new SignedPreKeysRepository();
            List<SignedPreKeys> signedPreKeys = (List<SignedPreKeys>)preKeysRepository.GetAll();
            foreach (SignedPreKeys signedPreKeyse in signedPreKeys)
            {
                retVal.Add(new SignedPreKeyRecord(signedPreKeyse.Record));
            }

            return retVal.Count > 0 ? retVal : null;
        }

        public void StoreSignedPreKey(uint signedPreKeyId, SignedPreKeyRecord record)
        {
            if (ContainsSignedPreKey(signedPreKeyId))
                RemovePreKey(signedPreKeyId);

            SignedPreKeysRepository signedPreKeysRepository = new SignedPreKeysRepository();
            SignedPreKeys signedPreKey = new SignedPreKeys()
            {
                PreKeyId = Convert.ToString(signedPreKeyId),
                Record = record.Serialize()
            };
            bool result = signedPreKeysRepository.Save(signedPreKey);
        }

        public bool ContainsSignedPreKey(uint signedPreKeyId)
        {
            SignedPreKeysRepository signedPreKeysRepository = new SignedPreKeysRepository();
            return signedPreKeysRepository.Contains(Convert.ToString(signedPreKeyId));
        }

        public void RemoveSignedPreKey(uint signedPreKeyId)
        {
            SignedPreKeysRepository signedPreKeysRepository = new SignedPreKeysRepository();
            List<SignedPreKeys> signedPreKeys = signedPreKeysRepository.GetSignedPreKeys(Convert.ToString(signedPreKeyId));
            if (signedPreKeys != null && signedPreKeys.Count > 0)
            {
                SignedPreKeys signedPreKey = signedPreKeys.First();
                signedPreKeysRepository.Delete(signedPreKey);
            }
        }

        public IdentityKeyPair GetIdentityKeyPair()
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            List<IdentityKeys> identityKeys = identityKeysRepository.GetIdentityKeys("-1");
            if (identityKeys != null && identityKeys.Count > 0)
            {
                IdentityKeys identityKey = identityKeys.First();
                return new IdentityKeyPair(new IdentityKey(new DjbECPublicKey(identityKey.PublicKey)), new DjbECPrivateKey(identityKey.PrivateKey));
            }

            return null;
        }

        public uint GetLocalRegistrationId()
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            List<IdentityKeys> identityKeys = identityKeysRepository.GetIdentityKeys("-1");
            if (identityKeys != null && identityKeys.Count > 0)
            {
                IdentityKeys identityKey = identityKeys.First();
                return Convert.ToUInt32(identityKey.RegistrationId);
            }

            return 0;
        }

        public bool SaveIdentity(string recipientId, IdentityKey identityKey)
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            List<IdentityKeys> identityKeys = identityKeysRepository.GetIdentityKeys(recipientId);
            if (identityKeys != null && identityKeys.Count > 0)
            {
                IdentityKeys identity = identityKeys.First();
                identityKeysRepository.Delete(identity);
            }

            IdentityKeys newKeys = new IdentityKeys()
            {
                RecipientId = recipientId,
                PublicKey = identityKey.GetPublicKey().Serialize()
            };

            return identityKeysRepository.Save(newKeys);
        }

        public void StoreLocalData(uint registrationId, IdentityKeyPair identityKey)
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            IdentityKeys newKeys = new IdentityKeys()
            {
                RecipientId = "-1",
                RegistrationId = Convert.ToString(registrationId),
                PublicKey = identityKey.GetPublicKey().Serialize(),
                PrivateKey = identityKey.GetPrivateKey().Serialize()
            };

            identityKeysRepository.Save(newKeys);
        }

        public bool IsTrustedIdentity(string name, IdentityKey identityKey)
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            List<IdentityKeys> identityKeys = identityKeysRepository.GetIdentityKeys(name);
            if (identityKeys != null && identityKeys.Count > 0)
            {
                IdentityKeys identity = identityKeys.First();
                return identity.PublicKey.SequenceEqual(identity.PublicKey);
            }

            return true;
        }

        public SessionRecord LoadSession(AxolotlAddress address)
        {
            SessionsRepository sessionsRepository = new SessionsRepository();
            List<Sessions> sessions = sessionsRepository.GetSessions(address.GetName(), address.GetDeviceId());
            if (sessions != null && sessions.Count > 0)
            {
                Sessions session = sessions.First();
                SessionRecord sessionRecord = new SessionRecord(session.Record);
                return sessionRecord;
            }

            return new SessionRecord();
        }

        public List<uint> GetSubDeviceSessions(string recipientId)
        {
            List<uint> retVal = new List<uint>();

            SessionsRepository sessionsRepository = new SessionsRepository();
            List<Sessions> sessions = sessionsRepository.GetSessions(recipientId);
            if (sessions != null && sessions.Count > 0)
            {
                foreach (Sessions session in sessions)
                {
                    retVal.Add(session.DeviceId);
                }
            }

            return retVal.Count > 0 ? retVal : null;
        }

        public void StoreSession(AxolotlAddress address, SessionRecord record)
        {
            DeleteSession(address);

            SessionsRepository sessionsRepository = new SessionsRepository();
            Sessions session = new Sessions()
            {
                RecipientId = address.GetName(),
                DeviceId = address.GetDeviceId(),
                Record = record.Serialize()
            };
            sessionsRepository.Save(session);
        }

        public bool ContainsSession(AxolotlAddress address)
        {
            SessionsRepository sessionsRepository = new SessionsRepository();
            return sessionsRepository.Contains(address.GetName(), address.GetDeviceId());
        }

        public void DeleteSession(AxolotlAddress address)
        {
            SessionsRepository sessionsRepository = new SessionsRepository();
            List<Sessions> sessions = sessionsRepository.GetSessions(address.GetName(), address.GetDeviceId());
            if (sessions != null && sessions.Count > 0)
            {
                Sessions session = sessions.First();
                sessionsRepository.Delete(session);
            }
        }

        public void DeleteAllSessions(string name)
        {
            SessionsRepository sessionsRepository = new SessionsRepository();
            List<Sessions> sessions = sessionsRepository.GetSessions(name);
            if (sessions != null && sessions.Count > 0)
            {
                foreach (Sessions session in sessions)
                {
                    sessionsRepository.Delete(session);
                }
            }
        }

        public void StoreSenderKey(SenderKeyName senderKeyName, SenderKeyRecord record)
        {
            if (ContainsSenderKey(senderKeyName))
                RemoveSenderKey(senderKeyName);

            String senderKeyId = senderKeyName.GetSender().GetName() + " : " + senderKeyName.GetGroupId();
            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();

            SenderKeys senderKey = new SenderKeys()
            {
                Record = record.Serialize(),
                SenderKeyId = senderKeyId
            };
            senderKeysRepository.Save(senderKey);
        }

        public SenderKeyRecord LoadSenderKey(SenderKeyName senderKeyName)
        {
            String senderKeyId = senderKeyName.GetSender().GetName() + " : " + senderKeyName.GetGroupId();
            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();
            List<SenderKeys> senderKeys = senderKeysRepository.GetSenderKeys(senderKeyId);
            if (senderKeys != null && senderKeys.Count > 0)
            {
                SenderKeys senderKey = senderKeys.First();
                SenderKeyRecord senderKeyRecord = new SenderKeyRecord(senderKey.Record);
                return senderKeyRecord;
            }

            return new SenderKeyRecord();
        }

        public bool ContainsSenderKey(SenderKeyName senderKeyName)
        {
            String senderKeyId = senderKeyName.GetSender().GetName() + " : " + senderKeyName.GetGroupId();
            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();
            return senderKeysRepository.Contains(senderKeyId);
        }

        public void RemoveSenderKey(SenderKeyName senderKeyName)
        {
            String senderKeyId = senderKeyName.GetSender().GetName() + " : " + senderKeyName.GetGroupId();
            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();
            List<SenderKeys> senderKeys = senderKeysRepository.GetSenderKeys(senderKeyId);
            if (senderKeys != null && senderKeys.Count > 0)
            {
                SenderKeys senderKey = senderKeys.First();
                senderKeysRepository.Delete(senderKey);
            }
        }

        public List<SenderKeyRecord> LoadSenderKeys()
        {
            List<SenderKeyRecord> retVal = new List<SenderKeyRecord>();

            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();
            List<SenderKeys> senderKeys = (List<SenderKeys>)senderKeysRepository.GetAll();
            foreach (SenderKeys senderKey in senderKeys)
            {
                retVal.Add(new SenderKeyRecord(senderKey.Record));
            }

            return retVal.Count > 0 ? retVal : null;
        }

        public void Clear()
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            identityKeysRepository.DeleteAll();

            PreKeysRepository preKeysRepository = new PreKeysRepository();
            preKeysRepository.DeleteAll();

            SenderKeysRepository senderKeysRepository = new SenderKeysRepository();
            senderKeysRepository.DeleteAll();

            SessionsRepository sessionsRepository = new SessionsRepository();
            sessionsRepository.DeleteAll();

            SignedPreKeysRepository signedPreKeysRepository = new SignedPreKeysRepository();
            signedPreKeysRepository.DeleteAll();
        }

        public void ClearRecipient(string recipientId)
        {
            IdentityKeysRepository identityKeysRepository = new IdentityKeysRepository();
            List<IdentityKeys> identityKeys = identityKeysRepository.GetIdentityKeys(recipientId);
            if (identityKeys != null && identityKeys.Count > 0)
            {
                identityKeysRepository.Delete(identityKeys.First());
            }

            SessionsRepository sessionsRepository = new SessionsRepository();
            List<Sessions> sessions = sessionsRepository.GetSessions(recipientId);
            if (sessions != null && sessions.Count > 0)
            {
                sessionsRepository.Delete(sessions.First());
            }
        }
    }
}
