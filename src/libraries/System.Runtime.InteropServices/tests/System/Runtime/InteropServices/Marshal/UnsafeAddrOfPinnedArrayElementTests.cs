// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class UnsafeAddrOfPinnedArrayElementTests
    {
        [Fact]
        public void UnsafeAddrOfPinnedArrayElement_NonGenericPrimitiveArray_ReturnsExpected()
        {
            Array array = new int[] { 1, 2, 3 };
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                Assert.Equal(1, Marshal.ReadInt32(v0));

                IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 1);
                Assert.Equal(2, Marshal.ReadInt32(v1));

                IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 2);
                Assert.Equal(3, Marshal.ReadInt32(v2));
            }
            finally
            {
                handle.Free();
            }
        }

        [Fact]
        public void UnsafeAddrOfPinnedArrayElement_NonGenericStructureArray_ReturnsExpected()
        {
            Array array = new Point[]
            {
                new Point() {x = 100, y = 100 },
                new Point() {x = -1, y = -1 },
                new Point() {x = 0, y = 0 }
            };

            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                Point p0 = Marshal.PtrToStructure<Point>(v0);
                Assert.Equal(100, p0.x);
                Assert.Equal(100, p0.y);

                IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 1);
                Point p1 = Marshal.PtrToStructure<Point>(v1);
                Assert.Equal(-1, p1.x);
                Assert.Equal(-1, p1.y);

                IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 2);
                Point p2 = Marshal.PtrToStructure<Point>(v2);
                Assert.Equal(0, p2.x);
                Assert.Equal(0, p2.y);
            }
            finally
            {
                handle.Free();
            }
        }

        [Fact]
        public void UnsafeAddrOfPinnedArrayElement_GenericPrimitiveArray_ReturnsExpected()
        {
            var array = new int[] { 1, 2, 3 };
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                Assert.Equal(1, Marshal.ReadInt32(v0));

                IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 1);
                Assert.Equal(2, Marshal.ReadInt32(v1));

                IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 2);
                Assert.Equal(3, Marshal.ReadInt32(v2));
            }
            finally
            {
                handle.Free();
            }
        }

        [Fact]
        public void UnsafeAddrOfPinnedArrayElement_GenericStructureArray_ReturnsExpected()
        {
            var array = new Point[]
            {
                new Point() {x = 100, y = 100 },
                new Point() {x = -1, y = -1 },
                new Point() {x = 0, y = 0 }
            };

            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                Point p0 = Marshal.PtrToStructure<Point>(v0);
                Assert.Equal(100, p0.x);
                Assert.Equal(100, p0.y);

                IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 1);
                Point p1 = Marshal.PtrToStructure<Point>(v1);
                Assert.Equal(-1, p1.x);
                Assert.Equal(-1, p1.y);

                IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement(array, 2);
                Point p2 = Marshal.PtrToStructure<Point>(v2);
                Assert.Equal(0, p2.x);
                Assert.Equal(0, p2.y);
            }
            finally
            {
                handle.Free();
            }
        }

        [Fact]
        public void UnsafeAddrOfPinnedArrayElement_NullArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("arr", () => Marshal.UnsafeAddrOfPinnedArrayElement(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("arr", () => Marshal.UnsafeAddrOfPinnedArrayElement((int[])null, 0));
        }

        public struct Point
        {
            public int x;
            public int y;
        }
    }
}
