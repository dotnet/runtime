// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorAddTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorAdd(T a, T b, T c)
        {
            Vector<T> A = new Vector<T>(a);
            Vector<T> B = new Vector<T>(b);
            Vector<T> C = A + B;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(C[i], c)))
                {
                    return Fail;
                }
            }
            return Pass;
        }
    }
    private class Vector4Test
    {
        public static int VectorAdd()
        {
            Vector4 A = new Vector4(2);
            Vector4 B = new Vector4(1);
            Vector4 C = A + B;
            if (!(CheckValue<float>(C.X, 3))) return Fail;
            if (!(CheckValue<float>(C.Y, 3))) return Fail;
            if (!(CheckValue<float>(C.Z, 3))) return Fail;
            if (!(CheckValue<float>(C.W, 3))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorAdd()
        {
            Vector3 A = new Vector3(2);
            Vector3 B = new Vector3(1);
            Vector3 C = A + B;
            if (!(CheckValue<float>(C.X, 3))) return Fail;
            if (!(CheckValue<float>(C.Y, 3))) return Fail;
            if (!(CheckValue<float>(C.Z, 3))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorAdd()
        {
            Vector2 A = new Vector2(2);
            Vector2 B = new Vector2(1);
            Vector2 C = A + B;
            if (!(CheckValue<float>(C.X, 3))) return Fail;
            if (!(CheckValue<float>(C.Y, 3))) return Fail;
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorAddTest<float>.VectorAdd(1, 2, (float)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<double>.VectorAdd(1, 2, (double)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<int>.VectorAdd(1, 2, (int)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<long>.VectorAdd(1, 2, (long)(1 + 2)) != Pass) returnVal = Fail;
        if (Vector4Test.VectorAdd() != Pass) returnVal = Fail;
        if (Vector3Test.VectorAdd() != Pass) returnVal = Fail;
        if (Vector2Test.VectorAdd() != Pass) returnVal = Fail;
        if (VectorAddTest<ushort>.VectorAdd(1, 2, (ushort)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<byte>.VectorAdd(1, 2, (byte)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<short>.VectorAdd(-1, -2, (short)(-1 - 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<sbyte>.VectorAdd(-1, -2, (sbyte)(-1 - 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<uint>.VectorAdd(0x41000000u, 0x42000000u, 0x41000000u + 0x42000000u) != Pass) returnVal = Fail;
        if (VectorAddTest<ulong>.VectorAdd(0x4100000000000000ul, 0x4200000000000000ul, 0x4100000000000000ul + 0x4200000000000000ul) != Pass) returnVal = Fail;
        if (VectorAddTest<nint>.VectorAdd(1, 2, (nint)(1 + 2)) != Pass) returnVal = Fail;
        if (VectorAddTest<nuint>.VectorAdd(0x41000000u, 0x42000000u, 0x41000000u + 0x42000000u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("op_Addition", "Single")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "Double")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "Int32")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "Int64")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:op_Addition")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:op_Addition")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:op_Addition")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "Byte")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "Int16")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "SByte")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("op_Addition", "UIntPtr")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
