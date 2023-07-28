// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct MyStruct
{
    public byte x1;
    public int x2;

}

public class MainApp
{
    static byte s = 1;

    [Fact]
    public static int TestEntryPoint()
    {
        MyStruct myStruct;

        myStruct.x1 = s;

        myStruct.x1 = (byte)(myStruct.x1 | 1);

        Console.WriteLine(myStruct.x1);

        if (myStruct.x1 == 1)
        {
            return 100;
        }
        else
        {
            return 101;
        }
    }
};
