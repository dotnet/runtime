// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class RC2 : SymmetricAlgorithm
    {
        protected int EffectiveKeySizeValue;

        protected RC2()
        {
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
            KeySizeValue = 128;
            BlockSizeValue = 64;
            FeedbackSizeValue = BlockSizeValue;
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public static new RC2 Create()
        {
            return new RC2Implementation();
        }

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new RC2? Create(string AlgName)
        {
            return (RC2?)CryptoConfig.CreateFromName(AlgName);
        }


        public override int KeySize
        {
            get { return KeySizeValue; }
            set
            {
                if (value < EffectiveKeySizeValue)
                    throw new CryptographicException(SR.Cryptography_RC2_EKSKS);

                base.KeySize = value;
            }
        }

        public virtual int EffectiveKeySize
        {
            get
            {
                if (EffectiveKeySizeValue == 0)
                    return KeySizeValue;

                return EffectiveKeySizeValue;
            }
            set
            {
                if (value > KeySizeValue)
                    throw new CryptographicException(SR.Cryptography_RC2_EKSKS);
                else if (value == 0)
                    EffectiveKeySizeValue = value;
                else if (value < 40)
                    throw new CryptographicException(SR.Cryptography_RC2_EKS40);
                else
                {
                    if (value.IsLegalSize(s_legalKeySizes))
                        EffectiveKeySizeValue = value;
                    else
                        throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                }
            }
        }

        private static readonly KeySizes[] s_legalBlockSizes =
        {
            new KeySizes(minSize: 64, maxSize: 64, skipSize: 0)
        };

        private static readonly KeySizes[] s_legalKeySizes =
        {
            new KeySizes(minSize: 40, maxSize: 1024, skipSize: 8) // 1024 bits is theoretical max according to the RFC
        };
    }
}
