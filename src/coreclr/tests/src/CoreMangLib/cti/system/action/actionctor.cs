// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ActionCtor
{
    private const int c_MIN_RANGE   = 64;
    private const int c_MAX_RANGE   = 128;
    private const int c_DELTA_RANGE = 55;

    public static int Main()
    {
        ActionCtor ac = new ActionCtor();

        TestLibrary.TestFramework.BeginTestCase("ActionCtor");

        if (ac.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool      retVal = true;
        List<int> list;
        int       startValue;
        int       endValue;
        int       sum;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Action<T> used in List<int>.ForEach");

        try
        {
            startValue = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_RANGE-c_MIN_RANGE)) + c_MIN_RANGE;
            endValue   = startValue + c_DELTA_RANGE;

            list = new List<int>();

            // populate the array
            sum = 0;
            for(int i=startValue; i<endValue; i++)
            {
                list.Add(i);
                sum += i;
            }

            // iterate through the entries
            list.ForEach( delegate(int value)
                {
                    sum -= value;
                    if (startValue > value || value > endValue)
                    {
                        TestLibrary.TestFramework.LogError("001", "Incorrect value: " + value);
                        retVal = false;
                    }
                }
            );

            // decent validation that all numbers were accounted for 
            if (0 != sum)
            {
                // if sum is not zero that means that some values where not 
                //  iterated through
                TestLibrary.TestFramework.LogError("002", "It appears that not all values where accounted for.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool         retVal = true;
        List<string> list;
        int          startValue;
        int          endValue;
        int          sum;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Action<T> used in List<string>.ForEach");

        try
        {
            startValue = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_RANGE-c_MIN_RANGE)) + c_MIN_RANGE;
            endValue   = startValue + c_DELTA_RANGE;

            list = new List<string>();

            // populate the array
            sum = 0;
            for(int i=startValue; i<endValue; i++)
            {
                list.Add(Convert.ToString(i));
                sum += i;
            }

            // iterate through the entries
            list.ForEach( delegate(string sValue)
                {
                    int value = Convert.ToInt32(sValue);
                    sum -= value;
                    if (startValue > value || value > endValue)
                    {
                        TestLibrary.TestFramework.LogError("004", "Incorrect value: " + value);
                        retVal = false;
                    }
                }
            );

            // decent validation that all numbers were accounted for 
            if (0 != sum)
            {
                // if sum is not zero that means that some values where not 
                //  iterated through
                TestLibrary.TestFramework.LogError("005", "It appears that not all values where accounted for.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
