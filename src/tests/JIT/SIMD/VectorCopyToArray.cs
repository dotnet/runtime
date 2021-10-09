// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private const int DefaultSeed = 20010415;
    private static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    private class VectorCopyToArrayTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorCopyToArray(int size, Random random)
        {
            int returnVal = Pass;

            if (size < Vector<T>.Count) size = Vector<T>.Count;
            int index = size - Vector<T>.Count;
            T[] inputArray = GetRandomArray<T>(size, random);

            Vector<T> v1 = new Vector<T>(inputArray);
            Vector<T> v2 = new Vector<T>(inputArray, index);
            bool caught;

            T[] outputArray = new T[2 * Vector<T>.Count];
            v1.CopyTo(outputArray);
            v2.CopyTo(outputArray, Vector<T>.Count);

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!CheckValue(v1[i], outputArray[i])) returnVal = Fail;
                if (!CheckValue(v2[i], outputArray[i + Vector<T>.Count])) returnVal = Fail;
            }

            // Test a null input array.
            caught = false;
            try
            {
                v1.CopyTo((T[])null);
            }
            catch (NullReferenceException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw NullReferenceException for a null input array.");
                returnVal = Fail;
            }

            // Test a negative index.
            caught = false;
            try
            {
                v1.CopyTo(outputArray, -1);
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
                Console.WriteLine("Failed to throw ArgumentOutOfRangeException for a negative index.");
                returnVal = Fail;
            }

            // Test an out-of-range index.
            caught = false;
            try
            {
                v1.CopyTo(outputArray, outputArray.Length);
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
                Console.WriteLine("Failed to throw ArgumentOutOfRangeException for an out-of-range index.");
                returnVal = Fail;
            }

            // Test insufficient range in target array.
            caught = false;
            try
            {
                v1.CopyTo(outputArray, outputArray.Length - 1);
            }
            catch (ArgumentException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw ArgumentException for insufficient range in target array.");
                returnVal = Fail;
            }

            return returnVal;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        Random random = new Random(Seed);

        if (VectorCopyToArrayTest<Single>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<Single>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<Double>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<Double>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<int>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<int>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<long>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<long>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<ushort>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<ushort>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<byte>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<byte>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<short>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<short>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<sbyte>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<sbyte>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<uint>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<uint>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<ulong>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<ulong>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<nint>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<nint>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<nuint>.VectorCopyToArray(12, random) == Fail) returnVal = Fail;
        if (VectorCopyToArrayTest<nuint>.VectorCopyToArray(17, random) == Fail) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("CopyTo(ref)", "Single")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Single")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "Double")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Double")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "Int32")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Int32")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "Int64")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Int64")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "UInt16")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "Byte")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Byte")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "Int16")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "Int16")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "SByte")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "SByte")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "UInt32")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "UInt64")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "IntPtr")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref)", "UIntPtr")) returnVal = Fail;
        if (!jitLog.Check("CopyTo(ref,int)", "UIntPtr")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}
