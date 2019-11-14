// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, SetLastError = true)]
        public static extern bool CryptSetKeyParam(SafeKeyHandle hKey, int dwParam, byte[] pbData, int dwFlags);

        [DllImport(Libraries.Advapi32, SetLastError = true)]
        public static extern bool CryptSetKeyParam(SafeKeyHandle safeKeyHandle, int dwParam, ref int pdw, int dwFlags);
    }
}
