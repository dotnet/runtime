// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public class FrozenDictionaryAlternateLookupTests
    {
        [Fact]
        public void AlternateLookup_Empty()
        {
            FrozenDictionary<string, string>[] unsupported =
            [
                FrozenDictionary<string, string>.Empty,
                FrozenDictionary.ToFrozenDictionary<string, string>([]),
                FrozenDictionary.ToFrozenDictionary<string, string>([], EqualityComparer<string>.Default),
            ];
            foreach (FrozenDictionary<string, string> frozen in unsupported)
            {
                Assert.Throws<InvalidOperationException>(() => frozen.GetAlternateLookup<ReadOnlySpan<char>>());
                Assert.False(frozen.TryGetAlternateLookup<ReadOnlySpan<char>>(out _));
            }

            FrozenDictionary<string, string>[] supported =
            [
                FrozenDictionary.ToFrozenDictionary<string, string>([], StringComparer.Ordinal),
                FrozenDictionary.ToFrozenDictionary<string, string>([], StringComparer.OrdinalIgnoreCase),
            ];
            foreach (FrozenDictionary<string, string> frozen in supported)
            {
                FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup = frozen.GetAlternateLookup<ReadOnlySpan<char>>();
                Assert.False(lookup.ContainsKey("anything".AsSpan()));
            }
        }

        [Fact]
        public void UnsupportedComparer_ThrowsOrReturnsFalse()
        {
            FrozenDictionary<string, int> frozen = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }.ToFrozenDictionary();
            Assert.Throws<InvalidOperationException>(() => frozen.GetAlternateLookup<ReadOnlySpan<char>>());
            Assert.False(frozen.TryGetAlternateLookup<ReadOnlySpan<char>>(out _));
        }

        [Fact]
        public void StringKey_OnlyCharSpanAlternateSupported()
        {
            Assert.False(FrozenDictionary.ToFrozenDictionary<string, string>([], new Int32ToStringComparer()).TryGetAlternateLookup<int>(out _));
            foreach (object[] input in FrozenFromKnownValuesTests.StringStringData())
            {
                Assert.False(((Dictionary<string, string>)input[0]).ToFrozenDictionary(new Int32ToStringComparer()).TryGetAlternateLookup<int>(out _));
            }
        }

        [Theory]
        [MemberData(nameof(FrozenFromKnownValuesTests.StringStringData), MemberType = typeof(FrozenFromKnownValuesTests))]
        public void AlternateLookup_String_AlternateKeyReadOnlySpanChar(Dictionary<string, string> source)
        {
            FrozenDictionary<string, string> frozen = source.ToFrozenDictionary(source.Comparer);

            Assert.True(frozen.TryGetAlternateLookup(out FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup1));
            var lookup2 = frozen.GetAlternateLookup<ReadOnlySpan<char>>();

            foreach (var lookup in new[] { lookup1, lookup2 })
            {
                Assert.Same(frozen, lookup.Dictionary);
                foreach (KeyValuePair<string, string> entry in source)
                {
                    Assert.True(lookup.ContainsKey(entry.Key.AsSpan()));

                    Assert.Equal(source[entry.Key], lookup[entry.Key.AsSpan()], frozen.Comparer);

                    Assert.True(lookup.TryGetValue(entry.Key.AsSpan(), out string value));
                    Assert.Equal(source[entry.Key], value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(FrozenFromKnownValuesTests.Int32StringData), MemberType = typeof(FrozenFromKnownValuesTests))]
        public void AlternateLookup_Int32_AlternateKeyString(Dictionary<int, string> source)
        {
            FrozenDictionary<int, string> frozen = source.ToFrozenDictionary(new StringToInt32Comparer());

            Assert.True(frozen.TryGetAlternateLookup(out FrozenDictionary<int, string>.AlternateLookup<string> lookup1));
            var lookup2 = frozen.GetAlternateLookup<string>();

            foreach (var lookup in new[] { lookup1, lookup2 })
            {
                Assert.Same(frozen, lookup.Dictionary);
                foreach (KeyValuePair<int, string> entry in source)
                {
                    Assert.True(lookup.ContainsKey(entry.Key.ToString()));

                    Assert.Equal(source[entry.Key], lookup[entry.Key.ToString()]);

                    Assert.True(lookup.TryGetValue(entry.Key.ToString(), out string value));
                    Assert.Equal(source[entry.Key], value);
                }
            }
        }

        [Fact]
        public void AlternateLookup_RomByte()
        {
            FrozenDictionary<ReadOnlyMemory<byte>, int> frozen = new Dictionary<ReadOnlyMemory<byte>, int>
            {
                ["abc"u8.ToArray()] = 1,
                ["def"u8.ToArray()] = 2,
                ["ghi"u8.ToArray()] = 3,
            }.ToFrozenDictionary(new ReadOnlyMemoryByteComparer());

            Assert.True(frozen.ContainsKey("abc"u8.ToArray()));
            Assert.True(frozen.ContainsKey("def"u8.ToArray()));
            Assert.True(frozen.ContainsKey("ghi"u8.ToArray()));
            Assert.False(frozen.ContainsKey("jkl"u8.ToArray()));

            FrozenDictionary<ReadOnlyMemory<byte>, int>.AlternateLookup<ReadOnlySpan<byte>> lookup = frozen.GetAlternateLookup<ReadOnlySpan<byte>>();

            Assert.True(lookup.ContainsKey("abc"u8));
            Assert.True(lookup.ContainsKey("def"u8));
            Assert.True(lookup.ContainsKey("ghi"u8));
            Assert.False(lookup.ContainsKey("jkl"u8));

            Assert.Equal(1, lookup["abc"u8]);
            Assert.Equal(2, lookup["def"u8]);
            Assert.Equal(3, lookup["ghi"u8]);
            Assert.Throws<KeyNotFoundException>(() => lookup["jkl"u8]);
        }

        public sealed class StringToInt32Comparer : IEqualityComparer<int>, IAlternateEqualityComparer<string, int>
        {
            public bool Equals(int x, int y) => x == y;
            public int GetHashCode(int obj) => obj.GetHashCode();

            public bool Equals(string alternate, int other) => int.Parse(alternate) == other;
            public int GetHashCode(string alternate) => int.Parse(alternate).GetHashCode();
            public int Create(string alternate) => int.Parse(alternate);
        }

        public sealed class Int32ToStringComparer : IEqualityComparer<string>, IAlternateEqualityComparer<int, string>
        {
            public bool Equals(string x, string y) => x == y;
            public int GetHashCode(string obj) => obj.GetHashCode();

            public bool Equals(int alternate, string other) => alternate.ToString() == other;
            public int GetHashCode(int alternate) => alternate.ToString().GetHashCode();
            public string Create(int alternate) => alternate.ToString();
        }

        public sealed class ReadOnlyMemoryByteComparer : IEqualityComparer<ReadOnlyMemory<byte>>, IAlternateEqualityComparer<ReadOnlySpan<byte>, ReadOnlyMemory<byte>>
        {
            public ReadOnlyMemory<byte> Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
            public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) => x.Span.SequenceEqual(y.Span);
            public bool Equals(ReadOnlySpan<byte> alternate, ReadOnlyMemory<byte> other) => alternate.SequenceEqual(other.Span);
            public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj) => GetHashCode(obj.Span);
            public int GetHashCode(ReadOnlySpan<byte> alternate)
            {
                HashCode hc = default;
                hc.AddBytes(alternate);
                return hc.ToHashCode();
            }
        }
    }
}
