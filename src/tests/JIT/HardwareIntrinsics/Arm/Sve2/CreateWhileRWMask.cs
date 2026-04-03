// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._Sve2
{
    public static partial class Program
    {
        [Fact]
        public static unsafe void CreateWhileReadAfterWriteMaskByte_SameAddress()
        {
            if (!Sve2.IsSupported)
            {
                return;
            }

            // When pointers are idential, all elements of returned mask are "1"
            byte* ptr = stackalloc byte[Vector<byte>.Count];
            Vector<byte> mask = Sve2.CreateWhileReadAfterWriteMaskByte(ptr, ptr);

            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                if (mask.GetElement(i) == 0)
                {
                    throw new Exception(
                        $"CreateWhileReadAfterWriteMaskByte(ptr, ptr): Expected all elements true, but element {i} was false.");
                }
            }
        }

        [Fact]
        public static unsafe void CreateWhileReadAfterWriteMaskByte_OffsetByOne()
        {
            if (!Sve2.IsSupported)
            {
                return;
            }

            // When pointers differ by one element, first element of returned mask is "1", the rest are "0"
            byte* ptr = stackalloc byte[Vector<byte>.Count + 1];
            Vector<byte> mask = Sve2.CreateWhileReadAfterWriteMaskByte(ptr, ptr + 1);

            if (mask.GetElement(0) == 0)
            {
                throw new Exception(
                    $"CreateWhileReadAfterWriteMaskByte(ptr, ptr+1): Expected element 0 to be true, but was false.");
            }

            for (int i = 1; i < Vector<byte>.Count; i++)
            {
                if (mask.GetElement(i) != 0)
                {
                    throw new Exception(
                        $"CreateWhileReadAfterWriteMaskByte(ptr, ptr+1): Expected element {i} to be false, but was true.");
                }
            }
        }
    }
}
