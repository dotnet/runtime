// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public class FrozenSetAlternateLookupTests
    {
        [Fact]
        public void AlternateLookup_Empty()
        {
            Assert.False(FrozenSet<string>.Empty.TryGetAlternateLookup<ReadOnlySpan<char>>(out _));

            foreach (StringComparer comparer in new[] { StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase })
            {
                FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> lookup = FrozenSet.ToFrozenSet([], comparer).GetAlternateLookup<ReadOnlySpan<char>>();
                Assert.False(lookup.Contains("anything".AsSpan()));
            }
        }

        [Fact]
        public void UnsupportedComparer()
        {
            FrozenSet<string> frozen = FrozenSet.ToFrozenSet(["a", "b"]);
            Assert.Throws<InvalidOperationException>(() => frozen.GetAlternateLookup<ReadOnlySpan<char>>());
            Assert.False(frozen.TryGetAlternateLookup<ReadOnlySpan<char>>(out _));
        }

        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void NullAndEmptySpan_TreatedSpecially(StringComparison comparison)
        {
            FrozenSet<string?> frozen;

            StringComparer comparer = comparison == StringComparison.Ordinal ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            frozen = FrozenSet.ToFrozenSet(["a", "b", "", null], comparer);
            Assert.True(frozen.Contains(null));
            Assert.True(frozen.Contains(""));
            Assert.True(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains(ReadOnlySpan<char>.Empty));
            Assert.True(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains("".AsSpan()));

            frozen = FrozenSet.ToFrozenSet(["a", "b", ""], comparer);
            Assert.False(frozen.Contains(null));
            Assert.True(frozen.Contains(""));
            Assert.True(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains(ReadOnlySpan<char>.Empty));
            Assert.True(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains("".AsSpan()));

            frozen = FrozenSet.ToFrozenSet(["a", "b", null], comparer);
            Assert.True(frozen.Contains(null));
            Assert.False(frozen.Contains(""));
            Assert.False(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains(ReadOnlySpan<char>.Empty));
            Assert.False(frozen.GetAlternateLookup<ReadOnlySpan<char>>().Contains("".AsSpan()));
        }

        [Fact]
        public void StringKey_OnlyCharSpanAlternateSupported()
        {
            var comparer = new FrozenDictionaryAlternateLookupTests.Int32ToStringComparer();

            Assert.False(FrozenSet.Create(comparer, []).TryGetAlternateLookup<int>(out _));
            foreach (object[] input in FrozenFromKnownValuesTests.StringStringData())
            {
                Assert.False(((Dictionary<string, string>)input[0]).Select(i => i.Key).ToFrozenSet(comparer).TryGetAlternateLookup<int>(out _));
            }
        }

        [Theory]
        [MemberData(nameof(FrozenFromKnownValuesTests.StringStringData), MemberType = typeof(FrozenFromKnownValuesTests))]
        public void AlternateLookup_String_AlternateKeyReadOnlySpanChar(Dictionary<string, string> source)
        {
            FrozenSet<string> frozen = source.Select(p => p.Key).ToFrozenSet(source.Comparer);

            Assert.True(frozen.TryGetAlternateLookup(out FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> lookup1));
            var lookup2 = frozen.GetAlternateLookup<ReadOnlySpan<char>>();

            foreach (var lookup in new[] { lookup1, lookup2 })
            {
                Assert.Same(frozen, lookup.Set);
                foreach (KeyValuePair<string, string> entry in source)
                {
                    Assert.True(lookup.Contains(entry.Key.AsSpan()));

                    Assert.True(lookup.TryGetValue(entry.Key.AsSpan(), out string value));
                    Assert.Equal(source[entry.Key], value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(FrozenFromKnownValuesTests.Int32StringData), MemberType = typeof(FrozenFromKnownValuesTests))]
        public void AlternateLookup_Int32_AlternateKeyString(Dictionary<int, string> source)
        {
            FrozenSet<int> frozen = source.Select(p => p.Key).ToFrozenSet(new FrozenDictionaryAlternateLookupTests.StringToInt32Comparer());

            Assert.True(frozen.TryGetAlternateLookup(out FrozenSet<int>.AlternateLookup<string> lookup1));
            FrozenSet<int>.AlternateLookup<string> lookup2 = frozen.GetAlternateLookup<string>();

            foreach (var lookup in new[] { lookup1, lookup2 })
            {
                Assert.Same(frozen, lookup.Set);
                foreach (KeyValuePair<int, string> entry in source)
                {
                    Assert.True(lookup.Contains(entry.Key.ToString()));

                    Assert.True(lookup.TryGetValue(entry.Key.ToString(), out int value));
                    Assert.Equal(entry.Key, value);
                }
            }
        }

        [Fact]
        public void AlternateLookup_RomByte()
        {
            FrozenSet<ReadOnlyMemory<byte>> frozen = FrozenSet.Create(
                new FrozenDictionaryAlternateLookupTests.ReadOnlyMemoryByteComparer(),
                [
                    "abc"u8.ToArray(),
                    "def"u8.ToArray(),
                    "ghi"u8.ToArray(),
                ]);

            Assert.True(frozen.Contains("abc"u8.ToArray()));
            Assert.True(frozen.Contains("def"u8.ToArray()));
            Assert.True(frozen.Contains("ghi"u8.ToArray()));
            Assert.False(frozen.Contains("jkl"u8.ToArray()));

            FrozenSet<ReadOnlyMemory<byte>>.AlternateLookup<ReadOnlySpan<byte>> lookup = frozen.GetAlternateLookup<ReadOnlySpan<byte>>();

            Assert.True(lookup.Contains("abc"u8));
            Assert.True(lookup.Contains("def"u8));
            Assert.True(lookup.Contains("ghi"u8));
            Assert.False(lookup.Contains("jkl"u8));
        }
    }
}
