using System;
using System.Reflection.Emit;

/// <summary>
/// OperandType.InlineBrTarget(v-yaduoj)
/// </summary>
public class OperandTypeTest
{
    private enum MyOperandType
    {
        InlineBrTarget = 0,
        InlineField = 1,
        InlineI = 2,
        InlineI8 = 3,
        InlineMethod = 4,
        InlineNone = 5,
        [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        InlinePhi = 6,
        InlineR = 7,
        InlineSig = 9,
        InlineString = 10,
        InlineSwitch = 11,
        InlineTok = 12,
        InlineType = 13,
        InlineVar = 14,
        ShortInlineBrTarget = 15,
        ShortInlineI = 16,
        ShortInlineR = 17,
        ShortInlineVar = 18,
    }

    public static int Main()
    {
        OperandTypeTest testObj = new OperandTypeTest();

        TestLibrary.TestFramework.BeginTestCase("for Enumeration: OperandType.InlineBrTarget");
        if (testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Operand type is InlineBrTarget";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyOperandType.InlineBrTarget;
            actualValue = (int)OperandType.InlineBrTarget;
            if (actualValue != expectedValue)
            {
                errorDesc = "InlineBrTarget value of OperandType is not the value " + expectedValue +
                            "as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}