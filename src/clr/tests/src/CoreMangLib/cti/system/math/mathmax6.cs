using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.Int64,System.Int64)
/// </summary>
public class MathMax6
{
    public static int Main(string[] args)
    {
        MathMax6 max6 = new MathMax6();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.Int64,System.Int64)...");

        if (max6.RunTests())
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
            Int64 var1 = TestLibrary.Generator.GetInt64(-55);
            Int64 var2 = TestLibrary.Generator.GetInt64(-55);

            if (var1 < var2)
            {
                if (Math.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("001", "The value should be var2!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("002","The value should be equal to var1!");
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

