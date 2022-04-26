// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class DESCryptoServiceProvider : DES
    {
        [SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DESCryptoServiceProvider")]
        public DESCryptoServiceProvider()
        {
            throw new PlatformNotSupportedException();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) => default!;
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) => default!;
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
}
