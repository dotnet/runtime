// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("ios13.0")]
    [SupportedOSPlatform("tvos13.0")]
    public sealed partial class AesGcm
    {
        [Obsolete(Obsoletions.AesGcmTagConstructorMessage, DiagnosticId = Obsoletions.AesGcmTagConstructorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AesGcm(ReadOnlySpan<byte> key)
        {
            ThrowIfNotSupported();

            AesAEAD.CheckKeySize(key.Length);
            ImportKey(key);
        }

        [Obsolete(Obsoletions.AesGcmTagConstructorMessage, DiagnosticId = Obsoletions.AesGcmTagConstructorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AesGcm(byte[] key)
            : this(new ReadOnlySpan<byte>(key ?? throw new ArgumentNullException(nameof(key))))
        {
        }
    }
}
