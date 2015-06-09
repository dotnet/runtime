// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//COMMAND LINE: csc /nologo /optimize+ /debug- /w:0 bug.cs
using System;

public struct AA
{
    public static int Main(string[] args)
    {
        bool flag = false;
        while (flag)
        {
            args[0] = "";
            while (flag)
            {
                while (flag) { }
                throw new Exception();
            }
            while (flag) { }
        }
        return 100;
    }
}
