// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>

///  StringComparerEquals1[v-chche]

/// </summary>
public class StringComparerEquals1
{

    #region Public Methods

    public bool RunTests()
    {

        bool retVal = true;



        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal = PosTest1() && retVal;



        


        return retVal;

    }



    #region Positive Test Cases

    public bool PosTest1()
    {

        bool retVal = true;



     

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Method Equal Value . ");



        try
        {
            StringComparer sc = StringComparer.Ordinal;
            bool expected = true;
            bool actual = sc.Equals(sc);
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





        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify Method Equal Value . ");



        try
        {
            StringComparer sc1 = StringComparer.Ordinal;
            StringComparer sc2 = StringComparer.CurrentCulture;
            bool expected = false;
            bool actual = sc1.Equals(sc2);
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

        StringComparerEquals1 test = new StringComparerEquals1();



        TestLibrary.TestFramework.BeginTestCase("StringComparerEquals1");



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

 
