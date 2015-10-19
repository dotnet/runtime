using System;
using System.Threading;

public class TestWaitHandle : WaitHandle
{
    public TestWaitHandle()
        : base()
    {
    }
}

/// <summary>
/// Ctor
/// </summary>
public class WaitHandleCtor
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Ctor to construct a new instance of WaitHandle");

        try
        {
            TestWaitHandle handle = new TestWaitHandle();

            if (handle == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling Ctor to construct a new instance of WaitHandle returns a null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WaitHandleCtor test = new WaitHandleCtor();

        TestLibrary.TestFramework.BeginTestCase("WaitHandleCtor");

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
}
