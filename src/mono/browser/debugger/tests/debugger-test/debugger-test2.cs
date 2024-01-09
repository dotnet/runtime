// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http.Json;

public class Misc
{ //Only append content to this class as the test suite depends on line info
    public static int CreateObject(int foo, int bar)
    {
        var f = new Fancy()
        {
            Foo = foo,
            Bar = bar,
        };

        Console.WriteLine($"{f.Foo} {f.Bar}");
        return f.Foo + f.Bar;
    }
}

public class Fancy
{
    public int Foo;
    public int Bar { get; set; }
    public static void Types()
    {
        double dPI = System.Math.PI;
        float fPI = (float)System.Math.PI;

        int iMax = int.MaxValue;
        int iMin = int.MinValue;
        uint uiMax = uint.MaxValue;
        uint uiMin = uint.MinValue;

        long l = uiMax * (long)2;
        long lMax = long.MaxValue; // cannot be represented as double
        long lMin = long.MinValue; // cannot be represented as double

        sbyte sbMax = sbyte.MaxValue;
        sbyte sbMin = sbyte.MinValue;
        byte bMax = byte.MaxValue;
        byte bMin = byte.MinValue;

        short sMax = short.MaxValue;
        short sMin = short.MinValue;
        ushort usMin = ushort.MinValue;
        ushort usMax = ushort.MaxValue;

        var d = usMin + usMax;
    }
}

public class UserBreak {
    public static void BreakOnDebuggerBreakCommand()
    {
        int a = 10;
        Debugger.Break();
        a = 20;
        a = 50;
        a = 100;
    }
}

public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string Summary { get; set; }
}

public class InspectTask
{
    public static async System.Threading.Tasks.Task RunInspectTask()
    {
        WeatherForecast[] forecasts = null;
        var httpClient = new System.Net.Http.HttpClient();
        var getJsonTask = httpClient.GetFromJsonAsync<WeatherForecast[]>("http://localhost:9400/weather.json");
        try
        {
            await getJsonTask.ContinueWith(t =>
                {
                    int a = 10;
                    Console.WriteLine(a);
                    if (t.IsCompletedSuccessfully)
                        forecasts = t.Result;

                    if (t.IsFaulted)
                        throw t.Exception!;
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex}");
            return;
        }
    }
}

public class TestParent2
{
    public int k = 30;
    public int GetK => k;
}

public class TestParent : TestParent2
{
    public int j = 20;
    public int GetJ => j;
}

public class TestChild : TestParent
{
    public int i = 50;
    public int GetI => i;
    public DateTime GetD => new DateTime(2020, 7, 6, 5, 4, 3);
    public TestChild()
    {
        Console.WriteLine("Hi");
    }
    public static void TestWatchWithInheritance()
    {
        TestChild test = new TestChild();
        Debugger.Break();
    }
}
