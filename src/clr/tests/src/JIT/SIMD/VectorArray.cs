// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;


internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorArrayTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private static void Move(Vector<T>[] pos, ref Vector<T> delta)
        {
            for (int i = 0; i < pos.Length; ++i)
                pos[i] += delta;
        }

        static public int VectorArray(T deltaValue)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector<T>[] v = new Vector<T>[3];
            for (int i = 0; i < v.Length; ++i)
                v[i] = new Vector<T>(GetValueFromInt<T>(i + 1));

            Vector<T> delta = new Vector<T>(GetValueFromInt<T>(1));
            Move(v, ref delta);

            for (int i = 0; i < v.Length; i++)
            {
                T checkValue = GetValueFromInt<T>(i + 2);
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    if (!(CheckValue<T>(v[i][j], checkValue))) return Fail;
                }
            }

            return Pass;
        }
    }

    private class Vector4Test
    {
        private static void Move(Vector4[] pos, ref Vector4 delta)
        {
            for (int i = 0; i < pos.Length; ++i)
                pos[i] += delta;
        }

        static public int VectorArray(float deltaValue)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector4[] v = new Vector4[3];
            for (int i = 0; i < 3; ++i)
                v[i] = new Vector4(i + 1);

            Vector4 delta = new Vector4(1);
            Move(v, ref delta);

            for (int i = 0; i < v.Length; i++)
            {
                float checkValue = (float)(i + 2);
                if (!(CheckValue<float>(v[i].X, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].Y, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].Z, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].W, checkValue))) return Fail;
            }

            return Pass;
        }
    }

    private class Vector3Test
    {
        private static void Move(Vector3[] pos, ref Vector3 delta)
        {
            for (int i = 0; i < pos.Length; ++i)
                pos[i] += delta;
        }

        static public int VectorArray(float deltaValue)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector3[] v = new Vector3[3];
            for (int i = 0; i < 3; ++i)
                v[i] = new Vector3(i + 1);

            Vector3 delta = new Vector3(1);
            Move(v, ref delta);

            for (int i = 0; i < v.Length; i++)
            {
                float checkValue = (float)(i + 2);
                if (!(CheckValue<float>(v[i].X, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].Y, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].Z, checkValue))) return Fail;
            }

            return Pass;
        }
    }

    private class Vector2Test
    {
        private static void Move(Vector2[] pos, ref Vector2 delta)
        {
            for (int i = 0; i < pos.Length; ++i)
                pos[i] += delta;
        }

        static public int VectorArray(float deltaValue)
        {
            const int Pass = 100;
            const int Fail = -1;

            Vector2[] v = new Vector2[3];
            for (int i = 0; i < 3; ++i)
                v[i] = new Vector2(i + 1);

            Vector2 delta = new Vector2(1);
            Move(v, ref delta);

            for (int i = 0; i < v.Length; i++)
            {
                float checkValue = (float)(i + 2);
                if (!(CheckValue<float>(v[i].X, checkValue))) return Fail;
                if (!(CheckValue<float>(v[i].Y, checkValue))) return Fail;
            }

            return Pass;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        try
        {
            if (VectorArrayTest<float>.VectorArray(1f) != Pass) returnVal = Fail;
            if (VectorArrayTest<double>.VectorArray(1d) != Pass) returnVal = Fail;
            if (VectorArrayTest<int>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<long>.VectorArray(1L) != Pass) returnVal = Fail;
            if (Vector4Test.VectorArray(1f) != Pass) returnVal = Fail;
            if (Vector3Test.VectorArray(1f) != Pass) returnVal = Fail;
            if (Vector2Test.VectorArray(1f) != Pass) returnVal = Fail;
            if (VectorArrayTest<ushort>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<byte>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<short>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<sbyte>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<uint>.VectorArray(1) != Pass) returnVal = Fail;
            if (VectorArrayTest<ulong>.VectorArray(1ul) != Pass) returnVal = Fail;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("Argument Exception was raised");
            Console.WriteLine(ex.StackTrace);
            return Fail;
        }
        return returnVal;
    }
}
