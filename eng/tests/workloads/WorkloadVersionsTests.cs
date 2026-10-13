// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace WorkloadVersions.Tests;

public sealed class WorkloadVersionsTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("runtime-workloads-").FullName;
    private readonly string _repository = FindRepository();

    [Theory]
    [InlineData("Current", "11.0.100-rc.1.26420.103", "12.0.100-alpha.1.26471.108", "12.0.100-alpha.1.26471.108", "12.0.100-alpha.1")]
    [InlineData("net10", "12.0.100", "11.0.100", "12.0.100", "12.0.100")]
    [InlineData("net9", "11.0.100-rc.1.26420.103", "", "11.0.100-rc.1.26420.103", "11.0.100-rc.1")]
    [InlineData("net8", "11.0.100-rtm.26420.103", "", "11.0.100-rtm.26420.103", "11.0.100")]
    [InlineData("net7", "11.0.100-servicing.26420.103", "", "11.0.100-servicing.26420.103", "11.0.100")]
    [InlineData("net6", "11.0.100-preview.1", "", "11.0.100-preview.1", "11.0.100")]
    public void ManifestPackingInitializesWorkloadVersions(string variant, string sdkVersion,
        string packageVersion, string selectedVersion, string manifestBand)
    {
        string name = $"Microsoft.NET.Workload.Mono.Toolchain.{variant}.Manifest";
        string project = Path.Combine(_repository, "src/mono/nuget", name, name + ".pkgproj");
        string globalJson = Path.Combine(_directory, "global.json");
        File.WriteAllText(globalJson, JsonSerializer.Serialize(new { sdk = new { version = sdkVersion } }));
        string properties = $"-p:_WorkloadGlobalJsonPath={globalJson};MicrosoftDotNetGenAPITaskPackageVersion={packageVersion}";
        const string QueryProperties = "-getProperty:SdkVersionForWorkloadTesting,SdkMajorVersion,SdkBandVersion,SdkPreReleaseLabel,SdkBandVersionForWorkload_FromRuntimeVersions,Id";
        using (JsonDocument evaluation = JsonDocument.Parse(RunDotNet("msbuild", project,
            properties, QueryProperties, "-getItem:WorkloadSdkBandVersions", "-nologo")))
        {
            foreach (string property in new[] { "SdkVersionForWorkloadTesting", "SdkMajorVersion", "SdkBandVersion",
                "SdkPreReleaseLabel", "SdkBandVersionForWorkload_FromRuntimeVersions" })
            {
                Assert.Equal("", evaluation.RootElement.GetProperty("Properties").GetProperty(property).GetString());
            }

            Assert.Equal(0, evaluation.RootElement.GetProperty("Items").GetProperty("WorkloadSdkBandVersions").GetArrayLength());
        }

        using JsonDocument execution = JsonDocument.Parse(RunDotNet("msbuild", project,
            properties, "-t:_PrepareForPack", QueryProperties, "-getItem:WorkloadSdkBandVersions", "-nologo"));
        JsonElement result = execution.RootElement.GetProperty("Properties");
        Assert.Equal(selectedVersion, result.GetProperty("SdkVersionForWorkloadTesting").GetString());
        Assert.Equal(manifestBand, result.GetProperty("SdkBandVersionForWorkload_FromRuntimeVersions").GetString());
        Assert.Equal($"Microsoft.NET.Workload.Mono.ToolChain.{variant}.Manifest-{manifestBand}", result.GetProperty("Id").GetString());
        JsonElement bands = execution.RootElement.GetProperty("Items").GetProperty("WorkloadSdkBandVersions");
        Assert.Equal(1, bands.GetArrayLength());
        Assert.Equal(selectedVersion.Split('.')[0] + ".0.100", bands[0].GetProperty("Identity").GetString());
        Assert.Equal("true", bands[0].GetProperty("SupportsMachineArch").GetString());
    }

    [Fact]
    public void GlobalJsonIsReadOnlyDuringExecution()
    {
        string project = Path.Combine(_directory, "global-json.proj");
        new XDocument(new XElement("Project",
            Import("eng/Versions.props"),
            Import("eng/workloadVersions.targets"))).Save(project);
        Assert.Equal("", RunDotNet("msbuild", project, "-getProperty:_GlobalJsonContent", "-nologo").Trim());
        using JsonDocument globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(_repository, "global.json")));
        Assert.Equal(globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString(),
            RunDotNet("msbuild", project, "-t:CalculateWorkloadSdkVersion", "-getProperty:_GlobalJsonSdkVersion", "-nologo").Trim());
    }

    [Fact]
    public void CachedProvisioningInitializesVersionsBeforeCheckingSdkStamp()
    {
        string binaryRoot = Path.Combine(_directory, "bin") + Path.DirectorySeparatorChar;
        string sdkDirectory = Path.Combine(binaryRoot, "dotnet-none") + Path.DirectorySeparatorChar;
        const string SelectedVersion = "99.0.100-alpha.1.26471.108";
        string stamp = Path.Combine(sdkDirectory, $".version-for-none-{SelectedVersion}.stamp");
        Directory.CreateDirectory(sdkDirectory);
        File.WriteAllText(stamp, "cached-sdk");
        string result = Path.Combine(_directory, "workload-inputs.txt");
        string project = Path.Combine(_directory, "workload-inputs.proj");
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                new XElement("ArtifactsBinDir", binaryRoot),
                new XElement("ArtifactsObjDir", _directory + Path.DirectorySeparatorChar),
                new XElement("ArtifactsDir", _directory),
                new XElement("MonoProjectRoot", Path.Combine(_repository, "src/mono") + Path.DirectorySeparatorChar),
                new XElement("WorkloadBuildTasksAssemblyPath", Path.Combine(AppContext.BaseDirectory, "WorkloadVersions.Tests.dll")),
                new XElement("InstallWorkloadForTesting", "true")),
            Import("eng/Versions.props"),
            Import("eng/workloadVersions.targets"),
            Import("eng/testing/workloads-testing.targets"),
            new XElement("Target", new XAttribute("Name", "SetWorkloadPackageVersion"),
                new XAttribute("BeforeTargets", "CalculateWorkloadSdkVersion"),
                new XElement("PropertyGroup", new XElement("MicrosoftDotNetGenAPITaskPackageVersion", SelectedVersion))),
            new XElement("Target", new XAttribute("Name", "RejectUnexpectedProvisioning"),
                new XAttribute("BeforeTargets", "_ProvisionDotNetForWorkloadTesting"),
                new XElement("Error", new XAttribute("Condition", "!Exists('$(_SdkWithNoWorkloadStampPath)')"),
                    new XAttribute("Text", "A cached SDK must not be provisioned again: $(_SdkWithNoWorkloadStampPath)"))),
            new XElement("Target", new XAttribute("Name", "_InstallSharedFrameworksForWorkloadTesting")),
            new XElement("Target", new XAttribute("Name", "Dump"), new XAttribute("DependsOnTargets", "ProvisionDotNetForWorkloadTesting"),
                new XElement("WriteLinesToFile", new XAttribute("File", result), new XAttribute("Overwrite", "true"),
                    new XAttribute("Lines", "$(SdkVersionForWorkloadTesting);$(VersionBandForManifestPackages);$(_SdkWithNoWorkloadStampPath);$(_DotNetInstallCommand)")))))
            .Save(project);

        using (JsonDocument evaluation = JsonDocument.Parse(RunDotNet("msbuild", project,
            "-getProperty:SdkVersionForWorkloadTesting,VersionBandForManifestPackages,_SdkWithNoWorkloadStampPath,_DotNetInstallCommand", "-nologo")))
        {
            foreach (JsonProperty property in evaluation.RootElement.GetProperty("Properties").EnumerateObject())
            {
                Assert.Equal("", property.Value.GetString());
            }
        }

        RunDotNet("msbuild", project, "-t:Dump", "-nologo");
        string[] lines = File.ReadAllLines(result);
        Assert.Equal(4, lines.Length);
        Assert.Equal(SelectedVersion, lines[0]);
        Assert.Equal("99.0.100-alpha.1", lines[1]);
        Assert.Equal(stamp.Replace('\\', '/'), lines[2].Replace('\\', '/'));
        Assert.EndsWith(OperatingSystem.IsWindows()
            ? $" -InstallDir {sdkDirectory} -Version {SelectedVersion}"
            : $" -i {sdkDirectory} -v {SelectedVersion}", lines[3]);
        Assert.Equal("cached-sdk", File.ReadAllText(stamp));
    }

    [Theory]
    [InlineData("")]
    [InlineData("11.0.100-test.1")]
    public void StandaloneInputsHonorExecutionTimeSdkSelection(string sdkVersion)
    {
        string result = Path.Combine(_directory, "standalone-workload.txt");
        string project = Path.Combine(_directory, "standalone-workload.proj");
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                new XElement("ArtifactsBinDir", _directory + Path.DirectorySeparatorChar),
                new XElement("ArtifactsObjDir", _directory + Path.DirectorySeparatorChar),
                new XElement("InstallWorkloadForTesting", "true"),
                new XElement("WorkloadBuildTasksAssemblyPath", Path.Combine(AppContext.BaseDirectory, "WorkloadVersions.Tests.dll")),
                new XElement("DotNetInstallArgumentsForWorkloadsTesting", "-Channel test-channel")),
            Import("src/mono/nuget/Microsoft.NET.Runtime.WorkloadTesting.Internal/Sdk/WorkloadTesting.Core.targets"),
            new XElement("Target", new XAttribute("Name", "SelectSdk"), new XAttribute("BeforeTargets", "InitializeWorkloadTestingInputs"),
                new XElement("PropertyGroup", new XElement("SdkVersionForWorkloadTesting", sdkVersion))),
            new XElement("Target", new XAttribute("Name", "Dump"), new XAttribute("DependsOnTargets", "InitializeWorkloadTestingInputs"),
                new XElement("WriteLinesToFile", new XAttribute("File", result), new XAttribute("Overwrite", "true"),
                    new XAttribute("Lines", "$(_DotNetInstallCommand);$(SdkWithNoWorkloadStampPath);$(SdkWithWorkloadStampPath)")))))
            .Save(project);
        Assert.Equal("", RunDotNet("msbuild", project, "-getProperty:_DotNetInstallCommand", "-nologo").Trim());
        RunDotNet("msbuild", project, "-t:Dump", "-nologo");
        string[] lines = File.ReadAllLines(result);
        Assert.Equal(3, lines.Length);
        Assert.EndsWith(sdkVersion.Length == 0 ? " -Channel test-channel"
            : OperatingSystem.IsWindows() ? " -Version " + sdkVersion : " -v " + sdkVersion, lines[0]);
        Assert.EndsWith($"version-{sdkVersion}.stamp", lines[1]);
        Assert.EndsWith($"version-{sdkVersion}.stamp", lines[2]);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private XElement Import(string relativePath) =>
        new("Import", new XAttribute("Project", Path.Combine(_repository, relativePath)));

    private string RunDotNet(params string[] arguments)
    {
        string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ??
            Path.Combine(_repository, ".dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var startInfo = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = _repository,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new IOException("Could not launch dotnet.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        string output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
        Assert.True(process.ExitCode == 0, output);
        return output;
    }

    private static string FindRepository()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Build.proj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate runtime Build.proj.");
    }
}
