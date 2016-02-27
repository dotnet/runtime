// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Dispose()
/// </summary>

public class EnumeratorDispose
{
    public static int Main()
    {
        EnumeratorDispose test = new EnumeratorDispose();

        TestLibrary.TestFramework.BeginTestCase("EnumeratorDispose");

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
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Resource of the Enumerator should be released after Dispose().");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            TestEnumerator.Dispose();
            string TestString = TestEnumerator.Current;
            TestLibrary.TestFramework.LogError("P01.1", "Resource of the Enumerator have not been released after Dispose()!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
