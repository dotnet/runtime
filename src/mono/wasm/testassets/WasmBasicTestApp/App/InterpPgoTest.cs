// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class InterpPgoTest
{
    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();
    
    [JSExport]
    internal static string Greeting()
    {
        var text = $"Hello, World! Greetings from {GetHRef()}";
        Console.WriteLine(text);
        return text;
    }
}
