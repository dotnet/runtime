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
                    (Array.MaxLength, 0),
                    (Array.MaxLength, Array.MaxLength)
                }
            select new object[] {mode, bufferContext.bufferSize, bufferContext.origin};

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [MemberData(nameof(MemoryStream_PositionOverflow_Throws_MemberData))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "https://github.com/dotnet/runtime/issues/92467")]
        public void MemoryStream_SeekOverflow_Throws(SeekMode mode, int bufferSize, int origin)
        {
            byte[] buffer = new byte[bufferSize];
            using (MemoryStream ms = new MemoryStream(buffer, origin, buffer.Length - origin, true))
            {
                Seek(mode, ms, int.MaxValue - origin);
                Assert.Throws<ArgumentOutOfRangeException>(() => Seek(mode, ms, (long)int.MaxValue - origin + 1));
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
    }
}
