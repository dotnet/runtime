// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

// Tests that Exchange<object>(object, object) works on variety 
// of casted types:  It just casts a bunch of different types to 
// object, then makes sure Exchange works on those objects.
public class InterlockedExchange7
{
    private const int c_NUM_LOOPS = 100;
    private const int c_MIN_STRING_LEN = 5;
    private const int c_MAX_STRING_LEN = 128;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedExchange7 test = new InterlockedExchange7();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange7");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Interlocked.Exchange<object>(object&,object)");

        try
        {
            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetByte(-55),
                               (object)TestLibrary.Generator.GetByte(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte[]");
            byte[] bArr1 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            byte[] bArr2 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            TestLibrary.Generator.GetBytes(-55, bArr1);
            TestLibrary.Generator.GetBytes(-55, bArr2);
            retVal = ExchangeObjects(
                               (object)bArr1,
                               (object)bArr2
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int16");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetInt16(-55),
                               (object)TestLibrary.Generator.GetInt16(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int32");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetInt32(-55),
                               (object)TestLibrary.Generator.GetInt32(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int64");
            retVal = ExchangeObjects(
                               (object)(object)TestLibrary.Generator.GetInt64(-55),
                               (object)TestLibrary.Generator.GetInt64(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Single");
            retVal = ExchangeObjects(
                               (object)(object)TestLibrary.Generator.GetSingle(-55),
                               (object)TestLibrary.Generator.GetSingle(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Double");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetDouble(-55),
                               (object)TestLibrary.Generator.GetDouble(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == string");
            retVal = ExchangeObjects(
                               TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN),
                               (object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == char");
            retVal = ExchangeObjects(
                               TestLibrary.Generator.GetChar(-55),
                               TestLibrary.Generator.GetChar(-55)
                               ) && retVal;

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool ExchangeObjects(object location, object value)
    {
        bool   retVal = true;
        object oldLocation;
        object prevLocation;

        prevLocation = location;

        // this is the main change from InterlockedExchange2.cs
        // here we use the <T> overload where T=object
        oldLocation = Interlocked.Exchange<object>(ref location, value);

        if (!location.Equals(value))
        {
            TestLibrary.TestFramework.LogError("003", "Interlocked.Exchange() did not do the exchange correctly: Expected(" + value + ") Actual(" + location + ")");
            retVal = false;
        }
 
        if (!oldLocation.Equals(prevLocation))
        {
            TestLibrary.TestFramework.LogError("004", "Interlocked.Exchange() did not return the expected value: Expected(" + prevLocation + ") Actual(" + oldLocation + ")");
            retVal = false;
        }

        return retVal;
    }

}
