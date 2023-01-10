
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    public static int int Main(string[] args)
    {
        string appContextKey = "Test.StartupHookForFunctionalTest.DidRun";
        mono_ios_set_summary($"Starting functional test");

        var data = AppContext.GetData (appContextKey);

        if (data != "Yes") {
            string msg = $"Expected startup hook to set {appContextKey} to 'Yes', got {data}";
            mono_ios_set_summary(msg);
            Console.Error.WriteLine(msg);
            return 100;
        }
        return 42;
    }
}
