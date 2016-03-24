// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToDateTime(IFormatProvider)
/// </summary>
public class Int64IConvertibleToDateTime
{
    public static int Main()
    {
        Int64IConvertibleToDateTime ui64IContDT = new Int64IConvertibleToDateTime();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToDateTime");

        if (ui64IContDT.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.DateTimeFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The Int64 MaxValue IConvertible To DateTime");
        try
        {
            long int64A = Int64.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            DateTime dtA = iConvert.ToDateTime(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "The InvalidCastException was not thrown as expected");
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:The Int64 MinValue IConvertible To DateTime");
        try
        {
            long int64A = Int64.MinValue;
            IConvertible iConvert = (IConvertible)(int64A);
            DateTime dtA = iConvert.ToDateTime(null);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "The InvalidCastException was not thrown as expected");
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:The random Int64 IConvertible To DateTime");
        try
        {
            long int64A = TestLibrary.Generator.GetInt64(-55);
            IConvertible iConvert = (IConvertible)(int64A);
            DateTime dtA = iConvert.ToDateTime(null);
            retVal = false;
            TestLibrary.TestFramework.LogError("N005", "The InvalidCastException was not thrown as expected");
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:The Int64 zero IConvertible To DateTime");
        try
        {
            long int64A = 0;
            IConvertible iConvert = (IConvertible)(int64A);
            DateTime dtA = iConvert.ToDateTime(null);
            retVal = false;
            TestLibrary.TestFramework.LogError("N007", "The InvalidCastException was not thrown as expected");
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
