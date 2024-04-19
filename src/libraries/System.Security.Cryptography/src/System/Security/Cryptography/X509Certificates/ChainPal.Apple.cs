// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SecTrustChainPal : IChainPal
    {
        private const X509ChainStatusFlags RevocationRelevantFlags =
            X509ChainStatusFlags.RevocationStatusUnknown |
            X509ChainStatusFlags.Revoked |
            X509ChainStatusFlags.OfflineRevocation;

        private static readonly SafeCreateHandle s_emptyArray = Interop.CoreFoundation.CFArrayCreate(Array.Empty<IntPtr>(), UIntPtr.Zero);
        private Stack<SafeHandle> _extraHandles;
        private SafeX509ChainHandle? _chainHandle;
        public X509ChainElement[]? ChainElements { get; private set; }
        public X509ChainStatus[]? ChainStatus { get; private set; }
        private DateTime _verificationTime;
        private X509RevocationMode _revocationMode;

        internal SecTrustChainPal()
        {
            _extraHandles = new Stack<SafeHandle>();
        }

        public SafeX509ChainHandle? SafeHandle => null;

        internal void OpenTrustHandle(
            ICertificatePal leafCert,
            X509Certificate2Collection? extraStore,
            X509RevocationMode revocationMode,
            X509Certificate2Collection? customTrustStore,
            X509ChainTrustMode trustMode)
        {
            _revocationMode = revocationMode;
            SafeCreateHandle policiesArray = PreparePoliciesArray(revocationMode != X509RevocationMode.NoCheck);
            SafeCreateHandle certsArray = PrepareCertsArray(leafCert, extraStore, customTrustStore, trustMode);

            int osStatus;

            SafeX509ChainHandle chain;
            int ret = Interop.AppleCrypto.AppleCryptoNative_X509ChainCreate(
                certsArray,
                policiesArray,
                out chain,
                out osStatus);

            if (ret == 1)
            {
                if (trustMode == X509ChainTrustMode.CustomRootTrust)
                {
                    SafeCreateHandle customCertsArray = s_emptyArray;
                    if (customTrustStore != null && customTrustStore.Count > 0)
                    {
                        customCertsArray = PrepareCustomCertsArray(customTrustStore);
                    }

                    try
                    {
                        int error = Interop.AppleCrypto.X509ChainSetTrustAnchorCertificates(chain, customCertsArray);
                        if (error != 0)
                        {
                            throw Interop.AppleCrypto.CreateExceptionForOSStatus(error);
                        }
                    }
                    finally
                    {
                        if (customCertsArray != s_emptyArray)
                        {
                            customCertsArray.Dispose();
                        }
                    }
                }

                _chainHandle = chain;
                return;
            }

            chain.Dispose();

            if (ret == 0)
            {
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_X509ChainCreate returned unexpected return value {ret}");
            throw new CryptographicException();
        }

        public void Dispose()
        {
            if (_extraHandles == null)
                return;

            Stack<SafeHandle> extraHandles = _extraHandles;
            _extraHandles = null!;

            _chainHandle?.Dispose();

            while (extraHandles.Count > 0)
            {
                extraHandles.Pop().Dispose();
            }
        }

        public bool? Verify(X509VerificationFlags flags, out Exception? exception)
        {
            exception = null;

            return UnixChainVerifier.Verify(ChainElements!, flags);
        }

        private SafeCreateHandle PreparePoliciesArray(bool checkRevocation)
        {
            Span<IntPtr> policies = checkRevocation ? stackalloc IntPtr[2] : stackalloc IntPtr[1];

            SafeHandle defaultPolicy = Interop.AppleCrypto.X509ChainCreateDefaultPolicy();

            if (defaultPolicy.IsInvalid)
            {
                defaultPolicy.Dispose();
                throw new PlatformNotSupportedException(nameof(X509Chain));
            }

            _extraHandles.Push(defaultPolicy);
            policies[0] = defaultPolicy.DangerousGetHandle();

            if (checkRevocation)
            {
                SafeHandle revPolicy = Interop.AppleCrypto.X509ChainCreateRevocationPolicy();
                _extraHandles.Push(revPolicy);
                policies[1] = revPolicy.DangerousGetHandle();
            }

            SafeCreateHandle policiesArray = Interop.CoreFoundation.CFArrayCreate(policies);

            _extraHandles.Push(policiesArray);
            return policiesArray;
        }

        private SafeCreateHandle PrepareCertsArray(
            ICertificatePal cert,
            X509Certificate2Collection? extraStore,
            X509Certificate2Collection? customTrustStore,
            X509ChainTrustMode trustMode)
        {
            List<SafeHandle> safeHandles = new List<SafeHandle> { ((AppleCertificatePal)cert).CertificateHandle };

            if (extraStore != null)
            {
                for (int i = 0; i < extraStore.Count; i++)
                {
                    safeHandles.Add(((AppleCertificatePal)extraStore[i].Pal).CertificateHandle);
                }
            }

            if (trustMode == X509ChainTrustMode.CustomRootTrust && customTrustStore != null)
            {
                for (int i = 0; i < customTrustStore.Count; i++)
                {
                    // Only adds non self issued certs to the untrusted certs array. Trusted self signed
                    // certs will be added to the custom certs array.
                    if (!customTrustStore[i].SubjectName.RawData.ContentsEqual(customTrustStore[i].IssuerName.RawData))
                    {
                        safeHandles.Add(((AppleCertificatePal)customTrustStore[i].Pal).CertificateHandle);
                    }
                }
            }

            return GetCertsArray(safeHandles);
        }

        private SafeCreateHandle PrepareCustomCertsArray(X509Certificate2Collection customTrustStore)
        {
            List<SafeHandle> rootCertificates = new List<SafeHandle>();
            foreach (X509Certificate2 cert in customTrustStore)
            {
                if (cert.SubjectName.RawData.ContentsEqual(cert.IssuerName.RawData))
                {
                    rootCertificates.Add(((AppleCertificatePal)cert.Pal).CertificateHandle);
                }
            }

            return GetCertsArray(rootCertificates);
        }

        private SafeCreateHandle GetCertsArray(List<SafeHandle> safeHandles)
        {
            int idx = 0;

            try
            {
                int handlesCount = safeHandles.Count;
                IntPtr[] ptrs = new IntPtr[handlesCount];
                for (; idx < handlesCount; idx++)
                {
                    SafeHandle handle = safeHandles[idx];
                    bool addedRef = false;
                    handle.DangerousAddRef(ref addedRef);
                    ptrs[idx] = handle.DangerousGetHandle();
                }

                // Creating the array has the effect of calling CFRetain() on all of the pointers, so the native
                // resource is safe even if we DangerousRelease=>ReleaseHandle them.
                SafeCreateHandle certsArray = Interop.CoreFoundation.CFArrayCreate(ptrs, (UIntPtr)ptrs.Length);
                _extraHandles.Push(certsArray);
                return certsArray;
            }
            finally
            {
                for (idx--; idx >= 0; idx--)
                {
                    safeHandles[idx].DangerousRelease();
                }
            }
        }

        internal void Execute(
            DateTime verificationTime,
            bool allowNetwork,
            OidCollection? applicationPolicy,
            OidCollection? certificatePolicy,
            X509RevocationFlag revocationFlag)
        {
            int osStatus;

            // Save the time code for determining which message to load for NotTimeValid.
            _verificationTime = verificationTime;
            int ret;

            using (SafeCFDateHandle cfEvaluationTime = Interop.CoreFoundation.CFDateCreate(verificationTime))
            {
                ret = Interop.AppleCrypto.AppleCryptoNative_X509ChainEvaluate(
                    _chainHandle!,
                    cfEvaluationTime,
                    allowNetwork,
                    out osStatus);
            }

            if (ret == 0)
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

            if (ret != 1)
            {
                Debug.Fail($"AppleCryptoNative_X509ChainEvaluate returned unknown result {ret}");
                throw new CryptographicException();
            }

            (X509Certificate2, int)[] elements = ParseResults(_chainHandle!, _revocationMode);
            Debug.Assert(elements.Length > 0);

            if (!IsPolicyMatch(elements, applicationPolicy, certificatePolicy))
            {
                for (int i = 0; i < elements.Length; i++)
                {
                    (X509Certificate2, int) currentValue = elements[i];

                    elements[i] = (currentValue.Item1, currentValue.Item2 | (int)X509ChainStatusFlags.NotValidForUsage);
                }
            }

            FixupRevocationStatus(elements, revocationFlag);
            BuildAndSetProperties(elements);
        }

        private static (X509Certificate2, int)[] ParseResults(
            SafeX509ChainHandle chainHandle,
            X509RevocationMode revocationMode)
        {
            long elementCount = Interop.AppleCrypto.X509ChainGetChainSize(chainHandle);
            var elements = new (X509Certificate2, int)[elementCount];

            using (var trustResults = Interop.AppleCrypto.X509ChainGetTrustResults(chainHandle))
            {
                for (long elementIdx = 0; elementIdx < elementCount; elementIdx++)
                {
                    IntPtr certHandle =
                        Interop.AppleCrypto.X509ChainGetCertificateAtIndex(chainHandle, elementIdx);

                    int dwStatus;
                    int ret = Interop.AppleCrypto.X509ChainGetStatusAtIndex(trustResults, elementIdx, out dwStatus);

                    // A return value of zero means no errors happened in locating the status (negative) or in
                    // parsing the status (positive).
                    if (ret != 0)
                    {
                        Debug.Fail($"X509ChainGetStatusAtIndex returned unexpected error {ret}");
                        throw new CryptographicException();
                    }

                    X509Certificate2 cert = new X509Certificate2(certHandle);

                    FixupStatus(cert, revocationMode, ref dwStatus);

                    elements[elementIdx] = (cert, dwStatus);
                }
            }

            return elements;
        }

        private static bool IsPolicyMatch(
            (X509Certificate2, int)[] elements,
            OidCollection? applicationPolicy,
            OidCollection? certificatePolicy)
        {
            if (applicationPolicy?.Count > 0 || certificatePolicy?.Count > 0)
            {
                List<X509Certificate2> certsToRead = new List<X509Certificate2>();

                for (int i = 0; i < elements.Length; i++)
                {
                    certsToRead.Add(elements[i].Item1);
                }

                CertificatePolicyChain policyChain = new CertificatePolicyChain(certsToRead);

                if (certificatePolicy?.Count > 0)
                {
                    if (!policyChain.MatchesCertificatePolicies(certificatePolicy))
                    {
                        return false;
                    }
                }

                if (applicationPolicy?.Count > 0)
                {
                    if (!policyChain.MatchesApplicationPolicies(applicationPolicy))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void BuildAndSetProperties((X509Certificate2, int)[] elementTuples)
        {
            X509ChainElement[] elements = new X509ChainElement[elementTuples.Length];
            int allStatus = 0;

            for (int i = 0; i < elementTuples.Length; i++)
            {
                (X509Certificate2 cert, int chainStatus) = elementTuples[i];

                elements[i] = new X509ChainElement(cert, BuildChainElementStatuses(cert, chainStatus), "");
                allStatus |= chainStatus;
            }

            ChainElements = elements;
            ChainStatus = BuildChainElementStatuses(null, allStatus);
        }

        private static void FixupRevocationStatus(
            (X509Certificate2, int)[] elements,
            X509RevocationFlag revocationFlag)
        {
            if (revocationFlag == X509RevocationFlag.ExcludeRoot)
            {
                // When requested
                int idx = elements.Length - 1;
                (X509Certificate2, int) element = elements[idx];
                X509ChainStatusFlags statusFlags = (X509ChainStatusFlags)element.Item2;

                // Apple will terminate the chain at the first "root" or "trustAsRoot" certificate
                // it finds, which it refers to as "anchors". We'll consider a "trustAsRoot" cert
                // as a root for the purposes of ExcludeRoot. So as long as the last element doesn't
                // have PartialChain consider it the root.
                if ((statusFlags & X509ChainStatusFlags.PartialChain) == 0)
                {
                    statusFlags &= ~RevocationRelevantFlags;
                    elements[idx] = (element.Item1, (int)statusFlags);
                }
            }
            else if (revocationFlag == X509RevocationFlag.EndCertificateOnly)
            {
                // In Windows the EndCertificateOnly flag (CERT_CHAIN_REVOCATION_CHECK_END_CERT) will apply
                // to a root if that's the only element, so we'll do the same.
                // Start at element 1, and move to the end.
                for (int i = 1; i < elements.Length; i++)
                {
                    (X509Certificate2, int) element = elements[i];
                    X509ChainStatusFlags statusFlags = (X509ChainStatusFlags)element.Item2;

                    statusFlags &= ~RevocationRelevantFlags;
                    elements[i] = (element.Item1, (int)statusFlags);
                }
            }
        }

        private static void FixupStatus(
            X509Certificate2 cert,
            X509RevocationMode revocationMode,
            ref int dwStatus)
        {
            X509ChainStatusFlags flags = (X509ChainStatusFlags)dwStatus;

            if ((flags & X509ChainStatusFlags.UntrustedRoot) != 0)
            {
                X509ChainStatusFlags newFlag = FindUntrustedRootReason(cert);

                if (newFlag != X509ChainStatusFlags.UntrustedRoot)
                {
                    flags &= ~X509ChainStatusFlags.UntrustedRoot;
                    flags |= newFlag;

                    dwStatus = (int)flags;
                }
            }

            if (revocationMode == X509RevocationMode.NoCheck)
            {
                // Clear any revocation-related flags if NoCheck was requested, since
                // the OS may use cached results opportunistically.
                flags &= ~RevocationRelevantFlags;

                dwStatus = (int)flags;
            }
        }

        private static X509ChainStatusFlags FindUntrustedRootReason(X509Certificate2 cert)
        {
            // UntrustedRoot comes back for at least the following reasons:
            // 1. The parent cert could not be found (no network, no AIA, etc) (PartialChain)
            // 2. The root cert was found, and wasn't trusted (UntrustedRoot)
            // 3. The certificate was tampered with, so the parent was declared invalid.
            //
            // In the #3 case we'd like to call it NotSignatureValid, but since we didn't get
            // the parent certificate we can't recompute that, so it'll just get called
            // PartialChain.
            if (!cert.SubjectName.RawData.ContentsEqual(cert.IssuerName.RawData))
            {
                return X509ChainStatusFlags.PartialChain;
            }

            // Okay, so we're looking at a self-signed certificate.
            // What are some situations?
            // 1. A private / new root certificate was matched which is not trusted (UntrustedRoot)
            // 2. A valid root certificate is tampered with (NotSignatureValid)
            // 3. A valid certificate is created which has the same subject name as
            //    an existing root cert (UntrustedRoot)
            //
            // To a user, case 2 and 3 aren't really distinguishable:
            // "What do you mean [my favorite CA] isn't trusted?".
            // NotSignatureValid would reveal the tamper, but since whoever was tampering can
            // easily re-sign a self-signed cert, it's not worth duplicating the signature
            // computation here.
            return X509ChainStatusFlags.UntrustedRoot;
        }

        private X509ChainStatus[] BuildChainElementStatuses(X509Certificate2? cert, int dwStatus)
        {
            if (dwStatus == 0)
            {
                return Array.Empty<X509ChainStatus>();
            }

            List<X509ChainStatus> statuses = new List<X509ChainStatus>();
            X509ChainStatusFlags flags = (X509ChainStatusFlags)dwStatus;

            foreach (X509ChainErrorMapping mapping in X509ChainErrorMapping.s_chainErrorMappings)
            {
                if ((mapping.ChainStatusFlag & flags) == mapping.ChainStatusFlag)
                {
                    int osStatus;
                    string? errorString;

                    // Disambiguate the NotTimeValid code to get the right string.
                    if (mapping.ChainStatusFlag == X509ChainStatusFlags.NotTimeValid)
                    {
                        const int errSecCertificateExpired = -67818;
                        const int errSecCertificateNotValidYet = -67819;

                        osStatus = cert != null && cert.NotBefore > _verificationTime ?
                            errSecCertificateNotValidYet :
                            errSecCertificateExpired;
                        errorString = Interop.AppleCrypto.GetSecErrorString(osStatus);
                    }
                    else
                    {
                        errorString = mapping.ErrorString;
                    }

                    statuses.Add(
                        new X509ChainStatus
                        {
                            Status = mapping.ChainStatusFlag,
                            StatusInformation = errorString
                        });
                }
            }

            return statuses.ToArray();
        }

        private readonly struct X509ChainErrorMapping
        {
            internal static readonly X509ChainErrorMapping[] s_chainErrorMappings =
            {
                new X509ChainErrorMapping(X509ChainStatusFlags.NotTimeValid),
                new X509ChainErrorMapping(X509ChainStatusFlags.NotTimeNested),
                new X509ChainErrorMapping(X509ChainStatusFlags.Revoked),
                new X509ChainErrorMapping(X509ChainStatusFlags.NotSignatureValid),
                new X509ChainErrorMapping(X509ChainStatusFlags.NotValidForUsage),
                new X509ChainErrorMapping(X509ChainStatusFlags.UntrustedRoot),
                new X509ChainErrorMapping(X509ChainStatusFlags.RevocationStatusUnknown),
                new X509ChainErrorMapping(X509ChainStatusFlags.Cyclic),
                new X509ChainErrorMapping(X509ChainStatusFlags.InvalidExtension),
                new X509ChainErrorMapping(X509ChainStatusFlags.InvalidPolicyConstraints),
                new X509ChainErrorMapping(X509ChainStatusFlags.InvalidBasicConstraints),
                new X509ChainErrorMapping(X509ChainStatusFlags.InvalidNameConstraints),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasNotSupportedNameConstraint),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasNotDefinedNameConstraint),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasNotPermittedNameConstraint),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasExcludedNameConstraint),
                new X509ChainErrorMapping(X509ChainStatusFlags.PartialChain),
                new X509ChainErrorMapping(X509ChainStatusFlags.CtlNotTimeValid),
                new X509ChainErrorMapping(X509ChainStatusFlags.CtlNotSignatureValid),
                new X509ChainErrorMapping(X509ChainStatusFlags.CtlNotValidForUsage),
                new X509ChainErrorMapping(X509ChainStatusFlags.OfflineRevocation),
                new X509ChainErrorMapping(X509ChainStatusFlags.NoIssuanceChainPolicy),
                new X509ChainErrorMapping(X509ChainStatusFlags.ExplicitDistrust),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasNotSupportedCriticalExtension),
                new X509ChainErrorMapping(X509ChainStatusFlags.HasWeakSignature),
            };

            internal readonly X509ChainStatusFlags ChainStatusFlag;
            internal readonly int OSStatus;
            internal readonly string? ErrorString;

            private X509ChainErrorMapping(X509ChainStatusFlags flag)
            {
                ChainStatusFlag = flag;
                OSStatus = Interop.AppleCrypto.GetOSStatusForChainStatus(flag);
                ErrorString = Interop.AppleCrypto.GetSecErrorString(OSStatus);
            }
        }
    }

    internal sealed partial class ChainPal
    {
#pragma warning disable IDE0060
        internal static partial IChainPal FromHandle(IntPtr chainContext)
        {
            // This is possible to do on Apple's platform, but is tricky in execution.
            // In Windows, CertGetCertificateChain is what allocates the handle, and it does
            // * Chain building
            // * Revocation checking as directed
            // But notably does not apply any policy rules (TLS hostname matching, etc), or
            // even inquire as to what policies should be applied.
            //
            // On Apple, the order is reversed.  Creating the SecTrust(Ref) object requires
            // the policy to match against, but when the handle is created it might not have
            // built the chain.  Then a call to SecTrustEvaluate actually does the chain building.
            //
            // This means that Windows never had to handle the "unevaluated chain" pointer, but
            // on Apple we would.  And so it means that the .NET API doesn't understand it can be in
            // that state.
            // * Is that an exception on querying the status or elements?
            // * An exception in this call chain (new X509Chain(IntPtr))?
            // * Should we build the chain on first data query?
            // * Should we build the chain now?
            //
            // The only thing that is known is that if this method succeeds it does not take ownership
            // of the handle.  So it should CFRetain the handle and let the PAL object's SafeHandle
            // still Dispose/CFRelease.
            //
            // For now, just PNSE, it didn't work when we used OpenSSL, and we can add this when we
            // decide what it should do.
            throw new PlatformNotSupportedException();
        }

        internal static partial bool ReleaseSafeX509ChainHandle(IntPtr handle)
        {
            Interop.CoreFoundation.CFRelease(handle);
            return true;
        }

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
            // If the time was given in Universal, it will stay Universal.
            // If the time was given in Local, it will be converted.
            // If the time was given in Unspecified, it will be assumed local, and converted.
            //
            // This matches the "assume Local unless explicitly Universal" implicit contract.
            verificationTime = verificationTime.ToUniversalTime();

            SecTrustChainPal chainPal = new SecTrustChainPal();

            // The allowNetwork controls all network activity for macOS chain building.
            // There is no way to independently enable or disable online revocation checking
            // and AIA fetching. If the caller specifies they want Online revocation checking,
            // then we need to allow network operations (including AIA fetching.)
            bool revocationRequiresNetwork = revocationMode != X509RevocationMode.NoCheck;

            try
            {
                chainPal.OpenTrustHandle(
                    cert,
                    extraStore,
                    revocationMode,
                    customTrustStore,
                    trustMode);

                chainPal.Execute(
                    verificationTime,
                    allowNetwork: !disableAia || revocationRequiresNetwork,
                    applicationPolicy,
                    certificatePolicy,
                    revocationFlag);
            }
            catch
            {
                chainPal.Dispose();
                throw;
            }

            return chainPal;
        }
#pragma warning restore IDE0060
    }
}
