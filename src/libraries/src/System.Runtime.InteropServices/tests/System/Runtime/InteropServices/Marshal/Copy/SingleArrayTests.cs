// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SingleArrayTests
    {
        [Theory]
        [InlineData(new float[] { 0.0F, 1.0F, 2.0F, 3.0F, 4.0F, 5.0F, 6.0F, 7.0F, 8.0F, 9.0F })]
        public void CopyTo_Roundtrip_MatchesOriginalInput(float[] values)
        {
            int sizeOfArray = sizeof(float) * values.Length;
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.Copy(values, 0, ptr, values.Length);

                float[] array1 = new float[values.Length];
                Marshal.Copy(ptr, array1, 0, values.Length);
                Assert.Equal<float>(values, array1);

                Marshal.Copy(values, 2, ptr, values.Length - 4);
                float[] array2 = new float[values.Length];
                Marshal.Copy(ptr, array2, 2, values.Length - 4);
                Assert.Equal<float>(values.AsSpan(2, values.Length - 4).ToArray(), array2.AsSpan(2, values.Length - 4).ToArray());
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void CopyTo_NullDestination_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("destination", () => Marshal.Copy(new float[10], 0, IntPtr.Zero, 0));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => Marshal.Copy(new IntPtr(1), (float[])null, 0, 0));
        }

        [Fact]
        public void CopyTo_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => Marshal.Copy((float[])null, 0, new IntPtr(1), 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => Marshal.Copy(IntPtr.Zero, new float[10], 0, 0));
        }

        [Fact]
        public void CopyTo_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            float[] array = new float[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(float) * array.Length);
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
            float[] array = new float[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(float) * array.Length);
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
            float[] array = new float[10];
            IntPtr ptr = Marshal.AllocCoTaskMem(sizeof(float) * array.Length);
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
