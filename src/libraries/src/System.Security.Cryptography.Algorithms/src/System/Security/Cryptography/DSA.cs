// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract partial class DSA : AsymmetricAlgorithm
    {
        public abstract DSAParameters ExportParameters(bool includePrivateParameters);

        public abstract void ImportParameters(DSAParameters parameters);

        protected DSA() { }

        public static new DSA Create(string algName)
        {
            return (DSA)CryptoConfig.CreateFromName(algName);
        }

        public static DSA Create(int keySizeInBits)
        {
            DSA dsa = Create();

            try
            {
                dsa.KeySize = keySizeInBits;
                return dsa;
            }
            catch
            {
                dsa.Dispose();
                throw;
            }
        }

        public static DSA Create(DSAParameters parameters)
        {
            DSA dsa = Create();

            try
            {
                dsa.ImportParameters(parameters);
                return dsa;
            }
            catch
            {
                dsa.Dispose();
                throw;
            }
        }

        // DSA does not encode the algorithm identifier into the signature blob, therefore CreateSignature and
        // VerifySignature do not need the HashAlgorithmName value, only SignData and VerifyData do.
        abstract public byte[] CreateSignature(byte[] rgbHash);

        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            return SignData(data, 0, data.Length, hashAlgorithm);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return CreateSignature(hash);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, hashAlgorithm);
            return CreateSignature(hash);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifySignature(hash, signature);
        }

        public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifySignature(hash, signature);
        }

        public virtual bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
        {
            byte[] sig = CreateSignature(hash.ToArray());
            if (sig.Length <= destination.Length)
            {
                new ReadOnlySpan<byte>(sig).CopyTo(destination);
                bytesWritten = sig.Length;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(array);
                byte[] hash = HashData(array, 0, data.Length, hashAlgorithm);
                if (destination.Length >= hash.Length)
                {
                    new ReadOnlySpan<byte>(hash).CopyTo(destination);
                    bytesWritten = hash.Length;
                    return true;
                }
                else
                {
                    bytesWritten = 0;
                    return false;
                }
            }
            finally
            {
                Array.Clear(array, 0, data.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }

            if (TryHashData(data, destination, hashAlgorithm, out int hashLength) &&
                TryCreateSignature(destination.Slice(0, hashLength), destination, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }

            for (int i = 256; ; i = checked(i * 2))
            {
                int hashLength = 0;
                byte[] hash = ArrayPool<byte>.Shared.Rent(i);
                try
                {
                    if (TryHashData(data, hash, hashAlgorithm, out hashLength))
                    {
                        return VerifySignature(new ReadOnlySpan<byte>(hash, 0, hashLength), signature);
                    }
                }
                finally
                {
                    Array.Clear(hash, 0, hashLength);
                    ArrayPool<byte>.Shared.Return(hash);
                }
            }
        }

        public virtual bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifySignature(hash.ToArray(), signature.ToArray());

        private static Exception DerivedClassMustOverride() =>
            new NotImplementedException(SR.NotSupported_SubclassOverride);

        internal static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            using (AsnWriter pkcs8PrivateKey = WritePkcs8())
            using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                passwordBytes,
                pkcs8PrivateKey,
                pbeParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            using (AsnWriter pkcs8PrivateKey = WritePkcs8())
            using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                password,
                pkcs8PrivateKey,
                pbeParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        public override bool TryExportPkcs8PrivateKey(
            Span<byte> destination,
            out int bytesWritten)
        {
            using (AsnWriter writer = WritePkcs8())
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            using (AsnWriter writer = WriteSubjectPublicKeyInfo())
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        private unsafe AsnWriter WritePkcs8()
        {
            DSAParameters dsaParameters = ExportParameters(true);

            fixed (byte* privPin = dsaParameters.X)
            {
                try
                {
                    return DSAKeyFormatHelper.WritePkcs8(dsaParameters);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(dsaParameters.X);
                }
            }
        }

        private AsnWriter WriteSubjectPublicKeyInfo()
        {
            DSAParameters dsaParameters = ExportParameters(false);
            return DSAKeyFormatHelper.WriteSubjectPublicKeyInfo(dsaParameters);
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                passwordBytes,
                out int localRead,
                out DSAParameters ret);

            fixed (byte* privPin = ret.X)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.X);
                }
            }

            bytesRead = localRead;
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                password,
                out int localRead,
                out DSAParameters ret);

            fixed (byte* privPin = ret.X)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.X);
                }
            }
            
            bytesRead = localRead;
        }

        public override unsafe void ImportPkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadPkcs8(
                source,
                out int localRead,
                out DSAParameters key);

            fixed (byte* privPin = key.X)
            {
                try
                {
                    ImportParameters(key);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key.X);
                }
            }

            bytesRead = localRead;
        }

        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                source,
                out int localRead,
                out DSAParameters key);

            ImportParameters(key);
            bytesRead = localRead;
        }
    }
}
