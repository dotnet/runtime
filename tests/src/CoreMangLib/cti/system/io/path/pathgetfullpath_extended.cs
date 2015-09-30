using System;
using System.IO;
using TestLibrary;

/// <summary>
/// System.IO.Path.GetFullPath(string)
/// </summary>
public class PathGetFullPath_Extended
{
    private const int c_MAX_PATH_LEN = 256;

    public static int Main()
    {
        PathGetFullPath_Extended pGetFullPath = new PathGetFullPath_Extended();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Path.GetFullPath(string)");

        if (pGetFullPath.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        if (Utilities.IsWindows)
        {
            retVal = NegTest6() && retVal;
        }
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region NagativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: the source path  contains  invalid characters";
        const string c_TEST_ID = "N001";

        string sourcePath = "myfolder>\\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: the source path  contains  a wildcard characters";
        const string c_TEST_ID = "N002";

        string sourcePath = "mydir\\da?\\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3: the source path  is a null reference";
        const string c_TEST_ID = "N003";

        string sourcePath = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest4: the source path  is empty";
        const string c_TEST_ID = "N004";

        string sourcePath = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest5: the source path  contains only white spaces";
        const string c_TEST_ID = "N005";

        string sourcePath = " ";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest6: the source path  contains a  colon";
        const string c_TEST_ID = "N006";

        string sourcePath = @"C:\mydi:r\ ";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "NotSupportedException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;

    }

    public bool NegTest7()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest7: the source path is longer than the system-defined maximum length ";
        const string c_TEST_ID = "N007";

        string strMaxLength = TestLibrary.Generator.GetString(-55, true, c_MAX_PATH_LEN, c_MAX_PATH_LEN);
        string sourcePath = strMaxLength + "\\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFullPath(sourcePath);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "PathTooLongException is not thrown as expected ." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (PathTooLongException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
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
