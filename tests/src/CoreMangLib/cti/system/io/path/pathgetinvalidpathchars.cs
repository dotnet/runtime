using System.Security;
using System;
using System.IO;

/// <summary>
/// System.IO.Path.GetInvalidPathChars()
/// </summary>
public class PathGetInvalidPathChars
{

   private static char[] RealInvalidPathChars = { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31 };

    public static int Main()
    {
        PathGetInvalidPathChars pGetInvalidPathChars = new PathGetInvalidPathChars();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.IO.Path.GetInvalidPathChars()");

        if (pGetInvalidPathChars.RunTests())
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

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify all elements of  Path.GetInvalidPathChars() . ";
        const string c_TEST_ID = "P001";
        

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();
            int i = 0;
            string errorDesc ="";

            foreach (char ichar in invalidPathChars)
            {
                if ((char)RealInvalidPathChars.GetValue(i) != ichar)
                {
                    errorDesc += "Value is not " + ichar.ToString() + " as expected: Actual(" + RealInvalidPathChars.GetValue(i).ToString()+ ")\n";
                    retVal = false;
                }

                i++;
            }

            if (invalidPathChars.Length != RealInvalidPathChars.Length)
            {
                errorDesc += "the total of elements is not " + RealInvalidPathChars.Length.ToString() + " as expected: Actual(" + RealInvalidPathChars.Length.ToString() + ")";
                retVal = false;
            }

            if (errorDesc != "")
            {
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

   

}