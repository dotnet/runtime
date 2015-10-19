using System;
using System.Runtime.InteropServices;
/// <summary>
/// GCHandleType.Normal [v-minch]
/// </summary>
public class GCHandleTypeNormal
{
    public static int Main()
    {
        GCHandleTypeNormal test = new GCHandleTypeNormal();
        TestLibrary.TestFramework.BeginTestCase("GCHandleType.Normal");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the Normal in the GCHandleType Enumerator");
        try
        {
            int myVal = (int)GCHandleType.Normal;
            if (myVal != 2)
            {
                TestLibrary.TestFramework.LogError("001", "the Normal in the GCHandleType ExpectResult is 2 but the ActualResult is  " + myVal);
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