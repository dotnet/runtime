// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Connections.Tests
{
    public class ConnectionTest
    {
        [Fact]
        public void CreateStream_CalledOnce_Success()
        {
            int callCount = 0;

            var con = new MockConnection();
            con.OnCreateStream = () =>
            {
                ++callCount;
                return new MemoryStream();
            };

            _ = con.Stream;
            _ = con.Stream;

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void CreatePipe_CalledOnce_Success()
        {
            int callCount = 0;

            var con = new MockConnection();
            con.OnCreatePipe = () =>
            {
                ++callCount;
                return new MockPipe();
            };

            _ = con.Pipe;
            _ = con.Pipe;

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void AccessStream_AccessPipe_Fail()
        {
            var con = new MockConnection();
            con.OnCreateStream = () => new MemoryStream();

            _ = con.Stream;
            Assert.Throws<InvalidOperationException>(() => _ = con.Pipe);
        }

        [Fact]
        public void AccessPipe_AccessStream_Fail()
        {
            var con = new MockConnection();
            con.OnCreatePipe = () => new MockPipe();

            _ = con.Pipe;
            Assert.Throws<InvalidOperationException>(() => _ = con.Stream);
        }

        [Fact]
        public void AccessStream_NoOverloads_Fail()
        {
            var con = new ConnectionWithoutStreamOrPipe();
            Assert.Throws<InvalidOperationException>(() => _ = con.Stream);
        }

        [Fact]
        public void AccessPipe_NoOverloads_Fail()
        {
            var con = new ConnectionWithoutStreamOrPipe();
            Assert.Throws<InvalidOperationException>(() => _ = con.Pipe);
        }

        [Fact]
        public async Task WrappedStream_Success()
        {
            var bytesA = Encoding.ASCII.GetBytes("foo");
            var bytesB = Encoding.ASCII.GetBytes("bar");

            var stream = new MemoryStream();
            stream.Write(bytesA);
            stream.Position = 0;

            var con = new MockConnection();
            con.OnCreateStream = () => stream;

            IDuplexPipe pipe = con.Pipe;

            ReadResult res = await pipe.Input.ReadAsync();
            Assert.Equal(bytesA, res.Buffer.ToArray());

            await pipe.Output.WriteAsync(bytesB);
            Assert.Equal(bytesA.Concat(bytesB).ToArray(), stream.ToArray());
        }

        [Fact]
        public async Task WrappedPipe_Success()
        {
            var bytesA = Encoding.ASCII.GetBytes("foo");
            var bytesB = Encoding.ASCII.GetBytes("bar");

            var stream = new MemoryStream();
            stream.Write(bytesA);
            stream.Position = 0;

            var pipe = new MockPipe
            {
                Input = PipeReader.Create(stream),
                Output = PipeWriter.Create(stream)
            };

            var con = new MockConnection();
            con.OnCreatePipe = () => pipe;

            Stream s = con.Stream;

            var readBuffer = new byte[4];
            int len = await s.ReadAsync(readBuffer);
            Assert.Equal(3, len);
            Assert.Equal(bytesA, readBuffer.AsSpan(0, len).ToArray());

            await s.WriteAsync(bytesB);
            Assert.Equal(bytesA.Concat(bytesB).ToArray(), stream.ToArray());
        }

        [Theory]
        [InlineData(ConnectionCloseMethod.GracefulShutdown, true)]
        [InlineData(ConnectionCloseMethod.Abort, false)]
        [InlineData(ConnectionCloseMethod.Immediate, false)]
        public async Task FromStream_CloseMethod_Flushed(ConnectionCloseMethod method, bool shouldFlush)
        {
            bool streamFlushed = false;

            var stream = new MockStream
            {
                OnFlushAsync = _ => { streamFlushed = true; return Task.CompletedTask; }
            };

            var con = Connection.FromStream(stream, leaveOpen: true);

            await con.CloseAsync(method);
            Assert.Equal(shouldFlush, streamFlushed);
        }

        [Theory]
        [InlineData(ConnectionCloseMethod.GracefulShutdown, true)]
        [InlineData(ConnectionCloseMethod.Abort, false)]
        [InlineData(ConnectionCloseMethod.Immediate, false)]
        public async Task FromPipe_CloseMethod_Flushed(ConnectionCloseMethod method, bool shouldFlush)
        {
            bool pipeFlushed = false;

            var pipe = new MockPipe
            {
                Input = new MockPipeReader()
                {
                    OnCompleteAsync = _ => default
                },
                Output = new MockPipeWriter()
                {
                    OnFlushAsync = _ => { pipeFlushed = true; return default; },
                    OnCompleteAsync = _ => default
                }
            };

            var con = Connection.FromPipe(pipe);

            await con.CloseAsync(method, new CancellationTokenSource().Token);
            Assert.Equal(shouldFlush, pipeFlushed);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task FromStream_LeaveOpen_StreamDisposed(bool leaveOpen, bool shouldDispose)
        {
            bool streamDisposed = false;

            var stream = new MockStream();
            stream.OnDisposeAsync = delegate { streamDisposed = true; return default; };

            var con = Connection.FromStream(stream, leaveOpen);

            await con.CloseAsync(ConnectionCloseMethod.Immediate);
            Assert.Equal(shouldDispose, streamDisposed);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task FromPipe_LeaveOpen_PipeDisposed(bool leaveOpen, bool shouldDispose)
        {
            bool pipeDisposed = false;

            var pipe = new MockPipe
            {
                OnDisposeAsync = () => { pipeDisposed = true; return default; },
                Input = new MockPipeReader()
                {
                    OnCompleteAsync = _ => default
                },
                Output = new MockPipeWriter()
                {
                    OnFlushAsync = _ => default,
                    OnCompleteAsync = _ => default
                }
            };

            var con = Connection.FromPipe(pipe, leaveOpen);

            await con.CloseAsync(ConnectionCloseMethod.Immediate);
            Assert.Equal(shouldDispose, pipeDisposed);
        }

        [Fact]
        public void FromStream_PropertiesInitialized()
        {
            var properties = new DummyConnectionProperties();
            var localEndPoint = new IPEndPoint(IPAddress.Any, 1);
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 2);

            Connection c = Connection.FromStream(new MockStream(), leaveOpen: false, properties, localEndPoint, remoteEndPoint);
            Assert.Same(properties, c.ConnectionProperties);
            Assert.Same(localEndPoint, c.LocalEndPoint);
            Assert.Same(remoteEndPoint, c.RemoteEndPoint);
        }

        [Fact]
        public void FromPipe_PropertiesInitialized()
        {
            var properties = new DummyConnectionProperties();
            var localEndPoint = new IPEndPoint(IPAddress.Any, 1);
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 2);

            Connection c = Connection.FromPipe(new MockPipe(), leaveOpen: false, properties, localEndPoint, remoteEndPoint);
            Assert.Same(properties, c.ConnectionProperties);
            Assert.Same(localEndPoint, c.LocalEndPoint);
            Assert.Same(remoteEndPoint, c.RemoteEndPoint);
        }

        private sealed class DummyConnectionProperties : IConnectionProperties
        {
            public bool TryGet(Type propertyKey, [NotNullWhen(true)] out object property)
            {
                throw new NotImplementedException();
            }
        }
    }
}
