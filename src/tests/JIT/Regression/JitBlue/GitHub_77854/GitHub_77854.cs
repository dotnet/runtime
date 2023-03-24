// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;

public class Program
{
    public static int Main()
    {
        new Program().TestFunction();
        return 100;
    }

    private TestStruct CreateStruct(FourKStruct s, int i)
    {
        return new TestStruct { s = s, i = i };
    }

    private TestStruct TestFunction()
    {
        return CreateStruct(new FourKStruct(), 42);
    }

    private struct TestStruct
    {
        public FourKStruct s;
        public int i;
    }
}

[StructLayout(LayoutKind.Sequential, Size=4096)]
public partial struct FourKStruct
{
    internal byte bytes;
}
