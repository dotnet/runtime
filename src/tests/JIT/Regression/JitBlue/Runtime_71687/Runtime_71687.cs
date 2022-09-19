// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

class Runtime_71687
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test<T>(ref T first, int i)
    {
        Consume(Unsafe.Add(ref first, i));
    }

    // Must be inlined so we end up with null check above
    private static void Consume<T>(T value) { }

    private static int Main()
    {
        Test(ref (new byte[10])[0], 5);
        Test(ref (new sbyte[10])[0], 5);
        Test(ref (new ushort[10])[0], 5);
        Test(ref (new short[10])[0], 5);
        Test(ref (new uint[10])[0], 5);
        Test(ref (new int[10])[0], 5);
        Test(ref (new ulong[10])[0], 5);
        Test(ref (new long[10])[0], 5);
        Test(ref (new float[10])[0], 5);
        Test(ref (new double[10])[0], 5);
        Test(ref (new object[10])[0], 5);
        Test(ref (new string[10])[0], 5);
        Test(ref (new Vector<float>[10])[0], 5);
        Test(ref (new Vector128<float>[10])[0], 5);
        Test(ref (new Vector256<float>[10])[0], 5);
        Test(ref (new Struct1[10])[0], 5);
        Test(ref (new Struct2[10])[0], 5);
        Test(ref (new Struct4[10])[0], 5);
        Test(ref (new Struct8[10])[0], 5);
        return 100;
    }

    private struct Struct1 { public byte Field; }
    private struct Struct2 { public short Field; }
    private struct Struct4 { public int Field; }
    private struct Struct8 { public long Field; }
}
