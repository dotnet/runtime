// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    internal class VirtualNetworkStream : Stream
    {
        private readonly VirtualNetwork _network;
        private MemoryStream _readStream;
        private readonly bool _isServer;
        private object _readStreamLock = new object();

        public VirtualNetworkStream(VirtualNetwork network, bool isServer)
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
            lock (_readStreamLock)
            {
                if (_readStream == null || (_readStream.Position >= _readStream.Length))
                {
                    byte[] innerBuffer;

                    _network.ReadFrame(_isServer, out innerBuffer);
                    _readStream = new MemoryStream(innerBuffer);
                }

                return _readStream.Read(buffer, offset, count);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] innerBuffer = new byte[count];

            Buffer.BlockCopy(buffer, offset, innerBuffer, 0, count);
            _network.WriteFrame(_isServer, innerBuffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead = Read(buffer, offset, count);
                return Task.FromResult<int>(bytesRead);
            }
            catch (Exception e)
            {
                return Task.FromException<int>(e);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }
    }
}
