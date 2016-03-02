// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.Append(String)
/// </summary>
public class StringBuilderAppend16
{
    private const int c_MIN_STR_LENGTH = 8;
    private const int c_MAX_STR_LENGTH = 256;
    public static int Main()
    {
        StringBuilderAppend16 sbAppend16 = new StringBuilderAppend16();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend16");
        if (sbAppend16.RunTests())
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke Append method in the initial StringBuilder 1");
        try
        {
            StringBuilder sb = new StringBuilder();
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            sb = sb.Append(strVal);
            if (sb.ToString() != strVal.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke Append method in the initial StringBuilder 2");
        try
        {
            string strSource = "formytest";
            StringBuilder sb = new StringBuilder(strSource);
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            sb = sb.Append(strVal);
            if (sb.ToString() != strSource + strVal.ToString())
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke Append method in the initial StringBuilder 3");
        try
        {
            string strSource = null;
            StringBuilder sb = new StringBuilder(strSource);
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            sb = sb.Append(strVal);
            if (sb.ToString() != strVal.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Invoke Append method in the initial StringBuilder 4");
        try
        {
            string strSource = null;
            StringBuilder sb = new StringBuilder(strSource);
            string strVal = null;
            sb = sb.Append(strVal);
            if (sb.ToString() != string.Empty)
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Invoke Append method in the initial StringBuilder 5");
        try
        {
            string strSource = "formytest";
            StringBuilder sb = new StringBuilder(strSource);
            string strVal = null;
            sb = sb.Append(strVal);
            if (sb.ToString() != strSource)
            {
                TestLibrary.TestFramework.LogError("009", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
