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
