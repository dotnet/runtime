// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public static class KeyAnalyzerTests
    {
        private static KeyAnalyzer.AnalysisResults RunAnalysis(string[] values, bool ignoreCase)
        {
            KeyAnalyzer.Analyze(values, ignoreCase, out KeyAnalyzer.AnalysisResults r);

            foreach (string s in values)
            {
                Assert.True(s.Length >= r.MinimumLength);
                Assert.True(s.Length <= r.MinimumLength + r.MaximumLengthDiff);
            }

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
            Assert.False(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "É1", "T1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ÉA1", "TA1", "ÉB1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(2, r.HashCount);

            r = RunAnalysis(new[] { "ABCDEÉ1ABCDEF", "ABCDETA1ABCDEF", "ABCDESB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(5, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ABCDEFÉ1ABCDEF", "ABCDEFTA1ABCDEF", "ABCDEFSB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(6, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "ABCÉDEFÉ1ABCDEF", "ABCÉDEFTA1ABCDEF", "ABCÉDEFSB1ABCDEF" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(7, r.HashIndex);
            Assert.Equal(1, r.HashCount);
        }

        [Fact]
        public static void LeftHandCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "S1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "S1", "T1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "SA1", "TA1", "SB1" }, true);
            Assert.False(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(0, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void RightHand()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "1S", "1T" }, false);
            Assert.True(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "1AS", "1AT", "1BS" }, false);
            Assert.True(r.RightJustifiedSubstring);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(-2, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void RightHandCaseInsensitive()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "1É", "1T" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "1AÉ", "1AT", "1BÉ" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
            Assert.Equal(-2, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void RightHandCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "1S", "1T" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(-1, r.HashIndex);
            Assert.Equal(1, r.HashCount);

            r = RunAnalysis(new[] { "1AS", "1AT", "1BS" }, true);
            Assert.True(r.RightJustifiedSubstring);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
            Assert.Equal(-2, r.HashIndex);
            Assert.Equal(2, r.HashCount);
        }

        [Fact]
        public static void Full()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "ABC", "DBC", "ADC", "ABD", "ABDABD" }, false);
            Assert.False(r.SubstringHashing);
            Assert.False(r.IgnoreCase);
            Assert.True(r.AllAscii);
        }

        [Fact]
        public static void FullCaseInsensitive()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "æbc", "DBC", "æDC", "æbd", "æbdæbd" }, true);
            Assert.False(r.SubstringHashing);
            Assert.True(r.IgnoreCase);
            Assert.False(r.AllAscii);
        }

        [Fact]
        public static void FullCaseInsensitiveAscii()
        {
            KeyAnalyzer.AnalysisResults r = RunAnalysis(new[] { "abc", "DBC", "aDC", "abd", "abdabd" }, true);
            Assert.False(r.SubstringHashing);
            Assert.True(r.IgnoreCase);
            Assert.True(r.AllAscii);
        }

        [Fact]
        public static void IsAllAscii()
        {
            Assert.True(KeyAnalyzer.IsAllAscii("abc".AsSpan()));
            Assert.True(KeyAnalyzer.IsAllAscii("abcdefghij".AsSpan()));
            Assert.False(KeyAnalyzer.IsAllAscii("abcdéfghij".AsSpan()));
        }
    }
}
