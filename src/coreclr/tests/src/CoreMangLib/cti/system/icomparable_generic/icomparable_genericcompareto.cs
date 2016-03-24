// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// CompareTo [v-jianq]
/// </summary>

#region Test Helper Class
public class Temperature : IComparable<Temperature>
{
    public int CompareTo(Temperature other)
    {
        return m_value.CompareTo(other.m_value);
    }

    protected double m_value = 0.0;

    public double Celsius   
    {
        get { return m_value - 273.15; }
    }

    public double Kelvin    
    {
        get { return m_value; }
        set
        {
            if (value < 0.0)
            {
                throw new ArgumentException("Temperature cannot be less than absolute zero.");
            }
            else
            {
                m_value = value;
            }
        }
    }

    public Temperature(double degreesKelvin)
    {
        this.Kelvin = degreesKelvin;
    }
}
#endregion

public class IComparable_GenericCompareTo
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify interface System.IComparable<T>.CompareTo(T)");

        try
        {
            IComparable<Temperature> temp = new Temperature(10.0d);

            int expected = 0;
            int actual = temp.CompareTo((Temperature)temp);

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method CompareTo Err .");
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
        IComparable_GenericCompareTo test = new IComparable_GenericCompareTo();

        TestLibrary.TestFramework.BeginTestCase("IComparable_GenericCompareTo");

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
