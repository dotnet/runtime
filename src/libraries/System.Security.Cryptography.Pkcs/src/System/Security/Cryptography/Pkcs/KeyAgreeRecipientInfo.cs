// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                return _lazyRecipientIdentifier ?? (_lazyRecipientIdentifier = Pal.RecipientIdentifier);
            }
        }

        public override AlgorithmIdentifier KeyEncryptionAlgorithm
        {
            get
            {
                return _lazyKeyEncryptionAlgorithm ?? (_lazyKeyEncryptionAlgorithm = Pal.KeyEncryptionAlgorithm);
            }
        }

        public override byte[] EncryptedKey
        {
            get
            {
                return _lazyEncryptedKey ?? (_lazyEncryptedKey = Pal.EncryptedKey);
            }
        }

        public SubjectIdentifierOrKey OriginatorIdentifierOrKey
        {
            get
            {
                return _lazyOriginatorIdentifierKey ?? (_lazyOriginatorIdentifierKey = Pal.OriginatorIdentifierOrKey);
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

        public CryptographicAttributeObject OtherKeyAttribute
        {
            get
            {
                return _lazyOtherKeyAttribute ?? (_lazyOtherKeyAttribute = Pal.OtherKeyAttribute);
            }
        }

        private new KeyAgreeRecipientInfoPal Pal
        {
            get
            {
                return (KeyAgreeRecipientInfoPal)(base.Pal);
            }
        }

        private volatile SubjectIdentifier _lazyRecipientIdentifier = null;
        private volatile AlgorithmIdentifier _lazyKeyEncryptionAlgorithm = null;
        private volatile byte[] _lazyEncryptedKey = null;
        private volatile SubjectIdentifierOrKey _lazyOriginatorIdentifierKey = null;
        private DateTime? _lazyDate = default(DateTime?);
        private volatile CryptographicAttributeObject _lazyOtherKeyAttribute = null;
    }
}


