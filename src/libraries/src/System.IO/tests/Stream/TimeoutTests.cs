using System;
using System.IO;
using Xunit;

namespace StreamTests
{
    public class MyStream : Stream
    {
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }


        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }


        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { return 0; }
        public override void Write(byte[] buffer, int offset, int count) { }

    }

    public class TimeoutTests
    {
        [Fact]
        public static void TestReadTimeoutCustomStream()
        {
            TestReadTimeout(new MyStream());
        }
        [Fact]
        public static void TestReadTimeoutMemoryStream()
        {
            TestReadTimeout(new MemoryStream());
        }


        private static void TestReadTimeout(Stream stream)
        {
            Assert.Throws<InvalidOperationException>(() => { int readTimeout = stream.ReadTimeout; });

            Assert.Throws<InvalidOperationException>(() => { stream.ReadTimeout = 500; });
        }
        [Fact]
        public static void TestWriteTimeoutCustomStream()
        {
            TestWriteTimeout(new MyStream());
        }
        [Fact]
        public static void TestWriteTimeoutMemoryStream()
        {
            TestWriteTimeout(new MemoryStream());
        }

        private static void TestWriteTimeout(Stream stream)
        {
            Assert.Throws<InvalidOperationException>(() => { int WriteTimeout = stream.WriteTimeout; });
            Assert.Throws<InvalidOperationException>(() => { stream.WriteTimeout = 500; });
        }

        [Fact]
        public static void TestCanTimeoutCustomStream()
        {
            TestCanTimeout(new MyStream());
        }

        [Fact]
        public static void TestCanTimeoutMemoryStream()
        {
            TestCanTimeout(new MemoryStream());
        }

        private static void TestCanTimeout(System.IO.Stream stream)
        {
            Assert.False(stream.CanTimeout, "Expected CanTimeout to return false");
        }
    }

}
