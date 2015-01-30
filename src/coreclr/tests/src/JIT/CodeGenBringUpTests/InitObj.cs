// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

public struct MyClass
{
    public int x;
    public int y;
    public int z;
}

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool InitObj()
    {
        MyClass c = new MyClass();
        return c.x == c.y &&
               c.y == c.z &&
               c.z == 0;
    }


    public static int Main()
    {
        if (InitObj())
        {
            return Pass;
        }
        else
        {
            return Fail;
        }
    }
}
