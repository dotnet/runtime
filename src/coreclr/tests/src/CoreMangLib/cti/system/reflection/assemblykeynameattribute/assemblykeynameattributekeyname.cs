// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
/// <summary>
/// AssemblyKeyNameAttribute.KeyName
/// </summary>
public class AssemblyKeyNameAttributeKeyName
{
    private int c_MIN_STR_LENGTH = 8;
    private int c_MAX_STR_LENGTH = 256;
    public static int Main()
    {
        AssemblyKeyNameAttributeKeyName assemKeyNameAttrKeyName = new AssemblyKeyNameAttributeKeyName();
        TestLibrary.TestFramework.BeginTestCase("AssemblyKeyNameAttributeKeyName");
        if (assemKeyNameAttrKeyName.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the KeyName info from initialized the AssemblyKeyNameAttribute 1");
        try
        {
            string keyname = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            AssemblyKeyNameAttribute assemKeyNameAttr = new AssemblyKeyNameAttribute(keyname);
            if (assemKeyNameAttr.KeyName != keyname)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the KeyName info from initialized the AssemblyKeyNameAttribute 2");
        try
        {
            string keyname = null;
            AssemblyKeyNameAttribute assemKeyNameAttr = new AssemblyKeyNameAttribute(keyname);
            if (assemKeyNameAttr.KeyName != null)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Return the KeyName info from initialized the AssemblyKeyNameAttribute 3");
        try
        {
            string keyname = string.Empty;
            AssemblyKeyNameAttribute assemKeyNameAttr = new AssemblyKeyNameAttribute(keyname);
            if (assemKeyNameAttr.KeyName != "")
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
