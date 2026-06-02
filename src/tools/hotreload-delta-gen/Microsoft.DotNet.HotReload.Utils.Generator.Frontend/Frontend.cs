// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Frontend;
public static class Frontend
{

    public static int Main(string[] args)
    {
        if (!ParseArgs (args, out var config))
            return 2;

        return RunWithExitStatus(config).Result;

    }

    static async Task<int> RunWithExitStatus(Microsoft.DotNet.HotReload.Utils.Generator.Config config)
    {
        try {
            await Run(config);
            return 0;
        } catch (Microsoft.DotNet.HotReload.Utils.Generator.DiffyException exn) {
            Console.Error.WriteLine ($"Error: {exn.Message}");
            if (exn.ExitStatus == 0)
                return 1; /* really shouldn't happen, but just in case */
            return exn.ExitStatus;
        }
    }
    static async Task Run (Microsoft.DotNet.HotReload.Utils.Generator.Config config)
    {
        var runner = Microsoft.DotNet.HotReload.Utils.Generator.Runner.Make (config);
        await runner.Run ();
        Console.WriteLine ("done");
    }




    private static void PrintUsage() {
        Console.WriteLine("hotreload-delta-gen.exe -msbuild:project.csproj [-p:Key=Value ...] [-live|-script:script.json [-outputSummary:results.json]]");
    }
    static bool ParseArgs (string[] args, [NotNullWhen(true)] out Microsoft.DotNet.HotReload.Utils.Generator.Config? config)
    {
        // FIXME: not all these options make sense together
        var builder = Microsoft.DotNet.HotReload.Utils.Generator.Config.Builder();

        config = null;

        for (int i = 0; i < args.Length; i++) {
            const string msbuildOptPrefix = "-msbuild:";
            const string scriptOptPrefix = "-script:";
            const string outputSummaryPrefix = "-outputSummary:";
            const string capabilitiesPrefix = "-capabilities:";
            string fn = args [i];
            if (fn.StartsWith(msbuildOptPrefix)) {
                builder.ProjectPath = fn[msbuildOptPrefix.Length..];
            } else if (fn == "-live") {
                builder.Live = true;
            } else if (fn.StartsWith("-p:")) {
                var s = fn[3..];
                if (s.IndexOf('=') is int j && j > 0 && j+1 < s.Length) {
                    var k = s[0..j];
                    var v = s[(j + 1)..];
                    // Console.WriteLine ($"got <{k}>=<{v}>");
                    builder.Properties.Add(KeyValuePair.Create(k,v));
                } else {
                    PrintUsage ();
                    Console.WriteLine("\t-p option needs a key=value pair");
                    return false;
                }
            } else if (fn.StartsWith(scriptOptPrefix)) {
                builder.ScriptPath = fn[scriptOptPrefix.Length..];
            } else if (fn.StartsWith(outputSummaryPrefix)) {
                builder.OutputSummaryPath = fn[outputSummaryPrefix.Length..];
            } else if (fn.StartsWith(capabilitiesPrefix)) {
                builder.EditAndContinueCapabilities.Add (fn[capabilitiesPrefix.Length..]);
            } else {
                PrintUsage();
                Console.WriteLine ($"\tUnexpected trailing option {fn}");
                return false;
            }
        }

        if (String.IsNullOrEmpty(builder.ProjectPath)) {
            PrintUsage();
            Console.WriteLine ("\tmsbuild project is required");
            return false;
        }

        if (!Xor(builder.Live, !String.IsNullOrEmpty(builder.ScriptPath))) {
            PrintUsage();
            Console.WriteLine("\tExactly one of -live or -script:script.json is required");
            return false;
        }

        if (builder.Live && !String.IsNullOrEmpty(builder.OutputSummaryPath)) {
            PrintUsage();
            Console.WriteLine ("-outputSummary and -live cannot be used at the same time");
        }

        config = builder.Bake();
        return true;
    }

    private static bool Xor (bool a, bool b) {
        return !(a == b);
    }

}
