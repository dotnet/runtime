// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal const int NID_undef = 0;

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjSn2Nid", CharSet = CharSet.Ansi)]
        internal static extern int ObjSn2Nid(string sn);
    }
}
