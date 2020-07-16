// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexConstructorTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { "foo", RegexOptions.None, Regex.InfiniteMatchTimeout };
            yield return new object[] { "foo", RegexOptions.RightToLeft, Regex.InfiniteMatchTimeout };
            yield return new object[] { "foo", RegexOptions.Compiled, Regex.InfiniteMatchTimeout };
            yield return new object[] { "foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout };
            yield return new object[] { "foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled, Regex.InfiniteMatchTimeout };
            yield return new object[] { "foo", RegexOptions.None, new TimeSpan(1) };
            yield return new object[] { "foo", RegexOptions.None, TimeSpan.FromMilliseconds(int.MaxValue - 1) };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public static void Ctor(string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            if (matchTimeout == Regex.InfiniteMatchTimeout)
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

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void CtorDebugInvoke(RegexOptions options)
        {
            Regex r;

            r = new Regex("[abc]def(ghi|jkl)", options | (RegexOptions)0x80 /*RegexOptions.Debug*/);
            Assert.False(r.Match("a").Success);
            Assert.True(r.Match("adefghi").Success);
            Assert.Equal("123456789", r.Replace("123adefghi789", "456"));

            r = new Regex("(ghi|jkl)*ghi", options | (RegexOptions)0x80 /*RegexOptions.Debug*/);
            Assert.False(r.Match("jkl").Success);
            Assert.True(r.Match("ghi").Success);
            Assert.Equal("123456789", r.Replace("123ghi789", "456"));

            r = new Regex("(ghi|jkl)*ghi", options | (RegexOptions)0x80 /*RegexOptions.Debug*/, TimeSpan.FromDays(1));
            Assert.False(r.Match("jkl").Success);
            Assert.True(r.Match("ghi").Success);
            Assert.Equal("123456789", r.Replace("123ghi789", "456"));
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void StaticCtor_InvalidTimeoutObject_ExceptionThrown()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, true);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void StaticCtor_InvalidTimeoutRange_ExceptionThrown()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.Zero);
                Assert.Throws<TypeInitializationException>(() => Regex.InfiniteMatchTimeout);
            }).Dispose();
        }

        [Fact]
        public void InitializeReferences_OnlyInvokedOnce()
        {
            var r = new DerivedRegex();
            r.InitializeReferences();
            Assert.Throws<NotSupportedException>(() => r.InitializeReferences());
        }

        [Fact]
        public void Ctor_CapNames_ReturnsDefaultValues()
        {
            var r = new DerivedRegex(@"(?<Name>\w*)");

            Assert.Null(r.Caps);

            IDictionary capNames = r.CapNames;
            Assert.NotNull(capNames);
            Assert.Same(capNames, r.CapNames);
            Assert.True(capNames.Contains("Name"));

            AssertExtensions.Throws<ArgumentNullException>("value", () => r.Caps = null);
            AssertExtensions.Throws<ArgumentNullException>("value", () => r.CapNames = null);

            r.Caps = new Dictionary<string, string>();
            Assert.IsType<Hashtable>(r.Caps);

            r.CapNames = new Dictionary<string, string>();
            Assert.IsType<Hashtable>(r.CapNames);

            var newHashtable = new Hashtable();

            r.CapNames = newHashtable;
            Assert.Same(newHashtable, r.CapNames);

            r.Caps = newHashtable;
            Assert.Same(newHashtable, r.Caps);
        }

        private sealed class DerivedRegex : Regex
        {
            public DerivedRegex() { }
            public DerivedRegex(string pattern) : base(pattern) { }

            public new void InitializeReferences() => base.InitializeReferences();

            public new IDictionary Caps { get => base.Caps; set => base.Caps = value; }
            public new IDictionary CapNames { get => base.CapNames; set => base.CapNames = value; }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Serialization_ThrowsNotSupported()
        {
            var r = new SerializableDerivedRegex();
            Assert.Throws<PlatformNotSupportedException>(() => new SerializableDerivedRegex(default, default));
            Assert.Throws<PlatformNotSupportedException>(() => ((ISerializable)r).GetObjectData(default, default));
        }

        [Serializable]
        private sealed class SerializableDerivedRegex : Regex
        {
            public SerializableDerivedRegex() : base("") { }
            public SerializableDerivedRegex(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Ctor_PatternInName()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Just make sure setting the environment variable doesn't cause problems.
                Environment.SetEnvironmentVariable("DOTNET_SYSTEM_TEXT_REGULAREXPRESSIONS_PATTERNINNAME", "1");

                // Short pattern
                var r = new Regex("abc", RegexOptions.Compiled);
                Assert.True(r.IsMatch("123abc456"));

                // Long pattern
                string pattern = string.Concat(Enumerable.Repeat("1234567890", 20));
                r = new Regex(pattern, RegexOptions.Compiled);
                Assert.True(r.IsMatch("abc" + pattern + "abc"));
            }).Dispose();
        }
    }
}
