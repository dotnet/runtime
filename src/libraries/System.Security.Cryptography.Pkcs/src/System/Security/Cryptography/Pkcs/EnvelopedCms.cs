// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class EnvelopedCms
    {
        //
        // Constructors
        //

        public EnvelopedCms()
            : this(new ContentInfo(Array.Empty<byte>()))
        {
        }

        public EnvelopedCms(ContentInfo contentInfo)
            : this(contentInfo, new AlgorithmIdentifier(Oids.Aes256CbcOid.CopyOid()))
        {
        }

        public EnvelopedCms(ContentInfo contentInfo, AlgorithmIdentifier encryptionAlgorithm)
        {
            if (contentInfo is null)
            {
                throw new ArgumentNullException(nameof(contentInfo));
            }
            if (encryptionAlgorithm is null)
            {
                throw new ArgumentNullException(nameof(encryptionAlgorithm));
            }

            Version = 0;  // It makes little sense to ask for a version before you've decoded, but since the .NET Framework returns 0 in that case, we will too.
            ContentInfo = contentInfo;
            ContentEncryptionAlgorithm = encryptionAlgorithm;
            Certificates = new X509Certificate2Collection();
            UnprotectedAttributes = new CryptographicAttributeObjectCollection();
            _decryptorPal = null;
            _lastCall = LastCall.Ctor;
        }

        //
        // Latched properties.
        //     - For senders, the caller sets their values and they act as implicit inputs to the Encrypt() method.
        //     - For recipients, the Decode() and Decrypt() methods reset their values and the caller can inspect them if desired.
        //

        public int Version { get; private set; }
        public ContentInfo ContentInfo { get; private set; }
        public AlgorithmIdentifier ContentEncryptionAlgorithm { get; private set; }
        public X509Certificate2Collection Certificates { get; private set; }
        public CryptographicAttributeObjectCollection UnprotectedAttributes { get; private set; }

        //
        // Recipients invoke this property to retrieve the recipient information after a Decode() operation.
        // Senders should not invoke this property.
        //
        public RecipientInfoCollection RecipientInfos
        {
            get
            {
                switch (_lastCall)
                {
                    case LastCall.Ctor:
                        return new RecipientInfoCollection();

                    case LastCall.Encrypt:
                        throw PkcsPal.Instance.CreateRecipientInfosAfterEncryptException();

                    case LastCall.Decode:
                    case LastCall.Decrypt:
                        Debug.Assert(_decryptorPal != null);
                        return _decryptorPal.RecipientInfos;

                    default:
                        Debug.Fail($"Unexpected _lastCall value: {_lastCall}");
                        throw new InvalidOperationException();
                }
            }
        }

        //
        // Encrypt() overloads. Senders invoke this to encrypt and encode a CMS. Afterward, invoke the Encode() method to retrieve the actual encoding.
        //
        public void Encrypt(CmsRecipient recipient)
        {
            if (recipient is null)
            {
                throw new ArgumentNullException(nameof(recipient));
            }

            Encrypt(new CmsRecipientCollection(recipient));
        }

        public void Encrypt(CmsRecipientCollection recipients)
        {
            if (recipients is null)
            {
                throw new ArgumentNullException(nameof(recipients));
            }

            // .NET Framework compat note: Unlike the desktop, we don't provide a free UI to select the recipient. The app must give it to us programmatically.
            if (recipients.Count == 0)
                throw new PlatformNotSupportedException(SR.Cryptography_Cms_NoRecipients);

            if (_decryptorPal != null)
            {
                _decryptorPal.Dispose();
                _decryptorPal = null;
            }
            _encodedMessage = PkcsPal.Instance.Encrypt(recipients, ContentInfo, ContentEncryptionAlgorithm, Certificates, UnprotectedAttributes);
            _lastCall = LastCall.Encrypt;
        }

        //
        // Senders invoke Encode() after a successful Encrypt() to retrieve the RFC-compliant on-the-wire representation.
        //
        public byte[] Encode()
        {
            if (_encodedMessage == null)
                throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotEncrypted);

            return _encodedMessage.CloneByteArray();
        }

        //
        // Recipients invoke Decode() to turn the on-the-wire representation into a usable EnvelopedCms instance. Next step is to call Decrypt().
        //
        public void Decode(byte[] encodedMessage)
        {
            if (encodedMessage is null)
            {
                throw new ArgumentNullException(nameof(encodedMessage));
            }

            Decode(new ReadOnlySpan<byte>(encodedMessage));
        }

        /// <summary>
        ///   Decodes the provided data as a CMS/PKCS#7 EnvelopedData message.
        /// </summary>
        /// <param name="encodedMessage">
        ///   The data to decode.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   The <paramref name="encodedMessage"/> parameter was not successfully decoded.
        /// </exception>
        public void Decode(ReadOnlySpan<byte> encodedMessage)
        {
            if (_decryptorPal != null)
            {
                _decryptorPal.Dispose();
                _decryptorPal = null;
            }

            int version;
            ContentInfo contentInfo;
            AlgorithmIdentifier contentEncryptionAlgorithm;
            X509Certificate2Collection originatorCerts;
            CryptographicAttributeObjectCollection unprotectedAttributes;
            _decryptorPal = PkcsPal.Instance.Decode(encodedMessage, out version, out contentInfo, out contentEncryptionAlgorithm, out originatorCerts, out unprotectedAttributes);
            Version = version;
            ContentInfo = contentInfo;
            ContentEncryptionAlgorithm = contentEncryptionAlgorithm;
            Certificates = originatorCerts;
            UnprotectedAttributes = unprotectedAttributes;

            // .NET Framework compat: Encode() after a Decode() returns you the same thing that ContentInfo.Content does.
            _encodedMessage = contentInfo.Content.CloneByteArray();

            _lastCall = LastCall.Decode;
        }

        //
        // Decrypt() overloads: Recipients invoke Decrypt() after a successful Decode(). Afterwards, invoke ContentInfo to get the decrypted content.
        //
        public void Decrypt()
        {
            DecryptContent(RecipientInfos, null);
        }

        public void Decrypt(RecipientInfo recipientInfo)
        {
            if (recipientInfo is null)
            {
                throw new ArgumentNullException(nameof(recipientInfo));
            }

            DecryptContent(new RecipientInfoCollection(recipientInfo), null);
        }

        public void Decrypt(RecipientInfo recipientInfo, X509Certificate2Collection extraStore)
        {
            if (recipientInfo is null)
            {
                throw new ArgumentNullException(nameof(recipientInfo));
            }
            if (extraStore is null)
            {
                throw new ArgumentNullException(nameof(extraStore));
            }

            DecryptContent(new RecipientInfoCollection(recipientInfo), extraStore);
        }

        public void Decrypt(X509Certificate2Collection extraStore)
        {
            if (extraStore is null)
            {
                throw new ArgumentNullException(nameof(extraStore));
            }

            DecryptContent(RecipientInfos, extraStore);
        }

        public void Decrypt(RecipientInfo recipientInfo, AsymmetricAlgorithm? privateKey)
        {
            if (recipientInfo is null)
            {
                throw new ArgumentNullException(nameof(recipientInfo));
            }

            CheckStateForDecryption();

            X509Certificate2Collection extraStore = new X509Certificate2Collection();
            ContentInfo? contentInfo = _decryptorPal!.TryDecrypt(
                recipientInfo,
                null,
                privateKey,
                Certificates,
                extraStore,
                out Exception? exception);

            if (exception != null)
                throw exception;

            SetContentInfo(contentInfo!);
        }

        private void DecryptContent(RecipientInfoCollection recipientInfos, X509Certificate2Collection? extraStore)
        {
            CheckStateForDecryption();
            extraStore ??= new X509Certificate2Collection();

            X509Certificate2Collection certs = new X509Certificate2Collection();
            PkcsPal.Instance.AddCertsFromStoreForDecryption(certs);
            certs.AddRange(extraStore);

            X509Certificate2Collection originatorCerts = Certificates;

            ContentInfo? newContentInfo = null;
            Exception? exception = PkcsPal.Instance.CreateRecipientsNotFoundException();
            foreach (RecipientInfo recipientInfo in recipientInfos)
            {
                X509Certificate2? cert = certs.TryFindMatchingCertificate(recipientInfo.RecipientIdentifier);
                if (cert == null)
                {
                    exception = PkcsPal.Instance.CreateRecipientsNotFoundException();
                    continue;
                }

                newContentInfo = _decryptorPal!.TryDecrypt(
                    recipientInfo,
                    cert,
                    null,
                    originatorCerts,
                    extraStore,
                    out exception);

                if (exception != null)
                    continue;

                break;
            }

            if (exception != null)
                throw exception;

            SetContentInfo(newContentInfo!);
        }

        private void CheckStateForDecryption()
        {
            switch (_lastCall)
            {
                case LastCall.Ctor:
                    throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotEncrypted);

                case LastCall.Encrypt:
                    throw PkcsPal.Instance.CreateDecryptAfterEncryptException();

                case LastCall.Decrypt:
                    throw PkcsPal.Instance.CreateDecryptTwiceException();

                case LastCall.Decode:
                    break; // This is the expected state.

                default:
                    Debug.Fail($"Unexpected _lastCall value: {_lastCall}");
                    throw new InvalidOperationException();
            }
        }

        private void SetContentInfo(ContentInfo contentInfo)
        {
            ContentInfo = contentInfo;

            // .NET Framework compat: Encode() after a Decrypt() returns you the same thing that ContentInfo.Content does.
            _encodedMessage = contentInfo.Content.CloneByteArray();

            _lastCall = LastCall.Decrypt;
        }

        //
        // Instance fields
        //

        private DecryptorPal? _decryptorPal;
        private byte[]? _encodedMessage;
        private LastCall _lastCall;

        private enum LastCall
        {
            Ctor = 1,
            Encrypt = 2,
            Decode = 3,
            Decrypt = 4,
        }
    }
}
