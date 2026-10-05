// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Formats.Asn1;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    internal static class KemTestDocuments
    {
        // ML-KEM-768, AES-256-KW, SHA-384
        internal static readonly byte[] MlKem768 = Convert.FromBase64String(
            """
            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
            AzYr7w==
            """);

        // ML-KEM-768, AES-256-KW, SHA-384, empty UKM
        internal static readonly byte[] MlKem768EmptyUkm = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-384, UKM 01 02 03 04 05 06 07 08
        internal static readonly byte[] MlKem768NonEmptyUkm = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-3-384
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

        // ML-KEM-512, AES-256-KW, SHA-384
        internal static readonly byte[] MlKem512 = Convert.FromBase64String(
            """
            MIIEBgYJKoZIhvcNAQcDoIID9zCCA/MCAQMxggOupIIDqgYLKoZIhvcNAQkQDQMwggOZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
            UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAQSCAwAgJsff2m/wlp5YsRBMevkZRPaEmT7sP/z6EniNEBVBgCPlIpPFxJllmTBR
            s3sbX6BP/jCCoY2FPWNeTxFr7F+geMlyWhZsTCuxV3ynLISQAbxhZzKWOzNohbtnKoVfdIK+J7TLSYoUWaGFKkPucDwYM7eBTEuQN7HbG0DMC94qiL9e
            z3hsqagwy+ISFEs0PaP77r2WDejSps3zAJjKgtxxzJpkkcShoNmVYZMy4sOljW/fHl26RFo1O3A+dt4H0ePI7ixFzew/As6hnKyM0exL5DEz170QNp2D
            vvHefqp77db2A/V4JmEAFvBIgtjKpO8KLriPO66pDcROjht+LHElcRAbbsn8YDZuIRBxqMgIgtsPntFODuVtXVgCRNbqSTBNiJ3tWoAXTdKQXBHwZQhO
            tGz11IG3e5Wo3jiicTv/29AdAmnyB+GnitO/vfF2TmHNfGpzpv+XUNTd2PzfQ2kWmHj+w/b3huW2KtwFkPQkkMyhsgbss/BNkVt/4vgAZi5TgaZUwIxI
            1OzAE0kJzzkj+xvRdyhehTtin0La8nQsZ6AGqDhRTMCSV1LWHSxeL/QvcJ9U+azZWCkz+tndORHS6+RVTa89hKYxsChChx0LSHYhUv0YHsfLD745Isq+
            cDPRhETTtGkxyEPRVf0+0r9EghtCzenSNIG7nQ2FLPWV0ZaBbnrAq+yuIw6yLkl8mZ8Mg/bX7Rp1ii30mQT6Ak1mNdKdLNMUiBcDeao4kikTVhld+eMs
            oq45hsHhilyMHPYGIOLa3FeUj0y3UIBmgLyEivbUNmkpNe2F0WhjvW06kjx+7PyHvUcoDDRaEwdOgkcmYCmcSgNfEHnFcELhBYE/Ih82nATClAaGbu6I
            0Rb2bbiiXhuD/lu6E0M2IdPKr8+cUSb5qGQKXZ2UZhSrWPkk3nzKYXjxjz5g9IIoOv6+jjQXPRaZzobnCIbolRQg+m2BxF9p6mLYKhvjaRh49665FITd
            hOBG/U2HywWyzHKb7HbJ7qfAExANY5EAbp+ZeiUwDQYLKoZIhvcNAQkQAx0CASAwCwYJYIZIAWUDBAEtBChIJ3h/kLhBbEA35r1klvCT+BeKk3qsqAGt
            WKGrgwOogcNQiLBhjHPgMDwGCSqGSIb3DQEHATAdBglghkgBZQMEASoEEMvgf+A0cmIqZqQB0Zh11eCAEEJd3TxgXLtl4mmIGuBQ8ZY=
            """);

        // ML-KEM-1024, AES-256-KW, SHA-384
        internal static readonly byte[] MlKem1024 = Convert.FromBase64String(
            """
            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
            Lzi28I40hA==
            """);

        // ML-KEM-768, AES-128-KW, SHA-384
        internal static readonly byte[] MlKem768Aes128Wrap = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-192-KW, SHA-384
        internal static readonly byte[] MlKem768Aes192Wrap = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-256
        internal static readonly byte[] MlKem768HkdfSha256 = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-512
        internal static readonly byte[] MlKem768HkdfSha512 = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-3-256
        internal static readonly byte[] MlKem768HkdfSha3_256 = Convert.FromBase64String(
            """
            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
            """);

        // ML-KEM-768, AES-256-KW, SHA-3-512
        internal static readonly byte[] MlKem768HkdfSha3_512 = Convert.FromBase64String(
            """
            MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
            UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBEBET4kSY815ShaPBsu3iBqSzjcUhPDUSwNKdjKF9hw2p6vVknIh/8i+5eCX
            kFBUgNdV4tONxfSKOVbUOqkALkKlUTv6GUCOAPA2/QSScP8lU6YaytdoTgqWS2qHsI3QM7xLGDBG/2XIpw4D/PF2iLPXcxspT2NrMfLPr7QtGkCmLew9
            RrkQv2gppehAfFSOj6dvtWry3K17sfyUYpMPCc9CaPeKAkRKYp9mnZwUqpI7SiRq+tY9ARObOmybLcrb4GQ5+8uxigN457Tm8FVThT2rVwdpI5m9cQsJ
            7HgqMKGlsf/xkwEp10Y8nfuQSx3IV2LyL1N0jBwpDPwBKNaXTL1mQIHX+HfjZE0w4JM5MOm8iURH6DSyQ+I7mKwdU2IFLW1HX5NFM0MkACnX1qWEvDTE
            2AxGvAOnS495NOypZ3aPXfl6+psWjm1pSJFuYEcpSHokKtRxvHmMCU7pcs7vwztKD5UQyeQqtNXwRTb6qjBLu6/HGTB9qWofmWHmiJm95SmPobezM9Xr
            F7orOm2V+6LjFYZkpP2YeLmyO3ZRwUFLMT3SQ3yyUmZ26P8KBy5uIFBYCm5dsLQ5mQyDcmKrqO5X+qM9K+0Q9iVkH756pXwEmokkuzDkoaSubLpLB23i
            YuMZQvq3etvOa+0HdG+zkBtmwYVnwPmMf41oAI9zheW1QXTr7dq2URCzRRfpNSol0Dwrsu790SnUhbqtc1k0HYwaHMtntVYejtnxwnbZ9vy3xVA3/ZWu
            NC/8O1vrQXps6EGLcQCgFWv+DR2nHdq2w9M1tntoNcnK2FVGHaL82pfChjR5YjWbszLUs6d1EDd/7eEm879CPGTJBwY6qEIsnbJ/mB2EEHDOv+NrbwbD
            0AaxbHTui7FYF1uw6fJuM76/wDT1yHsXSnQVnwLYmDEwYFF6UZ+qCnbobY6VGs2HyKaVQeinPotkjXuqacKquPEBGiXNSFB7LmVhKseTbBSdqZZM/FYR
            rhcJ1ThgaWKNJw5+cBRwW2euqdzkExj62G/Sz23gxdPvKt20PqhmnA06HbvA+DqdRc5Cc3yeCherx0kqrZac0Qgs/GKchq+D1z3SpWW3pm0Jk4m446kN
            qToUkWmr+/7GYqSxSqx8oOBiA+PHhn31ZylhzOHuCYlC5RXCX/WyWjRupyktpqaTQ9AQAdfycIBGoBGksj+gmeap8/GP6DXDpOfY5KiHiMqydBIkMkpV
            vetsl8TmMkVzEflvTvaMdZz+qY8t+MSidD+u/yqLQ/jTm1DvDr3zdKv8lwScr09GU1m0Bt9k9Pi2rmMgC6jHWbu3wSYw7RR1z7BAgF6S83DbjYENrqpm
            7qzqK0C1hXKmyXMta8ciadI0RSe4u+5KlJSEsaz85WIxtmGav8Mjz7oD+Bq4gp2YEVLyNVaGSgD0f2JxoZ2AZeunwCPq7xg6G3A0V74T9MR7ZKudyoD2
            wTANBgsqhkiG9w0BCRADHAIBIDALBglghkgBZQMEAS0EKBq1zc/H45zTXh0sCJhHGEYMG5mtQJ2pGTwyI79napyWuuBiAGpqp08wPAYJKoZIhvcNAQcB
            MB0GCWCGSAFlAwQBKgQQ7OveAaBF3gHrD+N/WR3pD4AQiwLi7EFBmT8LZqhILJQmmQ==
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
