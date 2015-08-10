// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StreamReaderTests
{
    public class StreamReaderTests
    {
        static Tuple<char[], StreamReader> GetCharArrayStream()
        {
            var chArr = new char[]{
                Char.MinValue
                ,Char.MaxValue
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


            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;

            return new Tuple<char[], StreamReader>(chArr, new StreamReader(ms));
        }

        static MemoryStream GetSmallStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            return new System.IO.MemoryStream(testData);
        }

        static MemoryStream GetLargeStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            // System.Collections.Generic.

            List<byte> data = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                data.AddRange(testData);
            }

            return new System.IO.MemoryStream(data.ToArray());
        }


        [Fact]
        public static void EndOfStream()
        {
            var sw = new StreamReader(GetSmallStream());

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public static void EndOfStreamSmallDataLargeBuffer()
        {
            var sw = new StreamReader(GetSmallStream(), Encoding.UTF8, true, 1024);

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public static void EndOfStreamLargeDataSmallBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public static void EndOfStreamLargeDataLargeBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1 << 16);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public static async Task ReadToEndAsync()
        {
            var sw = new StreamReader(GetLargeStream());
            var result = await sw.ReadToEndAsync();

            Assert.Equal(5000, result.Length);
        }

        [Fact]
        public static void GetBaseStream()
        {
            var ms = GetSmallStream();
            var sw = new StreamReader(ms);

            Assert.Same(sw.BaseStream, ms);
        }

        [Fact]
        public static void TestRead()
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
        public static void TestPeek()
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
        public static void ArgumentNullOnNullArray()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            Assert.Throws<ArgumentNullException>(() => sr.Read(null, 0, 0));
        }

        [Fact]
        public static void ArgumentOutOfRangeOnInvalidOffset()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentOutOfRangeException>(() => sr.Read(new char[0], -1, 0));
        }

        [Fact]
        public static void ArgumentOutOfRangeOnNegativCount()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentException>(() => sr.Read(new char[0], 0, 1));
        }

        [Fact]
        public static void ArgumentExceptionOffsetAndCount()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentException>(() => sr.Read(new Char[0], 2, 0));
        }

        [Fact]
        public static void ObjectDisposedExceptionDisposedStream()
        {
            var sr = GetCharArrayStream().Item2;
            sr.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public static void ObjectDisposedExceptionDisposedBaseStream()
        {
            var ms = GetSmallStream();
            var sr = new StreamReader(ms);
            ms.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public static void EmptyStream()
        {
            var ms = new MemoryStream();
            var sr = new StreamReader(ms);

            var buffer = new char[10];
            int read = sr.Read(buffer, 0, 1);
            Assert.Equal(0, read);
        }

        [Fact]
        public static void VanillaReads1()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            var chArr = new Char[baseInfo.Item1.Length];

            var read = sr.Read(chArr, 0, chArr.Length);

            Assert.Equal(chArr.Length, read);
            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i]);
            }
        }

        [Fact]
        public static async Task VanillaReads2WithAsync()
        {
            var baseInfo = GetCharArrayStream();

            var sr = baseInfo.Item2;

            var chArr = new Char[baseInfo.Item1.Length];

            var read = await sr.ReadAsync(chArr, 4, 3);

            Assert.Equal(read, 3);
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i + 4]);
            }
        }

        [Fact]
        public static void ObjectDisposedReadLine()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            sr.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public static void ObjectDisposedReadLineBaseStream()
        {
            var ms = GetLargeStream();
            var sr = new StreamReader(ms);

            ms.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }
       
        [Fact]
        public static void VanillaReadLines()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            String valueString = new String(baseInfo.Item1);


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
        public static void VanillaReadLines2()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            String valueString = new String(baseInfo.Item1);

            var temp = new char[10];
            sr.Read(temp, 0, 1);
            var data = sr.ReadLine();
            Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
        }

        [Fact]
        public static async Task ContinuousNewLinesAndTabsAsync()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write("\n\n\r\r\n");
            sw.Flush();

            ms.Position = 0;

            var sr = new StreamReader(ms);

            for (int i = 0; i < 4; i++)
            {
                var data = await sr.ReadLineAsync();
                Assert.Equal(String.Empty, data);
            }

            var eol = await sr.ReadLineAsync();
            Assert.Null(eol);
        }

        [Fact]
        public static void CurrentEncoding()
        {
            var ms = new MemoryStream();

            var sr = new StreamReader(ms);
            Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);

            sr = new StreamReader(ms, Encoding.Unicode);
            Assert.Equal(Encoding.Unicode, sr.CurrentEncoding);

        }
    }
}
