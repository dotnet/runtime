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

        private Lazy<SafeRsaHandle> _key;

        public RSAOpenSsl()
            : this(2048)
        {
        }

        public RSAOpenSsl(int keySize)
        {
            base.KeySize = keySize;
            _key = new Lazy<SafeRsaHandle>(GenerateKey);
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
                _key = new Lazy<SafeRsaHandle>(GenerateKey);
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
            SafeEvpPKeyHandle key = GetPKey();
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
                // Until EVP_PKEY is what gets stored, free the temporary key handle.
                key.Dispose();
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
            SafeEvpPKeyHandle key = GetPKey();
            int keySizeBytes = Interop.Crypto.EvpPKeySize(key);

            // OpenSSL does not take a length value for the destination, so it can write out of bounds.
            // To prevent the OOB write, decrypt into a temporary buffer.
            if (destination.Length < keySizeBytes)
            {
                // RSA up through 4096 use a stackalloc
                Span<byte> tmp = stackalloc byte[512];
                byte[]? rent = null;

                if (keySizeBytes > tmp.Length)
                {
                    rent = CryptoPool.Rent(keySizeBytes);
                    tmp = rent;
                }

                int written = Decrypt(key, data, tmp, padding);
                // Until EVP_PKEY is what gets stored, free the temporary key handle.
                key.Dispose();
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

                CryptographicOperations.ZeroMemory(tmp.Slice(0, written));

                if (rent != null)
                {
                    // Already cleared.
                    CryptoPool.Return(rent, clearSize: 0);
                }

                return ret;
            }

            bytesWritten = Decrypt(key, data, destination, padding);
            // Until EVP_PKEY is what gets stored, free the temporary key handle.
            key.Dispose();
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

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor? oaepProcessor);
            SafeRsaHandle key = GetKey();

            byte[] buf = new byte[Interop.Crypto.RsaSize(key)];

            bool encrypted = TryEncrypt(
                key,
                data,
                buf,
                rsaPadding,
                oaepProcessor,
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

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor? oaepProcessor);
            SafeRsaHandle key = GetKey();

            return TryEncrypt(key, data, destination, rsaPadding, oaepProcessor, out bytesWritten);
        }

        private static bool TryEncrypt(
            SafeRsaHandle key,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            Interop.Crypto.RsaPadding rsaPadding,
            RsaPaddingProcessor? rsaPaddingProcessor,
            out int bytesWritten)
        {
            int rsaSize = Interop.Crypto.RsaSize(key);

            if (destination.Length < rsaSize)
            {
                bytesWritten = 0;
                return false;
            }

            int returnValue;

            if (rsaPaddingProcessor != null)
            {
                Debug.Assert(rsaPadding == Interop.Crypto.RsaPadding.NoPadding);
                byte[] rented = CryptoPool.Rent(rsaSize);
                Span<byte> tmp = new Span<byte>(rented, 0, rsaSize);

                try
                {
                    rsaPaddingProcessor.PadOaep(data, tmp);
                    returnValue = Interop.Crypto.RsaPublicEncrypt(tmp.Length, tmp, destination, key, rsaPadding);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmp);
                    CryptoPool.Return(rented, clearSize: 0);
                }
            }
            else
            {
                Debug.Assert(rsaPadding != Interop.Crypto.RsaPadding.NoPadding);

                returnValue = Interop.Crypto.RsaPublicEncrypt(data.Length, data, destination, key, rsaPadding);
            }

            CheckReturn(returnValue);

            bytesWritten = returnValue;
            Debug.Assert(returnValue == rsaSize);
            return true;

        }

        private static Interop.Crypto.RsaPadding GetInteropPadding(
            RSAEncryptionPadding padding,
            out RsaPaddingProcessor? rsaPaddingProcessor)
        {
            if (padding == RSAEncryptionPadding.Pkcs1)
            {
                rsaPaddingProcessor = null;
                return Interop.Crypto.RsaPadding.Pkcs1;
            }

            if (padding == RSAEncryptionPadding.OaepSHA1)
            {
                rsaPaddingProcessor = null;
                return Interop.Crypto.RsaPadding.OaepSHA1;
            }

            if (padding.Mode == RSAEncryptionPaddingMode.Oaep)
            {
                rsaPaddingProcessor = RsaPaddingProcessor.OpenProcessor(padding.OaepHashAlgorithm);
                return Interop.Crypto.RsaPadding.NoPadding;
            }

            throw PaddingModeNotSupported();
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeRsaHandle key = GetKey();

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

            FreeKey();
            _key = new Lazy<SafeRsaHandle>(key);

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(BitsPerByte * Interop.Crypto.RsaSize(key));
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

            SafeRsaHandle key = Interop.Crypto.DecodeRsaPublicKey(source.Slice(0, read));

            Interop.Crypto.CheckValidOpenSslHandle(key);

            FreeKey();
            _key = new Lazy<SafeRsaHandle>(key);

            // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
            // with the already loaded key.
            ForceSetKeySize(BitsPerByte * Interop.Crypto.RsaSize(key));

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
                SafeRsaHandle handle = _key.Value;
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

        private SafeEvpPKeyHandle GetPKey()
        {
            SafeRsaHandle currentKey = GetKey();
            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetRsa(pkeyHandle, currentKey))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return pkeyHandle;
            }
            catch
            {
                pkeyHandle.Dispose();
                throw;
            }
        }

        private SafeRsaHandle GetKey()
        {
            ThrowIfDisposed();

            SafeRsaHandle key = _key.Value;

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            return key;
        }

        private static void CheckReturn(int returnValue)
        {
            if (returnValue == -1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        private static void CheckBoolReturn(int returnValue)
        {
            if (returnValue != 1)
            {
               throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        private SafeRsaHandle GenerateKey()
        {
            using (SafeEvpPKeyHandle pkey = Interop.Crypto.RsaGenerateKey(KeySize))
            {
                SafeRsaHandle rsa = Interop.Crypto.EvpPkeyGetRsa(pkey);

                if (rsa.IsInvalid)
                {
                    rsa.Dispose();
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return rsa;
            }
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

            signature = null;

            // Do not factor out getting _key.Value, since the key creation should not happen on
            // invalid padding modes.

            if (padding.Mode == RSASignaturePaddingMode.Pkcs1)
            {
                int algorithmNid = GetAlgorithmNid(hashAlgorithm);
                SafeRsaHandle rsa = GetKey();

                int bytesRequired = Interop.Crypto.RsaSize(rsa);

                if (allocateSignature)
                {
                    Debug.Assert(destination.Length == 0);
                    signature = new byte[bytesRequired];
                    destination = signature;
                }

                if (destination.Length < bytesRequired)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (!Interop.Crypto.RsaSign(algorithmNid, hash, hash.Length, destination, out int signatureSize, rsa))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                Debug.Assert(
                    signatureSize == bytesRequired,
                    $"RSA_sign reported signatureSize was {signatureSize}, when {bytesRequired} was expected");

                bytesWritten = signatureSize;
                return true;
            }
            else if (padding.Mode == RSASignaturePaddingMode.Pss)
            {
                RsaPaddingProcessor processor = RsaPaddingProcessor.OpenProcessor(hashAlgorithm);
                SafeRsaHandle rsa = GetKey();

                int bytesRequired = Interop.Crypto.RsaSize(rsa);

                if (allocateSignature)
                {
                    Debug.Assert(destination.Length == 0);
                    signature = new byte[bytesRequired];
                    destination = signature;
                }

                if (destination.Length < bytesRequired)
                {
                    bytesWritten = 0;
                    return false;
                }

                byte[] pssRented = CryptoPool.Rent(bytesRequired);
                Span<byte> pssBytes = new Span<byte>(pssRented, 0, bytesRequired);

                processor.EncodePss(hash, pssBytes, KeySize);

                int ret = Interop.Crypto.RsaSignPrimitive(pssBytes, destination, rsa);

                CryptoPool.Return(pssRented, bytesRequired);

                CheckReturn(ret);

                Debug.Assert(
                    ret == bytesRequired,
                    $"RSA_private_encrypt returned {ret} when {bytesRequired} was expected");

                bytesWritten = ret;
                return true;
            }

            throw PaddingModeNotSupported();
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
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            if (padding == RSASignaturePadding.Pkcs1)
            {
                int algorithmNid = GetAlgorithmNid(hashAlgorithm);
                SafeRsaHandle rsa = GetKey();
                return Interop.Crypto.RsaVerify(algorithmNid, hash, signature, rsa);
            }
            else if (padding == RSASignaturePadding.Pss)
            {
                RsaPaddingProcessor processor = RsaPaddingProcessor.OpenProcessor(hashAlgorithm);
                SafeRsaHandle rsa = GetKey();

                int requiredBytes = Interop.Crypto.RsaSize(rsa);

                if (signature.Length != requiredBytes)
                {
                    return false;
                }

                if (hash.Length != processor.HashLength)
                {
                    return false;
                }

                byte[] rented = CryptoPool.Rent(requiredBytes);
                Span<byte> unwrapped = new Span<byte>(rented, 0, requiredBytes);

                try
                {
                    int ret = Interop.Crypto.RsaVerificationPrimitive(signature, unwrapped, rsa);

                    CheckReturn(ret);

                    Debug.Assert(
                        ret == requiredBytes,
                        $"RSA_private_encrypt returned {ret} when {requiredBytes} was expected");

                    return processor.VerifyPss(hash, unwrapped, KeySize);
                }
                finally
                {
                    CryptoPool.Return(rented, requiredBytes);
                }
            }

            throw PaddingModeNotSupported();
        }

        private static int GetAlgorithmNid(HashAlgorithmName hashAlgorithmName)
        {
            // All of the current HashAlgorithmName values correspond to the SN values in OpenSSL 0.9.8.
            // If there's ever a new one that doesn't, translate it here.
            string sn = hashAlgorithmName.Name!;

            int nid = Interop.Crypto.ObjSn2Nid(sn);

            if (nid == Interop.Crypto.NID_undef)
            {
                Interop.Crypto.ErrClearError();
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            return nid;
        }

        private static void ValidatePadding(RSAEncryptionPadding padding)
        {
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            if (padding.Mode == RSAEncryptionPaddingMode.Pkcs1)
            {
                // Fail if any options are set other than the mode.
                if (padding != RSAEncryptionPadding.Pkcs1)
                {
                    throw PaddingModeNotSupported();
                }
            }
            else if (padding.Mode != RSAEncryptionPaddingMode.Oaep)
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
