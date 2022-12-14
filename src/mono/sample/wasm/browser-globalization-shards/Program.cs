// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Globalization;
using System.Text;

Console.WriteLine("Hello, Browser Shard Sample!");

public partial class Sample
{
    [JSExport]
    internal static string Greeting()
    {
        var existingCulture = new CultureInfo("en-GB");
        Console.WriteLine(existingCulture.NumberFormat.CurrencySymbol);
        try
        {
            var nonExistingCulture = new CultureInfo("pl-PL");
            Console.WriteLine(nonExistingCulture.NumberFormat.CurrencySymbol);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Culture pl-PL does not exist on this shard: {ex}");
        }
        var text = $"Hello, World! Greetings from {GetHRef()}";
        Console.WriteLine(text);
        return text;
    }

    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();
}
