// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace SizeParamIndex.PInvoke;

public class Helper
{

    #region General method

    public static T[] InitArray<T>(int arrSize)
    {
        T[] array = new T[arrSize];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (T)Convert.ChangeType(i, typeof(T));
        }
        return array;
    }

    public static bool EqualArray<T>(T[] actualArray, int actualSize, T[] expectedArray, int expectedSize)
    {
        int failures = 0;
        if (actualArray == null && expectedArray == null)
        {
            Console.WriteLine("\tTwo array are equal.Both of them null");
            return true;
        }
        else if (actualArray == null && expectedArray != null)
        {
            Console.WriteLine("\tTwo array are not equal.The sourcArr is null,but the expectedArray is not null");
            return false;
        }
        else if (actualArray != null && expectedArray == null)
        {
            Console.WriteLine("\tTwo array are not equal.The sourcArr is not null but the expectedArray is null");
            return false;
        }
        else if (!actualSize.Equals(expectedSize))
        {
            Console.WriteLine("\tTwo array are not equal.The sizes are not equal. Expected: {0}, Actual: {1}", expectedSize, actualSize);
            return false;
        }
        for (int i = 0; i < expectedSize; ++i)
        {
            if (!actualArray[i].Equals(expectedArray[i]))
            {
                Console.WriteLine("\tTwo array are not equal.The values of index {0} are not equal!", i);
                Console.WriteLine("\t\tThe actualArray is {0},the expectedArray is {1}", actualArray[i].ToString(), expectedArray[i].ToString());
                failures++;
            }
        }
        if (failures > 0)
            return false;
        return true;
    }

    public static T[] GetExpChangeArray<T>(int cSize)
    {
        T[] array = new T[cSize];

        for (int i = array.Length - 1; i >= 0; --i)
            array[i] = (T)Convert.ChangeType(array.Length - 1 - i, typeof(T));

        return array;
    }

    public static bool CheckAndChangeArray<T>(ref T[] arrArg, ref T arrSize, int actualArrSize, int expectedArrSize)
    {
        T[] actualArr = InitArray<T>(actualArrSize);
        if (!EqualArray<T>(arrArg, actualArrSize, actualArr, actualArrSize))
        {
            return false;
        }

        arrSize = (T)Convert.ChangeType(expectedArrSize, typeof(T));
        arrArg = GetExpChangeArray<T>(expectedArrSize);
        return true;
    }

    #endregion

}
