// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Threading;
using Xunit;

public struct Struct
{
    public uint a;
    public uint b;
}

public class Program
{
    [ThreadStatic]
    private static Struct TSStruct;

    [Fact]
    public static int TestEntryPoint()
    {
        if(TSStruct.a != 0 || TSStruct.b != 0)
            return 101;

        Struct str = new Struct ();
        str.a = 0xdeadbeef;
        str.b = 0xba5eba11;

        TSStruct = str;
        if(TSStruct.a != 0xdeadbeef || TSStruct.b != 0xba5eba11)
            return 102;

        Struct str2 = TSStruct;
        if(str2.a != 0xdeadbeef || str2.b != 0xba5eba11)
            return 103;

        Console.WriteLine("Test Succeeded.");
        return 100;
    }
}
