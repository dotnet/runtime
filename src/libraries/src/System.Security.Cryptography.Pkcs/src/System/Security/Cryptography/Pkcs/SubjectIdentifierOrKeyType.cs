// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public enum SubjectIdentifierOrKeyType
    {
        Unknown = 0,                // Use any of the following as appropriate
        IssuerAndSerialNumber = 1,  // X509IssuerSerial
        SubjectKeyIdentifier = 2,   // SKI hex string
        PublicKeyInfo = 3,          // PublicKeyInfo
    }
}


