using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatsAppApi.Helper;
using WhatsAppApi.Response;

namespace WhatsAppApi
{
    public class WhatsEventBase : ApiBase
    {
        //events
        public event ExceptionDelegate OnDisconnect;
        protected void FireOnDisconnect(Exception ex)
        {
            if (this.OnDisconnect != null)
            {
                this.OnDisconnect(ex);
            }
        }

        public event NullDelegate OnConnectSuccess;
        protected void FireOnConnectSuccess()
        {
            if (this.OnConnectSuccess != null)
            {
                this.OnConnectSuccess();
            }
        }

        public event ExceptionDelegate OnConnectFailed;
        protected void FireOnConnectFailed(Exception ex)
        {
            if (this.OnConnectFailed != null)
            {
                this.OnConnectFailed(ex);
            }
        }

        public event LoginSuccessDelegate OnLoginSuccess;
        protected void FireOnLoginSuccess(string pn, byte[] data)
        {
            if (this.OnLoginSuccess != null)
            {
                this.OnLoginSuccess(pn, data);
            }
        }

        public event StringDelegate OnLoginFailed;
        protected void FireOnLoginFailed(string data)
        {
            if (this.OnLoginFailed != null)
            {
                this.OnLoginFailed(data);
            }
        }

        public event OnGetMessageDelegate OnGetMessage;
        protected void FireOnGetMessage(ProtocolTreeNode messageNode, string from, string id, string name, string message, bool receipt_sent)
        {
            if (this.OnGetMessage != null)
            {
                this.OnGetMessage(messageNode, from, id, name, message, receipt_sent);
            }
        }

        public event OnGetMediaDelegate OnGetMessageImage;
        protected void FireOnGetMessageImage(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageImage != null)
            {
                this.OnGetMessageImage(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        public event OnGetMediaDelegate OnGetMessageVideo;
        protected void FireOnGetMessageVideo(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageVideo != null)
            {
                this.OnGetMessageVideo(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        public event OnGetMediaDelegate OnGetMessageAudio;
        protected void FireOnGetMessageAudio(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name)
        {
            if (this.OnGetMessageAudio != null)
            {
                this.OnGetMessageAudio(mediaNode, from, id, fileName, fileSize, url, preview, name);
            }
        }

        public event OnGetLocationDelegate OnGetMessageLocation;
        protected void FireOnGetMessageLocation(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string User)
        {
            if (this.OnGetMessageLocation != null)
            {
                this.OnGetMessageLocation(locationNode, from, id, lon, lat, url, name, preview, User);
            }
        }

        public event OnGetVcardDelegate OnGetMessageVcard;
        protected void FireOnGetMessageVcard(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data)
        {
            if (this.OnGetMessageVcard != null)
            {
                this.OnGetMessageVcard(vcardNode, from, id, name, data);
            }
        }

        public event OnErrorDelegate OnError;
        protected void FireOnError(string id, string from, int code, string text)
        {
            if (this.OnError != null)
            {
                this.OnError(id, from, code, text);
            }
        }

        public event OnNotificationPictureDelegate OnNotificationPicture;
        protected void FireOnNotificationPicture(string type, string jid, string id)
        {
            if (this.OnNotificationPicture != null)
            {
                this.OnNotificationPicture(type, jid, id);
            }
        }

        public event OnGetMessageReceivedDelegate OnGetMessageReceivedServer;
        protected void FireOnGetMessageReceivedServer(string from, string id)
        {
            if (this.OnGetMessageReceivedServer != null)
            {
                this.OnGetMessageReceivedServer(from, id);
            }
        }

        public event OnGetMessageReceivedDelegate OnGetMessageReceivedClient;
        protected void FireOnGetMessageReceivedClient(string from, string id)
        {
            if (this.OnGetMessageReceivedClient != null)
            {
                this.OnGetMessageReceivedClient(from, id);
            }
        }

        public event OnGetMessageReceivedDelegate OnGetMessageReadedClient;
        protected void FireOnGetMessageReadedClient(string from, string id)
        {
            if (this.OnGetMessageReadedClient != null)
            {
                this.OnGetMessageReadedClient(from, id);
            }
        }

        public event OnGetPresenceDelegate OnGetPresence;
        protected void FireOnGetPresence(string from, string type)
        {
            if (this.OnGetPresence != null)
            {
                this.OnGetPresence(from, type);
            }
        }

        public event OnGetGroupParticipantsDelegate OnGetGroupParticipants;
        protected void FireOnGetGroupParticipants(string gjid, string[] jids)
        {
            if (this.OnGetGroupParticipants != null)
            {
                this.OnGetGroupParticipants(gjid, jids);
            }
        }

        public event OnGetLastSeenDelegate OnGetLastSeen;
        protected void FireOnGetLastSeen(string from, DateTime lastSeen)
        {
            if (this.OnGetLastSeen != null)
            {
                this.OnGetLastSeen(from, lastSeen);
            }
        }

        public event OnGetChatStateDelegate OnGetTyping;
        protected void FireOnGetTyping(string from)
        {
            if (this.OnGetTyping != null)
            {
                this.OnGetTyping(from);
            }
        }

        public event OnGetChatStateDelegate OnGetPaused;
        protected void FireOnGetPaused(string from)
        {
            if (this.OnGetPaused != null)
            {
                this.OnGetPaused(from);
            }
        }

        public event OnGetPictureDelegate OnGetPhoto;
        protected void FireOnGetPhoto(string from, string id, byte[] data)
        {
            if (this.OnGetPhoto != null)
            {
                this.OnGetPhoto(from, id, data);
            }
        }

        public event OnGetPictureDelegate OnGetPhotoPreview;
        protected void FireOnGetPhotoPreview(string from, string id, byte[] data)
        {
            if (this.OnGetPhotoPreview != null)
            {
                this.OnGetPhotoPreview(from, id, data);
            }
        }

        public event OnGetGroupsDelegate OnGetGroups;
        protected void FireOnGetGroups(WaGroupInfo[] groups)
        {
            if (this.OnGetGroups != null)
            {
                this.OnGetGroups(groups);
            }
        }

        public event OnContactNameDelegate OnGetContactName;
        protected void FireOnGetContactName(string from, string contactName)
        {
            if (this.OnGetContactName != null)
            {
                this.OnGetContactName(from, contactName);
            }
        }

        public event OnGetStatusDelegate OnGetStatus;
        protected void FireOnGetStatus(string from, string type, string name, string status)
        {
            if (this.OnGetStatus != null)
            {
                this.OnGetStatus(from, type, name, status);
            }
        }

        public event OnGetSyncResultDelegate OnGetSyncResult;
        protected void FireOnGetSyncResult(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers)
        {
            if (this.OnGetSyncResult != null)
            {
                this.OnGetSyncResult(index, sid, existingUsers, failedNumbers);
            }
        }

        public event OnGetPrivacySettingsDelegate OnGetPrivacySettings;
        protected void FireOnGetPrivacySettings(Dictionary<VisibilityCategory, VisibilitySetting> settings)
        {
            if (this.OnGetPrivacySettings != null)
            {
                this.OnGetPrivacySettings(settings);
            }
        }

        public event OnGetParticipantAddedDelegate OnGetParticipantAdded;
        protected void FireOnGetParticipantAdded(string gjid, string jid, DateTime time)
        {
            if (this.OnGetParticipantAdded != null)
            {
                this.OnGetParticipantAdded(gjid, jid, time);
            }
        }

        public event OnGetParticipantRemovedDelegate OnGetParticipantRemoved;
        protected void FireOnGetParticipantRemoved(string gjid, string jid, string author, DateTime time)
        {
            if (this.OnGetParticipantRemoved != null)
            {
                this.OnGetParticipantRemoved(gjid, jid, author, time);
            }
        }

        public event OnGetParticipantRenamedDelegate OnGetParticipantRenamed;
        protected void FireOnGetParticipantRenamed(string gjid, string oldJid, string newJid, DateTime time)
        {
            if (this.OnGetParticipantRenamed != null)
            {
                this.OnGetParticipantRenamed(gjid, oldJid, newJid, time);
            }
        }

        public event OnGetGroupSubjectDelegate OnGetGroupSubject;
        protected void FireOnGetGroupSubject(string gjid, string jid, string username, string subject, DateTime time)
        {
            if (this.OnGetGroupSubject != null)
            {
                this.OnGetGroupSubject(gjid, jid, username, subject, time);
            }
        }

        //Event Delegates
        public delegate void OnContactNameDelegate(string from, string contactName);
        public delegate void NullDelegate();
        public delegate void ExceptionDelegate(Exception ex);
        public delegate void LoginSuccessDelegate(string phoneNumber, byte[] data);
        public delegate void StringDelegate(string data);
        public delegate void OnErrorDelegate(string id, string from, int code, string text);
        public delegate void OnGetMessageReceivedDelegate(string from, string id);
        public delegate void OnNotificationPictureDelegate(string type, string jid, string id);
        public delegate void OnGetMessageDelegate(ProtocolTreeNode messageNode, string from, string id, string name, string message, bool receipt_sent);
        public delegate void OnGetPresenceDelegate(string from, string type);
        public delegate void OnGetGroupParticipantsDelegate(string gjid, string[] jids);
        public delegate void OnGetLastSeenDelegate(string from, DateTime lastSeen);
        public delegate void OnGetChatStateDelegate(string from);
        public delegate void OnGetMediaDelegate(ProtocolTreeNode mediaNode, string from, string id, string fileName, int fileSize, string url, byte[] preview, string name);
        public delegate void OnGetLocationDelegate(ProtocolTreeNode locationNode, string from, string id, double lon, double lat, string url, string name, byte[] preview, string UserName);
        public delegate void OnGetVcardDelegate(ProtocolTreeNode vcardNode, string from, string id, string name, byte[] data);
        public delegate void OnGetPictureDelegate(string from, string id, byte[] data);
        public delegate void OnGetGroupsDelegate(WaGroupInfo[] groups);
        public delegate void OnGetStatusDelegate(string from, string type, string name, string status);
        public delegate void OnGetSyncResultDelegate(int index, string sid, Dictionary<string, string> existingUsers, string[] failedNumbers);
        public delegate void OnGetPrivacySettingsDelegate(Dictionary<VisibilityCategory, VisibilitySetting> settings);
        public delegate void OnGetParticipantAddedDelegate(string gjid, string jid, DateTime time);
        public delegate void OnGetParticipantRemovedDelegate(string gjid, string jid, string author, DateTime time);
        public delegate void OnGetParticipantRenamedDelegate(string gjid, string oldJid, string newJid, DateTime time);
        public delegate void OnGetGroupSubjectDelegate(string gjid, string jid, string username, string subject, DateTime time);
    }
}
