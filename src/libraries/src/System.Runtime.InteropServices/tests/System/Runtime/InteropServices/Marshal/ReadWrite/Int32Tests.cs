// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class Int32Tests
    {
        public static readonly object[][] ArrayData =
        {
            new object[] { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, int.MaxValue } }
        };

        [Theory]
        [MemberData(nameof(ArrayData))]
        public void NullValue_ThrowsException(int[] TestValues)
        {
            Exception e;

            e = Record.Exception(() => Marshal.ReadInt32(IntPtr.Zero));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.ReadInt32(IntPtr.Zero, 2));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.WriteInt32(IntPtr.Zero, TestValues[0]));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.WriteInt32(IntPtr.Zero, 2, TestValues[0]));
            Assert.True(e is AccessViolationException || e is NullReferenceException);
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        public void ReadWriteRoundTrip(int[] TestValues)
        {
            int sizeOfArray = Marshal.SizeOf(TestValues[0]) * TestValues.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteInt32(ptr, TestValues[0]);

                for (int i = 1; i < TestValues.Length; i++)
                {
                    Marshal.WriteInt32(ptr, i * Marshal.SizeOf(TestValues[0]), TestValues[i]);
                }

                int value = Marshal.ReadInt32(ptr);
                Assert.Equal(TestValues[0], value);

                for (int i = 1; i < TestValues.Length; i++)
                {
                    value = Marshal.ReadInt32(ptr, i * Marshal.SizeOf(TestValues[0]));
                    Assert.Equal(TestValues[i], value);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
