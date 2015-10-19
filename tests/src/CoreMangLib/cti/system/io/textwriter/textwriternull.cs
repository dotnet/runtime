using System.Security;
using System;
using System.IO;

[SecuritySafeCritical]
public class TextWriterNull
{
    public static int Main()
    {
        TextWriterNull test = new TextWriterNull();

        TestLibrary.TestFramework.BeginTestCase("TextWriterNull");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: ");

        try
        {
            TextWriter.Null.Write("abc");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
