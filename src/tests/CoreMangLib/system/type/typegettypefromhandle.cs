// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Globalization;
using System.Reflection;
using System.Collections;
using Xunit;
/// <summary>
///GetTypeCode
/// </summary>
public class TypeGetTypeFromHandle
{
    [Fact]
    public static int TestEntryPoint()
    {
        TypeGetTypeFromHandle TypeGetTypeFromHandle = new TypeGetTypeFromHandle();

        TestLibrary.TestFramework.BeginTestCase("TypeGetTypeFromHandle");
        if (TypeGetTypeFromHandle.RunTests())
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
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:  The type is user define type ");
        try
        {
           
            TestClass myClass = new TestClass();
            Type myClassType = Type.GetTypeFromHandle(myClass.GetType().TypeHandle);
            if(!myClassType.Equals(typeof(TestClass)))
            {
                TestLibrary.TestFramework.LogError("001", "GetTypeFromHandle error");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
  
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:  The type is System reference type ");
        try
        {
            object myClass = new object();
            Type myClassType = Type.GetTypeFromHandle(myClass.GetType().TypeHandle);
            if (!myClassType.Equals(typeof(object)))
            {
                TestLibrary.TestFramework.LogError("003", "GetTypeFromHandle error");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:  The type is System value type ");
        try
        {
            int myClass = 1;
            Type myClassType = Type.GetTypeFromHandle(myClass.GetType().TypeHandle);
            if (!myClassType.Equals(typeof(int)))
            {
                TestLibrary.TestFramework.LogError("005", "GetTypeFromHandle error");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
   
   
   
}
public class BaseClass
{
   
    public BaseClass(string param, string s, int i)
    {

    }
}
public class TestClass : BaseClass
{
     public TestClass(string param, string s)
        : base(param, s,1)
    {

    }
    public  TestClass()
        : base("", "", 1)
    {

    }

}
