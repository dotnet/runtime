// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslX509ChainProcessor : IChainPal
    {
        // Constructed (0x20) | Sequence (0x10) => 0x30.
        private const uint ConstructedSequenceTagId = 0x30;

        public void Dispose()
        {
        }

        public bool? Verify(X509VerificationFlags flags, out Exception exception)
        {
            exception = null;
            return ChainVerifier.Verify(ChainElements, flags);
        }

        public X509ChainElement[] ChainElements { get; private set; }
        public X509ChainStatus[] ChainStatus { get; private set; }

        public SafeX509ChainHandle SafeHandle
        {
            get { return null; }
        }

        public static IChainPal BuildChain(
            X509Certificate2 leaf,
            HashSet<X509Certificate2> candidates,
            HashSet<X509Certificate2> systemTrusted,
            OidCollection applicationPolicy,
            OidCollection certificatePolicy,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            DateTime verificationTime,
            ref TimeSpan remainingDownloadTime)
        {
            X509ChainElement[] elements;
            List<X509ChainStatus> overallStatus = new List<X509ChainStatus>();
            WorkingChain workingChain = new WorkingChain();
            Interop.Crypto.X509StoreVerifyCallback workingCallback = workingChain.VerifyCallback;

            // An X509_STORE is more comparable to Cryptography.X509Certificate2Collection than to
            // Cryptography.X509Store. So read this with OpenSSL eyes, not CAPI/CNG eyes.
            //
            // (If you need to think of it as an X509Store, it's a volatile memory store)
            using (SafeX509StoreHandle store = Interop.Crypto.X509StoreCreate())
            using (SafeX509StoreCtxHandle storeCtx = Interop.Crypto.X509StoreCtxCreate())
            using (SafeX509StackHandle extraCerts = Interop.Crypto.NewX509Stack())
            {
                Interop.Crypto.CheckValidOpenSslHandle(store);
                Interop.Crypto.CheckValidOpenSslHandle(storeCtx);
                Interop.Crypto.CheckValidOpenSslHandle(extraCerts);

                bool lookupCrl = revocationMode != X509RevocationMode.NoCheck;

                foreach (X509Certificate2 cert in candidates)
                {
                    OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)cert.Pal;

                    using (SafeX509Handle handle = Interop.Crypto.X509UpRef(pal.SafeHandle))
                    {
                        if (!Interop.Crypto.PushX509StackField(extraCerts, handle))
                        {
                            throw Interop.Crypto.CreateOpenSslCryptographicException();
                        }

                        // Ownership was transferred to the cert stack.
                        handle.SetHandleAsInvalid();
                    }

                    if (lookupCrl)
                    {
                        CrlCache.AddCrlForCertificate(
                            cert,
                            store,
                            revocationMode,
                            verificationTime,
                            ref remainingDownloadTime);

                        // If we only wanted the end-entity certificate CRL then don't look up
                        // any more of them.
                        lookupCrl = revocationFlag != X509RevocationFlag.EndCertificateOnly;
                    }
                }

                if (revocationMode != X509RevocationMode.NoCheck)
                {
                    if (!Interop.Crypto.X509StoreSetRevocationFlag(store, revocationFlag))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                foreach (X509Certificate2 trustedCert in systemTrusted)
                {
                    OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)trustedCert.Pal;

                    if (!Interop.Crypto.X509StoreAddCert(store, pal.SafeHandle))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                SafeX509Handle leafHandle = ((OpenSslX509CertificateReader)leaf.Pal).SafeHandle;

                if (!Interop.Crypto.X509StoreCtxInit(storeCtx, store, leafHandle, extraCerts))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                Interop.Crypto.X509StoreCtxSetVerifyCallback(storeCtx, workingCallback);
                Interop.Crypto.SetX509ChainVerifyTime(storeCtx, verificationTime);

                int verify = Interop.Crypto.X509VerifyCert(storeCtx);

                if (verify < 0)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // Because our callback tells OpenSSL that every problem is ignorable, it should tell us that the
                // chain is just fine (unless it returned a negative code for an exception)
                Debug.Assert(verify == 1, "verify == 1");

                using (SafeX509StackHandle chainStack = Interop.Crypto.X509StoreCtxGetChain(storeCtx))
                {
                    int chainSize = Interop.Crypto.GetX509StackFieldCount(chainStack);
                    elements = new X509ChainElement[chainSize];
                    int maybeRootDepth = chainSize - 1;

                    // The leaf cert is 0, up to (maybe) the root at chainSize - 1
                    for (int i = 0; i < chainSize; i++)
                    {
                        List<X509ChainStatus> status = new List<X509ChainStatus>();

                        List<Interop.Crypto.X509VerifyStatusCode> elementErrors =
                            i < workingChain.Errors.Count ? workingChain.Errors[i] : null;

                        if (elementErrors != null)
                        {
                            AddElementStatus(elementErrors, status, overallStatus);
                        }

                        IntPtr elementCertPtr = Interop.Crypto.GetX509StackField(chainStack, i);

                        if (elementCertPtr == IntPtr.Zero)
                        {
                            throw Interop.Crypto.CreateOpenSslCryptographicException();
                        }

                        // Duplicate the certificate handle
                        X509Certificate2 elementCert = new X509Certificate2(elementCertPtr);
                        elements[i] = new X509ChainElement(elementCert, status.ToArray(), "");
                    }
                }
            }

            GC.KeepAlive(workingCallback);

            if ((certificatePolicy != null && certificatePolicy.Count > 0) ||
                (applicationPolicy != null && applicationPolicy.Count > 0))
            {
                List<X509Certificate2> certsToRead = new List<X509Certificate2>();

                foreach (X509ChainElement element in elements)
                {
                    certsToRead.Add(element.Certificate);
                }

                CertificatePolicyChain policyChain = new CertificatePolicyChain(certsToRead);

                bool failsPolicyChecks = false;

                if (certificatePolicy != null)
                {
                    if (!policyChain.MatchesCertificatePolicies(certificatePolicy))
                    {
                        failsPolicyChecks = true;
                    }
                }

                if (applicationPolicy != null)
                {
                    if (!policyChain.MatchesApplicationPolicies(applicationPolicy))
                    {
                        failsPolicyChecks = true;
                    }
                }

                if (failsPolicyChecks)
                {
                    X509ChainElement leafElement = elements[0];

                    X509ChainStatus chainStatus = new X509ChainStatus
                    {
                        Status = X509ChainStatusFlags.NotValidForUsage,
                        StatusInformation = SR.Chain_NoPolicyMatch,
                    };

                    var elementStatus = new List<X509ChainStatus>(leafElement.ChainElementStatus.Length + 1);
                    elementStatus.AddRange(leafElement.ChainElementStatus);

                    AddUniqueStatus(elementStatus, ref chainStatus);
                    AddUniqueStatus(overallStatus, ref chainStatus);

                    elements[0] = new X509ChainElement(
                        leafElement.Certificate,
                        elementStatus.ToArray(),
                        leafElement.Information);
                }
            }

            return new OpenSslX509ChainProcessor
            {
                ChainStatus = overallStatus.ToArray(),
                ChainElements = elements,
            };
        }

        private static void AddElementStatus(
            List<Interop.Crypto.X509VerifyStatusCode> errorCodes,
            List<X509ChainStatus> elementStatus,
            List<X509ChainStatus> overallStatus)
        {
            foreach (var errorCode in errorCodes)
            {
                AddElementStatus(errorCode, elementStatus, overallStatus);
            }
        }

        private static void AddElementStatus(
            Interop.Crypto.X509VerifyStatusCode errorCode,
            List<X509ChainStatus> elementStatus,
            List<X509ChainStatus> overallStatus)
        {
            X509ChainStatusFlags statusFlag = MapVerifyErrorToChainStatus(errorCode);

            Debug.Assert(
                (statusFlag & (statusFlag - 1)) == 0,
                "Status flag has more than one bit set",
                "More than one bit is set in status '{0}' for error code '{1}'",
                statusFlag,
                errorCode);

            foreach (X509ChainStatus currentStatus in elementStatus)
            {
                if ((currentStatus.Status & statusFlag) != 0)
                {
                    return;
                }
            }

            X509ChainStatus chainStatus = new X509ChainStatus
            {
                Status = statusFlag,
                StatusInformation = Interop.Crypto.GetX509VerifyCertErrorString(errorCode),
            };

            elementStatus.Add(chainStatus);
            AddUniqueStatus(overallStatus, ref chainStatus);
        }
        
        private static void AddUniqueStatus(IList<X509ChainStatus> list, ref X509ChainStatus status)
        {
            X509ChainStatusFlags statusCode = status.Status;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Status == statusCode)
                {
                    return;
                }
            }

            list.Add(status);
        }

        private static X509ChainStatusFlags MapVerifyErrorToChainStatus(Interop.Crypto.X509VerifyStatusCode code)
        {
            switch (code)
            {
                case Interop.Crypto.X509VerifyStatusCode.X509_V_OK:
                    return X509ChainStatusFlags.NoError;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_NOT_YET_VALID:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_HAS_EXPIRED:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CERT_NOT_BEFORE_FIELD:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CERT_NOT_AFTER_FIELD:
                    return X509ChainStatusFlags.NotTimeValid;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_REVOKED:
                    return X509ChainStatusFlags.Revoked;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_DECODE_ISSUER_PUBLIC_KEY:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_SIGNATURE_FAILURE:
                    return X509ChainStatusFlags.NotSignatureValid;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_UNTRUSTED:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_SELF_SIGNED_CERT_IN_CHAIN:
                    return X509ChainStatusFlags.UntrustedRoot;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CRL_HAS_EXPIRED:
                    return X509ChainStatusFlags.OfflineRevocation;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CRL_NOT_YET_VALID:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CRL_SIGNATURE_FAILURE:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CRL_LAST_UPDATE_FIELD:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CRL_NEXT_UPDATE_FIELD:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_CRL_SIGN:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_DECRYPT_CRL_SIGNATURE:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION:
                    return X509ChainStatusFlags.RevocationStatusUnknown;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_INVALID_EXTENSION:
                    return X509ChainStatusFlags.InvalidExtension;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_VERIFY_LEAF_SIGNATURE:
                    return X509ChainStatusFlags.PartialChain;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_INVALID_PURPOSE:
                    return X509ChainStatusFlags.NotValidForUsage;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_INVALID_CA:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_INVALID_NON_CA:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_PATH_LENGTH_EXCEEDED:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_CERTSIGN:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE:
                    return X509ChainStatusFlags.InvalidBasicConstraints;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_INVALID_POLICY_EXTENSION:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_NO_EXPLICIT_POLICY:
                    return X509ChainStatusFlags.InvalidPolicyConstraints;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_REJECTED:
                    return X509ChainStatusFlags.ExplicitDistrust;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION:
                    return X509ChainStatusFlags.HasNotSupportedCriticalExtension;

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_CHAIN_TOO_LONG:
                    throw new CryptographicException();

                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_OUT_OF_MEM:
                    throw new OutOfMemoryException();

                default:
                    Debug.Fail("Unrecognized X509VerifyStatusCode:" + code);
                    throw new CryptographicException();
            }
        }

        internal static HashSet<X509Certificate2> FindCandidates(
            X509Certificate2 leaf,
            X509Certificate2Collection extraStore,
            HashSet<X509Certificate2> downloaded,
            HashSet<X509Certificate2> systemTrusted,
            ref TimeSpan remainingDownloadTime)
        {
            var candidates = new HashSet<X509Certificate2>();
            var toProcess = new Queue<X509Certificate2>();
            toProcess.Enqueue(leaf);

            using (var systemRootStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            using (var systemIntermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine))
            using (var userRootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            using (var userIntermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                systemRootStore.Open(OpenFlags.ReadOnly);
                systemIntermediateStore.Open(OpenFlags.ReadOnly);
                userRootStore.Open(OpenFlags.ReadOnly);
                userIntermediateStore.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection systemRootCerts = systemRootStore.Certificates;
                X509Certificate2Collection systemIntermediateCerts = systemIntermediateStore.Certificates;
                X509Certificate2Collection userRootCerts = userRootStore.Certificates;
                X509Certificate2Collection userIntermediateCerts = userIntermediateStore.Certificates;

                // fill the system trusted collection
                foreach (X509Certificate2 userRootCert in userRootCerts)
                {
                    if (!systemTrusted.Add(userRootCert))
                    {
                        // If we have already (effectively) added another instance of this certificate,
                        // then this one provides no value. A Disposed cert won't harm the matching logic.
                        userRootCert.Dispose();
                    }
                }

                foreach (X509Certificate2 systemRootCert in systemRootCerts)
                {
                    if (!systemTrusted.Add(systemRootCert))
                    {
                        // If we have already (effectively) added another instance of this certificate,
                        // (for example, because another copy of it was in the user store)
                        // then this one provides no value. A Disposed cert won't harm the matching logic.
                        systemRootCert.Dispose();
                    }
                }

                X509Certificate2Collection[] storesToCheck =
                {
                    extraStore,
                    userIntermediateCerts,
                    systemIntermediateCerts,
                    userRootCerts,
                    systemRootCerts,
                };

                while (toProcess.Count > 0)
                {
                    X509Certificate2 current = toProcess.Dequeue();

                    candidates.Add(current);

                    HashSet<X509Certificate2> results = FindIssuer(
                        current,
                        storesToCheck,
                        downloaded,
                        ref remainingDownloadTime);

                    if (results != null)
                    {
                        foreach (X509Certificate2 result in results)
                        {
                            if (!candidates.Contains(result))
                            {
                                toProcess.Enqueue(result);
                            }
                        }
                    }
                }

                // Avoid sending unused certs into the finalizer queue by doing only a ref check

                var candidatesByReference = new HashSet<X509Certificate2>(
                    candidates,
                    ReferenceEqualityComparer<X509Certificate2>.Instance);

                // Certificates come from 5 sources:
                //  1) extraStore.
                //     These are cert objects that are provided by the user, we shouldn't dispose them.
                //  2) the machine root store
                //     These certs are moving on to the "was I a system trust?" test, and we shouldn't dispose them.
                //  3) the user root store
                //     These certs are moving on to the "was I a system trust?" test, and we shouldn't dispose them.
                //  4) the machine intermediate store
                //     These certs were either path candidates, or not. If they were, don't dispose them. Otherwise do.
                //  5) the user intermediate store
                //     These certs were either path candidates, or not. If they were, don't dispose them. Otherwise do.
                DisposeUnreferenced(candidatesByReference, systemIntermediateCerts);
                DisposeUnreferenced(candidatesByReference, userIntermediateCerts);
            }

            return candidates;
        }

        private static void DisposeUnreferenced(
            ISet<X509Certificate2> referencedSet,
            X509Certificate2Collection storeCerts)
        {
            foreach (X509Certificate2 cert in storeCerts)
            {
                if (!referencedSet.Contains(cert))
                {
                    cert.Dispose();
                }
            }
        }

        private static HashSet<X509Certificate2> FindIssuer(
            X509Certificate2 cert,
            X509Certificate2Collection[] stores,
            HashSet<X509Certificate2> downloadedCerts,
            ref TimeSpan remainingDownloadTime)
        {
            if (IsSelfSigned(cert))
            {
                // It's a root cert, we won't make any progress.
                return null;
            }

            SafeX509Handle certHandle = ((OpenSslX509CertificateReader)cert.Pal).SafeHandle;

            foreach (X509Certificate2Collection store in stores)
            {
                HashSet<X509Certificate2> fromStore = null;

                foreach (X509Certificate2 candidate in store)
                {
                    var certPal = (OpenSslX509CertificateReader)candidate.Pal;

                    if (certPal == null)
                    {
                        continue;
                    }

                    SafeX509Handle candidateHandle = certPal.SafeHandle;

                    int issuerError = Interop.Crypto.X509CheckIssued(candidateHandle, certHandle);

                    if (issuerError == 0)
                    {
                        if (fromStore == null)
                        {
                            fromStore = new HashSet<X509Certificate2>();
                        }

                        fromStore.Add(candidate);
                    }
                }

                if (fromStore != null)
                {
                    return fromStore;
                }
            }

            byte[] authorityInformationAccess = null;

            foreach (X509Extension extension in cert.Extensions)
            {
                if (StringComparer.Ordinal.Equals(extension.Oid.Value, Oids.AuthorityInformationAccess))
                {
                    // If there's an Authority Information Access extension, it might be used for
                    // looking up additional certificates for the chain.
                    authorityInformationAccess = extension.RawData;
                    break;
                }
            }

            if (authorityInformationAccess != null)
            {
                X509Certificate2 downloaded = DownloadCertificate(
                    authorityInformationAccess,
                    ref remainingDownloadTime);

                if (downloaded != null)
                {
                    downloadedCerts.Add(downloaded);

                    return new HashSet<X509Certificate2>() { downloaded };
                }
            }

            return null;
        }

        private static bool IsSelfSigned(X509Certificate2 cert)
        {
            return StringComparer.Ordinal.Equals(cert.Subject, cert.Issuer);
        }

        private static X509Certificate2 DownloadCertificate(
            byte[] authorityInformationAccess,
            ref TimeSpan remainingDownloadTime)
        {
            // Don't do any work if we're over limit.
            if (remainingDownloadTime <= TimeSpan.Zero)
            {
                return null;
            }

            string uri = FindHttpAiaRecord(authorityInformationAccess, Oids.CertificateAuthorityIssuers);

            if (uri == null)
            {
                return null;
            }

            return CertificateAssetDownloader.DownloadCertificate(uri, ref remainingDownloadTime);
        }

        internal static string FindHttpAiaRecord(byte[] authorityInformationAccess, string recordTypeOid)
        {
            DerSequenceReader reader = new DerSequenceReader(authorityInformationAccess);

            while (reader.HasData)
            {
                DerSequenceReader innerReader = reader.ReadSequence();

                // If the sequence's first element is a sequence, unwrap it.
                if (innerReader.PeekTag() == ConstructedSequenceTagId)
                {
                    innerReader = innerReader.ReadSequence();
                }

                Oid oid = innerReader.ReadOid();

                if (StringComparer.Ordinal.Equals(oid.Value, recordTypeOid))
                {
                    string uri = innerReader.ReadIA5String();

                    Uri parsedUri;
                    if (!Uri.TryCreate(uri, UriKind.Absolute, out parsedUri))
                    {
                        continue;
                    }

                    if (!StringComparer.Ordinal.Equals(parsedUri.Scheme, "http"))
                    {
                        continue;
                    }

                    return uri;
                }
            }

            return null;
        }

        private class WorkingChain
        {
            internal readonly List<List<Interop.Crypto.X509VerifyStatusCode>> Errors =
                new List<List<Interop.Crypto.X509VerifyStatusCode>>();

            internal int VerifyCallback(int ok, IntPtr ctx)
            {
                if (ok != 0)
                {
                    return ok;
                }

                try
                {
                    using (var storeCtx = new SafeX509StoreCtxHandle(ctx, ownsHandle: false))
                    {
                        Interop.Crypto.X509VerifyStatusCode errorCode = Interop.Crypto.X509StoreCtxGetError(storeCtx);
                        int errorDepth = Interop.Crypto.X509StoreCtxGetErrorDepth(storeCtx);

                        // We don't report "OK" as an error.
                        // For compatibility with Windows / .NET Framework, do not report X509_V_CRL_NOT_YET_VALID.
                        if (errorCode != Interop.Crypto.X509VerifyStatusCode.X509_V_OK &&
                            errorCode != Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CRL_NOT_YET_VALID)
                        {
                            while (Errors.Count <= errorDepth)
                            {
                                Errors.Add(null);
                            }

                            if (Errors[errorDepth] == null)
                            {
                                Errors[errorDepth] = new List<Interop.Crypto.X509VerifyStatusCode>();
                            }

                            Errors[errorDepth].Add(errorCode);
                        }
                    }

                    return 1;
                }
                catch
                {
                    return -1;
                }
            }
        }
    }
}
