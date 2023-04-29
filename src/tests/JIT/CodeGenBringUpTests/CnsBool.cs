// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_CnsBool
{
    const int Pass = 100;
    const int Fail = -1;

    // Returns !b
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool CnsBool(bool b) 
    { 
       // Thisis just to exercise bool constants.
       // Otherwise we could write this as "return !b"
       if (b == true)
          return false;

       return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool b = CnsBool(false);
        if (b) return Pass;
        else return Fail;
    }
}
