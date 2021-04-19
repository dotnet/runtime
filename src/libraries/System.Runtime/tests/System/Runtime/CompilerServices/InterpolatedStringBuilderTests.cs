// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class InterpolatedStringBuilderTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(42, 84)]
        [InlineData(-1, 0)]
        [InlineData(-1, -1)]
        [InlineData(-16, 1)]
        public void LengthAndHoleArguments_Valid(int literalLength, int formattedCount)
        {
            InterpolatedStringBuilder.Create(literalLength, formattedCount);

            Span<char> scratch1 = stackalloc char[1];
            foreach (IFormatProvider provider in new IFormatProvider[] { null, new ConcatFormatter(), CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, new CultureInfo("en-US"), new CultureInfo("fr-FR") })
            {
                InterpolatedStringBuilder.Create(literalLength, formattedCount, provider);

                InterpolatedStringBuilder.Create(literalLength, formattedCount, provider, default);
                InterpolatedStringBuilder.Create(literalLength, formattedCount, provider, scratch1);
                InterpolatedStringBuilder.Create(literalLength, formattedCount, provider, Array.Empty<char>());
                InterpolatedStringBuilder.Create(literalLength, formattedCount, provider, new char[256]);
            }

            InterpolatedStringBuilder.Create(literalLength, formattedCount, Span<char>.Empty);
            InterpolatedStringBuilder.Create(literalLength, formattedCount, scratch1);
            InterpolatedStringBuilder.Create(literalLength, formattedCount, Array.Empty<char>());
            InterpolatedStringBuilder.Create(literalLength, formattedCount, new char[256]);
        }

        [Fact]
        public void ToString_DoesntClear()
        {
            InterpolatedStringBuilder builder = InterpolatedStringBuilder.Create(0, 0);
            builder.AppendLiteral("hi");
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal("hi", builder.ToString());
            }
            Assert.Equal("hi", builder.ToStringAndClear());
        }

        [Fact]
        public void ToStringAndClear_Clears()
        {
            InterpolatedStringBuilder builder = InterpolatedStringBuilder.Create(0, 0);
            builder.AppendLiteral("hi");
            Assert.Equal("hi", builder.ToStringAndClear());
            Assert.Equal(string.Empty, builder.ToStringAndClear());
        }

        [Fact]
        public void AppendLiteral()
        {
            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0);

            foreach (string s in new[] { "", "a", "bc", "def", "this is a long string", "!" })
            {
                expected.Append(s);
                actual.AppendLiteral(s);
            }

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Fact]
        public void AppendFormatted_ReadOnlySpanChar()
        {
            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0);

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

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Fact]
        public void AppendFormatted_String()
        {
            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0);

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

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Fact]
        public void AppendFormatted_String_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0, provider);

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

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes()
        {
            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0);

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
                    actual.AppendFormatted(o,  "X2");
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

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AppendFormatted_ReferenceTypes_CreateProviderFlowed(bool useScratch)
        {
            var provider = new CultureInfo("en-US");
            InterpolatedStringBuilder builder = useScratch ?
                InterpolatedStringBuilder.Create(1, 2, provider, stackalloc char[16]) :
                InterpolatedStringBuilder.Create(1, 2, provider);

            foreach (IHasToStringState tss in new IHasToStringState[] { new FormattableStringWrapper("hello"), new SpanFormattableStringWrapper("hello") })
            {
                builder.AppendFormatted(tss);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                builder.AppendFormatted(tss, 1);
                Assert.Same(provider, tss.ToStringState.LastProvider);

                builder.AppendFormatted(tss, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);

                builder.AppendFormatted(tss, 1, "X2");
                Assert.Same(provider, tss.ToStringState.LastProvider);
            }
        }

        [Fact]
        public void AppendFormatted_ReferenceTypes_ICustomFormatter()
        {
            var provider = new ConcatFormatter();

            var expected = new StringBuilder();
            InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0, provider);

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

            Assert.Equal(expected.ToString(), actual.ToStringAndClear());
        }

        [Fact]
        public void AppendFormatted_ValueTypes()
        {
            void Test<T>(T t)
            {
                var expected = new StringBuilder();
                InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0);

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

                Assert.Equal(expected.ToString(), actual.ToStringAndClear());
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AppendFormatted_ValueTypes_CreateProviderFlowed(bool useScratch)
        {
            void Test<T>(T t)
            {
                var provider = new CultureInfo("en-US");
                InterpolatedStringBuilder builder = useScratch ?
                    InterpolatedStringBuilder.Create(1, 2, provider, stackalloc char[16]) :
                    InterpolatedStringBuilder.Create(1, 2, provider);

                builder.AppendFormatted(t);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                builder.AppendFormatted(t, 1);
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                builder.AppendFormatted(t, "X2");
                Assert.Same(provider, ((IHasToStringState)t).ToStringState.LastProvider);

                builder.AppendFormatted(t, 1, "X2");
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
                InterpolatedStringBuilder actual = InterpolatedStringBuilder.Create(0, 0, provider);

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

                Assert.Equal(expected.ToString(), actual.ToStringAndClear());
            }

            Test(new FormattableInt32Wrapper(42));
            Test(new SpanFormattableInt32Wrapper(84));
            Test((FormattableInt32Wrapper?)new FormattableInt32Wrapper(42));
            Test((SpanFormattableInt32Wrapper?)new SpanFormattableInt32Wrapper(84));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Grow_Large(bool useScratch)
        {
            var expected = new StringBuilder();
            InterpolatedStringBuilder builder = useScratch ?
                InterpolatedStringBuilder.Create(3, 1000, null, stackalloc char[16]) :
                InterpolatedStringBuilder.Create(3, 1000);

            for (int i = 0; i < 1000; i++)
            {
                builder.AppendFormatted(i);
                expected.Append(i);

                builder.AppendFormatted(i, 3);
                expected.AppendFormat("{0,3}", i);
            }

            Assert.Equal(expected.ToString(), builder.ToStringAndClear());
        }

        private static void AssertModeMatchesType<T>(T tss) where T : IHasToStringState
        {
            ToStringMode expected =
                tss is ISpanFormattable ? ToStringMode.ISpanFormattableTryFormat :
                tss is IFormattable ? ToStringMode.IFormattableToString :
                ToStringMode.ObjectToString;
            Assert.Equal(expected, tss.ToStringState.ToStringMode);
        }

        private sealed class SpanFormattableStringWrapper : ISpanFormattable, IHasToStringState
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

        private struct SpanFormattableInt32Wrapper : ISpanFormattable, IHasToStringState
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
    }
}
