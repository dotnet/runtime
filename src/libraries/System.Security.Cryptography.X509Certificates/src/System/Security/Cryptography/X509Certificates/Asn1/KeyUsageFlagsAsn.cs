// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    // https://tools.ietf.org/html/rfc3280#section-4.2.1.3
    //
    // KeyUsage ::= BIT STRING {
    //     digitalSignature        (0),
    //     nonRepudiation          (1),
    //     keyEncipherment         (2),
    //     dataEncipherment        (3),
    //     keyAgreement            (4),
    //     keyCertSign             (5),
    //     cRLSign                 (6),
    //     encipherOnly            (7),
    //     decipherOnly            (8)
    // }
    //
    // Version of X509KeyUsageFlags with reversed bit order.
    [Flags]
    internal enum KeyUsageFlagsAsn
    {
        None = 0,
        DigitalSignature = 1 << 0,
        NonRepudiation = 1 << 1,
        KeyEncipherment = 1 << 2,
        DataEncipherment = 1 << 3,
        KeyAgreement = 1 << 4,
        KeyCertSign = 1 << 5,
        CrlSign = 1 << 6,
        EncipherOnly = 1 << 7,
        DecipherOnly = 1 << 8,
    }
}

