// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        }

        [Fact]
        public static void UmaCtorsThatFail()
        {
            FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99);
            FakeSafeBuffer nullBuffer = null;

            Assert.Throws<ArgumentNullException>(() => new UnmanagedMemoryAccessor(nullBuffer, 0, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryAccessor(fakeBuffer, -1, 1));

            Assert.Throws<ArgumentException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 2, 999));
            Assert.Throws<ArgumentException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 999, 9));
            Assert.Throws<ArgumentException>(() => new UnmanagedMemoryAccessor(fakeBuffer, 1, 100));

            Assert.Throws<ArgumentException>(() => new UnmanagedMemoryAccessor(fakeBuffer, Int32.MaxValue, 1));
        }
    }
}
