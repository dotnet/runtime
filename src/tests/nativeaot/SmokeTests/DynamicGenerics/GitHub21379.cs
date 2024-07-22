// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using RuntimeLibrariesTest;

// Regression test for https://github.com/dotnet/runtime/issues/22427
public class GitHub21379
{
    public static IEnumerable<object[]> BinarySearch_SZArray_TestData()
    {
        // SByte
        sbyte[] sbyteArray = new sbyte[] { sbyte.MinValue, 0, 0, sbyte.MaxValue };

        yield return new object[] { sbyteArray, 0, 4, sbyte.MinValue, null, 0 };
        yield return new object[] { sbyteArray, 0, 4, (sbyte)0, null, 1 };
        yield return new object[] { sbyteArray, 0, 4, sbyte.MaxValue, null, 3 };
        yield return new object[] { sbyteArray, 0, 4, (sbyte)1, null, -4 };

        yield return new object[] { sbyteArray, 0, 1, sbyte.MinValue, null, 0 };
        yield return new object[] { sbyteArray, 1, 3, sbyte.MaxValue, null, 3 };
        yield return new object[] { sbyteArray, 1, 3, sbyte.MinValue, null, -2 };
        yield return new object[] { sbyteArray, 1, 0, (sbyte)0, null, -2 };

        yield return new object[] { new sbyte[0], 0, 0, (sbyte)0, null, -1 };

        // Int16
        short[] shortArray = new short[] { short.MinValue, 0, 0, short.MaxValue };

        yield return new object[] { shortArray, 0, 4, short.MinValue, null, 0 };
        yield return new object[] { shortArray, 0, 4, (short)0, null, 1 };
        yield return new object[] { shortArray, 0, 4, short.MaxValue, null, 3 };
        yield return new object[] { shortArray, 0, 4, (short)1, null, -4 };

        yield return new object[] { shortArray, 0, 1, short.MinValue, null, 0 };
        yield return new object[] { shortArray, 1, 3, short.MaxValue, null, 3 };
        yield return new object[] { shortArray, 1, 3, short.MinValue, null, -2 };
        yield return new object[] { shortArray, 1, 0, (short)0, null, -2 };

        yield return new object[] { new short[0], 0, 0, (short)0, null, -1 };
    }

    public static void BinarySearch_Array(Array array, int index, int length, object value, IComparer comparer, int expected)
    {
        bool isDefaultComparer = comparer == null || comparer == Comparer.Default;
        if (index == array.GetLowerBound(0) && length == array.Length)
        {
            if (isDefaultComparer)
            {
                // Use BinarySearch(Array, object)
                Assert.AreEqual(expected, Array.BinarySearch(array, value));
                Assert.AreEqual(expected, Array.BinarySearch(array, value, Comparer.Default));
            }
            // Use BinarySearch(Array, object, IComparer)
            Assert.AreEqual(expected, Array.BinarySearch(array, value, comparer));
        }
        if (isDefaultComparer)
        {
            // Use BinarySearch(Array, int, int, object)
            Assert.AreEqual(expected, Array.BinarySearch(array, index, length, value));
        }
        // Use BinarySearch(Array, int, int, object, IComparer)
        Assert.AreEqual(expected, Array.BinarySearch(array, index, length, value, comparer));
    }

    public static void BinarySearch_SZArray<T>(T[] array, int index, int length, T value, IComparer<T> comparer, int expected)
    {
        // Forward to the non-generic overload if we can.
        bool isDefaultComparer = comparer == null || comparer == Comparer<T>.Default;
        if (isDefaultComparer || comparer is IComparer)
        {
            // Basic: forward SZArray
            BinarySearch_Array(array, index, length, value, (IComparer)comparer, expected);
        }

        if (index == 0 && length == array.Length)
        {
            if (isDefaultComparer)
            {
                // Use BinarySearch<T>(T[], T)
                Assert.AreEqual(expected, Array.BinarySearch(array, value));
                Assert.AreEqual(expected, Array.BinarySearch(array, value, Comparer<T>.Default));
            }
            // Use BinarySearch<T>(T[], T, IComparer)
            Assert.AreEqual(expected, Array.BinarySearch(array, value, comparer));
        }
        if (isDefaultComparer)
        {
            // Use BinarySearch<T>(T, int, int, T)
            Assert.AreEqual(expected, Array.BinarySearch(array, index, length, value));
        }
        // Use BinarySearch<T>(T[], int, int, T, IComparer)
        Assert.AreEqual(expected, Array.BinarySearch(array, index, length, value, comparer));
    }

    [TestMethod]
    public static void RunTest()
    {
        MethodInfo testMethodDefinition = typeof(GitHub21379).GetTypeInfo().GetDeclaredMethod("BinarySearch_SZArray");

        foreach (object[] testData in BinarySearch_SZArray_TestData())
        {
            Type elementType = testData[0].GetType().GetElementType();
            MethodInfo testMethod = testMethodDefinition.MakeGenericMethod(new Type[] { elementType });
            testMethod.Invoke(null, testData);
        }
    }
}
