// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

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
        public static ChainPal? BuildChain(
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
            CertificatePal certificatePal = (CertificatePal)cert;

            unsafe
            {
                using (SafeChainEngineHandle storeHandle = GetChainEngine(trustMode, customTrustStore, useMachineContext))
                using (SafeCertStoreHandle extraStoreHandle = ConvertStoreToSafeHandle(extraStore))
                {
                    CERT_CHAIN_PARA chainPara = default;
                    chainPara.cbSize = Marshal.SizeOf<CERT_CHAIN_PARA>();

                    int applicationPolicyCount;
                    using (SafeHandle applicationPolicyOids = applicationPolicy!.ToLpstrArray(out applicationPolicyCount))
                    {
                        if (!applicationPolicyOids.IsInvalid)
                        {
                            chainPara.RequestedUsage.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                            chainPara.RequestedUsage.Usage.cUsageIdentifier = applicationPolicyCount;
                            chainPara.RequestedUsage.Usage.rgpszUsageIdentifier = applicationPolicyOids.DangerousGetHandle();
                        }

                        int certificatePolicyCount;
                        using (SafeHandle certificatePolicyOids = certificatePolicy!.ToLpstrArray(out certificatePolicyCount))
                        {
                            if (!certificatePolicyOids.IsInvalid)
                            {
                                chainPara.RequestedIssuancePolicy.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                                chainPara.RequestedIssuancePolicy.Usage.cUsageIdentifier = certificatePolicyCount;
                                chainPara.RequestedIssuancePolicy.Usage.rgpszUsageIdentifier = certificatePolicyOids.DangerousGetHandle();
                            }

                            chainPara.dwUrlRetrievalTimeout = (int)Math.Floor(timeout.TotalMilliseconds);

                            FILETIME ft = FILETIME.FromDateTime(verificationTime);
                            CertChainFlags flags = MapRevocationFlags(revocationMode, revocationFlag, disableAia);
                            SafeX509ChainHandle chain;
                            if (!Interop.crypt32.CertGetCertificateChain(storeHandle.DangerousGetHandle(), certificatePal.CertContext, &ft, extraStoreHandle, ref chainPara, flags, IntPtr.Zero, out chain))
                            {
                                return null;
                            }

                            return new ChainPal(chain);
                        }
                    }
                }
            }
        }

        private static SafeChainEngineHandle GetChainEngine(
            X509ChainTrustMode trustMode,
            X509Certificate2Collection? customTrustStore,
            bool useMachineContext)
        {
            SafeChainEngineHandle chainEngineHandle;
            if (trustMode == X509ChainTrustMode.CustomRootTrust)
            {
                // Need to get a valid SafeCertStoreHandle otherwise the default stores will be trusted
                using (SafeCertStoreHandle customTrustStoreHandle = ConvertStoreToSafeHandle(customTrustStore, true))
                {
                    CERT_CHAIN_ENGINE_CONFIG customChainEngine = default;
                    customChainEngine.cbSize = Marshal.SizeOf<CERT_CHAIN_ENGINE_CONFIG>();
                    customChainEngine.hExclusiveRoot = customTrustStoreHandle.DangerousGetHandle();
                    chainEngineHandle = Interop.crypt32.CertCreateCertificateChainEngine(ref customChainEngine);
                }
            }
            else
            {
                chainEngineHandle = useMachineContext ? SafeChainEngineHandle.MachineChainEngine : SafeChainEngineHandle.UserChainEngine;
            }

            return chainEngineHandle;
        }

        private static SafeCertStoreHandle ConvertStoreToSafeHandle(X509Certificate2Collection? extraStore, bool returnEmptyHandle = false)
        {
            if ((extraStore == null || extraStore.Count == 0) && !returnEmptyHandle)
                return SafeCertStoreHandle.InvalidHandle;

            return ((StorePal)StorePal.LinkFromCertificateCollection(extraStore!)).SafeCertStoreHandle;
        }

        private static CertChainFlags MapRevocationFlags(
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            bool disableAia)
        {
            const CertChainFlags AiaDisabledFlags =
                CertChainFlags.CERT_CHAIN_DISABLE_AIA | CertChainFlags.CERT_CHAIN_DISABLE_AUTH_ROOT_AUTO_UPDATE;

            CertChainFlags dwFlags = disableAia ? AiaDisabledFlags : CertChainFlags.None;

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
