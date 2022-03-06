// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(uint.MaxValue)]
    [InlineData(uint.MinValue)]
    public void ConvertToVector256NInt(uint value)
    {
        Vector128<uint> vector128UInt32 = Vector128.Create(value);
        Vector128<ushort> vector128UInt16 = vector128UInt32.AsUInt16();
        Vector128<byte> vector128Byte = vector128UInt32.AsByte();

        Vector128<int> vector128Int32 = vector128UInt32.AsInt32();
        Vector128<short> vector128Int16 = vector128Int32.AsInt16();
        Vector128<sbyte> vector128SByte = vector128Int32.AsSByte();

        Vector256<nint> vector256UInt32 = Avx2.ConvertToVector256NInt(vector128UInt32);
        Vector256<nint> vector256UInt16 = Avx2.ConvertToVector256NInt(vector128UInt16);
        Vector256<nint> vector256Byte = Avx2.ConvertToVector256NInt(vector128Byte);
        Vector256<nint> vector256Int32 = Avx2.ConvertToVector256NInt(vector128Int32);
        Vector256<nint> vector256Int16 = Avx2.ConvertToVector256NInt(vector128Int16);
        Vector256<nint> vector256SByte = Avx2.ConvertToVector256NInt(vector128SByte);

        Assert.Equal(vector256UInt32, vector256UInt16);
        Assert.Equal(vector256UInt16, vector256Byte);
        Assert.Equal(vector256Byte, vector256Int32);
        Assert.Equal(vector256Int32, vector256Int16);
        Assert.Equal(vector256Int16, vector256SByte);

        Assert.Equal((nint)value, vector128Int32[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(uint.MaxValue)]
    [InlineData(uint.MinValue)]
    public void ConvertToVector256NInt_Pointer(uint value)
    {
        Span<uint> span = stackalloc uint[2];
        span[0] = value;
        span[1] = value;

        unsafe
        {
            fixed (void* ptr = &span[0])
            {
                uint* uintPtr = (uint*)ptr;
                ushort* ushortPtr = (ushort*)ptr;
                byte* bytePtr = (byte*)ptr;
                int* intPtr = (int*)ptr;
                short* shortPtr = (short*)ptr;
                sbyte* sbytePtr = (sbyte*)ptr;

                Vector256<nint> vector1 = Avx2.ConvertToVector256NInt(uintPtr);
                Vector256<nint> vector2 = Avx2.ConvertToVector256NInt(ushortPtr);
                Vector256<nint> vector3 = Avx2.ConvertToVector256NInt(bytePtr);
                Vector256<nint> vector4 = Avx2.ConvertToVector256NInt(intPtr);
                Vector256<nint> vector5 = Avx2.ConvertToVector256NInt(shortPtr);
                Vector256<nint> vector6 = Avx2.ConvertToVector256NInt(sbytePtr);

                Assert.Equal(vector1, vector2);
                Assert.Equal(vector2, vector3);
                Assert.Equal(vector3, vector4);
                Assert.Equal(vector4, vector5);
                Assert.Equal(vector5, vector6);

                Assert.Equal((nint)value, vector1[0]);
                Assert.Equal((nint)value, vector1[1]);
                Assert.Equal(0, vector1[2]);
                Assert.Equal(0, vector1[3]);
            }
        }
    }
}
