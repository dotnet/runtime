// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamReaderTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        protected virtual Stream GetSmallStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            return new MemoryStream(testData);
        }

        protected virtual Stream GetLargeStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            // System.Collections.Generic.

            List<byte> data = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                data.AddRange(testData);
            }

            return new MemoryStream(data.ToArray());
        }

        protected Tuple<char[], StreamReader> GetCharArrayStream()
        {
            var chArr = new char[]{
                char.MinValue
                ,char.MaxValue
                ,'\t'
                ,' '
                ,'$'
                ,'@'
                ,'#'
                ,'\0'
                ,'\v'
                ,'\''
                ,'\u3190'
                ,'\uC3A0'
                ,'A'
                ,'5'
                ,'\r'
                ,'\uFE70'
                ,'-'
                ,';'
                ,'\r'
                ,'\n'
                ,'T'
                ,'3'
                ,'\n'
                ,'K'
                ,'\u00E6'
            };
            var ms = CreateStream();
            var sw = new StreamWriter(ms);

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;

            return new Tuple<char[], StreamReader>(chArr, new StreamReader(ms));
        }

        [Fact]
        public void EndOfStream()
        {
            var sw = new StreamReader(GetSmallStream());

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamSmallDataLargeBuffer()
        {
            var sw = new StreamReader(GetSmallStream(), Encoding.UTF8, true, 1024);

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamLargeDataSmallBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamLargeDataLargeBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1 << 16);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public async Task ReadToEndAsync()
        {
            var sw = new StreamReader(GetLargeStream());
            var result = await sw.ReadToEndAsync();

            Assert.Equal(5000, result.Length);
        }

        [Fact]
        public void GetBaseStream()
        {
            var ms = GetSmallStream();
            var sw = new StreamReader(ms);

            Assert.Same(sw.BaseStream, ms);
        }

        [Fact]
        public void TestRead()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;


            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)baseInfo.Item1[i], tmp);
            }

            sr.Dispose();
        }

        [Fact]
        public void TestPeek()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                var peek = sr.Peek();
                Assert.Equal((int)baseInfo.Item1[i], peek);

                sr.Read();
            }
        }

        [Fact]
        public void ArgumentNullOnNullArray()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            Assert.Throws<ArgumentNullException>(() => sr.Read(null, 0, 0));
        }

        [Fact]
        public void ArgumentOutOfRangeOnInvalidOffset()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentOutOfRangeException>(() => sr.Read(new char[0], -1, 0));
        }

        [Fact]
        public void ArgumentOutOfRangeOnNegativCount()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentException>(() => sr.Read(new char[0], 0, 1));
        }

        [Fact]
        public void ArgumentExceptionOffsetAndCount()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentException>(() => sr.Read(new char[0], 2, 0));
        }

        [Fact]
        public void ObjectDisposedExceptionDisposedStream()
        {
            var sr = GetCharArrayStream().Item2;
            sr.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public void ObjectDisposedExceptionDisposedBaseStream()
        {
            var ms = GetSmallStream();
            var sr = new StreamReader(ms);
            ms.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public void EmptyStream()
        {
            var ms = CreateStream();
            var sr = new StreamReader(ms);

            var buffer = new char[10];
            int read = sr.Read(buffer, 0, 1);
            Assert.Equal(0, read);
        }

        [Fact]
        public void VanillaReads1()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            var chArr = new char[baseInfo.Item1.Length];

            var read = sr.Read(chArr, 0, chArr.Length);

            Assert.Equal(chArr.Length, read);
            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i]);
            }
        }

        [Fact]
        public async Task VanillaReads2WithAsync()
        {
            var baseInfo = GetCharArrayStream();

            var sr = baseInfo.Item2;

            var chArr = new char[baseInfo.Item1.Length];

            var read = await sr.ReadAsync(chArr, 4, 3);

            Assert.Equal(read, 3);
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i + 4]);
            }
        }

        [Fact]
        public void ObjectDisposedReadLine()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            sr.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void ObjectDisposedReadLineBaseStream()
        {
            var ms = GetLargeStream();
            var sr = new StreamReader(ms);

            ms.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void VanillaReadLines()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            string valueString = new string(baseInfo.Item1);


            var data = sr.ReadLine();
            Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

            data = sr.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

            data = sr.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

            data = sr.ReadLine();
            Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
        }

        [Fact]
        public void VanillaReadLines2()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            string valueString = new string(baseInfo.Item1);

            var temp = new char[10];
            sr.Read(temp, 0, 1);
            var data = sr.ReadLine();
            Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
        }

        [Fact]
        public async Task ContinuousNewLinesAndTabsAsync()
        {
            var ms = CreateStream();
            var sw = new StreamWriter(ms);
            sw.Write("\n\n\r\r\n");
            sw.Flush();

            ms.Position = 0;

            var sr = new StreamReader(ms);

            for (int i = 0; i < 4; i++)
            {
                var data = await sr.ReadLineAsync();
                Assert.Equal(string.Empty, data);
            }

            var eol = await sr.ReadLineAsync();
            Assert.Null(eol);
        }

        [Fact]
        public void CurrentEncoding()
        {
            var ms = CreateStream();

            var sr = new StreamReader(ms);
            Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);

            sr = new StreamReader(ms, Encoding.Unicode);
            Assert.Equal(Encoding.Unicode, sr.CurrentEncoding);

        }
    }
}
