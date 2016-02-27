// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>
/// Unint32.MaxValue
/// </summary>
public class UInt32MaxValue
{
    private UInt32 c_UINT32_MAXVALUE = 4294967295;
    public static int Main()
    {
        UInt32MaxValue ui32mv = new UInt32MaxValue();
        TestLibrary.TestFramework.BeginTestCase("UInt32MaxValue");

        if (ui32mv.RunTests())
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
        retVal = PosTest2() && retVal;
        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UnInt32.MaxValue should return the UnInt32' maxValue 1");
        try
        {
            UInt32 uintA = UInt32.MaxValue;
            if (uintA != c_UINT32_MAXVALUE)
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
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UnInt32.MaxValue should return the UnInt32' maxValue 2");
        try
        {
            UInt32 uintA = UInt32.MaxValue;
            if (uintA != 0xffffffff)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;

    }
}

