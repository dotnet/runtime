// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Formats.Asn1;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    internal static class KemTestDocuments
    {
        internal static readonly byte[] MlKem768 = Convert.FromBase64String(
            """
            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
            AzYr7w==
            """);

        internal static byte[] UnsupportedOtherRecipientInfo { get; } = BuildUnsupportedOtherRecipientInfo();

        private static byte[] BuildUnsupportedOtherRecipientInfo()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Asn1Tag context0 = new Asn1Tag(TagClass.ContextSpecific, 0);
            Asn1Tag context4 = new Asn1Tag(TagClass.ContextSpecific, 4);

            writer.PushSequence();
            writer.WriteObjectIdentifier(TestOids.Pkcs7Enveloped);
            writer.PushSequence(context0);
            writer.PushSequence();
            writer.WriteInteger(3);
            writer.PushSetOf();
            writer.PushSequence(context4);
            writer.WriteObjectIdentifier("1.2.3.4");
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, "other unsupported recipient type");
            writer.PopSequence(context4);
            writer.PopSetOf();
            writer.PushSequence();
            writer.WriteObjectIdentifier(TestOids.Pkcs7Data);
            writer.PushSequence();
            writer.WriteObjectIdentifier(TestOids.Aes256);
            writer.WriteOctetString(new byte[16]);
            writer.PopSequence();
            writer.PopSequence();
            writer.PopSequence();
            writer.PopSequence(context0);
            writer.PopSequence();

            return writer.Encode();
        }
    }
}
