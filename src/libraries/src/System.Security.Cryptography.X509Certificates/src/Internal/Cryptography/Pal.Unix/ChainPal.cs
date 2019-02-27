// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal
    {
        public static IChainPal FromHandle(IntPtr chainContext)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool ReleaseSafeX509ChainHandle(IntPtr handle)
        {
            return true;
        }

        public static IChainPal BuildChain(
            bool useMachineContext,
            ICertificatePal cert,
            X509Certificate2Collection extraStore,
            OidCollection applicationPolicy,
            OidCollection certificatePolicy,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            DateTime verificationTime,
            TimeSpan timeout)
        {
            // An input value of 0 on the timeout is "take all the time you need".
            if (timeout == TimeSpan.Zero)
            {
                timeout = TimeSpan.MaxValue;
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

            TimeSpan remainingDownloadTime = timeout;

            OpenSslX509ChainProcessor chainPal = OpenSslX509ChainProcessor.InitiateChain(
                ((OpenSslX509CertificateReader)cert).SafeHandle,
                verificationTime,
                remainingDownloadTime);

            Interop.Crypto.X509VerifyStatusCode status = chainPal.FindFirstChain(extraStore);

            if (!OpenSslX509ChainProcessor.IsCompleteChain(status))
            {
                List<X509Certificate2> tmp = null;
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

            if (revocationMode == X509RevocationMode.Online &&
                status != Interop.Crypto.X509VerifyStatusCode.X509_V_OK)
            {
                revocationMode = X509RevocationMode.Offline;
            }

            // In NoCheck+OK then we don't need to build the chain any more, we already
            // know it's error-free.  So skip straight to finish.
            if (status != Interop.Crypto.X509VerifyStatusCode.X509_V_OK ||
                revocationMode != X509RevocationMode.NoCheck)
            {
                chainPal.CommitToChain();
                chainPal.ProcessRevocation(revocationMode, revocationFlag);
            }

            chainPal.Finish(applicationPolicy, certificatePolicy);

#if DEBUG
            if (chainPal.ChainElements.Length > 0)
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
