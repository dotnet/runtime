// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Formats.Cbor;
using System.Collections.Generic;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseHeaderMapTests
    {
        [Theory]
        [MemberData(nameof(SetValueGetValueData))]
        public void SetValue_GetValue_KnownCoseHeaderLabel(SetValueMethod setMethod, GetValueMethod getMethod)
        {
            var map = new CoseHeaderMap();
            SetValue(map, CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256, setMethod);

            if (setMethod != SetValueMethod.AddShortcut)
            {
                SetEncodedValue(map, CoseHeaderLabel.CriticalHeaders, GetDummyCritHeaderValue(), setMethod);
            }

            SetValue(map, CoseHeaderLabel.ContentType, ContentTypeDummyValue, setMethod);
            SetValue(map, CoseHeaderLabel.KeyIdentifier, s_sampleContent, setMethod);

            Assert.Equal((int)ECDsaAlgorithm.ES256, GetValue<int>(map, CoseHeaderLabel.Algorithm, getMethod));

            if (getMethod != GetValueMethod.GetValueShortcut)
            {
                AssertExtensions.SequenceEqual(GetDummyCritHeaderValue(), GetEncodedValue(map, CoseHeaderLabel.CriticalHeaders, getMethod));
            }

            Assert.Equal(ContentTypeDummyValue, GetValue<string>(map, CoseHeaderLabel.ContentType, getMethod));
            AssertExtensions.SequenceEqual(s_sampleContent, GetValue<byte[]>(map, CoseHeaderLabel.KeyIdentifier, getMethod));
        }

        [Theory]
        [MemberData(nameof(KnownHeadersEncodedValues_TestData))]
        public void SetEncodedValue_GetEncodedValue_KnownCoseHeaderLabel(int knownHeader, byte[] encodedValue, SetValueMethod setMethod, GetValueMethod getMethod)
        {
            var map = new CoseHeaderMap();
            var label = new CoseHeaderLabel(knownHeader);

            SetEncodedValue(map, label, encodedValue, setMethod);

            ReadOnlySpan<byte> returnedEncocedValue = GetEncodedValue(map, label, getMethod);

            AssertExtensions.SequenceEqual(encodedValue, returnedEncocedValue);
        }

        [Theory]
        [MemberData(nameof(SetValueData))]
        public void SetValue_KnownHeaders_ThrowIf_IncorrectValue(SetValueMethod method)
        {
            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<ArgumentException>(() => SetValue(map, CoseHeaderLabel.Algorithm, ReadOnlySpan<byte>.Empty, method));
            // [ +label ] (non-empty array)
            Assert.Throws<ArgumentException>(() => SetValue(map, CoseHeaderLabel.CriticalHeaders, ReadOnlySpan<byte>.Empty, method));
            // tstr / uint
            Assert.Throws<ArgumentException>(() => SetValue(map, CoseHeaderLabel.ContentType, -1, method));
            // bstr
            Assert.Throws<ArgumentException>(() => SetValue(map, CoseHeaderLabel.KeyIdentifier, "foo", method));
        }

        [Theory]
        [MemberData(nameof(SetValueData))]
        public void SetEncodedValue_KnownHeaders_ThrowIf_IncorrectValue(SetValueMethod method)
        {
            if (method == SetValueMethod.AddShortcut)
            {
                return;
            }

            var writer = new CborWriter();
            writer.WriteNull();
            byte[] encodedNullValue = writer.Encode();

            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<ArgumentException>(() => SetEncodedValue(map, CoseHeaderLabel.Algorithm, encodedNullValue, method));
            // [ +label ] (non-empty array)
            Assert.Throws<ArgumentException>(() => SetEncodedValue(map, CoseHeaderLabel.CriticalHeaders, encodedNullValue, method));
            writer.Reset();
            writer.WriteStartArray(0);
            writer.WriteEndArray();
            Assert.Throws<ArgumentException>(() => SetEncodedValue(map, CoseHeaderLabel.CriticalHeaders, writer.Encode(), method));
            // tstr / uint
            Assert.Throws<ArgumentException>(() => SetEncodedValue(map, CoseHeaderLabel.ContentType, encodedNullValue, method));
            // bstr
            Assert.Throws<ArgumentException>(() => SetEncodedValue(map, CoseHeaderLabel.KeyIdentifier, encodedNullValue, method));
        }

        [Fact]
        public void SetValue_InvalidCoseHeaderValue()
        {
            CoseHeaderLabel[] labelsToTest = {
                new CoseHeaderLabel("foo"),
                new CoseHeaderLabel(42),
                CoseHeaderLabel.Algorithm,
                CoseHeaderLabel.ContentType,
                CoseHeaderLabel.CriticalHeaders,
                CoseHeaderLabel.KeyIdentifier
            };

            foreach (CoseHeaderLabel label in labelsToTest)
            {
                var map = new CoseHeaderMap();

                Assert.Throws<ArgumentException>(() => map.Add(label, new CoseHeaderValue()));
                Assert.Throws<ArgumentException>(() => map[label] = new CoseHeaderValue());

                Assert.Throws<ArgumentException>(() => map.Add(label, default(CoseHeaderValue)));
                Assert.Throws<ArgumentException>(() => map[label] = default(CoseHeaderValue));
            }
        }

        [Fact]
        public void Enumerate()
        {
            var map = new CoseHeaderMap();
            SetValue(map, CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256, default(SetValueMethod));
            SetEncodedValue(map, CoseHeaderLabel.CriticalHeaders, GetDummyCritHeaderValue(), default(SetValueMethod));
            SetValue(map ,CoseHeaderLabel.ContentType, ContentTypeDummyValue, default(SetValueMethod));
            SetValue(map, CoseHeaderLabel.KeyIdentifier, s_sampleContent, default(SetValueMethod));

            var writer = new CborWriter();
            int currentHeader = KnownHeaderAlg;
            foreach (KeyValuePair<CoseHeaderLabel, CoseHeaderValue> kvp in map)
            {
                CoseHeaderLabel label = kvp.Key;
                CoseHeaderValue value = kvp.Value;

                Assert.Equal(new CoseHeaderLabel(currentHeader), label);
                ReadOnlyMemory<byte> expectedValue = currentHeader switch
                {
                    KnownHeaderAlg => EncodeInt32((int)ECDsaAlgorithm.ES256, writer),
                    KnownHeaderCrit => GetDummyCritHeaderValue(),
                    KnownHeaderContentType => EncodeString(ContentTypeDummyValue, writer),
                    KnownHeaderKid => EncodeBytes(s_sampleContent, writer),
                    _ => throw new InvalidOperationException()
                };
                AssertExtensions.SequenceEqual(expectedValue.Span, value.EncodedValue.Span);
                currentHeader++;
            }
            Assert.Equal(KnownHeaderKid + 1, currentHeader);

            static ReadOnlyMemory<byte> EncodeInt32(int value, CborWriter writer)
            {
                writer.WriteInt32(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeString(string value, CborWriter writer)
            {
                writer.WriteTextString(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeBytes(ReadOnlySpan<byte> value, CborWriter writer)
            {
                writer.WriteByteString(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeAndReset(CborWriter writer)
            {
                ReadOnlyMemory<byte> encodedValue = writer.Encode();
                writer.Reset();
                return encodedValue;
            }
        }

        [Fact]
        public void DecodedProtectedMapShouldBeReadOnly()
        {
            byte[] encodedMessage = CoseSign1Message.SignEmbedded(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");
        }

        [Theory]
        [InlineData(GetValueMethod.ItemGet)]
        [InlineData(GetValueMethod.TryGetValue)]
        [InlineData(GetValueMethod.GetValueShortcut)]
        public void GetValueFromReadOnlyProtectedMap(GetValueMethod getMethod)
        {
            byte[] encodedMessage = CoseSign1Message.SignEmbedded(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            CoseHeaderMap protectedHeaders = message.ProtectedHeaders;

            Assert.True(protectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");

            int expectedAlgorithm = (int)ECDsaAlgorithm.ES256;
            int algorithm = GetValue<int>(protectedHeaders, CoseHeaderLabel.Algorithm, getMethod);
            Assert.Equal(expectedAlgorithm, algorithm);

            if (getMethod != GetValueMethod.GetValueShortcut)
            {
                ReadOnlySpan<byte> encodedAlgorithm = GetEncodedValue(protectedHeaders, CoseHeaderLabel.Algorithm, getMethod);
                Assert.Equal(expectedAlgorithm, new CborReader(encodedAlgorithm.ToArray()).ReadInt32());
            }
        }

        [Fact]
        public void SetValueAndRemoveAndClearThrowIfProtectedMapIsReadOnly()
        {
            byte[] encodedMessage = CoseSign1Message.SignEmbedded(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");

            CoseHeaderMap protectedHeaders = message.ProtectedHeaders;

            // New value.
            VerifyThrows(protectedHeaders, new CoseHeaderLabel("bar"));

            // Existing value.
            VerifyThrows(protectedHeaders, CoseHeaderLabel.Algorithm);

            // Verify existing value was not overwritten even after throwing.
            Assert.Equal((int)ECDsaAlgorithm.ES256, GetValue<int>(protectedHeaders, CoseHeaderLabel.Algorithm, default(GetValueMethod)));

            // Non-readonly header works correctly.
            CoseHeaderMap unprotectedHeaders = message.UnprotectedHeaders;
            var fooLabel = new CoseHeaderLabel("foo");
            foreach (SetValueMethod setMethod in Enum.GetValues(typeof(SetValueMethod)))
            {
                SetValue(unprotectedHeaders, fooLabel, 42, setMethod);
                unprotectedHeaders.Remove(fooLabel);
            }

            static void VerifyThrows(CoseHeaderMap map, CoseHeaderLabel label)
            {
                foreach (SetValueMethod setMethod in Enum.GetValues(typeof(SetValueMethod)))
                {
                    Assert.Throws<InvalidOperationException>(() => SetValue(map, label, 42, setMethod));
                }
                Assert.Throws<InvalidOperationException>(() => map.Remove(label));
                Assert.Throws<InvalidOperationException>(() => map.Clear());
            }
        }

        public enum SetValueMethod
        {
            ItemSet,
            Add,
            AddShortcut,
        }

        private static void SetValue(CoseHeaderMap map, CoseHeaderLabel label, ReadOnlySpan<byte> value, SetValueMethod method)
        {
            if (method == SetValueMethod.ItemSet)
            {
                map[label] = CoseHeaderValue.FromBytes(value);
            }
            else if (method == SetValueMethod.Add)
            {
                map.Add(label, CoseHeaderValue.FromBytes(value));
            }
            else
            {
                Assert.Equal(SetValueMethod.AddShortcut, method);
                map.Add(label, CoseHeaderValue.FromBytes(value));
            }
        }

        private static void SetValue<T>(CoseHeaderMap map, CoseHeaderLabel label, T value, SetValueMethod method)
        {
            if (method == SetValueMethod.ItemSet)
            {
                map[label] = ToCoseHeaderValue(value);
            }
            else if (method == SetValueMethod.Add)
            {
                map.Add(label, ToCoseHeaderValue(value));
            }
            else
            {
                Assert.Equal(SetValueMethod.AddShortcut, method);
                switch (value)
                {
                    case int intValue:
                        map.Add(label, intValue);
                        break;
                    case string stringValue:
                        map.Add(label, stringValue);
                        break;
                    case byte[] bytesValue:
                        map.Add(label, bytesValue);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static void SetEncodedValue(CoseHeaderMap map, CoseHeaderLabel label, ReadOnlySpan<byte> encodedValue, SetValueMethod method)
        {
            if (method == SetValueMethod.AddShortcut) // there's no shortcut for encoded values.
                throw new InvalidOperationException();

            CoseHeaderValue value = CoseHeaderValue.FromEncodedValue(encodedValue);

            if (method == SetValueMethod.ItemSet)
            {
                map[label] = value;
            }
            else
            {
                Assert.Equal(SetValueMethod.Add, method);
                map.Add(label, value);
            }
        }

        public enum GetValueMethod
        {
            ItemGet,
            TryGetValue,
            GetValueShortcut,
        }

        private static T GetValue<T>(CoseHeaderMap map, CoseHeaderLabel label, GetValueMethod method)
        {
            CoseHeaderValue value;

            if (method == GetValueMethod.ItemGet)
            {
                value = map[label];
            }
            else if (method == GetValueMethod.TryGetValue)
            {
                bool result = map.TryGetValue(label, out value);
                Assert.True(result, "CoseHeaderMap.TryGetValue retuned false");
            }
            else if (method == GetValueMethod.GetValueShortcut)
            {
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)map.GetValueAsInt32(label);
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)map.GetValueAsString(label);
                }

                if (typeof(T) == typeof(byte[]))
                {
                    return (T)(object)map.GetValueAsBytes(label);
                }

                throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException();
            }

            return FromCoseHeaderValue<T>(value);
        }

        private static byte[] GetEncodedValue(CoseHeaderMap map, CoseHeaderLabel label, GetValueMethod method)
        {
            if (method == GetValueMethod.ItemGet)
            {
                return map[label].EncodedValue.ToArray();
            }

            if (method == GetValueMethod.TryGetValue)
            {
                map.TryGetValue(label, out CoseHeaderValue value);
                return value.EncodedValue.ToArray();
            }

            Assert.Equal(GetValueMethod.GetValueShortcut, method);
            throw new InvalidOperationException();
        }

        private static CoseHeaderValue ToCoseHeaderValue<T>(T value)
        {
            return value switch
            {
                int intValue => CoseHeaderValue.FromInt32(intValue),
                string stringValue => CoseHeaderValue.FromString(stringValue),
                byte[] bytesValue => CoseHeaderValue.FromBytes(bytesValue),
                _ => throw new InvalidOperationException()
            };
        }

        private static T FromCoseHeaderValue<T>(CoseHeaderValue value)
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)value.GetValueAsInt32();
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.GetValueAsString();
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (T)(object)value.GetValueAsBytes();
            }

            throw new InvalidOperationException();
        }

        public static IEnumerable<object[]> SetValueData =>
            new List<object[]>
            {
                new object[] { SetValueMethod.ItemSet },
                new object[] { SetValueMethod.Add },
                new object[] { SetValueMethod.AddShortcut }
            };

        public static IEnumerable<object[]> SetValueGetValueData =>
            new List<object[]>
            {
                new object[] { SetValueMethod.ItemSet, GetValueMethod.ItemGet },
                new object[] { SetValueMethod.Add, GetValueMethod.TryGetValue },
                new object[] { SetValueMethod.AddShortcut, GetValueMethod.GetValueShortcut }
            };

        public static IEnumerable<object[]> KnownHeadersEncodedValues_TestData()
        {
            var writer = new CborWriter();

            var setGetValuePairs = new List<(SetValueMethod, GetValueMethod)>
            {
                (SetValueMethod.ItemSet, GetValueMethod.ItemGet),
                (SetValueMethod.Add, GetValueMethod.TryGetValue),
            };

            foreach ((SetValueMethod setMethod, GetValueMethod getMethod) in setGetValuePairs)
            {
                writer.WriteInt32((int)ECDsaAlgorithm.ES256);
                yield return ReturnDataAndReset(KnownHeaderAlg, writer, setMethod, getMethod);

                WriteDummyCritHeaderValue(writer);
                yield return ReturnDataAndReset(KnownHeaderCrit, writer, setMethod, getMethod);

                writer.WriteTextString(ContentTypeDummyValue);
                yield return ReturnDataAndReset(KnownHeaderContentType, writer, setMethod, getMethod);

                writer.WriteByteString(new byte[] { 0x42, 0x31, 0x31 });
                yield return ReturnDataAndReset(KnownHeaderKid, writer, setMethod, getMethod);
            }

            static object[] ReturnDataAndReset(int knownHeader, CborWriter w, SetValueMethod setMethod, GetValueMethod getMethod)
            {
                byte[] encodedValue = w.Encode();
                w.Reset();
                return new object[] { knownHeader, encodedValue, setMethod, getMethod };
            }
        }
    }
}
