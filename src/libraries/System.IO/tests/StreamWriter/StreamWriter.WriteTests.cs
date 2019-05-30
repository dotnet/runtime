// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class WriteTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void WriteChars()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write a wide variety of characters and read them back

            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
        }

        [Fact]
        public void NullArray()
        {
            // [] Exception for null array
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentNullException>(() => sw.Write(null, 0, 0));
            sw.Dispose();
        }

        [Fact]
        public void NegativeOffset()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if offset is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, -1, 0));
            sw.Dispose();
        }

        [Fact]
        public void NegativeCount()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if count is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, 0, -1));
            sw.Dispose();
        }

        [Fact]
        public void WriteCustomLenghtStrings()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write some custom length strings
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 2, 5);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);
            int tmp = 0;
            for (int i = 2; i < 7; i++)
            {
                tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
            ms.Dispose();
        }

        [Fact]
        public void WriteToStreamWriter()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Just construct a streamwriter and write to it    
            //-------------------------------------------------             
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 0, chArr.Length);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
            ms.Dispose();
        }

        [Fact]
        public void TestWritingPastEndOfArray()
        {
            char[] chArr = TestDataProvider.CharData;
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            AssertExtensions.Throws<ArgumentException>(null, () => sw.Write(chArr, 1, chArr.Length));
            sw.Dispose();
        }

        [Fact]
        public void VerifyWrittenString()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Write string with wide selection of characters and read it back        

            StringBuilder sb = new StringBuilder(40);
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sb.Append(chArr[i]);

            sw.Write(sb.ToString());
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
        }

        [Fact]
        public void NullStreamThrows()
        {
            // [] Null string should write nothing

            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write((string)null);
            sw.Flush();
            Assert.Equal(0, ms.Length);
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
    }
}
