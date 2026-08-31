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

        internal static readonly byte[] MlKem768HkdfSha3_384 = Convert.FromBase64String(
            """
            MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjER
            MA8GA1UEAxMITEFNUFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBEBUusDZ1LK5aav1+chVh5q4AZWM
            9unnLwK5qjwK/goc6O3q5EBGAUQcxTwixLFOWgCrqRo01fZ8TGpBdBN9EfttKQQ7JwUgVVGlUgMLmcGc+CO0Wxn1wyJY4moiRQk6
            xwy3ORMSj9TXluZNM7FRZOF70xx14fmvr9o7SlwOlBUfCxmwJR/+cLmz33mnopHf5mEeNGJ2YEzPitZQ2ns1vOFqmXOWEn+jkoWp
            W6mi1UxizCfElF//Vh6Y07mSU8LZVmvEq9oi/29n0P7OSAh/NLbKqBT/ZqeW6TnYd8oifNzj6du+fkoK0o2CmLY5DgdK3A0C16b8
            oyJi9rJ4ZOpW1mQ9VzNN5lOyJv68tR3piOTZhJxwE0iYaYUqXhDk0N5Kyu8xe9gTx5+XTrEMIKDv/CcsBURPEdNGWUN3N1X8ziaq
            WtUy8iHJUnvPH7faAKOwbqZb/VTVEMaGE1qzRNWaVpCOjnG4naUj/1T5vbv3dZzBqhiYWRFNYZf0uWw3ZaDDZ8NKO+uHH5ax1HcN
            B2XI+SysNms/n/15C/rnfz/ebviGJwLRIHcOsYg3xL/3D+9QrGMvpTVmmEzEUZv+skmBdqrzkijK+w08dxSA0jehT69CGr94xIBh
            lTmEe+IU97oTrP0mlzRgRGoYtoqjc/Q96+W9u+RSSUzImwqUS/sUUZmJRt4QYr/WTypwpGvmXZLVf1va4E81Put+EZ+8RZQ/lqAV
            tV0Bd4nscanbEJAQ2zh2QYylk7BOuEunndn5ey1KClxanQqBNqT6Hw8oK+4EcDDbfes8a86oqGAa8sUXT3hoY929FwJNvDl2gNxt
            1Xa4YU3ZuQy5ChvK0HZwZMeN09W5mj/NMrqAXyW6qTFASilaUTkZJ/TL31xqyOF5bo0Og8UHgkC50SUWZf+H5VsnoFIOTYwLqTnD
            wlVY37+LPhLbwVmFjD2q9Mcv4Iu0767Dl0FrbG4Tdrpqm7YjnCmxZGTyX/MjyMbiJGN8nJCyK40uUac40+/GcTaQZKDGjtOh66Gl
            tq1dFZuKxEs+f3Md5Weh5tpr602guHvYVijfb/gVBBPR3P/F74PAgDOCq3vbONA2V4tVBnzqQK4DxMscAQrD42q/GXmjh1aI2ymF
            S4IMmdkHotRSlU+R11NsH+U0kZoN1CynQR8ZMQRC4KBwL5AOnvzzxnlosRkUo8ZguX4Kc3sIVD/9eHp1iauuF7mjVmzdqjRnIzkG
            84SglkwafxrFDiOiDCVrLN5gyTDfM6DL5cZeRGR66lJdTOue8tTXGWzcrpw1HGfclcnc2d+FWCnnCEhIZVz8ll7uPP1gE+zWUP+f
            F+rd0nfIjWjIa0FRnmKWVNua4uSH7j0SV4wbA6OSwNjE7SuVnqDMZdyHKAryi8tI/kZhfESD9DBDToLWJnmmCahr2JVSJ2GbBXZq
            NqX2SK7bGzDk9zbK1pD4GdPbsjANBgsqhkiG9w0BCRADHAIBIDALBglghkgBZQMEAS0EKIV1CiYS1ilPY9ViYl12hbTik0f840dz
            4FUVDIK6qXJMevQWWl+CP6QwPAYJKoZIhvcNAQcBMB0GCWCGSAFlAwQBKgQQ0NoOCI+HY7B2CLV5cOOcVIAQNyxWNgIKUvs1xKFS
            lmLmag==
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
