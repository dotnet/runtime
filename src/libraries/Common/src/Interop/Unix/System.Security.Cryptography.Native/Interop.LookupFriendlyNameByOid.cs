// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_LookupFriendlyNameByOid", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int LookupFriendlyNameByOid(string oidValue, ref IntPtr friendlyNamePtr);
    }
}
