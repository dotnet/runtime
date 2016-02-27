// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// GetHashCode
/// </summary>
public class SingleGetHashCode
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: check a random Single has a correct hashcode.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            int expectValue=GetExpectValue(i1);
            int actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "GetHashCode return an error value. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: check 0 and -0  has a correct hashcode.");

        try
        {
            Single i1 = (Single)0;
            int expectValue = GetExpectValue(i1);
            int actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "GetHashCode return an error value. ");
                retVal = false;
            }
            i1 = (Single)(-0);
            expectValue = GetExpectValue(i1);
            actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "GetHashCode return an error value. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.3", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: check value  which is  not a number   has a correct hashcode.");

        try
        {
            Single i1 = Single.NaN;
            int expectValue = GetExpectValue(i1);
            int actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.1", "GetHashCode return an error value. ");
                retVal = false;
            }
            i1 = Single.NegativeInfinity;
            expectValue = GetExpectValue(i1);
            actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.2", "GetHashCode return an error value. ");
                retVal = false;
            }
            i1 = Single.PositiveInfinity;
            expectValue = GetExpectValue(i1);
            actualValue = i1.GetHashCode();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.3", "GetHashCode return an error value. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.4", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        SingleGetHashCode test = new SingleGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("SingleGetHashCode");

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
    #region private method
    public unsafe  int GetExpectValue(float myValue )
    {
        int v = BitConverter.ToInt32(BitConverter.GetBytes(myValue), 0);
        return v;
    }
    #endregion
}
