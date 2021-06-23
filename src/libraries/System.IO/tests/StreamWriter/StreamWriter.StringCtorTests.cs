// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class StreamWriter_StringCtorTests
    {
        [Fact]
        public static void NullArgs_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new StreamWriter((string)null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => new StreamWriter((string)null, null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => new StreamWriter((string)null, true));
            AssertExtensions.Throws<ArgumentNullException>("path", () => new StreamWriter((string)null, true, null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => new StreamWriter((string)null, true, null, -1));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("path", true, null));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("path", null, null));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("path", true, null, -1));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("", true, null));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("", null, null));
            AssertExtensions.Throws<ArgumentNullException>("encoding", () => new StreamWriter("", true, null, -1));
        }

        [Fact]
        public static void EmptyPath_ThrowsArgumentException()
        {
            // No argument name for the empty path exception
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter(""));
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter("", new FileStreamOptions()));
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter("", true));
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter("", true, Encoding.UTF8));
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter("", Encoding.UTF8, new FileStreamOptions()));
            AssertExtensions.Throws<ArgumentException>(null, () => new StreamWriter("", true, Encoding.UTF8, -1));
        }

        [Fact]
        public static void NegativeBufferSize_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => new StreamWriter("path", false, Encoding.UTF8, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => new StreamWriter("path", true, Encoding.UTF8, 0));
        }

        [Fact]
        public static void CreateStreamWriter()
        {
            string testfile = Path.GetTempFileName();
            string testString = "Hello World!";
            try
            {
                using (StreamWriter writer = new StreamWriter(testfile))
                {
                    writer.Write(testString);
                }

                using (StreamReader reader = new StreamReader(testfile))
                {
                    Assert.Equal(testString, reader.ReadToEnd());
                }
            }
            finally
            {
                File.Delete(testfile);
            }
        }

        public static IEnumerable<object[]> EncodingsToTestStreamWriter()
        {
            yield return new object[] { Encoding.UTF8, "This is UTF8\u00FF" };
            yield return new object[] { Encoding.BigEndianUnicode, "This is BigEndianUnicode\u00FF" };
            yield return new object[] { Encoding.Unicode, "This is Unicode\u00FF" };
        }

        [Theory]
        [MemberData(nameof(EncodingsToTestStreamWriter))]
        public static void TestEncoding(Encoding encoding, string testString)
        {
            string testfile = Path.GetTempFileName();
            try
            {
                using (StreamWriter writer = new StreamWriter(testfile, false, encoding))
                {
                    writer.Write(testString);
                }

                using (StreamReader reader = new StreamReader(testfile, encoding))
                {
                    Assert.Equal(testString, reader.ReadToEnd());
                }
            }
            finally
            {
                File.Delete(testfile);
            }
        }
    }
}
