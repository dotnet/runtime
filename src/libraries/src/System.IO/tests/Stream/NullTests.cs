using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace StreamTests
{
    public class NullCtorTests
    {
        [Fact]
        public static void TestNullStream()
        {
            var t = TestNullStream(Stream.Null);
            t.Wait();
        }

        [Fact]
        public static void TestNullTextReader()
        {
            TestTextReader(TextReader.Null);
        }

        [Fact]
        public static void TestNullStreamReader()
        {
            TestTextReader(StreamReader.Null);
        }

        [Fact]
        public static void TestNullStringReader()
        {
            TestTextReader(StringReader.Null);
        }

        [Fact]
        public static void TestNullTextWriter()
        {
            TestTextWriter(TextWriter.Null);

        }

        [Fact]
        public static void TestNullStreamWriter()
        {
            TestTextWriter(StreamWriter.Null);
        }

        [Fact]
        public static void TestNullStringWriter()
        {
            TestTextWriter(StringWriter.Null);
        }

        static async Task TestNullStream(Stream s)
        {
            int n;

            s.Flush();

            Assert.Equal(-1, s.ReadByte());

            s.WriteByte(5);
            n = s.Read(new byte[2], 0, 2);
            Assert.Equal(0, n);
            s.Write(new byte[2], 0, 2);

            Assert.Equal(0, await s.ReadAsync(new byte[2], 0, 2));

            s.Flush();
            s.Dispose();
        }

        static void TestTextReader(TextReader input)
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

        static void TestTextWriter(TextWriter output)
        {
            output.Flush();
            output.Dispose();

            output.WriteLine(Decimal.MinValue);
            output.WriteLine(Math.PI);
            output.WriteLine();
            output.Flush();
            output.Dispose();
        }
    }
}
