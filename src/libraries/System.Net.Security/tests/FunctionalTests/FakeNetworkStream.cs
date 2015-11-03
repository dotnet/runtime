// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.Security.Tests
{
    internal class FakeNetworkStream : Stream
    {
        private readonly MockNetwork _network;
        private MemoryStream _readStream;
        private readonly bool _isServer;

        public FakeNetworkStream(bool isServer, MockNetwork network)
        {
            _network = network;
            _isServer = isServer;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            UpdateReadStream();
            return _readStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] innerBuffer = new byte[count];

            Buffer.BlockCopy(buffer, offset, innerBuffer, 0, count);
            _network.WriteFrame(_isServer, innerBuffer);
        }

        private void UpdateReadStream()
        {
            if (_readStream != null && (_readStream.Position < _readStream.Length))
            {
                return;
            }

            byte[] innerBuffer;
            _network.ReadFrame(_isServer, out innerBuffer);
            _readStream = new MemoryStream(innerBuffer);
        }
    }
}
