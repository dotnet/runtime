// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public delegate void MyCallback();

class ForeignThreadExceptionsTest
{
    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void InvokeCallback(MyCallback callback);

    [DllImport("ForeignThreadExceptionsNative")]
    public static extern void InvokeCallbackOnNewThread(MyCallback callback);

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
    }

    public static int Main()
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