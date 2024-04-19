using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

Console.WriteLine("Hello, Browser!");

while(true) 
{
    StopwatchSample.Render();
    await Task.Delay(1000);
}

partial class StopwatchSample
{
    private static Stopwatch stopwatch = Stopwatch.StartNew();

    public static void Render() => SetInnerText("#time", stopwatch.Elapsed.ToString(@"mm\:ss"));
    
    [JSImport("dom.setInnerText", "main.js")]
    internal static partial void SetInnerText(string selector, string content);

    [JSExport]
    internal static bool Toggle()
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
            return false;
        }
        else
        {
            stopwatch.Start();
            return true;
        }
    }

    [JSExport]
    internal static void Reset()
    {
        if (stopwatch.IsRunning)
            stopwatch.Restart();
        else
            stopwatch.Reset();

        Render();
    }

    [JSExport]
    internal static bool IsRunning() => stopwatch.IsRunning;
}
