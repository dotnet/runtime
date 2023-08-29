// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Tests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StringWriterTests
    {
        static int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue, short.MinValue };
        static int[] iArrLargeValues = new int[] { int.MaxValue, int.MaxValue - 1, int.MaxValue / 2, int.MaxValue / 10, int.MaxValue / 100 };
        static int[] iArrValidValues = new int[] { 10000, 100000, int.MaxValue / 2000, int.MaxValue / 5000, short.MaxValue };

        private static StringBuilder getSb()
        {
            var chArr = TestDataProvider.CharData;
            var sb = new StringBuilder(40);
            for (int i = 0; i < chArr.Length; i++)
                sb.Append(chArr[i]);

            return sb;
        }

        [Fact]
        public static void Ctor()
        {
            StringWriter sw = new StringWriter();
            Assert.NotNull(sw);
        }

        [Fact]
        public static void CtorWithStringBuilder()
        {
            var sb = getSb();
            StringWriter sw = new StringWriter(getSb());
            Assert.NotNull(sw);
            Assert.Equal(sb.Length, sw.GetStringBuilder().Length);
        }

        [Fact]
        public static void CtorWithCultureInfo()
        {
            StringWriter sw = new StringWriter(new CultureInfo("en-gb"));
            Assert.NotNull(sw);

            Assert.Equal(new CultureInfo("en-gb"), sw.FormatProvider);
        }

        [Fact]
        public static void SimpleWriter()
        {
            var sw = new StringWriter();
            sw.Write(4);
            var sb = sw.GetStringBuilder();
            Assert.Equal("4", sb.ToString());
        }

        [Fact]
        public static void WriteArray()
        {
            var chArr = TestDataProvider.CharData;
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter(sb);

            var sr = new StringReader(sw.GetStringBuilder().ToString());

            for (int i = 0; i < chArr.Length; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
        }

        [Fact]
        public static void CantWriteNullArray()
        {
            var sw = new StringWriter();
            Assert.Throws<ArgumentNullException>(() => sw.Write(null, 0, 0));
        }

        [Fact]
        public static void CantWriteNegativeOffset()
        {
            var sw = new StringWriter();
            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(new char[0], -1, 0));
        }

        [Fact]
        public static void CantWriteNegativeCount()
        {
            var sw = new StringWriter();
            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(new char[0], 0, -1));
        }

        [Fact]
        public static void CantWriteIndexLargeValues()
        {
            var chArr = TestDataProvider.CharData;
            for (int i = 0; i < iArrLargeValues.Length; i++)
            {
                StringWriter sw = new StringWriter();
                AssertExtensions.Throws<ArgumentException>(null, () => sw.Write(chArr, iArrLargeValues[i], chArr.Length));
            }
        }

        [Fact]
        public static void CantWriteCountLargeValues()
        {
            var chArr = TestDataProvider.CharData;
            for (int i = 0; i < iArrLargeValues.Length; i++)
            {
                StringWriter sw = new StringWriter();
                AssertExtensions.Throws<ArgumentException>(null, () => sw.Write(chArr, 0, iArrLargeValues[i]));
            }
        }

        [Fact]
        public static void WriteWithOffset()
        {
            StringWriter sw = new StringWriter();
            StringReader sr;

            var chArr = TestDataProvider.CharData;

            sw.Write(chArr, 2, 5);

            sr = new StringReader(sw.ToString());
            for (int i = 2; i < 7; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
        }

        [Fact]
        public static void WriteWithLargeIndex()
        {
            for (int i = 0; i < iArrValidValues.Length; i++)
            {
                StringBuilder sb = new StringBuilder(int.MaxValue / 2000);
                StringWriter sw = new StringWriter(sb);

                var chArr = new char[int.MaxValue / 2000];
                for (int j = 0; j < chArr.Length; j++)
                    chArr[j] = (char)(j % 256);
                sw.Write(chArr, iArrValidValues[i] - 1, 1);

                string strTemp = sw.GetStringBuilder().ToString();
                Assert.Equal(1, strTemp.Length);
            }
        }

        [Fact]
        public static void WriteWithLargeCount()
        {
            for (int i = 0; i < iArrValidValues.Length; i++)
            {
                StringBuilder sb = new StringBuilder(int.MaxValue / 2000);
                StringWriter sw = new StringWriter(sb);

                var chArr = new char[int.MaxValue / 2000];
                for (int j = 0; j < chArr.Length; j++)
                    chArr[j] = (char)(j % 256);

                sw.Write(chArr, 0, iArrValidValues[i]);

                string strTemp = sw.GetStringBuilder().ToString();
                Assert.Equal(iArrValidValues[i], strTemp.Length);
            }
        }

        [Fact]
        public static void NewStringWriterIsEmpty()
        {
            var sw = new StringWriter();
            Assert.Equal(string.Empty, sw.ToString());
        }

        [Fact]
        public static void NewStringWriterHasEmptyStringBuilder()
        {
            var sw = new StringWriter();
            Assert.Equal(string.Empty, sw.GetStringBuilder().ToString());
        }

        [Fact]
        public static void ToStringReturnsWrittenData()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter(sb);

            sw.Write(sb.ToString());

            Assert.Equal(sb.ToString(), sw.ToString());
        }

        [Fact]
        public static void StringBuilderHasCorrectData()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter(sb);

            sw.Write(sb.ToString());

            Assert.Equal(sb.ToString(), sw.GetStringBuilder().ToString());
        }

        [Fact]
        public static void Closed_DisposedExceptions()
        {
            StringWriter sw = new StringWriter();
            sw.Close();
            ValidateDisposedExceptions(sw);
        }

        [Fact]
        public static void Disposed_DisposedExceptions()
        {
            StringWriter sw = new StringWriter();
            sw.Dispose();
            ValidateDisposedExceptions(sw);
        }

        private static void ValidateDisposedExceptions(StringWriter sw)
        {
            Assert.Throws<ObjectDisposedException>(() => { sw.Write('a'); });
            Assert.Throws<ObjectDisposedException>(() => { sw.Write(new char[10], 0, 1); });
            Assert.Throws<ObjectDisposedException>(() => { sw.Write("abc"); });
        }

        [Fact]
        public static async Task FlushAsyncWorks()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter(sb);

            sw.Write(sb.ToString());

            await sw.FlushAsync();

            Assert.Equal(sb.ToString(), sw.GetStringBuilder().ToString());
        }

        [Fact]
        public static async Task FlushAsyncWorks_Cancellation()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter(sb);

            sw.Write(sb.ToString());

            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task t = sw.FlushAsync(cts.Token);
            Assert.Equal(TaskStatus.Canceled, t.Status);
            Assert.Equal(cts.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t)).CancellationToken);

            await sw.FlushAsync(new CancellationTokenSource().Token);

            Assert.Equal(sb.ToString(), sw.GetStringBuilder().ToString());
        }

        [Fact]
        public static void MiscWrites()
        {
            var sw = new StringWriter();
            sw.Write('H');
            sw.Write("ello World!");

            Assert.Equal("Hello World!", sw.ToString());
        }

        [Fact]
        public static async Task MiscWritesAsync()
        {
            var sw = new StringWriter();
            await sw.WriteAsync('H');
            await sw.WriteAsync(new char[] { 'e', 'l', 'l', 'o', ' ' });
            await sw.WriteAsync("World!");

            Assert.Equal("Hello World!", sw.ToString());
        }

        [Fact]
        public static async Task MiscWriteLineAsync()
        {
            var sw = new StringWriter();
            await sw.WriteLineAsync('H');
            await sw.WriteLineAsync(new char[] { 'e', 'l', 'l', 'o' });
            await sw.WriteLineAsync("World!");

            Assert.Equal(
                string.Format("H{0}ello{0}World!{0}", Environment.NewLine),
                sw.ToString());
        }

        [Fact]
        public static void GetEncoding()
        {
            var sw = new StringWriter();
            Assert.Equal(new UnicodeEncoding(false, false), sw.Encoding);
            Assert.Equal(Encoding.Unicode.WebName, sw.Encoding.WebName);
        }

        [Fact]
        public static void TestWriteMisc()
        {
            using (new ThreadCultureChange("en-US")) // floating-point formatting comparison depends on culture
            {
                var sw = new StringWriter();

                sw.Write(true);
                sw.Write((char)'a');
                sw.Write(new decimal(1234.01));
                sw.Write((double)3452342.01);
                sw.Write((int)23456);
                sw.Write((long)long.MinValue);
                sw.Write((float)1234.50f);
                sw.Write((uint)uint.MaxValue);
                sw.Write((ulong)ulong.MaxValue);

                Assert.Equal("Truea1234.013452342.0123456-92233720368547758081234.5429496729518446744073709551615", sw.ToString());
            }
        }

        [Fact]
        public static void TestWriteObject()
        {
            var sw = new StringWriter();
            sw.Write(new object());
            Assert.Equal("System.Object", sw.ToString());
        }

        [Fact]
        public static void TestWriteLineMisc()
        {
            using (new ThreadCultureChange("en-US")) // floating-point formatting comparison depends on culture
            {
                var sw = new StringWriter();
                sw.WriteLine((bool)false);
                sw.WriteLine((char)'B');
                sw.WriteLine((int)987);
                sw.WriteLine((long)875634);
                sw.WriteLine((float)1.23457f);
                sw.WriteLine((uint)45634563);
                sw.WriteLine((ulong.MaxValue));

                Assert.Equal(
                    string.Format("False{0}B{0}987{0}875634{0}1.23457{0}45634563{0}18446744073709551615{0}", Environment.NewLine),
                    sw.ToString());
            }
        }

        [Fact]
        public static void TestWriteLineObject()
        {
            var sw = new StringWriter();
            sw.WriteLine(new object());
            Assert.Equal("System.Object" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public static void TestWriteLineAsyncCharArray()
        {
            StringWriter sw = new StringWriter();
            sw.WriteLineAsync(new char[] { 'H', 'e', 'l', 'l', 'o' });

            Assert.Equal("Hello" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public async Task NullNewLineAsync()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                string newLine;
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 16, true))
                {
                    newLine = sw.NewLine;
                    await sw.WriteLineAsync(default(string));
                    await sw.WriteLineAsync(default(string));
                }
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    Assert.Equal(newLine + newLine, await sr.ReadToEndAsync());
                }
            }
        }

        [Fact]
        public async Task WriteSpanMemory_Success()
        {
            var sw = new StringWriter();

            sw.Write((Span<char>)new char[0]);
            sw.Write((Span<char>)new char[] { 'a' });
            sw.Write((Span<char>)new char[] { 'b', 'c', 'd' });
            sw.WriteLine((Span<char>)new char[] { 'e' });

            await sw.WriteAsync((ReadOnlyMemory<char>)new char[0]);
            await sw.WriteAsync((ReadOnlyMemory<char>)new char[] { 'f' });
            await sw.WriteAsync((ReadOnlyMemory<char>)new char[] { 'g', 'h', 'i' });
            await sw.WriteLineAsync((ReadOnlyMemory<char>)new char[] { 'j' });

            Assert.Equal("abcde" + Environment.NewLine + "fghij" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public async Task Precanceled_ThrowsException()
        {
            var writer = new StringWriter();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(Memory<char>.Empty, new CancellationToken(true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteLineAsync(Memory<char>.Empty, new CancellationToken(true)));
        }

        [Fact]
        public void TestWriteStringBuilder()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            sw.Write(sb);
            Assert.Equal(sb.ToString(), sw.ToString());
        }

        [Fact]
        public async Task TestWriteAsyncStringBuilder()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            await sw.WriteAsync(sb);
            Assert.Equal(sb.ToString(), sw.ToString());
        }

        [Fact]
        public void TestWriteAsyncStringBuilderCancelled()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Equal(TaskStatus.Canceled, sw.WriteAsync(sb, cts.Token).Status);
        }

        [Fact]
        public void TestWriteLineStringBuilder()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            sw.WriteLine(sb);
            Assert.Equal(sb.ToString() + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public async Task TestWriteLineAsyncStringBuilder()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            await sw.WriteLineAsync(sb);
            Assert.Equal(sb.ToString() + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public void TestWriteLineAsyncStringBuilderCancelled()
        {
            StringBuilder sb = getSb();
            StringWriter sw = new StringWriter();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Equal(TaskStatus.Canceled, sw.WriteLineAsync(sb, cts.Token).Status);
        }

        [Fact]
        public void DisposeAsync_ClosesWriterAndLeavesBuilderAvailable()
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            sw.Write("hello");
            Assert.True(sw.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(sw.DisposeAsync().IsCompletedSuccessfully);
            Assert.Throws<ObjectDisposedException>(() => sw.Write(42));
            Assert.Equal("hello", sb.ToString());
        }
    }
}
