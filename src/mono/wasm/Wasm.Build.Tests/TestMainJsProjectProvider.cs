// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public class TestMainJsProjectProvider(string projectDir, ITestOutputHelper testOutput)
                : ProjectProviderBase(projectDir, testOutput)
{
    // no fingerprinting
    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(RuntimeVariant runtimeType)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", false },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", false },
               { "dotnet.runtime.js", false },
               { "dotnet.runtime.js.map", false }
            };

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(RuntimeVariant runtimeType, bool isPublish)
    {
        SortedSet<string>? res = null;
        if (runtimeType is RuntimeVariant.SingleThreaded)
        {
            res = new SortedSet<string>()
            {
               "dotnet.js",
               "dotnet.native.wasm",
               "dotnet.native.js",
               "dotnet.runtime.js",
            };

            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (runtimeType is RuntimeVariant.MultiThreaded)
        {
            res = new SortedSet<string>()
            {
               "dotnet.js",
               "dotnet.native.js",
               "dotnet.native.wasm",
               "dotnet.native.worker.js",
               "dotnet.runtime.js",
            };
            if (!isPublish)
            {
                res.Add("dotnet.js.map");
                res.Add("dotnet.runtime.js.map");
                res.Add("dotnet.native.worker.js.map");
            }
        }

        return res ?? throw new ArgumentException($"Unknown runtime type: {runtimeType}");
    }
}
