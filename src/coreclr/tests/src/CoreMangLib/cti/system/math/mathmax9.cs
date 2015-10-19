using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.UInt16,System.UInt16)
/// </summary>
public class MathMax9
{
    public static int Main(string[] args)
    {
        MathMax9 max9 = new MathMax9();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.UInt16,System.UInt16)...");

        if (max9.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of max function...");

        try
        {
            UInt16 var1 = (UInt16)TestLibrary.Generator.GetInt16(-55);
            UInt16 var2 = (UInt16)TestLibrary.Generator.GetInt16(-55);

            if (var1 < var2)
            {
                if (Math.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("001", "The return value should be var2!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("002","The return value should be var1!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
