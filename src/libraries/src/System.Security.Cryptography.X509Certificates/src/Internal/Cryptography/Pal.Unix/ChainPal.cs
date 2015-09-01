// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            X509Certificate2 leaf = new X509Certificate2(cert.Handle);
            List<X509Certificate2> downloaded = new List<X509Certificate2>();

            List<X509Certificate2> candidates = OpenSslX509ChainProcessor.FindCandidates(
                leaf,
                extraStore,
                downloaded,
                ref remainingDownloadTime);

            IChainPal chain = OpenSslX509ChainProcessor.BuildChain(
                leaf,
                candidates,
                downloaded,
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
            List<X509Certificate2> downloaded)
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

                for (int i = 0; i < chainDownloaded.Count; i++)
                {
                    try
                    {
                        userIntermediate.Add(downloaded[i]);
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
