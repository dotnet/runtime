// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class KeyAgreeRecipientInfo : RecipientInfo
    {
        internal KeyAgreeRecipientInfo(KeyAgreeRecipientInfoPal pal)
            : base(RecipientInfoType.KeyAgreement, pal)
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

        public SubjectIdentifierOrKey OriginatorIdentifierOrKey
        {
            get
            {
                return _lazyOriginatorIdentifierKey ??= Pal.OriginatorIdentifierOrKey;
            }
        }

        public DateTime Date
        {
            get
            {
                if (!_lazyDate.HasValue)
                {
                    _lazyDate = Pal.Date;
                    Interlocked.MemoryBarrier();
                }
                return _lazyDate.Value;
            }
        }

        public CryptographicAttributeObject? OtherKeyAttribute
        {
            get
            {
                return _lazyOtherKeyAttribute ??= Pal.OtherKeyAttribute;
            }
        }

        private new KeyAgreeRecipientInfoPal Pal
        {
            get
            {
                return (KeyAgreeRecipientInfoPal)(base.Pal);
            }
        }

        private volatile SubjectIdentifier? _lazyRecipientIdentifier;
        private volatile AlgorithmIdentifier? _lazyKeyEncryptionAlgorithm;
        private volatile byte[]? _lazyEncryptedKey;
        private volatile SubjectIdentifierOrKey? _lazyOriginatorIdentifierKey;
        private DateTime? _lazyDate;
        private volatile CryptographicAttributeObject? _lazyOtherKeyAttribute;
    }
}
