// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Microsoft.DotNet.XHarness.TestRunners.Xunit;

public class SimpleWasmTestRunner : WasmApplicationEntryPoint
{
    protected override string TestAssembly { get; set; } = "";
    protected override IEnumerable<string> ExcludedTraits { get; set; } = new List<string>();

    public static int Main(string[] args)
    {
        var testAssembly = args[0];
        var excludedTraits = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            var option = args[i];
            switch (option)
            {
                case "-notrait":
                    excludedTraits.Add (args[i + 1]);
                    i++;
                    break;
                default:
                    throw new ArgumentException($"Invalid argument '{option}'.");
            }
        }

        var runner = new SimpleWasmTestRunner()
        {
            TestAssembly = testAssembly,
            ExcludedTraits = excludedTraits
        };

        return runner.Run();
    }
}
