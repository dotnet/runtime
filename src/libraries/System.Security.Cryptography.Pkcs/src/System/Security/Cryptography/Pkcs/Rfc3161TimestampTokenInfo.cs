// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
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
        /// <param name="nonce">The nonce associated with this timestamp token. Using a nonce always allows to detect replays, and hence its use is recommended.</param>
        /// <param name="timestampAuthorityName">The hint in the TSA name identification. The actual identification of the entity that signed the response will always occur through the use of the certificate identifier.</param>
        /// <param name="extensions">The extension values associated with the timestamp.</param>
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
            ReadOnlyMemory<byte>? timestampAuthorityName = null,
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
                timestampAuthorityName,
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
        /// Gets the version of the timestamp token.
        /// </summary>
        /// <value>The version of the timestamp token.</value>
        public int Version => _parsedData.Version;

        /// <summary>
        /// Gets an OID representing the TSA's policy under which the response was produced.
        /// </summary>
        /// <value>An OID representing the TSA's policy under which the response was produced.</value>
        public Oid PolicyId => (_policyOid ??= new Oid(_parsedData.Policy, null));

        /// <summary>
        /// Gets an OID of the hash algorithm.
        /// </summary>
        /// <value>An OID of the hash algorithm.</value>
        public Oid HashAlgorithmId => (_hashAlgorithmId ??= new Oid(_parsedData.MessageImprint.HashAlgorithm.Algorithm, null));

        /// <summary>
        /// Gets the data representing the message hash.
        /// </summary>
        /// <returns>The data representing the message hash.</returns>
        public ReadOnlyMemory<byte> GetMessageHash() => _parsedData.MessageImprint.HashedMessage;

        /// <summary>
        /// Gets an integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.
        /// </summary>
        /// <returns>An integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.</returns>
        public ReadOnlyMemory<byte> GetSerialNumber() => _parsedData.SerialNumber;

        /// <summary>
        /// Gets the timestamp encoded in the token.
        /// </summary>
        /// <value>The timestamp encoded in the token.</value>
        public DateTimeOffset Timestamp => _parsedData.GenTime;

        /// <summary>
        /// Gets the accuracy with which <see cref="Timestamp"/> is compared.
        /// </summary>
        /// <seealso cref="IsOrdering" />
        /// <value>The accuracy with which <see cref="Timestamp"/> is compared.</value>
        public long? AccuracyInMicroseconds => _parsedData.Accuracy?.TotalMicros;

        /// <summary>
        /// Gets a value indicating if every timestamp token from the same TSA can always be ordered based on the <see cref="Timestamp"/>, regardless of the accuracy; If <see langword="false" />, <see cref="Timestamp"/> indicates when the token has been created by the TSA.
        /// </summary>
        /// <value>A value indicating if every timestamp token from the same TSA can always be ordered based on the <see cref="Timestamp"/>.</value>
        public bool IsOrdering => _parsedData.Ordering;

        /// <summary>
        /// Gets the nonce associated with this timestamp token.
        /// </summary>
        /// <returns>The nonce associated with this timestamp token.</returns>
        public ReadOnlyMemory<byte>? GetNonce() => _parsedData.Nonce;

        /// <summary>
        /// Gets a value indicating whether there are any extensions associated with this timestamp token.
        /// </summary>
        /// <value>A value indicating whether there are any extensions associated with this timestamp token.</value>
        public bool HasExtensions => _parsedData.Extensions?.Length > 0;

        /// <summary>
        /// Gets the data representing the hint in the TSA name identification.
        /// </summary>
        /// <returns>The data representing the hint in the TSA name identification.</returns>
        /// <remarks>
        /// The actual identification of the entity that signed the response
        /// will always occur through the use of the certificate identifier (ESSCertID Attribute)
        /// inside a SigningCertificate attribute which is part of the signer info.
        /// </remarks>
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
        /// Gets the extension values associated with the timestamp.
        /// </summary>
        /// <returns>The extension values associated with the timestamp.</returns>
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
        /// Encodes this object into a TSTInfo value
        /// </summary>
        /// <returns>The encoded TSTInfo value.</returns>
        public byte[] Encode()
        {
            return _encodedBytes.CloneByteArray();
        }

        /// <summary>
        /// Attempts to encode this object as a TSTInfo value, writing the result into the provided buffer.
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
        /// Decodes an encoded TSTInfo value.
        /// </summary>
        /// <param name="encodedBytes">The input or source buffer.</param>
        /// <param name="timestampTokenInfo">When this method returns <see langword="true" />, the decoded data. When this method returns <see langword="false" />, the value is <see langword="null" />, meaning the data could not be decoded.</param>
        /// <param name="bytesConsumed">The number of bytes used for decoding.</param>
        /// <returns><see langword="true" /> if the operation succeeded; <see langword="false" /> otherwise.</returns>
        public static bool TryDecode(
            ReadOnlyMemory<byte> encodedBytes,
            [NotNullWhen(true)] out Rfc3161TimestampTokenInfo? timestampTokenInfo,
            out int bytesConsumed)
        {
            if (TryDecode(encodedBytes, false, out Rfc3161TstInfo tstInfo, out bytesConsumed, out byte[]? copiedBytes))
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
            if (policyId is null)
            {
                throw new ArgumentNullException(nameof(policyId));
            }
            if (hashAlgorithmId is null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithmId));
            }

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
                tstInfo.Extensions = new X509ExtensionAsn[extensions.Count];
                for (int i = 0; i < extensions.Count; i++)
                {
                    tstInfo.Extensions[i] = new X509ExtensionAsn(extensions[i]);
                }
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            tstInfo.Encode(writer);
            return writer.Encode();
        }
    }
}
