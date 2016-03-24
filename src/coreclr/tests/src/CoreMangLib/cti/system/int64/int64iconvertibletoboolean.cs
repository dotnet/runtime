// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToBoolean(IFormatProvider)
/// </summary>
public class Int64IConvertibleToBoolean
{
    public static int Main()
    {
        Int64IConvertibleToBoolean ui64IContBool = new Int64IConvertibleToBoolean();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToBoolean");
        if (ui64IContBool.RunTests())
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The Int64 MaxValue IConvertible To Boolean");
        try
        {
            long int64A = Int64.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            bool boolA = iConvert.ToBoolean(provider);
            if (boolA!= true)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The Int64 MinValue IConvertible To Boolean");
        try
        {
            long int64A = Int64.MinValue;
            IConvertible iConvert = (IConvertible)(int64A);
            bool boolA = iConvert.ToBoolean(null);
            if (boolA != true)
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3:The random Int64 IConvertible To Boolean 1");
        try
        {
            long int64A = 0;
            IConvertible iConvert = (IConvertible)(int64A);
            bool boolA = iConvert.ToBoolean(null);
            if (boolA != false)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:The random Int64 IConvertible To Boolean 2");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            IConvertible iConvert = (IConvertible)(int64A);
            bool boolA = iConvert.ToBoolean(null);
            if (int64A == 0)
            {
                if (boolA != false)
                {
                    TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
            }
            else
            {
                if (boolA != true)
                {
                    TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest5:The random Int64 IConvertible To Boolean 3");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55) * (-1);
            IConvertible iConvert = (IConvertible)(int64A);
            bool boolA = iConvert.ToBoolean(provider);
            if (int64A == 0)
            {
                if (boolA != false)
                {
                    TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
            }
            else
            {
                if (boolA != true)
                {
                    TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                    retVal = false;
                }
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
