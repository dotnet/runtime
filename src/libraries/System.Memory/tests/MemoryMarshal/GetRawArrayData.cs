// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void GetRawArrayData_NullInput_ThrowsNullRef()
        {
            Assert.Throws<NullReferenceException>(() => MemoryMarshal.GetRawArrayData((object[])null));
        }

        [Fact]
        public static void GetRawArrayData_NonEmptyInput_ReturnsRefToFirstElement()
        {
            int[] theArray = new int[] { 10, 20, 30 };
            Assert.True(Unsafe.AreSame(ref theArray[0], ref MemoryMarshal.GetRawArrayData(theArray)));
        }

        [Fact]
        public static unsafe void GetRawArrayData_EmptyInput_ReturnsRefToWhereFirstElementWouldBe()
        {
            int[] theArray = new int[0];

            ref int theRef = ref MemoryMarshal.GetRawArrayData(theArray);

            Assert.True(Unsafe.AsPointer(ref theRef) != null);
            Assert.True(Unsafe.AreSame(ref theRef, ref MemoryMarshal.GetReference(theArray.AsSpan())));
        }

        [Fact]
        public static void GetRawArrayData_IgnoresArrayVarianceChecks()
        {
            string[] strArr = new string[] { "Hello" };

            // 'ref object' instead of 'ref string' because GetRawArrayData skips array variance checks.
            // We can deref it but we must not write to it unless we know the value being written is also a string.
            ref object refObj = ref MemoryMarshal.GetRawArrayData<object>(strArr);

            Assert.True(Unsafe.AreSame(ref refObj, ref Unsafe.As<string, object>(ref strArr[0])));
        }
    }
}
