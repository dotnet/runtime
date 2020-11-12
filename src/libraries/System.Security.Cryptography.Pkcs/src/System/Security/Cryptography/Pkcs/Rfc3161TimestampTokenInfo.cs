// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    /// <summary>
    /// Represents the timestamp token information class defined in RFC3161 as TSTInfo.
    /// </summary>
    public sealed class Rfc3161TimestampTokenInfo
    {
        private readonly byte[] _encodedBytes;
        private readonly Rfc3161TstInfo _parsedData;
        private Oid? _policyOid;
        private Oid? _hashAlgorithmId;
        private ReadOnlyMemory<byte>? _tsaNameBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rfc3161TimestampTokenInfo" /> class with the specified parameters.
        /// </summary>
        /// <param name="policyId">An OID representing the TSA's policy under which the response was produced.</param>
        /// <param name="hashAlgorithmId">A hash algorithm OID of the data to be timestamped.</param>
        /// <param name="messageHash">A hash value of the data to be timestamped.</param>
        /// <param name="serialNumber">An integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.</param>
        /// <param name="timestamp">The timestamp encoded in the token.</param>
        /// <param name="accuracyInMicroseconds">The accuracy with which <paramref name="timestamp"/> is compared. Also see <paramref name="isOrdering"/>.</param>
        /// <param name="isOrdering"><see langword="true" /> to ensure that every timestamp token from the same TSA can always be ordered based on the <paramref name="timestamp"/>, regardless of the accuracy; <see langword="false" /> to make <paramref name="timestamp"/> indicate when token has been created by the TSA.</param>
        /// <param name="nonce">An arbitrary number that can be used only once. Using a nonce always allows to detect replays, and hence its use is recommended.</param>
        /// <param name="tsaName">The hint in the TSA name identification. The actual identification of the entity that signed the response will always occur through the use of the certificate identifier.</param>
        /// <param name="extensions">A collection of X509 extensions.</param>
        /// <remarks>If <paramref name="hashAlgorithmId" />, <paramref name="messageHash" />, <paramref name="policyId" /> or <paramref name="nonce" /> are present in the <see cref="Rfc3161TimestampRequest"/>, then the same value should be used. If <paramref name="accuracyInMicroseconds"/> is not provided, then the accuracy may be available through other means such as i.e. <paramref name="policyId" />.</remarks>
        /// <exception cref="CryptographicException">ASN.1 corrupted data.</exception>
        public Rfc3161TimestampTokenInfo(
            Oid policyId,
            Oid hashAlgorithmId,
            ReadOnlyMemory<byte> messageHash,
            ReadOnlyMemory<byte> serialNumber,
            DateTimeOffset timestamp,
            long? accuracyInMicroseconds = null,
            bool isOrdering = false,
            ReadOnlyMemory<byte>? nonce = null,
            ReadOnlyMemory<byte>? tsaName = null,
            X509ExtensionCollection? extensions = null)
        {
            _encodedBytes = Encode(
                policyId,
                hashAlgorithmId,
                messageHash,
                serialNumber,
                timestamp,
                isOrdering,
                accuracyInMicroseconds,
                nonce,
                tsaName,
                extensions);

            if (!TryDecode(_encodedBytes, true, out _parsedData, out _, out _))
            {
                Debug.Fail("Unable to decode the data we encoded");
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private Rfc3161TimestampTokenInfo(byte[] copiedBytes, Rfc3161TstInfo tstInfo)
        {
            _encodedBytes = copiedBytes;
            _parsedData = tstInfo;
        }

        /// <summary>
        /// The version of the timestamp request.
        /// </summary>
        public int Version => _parsedData.Version;

        /// <summary>
        /// An OID representing the TSA's policy under which the response was produced.
        /// </summary>
        public Oid PolicyId => (_policyOid ??= new Oid(_parsedData.Policy, null));

        /// <summary>
        /// An OID of the hash algorithm.
        /// </summary>
        public Oid HashAlgorithmId => (_hashAlgorithmId ??= new Oid(_parsedData.MessageImprint.HashAlgorithm.Algorithm, null));

        /// <summary>
        /// The data representing the message hash.
        /// </summary>
        public ReadOnlyMemory<byte> GetMessageHash() => _parsedData.MessageImprint.HashedMessage;

        /// <summary>
        /// An integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.
        /// </summary>
        public ReadOnlyMemory<byte> GetSerialNumber() => _parsedData.SerialNumber;

        /// <summary>
        /// The timestamp encoded in the token.
        /// </summary>
        public DateTimeOffset Timestamp => _parsedData.GenTime;

        /// <summary>
        /// The accuracy with which <see cref="Timestamp"/> is compared. Also see <see cref="IsOrdering"/>.
        /// </summary>
        public long? AccuracyInMicroseconds => _parsedData.Accuracy?.TotalMicros;

        /// <summary>
        /// Gets a value indicating if every timestamp token from the same TSA can always be ordered based on the <see cref="Timestamp"/>, regardless of the accuracy; If <see langword="false" />, <see cref="Timestamp"/> indicates when the token has been created by the TSA.
        /// </summary>
        public bool IsOrdering => _parsedData.Ordering;

        /// <summary>
        /// An arbitrary number that can be used only once.
        /// </summary>
        public ReadOnlyMemory<byte>? GetNonce() => _parsedData.Nonce;

        /// <summary>
        /// Gets a value indicating whether there are any X509 extensions.
        /// </summary>
        public bool HasExtensions => _parsedData.Extensions?.Length > 0;

        /// <summary>
        /// Gets the data representing the hint in the TSA name identification.
        /// The actual identification of the entity that signed the response
        /// will always occur through the use of the certificate identifier (ESSCertID Attribute)
        /// inside a SigningCertificate attribute which is part of the signer info.
        /// </summary>
        public ReadOnlyMemory<byte>? GetTimestampAuthorityName()
        {
            if (_tsaNameBytes == null)
            {
                GeneralNameAsn? tsaName = _parsedData.Tsa;

                if (tsaName == null)
                {
                    return null;
                }

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                tsaName.Value.Encode(writer);
                _tsaNameBytes = writer.Encode();
                Debug.Assert(_tsaNameBytes.HasValue);
            }

            return _tsaNameBytes.Value;
        }

        /// <summary>
        /// Returns a collection of X509 certificates.
        /// </summary>
        public X509ExtensionCollection GetExtensions()
        {
            var coll = new X509ExtensionCollection();

            if (!HasExtensions)
            {
                return coll;
            }

            X509ExtensionAsn[] rawExtensions = _parsedData.Extensions!;

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

        /// <summary>
        /// Returns a byte array representing ASN.1 encoded data.
        /// </summary>
        public byte[] Encode()
        {
            return _encodedBytes.CloneByteArray();
        }

        /// <summary>
        /// Gets the ASN.1 encoded data.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the bytes written to the <paramref name="destination" /> buffer.</param>
        /// <returns><see langword="true" /> if the operation succeeded; <see langword="false" /> if the buffer size was insufficient.</returns>
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

        /// <summary>
        /// Decodes the ASN.1 encoded data.
        /// </summary>
        /// <param name="source">The input or source buffer.</param>
        /// <param name="timestampTokenInfo">When this method returns <see langword="true" />, the decoded data. When this method returns <see langword="false" />, the value is <see langword="null" />, meaning the data could not be decoded.</param>
        /// <param name="bytesConsumed">The number of bytes used for decoding.</param>
        /// <returns><see langword="true" /> if the operation succeeded; <see langword="false" /> otherwise.</returns>
        public static bool TryDecode(
            ReadOnlyMemory<byte> source,
            [NotNullWhen(true)] out Rfc3161TimestampTokenInfo? timestampTokenInfo,
            out int bytesConsumed)
        {
            if (TryDecode(source, false, out Rfc3161TstInfo tstInfo, out bytesConsumed, out byte[]? copiedBytes))
            {
                timestampTokenInfo = new Rfc3161TimestampTokenInfo(copiedBytes!, tstInfo);
                return true;
            }

            bytesConsumed = 0;
            timestampTokenInfo = null;
            return false;
        }

        private static bool TryDecode(
            ReadOnlyMemory<byte> source,
            bool ownsMemory,
            out Rfc3161TstInfo tstInfo,
            out int bytesConsumed,
            out byte[]? copiedBytes)
        {
            // https://tools.ietf.org/html/rfc3161#section-2.4.2
            // The eContent SHALL be the DER-encoded value of TSTInfo.
            AsnReader reader = new AsnReader(source, AsnEncodingRules.DER);

            try
            {
                ReadOnlyMemory<byte> firstElement = reader.PeekEncodedValue();

                if (ownsMemory)
                {
                    copiedBytes = null;
                }
                else
                {
                    // Copy the data so no ReadOnlyMemory values are pointing back to user data.
                    copiedBytes = firstElement.ToArray();
                    firstElement = copiedBytes;
                }

                Rfc3161TstInfo parsedInfo = Rfc3161TstInfo.Decode(firstElement, AsnEncodingRules.DER);

                // The deserializer doesn't do bounds checks.
                // Micros and Millis are defined as (1..999)
                // Seconds doesn't define that it's bounded by 0,
                // but negative accuracy doesn't make sense.
                //
                // (Reminder to readers: a null int? with an inequality operator
                // has the value false, so if accuracy is missing, or millis or micro is missing,
                // then the respective checks return false and don't throw).
                if (parsedInfo.Accuracy?.Micros > 999 ||
                    parsedInfo.Accuracy?.Micros < 1 ||
                    parsedInfo.Accuracy?.Millis > 999 ||
                    parsedInfo.Accuracy?.Millis < 1 ||
                    parsedInfo.Accuracy?.Seconds < 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                tstInfo = parsedInfo;
                bytesConsumed = firstElement.Length;
                return true;
            }
            catch (AsnContentException)
            {
                tstInfo = default;
                bytesConsumed = 0;
                copiedBytes = null;
                return false;
            }
            catch (CryptographicException)
            {
                tstInfo = default;
                bytesConsumed = 0;
                copiedBytes = null;
                return false;
            }
        }

        private static byte[] Encode(
            Oid policyId,
            Oid hashAlgorithmId,
            ReadOnlyMemory<byte> messageHash,
            ReadOnlyMemory<byte> serialNumber,
            DateTimeOffset timestamp,
            bool isOrdering,
            long? accuracyInMicroseconds,
            ReadOnlyMemory<byte>? nonce,
            ReadOnlyMemory<byte>? tsaName,
            X509ExtensionCollection? extensions)
        {
            if (policyId == null)
                throw new ArgumentNullException(nameof(policyId));
            if (hashAlgorithmId == null)
                throw new ArgumentNullException(nameof(hashAlgorithmId));

            var tstInfo = new Rfc3161TstInfo
            {
                // The only legal value as of 2017.
                Version = 1,
                Policy = policyId.Value!,
                MessageImprint =
                {
                    HashAlgorithm =
                    {
                        Algorithm = hashAlgorithmId.Value!,
                        Parameters = AlgorithmIdentifierAsn.ExplicitDerNull,
                    },

                    HashedMessage = messageHash,
                },
                SerialNumber = serialNumber,
                GenTime = timestamp,
                Ordering = isOrdering,
                Nonce = nonce,
            };

            if (accuracyInMicroseconds != null)
            {
                tstInfo.Accuracy = new Rfc3161Accuracy(accuracyInMicroseconds.Value);
            }

            if (tsaName != null)
            {
                tstInfo.Tsa = GeneralNameAsn.Decode(tsaName.Value, AsnEncodingRules.DER);
            }

            if (extensions != null)
            {
                tstInfo.Extensions = extensions.OfType<X509Extension>().
                    Select(ex => new X509ExtensionAsn(ex)).ToArray();
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            tstInfo.Encode(writer);
            return writer.Encode();
        }
    }
}
