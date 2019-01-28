using System;

namespace ZeroReact.Utils
{
    public class ReactException : Exception
    {
        public ReactException(string message) : base(message)
        {
            
        }

        public ReactException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
