// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    /// <summary>
    ///   Represents information about a key encapsulation recipient.
    /// </summary>
    public sealed class KemRecipientInfo : RecipientInfo
    {
        private AlgorithmIdentifier? _lazyKeyDerivationAlgorithm;
        private AlgorithmIdentifier? _lazyKeyEncapsulationAlgorithm;
        private AlgorithmIdentifier? _lazyKeyEncryptionAlgorithm;
        private byte[]? _lazyEncryptedKey;
        private SubjectIdentifier? _lazyRecipientIdentifier;

        internal KemRecipientInfo(KemRecipientInfoPal pal)
            : base(RecipientInfoType.KeyEncapsulation, pal)
        {
        }

        /// <inheritdoc/>
        public override int Version => Pal.Version;

        /// <inheritdoc/>
        public override SubjectIdentifier RecipientIdentifier =>
            _lazyRecipientIdentifier ??= Pal.RecipientIdentifier;

        /// <inheritdoc/>
        public override AlgorithmIdentifier KeyEncryptionAlgorithm =>
            _lazyKeyEncryptionAlgorithm ??= Pal.KeyEncryptionAlgorithm;

        /// <inheritdoc/>
        public override byte[] EncryptedKey => _lazyEncryptedKey ??= Pal.EncryptedKey;

        /// <summary>
        ///   Gets the key encapsulation algorithm.
        /// </summary>
        /// <value>The key encapsulation algorithm.</value>
        public AlgorithmIdentifier KeyEncapsulationAlgorithm =>
            _lazyKeyEncapsulationAlgorithm ??= Pal.KeyEncapsulationAlgorithm;

        /// <summary>
        ///   Gets the key encapsulation ciphertext.
        /// </summary>
        /// <value>The key encapsulation ciphertext.</value>
        public ReadOnlyMemory<byte> KeyEncapsulationCiphertext => Pal.KeyEncapsulationCiphertext;

        /// <summary>
        ///   Gets the key derivation algorithm.
        /// </summary>
        /// <value>The key derivation algorithm.</value>
        public AlgorithmIdentifier KeyDerivationAlgorithm =>
            _lazyKeyDerivationAlgorithm ??= Pal.KeyDerivationAlgorithm;

        /// <summary>
        ///   Gets the key-encryption key length, in bytes.
        /// </summary>
        /// <value>The key-encryption key length, in bytes.</value>
        public int KeyEncryptionKeyLengthInBytes => Pal.KeyEncryptionKeyLengthInBytes;

        /// <summary>
        ///   Gets the optional user keying material.
        /// </summary>
        /// <value>
        ///   The user keying material, or <see langword="null"/> when the optional value is not present.
        /// </value>
        public ReadOnlyMemory<byte>? UserKeyingMaterial => Pal.UserKeyingMaterial;

        private new KemRecipientInfoPal Pal => (KemRecipientInfoPal)base.Pal;
    }
}
