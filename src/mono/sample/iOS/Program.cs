// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    // Defined in main.m
    [DllImport("__Internal")]
    unsafe private static extern void ios_set_text(byte* value);

    [DllImport("__Internal")]
    unsafe private static extern void ios_register_button_click(delegate* unmanaged<void> callback);

    private static int counter = 0;

    private static void SetText(string txt)
    {
        byte[] ascii = ASCIIEncoding.ASCII.GetBytes(txt);
        
        unsafe 
        {
            fixed (byte* asciiPtr = ascii)
            {
                ios_set_text(asciiPtr);
            }
        }
    }

    // Called by native code, see main.m
    [UnmanagedCallersOnly]
    private static void OnButtonClick()
    {
        SetText("OnButtonClick! #" + counter++);
    }

#if CI_TEST
    public static async Task<int> Main(string[] args)
#else
    public static async Task Main(string[] args)
#endif
    {
        unsafe {
            // Register a managed callback (will be called by UIButton, see main.m)
            delegate* unmanaged<void> unmanagedPtr = &OnButtonClick;
            ios_register_button_click(unmanagedPtr);
        }
        const string msg = "Hello World!\n.NET Runtime";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            SetText(msg.Substring(0, i + 1));
            await Task.Delay(100);
        }

        Console.WriteLine("Done!");
#if CI_TEST
        await Task.Delay(5000);
        return 42;
#else
        await Task.Delay(-1);
#endif 
    }
}
