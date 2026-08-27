// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class Crypt32
    {
        internal enum CertControlStoreType : int
        {
            CERT_STORE_CTRL_AUTO_RESYNC = 4,
        }
    }
}
