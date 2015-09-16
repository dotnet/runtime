// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Internal.Cryptography
{
    internal interface ICertificatePal : IDisposable
    {
        bool HasPrivateKey { get; }
        IntPtr Handle { get; }
        string Issuer { get; }
        string Subject { get; }
        byte[] Thumbprint { get; }
        string KeyAlgorithm { get; }
        byte[] KeyAlgorithmParameters { get; }
        byte[] PublicKeyValue { get; }
        byte[] SerialNumber { get; }
        string SignatureAlgorithm { get; }
        DateTime NotAfter { get; }
        DateTime NotBefore { get; }
        byte[] RawData { get; }
        int Version { get; }
        bool Archived { get; set; }
        string FriendlyName { get; set; }
        X500DistinguishedName SubjectName { get; }
        X500DistinguishedName IssuerName { get; }
        IEnumerable<X509Extension> Extensions { get; }
        RSA GetRSAPrivateKey();
        ECDsa GetECDsaPrivateKey();
        string GetNameInfo(X509NameType nameType, bool forIssuer);
        void AppendPrivateKeyInfo(StringBuilder sb);
    }
}
