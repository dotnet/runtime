// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class IntPtrArrayTests
    {
        public static IEnumerable<object[]> CopyTo_TestData()
        {
            yield return new object[] { Enumerable.Range(0, 10).Select(i => (IntPtr)i).ToArray() };
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public void CopyTo_Roundtrip_MatchesOriginalInput(IntPtr[] values)
        {
            int sizeOfArray = IntPtr.Size * values.Length;
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.Copy(values, 0, ptr, values.Length);

                IntPtr[] array1 = new IntPtr[values.Length];
                Marshal.Copy(ptr, array1, 0, values.Length);
                Assert.Equal<IntPtr>(values, array1);

                Marshal.Copy(values, 2, ptr, values.Length - 4);
                IntPtr[] array2 = new IntPtr[values.Length];
                Marshal.Copy(ptr, array2, 2, values.Length - 4);
                Assert.Equal<IntPtr>(values.AsSpan(2, values.Length - 4).ToArray(), array2.AsSpan(2, values.Length - 4).ToArray());
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void CopyTo_NullDestination_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("destination", () => Marshal.Copy(new IntPtr[10], 0, IntPtr.Zero, 0));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => Marshal.Copy(new IntPtr(1), (IntPtr[])null, 0, 0));
        }

        [Fact]
        public void CopyTo_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => Marshal.Copy((IntPtr[])null, 0, new IntPtr(1), 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => Marshal.Copy(IntPtr.Zero, new IntPtr[10], 0, 0));
        }

        [Fact]
        public void CopyTo_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            IntPtr[] array = new IntPtr[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(IntPtr.Size * array.Length);
            try
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => Marshal.Copy(array, -1, ptr, 10));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => Marshal.Copy(ptr, array, -1, 10));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void CopyTo_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            IntPtr[] array = new IntPtr[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(IntPtr.Size * array.Length);
            try
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => Marshal.Copy(array, 0, ptr, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => Marshal.Copy(ptr, array, 0, -1));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Theory]
        [InlineData(0, 11)]
        [InlineData(11, 1)]
        [InlineData(2, 10)]
        public void CopyTo_InvalidStartIndexLength_ThrowsArgumentOutOfRangeException(int startIndex, int length)
        {
            IntPtr[] array = new IntPtr[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(IntPtr.Size * array.Length);
            try
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => Marshal.Copy(array, startIndex, ptr, length));
                AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => Marshal.Copy(ptr, array, startIndex, length));
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
