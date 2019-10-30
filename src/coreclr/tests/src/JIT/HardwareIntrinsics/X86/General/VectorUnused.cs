// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// This test case is ported from S.N.Vector counterpart 
// https://github.com/dotnet/coreclr/blob/master/tests/src/JIT/SIMD/VectorUnused.cs

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal partial class IntelHardwareIntrinsicTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class Vector128UnusedTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorUnused(T t1, T t2)
        {
            if (Sse2.IsSupported)
            {
                Vector128<T> v1 = CreateVector128<T>(t1);
                v1 = Vector128Add<T>(v1, Vector128<T>.Zero);
            }

            return Pass;
        }
    }

    private class Vector256UnusedTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorUnused(T t1, T t2)
        {
            if (Avx2.IsSupported)
            {
                Vector256<T> v1 = CreateVector256<T>(t1);
                v1 = Vector256Add<T>(v1, CreateVector256<T>(t2));
            }
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;

        if (Vector128UnusedTest<float>.VectorUnused(3f, 2f) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<double>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<int>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<uint>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<ushort>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<byte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<short>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector128UnusedTest<sbyte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Environment.Is64BitProcess)
        {
            if (Vector128UnusedTest<long>.VectorUnused(3, 2) != Pass) returnVal = Fail;
            if (Vector128UnusedTest<ulong>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        }

        if (Vector256UnusedTest<float>.VectorUnused(3f, 2f) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<double>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<int>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<uint>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<ushort>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<byte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<short>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Vector256UnusedTest<sbyte>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        if (Environment.Is64BitProcess)
        {
            if (Vector256UnusedTest<long>.VectorUnused(3, 2) != Pass) returnVal = Fail;
            if (Vector256UnusedTest<ulong>.VectorUnused(3, 2) != Pass) returnVal = Fail;
        }
        return returnVal;
    }
}
