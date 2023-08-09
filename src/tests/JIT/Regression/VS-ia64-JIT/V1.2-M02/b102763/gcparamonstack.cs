// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;
        i += ParamOnStack();
        return i + 97;
    }

    public static int ParamOnStack()
    {
        String strLoc = "ParamOnStack";
        return ParamOnStackHelper(1, 2, 3, 4, 5, 6, 7, 8, 9, strLoc);
    }

    public static int ParamOnStackHelper(int i1, int i2, int i3,
        int i4, int i5, int i6, int i7, int i8, int i9, String strParam)
    {
        return Func1(strParam);
    }

    public static int Func1(String strParam)
    {
        return 3;
    }
}



