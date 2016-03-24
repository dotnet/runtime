// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
/// <summary>
/// AssemblyDefaultAliasAttribute(String)
/// </summary>
public class AssemblyDefaultAliasAttributeCtor
{
    private int c_MIN_STR_LENGTH = 8;
    private int c_MAX_STR_LENGTH = 256;
    public static int Main()
    {
        AssemblyDefaultAliasAttributeCtor assemDefaultAliasAttrCtor = new AssemblyDefaultAliasAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("AssemblyDefaultAliasAttributeCtor");
        if (assemDefaultAliasAttrCtor.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the AssemblyDefaultAliasAttribute 1");
        try
        {
            string defaultAlias = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            AssemblyDefaultAliasAttribute assemDefaultAliasAttr = new AssemblyDefaultAliasAttribute(defaultAlias);
            if (assemDefaultAliasAttr == null || assemDefaultAliasAttr.DefaultAlias != defaultAlias)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the AssemblyDefaultAliasAttribute 2");
        try
        {
            string defaultAlias = null;
            AssemblyDefaultAliasAttribute assemDefaultAliasAttr = new AssemblyDefaultAliasAttribute(defaultAlias);
            if (assemDefaultAliasAttr == null || assemDefaultAliasAttr.DefaultAlias != null)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Initialize the AssemblyDefaultAliasAttribute 3");
        try
        {
            string defaultAlias = string.Empty;
            AssemblyDefaultAliasAttribute assemDefaultAliasAttr = new AssemblyDefaultAliasAttribute(defaultAlias);
            if (assemDefaultAliasAttr == null || assemDefaultAliasAttr.DefaultAlias != "")
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
