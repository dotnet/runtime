// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Problem: There were no SSA edges added for cloned finally blocks. A finally block is cloned if there is an EH, switch or throw within it. This implied bad codegen as described in the customer scenario below. The call to Console.WriteLine changes code movement around the call and so the issue appears to go away but the Console.WriteLine is not really related.
 *
 * Solution: Add an OPONERROR edge around the summary OPSIDEEFFECT tuples to show that they are conditional.
 * 
 * Customer Scenario: We seem to be hitting this strange bug (see repro code). If there is a try/catch statement in the finally block, the Test() method returns false, even if this should be impossible. Uncommenting the Console.WriteLine in the try block suddenly makes the test pass. Note that there is only one place where shouldFail becomes true. We tried this on an x86 machine, and the test always returns what we expected (true), we also get the correct result on VS2008, so the bug seems to be only on x64 builds.
 * 
 * */

using System;
using Xunit;

namespace Test_finallyclone
{
class ApplicationException : Exception { }

public class TestClass
{
    [Fact]
    public static int TestEntryPoint()
    {
        //this should return true;
        return Test() ? 100 : 101;
    }

    public static bool Test()
    {
        Console.WriteLine("Begin Test....");
        bool shouldFail = false;

        try
        {
            throw new ApplicationException();
        }
        catch (ApplicationException) //This is the expected behavior.
        {
            Console.WriteLine("correct exception occurred.");
        }
        catch (Exception e)
        {
            Console.WriteLine("ApplicationException  was expected, but instead got:" + e);
            shouldFail = true;
        }

        finally
        {
            Console.WriteLine("In finally");

            try
            {
                //Console.WriteLine("In finally - try block");
            }
            catch (Exception e)
            {
                Console.WriteLine("test threw an Exception in finally: " + e);
                shouldFail = true;
            }
        }

        Console.WriteLine("should fail...{0}", shouldFail);
        if (shouldFail)
        {
            Console.WriteLine("should fail...{0}", shouldFail);
            return false;
        }

        Console.WriteLine("End Test. (PASSED)");
        return true;
    }
}

}
