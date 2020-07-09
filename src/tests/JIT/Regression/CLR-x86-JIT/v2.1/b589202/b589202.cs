// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
class Program
{
    static int Main()
    {
        Test(null);
        Console.WriteLine("Test Success");
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test(string x)
    {
        for (int i = 0; i < 10; ++i)
        {
            if (String.IsNullOrEmpty(x))
            { }
        }
    }
}
