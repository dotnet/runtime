// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create() => new RSAImplementation.RSAOpenSsl();
    }

    internal static partial class RSAImplementation
    {
#endif
    public sealed partial class RSAOpenSsl : RSA
    {
        private const int BitsPerByte = 8;

        private Lazy<SafeEvpPKeyHandle> _key;

        public RSAOpenSsl()
            : this(2048)
        {
        }

        public RSAOpenSsl(int keySize)
        {
            base.KeySize = keySize;
            _key = new Lazy<SafeEvpPKeyHandle>(GenerateKey);
        }

        public override int KeySize
        {
            set
            {
                if (KeySize == value)
                {
                    return;
                }

                // Set the KeySize before FreeKey so that an invalid value doesn't throw away the key
                base.KeySize = value;

                ThrowIfDisposed();
                FreeKey();
                _key = new Lazy<SafeEvpPKeyHandle>(GenerateKey);
            }
        }

        private void ForceSetKeySize(int newKeySize)
        {
            // In the event that a key was loaded via ImportParameters or an IntPtr/SafeHandle
            // it could be outside of the bounds that we currently represent as "legal key sizes".
            // Since that is our view into the underlying component it can be detached from the
            // component's understanding.  If it said it has opened a key, and this is the size, trust it.
            KeySizeValue = newKeySize;
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                // While OpenSSL 1.0.x and 1.1.0 will generate RSA-384 keys,
                // OpenSSL 1.1.1 has lifted the minimum to RSA-512.
                //
                // Rather than make the matrix even more complicated,
                // the low limit now is 512 on all OpenSSL-based RSA.
                return new[] { new KeySizes(512, 16384, 8) };
            }
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            ValidatePadding(padding);
            SafeEvpPKeyHandle key = GetKey();
            int rsaSize = Interop.Crypto.EvpPKeySize(key);
            byte[]? buf = null;
            Span<byte> destination = default;

            try
            {
                buf = CryptoPool.Rent(rsaSize);
                destination = new Span<byte>(buf, 0, rsaSize);

                int bytesWritten = Decrypt(key, data, destination, padding);
                return destination.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(destination);
                CryptoPool.Return(buf!, clearSize: 0);
            }
        }

        public override bool TryDecrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            ValidatePadding(padding);
            SafeEvpPKeyHandle key = GetKey();
            int keySizeBytes = Interop.Crypto.EvpPKeySize(key);

            // OpenSSL requires that the decryption buffer be at least as large as EVP_PKEY_size.
            // So if the destination is too small, use a temporary buffer so we can match
            // Windows behavior of succeeding so long as the buffer can hold the final output.
            if (destination.Length < keySizeBytes)
            {
                // RSA up through 4096 bits use a stackalloc
                Span<byte> tmp = stackalloc byte[512];
                byte[]? rent = null;

                if (keySizeBytes > tmp.Length)
                {
                    rent = CryptoPool.Rent(keySizeBytes);
                    tmp = rent;
                }

                int written = Decrypt(key, data, tmp, padding);
                bool ret;

                if (destination.Length < written)
                {
                    bytesWritten = 0;
                    ret = false;
                }
                else
                {
                    tmp.Slice(0, written).CopyTo(destination);
                    bytesWritten = written;
                    ret = true;
                }

                // Whether a stackalloc or a rented array, clear our copy of
                // the decrypted content.
                CryptographicOperations.ZeroMemory(tmp.Slice(0, written));

                if (rent != null)
                {
                    // Already cleared.
                    CryptoPool.Return(rent, clearSize: 0);
                }

                return ret;
            }

            bytesWritten = Decrypt(key, data, destination, padding);
            return true;
        }

        private static int Decrypt(
            SafeEvpPKeyHandle key,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding)
        {
            // Caller should have already checked this.
            Debug.Assert(!key.IsInvalid);

            int rsaSize = Interop.Crypto.EvpPKeySize(key);

            if (data.Length != rsaSize)
            {
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            }

            if (destination.Length < rsaSize)
            {
                Debug.Fail("Caller is responsible for temporary decryption buffer creation");
                throw new CryptographicException();
            }

            IntPtr hashAlgorithm = IntPtr.Zero;

            if (padding.Mode == RSAEncryptionPaddingMode.Oaep)
            {
                Debug.Assert(padding.OaepHashAlgorithm.Name != null);
                hashAlgorithm = Interop.Crypto.HashAlgorithmToEvp(padding.OaepHashAlgorithm.Name);
            }

            return Interop.Crypto.RsaDecrypt(
                key,
                data,
                padding.Mode,
                hashAlgorithm,
                destination);
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            ValidatePadding(padding);
            SafeEvpPKeyHandle key = GetKey();

            byte[] buf = new byte[Interop.Crypto.EvpPKeySize(key)];

            bool encrypted = TryEncrypt(
                key,
                data,
                buf,
                padding,
                out int bytesWritten);

            if (!encrypted || bytesWritten != buf.Length)
            {
                Debug.Fail($"TryEncrypt behaved unexpectedly: {nameof(encrypted)}=={encrypted}, {nameof(bytesWritten)}=={bytesWritten}, {nameof(buf.Length)}=={buf.Length}");
                throw new CryptographicException();
            }

            return buf;
        }

        public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            ValidatePadding(padding);
            SafeEvpPKeyHandle? key = GetKey();

            return TryEncrypt(key, data, destination, padding, out bytesWritten);
        }

        private static bool TryEncrypt(
            SafeEvpPKeyHandle key,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            int rsaSize = Interop.Crypto.EvpPKeySize(key);

            if (destination.Length < rsaSize)
            {
                bytesWritten = 0;
                return false;
            }

            IntPtr hashAlgorithm = IntPtr.Zero;

            if (padding.Mode == RSAEncryptionPaddingMode.Oaep)
            {
                Debug.Assert(padding.OaepHashAlgorithm.Name != null);
                hashAlgorithm = Interop.Crypto.HashAlgorithmToEvp(padding.OaepHashAlgorithm.Name);
            }

            int written = Interop.Crypto.RsaEncrypt(
                key,
                data,
                padding.Mode,
                hashAlgorithm,
                destination);

            Debug.Assert(written == rsaSize);
            bytesWritten = written;
            return true;
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeEvpPKeyHandle key = GetKey();

            RSAParameters rsaParameters = Interop.Crypto.ExportRsaParameters(key, includePrivateParameters);
            bool hasPrivateKey = rsaParameters.D != null;

            if (hasPrivateKey != includePrivateParameters || !HasConsistentPrivateKey(ref rsaParameters))
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            return rsaParameters;
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            ValidateParameters(ref parameters);
            ThrowIfDisposed();

            SafeRsaHandle key = Interop.Crypto.RsaCreate();
            SafeEvpPKeyHandle pkey = Interop.Crypto.EvpPkeyCreate();
            bool imported = false;

            Interop.Crypto.CheckValidOpenSslHandle(key);

            try
            {
                if (!Interop.Crypto.SetRsaParameters(
                    key,
                    parameters.Modulus,
                    parameters.Modulus != null ? parameters.Modulus.Length : 0,
                    parameters.Exponent,
                    parameters.Exponent != null ? parameters.Exponent.Length : 0,
                    parameters.D,
                    parameters.D != null ? parameters.D.Length : 0,
                    parameters.P,
                    parameters.P != null ? parameters.P.Length : 0,
                    parameters.DP,
                    parameters.DP != null ? parameters.DP.Length : 0,
                    parameters.Q,
                    parameters.Q != null ? parameters.Q.Length : 0,
                    parameters.DQ,
                    parameters.DQ != null ? parameters.DQ.Length : 0,
                    parameters.InverseQ,
                    parameters.InverseQ != null ? parameters.InverseQ.Length : 0))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                imported = true;
            }
            finally
            {
                if (!imported)
                {
                    key.Dispose();
                }
            }

            if (!Interop.Crypto.EvpPkeySetRsa(pkey, key))
            {
                pkey.Dispose();
                key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            key.Dispose();
            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(pkey);

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(BitsPerByte * Interop.Crypto.EvpPKeySize(pkey));
        }

        public override void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();

            int read;

            try
            {
                AsnDecoder.ReadEncodedValue(
                    source,
                    AsnEncodingRules.BER,
                    out _,
                    out _,
                    out read);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            SafeEvpPKeyHandle pkey = Interop.Crypto.EvpPkeyCreate();
            SafeRsaHandle key = Interop.Crypto.DecodeRsaPublicKey(source.Slice(0, read));

            Interop.Crypto.CheckValidOpenSslHandle(key);

            if (!Interop.Crypto.EvpPkeySetRsa(pkey, key))
            {
                key.Dispose();
                pkey.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            key.Dispose();

            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(pkey);

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(BitsPerByte * Interop.Crypto.EvpPKeySize(pkey));

            bytesRead = read;
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();
            base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FreeKey();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private void FreeKey()
        {
            if (_key != null && _key.IsValueCreated)
            {
                SafeEvpPKeyHandle handle = _key.Value;
                handle?.Dispose();
            }
        }

        private static void ValidateParameters(ref RSAParameters parameters)
        {
            if (parameters.Modulus == null || parameters.Exponent == null)
                throw new CryptographicException(SR.Argument_InvalidValue);

            if (!HasConsistentPrivateKey(ref parameters))
                throw new CryptographicException(SR.Argument_InvalidValue);
        }

        private static bool HasConsistentPrivateKey(ref RSAParameters parameters)
        {
            if (parameters.D == null)
            {
                if (parameters.P != null ||
                    parameters.DP != null ||
                    parameters.Q != null ||
                    parameters.DQ != null ||
                    parameters.InverseQ != null)
                {
                    return false;
                }
            }
            else
            {
                if (parameters.P == null ||
                    parameters.DP == null ||
                    parameters.Q == null ||
                    parameters.DQ == null ||
                    parameters.InverseQ == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_key == null)
            {
                throw new ObjectDisposedException(
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
                    nameof(RSA)
#else
                    nameof(RSAOpenSsl)
#endif
                );
            }
        }

        private SafeEvpPKeyHandle GetKey()
        {
            ThrowIfDisposed();

            SafeEvpPKeyHandle key = _key.Value;

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            return key;
        }

        private SafeEvpPKeyHandle GenerateKey()
        {
            return Interop.Crypto.RsaGenerateKey(KeySize);
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

        protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
            AsymmetricAlgorithmHelpers.TryHashData(data, destination, hashAlgorithm, out bytesWritten);

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            if (!TrySignHash(
                hash,
                Span<byte>.Empty,
                hashAlgorithm, padding,
                true,
                out int bytesWritten,
                out byte[]? signature))
            {
                Debug.Fail("TrySignHash should not return false in allocation mode");
                throw new CryptographicException();
            }

            Debug.Assert(signature != null);
            return signature;
        }

        public override bool TrySignHash(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            bool ret = TrySignHash(
                hash,
                destination,
                hashAlgorithm,
                padding,
                false,
                out bytesWritten,
                out byte[]? alloced);

            Debug.Assert(alloced == null);
            return ret;
        }

        private bool TrySignHash(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            bool allocateSignature,
            out int bytesWritten,
            out byte[]? signature)
        {
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));
            Debug.Assert(padding != null);
            ValidatePadding(padding);

            signature = null;

            IntPtr digestAlgorithm = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithm.Name);
            SafeEvpPKeyHandle key = GetKey();
            int bytesRequired = Interop.Crypto.EvpPKeySize(key);

            if (allocateSignature)
            {
                Debug.Assert(destination.Length == 0);
                signature = new byte[bytesRequired];
                destination = signature;
            }
            else if (destination.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            int written = Interop.Crypto.RsaSignHash(key, padding.Mode, digestAlgorithm, hash, destination);
            Debug.Assert(written == bytesRequired);
            bytesWritten = written;

            return true;
        }

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            return VerifyHash(new ReadOnlySpan<byte>(hash), new ReadOnlySpan<byte>(signature), hashAlgorithm, padding);
        }

        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }

            ValidatePadding(padding);

            IntPtr digestAlgorithm = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithm.Name);
            SafeEvpPKeyHandle key = GetKey();

            return Interop.Crypto.RsaVerifyHash(
                key,
                padding.Mode,
                digestAlgorithm,
                hash,
                signature);
        }

        private static void ValidatePadding(RSAEncryptionPadding padding)
        {
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            // There are currently two defined padding modes:
            // * Oaep has an option (the hash algorithm)
            // * Pkcs1 has no options
            //
            // Anything other than those to modes is an error,
            // and Pkcs1 having options set is an error, so compare it to
            // the padding struct instead of the padding mode enum.
            if (padding.Mode != RSAEncryptionPaddingMode.Oaep &&
                padding != RSAEncryptionPadding.Pkcs1)
            {
                throw PaddingModeNotSupported();
            }
        }

        private static void ValidatePadding(RSASignaturePadding padding)
        {
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            // RSASignaturePadding currently only has the mode property, so
            // there's no need for a runtime check that PKCS#1 doesn't use
            // nonsensical options like with RSAEncryptionPadding.
            //
            // This would change if we supported PSS with an MGF other than MGF-1,
            // or with a custom salt size, or with a different MGF digest algorithm
            // than the data digest algorithm.
            if (padding.Mode == RSASignaturePaddingMode.Pkcs1)
            {
                Debug.Assert(padding == RSASignaturePadding.Pkcs1);
            }
            else if (padding.Mode == RSASignaturePaddingMode.Pss)
            {
                Debug.Assert(padding == RSASignaturePadding.Pss);
            }
            else
            {
                throw PaddingModeNotSupported();
            }
        }

        private static Exception PaddingModeNotSupported() =>
            new CryptographicException(SR.Cryptography_InvalidPaddingMode);

        private static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
