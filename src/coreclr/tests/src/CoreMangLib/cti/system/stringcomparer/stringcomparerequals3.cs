// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>

///  StringComparerEquals3[v-chche]

/// </summary>
public class StringComparerEquals3
{

    #region Public Methods

    public bool RunTests()
    {

        bool retVal = true;



        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;



      


        return retVal;

    }



    #region Positive Test Cases

    public bool PosTest1()
    {

        bool retVal = true;



       

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Method Equals .");



        try
        {

            StringComparer sc = StringComparer.CurrentCulture;
            string st1 = "aa";
            string st2 = "aa";
            string st3 = "";
            string st4 = "";
            string st5 = "12a_b";
            string st6 = "12a_b";
            bool expected1 = sc.Equals(st1, st2);
            bool expected2 = sc.Equals(st3, st4);
            bool expected3 = sc.Equals(st5, st6);
            bool expected = expected1 && expected2 && expected3;
            bool actual = true;
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Verify Method Equal Value Err .");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
            
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





        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify Method Equals .");



        try
        {

            StringComparer sc = StringComparer.CurrentCulture;
            string st1 = "aa";
            string st2 = "ab_";
            string st3 = "";
            string st4 = "-";
            bool expected1 = sc.Equals(st1, st2);
            bool expected2 = sc.Equals(st3, st4);
            bool expected = expected1 && expected2;
            bool actual = false;
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("002.1", "Verify Method Equal Value Err .");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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

    #endregion




    #endregion



    public static int Main()
    {

        StringComparerEquals3 test = new StringComparerEquals3();



        TestLibrary.TestFramework.BeginTestCase("StringComparerEquals3");



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

 
