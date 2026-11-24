// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    [DllImport("__Internal")]
    unsafe private static extern void invoke_external_native_api(delegate* unmanaged<void> callback);

    private static int counter = 1;

    [UnmanagedCallersOnly]
    private static void Callback()
    {
        counter = 42;
    }    
    
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);
    
    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");
        unsafe {
            delegate* unmanaged<void> unmanagedPtr = &Callback;
            invoke_external_native_api(unmanagedPtr);
        }
        Console.WriteLine("Done!");
        await Task.Delay(5000);

        return counter;
    }
}
