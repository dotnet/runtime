// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System;

public class Perf_Array
{
    private static int[] s_arr;
    private static Array s_arr1;
    private static Array s_arr2;
    private static Array s_arr3;

    private const int MAX_ARRAY_SIZE = 4096;

    private static readonly int s_DIM_1 = MAX_ARRAY_SIZE;
    private static readonly int s_DIM_2 = (int)Math.Pow(MAX_ARRAY_SIZE, (1.0 / 2.0));
    private static readonly int s_DIM_3 = (int)(Math.Pow(MAX_ARRAY_SIZE, (1.0 / 3.0)) + .001);

    [Benchmark]
    public static void ArrayCreate()
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                s_arr = new int[s_DIM_1];
    }

    [Benchmark]
    public static void ArrayCopy()
    {
        int[] dummy = new int[s_DIM_1];
        s_arr = new int[s_DIM_1];

        for (int i = 0; i < s_DIM_1; i++)
            s_arr[i] = i;

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_1; j++)
                {
                    dummy[j] = s_arr[j];
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayAssign()
    {
        s_arr = new int[s_DIM_1];

        for (int j = 0; j < s_DIM_1; j++)
            s_arr[j] = j;

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_1; j++)
                {
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                    s_arr[j] = j;
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayRetrieve()
    {
        int value;
        s_arr = new int[s_DIM_1];

        for (int i = 0; i < s_DIM_1; i++)
            s_arr[i] = i;

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_1; j++)
                {
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                    value = s_arr[j];
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayCreate1D()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                s_arr1 = Array.CreateInstance(typeof(System.Int32), s_DIM_1);
            }
        }
    }

    [Benchmark]
    public static void ArrayCreate2D()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                s_arr2 = Array.CreateInstance(typeof(System.Int32), s_DIM_2, s_DIM_2);
            }
        }
    }

    [Benchmark]
    public static void ArrayCreate3D()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                s_arr3 = Array.CreateInstance(typeof(System.Int32), s_DIM_3, s_DIM_3, s_DIM_3);
            }
        }
    }

    [Benchmark]
    public static void ArrayAssign1D()
    {
        s_arr1 = Array.CreateInstance(typeof(System.Int32), s_DIM_1);

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_1; j++)
                {
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                    s_arr1.SetValue(j, j);
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayAssign2D()
    {
        s_arr2 = Array.CreateInstance(typeof(System.Int32), s_DIM_2, s_DIM_2);

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_2; j++)
                {
                    for (int k = 0; k < s_DIM_2; k++)
                    {
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                        s_arr2.SetValue(j + k, j, k);
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayAssign3D()
    {
        s_arr3 = Array.CreateInstance(typeof(System.Int32), s_DIM_3, s_DIM_3, s_DIM_3);

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                    {
                        for (int l = 0; l < s_DIM_3; l++)
                        {
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                            s_arr3.SetValue(j + k + l, j, k, l);
                        }
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayRetrieve1D()
    {
        int value;
        s_arr1 = Array.CreateInstance(typeof(System.Int32), s_DIM_1);

        for (int i = 0; i < s_DIM_1; i++)
            s_arr1.SetValue(i, i);

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_1; j++)
                {
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                    value = (int)s_arr1.GetValue(j);
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayRetrieve2D()
    {
        int value;
        s_arr2 = Array.CreateInstance(typeof(System.Int32), s_DIM_2, s_DIM_2);

        for (int i = 0; i < s_DIM_2; i++)
        {
            for (int j = 0; j < s_DIM_2; j++)
                s_arr2.SetValue(i + j, i, j);
        }

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_2; j++)
                {
                    for (int k = 0; k < s_DIM_2; k++)
                    {
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                        value = (int)s_arr2.GetValue(j, k);
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayRetrieve3D()
    {
        int value;
        s_arr3 = Array.CreateInstance(typeof(System.Int32), s_DIM_3, s_DIM_3, s_DIM_3);

        for (int i = 0; i < s_DIM_3; i++)
        {
            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                    s_arr3.SetValue(i + j + k, i, j, k);
            }
        }

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                    {
                        for (int l = 0; l < s_DIM_3; l++)
                        {
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                            value = (int)s_arr3.GetValue(j, k, l);
                        }
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void ArrayCopy1D()
    {
        Array dummy = Array.CreateInstance(typeof(System.Int32), s_DIM_1);
        s_arr1 = Array.CreateInstance(typeof(System.Int32), s_DIM_1);

        for (int i = 0; i < s_DIM_1; i++)
            s_arr1.SetValue(i, i);

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Array.Copy(s_arr1, dummy, MAX_ARRAY_SIZE);
            }
        }
    }

    [Benchmark]
    public static void ArrayCopy2D()
    {
        int arrayLen = (int)Math.Pow(s_DIM_2, 2);

        Array dummy = Array.CreateInstance(typeof(System.Int32), s_DIM_2, s_DIM_2);
        s_arr2 = Array.CreateInstance(typeof(System.Int32), s_DIM_2, s_DIM_2);

        for (int i = 0; i < s_DIM_2; i++)
        {
            for (int j = 0; j < s_DIM_2; j++)
                s_arr2.SetValue(i + j, i, j);
        }

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Array.Copy(s_arr2, dummy, arrayLen);
            }
        }
    }

    [Benchmark]
    public static void ArrayCopy3D()
    {
        int arrayLen = (int)Math.Pow(s_DIM_3, 3);

        Array dummy = Array.CreateInstance(typeof(System.Int32), s_DIM_3, s_DIM_3, s_DIM_3);
        s_arr3 = Array.CreateInstance(typeof(System.Int32), s_DIM_3, s_DIM_3, s_DIM_3);

        for (int i = 0; i < s_DIM_3; i++)
        {
            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                {
                    s_arr3.SetValue(i + j + k, i, j, k);
                }
            }
        }

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Array.Copy(s_arr3, dummy, arrayLen);
            }
        }
    }

    [Benchmark]
    public static void ArrayResize()
    {
        // Test copying a subarray
        const int oldSize = 42;
        const int newSize = 41;

        foreach (var iteration in Benchmark.Iterations)
        {
            byte[] ary = new byte[oldSize];

            using (iteration.StartMeasurement())
            {
                Array.Resize<byte>(ref ary, newSize);
            }
        }
    }
}