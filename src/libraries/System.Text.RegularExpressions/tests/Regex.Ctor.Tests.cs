// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexConstructorTests : RemoteExecutorTestBase
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { "foo", RegexOptions.None, Timeout.InfiniteTimeSpan };
            yield return new object[] { "foo", RegexOptions.RightToLeft, Timeout.InfiniteTimeSpan };
            yield return new object[] { "foo", RegexOptions.Compiled, Timeout.InfiniteTimeSpan };
            yield return new object[] { "foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, Timeout.InfiniteTimeSpan };
            yield return new object[] { "foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout.InfiniteTimeSpan };
            yield return new object[] { "foo", RegexOptions.None, new TimeSpan(1) };
            yield return new object[] { "foo", RegexOptions.None, TimeSpan.FromMilliseconds(int.MaxValue - 1) };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public static void Ctor(string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            if (matchTimeout == Timeout.InfiniteTimeSpan)
            {
                if (options == RegexOptions.None)
                {
                    Regex regex1 = new Regex(pattern);
                    Assert.Equal(pattern, regex1.ToString());
                    Assert.Equal(options, regex1.Options);
                    Assert.False(regex1.RightToLeft);
                    Assert.Equal(matchTimeout, regex1.MatchTimeout);
                }
                Regex regex2 = new Regex(pattern, options);
                Assert.Equal(pattern, regex2.ToString());
                Assert.Equal(options, regex2.Options);
                Assert.Equal((options & RegexOptions.RightToLeft) != 0, regex2.RightToLeft);
                Assert.Equal(matchTimeout, regex2.MatchTimeout);
            }
            Regex regex3 = new Regex(pattern, options, matchTimeout);
            Assert.Equal(pattern, regex3.ToString());
            Assert.Equal(options, regex3.Options);
            Assert.Equal((options & RegexOptions.RightToLeft) != 0, regex3.RightToLeft);
            Assert.Equal(matchTimeout, regex3.MatchTimeout);
        }

        [Fact]
        public static void Ctor_Invalid()
        {
            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => new Regex(null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => new Regex(null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => new Regex(null, RegexOptions.None, new TimeSpan()));

            // Options are invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)(-1), new TimeSpan()));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)0x400));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)0x400, new TimeSpan()));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.RightToLeft));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Singleline));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace));

            // MatchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, new TimeSpan(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, TimeSpan.FromMilliseconds(int.MaxValue)));
        }

        [Fact]
        public static void StaticCtor_InvalidTimeoutObject_ExceptionThrown()
        {
            RemoteInvoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, true);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);

                return SuccessExitCode;
            });
        }

        [Fact]
        public static void StaticCtor_InvalidTimeoutRange_ExceptionThrown()
        {
            RemoteInvoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.Zero);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);

                return SuccessExitCode;
            });
        }

        [Fact]
        public void CacheSize_Get()
        {
            Assert.Equal(15, Regex.CacheSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        public void CacheSize_Set(int newCacheSize)
        {
            int originalCacheSize = Regex.CacheSize;

            Regex.CacheSize = newCacheSize;
            Assert.Equal(newCacheSize, Regex.CacheSize);

            Regex.CacheSize = originalCacheSize;
        }

        [Fact]
        public void CacheSize_Set_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => Regex.CacheSize = -1);
        }

        [Theory]
        // \d, \D, \s, \S, \w, \W, \P, \p inside character range
        [InlineData(@"cat([a-\d]*)dog", RegexOptions.None)]
        [InlineData(@"([5-\D]*)dog", RegexOptions.None)]
        [InlineData(@"cat([6-\s]*)dog", RegexOptions.None)]
        [InlineData(@"cat([c-\S]*)", RegexOptions.None)]
        [InlineData(@"cat([7-\w]*)", RegexOptions.None)]
        [InlineData(@"cat([a-\W]*)dog", RegexOptions.None)]
        [InlineData(@"([f-\p{Lu}]\w*)\s([\p{Lu}]\w*)", RegexOptions.None)]
        [InlineData(@"([1-\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", RegexOptions.None)]
        [InlineData(@"[\p]", RegexOptions.None)]
        [InlineData(@"[\P]", RegexOptions.None)]
        [InlineData(@"([\pcat])", RegexOptions.None)]
        [InlineData(@"([\Pcat])", RegexOptions.None)]
        [InlineData(@"(\p{", RegexOptions.None)]
        [InlineData(@"(\p{Ll", RegexOptions.None)]
        // \x, \u, \a, \b, \e, \f, \n, \r, \t, \v, \c, inside character range
        [InlineData(@"(cat)([\o]*)(dog)", RegexOptions.None)]
        // Use < in a group
        [InlineData(@"cat(?<0>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<1dog>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<dog)_*>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<dog!>)_*>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<dog >)_*>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<dog<>)_*>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<>dog)", RegexOptions.None)]
        [InlineData(@"cat(?<->dog)", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-16>dog)", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-1uosn>dog)", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-catdog>dog)", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-()*!@>dog)", RegexOptions.None)]
        // Use (? in a group
        [InlineData("cat(?(?#COMMENT)cat)", RegexOptions.None)]
        [InlineData("cat(?(?'cat'cat)dog)", RegexOptions.None)]
        [InlineData("cat(?(?<cat>cat)dog)", RegexOptions.None)]
        [InlineData("cat(?(?afdcat)dog)", RegexOptions.None)]
        // Pattern whitespace
        [InlineData(@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace", RegexOptions.IgnorePatternWhitespace)]
        [InlineData(@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace", RegexOptions.None)]
        // Back reference
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\kcat", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\k<cat2>", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\k<8>cat", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\k<8>cat", RegexOptions.ECMAScript)]
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\k8", RegexOptions.None)]
        [InlineData(@"(?<cat>cat)\s+(?<dog>dog)\k8", RegexOptions.ECMAScript)]
        // Octal, decimal
        [InlineData(@"(cat)(\7)", RegexOptions.None)]
        [InlineData(@"(cat)\s+(?<2147483648>dog)", RegexOptions.None)]
        [InlineData(@"(cat)\s+(?<21474836481097>dog)", RegexOptions.None)]
        // Scan control
        [InlineData(@"(cat)(\c*)(dog)", RegexOptions.None)]
        [InlineData(@"(cat)\c", RegexOptions.None)]
        [InlineData(@"(cat)(\c *)(dog)", RegexOptions.None)]
        [InlineData(@"(cat)(\c?*)(dog)", RegexOptions.None)]
        [InlineData("(cat)(\\c\0*)(dog)", RegexOptions.None)]
        [InlineData(@"(cat)(\c`*)(dog)", RegexOptions.None)]
        [InlineData(@"(cat)(\c\|*)(dog)", RegexOptions.None)]
        [InlineData(@"(cat)(\c\[*)(dog)", RegexOptions.None)]
        // Nested quantifiers
        [InlineData("^[abcd]{0,16}*$", RegexOptions.None)]
        [InlineData("^[abcd]{1,}*$", RegexOptions.None)]
        [InlineData("^[abcd]{1}*$", RegexOptions.None)]
        [InlineData("^[abcd]{0,16}?*$", RegexOptions.None)]
        [InlineData("^[abcd]{1,}?*$", RegexOptions.None)]
        [InlineData("^[abcd]{1}?*$", RegexOptions.None)]
        [InlineData("^[abcd]*+$", RegexOptions.None)]
        [InlineData("^[abcd]+*$", RegexOptions.None)]
        [InlineData("^[abcd]?*$", RegexOptions.None)]
        [InlineData("^[abcd]*?+$", RegexOptions.None)]
        [InlineData("^[abcd]+?*$", RegexOptions.None)]
        [InlineData("^[abcd]??*$", RegexOptions.None)]
        [InlineData("^[abcd]*{0,5}$", RegexOptions.None)]
        [InlineData("^[abcd]+{0,5}$", RegexOptions.None)]
        [InlineData("^[abcd]?{0,5}$", RegexOptions.None)]
        // Invalid character escapes
        [InlineData(@"\u", RegexOptions.None)]
        [InlineData(@"\ua", RegexOptions.None)]
        [InlineData(@"\u0", RegexOptions.None)]
        [InlineData(@"\x", RegexOptions.None)]
        [InlineData(@"\x2", RegexOptions.None)]
        // Invalid character class
        [InlineData("[", RegexOptions.None)]
        [InlineData("[]", RegexOptions.None)]
        [InlineData("[a", RegexOptions.None)]
        [InlineData("[^", RegexOptions.None)]
        [InlineData("[cat", RegexOptions.None)]
        [InlineData("[^cat", RegexOptions.None)]
        [InlineData("[a-", RegexOptions.None)]
        [InlineData(@"\p{", RegexOptions.None)]
        [InlineData(@"\p{cat", RegexOptions.None)]
        [InlineData(@"\p{cat}", RegexOptions.None)]
        [InlineData(@"\P{", RegexOptions.None)]
        [InlineData(@"\P{cat", RegexOptions.None)]
        [InlineData(@"\P{cat}", RegexOptions.None)]
        // Invalid grouping constructs
        [InlineData("(", RegexOptions.None)]
        [InlineData("(?", RegexOptions.None)]
        [InlineData("(?<", RegexOptions.None)]
        [InlineData("(?<cat>", RegexOptions.None)]
        [InlineData("(?'", RegexOptions.None)]
        [InlineData("(?'cat'", RegexOptions.None)]
        [InlineData("(?:", RegexOptions.None)]
        [InlineData("(?imn", RegexOptions.None)]
        [InlineData("(?imn )", RegexOptions.None)]
        [InlineData("(?=", RegexOptions.None)]
        [InlineData("(?!", RegexOptions.None)]
        [InlineData("(?<=", RegexOptions.None)]
        [InlineData("(?<!", RegexOptions.None)]
        [InlineData("(?>", RegexOptions.None)]
        [InlineData("(?)", RegexOptions.None)]
        [InlineData("(?<)", RegexOptions.None)]
        [InlineData("(?')", RegexOptions.None)]
        [InlineData(@"\1", RegexOptions.None)]
        [InlineData(@"\1", RegexOptions.None)]
        [InlineData(@"\k", RegexOptions.None)]
        [InlineData(@"\k<", RegexOptions.None)]
        [InlineData(@"\k<1", RegexOptions.None)]
        [InlineData(@"\k<cat", RegexOptions.None)]
        [InlineData(@"\k<>", RegexOptions.None)]
        // Invalid alternation constructs
        [InlineData("(?(", RegexOptions.None)]
        [InlineData("(?()|", RegexOptions.None)]
        [InlineData("(?(cat", RegexOptions.None)]
        [InlineData("(?(cat)|", RegexOptions.None)]
        // Regex with 0 numeric names
        [InlineData("foo(?<0>bar)", RegexOptions.None)]
        [InlineData("foo(?'0'bar)", RegexOptions.None)]
        // Regex without closing >
        [InlineData("foo(?<1bar)", RegexOptions.None)]
        [InlineData("foo(?'1bar)", RegexOptions.None)]
        // Misc
        [InlineData(@"\p{klsak", RegexOptions.None)]
        [InlineData("(?r:cat)", RegexOptions.None)]
        [InlineData("(?c:cat)", RegexOptions.None)]
        [InlineData("(??e:cat)", RegexOptions.None)]
        // Character class subtraction
        [InlineData("[a-f-[]]+", RegexOptions.None)]
        // Not character class substraction
        [InlineData("[A-[]+", RegexOptions.None)]
        // Invalid testgroup
        [InlineData("(?(?e))", RegexOptions.None)]
        [InlineData("(?(?a)", RegexOptions.None)]
        [InlineData("(?(?", RegexOptions.None)]
        [InlineData("(?(", RegexOptions.None)]
        [InlineData("?(a:b)", RegexOptions.None)]
        [InlineData("?(a)", RegexOptions.None)]
        [InlineData("?(a|b)", RegexOptions.None)]
        [InlineData("?((a)", RegexOptions.None)]
        [InlineData("?((a)a", RegexOptions.None)]
        [InlineData("?((a)a|", RegexOptions.None)]
        [InlineData("?((a)a|b", RegexOptions.None)]
        public void Ctor_InvalidPattern(string pattern, RegexOptions options)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Regex(pattern, options));
        }

        [Theory]
        // Testgroup with options
        [InlineData("(?(?i))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?I))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?m))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?M))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?s))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?S))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?x))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?X))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?n))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData("(?(?N))", RegexOptions.None, typeof(NullReferenceException))]
        [InlineData(" (?(?n))", RegexOptions.None, typeof(OutOfMemoryException))]
        public void Ctor_InvalidPattern(string pattern, RegexOptions options, Type exceptionType)
        {
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Throws(exceptionType, () => new Regex(pattern, options));
            }
            else
            {
                Ctor_InvalidPattern(pattern, options);
            }
        }
    }
}
