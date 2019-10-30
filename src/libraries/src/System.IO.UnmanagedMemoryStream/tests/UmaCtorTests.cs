// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class UmaCtorTests
    {
        [Fact]
        public static void UmaCtors()
        {
            using (FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99))
            using (var uma = new UnmanagedMemoryAccessor(fakeBuffer, 0, 0))
            {
                Assert.True(uma.CanRead);
                Assert.False(uma.CanWrite);
                Assert.Equal(0, uma.Capacity);
            }

            using (FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99))
            using (var duma = new DerivedUnmanagedMemoryAccessor())
            {
                Assert.False(duma.CanRead);
                Assert.False(duma.CanWrite);
                Assert.Equal(0, duma.Capacity);
                Assert.False(duma.IsOpen);
                duma.Initialize(fakeBuffer, 0, (long)fakeBuffer.ByteLength, FileAccess.ReadWrite);
                Assert.True(duma.IsOpen);
                Assert.Throws<InvalidOperationException>(() => duma.Initialize(fakeBuffer, 0, (long)fakeBuffer.ByteLength, FileAccess.ReadWrite));
            }
        }

        [Fact]
        public static void UmaCtorsThatFail()
        {
            FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99);
            FakeSafeBuffer nullBuffer = null;

            Assert.Throws<ArgumentNullException>(() => new UnmanagedMemoryAccessor(nullBuffer, 0, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 1, 2, (FileAccess)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 1, 2, (FileAccess)42));

            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryAccessor(fakeBuffer, 2, 999));
            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryAccessor(fakeBuffer, 999, 9));
            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryAccessor(fakeBuffer, 1, 100));

            AssertExtensions.Throws<ArgumentException>(null, () => new UnmanagedMemoryAccessor(fakeBuffer, int.MaxValue, 1));
        }

        // Derived class used to exercise protected members and to test behaviors before and after initialization
        private sealed class DerivedUnmanagedMemoryAccessor : UnmanagedMemoryAccessor
        {
            internal DerivedUnmanagedMemoryAccessor() { }

            internal void Initialize(FakeSafeBuffer buffer, long offset, long capacity, FileAccess access)
            {
                base.Initialize(buffer, offset, capacity, access);
            }

            internal new bool IsOpen { get { return base.IsOpen; } }
        }
    }
}
