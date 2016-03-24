// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
/// <summary>
/// Attribute.GetHashCode()
/// </summary>
public class AttributeGetHashCode
{
    public static int Main()
    {
        AttributeGetHashCode attrGetHashCode = new AttributeGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("Regression Test: DDB 125472");
        if (attrGetHashCode.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Regression Test for DDB 125472]");
        retVal = PosTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Attribute.GetHashCode()");
		try
		{
			MyAttribute myAttr = new MyAttribute();
			myAttr.GetHashCode();
		}
		catch (FieldAccessException e)
		{
			TestLibrary.TestFramework.LogError("001", "DDB 125472 is present. Exception:"+e);
			retVal = false;
		}
		catch (Exception e)
		{
			TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
			retVal = false;
		}
        return retVal;
    }
    #endregion
	class MyAttribute : Attribute { }   
}