// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BufferedStreamTests
{

    public class BufferedStream_StreamAsync : StreamTests.StreamAsync
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class BufferedStream_StreamMethods : StreamTests.StreamMethods
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        protected override Stream CreateStream(int bufferSize)
        {
            return new BufferedStream(new MemoryStream(), bufferSize);
        }
    }

    public class BufferedStream_TestLeaveOpen : StreamTests.TestLeaveOpen
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamWriterWithBufferedStream_CloseTests : StreamWriterTests.CloseTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamWriterWithBufferedStream_FlushTests : StreamWriterTests.FlushTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamWriterWithBufferedStream_WriteTests : StreamWriterTests.WriteTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamReaderWithBufferedStream_Tests : StreamReaderTests.StreamReaderTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        protected override Stream GetSmallStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            return new BufferedStream(new MemoryStream(testData));
        }

        protected override Stream GetLargeStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            List<byte> data = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                data.AddRange(testData);
            }

            return new BufferedStream(new MemoryStream(data.ToArray()));
        }
    }

    public class BinaryWriterWithBufferedStream_Tests : BinaryWriterTests.BinaryWriterTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        [Fact]
        public override void BinaryWriter_FlushTests()
        {
            // [] Check that flush updates the underlying stream
            using (Stream memstr2 = CreateStream())
            using (BinaryWriter bw2 = new BinaryWriter(memstr2))
            {
                string str = "HelloWorld";
                int expectedLength = str.Length + 1; // 1 for 7-bit encoded length
                bw2.Write(str);
                Assert.Equal(expectedLength, memstr2.Length);
                bw2.Flush();
                Assert.Equal(expectedLength, memstr2.Length);
            }

            // [] Flushing a closed writer may throw an exception depending on the underlying stream
            using (Stream memstr2 = CreateStream())
            {
                BinaryWriter bw2 = new BinaryWriter(memstr2);
                bw2.Dispose();
                Assert.Throws<ObjectDisposedException>(() => bw2.Flush());
            }
        }
    }

    public class BinaryWriterWithBufferedStream_WriteByteCharTests : BinaryWriterTests.BinaryWriter_WriteByteCharTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class BinaryWriterWithBufferedStream_WriteTests : BinaryWriterTests.BinaryWriter_WriteTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }
}
