// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        // 65537 (0x10001) in big-endian form
        private static readonly byte[] s_defaultExponent = { 0x01, 0x00, 0x01 };

        private Lazy<SafeRsaHandle> _key;

        public RSAOpenSsl()
            : this(2048)
        {
        }

        public RSAOpenSsl(int keySize)
        {
            KeySize = keySize;
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

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor oaepProcessor);
            SafeRsaHandle key = _key.Value;
            CheckInvalidKey(key);

            int rsaSize = Interop.Crypto.RsaSize(key);
            byte[] buf = null;
            Span<byte> destination = default;

            try
            {
                buf = ArrayPool<byte>.Shared.Rent(rsaSize);
                destination = new Span<byte>(buf, 0, rsaSize);

                if (!TryDecrypt(key, data, destination, rsaPadding, oaepProcessor, out int bytesWritten))
                {
                    Debug.Fail($"{nameof(TryDecrypt)} should not return false for RSA_size buffer");
                    throw new CryptographicException();
                }

                return destination.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(destination);
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public override bool TryDecrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor oaepProcessor);
            SafeRsaHandle key = _key.Value;
            CheckInvalidKey(key);

            int keySizeBytes = Interop.Crypto.RsaSize(key);

            // OpenSSL does not take a length value for the destination, so it can write out of bounds.
            // To prevent the OOB write, decrypt into a temporary buffer.
            if (destination.Length < keySizeBytes)
            {
                Span<byte> tmp = stackalloc byte[0];
                byte[] rent = null;

                // RSA up through 4096 stackalloc
                if (keySizeBytes <= 512)
                {
                    tmp = stackalloc byte[keySizeBytes];
                }
                else
                {
                    rent = ArrayPool<byte>.Shared.Rent(keySizeBytes);
                    tmp = rent;
                }

                bool ret = TryDecrypt(key, data, tmp, rsaPadding, oaepProcessor, out bytesWritten);

                if (ret)
                {
                    tmp = tmp.Slice(0, bytesWritten);

                    if (bytesWritten > destination.Length)
                    {
                        ret = false;
                        bytesWritten = 0;
                    }
                    else
                    {
                        tmp.CopyTo(destination);
                    }

                    CryptographicOperations.ZeroMemory(tmp);
                }

                if (rent != null)
                {
                    // Already cleared
                    ArrayPool<byte>.Shared.Return(rent);
                }

                return ret;
            }

            return TryDecrypt(key, data, destination, rsaPadding, oaepProcessor, out bytesWritten);
        }

        private static bool TryDecrypt(
            SafeRsaHandle key,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            Interop.Crypto.RsaPadding rsaPadding,
            RsaPaddingProcessor rsaPaddingProcessor,
            out int bytesWritten)
        {
            // If rsaPadding is PKCS1 or OAEP-SHA1 then no depadding method should be present.
            // If rsaPadding is NoPadding then a depadding method should be present.
            Debug.Assert(
                (rsaPadding == Interop.Crypto.RsaPadding.NoPadding) ==
                (rsaPaddingProcessor != null));

            // Caller should have already checked this.
            Debug.Assert(!key.IsInvalid);

            int rsaSize = Interop.Crypto.RsaSize(key);

            if (data.Length != rsaSize)
            {
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            }

            if (destination.Length < rsaSize)
            {
                bytesWritten = 0;
                return false;
            }

            Span<byte> decryptBuf = destination;
            byte[] paddingBuf = null;

            if (rsaPaddingProcessor != null)
            {
                paddingBuf = ArrayPool<byte>.Shared.Rent(rsaSize);
                decryptBuf = paddingBuf;
            }

            try
            {
                int returnValue = Interop.Crypto.RsaPrivateDecrypt(data.Length, data, decryptBuf, key, rsaPadding);
                CheckReturn(returnValue);

                if (rsaPaddingProcessor != null)
                {
                    return rsaPaddingProcessor.DepadOaep(paddingBuf, destination, out bytesWritten);
                }
                else
                {
                    // If the padding mode is RSA_NO_PADDING then the size of the decrypted block
                    // will be RSA_size. If any padding was used, then some amount (determined by the padding algorithm)
                    // will have been reduced, and only returnValue bytes were part of the decrypted
                    // body.  Either way, we can just use returnValue, but some additional bytes may have been overwritten
                    // in the destination span.
                    bytesWritten = returnValue;
                }

                return true;
            }
            finally
            {
                if (paddingBuf != null)
                {
                    // DecryptBuf is paddingBuf if paddingBuf is not null, erase it before returning it.
                    // If paddingBuf IS null then decryptBuf was destination, and shouldn't be cleared.
                    CryptographicOperations.ZeroMemory(decryptBuf);
                    ArrayPool<byte>.Shared.Return(paddingBuf);
                }
            }
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor oaepProcessor);
            SafeRsaHandle key = _key.Value;
            CheckInvalidKey(key);

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

            Interop.Crypto.RsaPadding rsaPadding = GetInteropPadding(padding, out RsaPaddingProcessor oaepProcessor);
            SafeRsaHandle key = _key.Value;
            CheckInvalidKey(key);

            return TryEncrypt(key, data, destination, rsaPadding, oaepProcessor, out bytesWritten);
        }

        private static bool TryEncrypt(
            SafeRsaHandle key,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            Interop.Crypto.RsaPadding rsaPadding,
            RsaPaddingProcessor rsaPaddingProcessor,
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
                byte[] rented = ArrayPool<byte>.Shared.Rent(rsaSize);
                Span<byte> tmp = new Span<byte>(rented, 0, rsaSize);

                try
                {
                    rsaPaddingProcessor.PadOaep(data, tmp);
                    returnValue = Interop.Crypto.RsaPublicEncrypt(tmp.Length, tmp, destination, key, rsaPadding);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmp);
                    ArrayPool<byte>.Shared.Return(rented);
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
            out RsaPaddingProcessor rsaPaddingProcessor)
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
            SafeRsaHandle key = _key.Value;

            CheckInvalidKey(key);

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

        public override unsafe void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    AsnReader reader = new AsnReader(manager.Memory, AsnEncodingRules.BER);
                    ReadOnlyMemory<byte> firstElement = reader.PeekEncodedValue();

                    SafeRsaHandle key = Interop.Crypto.DecodeRsaPublicKey(firstElement.Span);

                    Interop.Crypto.CheckValidOpenSslHandle(key);

                    FreeKey();
                    _key = new Lazy<SafeRsaHandle>(key);

                    // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
                    // with the already loaded key.
                    ForceSetKeySize(BitsPerByte * Interop.Crypto.RsaSize(key));

                    bytesRead = firstElement.Length;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FreeKey();
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

        private static void CheckInvalidKey(SafeRsaHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
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
            SafeRsaHandle key = Interop.Crypto.RsaCreate();
            bool generated = false;

            Interop.Crypto.CheckValidOpenSslHandle(key);

            try
            {
                using (SafeBignumHandle exponent = Interop.Crypto.CreateBignum(s_defaultExponent))
                {
                    // The documentation for RSA_generate_key_ex does not say that it returns only
                    // 0 or 1, so the call marshals it back as a full Int32 and checks for a value
                    // of 1 explicitly.
                    int response = Interop.Crypto.RsaGenerateKeyEx(
                        key,
                        KeySize,
                        exponent);

                    CheckBoolReturn(response);
                    generated = true;
                }
            }
            finally
            {
                if (!generated)
                {
                    key.Dispose();
                }
            }

            return key;
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
                out byte[] signature))
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
                out byte[] alloced);

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
            out byte[] signature)
        {
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));
            Debug.Assert(padding != null);

            signature = null;

            // Do not factor out getting _key.Value, since the key creation should not happen on
            // invalid padding modes.

            if (padding.Mode == RSASignaturePaddingMode.Pkcs1)
            {
                int algorithmNid = GetAlgorithmNid(hashAlgorithm);
                SafeRsaHandle rsa = _key.Value;

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
                SafeRsaHandle rsa = _key.Value;

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

                byte[] pssRented = ArrayPool<byte>.Shared.Rent(bytesRequired);
                Span<byte> pssBytes = new Span<byte>(pssRented, 0, bytesRequired);

                processor.EncodePss(hash, pssBytes, KeySize);

                int ret = Interop.Crypto.RsaSignPrimitive(pssBytes, destination, rsa);

                pssBytes.Clear();
                ArrayPool<byte>.Shared.Return(pssRented);

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
                SafeRsaHandle rsa = _key.Value;
                return Interop.Crypto.RsaVerify(algorithmNid, hash, signature, rsa);
            }
            else if (padding == RSASignaturePadding.Pss)
            {
                RsaPaddingProcessor processor = RsaPaddingProcessor.OpenProcessor(hashAlgorithm);
                SafeRsaHandle rsa = _key.Value;

                int requiredBytes = Interop.Crypto.RsaSize(rsa);

                if (signature.Length != requiredBytes)
                {
                    return false;
                }

                if (hash.Length != processor.HashLength)
                {
                    return false;
                }

                byte[] rented = ArrayPool<byte>.Shared.Rent(requiredBytes);
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
                    unwrapped.Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }

            throw PaddingModeNotSupported();
        }

        private static int GetAlgorithmNid(HashAlgorithmName hashAlgorithmName)
        {
            // All of the current HashAlgorithmName values correspond to the SN values in OpenSSL 0.9.8.
            // If there's ever a new one that doesn't, translate it here.
            string sn = hashAlgorithmName.Name;

            int nid = Interop.Crypto.ObjSn2Nid(sn);

            if (nid == Interop.Crypto.NID_undef)
            {
                Interop.Crypto.ErrClearError();
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            return nid;
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
