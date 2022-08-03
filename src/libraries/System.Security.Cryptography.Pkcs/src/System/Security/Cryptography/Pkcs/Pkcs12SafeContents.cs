// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12SafeContents
    {
        private ReadOnlyMemory<byte> _encrypted;
        private List<Pkcs12SafeBag>? _bags;

        public Pkcs12ConfidentialityMode ConfidentialityMode { get; private set; }
        public bool IsReadOnly { get; }

        public Pkcs12SafeContents()
        {
            ConfidentialityMode = Pkcs12ConfidentialityMode.None;
        }

        internal Pkcs12SafeContents(ReadOnlyMemory<byte> serialized)
        {
            IsReadOnly = true;
            ConfidentialityMode = Pkcs12ConfidentialityMode.None;
            _bags = ReadBags(serialized);
        }

        internal Pkcs12SafeContents(ContentInfoAsn contentInfoAsn)
        {
            IsReadOnly = true;

            switch (contentInfoAsn.ContentType)
            {
                case Oids.Pkcs7Encrypted:
                    ConfidentialityMode = Pkcs12ConfidentialityMode.Password;
                    _encrypted = contentInfoAsn.Content;
                    break;
                case Oids.Pkcs7Enveloped:
                    ConfidentialityMode = Pkcs12ConfidentialityMode.PublicKey;
                    _encrypted = contentInfoAsn.Content;
                    break;
                case Oids.Pkcs7Data:
                    ConfidentialityMode = Pkcs12ConfidentialityMode.None;
                    _bags = ReadBags(PkcsHelpers.DecodeOctetStringAsMemory(contentInfoAsn.Content));
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        public void AddSafeBag(Pkcs12SafeBag safeBag)
        {
            if (safeBag is null)
            {
                throw new ArgumentNullException(nameof(safeBag));
            }

            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            _bags ??= new List<Pkcs12SafeBag>();

            _bags.Add(safeBag);
        }

        public Pkcs12CertBag AddCertificate(X509Certificate2 certificate)
        {
            if (certificate is null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            Pkcs12CertBag bag = new Pkcs12CertBag(certificate);
            AddSafeBag(bag);
            return bag;
        }

        public Pkcs12KeyBag AddKeyUnencrypted(AsymmetricAlgorithm key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            byte[] pkcs8PrivateKey = key.ExportPkcs8PrivateKey();
            Pkcs12KeyBag bag = new Pkcs12KeyBag(pkcs8PrivateKey, skipCopy: true);
            AddSafeBag(bag);
            return bag;
        }

        public Pkcs12SafeContentsBag AddNestedContents(Pkcs12SafeContents safeContents)
        {
            if (safeContents is null)
            {
                throw new ArgumentNullException(nameof(safeContents));
            }

            if (safeContents.ConfidentialityMode != Pkcs12ConfidentialityMode.None)
                throw new ArgumentException(SR.Cryptography_Pkcs12_CannotProcessEncryptedSafeContents, nameof(safeContents));
            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            Pkcs12SafeContentsBag bag = Pkcs12SafeContentsBag.Create(safeContents);
            AddSafeBag(bag);
            return bag;
        }

        public Pkcs12ShroudedKeyBag AddShroudedKey(
            AsymmetricAlgorithm key,
            byte[]? passwordBytes,
            PbeParameters pbeParameters)
        {
            return AddShroudedKey(
                key,
                // Allows null
                new ReadOnlySpan<byte>(passwordBytes),
                pbeParameters);
        }

        public Pkcs12ShroudedKeyBag AddShroudedKey(
            AsymmetricAlgorithm key,
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);
            Pkcs12ShroudedKeyBag bag = new Pkcs12ShroudedKeyBag(encryptedPkcs8, skipCopy: true);
            AddSafeBag(bag);
            return bag;
        }

        public Pkcs12ShroudedKeyBag AddShroudedKey(
            AsymmetricAlgorithm key,
            string? password,
            PbeParameters pbeParameters)
        {
            return AddShroudedKey(
                key,
                // This extension method invoke allows null.
                password.AsSpan(),
                pbeParameters);
        }

        public Pkcs12ShroudedKeyBag AddShroudedKey(
            AsymmetricAlgorithm key,
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (IsReadOnly)
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsReadOnly);

            byte[] encryptedPkcs8 = key.ExportEncryptedPkcs8PrivateKey(password, pbeParameters);
            Pkcs12ShroudedKeyBag bag = new Pkcs12ShroudedKeyBag(encryptedPkcs8, skipCopy: true);
            AddSafeBag(bag);
            return bag;
        }

        public Pkcs12SecretBag AddSecret(Oid secretType, ReadOnlyMemory<byte> secretValue)
        {
            if (secretType is null)
            {
                throw new ArgumentNullException(nameof(secretType));
            }

            // Read to ensure that there is precisely one legally encoded value.
            PkcsHelpers.EnsureSingleBerValue(secretValue.Span);

            Pkcs12SecretBag bag = new Pkcs12SecretBag(secretType, secretValue);
            AddSafeBag(bag);
            return bag;
        }

        public void Decrypt(byte[]? passwordBytes)
        {
            // Null is permitted
            Decrypt(new ReadOnlySpan<byte>(passwordBytes));
        }

        public void Decrypt(ReadOnlySpan<byte> passwordBytes)
        {
            Decrypt(ReadOnlySpan<char>.Empty, passwordBytes);
        }

        public void Decrypt(string? password)
        {
            // The string.AsSpan extension method allows null.
            Decrypt(password.AsSpan());
        }

        public void Decrypt(ReadOnlySpan<char> password)
        {
            Decrypt(password, ReadOnlySpan<byte>.Empty);
        }

        private void Decrypt(ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes)
        {
            if (ConfidentialityMode != Pkcs12ConfidentialityMode.Password)
            {
                throw new InvalidOperationException(
                    SR.Format(
                        SR.Cryptography_Pkcs12_WrongModeForDecrypt,
                        Pkcs12ConfidentialityMode.Password,
                        ConfidentialityMode));
            }

            EncryptedDataAsn encryptedData = EncryptedDataAsn.Decode(_encrypted, AsnEncodingRules.BER);

            // https://tools.ietf.org/html/rfc5652#section-8
            if (encryptedData.Version != 0 && encryptedData.Version != 2)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Since the contents are supposed to be the BER-encoding of an instance of
            // SafeContents (https://tools.ietf.org/html/rfc7292#section-4.1) that implies the
            // content type is simply "data", and that content is present.
            if (encryptedData.EncryptedContentInfo.ContentType != Oids.Pkcs7Data)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (!encryptedData.EncryptedContentInfo.EncryptedContent.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            List<Pkcs12SafeBag> bags;
            int encryptedValueLength = encryptedData.EncryptedContentInfo.EncryptedContent.Value.Length;

            // Don't use the array pool because the parsed bags are going to have ReadOnlyMemory projections
            // over this data.
            byte[] destination = new byte[encryptedValueLength];

            int written = PasswordBasedEncryption.Decrypt(
                encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                password,
                passwordBytes,
                encryptedData.EncryptedContentInfo.EncryptedContent.Value.Span,
                destination);

            try
            {
                bags = ReadBags(destination.AsMemory(0, written));
            }
            catch
            {
                CryptographicOperations.ZeroMemory(destination.AsSpan(0, written));
                throw;
            }

            _encrypted = ReadOnlyMemory<byte>.Empty;
            _bags = bags;
            ConfidentialityMode = Pkcs12ConfidentialityMode.None;
        }

        public IEnumerable<Pkcs12SafeBag> GetBags()
        {
            if (ConfidentialityMode != Pkcs12ConfidentialityMode.None)
            {
                throw new InvalidOperationException(SR.Cryptography_Pkcs12_SafeContentsIsEncrypted);
            }

            if (_bags == null)
            {
                return Enumerable.Empty<Pkcs12SafeBag>();
            }

            return _bags.AsReadOnly();
        }

        private static List<Pkcs12SafeBag> ReadBags(ReadOnlyMemory<byte> serialized)
        {
            List<SafeBagAsn> serializedBags = new List<SafeBagAsn>();

            try
            {
                AsnValueReader reader = new AsnValueReader(serialized.Span, AsnEncodingRules.BER);
                AsnValueReader sequenceReader = reader.ReadSequence();

                reader.ThrowIfNotEmpty();
                while (sequenceReader.HasData)
                {
                    SafeBagAsn.Decode(ref sequenceReader, serialized, out SafeBagAsn serializedBag);
                    serializedBags.Add(serializedBag);
                }

                if (serializedBags.Count == 0)
                {
                    return new List<Pkcs12SafeBag>(0);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            List<Pkcs12SafeBag> bags = new List<Pkcs12SafeBag>(serializedBags.Count);

            for (int i = 0; i < serializedBags.Count; i++)
            {
                ReadOnlyMemory<byte> bagValue = serializedBags[i].BagValue;
                Pkcs12SafeBag? bag = null;

                try
                {
                    switch (serializedBags[i].BagId)
                    {
                        case Oids.Pkcs12KeyBag:
                            bag = new Pkcs12KeyBag(bagValue);
                            break;
                        case Oids.Pkcs12ShroudedKeyBag:
                            bag = new Pkcs12ShroudedKeyBag(bagValue);
                            break;
                        case Oids.Pkcs12CertBag:
                            bag = Pkcs12CertBag.DecodeValue(bagValue);
                            break;
                        case Oids.Pkcs12CrlBag:
                            // Known, but no first-class support currently.
                            break;
                        case Oids.Pkcs12SecretBag:
                            bag = Pkcs12SecretBag.DecodeValue(bagValue);
                            break;
                        case Oids.Pkcs12SafeContentsBag:
                            bag = Pkcs12SafeContentsBag.Decode(bagValue);
                            break;
                    }
                }
                catch (AsnContentException)
                {
                }
                catch (CryptographicException)
                {
                }

                bag ??= new Pkcs12SafeBag.UnknownBag(serializedBags[i].BagId, bagValue);

                bag.Attributes = SignerInfo.MakeAttributeCollection(serializedBags[i].BagAttributes);
                bags.Add(bag);
            }

            return bags;
        }

        internal byte[] Encrypt(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            Debug.Assert(pbeParameters != null);
            Debug.Assert(pbeParameters.IterationCount >= 1);

            AsnWriter contentsWriter = Encode();

            PasswordBasedEncryption.InitiateEncryption(
                pbeParameters,
                out SymmetricAlgorithm cipher,
                out string hmacOid,
                out string encryptionAlgorithmOid,
                out bool isPkcs12);

            int cipherBlockBytes = cipher.BlockSize / 8;
            byte[] encryptedRent = CryptoPool.Rent(contentsWriter.GetEncodedLength() + cipherBlockBytes);
            Span<byte> encryptedSpan = Span<byte>.Empty;
            Span<byte> iv = stackalloc byte[cipherBlockBytes];
            Span<byte> salt = stackalloc byte[16];
            RandomNumberGenerator.Fill(salt);

            try
            {
                int written = PasswordBasedEncryption.Encrypt(
                    password,
                    passwordBytes,
                    cipher,
                    isPkcs12,
                    contentsWriter,
                    pbeParameters,
                    salt,
                    encryptedRent,
                    iv);

                encryptedSpan = encryptedRent.AsSpan(0, written);

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                // EncryptedData
                writer.PushSequence();

                // version
                // Since we're not writing unprotected attributes, version=0
                writer.WriteInteger(0);

                // encryptedContentInfo
                {
                    writer.PushSequence();
                    writer.WriteObjectIdentifierForCrypto(Oids.Pkcs7Data);

                    PasswordBasedEncryption.WritePbeAlgorithmIdentifier(
                        writer,
                        isPkcs12,
                        encryptionAlgorithmOid,
                        salt,
                        pbeParameters.IterationCount,
                        hmacOid,
                        iv);

                    writer.WriteOctetString(encryptedSpan, new Asn1Tag(TagClass.ContextSpecific, 0));
                    writer.PopSequence();
                }

                writer.PopSequence();

                return writer.Encode();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encryptedSpan);
                CryptoPool.Return(encryptedRent, clearSize: 0);
            }
        }

        internal AsnWriter Encode()
        {
            AsnWriter writer;

            if (ConfidentialityMode == Pkcs12ConfidentialityMode.Password ||
                ConfidentialityMode == Pkcs12ConfidentialityMode.PublicKey)
            {
                writer = new AsnWriter(AsnEncodingRules.BER);
                writer.WriteEncodedValueForCrypto(_encrypted.Span);
                return writer;
            }

            Debug.Assert(ConfidentialityMode == Pkcs12ConfidentialityMode.None);

            writer = new AsnWriter(AsnEncodingRules.BER);

            writer.PushSequence();

            if (_bags != null)
            {
                foreach (Pkcs12SafeBag safeBag in _bags)
                {
                    safeBag.EncodeTo(writer);
                }
            }

            writer.PopSequence();
            return writer;
        }

        internal ContentInfoAsn EncodeToContentInfo()
        {
            AsnWriter contentsWriter = Encode();

            if (ConfidentialityMode == Pkcs12ConfidentialityMode.None)
            {
                AsnWriter valueWriter = new AsnWriter(AsnEncodingRules.DER);

                using (valueWriter.PushOctetString())
                {
                    contentsWriter.CopyTo(valueWriter);
                }

                return new ContentInfoAsn
                {
                    ContentType = Oids.Pkcs7Data,
                    Content = valueWriter.Encode(),
                };
            }

            if (ConfidentialityMode == Pkcs12ConfidentialityMode.Password)
            {
                return new ContentInfoAsn
                {
                    ContentType = Oids.Pkcs7Encrypted,
                    Content = contentsWriter.Encode(),
                };
            }

            if (ConfidentialityMode == Pkcs12ConfidentialityMode.PublicKey)
            {
                return new ContentInfoAsn
                {
                    ContentType = Oids.Pkcs7Enveloped,
                    Content = contentsWriter.Encode(),
                };
            }

            Debug.Fail($"No handler for {ConfidentialityMode}");
            throw new CryptographicException();
        }
    }
}
