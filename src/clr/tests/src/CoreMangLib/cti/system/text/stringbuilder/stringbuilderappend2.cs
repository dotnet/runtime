using System;
using System.Text;
/// <summary>
/// StringBuilder.Append(Byte)
/// </summary>
public class StringBuilderAppend2
{
    public static int Main()
    {
        StringBuilderAppend2 sbAppend2 = new StringBuilderAppend2();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend2");
        if (sbAppend2.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke Append method in the initial StringBuilder 1");
        try
        {
            StringBuilder sb = new StringBuilder();
            byte bValue = TestLibrary.Generator.GetByte(-55);
            sb = sb.Append(bValue);
            if (sb.ToString() != bValue.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke Append method in the initial StringBuilder 2");
        try
        {
            string strSource = "formytest";
            StringBuilder sb = new StringBuilder(strSource);
            byte bValue = TestLibrary.Generator.GetByte(-55);
            sb = sb.Append(bValue);
            if (sb.ToString() != strSource + bValue.ToString())
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke Append method in the initial StringBuilder 3");
        try
        {
            string strSource = null;
            StringBuilder sb = new StringBuilder(strSource);
            byte bValue = TestLibrary.Generator.GetByte(-55);
            sb = sb.Append(bValue);
            if (sb.ToString() != bValue.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
