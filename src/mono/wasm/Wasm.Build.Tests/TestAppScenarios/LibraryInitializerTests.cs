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

public class LibraryInitializerTests : AppTestBase
{
    public LibraryInitializerTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public async Task LoadLibraryInitializer()
    {
        CopyTestAsset("WasmBasicTestApp", "LibraryInitializerTests");
        PublishProject("Debug");

        var testOutput = await RunSdkStyleApp(new(Configuration: "Debug", ForPublish: true, TestScenario: "LibraryInitializerTest"));
        Assert.Collection(
            testOutput,
            m => Assert.Equal("Run from LibraryInitializer", m),
            m => Assert.Equal("LIBRARY_INITIALIZER_TEST = 1", m)
        );
    }
}
