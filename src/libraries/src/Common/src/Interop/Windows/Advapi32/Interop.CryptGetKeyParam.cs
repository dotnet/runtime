// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal enum CryptGetKeyParamFlags : int
        {
            CRYPT_EXPORT = 0x0004,
            KP_IV = 1,
            KP_PERMISSIONS = 6,
            KP_ALGID = 7,
            KP_KEYLEN = 9
        }

        [DllImport(Libraries.Advapi32, SetLastError = true)]
        public static extern bool CryptGetKeyParam(
            SafeKeyHandle hKey,
            CryptGetKeyParamFlags dwParam,
            byte[] pbData,
            ref int pdwDataLen,
            int dwFlags);
    }
}
