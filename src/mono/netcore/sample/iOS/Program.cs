using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    // defined in main.m
	[DllImport ("__Internal")]
	public extern static void ios_set_text ([MarshalAs (UnmanagedType.LPUTF8Str)] string value);


    public static async Task Main (string[] args)
    {
        const string msg = "Hello World!\n.NET 5.0";
        for (int i = 0; i < msg.Length; i++)
        {
            // a kind of an animation
            ios_set_text (msg.Substring (0, i + 1));
            await Task.Delay (100);
        }
    }
}