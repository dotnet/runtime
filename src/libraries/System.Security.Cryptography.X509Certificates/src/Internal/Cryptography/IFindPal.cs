// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IFindPal : IDisposable
    {
        string NormalizeOid(string maybeOid, OidGroup expectedGroup);

        void FindByThumbprint(byte[] thumbprint);
        void FindBySubjectName(string subjectName);
        void FindBySubjectDistinguishedName(string subjectDistinguishedName);
        void FindByIssuerName(string issuerName);
        void FindByIssuerDistinguishedName(string issuerDistinguishedName);
        void FindBySerialNumber(BigInteger hexValue, BigInteger decimalValue);
        void FindByTimeValid(DateTime dateTime);
        void FindByTimeNotYetValid(DateTime dateTime);
        void FindByTimeExpired(DateTime dateTime);
        void FindByTemplateName(string templateName);
        void FindByApplicationPolicy(string oidValue);
        void FindByCertificatePolicy(string oidValue);
        void FindByExtension(string oidValue);
        void FindByKeyUsage(X509KeyUsageFlags keyUsage);
        void FindBySubjectKeyIdentifier(byte[] keyIdentifier);
    }
}
