// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Data
{
    /// <summary>
    /// The exception that is throwing from strong typed DataSet when user access to DBNull value.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class StrongTypingException : DataException
    {
        protected StrongTypingException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        public StrongTypingException() : base()
        {
            HResult = HResults.StrongTyping;
        }

        public StrongTypingException(string message) : base(message)
        {
            HResult = HResults.StrongTyping;
        }

        public StrongTypingException(string s, Exception innerException) : base(s, innerException)
        {
            HResult = HResults.StrongTyping;
        }
    }
}
