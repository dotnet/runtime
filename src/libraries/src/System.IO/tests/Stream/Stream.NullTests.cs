// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class NullCtorTests
    {
        [Fact]
        public static async Task TestNullStream()
        {
            Stream s = Stream.Null;

            s.Flush();

            Assert.Equal(-1, s.ReadByte());

            s.WriteByte(5);
            int n = s.Read(new byte[2], 0, 2);
            Assert.Equal(0, n);
            s.Write(new byte[2], 0, 2);

            Assert.Equal(0, await s.ReadAsync(new byte[2], 0, 2));

            s.Flush();
            s.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullReaders))]
        public static void TestNullTextReader(TextReader input)
        {
            StreamReader sr = input as StreamReader;

            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");
            input.ReadLine();
            input.Dispose();

            input.ReadLine();
            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");
            input.Read();
            input.Peek();
            input.Read(new char[2], 0, 2);
            input.ReadToEnd();
            input.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullWriters))]
        public static void TextNullTextWriter(TextWriter output)
        {
            output.Flush();
            output.Dispose();

            output.WriteLine(decimal.MinValue);
            output.WriteLine(Math.PI);
            output.WriteLine();
            output.Flush();
            output.Dispose();
        }

        public static IEnumerable<object[]> NullReaders
        {
            get
            {
                yield return new object[] { TextReader.Null };
                yield return new object[] { StreamReader.Null };
                yield return new object[] { StringReader.Null };
            }
        }

        public static IEnumerable<object[]> NullWriters
        {
            get
            {
                yield return new object[] { TextWriter.Null };
                yield return new object[] { StreamWriter.Null };
                yield return new object[] { StringWriter.Null };
            }
        }

    }
}
