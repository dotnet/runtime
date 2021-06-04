// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorGetTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorGet(T value, int index)
        {
            int returnVal = Pass;

            Vector<T> A = new Vector<T>(value);

            // Test variable index.
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!CheckValue(A[i], value)) returnVal = Fail;
            }

            if (!CheckValue(A[index], value)) returnVal = Fail;

            // Test constant index.
            if (!CheckValue(A[0], value)) returnVal = Fail;
            if (Vector<T>.Count >= 2)
            {
                if (!CheckValue(A[1], value))
                {
                    Console.WriteLine("Failed for [1] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
            }
            if (Vector<T>.Count >= 4)
            {
                if (!CheckValue(A[2], value))
                {
                    Console.WriteLine("Failed for [2] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
                if (!CheckValue(A[3], value))
                {
                    Console.WriteLine("Failed for [3] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
            }
            if (Vector<T>.Count >= 8)
            {
                if (!CheckValue(A[4], value))
                {
                    Console.WriteLine("Failed for [4] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
                if (!CheckValue(A[5], value))
                {
                    Console.WriteLine("Failed for [5] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
                if (!CheckValue(A[6], value))
                {
                    Console.WriteLine("Failed for [6] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
                if (!CheckValue(A[7], value))
                {
                    Console.WriteLine("Failed for [7] for type " + typeof(T).ToString());
                    returnVal = Fail;
                }
            }
            if (Vector<T>.Count >= 16)
            {
                if (!CheckValue(A[8], value)) returnVal = Fail;
                if (!CheckValue(A[9], value)) returnVal = Fail;
                if (!CheckValue(A[10], value)) returnVal = Fail;
                if (!CheckValue(A[11], value)) returnVal = Fail;
                if (!CheckValue(A[12], value)) returnVal = Fail;
                if (!CheckValue(A[13], value)) returnVal = Fail;
                if (!CheckValue(A[14], value)) returnVal = Fail;
                if (!CheckValue(A[15], value)) returnVal = Fail;
            }
            if (Vector<T>.Count >= 32)
            {
                if (!CheckValue(A[16], value)) returnVal = Fail;
                if (!CheckValue(A[17], value)) returnVal = Fail;
                if (!CheckValue(A[18], value)) returnVal = Fail;
                if (!CheckValue(A[19], value)) returnVal = Fail;
                if (!CheckValue(A[20], value)) returnVal = Fail;
                if (!CheckValue(A[21], value)) returnVal = Fail;
                if (!CheckValue(A[22], value)) returnVal = Fail;
                if (!CheckValue(A[23], value)) returnVal = Fail;
                if (!CheckValue(A[24], value)) returnVal = Fail;
                if (!CheckValue(A[25], value)) returnVal = Fail;
                if (!CheckValue(A[26], value)) returnVal = Fail;
                if (!CheckValue(A[27], value)) returnVal = Fail;
                if (!CheckValue(A[28], value)) returnVal = Fail;
                if (!CheckValue(A[29], value)) returnVal = Fail;
                if (!CheckValue(A[30], value)) returnVal = Fail;
                if (!CheckValue(A[31], value)) returnVal = Fail;
            }

            return returnVal;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int VectorGetIndexerOutOfRange(T value, int index)
        {
            int returnVal = Pass;
            bool caught;

            Vector<T> A = new Vector<T>(value);

            T check;
            caught = false;
            try
            {
                switch (Vector<T>.Count)
                {
                    case 2: check = A[2]; break;
                    case 4: check = A[4]; break;
                    case 8: check = A[8]; break;
                    case 16: check = A[16]; break;
                    case 32: check = A[32]; break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw ArgumentOutOfRangeException for index == Count of " + Vector<T>.Count);
                returnVal = Fail;
            }

            // Check lower bounds
            caught = false;
            try
            {
                check = A[-1];
            }
            catch (ArgumentOutOfRangeException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw ArgumentOutOfRangeException for index == -1");
                returnVal = Fail;
            }

            return returnVal;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        if (VectorGetTest<Double>.VectorGet(101D, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<Double>.VectorGet(100D, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<Double>.VectorGetIndexerOutOfRange(100D, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<Single>.VectorGet(101F, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<Single>.VectorGet(100F, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<Single>.VectorGetIndexerOutOfRange(100F, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<int>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<int>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<int>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<long>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<long>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<long>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ushort>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ushort>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ushort>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<byte>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<byte>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<byte>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<short>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<short>.VectorGet(-100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<short>.VectorGetIndexerOutOfRange(-100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<sbyte>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<sbyte>.VectorGet(-100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<sbyte>.VectorGetIndexerOutOfRange(-100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<uint>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<uint>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<uint>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ulong>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ulong>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<ulong>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nint>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nint>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nint>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nuint>.VectorGet(101, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nuint>.VectorGet(100, 1) == Fail) returnVal = Fail;
        if (VectorGetTest<nuint>.VectorGetIndexerOutOfRange(100, 1) == Fail) returnVal = Fail;

        if (Sse41.IsSupported || AdvSimd.IsSupported)
        {
            JitLog jitLog = new JitLog();
            if (!jitLog.Check("get_Item", "Double")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Double")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "Single")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Single")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "Int32")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Int32")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "Int64")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Int64")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "UInt16")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "UInt16")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "Byte")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Byte")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "Int16")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "Int16")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "SByte")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "SByte")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "UInt32")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "UInt32")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "UInt64")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "UInt64")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "IntPtr")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "IntPtr")) returnVal = Fail;
            if (!jitLog.Check("get_Item", "UIntPtr")) returnVal = Fail;
            if (!jitLog.Check("get_Count", "UIntPtr")) returnVal = Fail;
            jitLog.Dispose();
        }
        return returnVal;
    }
}
