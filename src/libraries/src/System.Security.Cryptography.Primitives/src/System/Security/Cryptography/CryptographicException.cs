// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security.Cryptography
{
    [Serializable]
    public class CryptographicException : SystemException
    {
        public CryptographicException()
            : base(SR.Arg_CryptographyException)
        {
        }

        public CryptographicException(int hr)
            : base(SR.Arg_CryptographyException)
        {
            HResult = hr;
        }

        public CryptographicException(string message)
            : base(message)
        {
        }

        public CryptographicException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public CryptographicException(string format, string insert)
            : base(string.Format(CultureInfo.CurrentCulture, format, insert))
        {
        }

        protected CryptographicException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
