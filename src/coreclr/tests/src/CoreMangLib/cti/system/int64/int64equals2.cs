// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.Int64.Equals(Object)
/// </summary>
public class Int64Equals2
{
    private long c_INT64_MaxValue_10 = 9223372036854775807;
    private long c_INT64_MaxValue_16 = 0x7fffffffffffffff;
    public static int Main()
    {
        Int64Equals2 int64equ2 = new Int64Equals2();
        TestLibrary.TestFramework.BeginTestCase("Int64Equals2");
        if (int64equ2.RunTests())
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
        retVal = PosTest5() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify Int64 MaxValue decimal equals hexadecimal");
        try
        {
            long int64A = c_INT64_MaxValue_16;
            object objValue = c_INT64_MaxValue_10;
            if (!int64A.Equals(objValue))
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify Int64 minValue equals maxValue");
        try
        {
            long int64A = Int64.MinValue;
            object objValue = Int64.MaxValue;
            if (int64A.Equals(objValue))
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Verify Int64 maxValue string format equals maxValue");
        try
        {
            long int64A = Int64.MaxValue;
            object objValue = Int64.MaxValue.ToString();
            if (int64A.Equals(objValue))
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Verify a random Int64 equals a new object");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            object objValue = new object();
            if (int64A.Equals(objValue))
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
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Verify a random Int64 equals null");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            object objValue = null;
            if (int64A.Equals(objValue))
            {
                TestLibrary.TestFramework.LogError("009", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
