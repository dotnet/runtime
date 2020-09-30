// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void GetArrayDataReference_NullInput_ThrowsNullRef()
        {
            Assert.Throws<NullReferenceException>(() => MemoryMarshal.GetArrayDataReference((object[])null));
        }

        [Fact]
        public static void GetArrayDataReference_NonEmptyInput_ReturnsRefToFirstElement()
        {
            int[] theArray = new int[] { 10, 20, 30 };
            Assert.True(Unsafe.AreSame(ref theArray[0], ref MemoryMarshal.GetArrayDataReference(theArray)));
        }

        [Fact]
        public static unsafe void GetArrayDataReference_EmptyInput_ReturnsRefToWhereFirstElementWouldBe()
        {
            int[] theArray = new int[0];

            ref int theRef = ref MemoryMarshal.GetArrayDataReference(theArray);

            Assert.True(Unsafe.AsPointer(ref theRef) != null);
            Assert.True(Unsafe.AreSame(ref theRef, ref MemoryMarshal.GetReference(theArray.AsSpan())));
        }

        [Fact]
        public static void GetArrayDataReference_IgnoresArrayVarianceChecks()
        {
            string[] strArr = new string[] { "Hello" };

            // 'ref object' instead of 'ref string' because GetArrayDataReference skips array variance checks.
            // We can deref it but we must not write to it unless we know the value being written is also a string.
            ref object refObj = ref MemoryMarshal.GetArrayDataReference<object>(strArr);

            Assert.True(Unsafe.AreSame(ref refObj, ref Unsafe.As<string, object>(ref strArr[0])));
        }
    }
}
