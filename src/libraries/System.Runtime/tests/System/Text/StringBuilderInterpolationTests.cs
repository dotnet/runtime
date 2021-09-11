// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.Tests
{
    public class StringBuilderInterpolationTests
    {
        [Fact]
        public void Append_Nop()
        {
            var sb = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(1, 2, sb);

            Assert.Same(sb, sb.Append(ref iab));
            Assert.Same(sb, sb.Append(CultureInfo.InvariantCulture, ref iab));

            Assert.Equal(0, sb.Length);
        }

        [Fact]
        public void AppendLine_AppendsNewLine()
        {
            var sb = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(1, 2, sb);

            Assert.Same(sb, sb.AppendLine(ref iab));
            Assert.Same(sb, sb.AppendLine(CultureInfo.InvariantCulture, ref iab));

            Assert.Equal(Environment.NewLine + Environment.NewLine, sb.ToString());
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(42, 84)]
        [InlineData(-1, 0)]
        [InlineData(-1, -1)]
        [InlineData(-16, 1)]
        public void LengthAndHoleArguments_Valid(int baseLength, int holeCount)
        {
            var sb = new StringBuilder();

            new StringBuilder.AppendInterpolatedStringHandler(baseLength, holeCount, sb);

            foreach (IFormatProvider provider in new IFormatProvider[] { null, new ConcatFormatter(), CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, new CultureInfo("en-US"), new CultureInfo("fr-FR") })
            {
                new StringBuilder.AppendInterpolatedStringHandler(baseLength, holeCount, sb, provider);
            }
        }

        [Fact]
        public void AppendLiteral()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a long string", "!" })
            {
                expected.Append(s);
                iab.AppendLiteral(s);
            }

            actual.Append(ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_ReadOnlySpanChar()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a longer string", "!" })
            {
                // span
                expected.Append(s);
                iab.AppendFormatted((ReadOnlySpan<char>)s);

                // span, format
                expected.AppendFormat("{0:X2}", s);
                iab.AppendFormatted((ReadOnlySpan<char>)s, format: "X2");

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // span, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", s);
                    iab.AppendFormatted((ReadOnlySpan<char>)s, alignment);

                    // span, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", s);
                    iab.AppendFormatted((ReadOnlySpan<char>)s, alignment, "X2");
                }
            }

            actual.Append(ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_String()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual);

            foreach (string s in new[] { null, "", "a", "bc", "def", "this is a longer string", "!" })
            {
                // string
                expected.AppendFormat("{0}", s);
                iab.AppendFormatted(s);

                // string, format
                expected.AppendFormat("{0:X2}", s);
                iab.AppendFormatted(s, "X2");

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // string, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", s);
                    iab.AppendFormatted(s, alignment);

                    // string, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", s);
                    iab.AppendFormatted(s, alignment, "X2");
                }
            }

            actual.Append(ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_String_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual, provider);

            foreach (string s in new[] { null, "", "a" })
            {
                // string
                expected.AppendFormat(provider, "{0}", s);
                iab.AppendFormatted(s);

                // string, format
                expected.AppendFormat(provider, "{0:X2}", s);
                iab.AppendFormatted(s, "X2");

                // string, alignment
                expected.AppendFormat(provider, "{0,3}", s);
                iab.AppendFormatted(s, 3);

                // string, alignment, format
                expected.AppendFormat(provider, "{0,-3:X2}", s);
                iab.AppendFormatted(s, -3, "X2");
            }

            actual.Append(provider, ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual);

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
                    iab.AppendFormatted(o);
                    if (o is IHasToStringState tss1)
                    {
                        Assert.True(string.IsNullOrEmpty(tss1.ToStringState.LastFormat));
                        AssertModeMatchesType(tss1);
                    }

                    // object, format
                    expected.AppendFormat("{0:X2}", o);
                    iab.AppendFormatted(o, "X2");
                    if (o is IHasToStringState tss2)
                    {
                        Assert.Equal("X2", tss2.ToStringState.LastFormat);
                        AssertModeMatchesType(tss2);
                    }

                    foreach (int alignment in new[] { 0, 3, -3 })
                    {
                        // object, alignment
                        expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", o);
                        iab.AppendFormatted(o, alignment);
                        if (o is IHasToStringState tss3)
                        {
                            Assert.True(string.IsNullOrEmpty(tss3.ToStringState.LastFormat));
                            AssertModeMatchesType(tss3);
                        }

                        // object, alignment, format
                        expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", o);
                        iab.AppendFormatted(o, alignment, "X2");
                        if (o is IHasToStringState tss4)
                        {
                            Assert.Equal("X2", tss4.ToStringState.LastFormat);
                            AssertModeMatchesType(tss4);
                        }
                    }
                }
            }

            actual.Append(ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes_CreateProviderFlowed()
        {
            var provider = new CultureInfo("en-US");
            var sb = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(1, 2, sb, provider);

            foreach (IHasToStringState tss in new IHasToStringState[] { new FormattableStringWrapper("hello"), new SpanFormattableStringWrapper("hello") })
            {
                iab.AppendFormatted(tss);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                iab.AppendFormatted(tss, 1);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                iab.AppendFormatted(tss, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);

                iab.AppendFormatted(tss, 1, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);
            }

            sb.Append(ref iab);
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            var actual = new StringBuilder();
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual, provider);

            foreach (string s in new[] { null, "", "a" })
            {
                foreach (IHasToStringState tss in new IHasToStringState[] { new FormattableStringWrapper(s), new SpanFormattableStringWrapper(s) })
                {
                    void AssertTss(IHasToStringState tss, string format)
                    {
                        Assert.Equal(format, tss.ToStringState.LastFormat);
                        Assert.Same(provider, tss.ToStringState.LastProvider);
                        Assert.Equal(ToStringMode.ICustomFormatterFormat, tss.ToStringState.ToStringMode);
                    }

                    // object
                    expected.AppendFormat(provider, "{0}", tss);
                    iab.AppendFormatted(tss);
                    AssertTss(tss, null);

                    // object, format
                    expected.AppendFormat(provider, "{0:X2}", tss);
                    iab.AppendFormatted(tss, "X2");
                    AssertTss(tss, "X2");

                    // object, alignment
                    expected.AppendFormat(provider, "{0,3}", tss);
                    iab.AppendFormatted(tss, 3);
                    AssertTss(tss, null);

                    // object, alignment, format
                    expected.AppendFormat(provider, "{0,-3:X2}", tss);
                    iab.AppendFormatted(tss, -3, "X2");
                    AssertTss(tss, "X2");
                }
            }

            actual.Append(provider, ref iab);

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public void AppendFormatted_ValueTypes()
        {
            void Test<T>(T t)
            {
                var expected = new StringBuilder();
                var actual = new StringBuilder();
                StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual);

                // struct
                expected.AppendFormat("{0}", t);
                iab.AppendFormatted(t);
                Assert.True(string.IsNullOrEmpty(((IHasToStringState)t).ToStringState.LastFormat));
                AssertModeMatchesType(((IHasToStringState)t));

                // struct, format
                expected.AppendFormat("{0:X2}", t);
                iab.AppendFormatted(t, "X2");
                Assert.Equal("X2", ((IHasToStringState)t).ToStringState.LastFormat);
                AssertModeMatchesType(((IHasToStringState)t));

                foreach (int alignment in new[] { 0, 3, -3 })
                {
                    // struct, alignment
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + "}", t);
                    iab.AppendFormatted(t, alignment);
                    Assert.True(string.IsNullOrEmpty(((IHasToStringState)t).ToStringState.LastFormat));
                    AssertModeMatchesType(((IHasToStringState)t));

                    // struct, alignment, format
                    expected.AppendFormat("{0," + alignment.ToString(CultureInfo.InvariantCulture) + ":X2}", t);
                    iab.AppendFormatted(t, alignment, "X2");
                    Assert.Equal("X2", ((IHasToStringState)t).ToStringState.LastFormat);
                    AssertModeMatchesType(((IHasToStringState)t));
                }

                actual.Append(ref iab);

                Assert.Equal(expected.ToString(), actual.ToString());
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
                var sb = new StringBuilder();
                StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(1, 2, sb, provider);

                iab.AppendFormatted(t);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                iab.AppendFormatted(t, 1);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                iab.AppendFormatted(t, "X2");
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                iab.AppendFormatted(t, 1, "X2");
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                sb.Append(ref iab);
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
                var actual = new StringBuilder();
                StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, actual, provider);

                // struct
                expected.AppendFormat(provider, "{0}", t);
                iab.AppendFormatted(t);
                AssertTss(t, null);

                // struct, format
                expected.AppendFormat(provider, "{0:X2}", t);
                iab.AppendFormatted(t, "X2");
                AssertTss(t, "X2");

                // struct, alignment
                expected.AppendFormat(provider, "{0,3}", t);
                iab.AppendFormatted(t, 3);
                AssertTss(t, null);

                // struct, alignment, format
                expected.AppendFormat(provider, "{0,-3:X2}", t);
                iab.AppendFormatted(t, -3, "X2");
                AssertTss(t, "X2");

                Assert.Equal(expected.ToString(), actual.ToString());

                actual.Append(ref iab);
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AppendFormatted_InvalidTryFormatCharsWritten_Throws(bool tooBig) // vs tooSmall
        {
            StringBuilder.AppendInterpolatedStringHandler iab = new StringBuilder.AppendInterpolatedStringHandler(0, 0, new StringBuilder());
            Assert.Throws<FormatException>(() => iab.AppendFormatted(new InvalidCharsWritten(tooBig)));
        }

        private static void AssertModeMatchesType<T>(T tss) where T : IHasToStringState
        {
            ToStringMode expected =
                tss is ISpanFormattable ? ToStringMode.ISpanFormattableTryFormat :
                tss is IFormattable ? ToStringMode.IFormattableToString :
                ToStringMode.ObjectToString;
            Assert.Equal(expected, tss.ToStringState.ToStringMode);
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

        private sealed class InvalidCharsWritten : ISpanFormattable
        {
            private bool _tooBig;

            public InvalidCharsWritten(bool tooBig) => _tooBig = tooBig;

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                charsWritten = _tooBig ? destination.Length + 1 : -1;
                return true;
            }

            public string ToString(string format, IFormatProvider formatProvider) =>
                throw new NotImplementedException();
        }
    }
}
