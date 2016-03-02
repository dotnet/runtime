// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics;

/// <summary>
/// ConditionalAttribute.ConditionString(v-yaduoj)
/// </summary>
public class ConditionalAttributeConditionString
{
    public static int Main()
    {
        ConditionalAttributeConditionString testObj = new ConditionalAttributeConditionString();

        TestLibrary.TestFramework.BeginTestCase("for property: ConditionalAttribute.ConditionString");
        if (testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Get unempty ConditionString";
        string errorDesc;

        string expectedCondition;
        string actualCondition;
        expectedCondition = "CLR_Test_Case";
        ConditionalAttribute conditionalAtt = new ConditionalAttribute(expectedCondition);
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualCondition = conditionalAtt.ConditionString;
            if (actualCondition != expectedCondition)
            {
                errorDesc = "The value of property ConditionString of instance of class ConditionalAttribute is not " +
                            "the value \"" + expectedCondition + "\" as expected: Actually\"" + actualCondition +
                            "\"";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: Get empty ConditionString";
        string errorDesc;

        string expectedCondition;
        string actualCondition;
        expectedCondition = string.Empty;
        ConditionalAttribute conditionalAtt = new ConditionalAttribute(expectedCondition);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualCondition = conditionalAtt.ConditionString;
            if (actualCondition != expectedCondition)
            {
                errorDesc = "The value of property ConditionString of instance of class ConditionalAttribute is not " +
                            "the value string.Empty as expected: Actually\"" + actualCondition +
                            "\"";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: Get null reference ConditionString";
        string errorDesc;

        string expectedCondition;
        string actualCondition;
        expectedCondition = null;
        ConditionalAttribute conditionalAtt = new ConditionalAttribute(expectedCondition);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualCondition = conditionalAtt.ConditionString;
            if (actualCondition != expectedCondition)
            {
                errorDesc = "The value of property ConditionString of instance of class ConditionalAttribute is not " +
                            "the value null reference as expected: Actually\"" + actualCondition +
                            "\"";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
