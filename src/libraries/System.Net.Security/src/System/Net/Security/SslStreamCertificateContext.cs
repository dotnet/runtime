// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        internal readonly X509Certificate2 Certificate;
        internal readonly X509Certificate2[] IntermediateCertificates;

        public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline = false)
        {
            if (!target.HasPrivateKey)
            {
                throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2[] intermediates = Array.Empty<X509Certificate2>();
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
                bool chainStatus = chain.Build(target);

                if (!chainStatus && NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"Failed to build chain for {target.Subject}");
                }

                int count = chain.ChainElements.Count - 1;
#pragma warning disable 0162 // Disable unreachable code warning. TrimRootCertificate is const bool = false on some platforms
                if (TrimRootCertificate)
                {
                    count--;
                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        if (status.Status.HasFlag(X509ChainStatusFlags.PartialChain))
                        {
                            // The last cert isn't a root cert
                            count++;
                            break;
                        }
                    }
                }
#pragma warning restore 0162

                // Count can be zero for a self-signed certificate, or a cert issued directly from a root.
                if (count > 0 && chain.ChainElements.Count > 1)
                {
                    intermediates = new X509Certificate2[count];
                    for (int i = 0; i < count; i++)
                    {
                        intermediates[i] = chain.ChainElements[i + 1].Certificate;
                    }
                }

                // Dispose the copy of the target cert.
                chain.ChainElements[0].Certificate.Dispose();

                // Dispose the last cert, if we didn't include it.
                for (int i = count + 1; i < chain.ChainElements.Count; i++)
                {
                    chain.ChainElements[i].Certificate.Dispose();
                }
            }

            return new SslStreamCertificateContext(target, intermediates);
        }

        internal SslStreamCertificateContext Duplicate()
        {
            return new SslStreamCertificateContext(new X509Certificate2(Certificate), IntermediateCertificates);

        }
    }
}
