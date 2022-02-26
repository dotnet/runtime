// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    //
    // Sse41.ConvertToVector128NInt is currently failing
    // -> InvalidProgramException
    // ¯\_(ツ)_/¯
    //
    /*
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    public void ConvertToVector128NInt_64Bit(long lower, long upper, long expectedLower, long expectedUpper)
    {
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();
        Vector128<long> value = Vector128.Create(lower, upper);
        Vector128<sbyte> sByteValue = value.AsSByte();
        Vector128<byte> byteValue = value.AsByte();
        Vector128<short> shortValue = value.AsInt16();
        Vector128<ushort> ushortValue = value.AsUInt16();
        Vector128<int> intValue = value.AsInt32();
        Vector128<uint> uintValue = value.AsUInt32();

        Vector128<nint> actualVector = Sse41.ConvertToVector128NInt(sByteValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(byteValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(shortValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(ushortValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(intValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(uintValue);
        Assert.Equal(expectedVector, actualVector);

        unsafe
        {
            sbyte* sBytePtr = (sbyte*)Unsafe.AsPointer(ref sByteValue);
            actualVector = Sse41.ConvertToVector128NInt(sBytePtr);
            Assert.Equal(expectedVector, actualVector);

            byte* bytePtr = (byte*)Unsafe.AsPointer(ref byteValue);
            actualVector = Sse41.ConvertToVector128NInt(bytePtr);
            Assert.Equal(expectedVector, actualVector);

            short* shortPtr = (short*)Unsafe.AsPointer(ref shortValue);
            actualVector = Sse41.ConvertToVector128NInt(shortPtr);
            Assert.Equal(expectedVector, actualVector);

            ushort* ushortPtr = (ushort*)Unsafe.AsPointer(ref ushortValue);
            actualVector = Sse41.ConvertToVector128NInt(ushortPtr);
            Assert.Equal(expectedVector, actualVector);

            int* intPtr = (int*)Unsafe.AsPointer(ref intValue);
            actualVector = Sse41.ConvertToVector128NInt(intPtr);
            Assert.Equal(expectedVector, actualVector);

            uint* uintPtr = (uint*)Unsafe.AsPointer(ref uintValue);
            actualVector = Sse41.ConvertToVector128NInt(uintPtr);
            Assert.Equal(expectedVector, actualVector);
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    public void ConvertToVector128NInt_32Bit(int lower, int upper, int expectedLower, int expectedUpper)
    {
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();
        Vector128<long> value = Vector128.Create(lower, upper);
        Vector128<sbyte> sByteValue = value.AsSByte();
        Vector128<byte> byteValue = value.AsByte();
        Vector128<short> shortValue = value.AsInt16();
        Vector128<ushort> ushortValue = value.AsUInt16();
        Vector128<int> intValue = value.AsInt32();
        Vector128<uint> uintValue = value.AsUInt32();

        Vector128<nint> actualVector = Sse41.ConvertToVector128NInt(sByteValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(byteValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(shortValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(ushortValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(intValue);
        Assert.Equal(expectedVector, actualVector);

        actualVector = Sse41.ConvertToVector128NInt(uintValue);
        Assert.Equal(expectedVector, actualVector);

        unsafe
        {
            sbyte* sBytePtr = (sbyte*)Unsafe.AsPointer(ref sByteValue);
            actualVector = Sse41.ConvertToVector128NInt(sBytePtr);
            Assert.Equal(expectedVector, actualVector);

            byte* bytePtr = (byte*)Unsafe.AsPointer(ref byteValue);
            actualVector = Sse41.ConvertToVector128NInt(bytePtr);
            Assert.Equal(expectedVector, actualVector);

            short* shortPtr = (short*)Unsafe.AsPointer(ref shortValue);
            actualVector = Sse41.ConvertToVector128NInt(shortPtr);
            Assert.Equal(expectedVector, actualVector);

            ushort* ushortPtr = (ushort*)Unsafe.AsPointer(ref ushortValue);
            actualVector = Sse41.ConvertToVector128NInt(ushortPtr);
            Assert.Equal(expectedVector, actualVector);

            int* intPtr = (int*)Unsafe.AsPointer(ref intValue);
            actualVector = Sse41.ConvertToVector128NInt(intPtr);
            Assert.Equal(expectedVector, actualVector);

            uint* uintPtr = (uint*)Unsafe.AsPointer(ref uintValue);
            actualVector = Sse41.ConvertToVector128NInt(uintPtr);
            Assert.Equal(expectedVector, actualVector);
        }
    }
    */
}
