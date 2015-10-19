using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// Int64.ToString(IFormatProvider provider)
/// </summary>
public class Int64ToString2
{
    private string c_INT64_MinValue = "-9223372036854775808";
    private string c_INT64_MaxValue = "9223372036854775807";
    public static int Main()
    {
        Int64ToString2 int64ts2 = new Int64ToString2();
        TestLibrary.TestFramework.BeginTestCase("Int64ToString2");
        if (int64ts2.RunTests())
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
        retVal = PosTest(1, Int64.MinValue, "Int64.MinValue", new CultureInfo("en-us"), 
            "en-US NumberFormat", c_INT64_MinValue) && retVal;
        retVal = PosTest(2, Int64.MaxValue, "Int64.MaxValue", new CultureInfo("el-GR"), 
            "el-GR NumberFormat", c_INT64_MaxValue) && retVal;
        retVal = PosTest(3, -0, "-0", new CultureInfo("en-us"),
            "en-US NumberFormat", "0") && retVal;
        retVal = PosTest(4, -00012345, "-00012345", new CultureInfo("el-GR"), 
            "el-GR NumberFormat", "-12345") && retVal;
        retVal = PosTest(5, -00012345, "-00012345", new CultureInfo("en-us"), 
            "en-US NumberFormat", "-12345") && retVal;
        retVal = PosTest(6, 0xabcdef, "0xabcdef", new CultureInfo("el-GR"), 
            "el-GR NumberFormat", "11259375") && retVal;
        retVal = PosTest(7, 00065536, "00065536", null,
            "null", 00065536.ToString(CultureInfo.CurrentCulture)) && retVal;
        return retVal;
   
    } 
    #region PositiveTest
    public bool PosTest(int seqNumber, long value, string description, CultureInfo provider, string providerDesc, string expectedValue)
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest " + seqNumber.ToString() + ": " + description +
        ".ToString('" + providerDesc        + "')");
        try
        {
            string actual = value.ToString(provider);
            expectedValue = GlobLocHelper.OSInt64ToString(value, provider);
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
    #region NegativeTest

    #endregion
}
