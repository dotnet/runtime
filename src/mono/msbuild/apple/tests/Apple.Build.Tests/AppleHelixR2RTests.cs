// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Apple.Build.Tests;

// Covers the Apple Helix ReadyToRun proxy build. The test drives the real
// src/mono/msbuild/apple/data/ProxyProjectForAOTOnHelix.proj - shipped into the test payload as
// content - with `dotnet msbuild`, and stands in for crossgen2 by planting files where it would write
// them.
//
// A Helix retry re-runs the whole work item command in the same directory, so the R2R step has to
// leave the publish directory - its own crossgen2 input - unchanged, and has to rebuild
// everything it hands to the app builder from scratch on every attempt.
//
// Only MSBuild path handling is under test, so this runs on any ordinary build host: no Apple SDK, no
// device, and no workload provisioning.
public class AppleHelixR2RTests
{
    private const string ProxyProjectFileName = "ProxyProjectForAOTOnHelix.proj";
    private const string ProxyPropsFileName = "ProxyProjectForAOTOnHelix.props";
    private const string BundleStateFileName = "bundle-state.txt";
    private const string PreparedBundleStateFileName = "prepared-bundle-state.txt";
    private const string ExtraFileName = "libExtra.dylib";

    private static readonly TimeSpan s_buildTimeout = TimeSpan.FromMinutes(5);

    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    // Payload the build machine ships in the work item, which crossgen2 reads and must never rewrite.
    private static readonly (string RelativePath, string Content)[] s_publishInputFiles =
    {
        ("AppleTestRunner.dll", "publish-input-il:AppleTestRunner"),
        ("Lib.dll", "publish-input-il:Lib"),
        ("Lib.resources.dll", "publish-input-resources:Lib"),
        ("libnative.dylib", "publish-input-native"),
        ("nested/data.bin", "publish-input-nested-data"),
    };

    private static readonly (string RelativePath, string Content)[] s_firstAttemptR2ROutput =
    {
        ("AppleTestRunner.dll", "r2r-attempt1:AppleTestRunner"),
        ("Lib.dll", "r2r-attempt1:Lib"),
        ("FirstAttemptOnly.dll", "r2r-attempt1:FirstAttemptOnly"),
        ("app.r2r.dylib", "r2r-attempt1:composite"),
    };

    private static readonly (string RelativePath, string Content)[] s_secondAttemptR2ROutput =
    {
        ("AppleTestRunner.dll", "r2r-attempt2:AppleTestRunner"),
        ("Lib.dll", "r2r-attempt2:Lib"),
        ("app.r2r.dylib", "r2r-attempt2:composite"),
    };

    private readonly ITestOutputHelper _testOutput;

    public AppleHelixR2RTests(ITestOutputHelper testOutput) => _testOutput = testOutput;

    [Fact]
    public void R2RAppBundleSourceIsRebuiltWithoutModifyingPublishInputs()
    {
        DirectoryInfo workItemRoot = Directory.CreateTempSubdirectory("apple-helix-r2r-");
        try
        {
            string publishDir = Path.Combine(workItemRoot.FullName, "publish");
            string extraFilesDir = Path.Combine(workItemRoot.FullName, "extraFiles");
            string crossgenOutputDir = Path.Combine(workItemRoot.FullName, "crossgen-output");
            string r2rIntermediateDir = Path.Combine(workItemRoot.FullName, "obj", "R2R");
            string appBundleSourceDir = Path.Combine(workItemRoot.FullName, "obj", "r2r-app-bundle-source");

            CreateWorkItemLayout(workItemRoot.FullName, publishDir, extraFilesDir);

            Dictionary<string, string> publishInputContents = ReadDirectoryContents(publishDir);

            RunAttempt(workItemRoot.FullName, publishDir, crossgenOutputDir, r2rIntermediateDir, appBundleSourceDir, s_firstAttemptR2ROutput);
            AssertAttempt(workItemRoot.FullName, publishDir, publishInputContents, extraFilesDir, appBundleSourceDir, s_firstAttemptR2ROutput);

            RunAttempt(workItemRoot.FullName, publishDir, crossgenOutputDir, r2rIntermediateDir, appBundleSourceDir, s_secondAttemptR2ROutput);
            AssertAttempt(workItemRoot.FullName, publishDir, publishInputContents, extraFilesDir, appBundleSourceDir, s_secondAttemptR2ROutput);
        }
        finally
        {
            TryDeleteDirectory(workItemRoot.FullName);
        }
    }

    private static void CreateWorkItemLayout(string workItemRoot, string publishDir, string extraFilesDir)
    {
        foreach ((string relativePath, string content) in s_publishInputFiles)
            WriteFile(Path.Combine(publishDir, ToNativePath(relativePath)), content);

        WriteFile(Path.Combine(extraFilesDir, ExtraFileName), "extra-native");

        string proxyProject = Path.Combine(AppContext.BaseDirectory, "data", ProxyProjectFileName);
        Assert.True(File.Exists(proxyProject), $"Expected the proxy project to be copied into the test payload at '{proxyProject}'.");
        File.Copy(proxyProject, Path.Combine(publishDir, ProxyProjectFileName));

        WriteFile(Path.Combine(publishDir, ProxyPropsFileName), BuildProxyPropsStub());

        // The work item layout is created under the system temp directory, so shield the proxy
        // project from any Directory.Build.props/targets that might sit above it.
        WriteFile(Path.Combine(workItemRoot, "Directory.Build.props"), "<Project />");
        WriteFile(Path.Combine(workItemRoot, "Directory.Build.targets"), "<Project />");

        // The SDK resolver walks up from the working directory looking for a global.json, which is a
        // separate lookup from the Directory.Build one above and is not covered by it. A stray
        // global.json anywhere above the temp directory pins the child to an SDK version the resolved
        // host does not have, and the child then fails before MSBuild even starts. Wasm.Build.Tests
        // shields its own child builds the same way.
        WriteFile(Path.Combine(workItemRoot, "global.json"), "{}");
    }

    // The props file the proxy project imports. On Helix it carries the settings of the test project
    // being proxied; here it stands in for crossgen2 and records what the R2R target handed to the
    // Apple app builder.
    private static string BuildProxyPropsStub()
    {
        var targetFramework = new FrameworkName(AppContext.TargetFrameworkName!);
        return $"""
            <Project>
              <PropertyGroup>
                <TargetFramework>net{targetFramework.Version.Major}.{targetFramework.Version.Minor}</TargetFramework>
                <_TestCrossgenOutputDir>$([MSBuild]::NormalizeDirectory($(TestRootDir), '..', 'crossgen-output'))</_TestCrossgenOutputDir>
                <_TestBundleStateFile>$([MSBuild]::NormalizePath($(TestRootDir), '..', '{BundleStateFileName}'))</_TestBundleStateFile>
                <_TestPreparedBundleStateFile>$([MSBuild]::NormalizePath($(TestRootDir), '..', '{PreparedBundleStateFileName}'))</_TestPreparedBundleStateFile>
              </PropertyGroup>

              <Target Name="_TestAssertAndRecordPreparedBundleState" AfterTargets="_PrepareForAppleBuildAppOnHelix">
                <Error Condition="!Exists('$(AppleBuildDir)stale-marker.txt')"
                       Text="Expected the stale bundle-source marker to remain immediately after preparation, before bundle-source materialization." />
                <ItemGroup>
                  <_TestPreparedBundleState Include="AppleBuildDir=$(AppleBuildDir)" />
                  <_TestPreparedBundleState Include="@(AppleAssembliesToBundle->'Assembly=%(FullPath)')" />
                  <_TestPreparedBundleState Include="@(AppleNativeFilesToBundle->'Native=%(FullPath)')" />
                </ItemGroup>
                <WriteLinesToFile File="$(_TestPreparedBundleStateFile)" Lines="@(_TestPreparedBundleState)" Overwrite="true" WriteOnlyWhenDifferent="false" />
              </Target>

              <Target Name="_TestProduceR2ROutput" AfterTargets="_PrepareR2RItemsOnHelix">
                <ItemGroup>
                  <_TestCrossgenOutput Include="$(_TestCrossgenOutputDir)*" />
                </ItemGroup>
                <MakeDir Directories="$(IntermediateOutputPath)R2R" />
                <Copy SourceFiles="@(_TestCrossgenOutput)" DestinationFolder="$(IntermediateOutputPath)R2R" />
              </Target>

              <Target Name="_TestRecordBundleState" AfterTargets="_AddR2RFilesToAppleBundle">
                <ItemGroup>
                  <_TestBundleState Include="AppleBuildDir=$(AppleBuildDir)" />
                  <_TestBundleState Include="@(AppleAssembliesToBundle->'Assembly=%(FullPath)')" />
                  <_TestBundleState Include="@(AppleNativeFilesToBundle->'Native=%(FullPath)')" />
                </ItemGroup>
                <WriteLinesToFile File="$(_TestBundleStateFile)" Lines="@(_TestBundleState)" Overwrite="true" WriteOnlyWhenDifferent="false" />
              </Target>
            </Project>

            """;
    }

    private void RunAttempt(
        string workItemRoot,
        string publishDir,
        string crossgenOutputDir,
        string r2rIntermediateDir,
        string appBundleSourceDir,
        (string RelativePath, string Content)[] r2rOutput)
    {
        ReplaceDirectoryContent(crossgenOutputDir, r2rOutput);

        // A previous attempt that was killed mid write leaves output behind in both directories.
        // Neither leftover may reach the app: the R2R intermediate dir is wiped before compiling,
        // the app bundle source dir is recreated before the bundle source is staged into it.
        WriteFile(Path.Combine(r2rIntermediateDir, "KilledAttempt.dll"), "leftover-r2r-output");
        WriteFile(Path.Combine(appBundleSourceDir, "stale-marker.txt"), "leftover-bundle-source-file");
        WriteFile(Path.Combine(appBundleSourceDir, "bin-previous", "leftover.txt"), "leftover-bundle-source-tree");
        WriteFile(Path.Combine(appBundleSourceDir, "app.r2r.dylib"), "leftover-composite");

        string bundleStateFile = Path.Combine(workItemRoot, BundleStateFileName);
        string preparedBundleStateFile = Path.Combine(workItemRoot, PreparedBundleStateFileName);
        File.Delete(bundleStateFile);
        File.Delete(preparedBundleStateFile);

        RunMSBuild(workItemRoot, publishDir);

        Assert.True(File.Exists(bundleStateFile), $"The proxy build did not record the bundle state, so '{ProxyPropsFileName}' was not imported.");
        Assert.True(File.Exists(preparedBundleStateFile), $"The proxy build did not record the prepared bundle state, so '{ProxyPropsFileName}' was not imported.");
    }

    private void RunMSBuild(string workItemRoot, string publishDir)
    {
        string host = ResolveDotNetHost();
        var startInfo = new ProcessStartInfo(host)
        {
            WorkingDirectory = publishDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(ProxyProjectFileName);
        startInfo.ArgumentList.Add("-t:_PrepareForAppleBuildAppOnHelix;_PrepareR2RItemsOnHelix;_AddR2RFilesToAppleBundle");
        startInfo.ArgumentList.Add("-p:PublishReadyToRun=true");
        startInfo.ArgumentList.Add("-p:UseMonoRuntime=false");
        startInfo.ArgumentList.Add("-p:UseNativeAOTRuntime=false");
        startInfo.ArgumentList.Add("-p:PublishReadyToRunContainerFormat=macho");
        startInfo.ArgumentList.Add("-nodeReuse:false");
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-v:minimal");
        startInfo.Environment["HELIX_WORKITEM_ROOT"] = workItemRoot;
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        // The repo build sets this, and it makes the child pick up the wrong SDK.
        startInfo.Environment.Remove("MSBuildSDKsPath");

        // The repo build environment points these at its own dotnet, and the child has to use the one
        // that was just resolved.
        string hostDir = Path.GetDirectoryName(host)!;
        startInfo.Environment["DOTNET_ROOT"] = hostDir;
        startInfo.Environment["DOTNET_INSTALL_DIR"] = hostDir;
        // An empty PATH entry means the working directory on unix, so do not leave a trailing
        // separator behind when the parent has no PATH.
        string? path = Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["PATH"] = string.IsNullOrEmpty(path)
            ? hostDir
            : $"{hostDir}{Path.PathSeparator}{path}";

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => AppendLine(output, e.Data);
        process.ErrorDataReceived += (_, e) => AppendLine(output, e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)s_buildTimeout.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new XunitException($"The proxy project build did not finish within {s_buildTimeout}.{Environment.NewLine}{output}");
        }

        // Flushes the redirected streams.
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new XunitException($"The proxy project build failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");

        _testOutput.WriteLine(output.ToString());
    }

    private void AssertAttempt(
        string workItemRoot,
        string publishDir,
        Dictionary<string, string> expectedPublishContents,
        string extraFilesDir,
        string appBundleSourceDir,
        (string RelativePath, string Content)[] r2rOutput)
    {
        AssertDirectoryContents("publish inputs", expectedPublishContents, ReadDirectoryContents(publishDir));

        Dictionary<string, string> expectedBundleSourceContents = CreateExpectedBundleSourceContents(expectedPublishContents, r2rOutput);
        AssertDirectoryContents("app bundle source", expectedBundleSourceContents, ReadDirectoryContents(appBundleSourceDir));

        BundleState expectedBundleState = CreateExpectedBundleState(expectedPublishContents.Keys, extraFilesDir, appBundleSourceDir);

        // This checkpoint is recorded before the bundle-source directory is cleaned and materialized.
        AssertRecordedBundleState("prepared bundle state", expectedBundleState, ReadRecordedBundleState(workItemRoot, PreparedBundleStateFileName));
        AssertRecordedBundleState("materialized bundle state", expectedBundleState, ReadRecordedBundleState(workItemRoot, BundleStateFileName));
    }

    private static Dictionary<string, string> CreateExpectedBundleSourceContents(
        Dictionary<string, string> publishInputContents,
        (string RelativePath, string Content)[] r2rOutput)
    {
        Dictionary<string, string> expectedContents = new(publishInputContents, s_pathComparer);
        foreach ((string relativePath, string content) in r2rOutput)
            expectedContents[relativePath] = content;

        return expectedContents;
    }

    private static BundleState CreateExpectedBundleState(
        IEnumerable<string> publishInputPaths,
        string extraFilesDir,
        string appBundleSourceDir)
    {
        List<string> assemblies = publishInputPaths
            .Where(relativePath => IsTopLevel(relativePath)
                && relativePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && !relativePath.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            .Select(relativePath => Path.Combine(appBundleSourceDir, ToNativePath(relativePath)))
            .ToList();

        List<string> nativeFiles = publishInputPaths
            .Where(relativePath => !IsTopLevel(relativePath) || !relativePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(relativePath => Path.Combine(appBundleSourceDir, ToNativePath(relativePath)))
            .Append(Path.Combine(extraFilesDir, ExtraFileName))
            .ToList();

        return new BundleState(appBundleSourceDir, assemblies, nativeFiles);
    }

    private static BundleState ReadRecordedBundleState(string workItemRoot, string fileName)
    {
        string? appleBuildDir = null;
        List<string> assemblies = new();
        List<string> nativeFiles = new();

        foreach (string line in File.ReadAllLines(Path.Combine(workItemRoot, fileName)))
        {
            string[] parts = line.Split('=', 2);
            switch (parts)
            {
                case ["AppleBuildDir", string value]:
                    appleBuildDir = value;
                    break;
                case ["Assembly", string value]:
                    assemblies.Add(value);
                    break;
                case ["Native", string value]:
                    nativeFiles.Add(value);
                    break;
                default:
                    throw new XunitException($"Unexpected line in {fileName}: '{line}'.");
            }
        }

        if (appleBuildDir is null)
            throw new XunitException($"{fileName} did not record AppleBuildDir.");

        return new BundleState(appleBuildDir, assemblies, nativeFiles);
    }

    private static void AssertRecordedBundleState(string what, BundleState expected, BundleState actual)
    {
        Assert.Equal(NormalizeDirectory(expected.AppleBuildDir), NormalizeDirectory(actual.AppleBuildDir), s_pathComparer);
        AssertSamePaths($"{what} AppleAssembliesToBundle", expected.Assemblies, actual.Assemblies);
        AssertSamePaths($"{what} AppleNativeFilesToBundle", expected.NativeFiles, actual.NativeFiles);
    }

    private static void AssertDirectoryContents(string what, Dictionary<string, string> expected, Dictionary<string, string> actual)
    {
        List<string> differences = new();

        foreach ((string relativePath, string content) in expected.OrderBy(entry => entry.Key, s_pathComparer))
        {
            if (!actual.TryGetValue(relativePath, out string? actualContent))
                differences.Add($"  missing: {relativePath}");
            else if (actualContent != content)
                differences.Add($"  content changed: {relativePath}");
        }

        foreach (string relativePath in actual.Keys.Where(key => !expected.ContainsKey(key)).OrderBy(key => key, s_pathComparer))
            differences.Add($"  unexpected: {relativePath}");

        if (differences.Count > 0)
            throw new XunitException($"Unexpected content in the {what}:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
    }

    private static void AssertSamePaths(string what, IEnumerable<string> expected, IEnumerable<string> actual)
    {
        List<string> expectedPaths = expected.Select(Path.GetFullPath).Order(s_pathComparer).ToList();
        List<string> actualPaths = actual.Select(Path.GetFullPath).Order(s_pathComparer).ToList();

        if (!expectedPaths.SequenceEqual(actualPaths, s_pathComparer))
        {
            throw new XunitException(
                $"Unexpected {what}:{Environment.NewLine}expected:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", expectedPaths)}" +
                $"{Environment.NewLine}actual:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", actualPaths)}");
        }
    }

    private static Dictionary<string, string> ReadDirectoryContents(string root)
    {
        Dictionary<string, string> contents = new(s_pathComparer);
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            contents[ToRelativePath(root, file)] = File.ReadAllText(file);

        return contents;
    }

    // The proxy project is an SDK project, so the child needs a full SDK and not just a runtime host.
    // Everything is resolved to an absolute path here because the child runs with a different working
    // directory, and nothing is looked up relative to the current one for the same reason.
    private static string ResolveDotNetHost()
    {
        string fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        List<string> candidates = new();

        // The SDK sets this for everything it launches, so under `dotnet build`/`dotnet test` it is
        // already the muxer that is driving this build.
        AddHost(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"));

        // The xunit console runner is started as `dotnet exec ...`, so the current process is a muxer.
        AddHost(Environment.ProcessPath);

        // Set by the repo build scripts.
        AddHostIn(Environment.GetEnvironmentVariable("DOTNET_ROOT"));

        // The dotnet the repo provisions, found by walking up from the test assembly rather than from
        // the working directory, which the test host owns.
        AddHostIn(FindRepoDotNetDirectory(fileName));

        foreach (string candidate in candidates)
        {
            if (HasSdk(candidate))
                return candidate;
        }

        throw new XunitException(
            $"Could not find a dotnet host with an SDK. Tried:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", candidates)}");

        void AddHost(string? host)
        {
            if (!string.IsNullOrEmpty(host) && Path.GetFileName(host).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                candidates.Add(Path.GetFullPath(host));
        }

        void AddHostIn(string? directory)
        {
            if (!string.IsNullOrEmpty(directory))
                candidates.Add(Path.GetFullPath(Path.Combine(directory, fileName)));
        }
    }

    private static string? FindRepoDotNetDirectory(string fileName)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, ".dotnet");
            if (File.Exists(Path.Combine(candidate, fileName)))
                return candidate;
        }

        return null;
    }

    // A host that only ships a runtime cannot restore or build the proxy project, so keep looking.
    private static bool HasSdk(string host)
    {
        if (!File.Exists(host))
            return false;

        string sdkDir = Path.Combine(Path.GetDirectoryName(host)!, "sdk");
        return Directory.Exists(sdkDir) && Directory.EnumerateDirectories(sdkDir).Any();
    }

    private static void ReplaceDirectoryContent(string directory, (string RelativePath, string Content)[] files)
    {
        DeleteDirectory(directory);
        foreach ((string relativePath, string content) in files)
            WriteFile(Path.Combine(directory, ToNativePath(relativePath)), content);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    // Used to set the test up, so a directory that cannot be emptied has to fail the test instead of
    // leaving content from the previous attempt in place and asserting against the wrong inputs.
    private static void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    // Cleanup only: the work item root lives under the system temp directory, and failing to remove it
    // must not turn a passing test into a failing one.
    private static void TryDeleteDirectory(string path)
    {
        try
        {
            DeleteDirectory(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void AppendLine(StringBuilder output, string? line)
    {
        if (line is not null)
        {
            lock (output)
                output.AppendLine(line);
        }
    }

    private static bool IsTopLevel(string relativePath) => !relativePath.Contains('/');

    private static string ToNativePath(string relativePath) => relativePath.Replace('/', Path.DirectorySeparatorChar);

    private static string ToRelativePath(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string NormalizeDirectory(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private sealed record BundleState(string AppleBuildDir, List<string> Assemblies, List<string> NativeFiles);
}
