// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

// This tests CompareExchange<object>.
// It just casts a bunch of different types to object,
// then makes sure CompareExchange<object> works on those objects.
public class InterlockedCompareExchange7
{
    private const int c_NUM_LOOPS = 100;
    private const int c_MIN_STRING_LEN = 5;
    private const int c_MAX_STRING_LEN = 128;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedCompareExchange7 test = new InterlockedCompareExchange7();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange7");

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
        retVal = PosTest2() && retVal;

        return retVal;
    }

    // This particular test is for when the comparands are equal and the
    // switch should take place.
    public bool PosTest1()
    {
        bool   retVal = true;
        object location;

        TestLibrary.TestFramework.BeginScenario("PosTest1: object Interlocked.CompareExchange<object>(object&,object, object) where comparand is equal");

        try
        {
            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte");
            location = (object)TestLibrary.Generator.GetByte(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetByte(-55)
                               ) && retVal;
            // Note that (&&) performs a logical-AND of 
            // its bool operands, but only evaluates its second 
            // operand if necessary.  When ExchangeObjects is first
            // called (above), retVal (RHS) 
            // is true, as it was initialized above.  If ExchangeObjects 
            // returns true, then it checks retVal (RHS), it is also true,
            // so retVal (LHS) gets set to true.  This stays this 
            // way so long as ExchangeObjects returns true in this and
            // subsequent calls.
            // If some time ExchangeObjects returns false (0), this
            // expression does not check retVal (RHS), and instead 
            // retVal (LHS) becomes false.  Next call to ExchangeObjects,
            // retVal (RHS) is false even if ExchangeObjects returns true, so 
            // retVal (both RHS and LHS) remains false for all 
            // subsequent calls to ExchangeObjects. As such, if any one of 
            // the many calls to ExchangeObjects fails, retVal returns false
    
            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Byte[]");
            byte[] bArr1 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            byte[] bArr2 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            TestLibrary.Generator.GetBytes(-55, bArr1);
            TestLibrary.Generator.GetBytes(-55, bArr2);
            location = (object)bArr1;
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)bArr2
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int16");
            location = (object)TestLibrary.Generator.GetInt16(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetInt16(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int32");
            location = (object)TestLibrary.Generator.GetInt32(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetInt32(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Int64");
            location = (object)(object)TestLibrary.Generator.GetInt64(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetInt64(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Single");
            location = (object)(object)TestLibrary.Generator.GetSingle(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetSingle(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == Double");
            location = (object)(object)TestLibrary.Generator.GetDouble(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetDouble(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == string");
            location = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == char");
            location = TestLibrary.Generator.GetChar(-55);
            retVal = ExchangeObjects(
                               true,
                               location,
                               location,
                               (object)TestLibrary.Generator.GetChar(-55)
                               ) && retVal;

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // This particular test is for when the comparands are not equal and the
    // switch should not take place.
    public bool PosTest2()
    {
        bool   retVal = true;
        object location;
        object other;

        TestLibrary.TestFramework.BeginScenario("PosTest2: object Interlocked.CompareExchange<object>(object&,object, object) where comparand are not equal");

        try
        {
            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Byte");
            location = (object)TestLibrary.Generator.GetByte(-55);
            other = (object)TestLibrary.Generator.GetByte(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((byte)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Byte[]");
            byte[] bArr1 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            byte[] bArr2 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            byte[] bArr3 = new Byte[5 + (TestLibrary.Generator.GetInt32(-55) % 1024)];
            TestLibrary.Generator.GetBytes(-55, bArr1);
            TestLibrary.Generator.GetBytes(-55, bArr2);
            TestLibrary.Generator.GetBytes(-55, bArr3);
            location = (object)bArr1;
            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)bArr2,
                               (object)bArr3
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Int16");
            location = (object)TestLibrary.Generator.GetInt16(-55);
            other = (object)TestLibrary.Generator.GetInt16(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((Int16)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Int32");
            location = (object)TestLibrary.Generator.GetInt32(-55);
            other = (object)TestLibrary.Generator.GetInt32(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((Int32)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Int64");
            location = (object)(object)TestLibrary.Generator.GetInt64(-55);
            other = (object)TestLibrary.Generator.GetInt64(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((Int64)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Single");
            location = (object)(object)TestLibrary.Generator.GetSingle(-55);
            other = (object)TestLibrary.Generator.GetSingle(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((Single)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest2: object == Double");
            location = (object)(object)TestLibrary.Generator.GetDouble(-55);
            other = (object)TestLibrary.Generator.GetDouble(-55);

            retVal = ExchangeObjects(
                               false,
                               location,
                               (object)((Double)location+1),
                               other
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == string");
            location = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            retVal = ExchangeObjects(
                               false,
                               location,
                               (string)location+TestLibrary.Generator.GetChar(-55),
                               (object)TestLibrary.Generator.GetDouble(-55)
                               ) && retVal;

            TestLibrary.TestFramework.BeginScenario("PosTest1: object == char");
            location = TestLibrary.Generator.GetChar(-55);
            object comparand;
            do
            {
               comparand = TestLibrary.Generator.GetChar(-55);
            }
            while(comparand == location);
            retVal = ExchangeObjects(
                               false,
                               location,
                               comparand,
                               (object)TestLibrary.Generator.GetChar(-55)
                               ) && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool ExchangeObjects(bool exchange, object location, object comparand, object value)
    {
        bool   retVal = true;
        object oldLocation;
        object originalLocation = location;        

        if (!exchange && comparand == location)
        {
             TestLibrary.TestFramework.LogError("003", "Comparand and location are equal unexpectedly!!!!");
             retVal = false;
        }
        if (exchange && comparand != location)
        {
             TestLibrary.TestFramework.LogError("004", "Comparand and location are not equal unexpectadly!!!!");
             retVal = false;
        }
        
        // this is the only significant difference between this test
        // and InterlockedCompareExchange7.cs - here we use the 
        // generic overload, passing it T=object.
        oldLocation = Interlocked.CompareExchange<object>(ref location, value, comparand);

    // if exchange=true, then the exchange was supposed to take place.
        // as a result, assuming value did not equal comparand initially,
        // and location did equal comparand initially, then we should 
        // expect the following:
        // oldLoc holds locations old value,oldLocation == comparand, because oldLocation equals what
        //                location equaled before the exchange, and that
        //                equaled comparand
        // location == value, because the exchange took place
     
        if (exchange)
        {
            if (!Object.ReferenceEquals(value,location))
            {
                TestLibrary.TestFramework.LogError("005", "Interlocked.CompareExchange() did not do the exchange correctly: Expected location(" + location + ") to equal value(" + value + ")");
                retVal = false;
            }
            if (!Object.ReferenceEquals(oldLocation,originalLocation))
            {
                TestLibrary.TestFramework.LogError("006", "Interlocked.CompareExchange() did not return the expected value: Expected oldLocation(" + oldLocation + ") to equal originalLocation(" + originalLocation + ")");
                retVal = false;
            }
        }
        // if exchange!=true, then the exchange was supposed to NOT take place.
        // expect the following:
        // location == originalLocation, because the exchange did not happen
        // oldLocation == originalLocation, because the exchange did not happen 
        else
        {
            if (!Object.ReferenceEquals(location,originalLocation))
            {
                TestLibrary.TestFramework.LogError("007", "Interlocked.CompareExchange() should not change the location: Expected location(" + location + ") to equal originalLocation(" + originalLocation + ")");
                retVal = false;
            }
            if (!Object.ReferenceEquals(oldLocation,originalLocation))
            {
                TestLibrary.TestFramework.LogError("008", "Interlocked.CompareExchange() did not return the expected value: Expected oldLocation(" + oldLocation + ") to equal originalLocation(" + originalLocation + ")");
                retVal = false;
            }
        }

        return retVal;
    }

}

