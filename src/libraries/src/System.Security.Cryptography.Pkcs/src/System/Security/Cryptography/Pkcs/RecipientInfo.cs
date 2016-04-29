// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public abstract class RecipientInfo
    {
        internal RecipientInfo(RecipientInfoType type, RecipientInfoPal pal)
        {
#if DEBUG
            switch (type)
            {
                case RecipientInfoType.KeyTransport:
                    Debug.Assert(pal is KeyTransRecipientInfoPal);
                    break;

                case RecipientInfoType.KeyAgreement:
                    Debug.Assert(pal is KeyAgreeRecipientInfoPal);
                    break;

                default:
                    Debug.Fail($"Illegal recipientInfoType: {type}");
                    break;
            }
#endif

            Type = type;
            Pal = pal;
        }

        public RecipientInfoType Type { get; }

        public abstract int Version { get; }

        public abstract SubjectIdentifier RecipientIdentifier { get; }

        public abstract AlgorithmIdentifier KeyEncryptionAlgorithm { get; }

        public abstract byte[] EncryptedKey { get; }

        internal RecipientInfoPal Pal { get; }
    }
}


