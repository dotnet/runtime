using System;
using System.Runtime.InteropServices;
/// <summary>
/// StructLayoutAttribute.Pack [v-minch]
/// </summary>
public class StructLayoutAttributePack
{
    public static int Main()
    {
        StructLayoutAttributePack test = new StructLayoutAttributePack();
        TestLibrary.TestFramework.BeginTestCase("StructLayoutAttribute.Pack()");
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return the field Pack value in StructLayoutAttribute class 1");
        try
        {
            LayoutKind mylayoutkind = LayoutKind.Auto;
            StructLayoutAttribute myInstance = new StructLayoutAttribute(mylayoutkind);
            if (myInstance.Pack != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 0 but the ActualResult is " + myInstance.Pack.ToString());
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
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Return the field Pack value in StructLayoutAttribute class 2");
        try
        {
            LayoutKind mylayoutkind = LayoutKind.Sequential;
            StructLayoutAttribute myInstance = new StructLayoutAttribute(mylayoutkind);
            if (myInstance.Pack != 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is 0 but the ActualResult is " + myInstance.Pack.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}