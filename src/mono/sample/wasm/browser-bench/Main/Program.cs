// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace Wasm.Bench;

public partial class Program
{
    static Benchmark instance = new Benchmark();

    [JSExport]
    public static Task<string> RunBenchmark()
    {
        return instance.RunTasks();
    }

    [JSExport]
    public static void SetTasks(string taskNames)
    {
        instance.SetTasks(taskNames);
    }

    [JSExport]
    public static string GetFullJsonResults()
    {
        return instance.GetJsonResults();
    }
}

