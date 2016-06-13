// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;

using FILETIME = Internal.Cryptography.Pal.Native.FILETIME;
using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal : IDisposable, IChainPal
    {
        /// <summary>
        /// Does not throw on error. Returns null ChainPal instead.
        /// </summary>
        public static ChainPal BuildChain(
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
            CertificatePal certificatePal = (CertificatePal)cert;

            unsafe
            {
                using (SafeCertStoreHandle extraStoreHandle = ConvertExtraStoreToSafeHandle(extraStore))
                {
                    CERT_CHAIN_PARA chainPara = new CERT_CHAIN_PARA();
                    chainPara.cbSize = Marshal.SizeOf<CERT_CHAIN_PARA>();

                    int applicationPolicyCount;
                    using (SafeHandle applicationPolicyOids = applicationPolicy.ToLpstrArray(out applicationPolicyCount))
                    {
                        if (!applicationPolicyOids.IsInvalid)
                        {
                            chainPara.RequestedUsage.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                            chainPara.RequestedUsage.Usage.cUsageIdentifier = applicationPolicyCount;
                            chainPara.RequestedUsage.Usage.rgpszUsageIdentifier = applicationPolicyOids.DangerousGetHandle();
                        }

                        int certificatePolicyCount;
                        using (SafeHandle certificatePolicyOids = certificatePolicy.ToLpstrArray(out certificatePolicyCount))
                        {
                            if (!certificatePolicyOids.IsInvalid)
                            {
                                chainPara.RequestedIssuancePolicy.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                                chainPara.RequestedIssuancePolicy.Usage.cUsageIdentifier = certificatePolicyCount;
                                chainPara.RequestedIssuancePolicy.Usage.rgpszUsageIdentifier = certificatePolicyOids.DangerousGetHandle();
                            }

                            chainPara.dwUrlRetrievalTimeout = (int)Math.Floor(timeout.TotalMilliseconds);

                            FILETIME ft = FILETIME.FromDateTime(verificationTime);
                            CertChainFlags flags = MapRevocationFlags(revocationMode, revocationFlag);
                            ChainEngine chainEngine = useMachineContext ? ChainEngine.HCCE_LOCAL_MACHINE : ChainEngine.HCCE_CURRENT_USER;

                            SafeX509ChainHandle chain;
                            if (!Interop.crypt32.CertGetCertificateChain(chainEngine, certificatePal.CertContext, &ft, extraStoreHandle, ref chainPara, flags, IntPtr.Zero, out chain))
                                return null;
                            return new ChainPal(chain);
                        }
                    }
                }
            }
        }

        private static SafeCertStoreHandle ConvertExtraStoreToSafeHandle(X509Certificate2Collection extraStore)
        {
            if (extraStore == null || extraStore.Count == 0)
                return SafeCertStoreHandle.InvalidHandle;

            return ((StorePal)StorePal.LinkFromCertificateCollection(extraStore)).SafeCertStoreHandle;
        }

        private static CertChainFlags MapRevocationFlags(X509RevocationMode revocationMode, X509RevocationFlag revocationFlag)
        {
            CertChainFlags dwFlags = CertChainFlags.None;
            if (revocationMode == X509RevocationMode.NoCheck)
                return dwFlags;

            if (revocationMode == X509RevocationMode.Offline)
                dwFlags |= CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CACHE_ONLY;

            if (revocationFlag == X509RevocationFlag.EndCertificateOnly)
                dwFlags |= CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_END_CERT;
            else if (revocationFlag == X509RevocationFlag.EntireChain)
                dwFlags |= CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN;
            else
                dwFlags |= CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;

            return dwFlags;
        }

        private ChainPal(SafeX509ChainHandle chain)
        {
            _chain = chain;
        }
    }
}
