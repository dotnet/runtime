// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.CodeDom.Tests
{
    public class IndentedTextWriterTests
    {
        //  A StringWriter that remembers the name of the most recently-called write method.
        private sealed class IndicatingTextWriter : StringWriter
        {
            public string LastCalledMethod { get; private set; }

            public override void Write(bool value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(char value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                base.Write(buffer, index, count);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(char[] buffer)
            {
                base.Write(buffer);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(decimal value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(double value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(float value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(int value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(long value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(object value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(string format, object arg0)
            {
                base.Write(format, arg0);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(string format, object arg0, object arg1)
            {
                base.Write(format, arg0, arg1);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(string format, object arg0, object arg1, object arg2)
            {
                base.Write(format, arg0, arg1, arg2);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(string format, params object[] arg)
            {
                base.Write(format, arg);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(string value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(uint value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void Write(ulong value)
            {
                base.Write(value);
                LastCalledMethod = nameof(Write);
            }

            public override void WriteLine()
            {
                base.WriteLine();
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(bool value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(char value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(char[] buffer, int index, int count)
            {
                base.WriteLine(buffer, index, count);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(char[] buffer)
            {
                base.WriteLine(buffer);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(decimal value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(double value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(float value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(int value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(long value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(object value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(string format, object arg0)
            {
                base.WriteLine(format, arg0);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(string format, object arg0, object arg1)
            {
                base.WriteLine(format, arg0, arg1);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(string format, object arg0, object arg1, object arg2)
            {
                base.WriteLine(format, arg0, arg1, arg2);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(string format, params object[] arg)
            {
                base.WriteLine(format, arg);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(uint value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void WriteLine(ulong value)
            {
                base.WriteLine(value);
                LastCalledMethod = nameof(WriteLine);
            }

            public override void Flush()
            {
                base.Flush();
                LastCalledMethod = nameof(Flush);
            }

            public override Task FlushAsync()
            {
                Task result = base.FlushAsync();
                LastCalledMethod = nameof(FlushAsync);

                return result;
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                Task result = base.FlushAsync();
                LastCalledMethod = nameof(FlushAsync) + "Cancelable";

                return result;
            }

            public override Task WriteAsync(char value)
            {
                Task result = base.WriteAsync(value);
                LastCalledMethod = nameof(WriteAsync);

                return result;
            }

            public override Task WriteAsync(char[] buffer, int index, int count)
            {
                Task result = base.WriteAsync(buffer, index, count);
                LastCalledMethod = nameof(WriteAsync);

                return result;
            }

            public override Task WriteAsync(string value)
            {
                Task result = base.WriteAsync(value);
                LastCalledMethod = nameof(WriteAsync);

                return result;
            }

            public override Task WriteLineAsync()
            {
                Task result = base.WriteLineAsync();
                LastCalledMethod = nameof(WriteLineAsync);

                return result;
            }

            public override Task WriteLineAsync(char value)
            {
                Task result = base.WriteLineAsync(value);
                LastCalledMethod = nameof(WriteLineAsync);

                return result;
            }

            public override Task WriteLineAsync(char[] buffer, int index, int count)
            {
                Task result = base.WriteLineAsync(buffer, index, count);
                LastCalledMethod = nameof(WriteLineAsync);

                return result;
            }

            public override Task WriteLineAsync(string value)
            {
                Task result = base.WriteLineAsync(value);
                LastCalledMethod = nameof(WriteLineAsync);

                return result;
            }
        }

        [Fact]
        public static void Ctor_ExpectedDefaults()
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var itw = new IndentedTextWriter(sw);

            Assert.IsType<UnicodeEncoding>(itw.Encoding);
            Assert.Equal(0, itw.Indent);
            Assert.Same(sw, itw.InnerWriter);
            Assert.Equal(sw.NewLine, itw.NewLine);

            Assert.Equal("    ", IndentedTextWriter.DefaultTabString);
        }

        [Fact]
        public void Ctor_NullWriter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("writer", () => new IndentedTextWriter(null));
            Assert.Throws<ArgumentNullException>("writer", () => new IndentedTextWriter(null, "TabString"));
        }

        [Theory]
        [InlineData(42)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-4)]
        [InlineData(0)]
        [InlineData(8)]
        public static void Indent_RoundtripsAndAffectsOutput(int indent)
        {
            const string TabString = "\t\t";

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var itw = new IndentedTextWriter(sw, TabString);

            itw.Indent = indent;
            Assert.Equal(indent >= 0 ? indent : 0, itw.Indent);

            itw.WriteLine("first");
            itw.WriteLine("second");
            itw.WriteLine("third");

            string expectedTab = string.Concat(Enumerable.Repeat(TabString, itw.Indent));
            Assert.Equal(
                expectedTab + "first" + Environment.NewLine +
                expectedTab + "second" + Environment.NewLine +
                expectedTab + "third" + Environment.NewLine,
                sb.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("space")]
        [InlineData("    ")]
        public static void TabString_UsesProvidedString(string tabString)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            using (var itw = tabString == null ? new IndentedTextWriter(sw) : new IndentedTextWriter(sw, tabString))
            {
                itw.Indent = 1;
                if (tabString == null)
                {
                    tabString = IndentedTextWriter.DefaultTabString;
                }

                itw.WriteLine();
                itw.WriteLine("Should be indented");
                itw.Flush();

                Assert.Equal(tabString + itw.NewLine + tabString + "Should be indented" + itw.NewLine, sb.ToString());
                itw.Close();
            }
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("newline")]
        public static async Task Writes_ProducesExpectedOutput(string newline)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            var itw = new IndentedTextWriter(sw, "t");
            itw.Indent = 1;
            itw.NewLine = newline;
            itw.WriteLine();

            itw.Write(true);
            itw.Write('a');
            itw.Write(new char[] { 'b', 'c' });
            itw.Write(new char[] { 'd', 'e' }, 0, 2);
            itw.Write(4m);
            itw.Write(5.6);
            itw.Write(6.7f);
            itw.Write(8);
            itw.Write(9L);
            itw.Write((object)10);
            itw.Write("11");
            itw.Write(12u);
            itw.Write(13uL);
            itw.Write("{0}", 14);
            itw.Write("{0} {1}", 15, 16);
            itw.Write("{0} {1} {2}", 15, 16, 17);
            itw.Write("{0} {1} {2} {3}", 15, 16, 17, 18);

            itw.WriteLine(true);
            itw.WriteLine('a');
            itw.WriteLine(new char[] { 'b', 'c' });
            itw.WriteLine(new char[] { 'd', 'e' }, 0, 2);
            itw.WriteLine(4m);
            itw.WriteLine(5.6);
            itw.WriteLine(6.7f);
            itw.WriteLine(8);
            itw.WriteLine(9L);
            itw.WriteLine((object)10);
            itw.WriteLine("11");
            itw.WriteLine(12u);
            itw.WriteLine(13uL);
            itw.WriteLine("{0}", 14);
            itw.WriteLine("{0} {1}", 15, 16);
            itw.WriteLine("{0} {1} {2}", 15, 16, 17);
            itw.WriteLine("{0} {1} {2} {3}", 15, 16, 17, 18);

            await itw.WriteAsync('a');
            await itw.WriteAsync(new char[] { 'b', 'c' });
            await itw.WriteAsync(new char[] { 'd', 'e' }, 0, 2);
            await itw.WriteAsync("1");

            await itw.WriteLineAsync('a');
            await itw.WriteLineAsync(new char[] { 'b', 'c' });
            await itw.WriteLineAsync(new char[] { 'd', 'e' }, 0, 2);
            await itw.WriteLineAsync("1");

            itw.WriteLineNoTabs("notabs");

            Assert.Equal(
                "t" + newline +
                "tTrueabcde45.66.789101112131415 1615 16 1715 16 17 18True" + newline +
                "ta" + newline +
                "tbc" + newline +
                "tde" + newline +
                "t4" + newline +
                "t5.6" + newline +
                "t6.7" + newline +
                "t8" + newline +
                "t9" + newline +
                "t10" + newline +
                "t11" + newline +
                "t12" + newline +
                "t13" + newline +
                "t14" + newline +
                "t15 16" + newline +
                "t15 16 17" + newline +
                "t15 16 17 18" + newline +
                "tabcde1a" + newline +
                "tbc" + newline +
                "tde" + newline +
                "t1" + newline +
                "notabs" + newline,
                sb.ToString());
        }

        public static IEnumerable<object[]> Write_MemberData
        {
            get
            {
                object[] CreateParameters(Action<IndentedTextWriter> callWrite, string expected)
                {
                    return new object[] { callWrite, expected };
                }

                yield return CreateParameters(x => x.Write(true), true.ToString());
                yield return CreateParameters(x => x.Write('c'), "c");
                yield return CreateParameters(x => x.Write("Hello World".ToCharArray()), "Hello World");
                yield return CreateParameters(x => x.Write(1.234m), (1.234m).ToString(CultureInfo.InvariantCulture));
                yield return CreateParameters(x => x.Write(12345.0), (12345.0).ToString(CultureInfo.InvariantCulture));
                yield return CreateParameters(x => x.Write(12345.0f), (12345.0f).ToString(CultureInfo.InvariantCulture));
                yield return CreateParameters(x => x.Write(12345), (12345).ToString());
                yield return CreateParameters(x => x.Write(1234567890L), (1234567890L).ToString());
                yield return CreateParameters(x => x.Write(new object()), new object().ToString());
                yield return CreateParameters(x => x.Write("Hello World"), "Hello World");
                yield return CreateParameters(x => x.Write(0xDEADBEEF), (0xDEADBEEF).ToString());
                yield return CreateParameters(x => x.Write(0xDEADBEEFBAADF00DUL), (0xDEADBEEFBAADF00DUL).ToString());
                yield return CreateParameters(x => x.Write("Hello {0} World", "Digital"), "Hello Digital World");
                yield return CreateParameters(x => x.Write("Hello {0} World{1}", "Digital", "!!"), "Hello Digital World!!");
                yield return CreateParameters(x => x.Write("Hello {0} {1} World{2}", "Dot", "NET", "!!"), "Hello Dot NET World!!");
                yield return CreateParameters(x => x.Write("Hello {0} {1} {2} World{3}", "Digital", "Dot", "NET", "!!"), "Hello Digital Dot NET World!!");
                yield return CreateParameters(x => x.Write("Hello World".ToCharArray(), 6, 5), "World");
            }
        }

        public static IEnumerable<object[]> WriteLine_MemberData
        {
            get
            {
                object[] CreateParameters(Action<IndentedTextWriter> callWriteLine, string expected)
                {
                    return new object[] { callWriteLine, expected };
                }

                yield return CreateParameters(x => x.WriteLine(), NewLine);
                yield return CreateParameters(x => x.WriteLine(true), $"{true}{NewLine}");
                yield return CreateParameters(x => x.WriteLine('c'), $"c{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello World".ToCharArray()), $"Hello World{NewLine}");
                yield return CreateParameters(x => x.WriteLine(3.14159m), $"{(3.14159m).ToString(CultureInfo.InvariantCulture)}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(12345.0), $"{(12345.0).ToString(CultureInfo.InvariantCulture)}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(12345.0f), $"{(12345.0f).ToString(CultureInfo.InvariantCulture)}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(12345), $"{12345}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(0xDEADBEEFBADF00DL), $"{0xDEADBEEFBADF00DL}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(new object()), $"{new object()}{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello World"), $"Hello World{NewLine}");
                yield return CreateParameters(x => x.WriteLine(0xDEADBEEF), $"{0xDEADBEEF}{NewLine}");
                yield return CreateParameters(x => x.WriteLine(0xDEADBEEFBAADF00DUL), $"{0xDEADBEEFBAADF00DUL}{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello {0} World", "Digital"), $"Hello Digital World{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello {0} {1} World", "Dot", "NET"), $"Hello Dot NET World{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello {0} {1} World{2}", "Dot", "NET", "!!"), $"Hello Dot NET World!!{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello World".ToCharArray(), 6, 5), $"World{NewLine}");
                yield return CreateParameters(x => x.WriteLine("Hello {0} {1} {2} World{3}", "Digital", "Dot", "NET", "!!"), $"Hello Digital Dot NET World!!{NewLine}");
            }
        }

        public static IEnumerable<object[]> WriteAsync_MemberData
        {
            get
            {
                object[] CreateParameters(Func<IndentedTextWriter, Task> callWriteAsync, string expected)
                {
                    return new object[] { callWriteAsync, expected };
                }

                yield return CreateParameters(x => x.WriteAsync('c'), "c");
                yield return CreateParameters(x => x.WriteAsync("Hello World".ToCharArray(), 6, 5), "World");
                yield return CreateParameters(x => x.WriteAsync("Hello World"), "Hello World");
            }
        }

        public static IEnumerable<object[]> WriteLineAsync_MemberData
        {
            get
            {
                object[] CreateParameters(Func<IndentedTextWriter, Task> callWriteLineAsync, string expected)
                {
                    return new object[] { callWriteLineAsync, expected };
                }

                yield return CreateParameters(x => x.WriteLineAsync(), NewLine);
                yield return CreateParameters(x => x.WriteLineAsync('c'), $"c{NewLine}");
                yield return CreateParameters(x => x.WriteLineAsync("Hello World".ToCharArray(), 6, 5), $"World{NewLine}");
                yield return CreateParameters(x => x.WriteLineAsync("Hello World"), $"Hello World{NewLine}");
            }
        }

        private const string TabString = "   ";
        private const string NewLine = "\n";

        [Theory]
        [MemberData(nameof(WriteAsync_MemberData))]
        public async Task WriteAsync_WithoutIndents_CallsInnerWriteAsync(Func<IndentedTextWriter, Task> callWriteAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;

            await callWriteAsync(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteAsync), indicator.LastCalledMethod);
            Assert.Equal(expected, indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteAsync_MemberData))]
        public async Task WriteAsync_WithIndents_WritesTabsAfterWriteLineAsync(Func<IndentedTextWriter, Task> callWriteAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            string prefix = $"prefix";
            await itw.WriteLineAsync(prefix);
            await callWriteAsync(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteAsync), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{prefix}{NewLine}{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteAsync_MemberData))]
        public async Task WriteAsync_WithIndents_OmitsTabsAfterWriteAsync(Func<IndentedTextWriter, Task> callWriteAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            string prefix = "prefix";
            await itw.WriteAsync(prefix);
            await callWriteAsync(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteAsync), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{prefix}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLineAsync_MemberData))]
        public async Task WriteLineAsync_WithoutIndents_CallsInnerWriteLineAsync(Func<IndentedTextWriter, Task> callWriteLineAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            await callWriteLineAsync(itw);

            Assert.Equal(nameof(TextWriter.WriteLineAsync), indicator.LastCalledMethod);
            Assert.Equal(expected, indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLineAsync_MemberData))]
        public async Task WriteLineAsync_WithIndents(Func<IndentedTextWriter, Task> callWriteLineAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;
            await callWriteLineAsync(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteLineAsync), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLineAsync_MemberData))]
        public async Task WriteLineAsync_WithIndents_SubsequentLines_AreIndented(Func<IndentedTextWriter, Task> callWriteLineAsync, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            const string prefix = "prefix";
            await itw.WriteLineAsync(prefix);
            await callWriteLineAsync(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteLineAsync), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{prefix}{NewLine}{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Fact]
        public async Task ParameterlessWriteLineAsync_IndentsLinesAfterIndentIsSet()
        {
            var sw = new StringWriter();
            var itw = new IndentedTextWriter(sw, TabString);
            itw.NewLine = NewLine;
            await itw.WriteLineAsync("Wibble");
            await itw.WriteAsync("Wobble");
            itw.Indent++;
            await itw.WriteLineAsync();
            await itw.WriteLineAsync("Wooble");
            await itw.WriteLineAsync("Qwux");

            string expected = $"Wibble{NewLine}Wobble{NewLine}{TabString}Wooble{NewLine}{TabString}Qwux{NewLine}";

            Assert.Equal(expected, sw.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(Write_MemberData))]
        public void Write_WithoutIndents_CallsInnerWrite(Action<IndentedTextWriter> callWrite, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;

            callWrite(itw);

            Assert.Equal(nameof(IndentedTextWriter.Write), indicator.LastCalledMethod);
            Assert.Equal(expected, indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(Write_MemberData))]
        public void Write_WithIndents_FirstLine_IsIndented(Action<IndentedTextWriter> callWrite, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            callWrite(itw);

            Assert.Equal(nameof(IndentedTextWriter.Write), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(Write_MemberData))]
        public void Write_IsIndented(Action<IndentedTextWriter> callWrite, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            var prefix = "prefix";
            itw.WriteLine(prefix);
            callWrite(itw);

            Assert.Equal(nameof(IndentedTextWriter.Write), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{prefix}{NewLine}{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLine_MemberData))]
        public void WriteLine_CallsInnerWriteLine(Action<IndentedTextWriter> callWriteLine, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;

            callWriteLine(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteLine), indicator.LastCalledMethod);
            Assert.Equal(expected, indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLine_MemberData))]
        public void WriteLine_FirstLine_IsNotIndented(Action<IndentedTextWriter> callWriteLine, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            callWriteLine(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteLine), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Theory]
        [MemberData(nameof(WriteLine_MemberData))]
        public void WriteLine_IsIndented(Action<IndentedTextWriter> callWriteLine, string expected)
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator, TabString);
            itw.NewLine = NewLine;
            itw.Indent = 1;

            var prefix = "prefix";
            itw.WriteLine(prefix);
            callWriteLine(itw);

            Assert.Equal(nameof(IndentedTextWriter.WriteLine), indicator.LastCalledMethod);
            Assert.Equal($"{TabString}{prefix}{NewLine}{TabString}{expected}", indicator.GetStringBuilder().ToString());
        }

        [Fact]
        public async Task FlushAsync_CallsUnderlyingFlushAsync()
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator);

            await itw.FlushAsync();

            Assert.Equal(nameof(IndentedTextWriter.FlushAsync), indicator.LastCalledMethod);
        }

        [Fact]
        public async Task FlushAsync_Cancellation_CallsUnderlyingFlushAsync()
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator);

            await itw.FlushAsync(new CancellationTokenSource().Token);
            Assert.Equal(nameof(IndentedTextWriter.FlushAsync) + "Cancelable", indicator.LastCalledMethod);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task t = itw.FlushAsync(cts.Token);
            Assert.Equal(TaskStatus.Canceled, t.Status);
            Assert.Equal(cts.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t)).CancellationToken);
        }

        [Fact]
        public async Task FlushAsync_DerivedIndentedTextWriter_NonCancelableFlushAsyncInvoked()
        {
            var itw = new DerivedIndentedTextWriter(TextWriter.Null);
            await itw.FlushAsync(new CancellationTokenSource().Token);
            Assert.True(itw.NonCancelableFlushAsyncInvoked);
        }

        private sealed class DerivedIndentedTextWriter : IndentedTextWriter
        {
            public bool NonCancelableFlushAsyncInvoked;

            public DerivedIndentedTextWriter(TextWriter writer) : base(writer) { }

            public override Task FlushAsync()
            {
                NonCancelableFlushAsyncInvoked = true;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Flush_CallsUnderlyingFlush()
        {
            var indicator = new IndicatingTextWriter();
            var itw = new IndentedTextWriter(indicator);

            itw.Flush();

            Assert.Equal(nameof(IndentedTextWriter.Flush), indicator.LastCalledMethod);
        }
    }
}
