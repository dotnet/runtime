// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Reflection;
using System.Collections;
/// <summary>
///IsByRefImpl
/// </summary>
public class TypeIsByRefImpl
{
    public static int Main()
    {
        TypeIsByRefImpl TypeIsByRefImpl = new TypeIsByRefImpl();

        TestLibrary.TestFramework.BeginTestCase("TypeIsByRefImpl");
        if (TypeIsByRefImpl.RunTests())
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
        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:  override isByRefImpl and Only these type which BaseType is BaseClass Return true ,TestClass as test object ");
        try
        {
            TestClass myobject = new TestClass();
            if (myobject.GetType().IsByRef)
            {
                TestLibrary.TestFramework.LogError("001", "TestClass is not Passed by reference.");
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

        TestLibrary.TestFramework.BeginScenario("PosTest1:   override isByRefImpl and Only these type which BaseType is BaseClass Return true ,TestClass1 as test object  ");
        try
        {
            TestClass1 myobject = new TestClass1();
            if (myobject.GetType().IsByRef)
            {
                TestLibrary.TestFramework.LogError("003", "TestClass1 is not Passed by reference.");
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
    

}
public class BaseClass
{

}
public class TestClass : BaseClass
{

}
public class TestClass1
{

}
