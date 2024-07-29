// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Secur32
    {
        [LibraryImport(Libraries.Secur32)]
        internal static partial uint LsaConnectUntrusted(out LsaLogonProcessSafeHandle lsaHandle);
    }
}
