// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
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
    }
}
