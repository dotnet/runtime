// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class IntPtrZero
{

    public static int Main()
    {
        IntPtrZero testCase = new IntPtrZero();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.Zero");
        if (testCase.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(0);
            if (ip != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001", "expect new IntPtr(0) == IntPtr.Zero");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr((long)0);
            if (ip != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("002", "expect new IntPtr((long)0) == IntPtr.Zero");
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
    
    public bool NegTest1()
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(123);
            if (ip == IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001", "expect new IntPtr(123) != IntPtr.Zero");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
