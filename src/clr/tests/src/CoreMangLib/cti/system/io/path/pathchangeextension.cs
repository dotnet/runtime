
using System.Security;
using System;
using System.IO;


/// <summary>
/// System.IO.Path.ChangeExtension(string,string)
/// </summary>
public class PathChangeExtension
{
    

    public static int Main()
    {
        PathChangeExtension changeExtension = new PathChangeExtension();
        TestLibrary.TestFramework.BeginTestCase("PathChangeExtension");

        if (changeExtension.RunTests())
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
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
	retVal = PosTest11() && retVal;

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
        string newExtension =".doc";


        try
        {
            string newPath = "";
            string changedPath = @"C:\mydir\myfolder\test.doc";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("001", GetDataString(oldPath, newExtension, newPath));
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: the path doesn't contain extension ");

        string oldPath = @"C:\mydir\myfolder\test";
        string newExtension = ".doc";

        try
        {
            string newPath ;
            string changedPath = @"C:\mydir\myfolder\test.doc";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("003", GetDataString(oldPath, newExtension, newPath));
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: the path is a null reference ");

        string oldPath = null;
        string newExtension = ".doc";

        try
        {
            string newPath ;
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != null)
            {
                TestLibrary.TestFramework.LogError("005", GetDataString(oldPath, newExtension, newPath));
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: the path is empty ");

        string oldPath = "";
        string newExtension = ".doc";

        try
        {
            string newPath ;
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != "")
            {
                TestLibrary.TestFramework.LogError("007", GetDataString(oldPath,newExtension,newPath));
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: the  path contains multiple periods ");

        string oldPath = @"C:\mydir\myfolder\test.txt.cs";
        string newExtension = ".doc";


        try
        {
            string newPath ;
            string changedPath = @"C:\mydir\myfolder\test.txt.doc";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("009", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: the  Extension is null ");

        string oldPath = @"C:\mydir\myfolder\test.txt";
        string newExtension = null;


        try
        {
            string newPath;
            string changedPath = @"C:\mydir\myfolder\test"; 
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("011", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: the  Extension is empty ");

        string oldPath = @"C:\mydir\myfolder\test.txt";
        string newExtension = "";


        try
        {
            string newPath;
            string changedPath = @"C:\mydir\myfolder\test.";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("013", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: the  Extension doesn't contain period ");

        string oldPath = @"C:\mydir\myfolder\test.txt";
        string newExtension = "doc";


        try
        {
            string newPath;
            string changedPath = @"C:\mydir\myfolder\test.doc";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("015", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: the  extension and the path don't contain period ");

        string oldPath = @"C:\mydir\myfolder\test";
        string newExtension = "doc";


        try
        {
            string newPath;
            string changedPath = @"C:\mydir\myfolder\test.doc";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("017", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: the  extension contains multiple period ");

        string oldPath = @"C:\mydir\myfolder\test.txt";
        string newExtension = ".doc.cs";


        try
        {
            string newPath;
            string changedPath = @"C:\mydir\myfolder\test.doc.cs";
            newPath = Path.ChangeExtension(oldPath, newExtension);
            if (newPath != changedPath)
            {
                TestLibrary.TestFramework.LogError("019", GetDataString(oldPath, newExtension, newPath));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

   public bool PosTest11()
    {
        bool retVal = true;
        string oldPath = @"C:\*\myfolder\test.txt";
        string newExtension = ".doc";
        string newPath = "";

        TestLibrary.TestFramework.BeginScenario("PosTest11: the  path contains contains a wildcard character");
        try
        {
            newPath = Path.ChangeExtension(oldPath, newExtension);
    	    if (! newPath.Equals(@"C:\*\myfolder\test.doc"))
		{
		 TestLibrary.TestFramework.LogError("024", "Unexpected output: "+newPath+", expected C:\\*\\myfolder\\test.doc");
	         retVal = false;
		}
        }
         catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception: " + e + GetDataString(oldPath, newExtension, newPath));
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;

        string oldPath = @"C:\mydir\myfolder\test.txt" + Path.GetInvalidPathChars()[0].ToString();
        string newExtension = ".doc";
        string newPath = "";

        TestLibrary.TestFramework.BeginScenario("NegTest1: the  path contains contains one  of the invalid characters");
        try
        {
            newPath = Path.ChangeExtension(oldPath, newExtension);
            TestLibrary.TestFramework.LogError("021", "ArgumentException is not thrown as expected." + GetDataString(oldPath, newExtension, newPath));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e + GetDataString(oldPath, newExtension, newPath));
            retVal = false;
        }

        return retVal;
    }

 
    #endregion

    #region Helper methords for testing
    private string GetDataString(string oldPath, string Extension, string newPath)
    {
        string str, strOldPath, strExtension, strNewPath;

        if (null == oldPath)
        {
            strOldPath = "null";
           
        }
        else if("" == oldPath)
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


        if (null == newPath)
        {
            strNewPath = "null";
        }
        else if ("" == newPath)
        {
            strNewPath = "empty";
        }
        else 
        {
            strNewPath = newPath;
        }

        str = string.Format("\n[Source Path value]\n \"{0}\"", strOldPath);
        str += string.Format("\n[new Extension]\n{0}", strExtension);
        str += string.Format("\n[new Path value]\n{0}", strNewPath);

        return str;
    }
    #endregion
}