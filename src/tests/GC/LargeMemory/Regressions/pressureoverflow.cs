// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* NAME: PressureOverflow
 * DATE: 2004-03-22
 */

using System;

public class PressureOverflow {

    int numTests = 0;

    // memory pressure should overflow when increased beyond ulong.MaxInt
    public bool AddTest() {
        numTests++;
        bool retVal = false;

        for (int i=0; i<3; i++) {
            try {
                GC.AddMemoryPressure(Int64.MaxValue);
                retVal = true;
            } catch (ArgumentOutOfRangeException) {
                Console.WriteLine("This test is for 64-bit only!");
                retVal = true;
                break;
            } catch (Exception e) {
                Console.WriteLine("Caught unexpected exception at {0}", i);
                Console.WriteLine(e);
                retVal = false;
                break;
            }

        }

        return retVal;

    }

    // memory pressure should underflow when decreased beyond ulong.MaxInt
    public bool RemoveTest() {
        numTests++;
        bool retVal = false;

        for (int i=0; i<3; i++) {
            try {
                GC.RemoveMemoryPressure(Int64.MaxValue);
                retVal = true;
            } catch (ArgumentOutOfRangeException) {
                Console.WriteLine("This test is for 64-bit only!");
                retVal = true;
                break;
            } catch (Exception e) {
                Console.WriteLine("Caught unexpected exception at {0}", i);
                Console.WriteLine(e);
                retVal = false;
                break;
            }
        }
        return retVal;

    }

    public bool RunTest() {

        int numPassed = 0;

        if (AddTest())
            numPassed++;
        if (RemoveTest())
            numPassed++;

        return (numPassed == numTests);
    }


    public static int Main() {

            PressureOverflow a = new PressureOverflow();

            if (a.RunTest()) {
                Console.WriteLine("Test Passed!");
                return 100;
            }
            Console.WriteLine("Test Failed!");
            return 1;


    }
}
