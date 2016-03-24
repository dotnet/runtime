// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayReverse1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayReverse1 ac = new ArrayReverse1();

        TestLibrary.TestFramework.BeginTestCase("Array.Reverse(Array)");

        if (ac.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool  retVal = true;
        Array afterArray;
        Array beforeArray;
        int   length;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.Reverse(Array)");

        try
        {
            // creat the array
            length       = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            beforeArray  = Array.CreateInstance(typeof(Int32), length);
            afterArray   = Array.CreateInstance(typeof(Int32), length);

            // fill the array
            for (int i=0; i<beforeArray.Length; i++)
            {
                beforeArray.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // copy the array
            Array.Copy(beforeArray, afterArray, length);

            Array.Reverse(afterArray);

            if (beforeArray.Length != afterArray.Length)
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected length: Expected(" + beforeArray.Length + ") Actual(" + afterArray.Length + ")");
                retVal = false;
            }

            for (int i=0; i<beforeArray.Length; i++)
            {
                if ((int)beforeArray.GetValue(length-i-1) != (int)afterArray.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(" + beforeArray.GetValue(length-i-1) + ") Actual(" + afterArray.GetValue(i) + ")");
                    retVal = false;
                }
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
        bool  retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Reverse(Array) where array is null");

        try
        {
            Array.Reverse(null);

            TestLibrary.TestFramework.LogError("003", "Exception expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
