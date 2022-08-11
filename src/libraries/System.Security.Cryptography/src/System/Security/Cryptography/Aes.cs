// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {
        protected Aes()
        {
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = AesImplementation.s_legalKeySizes.CloneKeySizesArray();

            BlockSizeValue = 128;
            FeedbackSizeValue = 8;
            KeySizeValue = 256;
            ModeValue = CipherMode.CBC;
        }

        public static new Aes Create()
        {
            return new AesImplementation();
        }

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new Aes? Create(string algorithmName)
        {
            return (Aes?)CryptoConfig.CreateFromName(algorithmName);
        }

        private static readonly KeySizes[] s_legalBlockSizes = { new KeySizes(128, 128, 0) };
    }
}
