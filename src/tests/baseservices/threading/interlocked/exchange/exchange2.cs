// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

// Tests that Exchange(object, object) works on variety 
// of casted types:  It just casts a bunch of different types to 
// object, then makes sure Exchange works on those objects.
public class InterlockedExchange2
{
    private const int c_NUM_LOOPS = 100;
    private const int c_MIN_STRING_LEN = 5;
    private const int c_MAX_STRING_LEN = 128;

    public static int Main()
    {
        InterlockedExchange2 test = new InterlockedExchange2();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange2");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: object Interlocked.Exchange(objct&,object)");

        try
        {
            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetByte(),
                               (object)TestLibrary.Generator.GetByte()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte[]");
            byte[] bArr1 = new Byte[5 + (TestLibrary.Generator.GetInt32() % 1024)];
            byte[] bArr2 = new Byte[5 + (TestLibrary.Generator.GetInt32() % 1024)];
            TestLibrary.Generator.GetBytes(bArr1);
            TestLibrary.Generator.GetBytes(bArr2);
            retVal = ExchangeObjects(
                               (object)bArr1,
                               (object)bArr2
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int16");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetInt16(),
                               (object)TestLibrary.Generator.GetInt16()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int32");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetInt32(),
                               (object)TestLibrary.Generator.GetInt32()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int64");
            retVal = ExchangeObjects(
                               (object)(object)TestLibrary.Generator.GetInt64(),
                               (object)TestLibrary.Generator.GetInt64()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Single");
            retVal = ExchangeObjects(
                               (object)(object)TestLibrary.Generator.GetSingle(),
                               (object)TestLibrary.Generator.GetSingle()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Double");
            retVal = ExchangeObjects(
                               (object)TestLibrary.Generator.GetDouble(),
                               (object)TestLibrary.Generator.GetDouble()
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == string");
            retVal = ExchangeObjects(
                               TestLibrary.Generator.GetString(false, c_MIN_STRING_LEN, c_MAX_STRING_LEN),
                               (object)TestLibrary.Generator.GetString(false, c_MIN_STRING_LEN, c_MAX_STRING_LEN)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == char");
            retVal = ExchangeObjects(
                               TestLibrary.Generator.GetChar(),
                               TestLibrary.Generator.GetChar()
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
        // here we use the object overload
        oldLocation = Interlocked.Exchange(ref location, value);

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
