// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;


public struct ValX1<T>
{
    public T t;
    public ValX1(T t)
    {
        this.t = t;
    }
}
public class RefX1<T>
{
    public T t;
    public RefX1(T t)
    {
        this.t = t;
    }
}


public struct Gen<T>
{
    public T Fld1;

    public Gen(T fld1)
    {
        Fld1 = fld1;
    }
}

public class ArrayTest<T>
{
    public void DoArrayTest(T[] InArr, out T[] OutArr)
    {
        int size = 2;
        int i, j;

        Gen<T>[,] GenArray = new Gen<T>[size, size];

        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                GenArray[i, j] = new Gen<T>(InArr[i * size + j]);
            }
        }

        OutArr = new T[InArr.Length];
        for (i = 0; i < size; i++)
        {
            for (j = 0; j < size; j++)
            {
                OutArr[i * size + j] = GenArray[i, j].Fld1;
            }
        }
    }
}

public class Test_struct01_gen
{
    public static int counter = 0;
    public static bool result = true;
    internal static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine("Test Failed at location: " + counter);
        }

    }

    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;
        int[] int_arr = new int[] { 0, 1, 2, 3 };
        int[] int_arr_res;

        new ArrayTest<int>().DoArrayTest(int_arr, out int_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(int_arr[i].Equals(int_arr_res[i]));
        }

        double[] double_arr = new double[] { 0, 1, 2, 3 };
        double[] double_arr_res;

        new ArrayTest<double>().DoArrayTest(double_arr, out double_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(double_arr[i].Equals(double_arr_res[i]));
        }

        string[] string_arr = new string[] { "0", "1", "2", "3" };
        string[] string_arr_res;

        new ArrayTest<string>().DoArrayTest(string_arr, out string_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(string_arr[i].Equals(string_arr_res[i]));
        }

        object[] object_arr = new object[] { "0", "1", "2", "3" };
        object[] object_arr_res;

        new ArrayTest<object>().DoArrayTest(object_arr, out object_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(object_arr[i].Equals(object_arr_res[i]));
        }

        RefX1<int>[] RefX1Int_arr = new RefX1<int>[] { new RefX1<int>(0), new RefX1<int>(1), new RefX1<int>(2), new RefX1<int>(3) };
        RefX1<int>[] RefX1Int_arr_res;

        new ArrayTest<RefX1<int>>().DoArrayTest(RefX1Int_arr, out RefX1Int_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(RefX1Int_arr[i].Equals(RefX1Int_arr_res[i]));
        }

        ValX1<int>[] ValX1Int_arr = new ValX1<int>[] { new ValX1<int>(0), new ValX1<int>(1), new ValX1<int>(2), new ValX1<int>(3) };
        ValX1<int>[] ValX1Int_arr_res;

        new ArrayTest<ValX1<int>>().DoArrayTest(ValX1Int_arr, out ValX1Int_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(ValX1Int_arr[i].Equals(ValX1Int_arr_res[i]));
        }

        RefX1<string>[] RefX1_arr = new RefX1<string>[] { new RefX1<string>("0"), new RefX1<string>("1"), new RefX1<string>("2"), new RefX1<string>("3") };
        RefX1<string>[] RefX1_arr_res;

        new ArrayTest<RefX1<string>>().DoArrayTest(RefX1_arr, out RefX1_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(RefX1_arr[i].Equals(RefX1_arr_res[i]));
        }

        ValX1<string>[] ValX1_arr = new ValX1<string>[] { new ValX1<string>("0"), new ValX1<string>("1"), new ValX1<string>("2"), new ValX1<string>("3") };
        ValX1<string>[] ValX1_arr_res;

        new ArrayTest<ValX1<string>>().DoArrayTest(ValX1_arr, out ValX1_arr_res);
        for (i = 0; (i < 4); i++)
        {
            Eval(ValX1_arr[i].Equals(ValX1_arr_res[i]));
        }

        if (result)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed");
            return 1;
        }
    }

}

