// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;


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

public class ArrayHolder
{
    public Gen<int>[, ,] GenArray = new Gen<int>[10, 10, 10];
}

public class Test
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

    public static int Main()
    {
        int size = 10;
        int i, j, k;
        double sum = 0;
        int cLoc = 0;

        ArrayHolder ArrayHolderInst = new ArrayHolder();

        for (i = 0; (i < size); i++)
        {
            for (j = 0; (j < size); j++)
            {
                for (k = 0; (k < size); k++)
                {
                    ArrayHolderInst.GenArray[i, j, k] = new Gen<int>(cLoc);
                    cLoc++;
                }
            }
        }

        for (i = 0; (i < size); i++)
        {
            for (j = 0; (j < size); j++)
            {
                for (k = 0; (k < size); k++)
                {
                    sum += ArrayHolderInst.GenArray[i, j, k].Fld1;
                    cLoc++;
                }
            }
        }




        Eval(sum == 499500);
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

