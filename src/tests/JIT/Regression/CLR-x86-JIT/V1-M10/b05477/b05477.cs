// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class TestClass
{
    public int IntI = 0;
}

public class mem035
{

    public static TestClass getTC
    {
        get
        {
            return null;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int RetInt = 1;

        try
        {
            int TempInt = getTC.IntI;
        }
        catch (NullReferenceException)
        {
            RetInt = 100;
        }
        return RetInt;
    }
}
