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


public class Gen<T>
{
    public T Fld1;

    public Gen(T fld1)
    {
        Fld1 = fld1;
    }


}

public class Test_class06
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
        int size = 10;
        int i, j, k, l, m;
        double sum = 0;

        Gen<ValX1<int>>[][][][][] GenArray = new Gen<ValX1<int>>[size][][][][];



        for (i = 0; i < size; i++)
        {
            GenArray[i] = new Gen<ValX1<int>>[i][][][];
            for (j = 0; j < i; j++)
            {
                GenArray[i][j] = new Gen<ValX1<int>>[j][][];
                for (k = 0; k < j; k++)
                {
                    GenArray[i][j][k] = new Gen<ValX1<int>>[k][];
                    for (l = 0; l < k; l++)
                    {
                        GenArray[i][j][k][l] = new Gen<ValX1<int>>[l];
                        for (m = 0; m < l; m++)
                        {
                            GenArray[i][j][k][l][m] = new Gen<ValX1<int>>(new ValX1<int>(i * j * k * l * m));
                        }
                    }
                }
            }
        }

        for (i = 0; i < size; i++)
        {
            for (j = 0; j < i; j++)
            {
                for (k = 0; k < j; k++)
                {
                    for (l = 0; l < k; l++)
                    {
                        for (m = 0; m < l; m++)
                        {
                            sum += GenArray[i][j][k][l][m].Fld1.t;
                        }
                    }
                }
            }
        }

        Eval(sum == 269325);
        sum = 0;

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

