// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class ChainPal : IDisposable, IChainPal
    {
        /// <summary>
        /// Does not throw on error. Returns null ChainPal instead.
        /// </summary>
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
            CertificatePal certificatePal = (CertificatePal)cert;

            unsafe
            {
                using (SafeChainEngineHandle storeHandle = GetChainEngine(trustMode, customTrustStore, useMachineContext))
                using (SafeCertStoreHandle extraStoreHandle = ConvertStoreToSafeHandle(extraStore))
                {
                    Interop.Crypt32.CERT_CHAIN_PARA chainPara = default;
                    chainPara.cbSize = Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_PARA>();

                    int applicationPolicyCount;
                    using (SafeHandle applicationPolicyOids = applicationPolicy!.ToLpstrArray(out applicationPolicyCount))
                    {
                        if (!applicationPolicyOids.IsInvalid)
                        {
                            chainPara.RequestedUsage.dwType = Interop.Crypt32.CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                            chainPara.RequestedUsage.Usage.cUsageIdentifier = applicationPolicyCount;
                            chainPara.RequestedUsage.Usage.rgpszUsageIdentifier = applicationPolicyOids.DangerousGetHandle();
                        }

                        int certificatePolicyCount;
                        using (SafeHandle certificatePolicyOids = certificatePolicy!.ToLpstrArray(out certificatePolicyCount))
                        {
                            if (!certificatePolicyOids.IsInvalid)
                            {
                                chainPara.RequestedIssuancePolicy.dwType = Interop.Crypt32.CertUsageMatchType.USAGE_MATCH_TYPE_AND;
                                chainPara.RequestedIssuancePolicy.Usage.cUsageIdentifier = certificatePolicyCount;
                                chainPara.RequestedIssuancePolicy.Usage.rgpszUsageIdentifier = certificatePolicyOids.DangerousGetHandle();
                            }

                            chainPara.dwUrlRetrievalTimeout = (int)Math.Floor(timeout.TotalMilliseconds);

                            Interop.Crypt32.FILETIME ft = Interop.Crypt32.FILETIME.FromDateTime(verificationTime);
                            Interop.Crypt32.CertChainFlags flags = MapRevocationFlags(revocationMode, revocationFlag, disableAia);
                            SafeX509ChainHandle chain;
                            using (SafeCertContextHandle certContext = certificatePal.GetCertContext())
                            {
                                if (!Interop.Crypt32.CertGetCertificateChain(storeHandle.DangerousGetHandle(), certContext, &ft, extraStoreHandle, ref chainPara, flags, IntPtr.Zero, out chain))
                                {
                                    chain.Dispose();
                                    return null;
                                }
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
                    Interop.Crypt32.CERT_CHAIN_ENGINE_CONFIG customChainEngine = default;
                    customChainEngine.cbSize = Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_ENGINE_CONFIG>();
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

        private static Interop.Crypt32.CertChainFlags MapRevocationFlags(
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            bool disableAia)
        {
            const Interop.Crypt32.CertChainFlags AiaDisabledFlags =
                Interop.Crypt32.CertChainFlags.CERT_CHAIN_DISABLE_AIA | Interop.Crypt32.CertChainFlags.CERT_CHAIN_DISABLE_AUTH_ROOT_AUTO_UPDATE;

            Interop.Crypt32.CertChainFlags dwFlags = disableAia ? AiaDisabledFlags : Interop.Crypt32.CertChainFlags.None;

            if (revocationMode == X509RevocationMode.NoCheck)
                return dwFlags;

            if (revocationMode == X509RevocationMode.Offline)
                dwFlags |= Interop.Crypt32.CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CACHE_ONLY;

            if (revocationFlag == X509RevocationFlag.EndCertificateOnly)
                dwFlags |= Interop.Crypt32.CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_END_CERT;
            else if (revocationFlag == X509RevocationFlag.EntireChain)
                dwFlags |= Interop.Crypt32.CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN;
            else
                dwFlags |= Interop.Crypt32.CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;

            return dwFlags;
        }
    }
}
