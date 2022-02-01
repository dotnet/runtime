// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

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
            var chainPal = new AndroidCertPath();
            try
            {
                chainPal.Initialize(cert, extraStore, customTrustStore, trustMode);
                chainPal.Evaluate(verificationTime, applicationPolicy, certificatePolicy, revocationMode, revocationFlag);
            }
            catch
            {
                chainPal.Dispose();
                throw;
            }

            return chainPal;
        }

        private sealed class AndroidCertPath : IChainPal
        {
            public X509ChainElement[]? ChainElements { get; private set; }
            public X509ChainStatus[]? ChainStatus { get; private set; }

            public SafeX509ChainHandle? SafeHandle => null;

            private SafeX509ChainContextHandle? _chainContext;
            private bool _isValid;

            public void Dispose()
            {
                if (_chainContext != null)
                {
                    _chainContext.Dispose();
                }
            }

            public bool? Verify(X509VerificationFlags flags, out Exception? exception)
            {
                Debug.Assert(_chainContext != null);
                exception = null;

                if (!_isValid)
                {
                    // There is no way to bypass certain validation - time, trusted root, name,
                    // policy constraint - on Android. It will not build any chain without these
                    // all being valid. This will be an empty chain with PartialChain status.
                    Debug.Assert(ChainElements!.Length == 0);
                    Debug.Assert(ChainStatus!.Length > 0 && (ChainStatus[0].Status & X509ChainStatusFlags.PartialChain) == X509ChainStatusFlags.PartialChain);
                    return false;
                }

                return ChainVerifier.Verify(ChainElements!, flags);
            }

            internal void Initialize(
                ICertificatePal cert,
                X509Certificate2Collection? extraStore,
                X509Certificate2Collection customTrustStore,
                X509ChainTrustMode trustMode)
            {
                List<SafeHandle> extraCertHandles = new List<SafeHandle>() { ((AndroidCertificatePal)cert).SafeHandle };
                if (extraStore != null)
                {
                    foreach (X509Certificate2 extraCert in extraStore)
                    {
                        extraCertHandles.Add(((AndroidCertificatePal)extraCert.Pal).SafeHandle);
                    }
                }

                Debug.Assert(
                    trustMode == X509ChainTrustMode.System || trustMode == X509ChainTrustMode.CustomRootTrust,
                    "Unsupported trust mode. Only System and CustomRootTrust are currently handled");

                List<SafeHandle> customTrustCertHandles = new List<SafeHandle>();
                bool useCustomRootTrust = trustMode == X509ChainTrustMode.CustomRootTrust;
                if (useCustomRootTrust && customTrustStore != null)
                {
                    foreach (X509Certificate2 custom in customTrustStore)
                    {
                        SafeHandle certHandle = ((AndroidCertificatePal)custom.Pal).SafeHandle;
                        if (custom.SubjectName.RawData.ContentsEqual(custom.IssuerName.RawData))
                        {
                            // Add self-issued certs to custom root trust cert
                            customTrustCertHandles.Add(certHandle);
                        }
                        else
                        {
                            // Add non-self-issued certs to extra certs
                            extraCertHandles.Add(certHandle);
                        }
                    }
                }

                int extraIdx = 0;
                int customIdx = 0;
                try
                {
                    IntPtr[] extraCerts = new IntPtr[extraCertHandles.Count];
                    for (extraIdx = 0; extraIdx < extraCertHandles.Count; extraIdx++)
                    {
                        SafeHandle handle = extraCertHandles[extraIdx];
                        bool addedRef = false;
                        handle.DangerousAddRef(ref addedRef);
                        extraCerts[extraIdx] = handle.DangerousGetHandle();
                    }

                    _chainContext = Interop.AndroidCrypto.X509ChainCreateContext(
                        ((AndroidCertificatePal)cert).SafeHandle,
                        extraCerts,
                        extraCerts.Length);

                    if (useCustomRootTrust)
                    {
                        // Android does not support an empty set of trust anchors
                        if (customTrustCertHandles.Count == 0)
                        {
                            throw new PlatformNotSupportedException(SR.Chain_EmptyCustomTrustNotSupported);
                        }

                        IntPtr[] customTrustCerts = new IntPtr[customTrustCertHandles.Count];
                        for (customIdx = 0; customIdx < customTrustCertHandles.Count; customIdx++)
                        {
                            SafeHandle handle = customTrustCertHandles[customIdx];
                            bool addedRef = false;
                            handle.DangerousAddRef(ref addedRef);
                            customTrustCerts[customIdx] = handle.DangerousGetHandle();
                        }

                        int res = Interop.AndroidCrypto.X509ChainSetCustomTrustStore(_chainContext, customTrustCerts, customTrustCerts.Length);
                        if (res != 1)
                        {
                            throw new CryptographicException();
                        }
                    }
                }
                finally
                {
                    for (extraIdx -= 1; extraIdx >= 0; extraIdx--)
                    {
                        extraCertHandles[extraIdx].DangerousRelease();
                    }

                    for (customIdx -= 1; customIdx >= 0; customIdx--)
                    {
                        customTrustCertHandles[customIdx].DangerousRelease();
                    }
                }
            }

            internal void Evaluate(
                DateTime verificationTime,
                OidCollection applicationPolicy,
                OidCollection certificatePolicy,
                X509RevocationMode revocationMode,
                X509RevocationFlag revocationFlag)
            {
                Debug.Assert(_chainContext != null);

                long timeInMsFromUnixEpoch = new DateTimeOffset(verificationTime).ToUnixTimeMilliseconds();
                _isValid = Interop.AndroidCrypto.X509ChainBuild(_chainContext, timeInMsFromUnixEpoch);
                if (!_isValid)
                {
                    // Android always validates name, time, signature, and trusted root.
                    // There is no way bypass that validation and build a path.
                    ChainElements = Array.Empty<X509ChainElement>();

                    Interop.AndroidCrypto.ValidationError[] errors = Interop.AndroidCrypto.X509ChainGetErrors(_chainContext);
                    var chainStatus = new X509ChainStatus[errors.Length];
                    for (int i = 0; i < errors.Length; i++)
                    {
                        Interop.AndroidCrypto.ValidationError error = errors[i];
                        chainStatus[i] = ValidationErrorToChainStatus(error);
                        Marshal.FreeHGlobal(error.Message);
                    }

                    ChainStatus = chainStatus;
                    return;
                }

                byte checkedRevocation;
                int res = Interop.AndroidCrypto.X509ChainValidate(_chainContext, revocationMode, revocationFlag, out checkedRevocation);
                if (res != 1)
                    throw new CryptographicException();

                X509Certificate2[] certs = Interop.AndroidCrypto.X509ChainGetCertificates(_chainContext);
                List<X509ChainStatus> overallStatus = new List<X509ChainStatus>();
                List<X509ChainStatus>[] statuses = new List<X509ChainStatus>[certs.Length];

                // Android will stop checking after the first error it hits, so we track the first
                // instances of revocation and non-revocation errors to fix-up the status of elements
                // beyond the first error
                int firstNonRevocationErrorIndex = -1;
                int firstRevocationErrorIndex = -1;
                Dictionary<int, List<X509ChainStatus>> errorsByIndex = GetStatusByIndex(_chainContext);
                foreach (int index in errorsByIndex.Keys)
                {
                    List<X509ChainStatus> errors = errorsByIndex[index];
                    for (int i = 0; i < errors.Count; i++)
                    {
                        X509ChainStatus status = errors[i];
                        AddUniqueStatus(overallStatus, ref status);
                    }

                    // -1 indicates that error is not tied to a specific index
                    if (index != -1)
                    {
                        statuses[index] = errorsByIndex[index];
                        if (errorsByIndex[index].Exists(s => s.Status == X509ChainStatusFlags.Revoked || s.Status == X509ChainStatusFlags.RevocationStatusUnknown))
                        {
                            firstRevocationErrorIndex = Math.Max(index, firstRevocationErrorIndex);
                        }
                        else
                        {
                            firstNonRevocationErrorIndex = Math.Max(index, firstNonRevocationErrorIndex);
                        }
                    }
                }

                if (firstNonRevocationErrorIndex > 0)
                {
                    // Assign PartialChain to everything from the first non-revocation error to the end certificate
                    X509ChainStatus partialChainStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.PartialChain,
                        StatusInformation = SR.Chain_PartialChain,
                    };
                    AddStatusFromIndexToEndCertificate(firstNonRevocationErrorIndex - 1, ref partialChainStatus, statuses, overallStatus);
                }

                if (firstRevocationErrorIndex > 0)
                {
                    // Assign RevocationStatusUnknown to everything from the first revocation error to the end certificate
                    X509ChainStatus revocationUnknownStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.RevocationStatusUnknown,
                        StatusInformation = SR.Chain_RevocationStatusUnknown,
                    };
                    AddStatusFromIndexToEndCertificate(firstRevocationErrorIndex - 1, ref revocationUnknownStatus, statuses, overallStatus);
                }

                if (revocationMode != X509RevocationMode.NoCheck && checkedRevocation == 0)
                {
                    // Revocation checking was requested, but not performed (due to basic validation failing)
                    // Assign RevocationStatusUnknown to everything
                    X509ChainStatus revocationUnknownStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.RevocationStatusUnknown,
                        StatusInformation = SR.Chain_RevocationStatusUnknown,
                    };
                    AddStatusFromIndexToEndCertificate(statuses.Length - 1, ref revocationUnknownStatus, statuses, overallStatus);
                }

                if (!IsPolicyMatch(certs, applicationPolicy, certificatePolicy))
                {
                    // Assign NotValidForUsage to everything
                    X509ChainStatus policyFailStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.NotValidForUsage,
                        StatusInformation = SR.Chain_NoPolicyMatch,
                    };
                    AddStatusFromIndexToEndCertificate(statuses.Length - 1, ref policyFailStatus, statuses, overallStatus);
                }

                X509ChainElement[] elements = new X509ChainElement[certs.Length];
                for (int i = 0; i < certs.Length; i++)
                {
                    X509ChainStatus[] elementStatus = statuses[i] == null ? Array.Empty<X509ChainStatus>() : statuses[i].ToArray();
                    elements[i] = new X509ChainElement(certs[i], elementStatus, string.Empty);
                }

                ChainElements = elements;
                ChainStatus = overallStatus.ToArray();
            }

            private static void AddStatusFromIndexToEndCertificate(
                int index,
                ref X509ChainStatus statusToSet,
                List<X509ChainStatus>[] statuses,
                List<X509ChainStatus> overallStatus)
            {
                AddUniqueStatus(overallStatus, ref statusToSet);
                for (int i = index; i >= 0; i--)
                {
                    if (statuses[i] == null)
                    {
                        statuses[i] = new List<X509ChainStatus>();
                    }

                    AddUniqueStatus(statuses[i], ref statusToSet);
                }
            }

            private static void AddUniqueStatus(List<X509ChainStatus> list, ref X509ChainStatus status)
            {
                X509ChainStatusFlags statusFlags = status.Status;
                string statusInfo = status.StatusInformation;
                if (!list.Exists(s => s.Status == statusFlags && s.StatusInformation == statusInfo))
                {
                    list.Add(status);
                }
            }

            private static Dictionary<int, List<X509ChainStatus>> GetStatusByIndex(SafeX509ChainContextHandle ctx)
            {
                var statusByIndex = new Dictionary<int, List<X509ChainStatus>>();
                Interop.AndroidCrypto.ValidationError[] errors = Interop.AndroidCrypto.X509ChainGetErrors(ctx);
                for (int i = 0; i < errors.Length; i++)
                {
                    Interop.AndroidCrypto.ValidationError error = errors[i];
                    X509ChainStatus chainStatus = ValidationErrorToChainStatus(error);
                    Marshal.FreeHGlobal(error.Message);

                    if (!statusByIndex.ContainsKey(error.Index))
                    {
                        statusByIndex.Add(error.Index, new List<X509ChainStatus>());
                    }

                    statusByIndex[error.Index].Add(chainStatus);
                }

                return statusByIndex;
            }

            private static X509ChainStatus ValidationErrorToChainStatus(Interop.AndroidCrypto.ValidationError error)
            {
                X509ChainStatusFlags statusFlags = (X509ChainStatusFlags)error.Status;
                Debug.Assert(statusFlags != X509ChainStatusFlags.NoError);

                return new X509ChainStatus
                {
                    Status = statusFlags,
                    StatusInformation = Marshal.PtrToStringUni(error.Message)
                };
            }

            private static bool IsPolicyMatch(
                X509Certificate2[] certs,
                OidCollection? applicationPolicy,
                OidCollection? certificatePolicy)
            {
                bool hasApplicationPolicy = applicationPolicy != null && applicationPolicy.Count > 0;
                bool hasCertificatePolicy = certificatePolicy != null && certificatePolicy.Count > 0;

                if (!hasApplicationPolicy && !hasCertificatePolicy)
                    return true;

                List<X509Certificate2> certsToRead = new List<X509Certificate2>(certs);
                CertificatePolicyChain policyChain = new CertificatePolicyChain(certsToRead);
                if (hasCertificatePolicy && !policyChain.MatchesCertificatePolicies(certificatePolicy!))
                {
                    return false;
                }

                if (hasApplicationPolicy && !policyChain.MatchesApplicationPolicies(applicationPolicy!))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
