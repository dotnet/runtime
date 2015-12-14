// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class LIM
{
    public static int accumulator = 0;

    public static int Main()
    {
        int x;
        int a = GetInt(1);

        for (int i = 0; i < 5; i++)
        {
            x = a;
            Accumulate(x);
            x = GetInt(0);
            Accumulate(x);
        }

        if (accumulator == 5)
        {
            System.Console.WriteLine("Pass");
            return 100;
        }

        System.Console.WriteLine("!!!FAIL!!!");
        return -1;
    }



    public static int GetInt(int x)
    {
        try
        {
            return x;
        }
        catch
        {
            throw;
        }
    }


    public static void Accumulate(int x)
    {
        try
        {
            accumulator += x;
        }
        catch
        {
            throw;
        }
    }
}