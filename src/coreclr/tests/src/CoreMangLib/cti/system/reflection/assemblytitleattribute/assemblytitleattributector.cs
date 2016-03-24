// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
/// <summary>
/// AssemblyTitleAttribute(String)
/// </summary>
public class AssemblyTitleAttributeCtor
{
    private int c_MIN_STR_LENGTH = 8;
    private int c_MAX_STR_LENGTH = 256;
    public static int Main()
    {
        AssemblyTitleAttributeCtor assemTitleAttrCtor = new AssemblyTitleAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("AssemblyTitleAttributeCtor");
        if (assemTitleAttrCtor.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the AssemblyTitleAttribute 1");
        try
        {
            string title = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            AssemblyTitleAttribute assemTitleAttr = new AssemblyTitleAttribute(title);
            if (assemTitleAttr == null || assemTitleAttr.Title != title)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the AssemblyTitleAttribute 2");
        try
        {
            string title = null;
            AssemblyTitleAttribute assemTitleAttr = new AssemblyTitleAttribute(title);
            if (assemTitleAttr == null || assemTitleAttr.Title != null)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Initialize the AssemblyTitleAttribute 3");
        try
        {
            string title = string.Empty;
            AssemblyTitleAttribute assemTitleAttr = new AssemblyTitleAttribute(title);
            if (assemTitleAttr == null || assemTitleAttr.Title != "")
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
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
