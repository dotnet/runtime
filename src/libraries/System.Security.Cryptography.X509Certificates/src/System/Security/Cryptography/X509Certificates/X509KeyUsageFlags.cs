// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    //
    // Key Usage flags map the definition in wincrypt.h, so that no mapping will be necessary.
    //

    [Flags]
    public enum X509KeyUsageFlags
    {
        None = 0x0000,
        EncipherOnly = 0x0001,
        CrlSign = 0x0002,
        KeyCertSign = 0x0004,
        KeyAgreement = 0x0008,
        DataEncipherment = 0x0010,
        KeyEncipherment = 0x0020,
        NonRepudiation = 0x0040,
        DigitalSignature = 0x0080,
        DecipherOnly = 0x8000,
    }
}

