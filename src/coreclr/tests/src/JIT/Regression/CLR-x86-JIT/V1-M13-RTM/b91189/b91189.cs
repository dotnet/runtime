// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
