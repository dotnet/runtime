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

        public static int BitsToBytes(int bitLength)
        {
            int byteLength = (bitLength + 7) / 8;
            return byteLength;
        }

        private static void CopySignatureField(ReadOnlySpan<byte> signatureField, Span<byte> response)
        {
            if (signatureField.Length > response.Length)
            {
                // The only way this should be true is if the value required a zero-byte-pad.
                Debug.Assert(signatureField.Length == response.Length + 1, "signatureField.Length == fieldLength + 1");
                Debug.Assert(signatureField[0] == 0, "signatureField[0] == 0");
                Debug.Assert(signatureField[1] > 0x7F, "signatureField[1] > 0x7F");
                signatureField = signatureField.Slice(1);
            }

            // If the field is too short then it needs to be prepended
            // with zeroes in the response.  Since the array was already
            // zeroed out, just figure out where we need to start copying.
            int writeOffset = response.Length - signatureField.Length;
            signatureField.CopyTo(response.Slice(writeOffset));
        }
    }
}
