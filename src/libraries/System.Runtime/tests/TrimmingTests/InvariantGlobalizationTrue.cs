// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Ensures setting InvariantGlobalization = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // since we are using Invariant GlobalizationMode = true, setting the culture doesn't matter.
        // The app will always use Invariant mode, so even in the Turkish culture, 'i' ToUpper will be "I"
        Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        if ("i".ToUpper() != "I")
        {
            // 'i' ToUpper was not "I", so fail
            return -1;
        }

        return 100;
    }
}
