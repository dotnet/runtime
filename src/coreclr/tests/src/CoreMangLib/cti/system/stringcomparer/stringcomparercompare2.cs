// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>

///  StringComparerCompare2[v-chche]

/// </summary>
public class StringComparerCompare2
{

    #region Public Methods

    public bool RunTests()
    {

        bool retVal = true;



        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
       
        return retVal;

    }



    #region Positive Test Cases

    public bool PosTest1()
    {

        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify method compare(string, string) value ");



        try
        {

            StringComparer sc = StringComparer.Ordinal;
            retVal = VerificationHelper(sc, "a", "a", 0, "001.1") && retVal;
            retVal = VerificationHelper(sc, null, "a", -1, "001.2") && retVal;
            retVal = VerificationHelper(sc, "a", null, 1, "001.3") && retVal;
            retVal = VerificationHelper(sc, "abcd", "abcd", 0, "001.4") && retVal;
            

        }

        catch (Exception e)
        {

            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);

            TestLibrary.TestFramework.LogInformation(e.StackTrace);

            retVal = false;

        }



        return retVal;

    }
    public bool PosTest2()
    {

        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify method compare(string, string) value ");



        try
        {

            StringComparer sc = StringComparer.Ordinal;
            
            if (sc.Compare("abc","ab" )<= 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Verify method compare(string, string) value Err.");

                retVal = false;
            }




        }

        catch (Exception e)
        {

            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);

            TestLibrary.TestFramework.LogInformation(e.StackTrace);

            retVal = false;

        }



        return retVal;

    }
    public bool PosTest3()
    {

        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Verify method compare(string, string) value ");



        try
        {

            StringComparer sc = StringComparer.Ordinal;
            
            if (sc.Compare("a", "abc") >= 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "Verify method compare(string, string) value Err.");

                retVal = false;
            }




        }

        catch (Exception e)
        {

            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);

            TestLibrary.TestFramework.LogInformation(e.StackTrace);

            retVal = false;

        }



        return retVal;

    }
    

    #endregion



    

    #endregion

    #region Private Methods
    private bool VerificationHelper(StringComparer sc, string x, string y, int expected, string errorno)
    {
        bool retVal = true;

       int actual = sc.Compare(x, y);

       


        if (actual != expected)
        {
            TestLibrary.TestFramework.LogError(errorno, "Compare returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE]");
            retVal = false;
        }
        else if (actual < expected)
        {

 
        }

        return retVal;
    }
    #endregion



    public static int Main()
    {

        StringComparerCompare2 test = new StringComparerCompare2();



        TestLibrary.TestFramework.BeginTestCase(" StringComparerCompare2");



        if (test.RunTests())
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

}


