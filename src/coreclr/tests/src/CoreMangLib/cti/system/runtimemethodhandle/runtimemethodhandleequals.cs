// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

public class RuntimeMethodHandleEquals
{
    public object myRuntimeMethodHandle = new RuntimeMethodHandle();
    public object myObj = new object();

    public static int Main(string[] args)
    {
        RuntimeMethodHandleEquals handleEquals =
            new RuntimeMethodHandleEquals();
        TestLibrary.TestFramework.BeginTestCase("Tesing System.RuntimeMethodHandle.Equals...");

        if (handleEquals.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify clone instance is equal to it original...");

        try
        {
            RuntimeMethodHandle myHandle = new RuntimeMethodHandle();
            object cloneHandle = myHandle;
            if (!myHandle.Equals(cloneHandle))
            {
                TestLibrary.TestFramework.LogError("001","The two instances should be equal!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
    
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify different instances are equal when they have the same values...");

        try
        {
            //default value is zero
            RuntimeMethodHandle myHandle = new RuntimeMethodHandle(); 
            if (!myHandle.Equals(myRuntimeMethodHandle))
            {
                TestLibrary.TestFramework.LogError("003","The two instance should be different!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify RuntimeMethodHandle instance is not equal to common object instance...");

        try
        {
            RuntimeMethodHandle myHandle = new RuntimeMethodHandle();
            if (myHandle.Equals(myObj))
            {
                TestLibrary.TestFramework.LogError("005","The two instances should not be equal!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
