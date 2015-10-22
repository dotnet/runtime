using System;

public class Test
{
	public static int Main()
	{
        bool retVal = true;

        try
        {
            string name = typeof(Test).Name;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        try
        {
            string name2 = typeof(Test).ToString();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        if (retVal)
        {
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.LogVerbose("FAIL");
            return 101;
        }
	}
}
