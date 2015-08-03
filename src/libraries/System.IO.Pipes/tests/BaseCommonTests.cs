// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public abstract class BaseCommonTests
    {
        public static void ConnectedPipeWriteBufferNullThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanWrite);

            Assert.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, 0, 1));
            Assert.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, 0, 1); } );
        }

        public static void ConnectedPipeWriteNegativeOffsetThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanWrite);

            Assert.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Write(new byte[5], -1, 1));

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, -1, 1));

            Assert.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, -1, 1); } );
        }

        public static void ConnectedPipeWriteNegativeCountThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanWrite);

            Assert.Throws<ArgumentOutOfRangeException>("count", () => pipe.Write(new byte[5], 0, -1));

            // offset is checked before count
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Write(new byte[1], -1, -1));

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => pipe.Write(null, -1, -1));

            Assert.Throws<ArgumentOutOfRangeException>("count", () => { pipe.WriteAsync(new byte[5], 0, -1); } );

            // offset is checked before count
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.WriteAsync(new byte[1], -1, -1); } );

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => { pipe.WriteAsync(null, -1, -1); } );
        }

        public static void ConnectedPipeWriteArrayOutOfBoundsThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanWrite);

            // offset out of bounds
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[1], 1, 1));

            // offset out of bounds for 0 count read
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[1], 2, 0));

            // offset out of bounds even for 0 length buffer
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], 1, 0));

            // combination offset and count out of bounds
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[2], 1, 2));

            // edges
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], int.MaxValue, 0));
            Assert.Throws<ArgumentException>(null, () => pipe.Write(new byte[0], int.MaxValue, int.MaxValue));

            Assert.Throws<ArgumentException>(() => pipe.Write(new byte[5], 3, 4));

            // offset out of bounds
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[1], 1, 1); } );

            // offset out of bounds for 0 count read
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[1], 2, 0); } );

            // offset out of bounds even for 0 length buffer
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], 1, 0); } );

            // combination offset and count out of bounds
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[2], 1, 2); } );

            // edges
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], int.MaxValue, 0); } );
            Assert.Throws<ArgumentException>(null, () => { pipe.WriteAsync(new byte[0], int.MaxValue, int.MaxValue); } );

            Assert.Throws<ArgumentException>(() => { pipe.WriteAsync(new byte[5], 3, 4); } );
        }

        public static void ConnectedPipeReadOnlyThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.False(pipe.CanWrite);

            Assert.Throws<NotSupportedException>(() => pipe.Write(new byte[5], 0, 5));

            Assert.Throws<NotSupportedException>(() => pipe.WriteByte(123));

            Assert.Throws<NotSupportedException>(() => pipe.Flush());

            Assert.Throws<NotSupportedException>(() => pipe.OutBufferSize);

            Assert.Throws<NotSupportedException>(() => pipe.WaitForPipeDrain());

            Assert.Throws<NotSupportedException>(() => { pipe.WriteAsync(new byte[5], 0, 5); } );
        }

        public static void ConnectedPipeReadBufferNullThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanRead);

            Assert.Throws<ArgumentNullException>(() => pipe.Read(null, 0, 1));

            Assert.Throws<ArgumentNullException>(() => { pipe.ReadAsync(null, 0, 1); } );
        }

        public static void ConnectedPipeReadNegativeOffsetThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanRead);

            Assert.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Read(new byte[6], -1, 1));

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, 1));

            Assert.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.ReadAsync(new byte[4], -1, 1); } );

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, 1); } );
        }

        public static void ConnectedPipeReadNegativeCountThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanRead);

            Assert.Throws<System.ArgumentOutOfRangeException>("count", () => pipe.Read(new byte[3], 0, -1));

            // offset is checked before count
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Read(new byte[1], -1, -1));

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, -1));

            Assert.Throws<System.ArgumentOutOfRangeException>("count", () => { pipe.ReadAsync(new byte[7], 0, -1); } );

            // offset is checked before count
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.ReadAsync(new byte[2], -1, -1); } );

            // array is checked first
            Assert.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, -1); } );
        }

        public static void ConnectedPipeReadArrayOutOfBoundsThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanRead);

            // offset out of bounds
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 1, 1));

            // offset out of bounds for 0 count read
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 2, 0));

            // offset out of bounds even for 0 length buffer
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], 1, 0));

            // combination offset and count out of bounds
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[2], 1, 2));

            // edges
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, 0));
            Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, int.MaxValue));

            Assert.Throws<ArgumentException>(() => pipe.Read(new byte[5], 3, 4));

            // offset out of bounds
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 1, 1); } );

            // offset out of bounds for 0 count read
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 2, 0); } );

            // offset out of bounds even for 0 length buffer
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], 1, 0); } );

            // combination offset and count out of bounds
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[2], 1, 2); } );

            // edges
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, 0); } );
            Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, int.MaxValue); } );

            Assert.Throws<ArgumentException>(() => { pipe.ReadAsync(new byte[5], 3, 4); } );
        }

        public static void ConnectedPipeWriteOnlyThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);
            Assert.False(pipe.CanRead);

            Assert.Throws<NotSupportedException>(() => pipe.Read(new byte[9], 0, 5));

            Assert.Throws<NotSupportedException>(() => pipe.ReadByte());

            Assert.Throws<NotSupportedException>(() => pipe.InBufferSize);

            Assert.Throws<NotSupportedException>(() => { pipe.ReadAsync(new byte[10], 0, 5); } );
        }

        public static void ConnectedPipeUnsupportedOperationThrows(PipeStream pipe)
        {
            Assert.True(pipe.IsConnected);

            Assert.Throws<NotSupportedException>(() => pipe.Length);

            Assert.Throws<NotSupportedException>(() => pipe.SetLength(10L));

            Assert.Throws<NotSupportedException>(() => pipe.Position);

            Assert.Throws<NotSupportedException>(() => pipe.Position = 10L);

            Assert.Throws<NotSupportedException>(() => pipe.Seek(10L, System.IO.SeekOrigin.Begin));
        }

        public static void ConnectedPipeCancelReadTokenThrows(PipeStream pipe)
        {
            byte[] buffer11 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.True(pipe.IsConnected);
            Assert.True(pipe.CanRead);

            var ctx = new CancellationTokenSource();
            Task readToken = pipe.ReadAsync(buffer11, 0, buffer11.Length, ctx.Token);
            ctx.Cancel();
            Assert.ThrowsAsync<TimeoutException>(() => readToken);
            Assert.ThrowsAsync<TimeoutException>(() => pipe.ReadAsync(buffer11, 0, buffer11.Length, ctx.Token));
        }

        public static void AfterDisconnectReadWritePipeThrows(PipeStream pipe)
        {
            Assert.False(pipe.IsConnected);

            AfterDisconnectReadOnlyPipeThrows(pipe);
            AfterDisconnectWriteOnlyPipeThrows(pipe);
        }

        public static void AfterDisconnectWriteOnlyPipeThrows(PipeStream pipe)
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };

            Assert.Throws<InvalidOperationException>(() => pipe.Write(buffer, 0, buffer.Length));
            Assert.Throws<InvalidOperationException>(() => pipe.WriteByte(5));
            Assert.Throws<InvalidOperationException>(() => { pipe.WriteAsync(buffer, 0, buffer.Length); } );
            Assert.Throws<InvalidOperationException>(() => pipe.Flush());
        }

        public static void AfterDisconnectReadOnlyPipeThrows(PipeStream pipe)
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };

            Assert.Throws<InvalidOperationException>(() => pipe.Read(buffer, 0, buffer.Length));
            Assert.Throws<InvalidOperationException>(() => pipe.ReadByte());
            Assert.Throws<InvalidOperationException>(() => { pipe.ReadAsync(buffer, 0, buffer.Length); } );
            Assert.Throws<InvalidOperationException>(() => pipe.IsMessageComplete);
        }

        public static void WhenDisposedPipeThrows(PipeStream pipe)
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };

            Assert.Throws<ObjectDisposedException>(() => pipe.Write(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => pipe.WriteByte(5));
            Assert.Throws<ObjectDisposedException>(() => { pipe.WriteAsync(buffer, 0, buffer.Length); } );
            Assert.Throws<ObjectDisposedException>(() => pipe.Flush());
            Assert.Throws<ObjectDisposedException>(() => pipe.Read(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => pipe.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => { pipe.ReadAsync(buffer, 0, buffer.Length); } );
            Assert.Throws<ObjectDisposedException>(() => pipe.IsMessageComplete);
        }

        public static void OtherSidePipeDisconnectWriteThrows(PipeStream pipe)
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };

            Assert.Throws<IOException>(() => pipe.Write(buffer, 0, buffer.Length));
            Assert.Throws<IOException>(() => pipe.WriteByte(123));
            Assert.Throws<IOException>(() => { pipe.WriteAsync(buffer, 0, buffer.Length); } );
            Assert.Throws<IOException>(() => pipe.Flush());
        }

        public static void OtherSidePipeDisconnectVerifyRead(PipeStream pipe)
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };

            int length = pipe.Read(buffer, 0, buffer.Length);  // for some reason these are ok
            Assert.Equal(0, length);
            int byt = pipe.ReadByte();
        }
    }
}
