// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public enum X509ContentType
    {
        Unknown = 0x00,
        Cert = 0x01,
        SerializedCert = 0x02,
        Pfx = 0x03,
        Pkcs12 = Pfx,
        SerializedStore = 0x04,
        Pkcs7 = 0x05,
        Authenticode = 0x06,
    }
}

