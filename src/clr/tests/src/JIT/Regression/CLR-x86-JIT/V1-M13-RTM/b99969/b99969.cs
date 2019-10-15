// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

internal class Test
{
    public int i;
    private volatile bool _bSpoof = false;
    private volatile bool _bDoSpoof = false;

    private static Test s_target;

    private static void DoSpoof()
    {
        while (!s_target._bDoSpoof) ;
        s_target.i = 80000000;
        s_target._bSpoof = true;
    }

    private int Function1()
    {
        try
        {
            return Function();
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("IndexOutOfRangeException caught");
            return 99;
        }
    }

    private int Function()
    {
        int[] arr = new int[1];

        i = 0;
        _bSpoof = false;
        _bDoSpoof = false;


        int result = arr[i];

        result += arr[i];

        _bDoSpoof = true;
        result += arr[i];

        while (_bSpoof == false) ;

        result += arr[i];

        return result;
    }

    private static int Main()
    {
        Test t = new Test();

        s_target = t;

        Thread thread = new Thread(new ThreadStart(DoSpoof));
        thread.Start();

        int a = t.Function1();

        if (a == 99)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
