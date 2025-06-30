// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Tests;
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
        internal static readonly byte[] s_sampleContent = "This is the content."u8.ToArray();
        internal const string ContentTypeDummyValue = "application/cose; cose-type=\"cose-sign1\"";

        internal const string NullCborHex = "F6";
        internal const string SampleContentByteStringCborHex = "54546869732069732074686520636F6E74656E742E";

        public enum CoseAlgorithm
        {
            ES256 = -7,
            ES384 = -35,
            ES512 = -36,
            PS256 = -37,
            PS384 = -38,
            PS512 = -39,
            RS256 = -257,
            RS384 = -258,
            RS512 = -259,
            MLDsa44 = -48,
            MLDsa65 = -49,
            MLDsa87 = -50,
        }

        public enum ContentTestCase
        {
            Empty,
            Small,
            Large
        }

        internal static string GetExpectedAlgorithmStringValue(CoseAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CoseAlgorithm.ES256:
                case CoseAlgorithm.ES384:
                case CoseAlgorithm.ES512:
                case CoseAlgorithm.PS256:
                case CoseAlgorithm.PS384:
                case CoseAlgorithm.PS512:
                case CoseAlgorithm.RS256:
                case CoseAlgorithm.RS384:
                case CoseAlgorithm.RS512:
                    return algorithm.ToString();
                case CoseAlgorithm.MLDsa44:
                    return "ML-DSA-44";
                case CoseAlgorithm.MLDsa65:
                    return "ML-DSA-65";
                case CoseAlgorithm.MLDsa87:
                    return "ML-DSA-87";
                default:
                    throw new NotImplementedException($"Unhandled algorithm: {algorithm}");
            }
        }

        internal static CoseAlgorithm ParseAlgorithmStringValue(string value)
        {
            if (Enum.TryParse(value, out CoseAlgorithm alg))
                return alg;

            return value switch
            {
                "ML-DSA-44" => CoseAlgorithm.MLDsa44,
                "ML-DSA-65" => CoseAlgorithm.MLDsa65,
                "ML-DSA-87" => CoseAlgorithm.MLDsa87,
                _ => throw new NotImplementedException($"Unhandled algorithm: {value}")
            };
        }

        internal static CoseHeaderMap GetHeaderMapWithAlgorithm(CoseAlgorithm algorithm = CoseAlgorithm.ES256)
        {
            var protectedHeaders = new CoseHeaderMap();
            protectedHeaders.Add(CoseHeaderLabel.Algorithm, (int)algorithm);
            return protectedHeaders;
        }

        internal static CoseHeaderMap GetEmptyHeaderMap() => new CoseHeaderMap();

        internal static List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> GetExpectedProtectedHeaders(CoseAlgorithm algorithm)
        {
            var l = new List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>();
            AddEncoded(l, CoseHeaderLabel.Algorithm, (int)algorithm);

            return l;
        }

        internal static List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> GetEmptyExpectedHeaders() => new();

        internal static void AddEncoded(List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> list, CoseHeaderLabel label, int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);
            list.Add((label, writer.Encode()));
        }

        internal static void AddEncoded(List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> list, CoseHeaderLabel label, string value)
        {
            var writer = new CborWriter();
            writer.WriteTextString(value);
            list.Add((label, writer.Encode()));
        }

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

        internal static void AssertSign1MessageCore(
            ReadOnlySpan<byte> encodedMsg,
            ReadOnlySpan<byte> expectedContent,
            IDisposable signingKey,
            CoseAlgorithm algorithm,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedUnprotectedHeaders = null,
            bool expectedDetachedContent = false)
        {
            var reader = new CborReader(encodedMsg.ToArray());

            // Start
            Assert.Equal((CborTag)18, reader.ReadTag());
            Assert.Equal(4, reader.ReadStartArray());

            // Protected headers
            byte[] rawProtectedHeaders = reader.ReadByteString();
            AssertProtectedHeaders(rawProtectedHeaders, expectedProtectedHeaders ?? GetExpectedProtectedHeaders(algorithm));

            // Unprotected headers
            AssertHeaders(reader, expectedUnprotectedHeaders ?? GetEmptyExpectedHeaders());

            // Content
            if (expectedDetachedContent)
            {
                reader.ReadNull();
            }
            else
            {
                AssertExtensions.SequenceEqual(expectedContent, reader.ReadByteString());
            }

            // Signature
            byte[] signatureBytes = reader.ReadByteString();
            Assert.Equal(GetSignatureSize(signingKey), signatureBytes.Length);

            // End
            reader.ReadEndArray();
            Assert.Equal(0, reader.BytesRemaining);

            // Verify
            CoseSign1Message msg = CoseMessage.DecodeSign1(encodedMsg);
            if (signingKey is AsymmetricAlgorithm signingKeyAsymmetricAlgorithm)
            {
                if (expectedDetachedContent)
                {
                    Assert.True(msg.VerifyDetached(signingKeyAsymmetricAlgorithm, expectedContent), "msg.Verify(AsymmetricAlgorithm, content)");
                }
                else
                {
                    Assert.True(msg.VerifyEmbedded(signingKeyAsymmetricAlgorithm), "msg.Verify(AsymmetricAlgorithm)");
                }
            }

            CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, signingKey);
            if (expectedDetachedContent)
            {
                Assert.True(msg.VerifyDetached(coseKey, expectedContent), "msg.Verify(CoseKey, content)");
            }
            else
            {
                Assert.True(msg.VerifyEmbedded(coseKey), "msg.Verify(CoseKey)");
            }

            // Raw Protected Headers
            AssertExtensions.SequenceEqual(rawProtectedHeaders, msg.RawProtectedHeaders.Span);

            // Signature
            AssertExtensions.SequenceEqual(signatureBytes, msg.Signature.Span);

            // GetEncodedLength
            Assert.Equal(encodedMsg.Length, msg.GetEncodedLength());

            // Re-Encode
            AssertExtensions.SequenceEqual(msg.Encode(), encodedMsg);
        }

        internal static void AssertMultiSignMessageCore(
            ReadOnlySpan<byte> encodedMsg,
            ReadOnlySpan<byte> expectedContent,
            IDisposable signingKey,
            CoseAlgorithm algorithm,
            int expectedSignatures,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedBodyProtectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedBodyUnprotectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedSignProtectedHeaders = null,
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedSignUnprotectedHeaders = null,
            bool expectedDetachedContent = false)
        {
            var reader = new CborReader(encodedMsg.ToArray());

            // Start
            Assert.Equal((CborTag)98, reader.ReadTag());
            Assert.Equal(4, reader.ReadStartArray());

            // Body's Protected headers
            byte[] encodedBodyProtectedHeaders = reader.ReadByteString();
            AssertProtectedHeaders(encodedBodyProtectedHeaders, expectedBodyProtectedHeaders);

            // Body's Unprotected headers
            AssertHeaders(reader, expectedBodyUnprotectedHeaders ?? GetEmptyExpectedHeaders());

            // Content
            if (expectedDetachedContent)
            {
                reader.ReadNull();
            }
            else
            {
                AssertExtensions.SequenceEqual(expectedContent, reader.ReadByteString());
            }

            Assert.Equal(expectedSignatures, reader.ReadStartArray());
            List<byte[]> listOfRawSignProtectedHeaders = new();
            List<byte[]> listOfSignatureBytes = new();

            for (int i = 0; i < expectedSignatures; i++)
            {
                // Cose_Signature
                Assert.Equal(3, reader.ReadStartArray());

                // Sign's Protected headers
                byte[] rawSignProtectedHeaders = reader.ReadByteString();
                listOfRawSignProtectedHeaders.Add(rawSignProtectedHeaders);
                AssertProtectedHeaders(rawSignProtectedHeaders, expectedSignProtectedHeaders ?? GetExpectedProtectedHeaders(algorithm));

                // Sign's Unprotected headers
                AssertHeaders(reader, expectedSignUnprotectedHeaders ?? GetEmptyExpectedHeaders());

                // Signature
                byte[] signatureBytes = reader.ReadByteString();
                Assert.Equal(GetSignatureSize(signingKey), signatureBytes.Length);
                listOfSignatureBytes.Add(signatureBytes);

                reader.ReadEndArray(); // End of Cose_Signature.
            }
            reader.ReadEndArray(); // End of Cose_Signatures.
            reader.ReadEndArray(); // End of message.

            Assert.Equal(0, reader.BytesRemaining);

            // Verify
            CoseMultiSignMessage msg = CoseMessage.DecodeMultiSign(encodedMsg);
            Assert.Equal(expectedSignatures, msg.Signatures.Count);

            ReadOnlyCollection<CoseSignature> signatures = msg.Signatures;
            CoseSignature signature = signatures[0];

            if (signingKey is AsymmetricAlgorithm signingKeyAsymmetricAlgorithm)
            {
                if (expectedDetachedContent)
                {
                    Assert.True(signature.VerifyDetached(signingKeyAsymmetricAlgorithm, expectedContent), "msg.Verify(AsymmetricAlgorithm, content)");
                }
                else
                {
                    Assert.True(signature.VerifyEmbedded(signingKeyAsymmetricAlgorithm), "msg.Verify(AsymmetricAlgorithm)");
                }
            }

            CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, signingKey);
            if (expectedDetachedContent)
            {
                Assert.True(signature.VerifyDetached(coseKey, expectedContent), "msg.Verify(CoseKey, content)");
            }
            else
            {
                Assert.True(signature.VerifyEmbedded(coseKey), "msg.Verify(CoseKey)");
            }

            // Raw Body Protected Headers
            AssertExtensions.SequenceEqual(encodedBodyProtectedHeaders, msg.RawProtectedHeaders.Span);

            for (int i = 0; i < signatures.Count; i++)
            {
                // Raw Sign Protected Headers
                AssertExtensions.SequenceEqual(listOfRawSignProtectedHeaders[i], signatures[i].RawProtectedHeaders.Span);

                // Signature
                AssertExtensions.SequenceEqual(listOfSignatureBytes[i], signatures[i].Signature.Span);
            }

            // GetEncodedLength
            Assert.Equal(encodedMsg.Length, msg.GetEncodedLength());

            // Re-Encode
            AssertExtensions.SequenceEqual(msg.Encode(), encodedMsg);
        }

        internal static int GetSignatureSize(IDisposable key)
        {
            if (key is MLDsa mldsa)
            {
                return mldsa.Algorithm.SignatureSizeInBytes;
            }

            AsymmetricAlgorithm asymmetricKey = (AsymmetricAlgorithm)key;

            int size = (asymmetricKey.KeySize + 7) / 8;

            if (asymmetricKey is ECDsa)
            {
                size *= 2;
            }

            return size;
        }

        internal static CoseAlgorithm GetCoseAlgorithmFromCoseMessage(CoseMessage coseMessage)
        {
            if (coseMessage is CoseSign1Message sign1CoseMessage)
            {
                return GetCoseAlgorithmFromProtectedHeaders(sign1CoseMessage.ProtectedHeaders);
            }
            else
            {
                CoseMultiSignMessage multiSignCoseMessage = Assert.IsType<CoseMultiSignMessage>(coseMessage);
                AssertExtensions.TrueExpression(multiSignCoseMessage.Signatures.Count > 0, "MultiSignMessage should have at least one signature.");
                return GetCoseAlgorithmFromProtectedHeaders(multiSignCoseMessage.Signatures[0].ProtectedHeaders);
            }
        }

        internal static CoseAlgorithm GetCoseAlgorithmFromProtectedHeaders(CoseHeaderMap protectedHeaders)
        {
            if (protectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out CoseHeaderValue value))
            {
                var reader = new CborReader(value.EncodedValue);
                CborReaderState state = reader.PeekState();

                if (state == CborReaderState.NegativeInteger)
                {
                    int algorithmValue = reader.ReadInt32();
                    AssertExtensions.TrueExpression(Enum.IsDefined(typeof(CoseAlgorithm), algorithmValue));
                    return (CoseAlgorithm)algorithmValue;
                }
                else if (state == CborReaderState.TextString)
                {
                    string algorithmString = reader.ReadTextString();
                    return ParseAlgorithmStringValue(algorithmString);
                }
            }

            throw new InvalidOperationException("Protected headers do not contain an algorithm header.");
        }

        internal static CoseKey CoseKeyFromAlgorithmAndKey(CoseAlgorithm algorithm, IDisposable key)
        {
            if (key is ECDsa ecdsaKey)
            {
                return algorithm switch
                {
                    CoseAlgorithm.ES256 => new CoseKey(ecdsaKey, HashAlgorithmName.SHA256),
                    CoseAlgorithm.ES384 => new CoseKey(ecdsaKey, HashAlgorithmName.SHA384),
                    CoseAlgorithm.ES512 => new CoseKey(ecdsaKey, HashAlgorithmName.SHA512),
                    _ => throw new Exception($"Unknown algorithm {algorithm} for {key.GetType().Name}")
                };
            }
            else if (key is RSA rsaKey)
            {
                return algorithm switch
                {
                    CoseAlgorithm.RS256 => new CoseKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256),
                    CoseAlgorithm.RS384 => new CoseKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA384),
                    CoseAlgorithm.RS512 => new CoseKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA512),
                    CoseAlgorithm.PS256 => new CoseKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA256),
                    CoseAlgorithm.PS384 => new CoseKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA384),
                    CoseAlgorithm.PS512 => new CoseKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA512),
                    _ => throw new Exception($"Unknown algorithm {algorithm} for {key.GetType().Name}")
                };
            }
            else if (key is MLDsa mldsaKey)
            {
                return algorithm switch
                {
                    CoseAlgorithm.MLDsa44 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa44, mldsaKey),
                    CoseAlgorithm.MLDsa65 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa65, mldsaKey),
                    CoseAlgorithm.MLDsa87 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa87, mldsaKey),
                    _ => throw new Exception($"Unknown algorithm {algorithm} for {key.GetType().Name}")
                };

                CoseKey FromKeyWithExpectedAlgorithm(MLDsaAlgorithm expected, MLDsa key)
                    => key.Algorithm.Name == expected.Name ? new CoseKey(key) : throw new Exception($"Unknown algorithm {algorithm} for {key.GetType().Name}");
            }
            else
            {
                throw new Exception($"Unknown algorithm {algorithm} for {key.GetType().Name}");
            }
        }

        private static void AssertProtectedHeaders(byte[] protectedHeadersBytes, List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders)
        {
            if (expectedProtectedHeaders == null || expectedProtectedHeaders.Count == 0)
            {
                Assert.Equal(0, protectedHeadersBytes.Length);
                return;
            }

            var reader = new CborReader(protectedHeadersBytes);
            AssertHeaders(reader, expectedProtectedHeaders);

            Assert.Equal(0, reader.BytesRemaining);
        }

        private static void AssertHeaders(CborReader reader, List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedHeaders)
        {
            Assert.Equal(expectedHeaders.Count, reader.ReadStartMap());
            CoseHeaderMap headers = new();

            int headerCount = 0;
            while(reader.PeekState() != CborReaderState.EndMap)
            {
                CoseHeaderLabel label = reader.PeekState() switch
                {
                    CborReaderState.NegativeInteger or CborReaderState.UnsignedInteger => new CoseHeaderLabel(reader.ReadInt32()),
                    CborReaderState.TextString => new CoseHeaderLabel(reader.ReadTextString()),
                    _ => throw new InvalidOperationException()
                };

                headers[label] = CoseHeaderValue.FromEncodedValue(reader.ReadEncodedValue().Span);
                headerCount++;
            }

            reader.ReadEndMap();
            Assert.Equal(expectedHeaders.Count, headerCount);

            foreach ((CoseHeaderLabel expectedLabel, ReadOnlyMemory<byte> expectedEncodedValue) in expectedHeaders)
            {
                Assert.True(headers.TryGetValue(expectedLabel, out CoseHeaderValue value), "headers.TryGetValue(expectedLabel, out ReadOnlyMemory<byte> encodedValue)");
                AssertExtensions.SequenceEqual(expectedEncodedValue.Span, value.EncodedValue.Span);
            }
        }

        private static ECParameters _ec256Parameters = CreateECParameters("nistP256", "usWxHK2PmfnHKwXPS54m0kTcGJ90UiglWiGahtagnv8", "IBOL-C3BttVivg-lSreASjpkttcsz-1rb7btKLv8EX4", "V8kgd2ZBRuh2dgyVINBUqpPDr7BOMGcF22CQMIUHtNM");
        private static ECParameters _ec384Parameters = CreateECParameters("nistP384", "kTJyP2KSsBBhnb4kjWmMF7WHVsY55xUPgb7k64rDcjatChoZ1nvjKmYmPh5STRKc", "mM0weMVU2DKsYDxDJkEP9hZiRZtB8fPfXbzINZj_fF7YQRynNWedHEyzAJOX2e8s", "ok3Nq97AXlpEusO7jIy1FZATlBP9PNReMU7DWbkLQ5dU90snHuuHVDjEPmtV0fTo");
        private static ECParameters _ec512Parameters = CreateECParameters("nistP521", "AHKZLLOsCOzz5cY97ewNUajB957y-C-U88c3v13nmGZx6sYl_oJXu9A5RkTKqjqvjyekWF-7ytDyRXYgCF5cj0Kt", "AdymlHvOiLxXkEhayXQnNCvDX4h9htZaCJN34kfmC6pV5OhQHiraVySsUdaQkAgDPrwQrJmbnX9cwlGfP-HqHZR1", "AAhRON2r9cqXX1hg-RoI6R1tX5p2rUAYdmpHZoC1XNM56KtscrX6zbKipQrCW9CGZH3T4ubpnoTKLDYJ_fF3_rJt");

        [ThreadStatic]
        private static ECDsa? t_es256;
        [ThreadStatic]
        private static ECDsa? t_es384;
        [ThreadStatic]
        private static ECDsa? t_es512;

        internal static ECDsa ES256 => t_es256 ??= CreateECDsa(_ec256Parameters, true);
        internal static ECDsa ES384 => t_es384 ??= CreateECDsa(_ec384Parameters, true);
        internal static ECDsa ES512 => t_es512 ??= CreateECDsa(_ec512Parameters, true);

        [ThreadStatic]
        private static ECDsa? t_es256WithoutPrivateKey;
        [ThreadStatic]
        private static ECDsa? t_es384WithoutPrivateKey;
        [ThreadStatic]
        private static ECDsa? t_es512WithoutPrivateKey;

        private static ECDsa ES256WithoutPrivateKey => t_es256WithoutPrivateKey ??= CreateECDsa(_ec256Parameters, false);
        private static ECDsa ES384WithoutPrivateKey => t_es384WithoutPrivateKey ??= CreateECDsa(_ec384Parameters, false);
        private static ECDsa ES512WithoutPrivateKey => t_es512WithoutPrivateKey ??= CreateECDsa(_ec512Parameters, false);

        internal static ECDsa DefaultKey => ES256;
        internal static HashAlgorithmName DefaultHash { get; } = HashAlgorithmName.SHA256;

        [ThreadStatic]
        internal static RSA? t_rsaKey;
        [ThreadStatic]
        internal static RSA? t_rsaKeyWithoutPrivateKey;

        internal static RSA RSAKey => t_rsaKey ??= CreateRSA(true);
        internal static RSA RSAKeyWithoutPrivateKey => t_rsaKeyWithoutPrivateKey ??= CreateRSA(false);

        [ThreadStatic]
        private static MLDsa? t_mldsa44Key;
        [ThreadStatic]
        private static MLDsa? t_mldsa44KeyWithoutPrivateKey;
        [ThreadStatic]
        private static MLDsa? t_mldsa65Key;
        [ThreadStatic]
        private static MLDsa? t_mldsa65KeyWithoutPrivateKey;
        [ThreadStatic]
        private static MLDsa? t_mldsa87Key;
        [ThreadStatic]
        private static MLDsa? t_mldsa87KeyWithoutPrivateKey;

        internal static MLDsa MLDsa44Key => t_mldsa44Key ??= CreateMLDsa(MLDsaAlgorithm.MLDsa44, true);
        internal static MLDsa MLDsa44KeyWithoutPrivateKey => t_mldsa44KeyWithoutPrivateKey ??= CreateMLDsa(MLDsaAlgorithm.MLDsa44, false);
        internal static MLDsa MLDsa65Key => t_mldsa65Key ??= CreateMLDsa(MLDsaAlgorithm.MLDsa65, true);
        internal static MLDsa MLDsa65KeyWithoutPrivateKey => t_mldsa65KeyWithoutPrivateKey ??= CreateMLDsa(MLDsaAlgorithm.MLDsa65, false);
        internal static MLDsa MLDsa87Key => t_mldsa87Key ??= CreateMLDsa(MLDsaAlgorithm.MLDsa87, true);
        internal static MLDsa MLDsa87KeyWithoutPrivateKey => t_mldsa87KeyWithoutPrivateKey ??= CreateMLDsa(MLDsaAlgorithm.MLDsa87, false);

        private static ECParameters CreateECParameters(string curveFriendlyName, string base64UrlQx, string base64UrlQy, string base64UrlPrivateKey)
        {
            return new ECParameters()
            {
                Curve = ECCurve.CreateFromFriendlyName(curveFriendlyName),
                Q = new ECPoint
                {
                    X = Base64UrlEncoder.DecodeBytes(base64UrlQx),
                    Y = Base64UrlEncoder.DecodeBytes(base64UrlQy),
                },
                D = Base64UrlEncoder.DecodeBytes(base64UrlPrivateKey),
            };
        }

        private static ECDsa CreateECDsa(ECParameters parameters, bool includePrivateKey)
        {
            ECParameters parametersLocalCopy = parameters;
            if (!includePrivateKey)
            {
                parametersLocalCopy.D = null;
            }

            return ECDsa.Create(parametersLocalCopy);
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

        private static MLDsa CreateMLDsa(MLDsaAlgorithm algorithm, bool includePrivateKey)
        {
            MLDsaNistTestCase nistKey = MLDsaTestsData.GetPassingNistTestCase(algorithm);

            if (includePrivateKey)
            {
                return MLDsa.ImportMLDsaSecretKey(algorithm, nistKey.SecretKey);
            }
            else
            {
                return MLDsa.ImportMLDsaPublicKey(algorithm, nistKey.PublicKey);
            }
        }

        internal static bool AlgorithmNeedsHashAlgorithm(CoseAlgorithm algorithm)
            => algorithm is
                CoseAlgorithm.ES256 or CoseAlgorithm.ES384 or CoseAlgorithm.ES512 or
                CoseAlgorithm.PS256 or CoseAlgorithm.PS384 or CoseAlgorithm.PS512 or
                CoseAlgorithm.RS256 or CoseAlgorithm.RS384 or CoseAlgorithm.RS512;

        internal static bool AlgorithmIsSupported(CoseAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CoseAlgorithm.MLDsa44:
                case CoseAlgorithm.MLDsa65:
                case CoseAlgorithm.MLDsa87:
                    return MLDsa.IsSupported;
                default: return true;
            }
        }

        internal static (T Key, HashAlgorithmName? Hash, RSASignaturePadding? Padding) GetKeyHashPaddingTriplet<T>(CoseAlgorithm algorithm, bool useNonPrivateKey = false)
        {
            return algorithm switch
            {
                CoseAlgorithm.ES256 => (GetKey(ES256, ES256WithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA256, null),
                CoseAlgorithm.ES384 => (GetKey(ES384, ES384WithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA384, null),
                CoseAlgorithm.ES512 => (GetKey(ES512, ES512WithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA512, null),
                CoseAlgorithm.PS256 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                CoseAlgorithm.PS384 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                CoseAlgorithm.PS512 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                CoseAlgorithm.RS256 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                CoseAlgorithm.RS384 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                CoseAlgorithm.RS512 => (GetKey(RSAKey, RSAKeyWithoutPrivateKey, useNonPrivateKey), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                CoseAlgorithm.MLDsa44 => (GetKey(MLDsa44Key, MLDsa44KeyWithoutPrivateKey, useNonPrivateKey), null, null),
                CoseAlgorithm.MLDsa65 => (GetKey(MLDsa65Key, MLDsa65KeyWithoutPrivateKey, useNonPrivateKey), null, null),
                CoseAlgorithm.MLDsa87 => (GetKey(MLDsa87Key, MLDsa87KeyWithoutPrivateKey, useNonPrivateKey), null, null),
                _ => throw new NotImplementedException($"Unhandled {nameof(CoseAlgorithm)}: {algorithm}")
            };


            T GetKey(IDisposable privateKey, IDisposable nonPrivateKey, bool useNonPrivateKey)
            {
                if (privateKey is T privateKeyAsT && nonPrivateKey is T nonPrivateKeyAsT)
                {
                    return useNonPrivateKey ? nonPrivateKeyAsT : privateKeyAsT;
                }

                throw new InvalidOperationException($"Specified algorithm {algorithm} doesn't match the type {typeof(T)}");
            }
        }

        internal static Stream GetTestStream(byte[] content, StreamKind streamKind = StreamKind.Normal)
        {
            MemoryStream ms = streamKind switch
            {
                StreamKind.Normal => new MemoryStream(),
                StreamKind.Unseekable => new UnseekableMemoryStream(),
                StreamKind.Unreadable => new UnreadableMemoryStream(),
                _ => throw new InvalidOperationException()
            };

            ms.Write(content, 0, content.Length);
            ms.Position = 0;
            return ms;
        }

        internal static CoseSigner GetCoseSigner(IDisposable key, HashAlgorithmName? hash, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, RSASignaturePadding? padding = null)
        {
            if (key is RSA rsa)
            {
                AssertExtensions.TrueExpression(hash.HasValue);
                return new CoseSigner(rsa, padding ?? RSASignaturePadding.Pss, hash!.Value, protectedHeaders, unprotectedHeaders);
            }

            AssertExtensions.TrueExpression(padding == null);

            if (key is ECDsa ecdsa)
            {
                AssertExtensions.TrueExpression(hash.HasValue);
                return new CoseSigner(ecdsa, hash!.Value, protectedHeaders, unprotectedHeaders);
            }

            if (key is MLDsa mldsa)
            {
                AssertExtensions.FalseExpression(hash.HasValue);
                CoseKey mldsaKey = new CoseKey(mldsa);
                return new CoseSigner(mldsaKey, protectedHeaders, unprotectedHeaders);
            }

            throw new NotImplementedException($"Unhandled key type: {key.GetType()}");
        }

        internal static bool Sign1Verify(CoseMessage msg, IDisposable key, byte[] content, byte[]? associatedData = null)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);

            CoseAlgorithm algorithm = GetCoseAlgorithmFromCoseMessage(sign1Msg);
            CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, key);

            bool result = sign1Msg.Content.HasValue ? sign1Msg.VerifyEmbedded(coseKey, associatedData) : sign1Msg.VerifyDetached(coseKey, content, associatedData);

            if (key is AsymmetricAlgorithm keyAsymmetricAlgorithm)
            {
                AssertExtensions.TrueExpression(result == (sign1Msg.Content.HasValue ? sign1Msg.VerifyEmbedded(keyAsymmetricAlgorithm, associatedData) : sign1Msg.VerifyDetached(keyAsymmetricAlgorithm, content, associatedData)));
            }

            return result;
        }

        internal static bool MultiSignVerify(CoseMessage msg, IDisposable key, byte[] content, int expectedSignatures, byte[]? associatedData = null)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(expectedSignatures, signatures.Count);

            bool isDetached = !multiSignMsg.Content.HasValue;
            bool result = false;
            CoseAlgorithm algorithm = GetCoseAlgorithmFromCoseMessage(multiSignMsg);

            foreach (CoseSignature s in signatures)
            {
                if (key is AsymmetricAlgorithm keyAsymmetricAlgorithm)
                {
                    if (isDetached)
                    {
                        result = s.VerifyDetached(keyAsymmetricAlgorithm, content, associatedData);
                    }
                    else
                    {
                        result = s.VerifyEmbedded(keyAsymmetricAlgorithm, associatedData);
                    }

                    if (!result)
                    {
                        break;
                    }
                }

                CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, key);
                if (isDetached)
                {
                    result = s.VerifyDetached(coseKey, content, associatedData);
                }
                else
                {
                    result = s.VerifyEmbedded(coseKey, associatedData);
                }

                if (!result)
                {
                    break;
                }
            }

            return result;
        }

        internal static void MultiSignAddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
        {
            if (msg.Content.HasValue)
            {
                msg.AddSignatureForEmbedded(signer, associatedData);
            }
            else
            {
                msg.AddSignatureForDetached(content, signer, associatedData);
            }
        }

        private class UnseekableMemoryStream : MemoryStream
        {
            public override bool CanSeek => false;
        }

        private class UnreadableMemoryStream : MemoryStream
        {
            public override bool CanRead => false;
        }

        internal enum StreamKind
        {
            Normal,
            Unseekable,
            Unreadable
        }

        // each kind is represented by the value of its CBOR tag.
        internal enum CoseMessageKind
        {
            Sign1 = 18,
            MultiSign = 98
        }

        internal static void WriteDummyCritHeaderValue(CborWriter writer, bool useIndefiniteLength = false)
        {
            writer.WriteStartArray(useIndefiniteLength ? null : 1);
            writer.WriteInt32(42);
            writer.WriteEndArray();
        }

        internal static byte[] GetDummyCritHeaderValue(bool useIndefiniteLength = false)
        {
            var writer = new CborWriter();
            WriteDummyCritHeaderValue(writer, useIndefiniteLength);
            return writer.Encode();
        }

        internal static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static IEnumerable<byte[]> AllCborTypes()
        {
            var w = new CborWriter();

            w.WriteBigInteger(default);
            yield return ReturnDataAndReset(w);

            w.WriteBoolean(true);
            yield return ReturnDataAndReset(w);

            w.WriteByteString(s_sampleContent);
            yield return ReturnDataAndReset(w);

            w.WriteCborNegativeIntegerRepresentation(default);
            yield return ReturnDataAndReset(w);

            w.WriteDateTimeOffset(default);
            yield return ReturnDataAndReset(w);

            w.WriteDecimal(default);
            yield return ReturnDataAndReset(w);

            w.WriteDecimal(default);
            yield return ReturnDataAndReset(w);

            w.WriteDouble(default);
            yield return ReturnDataAndReset(w);
#if NET
            w.WriteHalf(default);
            yield return ReturnDataAndReset(w);
#endif
            w.WriteInt32(default);
            yield return ReturnDataAndReset(w);

            w.WriteInt64(default);
            yield return ReturnDataAndReset(w);

            w.WriteNull();
            yield return ReturnDataAndReset(w);

            w.WriteSimpleValue(CborSimpleValue.Undefined);
            yield return ReturnDataAndReset(w);

            w.WriteSingle(default);
            yield return ReturnDataAndReset(w);

            w.WriteTag(CborTag.UnsignedBigNum);
            w.WriteInt32(42);
            yield return ReturnDataAndReset(w);

            w.WriteTextString(string.Empty);
            yield return ReturnDataAndReset(w);

            w.WriteUInt32(default);
            yield return ReturnDataAndReset(w);

            w.WriteUInt64(default);
            yield return ReturnDataAndReset(w);

            w.WriteUnixTimeSeconds(default);
            yield return ReturnDataAndReset(w);

            // Array
            w.WriteStartArray(2);
            w.WriteInt32(42);
            w.WriteTextString("foo");
            w.WriteEndArray();
            yield return ReturnDataAndReset(w);

            // Map
            w.WriteStartMap(2);
            // first label-value pair.
            w.WriteInt32(42);
            w.WriteTextString("4242");
            // second label-value pair.
            w.WriteTextString("42");
            w.WriteInt32(4242);
            w.WriteEndMap();
            yield return ReturnDataAndReset(w);

            // Indefinite length array
            w.WriteStartArray(null);
            w.WriteInt32(42);
            w.WriteTextString("foo");
            w.WriteEndArray();
            yield return ReturnDataAndReset(w);

            // Indefinite length map
            w.WriteStartMap(null);
            // first label-value pair.
            w.WriteInt32(42);
            w.WriteTextString("4242");
            // second label-value pair.
            w.WriteTextString("42");
            w.WriteInt32(4242);
            w.WriteEndMap();
            yield return ReturnDataAndReset(w);

            // Indefinite length tstr
            w.WriteStartIndefiniteLengthTextString();
            w.WriteTextString("foo");
            w.WriteEndIndefiniteLengthTextString();
            yield return ReturnDataAndReset(w);

            // Indefinite length bstr
            w.WriteStartIndefiniteLengthByteString();
            w.WriteByteString(s_sampleContent);
            w.WriteEndIndefiniteLengthByteString();
            yield return ReturnDataAndReset(w);

            static byte[] ReturnDataAndReset(CborWriter w)
            {
                byte[] encodedValue = w.Encode();
                w.Reset();
                return encodedValue;
            }
        }

        internal static byte[] GetCounterSign(CoseMultiSignMessage msg, CoseSignature signature, CoseAlgorithm algorithm)
        {
            Assert.True(msg.Signatures.Contains(signature));
            var writer = new CborWriter();
            writer.WriteStartArray(3);

            // encoded protected
            byte[] encodedProtectedHeaders = GetCounterSignProtectedHeaders((int)algorithm);
            writer.WriteByteString(encodedProtectedHeaders);

            // empty unprotected headers
            writer.WriteStartMap(0);
            writer.WriteEndMap();

            // signature
            (IDisposable key, HashAlgorithmName? hash, _) = GetKeyHashPaddingTriplet<AsymmetricAlgorithm>(algorithm);
            byte[] signatureBytes = GetSignature(key, hash, GetToBeSignedForCounterSign(msg, signature, encodedProtectedHeaders));
            writer.WriteByteString(signatureBytes);
            writer.WriteEndArray();

            return writer.Encode();
        }

        private static byte[] GetCounterSignProtectedHeaders(int algorithm)
        {
            var writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(KnownHeaderAlg);
            writer.WriteInt32(algorithm);
            writer.WriteEndMap();

            return writer.Encode();
        }

        private static byte[] GetSignature(IDisposable key, HashAlgorithmName? hash, byte[] toBeSigned)
        {
            if (key is ECDsa ecdsa)
            {
                Assert.NotNull(hash);
                return ecdsa.SignData(toBeSigned, hash.Value);
            }
            else if (key is RSA rsa)
            {
                Assert.NotNull(hash);
                return rsa.SignData(toBeSigned, hash.Value, RSASignaturePadding.Pss);
            }
            else if (key is MLDsa mldsa)
            {
                Assert.Null(hash);
                byte[] sig = new byte[mldsa.Algorithm.SignatureSizeInBytes];
                Assert.Equal(sig.Length, mldsa.SignData(toBeSigned, sig));
                return sig;
            }

            throw new NotImplementedException($"Unhandled key type: {key.GetType()}");
        }

        internal static bool VerifyCounterSign(IDisposable key, HashAlgorithmName? hash, byte[] toBeSigned, byte[] signature)
        {
            if (key is ECDsa ecdsa)
            {
                Assert.NotNull(hash);
                return ecdsa.VerifyData(toBeSigned, signature, hash.Value);
            }
            else if (key is RSA rsa)
            {
                Assert.NotNull(hash);
                return rsa.VerifyData(toBeSigned, signature, hash.Value, RSASignaturePadding.Pss);
            }
            else if (key is MLDsa mldsa)
            {
                Assert.Null(hash);
                return mldsa.VerifyData(toBeSigned, signature);
            }

            throw new NotImplementedException($"Unhandled key type: {key.GetType()}");
        }

        internal static byte[] GetToBeSignedForCounterSign(CoseMultiSignMessage msg, CoseSignature signature, byte[] signProtected)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(5);
            writer.WriteTextString("CounterSignature");
            writer.WriteByteString(msg.RawProtectedHeaders.Span); // body_protected
            writer.WriteByteString(signProtected); // sign_protected
            writer.WriteByteString(default(Span<byte>)); // external_aad
            writer.WriteByteString(signature.Signature.Span);
            writer.WriteEndArray();

            return writer.Encode();
        }

        internal static (byte[], byte[]) ReadCounterSign(CoseHeaderValue value, IDisposable key)
        {
            var reader = new CborReader(value.EncodedValue);
            Assert.Equal(3, reader.ReadStartArray());

            // encoded protected
            byte[] encodedProtectedHeaders = reader.ReadByteString();

            // empty unprotected headers
            Assert.Equal(0, reader.ReadStartMap());
            reader.ReadEndMap();

            // signature
            byte[] signature = reader.ReadByteString();
            Assert.Equal(GetSignatureSize(key), signature.Length);

            reader.ReadEndArray();
            return (encodedProtectedHeaders, signature);
        }
    }
}
