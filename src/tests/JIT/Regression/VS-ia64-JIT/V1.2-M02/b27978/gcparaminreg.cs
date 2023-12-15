// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test is intended to test using SSIs to report cases where
// the address of an SSI escapes the current function.
// Note that this probably needs to be modifies in msil to 
// actually process the address of the local, rather than the local.

using System;
using Xunit;

public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;
        i += ParamInReg();
        return i + 97;
    }

    public static int ParamInReg()
    {
        String strLoc = "Param";
        return ParamHelper(strLoc);
    }

    public static int ParamHelper(String strParam)
    {
        return Func1(strParam);
    }

    public static int Func1(String strParam)
    {
        return 3;
    }
}



