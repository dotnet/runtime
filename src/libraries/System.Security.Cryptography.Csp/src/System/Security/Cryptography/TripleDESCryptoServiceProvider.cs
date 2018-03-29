// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TripleDESCryptoServiceProvider : TripleDES
    {
        private readonly TripleDES _impl;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "This is the implementation of TripleDES")]
        public TripleDESCryptoServiceProvider()
        {
            // This class wraps TripleDES
            _impl = TripleDES.Create();
            _impl.FeedbackSize = 8;
        }

        public override int FeedbackSize
        {
            get { return _impl.FeedbackSize; }
            set { _impl.FeedbackSize = value; }
        }

        public override int BlockSize
        {
            get { return _impl.BlockSize; }
            set { _impl.BlockSize = value; }
        }

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
            set { _impl.KeySize = value; }
        }
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

        public override KeySizes[] LegalBlockSizes => _impl.LegalBlockSizes;
        public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;
        public override ICryptoTransform CreateEncryptor() => _impl.CreateEncryptor();
        public override ICryptoTransform CreateDecryptor() => _impl.CreateDecryptor();
        public override void GenerateIV() => _impl.GenerateIV();
        public override void GenerateKey() => _impl.GenerateKey();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "This is the implementation of TripleDES")]
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) =>
            _impl.CreateEncryptor(rgbKey, TrimLargeIV(rgbIV));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "This is the implementation of TripleDES")]
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) =>
            _impl.CreateDecryptor(rgbKey, TrimLargeIV(rgbIV));

        private byte[] TrimLargeIV(byte[] iv)
        {
            // The BlockSize for 3DES is 64, so this is always 8, but let's use
            // the formula anyways.
            int blockSizeBytes = (BlockSize + 7) / 8;

            if (iv?.Length > blockSizeBytes)
            {
                byte[] tmp = new byte[blockSizeBytes];
                Buffer.BlockCopy(iv, 0, tmp, 0, tmp.Length);
                return tmp;
            }

            return iv;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
