// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        // No leaf, include root.
        private const bool TrimRootCertificate = false;

        internal static SslStreamCertificateContext Create(X509Certificate2 target)
        {
            // On Windows we do not need to build chain unless we are asked for it.
            return new SslStreamCertificateContext(target, Array.Empty<X509Certificate2>(), null);
        }

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates, SslCertificateTrust? trust)
        {
            if (intermediates.Length > 0)
            {
                using (X509Chain chain = new X509Chain())
                {
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.DisableCertificateDownloads = true;
                    bool osCanBuildChain = chain.Build(target);

                    int count = 0;
                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        if (status.Status.HasFlag(X509ChainStatusFlags.PartialChain) || status.Status.HasFlag(X509ChainStatusFlags.NotSignatureValid))
                        {
                            osCanBuildChain = false;
                            break;
                        }

                        count++;
                    }

                    // OS failed to build the chain but we have at least some intermediates.
                    // We will try to add them to "Intermediate Certification Authorities" store.
                    if (!osCanBuildChain)
                    {
                        X509Store? store = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);

                        try
                        {
                            store.Open(OpenFlags.ReadWrite);
                        }
                        catch
                        {
                            // If using system store fails, try to fall-back to user store.
                            store.Dispose();
                            store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
                            try
                            {
                                store.Open(OpenFlags.ReadWrite);
                            }
                            catch
                            {
                                store.Dispose();
                                store = null;
                                if (NetEventSource.Log.IsEnabled())
                                {
                                    NetEventSource.Error(this, $"Failed to open certificate store for intermediates.");
                                }
                            }
                        }

                        if (store != null)
                        {
                            using (store)
                            {
                                // Add everything except the root
                                for (int index = count; index < intermediates.Length - 1; index++)
                                {
                                    store.Add(intermediates[index]);
                                }

                                osCanBuildChain = chain.Build(target);
                                foreach (X509ChainStatus status in chain.ChainStatus)
                                {
                                    if (status.Status.HasFlag(X509ChainStatusFlags.PartialChain) || status.Status.HasFlag(X509ChainStatusFlags.NotSignatureValid))
                                    {
                                        osCanBuildChain = false;
                                        break;
                                    }
                                }

                                if (!osCanBuildChain)
                                {
                                    // Add also root to Intermediate CA store so OS can complete building chain.
                                    // (This does not make it trusted.
                                    store.Add(intermediates[intermediates.Length - 1]);
                                }
                            }
                        }
                    }
                }
            }

            Certificate = target;
            IntermediateCertificates = intermediates;
            Trust = trust;
        }
    }
}
