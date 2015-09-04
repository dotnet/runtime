// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class SslStreamStreamToStreamTest
    {
        [Fact]
        public void SslStream_StreamToStream_Authentication_Success()
        {
            MockNetwork network = new MockNetwork();

            using (var clientStream = new FakeNetworkStream(false, network))
            using (var serverStream = new FakeNetworkStream(true, network))
            using (var client = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            {
                X509Certificate2 certificate = TestConfiguration.GetServerCertificate();
                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync(certificate.Subject);
                auth[1] = server.AuthenticateAsServerAsync(certificate);

                Task.WaitAll(auth);
            }
        }

        public bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            return false;
        }

        [Fact]
        public void SslStream_StreamToStream_Authentication_IncorrectServerName_Fail()
        {
            MockNetwork network = new MockNetwork();

            using (var clientStream = new FakeNetworkStream(false, network))
            using (var serverStream = new FakeNetworkStream(true, network))
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            {
                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync("incorrectServer");
                auth[1] = server.AuthenticateAsServerAsync(TestConfiguration.GetServerCertificate());

                Assert.Throws<AuthenticationException>(() =>
                {
                    auth[0].GetAwaiter().GetResult();
                });

                auth[1].GetAwaiter().GetResult();
            }
        }

        internal class MockNetwork
        {
            private readonly Queue<byte[]> _clientWriteQueue = new Queue<byte[]>();
            private readonly Queue<byte[]> _serverWriteQueue = new Queue<byte[]>();

            private readonly SemaphoreSlim _clientDataAvailable = new SemaphoreSlim(0);
            private readonly SemaphoreSlim _serverDataAvailable = new SemaphoreSlim(0);

            public MockNetwork()
            {
            }

            public void ReadFrame(bool server, out byte[] buffer)
            {
                SemaphoreSlim semaphore;
                Queue<byte[]> packetQueue;

                if (server)
                {
                    semaphore = _clientDataAvailable;
                    packetQueue = _clientWriteQueue;
                }
                else
                {
                    semaphore = _serverDataAvailable;
                    packetQueue = _serverWriteQueue;
                }

                semaphore.Wait();
                buffer = packetQueue.Dequeue();
            }

            public void WriteFrame(bool server, byte[] buffer)
            {
                SemaphoreSlim semaphore;
                Queue<byte[]> packetQueue;

                if (server)
                {
                    semaphore = _serverDataAvailable;
                    packetQueue = _serverWriteQueue;
                }
                else
                {
                    semaphore = _clientDataAvailable;
                    packetQueue = _clientWriteQueue;
                }

                byte[] innerBuffer = new byte[buffer.Length];
                buffer.CopyTo(innerBuffer, 0);

                packetQueue.Enqueue(innerBuffer);
                semaphore.Release();
            }
        }

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
                _network.WriteFrame(_isServer, buffer);
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
}
