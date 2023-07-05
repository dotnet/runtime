// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        internal readonly SslCertificateTrust? Trust;

        /// <summary>
        /// Gets the target (leaf) certificate of the built chain.
        /// </summary>
        public X509Certificate2 TargetCertificate { get; }

        /// <summary>
        /// Gets the intermediate certificates for the built chain.
        /// </summary>
        public ReadOnlyCollection<X509Certificate2> IntermediateCertificates { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline)
        {
            return Create(target, additionalCertificates, offline, null);
        }

        public static SslStreamCertificateContext Create(X509Certificate2 target, X509Certificate2Collection? additionalCertificates, bool offline = false, SslCertificateTrust? trust = null)
        {
            return Create(target, additionalCertificates, offline, trust, noOcspFetch: false);
        }

        internal static SslStreamCertificateContext Create(
            X509Certificate2 target,
            X509Certificate2Collection? additionalCertificates,
            bool offline,
            SslCertificateTrust? trust,
            bool noOcspFetch)
        {
            if (!target.HasPrivateKey)
            {
                throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2[] intermediates = Array.Empty<X509Certificate2>();
            X509Certificate2? root = null;

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

                // Some platforms (e.g. Android) can't ignore all verification and will return zero
                // certificates on failure to build a chain. Treat this as not finding any intermediates.
                if (count >= 0)
                {
#pragma warning disable 0162 // Disable unreachable code warning. TrimRootCertificate is const bool = false on some platforms
                    if (TrimRootCertificate)
                    {
                        count--;
                        root = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;

                        foreach (X509ChainStatus status in chain.ChainStatus)
                        {
                            if (status.Status.HasFlag(X509ChainStatusFlags.PartialChain))
                            {
                                // The last cert isn't a root cert
                                count++;
                                root = null;
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

                    // Dispose of the certificates that we do not need. If we are holding on to the root,
                    // don't dispose of it.
                    int stopDisposingChainPosition = root is null ?
                        chain.ChainElements.Count :
                        chain.ChainElements.Count - 1;

                    for (int i = count + 1; i < stopDisposingChainPosition; i++)
                    {
                        chain.ChainElements[i].Certificate.Dispose();
                    }
                }
            }

            SslStreamCertificateContext ctx = new SslStreamCertificateContext(target, new ReadOnlyCollection<X509Certificate2>(intermediates), trust);

            // On Linux, AddRootCertificate will start a background download of an OCSP response,
            // unless this context was built "offline", or this came from the internal Create(X509Certificate2)
            ctx.SetNoOcspFetch(offline || noOcspFetch);

            bool transferredOwnership = false;
            ctx.AddRootCertificate(root, ref transferredOwnership);

            if (!transferredOwnership)
            {
                root?.Dispose();
            }

            return ctx;
        }

        partial void AddRootCertificate(X509Certificate2? rootCertificate, ref bool transferredOwnership);
        partial void SetNoOcspFetch(bool noOcspFetch);

        internal SslStreamCertificateContext Duplicate()
        {
            return new SslStreamCertificateContext(new X509Certificate2(TargetCertificate), IntermediateCertificates, Trust);
        }
    }
}
