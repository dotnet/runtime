// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class CryptoThrowHelper
    {
        public static CryptographicException ToCryptographicException(this int hr)
        {
            string message = Interop.Kernel32.GetMessage(hr);

            if ((hr & 0x80000000) != 0x80000000)
                hr = (hr & 0x0000FFFF) | unchecked((int)0x80070000);

            return new WindowsCryptographicException(hr, message);
        }

        private sealed class WindowsCryptographicException : CryptographicException
        {
            public WindowsCryptographicException(int hr, string message)
                : base(message)
            {
                HResult = hr;
            }
        }
    }
}
