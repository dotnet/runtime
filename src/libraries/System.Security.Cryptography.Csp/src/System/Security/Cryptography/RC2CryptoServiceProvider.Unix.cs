// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class RC2CryptoServiceProvider : RC2
    {
        private readonly RC2 _impl;

        private static KeySizes[] s_legalKeySizes =
        {
            new KeySizes(40, 128, 8)  // csp implementation only goes up to 128
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of RC2CryptoServiceProvider")]
        public RC2CryptoServiceProvider()
        {
            _impl = RC2.Create();
            _impl.FeedbackSize = 8;
        }

        public override int BlockSize
        {
            get { return _impl.BlockSize; }
            set { _impl.BlockSize = value; }
        }

        public override ICryptoTransform CreateDecryptor() => _impl.CreateDecryptor();
        public override ICryptoTransform CreateEncryptor() => _impl.CreateEncryptor();

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) =>
            _impl.CreateEncryptor(rgbKey, Helpers.TrimLargeIV(rgbIV, BlockSize));

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) =>
            _impl.CreateDecryptor(rgbKey, Helpers.TrimLargeIV(rgbIV, BlockSize));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
                base.Dispose(disposing);
            }
        }

        public override int EffectiveKeySize
        {
            get { return _impl.EffectiveKeySize; }
            set { _impl.EffectiveKeySize = value; }
        }

        public override int FeedbackSize
        {
            get { return _impl.FeedbackSize; }
            set { _impl.FeedbackSize = value; }
        }

        public override void GenerateIV() => _impl.GenerateIV();
        public override void GenerateKey() => _impl.GenerateKey();

        public override byte[] IV
        {
            get { return _impl.IV; }
            set { _impl.IV = value; }
        }

        public override byte[] Key
        {
            get { return _impl.Key; }
            set { _impl.Key = value; }
        }

        public override int KeySize
        {
            get { return _impl.KeySize; }
            set
            {
                // Perform the check here because LegalKeySizes are more restrictive here than _impl
                if (!value.IsLegalSize(s_legalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                _impl.KeySize = value;
            }
        }

        public override KeySizes[] LegalBlockSizes => _impl.LegalBlockSizes;
        public override KeySizes[] LegalKeySizes => s_legalKeySizes.CloneKeySizesArray();

        public override CipherMode Mode
        {
            get { return _impl.Mode; }
            set { _impl.Mode = value; }
        }

        public override PaddingMode Padding
        {
            get { return _impl.Padding; }
            set { _impl.Padding = value; }
        }

        public bool UseSalt
        {
            get { return false; }
            [SupportedOSPlatform("windows")]
            set
            {
                // Don't allow a true value
                if (value)
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(UseSalt)));
            }
        }
    }
}
