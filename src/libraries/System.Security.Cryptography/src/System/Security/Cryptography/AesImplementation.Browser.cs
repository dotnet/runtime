// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            // todo: eerhardt validate cipherMode and paddingMode

            return new RijndaelManagedTransform(
                key,
                cipherMode,
                iv,
                blockSize,
                feedbackSize,
                PaddingMode.PKCS7, // todo: eerhardt verify
                encrypting ? RijndaelManagedTransformMode.Encrypt : RijndaelManagedTransformMode.Decrypt);
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            // todo: eerhardt validate cipherMode and paddingMode

            return new RijndaelManagedTransform(
                key.ToArray(), // todo: eerhardt is this OK?
                cipherMode,
                iv,
                blockSize,
                feedbackSize,
                PaddingMode.PKCS7, // todo: eerhardt verify
                encrypting ? RijndaelManagedTransformMode.Encrypt : RijndaelManagedTransformMode.Decrypt);
        }
    }
}
