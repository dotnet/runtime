// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

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
                v1.CopyTo(null);
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
        Random random = new Random(100);

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
        return returnVal;
    }
}
