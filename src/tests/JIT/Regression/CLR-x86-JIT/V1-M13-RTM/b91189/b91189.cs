// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//COMMAND LINE: csc /nologo /optimize+ /debug- /w:0 bug.cs
using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct AA
{
    [Fact]
    public static void TestEntryPoint() => Run(new string[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run(string[] args)
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
    }
}
