// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* NAME:        LargeArrayTest
 * DATE:        2004-03-02
 * DESCRIPTION: creates arrays of size Int32.MaxValue through Int32.MaxValue-100 inclusive
 */

using System;

public class LargeArrayTest {

    public static int Main(string[] args) {

        for (int i=0; i<= 100; i++) {
            try {
                Console.Write("now try Int32.MaxValue-{0}: ", i);
                Array a = Array.CreateInstance((new byte().GetType()), Int32.MaxValue-i);
                Console.WriteLine(a.Length);
                a = null;
            } catch (OutOfMemoryException e) {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            } catch (Exception e) {
                Console.WriteLine();
                Console.WriteLine("Unexpected Exception!");
                Console.WriteLine(e);
                Console.WriteLine("Test Failed!");
                return 0;
            }
        }

        Console.WriteLine("Test Passed!");
        return 100;
    }

}
