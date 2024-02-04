// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class SatelliteLoadingTests : AppTestBase
{
    public SatelliteLoadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task LoadSatelliteAssembly()
    {
        CopyTestAsset("WasmBasicTestApp", "SatelliteLoadingTests");
        BuildProject("Debug");

        var result = await RunSdkStyleApp(new(Configuration: "Debug", TestScenario: "SatelliteAssembliesTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES without satellite: hello", m),
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES with satellite: hola", m)
        );
    }
}
