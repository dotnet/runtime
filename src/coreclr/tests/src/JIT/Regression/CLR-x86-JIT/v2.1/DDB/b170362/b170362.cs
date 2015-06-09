// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public struct MyStruct
{
    public byte x1;
    public int x2;

}

class MainApp
{
    static byte s = 1;

    public static int Main()
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
