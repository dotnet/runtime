using System;
using TestLibrary;

/// <summary>
/// Int64.ToString()
/// </summary>
public class Int64ToString1
{
    private string c_INT64_MinValue = "-9223372036854775808";
    private string c_INT64_MaxValue = "9223372036854775807";
    public static int Main()
    {
        Int64ToString1 int64ts1 = new Int64ToString1();
        TestLibrary.TestFramework.BeginTestCase("Int64ToString1");
        if (int64ts1.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[PosTest]");
        retVal = PosTest(1, Int64.MinValue, "Int64.MinValue", c_INT64_MinValue) && retVal;
        retVal = PosTest(2, Int64.MaxValue, "Int64.MaxValue", c_INT64_MaxValue) && retVal;
        retVal = PosTest(3, 0, "0", "0") && retVal;
        retVal = PosTest(4, 12345, "12345", "12345") && retVal;
        retVal = PosTest(5, -12345, "-12345", "-12345") && retVal;
        retVal = PosTest(6, -00012345, "-00012345", "-12345") && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest(int seqNumber, long value, string description,  string expectedValue)
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest " + seqNumber.ToString() + ": " + description +
        ".ToString()");
        try
        {
            string actual = value.ToString();
            expectedValue = GlobLocHelper.OSInt64ToString(value);
            if (!actual.Equals(expectedValue))
            {
                TestLibrary.TestFramework.LogError("00" + seqNumber.ToString() + ".1",
                    "Expected: " + expectedValue + ", Actual: " + actual);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("00" + seqNumber.ToString() + ".2", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}