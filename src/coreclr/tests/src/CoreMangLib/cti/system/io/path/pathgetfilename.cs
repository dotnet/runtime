using System.Security;
using System;
using System.IO;



/// <summary>
/// System.IO.Path.GetFileName(string)
/// </summary>
public class PathGetFileName
{

    public static int Main()
    {
        PathGetFileName pGetFileName = new PathGetFileName();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.IO.Path.GetFileName(string)");

        if (pGetFileName.RunTests())
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
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:the source path is a file name.";
        const string c_TEST_ID = "P001";

        string sourcePath = @"C:\mydir\myfolder\test.txt";
        string FileName = "test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not " + FileName + " as expected: Actual(" + resFN + ")";
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
        const string c_TEST_DESC = "PosTest2:the source path is not a file name.";
        const string c_TEST_ID = "P002";

        string sourcePath = @"C:\mydir\myfolder\test";
        string FileName = "test";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not " + FileName + " as expected: Actual(" + resFN + ")";
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
        const string c_TEST_DESC = "PosTest3:the last of character of source path is a directory separator character.";
        const string c_TEST_ID = "P003";

        string sourcePath = @"C:\mydir\myfolder\";
        string FileName = string.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not " + FileName + " as expected: Actual(" + resFN + ")";
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
        const string c_TEST_DESC = "PosTest4:the last of character of source path is a volume separator character.";
        const string c_TEST_ID = "P004";

        string sourcePath = @"C:";
        string FileName = TestLibrary.Utilities.IsWindows?string.Empty:"C:";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not " + FileName + " as expected: Actual(" + resFN + ")";
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
        const string c_TEST_DESC = "PosTest5:the source path is a null reference.";
        const string c_TEST_ID = "P005";

        string sourcePath = null;
        string FileName = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not a null reference as expected: Actual(" + resFN + ")";
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

    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6:the source path is empty.";
        const string c_TEST_ID = "P006";

        string sourcePath = string.Empty;
        string FileName = string.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            string resFN = Path.GetFileName(sourcePath);
            if (resFN != FileName)
            {
                string errorDesc = "Value is not empty as expected: Actual(" + resFN + ")";
                errorDesc += GetDataString(sourcePath);
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(sourcePath));
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
            Path.GetFileName(sourcePath);
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

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: the source path  contains  a wildcard characters";
        const string c_TEST_ID = "N002";

        string sourcePath = "C:\\mydir\\<\\test.txt";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.GetFileName(sourcePath);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(sourcePath));
            retVal = false;
        }
        catch (ArgumentException)
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