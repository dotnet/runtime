// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract class HMAC : KeyedHashAlgorithm
    {
        private string _hashName;
        private int _blockSizeValue = 64;

        protected int BlockSizeValue
        {
            get => _blockSizeValue;
            set => _blockSizeValue = value;
        }

        protected HMAC() { }

        public static new HMAC Create() => Create("System.Security.Cryptography.HMAC");

        public static new HMAC Create(string algorithmName) => throw new PlatformNotSupportedException();

        public string HashName
        {
            get => _hashName;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(HashName));
                }

                // On the desktop, setting the HashName selects (or switches over to) a new hashing algorithm via CryptoConfig.
                // Our intended refactoring turns HMAC back into an abstract class with no algorithm-specific implementation.
                // Changing the HashName would not have the intended effect so throw a proper exception so the developer knows what's up.
                //
                // We still have to allow setting it the first time as the contract provides no other way to do so.
                // Since the set is public, ensure that hmac.HashName = hmac.HashName works without throwing.

                if (_hashName != null && value != _hashName)
                {
                    throw new PlatformNotSupportedException(SR.HashNameMultipleSetNotSupported);
                }

                _hashName = value;
            }
        }

        public override byte[] Key
        {
            get => base.Key;
            set => base.Key = value;
        }

        protected override void Dispose(bool disposing) =>
            base.Dispose(disposing);

        protected override void HashCore(byte[] rgb, int ib, int cb) =>
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);

        protected override void HashCore(ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);

        protected override byte[] HashFinal() =>
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);

        public override void Initialize() =>
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);
    }
}
