// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    internal partial class ChainPal
    {
        internal static partial IChainPal FromHandle(IntPtr chainContext);

        internal static partial bool ReleaseSafeX509ChainHandle(IntPtr handle);

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
            bool disableAia);
    }
}
