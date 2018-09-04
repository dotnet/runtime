// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        private sealed class ManagedKeyAgreePal : KeyAgreeRecipientInfoPal
        {
            private readonly KeyAgreeRecipientInfoAsn _asn;
            private readonly int _index;

            internal ManagedKeyAgreePal(KeyAgreeRecipientInfoAsn asn, int index)
            {
                _asn = asn;
                _index = index;
            }

            public override byte[] EncryptedKey =>
                _asn.RecipientEncryptedKeys[_index].EncryptedKey.ToArray();

            public override AlgorithmIdentifier KeyEncryptionAlgorithm =>
                _asn.KeyEncryptionAlgorithm.ToPresentationObject();

            public override SubjectIdentifier RecipientIdentifier =>
                new SubjectIdentifier(
                    _asn.RecipientEncryptedKeys[_index].Rid.IssuerAndSerialNumber,
                    _asn.RecipientEncryptedKeys[_index].Rid.RKeyId?.SubjectKeyIdentifier);

            public override int Version => _asn.Version;

            public override DateTime Date
            {
                get
                {
                    KeyAgreeRecipientIdentifierAsn rid = _asn.RecipientEncryptedKeys[_index].Rid;

                    if (!rid.RKeyId.HasValue)
                    {
                        throw new InvalidOperationException(SR.Cryptography_Cms_Key_Agree_Date_Not_Available);
                    }

                    if (!rid.RKeyId.Value.Date.HasValue)
                    {
                        // Compatibility with Windows/NetFX.
                        return DateTime.FromFileTimeUtc(0);
                    }

                    return rid.RKeyId.Value.Date.Value.LocalDateTime;
                }
            }

            public override SubjectIdentifierOrKey OriginatorIdentifierOrKey =>
                _asn.Originator.ToSubjectIdentifierOrKey();

            public override CryptographicAttributeObject OtherKeyAttribute
            {
                get
                {
                    KeyAgreeRecipientIdentifierAsn rid = _asn.RecipientEncryptedKeys[_index].Rid;

                    if (!rid.RKeyId.HasValue)
                    {
                        // Yes, "date" (Windows compat)
                        throw new InvalidOperationException(SR.Cryptography_Cms_Key_Agree_Date_Not_Available);
                    }

                    if (!rid.RKeyId.Value.Other.HasValue)
                    {
                        return null;
                    }

                    Oid oid = new Oid(rid.RKeyId.Value.Other.Value.KeyAttrId);
                    byte[] rawData = Array.Empty<byte>();

                    if (rid.RKeyId.Value.Other.Value.KeyAttr != null)
                    {
                        rawData = rid.RKeyId.Value.Other.Value.KeyAttr.Value.ToArray();
                    }

                    Pkcs9AttributeObject pkcs9AttributeObject = new Pkcs9AttributeObject(oid, rawData);
                    AsnEncodedDataCollection values = new AsnEncodedDataCollection(pkcs9AttributeObject);
                    return new CryptographicAttributeObject(oid, values);
                }
            }
        }
    }
}
