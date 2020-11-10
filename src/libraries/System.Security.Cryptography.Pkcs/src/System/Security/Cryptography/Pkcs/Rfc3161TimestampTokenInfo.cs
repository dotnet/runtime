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
    /// Represents time stamp token info class defined in RFC3161 as TSTInfo.
    /// </summary>
    public sealed class Rfc3161TimestampTokenInfo
    {
        private readonly byte[] _encodedBytes;
        private readonly Rfc3161TstInfo _parsedData;
        private Oid? _policyOid;
        private Oid? _hashAlgorithmId;
        private ReadOnlyMemory<byte>? _tsaNameBytes;

        /// <summary>
        /// Initializes a new instance of the Rfc3161TimestampTokenInfo class.
        /// </summary>
        /// <param name="policyId">An OID representing TSA's policy under which the response was produced.</param>
        /// <param name="hashAlgorithmId">A hash algorithm OID of the data to be time-stamped./param>
        /// <param name="messageHash">A hash value of the data to be time-stamped.</param>
        /// <param name="serialNumber">An integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.</param>
        /// <param name="timestamp">Timestamp encoded in the token.</param>
        /// <param name="accuracyInMicroseconds">Accuracy with which <paramref name="timestamp"/> is compared. Also see <paramref name="isOrdering"/>.</param>
        /// <param name="isOrdering">true to ensure that every time-stamp token from the same TSA can always be ordered based on the <paramref name="timestamp"/>, regardless of the accuracy; false to make <paramref name="timestamp"/> indicate when token has been created by the TSA.</param>
        /// <param name="nonce">An arbitrary number that can be used only once. Using a nonce always allows to detect replays, and hence its use is recommended.</param>
        /// <param name="tsaName">Hint in the TSA name identification. The actual identification of the entity that signed the response will always occur through the use of the certificate identifier.</param>
        /// <param name="extensions">Collection of X509 extensions.</param>
        /// <remarks>If hash OID, message hash, policy or nonce is present in the <see cref="Rfc3161TimestampRequest"/>, then the same value should be used. If <paramref name="accuracyInMicroseconds"/> is not provided, then the accuracy may be available through other means such as i.e. policy.</remarks>
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
        /// version of the Time-Stamp request.
        /// </summary>
        public int Version => _parsedData.Version;

        /// <summary>
        /// An OID representing TSA's policy under which the response was produced.
        /// </summary>
        public Oid PolicyId => (_policyOid ??= new Oid(_parsedData.Policy, null));

        /// <summary>
        /// An OID of the hash algorithm.
        /// </summary>
        public Oid HashAlgorithmId => (_hashAlgorithmId ??= new Oid(_parsedData.MessageImprint.HashAlgorithm.Algorithm, null));

        /// <summary>
        /// Data representing message hash.
        /// </summary>
        public ReadOnlyMemory<byte> GetMessageHash() => _parsedData.MessageImprint.HashedMessage;

        /// <summary>
        /// An integer assigned by the TSA to the <see cref="Rfc3161TimestampTokenInfo"/>.
        /// </summary>
        public ReadOnlyMemory<byte> GetSerialNumber() => _parsedData.SerialNumber;

        /// <summary>
        /// Timestamp encoded in the token.
        /// </summary>
        public DateTimeOffset Timestamp => _parsedData.GenTime;

        /// <summary>
        /// Accuracy with which <see cref="Timestamp"/> is compared. Also see <see cref="IsOrdering"/>.
        /// </summary>
        public long? AccuracyInMicroseconds => _parsedData.Accuracy?.TotalMicros;

        /// <summary>
        /// Gets a value indicating if every time-stamp token from the same TSA can always be ordered based on the <see cref="Timestamp"/>, regardless of the accuracy; If false <see cref="Timestamp"/> indicate when token has been created by the TSA.
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
        /// Gets data representing hint in the TSA name identification.
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
        /// Returns collection of X509 certificates.
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
        /// Returns byte array representing ASN.1 encoded data.
        /// </summary>
        public byte[] Encode()
        {
            return _encodedBytes.CloneByteArray();
        }

        /// <summary>
        /// Gets ASN.1 encoded data.
        /// </summary>
        /// <param name="destination">Destination buffer.</param>
        /// <param name="bytesWritten">Outputs bytes written to destination buffer.</param>
        /// <returns>true if operation succeeded; false if buffer size was insufficient.</returns>
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
        /// Decodes ASN.1 encoded data.
        /// </summary>
        /// <param name="source">Input or source buffer.</param>
        /// <param name="timestampTokenInfo">Class representing decoded data or null when data could not be decoded.</param>
        /// <param name="bytesConsumed">Number of bytes used for decoding.</param>
        /// <returns>true if operation succeeded; false otherwise.</returns>
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
