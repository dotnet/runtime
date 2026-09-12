// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class EnvelopedCms
    {
        /// <summary>
        ///   Decrypts the content using the specified recipient information and ML-KEM private key.
        /// </summary>
        /// <param name="recipientInfo">The recipient information that identifies the encrypted key.</param>
        /// <param name="privateKey">The private key to use for decapsulation.</param>
        public void Decrypt(KemRecipientInfo recipientInfo, MLKem privateKey)
        {
            ArgumentNullException.ThrowIfNull(recipientInfo);
            ArgumentNullException.ThrowIfNull(privateKey);

            DecryptWithKey(recipientInfo, privateKey);
        }

        /// <summary>
        ///   Decrypts the content using the specified recipient information and Composite ML-KEM private key.
        /// </summary>
        /// <param name="recipientInfo">The recipient information that identifies the encrypted key.</param>
        /// <param name="privateKey">The private key to use for decapsulation.</param>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public void Decrypt(KemRecipientInfo recipientInfo, CompositeMLKem privateKey)
        {
            ArgumentNullException.ThrowIfNull(recipientInfo);
            ArgumentNullException.ThrowIfNull(privateKey);

            throw new PlatformNotSupportedException(
                SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
        }
    }
}
