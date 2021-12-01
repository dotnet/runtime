// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        if (VectorRelopTest<nint>.VectorRelOp(3, 2) != Pass) returnVal = Fail;
        if (VectorRelopTest<nuint>.VectorRelOp(3u, 2u) != Pass) returnVal = Fail;

        JitLog jitLog = new JitLog();

        // ConditionalSelect, LessThanOrEqual and GreaterThanOrEqual are defined
        // on the Vector type, so the overloads can't be distinguished.
        //
        if (!jitLog.Check("System.Numerics.Vector:ConditionalSelect(struct,struct,struct):struct")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector:LessThanOrEqual(struct,struct):struct")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector:GreaterThanOrEqual(struct,struct):struct")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Single")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Single")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Single")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Single")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Single")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Single")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Single")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Single")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Double")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Double")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Double")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Double")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Double")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Double")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Double")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Double")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Int32")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Int32")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int32")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Int32")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Int32")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int32")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Int32")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Int32")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Int64")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Int64")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int64")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Int64")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Int64")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int64")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Int64")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Int64")) returnVal = Fail;

        if (!jitLog.Check("Equals", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt16")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "UInt16")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Byte")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Byte")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Byte")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Byte")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Byte")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Byte")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Byte")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Byte")) returnVal = Fail;

        if (!jitLog.Check("Equals", "Int16")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "Int16")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "Int16")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "Int16")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int16")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "Int16")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "Int16")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "Int16")) returnVal = Fail;

        if (!jitLog.Check("Equals", "SByte")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "SByte")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "SByte")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "SByte")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "SByte")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "SByte")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "SByte")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "SByte")) returnVal = Fail;

        if (!jitLog.Check("Equals", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt32")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "UInt32")) returnVal = Fail;

        if (!jitLog.Check("Equals", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UInt64")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "UInt64")) returnVal = Fail;

        if (!jitLog.Check("Equals", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "IntPtr")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "IntPtr")) returnVal = Fail;

        if (!jitLog.Check("Equals", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("LessThan", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("op_BitwiseAnd", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("op_ExclusiveOr", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("GreaterThan", "UIntPtr")) returnVal = Fail;
        // This relies on an implementation detail - i.e. that the One and Zero property are implemented
        // in the library by GetOneValue and GetZeroValue, respectively.
        if (!jitLog.Check("GetOneValue", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("GetZeroValue", "UIntPtr")) returnVal = Fail;

        jitLog.Dispose();

        return returnVal;
    }
}
