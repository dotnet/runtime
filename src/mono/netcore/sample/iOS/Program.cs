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
    // defined in main.m
    [DllImport ("__Internal")]
    private extern static void ios_set_text ([MarshalAs (UnmanagedType.LPUTF8Str)] string value);

    [DllImport ("__Internal")]
    private extern static void ios_register_button_click (Action action);

    private static int counter = 0;

    [MonoPInvokeCallback (typeof (Action))]
    private static async void OnButtonClick ()
    {
        ios_set_text ("OnButtonClick! #" + counter++);
    }

    public static async Task Main (string[] args)
    {
        ios_register_button_click (OnButtonClick);

        const string msg = "Hello World!\n.NET 5.0";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            ios_set_text (msg.Substring (0, i + 1));
            await Task.Delay (100);
        }

        // TODO: Implement ConsolePal.iOS.cs for TargetsiOS==true.
        // to redirect stdout to NSLog
        Console.WriteLine("Done!");
    }
}