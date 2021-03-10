// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    if ((flags & X509VerificationFlags.IgnoreNotTimeValid) == X509VerificationFlags.IgnoreNotTimeValid)
                    {
                        // There is no way to bypass time validation on Android.
                        // It will not build any chain without a valid time.
                        exception = new PlatformNotSupportedException(
                            SR.Format(SR.Cryptography_VerificationFlagNotSupported, X509VerificationFlags.IgnoreNotTimeValid));
                        return default(bool?);
                    }

                    if ((flags & X509VerificationFlags.AllowUnknownCertificateAuthority) == X509VerificationFlags.AllowUnknownCertificateAuthority)
                    {
                        // There is no way to allow an untrusted root on Android.
                        // It will not build any chain without a trusted root.
                        exception = new PlatformNotSupportedException(
                            SR.Format(SR.Cryptography_VerificationFlagNotSupported, X509VerificationFlags.AllowUnknownCertificateAuthority));
                        return default(bool?);
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
                    IntPtr[] customTrustArray = customTrustCerts.ToArray();
                    Interop.AndroidCrypto.X509ChainSetCustomTrustStore(_chainContext, customTrustArray, customTrustArray.Length);
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
                _isValid = Interop.AndroidCrypto.X509ChainEvaluate(_chainContext, timeInMsFromUnixEpoch);
                if (!_isValid)
                {
                    ChainElements = Array.Empty<X509ChainElement>();
                    var status = new X509ChainStatus()
                    {
                        Status = X509ChainStatusFlags.PartialChain
                    };
                    ChainStatus = new X509ChainStatus[] { status };
                    return;
                }

                (X509Certificate2, List<X509ChainStatus>)[] results = GetValidationResults(_chainContext, applicationPolicy, certificatePolicy, revocationMode, revocationFlag);

                if (!IsPolicyMatch(results, applicationPolicy, certificatePolicy))
                {
                    X509ChainStatus policyFailStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.NotValidForUsage,
                        StatusInformation = SR.Chain_NoPolicyMatch,
                    };

                    for (int i = 0; i < results.Length; i++)
                    {
                        results[i].Item2.Add(policyFailStatus);
                    }
                }

                X509ChainElement[] elements = new X509ChainElement[results.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    X509Certificate2 cert = results[i].Item1;
                    elements[i] = new X509ChainElement(cert, results[i].Item2.ToArray(), string.Empty);
                }

                ChainElements = elements;
            }

            private static (X509Certificate2, List<X509ChainStatus>)[] GetValidationResults(
                SafeX509ChainContextHandle ctx,
                OidCollection applicationPolicy,
                OidCollection certificatePolicy,
                X509RevocationMode revocationMode,
                X509RevocationFlag revocationFlag)
            {
                IntPtr[] certPtrs = Interop.AndroidCrypto.X509ChainGetCertificates(ctx);
                var results = new (X509Certificate2, List<X509ChainStatus>)[certPtrs.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i].Item1 = new X509Certificate2(certPtrs[i]);
                    results[i].Item2 = new List<X509ChainStatus>();
                }

                int success = Interop.AndroidCrypto.X509ChainValidate(ctx, revocationMode, revocationFlag);
                if (success != 1)
                {
                    // TODO: [AndroidCrypto] Get actual validation errors
                    X509ChainStatus status = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.PartialChain,
                    };

                    for (int i = 0; i < results.Length; i++)
                    {
                        results[i].Item2.Add(status);
                    }
                }

                return results;
            }

            private static bool IsPolicyMatch(
                (X509Certificate2, List<X509ChainStatus>)[] results,
                OidCollection? applicationPolicy,
                OidCollection? certificatePolicy)
            {
                bool hasApplicationPolicy = applicationPolicy != null && applicationPolicy.Count > 0;
                bool hasCertificatePolicy = certificatePolicy != null && certificatePolicy.Count > 0;

                if (!hasApplicationPolicy && !hasCertificatePolicy)
                    return true;

                List<X509Certificate2> certsToRead = new List<X509Certificate2>(results.Length);
                for (int i = 0; i < results.Length; i++)
                {
                    certsToRead.Add(results[i].Item1);
                }

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
