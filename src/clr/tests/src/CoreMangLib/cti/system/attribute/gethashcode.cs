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
        TestLibrary.TestFramework.BeginTestCase("AttributeGetHashCode");
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
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Get the Attribute Hash Code");
        try
        {
            int intVal = TestLibrary.Generator.GetInt32(-55);
            MyAttribute myAttr = new MyAttribute(intVal);
            int hashcode = myAttr.GetHashCode();
            if (hashcode != intVal)
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
    #endregion
    #region ForTestClass
    public class MyAttribute : Attribute
    {
        protected int hashcod;
        public MyAttribute(int HashCode)
        {
            this.hashcod = HashCode;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    #endregion
}
