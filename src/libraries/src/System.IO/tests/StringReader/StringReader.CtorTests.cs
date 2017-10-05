// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public partial class StringReaderTests
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
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(string.Empty, sr.ReadToEnd());
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
        public static void ReadEmptyString() {
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(-1, sr.Read());

        }

        [Fact]
        public static void ReadString() {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }
        }

        [Fact]
        public static void ReadLine()
        {
            string str1 = "Hello\0\t\v   \\ World";
            string str2 = str1 + Environment.NewLine + str1;

            using (StringReader sr = new StringReader(str1))
            {
                Assert.Equal(str1, sr.ReadLine());
            }
            using (StringReader sr = new StringReader(str2))
            {
                Assert.Equal(str1, sr.ReadLine());
                Assert.Equal(str1, sr.ReadLine());
            }
        }

        [Fact]
        public static void ReadPseudoRandomString()
        {
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }
        }

        [Fact]
        public static void PeekEmptyString()
        {
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(-1, sr.Peek());

        }

        [Fact]
        public static void PeekString()
        {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                int test = sr.Peek();
                sr.Read();
                Assert.Equal((int)str1[i], test);
            }

        }

        [Fact]
        public static void PeekPseudoRandomString()
        {
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (char)r.Next(0, 255);

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

            sr = new StringReader(string.Empty);
            Assert.Equal(string.Empty, sr.ReadToEnd());

        }

        [Fact]
        public static void ReadToEndString() {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }

        [Fact]
        public static void ReadToEndPseudoRandom() {
            // [] Try with large random strings
            //-----------------------------------------------------------
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 10000; i++)
                str1 += (char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }

        [Fact]
        public static void Closed_DisposedExceptions()
        {
            StringReader sr = new StringReader("abcdefg");
            sr.Close();
            ValidateDisposedExceptions(sr);
        }

        [Fact]
        public static void Disposed_DisposedExceptions()
        {
            StringReader sr = new StringReader("abcdefg");
            sr.Dispose();
            ValidateDisposedExceptions(sr);
        }

        private static void ValidateDisposedExceptions(StringReader sr)
        {
            Assert.Throws<ObjectDisposedException>(() => { sr.Peek(); });
            Assert.Throws<ObjectDisposedException>(() => { sr.Read(); });
            Assert.Throws<ObjectDisposedException>(() => { sr.Read(new char[10], 0 , 1); });
        }
    }
}
