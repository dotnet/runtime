// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class UmsCtorTests
    {
        [Fact]
        public static unsafe void CtorsThatFail()
        {
            Assert.Throws<ArgumentNullException>(() => { var ums = new UnmanagedMemoryStream(null, 0); });

            TestSafeBuffer nullBuffer = null;
            FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99);
            Assert.Throws<ArgumentNullException>(() => new UnmanagedMemoryStream(nullBuffer, 0, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 1, 2, (FileAccess)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 1, 2, (FileAccess)42));

            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryStream(fakeBuffer, 2, 999));
            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryStream(fakeBuffer, 999, 9));
            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryStream(fakeBuffer, 1, 100));

            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryStream(fakeBuffer, int.MaxValue, 1));
        }

        [Fact]
        public static unsafe void PointerCtor()
        {
            int someInt32 = 42;
            int* pInt32 = &someInt32;
            byte* pByte = (byte*)pInt32;

            using (var stream = new UnmanagedMemoryStream(pByte, 0))
            {
                Assert.True(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.False(stream.CanWrite);
                Assert.False(stream.CanTimeout);

                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Capacity);
                Assert.Equal(0, stream.Position);

                Assert.Throws<InvalidOperationException>(() => stream.ReadTimeout);
                Assert.Throws<InvalidOperationException>(() => stream.ReadTimeout = 42);
                Assert.Throws<InvalidOperationException>(() => stream.WriteTimeout);
                Assert.Throws<InvalidOperationException>(() => stream.WriteTimeout = 42);

                Assert.True(stream.PositionPointer == pByte);
            }

            using (var stream = new DerivedUnmanagedMemoryStream())
            {
                Assert.False(stream.CanRead);
                Assert.False(stream.CanSeek);
                Assert.False(stream.CanWrite);
                Assert.False(stream.CanTimeout);

                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Initialize(pByte, -1, 4, FileAccess.Read));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Initialize(pByte, 1, -4, FileAccess.Read));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Initialize(pByte, 5, 4, FileAccess.Read));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Initialize(pByte, 1, 4, (FileAccess)12345));
                stream.Initialize(pByte, 1, 4, FileAccess.ReadWrite);
                Assert.Throws<InvalidOperationException>(() => stream.Initialize(pByte, 1, 4, FileAccess.ReadWrite));

                Assert.True(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.True(stream.CanWrite);

                Assert.Equal(1, stream.Length);
                Assert.Equal(4, stream.Capacity);
                Assert.Equal(0, stream.Position);

                Assert.Throws<InvalidOperationException>(() => stream.ReadTimeout);
                Assert.Throws<InvalidOperationException>(() => stream.ReadTimeout = 42);
                Assert.Throws<InvalidOperationException>(() => stream.WriteTimeout);
                Assert.Throws<InvalidOperationException>(() => stream.WriteTimeout = 42);

                Assert.True(stream.PositionPointer == pByte);
            }
        }

        [Fact]
        public static unsafe void BufferCtor()
        {
            const int length = 99;
            using (FakeSafeBuffer buffer = new FakeSafeBuffer(length))
            using (var stream = new DerivedUnmanagedMemoryStream(buffer, length, FileAccess.Read))
            {
                Assert.True(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.False(stream.CanWrite);

                Assert.Equal(length, stream.Length);
                Assert.Equal(length, stream.Capacity);
                Assert.Equal(0, stream.Position);
            }

            using (FakeSafeBuffer buffer = new FakeSafeBuffer(length))
            using (var stream = new DerivedUnmanagedMemoryStream())
            {
                Assert.False(stream.CanRead);
                Assert.False(stream.CanSeek);
                Assert.False(stream.CanWrite);

                stream.Initialize(buffer, 0, length, FileAccess.Write);
                Assert.Throws<InvalidOperationException>(() => stream.Initialize(buffer, 0, length, FileAccess.Write));

                Assert.False(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.True(stream.CanWrite);

                Assert.Equal(length, stream.Length);
                Assert.Equal(length, stream.Capacity);
                Assert.Equal(0, stream.Position);

                Assert.Throws<NotSupportedException>(() => stream.SetLength(1));
            }
        }
    }

    // Derived class used to exercise protected members and to test behaviors before and after initialization
    internal sealed unsafe class DerivedUnmanagedMemoryStream : UnmanagedMemoryStream
    {
        internal DerivedUnmanagedMemoryStream()
        {
        }

        internal DerivedUnmanagedMemoryStream(FakeSafeBuffer buffer, long length, FileAccess access) : base(buffer, 0, length, access)
        {
        }

        internal new void Initialize(byte* pointer, long length, long capacity, FileAccess access)
        {
            base.Initialize(pointer, length, capacity, access);
        }

        internal void Initialize(FakeSafeBuffer buffer, long offset, long capacity, FileAccess access)
        {
            base.Initialize(buffer, offset, capacity, access);
        }
    }
}
