// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
        }

        public CryptographicException(String message)
            : base(message)
        {
        }

        public CryptographicException(String message, Exception inner)
            : base(message, inner)
        {
        }

        public CryptographicException(String format, String insert)
            : base(String.Format(CultureInfo.CurrentCulture, format, insert))
        {
        }
    }
}
