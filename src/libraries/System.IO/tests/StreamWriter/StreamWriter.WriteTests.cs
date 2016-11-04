// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
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
            char[] chArr = setupArray();

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

        private static char[] setupArray()
        {
            return new char[]{
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
            ,'\uFE70' 
            ,'-'
            ,';'
            ,'\u00E6'
        };
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
            char[] chArr = setupArray();

            // [] Exception if offset is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, -1, 0));
            sw.Dispose();
        }

        [Fact]
        public void NegativeCount()
        {
            char[] chArr = setupArray();

            // [] Exception if count is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, 0, -1));
            sw.Dispose();
        }

        [Fact]
        public void WriteCustomLenghtStrings()
        {
            char[] chArr = setupArray();

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
            char[] chArr = setupArray();
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
            char[] chArr = setupArray();
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentException>(() => sw.Write(chArr, 1, chArr.Length));
            sw.Dispose();
        }

        [Fact]
        public void VerifyWrittenString()
        {
            char[] chArr = setupArray();
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
    }
}
