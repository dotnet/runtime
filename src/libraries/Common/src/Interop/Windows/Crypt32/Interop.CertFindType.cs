// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertFindType : int
        {
            CERT_FIND_SUBJECT_CERT = 0x000b0000,
            CERT_FIND_HASH         = 0x00010000,
            CERT_FIND_SUBJECT_STR  = 0x00080007,
            CERT_FIND_ISSUER_STR   = 0x00080004,
            CERT_FIND_EXISTING     = 0x000d0000,
            CERT_FIND_ANY          = 0x00000000,
        }
    }
}
