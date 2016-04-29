// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public CmsRecipient(SubjectIdentifierType recipientIdentifierType, X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

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

        public SubjectIdentifierType RecipientIdentifierType { get; }
        public X509Certificate2 Certificate { get; }
    }
}
