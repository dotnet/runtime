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
    public static int size = 10;



    public T[,] TArray;

    public void StoreTArray(T[] arr)
    {
        TArray = new T[size, size];
        int i, j;

        for (i = 0; (i < size); i++)
        {
            for (j = 0; (j < size); j++)
            {
                TArray[i, j] = arr[(i * 10) + j];
            }
        }
    }

    public void LoadTArray(out T[] arr)
    {
        arr = new T[size * size];
        int i, j;
        for (i = 0; (i < size); i++)
        {
            for (j = 0; (j < size); j++)
            {
                arr[(i * 10) + j] = TArray[i, j];
            }
        }
    }

    public bool VerifyTArray(T[] arr)
    {
        int i, j;
        for (i = 0; (i < size); i++)
        {
            for (j = 0; (j < size); j++)
            {
                if (!(arr[(i * 10) + j].Equals(TArray[i, j])))
                {
                    Console.WriteLine("Failed Verification of Element TArray[{0}][{1}]", i, j);
                    return false;
                }
            }
        }
        return true;
    }

}

public class Test_struct01
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
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

        int[] IntArr_in = new int[100];
        for (i = 0; (i < (10 * 10)); i++)
        {
            IntArr_in[i] = i;
        }

        int[] IntArr_out;
        Gen<int> GenInt = new Gen<int>();
        GenInt.StoreTArray(IntArr_in);
        GenInt.LoadTArray(out IntArr_out);
        Eval(GenInt.VerifyTArray(IntArr_out));

        double[] DoubleArr_in = new double[100];
        for (i = 0; (i < 10 * 10); i++)
        {
            DoubleArr_in[i] = i;
        }

        double[] DoubleArr_out;
        Gen<double> GenDouble = new Gen<double>();
        GenDouble.StoreTArray(DoubleArr_in);
        GenDouble.LoadTArray(out DoubleArr_out);
        Eval(GenDouble.VerifyTArray(DoubleArr_out));


        string[] StringArr_in = new String[100];
        for (i = 0; (i < 10 * 10); i++)
        {
            StringArr_in[i] = i.ToString();
        }

        String[] StringArr_out;
        Gen<String> GenString = new Gen<String>();
        GenString.StoreTArray(StringArr_in);
        GenString.LoadTArray(out StringArr_out);
        Eval(GenString.VerifyTArray(StringArr_out));


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

