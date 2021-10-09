// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorAbsTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorAbs(T value, T checkValue)
        {
            Vector<T> A = new Vector<T>(value);
            Vector<T> B = Vector.Abs<T>(A);

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!(CheckValue<T>(B[i], checkValue)))
                {
                    return Fail;
                }
            }
            return Pass;
        }
    }

    private class Vector4Test
    {
        public static int VectorAbs()
        {
            Vector4 A = new Vector4(-1f);
            Vector4 B = Vector4.Abs(A);
            if (!(CheckValue<float>(B.X, 1f))) return Fail;
            if (!(CheckValue<float>(B.Y, 1f))) return Fail;
            if (!(CheckValue<float>(B.Z, 1f))) return Fail;
            if (!(CheckValue<float>(B.W, 1f))) return Fail;
            return Pass;
        }
    }

    private class Vector3Test
    {
        public static int VectorAbs()
        {
            Vector3 A = new Vector3(-1f);
            Vector3 B = Vector3.Abs(A);
            if (!(CheckValue<float>(B.X, 1f))) return Fail;
            if (!(CheckValue<float>(B.Y, 1f))) return Fail;
            if (!(CheckValue<float>(B.Z, 1f))) return Fail;
            return Pass;
        }
    }

    private class Vector2Test
    {
        public static int VectorAbs()
        {
            Vector2 A = new Vector2(-1f);
            Vector2 B = Vector2.Abs(A);
            if (!(CheckValue<float>(B.X, 1f))) return Fail;
            if (!(CheckValue<float>(B.Y, 1f))) return Fail;
            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;

        if (VectorAbsTest<float>.VectorAbs(-1f, 1f) != Pass) returnVal = Fail;
        if (VectorAbsTest<Double>.VectorAbs(-1d, 1d) != Pass) returnVal = Fail;
        if (VectorAbsTest<int>.VectorAbs(-1, 1) != Pass) returnVal = Fail;
        if (VectorAbsTest<long>.VectorAbs(-1, 1) != Pass) returnVal = Fail;
        if (VectorAbsTest<short>.VectorAbs((short)-1, (short)1) != Pass) returnVal = Fail;
        if (VectorAbsTest<sbyte>.VectorAbs((sbyte)-1, (sbyte)1) != Pass) returnVal = Fail;
        if (Vector4Test.VectorAbs() != Pass) returnVal = Fail;
        if (Vector3Test.VectorAbs() != Pass) returnVal = Fail;
        if (Vector2Test.VectorAbs() != Pass) returnVal = Fail;
        if (VectorAbsTest<ushort>.VectorAbs((ushort)0xffff, (ushort)0xffff) != Pass) returnVal = Fail;
        if (VectorAbsTest<byte>.VectorAbs((byte)0xff, (byte)0xff) != Pass) returnVal = Fail;
        if (VectorAbsTest<uint>.VectorAbs(0x41000000u, 0x41000000u) != Pass) returnVal = Fail;
        if (VectorAbsTest<ulong>.VectorAbs(0x4100000000000000ul, 0x4100000000000000ul) != Pass) returnVal = Fail;
        if (VectorAbsTest<nint>.VectorAbs(-1, 1) != Pass) returnVal = Fail;
        if (VectorAbsTest<nuint>.VectorAbs(0x41000000u, 0x41000000u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("Abs", "Single")) returnVal = Fail;
        if (!jitLog.Check("Abs", "Double")) returnVal = Fail;
        if (!jitLog.Check("Abs", "Int64")) returnVal = Fail;
        if (!jitLog.Check("Abs", "Int32")) returnVal = Fail;
        if (!jitLog.Check("Abs", "Int16")) returnVal = Fail;
        if (!jitLog.Check("Abs", "SByte")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:Abs")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:Abs")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector2:Abs")) returnVal = Fail;
        if (!jitLog.Check("Abs", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("Abs", "Byte")) returnVal = Fail;
        if (!jitLog.Check("Abs", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("Abs", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("Abs", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("Abs", "UIntPtr")) returnVal = Fail;

        jitLog.Dispose();

        return returnVal;
    }
}
