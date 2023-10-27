// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class Test12224
{    
    // Regression test for EH getting stuck in an infinite loop when NullReferenceException
    // happens inside a handler of another NullReferenceException.
    static void ExecuteTest(object context)
    {
        string s = null;
        try
        {
            try
            {
                int x = s.Length;
            }
            catch (NullReferenceException)
            {
                int x = s.Length;
            }
        }
        catch (NullReferenceException)
        {

        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Thread thread = new Thread(new ParameterizedThreadStart(Test12224.ExecuteTest));
        thread.IsBackground = true;
        thread.Start(null);

        // Give the thread 30 seconds to complete (it should be immediate). If it fails
        // to complete within that timeout, it has hung.
        bool terminated = thread.Join(new TimeSpan(0, 0, 30));

        return terminated ? 100 : -1;
    }
}
