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

        [field: ThreadStatic]
        private static TestKeyRing ThreadKeys => field ??= new();

        internal static ECDsa ES256 => ThreadKeys.ES256;
        internal static ECDsa ES384 => ThreadKeys.ES384;
        internal static ECDsa ES512 => ThreadKeys.ES512;

        internal static ECDsa ES256WithoutPrivateKey => ThreadKeys.ES256WithoutPrivateKey;
        internal static ECDsa ES384WithoutPrivateKey => ThreadKeys.ES384WithoutPrivateKey;
        internal static ECDsa ES512WithoutPrivateKey => ThreadKeys.ES512WithoutPrivateKey;

        internal static ECDsa DefaultKey => ES256;
        internal static HashAlgorithmName DefaultHash { get; } = HashAlgorithmName.SHA256;

        internal static RSA RSAKey => ThreadKeys.RSAKey;
        internal static RSA RSAKeyWithoutPrivateKey => ThreadKeys.RSAKeyWithoutPrivateKey;

        internal static MLDsa MLDsa44Key => ThreadKeys.MLDsa44Key;
        internal static MLDsa MLDsa44KeyWithoutPrivateKey => ThreadKeys.MLDsa44KeyWithoutPrivateKey;
        internal static MLDsa MLDsa65Key => ThreadKeys.MLDsa65Key;
        internal static MLDsa MLDsa65KeyWithoutPrivateKey => ThreadKeys.MLDsa65KeyWithoutPrivateKey;
        internal static MLDsa MLDsa87Key => ThreadKeys.MLDsa87Key;
        internal static MLDsa MLDsa87KeyWithoutPrivateKey => ThreadKeys.MLDsa87KeyWithoutPrivateKey;

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
                default:
                    return true;
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
                return mldsa.SignData(toBeSigned);
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
