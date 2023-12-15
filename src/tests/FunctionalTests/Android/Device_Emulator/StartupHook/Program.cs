// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    public static int Main()
    {
        string appContextKey = "Test.StartupHookForFunctionalTest.DidRun";
        var data = (string) AppContext.GetData (appContextKey);

        if (data != "Yes") {
            string msg = $"Expected startup hook to set {appContextKey} to 'Yes', got '{data}'";
            Console.Error.WriteLine(msg);
            return 104;
        }
        return 42;
    }
}
