// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>

///  StringComparerGetType[v-chche]

/// </summary>
#pragma warning disable
#region StringComparerCtorTest
public class StringComparerTest : StringComparer
{

    public StringComparerTest()
        : base() { }


    public override bool Equals(object obj)
    {
        return false;
    }

    public override bool Equals(string str1, string str2)
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

public class StringComparerGetType
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



        

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Method GetType. ");



        try
        {

            StringComparerTest sc1 = new StringComparerTest();
           
           
            if (sc1.GetType() != typeof(StringComparerTest))
            {
                TestLibrary.TestFramework.LogError("001.1", "Verify Method GetType Err.");
                
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

        StringComparerGetType test = new StringComparerGetType();



        TestLibrary.TestFramework.BeginTestCase("StringComparerGetType");



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

 
