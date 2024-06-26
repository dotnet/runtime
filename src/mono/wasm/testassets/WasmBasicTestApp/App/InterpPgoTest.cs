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
    internal static void TryToTier(int iterationCount)
    {
        var buffer = new int[4096];
        var random = new Random();
        for (int i = 0; i < iterationCount; i++) {
            for (int j = 0; j < buffer.Length; j++)
                buffer[j] = random.Next();
        }
        var text = $"Greetings from {GetHRef()}. I filled a buffer with random items {iterationCount} times.";
        Console.WriteLine(text);
    }
}
