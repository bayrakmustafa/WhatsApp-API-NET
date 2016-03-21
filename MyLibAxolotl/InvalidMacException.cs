using System;

namespace TextSecure.libaxolotl
{
    internal class InvalidMacException : Exception
    {
        public InvalidMacException(String detailMessage)
            : base(detailMessage)
        {
        }

        public InvalidMacException(Exception exception)
            : base(exception.Message)
        {
        }
    }
}