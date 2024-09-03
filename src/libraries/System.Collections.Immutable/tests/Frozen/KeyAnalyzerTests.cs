// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public static class KeyAnalyzerTests
    {
        private static KeyAnalyzer.AnalysisResults RunAnalysis(string[] values, bool ignoreCase)
        {
            int minLength = int.MaxValue, maxLength = 0;
            foreach (string s in values)
            {
                if (s.Length < minLength) minLength = s.Length;
                if (s.Length > maxLength) maxLength = s.Length;
            }

            KeyAnalyzer.AnalysisResults r = KeyAnalyzer.Analyze(values, ignoreCase, minLength, maxLength);

            Assert.All(values, s => Assert.InRange(s.Length, r.MinimumLength, int.MaxValue));
            Assert.All(values, s => Assert.InRange(s.Length, 0, r.MinimumLength + r.MaximumLengthDiff));

            return r;
        }

        [Fact]
        public static void LeftHand()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "K0", "K20", "K300" }, false);
            Assert.False(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.Equal(1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "S1" }, false);
            Assert.False(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "S1", "T1" }, false);
            Assert.False(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "SA1", "TA1", "SB1" }, false);
            Assert.False(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void LeftHandCaseInsensitive()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "É1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "É1", "T1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ÉA1", "TA1", "ÉB1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(2, r.HashCount);

            r = RunAnalysis(new[] { "ABCDEÉ1ABCDEF", "ABCDETA1ABCDEF", "ABCDESB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(5, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ABCDEFÉ1ABCDEF", "ABCDEFTA1ABCDEF", "ABCDEFSB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(6, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ABCÉDEFÉ1ABCDEF", "ABCÉDEFTA1ABCDEF", "ABCÉDEFSB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(7, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "1abc", "2abc", "3abc", "4abc", "5abc", "6abc" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "0001", "0002", "0003", "0004", "0005", "0006" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(3, r.HashIndex);
            Assert.Equal(1, r.HashCount);

        }

        [Fact]
        public static void LeftHandCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "S1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "S1", "T1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "SA1", "TA1", "SB1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void RightHand()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "1T1", "1T" }, false);
            Assert.True(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "1ATA", "1ATB", "1BS" }, false);
            Assert.True(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);
        }

        [Fact]
        public static void RightHandCaseInsensitive()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "1ÉÉ", "1É" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-2, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ÉA", "1AT", "1AÉT" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-2, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void RightHandCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "a1", "A1T" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "bÉÉ", "caT", "cAÉT" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
            Assert.Equal(-3, r.HashIndex);
            Assert.Equal(1, r.HashCount);
        }

        [Fact]
        public static void Full()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "ABC", "DBC", "ADC", "ABD", "ABDABD" }, false);
            Assert.False(r.SubstringHashing);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
        }

        [Fact]
        public static void FullCaseInsensitive()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "æbc", "DBC", "æDC", "æbd", "æbdæbd" }, true);
            Assert.False(r.SubstringHashing);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAsciiIfIgnoreCase);
        }

        [Fact]
        public static void FullCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "abc", "DBC", "aDC", "abd", "abdabd" }, true);
            Assert.False(r.SubstringHashing);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAsciiIfIgnoreCase);
        }

        [Fact]
        public static void IsAllAscii()
        {
            Assert.True(KeyAnalyzer.IsAllAscii("abc".AsSpan()));
            Assert.True(KeyAnalyzer.IsAllAscii("abcdefghij".AsSpan()));
            Assert.False(KeyAnalyzer.IsAllAscii("abcdéfghij".AsSpan()));
        }

        [Fact]
        public static void ContainsAnyLetters()
        {
            Assert.True(KeyAnalyzer.ContainsAnyAsciiLetters("abc".AsSpan()));
            Assert.True(KeyAnalyzer.ContainsAnyAsciiLetters("ABC".AsSpan()));
            Assert.False(KeyAnalyzer.ContainsAnyAsciiLetters("123".AsSpan()));
            // note, must only pass ASCII to ContainsAnyLetters, anything else is a
            // Debug.Assert and would not have been called in the actual implementation
        }

        [Fact]
        public static void HasSufficientUniquenessFactor()
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "b", "c" }, 0));
            Assert.Equal(3, set.Count);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "b", "a" }, 1));
            Assert.Equal(2, set.Count); // set should only have the non-collided ones

            Assert.False(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "aa", "ab", "aa" }, 0));
            Assert.Equal(2, set.Count);
        }

        [Fact]
        public static void HasSufficientUniquenessFactorInsensitive()
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "B", "c" }, 0));
            Assert.Equal(3, set.Count);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "aa", "AA" }, 1));
            Assert.Equal(1, set.Count); // set should only have the non-collided ones

            Assert.False(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "aa", "AA" }, 0));
        }

        [Fact]
        public static void HasSufficientUniquenessFactorLarge()
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "b", "c" }, 1));
            Assert.Equal(3, set.Count);

            Assert.True(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "b", "a" }, 1));
            Assert.Equal(2, set.Count); // set should only have the non-collided ones

            Assert.False(KeyAnalyzer.HasSufficientUniquenessFactor(set, new[] { "a", "a", "a" }, 1));
        }

        // reuse the typical data declared in the FrozenFromKnownValuesTests
        public static IEnumerable<object[]> TypicalData() => FrozenFromKnownValuesTests.StringStringData();

        [Theory]
        [MemberData(nameof(TypicalData))]
        public static void HasSufficientUniquenessKnownData(Dictionary<string, string> source)
        {
            string[] keys = source.Keys.ToArray();
            HashSet<string> set = new HashSet<string>(source.Comparer);

            int allowedCollisions = keys.Length / 20;
            bool passable = KeyAnalyzer.HasSufficientUniquenessFactor(set, keys.AsSpan(), allowedCollisions);

            if (passable)
                Assert.InRange(set.Count, keys.Length - allowedCollisions, keys.Length);
        }
    }
}
