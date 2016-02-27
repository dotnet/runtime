// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// UInt32.MinValue
/// </summary>
public class UInt32MinValue
{
    private UInt32 c_UINT32_MINVALUE = 0;

    public static int Main()
    {
        UInt32MinValue ui32minv = new UInt32MinValue();
        TestLibrary.TestFramework.BeginTestCase("UInt32MinValue");

        if (ui32minv.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        return retVal;
    }
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UnInt32.MinValue should return the UnInt32' minValue");
        try
        {
            UInt32 uintA = UInt32.MinValue;
            if (uintA != c_UINT32_MINVALUE)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
}

