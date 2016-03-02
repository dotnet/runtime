// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.Append(Decimal)
/// </summary>
public class StringBuilderAppend5
{
    public static int Main()
    {
        StringBuilderAppend5 sbAppend5 = new StringBuilderAppend5();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend5");
        if (sbAppend5.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke Append method in the initial StringBuilder 1");
        try
        {
            StringBuilder sb = new StringBuilder();
            decimal decimalVal = (decimal)TestLibrary.Generator.GetInt32(-55);
            sb = sb.Append(decimalVal);
            if (sb.ToString() != decimalVal.ToString())
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
            decimal decimalVal = (decimal)TestLibrary.Generator.GetInt32(-55);
            sb = sb.Append(decimalVal);
            if (sb.ToString() != strSource + decimalVal.ToString())
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
            decimal decimalVal = (decimal)TestLibrary.Generator.GetInt32(-55);
            sb = sb.Append(decimalVal);
            if (sb.ToString() != decimalVal.ToString())
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
    #endregion
}
