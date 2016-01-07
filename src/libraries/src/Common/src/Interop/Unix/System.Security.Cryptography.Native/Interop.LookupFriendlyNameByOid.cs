// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_LookupFriendlyNameByOid")]
        internal static extern int LookupFriendlyNameByOid(string oidValue, ref IntPtr friendlyNamePtr);
    }
}
