// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * We need to propigate array dimmension changes through OPADDs that are already NonNull. 
 * Expected and actual output is at the end of the test.
 * */

using System;

public class Test

{
    private static int Main()

    {
        int[] iAr1 = null;

        for (int j = 10; j < 20; j++)

        {
            Console.WriteLine("j=" + j);

            iAr1 = new int[j];

            Console.WriteLine(iAr1.Length); // wrong when j=11

            for (int i = 0; i < j; i++)

            {
                Console.Write(i + " ");

                iAr1[i] = i; // IndexOutOfRangeException when j=11, i=10
            }

            Console.WriteLine();
        }

        Console.WriteLine("Done");

        return 100;
    }
}


/* 
Expected: 

C:\Temp>repro
j=10
10
0 1 2 3 4 5 6 7 8 9
j=11
11
0 1 2 3 4 5 6 7 8 9 10
j=12
12
0 1 2 3 4 5 6 7 8 9 10 11
j=13
13
0 1 2 3 4 5 6 7 8 9 10 11 12
j=14
14
0 1 2 3 4 5 6 7 8 9 10 11 12 13
j=15
15
0 1 2 3 4 5 6 7 8 9 10 11 12 13 14
j=16
16
0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
j=17
17
0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16
j=18
18
0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17
j=19
19
0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18
Done

 

Actual:

C:\Temp>repro
j=10
10
0 1 2 3 4 5 6 7 8 9
j=11
10
0 1 2 3 4 5 6 7 8 9 10
Unhandled Exception: System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at Test.Main() 
 
*/
