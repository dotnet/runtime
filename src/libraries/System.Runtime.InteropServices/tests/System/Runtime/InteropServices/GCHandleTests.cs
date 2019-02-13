// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GCHandleTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var handle = new GCHandle();
            Assert.Throws<InvalidOperationException>(() => handle.Target);
            Assert.False(handle.IsAllocated);

            Assert.Equal(IntPtr.Zero, GCHandle.ToIntPtr(handle));
            Assert.Equal(IntPtr.Zero, (IntPtr)handle);
        }

        public static IEnumerable<object[]> Alloc_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "String" };
            yield return new object[] { 123 };
            yield return new object[] { new int[1] };
            yield return new object[] { new NonBlittable[1] };
            yield return new object[] { new object[1] };
            yield return new object[] { new NonBlittable() };
        }

        [Theory]
        [MemberData(nameof(Alloc_TestData))]
        public void Alloc_Value_ReturnsExpected(object value)
        {
            GCHandle handle = GCHandle.Alloc(value);
            ValidateGCHandle(handle, GCHandleType.Normal, value);
        }
        
        public static IEnumerable<object[]> Alloc_Type_TestData()
        {
            foreach (object[] data in Alloc_TestData())
            {
                yield return new object[] { data[0], GCHandleType.Normal };
                yield return new object[] { data[0], GCHandleType.Weak };
                yield return new object[] { data[0], GCHandleType.WeakTrackResurrection };
            }

            yield return new object[] { null, GCHandleType.Pinned };
            yield return new object[] { "", GCHandleType.Pinned };
            yield return new object[] { 1, GCHandleType.Pinned };
            yield return new object[] { new Blittable(), GCHandleType.Pinned };
            yield return new object[] { new Blittable(), GCHandleType.Pinned };
            yield return new object[] { new Blittable[0], GCHandleType.Pinned };
        }

        [Theory]
        [MemberData(nameof(Alloc_Type_TestData))]
        public static void Alloc_Type_ReturnsExpected(object value, GCHandleType type)
        {
            GCHandle handle = GCHandle.Alloc(value, type);
            ValidateGCHandle(handle, type, value);
        }

        public static IEnumerable<object[]> InvalidPinnedObject_TestData()
        {
            yield return new object[] { new NonBlittable() };
            yield return new object[] { new object[0] };
            yield return new object[] { new NonBlittable[0] };
        }

        [Theory]
        [MemberData(nameof(InvalidPinnedObject_TestData))]
        public void Alloc_InvalidPinnedObject_ThrowsArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => GCHandle.Alloc(value, GCHandleType.Pinned));
        }

        [Theory]
        [InlineData(GCHandleType.Weak - 1)]
        [InlineData(GCHandleType.Pinned + 1)]
        public void Alloc_InvalidGCHandleType_ThrowsArgumentOutOfRangeException(GCHandleType type)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("type", () => GCHandle.Alloc(new object(), type));
        }

        [Fact]
        public void FromIntPtr_Zero_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => GCHandle.FromIntPtr(IntPtr.Zero));
        }

        [Fact]
        public void AddrOfPinnedObject_NotInitialized_ThrowsInvalidOperationException()
        {
            var handle = new GCHandle();
            Assert.Throws<InvalidOperationException>(() => handle.AddrOfPinnedObject());
        }

        [Fact]
        public void AddrOfPinnedObject_NotPinned_ThrowsInvalidOperationException()
        {
            GCHandle handle = GCHandle.Alloc(new object());
            try
            {
                Assert.Throws<InvalidOperationException>(() => handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        [Fact]
        public void Free_NotInitialized_ThrowsInvalidOperationException()
        {
            var handle = new GCHandle();
            Assert.Throws<InvalidOperationException>(() => handle.Free());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            GCHandle handle = GCHandle.Alloc(new object());
            yield return new object[] { handle, handle, true };
            yield return new object[] { GCHandle.Alloc(new object()), GCHandle.Alloc(new object()), false };

            yield return new object[] { GCHandle.Alloc(new object()), new object(), false };
            yield return new object[] { GCHandle.Alloc(new object()), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(GCHandle handle, object other, bool expected)
        {
            try
            {
                Assert.Equal(expected, handle.Equals(other));
                if (other is GCHandle otherHandle)
                {
                    Assert.Equal(expected, handle == otherHandle);
                    Assert.Equal(!expected, handle != otherHandle);
                }
            }
            finally
            {
                handle.Free();
                if (other is GCHandle otherHandle && !expected)
                {
                    otherHandle.Free();
                }
            }
        }

        private static void ValidateGCHandle(GCHandle handle, GCHandleType type, object target)
        {
            try
            {
                Assert.Equal(target, handle.Target);
                Assert.True(handle.IsAllocated);

                Assert.NotEqual(IntPtr.Zero, GCHandle.ToIntPtr(handle));
                Assert.Equal(GCHandle.ToIntPtr(handle), (IntPtr)handle);
                Assert.Equal(((IntPtr)handle).GetHashCode(), handle.GetHashCode());

                if (type == GCHandleType.Pinned)
                {
                    if (target == null)
                    {
                        Assert.Equal(IntPtr.Zero, handle.AddrOfPinnedObject());
                    }
                    else
                    {
                        Assert.NotEqual(IntPtr.Zero, handle.AddrOfPinnedObject());
                    }
                }

            }
            finally
            {
                handle.Free();
                Assert.False(handle.IsAllocated);
            }
        }

        public struct Blittable
        {
            public int Object { get; set; }
        }

        public struct NonBlittable
        {
            public List<string> Object { get; set; }
        }
    }
}
