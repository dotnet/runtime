// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices
{
    public static class MarshalTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocHGlobal_Int32_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocHGlobal(size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocHGlobal_IntPtr_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocHGlobal((IntPtr)size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        [Fact]
        public static void ReAllocHGlobal_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocHGlobal((IntPtr)Size);
            IntPtr p2 = p1;
            try
            {
                WriteBytes(p1, Size);
                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocHGlobal(p2, (IntPtr)(Size + add));
                    VerifyBytes(p2, Size);
                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeHGlobal(p2);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocCoTaskMem_Int32_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocCoTaskMem(size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeCoTaskMem(p);
            }
        }

        [Fact]
        public static void ReAllocCoTaskMem_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocCoTaskMem(Size);
            IntPtr p2 = p1;
            try
            {
                WriteBytes(p1, Size);
                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocCoTaskMem(p2, Size + add);
                    VerifyBytes(p2, Size);
                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeCoTaskMem(p2);
            }
        }

        private static void WriteBytes(IntPtr p, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Marshal.WriteByte(p + i, (byte)i);
            }
        }

        private static void VerifyBytes(IntPtr p, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Assert.Equal((byte)i, Marshal.ReadByte(p + i));
            }
        }

        [Fact]
        public static void GetHRForException()
        {
            Exception e = new Exception();

            try
            {
                Assert.Equal(0, Marshal.GetHRForException(null));
                
                Assert.InRange(Marshal.GetHRForException(e), int.MinValue, -1);            
                Assert.Equal(e.HResult, Marshal.GetHRForException(e));
            }
            finally
            {
                // This GetExceptionForHR call is needed to 'eat' the IErrorInfo put to TLS by 
                // Marshal.GetHRForException call above. Otherwise other Marshal.GetExceptionForHR 
                // calls would randomly return previous exception objects passed to 
                // Marshal.GetHRForException.
                // The correct way is to use Marshal.GetHRForException at interop boundary only, but for the
                // time being we'll keep this code as-is.
                Marshal.GetExceptionForHR(e.HResult);
            }
        }
    }
}
