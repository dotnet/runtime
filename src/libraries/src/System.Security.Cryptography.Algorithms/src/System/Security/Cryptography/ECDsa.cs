// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : AsymmetricAlgorithm
    {
        private static readonly string[] s_validOids =
        {
            Oids.EcPublicKey,
            // ECDH and ECMQV are not valid in this context.
        };

        protected ECDsa() { }

        public static new ECDsa Create(string algorithm)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            return CryptoConfig.CreateFromName(algorithm) as ECDsa;
        }

        /// <summary>
        /// When overridden in a derived class, exports the named or explicit ECParameters for an ECCurve.
        /// If the curve has a name, the Curve property will contain named curve parameters otherwise it will contain explicit parameters.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns></returns>
        public virtual ECParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, exports the explicit ECParameters for an ECCurve.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns></returns>
        public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, imports the specified ECParameters.
        /// </summary>
        /// <param name="parameters">The curve parameters.</param>
        public virtual void ImportParameters(ECParameters parameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, generates a new public/private keypair for the specified curve.
        /// </summary>
        /// <param name="curve">The curve to use.</param>
        public virtual void GenerateKey(ECCurve curve)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        public virtual byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return SignData(data, 0, data.Length, hashAlgorithm);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash);
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            }

            if (TryHashData(data, destination, hashAlgorithm, out int hashLength) &&
                TrySignHash(destination.Slice(0, hashLength), destination, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            }

            // The biggest hash algorithm supported is SHA512, which is only 64 bytes (512 bits).
            // So this should realistically never hit the fallback
            // (it'd require a derived type to add support for a different hash algorithm, and that
            // algorithm to have a large output.)
            Span<byte> buf = stackalloc byte[128];
            ReadOnlySpan<byte> hash = stackalloc byte[0];

            if (TryHashData(data, buf, hashAlgorithm, out int hashLength))
            {
                hash = buf.Slice(0, hashLength);
            }
            else
            {
                // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
                try
                {
                    data.CopyTo(array);
                    hash = HashData(array, 0, data.Length, hashAlgorithm);
                }
                finally
                {
                    Array.Clear(array, 0, data.Length);
                    ArrayPool<byte>.Shared.Return(array);
                }
            }

            return VerifyHash(hash, signature);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature);
        }

        public abstract byte[] SignHash(byte[] hash);
        public abstract bool VerifyHash(byte[] hash, byte[] signature);

        public override string KeyExchangeAlgorithm => null;
        public override string SignatureAlgorithm => "ECDsa";

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(array);
                byte[] hash = HashData(array, 0, data.Length, hashAlgorithm);
                if (hash.Length <= destination.Length)
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

        public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
        {
            byte[] result = SignHash(hash.ToArray());
            if (result.Length <= destination.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifyHash(hash.ToArray(), signature.ToArray());

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
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

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        passwordBytes,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
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

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        password,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override unsafe bool TryExportPkcs8PrivateKey(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(false);

            using (AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                passwordBytes,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                password,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        public override unsafe void ImportPkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadPkcs8<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters key);

            fixed (byte* privPin = key.D)
            {
                try
                {
                    ImportParameters(key);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key.D);
                }
            }
        }

        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPublicKey,
                out int localRead,
                out ECParameters key);

            ImportParameters(key);
            bytesRead = localRead;
        }

        public virtual unsafe void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ECParameters ecParameters = EccKeyFormatHelper.FromECPrivateKey(source, out int localRead);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    ImportParameters(ecParameters);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public virtual unsafe byte[] ExportECPrivateKey()
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.Encode();
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public virtual unsafe bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }
    }
}
