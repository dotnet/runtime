// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal
    {
        private static readonly TimeSpan s_maxUrlRetrievalTimeout = TimeSpan.FromMinutes(1);

        public static IChainPal FromHandle(IntPtr chainContext)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool ReleaseSafeX509ChainHandle(IntPtr handle)
        {
            return true;
        }

        public static void FlushStores()
        {
            OpenSslX509ChainProcessor.FlushStores();
        }

        public static IChainPal BuildChain(
            bool useMachineContext,
            ICertificatePal cert,
            X509Certificate2Collection? extraStore,
            OidCollection applicationPolicy,
            OidCollection certificatePolicy,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            X509Certificate2Collection customTrustStore,
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

            if (!OpenSslX509ChainProcessor.IsCompleteChain(status) && !disableAia)
            {
                List<X509Certificate2>? tmp = null;
                status = chainPal.FindChainViaAia(ref tmp);

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

            // In NoCheck+OK then we don't need to build the chain any more, we already
            // know it's error-free.  So skip straight to finish.
            if (status != Interop.Crypto.X509VerifyStatusCode.X509_V_OK ||
                revocationMode != X509RevocationMode.NoCheck)
            {
                if (OpenSslX509ChainProcessor.IsCompleteChain(status))
                {
                    chainPal.CommitToChain();
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
                    return;
                }

                foreach (X509Certificate2 cert in downloadedCerts)
                {
                    try
                    {
                        userIntermediate.Add(cert);
                    }
                    catch
                    {
                        // Saving is opportunistic, just ignore failures
                    }
                }
            }
        }
    }
}
