// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.Security;
using Xunit;

[SecuritySafeCritical]
public class Program {
    [Fact]
    public static int TestEntryPoint() {
        Console.WriteLine("Attempting delegate construction with null method pointer.");
        Console.WriteLine("Expecting: ArgumentNullException wrapped in TargetInvocationException.");
        try {
            Activator.CreateInstance(typeof(Action<object>), null, IntPtr.Zero);
            Console.WriteLine("FAIL: Creation succeeded");
            return 200;
        }
        catch (TargetInvocationException ex) {
            Console.WriteLine("Caught expected TargetInvocationException");
            if (ex.InnerException == null) {
                Console.WriteLine("No inner exception was provided");
                Console.WriteLine("FAILED");
                return 201;
            }
            else if (ex.InnerException is ArgumentNullException) {
                Console.WriteLine("Inner exception is ArgumentNullException as expected");
                Console.WriteLine("PASSED");
                return 100;
            }
            else {
                Console.WriteLine("Unexpected inner exception: {0}", ex.InnerException);
                Console.WriteLine("FAILED");
                return 202;
            }
        }
        catch (Exception ex) {
            Console.WriteLine("Caught unexpected exception: {0}", ex);
            Console.WriteLine("FAILED");
            return 203;
        }
    }
}
