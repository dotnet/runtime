
using System.Security;
using System;
using System.IO;

/// <summary>
/// System.IO.Path.GetExtension(string)
/// </summary>
public class PathGetDirectoryName
{

    public static int Main()
    {
        PathGetDirectoryName pGetDirectoryname = new PathGetDirectoryName();
        TestLibrary.TestFramework.BeginTestCase("PathGetDirectoryName");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: the path contains extension");

        string oldPath = @"C:\mydir\myfolder\test.txt";
        string strExpectedExtension = ".txt";


        try
        {
            string strExtension = "";
            strExtension = Path.GetExtension(oldPath);
            if (strExpectedExtension != strExtension)
            {
                TestLibrary.TestFramework.LogError("001", GetDataString(oldPath, strExtension));
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: the path doesn't contain extension");

        string oldPath = @"C:\mydir\myfolder\";
        string strExpectedExtension = "";


        try
        {
            string strExtension = "";
            strExtension = Path.GetExtension(oldPath);
            if (strExpectedExtension != strExtension)
            {
                TestLibrary.TestFramework.LogError("003", GetDataString(oldPath, strExtension));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: the path is a null reference");

        string oldPath = null;
        string strExtension = null;


        try
        {
            strExtension = Path.GetExtension(oldPath);
            if (strExtension != null)
            {
                TestLibrary.TestFramework.LogError("005", GetDataString(oldPath, strExtension));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: the path is empty");

        string oldPath = "";
        string strExpectedExtension = "";


        try
        {
            string strExtension = "";
            strExtension = Path.GetExtension(oldPath);
            if (strExpectedExtension != strExtension)
            {
                TestLibrary.TestFramework.LogError("007", GetDataString(oldPath, strExtension));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;

        string oldPath = @"C:\mydir\myfolder>\test.txt";
        string strExtension = "";

        TestLibrary.TestFramework.BeginScenario("NegTest1: the  path contains contains one  of the invalid characters");
        try
        {
            strExtension = Path.GetExtension(oldPath);

            TestLibrary.TestFramework.LogError("009", "ArgumentException is not thrown as expected." + GetDataString(oldPath, strExtension));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e + GetDataString(oldPath, strExtension));
            retVal = false;
        }

        return retVal;
    }

  
    #endregion


    #region Helper methords for testing
    private string GetDataString(string oldPath, string Extension)
    {
        string str, strOldPath, strExtension;

        if (null == oldPath)
        {
            strOldPath = "null";

        }
        else if ("" == oldPath)
        {
            strOldPath = "empty";
        }
        else
        {
            strOldPath = oldPath;
        }

        if (null == Extension)
        {
            strExtension = "null";
        }
        else if ("" == Extension)
        {
            strExtension = "empty";
        }
        else
        {
            strExtension = Extension;
        }

        str = string.Format("\n[Source Path value]\n \"{0}\"", strOldPath);
        str += string.Format("\n[new Extension]\n{0}", strExtension);

        return str;
    }
    #endregion
}