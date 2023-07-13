// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmSdkBasedProjectProvider(string projectDir, ITestOutputHelper _testOutput)
                : ProjectProviderBase(projectDir, _testOutput)
{
    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(RuntimeVariant runtimeType)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", true },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", true },
               { "dotnet.runtime.js", true },
               { "dotnet.runtime.js.map", false }
            };

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(RuntimeVariant runtimeType, bool isPublish)
    {
        SortedSet<string> res = new()
        {
           "dotnet.js",
           "dotnet.native.wasm",
           "dotnet.native.js",
           "dotnet.runtime.js",
        };
        if (runtimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.native.worker.js");
        }

        if (!isPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        return res;
    }
}
