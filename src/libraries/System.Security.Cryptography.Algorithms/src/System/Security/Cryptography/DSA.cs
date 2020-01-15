// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
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
        public abstract byte[] CreateSignature(byte[] rgbHash);

        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

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

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            return SignDataCore(data, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }

            return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
        }

        protected virtual byte[] SignDataCore(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            int size = GetMaxSignatureSize(signatureFormat);
            Debug.Assert(size <= 256, $"GetMaxSignatureSize returned more than expected ({size}) for {signatureFormat}.");
            Span<byte> signature = stackalloc byte[size];
            bool result = TrySignDataCore(data, signature, hashAlgorithm, signatureFormat, out int bytesWritten);
            Debug.Assert(result, $"GetMaxSignatureSize returned insufficient size ({size}) for {signatureFormat}.");
            return signature.Slice(0, bytesWritten).ToArray();
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            return SignDataCore(data, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        protected virtual byte[] SignDataCore(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, hashAlgorithm);
            return CreateSignatureCore(hash, signatureFormat);
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

            return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }

            return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, signatureFormat);
        }

        public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (string.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            return VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public byte[] CreateSignature(byte[] rgbHash, DSASignatureFormat signatureFormat)
        {
            if (rgbHash == null)
            {
                throw new ArgumentNullException(nameof(rgbHash));
            }

            return CreateSignatureCore(rgbHash, signatureFormat);
        }

        protected virtual byte[] CreateSignatureCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
        {
            int size = GetMaxSignatureSize(signatureFormat);
            Debug.Assert(size <= 256, $"GetMaxSignatureSize returned more than expected ({size}) for {signatureFormat}.");
            Span<byte> signature = stackalloc byte[size];
            bool result = TryCreateSignatureCore(hash, signature, signatureFormat, out int bytesWritten);
            Debug.Assert(result, $"GetMaxSignatureSize returned insufficient size ({size}) for {signatureFormat}.");
            return signature.Slice(0, bytesWritten).ToArray();
        }

        public virtual bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            => TryCreateSignatureCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);

        public bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
            => TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);

        protected virtual bool TryCreateSignatureCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
        {
            // This method is expected to be overriden with better implementation

            // The only available implmentation here is abstract method, use it
            byte[] sig = CreateSignature(hash.ToArray());
            sig = AsymmetricAlgorithmHelpers.ConvertIeeeP1363Signature(sig, signatureFormat);
            return Helpers.TryCopyToDestination(sig, destination, out bytesWritten);
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(array);
                byte[] hash = HashData(array, 0, data.Length, hashAlgorithm);
                return Helpers.TryCopyToDestination(hash, destination, out bytesWritten);
            }
            finally
            {
                Array.Clear(array, 0, data.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {

            return TrySignDataCore(data, destination, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
        }

        public bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
        {
            return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
        }

        protected virtual bool TrySignDataCore(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }

            if (hashAlgorithm.TryGetSizeInBytes(out int hashSize))
            {
                Span<byte> hash = stackalloc byte[hashSize];
                if (TryHashData(data, hash, hashAlgorithm, out int hashLength))
                {
                    return TryCreateSignatureCore(hash.Slice(0, hashLength), destination, signatureFormat, out bytesWritten);
                }
            }
            else
            {
                // This will likely fail but since HashData is virtual we will attempt the slow path
                byte[] hash = HashData(data.ToArray(), 0, data.Length, hashAlgorithm);
                return TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
            => VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        protected virtual bool VerifyDataCore(Stream data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifySignatureCore(hash, signature, signatureFormat);
        }

        public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        protected virtual bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            if (hashAlgorithm.TryGetSizeInBytes(out int hashSize))
            {
                Span<byte> hash = stackalloc byte[hashSize];
                bool result = TryHashData(data, hash, hashAlgorithm, out int bytesWritten);
                Debug.Assert(result, $"TryGetSizeInBytes returned insufficient size for {hashAlgorithm.Name}: {hashSize}. TryHashData wrote {bytesWritten}.");
                Debug.Assert(bytesWritten == hashSize, $"TryGetSizeInBytes returned too large size for {hashAlgorithm.Name}: {hashSize}. TryHashData wrote {bytesWritten}.");
                return VerifySignatureCore(hash, signature, signatureFormat);
            }
            else
            {
                // This is expected to fail but we will try it anyway since HashData can be overriden
                byte[] hash = HashData(data.ToArray(), 0, data.Length, hashAlgorithm);
                return VerifySignatureCore(hash, signature, signatureFormat);
            }
        }

        public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature, DSASignatureFormat signatureFormat)
        {
            if (rgbHash == null)
            {
                throw new ArgumentNullException(nameof(rgbHash));
            }

            if (rgbSignature == null)
            {
                throw new ArgumentNullException(nameof(rgbSignature));
            }

            return VerifySignatureCore(rgbHash, rgbSignature, signatureFormat);
        }

        public virtual bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
            => VerifySignatureCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        public bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
            => VerifySignatureCore(hash, signature, signatureFormat);

        protected virtual bool VerifySignatureCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
        {
            // This method is expected to be overriden with better implementation

            byte[] sig = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);

            if (sig == null)
                return false;

            // The only available implmentation here is abstract method, use it
            return VerifySignature(hash.ToArray(), sig);
        }

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

        public int GetMaxSignatureSize(DSASignatureFormat signatureFormat)
        {
            DSAParameters dsaParameters = ExportParameters(false);
            int qLength = dsaParameters.Q.Length;

            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return qLength * 2;
                case DSASignatureFormat.Rfc3279DerSequence:
                    // Add 15 for extra ASN.1 headers
                    return qLength * 2 + 15;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signatureFormat));
            }
        }
    }
}
