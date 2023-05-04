// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public partial class Utf8Tests
    {
        private readonly byte[] _buffer = new byte[4096];

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(42, 84)]
        [InlineData(-1, 0)]
        [InlineData(-1, -1)]
        [InlineData(-16, 1)]
        public void LengthAndHoleArguments_Valid(int literalLength, int formattedCount)
        {
            bool shouldAppend;

            new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[Math.Max(0, literalLength)], out shouldAppend);
            Assert.True(shouldAppend);

            new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[1 + Math.Max(0, literalLength)], out shouldAppend);
            Assert.True(shouldAppend);

            if (literalLength > 0)
            {
                new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[literalLength - 1], out shouldAppend);
                Assert.False(shouldAppend);
            }

            foreach (IFormatProvider provider in new IFormatProvider[] { null, new ConcatFormatter(), CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, new CultureInfo("en-US"), new CultureInfo("fr-FR") })
            {
                new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[Math.Max(0, literalLength)], out shouldAppend);
                Assert.True(shouldAppend);

                new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[1 + Math.Max(0, literalLength)], out shouldAppend);
                Assert.True(shouldAppend);

                if (literalLength > 0)
                {
                    new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new byte[literalLength - 1], out shouldAppend);
                    Assert.False(shouldAppend);
                }
            }
        }

        [Fact]
        public void AppendLiteral()
        {
            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a long string", "!" })
            {
                expected.Append(s);
                actual.AppendLiteral(s);
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_ReadOnlySpanChar()
        {
            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a longer string", "!" })
            {
                // span
                expected.Append(s);
                actual.AppendFormatted((ReadOnlySpan<char>)s);

                // span, format
                expected.AppendFormat("{0:X2}", s);
                actual.AppendFormatted((ReadOnlySpan<char>)s, format: "X2");

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // span, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", s);
                    actual.AppendFormatted((ReadOnlySpan<char>)s, alignment);

                    // span, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", s);
                    actual.AppendFormatted((ReadOnlySpan<char>)s, alignment, "X2");
                }
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_ReadOnlySpanByte()
        {
            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a longer string", "!" })
            {
                ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes(s);

                // span
                expected.Append(s);
                actual.AppendFormatted(utf8);

                // span, format
                expected.AppendFormat("{0:X2}", s);
                actual.AppendFormatted(utf8, format: "X2");

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // span, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", s);
                    actual.AppendFormatted(utf8, alignment);

                    // span, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", s);
                    actual.AppendFormatted(utf8, alignment, "X2");
                }
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_String()
        {
            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

            foreach (string s in new[] { null, "", "a", "bc", "def", "this is a longer string", "!" })
            {
                // string
                expected.AppendFormat("{0}", s);
                actual.AppendFormatted(s);

                // string, format
                expected.AppendFormat("{0:X2}", s);
                actual.AppendFormatted(s, "X2");

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // string, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", s);
                    actual.AppendFormatted(s, alignment);

                    // string, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", s);
                    actual.AppendFormatted(s, alignment, "X2");
                }
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_String_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, provider, out _);

            foreach (string s in new[] { null, "", "a" })
            {
                // string
                expected.AppendFormat(provider, "{0}", s);
                actual.AppendFormatted(s);

                // string, format
                expected.AppendFormat(provider, "{0:X2}", s);
                actual.AppendFormatted(s, "X2");

                // string, alignment
                expected.AppendFormat(provider, "{0,3}", s);
                actual.AppendFormatted(s, 3);

                // string, alignment, format
                expected.AppendFormat(provider, "{0,-3:X2}", s);
                actual.AppendFormatted(s, -3, "X2");
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes()
        {
            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

            foreach (string rawInput in new[] { null, "", "a", "bc", "def", "this is a longer string", "!" })
            {
                foreach (object o in new object[]
                {
                    rawInput, // raw string directly; ToString will return itself
                    new StringWrapper(rawInput), // wrapper object that returns string from ToString
                    new FormattableStringWrapper(rawInput), // IFormattable wrapper around string
                    new SpanFormattableStringWrapper(rawInput) // ISpanFormattable wrapper around string
                })
                {
                    // object
                    expected.AppendFormat("{0}", o);
                    actual.AppendFormatted(o);
                    if (o is IHasToStringState tss1)
                    {
                        Assert.True(string.IsNullOrEmpty(tss1.ToStringState.LastFormat));
                        AssertModeMatchesType(tss1);
                    }

                    // object, format
                    expected.AppendFormat("{0:X2}", o);
                    actual.AppendFormatted(o, "X2");
                    if (o is IHasToStringState tss2)
                    {
                        Assert.Equal("X2", tss2.ToStringState.LastFormat);
                        AssertModeMatchesType(tss2);
                    }

                    foreach (int alignment in new[] { 0, 3, -3 })
                    {
                        // object, alignment
                        expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", o);
                        actual.AppendFormatted(o, alignment);
                        if (o is IHasToStringState tss3)
                        {
                            Assert.True(string.IsNullOrEmpty(tss3.ToStringState.LastFormat));
                            AssertModeMatchesType(tss3);
                        }

                        // object, alignment, format
                        expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", o);
                        actual.AppendFormatted(o, alignment, "X2");
                        if (o is IHasToStringState tss4)
                        {
                            Assert.Equal("X2", tss4.ToStringState.LastFormat);
                            AssertModeMatchesType(tss4);
                        }
                    }
                }
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes_CreateProviderFlowed()
        {
            var provider = new CultureInfo("en-US");
            Utf8.TryWriteInterpolatedStringHandler handler = new Utf8.TryWriteInterpolatedStringHandler(1, 2, _buffer, provider, out _);

            foreach (IHasToStringState tss in new IHasToStringState[] { new FormattableStringWrapper("hello"), new  SpanFormattableStringWrapper("hello") })
            {
                handler.AppendFormatted(tss);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                handler.AppendFormatted(tss, 1);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                handler.AppendFormatted(tss, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);

                handler.AppendFormatted(tss, 1, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);
            }
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, provider, out _);

            foreach (string s in new[] { null, "", "a" })
            {
                foreach (IHasToStringState tss in new IHasToStringState[] { new FormattableStringWrapper(s), new  SpanFormattableStringWrapper(s) })
                {
                    void AssertTss(IHasToStringState tss, string format)
                    {
                        Assert.Equal(format, tss.ToStringState.LastFormat);
                        Assert.Same(provider, tss.ToStringState.LastProvider);
                        Assert.Equal(ToStringMode.ICustomFormatterFormat, tss.ToStringState.ToStringMode);
                    }

                    // object
                    expected.AppendFormat(provider, "{0}", tss);
                    actual.AppendFormatted(tss);
                    AssertTss(tss, null);

                    // object, format
                    expected.AppendFormat(provider, "{0:X2}", tss);
                    actual.AppendFormatted(tss, "X2");
                    AssertTss(tss, "X2");

                    // object, alignment
                    expected.AppendFormat(provider, "{0,3}", tss);
                    actual.AppendFormatted(tss, 3);
                    AssertTss(tss, null);

                    // object, alignment, format
                    expected.AppendFormat(provider, "{0,-3:X2}", tss);
                    actual.AppendFormatted(tss, -3, "X2");
                    AssertTss(tss, "X2");
                }
            }

            Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
            Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void AppendFormatted_ValueTypes()
        {
            void Test<T>(T t)
            {
                var expected = new StringBuilder();
                var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, out _);

                // struct
                expected.AppendFormat("{0}", t);
                actual.AppendFormatted(t);
                Assert.True(string.IsNullOrEmpty(((IHasToStringState)t).ToStringState.LastFormat));
                AssertModeMatchesType(((IHasToStringState)t));

                // struct, format
                expected.AppendFormat("{0:X2}", t);
                actual.AppendFormatted(t, "X2");
                Assert.Equal("X2", ((IHasToStringState)t).ToStringState.LastFormat);
                AssertModeMatchesType(((IHasToStringState)t));

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // struct, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", t);
                    actual.AppendFormatted(t, alignment);
                    Assert.True(string.IsNullOrEmpty(((IHasToStringState)t).ToStringState.LastFormat));
                    AssertModeMatchesType(((IHasToStringState)t));

                    // struct, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", t);
                    actual.AppendFormatted(t, alignment, "X2");
                    Assert.Equal("X2", ((IHasToStringState)t).ToStringState.LastFormat);
                    AssertModeMatchesType(((IHasToStringState)t));
                }

                Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
                Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Fact]
        public void AppendFormatted_ValueTypes_CreateProviderFlowed()
        {
            void Test<T>(T t)
            {
                var provider = new CultureInfo("en-US");
                Utf8.TryWriteInterpolatedStringHandler handler = new Utf8.TryWriteInterpolatedStringHandler(1, 2, _buffer, provider, out _);

                handler.AppendFormatted(t);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                handler.AppendFormatted(t, 1);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                handler.AppendFormatted(t, "X2");
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                handler.AppendFormatted(t, 1, "X2");
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Fact]
        public void AppendFormatted_ValueTypes_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            void Test<T>(T t)
            {
                void AssertTss(T tss, string format)
                {
                    Assert.Equal(format, ((IHasToStringState)tss).ToStringState.LastFormat);
                    Assert.Same(provider, ((IHasToStringState)tss).ToStringState.LastProvider);
                    Assert.Equal(ToStringMode.ICustomFormatterFormat, ((IHasToStringState)tss).ToStringState.ToStringMode);
                }

                var expected = new StringBuilder();
                var actual = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer, provider, out _);

                // struct
                expected.AppendFormat(provider, "{0}", t);
                actual.AppendFormatted(t);
                AssertTss(t, null);

                // struct, format
                expected.AppendFormat(provider, "{0:X2}", t);
                actual.AppendFormatted(t, "X2");
                AssertTss(t, "X2");

                // struct, alignment
                expected.AppendFormat(provider, "{0,3}", t);
                actual.AppendFormatted(t, 3);
                AssertTss(t, null);

                // struct, alignment, format
                expected.AppendFormat(provider, "{0,-3:X2}", t);
                actual.AppendFormatted(t, -3, "X2");
                AssertTss(t, "X2");

                Assert.True(Utf8.TryWrite(_buffer, ref actual, out int bytesWritten));
                Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Fact]
        public void AppendFormatted_EmptyBuffer_ZeroLengthWritesSuccessful()
        {
            Utf8.TryWriteInterpolatedStringHandler b = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer.AsSpan(0, 0), out bool shouldAppend);
            Assert.True(shouldAppend);

            Assert.True(b.AppendLiteral(""));
            Assert.True(b.AppendFormatted((object)"", alignment: 0, format: "X2"));
            Assert.True(b.AppendFormatted((string?)null));
            Assert.True(b.AppendFormatted(""));
            Assert.True(b.AppendFormatted("", alignment: 0, format: "X2"));
            Assert.True(b.AppendFormatted<string>(""));
            Assert.True(b.AppendFormatted<string>("", alignment: 0));
            Assert.True(b.AppendFormatted<string>("", format: "X2"));
            Assert.True(b.AppendFormatted<string>("", alignment: 0, format: "X2"));
            Assert.True(b.AppendFormatted("".AsSpan()));
            Assert.True(b.AppendFormatted("".AsSpan(), alignment: 0, format: "X2"));

            Assert.True(Utf8.TryWrite(_buffer.AsSpan(0, 0), ref b, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void AppendFormatted_BufferTooSmall(int bufferLength)
        {
            Span<byte> buffer = _buffer.AsSpan(0, bufferLength);

            for (int i = 0; i <= 31; i++)
            {
                Utf8.TryWriteInterpolatedStringHandler b = new Utf8.TryWriteInterpolatedStringHandler(0, 0, buffer, out bool shouldAppend);
                Assert.True(shouldAppend);

                Assert.True(b.AppendLiteral(new string('s', bufferLength)));

                bool result = i switch
                {
                    0 => b.AppendLiteral(" "),
                    1 => b.AppendFormatted((object)" ", alignment: 0, format: "X2"),
                    2 => b.AppendFormatted(" "),
                    3 => b.AppendFormatted(" ", alignment: 0, format: "X2"),
                    4 => b.AppendFormatted<string>(" "),
                    5 => b.AppendFormatted<string>(" ", alignment: 0),
                    6 => b.AppendFormatted<string>(" ", format: "X2"),
                    7 => b.AppendFormatted<string>(" ", alignment: 0, format: "X2"),
                    8 => b.AppendFormatted(" ".AsSpan()),
                    9 => b.AppendFormatted(" ".AsSpan(), alignment: 0, format: "X2"),
                    10 => b.AppendFormatted(new FormattableStringWrapper(" ")),
                    11 => b.AppendFormatted(new FormattableStringWrapper(" "), alignment: 0),
                    12 => b.AppendFormatted(new FormattableStringWrapper(" "), format: "X2"),
                    13 => b.AppendFormatted(new FormattableStringWrapper(" "), alignment: 0, format: "X2"),
                    14 => b.AppendFormatted(new SpanFormattableStringWrapper(" ")),
                    15 => b.AppendFormatted(new SpanFormattableStringWrapper(" "), alignment: 0),
                    16 => b.AppendFormatted(new SpanFormattableStringWrapper(" "), format: "X2"),
                    17 => b.AppendFormatted(new SpanFormattableStringWrapper(" "), alignment: 0, format: "X2"),
                    18 => b.AppendFormatted(new FormattableInt32Wrapper(1)),
                    19 => b.AppendFormatted(new FormattableInt32Wrapper(1), alignment: 0),
                    20 => b.AppendFormatted(new FormattableInt32Wrapper(1), format: "X2"),
                    21 => b.AppendFormatted(new FormattableInt32Wrapper(1), alignment: 0, format: "X2"),
                    22 => b.AppendFormatted(new SpanFormattableInt32Wrapper(1)),
                    23 => b.AppendFormatted(new SpanFormattableInt32Wrapper(1), alignment: 0),
                    24 => b.AppendFormatted(new SpanFormattableInt32Wrapper(1), format: "X2"),
                    25 => b.AppendFormatted(new SpanFormattableInt32Wrapper(1), alignment: 0, format: "X2"),
                    26 => b.AppendFormatted<string>("", alignment: 1),
                    27 => b.AppendFormatted<string>("", alignment: -1),
                    28 => b.AppendFormatted<string>(" ", alignment: 1, format: "X2"),
                    29 => b.AppendFormatted<string>(" ", alignment: -1, format: "X2"),
                    30 => b.AppendFormatted(" "u8),
                    31 => b.AppendFormatted(" "u8, alignment: 0, format: "X2"),
                    _ => throw new Exception(),
                };
                Assert.False(result);

                Assert.False(Utf8.TryWrite(buffer, ref b, out int bytesWritten));
                Assert.Equal(0, bytesWritten);
            }
        }

        [Fact]
        public void AppendFormatted_BufferTooSmall_CustomFormatter()
        {
            var provider = new ConstFormatter(" ");

            Utf8.TryWriteInterpolatedStringHandler b = new Utf8.TryWriteInterpolatedStringHandler(0, 0, _buffer.AsSpan(0, 0), provider, out bool shouldAppend);
            Assert.True(shouldAppend);

            // don't use custom formatter
            Assert.True(b.AppendLiteral(""));
            Assert.True(b.AppendFormatted("".AsSpan()));
            Assert.True(b.AppendFormatted("".AsSpan(), alignment: 0, format: "X2"));
            Assert.True(b.AppendFormatted(""u8));
            Assert.True(b.AppendFormatted(""u8, alignment: 0, format: "X2"));

            // do use custom formatter
            Assert.False(b.AppendFormatted((object)"", alignment: 0, format: "X2"));
            Assert.False(b.AppendFormatted((string?)null));
            Assert.False(b.AppendFormatted(""));
            Assert.False(b.AppendFormatted("", alignment: 0, format: "X2"));
            Assert.False(b.AppendFormatted<string>(""));
            Assert.False(b.AppendFormatted<string>("", alignment: 0));
            Assert.False(b.AppendFormatted<string>("", format: "X2"));
            Assert.False(b.AppendFormatted<string>("", alignment: 0, format: "X2"));

            Assert.False(Utf8.TryWrite(_buffer.AsSpan(0, 0), ref b, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void UseInterpolationSyntax_ResultMatchesExpected()
        {
            int int32Value = 1024;
            bool boolValue = true;
            Version versionValue = Version.Parse("1.2.3.4");

            bool formatted = Utf8.TryWrite(_buffer, $"Hello {int32Value,8:X4}  {boolValue}   {versionValue}", out int bytesWritten);
            Assert.True(formatted);
            Assert.Equal("Hello     0400  True   1.2.3.4", Encoding.UTF8.GetString(_buffer.AsSpan(0, bytesWritten)));
        }

        private static void AssertModeMatchesType<T>(T tss) where T : IHasToStringState
        {
            ToStringMode expected =
                tss is ISpanFormattable ? ToStringMode.ISpanFormattableTryFormat :
                tss is IFormattable ? ToStringMode.IFormattableToString :
                ToStringMode.ObjectToString;
            Assert.Equal(expected, tss.ToStringState.ToStringMode);
        }

        private struct Utf8SpanFormattableInt32Wrapper : IUtf8SpanFormattable, IHasToStringState
        {
            private readonly int _value;
            public ToStringState ToStringState { get; }

            public Utf8SpanFormattableInt32Wrapper(int value)
            {
                ToStringState = new ToStringState();
                _value = value;
            }

            public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                ToStringState.LastFormat = format.ToString();
                ToStringState.LastProvider = provider;
                ToStringState.ToStringMode = ToStringMode.ISpanFormattableTryFormat;

                ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes(_value.ToString(format.ToString(), provider)).AsSpan();
                if (src.TryCopyTo(utf8Destination))
                {
                    bytesWritten = src.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                ToStringState.LastFormat = format;
                ToStringState.LastProvider = formatProvider;
                ToStringState.ToStringMode = ToStringMode.IFormattableToString;
                return _value.ToString(format, formatProvider);
            }

            public override string ToString()
            {
                ToStringState.LastFormat = null;
                ToStringState.LastProvider = null;
                ToStringState.ToStringMode = ToStringMode.ObjectToString;
                return _value.ToString();
            }
        }

        private sealed class SpanFormattableStringWrapper : IFormattable, ISpanFormattable, IHasToStringState
        {
            private readonly string _value;
            public ToStringState ToStringState { get; } = new ToStringState();

            public SpanFormattableStringWrapper(string value) => _value = value;

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                ToStringState.LastFormat = format.ToString();
                ToStringState.LastProvider = provider;
                ToStringState.ToStringMode = ToStringMode.ISpanFormattableTryFormat;

                if (_value is null)
                {
                    charsWritten = 0;
                    return true;
                }

                if (_value.Length > destination.Length)
                {
                    charsWritten = 0;
                    return false;
                }

                charsWritten = _value.Length;
                _value.AsSpan().CopyTo(destination);
                return true;
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                ToStringState.LastFormat = format;
                ToStringState.LastProvider = formatProvider;
                ToStringState.ToStringMode = ToStringMode.IFormattableToString;
                return _value;
            }

            public override string ToString()
            {
                ToStringState.LastFormat = null;
                ToStringState.LastProvider = null;
                ToStringState.ToStringMode = ToStringMode.ObjectToString;
                return _value;
            }
        }

        private struct SpanFormattableInt32Wrapper : IFormattable, ISpanFormattable, IHasToStringState
        {
            private readonly int _value;
            public ToStringState ToStringState { get; }

            public SpanFormattableInt32Wrapper(int value)
            {
                ToStringState = new ToStringState();
                _value = value;
            }

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                ToStringState.LastFormat = format.ToString();
                ToStringState.LastProvider = provider;
                ToStringState.ToStringMode = ToStringMode.ISpanFormattableTryFormat;

                return _value.TryFormat(destination, out charsWritten, format, provider);
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                ToStringState.LastFormat = format;
                ToStringState.LastProvider = formatProvider;
                ToStringState.ToStringMode = ToStringMode.IFormattableToString;
                return _value.ToString(format, formatProvider);
            }

            public override string ToString()
            {
                ToStringState.LastFormat = null;
                ToStringState.LastProvider = null;
                ToStringState.ToStringMode = ToStringMode.ObjectToString;
                return _value.ToString();
            }
        }

        private sealed class FormattableStringWrapper : IFormattable, IHasToStringState
        {
            private readonly string _value;
            public ToStringState ToStringState { get; } = new ToStringState();

            public FormattableStringWrapper(string s) => _value = s;

            public string ToString(string format, IFormatProvider formatProvider)
            {
                ToStringState.LastFormat = format;
                ToStringState.LastProvider = formatProvider;
                ToStringState.ToStringMode = ToStringMode.IFormattableToString;
                return _value;
            }

            public override string ToString()
            {
                ToStringState.LastFormat = null;
                ToStringState.LastProvider = null;
                ToStringState.ToStringMode = ToStringMode.ObjectToString;
                return _value;
            }
        }

        private struct FormattableInt32Wrapper : IFormattable, IHasToStringState
        {
            private readonly int _value;
            public ToStringState ToStringState { get; }

            public FormattableInt32Wrapper(int i)
            {
                ToStringState = new ToStringState();
                _value = i;
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                ToStringState.LastFormat = format;
                ToStringState.LastProvider = formatProvider;
                ToStringState.ToStringMode = ToStringMode.IFormattableToString;
                return _value.ToString(format, formatProvider);
            }

            public override string ToString()
            {
                ToStringState.LastFormat = null;
                ToStringState.LastProvider = null;
                ToStringState.ToStringMode = ToStringMode.ObjectToString;
                return _value.ToString();
            }
        }

        private sealed class ToStringState
        {
            public string LastFormat { get; set; }
            public IFormatProvider LastProvider { get; set; }
            public ToStringMode ToStringMode { get; set; }
        }

        private interface IHasToStringState
        {
            ToStringState ToStringState { get; }
        }

        private enum ToStringMode
        {
            ObjectToString,
            IFormattableToString,
            ISpanFormattableTryFormat,
            ICustomFormatterFormat,
        }

        private sealed class StringWrapper
        {
            private readonly string _value;

            public StringWrapper(string s) => _value = s;

            public override string ToString() => _value;
        }

        private sealed class ConcatFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                string s = format + " " + arg + formatProvider;

                if (arg is IHasToStringState tss)
                {
                    // Set after using arg.ToString() in concat above
                    tss.ToStringState.LastFormat = format;
                    tss.ToStringState.LastProvider = formatProvider;
                    tss.ToStringState.ToStringMode = ToStringMode.ICustomFormatterFormat;
                }

                return s;
            }
        }

        private sealed class ConstFormatter : IFormatProvider, ICustomFormatter
        {
            private readonly string _value;

            public ConstFormatter(string value) => _value = value;

            public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;

            public string Format(string format, object arg, IFormatProvider formatProvider) => _value;
        }
    }
}
