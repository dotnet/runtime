// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography.Asn1;
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
            Span<byte> destination = default;
            byte[] buf = CryptoPool.Rent(rsaSize);

            try
            {
                destination = new Span<byte>(buf, 0, rsaSize);

                int bytesWritten = Decrypt(key, data, destination, padding);
                return destination.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(destination);
                CryptoPool.Return(buf, clearSize: 0);
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

        private delegate T ExportPrivateKeyFunc<T>(ReadOnlyMemory<byte> pkcs8, ReadOnlyMemory<byte> pkcs1);

        private delegate ReadOnlyMemory<byte> TryExportPrivateKeySelector(
            ReadOnlyMemory<byte> pkcs8,
            ReadOnlyMemory<byte> pkcs1);

        private T ExportPrivateKey<T>(ExportPrivateKeyFunc<T> exporter)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeEvpPKeyHandle key = GetKey();

            ArraySegment<byte> p8 = Interop.Crypto.RentEncodePkcs8PrivateKey(key);

            try
            {
                ReadOnlyMemory<byte> pkcs1 = VerifyPkcs8(p8);
                return exporter(p8, pkcs1);
            }
            finally
            {
                CryptoPool.Return(p8);
            }
        }

        private bool TryExportPrivateKey(TryExportPrivateKeySelector selector, Span<byte> destination, out int bytesWritten)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeEvpPKeyHandle key = GetKey();

            ArraySegment<byte> p8 = Interop.Crypto.RentEncodePkcs8PrivateKey(key);

            try
            {
                ReadOnlyMemory<byte> pkcs1 = VerifyPkcs8(p8);
                ReadOnlyMemory<byte> selected = selector(p8, pkcs1);
                return selected.Span.TryCopyToDestination(destination, out bytesWritten);
            }
            finally
            {
                CryptoPool.Return(p8);
            }
        }

        private T ExportPublicKey<T>(Func<ReadOnlyMemory<byte>, T> exporter)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeEvpPKeyHandle key = GetKey();

            ArraySegment<byte> spki = Interop.Crypto.RentEncodeSubjectPublicKeyInfo(key);

            try
            {
                return exporter(spki);
            }
            finally
            {
                CryptoPool.Return(spki);
            }
        }

        private bool TryExportPublicKey(
            Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>? transform,
            Span<byte> destination,
            out int bytesWritten)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeEvpPKeyHandle key = GetKey();

            ArraySegment<byte> spki = Interop.Crypto.RentEncodeSubjectPublicKeyInfo(key);

            try
            {
                ReadOnlyMemory<byte> data = spki;

                if (transform != null)
                {
                    data = transform(data);
                }

                return data.Span.TryCopyToDestination(destination, out bytesWritten);
            }
            finally
            {
                CryptoPool.Return(spki);
            }
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            return TryExportPrivateKey(static (pkcs8, pkcs1) => pkcs8, destination, out bytesWritten);
        }

        public override byte[] ExportPkcs8PrivateKey()
        {
            return ExportPrivateKey(static (pkcs8, pkcs1) => pkcs8.ToArray());
        }

        public override bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            return TryExportPrivateKey(static (pkcs8, pkcs1) => pkcs1, destination, out bytesWritten);
        }

        public override byte[] ExportRSAPrivateKey()
        {
            return ExportPrivateKey(static (pkcs8, pkcs1) => pkcs1.ToArray());
        }

        public override byte[] ExportRSAPublicKey()
        {
            return ExportPublicKey(
                static spki =>
                {
                    ReadOnlyMemory<byte> pkcs1 = RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(spki, out int read);
                    Debug.Assert(read == spki.Length);
                    return pkcs1.ToArray();
                });
        }

        public override bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten)
        {
            return TryExportPublicKey(
                spki =>
                {
                    ReadOnlyMemory<byte> pkcs1 = RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(spki, out int read);
                    Debug.Assert(read == spki.Length);
                    return pkcs1;
                },
                destination,
                out bytesWritten);
        }

        public override byte[] ExportSubjectPublicKeyInfo()
        {
            return ExportPublicKey(static spki => spki.ToArray());
        }

        public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            return TryExportPublicKey(
                transform: null,
                destination,
                out bytesWritten);
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            if (includePrivateParameters)
            {
                return ExportPrivateKey(
                    static (pkcs8, pkcs1) =>
                    {
                        AlgorithmIdentifierAsn algId = default;
                        RSAParameters ret;
                        RSAKeyFormatHelper.FromPkcs1PrivateKey(pkcs1, in algId, out ret);
                        return ret;
                    });
            }

            return ExportPublicKey(
                static spki =>
                {
                    RSAParameters ret;
                    RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        spki.Span,
                        out int read,
                        out ret);

                    Debug.Assert(read == spki.Length);
                    return ret;
                });
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            ValidateParameters(ref parameters);
            ThrowIfDisposed();

            if (parameters.D != null)
            {
                AsnWriter writer = RSAKeyFormatHelper.WritePkcs8PrivateKey(parameters);
                ArraySegment<byte> pkcs8 = writer.RentAndEncode();

                try
                {
                    ImportPkcs8PrivateKey(pkcs8, checkAlgorithm: false, out _);
                }
                finally
                {
                    CryptoPool.Return(pkcs8);
                }
            }
            else
            {
                AsnWriter writer = RSAKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                ArraySegment<byte> spki = writer.RentAndEncode();

                try
                {
                    ImportSubjectPublicKeyInfo(spki, checkAlgorithm: false, out _);
                }
                finally
                {
                    CryptoPool.Return(spki);
                }
            }
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

            AsnWriter writer = RSAKeyFormatHelper.WriteSubjectPublicKeyInfo(source.Slice(0, read));
            ArraySegment<byte> spki = writer.RentAndEncode();

            try
            {
                ImportSubjectPublicKeyInfo(spki, checkAlgorithm: false, out _);
            }
            finally
            {
                CryptoPool.Return(spki);
            }

            bytesRead = read;
        }

        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();

            ImportSubjectPublicKeyInfo(source, checkAlgorithm: true, out bytesRead);
        }

        private void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            bool checkAlgorithm,
            out int bytesRead)
        {
            int read;

            if (checkAlgorithm)
            {
                read = RSAKeyFormatHelper.CheckSubjectPublicKeyInfo(source);
            }
            else
            {
                read = source.Length;
            }

            SafeEvpPKeyHandle newKey = Interop.Crypto.DecodeSubjectPublicKeyInfo(
                source.Slice(0, read),
                Interop.Crypto.EvpAlgorithmId.RSA);

            Debug.Assert(!newKey.IsInvalid);
            SetKey(newKey);
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

        public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();

            ImportPkcs8PrivateKey(source, checkAlgorithm: true, out bytesRead);
        }

        private void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, bool checkAlgorithm, out int bytesRead)
        {
            int read;

            if (checkAlgorithm)
            {
                read = RSAKeyFormatHelper.CheckPkcs8(source);
            }
            else
            {
                read = source.Length;
            }

            SafeEvpPKeyHandle newKey = Interop.Crypto.DecodePkcs8PrivateKey(
                source.Slice(0, read),
                Interop.Crypto.EvpAlgorithmId.RSA);

            Debug.Assert(!newKey.IsInvalid);
            SetKey(newKey);
            bytesRead = read;
        }

        public override void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
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

            AsnWriter writer = RSAKeyFormatHelper.WritePkcs8PrivateKey(source.Slice(0, read));
            ArraySegment<byte> pkcs8 = writer.RentAndEncode();

            try
            {
                ImportPkcs8PrivateKey(pkcs8, checkAlgorithm: false, out _);
            }
            finally
            {
                CryptoPool.Return(pkcs8);
            }

            bytesRead = read;
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

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_key))]
        private void SetKey(SafeEvpPKeyHandle newKey)
        {
            Debug.Assert(!newKey.IsInvalid);
            FreeKey();
            _key = new Lazy<SafeEvpPKeyHandle>(newKey);

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(BitsPerByte * Interop.Crypto.EvpPKeySize(newKey));
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

        private static ReadOnlyMemory<byte> VerifyPkcs8(ReadOnlyMemory<byte> pkcs8)
        {
            // OpenSSL 1.1.1 will export RSA public keys as a PKCS#8, but this makes a broken structure.
            //
            // So, crack it back open.  If we can walk the payload it's valid, otherwise throw the
            // "there's no private key" exception.

            try
            {
                ReadOnlyMemory<byte> pkcs1Priv = RSAKeyFormatHelper.ReadPkcs8(pkcs8, out int read);
                Debug.Assert(read == pkcs8.Length);
                _ = RSAPrivateKeyAsn.Decode(pkcs1Priv, AsnEncodingRules.BER);
                return pkcs1Priv;
            }
            catch (CryptographicException)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }
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
