// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * This is a potential security exploit. Variance allows a sealed type to be cast to/from another sealed type that is neither it's base class or derived class (which to the JIT makes it look like interfaces or other unsealed types).
 */

using System;

internal static class Repro
{
    private static bool CheckType(Action<string> a)
    {
        return a.GetType() == typeof(Action<object>);
    }
    private static int Main()
    {
        Action<string> a = (Action<object>)Console.WriteLine;
        if (CheckType(a))
        {
            Console.WriteLine("pass");
            return 100;
        }
        Console.WriteLine("FAIL");
        return 101;
    }
}
