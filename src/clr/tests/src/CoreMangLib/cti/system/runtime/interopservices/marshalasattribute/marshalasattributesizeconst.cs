using System;
using System.Runtime.InteropServices;
/// <summary>
/// MarshalAsAttribute.SizeConst [v-minch]
/// </summary>
public class MarshalAsAttributeSizeConst
{
    public static int Main()
    {
        MarshalAsAttributeSizeConst test = new MarshalAsAttributeSizeConst();
        TestLibrary.TestFramework.BeginTestCase("MarshalAsAttribute.SizeConst");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return the field SizeConst in MarshalAsAttribute class 1");
        try
        {
            short unmanagedType = Int16.MaxValue;
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(unmanagedType);
            int mySizeConst = myMarshalAsAttribute.SizeConst;
            if (mySizeConst != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 0 but the ActualResult is " + mySizeConst);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Return the field SizeConst in MarshalAsAttribute class 2");
        try
        {
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(UnmanagedType.Currency);
            int mySizeConst = myMarshalAsAttribute.SizeConst;
            if (mySizeConst != 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is 0 but the ActualResult is " + mySizeConst);
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