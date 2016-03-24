// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

// This tests Interlocked.Add.  
// Interlocked.Add Adds two 64-bit integers and replaces the first 
// with the sum, as an atomic operation. 
// These tests make sure Additions are equivalent with normal 
// Int64 adds, which means:
// * A+B in interlocked is the same as A+B non-interlocked
// * Positive and negative overflows wrap and do not throw exceptions.
public class InterlockedAdd2
{
    private const int c_NUM_LOOPS = 100;

    public static int Main()
    {
        InterlockedAdd2 test = new InterlockedAdd2();

        TestLibrary.TestFramework.BeginTestCase("InterlockedAdd2");

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
        retVal = PosTest3() && retVal;

        return retVal;
    }

    // PosTest1 Makes sure Interlocked.Add adds Int64s 
    // correctly vs. the sequential add, and does not throw an exception.
    // This test iterates 100 times.  Each time it gets two Int64s, 
    // value and location.  It adds them to get a "manual" value, 
    // totalMan.  It then uses Interlocked.Add to add value to location.
    // The sum is returned as totalInc.  Then it checks that all three 
    // are equal.  If any time they are not equal, retVal gets set to 
    // false, and ultimately it returns false.  It should not throw an 
    // exception, doing so causes the test to fail.
    public bool PosTest1()
    {
        bool           retVal = true;
        Int64 location;
        Int64 value;
        Int64 totalInc;
        Int64 totalMan;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int64 Interlocked.Add(Int64&,Int64)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value    = TestLibrary.Generator.GetInt64(-55);
                location = TestLibrary.Generator.GetInt64(-55);
     
                totalMan  = location + value;
                totalInc  = Interlocked.Add(ref location, value);
                // At this point, we should be able to make 
                // the following assertion:
                // totalInc = totalMan = location

                retVal = CheckValues(totalMan, totalInc, location) && retVal;
                // Note that (&&) performs a logical-AND of 
                // its bool operands, but only evaluates its second 
                // operand if necessary.  first time thru, retVal (RHS) 
                // is true, as it was initialized above.  If CheckValues 
                // is true, then it checks retVal (RHS), it is also true,
                // so retVal (LHS) gets set to true.  This stays this 
                // way so long as CheckValues keeps returning true.
                // Then, if some time CheckValues returns false (0), this
                // expression does not check retVal (RHS), and instead 
                // retVal (LHS) becomes false.  Next time thru, retVal 
                // (RHS) is false even if CheckValues returns true, so 
                // retVal (both RHS and LHS) remains false for all 
                // subsequent iterations. As such, if any one of the 100 
                // comparisons fails, retVal returns false
            }
        }
        catch (Exception e)
        {
            // any exception causes test failure.
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // PosTest2 makes sure Interlocked.Add handles an 
    // overflow condition by wrapping: if the value being added to is 
    // Int64.MaxValue and the added value is 1, the result is 
    // Int64.MinValue; if added value is 2, the result is 
    // (Int64.MinValue + 1); and so on. No exception should be thrown.
    // As such, throwing an exception during this test is a failure.
    public bool PosTest2()
    {
        bool           retVal = true;
        Int64 location;
        Int64 value;
        Int64 totalInc;
        Int64 totalMan;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Cause a positive Int64 overflow");

        try
        {
            value    = Int64.MaxValue;
            location = 10;
     
            totalMan  = location + value;
            totalInc  = Interlocked.Add(ref location, value);
            // at this point, we should be able to make 
            // the following assertion: totalInc = totalMan = location
            // Note that we should also be able to make the more precise
            // assertion: 
            // totalInc = totalMan = location = 9 = (original)location-1
            // But this test does not do that

            retVal = CheckValues(totalMan, totalInc, location) && retVal;
            // Note that (&&) performs a logical-AND of 
            // its bool operands, but only evaluates its second operand 
            // if necessary. retVal (RHS) is true, as it was initialized 
            // above. if CheckValues is true, then it checks retVal 
            // (RHS), it is also true, so retVal (LHS) gets set to true.
            // If returns false (0), this expression does not check 
            // retVal (RHS), and instead retVal (LHS) becomes false.
        }
        catch (Exception e)
        {
            // any exception causes test failure.
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // PosTest3 makes sure Interlocked.Add handles a 
    // negative overflow condition by wrapping: if the value being 
    // added to is Int64.MinValue and the added value is -1, the 
    // result should be Int64.MaxValue; if added value is -2, the 
    // result should be (Int64.MaxValue - 1); and so on. No exception 
    // should be thrown. As such, throwing an exception during this 
    // test is a failure.
    public bool PosTest3()
    {
        bool           retVal = true;
        Int64 location;
        Int64 value;
        Int64 totalInc;
        Int64 totalMan;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Cause a negative Int64 overflow");

        try
        {
            value    = -10;
            location = Int64.MinValue;

            totalMan  = location + value;
            totalInc  = Interlocked.Add(ref location, value);
            // at this point, we should be able to make the
            // following assertion: totalInc = totalMan = location
            // Note that we should also be able to make the more precise
            // assertion:
            // totalInc = totalMan = location = -9 = (original)location+1
            // But this test does not do that

            retVal = CheckValues(totalMan, totalInc, location) && retVal;
        }
        catch (Exception e)
        {
            // any exception causes test failure.
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // CheckValues returns false if the three values passed are not 
    // the same.
    public bool CheckValues(Int64 totalMan, Int64 totalInc, Int64 location)
    {
        if (totalInc != totalMan || location != totalMan)
        {
            TestLibrary.TestFramework.LogError("005", "Interlocked.Add() returned wrong value. Expected(" + totalMan + ") Got(" + totalInc + ") and (" + location + ")");
            return false;
        }

        return true;
    }

}
