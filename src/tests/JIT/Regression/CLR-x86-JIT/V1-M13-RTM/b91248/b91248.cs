// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//COMMAND LINE: csc /nologo /optimize- /debug- /w:0 bug.cs
using System;
public struct AA
{
    static void Test(int param, __arglist)
    {
        int[,] aa = new int[2, 2];
        do
        {
            try { }
            catch (Exception) { }
            aa[param, Math.Min(0, 1)] = 0;
        } while ((new bool[2, 2])[param, param]);
    }
    static int Main() { Test(0, __arglist()); return 100; }
}
