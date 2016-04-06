using System;
using System.ComponentModel;
using WhatsAppApi.Parser;

namespace WhatsAppApi.Response
{
    /// <summary>
    /// Handles events
    /// </summary>
    public static class WhatsEventHandler
    {
        #region Delegates

        /// <summary>
        /// Event occures when the message has been recieved
        /// </summary>
        /// <param name="mess">The message that has been recieved</param>
        public delegate void MessageRecievedHandler(FMessage mess);

        /// <summary>
        /// Handles string arrays
        /// </summary>
        /// <param name="value">A string array</param>
        public delegate void StringArrayHandler(string[] value);

        /// <summary>
        /// Handles boolean valies
        /// </summary>
        /// <param name="from">Sender</param>
        /// <param name="value">Boolean value</param>
        public delegate void BoolHandler(string from, bool value);

        /// <summary>
        /// Event occures when somebody changes his profile picture
        /// </summary>
        /// <param name="from">The sender</param>
        /// <param name="uJid">The user id that changed his profile picture</param>
        /// <param name="photoId">The id of the new photo</param>
        public delegate void PhotoChangedHandler(string from, string uJid, string photoId);

        /// <summary>
        /// Event occurs when the group subject has changed
        /// </summary>
        /// <param name="from">The changer</param>
        /// <param name="uJid">The uid of the changer</param>
        /// <param name="subject">The new subject</param>
        /// <param name="t">?</param>
        public delegate void GroupNewSubjectHandler(string from, string uJid, string subject, int t);

        #endregion Delegates

        #region Events

        /// <summary>
        /// Event occures when the message has been recieved
        /// </summary>
        public static event MessageRecievedHandler MessageRecievedEvent;

        /// <summary>
        /// Handles boolean valies
        /// </summary>
        public static event BoolHandler IsTypingEvent;

        /// <summary>
        /// Event occurs when the group subject has changed
        /// </summary>
        public static event GroupNewSubjectHandler GroupNewSubjectEvent;

        /// <summary>
        /// Event occures when somebody changes his profile picture
        /// </summary>
        public static event PhotoChangedHandler PhotoChangedEvent;

        #endregion Events

        #region OnMethods

        /*
         * No need to add documentation here
         * User will only handle the delagates and events
         * */

        public static void OnMessageRecievedEventHandler(FMessage mess)
        {
            MessageRecievedHandler h = MessageRecievedEvent;
            if (h == null)
                return;
            foreach (Delegate tmpSingleCast in h.GetInvocationList())
            {
                ISynchronizeInvoke tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.Invoke(tmpSingleCast, new object[] { mess });
                    continue;
                }
                h.Invoke(mess);
            }
        }

        public static void OnPhotoChangedEventHandler(FMessage mess)
        {
            MessageRecievedHandler h = MessageRecievedEvent;
            if (h == null)
                return;
            foreach (Delegate tmpSingleCast in h.GetInvocationList())
            {
                ISynchronizeInvoke tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.Invoke(tmpSingleCast, new object[] { mess });
                    continue;
                }
                h.Invoke(mess);
            }
        }

        public static void OnIsTypingEventHandler(string from, bool isTyping)
        {
            BoolHandler h = IsTypingEvent;
            if (h == null)
                return;
            foreach (Delegate tmpSingleCast in h.GetInvocationList())
            {
                ISynchronizeInvoke tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.Invoke(tmpSingleCast, new object[] { from, isTyping });
                    continue;
                }
                h.Invoke(from, isTyping);
            }
        }

        public static void OnGroupNewSubjectEventHandler(string from, string uJid, string subject, int t)
        {
            GroupNewSubjectHandler h = GroupNewSubjectEvent;
            if (h == null)
                return;
            foreach (Delegate tmpSingleCast in h.GetInvocationList())
            {
                ISynchronizeInvoke tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.Invoke(tmpSingleCast, new object[] { from, uJid, subject, t });
                    continue;
                }
                h.Invoke(from, uJid, subject, t);
            }
        }

        public static void OnPhotoChangedEventHandler(string from, string uJid, string photoId)
        {
            PhotoChangedHandler h = PhotoChangedEvent;
            if (h == null)
                return;
            foreach (Delegate tmpSingleCast in h.GetInvocationList())
            {
                ISynchronizeInvoke tmpSyncInvoke = tmpSingleCast.Target as ISynchronizeInvoke;
                if (tmpSyncInvoke != null && tmpSyncInvoke.InvokeRequired)
                {
                    tmpSyncInvoke.Invoke(tmpSingleCast, new object[] { from, uJid, photoId });
                    continue;
                }
                h.Invoke(from, uJid, photoId);
            }
        }

        #endregion OnMethods
    }
}