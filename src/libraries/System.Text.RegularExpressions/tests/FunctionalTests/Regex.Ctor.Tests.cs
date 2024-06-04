// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexConstructorTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { "foo", RegexHelpers.RegexOptionNonBacktracking, Regex.InfiniteMatchTimeout };
                yield return new object[] { "foo", RegexHelpers.RegexOptionNonBacktracking, new TimeSpan(1) };
            }
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

        public static IEnumerable<object[]> NoneCompiledBacktracking()
        {
            yield return new object[] { RegexOptions.None };
            yield return new object[] { RegexOptions.Compiled };
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public void CtorDebugInvoke(RegexOptions options)
        {
            Regex r;

            r = new Regex("[abc]def(ghi|jkl)", options | RegexHelpers.RegexOptionDebug);
            Assert.False(r.Match("a").Success);
            Assert.True(r.Match("adefghi").Success);
            string repl = r.Replace("123adefghi78bdefjkl9", "###");
            Assert.Equal("123###78###9", repl);

            r = new Regex("(ghi|jkl)*ghi", options | RegexHelpers.RegexOptionDebug);
            Assert.False(r.Match("jkl").Success);
            Assert.True(r.Match("ghi").Success);
            Assert.Equal("123456789", r.Replace("123ghi789", "456"));

            r = new Regex("(ghi|jkl)*ghi", options | RegexHelpers.RegexOptionDebug, TimeSpan.FromDays(1));
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

            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)0x800));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", (RegexOptions)0x800, new TimeSpan()));
            if (PlatformDetection.IsNetFramework)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexHelpers.RegexOptionNonBacktracking));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexHelpers.RegexOptionNonBacktracking, new TimeSpan()));
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.RightToLeft));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Singleline));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.ECMAScript | RegexHelpers.RegexOptionNonBacktracking));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new Regex("foo", RegexOptions.RightToLeft | RegexHelpers.RegexOptionNonBacktracking));

            // MatchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, new TimeSpan(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => new Regex("foo", RegexOptions.None, TimeSpan.FromMilliseconds(int.MaxValue)));

            if (PlatformDetection.IsNetCore)
            {
                // Unsupported pattern constructs with specific options
                Assert.Throws<NotSupportedException>(() => new Regex("(?=a)", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and positive lookaheads
                Assert.Throws<NotSupportedException>(() => new Regex("(?!a)", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and negative lookaheads
                Assert.Throws<NotSupportedException>(() => new Regex("(?<=a)", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and positive lookbehinds
                Assert.Throws<NotSupportedException>(() => new Regex("(?<!a)", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and negative lookbehinds
                Assert.Throws<NotSupportedException>(() => new Regex(@"(\w)\1", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and backreferences
                Assert.Throws<NotSupportedException>(() => new Regex(@"(?(0)ab)", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and backreference conditionals
                Assert.Throws<NotSupportedException>(() => new Regex(@"([ab])\1", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and expression conditionals
                Assert.Throws<NotSupportedException>(() => new Regex(@"(?>a*)a", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and atomics
                Assert.Throws<NotSupportedException>(() => new Regex(@"\Ga", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and start anchors
                Assert.Throws<NotSupportedException>(() => new Regex(@"(?<C>A)(?<-C>B)$", RegexHelpers.RegexOptionNonBacktracking)); // NonBacktracking and balancing groups
                Assert.Throws<NotSupportedException>(() => new Regex(@"\w{1,1001}", RegexHelpers.RegexOptionNonBacktracking)); // Potentially large automata expansion
            }
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
            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<NotSupportedException>(() => r.InitializeReferences());
            }
            else
            {
                // As of .NET 7, this method is a nop.
                r.InitializeReferences();
            }
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

#pragma warning disable SYSLIB0052 // Type or member is obsolete
            public new void InitializeReferences() => base.InitializeReferences();
#pragma warning restore SYSLIB0052 // Type or member is obsolete

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
