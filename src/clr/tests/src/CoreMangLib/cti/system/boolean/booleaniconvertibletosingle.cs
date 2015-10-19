using System;

/// <summary>
/// System.Boolean.IConvertible.ToSingle
/// </summary>
public class BooleanIConvertibleToSingle
{

    public static int Main()
    {
        BooleanIConvertibleToSingle testCase = new BooleanIConvertibleToSingle();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToSingle");
        if (testCase.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            if ((true as IConvertible).ToSingle(null) != 1.0f)
            {
                TestLibrary.TestFramework.LogError("001", "expect (true as IConvertible).ToSingle(null) == 1.0f");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            if ((false as IConvertible).ToSingle(null) != 0.0f)
            {
                TestLibrary.TestFramework.LogError("002", "expect (false as IConvertible).ToSingle(null) == 0.0f");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
