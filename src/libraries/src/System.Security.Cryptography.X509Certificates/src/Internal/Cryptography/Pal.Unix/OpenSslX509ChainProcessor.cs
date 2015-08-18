// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void Dispose()
        {
        }

        public bool? Verify(X509VerificationFlags flags, out Exception exception)
        {
            if (flags != X509VerificationFlags.NoFlag)
            {
                // TODO (#2204): Add support for X509VerificationFlags, or throw PlatformNotSupportedException.
                throw new NotSupportedException(SR.WorkInProgress);
            }
            
            exception = null;
            return ChainStatus.Length == 0;
        }

        public X509ChainElement[] ChainElements { get; private set; }
        public X509ChainStatus[] ChainStatus { get; private set; }

        public SafeX509ChainHandle SafeHandle
        {
            get { return null; }
        }

        public static IChainPal BuildChain(
            X509Certificate2 leaf,
            X509Certificate2Collection candidates,
            OidCollection applicationPolicy,
            OidCollection certificatePolicy,
            DateTime verificationTime)
        {
            X509ChainElement[] elements;
            List<X509ChainStatus> overallStatus = new List<X509ChainStatus>();

            // An X509_STORE is more comparable to Cryptography.X509Certificate2Collection than to
            // Cryptography.X509Store. So read this with OpenSSL eyes, not CAPI/CNG eyes.
            //
            // (If you need to think of it as an X509Store, it's a volatile memory store)
            using (SafeX509StoreHandle store = Interop.libcrypto.X509_STORE_new())
            using (SafeX509StoreCtxHandle storeCtx = Interop.libcrypto.X509_STORE_CTX_new())
            {
                Interop.libcrypto.CheckValidOpenSslHandle(store);
                Interop.libcrypto.CheckValidOpenSslHandle(storeCtx);

                foreach (X509Certificate2 cert in candidates)
                {
                    OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)cert.Pal;

                    if (!Interop.libcrypto.X509_STORE_add_cert(store, pal.SafeHandle))
                    {
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();
                    }
                }

                // When CRL checking support is added, it should be done before the call to
                // X509_STORE_CTX_init (aka here) by calling X509_STORE_set_flags(store, flags);

                SafeX509Handle leafHandle = ((OpenSslX509CertificateReader)leaf.Pal).SafeHandle;

                if (!Interop.libcrypto.X509_STORE_CTX_init(storeCtx, store, leafHandle, IntPtr.Zero))
                {
                    throw Interop.libcrypto.CreateOpenSslCryptographicException();
                }

                Interop.Crypto.SetX509ChainVerifyTime(storeCtx, verificationTime);

                int verify = Interop.libcrypto.X509_verify_cert(storeCtx);

                if (verify < 0)
                {
                    throw Interop.libcrypto.CreateOpenSslCryptographicException();
                }

                using (SafeX509StackHandle chainStack = Interop.libcrypto.X509_STORE_CTX_get1_chain(storeCtx))
                {
                    int chainSize = Interop.Crypto.GetX509StackFieldCount(chainStack);
                    int errorDepth = -1;
                    Interop.libcrypto.X509VerifyStatusCode errorCode = 0;
                    string errorMsg = null;

                    if (verify == 0)
                    {
                        errorCode = Interop.libcrypto.X509_STORE_CTX_get_error(storeCtx);
                        errorDepth = Interop.libcrypto.X509_STORE_CTX_get_error_depth(storeCtx);
                        errorMsg = Interop.libcrypto.X509_verify_cert_error_string(errorCode);
                    }

                    elements = new X509ChainElement[chainSize];

                    for (int i = 0; i < chainSize; i++)
                    {
                        List<X509ChainStatus> status = new List<X509ChainStatus>();

                        if (i == errorDepth)
                        {
                            X509ChainStatus chainStatus = new X509ChainStatus
                            {
                                Status = MapVerifyErrorToChainStatus(errorCode),
                                StatusInformation = errorMsg,
                            };

                            status.Add(chainStatus);
                            AddUniqueStatus(overallStatus, ref chainStatus);
                        }

                        IntPtr elementCertPtr = Interop.Crypto.GetX509StackField(chainStack, i);

                        if (elementCertPtr == IntPtr.Zero)
                        {
                            throw Interop.libcrypto.CreateOpenSslCryptographicException();
                        }

                        // Duplicate the certificate handle
                        X509Certificate2 elementCert = new X509Certificate2(elementCertPtr);

                        elements[i] = new X509ChainElement(elementCert, status.ToArray(), "");
                    }
                }
            }

            if ((certificatePolicy != null && certificatePolicy.Count > 0) ||
                (applicationPolicy != null && applicationPolicy.Count > 0))
            {
                X509Certificate2Collection certsToRead = new X509Certificate2Collection();

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
                        Status = X509ChainStatusFlags.InvalidPolicyConstraints,
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

        private static X509ChainStatusFlags MapVerifyErrorToChainStatus(Interop.libcrypto.X509VerifyStatusCode code)
        {
            switch (code)
            {
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_OK:
                    return X509ChainStatusFlags.NoError;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_NOT_YET_VALID:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_HAS_EXPIRED:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CERT_NOT_BEFORE_FIELD:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CERT_NOT_AFTER_FIELD:
                    return X509ChainStatusFlags.NotTimeValid;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_REVOKED:
                    return X509ChainStatusFlags.Revoked;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_SIGNATURE_FAILURE:
                    return X509ChainStatusFlags.NotSignatureValid;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_UNTRUSTED:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_SELF_SIGNED_CERT_IN_CHAIN:
                    return X509ChainStatusFlags.UntrustedRoot;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CRL_HAS_EXPIRED:
                    return X509ChainStatusFlags.OfflineRevocation;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CRL_NOT_YET_VALID:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CRL_SIGNATURE_FAILURE:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CRL_LAST_UPDATE_FIELD:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_ERROR_IN_CRL_NEXT_UPDATE_FIELD:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_CRL_SIGN:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_DECRYPT_CRL_SIGNATURE:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION:
                    return X509ChainStatusFlags.RevocationStatusUnknown;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_INVALID_EXTENSION:
                    return X509ChainStatusFlags.InvalidExtension;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_VERIFY_LEAF_SIGNATURE:
                    return X509ChainStatusFlags.PartialChain;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_INVALID_PURPOSE:
                    return X509ChainStatusFlags.NotValidForUsage;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_INVALID_CA:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_INVALID_NON_CA:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_PATH_LENGTH_EXCEEDED:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_CERTSIGN:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE:
                    return X509ChainStatusFlags.InvalidBasicConstraints;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_INVALID_POLICY_EXTENSION:
                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_NO_EXPLICIT_POLICY:
                    return X509ChainStatusFlags.InvalidPolicyConstraints;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_REJECTED:
                    return X509ChainStatusFlags.ExplicitDistrust;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION:
                    return X509ChainStatusFlags.HasNotSupportedCriticalExtension;

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_CERT_CHAIN_TOO_LONG:
                    throw new CryptographicException();

                case Interop.libcrypto.X509VerifyStatusCode.X509_V_ERR_OUT_OF_MEM:
                    throw new OutOfMemoryException();

                default:
                    Debug.Fail("Unrecognized X509VerifyStatusCode:" + code);
                    throw new CryptographicException();
            }
        }

        internal static X509Certificate2Collection FindCandidates(
            X509Certificate2 leaf,
            X509Certificate2Collection extraStore)
        {
            X509Certificate2Collection candidates = new X509Certificate2Collection();
            Queue<X509Certificate2> toProcess = new Queue<X509Certificate2>();
            toProcess.Enqueue(leaf);

            using (var rootStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            using (var intermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine))
            {
                rootStore.Open(OpenFlags.ReadOnly);
                intermediateStore.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection rootCerts = rootStore.Certificates;
                X509Certificate2Collection intermediateCerts = intermediateStore.Certificates;

                X509Certificate2Collection[] storesToCheck =
                {
                    extraStore,
                    intermediateCerts,
                    rootCerts,
                };

                while (toProcess.Count > 0)
                {
                    X509Certificate2 current = toProcess.Dequeue();

                    if (!candidates.Contains(current))
                    {
                        candidates.Add(current);
                    }

                    X509Certificate2Collection results = FindIssuer(
                        current,
                        storesToCheck);

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
            }

            return candidates;
        }

        private static X509Certificate2Collection FindIssuer(
            X509Certificate2 cert,
            X509Certificate2Collection[] stores)
        {
            if (StringComparer.Ordinal.Equals(cert.Subject, cert.Issuer))
            {
                // It's a root cert, we won't make any progress.
                return null;
            }

            SafeX509Handle certHandle = ((OpenSslX509CertificateReader)cert.Pal).SafeHandle;

            foreach (X509Certificate2Collection store in stores)
            {
                X509Certificate2Collection fromStore = null;

                foreach (X509Certificate2 candidate in store)
                {
                    SafeX509Handle candidateHandle = ((OpenSslX509CertificateReader)candidate.Pal).SafeHandle;

                    int issuerError = Interop.libcrypto.X509_check_issued(candidateHandle, certHandle);

                    if (issuerError == 0)
                    {
                        if (fromStore == null)
                        {
                            fromStore = new X509Certificate2Collection();
                        }

                        if (!fromStore.Contains(candidate))
                        {
                            fromStore.Add(candidate);
                        }
                    }
                }

                if (fromStore != null)
                {
                    return fromStore;
                }
            }

            return null;
        }
    }
}
