// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

// 10 byte struct
public struct A
{
    public int a;
    public int b;
    public short c;
}

public class TailCallStructPassingSimple
{
    // Simple tail call candidate that would be ignored on Arm64 and amd64 Unix
    // due to https://github.com/dotnet/runtime/issues/4941
    public static int ImplicitTailCallTenByteStruct(A a, int count=1000)
    {
        if (count-- == 0)
        {
            return 100;
        }

        return ImplicitTailCallTenByteStruct(a, count);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        A temp = new A();
        temp.a = 50;
        temp.b = 500;
        temp.c = 62;

        int ret = ImplicitTailCallTenByteStruct(temp);
        return ret;
    } 
}
