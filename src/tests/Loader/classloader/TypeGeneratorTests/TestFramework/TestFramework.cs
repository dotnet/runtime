// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

// There are 100000s of uses of TestFramework in the IL files.
#pragma warning disable CA1050 // Declare types in namespaces

public class TestFramework
{
    public static void MethodCallTest(string actualResult, string expectedResults, string invocationString)
    {
        if (expectedResults != actualResult)
        {
            Console.WriteLine("Wrong method called when calling " + invocationString);

            Console.WriteLine(invocationString);
            Console.WriteLine("    -> EXPECTED: " + expectedResults);
            Console.WriteLine("    -> GOT:      " + actualResult);

            throw new Exception("Wrong method called");
        }
    }

    public static void MethodCallTest(string expectedResults, string constrainedCallerMethod, int count, params string[] actualResults)
    {
        string[] expectedResultsArray = expectedResults.Split(new char[] { '#' });

        if ((expectedResults == "" && count != 0) || (expectedResults != "" && count == 0) || ((expectedResultsArray.Length - 1) != count) || (count > 0 && count != actualResults.Length))
        {
            Console.WriteLine("Error in method count in constrained caller [ " + constrainedCallerMethod + " ]");

            Console.WriteLine(constrainedCallerMethod);
    
            Console.WriteLine("   # count = " + count);
            Console.WriteLine("   # expectedResultsArray.Length = " + (expectedResultsArray.Length - 1));
            for (int i = 0; i < expectedResultsArray.Length - 1; i++)
                Console.WriteLine("      # expectedResultsArray[" + i + "] = '" + expectedResultsArray[i] + "'");
    
            throw new Exception("Method count failure");
        }

        bool success = true;
        for (int i = 0; i < count; i++)
        {
            if (expectedResultsArray[i] != actualResults[i])
            {
                Console.WriteLine("Wrong method called in constrained caller " + constrainedCallerMethod);

                Console.WriteLine("    -> EXPECTED: " + expectedResultsArray[i]);
                Console.WriteLine("    -> GOT:      " + actualResults[i]);

                success = false;
            }
        }
        if (!success) throw new Exception("Wrong method called");
    }
}
#pragma warning restore CA1050
