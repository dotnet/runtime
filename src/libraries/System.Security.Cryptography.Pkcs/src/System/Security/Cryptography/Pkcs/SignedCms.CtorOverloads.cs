// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class SignedCms
    {
        private static ContentInfo MakeEmptyContentInfo() =>
            new ContentInfo(Oids.Pkcs7DataOid.CopyOid(), Array.Empty<byte>());

        public SignedCms()
            : this(SubjectIdentifierType.IssuerAndSerialNumber, MakeEmptyContentInfo(), false)
        {
        }

        public SignedCms(SubjectIdentifierType signerIdentifierType)
            : this(signerIdentifierType, MakeEmptyContentInfo(), false)
        {
        }

        public SignedCms(ContentInfo contentInfo)
            : this(SubjectIdentifierType.IssuerAndSerialNumber, contentInfo, false)
        {
        }

        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo)
            : this(signerIdentifierType, contentInfo, false)
        {
        }

        public SignedCms(ContentInfo contentInfo, bool detached)
            : this(SubjectIdentifierType.IssuerAndSerialNumber, contentInfo, detached)
        {
        }
    }
}
