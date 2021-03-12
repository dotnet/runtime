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
                    // There is no way to bypass time, trusted root, name, or policy constraint
                    // validation on Android. It will not build any chain without these all
                    // being valid.
                    X509VerificationFlags[] unsupportedFlags = new X509VerificationFlags[]
                    {
                        X509VerificationFlags.IgnoreNotTimeValid,
                        X509VerificationFlags.AllowUnknownCertificateAuthority,
                        X509VerificationFlags.IgnoreInvalidName,
                        X509VerificationFlags.IgnoreInvalidPolicy,
                    };
                    foreach (X509VerificationFlags unsupported in unsupportedFlags)
                    {
                        if ((flags & unsupported) == unsupported)
                        {
                            exception = new PlatformNotSupportedException(SR.Format(SR.Chain_VerificationFlagNotSupported, unsupported));
                            return default(bool?);
                        }
                    }

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
                List<IntPtr> extraCerts = new List<IntPtr>() { cert.Handle };
                if (extraStore != null)
                {
                    foreach (X509Certificate2 extraCert in extraStore)
                    {
                        extraCerts.Add(extraCert.Pal.Handle);
                    }
                }

                List<IntPtr> customTrustCerts = new List<IntPtr>();
                bool useCustomRootTrust = trustMode == X509ChainTrustMode.CustomRootTrust;
                if (useCustomRootTrust && customTrustStore != null)
                {
                    foreach (X509Certificate2 custom in customTrustStore)
                    {
                        IntPtr certHandle = custom.Pal.Handle;
                        if (custom.SubjectName.RawData.ContentsEqual(custom.IssuerName.RawData))
                        {
                            // Add self-issued certs to custom root trust cert
                            customTrustCerts.Add(certHandle);
                        }
                        else
                        {
                            // Add non-self-issued certs to extra certs
                            extraCerts.Add(certHandle);
                        }
                    }
                }

                IntPtr[] extraArray = extraCerts.ToArray();
                _chainContext = Interop.AndroidCrypto.X509ChainCreateContext(
                    ((AndroidCertificatePal)cert).SafeHandle,
                    extraArray,
                    extraArray.Length);

                if (useCustomRootTrust)
                {
                    // Android does not support an empty set of trust anchors
                    if (customTrustCerts.Count == 0)
                    {
                        throw new PlatformNotSupportedException(SR.Chain_EmptyCustomTrustNotSupported);
                    }

                    IntPtr[] customTrustArray = customTrustCerts.ToArray();
                    int res = Interop.AndroidCrypto.X509ChainSetCustomTrustStore(_chainContext, customTrustArray, customTrustArray.Length);
                    if (res != 1)
                    {
                        throw new CryptographicException();
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

                if (revocationMode != X509RevocationMode.NoCheck)
                {
                    if (revocationFlag == X509RevocationFlag.EntireChain)
                    {
                        throw new NotImplementedException($"{nameof(Evaluate)} (X509RevocationFlag.{revocationFlag})");
                    }

                    if (!Interop.AndroidCrypto.X509ChainSupportsRevocationOptions())
                    {
                        if (revocationFlag == X509RevocationFlag.EndCertificateOnly)
                        {
                            // No way to specfiy end certificate only if revocation options are not available
                            throw new PlatformNotSupportedException(SR.Format(SR.Chain_SettingNotSupported, $"{nameof(X509RevocationFlag)}.{nameof(X509RevocationFlag.EndCertificateOnly)}"));
                        }

                        // Defaults to offline when revocation options are not available
                        if (revocationMode == X509RevocationMode.Online)
                        {
                            throw new NotImplementedException($"{nameof(Evaluate)} (X509RevocationMode.{revocationMode})");
                        }
                    }
                }

                int res = Interop.AndroidCrypto.X509ChainValidate(_chainContext, revocationMode, revocationFlag);
                if (res != 1)
                    throw new CryptographicException();

                X509Certificate2[] certs = Interop.AndroidCrypto.X509ChainGetCertificates(_chainContext);
                List<X509ChainStatus> overallStatus = new List<X509ChainStatus>();
                List<X509ChainStatus>[] statuses = new List<X509ChainStatus>[certs.Length];

                int firstErrorIndex = -1;
                Dictionary<int, List<X509ChainStatus>> errorsByIndex = GetStatusByIndex(_chainContext);
                foreach (int index in errorsByIndex.Keys)
                {
                    overallStatus.AddRange(errorsByIndex[index]);

                    // -1 indicates that error is not tied to a specific index
                    if (index != -1)
                    {
                        statuses[index] = errorsByIndex[index];
                        firstErrorIndex = Math.Max(index, firstErrorIndex);
                    }
                }

                // Android will stop checking after the first error it hits, so we explicitly
                // assign PartialChain to everything from the first error to the end certificate
                if (firstErrorIndex > 0)
                {
                    X509ChainStatus partialChainStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.PartialChain,
                        StatusInformation = SR.Chain_PartialChain,
                    };
                    for (int i = firstErrorIndex - 1; i >= 0; i--)
                    {
                        if (statuses[i] == null)
                        {
                            statuses[i] = new List<X509ChainStatus>();
                        }

                        statuses[i].Add(partialChainStatus);
                    }
                }

                if (!IsPolicyMatch(certs, applicationPolicy, certificatePolicy))
                {
                    X509ChainStatus policyFailStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.NotValidForUsage,
                        StatusInformation = SR.Chain_NoPolicyMatch,
                    };

                    for (int i = 0; i < statuses.Length; i++)
                    {
                        if (statuses[i] == null)
                        {
                            statuses[i] = new List<X509ChainStatus>();
                        }

                        statuses[i].Add(policyFailStatus);
                    }

                    overallStatus.Add(policyFailStatus);
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
                if (statusFlags == X509ChainStatusFlags.NoError)
                {
                    // Android returns NoError as the error status when it cannot determine the status
                    // We just map that to partial chain.
                    statusFlags = X509ChainStatusFlags.PartialChain;
                }

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
