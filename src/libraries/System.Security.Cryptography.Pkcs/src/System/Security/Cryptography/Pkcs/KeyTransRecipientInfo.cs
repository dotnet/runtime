// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class KeyTransRecipientInfo : RecipientInfo
    {
        internal KeyTransRecipientInfo(KeyTransRecipientInfoPal pal)
            : base(RecipientInfoType.KeyTransport, pal)
        {
        }

        public override int Version
        {
            get
            {
                return Pal.Version;
            }
        }

        public override SubjectIdentifier RecipientIdentifier
        {
            get
            {
                return _lazyRecipientIdentifier ??= Pal.RecipientIdentifier;
            }
        }

        public override AlgorithmIdentifier KeyEncryptionAlgorithm
        {
            get
            {
                return _lazyKeyEncryptionAlgorithm ??= Pal.KeyEncryptionAlgorithm;
            }
        }

        public override byte[] EncryptedKey
        {
            get
            {
                return _lazyEncryptedKey ??= Pal.EncryptedKey;
            }
        }

        private new KeyTransRecipientInfoPal Pal
        {
            get
            {
                return (KeyTransRecipientInfoPal)(base.Pal);
            }
        }

        private volatile SubjectIdentifier? _lazyRecipientIdentifier;
        private volatile AlgorithmIdentifier? _lazyKeyEncryptionAlgorithm;
        private volatile byte[]? _lazyEncryptedKey;
    }
}
