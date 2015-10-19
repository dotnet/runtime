using System.Security;
using System;
using System.IO;
using TestLibrary;

/// <summary>
/// System.IO.Path.GetDirectoryName(string)
/// </summary>
public class PathGetDirectoryName
{
    private const int c_MAX_PATH_LEN = 256;

    public static int Main()
    {
        PathGetDirectoryName pGetDirectoryname = new PathGetDirectoryName();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.IO.Path.GetDirectoryName(string)");

        if (pGetDirectoryname.RunTests())
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:the source path is a file name.";
        const string c_TEST_ID = "P001";

        string sourcePath = @"C:" + Env.FileSeperator + "mydir" + Env.FileSeperator + "myfolder" + Env.FileSeperator + "test.txt";
        string directoryName = @"C:" + Env.FileSeperator + "mydir" + Env.FileSeperator + "myfolder";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resDN = Path.GetDirectoryName(sourcePath);
            if (resDN != directoryName)
            {
                string errorDesc = "Value is not " + directoryName + " as expected: Actual(" + resDN + ")";
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
        const string c_TEST_DESC = "PosTest2:the source Path is  a root directory";
        const string c_TEST_ID = "P002";

        string sourcePath = @"C:\";
        string directoryName = Utilities.IsWindows?null:"C:";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resDN = Path.GetDirectoryName(sourcePath);
            if (resDN != directoryName)
            {
                string errorDesc = "Value is not " + directoryName + " as expected: Actual(" + resDN + ")";
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
        const string c_TEST_DESC = "PosTest3: source Path does not contain directory information";
        const string c_TEST_ID = "P003";

        string sourcePath = "mydiymyfolder.mydoc";
        string directoryName = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resDN = Path.GetDirectoryName(sourcePath);
            if (resDN != directoryName)
            {
                string errorDesc = "Value is not  empty string  as expected: Actual(" + resDN + ")";
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
        const string c_TEST_DESC = "PosTest4:the source Path is a null reference";
        const string c_TEST_ID = "P004";

        string sourcePath =null;
        string directoryName = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resDN = Path.GetDirectoryName(sourcePath);
            if (resDN != directoryName)
            {
                string errorDesc = "Value is not " + directoryName + " as expected: Actual(" + resDN + ")";
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
        const string c_TEST_DESC = "PosTest5:the source Path is a null reference";
        const string c_TEST_ID = "P005";

        string sourcePath = null;
        string directoryName = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resDN = Path.GetDirectoryName(sourcePath);
            if (resDN != directoryName)
            {
                string errorDesc = "Value is not null as expected: Actual(" + resDN + ")";
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

        string sourcePath = "C:\\mydir\\myfolder>\\test.txt"; 

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetDirectoryName(sourcePath);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(sourcePath));
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

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: the source path  contains only white spaces";
        const string c_TEST_ID = "N002";

        string sourcePath = " ";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetDirectoryName(sourcePath);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected" + GetDataString(sourcePath));
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

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3: the source path  contains a wildcard character";
        const string c_TEST_ID = "N003";

        string sourcePath = @"C:\<\myfolder\test.txt"; 

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetDirectoryName(sourcePath);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected" + GetDataString(sourcePath));
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

    public bool NegTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest4: the source path is longer than the system-defined maximum length ";
        const string c_TEST_ID = "N004";

        string strMaxLength = TestLibrary.Generator.GetString(-55, true, c_MAX_PATH_LEN, c_MAX_PATH_LEN);

        string sourcePath = "C:\\"+strMaxLength+"\\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetDirectoryName(sourcePath);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "PathTooLongException is not thrown as expected" + GetDataString(sourcePath));
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
