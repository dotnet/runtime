// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// CompareTo [v-jianq]
/// </summary>

#region Test Helper Class
public class Temperature : IComparable
{
    protected double m_value;

    public int CompareTo(object obj)
    {
        if (obj is Temperature)
        {
            Temperature temp = (Temperature)obj;
            return m_value.CompareTo(temp.m_value);
        }
        throw new ArgumentException("object is not a Temperature");
    }

    public double Value
    {
        get { return m_value; }
        set { m_value = value; }
    }

    public double Celsius
    {
        get { return (m_value - 32) / 1.8; }
        set { m_value = (value * 1.8) + 32; }
    }
}

#endregion

public class IComparableCompareTo
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify interface IComparable .");

        try
        {
            IComparable temp = new Temperature();

            int expected = 0;
            int actual = temp.CompareTo(temp);

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method IComparable.CompareTo Err .");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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
        IComparableCompareTo test = new IComparableCompareTo();

        TestLibrary.TestFramework.BeginTestCase("IComparableCompareTo");

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
