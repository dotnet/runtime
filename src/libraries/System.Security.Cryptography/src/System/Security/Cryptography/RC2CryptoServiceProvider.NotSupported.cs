// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed partial class RC2CryptoServiceProvider : RC2
    {
        [SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of RC2CryptoServiceProvider")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public RC2CryptoServiceProvider()
        {
            throw new PlatformNotSupportedException();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) => default!;
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) => default!;
        public override void GenerateIV() { }
        public override void GenerateKey() { }

        public bool UseSalt
        {
            get { return false; }
            [SupportedOSPlatform("windows")]
            set { }
        }
    }
}
