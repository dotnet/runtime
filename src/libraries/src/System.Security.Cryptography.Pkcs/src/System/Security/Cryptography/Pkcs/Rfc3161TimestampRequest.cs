// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Rfc3161TimestampRequest
    {
        private byte[] _encodedBytes;
        private Rfc3161TimeStampReq _parsedData;

        private Rfc3161TimestampRequest()
        {
        }

        public int Version => _parsedData.Version;
        public ReadOnlyMemory<byte> GetMessageHash() => _parsedData.MessageImprint.HashedMessage;
        public Oid HashAlgorithmId => _parsedData.MessageImprint.HashAlgorithm.Algorithm;
        public Oid RequestedPolicyId => _parsedData.ReqPolicy;
        public bool RequestSignerCertificate => _parsedData.CertReq;
        public ReadOnlyMemory<byte>? GetNonce() => _parsedData.Nonce;
        public bool HasExtensions => _parsedData.Extensions?.Length > 0;

        public X509ExtensionCollection GetExtensions()
        {
            var coll = new X509ExtensionCollection();

            if (!HasExtensions)
            {
                return coll;
            }

            X509ExtensionAsn[] rawExtensions = _parsedData.Extensions;

            foreach (X509ExtensionAsn rawExtension in rawExtensions)
            {
                X509Extension extension = new X509Extension(
                    rawExtension.ExtnId,
                    rawExtension.ExtnValue.ToArray(),
                    rawExtension.Critical);

                // Currently there are no extensions defined.
                // Should this dip into CryptoConfig or other extensible
                // mechanisms for the CopyTo rich type uplift?
                coll.Add(extension);
            }

            return coll;
        }

        public Rfc3161TimestampToken ProcessResponse(ReadOnlyMemory<byte> source, out int bytesConsumed)
        {
            Rfc3161RequestResponseStatus status;
            Rfc3161TimestampToken token;

            if (ProcessResponse(source, out token, out status, out int localBytesRead, shouldThrow: true))
            {
                Debug.Assert(status == Rfc3161RequestResponseStatus.Accepted);
                bytesConsumed = localBytesRead;
                return token;
            }

            Debug.Fail($"AcceptResponse should have thrown or returned true (status={status})");
            throw new CryptographicException();
        }

        private bool ProcessResponse(
            ReadOnlyMemory<byte> source,
            out Rfc3161TimestampToken token,
            out Rfc3161RequestResponseStatus status,
            out int bytesConsumed,
            bool shouldThrow)
        {
            status = Rfc3161RequestResponseStatus.Unknown;
            token = null;

            Rfc3161TimeStampResp resp;

            try
            {
                AsnReader reader = new AsnReader(source, AsnEncodingRules.DER);
                int localBytesRead = reader.PeekEncodedValue().Length;

                Rfc3161TimeStampResp.Decode(reader, out resp);
                bytesConsumed = localBytesRead;
            }
            catch (CryptographicException) when (!shouldThrow)
            {
                bytesConsumed = 0;
                status = Rfc3161RequestResponseStatus.DoesNotParse;
                return false;
            }

            // bytesRead will be set past this point

            PkiStatus pkiStatus = (PkiStatus)resp.Status.Status;

            if (pkiStatus != PkiStatus.Granted &&
                pkiStatus != PkiStatus.GrantedWithMods)
            {
                if (shouldThrow)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_TimestampReq_Failure,
                            pkiStatus,
                            resp.Status.FailInfo.GetValueOrDefault()));
                }

                status = Rfc3161RequestResponseStatus.RequestFailed;
                return false;
            }

            if (!Rfc3161TimestampToken.TryDecode(resp.TimeStampToken.GetValueOrDefault(), out token, out _))
            {
                if (shouldThrow)
                {
                    throw new CryptographicException(SR.Cryptography_TimestampReq_BadResponse);
                }

                bytesConsumed = 0;
                status = Rfc3161RequestResponseStatus.DoesNotParse;
                return false;
            }

            status = ValidateResponse(token, shouldThrow);
            return status == Rfc3161RequestResponseStatus.Accepted;
        }

        public byte[] Encode()
        {
            return _encodedBytes.CloneByteArray();
        }

        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < _encodedBytes.Length)
            {
                bytesWritten = 0;
                return false;
            }

            _encodedBytes.AsSpan().CopyTo(destination);
            bytesWritten = _encodedBytes.Length;
            return true;
        }

        public static Rfc3161TimestampRequest CreateFromSignerInfo(
            SignerInfo signerInfo,
            HashAlgorithmName hashAlgorithm,
            Oid requestedPolicyId = null,
            ReadOnlyMemory<byte>? nonce = null,
            bool requestSignerCertificates = false,
            X509ExtensionCollection extensions = null)
        {
            if (signerInfo == null)
            {
                throw new ArgumentNullException(nameof(signerInfo));
            }

            // https://tools.ietf.org/html/rfc3161, Appendix A.
            //
            // The value of messageImprint field within TimeStampToken shall be a
            // hash of the value of signature field within SignerInfo for the
            // signedData being time-stamped.
            return CreateFromData(
                signerInfo.GetSignature(),
                hashAlgorithm,
                requestedPolicyId,
                nonce,
                requestSignerCertificates,
                extensions);
        }

        public static Rfc3161TimestampRequest CreateFromData(
            ReadOnlySpan<byte> data,
            HashAlgorithmName hashAlgorithm,
            Oid requestedPolicyId = null,
            ReadOnlyMemory<byte>? nonce = null,
            bool requestSignerCertificates = false,
            X509ExtensionCollection extensions = null)
        {
            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm))
            {
                hasher.AppendData(data);
                byte[] digest = hasher.GetHashAndReset();

                return CreateFromHash(
                    digest,
                    hashAlgorithm,
                    requestedPolicyId,
                    nonce,
                    requestSignerCertificates,
                    extensions);
            }
        }

        public static Rfc3161TimestampRequest CreateFromHash(
            ReadOnlyMemory<byte> hash,
            HashAlgorithmName hashAlgorithm,
            Oid requestedPolicyId = null,
            ReadOnlyMemory<byte>? nonce = null,
            bool requestSignerCertificates = false,
            X509ExtensionCollection extensions = null)
        {
            string oidStr = PkcsHelpers.GetOidFromHashAlgorithm(hashAlgorithm);
            
            return CreateFromHash(
                hash,
                new Oid(oidStr, oidStr),
                requestedPolicyId,
                nonce,
                requestSignerCertificates,
                extensions);
        }

        /// <summary>
        /// Create a timestamp request using a pre-computed hash value.
        /// </summary>
        /// <param name="hash">The pre-computed hash value to be timestamped.</param>
        /// <param name="hashAlgorithmId">
        ///   The Object Identifier (OID) for the hash algorithm which produced <paramref name="hash"/>.
        /// </param>
        /// <param name="requestedPolicyId">
        ///   The Object Identifier (OID) for a timestamp policy the Timestamp Authority (TSA) should use,
        ///   or <c>null</c> to express no preference.
        /// </param>
        /// <param name="nonce">
        ///   An optional nonce (number used once) to uniquely identify this request to pair it with the response.
        ///   The value is interpreted as an unsigned big-endian integer and may be normalized to the encoding format.
        /// </param>
        /// <param name="requestSignerCertificates">
        ///   Indicates whether the Timestamp Authority (TSA) must (<c>true</c>) or must not (<c>false</c>) include
        ///   the signing certificate in the issued timestamp token.
        /// </param>
        /// <param name="extensions">RFC3161 extensions to present with the request.</param>
        /// <returns>
        ///   An <see cref="Rfc3161TimestampRequest"/> representing the chosen values.
        /// </returns>
        /// <seealso cref="Encode"/>
        /// <seealso cref="TryEncode"/>
        public static Rfc3161TimestampRequest CreateFromHash(
            ReadOnlyMemory<byte> hash,
            Oid hashAlgorithmId,
            Oid requestedPolicyId = null,
            ReadOnlyMemory<byte>? nonce = null,
            bool requestSignerCertificates = false,
            X509ExtensionCollection extensions = null)
        {
            // Normalize the nonce:
            if (nonce.HasValue)
            {
                ReadOnlyMemory<byte> nonceMemory = nonce.Value;
                ReadOnlySpan<byte> nonceSpan = nonceMemory.Span;

                // If it's empty, or it would be negative, insert the requisite byte.
                if (nonceSpan.Length == 0 || nonceSpan[0] >= 0x80)
                {
                    byte[] temp = new byte[nonceSpan.Length + 1];
                    nonceSpan.CopyTo(temp.AsSpan(1));
                    nonce = temp;
                }
                else
                {
                    int slice = 0;

                    // Find all extra leading 0x00 values and trim them off.
                    while (slice < nonceSpan.Length && nonceSpan[slice] == 0)
                    {
                        slice++;
                    }

                    // Back up one if it was all zero, or we turned the number negative.
                    if (slice == nonceSpan.Length || nonceSpan[slice] >= 0x80)
                    {
                        slice--;
                    }

                    nonce = nonceMemory.Slice(slice);
                }
            }

            var req = new Rfc3161TimeStampReq
            {
                Version = 1,
                MessageImprint = new MessageImprint
                {
                    HashAlgorithm =
                    {
                        Algorithm = hashAlgorithmId,
                        Parameters = AlgorithmIdentifierAsn.ExplicitDerNull,
                    },

                    HashedMessage = hash,
                },
                ReqPolicy = requestedPolicyId,
                CertReq = requestSignerCertificates,
                Nonce = nonce,
            };

            if (extensions != null)
            {
                req.Extensions =
                    extensions.OfType<X509Extension>().Select(e => new X509ExtensionAsn(e)).ToArray();
            }

            // The RFC implies DER (see TryParse), and DER is the most widely understood given that
            // CER isn't specified.
            const AsnEncodingRules ruleSet = AsnEncodingRules.DER;
            using (AsnWriter writer = new AsnWriter(ruleSet))
            {
                req.Encode(writer);

                byte[] encodedBytes = writer.Encode();

                // Make sure everything normalizes
                req = Rfc3161TimeStampReq.Decode(encodedBytes, ruleSet);

                return new Rfc3161TimestampRequest
                {
                    _encodedBytes = writer.Encode(),
                    _parsedData = req,
                };
            }
        }

        public static bool TryDecode(
            ReadOnlyMemory<byte> encodedBytes,
            out Rfc3161TimestampRequest request,
            out int bytesConsumed)
        {
            try
            {
                // RFC 3161 doesn't have a concise statement that TimeStampReq will
                // be DER encoded, but under the email protocol (3.1), file protocol (3.2),
                // socket protocol (3.3) and HTTP protocol (3.4) they all say DER for the
                // transmission.
                //
                // Since nothing says BER, assume DER only.
                const AsnEncodingRules RuleSet = AsnEncodingRules.DER;

                AsnReader reader = new AsnReader(encodedBytes, RuleSet);
                ReadOnlyMemory<byte> firstElement = reader.PeekEncodedValue();

                Rfc3161TimeStampReq.Decode(reader, out Rfc3161TimeStampReq req);

                request = new Rfc3161TimestampRequest
                {
                    _parsedData = req,
                    _encodedBytes = firstElement.ToArray(),
                };

                bytesConsumed = firstElement.Length;
                return true;
            }
            catch (CryptographicException)
            {
            }

            request = null;
            bytesConsumed = 0;
            return false;
        }

        private Rfc3161RequestResponseStatus ValidateResponse(
            Rfc3161TimestampToken token,
            bool shouldThrow)
        {
            Debug.Assert(token != null);

            // This method validates the acceptance criteria sprinkled throughout the
            // field descriptions in https://tools.ietf.org/html/rfc3161#section-2.4.1 and
            // https://tools.ietf.org/html/rfc3161#section-2.4.2

            if (!token.VerifyHash(GetMessageHash().Span, HashAlgorithmId.Value))
            {
                if (shouldThrow)
                {
                    throw new CryptographicException(SR.Cryptography_BadHashValue);
                }

                return Rfc3161RequestResponseStatus.HashMismatch;
            }

            Rfc3161TimestampTokenInfo tokenInfo = token.TokenInfo;

            // We only understand V1 messaging and validation
            if (tokenInfo.Version != 1)
            {
                if (shouldThrow)
                {
                    throw new CryptographicException(SR.Cryptography_TimestampReq_BadResponse);
                }

                return Rfc3161RequestResponseStatus.VersionTooNew;
            }

            // reqPolicy is what the policy SHOULD be, so we can't reject it here.

            ReadOnlyMemory<byte>? requestNonce = GetNonce();
            ReadOnlyMemory<byte>? responseNonce = tokenInfo.GetNonce();

            // The RFC says that if a nonce was in the request it MUST be present in
            // the response and it MUST be equal.
            //
            // It does not say that if no nonce was requested that the response MUST NOT include one, so
            // don't check anything if no nonce was requested.
            if (requestNonce != null)
            {
                if (responseNonce == null ||
                    !requestNonce.Value.Span.SequenceEqual(responseNonce.Value.Span))
                {
                    if (shouldThrow)
                    {
                        throw new CryptographicException(SR.Cryptography_TimestampReq_BadNonce);
                    }

                    return Rfc3161RequestResponseStatus.NonceMismatch;
                }
            }

            SignedCms tokenCms = token.AsSignedCms();

            if (RequestSignerCertificate)
            {
                // If the certificate was requested it
                // A) MUST be present in token.AsSignedCms().Certificates
                // B) the ESSCertID(2) identifier MUST be correct.
                //
                // Other certificates are permitted, and will not be validated.

                if (tokenCms.SignerInfos[0].Certificate == null)
                {
                    if (shouldThrow)
                    {
                        throw new CryptographicException(SR.Cryptography_TimestampReq_NoCertFound);
                    }

                    return Rfc3161RequestResponseStatus.RequestedCertificatesMissing;
                }
            }
            else
            {
                // If no certificate was requested then the CMS Certificates collection
                // MUST be empty.

                if (tokenCms.Certificates.Count != 0)
                {
                    if (shouldThrow)
                    {
                        throw new CryptographicException(SR.Cryptography_TimestampReq_UnexpectedCertFound);
                    }

                    return Rfc3161RequestResponseStatus.UnexpectedCertificates;
                }
            }

            return Rfc3161RequestResponseStatus.Accepted;
        }
    }
}
