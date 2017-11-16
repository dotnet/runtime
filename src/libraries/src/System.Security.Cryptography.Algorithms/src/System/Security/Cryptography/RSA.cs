// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create(string algName)
        {
            return (RSA)CryptoConfig.CreateFromName(algName);
        }

        public static RSA Create(int keySizeInBits)
        {
            RSA rsa = Create();

            try
            {
                rsa.KeySize = keySizeInBits;
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        public static RSA Create(RSAParameters parameters)
        {
            RSA rsa = Create();

            try
            {
                rsa.ImportParameters(parameters);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        public abstract RSAParameters ExportParameters(bool includePrivateParameters);
        public abstract void ImportParameters(RSAParameters parameters);
        public virtual byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => throw DerivedClassMustOverride();
        public virtual byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => throw DerivedClassMustOverride();
        public virtual byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) => throw DerivedClassMustOverride();
        public virtual bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) => throw DerivedClassMustOverride();

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) => throw DerivedClassMustOverride();
        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) => throw DerivedClassMustOverride();

        public virtual bool TryDecrypt(ReadOnlySpan<byte> source, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            byte[] result = Decrypt(source.ToArray(), padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool TryEncrypt(ReadOnlySpan<byte> source, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            byte[] result = Encrypt(source.ToArray(), padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            byte[] result;
            byte[] array = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                source.CopyTo(array);
                result = HashData(array, 0, source.Length, hashAlgorithm);
            }
            finally
            {
                Array.Clear(array, 0, source.Length);
                ArrayPool<byte>.Shared.Return(array);
            }

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            byte[] result = SignHash(source.ToArray(), hashAlgorithm, padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            VerifyHash(hash.ToArray(), signature.ToArray(), hashAlgorithm, padding);

        private static Exception DerivedClassMustOverride() =>
            new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual byte[] DecryptValue(byte[] rgb) =>
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop

        public virtual byte[] EncryptValue(byte[] rgb) =>
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return SignData(data, 0, data.Length, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            if (TryHashData(source, destination, hashAlgorithm, out int hashLength) &&
                TrySignHash(destination.Slice(0, hashLength), destination, hashAlgorithm, padding, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            for (int i = 256; ; i = checked(i * 2))
            {
                int hashLength = 0;
                byte[] hash = ArrayPool<byte>.Shared.Rent(i);
                try
                {
                    if (TryHashData(data, hash, hashAlgorithm, out hashLength))
                    {
                        return VerifyHash(new ReadOnlySpan<byte>(hash, 0, hashLength), signature, hashAlgorithm, padding);
                    }
                }
                finally
                {
                    Array.Clear(hash, 0, hashLength);
                    ArrayPool<byte>.Shared.Return(hash);
                }
            }
        }

        public override string KeyExchangeAlgorithm => "RSA";
        public override string SignatureAlgorithm => "RSA";

        private static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
    }
}
