// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
