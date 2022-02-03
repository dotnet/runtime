// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseTestHelpers
    {
        internal const int KnownHeaderAlg = 1;
        internal const int KnownHeaderCrit = 2;
        internal const int KnownHeaderContentType = 3;
        internal const int KnownHeaderKid = 4;
        internal const int KnownHeaderIV = 5;
        internal const int KnownHeaderPartialIV = 6;
        internal const int KnownHeaderCounterSignature = 7;
        internal static readonly byte[] s_sampleContent = Encoding.UTF8.GetBytes("This is the content.");
        internal const string ContentTypeDummyValue = "application/cose; cose-type=\"cose-sign1\"";

        public enum ECDsaAlgorithm
        {
            ES256 = -7,
            ES384 = -35,
            ES512 = -36
        }

        internal enum RSAAlgorithm
        {
            PS256 = -37,
            PS384 = -38,
            PS512 = -39
        }

        public enum ContentTestCase
        {
            Empty,
            Small,
            Large
        }

        internal static CoseHeaderMap GetHeaderMapWithAlgorithm(int algorithm)
        {
            var protectedHeaders = new CoseHeaderMap();
            protectedHeaders.SetValue(CoseHeaderLabel.Algorithm, algorithm);
            return protectedHeaders;
        }

        internal static CoseHeaderMap GetEmptyHeaderMap() => new CoseHeaderMap();

        internal static byte[] GetDummyContent(ContentTestCase @case)
        {
            return @case switch
            {
                ContentTestCase.Empty => Array.Empty<byte>(),
                ContentTestCase.Small => s_sampleContent,
                ContentTestCase.Large => Enumerable.Repeat(default(byte), 1024).ToArray(),
                _ => throw new InvalidOperationException(),
            };
        }

        internal static void AssertSign1Message(
            byte[] encodedMsg,
            int expectedAlg,
            byte[]? expectedContent,
            AsymmetricAlgorithm key,
            int expectedProtectedHeaders = 1,
            int expectedUnprotectedHeaders = 0)
        {
            Assert.NotNull(encodedMsg);
            var reader = new CborReader(encodedMsg);

            // Start
            Assert.Equal((CborTag)18, reader.ReadTag());
            Assert.Equal(4, reader.ReadStartArray());

            // Protected headers
            AssertSign1ProtectedHeaders(reader.ReadByteString(), expectedAlg, expectedProtectedHeaders);

            // Unprotected headers
            AssertSign1Headers(reader, expectedUnprotectedHeaders);

            // Content
            if (expectedContent != null)
            {
                AssertExtensions.SequenceEqual(expectedContent, reader.ReadByteString());
            }
            else
            {
                reader.ReadNull();
            }

            // Signature
            byte[] signatureBytes = reader.ReadByteString();
            Assert.Equal(GetSignatureSize(key), signatureBytes.Length);

            // End
            reader.ReadEndArray();
            Assert.Equal(0, reader.BytesRemaining);
        }

        internal static int GetSignatureSize(AsymmetricAlgorithm key)
        {
            int size = (key.KeySize + 7) / 8;

            if (key is ECDsa)
            {
                size *= 2;
            }

            return size;
        }

        private static void AssertSign1ProtectedHeaders(byte[] protectedHeadersBytes, int expectedAlg, int expectedProtectedHeaders)
        {
            var reader = new CborReader(protectedHeadersBytes);
            CoseHeaderMap protectedHeaders = AssertSign1Headers(reader, expectedProtectedHeaders);

            Assert.Equal(expectedAlg, protectedHeaders.GetValueAsInt32(CoseHeaderLabel.Algorithm));
            Assert.Equal(0, reader.BytesRemaining);
        }

        private static CoseHeaderMap AssertSign1Headers(CborReader reader, int expectedHeaders)
        {
            Assert.Equal(expectedHeaders, reader.ReadStartMap());
            CoseHeaderMap headers = new();
            for (int i = 0; i < expectedHeaders; i++)
            {
                CoseHeaderLabel label = reader.PeekState() switch
                {
                    CborReaderState.NegativeInteger or CborReaderState.UnsignedInteger => new CoseHeaderLabel(reader.ReadInt32()),
                    CborReaderState.TextString => new CoseHeaderLabel(reader.ReadTextString()),
                    _ => throw new InvalidOperationException()
                };

                headers.SetEncodedValue(label, reader.ReadEncodedValue().Span);
            }
            reader.ReadEndMap();
            return headers;
        }

        internal static readonly Dictionary<ECDsaAlgorithm, ECDsa> ECDsaKeys = CreateECDsaKeys(true);

        internal static readonly Dictionary<ECDsaAlgorithm, ECDsa> ECDsaKeysWithoutPrivateKey = CreateECDsaKeys(false);

        internal static readonly RSA RSAKey = CreateRSA(true);

        internal static readonly RSA RSAKeyWithoutPrivateKey = CreateRSA(false);

        internal static Dictionary<ECDsaAlgorithm, ECDsa> CreateECDsaKeys(bool includePrivateKey)
        {
            var dictionary = new Dictionary<ECDsaAlgorithm, ECDsa>();

            dictionary.Add(ECDsaAlgorithm.ES256,
                CreateECDsaKey("nistP256",
                "usWxHK2PmfnHKwXPS54m0kTcGJ90UiglWiGahtagnv8",
                "IBOL-C3BttVivg-lSreASjpkttcsz-1rb7btKLv8EX4",
                "V8kgd2ZBRuh2dgyVINBUqpPDr7BOMGcF22CQMIUHtNM",
                includePrivateKey));

            dictionary.Add(ECDsaAlgorithm.ES384,
                CreateECDsaKey("nistP384",
                "kTJyP2KSsBBhnb4kjWmMF7WHVsY55xUPgb7k64rDcjatChoZ1nvjKmYmPh5STRKc",
                "mM0weMVU2DKsYDxDJkEP9hZiRZtB8fPfXbzINZj_fF7YQRynNWedHEyzAJOX2e8s",
                "ok3Nq97AXlpEusO7jIy1FZATlBP9PNReMU7DWbkLQ5dU90snHuuHVDjEPmtV0fTo",
                includePrivateKey));

            dictionary.Add(ECDsaAlgorithm.ES512,
                CreateECDsaKey("nistP521",
                "AHKZLLOsCOzz5cY97ewNUajB957y-C-U88c3v13nmGZx6sYl_oJXu9A5RkTKqjqvjyekWF-7ytDyRXYgCF5cj0Kt",
                "AdymlHvOiLxXkEhayXQnNCvDX4h9htZaCJN34kfmC6pV5OhQHiraVySsUdaQkAgDPrwQrJmbnX9cwlGfP-HqHZR1",
                "AAhRON2r9cqXX1hg-RoI6R1tX5p2rUAYdmpHZoC1XNM56KtscrX6zbKipQrCW9CGZH3T4ubpnoTKLDYJ_fF3_rJt",
                includePrivateKey));

            return dictionary;

            static ECDsa CreateECDsaKey(string curveFriendlyName, string base64UrlQx, string base64UrlQy, string base64UrlPrivateKey, bool includePrivateKey)
            {
                ECParameters ecParams = new()
                {
                    Curve = ECCurve.CreateFromFriendlyName(curveFriendlyName),
                    Q = new ECPoint
                    {
                        X = Base64UrlEncoder.DecodeBytes(base64UrlQx),
                        Y = Base64UrlEncoder.DecodeBytes(base64UrlQy),
                    },
                    D = includePrivateKey ? Base64UrlEncoder.DecodeBytes(base64UrlPrivateKey) : null,
                };
                return ECDsa.Create(ecParams);
            }
        }

        private static RSA CreateRSA(bool includePrivateKey)
        {
            var rsaParameters = new RSAParameters
            {
                Modulus = ByteUtils.HexToByteArray("BC7E29D0DF7E20CC9DC8D509E0F68895922AF0EF452190D402C61B554334A7BF91C9A570240F994FAE1B69035BCFAD4F7E249EB26087C2665E7C958C967B1517413DC3F97A431691A5999B257CC6CD356BAD168D929B8BAE9020750E74CF60F6FD35D6BB3FC93FC28900478694F508B33E7C00E24F90EDF37457FC3E8EFCFD2F42306301A8205AB740515331D5C18F0C64D4A43BE52FC440400F6BFC558A6E32884C2AF56F29E5C52780CEA7285F5C057FC0DFDA232D0ADA681B01495D9D0E32196633588E289E59035FF664F056189F2F10FE05827B796C326E3E748FFA7C589ED273C9C43436CDDB4A6A22523EF8BCB2221615B799966F1ABA5BC84B7A27CF"),
                Exponent = ByteUtils.HexToByteArray("010001"),
                D = ByteUtils.HexToByteArray("0969FF04FCC1E1647C20402CF3F736D4CAE33F264C1C6EE3252CFCC77CDEF533D700570AC09A50D7646EDFB1F86A13BCABCF00BD659F27813D08843597271838BC46ED4743FE741D9BC38E0BF36D406981C7B81FCE54861CEBFB85AD23A8B4833C1BEE18C05E4E436A869636980646EECB839E4DAF434C9C6DFBF3A55CE1DB73E4902F89384BD6F9ECD3399FB1ED4B83F28D356C8E619F1F0DC96BBE8B75C1812CA58F360259EAEB1D17130C3C0A2715A99BE49898E871F6088A29570DC2FFA0CEFFFA27F1F055CBAABFD8894E0CC24F176E34EBAD32278A466F8A34A685ACC8207D9EC1FCBBD094996DC73C6305FCA31668BE57B1699D0BB456CC8871BFFBCD"),
                P = ByteUtils.HexToByteArray("F331593E147FD3A3235675F0D36A06E5426F7C5E78E49B2ACD3E268BA50E48ED2A52F3B4FA492D6BCF70EB3F915A716078A113652E3FA4C6D50AF8606C2D2C28ECAF083B712D6CEE1263C1205DA03BBBFA6F5C2D8B1A96194089CACB306C844A832E2B032B5F96A7EAB6CFE1107299013C8B0E9F089BBABBC504DD8BC138BA4B"),
                Q = ByteUtils.HexToByteArray("C66B5DDCAB7017E14083F2854F61997F35636C86F2F92B172D2555588EE1ED899BA6B6ADEC0A02024B2E78A91C891256A8571E0EFB3BAC3F41724DE036EC8FA0F93E2CFBDDA59C6FF1816EB3DC938D4E45912423F3F34B7E96C39E2E4D65A3DCD6DFD2B4EF527841001272F77855B6D75D40D54BB65BD1DF8538E96EC4DAD60D"),
                DP = ByteUtils.HexToByteArray("1F677CFDBE49EF7B7EA1B8A33BB9D260229F20F1562D373864BEA4DD9D97E5A4F2B53991624CB6D7D836DDBA1CBC102E0405D0EA5CF98CFEBC1E298AD20D5749859EE8B23C604053D1FE1DBF5F37C4DEF66D10FB349E5F49AD82DDB435719DF7BD4EE5F107D5D52FA3E8AD9983B538BAE72591E2C98ACAA75ABED1192DFF7457"),
                DQ = ByteUtils.HexToByteArray("2CCC9F13ACCD9146B57755318E3BBE197FA7642090097C162E86485FC75AF173E965D9C7290D1569092A83E9C2DC9BFC5EE3D490935EE4C41F75BC698C5D1B0CC059AE746B95F1DD408CF5BEBC65C038D4F23153C0C7C4DADF1569C890870B5958568ECF755D8C73389DF1C138353A242414F853B0E7C85A0C4D4E3F4949139D"),
                InverseQ = ByteUtils.HexToByteArray("7B6E2406FD03BC75EA22AB94A8D242506A6BBFE36BC8132DBBCE50B8425425062B697AFA180F5685E90E11EB5712D2E6E2B24E2A1E7C75D5940E08301E824470EF38561BE3E9D05F9FCA8E6F69A028A928E85E58212E789BA577B80378D7A995FA6AFEA74BE364661A679F82776C5905F43F7A35692986271E594E1D11F9668D"),
            };

            if (!includePrivateKey)
            {
                return RSA.Create(new RSAParameters { Modulus = rsaParameters.Modulus, Exponent = rsaParameters.Exponent });
            }

            return RSA.Create(rsaParameters);
        }
    }
}
