// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Internal.Cryptography
{
    /// <summary>Provides specific implementation for X509Certificate2.</summary>
    internal interface ICertificatePal : ICertificatePalCore
    {
        int Version { get; }
        bool Archived { get; set; }
        string FriendlyName { get; set; }
        X500DistinguishedName SubjectName { get; }
        X500DistinguishedName IssuerName { get; }
        IEnumerable<X509Extension> Extensions { get; }
        RSA GetRSAPrivateKey();
        DSA GetDSAPrivateKey();
        ECDsa GetECDsaPrivateKey();
        string GetNameInfo(X509NameType nameType, bool forIssuer);
        void AppendPrivateKeyInfo(StringBuilder sb);
        ICertificatePal CopyWithPrivateKey(DSA privateKey);
        ICertificatePal CopyWithPrivateKey(ECDsa privateKey);
        ICertificatePal CopyWithPrivateKey(RSA privateKey);
    }
}
