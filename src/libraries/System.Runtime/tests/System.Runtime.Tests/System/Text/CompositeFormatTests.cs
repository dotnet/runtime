// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public partial class CompositeFormatTests
    {
        [Fact]
        public void NullArgument_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("format", () => CompositeFormat.Parse(null));

            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, 0, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, (ReadOnlySpan<object>)new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => string.Format(null, (CompositeFormat)null, null));
            AssertExtensions.Throws<ArgumentNullException>("args", () => string.Format(null, CompositeFormat.Parse("abc"), null));

            var sb = new StringBuilder();
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, 0, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, (ReadOnlySpan<object>)new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => sb.AppendFormat(null, (CompositeFormat)null, null));
            AssertExtensions.Throws<ArgumentNullException>("args", () => sb.AppendFormat(null, CompositeFormat.Parse("abc"), null));

            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, (ReadOnlySpan<object>)new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("format", () => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, null));
            AssertExtensions.Throws<ArgumentNullException>("args", () => Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("abc"), out _, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/57588", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void DebuggerDisplay_ShowsFormat()
        {
            string format = "abc {0} def {1}";
            CompositeFormat cf = CompositeFormat.Parse(format);

            Assert.NotNull(cf);
            Assert.Equal(format, cf.Format);

            Assert.Equal($"\"{format}\"", DebuggerAttributes.ValidateDebuggerDisplayReferences(cf));
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("testing 123", 0)]
        [InlineData("testing {{123}}", 0)]
        [InlineData("{0}", 1)]
        [InlineData("{0} {1}", 2)]
        [InlineData("{2}", 3)]
        [InlineData("{2} {0}", 3)]
        [InlineData("{1} {34} {3}", 35)]
        public static void MinimumArgumentCount_MatchesExpectedValue(string format, int expected)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);

            Assert.Equal(expected, cf.MinimumArgumentCount);

            string s = string.Format(null, cf, Enumerable.Repeat((object)"arg", expected).ToArray());
            Assert.NotNull(s);

            if (expected != 0)
            {
                Assert.Throws<FormatException>(() => string.Format(null, cf, Enumerable.Repeat((object)"arg", expected - 1).ToArray()));
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Valid_TestData), MemberType = typeof(System.Tests.StringTests))]
        public static void StringFormat_Valid(IFormatProvider provider, string format, object[] values, string expected)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);
            Assert.Same(format, cf.Format);

            Assert.Equal(expected, string.Format(provider, cf, values));

            Assert.Equal(expected, string.Format(provider, cf, (ReadOnlySpan<object?>)values));

            switch (values.Length)
            {
                case 1:
                    Assert.Equal(expected, string.Format(provider, cf, values[0]));
                    break;

                case 2:
                    Assert.Equal(expected, string.Format(provider, cf, values[0], values[1]));
                    break;

                case 3:
                    Assert.Equal(expected, string.Format(provider, cf, values[0], values[1], values[2]));
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Valid_TestData), MemberType = typeof(System.Tests.StringTests))]
        public static void StringBuilderAppendFormat_Valid(IFormatProvider provider, string format, object[] values, string expected)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);
            Assert.Same(format, cf.Format);

            var sb = new StringBuilder();

            Assert.Same(sb, sb.AppendFormat(provider, cf, values));
            Assert.Equal(expected, sb.ToString());

            Assert.Same(sb, sb.AppendFormat(provider, cf, (ReadOnlySpan<object?>)values));
            Assert.Equal(expected + expected, sb.ToString());

            sb.Clear();
            switch (values.Length)
            {
                case 1:
                    Assert.Same(sb, sb.AppendFormat(provider, cf, values[0]));
                    Assert.Equal(expected, sb.ToString());
                    break;

                case 2:
                    Assert.Same(sb, sb.AppendFormat(provider, cf, values[0], values[1]));
                    Assert.Equal(expected, sb.ToString());
                    break;

                case 3:
                    Assert.Same(sb, sb.AppendFormat(provider, cf, values[0], values[1], values[2]));
                    Assert.Equal(expected, sb.ToString());
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Valid_TestData), MemberType = typeof(System.Tests.StringTests))]
        public static void MemoryExtensionsTryWrite_Valid(IFormatProvider provider, string format, object[] values, string expected)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);
            Assert.Same(format, cf.Format);

            char[] dest = new char[expected.Length];
            int charsWritten;

            Assert.True(dest.AsSpan().TryWrite(provider, cf, out charsWritten, values));
            Assert.Equal(expected.Length, charsWritten);
            AssertExtensions.SequenceEqual(expected.AsSpan(), dest.AsSpan(0, charsWritten));

            dest.AsSpan().Clear();
            Assert.True(dest.AsSpan().TryWrite(provider, cf, out charsWritten, (ReadOnlySpan<object?>)values));
            Assert.Equal(expected.Length, charsWritten);
            AssertExtensions.SequenceEqual(expected.AsSpan(), dest.AsSpan(0, charsWritten));

            dest.AsSpan().Clear();
            switch (values.Length)
            {
                case 1:
                    Assert.True(dest.AsSpan().TryWrite(provider, cf, out charsWritten, values[0]));
                    Assert.Equal(expected.Length, charsWritten);
                    AssertExtensions.SequenceEqual(expected.AsSpan(), dest.AsSpan(0, charsWritten));
                    break;

                case 2:
                    Assert.True(dest.AsSpan().TryWrite(provider, cf, out charsWritten, values[0], values[1]));
                    Assert.Equal(expected.Length, charsWritten);
                    AssertExtensions.SequenceEqual(expected.AsSpan(), dest.AsSpan(0, charsWritten));
                    break;

                case 3:
                    Assert.True(dest.AsSpan().TryWrite(provider, cf, out charsWritten, values[0], values[1], values[2]));
                    Assert.Equal(expected.Length, charsWritten);
                    AssertExtensions.SequenceEqual(expected.AsSpan(), dest.AsSpan(0, charsWritten));
                    break;
            }

            if (expected.Length > 0)
            {
                dest = new char[expected.Length - 1];
                Assert.False(dest.AsSpan().TryWrite(provider, cf, out charsWritten, values));
                Assert.Equal(0, charsWritten);
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Invalid_FormatExceptionFromFormat_MemberData), MemberType = typeof(System.Tests.StringTests))]
        public static void Parse_Invalid_FormatExceptionFromFormat(IFormatProvider provider, string format, object[] args)
        {
            _ = provider;
            _ = args;

            Assert.Throws<FormatException>(() => CompositeFormat.Parse(format));
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Invalid_FormatExceptionFromArgs_MemberData), MemberType = typeof(System.Tests.StringTests))]
        public static void StringFormat_Invalid_FormatExceptionFromArgs(IFormatProvider provider, string format, object[] args)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);

            Assert.Throws<FormatException>(() => string.Format(provider, cf, args));
            switch (args.Length)
            {
                case 1:
                    Assert.Throws<FormatException>(() => string.Format(provider, cf, args[0]));
                    break;
                case 2:
                    Assert.Throws<FormatException>(() => string.Format(provider, cf, args[0], args[1]));
                    break;
                case 3:
                    Assert.Throws<FormatException>(() => string.Format(provider, cf, args[0], args[1], args[2]));
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Invalid_FormatExceptionFromArgs_MemberData), MemberType = typeof(System.Tests.StringTests))]
        public static void StringBuilderAppendFormat_Invalid_FormatExceptionFromArgs(IFormatProvider provider, string format, object[] args)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);

            var sb = new StringBuilder();

            Assert.Throws<FormatException>(() => sb.AppendFormat(provider, cf, args));
            switch (args.Length)
            {
                case 1:
                    Assert.Throws<FormatException>(() => sb.AppendFormat(provider, cf, args[0]));
                    break;
                case 2:
                    Assert.Throws<FormatException>(() => sb.AppendFormat(provider, cf, args[0], args[1]));
                    break;
                case 3:
                    Assert.Throws<FormatException>(() => sb.AppendFormat(provider, cf, args[0], args[1], args[2]));
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(System.Tests.StringTests.Format_Invalid_FormatExceptionFromArgs_MemberData), MemberType = typeof(System.Tests.StringTests))]
        public static void MemoryExtensionsTryWrite_Invalid_FormatExceptionFromArgs(IFormatProvider provider, string format, object[] args)
        {
            CompositeFormat cf = CompositeFormat.Parse(format);
            Assert.NotNull(cf);

            char[] dest = new char[1024];

            Assert.Throws<FormatException>(() => new Span<char>(dest).TryWrite(provider, cf, out _, args));
            switch (args.Length)
            {
                case 1:
                    Assert.Throws<FormatException>(() => new Span<char>(dest).TryWrite(provider, cf, out _, args[0]));
                    break;
                case 2:
                    Assert.Throws<FormatException>(() => new Span<char>(dest).TryWrite(provider, cf, out _, args[0], args[1]));
                    break;
                case 3:
                    Assert.Throws<FormatException>(() => new Span<char>(dest).TryWrite(provider, cf, out _, args[0], args[1], args[2]));
                    break;
            }
        }
    }
}
