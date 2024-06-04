// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct HFAStruct
{
    public float f1;
    public float f2;
    public float f3;
}

public struct IntStruct
{
    public int i1;
    public int i2;
    public int i3;
}


public class Test_hfaParam
{
    public static int TestMethod(HFAStruct hfaStruct, IntStruct intStruct)
    {
        if (hfaStruct.f1 != 1.0f)
            return 0;
        if (hfaStruct.f2 != 2.0f)
            return 1;
        if (hfaStruct.f3 != 3.0f)
            return 2;
        if (intStruct.i1 != 1)
            return 3;
        if (intStruct.i2 != 2)
            return 4;
        if (intStruct.i3 != 3)
            return 5;

        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        HFAStruct hfaStruct = new HFAStruct();
        hfaStruct.f1 = 1.0f;
        hfaStruct.f2 = 2.0f;
        hfaStruct.f3 = 3.0f;

        IntStruct intStruct = new IntStruct();
        intStruct.i1 = 1;
        intStruct.i2 = 2;
        intStruct.i3 = 3;

        int result = TestMethod(hfaStruct, intStruct);
        if (result != 100)
            return -result;

        return (int)typeof(Test_hfaParam).GetMethod("TestMethod").Invoke(null, new object[] {hfaStruct, intStruct});
    }
}
