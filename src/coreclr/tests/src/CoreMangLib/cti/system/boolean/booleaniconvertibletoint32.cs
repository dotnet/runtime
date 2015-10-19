using System;

public class BooleanIConvertibleToInt32
{

    public static int Main()
    {
        BooleanIConvertibleToInt32 testCase = new BooleanIConvertibleToInt32();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToInt32");
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
            if ( (true as IConvertible).ToInt32(null) != 1 )
            {
                TestLibrary.TestFramework.LogError("001", "expect (true as IConvertible).ToInt32(null) == 1");
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
            if ( (false as IConvertible).ToInt32(null) != 0)
            {
                TestLibrary.TestFramework.LogError("002", "expect (false as IConvertible).ToInt32(null) == 0");
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
