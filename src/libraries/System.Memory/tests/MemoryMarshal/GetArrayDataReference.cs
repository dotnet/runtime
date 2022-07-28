// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void GetArrayDataReference_NullInput_ThrowsNullRef()
        {
            Assert.Throws<NullReferenceException>(() => MemoryMarshal.GetArrayDataReference<object>((object[])null));
            Assert.Throws<NullReferenceException>(() => MemoryMarshal.GetArrayDataReference((Array)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public static void GetArrayDataReference_NonEmptyInput_ReturnsRefToFirstElement()
        {
            // szarray
            int[] szArray = new int[] { 10, 20, 30 };
            Assert.True(Unsafe.AreSame(ref szArray[0], ref MemoryMarshal.GetArrayDataReference(szArray)));
            Assert.True(Unsafe.AreSame(ref szArray[0], ref Unsafe.As<byte, int>(ref MemoryMarshal.GetArrayDataReference((Array)szArray))));

            // mdarray, rank 2
            int[,] mdArrayRank2 = new int[3, 2];
            Assert.True(Unsafe.AreSame(ref mdArrayRank2[0, 0], ref Unsafe.As<byte, int>(ref MemoryMarshal.GetArrayDataReference(mdArrayRank2))));

            // mdarray, custom bounds
            // there's no baseline way to get a ref to element (10, 20, 30), so we'll deref the result of GetArrayDataReference and compare
            Array mdArrayCustomBounds = Array.CreateInstance(typeof(int), new[] { 1, 2, 3 }, new int[] { 10, 20, 30 });
            mdArrayCustomBounds.SetValue(0x12345678, new[] { 10, 20, 30 });
            Assert.Equal(0x12345678, Unsafe.As<byte, int>(ref MemoryMarshal.GetArrayDataReference(mdArrayCustomBounds)));
        }

        [Fact]
        public static unsafe void GetArrayDataReference_EmptyInput_ReturnsRefToWhereFirstElementWouldBe_SzArray()
        {
            int[] theArray = new int[0];

            ref int theRef = ref MemoryMarshal.GetArrayDataReference(theArray);

            Assert.True(Unsafe.AsPointer(ref theRef) != null);
            Assert.True(Unsafe.AreSame(ref theRef, ref MemoryMarshal.GetReference(theArray.AsSpan())));

            ref int theMdArrayRef = ref Unsafe.As<byte, int>(ref MemoryMarshal.GetArrayDataReference((Array)theArray)); // szarray passed to generalized Array helper
            Assert.True(Unsafe.AreSame(ref theRef, ref theMdArrayRef));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static unsafe void GetArrayDataReference_EmptyInput_ReturnsRefToWhereFirstElementWouldBe_MdArray(int rank)
        {
            // First, compute how much distance there is between the start of the object data and the first element
            // of a baseline (non-empty) array.

            int[] lowerDims = Enumerable.Range(100, rank).ToArray();
            int[] lengths = Enumerable.Range(1, rank).ToArray();

            Array baselineArray = Array.CreateInstance(typeof(int), lengths, lowerDims);
            IntPtr baselineOffset = Unsafe.ByteOffset(ref Unsafe.As<RawObject>(baselineArray).Data, ref MemoryMarshal.GetArrayDataReference(baselineArray));

            // Then, perform the same calculation with an empty array of equal rank, and ensure the offsets are identical.

            lengths = new int[rank]; // = { 0, 0, 0, ... }

            Array emptyArray = Array.CreateInstance(typeof(int), lengths, lowerDims);
            IntPtr emptyArrayOffset = Unsafe.ByteOffset(ref Unsafe.As<RawObject>(emptyArray).Data, ref MemoryMarshal.GetArrayDataReference(emptyArray));

            Assert.Equal(baselineOffset, emptyArrayOffset);
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

        private sealed class RawObject
        {
            internal byte Data;
        }
    }
}
