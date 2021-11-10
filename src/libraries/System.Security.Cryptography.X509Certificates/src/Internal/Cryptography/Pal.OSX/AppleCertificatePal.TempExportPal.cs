// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private sealed class TempExportPal : ICertificatePalCore
        {
            private readonly ICertificatePal _realPal;

            internal TempExportPal(AppleCertificatePal realPal)
            {
                _realPal = realPal;
            }

            public bool HasPrivateKey => true;

            public void Dispose()
            {
                // No-op.
            }

            // Forwarders to make the interface compliant.
            public IntPtr Handle => _realPal.Handle;
            public string Issuer => _realPal.Issuer;
            public string Subject => _realPal.Subject;
            public string LegacyIssuer => _realPal.LegacyIssuer;
            public string LegacySubject => _realPal.LegacySubject;
            public byte[] Thumbprint => _realPal.Thumbprint;
            public string KeyAlgorithm => _realPal.KeyAlgorithm;
            public byte[] KeyAlgorithmParameters => _realPal.KeyAlgorithmParameters;
            public byte[] PublicKeyValue => _realPal.PublicKeyValue;
            public byte[] SerialNumber => _realPal.SerialNumber;
            public string SignatureAlgorithm => _realPal.SignatureAlgorithm;
            public DateTime NotAfter => _realPal.NotAfter;
            public DateTime NotBefore => _realPal.NotBefore;
            public byte[] RawData => _realPal.RawData;
            public byte[] Export(X509ContentType contentType, SafePasswordHandle password) =>
                _realPal.Export(contentType, password);
        }
    }
}
