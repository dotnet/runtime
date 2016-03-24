// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.Int64.Equals(int64)
/// </summary>
public class Int64Equals1
{
    private long c_INT64_MaxValue_10 = 9223372036854775807;
    private long c_INT64_MaxValue_16 = 0x7fffffffffffffff;
    public static int Main()
    {
        Int64Equals1 int64equ1 = new Int64Equals1();
        TestLibrary.TestFramework.BeginTestCase("Int64Equals1");
        if (int64equ1.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Int64 MinValue equals to zero");

        try
        {
            long minValue = Int64.MinValue;
            long int64A = 0;
            if (int64A.Equals(minValue))
            {
                TestLibrary.TestFramework.LogError("001", "The ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify Int64 MaxValue decimal equals hexadecimal");

        try
        {
            long maxValue = c_INT64_MaxValue_10;
            long int64A = c_INT64_MaxValue_16;
            if (!int64A.Equals(maxValue))
            {
                TestLibrary.TestFramework.LogError("003", "The ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Verify Int64 +0 equals-0");

        try
        {
            long maxValue = +0;
            long int64A = -0;
            if (!int64A.Equals(maxValue))
            {
                TestLibrary.TestFramework.LogError("005", "The ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Verify Int64 minValue equals maxValue");

        try
        {
            long int64A = Int64.MinValue;
            long maxValue = Int64.MaxValue;
            if (int64A.Equals(maxValue))
            {
                TestLibrary.TestFramework.LogError("007", "The ActualResult is not the ExpectResult");
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