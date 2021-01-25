// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override Exception CreateRecipientsNotFoundException()
        {
            return new CryptographicException(SR.Cryptography_Cms_RecipientNotFound);
        }

        public override Exception CreateRecipientInfosAfterEncryptException()
        {
            return CreateInvalidMessageTypeException();
        }

        public override Exception CreateDecryptAfterEncryptException()
        {
            return CreateInvalidMessageTypeException();
        }

        public override Exception CreateDecryptTwiceException()
        {
            return CreateInvalidMessageTypeException();
        }

        private static Exception CreateInvalidMessageTypeException()
        {
            // Windows CRYPT_E_INVALID_MSG_TYPE
            return new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
        }
    }
}
