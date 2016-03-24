// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.Int64.MinValue
/// </summary>
public class Int64MinValue
{
    private long c_INT64_MinValue_10 = -9223372036854775808;
    //private long c_INT64_MinValue_16 = 0x8000000000000000;
    public static int Main()
    {
        Int64MinValue int64Min = new Int64MinValue();

        TestLibrary.TestFramework.BeginTestCase("Int64MinValue");

        if (int64Min.RunTests())
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
        //retVal = PosTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Int64.MinValue should return the Int64' minValue 1");
        try
        {
            Int64 int64A = Int64.MinValue;
            if (int64A != c_INT64_MinValue_10)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
        
    }
    //public bool PosTest2()
    //{
    //    bool retVal = true;
    //    TestLibrary.TestFramework.BeginScenario("PosTest1: Int64.MinValue should return the Int64' minValue 2");
    //    try
    //    {
    //        Int64 int64A = Int64.MinValue;
    //        if (int64A != c_INT64_MinValue_16)
    //        {
    //            TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
    //            retVal = false;
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
    //        retVal = false;
    //    }
    //    return retVal;
    //}
    #endregion
}