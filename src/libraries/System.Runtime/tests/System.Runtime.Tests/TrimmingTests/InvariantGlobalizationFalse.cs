// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Ensures setting InvariantGlobalization = false still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // since we are using Invariant GlobalizationMode = false, setting the culture matters.
        // The app will always use the current culture, so in the Turkish culture, 'i' ToUpper will NOT be "I"
        Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        if ("i".ToUpper() == "I")
        {
            // 'i' ToUpper was "I", but shouldn't be in the Turkish culture, so fail
            return -1;
        }

        return 100;
    }
}
