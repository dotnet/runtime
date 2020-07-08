// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//COMMAND LINE: csc /nologo /optimize+ /debug- /w:0 bug.cs
using System;
public struct AA
{
    static int Main()
    {
        sbyte local2 = 0;
        while (
            (new bool[5, 5])[Math.Max(local2, local2), local2]
            ) { }
        return 100;
    }
}
