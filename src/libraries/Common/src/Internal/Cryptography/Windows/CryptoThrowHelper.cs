// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class CryptoThrowHelper
    {
        public static CryptographicException ToCryptographicException(this int hr)
        {
            string message = Interop.mincore.GetMessage(hr);
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
