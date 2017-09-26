// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.NativeCrypto;
using System.IO;

namespace System.Security.Cryptography
{
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        private const int DefaultKeySize = 1024;

        private readonly RSA _impl;
        private bool _publicOnly;

        public RSACryptoServiceProvider()
            : this(DefaultKeySize) { }

        public RSACryptoServiceProvider(int dwKeySize)
        {
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException(nameof(dwKeySize), SR.ArgumentOutOfRange_NeedNonNegNum);

            // This class wraps RSA
            _impl = RSA.Create(dwKeySize);
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        public RSACryptoServiceProvider(CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        public CspKeyContainerInfo CspKeyContainerInfo =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspKeyContainerInfo)));

        public byte[] Decrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
                throw new ArgumentNullException(nameof(rgb));

            // size check -- must be at most the modulus size
            if (rgb.Length > (KeySize / 8))
                throw new CryptographicException(SR.Format(SR.Cryptography_Padding_DecDataTooBig, Convert.ToString(KeySize / 8)));

            return _impl.Decrypt(rgb, fOAEP ? RSAEncryptionPadding.OaepSHA1 : RSAEncryptionPadding.Pkcs1);
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            return
                padding == RSAEncryptionPadding.Pkcs1 ? Decrypt(data, fOAEP: false) :
                padding == RSAEncryptionPadding.OaepSHA1 ? Decrypt(data, fOAEP: true) : // For compat, this prevents OaepSHA2 options as fOAEP==true will cause Decrypt to use OaepSHA1
                throw PaddingModeNotSupported();
        }

        public override bool TryDecrypt(ReadOnlySpan<byte> source, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));
            if (source.Length > (KeySize / 8))
                throw new CryptographicException(SR.Format(SR.Cryptography_Padding_DecDataTooBig, Convert.ToString(KeySize / 8)));
            if (padding != RSAEncryptionPadding.Pkcs1 && padding != RSAEncryptionPadding.OaepSHA1)
                throw PaddingModeNotSupported();

            return _impl.TryDecrypt(source, destination, padding, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
                base.Dispose(disposing);
            }
        }

        public byte[] Encrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
                throw new ArgumentNullException(nameof(rgb));

            return _impl.Encrypt(rgb, fOAEP ? RSAEncryptionPadding.OaepSHA1 : RSAEncryptionPadding.Pkcs1);
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            return
                padding == RSAEncryptionPadding.Pkcs1 ? Encrypt(data, fOAEP: false) :
                padding == RSAEncryptionPadding.OaepSHA1 ?  Encrypt(data, fOAEP: true) : // For compat, this prevents OaepSHA2 options as fOAEP==true will cause Decrypt to use OaepSHA1
                throw PaddingModeNotSupported();
        }

        public override bool TryEncrypt(ReadOnlySpan<byte> source, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));
            if (padding != RSAEncryptionPadding.Pkcs1 && padding != RSAEncryptionPadding.OaepSHA1)
                throw PaddingModeNotSupported();

            return _impl.TryEncrypt(source, destination, padding, out bytesWritten);
        }

        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            RSAParameters parameters = ExportParameters(includePrivateParameters);
            return parameters.ToKeyBlob();
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters) =>
            _impl.ExportParameters(includePrivateParameters);

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
            AsymmetricAlgorithmHelpers.TryHashData(source, destination, hashAlgorithm, out bytesWritten);

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

        public override void FromXmlString(string xmlString) => _impl.FromXmlString(xmlString);

        public void ImportCspBlob(byte[] keyBlob)
        {
            RSAParameters parameters = CapiHelper.ToRSAParameters(keyBlob, !IsPublic(keyBlob));
            ImportParameters(parameters);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            // Although _impl supports larger Exponent, limit here for compat.
            if (parameters.Exponent == null || parameters.Exponent.Length > 4)
                throw new CryptographicException(SR.Argument_InvalidValue);

            _impl.ImportParameters(parameters);

            // P was verified in ImportParameters
            _publicOnly = (parameters.P == null || parameters.P.Length == 0);
        }

        public override string KeyExchangeAlgorithm => _impl.KeyExchangeAlgorithm;

        public override int KeySize
        {
            get { return _impl.KeySize; }
            set { _impl.KeySize = value; }
        }

        public override KeySizes[] LegalKeySizes => 
            _impl.LegalKeySizes; // Csp Windows and RSAOpenSsl are the same (384, 16384, 8)

        // PersistKeyInCsp has no effect in Unix
        public bool PersistKeyInCsp { get; set; }

        public bool PublicOnly => _publicOnly;

        public override string SignatureAlgorithm => "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

        public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignData(data, hashAlgorithm, padding);

        public override byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignData(data, offset, count, hashAlgorithm, padding);

        public override bool TrySignData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten) =>
            _impl.TrySignData(source, destination, hashAlgorithm, padding, out bytesWritten);

        public byte[] SignData(byte[] buffer, int offset, int count, object halg) =>
            _impl.SignData(buffer, offset, count, HashAlgorithmNames.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public byte[] SignData(byte[] buffer, object halg) =>
            _impl.SignData(buffer, HashAlgorithmNames.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public byte[] SignData(Stream inputStream, object halg) =>
            _impl.SignData(inputStream, HashAlgorithmNames.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignHash(hash, hashAlgorithm, padding);

        public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten) =>
            _impl.TrySignHash(source, destination, hashAlgorithm, padding, out bytesWritten);

        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (PublicOnly)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            HashAlgorithmName algName = HashAlgorithmNames.NameOrOidToHashAlgorithmName(str);
            return _impl.SignHash(rgbHash, algName, RSASignaturePadding.Pkcs1);
        }

        public override string ToXmlString(bool includePrivateParameters) => _impl.ToXmlString(includePrivateParameters);

        public bool VerifyData(byte[] buffer, object halg, byte[] signature) =>
            _impl.VerifyData(buffer, signature, HashAlgorithmNames.ObjToHashAlgorithmName(halg), RSASignaturePadding.Pkcs1);

        public override bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.VerifyData(data, offset, count, signature, hashAlgorithm, padding);

        public override bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.VerifyData(data, signature, hashAlgorithm, padding);

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
        }

        public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            padding == null ? throw new ArgumentNullException(nameof(padding)) :
            padding != RSASignaturePadding.Pkcs1 ? throw PaddingModeNotSupported() :
            _impl.VerifyHash(hash, signature, hashAlgorithm, padding);

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            if (rgbHash == null)
            {
                throw new ArgumentNullException(nameof(rgbHash));
            }
            if (rgbSignature == null)
            {
                throw new ArgumentNullException(nameof(rgbSignature));
            }

            return VerifyHash(
                (ReadOnlySpan<byte>)rgbHash, (ReadOnlySpan<byte>)rgbSignature,
                HashAlgorithmNames.NameOrOidToHashAlgorithmName(str), RSASignaturePadding.Pkcs1);
        }

        // UseMachineKeyStore has no effect in Unix
        public static bool UseMachineKeyStore { get; set; }

        private static Exception PaddingModeNotSupported()
        {
            return new CryptographicException(SR.Cryptography_InvalidPaddingMode);
        }

        /// <summary>
        /// find whether an RSA key blob is public.
        /// </summary>
        private static bool IsPublic(byte[] keyBlob)
        {
            if (keyBlob == null)
                throw new ArgumentNullException(nameof(keyBlob));

            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY

            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != CapiHelper.PUBLICKEYBLOB)
            {
                return false;
            }

            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52)
            {
                return false;
            }

            return true;
        }
    }
}
