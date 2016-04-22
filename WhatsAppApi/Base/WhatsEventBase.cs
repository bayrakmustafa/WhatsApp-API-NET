using System;
using System.Collections.Generic;
using System.Linq;
using Tr.Com.Eimza.LibAxolotl;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Groups;
using Tr.Com.Eimza.LibAxolotl.Groups.State;
using Tr.Com.Eimza.LibAxolotl.State;
using WhatsAppApi.Helper;
using WhatsAppApi.Response;
using WhatsAppApi.Store;

namespace WhatsAppApi
{
    public class WhatsEventBase : ApiBase
    {
        public delegate void ExceptionDelegate(Exception ex);

        public delegate void LoginSuccessDelegate(string phoneNumber, byte[] data);

        public delegate void NullDelegate();

        public delegate void OnContactNameDelegate(string from, string contactName);

        public delegate bool OnContainsPreKeyDelegate(uint preKeyId);

        public delegate bool OnContainsSenderKeyDelegate(SenderKeyName senderKeyName);

        public delegate bool OnContainsSessionDelegate(AxolotlAddress address);

        public delegate bool OnContainsSignedPreKeyDelegate(uint preKeyId);

        public delegate void OnDeleteAllSessionsDelegate(string recipientId);

        public delegate void OnDeleteSessionDelegate(AxolotlAddress address);

        public delegate void OnErrorAxolotlDelegate(string ErrorMessage);

        public delegate void OnErrorDelegate(string id, string from, int code, string text);

        public delegate void OnGetBroadcastListsDelegate(string phoneNumber, List<WaBroadcast> broadcastLists);

        public delegate void OnGetChatStateDelegate(string from);

        public delegate void OnGetGroupParticipantsDelegate(string gjid, string[] jids);

        public delegate void OnGetGroupsDelegate(WaGroupInfo[] groups);

        public delegate void OnGetGroupSubjectDelegate(string gjid, string jid, string username, string subject, DateTime time);

        public delegate IdentityKeyPair OnGetIdentityKeyPairDelegate();

        public delegate void OnGetLastSeenDelegate(string from, DateTime lastSeen);

        public delegate uint OnGetLocalRegistrationIdDelegate();

        public delegate void OnGetLocationDelegate(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string UserName);

        public delegate void OnGetMediaDelegate(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name);

        public delegate void OnGetMessageDelegate(ProtocolTreeNode messageNode, string from, string id, string name, string message, bool receipt_sent);

        public delegate void OnGetMessageReceivedDelegate(string from, string id);

        public delegate void OnGetParticipantAddedDelegate(string gjid, string jid, DateTime time);

        public delegate void OnGetParticipantRemovedDelegate(string gjid, string jid, string author, DateTime time);

        public delegate void OnGetParticipantRenamedDelegate(string gjid, string oldJid, string newJid, DateTime time);

        public delegate void OnGetPictureDelegate(string from, string id, byte[] data);

        public delegate void OnGetPresenceDelegate(string from, string type);

        public delegate void OnGetPrivacySettingsDelegate(Dictionary<VisibilityCategory, VisibilitySetting> settings);

        public delegate void OnGetStatusDelegate(string from, string type, string name, string status);

        public delegate List<uint> OnGetSubDeviceSessionsDelegate(string recipientId);

        public delegate void OnGetSyncResultDelegate(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers);

        public delegate void OnGetVcardDelegate(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data);

        public delegate bool OnIsTrustedIdentityDelegate(string recipientId, IdentityKey identityKey);

        public delegate PreKeyRecord OnLoadPreKeyDelegate(uint preKeyId);

        public delegate List<PreKeyRecord> OnLoadPreKeysDelegate();

        public delegate SenderKeyRecord OnLoadSenderKeyDelegate(SenderKeyName senderKeyName);

        public delegate List<SenderKeyRecord> OnLoadSenderKeysDelegate();

        public delegate SessionRecord OnLoadSessionDelegate(AxolotlAddress address);

        public delegate SignedPreKeyRecord OnLoadSignedPreKeyDelegate(uint preKeyId);

        public delegate List<SignedPreKeyRecord> OnLoadSignedPreKeysDelegate();

        public delegate void OnNotificationPictureDelegate(string type, string jid, string id);

        public delegate void OnRemoveAllPreKeysDelegate();

        public delegate void OnRemovePreKeyDelegate(uint preKeyId);

        public delegate void OnRemoveSenderKeyDelegate(SenderKeyName senderKeyName);

        public delegate void OnRemoveSignedPreKeyDelegate(uint preKeyId);

        public delegate bool OnSaveIdentityDelegate(string recipientId, IdentityKey identityKey);

        public delegate void OnStoreLocalDataDelegate(uint registrationId, IdentityKeyPair identityKeyPair);

        public delegate void OnStorePreKeyDelegate(uint prekeyId, PreKeyRecord preKeyRecord);

        public delegate void OnStoreSenderKeyDelegate(SenderKeyName senderKeyName, SenderKeyRecord record);

        public delegate void OnStoreSessionDataDelegate(AxolotlAddress address, SessionRecord record);

        public delegate void OnStoreSignedPreKeyDelegate(uint signedPreKeyId, SignedPreKeyRecord signedPreKeyRecord);

        public delegate void StringDelegate(string data);

        public event ExceptionDelegate OnConnectFailed;

        public event NullDelegate OnConnectSuccess;

        public event OnContainsPreKeyDelegate OnContainsPreKey;

        public event OnContainsSenderKeyDelegate OnContainsSenderKey;

        public event OnContainsSessionDelegate OnContainsSession;

        public event OnContainsSignedPreKeyDelegate OnContainsSignedPreKey;

        public event OnDeleteAllSessionsDelegate OnDeleteAllSessions;

        public event OnDeleteSessionDelegate OnDeleteSession;

        public event ExceptionDelegate OnDisconnect;

        public event OnErrorDelegate OnError;

        public event OnErrorAxolotlDelegate OnErrorAxolotl;

        public event OnContactNameDelegate OnGetContactName;

        public event OnGetBroadcastListsDelegate OnGetBroadcastLists;

        public event OnGetGroupParticipantsDelegate OnGetGroupParticipants;

        public event OnGetGroupsDelegate OnGetGroups;

        public event OnGetGroupSubjectDelegate OnGetGroupSubject;

        public event OnGetIdentityKeyPairDelegate OnGetIdentityKeyPair;

        public event OnGetLastSeenDelegate OnGetLastSeen;

        public event OnGetLocalRegistrationIdDelegate OnGetLocalRegistrationId;

        public event OnGetMessageDelegate OnGetMessage;

        public event OnGetMediaDelegate OnGetMessageAudio;

        public event OnGetMediaDelegate OnGetMessageImage;

        public event OnGetLocationDelegate OnGetMessageLocation;

        public event OnGetMessageReceivedDelegate OnGetMessageReadedClient;

        public event OnGetMessageReceivedDelegate OnGetMessageReceivedClient;

        public event OnGetMessageReceivedDelegate OnGetMessageReceivedServer;

        public event OnGetVcardDelegate OnGetMessageVcard;

        public event OnGetMediaDelegate OnGetMessageVideo;

        public event OnGetParticipantAddedDelegate OnGetParticipantAdded;

        public event OnGetParticipantRemovedDelegate OnGetParticipantRemoved;

        public event OnGetParticipantRenamedDelegate OnGetParticipantRenamed;

        public event OnGetChatStateDelegate OnGetPaused;

        public event OnGetPictureDelegate OnGetPhoto;

        public event OnGetPictureDelegate OnGetPhotoPreview;

        public event OnGetPresenceDelegate OnGetPresence;

        public event OnGetPrivacySettingsDelegate OnGetPrivacySettings;

        public event OnGetStatusDelegate OnGetStatus;

        public event OnGetSubDeviceSessionsDelegate OnGetSubDeviceSessions;

        public event OnGetSyncResultDelegate OnGetSyncResult;

        public event OnGetChatStateDelegate OnGetTyping;

        public event OnIsTrustedIdentityDelegate OnIsTrustedIdentity;

        public event OnLoadPreKeyDelegate OnLoadPreKey;

        public event OnLoadPreKeysDelegate OnLoadPreKeys;

        public event OnLoadSenderKeyDelegate OnLoadSenderKey;

        public event OnLoadSenderKeysDelegate OnLoadSenderKeys;

        public event OnLoadSessionDelegate OnLoadSession;

        public event OnLoadSignedPreKeyDelegate OnLoadSignedPreKey;

        public event OnLoadSignedPreKeysDelegate OnLoadSignedPreKeys;

        public event StringDelegate OnLoginFailed;

        public event LoginSuccessDelegate OnLoginSuccess;

        public event OnNotificationPictureDelegate OnNotificationPicture;

        public event OnRemoveAllPreKeysDelegate OnRemoveAllPreKeys;

        public event OnRemovePreKeyDelegate OnRemovePreKey;

        public event OnRemoveSenderKeyDelegate OnRemoveSenderKey;

        public event OnRemoveSignedPreKeyDelegate OnRemoveSignedPreKey;

        public event OnSaveIdentityDelegate OnSaveIdentity;

        public event OnStoreLocalDataDelegate OnStoreLocalData;

        public event OnStorePreKeyDelegate OnStorePreKey;

        public event OnStoreSenderKeyDelegate OnStoreSenderKey;

        public event OnStoreSessionDataDelegate OnStoreSession;

        public event OnStoreSignedPreKeyDelegate OnStoreSignedPreKey;

        public bool ContainsPreKey(uint preKeyId)
        {
            if (this.OnContainsPreKey != null)
            {
                return this.OnContainsPreKey(preKeyId);
            }
            return false;
        }

        public bool ContainsSenderKey(SenderKeyName senderKeyName)
        {
            if (this.OnContainsSenderKey != null)
            {
                return this.OnContainsSenderKey(senderKeyName);
            }

            return false;
        }

        public bool ContainsSession(AxolotlAddress address)
        {
            if (this.OnContainsSession != null)
            {
                return this.OnContainsSession(address);
            }
            return false;
        }

        public bool ContainsSignedPreKey(uint signedPreKeyId)
        {
            if (this.OnContainsSignedPreKey != null)
            {
                return this.OnContainsSignedPreKey(signedPreKeyId);
            }
            return false;
        }

        public void DeleteAllSessions(string name)
        {
            if (this.OnDeleteAllSessions != null)
            {
                this.OnDeleteAllSessions(name);
            }
        }

        public void DeleteSession(AxolotlAddress address)
        {
            if (this.OnDeleteSession != null)
            {
                this.OnDeleteSession(address);
            }
        }

        public void ErrorAxolotl(String ErrorMessage)
        {
            if (this.OnErrorAxolotl != null)
            {
                this.OnErrorAxolotl(ErrorMessage);
            }
        }

        public IdentityKeyPair GetIdentityKeyPair()
        {
            if (this.OnGetIdentityKeyPair != null)
            {
                return this.OnGetIdentityKeyPair();
            }
            return null;
        }

        public uint GetLocalRegistrationId()
        {
            if (this.OnGetLocalRegistrationId != null)
            {
                return this.OnGetLocalRegistrationId();
            }
            return 0;
        }

        public List<uint> GetSubDeviceSessions(string recipientId)
        {
            if (this.OnGetSubDeviceSessions != null)
            {
                return this.OnGetSubDeviceSessions(recipientId);
            }
            return null;
        }

        public bool IsTrustedIdentity(string name, IdentityKey identityKey)
        {
            if (this.OnIsTrustedIdentity != null)
            {
                return this.OnIsTrustedIdentity(name, identityKey);
            }
            return false;
        }

        public PreKeyRecord LoadPreKey(uint preKeyId)
        {
            if (this.OnLoadPreKey != null)
            {
                return this.OnLoadPreKey(preKeyId);
            }
            return null;
        }

        public List<PreKeyRecord> LoadPreKeys()
        {
            if (this.OnLoadPreKeys != null)
            {
                return this.OnLoadPreKeys();
            }
            return null;
        }

        public SenderKeyRecord LoadSenderKey(SenderKeyName senderKeyName)
        {
            if (this.OnLoadSenderKey != null)
            {
                return this.OnLoadSenderKey(senderKeyName);
            }
            return null;
        }

        public List<SenderKeyRecord> LoadSenderKeys()
        {
            if (this.OnLoadSenderKeys != null)
            {
                return this.OnLoadSenderKeys();
            }
            return null;
        }

        public SessionRecord LoadSession(AxolotlAddress address)
        {
            if (this.OnLoadSession != null)
            {
                return this.OnLoadSession(address);
            }
            return null;
        }

        public SignedPreKeyRecord LoadSignedPreKey(uint signedPreKeyId)
        {
            if (this.OnLoadSignedPreKey != null)
            {
                return this.OnLoadSignedPreKey(signedPreKeyId);
            }
            return null;
        }

        public List<SignedPreKeyRecord> LoadSignedPreKeys()
        {
            if (this.OnLoadSignedPreKeys != null)
            {
                List<SignedPreKeyRecord> inputList = this.OnLoadSignedPreKeys();
                return inputList;
            }
            return null;
        }

        public void RemoveAllPreKeys()
        {
            if (this.OnRemoveAllPreKeys != null)
            {
                this.OnRemoveAllPreKeys();
            }
        }

        public void RemovePreKey(uint preKeyId)
        {
            if (this.OnRemovePreKey != null)
            {
                this.OnRemovePreKey(preKeyId);
            }
        }

        public void RemoveSenderKey(SenderKeyName senderKeyName)
        {
            if (this.OnRemoveSenderKey != null)
            {
                this.OnRemoveSenderKey(senderKeyName);
            }
        }

        public void RemoveSignedPreKey(uint signedPreKeyId)
        {
            if (this.OnRemoveSignedPreKey != null)
            {
                this.OnRemoveSignedPreKey(signedPreKeyId);
            }
        }

        public bool SaveIdentity(string name, IdentityKey identityKey)
        {
            if (this.OnSaveIdentity != null)
            {
                return this.OnSaveIdentity(name, identityKey);
            }
            return false;
        }

        public void StoreLocalData(uint registrationId, IdentityKeyPair identityKeyPair)
        {
            if (this.OnStoreLocalData != null)
            {
                this.OnStoreLocalData(registrationId, identityKeyPair);
            }
        }

        public void StorePreKey(uint preKeyId, PreKeyRecord record)
        {
            if (this.OnStorePreKey != null)
            {
                this.OnStorePreKey(preKeyId, record);
            }
        }

        public void StorePreKeys(IList<PreKeyRecord> keys)
        {
            foreach (PreKeyRecord key in keys)
                StorePreKey(key.GetId(), key);
        }

        public void StoreSenderKey(SenderKeyName senderKeyName, SenderKeyRecord record)
        {
            if (this.OnStoreSenderKey != null)
            {
                this.OnStoreSenderKey(senderKeyName, record);
            }
        }

        public void StoreSession(AxolotlAddress address, SessionRecord record)
        {
            if (this.OnStoreSession != null)
            {
                this.OnStoreSession(address, record);
            }
        }

        public void StoreSignedPreKey(uint signedPreKeyId, SignedPreKeyRecord record)
        {
            if (this.OnStoreSignedPreKey != null)
            {
                this.OnStoreSignedPreKey(signedPreKeyId, record);
            }
        }

        protected void FireOnConnectFailed(Exception ex)
        {
            if (this.OnConnectFailed != null)
            {
                this.OnConnectFailed(ex);
            }
        }

        protected void FireOnConnectSuccess()
        {
            if (this.OnConnectSuccess != null)
            {
                this.OnConnectSuccess();
            }
        }

        protected void FireOnDisconnect(Exception ex)
        {
            if (this.OnDisconnect != null)
            {
                this.OnDisconnect(ex);
            }
        }
        protected void FireOnError(string id, string from, int code, string text)
        {
            if (this.OnError != null)
            {
                this.OnError(id, from, code, text);
            }
        }

        protected void FireOnGetBroadcastLists(string phoneNumber, List<WaBroadcast> broadcastLists)
        {
            if (this.OnGetBroadcastLists != null)
            {
                this.OnGetBroadcastLists(phoneNumber, broadcastLists);
            }
        }

        protected void FireOnGetContactName(string from, string contactName)
        {
            if (this.OnGetContactName != null)
            {
                this.OnGetContactName(from, contactName);
            }
        }

        protected void FireOnGetGroupParticipants(string gjid, string[] jids)
        {
            if (this.OnGetGroupParticipants != null)
            {
                this.OnGetGroupParticipants(gjid, jids);
            }
        }

        protected void FireOnGetGroups(WaGroupInfo[] groups)
        {
            if (this.OnGetGroups != null)
            {
                this.OnGetGroups(groups);
            }
        }

        protected void FireOnGetGroupSubject(string gjid, string jid, string username, string subject, DateTime time)
        {
            if (this.OnGetGroupSubject != null)
            {
                this.OnGetGroupSubject(gjid, jid, username, subject, time);
            }
        }

        protected void FireOnGetLastSeen(string from, DateTime lastSeen)
        {
            if (this.OnGetLastSeen != null)
            {
                this.OnGetLastSeen(from, lastSeen);
            }
        }

        protected void FireOnGetMessage(ProtocolTreeNode messageNode, string from, string id, string name, string message, bool receipt_sent)
        {
            if (this.OnGetMessage != null)
            {
                this.OnGetMessage(messageNode, from, id, name, message, receipt_sent);
            }
        }

        protected void FireOnGetMessageAudio(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageAudio != null)
            {
                this.OnGetMessageAudio(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        protected void FireOnGetMessageImage(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageImage != null)
            {
                this.OnGetMessageImage(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        protected void FireOnGetMessageLocation(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string User)
        {
            if (this.OnGetMessageLocation != null)
            {
                this.OnGetMessageLocation(locationNode, from, id, lon, lat, url, name, preview, User);
            }
        }

        protected void FireOnGetMessageReadedClient(string from, string id)
        {
            if (this.OnGetMessageReadedClient != null)
            {
                this.OnGetMessageReadedClient(from, id);
            }
        }

        protected void FireOnGetMessageReceivedClient(string from, string id)
        {
            if (this.OnGetMessageReceivedClient != null)
            {
                this.OnGetMessageReceivedClient(from, id);
            }
        }

        protected void FireOnGetMessageReceivedServer(string from, string id)
        {
            if (this.OnGetMessageReceivedServer != null)
            {
                this.OnGetMessageReceivedServer(from, id);
            }
        }

        protected void FireOnGetMessageVcard(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data)
        {
            if (this.OnGetMessageVcard != null)
            {
                this.OnGetMessageVcard(vcardNode, from, id, name, data);
            }
        }

        protected void FireOnGetMessageVideo(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageVideo != null)
            {
                this.OnGetMessageVideo(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        protected void FireOnGetParticipantAdded(string gjid, string jid, DateTime time)
        {
            if (this.OnGetParticipantAdded != null)
            {
                this.OnGetParticipantAdded(gjid, jid, time);
            }
        }

        protected void FireOnGetParticipantRemoved(string gjid, string jid, string author, DateTime time)
        {
            if (this.OnGetParticipantRemoved != null)
            {
                this.OnGetParticipantRemoved(gjid, jid, author, time);
            }
        }

        protected void FireOnGetParticipantRenamed(string gjid, string oldJid, string newJid, DateTime time)
        {
            if (this.OnGetParticipantRenamed != null)
            {
                this.OnGetParticipantRenamed(gjid, oldJid, newJid, time);
            }
        }

        protected void FireOnGetPaused(string from)
        {
            if (this.OnGetPaused != null)
            {
                this.OnGetPaused(from);
            }
        }

        protected void FireOnGetPhoto(string from, string id, byte[] data)
        {
            if (this.OnGetPhoto != null)
            {
                this.OnGetPhoto(from, id, data);
            }
        }

        protected void FireOnGetPhotoPreview(string from, string id, byte[] data)
        {
            if (this.OnGetPhotoPreview != null)
            {
                this.OnGetPhotoPreview(from, id, data);
            }
        }

        protected void FireOnGetPresence(string from, string type)
        {
            if (this.OnGetPresence != null)
            {
                this.OnGetPresence(from, type);
            }
        }

        protected void FireOnGetPrivacySettings(Dictionary<VisibilityCategory, VisibilitySetting> settings)
        {
            if (this.OnGetPrivacySettings != null)
            {
                this.OnGetPrivacySettings(settings);
            }
        }

        protected void FireOnGetStatus(string from, string type, string name, string status)
        {
            if (this.OnGetStatus != null)
            {
                this.OnGetStatus(from, type, name, status);
            }
        }

        protected void FireOnGetSyncResult(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers)
        {
            if (this.OnGetSyncResult != null)
            {
                this.OnGetSyncResult(index, sid, existingUsers, failedNumbers);
            }
        }

        protected void FireOnGetTyping(string from)
        {
            if (this.OnGetTyping != null)
            {
                this.OnGetTyping(from);
            }
        }

        protected void FireOnLoginFailed(string data)
        {
            if (this.OnLoginFailed != null)
            {
                this.OnLoginFailed(data);
            }
        }

        protected void FireOnLoginSuccess(string pn, byte[] data)
        {
            if (this.OnLoginSuccess != null)
            {
                this.OnLoginSuccess(pn, data);
            }
        }
        protected void FireOnNotificationPicture(string type, string jid, string id)
        {
            if (this.OnNotificationPicture != null)
            {
                this.OnNotificationPicture(type, jid, id);
            }
        }
    }
}