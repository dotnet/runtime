// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.CodeDom.Compiler;
using Xunit;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace System.CodeDom.Tests
{
    public class IndentedTextWriterTests
    {
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

            Assert.Equal(new string(' ', 4), IndentedTextWriter.DefaultTabString);
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
                "first" + Environment.NewLine +
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

                Assert.Equal(itw.NewLine + tabString + "Should be indented" + itw.NewLine, sb.ToString());
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
                "" + newline +
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
    }
}
