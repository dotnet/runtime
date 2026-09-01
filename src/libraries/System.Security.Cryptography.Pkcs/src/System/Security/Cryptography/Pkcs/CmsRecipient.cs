// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class CmsRecipient
    {
        public CmsRecipient(X509Certificate2 certificate)
            : this(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
        }

#if NETSTANDARD2_0
        internal
#else
        public
#endif
        CmsRecipient(X509Certificate2 certificate, RSAEncryptionPadding rsaEncryptionPadding)
            : this(certificate)
        {
            ArgumentNullException.ThrowIfNull(rsaEncryptionPadding);

            ValidateRSACertificate(certificate);
            RSAEncryptionPadding = rsaEncryptionPadding;
        }

#if NETSTANDARD2_0
        internal
#else
        public
#endif
        CmsRecipient(SubjectIdentifierType recipientIdentifierType, X509Certificate2 certificate, RSAEncryptionPadding rsaEncryptionPadding)
            : this(recipientIdentifierType, certificate)
        {
            ArgumentNullException.ThrowIfNull(rsaEncryptionPadding);

            ValidateRSACertificate(certificate);
            RSAEncryptionPadding = rsaEncryptionPadding;
        }

        public CmsRecipient(SubjectIdentifierType recipientIdentifierType, X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            switch (recipientIdentifierType)
            {
                case SubjectIdentifierType.Unknown:
                    recipientIdentifierType = SubjectIdentifierType.IssuerAndSerialNumber;
                    break;
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    break;
                case SubjectIdentifierType.SubjectKeyIdentifier:
                    break;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_Cms_Invalid_Subject_Identifier_Type, recipientIdentifierType));
            }

            RecipientIdentifierType = recipientIdentifierType;
            Certificate = certificate;
        }

#if NETSTANDARD2_0
        internal
#else
        public
#endif
        RSAEncryptionPadding? RSAEncryptionPadding { get; }
        public SubjectIdentifierType RecipientIdentifierType { get; }
        public X509Certificate2 Certificate { get; }

#if NET11_0_OR_GREATER
        internal bool IsKeyEncapsulation { get; private set; }
        internal ReadOnlyMemory<byte>? KeyEncapsulationUserKeyingMaterial { get; private set; }

        /// <summary>
        /// Creates a recipient that uses key encapsulation.
        /// </summary>
        /// <param name="certificate">The recipient certificate.</param>
        /// <param name="userKeyingMaterial">The optional user keying material.</param>
        /// <returns>A recipient that uses key encapsulation.</returns>
        public static CmsRecipient CreateForKeyEncapsulation(
            X509Certificate2 certificate,
            ReadOnlySpan<byte> userKeyingMaterial) =>
            new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, certificate, userKeyingMaterial);

        /// <summary>
        /// Creates a recipient that uses key encapsulation.
        /// </summary>
        /// <param name="recipientIdentifierType">
        /// One of the enumeration values that specifies how the recipient is identified.
        /// </param>
        /// <param name="certificate">The recipient certificate.</param>
        /// <param name="userKeyingMaterial">The optional user keying material.</param>
        /// <returns>A recipient that uses key encapsulation.</returns>
        public static CmsRecipient CreateForKeyEncapsulation(
            SubjectIdentifierType recipientIdentifierType,
            X509Certificate2 certificate,
            ReadOnlySpan<byte> userKeyingMaterial) =>
            new CmsRecipient(recipientIdentifierType, certificate, userKeyingMaterial);

        internal static bool IsMLKemAlgorithm(string? oid) =>
            oid is Oids.MlKem512 or
                Oids.MlKem768 or
                Oids.MlKem1024;

        internal static bool IsCompositeMLKemAlgorithm(string? oid) =>
            oid is Oids.MLKem768WithRsaOaep2048Sha3_256 or
                Oids.MLKem768WithRsaOaep3072Sha3_256 or
                Oids.MLKem768WithRsaOaep4096Sha3_256 or
                Oids.MLKem768WithX25519Sha3_256 or
                Oids.MLKem768WithECDiffieHellmanP256Sha3_256 or
                Oids.MLKem768WithECDiffieHellmanP384Sha3_256 or
                Oids.MLKem768WithECDiffieHellmanBrainpoolP256r1Sha3_256 or
                Oids.MLKem1024WithRsaOaep3072Sha3_256 or
                Oids.MLKem1024WithECDiffieHellmanP384Sha3_256 or
                Oids.MLKem1024WithECDiffieHellmanBrainpoolP384r1Sha3_256 or
                Oids.MLKem1024WithX448Sha3_256 or
                Oids.MLKem1024WithECDiffieHellmanP521Sha3_256;

        internal static bool IsKeyEncapsulationAlgorithm(string? oid) =>
            IsMLKemAlgorithm(oid) || IsCompositeMLKemAlgorithm(oid);

        private CmsRecipient(
            SubjectIdentifierType recipientIdentifierType,
            X509Certificate2 certificate,
            ReadOnlySpan<byte> userKeyingMaterial)
            : this(recipientIdentifierType, certificate)
        {
            string keyAlgorithm = certificate.GetKeyAlgorithm();

            if (!IsKeyEncapsulationAlgorithm(keyAlgorithm))
            {
                throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, keyAlgorithm);
            }

            IsKeyEncapsulation = true;
            KeyEncapsulationUserKeyingMaterial = userKeyingMaterial.ToArray();
        }
#endif

        private static void ValidateRSACertificate(X509Certificate2 certificate)
        {
            switch (certificate.GetKeyAlgorithm())
            {
                case Oids.Rsa:
                case Oids.RsaOaep:
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_Cms_Recipient_RSARequired_RSAPaddingModeSupplied);
            }
        }
    }
}
