// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    internal static class SslClientAuthenticationOptionsExtensions
    {
        public static SslClientAuthenticationOptions ShallowClone(this SslClientAuthenticationOptions options) =>
            new SslClientAuthenticationOptions()
            {
                AllowRenegotiation = options.AllowRenegotiation,
                ApplicationProtocols = options.ApplicationProtocols,
                CertificateRevocationCheckMode = options.CertificateRevocationCheckMode,
                ClientCertificates = options.ClientCertificates,
                EnabledSslProtocols = options.EnabledSslProtocols,
                EncryptionPolicy = options.EncryptionPolicy,
                LocalCertificateSelectionCallback = options.LocalCertificateSelectionCallback,
                RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback,
                TargetHost = options.TargetHost
            };
    }
}
