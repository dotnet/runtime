// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

namespace Wasm.Build.Tests;

public class SatelliteLoadingTests : WasmTemplateTestsBase
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
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "SatelliteLoadingTests");
        BuildProject(info, config);

        var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(
            Configuration: config,
            TestScenario: "SatelliteAssembliesTest",
            BrowserQueryString: new NameValueCollection { {"loadAllSatelliteResources", loadAllSatelliteResources.ToString().ToLowerInvariant() } }
        ));

        var expectedOutput = new List<Action<string>>();
        if (!loadAllSatelliteResources)
        {
            // If we are loading all satellite, we don't have a way to test resources without satellite assemblies being loaded.
            // So there messages are should be present only when we are lazily loading satellites.
            expectedOutput.Add(m => Assert.Equal("default: hello", m));
            expectedOutput.Add(m => Assert.Equal("es-ES without satellite: hello", m));
            expectedOutput.Add(m => Assert.Equal("fr-FR without satellite: hello", m));
        }

        expectedOutput.Add(m => Assert.Equal("default: hello", m));
        expectedOutput.Add(m => Assert.Equal("es-ES with satellite: hola", m));
        expectedOutput.Add(m => Assert.Equal("fr-FR with satellite: bonjour", m));

        Assert.Collection(
            result.TestOutput,
            expectedOutput.ToArray()
        );
    }

    [Fact, TestCategory("bundler-friendly")]
    public async Task LoadSatelliteAssemblyFromReference()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "SatelliteLoadingTestsFromReference");

        // Replace ProjectReference with Reference
        var appCsprojPath = Path.Combine(_projectDir, "WasmBasicTestApp.csproj");
        var appCsproj = XDocument.Load(appCsprojPath);

        var projectReference = appCsproj.Descendants("ProjectReference").Single(pr => pr.Attribute("Include")?.Value?.Contains("ResourceLibrary") ?? false);
        var itemGroup = projectReference.Parent!;
        projectReference.Remove();

        var reference = new XElement("Reference");
        reference.SetAttributeValue("Include", $"..\\ResourceLibrary\\bin\\Release\\{DefaultTargetFramework}\\ResourceLibrary.dll");
        itemGroup.Add(reference);

        appCsproj.Save(appCsprojPath);

        // Build the library
        var libraryCsprojPath = Path.GetFullPath(Path.Combine(_projectDir, "..", "ResourceLibrary"));
        using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput);
        CommandResult res = cmd.WithWorkingDirectory(libraryCsprojPath)
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("build -c Release")
            .EnsureSuccessful();

        // Publish the app and assert
        PublishProject(info, config);

        var result = await RunForPublishWithWebServer(new BrowserRunOptions(Configuration: Configuration.Release, TestScenario: "SatelliteAssembliesTest"));
        Assert.Collection(
            result.TestOutput,
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES without satellite: hello", m),
            m => Assert.Equal("fr-FR without satellite: hello", m),
            m => Assert.Equal("default: hello", m),
            m => Assert.Equal("es-ES with satellite: hola", m),
            m => Assert.Equal("fr-FR with satellite: bonjour", m)
        );
    }

    [Fact, TestCategory("no-fingerprinting")]
    public void SatelliteAssembliesFromPackageReference()
    {
        Configuration config = Configuration.Release;
        ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "SatelliteLoadingTestsFromPackageReference");
        BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: "-p:TestSatelliteAssembliesFromPackage=true"));

        string binFrameworkDir = GetBinFrameworkDir(config, forPublish: false);

        // Microsoft.CodeAnalysis.CSharp has satellite assemblies for multiple locales
        // Verify that at least some of them are present in the AppBundle
        string[] expectedLocales = ["cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant"];
        foreach (string locale in expectedLocales)
        {
            string satelliteDir = Path.Combine(binFrameworkDir, locale);
            Assert.True(Directory.Exists(satelliteDir), $"Expected satellite directory '{locale}' to exist in {binFrameworkDir}");

            string[] satelliteFiles = Directory.GetFiles(satelliteDir, "Microsoft.CodeAnalysis.CSharp.resources*");
            Assert.True(satelliteFiles.Length > 0, $"Expected Microsoft.CodeAnalysis.CSharp.resources.dll in {satelliteDir}");
        }
    }
}
