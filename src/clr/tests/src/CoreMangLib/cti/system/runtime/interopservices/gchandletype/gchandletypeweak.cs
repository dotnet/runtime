using System;
using System.Runtime.InteropServices;
/// <summary>
/// GCHandleType.Weak [v-minch]
/// </summary>
public class GCHandleTypeWeak
{
    public static int Main()
    {
        GCHandleTypeWeak test = new GCHandleTypeWeak();
        TestLibrary.TestFramework.BeginTestCase("GCHandleType.Weak");
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the Weak in the GCHandleType Enumerator");
        try
        {
            int myVal = (int)GCHandleType.Weak;
            if (myVal != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the Weak in the GCHandleType ExpectResult is 0 but the ActualResult is  " + myVal);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}