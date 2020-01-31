// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

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
            Debug.Assert(input.Length % 2 == 0);
            Debug.Assert(input.Length > 1);

            // Input is (r, s), each of them exactly half of the array.
            // Output is the DER encoded value of CONSTRUCTEDSEQUENCE(INTEGER(r), INTEGER(s)).
            int halfLength = input.Length / 2;

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                writer.WriteKeyParameterInteger(input.Slice(0, halfLength));
                writer.WriteKeyParameterInteger(input.Slice(halfLength, halfLength));
                writer.PopSequence();
                return writer.Encode();
            }
        }

        /// <summary>
        /// Convert Der format of (r, s) to Ieee1363 format
        /// </summary>
        public static byte[] ConvertDerToIeee1363(byte[] input, int inputOffset, int inputCount, int fieldSizeBits)
        {
            int size = BitsToBytes(fieldSizeBits);

            AsnReader reader = new AsnReader(input.AsMemory(inputOffset, inputCount), AsnEncodingRules.DER);
            AsnReader sequenceReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();
            ReadOnlySpan<byte> rDer = sequenceReader.ReadIntegerBytes().Span;
            ReadOnlySpan<byte> sDer = sequenceReader.ReadIntegerBytes().Span;
            sequenceReader.ThrowIfNotEmpty();

            byte[] response = new byte[2 * size];
            CopySignatureField(rDer, response.AsSpan(0, size));
            CopySignatureField(sDer, response.AsSpan(size, size));

            return response;
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        /// <summary>
        /// Converts IeeeP1363 format to the specified signature format
        /// </summary>
        public static byte[] ConvertIeeeP1363Signature(byte[] signature, DSASignatureFormat signatureFormat)
        {
            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return signature;
                case DSASignatureFormat.Rfc3279DerSequence:
                    return ConvertIeee1363ToDer(signature);
                default:
                    throw new ArgumentOutOfRangeException(nameof(signatureFormat));
            }
        }

        /// <summary>
        /// Converts signature in the specified signature format to IeeeP1363
        /// </summary>
        public static byte[] ConvertSignatureToIeeeP1363(DSASignatureFormat signatureFormat, byte[] signature, int offset, int count, int fieldSizeBits)
        {
            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return signature;
                case DSASignatureFormat.Rfc3279DerSequence:
                    return ConvertDerToIeee1363(signature, offset, count, fieldSizeBits);
                default:
                    throw new ArgumentOutOfRangeException(nameof(signatureFormat));
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
                    throw new CryptographicException();
                }

                signatureField = signatureField.Slice(1);
            }

            // If the field is too short then it needs to be prepended
            // with zeroes in the response.  Since the array was already
            // zeroed out, just figure out where we need to start copying.
            int writeOffset = response.Length - signatureField.Length;
            signatureField.CopyTo(response.Slice(writeOffset));
        }

#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        internal static byte[] ConvertSignatureToIeeeP1363(this DSA dsa, DSASignatureFormat signatureFormat, ReadOnlySpan<byte> signature)
        {
            try
            {
                DSAParameters pars = dsa.ExportParameters(false);
                return AsymmetricAlgorithmHelpers.ConvertSignatureToIeeeP1363(signatureFormat, signature.ToArray(), 0, signature.Length, pars.Q.Length * 8);
            }
            catch (CryptographicException)
            {
                // This method is used only for verification where we want to return false when signature is incorrectly formatted
                // We do not want to bubble up the exception anywhere.
                return null;
            }
        }

        internal static byte[] ConvertSignatureToIeeeP1363(this ECDsa ecdsa, DSASignatureFormat signatureFormat, ReadOnlySpan<byte> signature)
        {
            try
            {
                return AsymmetricAlgorithmHelpers.ConvertSignatureToIeeeP1363(signatureFormat, signature.ToArray(), 0, signature.Length, ecdsa.KeySize);
            }
            catch (CryptographicException)
            {
                // This method is used only for verification where we want to return false when signature is incorrectly formatted
                // We do not want to bubble up the exception anywhere.
                return null;
            }
        }
#endif
    }
}
