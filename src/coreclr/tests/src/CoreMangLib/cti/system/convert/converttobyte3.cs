using System;

/// <summary>
/// ToByte(System.Char)
/// </summary>
public class ConvertToByte3
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ToByte should Converts the value of the specified Unicode character to the equivalent 8-bit unsigned integer");

        try
        {
            short mask = 0x00FF;
            char c = TestLibrary.Generator.GetChar(-55);

            c = (char)(c & mask);
            byte expected = (byte)(c & mask);
            byte actual = Convert.ToByte(c);

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToByte can not Converts the value of the specified Unicode character to the equivalent 8-bit unsigned integer");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected + ", c = " + c);
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException should be thrown when value is greater than Byte.MaxValue");

        try
        {
            char c = '\x01FF';

            byte actual = Convert.ToByte(c);

            TestLibrary.TestFramework.LogError("101.1", "OverflowException is not thrown when value is greater than Byte.MaxValue");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual);
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToByte3 test = new ConvertToByte3();

        TestLibrary.TestFramework.BeginTestCase("ConvertToByte3");

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
