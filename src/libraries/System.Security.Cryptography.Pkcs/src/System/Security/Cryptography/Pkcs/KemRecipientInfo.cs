// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    /// <summary>
    /// Represents information about a key encapsulation recipient.
    /// </summary>
    public sealed class KemRecipientInfo : RecipientInfo
    {
        internal KemRecipientInfo()
            : base(RecipientInfoType.KeyEncapsulation, GetPal())
        {
        }

        /// <inheritdoc/>
        public override int Version => throw new NotImplementedException();

        /// <inheritdoc/>
        public override SubjectIdentifier RecipientIdentifier => throw new NotImplementedException();

        /// <inheritdoc/>
        public override AlgorithmIdentifier KeyEncryptionAlgorithm => throw new NotImplementedException();

        /// <inheritdoc/>
        public override byte[] EncryptedKey => throw new NotImplementedException();

        /// <summary>
        /// Gets the key encapsulation algorithm.
        /// </summary>
        /// <value>The key encapsulation algorithm.</value>
        public AlgorithmIdentifier KeyEncapsulationAlgorithm => throw new NotImplementedException();

        /// <summary>
        /// Gets the key encapsulation ciphertext.
        /// </summary>
        /// <value>The key encapsulation ciphertext.</value>
        public ReadOnlyMemory<byte> KeyEncapsulationCiphertext => throw new NotImplementedException();

        /// <summary>
        /// Gets the key derivation algorithm.
        /// </summary>
        /// <value>The key derivation algorithm.</value>
        public AlgorithmIdentifier KeyDerivationAlgorithm => throw new NotImplementedException();

        /// <summary>
        /// Gets the key-encryption key length, in bytes.
        /// </summary>
        /// <value>The key-encryption key length, in bytes.</value>
        public int KeyEncryptionKeyLengthInBytes => throw new NotImplementedException();

        /// <summary>
        /// Gets the optional user keying material.
        /// </summary>
        /// <value>
        /// The user keying material, or <see langword="null"/> when the optional value is not present.
        /// </value>
        public ReadOnlyMemory<byte>? UserKeyingMaterial => throw new NotImplementedException();

        private static RecipientInfoPal GetPal() => throw new NotImplementedException();
    }
}
