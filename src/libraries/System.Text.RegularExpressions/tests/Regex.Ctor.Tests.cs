// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexConstructorTests
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
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, true);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void StaticCtor_InvalidTimeoutRange_ExceptionThrown()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.Zero);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }
    }
}
