using System;
using System.Runtime.InteropServices;
/// <summary>
/// GCHandleType.WeakTrackResurrection [v-minch]
/// </summary>
public class GCHandleTypeWeakTrackResurrection
{
    public static int Main()
    {
        GCHandleTypeWeakTrackResurrection test = new GCHandleTypeWeakTrackResurrection();
        TestLibrary.TestFramework.BeginTestCase("GCHandleType.WeakTrackResurrection");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the WeakTrackResurrection in the GCHandleType Enumerator");
        try
        {
            int myVal = (int)GCHandleType.WeakTrackResurrection;
            if (myVal != 1)
            {
                TestLibrary.TestFramework.LogError("001", "the WeakTrackResurrection in the GCHandleType ExpectResult is 1 but the ActualResult is  " + myVal);
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