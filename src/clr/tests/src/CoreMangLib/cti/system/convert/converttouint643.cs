using System;
/// <summary>
/// Convert.ToUInt64(Char)
/// </summary>
public class ConvertToUInt643
{
    public static int Main()
    {
        ConvertToUInt643 convertToUInt643 = new ConvertToUInt643();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt643");
        if (convertToUInt643.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt64 from Char 1");
        try
        {
            char charVal = char.MaxValue;
            ulong ulongVal = Convert.ToUInt64(charVal);
            if (ulongVal != (ulong)(charVal))
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt64 from Char 2");
        try
        {
            char charVal = char.MinValue;
            ulong ulongVal = Convert.ToUInt64(charVal);
            if (ulongVal != (ulong)(charVal))
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert to UInt64 from Char 3");
        try
        {
            char charVal = TestLibrary.Generator.GetChar(-55);
            ulong ulongVal = Convert.ToUInt64(charVal);
            if (ulongVal != (ulong)(charVal))
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
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
