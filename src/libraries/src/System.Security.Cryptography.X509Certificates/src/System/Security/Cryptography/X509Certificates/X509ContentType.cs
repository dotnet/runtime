// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

