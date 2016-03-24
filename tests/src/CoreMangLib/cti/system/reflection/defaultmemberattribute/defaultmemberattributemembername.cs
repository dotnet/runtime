// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// System.Reflection.DefaultMemberAttribute.MemberName
/// </summary>
public class DefaultMemberAttributeMemberName
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        DefaultMemberAttributeMemberName dmaMemberName = new DefaultMemberAttributeMemberName();
        TestLibrary.TestFramework.BeginTestCase("Testing for Method: DefaultMemberAttribute.Ctor()...");

        if (dmaMemberName.RunTests())
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
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verfify property MemberName is random string ... ";
        string c_TEST_ID = "P001";
        string memberName = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        DefaultMemberAttribute dma = new DefaultMemberAttribute(memberName);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (dma.MemberName != memberName)
            {
                string errorDesc = "value is not " + memberName + " as expected: Actual is " + dma.MemberName;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verfify the MemberName is empty ... ";
        string c_TEST_ID = "P002";
        string memberName = "";

        DefaultMemberAttribute dma = new DefaultMemberAttribute(memberName);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (dma.MemberName != memberName)
            {
                string errorDesc = "value is not " + memberName + " as expected: Actual is empty";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verfify the MemberName is a null reference ... ";
        string c_TEST_ID = "P003";
        string memberName = null;

        DefaultMemberAttribute dma = new DefaultMemberAttribute(memberName);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (dma.MemberName != memberName)
            {
                string errorDesc = "value is not " + memberName + " as expected: Actual is null";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexcpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
