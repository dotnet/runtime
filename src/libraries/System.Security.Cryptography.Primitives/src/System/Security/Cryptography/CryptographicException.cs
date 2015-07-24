// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.Security.Cryptography
{
    public class CryptographicException : Exception
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
    }
}
