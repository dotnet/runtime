// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class DelegateToDelegate
{
    public delegate string OuterDelegate();

    private static string InnerMethod() {
        return "PASS";
    }

    [Fact]
    public static int TestEntryPoint() {
        int retVal = 100;

        Func<string> innerDelegate = InnerMethod;

        // The initial issue where we observed a need for this test failed if the inner delegate type was used before we attempted to 
        innerDelegate();

        var del = (OuterDelegate)Delegate.CreateDelegate(typeof(OuterDelegate), innerDelegate, typeof(Func<string>).GetMethod ("Invoke"));
        if (del() != "PASS")
            retVal = 1;
        
        return retVal;
    }
}
