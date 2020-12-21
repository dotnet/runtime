// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for AsymmetricAlgorithm-derived classes that layer on OpenSSL.
    //
    internal static partial class AsymmetricAlgorithmHelpers
    {
        /// <summary>
        /// Convert Ieee1363 format of (r, s) to Der format
        /// </summary>
        public static byte[] ConvertIeee1363ToDer(ReadOnlySpan<byte> input)
        {
            AsnWriter writer = WriteIeee1363ToDer(input);
            return writer.Encode();
        }

        internal static bool TryConvertIeee1363ToDer(
            ReadOnlySpan<byte> input,
            Span<byte> destination,
            out int bytesWritten)
        {
            AsnWriter writer = WriteIeee1363ToDer(input);
            return writer.TryEncode(destination, out bytesWritten);
        }

        private static AsnWriter WriteIeee1363ToDer(ReadOnlySpan<byte> input)
        {
            Debug.Assert(input.Length % 2 == 0);
            Debug.Assert(input.Length > 1);

            // Input is (r, s), each of them exactly half of the array.
            // Output is the DER encoded value of SEQUENCE(INTEGER(r), INTEGER(s)).
            int halfLength = input.Length / 2;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteKeyParameterInteger(input.Slice(0, halfLength));
            writer.WriteKeyParameterInteger(input.Slice(halfLength, halfLength));
            writer.PopSequence();
            return writer;
        }

        /// <summary>
        /// Convert Der format of (r, s) to Ieee1363 format
        /// </summary>
        public static byte[] ConvertDerToIeee1363(ReadOnlySpan<byte> input, int fieldSizeBits)
        {
            int fieldSizeBytes = BitsToBytes(fieldSizeBits);
            int encodedSize = 2 * fieldSizeBytes;
            byte[] response = new byte[encodedSize];

            ConvertDerToIeee1363(input, fieldSizeBits, response);
            return response;
        }

        internal static int ConvertDerToIeee1363(ReadOnlySpan<byte> input, int fieldSizeBits, Span<byte> destination)
        {
            int fieldSizeBytes = BitsToBytes(fieldSizeBits);
            int encodedSize = 2 * fieldSizeBytes;

            Debug.Assert(destination.Length >= encodedSize);

            try
            {
                AsnValueReader reader = new AsnValueReader(input, AsnEncodingRules.DER);
                AsnValueReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();
                ReadOnlySpan<byte> rDer = sequenceReader.ReadIntegerBytes();
                ReadOnlySpan<byte> sDer = sequenceReader.ReadIntegerBytes();
                sequenceReader.ThrowIfNotEmpty();

                CopySignatureField(rDer, destination.Slice(0, fieldSizeBytes));
                CopySignatureField(sDer, destination.Slice(fieldSizeBytes, fieldSizeBytes));
                return encodedSize;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static int GetMaxDerSignatureSize(int fieldSizeBits)
        {
            // This encoding format is the DER-encoded representation of
            // SEQUENCE(INTEGER(r), INTEGER(s)).
            // Each of r and s are unsigned fieldSizeBits integers, and if byte-aligned
            // then they may gain a padding byte to avoid being a negative number.
            // The biggest single-byte length encoding for DER is 0x7F bytes, but we're
            // symmetric, so 0x7E (126).
            // 63 bytes per half allows for 61 content bytes (prefix 02 3D), which can
            // encode up to a ((61 * 8) - 1)-bit integer.
            // So, any fieldSizeBits <= 487 maximally needs 2 * fieldSizeBytes + 6 bytes,
            // because all lengths fit in one byte. (30 7E 02 3D ... 02 3D ...)

            // Add the padding bit because of unsigned -> signed.
            int paddedFieldSizeBytes = BitsToBytes(fieldSizeBits + 1);

            if (paddedFieldSizeBytes <= 61)
            {
                return 2 * paddedFieldSizeBytes + 6;
            }

            // Past this point the sequence length grows (30 81 xx) up until 0xFF payload.
            // Per our symmetry, that happens when the integers themselves max out, which is
            // when paddedFieldSizeBytes is 0x7F; which covers up to a 1015-bit (before padding) field.

            if (paddedFieldSizeBytes <= 0x7F)
            {
                return 2 * paddedFieldSizeBytes + 7;
            }

            // Beyond here, we'll just do math.
            int segmentSize = 2 + GetDerLengthLength(paddedFieldSizeBytes) + paddedFieldSizeBytes;
            int payloadSize = 2 * segmentSize;
            int sequenceSize = 2 + GetDerLengthLength(payloadSize) + payloadSize;
            return sequenceSize;

            static int GetDerLengthLength(int payloadLength)
            {
                Debug.Assert(payloadLength >= 0);

                if (payloadLength <= 0x7F)
                    return 0;

                if (payloadLength <= 0xFF)
                    return 1;

                if (payloadLength <= 0xFFFF)
                    return 2;

                if (payloadLength <= 0xFFFFFF)
                    return 3;

                return 4;
            }
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        /// <summary>
        /// Converts IeeeP1363 format to the specified signature format
        /// </summary>
        internal static byte[] ConvertFromIeeeP1363Signature(byte[] signature, DSASignatureFormat targetFormat)
        {
            switch (targetFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return signature;
                case DSASignatureFormat.Rfc3279DerSequence:
                    return ConvertIeee1363ToDer(signature);
                default:
                    throw new CryptographicException(
                        SR.Cryptography_UnknownSignatureFormat,
                        targetFormat.ToString());
            }
        }

        /// <summary>
        /// Converts signature in the specified signature format to IeeeP1363
        /// </summary>
        internal static byte[] ConvertSignatureToIeeeP1363(
            DSASignatureFormat currentFormat,
            ReadOnlySpan<byte> signature,
            int fieldSizeBits)
        {
            switch (currentFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return signature.ToArray();
                case DSASignatureFormat.Rfc3279DerSequence:
                    return ConvertDerToIeee1363(signature, fieldSizeBits);
                default:
                    throw new CryptographicException(
                        SR.Cryptography_UnknownSignatureFormat,
                        currentFormat.ToString());
            }
        }
#endif

        public static int BitsToBytes(int bitLength)
        {
            int byteLength = (bitLength + 7) / 8;
            return byteLength;
        }

        private static void CopySignatureField(ReadOnlySpan<byte> signatureField, Span<byte> response)
        {
            if (signatureField.Length > response.Length)
            {
                if (signatureField.Length != response.Length + 1 ||
                    signatureField[0] != 0 ||
                    signatureField[1] <= 0x7F)
                {
                    // The only way this should be true is if the value required a zero-byte-pad.
                    Debug.Fail($"A signature field was longer ({signatureField.Length}) than expected ({response.Length})");
                    throw new CryptographicException();
                }

                signatureField = signatureField.Slice(1);
            }

            // If the field is too short then it needs to be prepended
            // with zeroes in the response.  Since the array was already
            // zeroed out, just figure out where we need to start copying.
            int writeOffset = response.Length - signatureField.Length;
            response.Slice(0, writeOffset).Clear();
            signatureField.CopyTo(response.Slice(writeOffset));
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        internal static byte[]? ConvertSignatureToIeeeP1363(
            this DSA dsa,
            DSASignatureFormat currentFormat,
            ReadOnlySpan<byte> signature,
            int fieldSizeBits = 0)
        {
            try
            {
                if (fieldSizeBits == 0)
                {
                    DSAParameters pars = dsa.ExportParameters(false);
                    fieldSizeBits = pars.Q!.Length * 8;
                }

                return ConvertSignatureToIeeeP1363(
                    currentFormat,
                    signature,
                    fieldSizeBits);
            }
            catch (CryptographicException)
            {
                // This method is used only for verification where we want to return false when signature is
                // incorrectly formatted.
                // We do not want to bubble up the exception anywhere.
                return null;
            }
        }

        internal static byte[]? ConvertSignatureToIeeeP1363(
            this ECDsa ecdsa,
            DSASignatureFormat currentFormat,
            ReadOnlySpan<byte> signature)
        {
            try
            {
                return ConvertSignatureToIeeeP1363(
                    currentFormat,
                    signature,
                    ecdsa.KeySize);
            }
            catch (CryptographicException)
            {
                // This method is used only for verification where we want to return false when signature is
                // incorrectly formatted.
                // We do not want to bubble up the exception anywhere.
                return null;
            }
        }
#endif
    }
}
