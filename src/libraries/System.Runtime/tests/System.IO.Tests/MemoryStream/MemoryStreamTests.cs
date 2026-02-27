// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class MemoryStreamTests : StandaloneStreamConformanceTests
    {
        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(new MemoryStream(initialData ?? Array.Empty<byte>(), writable: false));

        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(
                initialData != null ? new MemoryStream(initialData) :
                new MemoryStream());

        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(null);

        [Fact]
        public static void MemoryStream_WriteToTests()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                // [] Write to FileStream, check the filestream
                ms2.Write(bytArr, 0, bytArr.Length);

                using (MemoryStream readonlyStream = new MemoryStream())
                {
                    ms2.WriteTo(readonlyStream);
                    readonlyStream.Flush();
                    readonlyStream.Position = 0;
                    bytArrRet = new byte[(int)readonlyStream.Length];
                    readonlyStream.Read(bytArrRet, 0, (int)readonlyStream.Length);
                    for (int i = 0; i < bytArr.Length; i++)
                    {
                        Assert.Equal(bytArr[i], bytArrRet[i]);
                    }
                }
            }

            // [] Write to memoryStream, check the memoryStream
            using (MemoryStream ms2 = new MemoryStream())
            using (MemoryStream ms3 = new MemoryStream())
            {
                byte[] bytArrRet;
                byte[] bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

                ms2.Write(bytArr, 0, bytArr.Length);
                ms2.WriteTo(ms3);
                ms3.Position = 0;
                bytArrRet = new byte[(int)ms3.Length];
                ms3.Read(bytArrRet, 0, (int)ms3.Length);
                for (int i = 0; i < bytArr.Length; i++)
                {
                    Assert.Equal(bytArr[i], bytArrRet[i]);
                }
            }
        }

        [Fact]
        public static void MemoryStream_WriteToTests_Negative()
        {
            using (MemoryStream ms2 = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>(() => ms2.WriteTo(null));

                ms2.Write(new byte[] { 1 }, 0, 1);
                MemoryStream readonlyStream = new MemoryStream(new byte[1028], false);
                Assert.Throws<NotSupportedException>(() => ms2.WriteTo(readonlyStream));

                readonlyStream.Dispose();

                // [] Pass in a closed stream
                Assert.Throws<ObjectDisposedException>(() => ms2.WriteTo(readonlyStream));
            }
        }

        public static IEnumerable<object[]> MemoryStream_PositionOverflow_Throws_MemberData() =>
            from mode in Enum.GetValues<SeekMode>()
            from bufferContext in
                new (int bufferSize, int origin)[]
                {
                    (0, 0),
                    (1, 0),
                    (1, 1),
                    (10, 0),
                    (10, 5),
                    (10, 10),
                }
            select new object[] {mode, bufferContext.bufferSize, bufferContext.origin};

        [Theory]
        [MemberData(nameof(MemoryStream_PositionOverflow_Throws_MemberData))]
        public void MemoryStream_SeekOverflow_Throws(SeekMode mode, int bufferSize, int origin)
        {
            byte[] buffer = new byte[bufferSize];
            using (MemoryStream ms = new MemoryStream(buffer, origin, buffer.Length - origin, true))
            {
                Seek(mode, ms, Array.MaxLength - origin);
                Assert.Throws<ArgumentOutOfRangeException>(() => Seek(mode, ms, (long)Array.MaxLength - origin + 1));
                Assert.ThrowsAny<Exception>(() => Seek(mode, ms, long.MinValue + 1));
                Assert.ThrowsAny<Exception>(() => Seek(mode, ms, long.MaxValue - 1));
            }
        }

        [Fact]
        public void DerivedMemoryStream_ReadWriteSpanCalled_ReadWriteArrayUsed()
        {
            var s = new ReadWriteOverridingMemoryStream();
            Assert.False(s.WriteArrayInvoked);
            Assert.False(s.ReadArrayInvoked);

            s.Write((ReadOnlySpan<byte>)new byte[1]);
            Assert.True(s.WriteArrayInvoked);
            Assert.False(s.ReadArrayInvoked);

            s.Position = 0;
            s.Read((Span<byte>)new byte[1]);
            Assert.True(s.WriteArrayInvoked);
            Assert.True(s.ReadArrayInvoked);
        }

        [Fact]
        public async Task DerivedMemoryStream_ReadWriteAsyncMemoryCalled_ReadWriteAsyncArrayUsed()
        {
            var s = new ReadWriteOverridingMemoryStream();
            Assert.False(s.WriteArrayInvoked);
            Assert.False(s.ReadArrayInvoked);

            await s.WriteAsync((ReadOnlyMemory<byte>)new byte[1]);
            Assert.True(s.WriteArrayInvoked);
            Assert.False(s.ReadArrayInvoked);

            s.Position = 0;
            await s.ReadAsync((Memory<byte>)new byte[1]);
            Assert.True(s.WriteArrayInvoked);
            Assert.True(s.ReadArrayInvoked);
        }

        [Fact]
        [SkipOnCI("Skipping on CI due to large memory allocation")]
        public void MemoryStream_CapacityBoundaryChecks()
        {
            int MaxSupportedLength = Array.MaxLength;

            using (var ms = new MemoryStream())
            {
                ms.Capacity = MaxSupportedLength - 1;
                Assert.Equal(MaxSupportedLength - 1, ms.Capacity);

                ms.Capacity = MaxSupportedLength;
                Assert.Equal(MaxSupportedLength, ms.Capacity);

                Assert.Throws<ArgumentOutOfRangeException>(() => ms.Capacity = MaxSupportedLength + 1);

                Assert.Throws<ArgumentOutOfRangeException>(() => ms.Capacity = int.MaxValue);
            }
        }

        private class ReadWriteOverridingMemoryStream : MemoryStream
        {
            public bool ReadArrayInvoked, WriteArrayInvoked;
            public bool ReadAsyncArrayInvoked, WriteAsyncArrayInvoked;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadArrayInvoked = true;
                return base.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteArrayInvoked = true;
                base.Write(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ReadAsyncArrayInvoked = true;
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                WriteAsyncArrayInvoked = true;
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_BasicRead()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<byte> rom = data.AsMemory();

            using var ms = new MemoryStream(rom);
            Assert.True(ms.CanRead);
            Assert.True(ms.CanSeek);
            Assert.False(ms.CanWrite);
            Assert.Equal(5, ms.Length);
            Assert.Equal(0, ms.Position);

            byte[] readBuf = new byte[5];
            int bytesRead = ms.Read(readBuf, 0, 5);
            Assert.Equal(5, bytesRead);
            Assert.Equal(data, readBuf);
            Assert.Equal(5, ms.Position);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_BasicReadWrite()
        {
            byte[] data = new byte[10];
            Memory<byte> mem = data.AsMemory();

            using var ms = new MemoryStream(mem);
            Assert.True(ms.CanRead);
            Assert.True(ms.CanSeek);
            Assert.True(ms.CanWrite);
            Assert.Equal(10, ms.Capacity);

            ms.Write(new byte[] { 10, 20, 30 }, 0, 3);
            Assert.Equal(3, ms.Position);

            ms.Position = 0;
            byte[] readBuf = new byte[3];
            int bytesRead = ms.Read(readBuf, 0, 3);
            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, readBuf);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_NotWritable()
        {
            Memory<byte> mem = new byte[5].AsMemory();
            using var ms = new MemoryStream(mem, writable: false);

            Assert.True(ms.CanRead);
            Assert.False(ms.CanWrite);
            Assert.Throws<NotSupportedException>(() => ms.WriteByte(1));
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_WriteThrows()
        {
            ReadOnlyMemory<byte> rom = new byte[] { 1, 2, 3 }.AsMemory();
            using var ms = new MemoryStream(rom);

            Assert.Throws<NotSupportedException>(() => ms.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => ms.Write(new ReadOnlySpan<byte>(new byte[1])));
            Assert.Throws<NotSupportedException>(() => ms.WriteByte(1));
            Assert.Throws<NotSupportedException>(() => ms.SetLength(1));
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_ReadByte()
        {
            ReadOnlyMemory<byte> rom = new byte[] { 42, 99 }.AsMemory();
            using var ms = new MemoryStream(rom);

            Assert.Equal(42, ms.ReadByte());
            Assert.Equal(99, ms.ReadByte());
            Assert.Equal(-1, ms.ReadByte());
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_ReadSpan()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<byte> rom = data.AsMemory();

            using var ms = new MemoryStream(rom);
            byte[] buf = new byte[3];
            int n = ms.Read(buf.AsSpan());
            Assert.Equal(3, n);
            Assert.Equal(new byte[] { 1, 2, 3 }, buf);
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_Seek()
        {
            ReadOnlyMemory<byte> rom = new byte[] { 10, 20, 30, 40, 50 }.AsMemory();
            using var ms = new MemoryStream(rom);

            Assert.Equal(2, ms.Seek(2, SeekOrigin.Begin));
            Assert.Equal(30, ms.ReadByte());

            Assert.Equal(1, ms.Seek(-2, SeekOrigin.Current));
            Assert.Equal(20, ms.ReadByte());

            Assert.Equal(4, ms.Seek(-1, SeekOrigin.End));
            Assert.Equal(50, ms.ReadByte());

            Assert.Throws<IOException>(() => ms.Seek(-1, SeekOrigin.Begin));
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_Position()
        {
            ReadOnlyMemory<byte> rom = new byte[] { 1, 2, 3 }.AsMemory();
            using var ms = new MemoryStream(rom);

            ms.Position = 2;
            Assert.Equal(2, ms.Position);
            Assert.Equal(3, ms.ReadByte());

            Assert.Throws<ArgumentOutOfRangeException>(() => ms.Position = -1);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_SetLength()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            Memory<byte> mem = data.AsMemory();
            using var ms = new MemoryStream(mem);

            ms.SetLength(3);
            Assert.Equal(3, ms.Length);

            ms.SetLength(5);
            Assert.Equal(5, ms.Length);

            Assert.Throws<NotSupportedException>(() => ms.SetLength(11));
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_ToArray()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<byte> rom = data.AsMemory();
            using var ms = new MemoryStream(rom);

            byte[] arr = ms.ToArray();
            Assert.Equal(data, arr);
            Assert.NotSame(data, arr);
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_GetBuffer_Throws()
        {
            ReadOnlyMemory<byte> rom = new byte[5].AsMemory();
            using var ms = new MemoryStream(rom);

            Assert.Throws<UnauthorizedAccessException>(() => ms.GetBuffer());
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_TryGetBuffer_ReturnsFalse()
        {
            ReadOnlyMemory<byte> rom = new byte[5].AsMemory();
            using var ms = new MemoryStream(rom);

            Assert.False(ms.TryGetBuffer(out ArraySegment<byte> buffer));
            Assert.Equal(0, buffer.Offset);
            Assert.Equal(0, buffer.Count);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_WriteTo()
        {
            byte[] data = new byte[] { 1, 2, 3 };
            Memory<byte> mem = data.AsMemory();
            using var ms = new MemoryStream(mem);
            using var dest = new MemoryStream();

            ms.WriteTo(dest);
            Assert.Equal(data, dest.ToArray());
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_CopyTo()
        {
            byte[] data = new byte[] { 10, 20, 30, 40, 50 };
            ReadOnlyMemory<byte> rom = data.AsMemory();
            using var ms = new MemoryStream(rom);
            using var dest = new MemoryStream();

            ms.Position = 2;
            ms.CopyTo(dest);
            Assert.Equal(new byte[] { 30, 40, 50 }, dest.ToArray());
            Assert.Equal(5, ms.Position);
        }

        [Fact]
        public static async Task MemoryStream_Ctor_ReadOnlyMemory_CopyToAsync()
        {
            byte[] data = new byte[] { 10, 20, 30, 40, 50 };
            ReadOnlyMemory<byte> rom = data.AsMemory();
            using var ms = new MemoryStream(rom);
            using var dest = new MemoryStream();

            ms.Position = 1;
            await ms.CopyToAsync(dest);
            Assert.Equal(new byte[] { 20, 30, 40, 50 }, dest.ToArray());
        }

        [Fact]
        public static async Task MemoryStream_Ctor_ReadOnlyMemory_ReadAsync()
        {
            byte[] data = new byte[] { 5, 10, 15, 20 };
            ReadOnlyMemory<byte> rom = data.AsMemory();
            using var ms = new MemoryStream(rom);

            byte[] buf = new byte[4];
            int n = await ms.ReadAsync(buf, 0, 4);
            Assert.Equal(4, n);
            Assert.Equal(data, buf);
        }

        [Fact]
        public static async Task MemoryStream_Ctor_Memory_WriteAsync()
        {
            byte[] data = new byte[5];
            Memory<byte> mem = data.AsMemory();
            using var ms = new MemoryStream(mem);

            await ms.WriteAsync(new byte[] { 1, 2, 3 }, 0, 3);
            Assert.Equal(3, ms.Position);

            ms.Position = 0;
            byte[] readBuf = new byte[3];
            await ms.ReadAsync(readBuf, 0, 3);
            Assert.Equal(new byte[] { 1, 2, 3 }, readBuf);
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_Dispose()
        {
            ReadOnlyMemory<byte> rom = new byte[] { 1, 2, 3 }.AsMemory();
            var ms = new MemoryStream(rom);

            ms.Dispose();

            Assert.False(ms.CanRead);
            Assert.False(ms.CanSeek);
            Assert.False(ms.CanWrite);
            Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => ms.Position);

            byte[] arr = ms.ToArray();
            Assert.Equal(new byte[] { 1, 2, 3 }, arr);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_NotExpandable()
        {
            Memory<byte> mem = new byte[5].AsMemory();
            using var ms = new MemoryStream(mem);

            Assert.Equal(5, ms.Capacity);
            Assert.Throws<NotSupportedException>(() => ms.Capacity = 10);

            ms.Capacity = 5;
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_WriteSpan()
        {
            byte[] data = new byte[10];
            Memory<byte> mem = data.AsMemory();
            using var ms = new MemoryStream(mem);

            ms.Write(new ReadOnlySpan<byte>(new byte[] { 100, 200 }));
            Assert.Equal(2, ms.Position);

            ms.Position = 0;
            Span<byte> readBuf = stackalloc byte[2];
            int n = ms.Read(readBuf);
            Assert.Equal(2, n);
            Assert.Equal(100, readBuf[0]);
            Assert.Equal(200, readBuf[1]);
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_EmptyMemory()
        {
            ReadOnlyMemory<byte> rom = ReadOnlyMemory<byte>.Empty;
            using var ms = new MemoryStream(rom);

            Assert.Equal(0, ms.Length);
            Assert.Equal(0, ms.Position);
            Assert.Equal(-1, ms.ReadByte());
            Assert.Equal(0, ms.Read(new byte[5], 0, 5));
            Assert.Empty(ms.ToArray());
        }

        [Fact]
        public static void MemoryStream_Ctor_ReadOnlyMemory_SlicedMemory()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            ReadOnlyMemory<byte> rom = data.AsMemory(3, 4);

            using var ms = new MemoryStream(rom);
            Assert.Equal(4, ms.Length);

            byte[] readBuf = new byte[4];
            int n = ms.Read(readBuf, 0, 4);
            Assert.Equal(4, n);
            Assert.Equal(new byte[] { 4, 5, 6, 7 }, readBuf);
        }

        [Fact]
        public static void MemoryStream_Ctor_Memory_WriteBeyondCapacity_Throws()
        {
            Memory<byte> mem = new byte[3].AsMemory();
            using var ms = new MemoryStream(mem);

            ms.Write(new byte[] { 1, 2, 3 }, 0, 3);
            Assert.Throws<NotSupportedException>(() => ms.WriteByte(4));
        }
    }
}
