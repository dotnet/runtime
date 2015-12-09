// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorArrayInitTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        public static int VectorArrayInit(int size, Random random)
        {
            int returnVal = Pass;

            if (size < Vector<T>.Count) size = Vector<T>.Count;
            int index = size - Vector<T>.Count;
            T[] inputArray = GetRandomArray<T>(size, random);
            bool caught;

            Vector<T> v1 = new Vector<T>(inputArray);
            Vector<T> v2 = new Vector<T>(inputArray, index);

            for (int i = 0; i < Vector<T>.Count; i++)
            {
                if (!CheckValue(v1[i], inputArray[i])) returnVal = Fail;
                if (!CheckValue(v2[i], inputArray[index + i])) returnVal = Fail;
            }

            // Test a null input array.
            caught = false;
            try
            {
                Vector<T> v = new Vector<T>(null, 0);
                // Check one of the values so that v is not optimized away.
                // TODO: Also test without this because it should still throw.
                if (!CheckValue(v[0], inputArray[0])) returnVal = Fail;
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
                Vector<T> v = new Vector<T>(inputArray, -1);
            }
            catch (IndexOutOfRangeException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw IndexOutOfRangeException for a negative index.");
                returnVal = Fail;
            }

            // Test an out-of-range index.
            caught = false;
            try
            {
                Vector<T> v = new Vector<T>(inputArray, inputArray.Length);
            }
            catch (IndexOutOfRangeException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw IndexOutOfRangeException for an out-of-range index.");
                returnVal = Fail;
            }

            // Test insufficient range in target array.
            caught = false;
            try
            {
                Vector<T> v = new Vector<T>(inputArray, inputArray.Length - 1);
            }
            catch (IndexOutOfRangeException)
            {
                caught = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.GetType());
            }
            if (!caught)
            {
                Console.WriteLine("Failed to throw IndexOutOfRangeException for insufficient range in target array.");
                returnVal = Fail;
            }

            return returnVal;
        }
    }

    private static int Main()
    {
        int returnVal = Pass;
        Random random = new Random(100);

        if (VectorArrayInitTest<Single>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<Single>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<Double>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<Double>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<int>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<int>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<long>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<long>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<ushort>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<ushort>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<byte>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<byte>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<short>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<short>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<sbyte>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<sbyte>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<uint>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<uint>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<ulong>.VectorArrayInit(12, random) == Fail) returnVal = Fail;
        if (VectorArrayInitTest<ulong>.VectorArrayInit(17, random) == Fail) returnVal = Fail;
        return returnVal;
    }
}
