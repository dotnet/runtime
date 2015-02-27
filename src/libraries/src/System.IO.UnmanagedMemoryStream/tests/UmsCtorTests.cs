// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public class UmsCtorTests
    {
        [Fact]
        public static void CtorsThatFail()
        {
            unsafe
            {
                Assert.Throws<ArgumentNullException>(() => { var ums = new UnmanagedMemoryStream(null, 0); });

                TestSafeBuffer nullBuffer = null;
                FakeSafeBuffer fakeBuffer = new FakeSafeBuffer(99);
                Assert.Throws<ArgumentNullException>(() => new UnmanagedMemoryStream(nullBuffer, 0, 1));

                Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 2, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 1, 2, (FileAccess)(-1)));
                Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedMemoryStream(fakeBuffer, 1, 2, (FileAccess)42));

                Assert.Throws<ArgumentException>(() => new UnmanagedMemoryStream(fakeBuffer, 2, 999));
                Assert.Throws<ArgumentException>(() => new UnmanagedMemoryStream(fakeBuffer, 999, 9));
                Assert.Throws<ArgumentException>(() => new UnmanagedMemoryStream(fakeBuffer, 1, 100));

                Assert.Throws<ArgumentException>(() => new UnmanagedMemoryStream(fakeBuffer, Int32.MaxValue, 1));
            }
        }

        [Fact]
        public static void PointerCtor()
        {
            int someInt32 = 42;
            unsafe
            {
                int* pInt32 = &someInt32;
                byte* pByte = (byte*)pInt32;
                using (var stream = new UnmanagedMemoryStream(pByte, 0))
                {
                    Assert.True(stream.CanRead);
                    Assert.True(stream.CanSeek);
                    Assert.False(stream.CanWrite);

                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Capacity);
                    Assert.Equal(0, stream.Position);

                    if (stream.PositionPointer != pByte)
                    {
                        Assert.True(false);
                    }
                }
            }
        }
    }
}
