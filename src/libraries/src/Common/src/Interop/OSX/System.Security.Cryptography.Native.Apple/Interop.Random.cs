// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal static unsafe void GetRandomBytes(byte* pbBuffer, int count)
        {
            Debug.Assert(pbBuffer != null);
            Debug.Assert(count >= 0);

            int errorCode;
            int ret = AppleCryptoNative_GetRandomBytes(pbBuffer, count, out errorCode);

            if (ret == 0)
            {
                throw CreateExceptionForCCError(errorCode, CCRNGStatus);
            }

            if (ret != 1)
            {
                throw new CryptographicException();
            }
        }

        [DllImport(Libraries.AppleCryptoNative)]
        private static unsafe extern int AppleCryptoNative_GetRandomBytes(byte* buf, int num, out int errorCode);
    }
}
