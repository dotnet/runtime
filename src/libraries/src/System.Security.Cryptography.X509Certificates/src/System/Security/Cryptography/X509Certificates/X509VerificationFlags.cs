// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    [Flags]
    public enum X509VerificationFlags
    {
        NoFlag = 0x00000000,
        IgnoreNotTimeValid = 0x00000001,
        IgnoreCtlNotTimeValid = 0x00000002,
        IgnoreNotTimeNested = 0x00000004,
        IgnoreInvalidBasicConstraints = 0x00000008,
        AllowUnknownCertificateAuthority = 0x00000010,
        IgnoreWrongUsage = 0x00000020,
        IgnoreInvalidName = 0x00000040,
        IgnoreInvalidPolicy = 0x00000080,
        IgnoreEndRevocationUnknown = 0x00000100,
        IgnoreCtlSignerRevocationUnknown = 0x00000200,
        IgnoreCertificateAuthorityRevocationUnknown = 0x00000400,
        IgnoreRootRevocationUnknown = 0x00000800,
        AllFlags = 0x00000FFF,
    }
}

