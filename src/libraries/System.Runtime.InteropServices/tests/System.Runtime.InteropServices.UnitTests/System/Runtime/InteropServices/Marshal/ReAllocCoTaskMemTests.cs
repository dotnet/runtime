// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ReAllocCoTaskMemTests
    {
        [Fact]
        public void ReAllocCoTaskMem_Invoke_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocCoTaskMem(Size);
            IntPtr p2 = p1;
            try
            {
                for (int i = 0; i < Size; i++)
                {
                    Marshal.WriteByte(p1 + i, (byte)i);
                }

                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocCoTaskMem(p2, Size + add);
                    for (int i = 0; i < Size; i++)
                    {
                        Assert.Equal((byte)i, Marshal.ReadByte(p2 + i));
                    }

                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeCoTaskMem(p2);
            }
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [Theory]
        public void ReAllocCoTaskMem_PositiveSize(int size)
        {
            IntPtr p = Marshal.ReAllocCoTaskMem(IntPtr.Zero, size);
            Assert.NotEqual(IntPtr.Zero, p);

            IntPtr p1 = Marshal.ReAllocCoTaskMem(p, size + 1);
            Assert.NotEqual(IntPtr.Zero, p1);

            IntPtr p2 = Marshal.ReAllocCoTaskMem(p1, 0);
            Assert.Equal(IntPtr.Zero, p2);
        }

        [Fact]
        [OuterLoop]
        public void ReAllocCoTaskMem_NegativeSize_ThrowsOutOfMemoryException()
        {
            // -1 is treated as (uint)-1 by ReAllocCoTaskMem. The allocation may succeed on 64-bit machines.

            try
            {
                IntPtr p1 = Marshal.ReAllocCoTaskMem(IntPtr.Zero, -1);
                Assert.NotEqual(IntPtr.Zero, p1);
                Marshal.FreeCoTaskMem(p1);
            }
            catch (OutOfMemoryException)
            {
            }

            IntPtr p2 = Marshal.AllocCoTaskMem(1);
            try
            {
                p2 = Marshal.ReAllocCoTaskMem(p2, -1);
                Assert.NotEqual(IntPtr.Zero, p2);
            }
            catch (OutOfMemoryException)
            {
            }
            Marshal.FreeCoTaskMem(p2);
        }
    }
}
