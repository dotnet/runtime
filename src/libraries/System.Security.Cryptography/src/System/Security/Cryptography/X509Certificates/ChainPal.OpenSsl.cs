// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class ChainPal
    {
        private static readonly TimeSpan s_maxUrlRetrievalTimeout = TimeSpan.FromMinutes(1);

#pragma warning disable IDE0060
        internal static partial IChainPal FromHandle(IntPtr chainContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal static partial bool ReleaseSafeX509ChainHandle(IntPtr handle)
        {
            return true;
        }
#pragma warning restore IDE0060

        public static void FlushStores()
        {
            OpenSslX509ChainProcessor.FlushStores();
        }

        internal static partial IChainPal? BuildChain(
            bool useMachineContext,
            ICertificatePal cert,
            X509Certificate2Collection? extraStore,
            OidCollection? applicationPolicy,
            OidCollection? certificatePolicy,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            X509Certificate2Collection? customTrustStore,
            X509ChainTrustMode trustMode,
            DateTime verificationTime,
            TimeSpan timeout,
            bool disableAia)
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.ChainStart();
            }

            try
            {
                return BuildChainCore(
                    useMachineContext,
                    cert,
                    extraStore,
                    applicationPolicy,
                    certificatePolicy,
                    revocationMode,
                    revocationFlag,
                    customTrustStore,
                    trustMode,
                    verificationTime,
                    timeout,
                    disableAia);
            }
            finally
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.ChainStop();
                }
            }
        }

        private static OpenSslX509ChainProcessor? BuildChainCore(
            bool useMachineContext,
            ICertificatePal cert,
            X509Certificate2Collection? extraStore,
            OidCollection? applicationPolicy,
            OidCollection? certificatePolicy,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            X509Certificate2Collection? customTrustStore,
            X509ChainTrustMode trustMode,
            DateTime verificationTime,
            TimeSpan timeout,
            bool disableAia)
        {
            if (timeout == TimeSpan.Zero)
            {
                // An input value of 0 on the timeout is treated as 15 seconds, to match Windows.
                timeout = TimeSpan.FromSeconds(15);
            }
            else if (timeout > s_maxUrlRetrievalTimeout || timeout < TimeSpan.Zero)
            {
                // Windows has a max timeout of 1 minute, so we'll match. Windows also treats
                // the timeout as unsigned, so a negative value gets treated as a large positive
                // value that is also clamped.
                timeout = s_maxUrlRetrievalTimeout;
            }

            // Let Unspecified mean Local, so only convert if the source was UTC.
            //
            // Converge on Local instead of UTC because OpenSSL is going to assume we gave it
            // local time.
            if (verificationTime.Kind == DateTimeKind.Utc)
            {
                verificationTime = verificationTime.ToLocalTime();
            }

            // Until we support the Disallowed store, ensure it's empty (which is done by the ctor)
            using (new X509Store(StoreName.Disallowed, StoreLocation.CurrentUser, OpenFlags.ReadOnly))
            {
            }

            TimeSpan downloadTimeout = timeout;

            OpenSslX509ChainProcessor chainPal = OpenSslX509ChainProcessor.InitiateChain(
                ((OpenSslX509CertificateReader)cert).SafeHandle,
                customTrustStore,
                trustMode,
                verificationTime,
                downloadTimeout);

            Interop.Crypto.X509VerifyStatusCode status = chainPal.FindFirstChain(extraStore);

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.FindFirstChainFinished(status);
            }

            if (!OpenSslX509ChainProcessor.IsCompleteChain(status))
            {
                if (disableAia)
                {
                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.AiaDisabled();
                    }
                }
                else
                {
                    List<X509Certificate2>? tmp = null;
                    status = chainPal.FindChainViaAia(ref tmp);

                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.FindChainViaAiaFinished(status, tmp?.Count ?? 0);
                    }

                    if (tmp != null)
                    {
                        if (status == Interop.Crypto.X509VerifyStatusCode.X509_V_OK)
                        {
                            SaveIntermediateCertificates(tmp);
                        }

                        foreach (X509Certificate2 downloaded in tmp)
                        {
                            downloaded.Dispose();
                        }
                    }
                }
            }

            chainPal.CommitToChain();

            if (revocationMode != X509RevocationMode.NoCheck)
            {
                if (OpenSslX509ChainProcessor.IsCompleteChain(status))
                {
                    // Checking the validity period for the certificates in the chain is done after the
                    // check for a trusted root, so accept expired (or not yet valid) as acceptable for
                    // processing revocation.
                    if (status != Interop.Crypto.X509VerifyStatusCode.X509_V_OK &&
                        status != Interop.Crypto.X509VerifyStatusCodeUniversal.X509_V_ERR_CERT_NOT_YET_VALID &&
                        status != Interop.Crypto.X509VerifyStatusCodeUniversal.X509_V_ERR_CERT_HAS_EXPIRED)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.UntrustedChainWithRevocation();
                        }

                        revocationMode = X509RevocationMode.NoCheck;
                    }

                    chainPal.ProcessRevocation(revocationMode, revocationFlag);
                }
            }

            chainPal.Finish(applicationPolicy, certificatePolicy);

#if DEBUG
            if (chainPal.ChainElements!.Length > 0)
            {
                X509Certificate2 reportedLeaf = chainPal.ChainElements[0].Certificate;
                Debug.Assert(reportedLeaf != null, "reportedLeaf != null");
                Debug.Assert(!ReferenceEquals(cert, reportedLeaf.Pal), "!ReferenceEquals(cert, reportedLeaf.Pal)");
            }
#endif
            return chainPal;
        }

        private static void SaveIntermediateCertificates(List<X509Certificate2> downloadedCerts)
        {
            using (var userIntermediate = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                try
                {
                    userIntermediate.Open(OpenFlags.ReadWrite);
                }
                catch (CryptographicException)
                {
                    // Saving is opportunistic, just ignore failures

                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.CouldNotOpenCAStore();
                    }

                    return;
                }

                foreach (X509Certificate2 cert in downloadedCerts)
                {
                    try
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CachingIntermediate(cert);
                        }

                        userIntermediate.Add(cert);
                    }
                    catch
                    {
                        // Saving is opportunistic, just ignore failures

                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.CachingIntermediateFailedMessage();
                        }
                    }
                }
            }
        }
    }
}
