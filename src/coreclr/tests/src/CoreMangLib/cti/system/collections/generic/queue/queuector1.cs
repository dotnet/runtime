using System;
using System.Collections.Generic;

/// <summary>
/// ctor()
/// </summary>

public class QueueCtor1
{
    public static int Main()
    {
        QueueCtor1 test = new QueueCtor1();

        TestLibrary.TestFramework.BeginTestCase("QueueCtor1");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether ctor() is successful.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            if (TestQueue == null || TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "ctor() failed!");
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
}
