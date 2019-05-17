// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Asn1;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslX509ChainProcessor : IChainPal
    {
        // The average chain is 3 (End-Entity, Intermediate, Root)
        // 10 is plenty big.
        private const int DefaultChainCapacity = 10;

        private static readonly CachedDirectoryStoreProvider s_userRootStore =
            new CachedDirectoryStoreProvider(X509Store.RootStoreName);

        private static readonly CachedDirectoryStoreProvider s_userIntermediateStore =
            new CachedDirectoryStoreProvider(X509Store.IntermediateCAStoreName);

        private static readonly CachedDirectoryStoreProvider s_userPersonalStore =
            new CachedDirectoryStoreProvider(X509Store.MyStoreName);

        private SafeX509Handle _leafHandle;
        private SafeX509StoreHandle _store;
        private readonly SafeX509StackHandle _untrustedLookup;
        private readonly SafeX509StoreCtxHandle _storeCtx;
        private readonly DateTime _verificationTime;
        private TimeSpan _remainingDownloadTime;
        private X509RevocationMode _revocationMode;

        private OpenSslX509ChainProcessor(
            SafeX509Handle leafHandle,
            SafeX509StoreHandle store,
            SafeX509StackHandle untrusted,
            SafeX509StoreCtxHandle storeCtx,
            DateTime verificationTime,
            TimeSpan remainingDownloadTime)
        {
            _leafHandle = leafHandle;
            _store = store;
            _untrustedLookup = untrusted;
            _storeCtx = storeCtx;
            _verificationTime = verificationTime;
            _remainingDownloadTime = remainingDownloadTime;
        }

        public void Dispose()
        {
            _storeCtx?.Dispose();
            _untrustedLookup?.Dispose();
            _store?.Dispose();
            
            // We don't own this one.
            _leafHandle = null;
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

        internal static OpenSslX509ChainProcessor InitiateChain(
            SafeX509Handle leafHandle,
            DateTime verificationTime,
            TimeSpan remainingDownloadTime)
        {
            CachedSystemStoreProvider.GetNativeCollections(
                out SafeX509StackHandle systemTrust,
                out SafeX509StackHandle systemIntermediate);

            SafeX509StoreHandle store = null;
            SafeX509StackHandle untrusted = null;
            SafeX509StoreCtxHandle storeCtx = null;

            try
            {
                store = Interop.Crypto.X509ChainNew(systemTrust, s_userRootStore.GetNativeCollection());

                untrusted = Interop.Crypto.NewX509Stack();
                Interop.Crypto.X509StackAddMultiple(untrusted, s_userIntermediateStore.GetNativeCollection());
                Interop.Crypto.X509StackAddMultiple(untrusted, s_userPersonalStore.GetNativeCollection());
                Interop.Crypto.X509StackAddMultiple(untrusted, systemIntermediate);
                Interop.Crypto.X509StoreSetVerifyTime(store, verificationTime);

                storeCtx = Interop.Crypto.X509StoreCtxCreate();

                if (!Interop.Crypto.X509StoreCtxInit(storeCtx, store, leafHandle, untrusted))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return new OpenSslX509ChainProcessor(
                    leafHandle,
                    store,
                    untrusted,
                    storeCtx,
                    verificationTime,
                    remainingDownloadTime);
            }
            catch
            {
                store?.Dispose();
                untrusted?.Dispose();
                storeCtx?.Dispose();
                throw;
            }
        }

        internal Interop.Crypto.X509VerifyStatusCode FindFirstChain(X509Certificate2Collection extraCerts)
        {
            SafeX509StoreCtxHandle storeCtx = _storeCtx;

            // While this returns true/false, at this stage we care more about the detailed error code.
            Interop.Crypto.X509VerifyCert(storeCtx);
            Interop.Crypto.X509VerifyStatusCode statusCode = Interop.Crypto.X509StoreCtxGetError(storeCtx);

            if (IsCompleteChain(statusCode))
            {
                return statusCode;
            }

            SafeX509StackHandle untrusted = _untrustedLookup;

            if (extraCerts?.Count > 0)
            {
                foreach(X509Certificate2 cert in extraCerts)
                {
                    AddToStackAndUpRef(((OpenSslX509CertificateReader)cert.Pal).SafeHandle, untrusted);
                }

                Interop.Crypto.X509StoreCtxRebuildChain(storeCtx);
                statusCode = Interop.Crypto.X509StoreCtxGetError(storeCtx);
            }

            return statusCode;
        }

        internal static bool IsCompleteChain(Interop.Crypto.X509VerifyStatusCode statusCode)
        {
            switch (statusCode)
            {
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT:
                case Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY:
                    return false;
                default:
                    return true;
            }
        }

        internal Interop.Crypto.X509VerifyStatusCode FindChainViaAia(
            ref List<X509Certificate2> downloadedCerts)
        {
            IntPtr lastCert = IntPtr.Zero;
            SafeX509StoreCtxHandle storeCtx = _storeCtx;

            Interop.Crypto.X509VerifyStatusCode statusCode =
                Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT;

            while (!IsCompleteChain(statusCode))
            {
                using (SafeX509Handle currentCert = Interop.Crypto.X509StoreCtxGetCurrentCert(storeCtx))
                {
                    IntPtr currentHandle = currentCert.DangerousGetHandle();

                    // No progress was made, give up.
                    if (currentHandle == lastCert)
                    {
                        break;
                    }

                    lastCert = currentHandle;

                    ArraySegment<byte> authorityInformationAccess =
                        OpenSslX509CertificateReader.FindFirstExtension(
                            currentCert,
                            Oids.AuthorityInformationAccess);

                    if (authorityInformationAccess.Count == 0)
                    {
                        break;
                    }

                    X509Certificate2 downloaded = DownloadCertificate(
                        authorityInformationAccess,
                        ref _remainingDownloadTime);

                    // The AIA record is contained in a public structure, so no need to clear it.
                    CryptoPool.Return(authorityInformationAccess.Array, clearSize: 0);

                    if (downloaded == null)
                    {
                        break;
                    }

                    if (downloadedCerts == null)
                    {
                        downloadedCerts = new List<X509Certificate2>();
                    }

                    AddToStackAndUpRef(downloaded.Handle, _untrustedLookup);
                    downloadedCerts.Add(downloaded);
                    
                    Interop.Crypto.X509StoreCtxRebuildChain(storeCtx);
                    statusCode = Interop.Crypto.X509StoreCtxGetError(storeCtx);
                }
            }

            if (statusCode == Interop.Crypto.X509VerifyStatusCode.X509_V_OK && downloadedCerts != null)
            {
                using (SafeX509StackHandle chainStack = Interop.Crypto.X509StoreCtxGetChain(_storeCtx))
                {
                    int chainSize = Interop.Crypto.GetX509StackFieldCount(chainStack);
                    Span<IntPtr> tempChain = stackalloc IntPtr[DefaultChainCapacity];
                    byte[] tempChainRent = null;

                    if (chainSize <= tempChain.Length)
                    {
                        tempChain = tempChain.Slice(0, chainSize);
                    }
                    else
                    {
                        int targetSize = checked(chainSize * IntPtr.Size);
                        tempChainRent = CryptoPool.Rent(targetSize);
                        tempChain = MemoryMarshal.Cast<byte, IntPtr>(tempChainRent.AsSpan(0, targetSize));
                    }

                    for (int i = 0; i < chainSize; i++)
                    {
                        tempChain[i] = Interop.Crypto.GetX509StackField(chainStack, i);
                    }

                    // In the average case we never made it here.
                    //
                    // Given that we made it here, in the average remaining case
                    // we are doing a one item for which will match in the second position
                    // of an (on-average) 3 item collection.
                    //
                    // The only case where this loop really matters is if downloading the
                    // certificate made an alternate chain better, which may have resulted in
                    // an extra download and made the first one not be involved any longer. In
                    // that case, it's a 2 item for loop matching against a three item set.
                    //
                    // So N*M is well contained.
                    for (int i = downloadedCerts.Count - 1; i >= 0; i--)
                    {
                        X509Certificate2 downloadedCert = downloadedCerts[i];

                        if (!tempChain.Contains(downloadedCert.Handle))
                        {
                            downloadedCert.Dispose();
                            downloadedCerts.RemoveAt(i);
                        }
                    }

                    if (downloadedCerts.Count == 0)
                    {
                        downloadedCerts = null;
                    }

                    if (tempChainRent != null)
                    {
                        CryptoPool.Return(tempChainRent);
                    }
                }
            }

            return statusCode;
        }

        internal void CommitToChain()
        {
            Interop.Crypto.X509StoreCtxCommitToChain(_storeCtx);
        }

        internal void ProcessRevocation(
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag)
        {
            _revocationMode = revocationMode;

            if (revocationMode == X509RevocationMode.NoCheck)
            {
                return;
            }

            using (SafeX509StackHandle chainStack = Interop.Crypto.X509StoreCtxGetChain(_storeCtx))
            {
                int chainSize =
                    revocationFlag == X509RevocationFlag.EndCertificateOnly ?
                        1 :
                        Interop.Crypto.GetX509StackFieldCount(chainStack);

                for (int i = 0; i < chainSize; i++)
                {
                    using (SafeX509Handle cert =
                        Interop.Crypto.X509UpRef(Interop.Crypto.GetX509StackField(chainStack, i)))
                    {
                        CrlCache.AddCrlForCertificate(
                            cert,
                            _store,
                            revocationMode,
                            _verificationTime,
                            ref _remainingDownloadTime);
                    }
                }
            }

            Interop.Crypto.X509StoreSetRevocationFlag(_store, revocationFlag);
            Interop.Crypto.X509StoreCtxRebuildChain(_storeCtx);
        }

        internal void Finish(OidCollection applicationPolicy, OidCollection certificatePolicy)
        {
            WorkingChain workingChain = null;

            // If the chain had any errors during the previous build we need to walk it again with
            // the error collector running.
            if (Interop.Crypto.X509StoreCtxGetError(_storeCtx) !=
                Interop.Crypto.X509VerifyStatusCode.X509_V_OK)
            {
                Interop.Crypto.X509StoreCtxReset(_storeCtx);

                workingChain = new WorkingChain();
                Interop.Crypto.X509StoreVerifyCallback workingCallback = workingChain.VerifyCallback;
                Interop.Crypto.X509StoreCtxSetVerifyCallback(_storeCtx, workingCallback);

                bool verify = Interop.Crypto.X509VerifyCert(_storeCtx);

                if (workingChain.AbortedForSignatureError)
                {
                    Debug.Assert(!verify, "verify should have returned false for signature error");
                    CloneChainForSignatureErrors();

                    // Reset to a WorkingChain that won't fail.
                    workingChain = new WorkingChain(abortOnSignatureError: false);
                    workingCallback = workingChain.VerifyCallback;
                    Interop.Crypto.X509StoreCtxSetVerifyCallback(_storeCtx, workingCallback);

                    verify = Interop.Crypto.X509VerifyCert(_storeCtx);
                }

                GC.KeepAlive(workingCallback);

                // Because our callback tells OpenSSL that every problem is ignorable, it should tell us that the
                // chain is just fine (unless it returned a negative code for an exception)
                Debug.Assert(verify, "verify should have returned true");

                const Interop.Crypto.X509VerifyStatusCode NoCrl =
                    Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL;

                ErrorCollection? errors =
                    workingChain.LastError > 0 ? (ErrorCollection?)workingChain[0] : null;

                if (_revocationMode == X509RevocationMode.Online &&
                    _remainingDownloadTime > TimeSpan.Zero &&
                    errors?.HasError(NoCrl) == true)
                {
                    Interop.Crypto.X509VerifyStatusCode ocspStatus = CheckOcsp();

                    ref ErrorCollection refErrors = ref workingChain[0];

                    if (ocspStatus == Interop.Crypto.X509VerifyStatusCode.X509_V_OK)
                    {
                        refErrors.ClearError(NoCrl);
                    }
                    else if (ocspStatus != NoCrl)
                    {
                        refErrors.ClearError(NoCrl);
                        refErrors.Add(ocspStatus);
                    }
                }
            }

            X509ChainElement[] elements = BuildChainElements(
                workingChain,
                out List<X509ChainStatus> overallStatus);

            workingChain?.Dispose();

            if (applicationPolicy?.Count > 0 || certificatePolicy?.Count > 0)
            {
                ProcessPolicy(elements, ref overallStatus, applicationPolicy, certificatePolicy);
            }

            ChainStatus = overallStatus?.ToArray() ?? Array.Empty<X509ChainStatus>();
            ChainElements = elements;

            // The native resources are not needed any longer.
            Dispose();
        }

        private void CloneChainForSignatureErrors()
        {
            SafeX509StoreHandle newStore;
            Interop.Crypto.X509StoreCtxResetForSignatureError(_storeCtx, out newStore);

            if (newStore != null)
            {
                _store.Dispose();
                _store = newStore;
            }
        }

        private Interop.Crypto.X509VerifyStatusCode CheckOcsp()
        {
            string ocspCache = CrlCache.GetCachedOcspResponseDirectory();
            Interop.Crypto.X509VerifyStatusCode status =
                Interop.Crypto.X509ChainGetCachedOcspStatus(_storeCtx, ocspCache);

            if (status != Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL)
            {
                return status;
            }

            string baseUri = GetOcspEndpoint(_leafHandle);

            if (baseUri == null)
            {
                return status;
            }
            
            using (SafeOcspRequestHandle req = Interop.Crypto.X509ChainBuildOcspRequest(_storeCtx))
            {
                ArraySegment<byte> encoded = Interop.Crypto.OpenSslRentEncode(
                    handle => Interop.Crypto.GetOcspRequestDerSize(handle),
                    (handle, buf) => Interop.Crypto.EncodeOcspRequest(handle, buf),
                    req);

                ArraySegment<char> urlEncoded = Base64UrlEncode(encoded);
                string requestUrl = UrlPathAppend(baseUri, urlEncoded);

                // Nothing sensitive is in the encoded request (it was sent via HTTP-non-S)
                CryptoPool.Return(encoded.Array, clearSize: 0);
                ArrayPool<char>.Shared.Return(urlEncoded.Array);

                // https://tools.ietf.org/html/rfc6960#appendix-A describes both a GET and a POST
                // version of an OCSP responder.
                //
                // Doing POST via the reflection indirection to HttpClient is difficult, and
                // CA/Browser Forum Baseline Requirements (version 1.6.3) section 4.9.10
                // (On-line REvocation Checking Requirements) says that the GET method must be supported.
                //
                // So, for now, only try GET.

                SafeOcspResponseHandle resp =
                    CertificateAssetDownloader.DownloadOcspGet(requestUrl, ref _remainingDownloadTime);

                using (resp)
                {
                    if (resp == null || resp.IsInvalid)
                    {
                        return Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_UNABLE_TO_GET_CRL;
                    }

                    try
                    {
                        System.IO.Directory.CreateDirectory(ocspCache);
                    }
                    catch
                    {
                        // Opportunistic create, suppress all errors.
                    }

                    return Interop.Crypto.X509ChainVerifyOcsp(_storeCtx, req, resp, ocspCache);
                }
            }
        }

        private static string UrlPathAppend(string baseUri, ReadOnlyMemory<char> resource)
        {
            Debug.Assert(baseUri.Length > 0);
            Debug.Assert(resource.Length > 0);

            int count = baseUri.Length + resource.Length;

            if (baseUri[baseUri.Length - 1] == '/')
            {
                return string.Create(
                    count,
                    (baseUri, resource),
                    (buf, st) =>
                    {
                        st.baseUri.AsSpan().CopyTo(buf);
                        st.resource.Span.CopyTo(buf.Slice(st.Item1.Length));
                    });
            }

            return string.Create(
                count + 1,
                (baseUri, resource),
                (buf, st) =>
                {
                    st.baseUri.AsSpan().CopyTo(buf);
                    buf[st.Item1.Length] = '/';
                    st.resource.Span.CopyTo(buf.Slice(st.Item1.Length + 1));
                });
        }

        private static ArraySegment<char> Base64UrlEncode(ReadOnlySpan<byte> input)
        {
            // Every 3 bytes turns into 4 chars for the Base64 operation
            int base64Len = ((input.Length + 2) / 3) * 4;
            char[] base64 = ArrayPool<char>.Shared.Rent(base64Len);

            if (!Convert.TryToBase64Chars(input, base64, out int charsWritten))
            {
                Debug.Fail($"Convert.TryToBase64 failed with {input.Length} bytes to a {base64.Length} buffer");
                throw new CryptographicException();
            }

            Debug.Assert(charsWritten == base64Len);

            // In the degenerate case every char will turn into 3 chars.
            int urlEncodedLen = charsWritten * 3;
            char[] urlEncoded = ArrayPool<char>.Shared.Rent(urlEncodedLen);
            int writeIdx = 0;

            for (int readIdx = 0; readIdx < charsWritten; readIdx++)
            {
                char cur = base64[readIdx];

                if ((cur >= 'A' && cur <= 'Z') ||
                    (cur >= 'a' && cur <= 'z') ||
                    (cur >= '0' && cur <= '9'))
                {
                    urlEncoded[writeIdx++] = cur;
                }
                else if (cur == '+')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '2';
                    urlEncoded[writeIdx++] = 'B';
                }
                else if (cur == '/')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '2';
                    urlEncoded[writeIdx++] = 'F';
                }
                else if (cur == '=')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '3';
                    urlEncoded[writeIdx++] = 'D';
                }
                else
                {
                    Debug.Fail($"'{cur}' is not a valid Base64 character");
                    throw new CryptographicException();
                }
            }

            ArrayPool<char>.Shared.Return(base64);
            return new ArraySegment<char>(urlEncoded, 0, writeIdx);
        }

        private X509ChainElement[] BuildChainElements(
            WorkingChain workingChain,
            out List<X509ChainStatus> overallStatus)
        {
            X509ChainElement[] elements;
            overallStatus = null;

            using (SafeX509StackHandle chainStack = Interop.Crypto.X509StoreCtxGetChain(_storeCtx))
            {
                int chainSize = Interop.Crypto.GetX509StackFieldCount(chainStack);
                elements = new X509ChainElement[chainSize];

                for (int i = 0; i < chainSize; i++)
                {
                    X509ChainStatus[] status = Array.Empty<X509ChainStatus>();
                    ErrorCollection? elementErrors =
                        workingChain?.LastError > i ? (ErrorCollection?)workingChain[i] : null;

                    if (elementErrors.HasValue && elementErrors.Value.HasErrors)
                    {
                        List<X509ChainStatus> statusBuilder = new List<X509ChainStatus>();
                        overallStatus = overallStatus ?? new List<X509ChainStatus>();

                        AddElementStatus(elementErrors.Value, statusBuilder, overallStatus);
                        status = statusBuilder.ToArray();
                    }

                    IntPtr elementCertPtr = Interop.Crypto.GetX509StackField(chainStack, i);

                    if (elementCertPtr == IntPtr.Zero)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    // Duplicate the certificate handle
                    X509Certificate2 elementCert = new X509Certificate2(elementCertPtr);
                    elements[i] = new X509ChainElement(elementCert, status, "");
                }
            }

            return elements;
        }

        private static void ProcessPolicy(
            X509ChainElement[] elements,
            ref List<X509ChainStatus> overallStatus,
            OidCollection applicationPolicy,
            OidCollection certificatePolicy)
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
                if (overallStatus == null)
                {
                    overallStatus = new List<X509ChainStatus>();
                }

                X509ChainStatus chainStatus = new X509ChainStatus
                {
                    Status = X509ChainStatusFlags.NotValidForUsage,
                    StatusInformation = SR.Chain_NoPolicyMatch,
                };

                AddUniqueStatus(overallStatus, ref chainStatus);

                // No individual element can have seen more errors than the chain overall,
                // so avoid regrowth of the list.
                var elementStatus = new List<X509ChainStatus>(overallStatus.Count);

                for (int i = 0; i < elements.Length; i++)
                {
                    X509ChainElement element = elements[i];
                    elementStatus.Clear();
                    elementStatus.AddRange(element.ChainElementStatus);

                    AddUniqueStatus(elementStatus, ref chainStatus);

                    elements[i] = new X509ChainElement(
                        element.Certificate,
                        elementStatus.ToArray(),
                        element.Information);
                }
            }
        }

        private static void AddElementStatus(
            ErrorCollection errorCodes,
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

        private static X509Certificate2 DownloadCertificate(
            ReadOnlyMemory<byte> authorityInformationAccess,
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

        private static string GetOcspEndpoint(SafeX509Handle cert)
        {
            ArraySegment<byte> authorityInformationAccess =
                OpenSslX509CertificateReader.FindFirstExtension(
                    cert,
                    Oids.AuthorityInformationAccess);

            if (authorityInformationAccess.Count == 0)
            {
                return null;
            }

            string baseUrl = FindHttpAiaRecord(authorityInformationAccess, Oids.OcspEndpoint);
            CryptoPool.Return(authorityInformationAccess.Array, clearSize: 0);
            return baseUrl;
        }

        private static string FindHttpAiaRecord(ReadOnlyMemory<byte> authorityInformationAccess, string recordTypeOid)
        {
            try
            {
                AsnReader reader = new AsnReader(authorityInformationAccess, AsnEncodingRules.DER);
                AsnReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                while (sequenceReader.HasData)
                {
                    AccessDescriptionAsn.Decode(sequenceReader, out AccessDescriptionAsn description);
                    if (StringComparer.Ordinal.Equals(description.AccessMethod, recordTypeOid))
                    {
                        GeneralNameAsn name = description.AccessLocation;
                        if (name.Uri != null &&
                            Uri.TryCreate(name.Uri, UriKind.Absolute, out Uri uri) &&
                            uri.Scheme == "http")
                        {
                            return name.Uri;
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                // Treat any ASN errors as if the extension was missing.
            }

            return null;
        }

        private static void AddToStackAndUpRef(SafeX509Handle cert, SafeX509StackHandle stack)
        {
            using (SafeX509Handle tmp = Interop.Crypto.X509UpRef(cert))
            {
                if (!Interop.Crypto.PushX509StackField(stack, tmp))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // Ownership was transferred to the cert stack.
                tmp.SetHandleAsInvalid();
            }
        }

        private static void AddToStackAndUpRef(IntPtr cert, SafeX509StackHandle stack)
        {
            using (SafeX509Handle tmp = Interop.Crypto.X509UpRef(cert))
            {
                if (!Interop.Crypto.PushX509StackField(stack, tmp))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // Ownership was transferred to the cert stack.
                tmp.SetHandleAsInvalid();
            }
        }

        private sealed class WorkingChain : IDisposable
        {
            // OpenSSL 1.0 sets a "signature valid, don't check again" if we OK the signature error
            // OpenSSL 1.1 does not.
            private const long OpenSSL_1_1_0_RTM = 0x10100000L;
            private static readonly bool s_defaultAbort = SafeEvpPKeyHandle.OpenSslVersion < OpenSSL_1_1_0_RTM;

            private ErrorCollection[] _errors;

            internal bool AbortOnSignatureError { get; }
            internal bool AbortedForSignatureError { get; private set; }

            internal WorkingChain()
                : this(s_defaultAbort)
            {
            }

            internal WorkingChain(bool abortOnSignatureError)
            {
                AbortOnSignatureError = abortOnSignatureError;
            }

            internal int LastError => _errors?.Length ?? 0;

            internal ref ErrorCollection this[int idx] => ref _errors[idx];

            public void Dispose()
            {
                ErrorCollection[] toReturn = _errors;
                _errors = null;

                if (toReturn != null)
                {
                    ArrayPool<ErrorCollection>.Shared.Return(toReturn);
                }
            }

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

                        if (AbortOnSignatureError &&
                            errorCode == Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CERT_SIGNATURE_FAILURE)
                        {
                            AbortedForSignatureError = true;
                            return 0;
                        }

                        // We don't report "OK" as an error.
                        // For compatibility with Windows / .NET Framework, do not report X509_V_CRL_NOT_YET_VALID.
                        if (errorCode != Interop.Crypto.X509VerifyStatusCode.X509_V_OK &&
                            errorCode != Interop.Crypto.X509VerifyStatusCode.X509_V_ERR_CRL_NOT_YET_VALID)
                        {
                            if (_errors == null)
                            {
                                int size = Math.Max(DefaultChainCapacity, errorDepth + 1);
                                // Since ErrorCollection is a non-public type, this is a private pool.
                                _errors = ArrayPool<ErrorCollection>.Shared.Rent(size);

                                // We only do spares writes.
                                _errors.AsSpan().Clear();
                            }
                            else if (errorDepth >= _errors.Length)
                            {
                                ErrorCollection[] toReturn = _errors;
                                _errors = ArrayPool<ErrorCollection>.Shared.Rent(errorDepth + 1);
                                toReturn.AsSpan().CopyTo(_errors);
                                
                                // We only do spares writes, clear the remainder.
                                _errors.AsSpan(toReturn.Length).Clear();
                                ArrayPool<ErrorCollection>.Shared.Return(toReturn);
                            }

                            _errors[errorDepth].Add(errorCode);
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

        private unsafe struct ErrorCollection
        {
            // As of OpenSSL 1.1.1 there are 74 defined X509_V_ERR values,
            // therefore it fits in a bitvector backed by 3 ints (96 bits available).
            private const int BucketCount = 3;
            private const int OverflowValue = BucketCount * sizeof(int) * 8 - 1;
            private fixed int _codes[BucketCount];

            internal bool HasOverflow => _codes[2] < 0;

            internal bool HasErrors =>
                _codes[0] != 0 || _codes[1] != 0 || _codes[2] != 0;

            internal void Add(Interop.Crypto.X509VerifyStatusCode statusCode)
            {
                int bucket = FindBucket(statusCode, out int bitValue);
                _codes[bucket] |= bitValue;
            }

            public void ClearError(Interop.Crypto.X509VerifyStatusCode statusCode)
            {
                int bucket = FindBucket(statusCode, out int bitValue);
                _codes[bucket] &= ~bitValue;
            }

            internal bool HasError(Interop.Crypto.X509VerifyStatusCode statusCode)
            {
                int bucket = FindBucket(statusCode, out int bitValue);
                return (_codes[bucket] & bitValue) != 0;
            }

            public Enumerator GetEnumerator()
            {
                if (HasOverflow)
                {
                    throw new CryptographicException();
                }

                return new Enumerator(this);
            }

            private static int FindBucket(Interop.Crypto.X509VerifyStatusCode statusCode, out int bitValue)
            {
                int val = (int)statusCode;

                int bucket;

                if (val >= OverflowValue)
                {
                    Debug.Fail($"Out of range X509VerifyStatusCode returned {val} >= {OverflowValue}");
                    bucket = BucketCount - 1;
                    bitValue = 1 << 31;
                }
                else
                {
                    bucket = Math.DivRem(val, 32, out int localBitNumber);
                    bitValue = (1 << localBitNumber);
                }

                return bucket;
            }

            internal struct Enumerator
            {
                private ErrorCollection _collection;
                private int _lastBucket;
                private int _lastBit;

                internal Enumerator(ErrorCollection coll)
                {
                    _collection = coll;
                    _lastBucket = -1;
                    _lastBit = -1;
                }

                public bool MoveNext()
                {
                    if (_lastBucket >= BucketCount)
                    {
                        return false;
                    }

FindNextBit:
                    if (_lastBit == -1)
                    {
                        _lastBucket++;

                        while (_lastBucket < BucketCount && _collection._codes[_lastBucket] == 0)
                        {
                            _lastBucket++;
                        }

                        if (_lastBucket >= BucketCount)
                        {
                            return false;
                        }
                    }

                    _lastBit++;
                    int val = _collection._codes[_lastBucket];

                    while (_lastBit < 32)
                    {
                        if ((val & (1 << _lastBit)) != 0)
                        {
                            return true;
                        }

                        _lastBit++;
                    }

                    _lastBit = -1;
                    goto FindNextBit;
                }

                public Interop.Crypto.X509VerifyStatusCode Current =>
                    _lastBit == -1 ?
                        Interop.Crypto.X509VerifyStatusCode.X509_V_OK :
                        (Interop.Crypto.X509VerifyStatusCode)(_lastBit + 32 * _lastBucket);
            }
        }
    }
}
