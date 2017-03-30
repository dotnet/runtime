// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security.Cryptography
{
    [Serializable]
    public class CryptographicUnexpectedOperationException : CryptographicException
    {
        public CryptographicUnexpectedOperationException()
            : base(SR.Arg_CryptographyException)
        {
        }

        public CryptographicUnexpectedOperationException(string message)
            : base(message)
        {
        }

        public CryptographicUnexpectedOperationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public CryptographicUnexpectedOperationException(string format, string insert)
            : base(string.Format(CultureInfo.CurrentCulture, format, insert))
        {
        }

        protected CryptographicUnexpectedOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
