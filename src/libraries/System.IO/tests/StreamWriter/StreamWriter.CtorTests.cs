// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class CtorTests
    {
        [Fact]
        public static void CreateStreamWriter()
        {
            StreamWriter sw2;
            StreamReader sr2;
            string str2;
            MemoryStream memstr2;

            // [] Construct writer with MemoryStream
            //-----------------------------------------------------------------

            memstr2 = new MemoryStream();
            sw2 = new StreamWriter(memstr2);
            sw2.Write("HelloWorld");
            sw2.Flush();
            sr2 = new StreamReader(memstr2);
            memstr2.Position = 0;
            str2 = sr2.ReadToEnd();
            Assert.Equal("HelloWorld", str2);
        }

        [Fact]
        public static void UTF8Encoding()
        {
            TestEncoding(System.Text.Encoding.UTF8, "This is UTF8\u00FF");
        }

        [Fact]
        public static void BigEndianUnicodeEncoding()
        {
            TestEncoding(System.Text.Encoding.BigEndianUnicode, "This is BigEndianUnicode\u00FF");
        }

        [Fact]
        public static void UnicodeEncoding()
        {
            TestEncoding(System.Text.Encoding.Unicode, "This is Unicode\u00FF");
        }

        private static void TestEncoding(System.Text.Encoding encoding, string testString)
        {
            StreamWriter sw2;
            StreamReader sr2;
            string str2;

            var ms = new MemoryStream();
            sw2 = new StreamWriter(ms, encoding);
            sw2.Write(testString);
            sw2.Dispose();

            var ms2 = new MemoryStream(ms.ToArray());
            sr2 = new StreamReader(ms2, encoding);
            str2 = sr2.ReadToEnd();
            Assert.Equal(testString, str2);
        }
    }
}
