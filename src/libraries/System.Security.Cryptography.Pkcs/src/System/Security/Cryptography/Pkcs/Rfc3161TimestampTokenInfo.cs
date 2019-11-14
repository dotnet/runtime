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
    public sealed class Rfc3161TimestampTokenInfo
    {
        private readonly byte[] _encodedBytes;
        private readonly Rfc3161TstInfo _parsedData;
        private ReadOnlyMemory<byte>? _tsaNameBytes;

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
            X509ExtensionCollection extensions = null)
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

        public int Version => _parsedData.Version;
        public Oid PolicyId => _parsedData.Policy;
        public Oid HashAlgorithmId => _parsedData.MessageImprint.HashAlgorithm.Algorithm;
        public ReadOnlyMemory<byte> GetMessageHash() => _parsedData.MessageImprint.HashedMessage;
        public ReadOnlyMemory<byte> GetSerialNumber() => _parsedData.SerialNumber;
        public DateTimeOffset Timestamp => _parsedData.GenTime;
        public long? AccuracyInMicroseconds => _parsedData.Accuracy?.TotalMicros;
        public bool IsOrdering => _parsedData.Ordering;
        public ReadOnlyMemory<byte>? GetNonce() => _parsedData.Nonce;
        public bool HasExtensions => _parsedData.Extensions?.Length > 0;

        public ReadOnlyMemory<byte>? GetTimestampAuthorityName()
        {
            if (_tsaNameBytes == null)
            {
                GeneralNameAsn? tsaName = _parsedData.Tsa;

                if (tsaName == null)
                {
                    return null;
                }

                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    tsaName.Value.Encode(writer);
                    _tsaNameBytes = writer.Encode();
                    Debug.Assert(_tsaNameBytes.HasValue);
                }
            }

            return _tsaNameBytes.Value;
        }

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

        public static bool TryDecode(
            ReadOnlyMemory<byte> source,
            out Rfc3161TimestampTokenInfo timestampTokenInfo,
            out int bytesConsumed)
        {
            if (TryDecode(source, false, out Rfc3161TstInfo tstInfo, out bytesConsumed, out byte[] copiedBytes))
            {
                timestampTokenInfo = new Rfc3161TimestampTokenInfo(copiedBytes, tstInfo);
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
            out byte[] copiedBytes)
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
            X509ExtensionCollection extensions)
        {
            if (policyId == null)
                throw new ArgumentNullException(nameof(policyId));
            if (hashAlgorithmId == null)
                throw new ArgumentNullException(nameof(hashAlgorithmId));

            var tstInfo = new Rfc3161TstInfo
            {
                // The only legal value as of 2017.
                Version = 1,
                Policy = policyId,
                MessageImprint =
                {
                    HashAlgorithm =
                    {
                        Algorithm = hashAlgorithmId,
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

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                tstInfo.Encode(writer);
                return writer.Encode();
            }
        }
    }
}
