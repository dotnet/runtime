// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>

///  StringComparerActor[v-chche]

/// </summary>
#region StringComparerCtorTest
public class StringComparerCtorTest : StringComparer
{

    public StringComparerCtorTest()
        : base() { }

    public override bool Equals(object obj)
    {
        return false;
    }

    public override bool Equals(string str1,string str2)
    {
        return false;
    }
    public override int GetHashCode(string str)
    {
        return 0;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public override int Compare(string x, string y)
    {
        return 0;
    }
}
#endregion 

public class StringComparerActor
{

    #region Public Methods

    public bool RunTests()
    {

        bool retVal = true;



        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal = PosTest1() && retVal;



        //

        // TODO: Add your negative test cases here

        //

        // TestLibrary.TestFramework.LogInformation("[Negative]");

        // retVal = NegTest1() && retVal;



        return retVal;

    }



    #region Positive Test Cases

    public bool PosTest1()
    {

        bool retVal = true;



        // Add your scenario description here

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Class StringComparer Ctor");



        try
        {

            StringComparer sc = new StringComparerCtorTest();
            if (sc == null)
            {
                TestLibrary.TestFramework.LogError("001.1", " StringComparer Ctor Err.");
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

    #endregion



    #region Nagetive Test Cases

    //public bool NegTest1()

    //{

    //    bool retVal = true;



    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");



    //    try

    //    {

    //          //

    //          // Add your test logic here

    //          //

    //    }

    //    catch (Exception e)

    //    {

    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);

    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);

    //        retVal = false;

    //    }



    //    return retVal;

    //}

    #endregion

    #endregion



    public static int Main()
    {

        StringComparerActor test = new StringComparerActor();



        TestLibrary.TestFramework.BeginTestCase("StringComparerActor");



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

 
