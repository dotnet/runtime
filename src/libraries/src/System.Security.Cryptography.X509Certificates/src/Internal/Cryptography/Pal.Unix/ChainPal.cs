// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            CheckRevocationMode(revocationMode);

            X509Certificate2 leaf = new X509Certificate2(cert.Handle);

            X509Certificate2Collection candidates = OpenSslX509ChainProcessor.FindCandidates(leaf, extraStore);

            return OpenSslX509ChainProcessor.BuildChain(
                leaf,
                candidates,
                applicationPolicy,
                certificatePolicy,
                verificationTime);
        }

        private static void CheckRevocationMode(X509RevocationMode revocationMode)
        {
            if (revocationMode != X509RevocationMode.NoCheck)
            {
                // TODO (#2203): Add support for revocation once networking is ready.
                throw new NotImplementedException(SR.WorkInProgress);
            }
        }
    }
}
