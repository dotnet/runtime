using System.Security;
using System;
using System.IO;
using TestLibrary;

/// <summary>
/// System.IO.Path.IsPathRoot(string)
/// </summary>
public class PathGetPathRoot
{

    public static int Main()
    {
        PathGetPathRoot pGetPathRoot = new PathGetPathRoot();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.IO.Path.GetPathRoot(string)");

        if (pGetPathRoot.RunTests())
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
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:the source path contains driver.";
        const string c_TEST_ID = "P001";

        string sourcePath = Utilities.IsWindows?@"C:\mydir\myfolder\test.txt":"/Users/Foo/AFolder/AFile.exe";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
           
            if (!Path.IsPathRooted(sourcePath))
            {
                string errorDesc = "Value is not true as expected: Actual(false)";
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
        const string c_TEST_DESC = "PosTest2:the source path is a absolute path.";
        const string c_TEST_ID = "P002";

        string sourcePath = @"\mydir\myfolder\";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!Path.IsPathRooted(sourcePath))
            {
                string errorDesc = "Value is not true as expected: Actual(false)";
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
        const string c_TEST_DESC = "PosTest3:the source path is a relative path.";
        const string c_TEST_ID = "P003";

        string sourcePath = @"mydir\myfolder\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Path.IsPathRooted(sourcePath))
            {
                string errorDesc = "Value is not true as expected: Actual(false)";
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
        const string c_TEST_DESC = "PosTest4:the source path is a null reference.";
        const string c_TEST_ID = "P003";

        string sourcePath = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Path.IsPathRooted(sourcePath))
            {
                string errorDesc = "Value is not true as expected: Actual(false)";
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

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4:the source path is empty.";
        const string c_TEST_ID = "P003";

        string sourcePath = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Path.IsPathRooted(sourcePath))
            {
                string errorDesc = "Value is not true as expected: Actual(false)";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NagativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: the source path  contains a invalid characters";
        const string c_TEST_ID = "N001";

        char[]  invalidChars = Path.GetInvalidPathChars();
        System.Text.StringBuilder sourcePath = new System.Text.StringBuilder("C:\\mydir\\myfolder\\test.txt");
        

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        for (int i = 0; i < invalidChars.Length;i++ )
        {
            sourcePath.Append(invalidChars[i]);
            try
            {
                Path.IsPathRooted(sourcePath.ToString());
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + GetDataString(sourcePath.ToString()));
                retVal = false;
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath.ToString()));
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