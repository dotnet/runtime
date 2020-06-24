// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public class SslStreamCertificateContext
    {
        internal readonly X509Certificate2 Certificate;
        internal readonly X509Certificate2[]? IntermediateCertificates;

        public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline = false)
        {
            if (!target.HasPrivateKey)
            {
                throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2[]? intermediates = null;

            using (X509Chain chain = new X509Chain())
            {
                if (additionalCertificates != null)
                {
                    foreach (X509Certificate cert in additionalCertificates)
                    {
                        chain.ChainPolicy.ExtraStore.Add(cert);
                    }
                }

                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.DisableCertificateDownloads = offline;
                chain.Build(target);

                if (chain.ChainElements.Count > 2)
                {
                    int count = chain.ChainElements.Count;
                    intermediates = new X509Certificate2[count];
                    for (int i = 0; i < count; i++)
                    {
                        intermediates[i] = chain.ChainElements[i + 1].Certificate;
                    }
                }
            }

            return new SslStreamCertificateContext(target, intermediates);
        }

        internal SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[]? intermediates = null)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
        }
    }
}
