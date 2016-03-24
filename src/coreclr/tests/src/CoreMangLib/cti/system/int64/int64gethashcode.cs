// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Int64.GetHashCode()
/// </summary>
public class Int64GetHashCode
{
    public static int Main()
    {
        Int64GetHashCode int64ghc = new Int64GetHashCode();
        TestLibrary.TestFramework.BeginTestCase("Int64GetHashCode");
        if (int64ghc.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the Int64'minValue hash code");
        try
        {
            long  int64A = Int64.MinValue;
            Int32 hashCode = int64A.GetHashCode();
            if (hashCode != Int32.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the Int64'maxValue hash code");
        try
        {
            long int64A = Int64.MaxValue;
            Int32 hashCode = int64A.GetHashCode();
            if (hashCode != Int32.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the a random Int64 which is out of Int32.MaxValue or Int32.MinValue hash code");
        try
        {
            Int32 temp = this.GetInt32(1, Int32.MaxValue);
            long int64A = (long)(Int64.MaxValue - temp);
            long int64A2 = (long)(Int64.MinValue + temp);
            Int32 hashCode = int64A.GetHashCode();
            Int32 hashCode2 = int64A2.GetHashCode();
            if (hashCode != Int32.MinValue + temp|| hashCode2!= Int32.MinValue + temp)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the Int64 which is in the Int32.MinValue and Int32.MaxValue hash code");
        try
        {
            Int32 temp = this.GetInt32(0, Int32.MaxValue);
            long int64A = temp;
            long int64A2 = temp * (-1);
            Int32 hashCode = int64A.GetHashCode();
            Int32 hashCode2 = int64A2.GetHashCode();
            if (hashCode != temp || hashCode2 != temp - 1)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the Int64 which is equal the Int32.MinValue or Int32.MaxValue hash code");
        try
        {
            long int64A = Int32.MaxValue;
            long int64A2 = Int32.MinValue ;
            Int32 hashCode = int64A.GetHashCode();
            Int32 hashCode2 = int64A2.GetHashCode();
            if (hashCode != Int32.MaxValue || hashCode2 != Int32.MaxValue)
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
    #region HelpMethod
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
