// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class RuntimeMethodHanldeGetHashCode
{
    public static int Main(string[] args)
    {
        RuntimeMethodHanldeGetHashCode getHashCode =
            new RuntimeMethodHanldeGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("Testing System.RuntimeMethodHandle.GetHashCode method...");

        if (getHashCode.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify different instances of RuntimeMethodGetHashCode...");

        try
        {
            RuntimeMethodHandle myHandle1 = new RuntimeMethodHandle();
            RuntimeMethodHandle myHandle2 = new RuntimeMethodHandle();

            if (myHandle1.GetHashCode() != myHandle2.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001","The two instance should have the same HashCode!");
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
        TestLibrary.TestFramework.BeginScenario("Verify cloned instance has the same hashcode of it original...");

        try
        {
            RuntimeMethodHandle myHandle1 = new RuntimeMethodHandle();
            RuntimeMethodHandle myHandle2 = myHandle1;

            if (myHandle1.GetHashCode() != myHandle2.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("003","The two instances should have the same hashcode!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            return retVal;
        }

        return retVal;
    }
}
