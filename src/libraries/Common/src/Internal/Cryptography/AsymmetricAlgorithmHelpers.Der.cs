// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
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
            Debug.Assert(input.Length % 2 == 0);
            Debug.Assert(input.Length > 1);

            // Input is (r, s), each of them exactly half of the array. 
            // Output is the DER encoded value of CONSTRUCTEDSEQUENCE(INTEGER(r), INTEGER(s)). 
            int halfLength = input.Length / 2;

            byte[][] rEncoded = DerEncoder.SegmentedEncodeUnsignedInteger(input.Slice(0, halfLength));
            byte[][] sEncoded = DerEncoder.SegmentedEncodeUnsignedInteger(input.Slice(halfLength, halfLength));

            return DerEncoder.ConstructSequence(rEncoded, sEncoded);
        }

        /// <summary>
        /// Convert Der format of (r, s) to Ieee1363 format
        /// </summary>
        public static byte[] ConvertDerToIeee1363(byte[] input, int inputOffset, int inputCount, int fieldSizeBits)
        {
            int size = BitsToBytes(fieldSizeBits);

            try
            {
                DerSequenceReader reader = new DerSequenceReader(input, inputOffset, inputCount);
                byte[] rDer = reader.ReadIntegerBytes();
                byte[] sDer = reader.ReadIntegerBytes();
                byte[] response = new byte[2 * size];

                CopySignatureField(rDer, response, 0, size);
                CopySignatureField(sDer, response, size, size);

                return response;
            }
            catch (InvalidOperationException e)
            {
                throw new CryptographicException(SR.Arg_CryptographyException, e);
            }
        }

        public static int BitsToBytes(int bitLength)
        {
            int byteLength = (bitLength + 7) / 8;
            return byteLength;
        }

        private static void CopySignatureField(byte[] signatureField, byte[] response, int offset, int fieldLength)
        {
            if (signatureField.Length > fieldLength)
            {
                // The only way this should be true is if the value required a zero-byte-pad.
                Debug.Assert(signatureField.Length == fieldLength + 1, "signatureField.Length == fieldLength + 1");
                Debug.Assert(signatureField[0] == 0, "signatureField[0] == 0");
                Debug.Assert(signatureField[1] > 0x7F, "signatureField[1] > 0x7F");

                Buffer.BlockCopy(signatureField, 1, response, offset, fieldLength);
            }
            else if (signatureField.Length == fieldLength)
            {
                Buffer.BlockCopy(signatureField, 0, response, offset, fieldLength);
            }
            else
            {
                // If the field is too short then it needs to be prepended
                // with zeroes in the response.  Since the array was already
                // zeroed out, just figure out where we need to start copying.
                int writeOffset = fieldLength - signatureField.Length;

                Buffer.BlockCopy(signatureField, 0, response, offset + writeOffset, signatureField.Length);
            }
        }
    }
}
