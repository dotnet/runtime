using System.Security;
using System;
using System.IO;

/// <summary>
/// System.IO.Path.HasExtension(string)
/// </summary>
public class PathHasExtension
{

    public static int Main()
    {
        PathHasExtension pHasExtension = new PathHasExtension();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.IO.Path.HasExtension(string)");

        if (pHasExtension.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:the source path has extension.";
        const string c_TEST_ID = "P001";

        string sourcePath = @"C:\mydir\myfolder\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!Path.HasExtension(sourcePath))
            {
                string errorDesc = "result is not true as expected: Actual(false)";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:the source path ends with a backslash ";
        const string c_TEST_ID = "P002";

        string sourcePath = @"mydir\myfolder\";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.HasExtension(sourcePath))
            {
                string errorDesc = "result is not false as expected: Actual(true)";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:the source path ends with  volume separator  ";
        const string c_TEST_ID = "P003";

        string sourcePath = @"D:";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.HasExtension(sourcePath))
            {
                string errorDesc = "result is not false as expected: Actual(true)";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4:the source path hasn't extension ";
        const string c_TEST_ID = "P004";

        string sourcePath = @"D:\mytest\mydoc";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.HasExtension(sourcePath))
            {
                string errorDesc = "result is not false as expected: Actual(true)";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NagativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: the source path  contains  invalid characters";
        const string c_TEST_ID = "N001";

        char[] invalidChars = Path.GetInvalidPathChars();
        System.Text.StringBuilder sourcePath = new System.Text.StringBuilder("C:\\mydir\\myfolder\\test.txt");


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        for (int i = 0; i < invalidChars.Length; i++)
        {
            sourcePath.Append(invalidChars[i]);
            try
            {
                Path.HasExtension(sourcePath.ToString());
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath.ToString()));
                retVal = false;
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath.ToString()));
                retVal = false;
            }
        }
        return retVal;

    }
    #endregion

    #region Helper methords for testing
    private string GetDataString(string path)
    {
        string str, strPath1;

        if (null == path)
        {
            strPath1 = "null";

        }
        else if ("" == path)
        {
            strPath1 = "empty";
        }
        else
        {
            strPath1 = path;
        }


        str = string.Format("\n[souce Path value]\n \"{0}\"", strPath1);
        return str;
    }
    #endregion
}