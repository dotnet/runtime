// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorRelopTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorRelOp(T larger, T smaller)
        {
            const int Pass = 100;
            const int Fail = -1;
            int returnVal = Pass;

            Vector<T> A = new Vector<T>(larger);
            Vector<T> B = new Vector<T>(smaller);
            Vector<T> C = new Vector<T>(larger);
            Vector<T> D;

            // less than
            Vector<T> condition = Vector.LessThan<T>(A, B);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(B[i]))
                {
                    Console.WriteLine("Less than condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }
            condition = Vector.LessThan<T>(B, A);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(A[i]))
                {
                    Console.WriteLine("Less than condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            // greater than
            condition = Vector.GreaterThan<T>(A, B);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(A[i]))
                {
                    Console.WriteLine("Greater than condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            condition = Vector.GreaterThan<T>(B, A);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(B[i]))
                {
                    Console.WriteLine("Greater than condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            // less than or equal
            condition = Vector.LessThanOrEqual<T>(A, C);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(A[i]))
                {
                    Console.WriteLine("Less than or equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            condition = Vector.LessThanOrEqual<T>(A, B);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(B[i]))
                {
                    Console.WriteLine("Less than or equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            // greater than or equal
            condition = Vector.GreaterThanOrEqual<T>(A, C);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(A[i]))
                {
                    Console.WriteLine("Greater than or equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            condition = Vector.GreaterThanOrEqual<T>(B, C);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(B[i]))
                {
                    Console.WriteLine("Greater than or equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            // equal
            condition = Vector.Equals<T>(A, C);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(A[i]))
                {
                    Console.WriteLine("Equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            condition = Vector.Equals<T>(B, C);
            D = Vector.ConditionalSelect(condition, A, B);
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!D[i].Equals(B[i]))
                {
                    Console.WriteLine("Equal condition failed for type " + typeof(T).Name + " at index " + i);
                    returnVal = Fail;
                }
            }

            return returnVal;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorRelopTest<float>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<double>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<int>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<long>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<ushort>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<byte>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<short>.VectorRelOp(-2, -3) != Pass) returnVal = Fail;
        if (VectorRelopTest<sbyte>.VectorRelOp(-2, -3) != Pass) returnVal = Fail;
        if (VectorRelopTest<uint>.VectorRelOp(3u, 2u) != Pass) returnVal = Fail;
        if (VectorRelopTest<ulong>.VectorRelOp(3ul, 2ul) != Pass) returnVal = Fail;
        return returnVal;
    }
}
