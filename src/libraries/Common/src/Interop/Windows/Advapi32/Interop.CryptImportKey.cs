// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe bool CryptImportKey(
            SafeProvHandle hProv,
            byte* pbData,
            int dwDataLen,
            SafeKeyHandle hPubKey,
            int dwFlags,
            out SafeKeyHandle phKey);
    }
}
