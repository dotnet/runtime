// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertNameStringType : int
        {
            CERT_X500_NAME_STR = 3,
            CERT_NAME_STR_REVERSE_FLAG = 0x02000000,
        }
    }
}
