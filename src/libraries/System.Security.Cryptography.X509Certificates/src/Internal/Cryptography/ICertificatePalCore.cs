// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    /// <summary>Provides specific implementation for X509Certificate.</summary>
    internal interface ICertificatePalCore : IDisposable
    {
        bool HasPrivateKey { get; }
        IntPtr Handle { get; }
        string Issuer { get; }
        string Subject { get; }
        string LegacyIssuer { get; }
        string LegacySubject { get; }
        byte[] Thumbprint { get; }
        string KeyAlgorithm { get; }
        byte[] KeyAlgorithmParameters { get; }
        byte[] PublicKeyValue { get; }
        byte[] SerialNumber { get; }
        string SignatureAlgorithm { get; }
        DateTime NotAfter { get; }
        DateTime NotBefore { get; }
        byte[] RawData { get; }
        byte[] Export(X509ContentType contentType, SafePasswordHandle password);
    }
}
