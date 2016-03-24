// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.IConvertible.ToSingle(System.IFormatProvider)
/// </summary>

public class DoubleIConvertibleToSingle
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random positive Double to single");

        try
        {

            Single s = TestLibrary.Generator.GetSingle(-55);
            Double i1 = (Double)s;

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToSingle(null) != s)
            {
                TestLibrary.TestFramework.LogError("001.1", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random negtive Double to single");

        try
        {

            Single s = -TestLibrary.Generator.GetSingle(-55);
            Double i1 = (Double)s;

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToSingle(null) != s)
            {
                TestLibrary.TestFramework.LogError("002.1", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert zero to single ");

        try
        {
            Double i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToSingle(null) != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "The result is not zero as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert (double)Single.MaxValue to single.");

        try
        {
            Double i1 = (Double)Single.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToSingle(null) != Single.MaxValue)
            {
                TestLibrary.TestFramework.LogError("004.1", "The result is not expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert (double)Single.MinValue to single.");

        try
        {
            Double i1 = (Double)Single.MinValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToSingle(null) != Single.MinValue)
            {
                TestLibrary.TestFramework.LogError("005.1", "The result is not expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
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
        DoubleIConvertibleToSingle test = new DoubleIConvertibleToSingle();

        TestLibrary.TestFramework.BeginTestCase("DoubleIConvertibleToSingle");

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
