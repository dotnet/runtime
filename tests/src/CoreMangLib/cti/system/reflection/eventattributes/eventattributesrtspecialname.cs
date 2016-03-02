// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
///  RTSpecialName [v-ly]
/// </summary>
public class EventAttributesRTSpecialName
{
    public static int Main()
    {
        EventAttributesRTSpecialName test = new EventAttributesRTSpecialName();

        TestLibrary.TestFramework.BeginTestCase("EventAttributesRTSpecialName");

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

        //TestLibrary.TestFramework.LogInformation("[Negative]");
        //retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Postive Test Case
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ");

        try
        {
            int expected = 0x0400;
            int actual = (int)EventAttributes.RTSpecialName;
            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "RTSpecialName's value is not 0x0400");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.Message);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
}