// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.IConvertible.ToInt16(System.IFormatProvider)
/// </summary>

public class DoubleIConvertibleToInt16
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Double( <= 0.5 ) to int16");

        try
        {
            Double i1;
            do
                i1 = (Double)TestLibrary.Generator.GetDouble(-55);
            while (i1 > 0.5D);

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToInt16(null) != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random Double( > 0.5 ) to int16");

        try
        {
            Double i1;
            do
                i1 = (Double)TestLibrary.Generator.GetDouble(-55);
            while (i1 <= 0.5D);

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToInt16(null) != 1)
            {
                TestLibrary.TestFramework.LogError("002.1", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert zero to int16 ");

        try
        {
            Double i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != 0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert (double)int16.MaxValue to int16 ");

        try
        {
            Double i1 = (Double)Int16.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != Int16.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert (double)int16.MinValue to int16 ");

        try
        {
            Double i1 = (Double)Int16.MinValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != Int16.MinValue)
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
        DoubleIConvertibleToInt16 test = new DoubleIConvertibleToInt16();

        TestLibrary.TestFramework.BeginTestCase("DoubleIConvertibleToInt16");

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
