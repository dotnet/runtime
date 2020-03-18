using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class MonoPInvokeCallbackAttribute : Attribute
{
    public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public static class Program
{
    // Defined in main.m
    [DllImport ("__Internal")]
    extern static void ios_set_text ([MarshalAs (UnmanagedType.LPUTF8Str)] string value);

    [DllImport ("__Internal")]
    extern static void ios_register_button_click (Action action);
    static Action buttonClickHandler = null;

    static int counter = 0;

    // Called by native code, see main.m
    [MonoPInvokeCallback (typeof (Action))]
    static async void OnButtonClick ()
    {
        ios_set_text ("OnButtonClick! #" + counter++);
    }

    public static async Task Main (string[] args)
    {
        // Register a managed callback (will be called by UIButton, see main.m)
        // Also, keep the handler alive so GC won't collect it.
        ios_register_button_click (buttonClickHandler = OnButtonClick);

        const string msg = "Hello World!\n.NET 5.0";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            ios_set_text (msg.Substring (0, i + 1));
            await Task.Delay (100);
        }

        // TODO: Implement ConsolePal.iOS.cs for TargetsiOS==true.
        // to redirect stdout to NSLog
        Console.WriteLine ("Done!");
    }
}