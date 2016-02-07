// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal
    {
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

            TimeSpan remainingDownloadTime = timeout;
            var leaf = new X509Certificate2(cert.Handle);
            var downloaded = new HashSet<X509Certificate2>();
            var systemTrusted = new HashSet<X509Certificate2>();

            HashSet<X509Certificate2> candidates = OpenSslX509ChainProcessor.FindCandidates(
                leaf,
                extraStore,
                downloaded,
                systemTrusted,
                ref remainingDownloadTime);

            IChainPal chain = OpenSslX509ChainProcessor.BuildChain(
                leaf,
                candidates,
                downloaded,
                systemTrusted,
                applicationPolicy,
                certificatePolicy,
                revocationMode,
                revocationFlag,
                verificationTime,
                ref remainingDownloadTime);

            if (chain.ChainStatus.Length == 0 && downloaded.Count > 0)
            {
                SaveIntermediateCertificates(chain.ChainElements, downloaded);
            }

            return chain;
        }

        private static void SaveIntermediateCertificates(
            X509ChainElement[] chainElements,
            HashSet<X509Certificate2> downloaded)
        {
            List<X509Certificate2> chainDownloaded = new List<X509Certificate2>(chainElements.Length);

            // It should be very unlikely that we would have downloaded something, the chain succeed,
            // and the thing we downloaded not being a part of the chain, but safer is better.
            for (int i = 0; i < chainElements.Length; i++)
            {
                X509Certificate2 elementCert = chainElements[i].Certificate;

                if (downloaded.Contains(elementCert))
                {
                    chainDownloaded.Add(elementCert);
                }
            }

            if (chainDownloaded.Count == 0)
            {
                return;
            }

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

                foreach (X509Certificate2 cert in chainDownloaded)
                {
                    try
                    {
                        userIntermediate.Add(cert);
                    }
                    catch (CryptographicException)
                    {
                        // Saving is opportunistic, just ignore failures
                    }
                    catch (IOException)
                    {
                        // Saving is opportunistic, just ignore failures
                    }
                }
            }
        }
    }
}
