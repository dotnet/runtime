// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe class ArrayWithOffsetTest
{
    public static int Main()
    {
        try
        {
            Span<int> expected = new int[] {1, 2, 3, 4, 5, 6};
            Span<int> newValue = new int[] {7, 8, 9, 10, 11, 12};

            for (int i = 0; i < expected.Length; i++)
            {
                int[] array = new int[] {1, 2, 3, 4, 5, 6};
                ArrayWithOffset offset = new ArrayWithOffset(array, i * 4); // The offset parameter in ArrayWithOffset is a byte-offset, not an element offset.

                fixed (int* expectedSubArray = expected.Slice(i))
                fixed (int* newValueSubArray = newValue.Slice(i))
                {
                    Assert.IsTrue(ArrayWithOffsetNative.Marshal_InOut(expectedSubArray, offset, expected.Length - i, newValueSubArray), $"Native call failed with element offset {i}.");
                }

                for (int j = 0; j < i; j++)
                {
                    Assert.AreEqual(expected[j], array[j]);
                }

                for (int j = i; j < array.Length; j++)
                {
                    Assert.AreEqual(newValue[j], array[j]);
                }
            }

            ArrayWithOffset arrayWithOffset = new ArrayWithOffset(new int[]{ 1 }, 0);

            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid(arrayWithOffset));
            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid(ref arrayWithOffset));
            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid_Return());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }

        return 100;
    }
}
