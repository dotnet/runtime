using System;
using System.IO;
using System.Reflection;

public class FileAttributesEnum
{
    public static int Main()
    {
        FileAttributesEnum ac = new FileAttributesEnum();

        TestLibrary.TestFramework.BeginTestCase("FileAttributesEnum");

        if (ac.RunTests())
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
        int  enumValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1: FileAttributesEnum");

        try
        {
            enumValue = (int)FileAttributes.Directory;

            if (16 != enumValue)
            {
                TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(16) Actual("+enumValue+")");
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

