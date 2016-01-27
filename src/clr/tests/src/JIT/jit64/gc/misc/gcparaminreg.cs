// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// This test is intended to test using SSIs to report cases where
// the address of an SSI escapes the current function.
// Note that this probably needs to be modifies in msil to 
// actually process the address of the local, rather than the local.

using System;

class test
{
    public static int Main()
    {
        int i = 0;
        i += ParamInReg();
        return i;
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
        if (strParam == "Param")
            return 100;
        else return -1;
    }
}



