// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class IntPtrTests
    {
        public static readonly object[][] ArrayData =
        {
            new object[] { GenerateIntPtrArray() }
        };

        public static IntPtr[] GenerateIntPtrArray()
        {
            IntPtr[] testArray = new IntPtr[10];
            for (int i = 0; i < testArray.Length; i++)
                testArray[i] = new IntPtr(i);
            return testArray;
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        public void NullValue_ThrowsException(IntPtr[] TestValues)
        {
            Exception e;

            e = Record.Exception(() => Marshal.ReadIntPtr(IntPtr.Zero));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.ReadIntPtr(IntPtr.Zero, 2));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.WriteIntPtr(IntPtr.Zero, TestValues[0]));
            Assert.True(e is AccessViolationException || e is NullReferenceException);

            e = Record.Exception(() => Marshal.WriteIntPtr(IntPtr.Zero, 2, TestValues[0]));
            Assert.True(e is AccessViolationException || e is NullReferenceException);
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        public void ReadWriteRoundTrip(IntPtr[] TestValues)
        {
            int sizeOfArray = Marshal.SizeOf(TestValues[0]) * TestValues.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteIntPtr(ptr, TestValues[0]);

                for (int i = 1; i < TestValues.Length; i++)
                {
                    Marshal.WriteIntPtr(ptr, i * Marshal.SizeOf(TestValues[0]), TestValues[i]);
                }

                IntPtr value = Marshal.ReadIntPtr(ptr);
                Assert.Equal(TestValues[0], value);

                for (int i = 1; i < TestValues.Length; i++)
                {
                    value = Marshal.ReadIntPtr(ptr, i * Marshal.SizeOf(TestValues[0]));
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
