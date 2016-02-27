// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// ctor(System.Int32)
/// </summary>

public class QueueCtor3
{
    public static int Main()
    {
        QueueCtor3 test = new QueueCtor3();

        TestLibrary.TestFramework.BeginTestCase("QueueCtor3");

        if (test.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether ctor(Int32) is successful when passing 4096.");

        try
        {
            Queue<string> TestQueue = new Queue<string>(4096);
            if (TestQueue == null || TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "ctor(Int32) failed when passing 4096!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether ctor(Int32) is successful when passing zero value.");

        try
        {
            Queue<int> TestQueue = new Queue<int>(0);
            if (TestQueue == null || TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P02.1", "ctor(Int32) failed when passing zero value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Test whether ctor(Int32) is successful when passing an random positive Int32 value.");

        try
        {
            Int32 TestInt32;
            do
            {
                TestInt32 = TestLibrary.Generator.GetInt32(-55) % 8192;
            }
            while (TestInt32 <= 0);
            Queue<string> TestQueue = new Queue<string>(TestInt32);
            if (TestQueue == null || TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P03.1", "ctor(Int32) failed when passing an random positive Int32 value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when capacity is less than zero (random negative Int32 value).");

        try
        {
            Int32 TestInt32;
            do
            {
                TestInt32 = TestLibrary.Generator.GetInt32(-55);
            }
            while (TestInt32 == 0);
            Queue<string> TestQueue = new Queue<string>(-TestInt32);
            TestLibrary.TestFramework.LogError("N01.1", "ArgumentOutOfRangeException is not thrown when capacity is less than zero (random negative Int32 value)!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when capacity is less than zero (mininum Int32 value).");

        try
        {
            Queue<string> TestQueue = new Queue<string>(Int32.MinValue);
            TestLibrary.TestFramework.LogError("N02.1", "ArgumentOutOfRangeException is not thrown when capacity is less than zero (mininum Int32 value)!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
