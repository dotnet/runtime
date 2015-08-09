// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace StringReaderTests
{
    public class ReaderTests
    {
        [Fact]
        public static void StringReaderWithNullString()
        {
            Assert.Throws<ArgumentNullException>(() => new StringReader(null));
        }

        [Fact]
        public static void StringReaderWithEmptyString()
        {

            // [] Check vanilla construction
            //-----------------------------------------------------------
            StringReader sr = new StringReader(String.Empty);
            Assert.Equal(String.Empty, sr.ReadToEnd());
        }

        [Fact]
        public static void StringReaderWithGenericString()
        {
            // [] Another vanilla construction
            //-----------------------------------------------------------

            StringReader sr = new StringReader("Hello\0World");
            Assert.Equal("Hello\0World", sr.ReadToEnd());
        }

        [Fact]
        public static void ReadEmtpyString() {
            StringReader sr = new StringReader(String.Empty);
            Assert.Equal(-1, sr.Read());

        }

        [Fact]
        public static void ReadString() {
            String str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }

        }

        [Fact]
        public static void ReadPsudoRandomString()
        {
            String str1 = String.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (Char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }
        }





        [Fact]
        public static void PeedEmtpyString()
        {
            StringReader sr = new StringReader(String.Empty);
            Assert.Equal(-1, sr.Peek());

        }

        [Fact]
        public static void PeekString()
        {
            String str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                int test = sr.Peek();
                sr.Read();
                Assert.Equal((int)str1[i], test);
            }

        }

        [Fact]
        public static void PeekPsudoRandomString()
        {
            String str1 = String.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (Char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                int test = sr.Peek();
                sr.Read();
                Assert.Equal((int)str1[i], test);
            }
        }

        [Fact]
        public static void ReadToEndEmptyString()
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            StringReader sr;

            sr = new StringReader(String.Empty);
            Assert.Equal(String.Empty, sr.ReadToEnd());

        }

        [Fact]
        public static void ReadToEndString() {
            String str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }

        [Fact]
        public static void ReadToEndPsuedoRandom() {
            // [] Try with large random strings
            //-----------------------------------------------------------
            String str1 = String.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 10000; i++)
                str1 += (Char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }
    }
}
