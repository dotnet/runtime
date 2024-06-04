// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public unsafe class Program
{
    static int*[,] s_mdArray;

    static void Init(ref int* elem)
    {
        elem = (int*)98;
    }

    static Program()
    {
        s_mdArray = new int*[2,2];
        Init(ref s_mdArray[0, 0]);
        s_mdArray[1, 1] = (int*)2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return (int)s_mdArray[0, 0] + (int)s_mdArray[1, 1];
    }
}
