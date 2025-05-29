// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public delegate void MyCallback();

public class ForeignThreadExceptionsTest
{
    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void InvokeCallback(MyCallback callback);

    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void InvokeCallbackOnNewThread(MyCallback callback);

    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void InvokeCallbackAndCatchTwiceOnNewThread(MyCallback callback);

    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void ThrowException();

    public static void MethodThatThrows()
    {
        throw new Exception("This is MethodThatThrows.");
    }

    public static void RunTest()
    {
        InvokeCallback(() => {
            try
            {
                MethodThatThrows();
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception thrown in a function called by a delegate called through Reverse PInvoke.");
            }
        });

        InvokeCallbackOnNewThread(() => {
            try
            {
                throw new Exception("Exception from delegate on foreign thread!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception thrown in a delegate called through Reverse PInvoke on a foreign thread.");
            }

        });

        InvokeCallbackOnNewThread(() => {
            string s = null;
            try
            {
                int len = s.Length;
            }
            catch(Exception e)
            {
                Console.WriteLine("Caught hardware exception in a delegate called through Reverse PInvoke on a foreign thread.");
            }
        });

        if (OperatingSystem.IsWindows() && !TestLibrary.Utilities.IsNativeAot && !TestLibrary.Utilities.IsMonoRuntime)
        {
            InvokeCallbackAndCatchTwiceOnNewThread(() => {
                throw new Exception("Exception unhandled in any managed code");
            });

            int finallyCallsCount = 0;
            InvokeCallbackAndCatchTwiceOnNewThread(() => {
                try
                {
                    // Throw native exception that is not handled in any managed code
                    ThrowException();
                }
                finally
                {
                    finallyCallsCount++;
                }
            });

            Assert.Equal(2, finallyCallsCount);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            RunTest();
            return 100;
        }

        catch (Exception ex)
        {
            Console.WriteLine("Failed to catch an exception! "+ ex.ToString());
        }

        return 1;
    }
}
