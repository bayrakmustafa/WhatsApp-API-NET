using System;

namespace WhatsAppApi.Helper
{
    internal class ConnectionException : Exception
    {
        public ConnectionException()
            : base()
        {
        }

        public ConnectionException(string message)
            : base(message)
        {
        }

        public ConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}