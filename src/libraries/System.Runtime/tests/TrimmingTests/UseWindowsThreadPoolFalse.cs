// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Ensures setting UseWindowsThreadPool = false still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        
        return 100;
    }
}