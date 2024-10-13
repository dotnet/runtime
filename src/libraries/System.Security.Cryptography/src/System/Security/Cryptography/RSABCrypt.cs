// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class RSABCrypt : RSA
    {
        private static readonly SafeBCryptAlgorithmHandle s_algHandle =
            Interop.BCrypt.BCryptOpenAlgorithmProvider(BCryptNative.AlgorithmName.RSA);

        // See https://msdn.microsoft.com/en-us/library/windows/desktop/bb931354(v=vs.85).aspx
        // All values are in bits.
        private static readonly KeySizes s_keySizes =
            new KeySizes(minSize: 512, maxSize: 16384, skipSize: 64);

        private SafeBCryptKeyHandle? _key;
        private int _lastKeySize;
        private bool _publicOnly;

        internal RSABCrypt()
        {
            KeySizeValue = 2048;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
            }

            _lastKeySize = -1;
        }

        private SafeBCryptKeyHandle GetKey()
        {
            int keySize = KeySize;

            // Since _lastKeySize also tracks the disposal state, we can do the equals check first.
            if (_lastKeySize == keySize)
            {
                Debug.Assert(_key != null);
                return _key;
            }

            ThrowIfDisposed();

            SafeBCryptKeyHandle newKey = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keySize);
            Interop.BCrypt.BCryptFinalizeKeyPair(newKey);
            SetKey(newKey, publicOnly: false);
            return newKey;
        }

        private void SetKey(SafeBCryptKeyHandle newKey, bool publicOnly)
        {
            Debug.Assert(!newKey.IsInvalid);

            int keySize = Interop.BCrypt.BCryptGetDWordProperty(
                newKey,
                Interop.BCrypt.BCryptPropertyStrings.BCRYPT_KEY_STRENGTH);

            SafeBCryptKeyHandle? oldKey = Interlocked.Exchange(ref _key, newKey);
            ForceSetKeySize(keySize);
            _publicOnly = publicOnly;
            oldKey?.Dispose();
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            SafeBCryptKeyHandle key = GetKey();

            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(
                key,
                includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB);

            RSAParameters ret = default;
            ret.FromBCryptBlob(keyBlob, includePrivateParameters);

            // FromBCryptBlob isn't expected to have any failures since it's reading
            // data directly from BCryptExportKey, so we don't need to bother with
            // a try/finally.
            CryptoPool.Return(keyBlob);

            return ret;
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            ThrowIfDisposed();

            ArraySegment<byte> keyBlob = parameters.ToBCryptBlob();
            SafeBCryptKeyHandle newKey;

            try
            {
                newKey = Interop.BCrypt.BCryptImportKeyPair(
                    s_algHandle,
                    parameters.D != null ?
                        Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB :
                        Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB,
                    keyBlob);
            }
            finally
            {
                // Return (and clear) the BCryptBlob array even if the parameters
                // are invalid and the import fails/throws (e.g. P*Q != Modulus).
                CryptoPool.Return(keyBlob);
            }

            SetKey(newKey, publicOnly: parameters.D is null);
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(padding);

            byte[] ret = new byte[GetMaxOutputSize()];
            int written = Encrypt(new ReadOnlySpan<byte>(data), ret.AsSpan(), padding);

            VerifyWritten(ret, written);
            return ret;
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(padding);

            return Decrypt(new ReadOnlySpan<byte>(data), padding);
        }

        public override byte[] SignHash(
            byte[] hash,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            byte[] ret = new byte[GetMaxOutputSize()];

            int written = SignHash(
                new ReadOnlySpan<byte>(hash),
                ret.AsSpan(),
                hashAlgorithm,
                padding);

            VerifyWritten(ret, written);
            return ret;
        }

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            return VerifyHash(
                new ReadOnlySpan<byte>(hash),
                new ReadOnlySpan<byte>(signature),
                hashAlgorithm,
                padding);
        }

        public override bool TryDecrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(padding);

            SafeBCryptKeyHandle key = GetKey();
            int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

            if (data.Length != modulusSizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            }

            ThrowIfPublicOnly();

            switch (padding.Mode)
            {
                case RSAEncryptionPaddingMode.Pkcs1:
                    return Interop.BCrypt.BCryptDecryptPkcs1(key, data, destination, out bytesWritten);
                case RSAEncryptionPaddingMode.Oaep:
                    return Interop.BCrypt.BCryptDecryptOaep(
                        key,
                        data,
                        destination,
                        padding.OaepHashAlgorithm.Name,
                        out bytesWritten);
            }

            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
        }

        public override bool TryEncrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(padding);

            SafeBCryptKeyHandle key = GetKey();
            int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

            if (destination.Length < modulusSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            const int Pkcs1PaddingOverhead = 11;

            switch (padding.Mode)
            {
                case RSAEncryptionPaddingMode.Pkcs1:
                    if (modulusSizeInBytes - Pkcs1PaddingOverhead < data.Length)
                    {
                        throw new CryptographicException(
                            SR.Format(
                                SR.Cryptography_Encryption_MessageTooLong,
                                modulusSizeInBytes - Pkcs1PaddingOverhead));
                    }

                    bytesWritten = Interop.BCrypt.BCryptEncryptPkcs1(key, data, destination);
                    return true;
                case RSAEncryptionPaddingMode.Oaep:
                    bytesWritten = Interop.BCrypt.BCryptEncryptOaep(
                        key,
                        data,
                        destination,
                        padding.OaepHashAlgorithm.Name);

                    return true;
            }

            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
        }

        public override bool TrySignHash(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            out int bytesWritten)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);
            ThrowIfPublicOnly();

            SafeBCryptKeyHandle key = GetKey();

            if (hash.Length != RSACng.GetHashSizeInBytes(hashAlgorithm))
            {
                throw new CryptographicException(SR.Cryptography_SignHash_WrongSize);
            }

            Interop.BCrypt.NTSTATUS status;
            int written;

            switch (padding.Mode)
            {
                case RSASignaturePaddingMode.Pkcs1:
                    status = Interop.BCrypt.BCryptSignHashPkcs1(
                        key,
                        hash,
                        destination,
                        hashAlgorithmName,
                        out written);

                    break;
                case RSASignaturePaddingMode.Pss:
                    status = Interop.BCrypt.BCryptSignHashPss(
                        key,
                        hash,
                        destination,
                        hashAlgorithmName,
                        out written);

                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
            }

            if (status == Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                bytesWritten = written;
                return true;
            }

            if (status == Interop.BCrypt.NTSTATUS.STATUS_BUFFER_TOO_SMALL)
            {
                bytesWritten = 0;
                return false;
            }

            throw Interop.BCrypt.CreateCryptographicException(status);
        }

        public override bool VerifyHash(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            SafeBCryptKeyHandle key = GetKey();

            if (hash.Length != RSACng.GetHashSizeInBytes(hashAlgorithm))
            {
                return false;
            }

            switch (padding.Mode)
            {
                case RSASignaturePaddingMode.Pkcs1:
                    return Interop.BCrypt.BCryptVerifySignaturePkcs1(
                        key,
                        hash,
                        signature,
                        hashAlgorithmName);
                case RSASignaturePaddingMode.Pss:
                    return Interop.BCrypt.BCryptVerifySignaturePss(
                        key,
                        hash,
                        signature,
                        hashAlgorithmName);
                default:
                    throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
            }
        }

        public override KeySizes[] LegalKeySizes => new KeySizes[] { s_keySizes };

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
        }

        public override unsafe void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportPkcs8PrivateKey(source, out bytesRead);
        }

        public override void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportRSAPrivateKey(source, out bytesRead);
        }

        public override void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportRSAPublicKey(source, out bytesRead);
        }

        public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportSubjectPublicKeyInfo(source, out bytesRead);
        }

        private void ForceSetKeySize(int newKeySize)
        {
            // Our LegalKeySizes value stores the values that we encoded as being the correct
            // legal key size limitations for this algorithm, as documented on MSDN.
            //
            // But on a new OS version we might not question if our limit is accurate, or MSDN
            // could have been inaccurate to start with.
            //
            // Since the key is already loaded, we know that Windows thought it to be valid;
            // therefore we should set KeySizeValue directly to bypass the LegalKeySizes conformance
            // check.
            //
            // For RSA there are known cases where this change matters. RSACryptoServiceProvider can
            // create a 384-bit RSA key, which we consider too small to be legal. It can also create
            // a 1032-bit RSA key, which we consider illegal because it doesn't match our 64-bit
            // alignment requirement. (In both cases Windows loads it just fine)
            KeySizeValue = newKeySize;
            _lastKeySize = newKeySize;
        }

        private static void VerifyWritten(byte[] array, int written)
        {
            if (array.Length != written)
            {
                Debug.Fail(
                    $"An array-filling operation wrote {written} when {array.Length} was expected.");

                throw new CryptographicException();
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_lastKeySize < 0, this);
        }

        private void ThrowIfPublicOnly()
        {
            if (_publicOnly)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }
        }
    }
}
