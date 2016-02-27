// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.Int64.CompareTo(Int64)
/// </summary>
public class Int64CompareTo1
{
    private long c_INT64_MaxValue_10 = 9223372036854775807;
    public static int Main()
    {
        Int64CompareTo1 int64ct1 = new Int64CompareTo1();
        TestLibrary.TestFramework.BeginTestCase("Int64CompareTo1");
        if (int64ct1.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[PosTest]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: long Value is less than the instance");

        try
        {
            long value = TestLibrary.Generator.GetInt64(-55);
            long int64A = Int64.MinValue;
            if (int64A.CompareTo(value) >= 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: long Value is larger than the instance");

        try
        {
            long value = TestLibrary.Generator.GetInt64(-55);
            long int64A = Int64.MaxValue;
            if (int64A.CompareTo(value) <= 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: long Value is equal the instance");
        try
        {
            long value = c_INT64_MaxValue_10;
            long int64A = Int64.MaxValue;
            if (int64A.CompareTo(value) != 0)
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
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
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: long Value is  not equal the instance");
        try
        {
            long value = TestLibrary.Generator.GetInt64(-55);
            long int64A1 = value + 1;
            long int64A2 = value - 1;
            if (int64A1.CompareTo(value) <= 0 || int64A2.CompareTo(value) >= 0)
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
