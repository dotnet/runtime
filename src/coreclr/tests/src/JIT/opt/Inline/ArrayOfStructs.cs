// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

struct Foo
{
    public int x;

}

public class Test
{
    static int StructTaker_Inline(Foo FooStruct)
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
