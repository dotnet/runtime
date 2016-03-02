// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.AttributeUsageAttribute.AttributeUsageAttribute(AttributeTargets validOn)
///</summary>

public class AttributeUsageAttributeCtor
{

    public static int Main()
    {
        AttributeUsageAttributeCtor testObj = new AttributeUsageAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("for Constructor of System.AttributeUsageAttribute");
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
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;
        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.All;

        TestLibrary.TestFramework.BeginScenario("PosTest1:set ValidOn as AttributeTargets.All and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Assembly;

        TestLibrary.TestFramework.BeginScenario("PosTest2:set ValidOn as AttributeTargets.Assembly and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Module;

        TestLibrary.TestFramework.BeginScenario("PosTest3:set ValidOn as AttributeTargets.Module and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Class;

        TestLibrary.TestFramework.BeginScenario("PosTest4:set ValidOn as AttributeTargets.Class and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Struct;

        TestLibrary.TestFramework.BeginScenario("PosTest5:set ValidOn as AttributeTargets.Struct and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("009", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.All;

        TestLibrary.TestFramework.BeginScenario("PosTest6:set ValidOn as AttributeTargets.Enum and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("011", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest7()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Constructor;

        TestLibrary.TestFramework.BeginScenario("PosTest7:set ValidOn as AttributeTargets.Constructor and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("013", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Method;

        TestLibrary.TestFramework.BeginScenario("PosTest8:set ValidOn as AttributeTargets.Method and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("015", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Property;

        TestLibrary.TestFramework.BeginScenario("PosTest9:set ValidOn as AttributeTargets.Property and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("017", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Field;

        TestLibrary.TestFramework.BeginScenario("PosTest10:set ValidOn as AttributeTargets.Field and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("019", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Event;

        TestLibrary.TestFramework.BeginScenario("PosTest11:set ValidOn as AttributeTargets.Event and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("021", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Interface;

        TestLibrary.TestFramework.BeginScenario("PosTest12:set ValidOn as AttributeTargets.Interface and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("023", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Parameter;

        TestLibrary.TestFramework.BeginScenario("PosTest13:set ValidOn as AttributeTargets.Parameter and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("025", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.Delegate;

        TestLibrary.TestFramework.BeginScenario("PosTest14:set ValidOn as AttributeTargets.Delegate and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("027", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.ReturnValue;

        TestLibrary.TestFramework.BeginScenario("PosTest15:set ValidOn as AttributeTargets.ReturnValue and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("029", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;

        AttributeTargets validOn = AttributeTargets.GenericParameter;

        TestLibrary.TestFramework.BeginScenario("PosTest16:set ValidOn as AttributeTargets.GenericParameter and create a instance of class AttributeUsageAttribute.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(validOn);
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("031", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    
    public bool PosTest17()
    {
        bool retVal = true;

        AttributeTargets expectedValue = (AttributeTargets)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));

        TestLibrary.TestFramework.BeginScenario("PosTest17:set ValidOn as Random int16 and try to get it.");
        try
        {
            AttributeUsageAttribute aUT = new AttributeUsageAttribute(expectedValue);
            
            if (aUT == null)
            {
                TestLibrary.TestFramework.LogError("033", "ExpectedObject(Not null) !=Actual(null)");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
