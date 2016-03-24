// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// MarshalAsAttribute.ArraySubType [v-minch]
/// </summary>
public class MarshalAsAttributeArraySubType
{
    public static int Main()
    {
        MarshalAsAttributeArraySubType test = new MarshalAsAttributeArraySubType();
        TestLibrary.TestFramework.BeginTestCase("MarshalAsAttribute.ArraySubType");
        if (test.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return the field ArraySubType in MarshalAsAttribute class 1");
        try
        {
            short unmanagedType = Int16.MaxValue;
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(unmanagedType);
            UnmanagedType myVal = myMarshalAsAttribute.ArraySubType;
            if (myVal != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 0 but the ActualResult is " + myVal.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Return the field ArraySubType in MarshalAsAttribute class 2");
        try
        {
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(UnmanagedType.ByValArray| UnmanagedType.LPArray);
            UnmanagedType myVal = myMarshalAsAttribute.ArraySubType;
            if (myVal != 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is 0 but the ActualResult is " + myVal.ToString());
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
    #endregion

}