// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public abstract class HMAC : KeyedHashAlgorithm
    {
        private string _hashName = null!;
        private int _blockSizeValue = 64;

        protected int BlockSizeValue
        {
            get => _blockSizeValue;
            set => _blockSizeValue = value;
        }

        protected HMAC() { }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static new HMAC Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfigForwarder.CreateFromNameUnreferencedCodeMessage)]
        public static new HMAC? Create(string algorithmName) =>
            CryptoConfigForwarder.CreateFromName<HMAC>(algorithmName);

        public string HashName
        {
            get => _hashName;
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(HashName));

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
