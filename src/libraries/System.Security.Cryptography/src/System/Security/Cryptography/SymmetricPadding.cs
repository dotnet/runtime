// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static class SymmetricPadding
    {
        public static int GetCiphertextLength(int plaintextLength, int paddingSizeInBytes, PaddingMode paddingMode)
        {
            Debug.Assert(plaintextLength >= 0);

             //divisor and factor are same and won't overflow.
            int wholeBlocks = Math.DivRem(plaintextLength, paddingSizeInBytes, out int remainder) * paddingSizeInBytes;

            switch (paddingMode)
            {
                case PaddingMode.None when (remainder != 0):
                    throw new CryptographicException(SR.Cryptography_PartialBlock);
                case PaddingMode.None:
                case PaddingMode.Zeros when (remainder == 0):
                    return plaintextLength;
                case PaddingMode.Zeros:
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    return checked(wholeBlocks + paddingSizeInBytes);
                default:
                    Debug.Fail($"Unknown padding mode {paddingMode}.");
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }

            public static int PadBlock(ReadOnlySpan<byte> block, Span<byte> destination, int paddingSizeInBytes, PaddingMode paddingMode)
        {
            int count = block.Length;
            int paddingRemainder = count % paddingSizeInBytes;
            int padBytes = paddingSizeInBytes - paddingRemainder;

            switch (paddingMode)
            {
                case PaddingMode.None when (paddingRemainder != 0):
                    throw new CryptographicException(SR.Cryptography_PartialBlock);

                case PaddingMode.None:
                    if (destination.Length < count)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    return count;

                // ANSI padding fills the blocks with zeros and adds the total number of padding bytes as
                // the last pad byte, adding an extra block if the last block is complete.
                //
                // xx 00 00 00 00 00 00 07
                case PaddingMode.ANSIX923:
                    int ansiSize = count + padBytes;

                    if (destination.Length < ansiSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes - 1).Clear();
                    destination[count + padBytes - 1] = (byte)padBytes;
                    return ansiSize;

                // ISO padding fills the blocks up with random bytes and adds the total number of padding
                // bytes as the last pad byte, adding an extra block if the last block is complete.
                //
                // xx rr rr rr rr rr rr 07
                case PaddingMode.ISO10126:
                    int isoSize = count + padBytes;

                    if (destination.Length < isoSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    RandomNumberGenerator.Fill(destination.Slice(count, padBytes - 1));
                    destination[count + padBytes - 1] = (byte)padBytes;
                    return isoSize;

                // PKCS padding fills the blocks up with bytes containing the total number of padding bytes
                // used, adding an extra block if the last block is complete.
                //
                // xx xx 06 06 06 06 06 06
                case PaddingMode.PKCS7:
                    int pkcsSize = count + padBytes;

                    if (destination.Length < pkcsSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes).Fill((byte)padBytes);
                    return pkcsSize;

                // Zeros padding fills the last partial block with zeros, and does not add a new block to
                // the end if the last block is already complete.
                //
                //  xx 00 00 00 00 00 00 00
                case PaddingMode.Zeros:
                    if (padBytes == paddingSizeInBytes)
                    {
                        padBytes = 0;
                    }

                    int zeroSize = count + padBytes;

                    if (destination.Length < zeroSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes).Clear();
                    return zeroSize;

                default:
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }

        public static bool DepaddingRequired(PaddingMode padding)
        {
            // Some padding modes encode sufficient information to allow for automatic depadding to happen.
            switch (padding)
            {
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    return true;
                case PaddingMode.Zeros:
                case PaddingMode.None:
                    return false;
                default:
                    Debug.Fail($"Unknown padding mode {padding}.");
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }

        public static int GetPaddingLength(ReadOnlySpan<byte> block, PaddingMode paddingMode, int blockSize)
        {
            int padBytes;

            // See PadBlock for a description of the padding modes.
            switch (paddingMode)
            {
                case PaddingMode.ANSIX923:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > blockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Verify that all the padding bytes are 0s
                    if (block.Slice(block.Length - padBytes, padBytes - 1).IndexOfAnyExcept((byte)0) >= 0)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    break;

                case PaddingMode.ISO10126:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > blockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Since the padding consists of random bytes, we cannot verify the actual pad bytes themselves
                    break;

                case PaddingMode.PKCS7:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > blockSize)
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);

                    // Verify all the padding bytes match the amount of padding
                    for (int i = block.Length - padBytes; i < block.Length - 1; i++)
                    {
                        if (block[i] != padBytes)
                            throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    break;

                // We cannot remove Zeros padding because we don't know if the zeros at the end of the block
                // belong to the padding or the plaintext itself.
                case PaddingMode.Zeros:
                case PaddingMode.None:
                    padBytes = 0;
                    break;

                default:
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }

            return block.Length - padBytes;
        }
    }
}
