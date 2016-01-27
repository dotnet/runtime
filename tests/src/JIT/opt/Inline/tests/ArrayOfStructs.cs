// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal struct Foo
{
    public int x;
}

public class Test
{
    private static int StructTaker_Inline(Foo FooStruct)
    {
        Foo[] array = new Foo[2];
        array[0].x = 3;
        return array[0].x;
    }
    public static int Main()
    {
        try
        {
            Foo testStruct = new Foo();

            int val = StructTaker_Inline(testStruct);
            if (val == 3)
            {
                return 100;
            }

            else
            {
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
