// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs8PrivateKeyInfo
    {
        public Oid AlgorithmId { get; }
        public ReadOnlyMemory<byte>? AlgorithmParameters { get; }
        public CryptographicAttributeObjectCollection Attributes { get; }
        public ReadOnlyMemory<byte> PrivateKeyBytes { get; }

        public Pkcs8PrivateKeyInfo(
            Oid algorithmId,
            ReadOnlyMemory<byte>? algorithmParameters,
            ReadOnlyMemory<byte> privateKey,
            bool skipCopies = false)
        {
            if (algorithmId == null)
                throw new ArgumentNullException(nameof(algorithmId));

            if (algorithmParameters?.Length > 0)
            {
                // Read to ensure that there is precisely one legally encoded value.
                PkcsHelpers.EnsureSingleBerValue(algorithmParameters.Value.Span);
            }

            AlgorithmId = algorithmId;
            AlgorithmParameters = skipCopies ? algorithmParameters : algorithmParameters?.ToArray();
            PrivateKeyBytes = skipCopies ? privateKey : privateKey.ToArray();
            Attributes = new CryptographicAttributeObjectCollection();
        }

        private Pkcs8PrivateKeyInfo(
            Oid algorithmId,
            ReadOnlyMemory<byte>? algorithmParameters,
            ReadOnlyMemory<byte> privateKey,
            CryptographicAttributeObjectCollection attributes)
        {
            Debug.Assert(algorithmId != null);

            AlgorithmId = algorithmId;
            AlgorithmParameters = algorithmParameters;
            PrivateKeyBytes = privateKey;
            Attributes = attributes;
        }

        public static Pkcs8PrivateKeyInfo Create(AsymmetricAlgorithm privateKey)
        {
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            byte[] pkcs8 = privateKey.ExportPkcs8PrivateKey();
            return Decode(pkcs8, out _, skipCopy: true);
        }

        public static Pkcs8PrivateKeyInfo Decode(
            ReadOnlyMemory<byte> source,
            out int bytesRead,
            bool skipCopy = false)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
                // By using the default/empty ReadOnlyMemory value, the Decode method will have to
                // make copies of the data when assigning ReadOnlyMemory fields.
                ReadOnlyMemory<byte> rebind = skipCopy ? source : default;

                int localRead = reader.PeekEncodedValue().Length;
                PrivateKeyInfoAsn.Decode(ref reader, rebind, out PrivateKeyInfoAsn privateKeyInfo);
                bytesRead = localRead;

                return new Pkcs8PrivateKeyInfo(
                    new Oid(privateKeyInfo.PrivateKeyAlgorithm.Algorithm, null),
                    privateKeyInfo.PrivateKeyAlgorithm.Parameters,
                    privateKeyInfo.PrivateKey,
                    SignerInfo.MakeAttributeCollection(privateKeyInfo.Attributes));
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public byte[] Encode()
        {
            AsnWriter writer = WritePkcs8();
            return writer.Encode();
        }

        public byte[] Encrypt(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            AsnWriter pkcs8 = WritePkcs8();
            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8, pbeParameters);
            {
                return writer.Encode();
            }
        }

        public byte[] Encrypt(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            AsnWriter pkcs8 = WritePkcs8();
            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8, pbeParameters);
            return writer.Encode();
        }

        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter writer = WritePkcs8();
            return writer.TryEncode(destination, out bytesWritten);
        }

        public bool TryEncrypt(
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

            AsnWriter pkcs8 = WritePkcs8();
            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8, pbeParameters);
            return writer.TryEncode(destination, out bytesWritten);
        }

        public bool TryEncrypt(
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

            AsnWriter pkcs8 = WritePkcs8();
            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8, pbeParameters);
            return writer.TryEncode(destination, out bytesWritten);
        }

        public static Pkcs8PrivateKeyInfo DecryptAndDecode(
            ReadOnlySpan<char> password,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            ArraySegment<byte> decrypted = KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                out int localRead);

            Memory<byte> decryptedMemory = decrypted;

            try
            {
                Pkcs8PrivateKeyInfo ret = Decode(decryptedMemory, out int decoded);
                Debug.Assert(!ret.PrivateKeyBytes.Span.Overlaps(decryptedMemory.Span));

                if (decoded != decryptedMemory.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                bytesRead = localRead;
                return ret;
            }
            finally
            {
                CryptoPool.Return(decrypted);
            }
        }

        public static Pkcs8PrivateKeyInfo DecryptAndDecode(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            ArraySegment<byte> decrypted = KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                out int localRead);

            Memory<byte> decryptedMemory = decrypted;

            try
            {
                Pkcs8PrivateKeyInfo ret = Decode(decryptedMemory, out int decoded);
                Debug.Assert(!ret.PrivateKeyBytes.Span.Overlaps(decryptedMemory.Span));

                if (decoded != decryptedMemory.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                bytesRead = localRead;
                return ret;
            }
            finally
            {
                CryptoPool.Return(decrypted);
            }
        }

        private AsnWriter WritePkcs8()
        {
            PrivateKeyInfoAsn info = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm =
                {
                    Algorithm = AlgorithmId.Value!,
                },
                PrivateKey = PrivateKeyBytes,
            };

            if (AlgorithmParameters?.Length > 0)
            {
                info.PrivateKeyAlgorithm.Parameters = AlgorithmParameters;
            }

            if (Attributes.Count > 0)
            {
                info.Attributes = PkcsHelpers.NormalizeAttributeSet(CmsSigner.BuildAttributes(Attributes).ToArray());
            }

            // Write in BER in case any of the provided fields was BER.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            info.Encode(writer);
            return writer;
        }
    }
}
