// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace StreamWriterTests
{
    public class Co5563Write_ch
    {
        [Fact]
        public static void WriteChars()
        {
            char[] chArr = setupArray();

            // [] Write a wide variety of characters and read them back

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((Int32)chArr[i], sr.Read());
            }
        }

        private static char[] setupArray()
        {
            return new Char[]{
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
			,'\uFE70' 
			,'-'
			,';'
			,'\u00E6'
		};
        }
        [Fact]
        public static void NullArray()
        {
            // [] Exception for null array
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentNullException>(() => sw.Write(null, 0, 0));
            sw.Dispose();
        }
        [Fact]
        public static void NegativeOffset()
        {
            char[] chArr = setupArray();

            // [] Exception if offset is negative
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, -1, 0));
            sw.Dispose();
        }
        [Fact]
        public static void NegativeCount()
        {
            char[] chArr = setupArray();

            // [] Exception if count is negative
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, 0, -1));
            sw.Dispose();
        }

        [Fact]
        public static void WriteCustomLenghtStrings()
        {
            char[] chArr = setupArray();

            // [] Write some custom length strings
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 2, 5);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);
            Int32 tmp = 0;
            for (int i = 2; i < 7; i++)
            {
                tmp = sr.Read();
                Assert.Equal((Int32)chArr[i], tmp);
            }
            ms.Dispose();
        }

        [Fact]
        public static void WriteToStreamWriter()
        {
            char[] chArr = setupArray();
            // [] Just construct a streamwriter and write to it	
            //------------------------------------------------- 			
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 0, chArr.Length);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((Int32)chArr[i], sr.Read());
            }
            ms.Dispose();
        }
        [Fact]
        public static void TestWritingPastEndOfArray()
        {
            char[] chArr = setupArray();
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentException>(() => sw.Write(chArr, 1, chArr.Length));
            sw.Dispose();
        }

        [Fact]
        public static void VerifyWrittenString()
        {
            char[] chArr = setupArray();
            // [] Write string with wide selection of characters and read it back		

            StringBuilder sb = new StringBuilder(40);
            MemoryStream ms = new MemoryStream();
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
                Assert.Equal((Int32)chArr[i], sr.Read());
            }
        }

        [Fact]
        public static void NullStreamThrows()
        {
            // [] Null string should write nothing

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write((String)null);
            sw.Flush();
            Assert.Equal(0, ms.Length);
        }
    }
}
