// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Reflection;
using System.Collections;
/// <summary>
///HasElementTypeImpl
/// </summary>
public class TypeHasElementTypeImpl
{
    public static int Main()
    {
        TypeHasElementTypeImpl TypeHasElementTypeImpl = new TypeHasElementTypeImpl();

        TestLibrary.TestFramework.BeginTestCase("TypeHasElementTypeImpl");
        if (TypeHasElementTypeImpl.RunTests())
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

        TestLibrary.TestFramework.BeginScenario("PosTest1:  The Type is an array ");
        try
        {
            int[] myArray = new int[5];
            if (!myArray.GetType().HasElementType)
            {
                TestLibrary.TestFramework.LogError("001", "HasElementType should return false." );
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
    public  bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:  The Type is IntPtr ");
        try
        {
            IntPtr myInt=new IntPtr(5);
            if (myInt.GetType().HasElementType)
            {
                TestLibrary.TestFramework.LogError("001", "HasElementType should return false.");
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
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:  The Type is reference type");
        try
        {
            TestClass myInstance = new TestClass();
            if (myInstance.GetType().HasElementType)
            {
                TestLibrary.TestFramework.LogError("001", "HasElementType should return false.");
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
    private void MyMethod(ref TestClass myTest)
    {

    }
   
}

public class TestClass
{

}