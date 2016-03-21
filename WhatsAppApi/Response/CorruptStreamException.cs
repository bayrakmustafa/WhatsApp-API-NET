using System;

namespace WhatsAppApi.Response
{
    internal class CorruptStreamException : Exception
    {
        public string EMessage
        {
            get; private set;
        }

        public CorruptStreamException(string pMessage)
        {
            // TODO: Complete member initialization
            this.EMessage = pMessage;
        }
    }
}