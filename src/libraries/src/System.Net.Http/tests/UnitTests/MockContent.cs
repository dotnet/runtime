// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Tests
{
    [Flags]
    public enum MockOptions
    {
        None = 0x0,
        ThrowInSerializeMethods = 0x1,
        ReturnNullInCopyToAsync = 0x2,
        UseWriteByteInCopyTo = 0x4,
        DontOverrideCreateContentReadStream = 0x8,
        CanCalculateLength = 0x10,
        ThrowInTryComputeLength = 0x20,
        ThrowInAsyncSerializeMethods = 0x40
    }

    public class MockException : Exception
    {
        public MockException() { }
        public MockException(string message) : base(message) { }
        public MockException(string message, Exception inner) : base(message, inner) { }
    }

    public class MockContent : HttpContent
    {
        private byte[] _mockData;
        private MockOptions _options;
        private Exception _customException;

        public int TryComputeLengthCount { get; private set; }
        public int SerializeToStreamAsyncCount { get; private set; }
        public int CreateContentReadStreamCount { get; private set; }
        public int DisposeCount { get; private set; }

        public byte[] MockData
        {
            get { return _mockData; }
        }

        public MockContent()
            : this((byte[])null, MockOptions.None)
        {
        }

        public MockContent(byte[] mockData)
            : this(mockData, MockOptions.None)
        {
        }

        public MockContent(MockOptions options)
            : this((byte[])null, options)
        {
        }

        public MockContent(Exception customException, MockOptions options)
            : this((byte[])null, options)
        {
            _customException = customException;
        }

        public MockContent(byte[] mockData, MockOptions options)
        {
            _options = options;

            if (mockData == null)
            {
                _mockData = Encoding.UTF8.GetBytes("data");
            }
            else
            {
                _mockData = mockData;
            }
        }

        public byte[] GetMockData()
        {
            return _mockData;
        }

        protected internal override bool TryComputeLength(out long length)
        {
            TryComputeLengthCount++;

            if ((_options & MockOptions.ThrowInTryComputeLength) != 0)
            {
                throw new MockException();
            }

            if ((_options & MockOptions.CanCalculateLength) != 0)
            {
                length = _mockData.Length;
                return true;
            }
            else
            {
                length = 0;
                return false;
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            SerializeToStreamAsyncCount++;

            if ((_options & MockOptions.ReturnNullInCopyToAsync) != 0)
            {
                return null;
            }

            if ((_options & MockOptions.ThrowInAsyncSerializeMethods) != 0)
            {
                throw _customException;
            }

            return Task.Run(() =>
            {
                CheckThrow();
                return stream.WriteAsync(_mockData, 0, _mockData.Length);
            });
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            CreateContentReadStreamCount++;

            if ((_options & MockOptions.DontOverrideCreateContentReadStream) != 0)
            {
                return base.CreateContentReadStreamAsync();
            }
            else
            {
                return Task.FromResult<Stream>(new MockMemoryStream(_mockData, 0, _mockData.Length, false));
            }
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }

        private void CheckThrow()
        {
            if ((_options & MockOptions.ThrowInSerializeMethods) != 0)
            {
                throw _customException;
            }
        }
    }

    public class MockMemoryStream : MemoryStream
    {
        public int DisposeCount { get; private set; }

        public MockMemoryStream(byte[] buffer, int index, int count, bool writable)
            : base(buffer, index, count, writable)
        {
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }
    }
}
