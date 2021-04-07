// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    // Defined in main.m
    [DllImport("__Internal")]
    private static extern void ios_set_text(string value);

    [DllImport("__Internal")]
    unsafe private static extern void ios_register_button_click(delegate* unmanaged<void> callback);

    [DllImport("__Internal")]
    unsafe private static extern void ios_register_applyupdate_click(delegate* unmanaged<void> callback);

    private static int counter = 0;

    // Called by native code, see main.m
    [UnmanagedCallersOnly]
    private static void OnButtonClick()
    {
        ios_set_text("OnButtonClick! #" + ChangeablePart.UpdateCounter (ref counter));
    }

    [UnmanagedCallersOnly]
    private static void OnApplyUpdateClick()
    {
        deltaHelper.Update (typeof(ChangeablePart).Assembly);
    }

    static MonoDelta.DeltaHelper deltaHelper;

    public static async Task Main(string[] args)
    {
        unsafe {
            // Register a managed callback (will be called by UIButton, see main.m)
            delegate* unmanaged<void> unmanagedPtr = &OnButtonClick;
            ios_register_button_click(unmanagedPtr);
            delegate* unmanaged<void> unmanagedPtr2 = &OnApplyUpdateClick;
            ios_register_applyupdate_click(unmanagedPtr2);
        }
        deltaHelper = MonoDelta.DeltaHelper.Make();
        const string msg = "Hello World!\n.NET 5.0";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            ios_set_text(msg.Substring(0, i + 1));
            await Task.Delay(100);
        }

        Console.WriteLine("Done!");
        await Task.Delay(-1);
    }
}
