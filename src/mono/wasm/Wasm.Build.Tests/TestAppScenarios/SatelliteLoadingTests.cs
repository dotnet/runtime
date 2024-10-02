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
using System.Xml.Linq;

#nullable enable

namespace Wasm.Build.Tests.TestAppScenarios;

public class SatelliteLoadingTests : AppTestBase
{
    public SatelliteLoadingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory, TestCategory("no-fingerprinting")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadSatelliteAssembly(bool loadAllSatelliteResources)
    {
        CopyTestAsset("WasmBasicTestApp", "SatelliteLoadingTests", "App");
        BuildProject("Debug");

        var result = await RunSdkStyleAppForBuild(new(
            Configuration: "Debug", 
            TestScenario: "SatelliteAssembliesTest",
            BrowserQueryString: new Dictionary<string, string> { ["loadAllSatelliteResources"] = loadAllSatelliteResources.ToString().ToLowerInvariant() }
        ));

        var expectedOutput = new List<Action<string>>();
        if (!loadAllSatelliteResources)
        {
            // If we are loading all satellite, we don't have a way to test resources without satellite assemblies being loaded.
            // So there messages are should be present only when we are lazily loading satellites.
            expectedOutput.Add(m => Assert.Equal("default: hello", m));
            expectedOutput.Add(m => Assert.Equal("es-ES without satellite: hello", m));
        }

        expectedOutput.Add(m => Assert.Equal("default: hello", m));
        expectedOutput.Add(m => Assert.Equal("es-ES with satellite: hola", m));

        Assert.Collection(
            result.TestOutput,
            expectedOutput.ToArray()
        );
    }

    [Fact]
    public async Task LoadSatelliteAssemblyFromReference()
    {
        CopyTestAsset("WasmBasicTestApp", "SatelliteLoadingTestsFromReference", "App");

        // Replace ProjectReference with Reference
        var appCsprojPath = Path.Combine(_projectDir!, "WasmBasicTestApp.csproj");
        var appCsproj = XDocument.Load(appCsprojPath);

        var projectReference = appCsproj.Descendants("ProjectReference").Where(pr => pr.Attribute("Include")?.Value?.Contains("ResourceLibrary") ?? false).Single();
        var itemGroup = projectReference.Parent!;
        projectReference.Remove();

        var reference = new XElement("Reference");
        reference.SetAttributeValue("Include", "..\\ResourceLibrary\\bin\\Release\\net9.0\\ResourceLibrary.dll");
        itemGroup.Add(reference);

        appCsproj.Save(appCsprojPath);

        // Build the library
        var libraryCsprojPath = Path.GetFullPath(Path.Combine(_projectDir!, "..", "ResourceLibrary"));
        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput);
        CommandResult res = cmd.WithWorkingDirectory(libraryCsprojPath)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("build -c Release")
            .EnsureSuccessful();

        // Publish the app and assert
        PublishProject("Release");

        var result = await RunSdkStyleAppForPublish(new(Configuration: "Release", TestScenario: "SatelliteAssembliesTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES without satellite: hello", m),
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES with satellite: hola", m)
        );
    }
}
