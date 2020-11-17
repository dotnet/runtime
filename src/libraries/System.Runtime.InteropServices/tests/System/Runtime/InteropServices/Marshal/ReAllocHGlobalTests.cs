// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ReAllocHGlobalTests
    {
        [Fact]
        public void ReAllocHGlobal_Invoke_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocHGlobal((IntPtr)Size);
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
                    p2 = Marshal.ReAllocHGlobal(p2, (IntPtr)(Size + add));
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
                Marshal.FreeHGlobal(p2);
            }
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [Theory]
        public void ReAllocHGlobal_PositiveSize(int size)
        {
            IntPtr p = Marshal.ReAllocHGlobal(IntPtr.Zero, (IntPtr)size);
            Assert.NotEqual(IntPtr.Zero, p);

            IntPtr p1 = Marshal.ReAllocHGlobal(p, (IntPtr)(size + 1));
            Assert.NotEqual(IntPtr.Zero, p1);

            // ReAllocHGlobal never returns null, even for 0 size (different from standard C/C++ realloc)
            IntPtr p2 = Marshal.ReAllocHGlobal(p1, IntPtr.Zero);
            Assert.NotEqual(IntPtr.Zero, p2);

            Marshal.FreeHGlobal(p2);
        }

        [Fact]
        public void ReAllocHGlobal_NegativeSize_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Marshal.ReAllocHGlobal(IntPtr.Zero, (IntPtr)(-1)));

            IntPtr p = Marshal.AllocHGlobal((IntPtr)1);
            Assert.Throws<OutOfMemoryException>(() => Marshal.ReAllocHGlobal(p, (IntPtr)(-1)));
            Marshal.FreeHGlobal(p);
        }
    }
}
