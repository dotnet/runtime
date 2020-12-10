// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

// it's not part of the BCL but runtime needs it for native-to-managed callbacks in AOT
// To be replaced with NativeCallableAttribute
public class MonoPInvokeCallbackAttribute : Attribute
{
    public MonoPInvokeCallbackAttribute(Type delegateType) { }
}

public static class Program
{
    // Defined in main.m
    [DllImport("__Internal")]
    private extern static void ios_set_text(string value);

    [DllImport("__Internal")]
    private extern static void ios_register_button_click(Action action);

    private static Action buttonClickHandler = null;

    private static int counter = 0;

    // Called by native code, see main.m
    [MonoPInvokeCallback(typeof(Action))]
    private static void OnButtonClick()
    {
        ios_set_text("OnButtonClick! #" + counter++);
    }

#if CI_TEST
    public static async Task<int> Main(string[] args)
#else
    public static async Task Main(string[] args)
#endif
    {
        // Register a managed callback (will be called by UIButton, see main.m)
        // Also, keep the handler alive so GC won't collect it.
        ios_register_button_click(buttonClickHandler = OnButtonClick);

        const string msg = "Hello World!\n.NET 5.0";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            ios_set_text(msg.Substring(0, i + 1));
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
