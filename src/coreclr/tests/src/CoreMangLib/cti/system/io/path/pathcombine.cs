
using System.Security;
using System;
using System.IO;
using TestLibrary;


/// <summary>
/// System.IO.Path.Combine(string,string)
/// </summary>
public class PathCombine
{

    public static int Main()
    {
        PathCombine pc = new PathCombine();
        TestLibrary.TestFramework.BeginTestCase("PathCombine");

        if (pc.RunTests())
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
        if (Utilities.IsWindows)
        {
            retVal = PosTest4() && retVal;
            retVal = PosTest5() && retVal;
        }
        retVal = PosTest6() && retVal;

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
        const string c_TEST_DESC = "PosTest1: Verify Combine  two paths . ";
        const string c_TEST_ID = "P001";

        string path1 = @"mydir" + Env.FileSeperator + "myfolder" + Env.FileSeperator + "";
        string path2 = @"youfolder" + Env.FileSeperator + "youfile";
        string resPath = @"mydir" + Env.FileSeperator + "myfolder" + Env.FileSeperator + "youfolder" + Env.FileSeperator + "youfile";
        string newPath ;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }
       

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: path1 does not end with a valid separator character. ";
        const string c_TEST_ID = "P002";

        string path1 = @"mydir" + Env.FileSeperator + "myfolder";
        string path2 = @"youfolder" + Env.FileSeperator + "youfile";
        string resPath = @"mydir" + Env.FileSeperator + "myfolder" + Env.FileSeperator + "youfolder" + Env.FileSeperator + "youfile";
        string newPath;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: path1 is empty";
        const string c_TEST_ID = "P003";

        string path1 = "";
        string path2 = @"yourfolder" + Env.FileSeperator + "" + Env.FileSeperator + "youfile";
        string resPath = @"yourfolder" + Env.FileSeperator + "" + Env.FileSeperator + "youfile";
        string newPath;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: path2 includes a drive specification";
        const string c_TEST_ID = "P004";

        string path1 = @"mydir\myfolder";
        string path2 = @"D:\yourfolder\youfile";
        string resPath = path2;
        string newPath;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: path2 have white space ";
        const string c_TEST_ID = "P005";

        string path1 = "mydir\\my folder";
        string path2 = " D:\\ ";
        string resPath = "mydir\\my folder\\ D:\\ ";
        string newPath;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6: path2 have a willcard";
        const string c_TEST_ID = "P006";

        string path1 = "mydir" + Env.FileSeperator + "my folder";
        string path2 = "*.txt";
        string resPath = "mydir" + Env.FileSeperator + "my folder" + Env.FileSeperator + "*.txt";
        string newPath;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            newPath = Path.Combine(path1, path2);
            if (newPath != resPath)
            {
                string errorDesc = "Value is not " + resPath + " as expected: Actual(" + newPath + ")";
                errorDesc += GetDataString(path1, path2);
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }


        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: path1 contains one  of the invalid characters";
        const string c_TEST_ID = "N001";

        string path1 = "mydir\\my| folder";
        string path2 = "yourfolder\\youfile";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.Combine(path1, path2);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(path1, path2));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }

        return retVal;
 
    }

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: path2 contains a wildcard";
        const string c_TEST_ID = "N002";

        string path1 = "mydir\\my folder";
        string path2 = "yourfolder\\>youfile";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.Combine(path1, path2);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(path1, path2));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3: path2 is null reference";
        const string c_TEST_ID = "N003";

        string path1 = "mydir\\my folder";
        string path2 = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.Combine(path1, path2);
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(path1, path2));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest4: path1 is null reference";
        const string c_TEST_ID = "N004";

        string path1 = null;
        string path2 = "mydir\\my folder";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Path.Combine(path1, path2);
            TestLibrary.TestFramework.LogError("019" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(path1, path2));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(path1, path2));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Helper methords for testing
    private string GetDataString(string path1, string path2)
    {
        string str, strPath1, strPath2;

        if (null == path1)
        {
            strPath1 = "null";

        }
        else if ("" == path1)
        {
            strPath1 = "empty";
        }
        else
        {
            strPath1 = path1;
        }

        if (null == path2)
        {
            strPath2 = "null";
        }
        else if ("" == path2)
        {
            strPath2 = "empty";
        }
        else
        {
            strPath2 = path2;
        }

        str = string.Format("\n[Path1 value]\n \"{0}\"", strPath1);
        str += string.Format("\n[Path2 value]\n \"{0}\"", strPath2);

        return str;
    }
    #endregion
}