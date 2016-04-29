// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private new KeyTransRecipientInfoPal Pal
        {
            get
            {
                return (KeyTransRecipientInfoPal)(base.Pal);
            }
        }

        private volatile SubjectIdentifier _lazyRecipientIdentifier = null;
        private volatile AlgorithmIdentifier _lazyKeyEncryptionAlgorithm = null;
        private volatile byte[] _lazyEncryptedKey = null;
    }
}


