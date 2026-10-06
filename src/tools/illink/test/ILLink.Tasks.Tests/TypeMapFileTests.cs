// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace ILLink.Tasks.Tests;

public class TypeMapFileTests
{
    [Theory]
    [InlineData("full", true)]
    [InlineData("partial", true)]
    [InlineData("partial", false)]
    public async Task SDKProducesIncrementalTypeMapSidecar(string trimMode, bool isTrimmable)
    {
        string directory = Path.Combine(Path.GetTempPath(), nameof(TypeMapFileTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string input = Path.Combine(directory, "App.dll");
            string linkedDirectory = Path.Combine(directory, "linked");
            string sidecar = Path.Combine(linkedDirectory, "App.typemaps.xml");
            string publishedSidecar = Path.Combine(directory, "publish", "App.typemaps.xml");
            string project = Path.Combine(directory, "App.proj");
            string tasksAssembly = (string)AppContext.GetData("ILLink.Tasks.Tests.ILLinkTasksAssembly");
            string msbuild = (string)AppContext.GetData("ILLink.Tasks.Tests.MSBuildPath");
            string sdkTasksAssembly = (string)AppContext.GetData("ILLink.Tasks.Tests.SDKTasksAssembly");
            string corelib = typeof(object).Assembly.Location;
            await CreateApplication(input, corelib, "kept");
            new XElement("Project",
                new XElement("UsingTask", new XAttribute("TaskName", "NETSdkInformation"), new XAttribute("AssemblyFile", sdkTasksAssembly)),
                new XElement("UsingTask", new XAttribute("TaskName", "NETSdkError"), new XAttribute("AssemblyFile", sdkTasksAssembly)),
                new XElement("PropertyGroup",
                    new XElement("PublishTrimmed", "true"),
                    new XElement("TypeMapGenerateXmlFile", "true"),
                    new XElement("TargetFrameworkIdentifier", ".NETCoreApp"),
                    new XElement("_TargetFrameworkVersionWithoutV", Environment.Version.ToString(2)),
                    new XElement("AssemblyName", "App"),
                    new XElement("TargetName", "App"),
                    new XElement("IntermediateOutputPath", Path.Combine(directory, "obj") + Path.DirectorySeparatorChar),
                    new XElement("IntermediateLinkDir", linkedDirectory + Path.DirectorySeparatorChar),
                    new XElement("ILLinkTasksAssembly", tasksAssembly),
                    new XElement("MicrosoftNETBuildTasksAssembly", sdkTasksAssembly),
                    new XElement("TrimMode", trimMode),
                    new XElement("_ExtraTrimmerArgs", "--ignore-descriptors true --ignore-substitutions true --ignore-link-attributes false --deterministic true")),
                new XElement("ItemGroup",
                    new XElement("ManagedAssemblyToLink", new XAttribute("Include", input), new XElement("IsTrimmable", isTrimmable)),
                    new XElement("ManagedAssemblyToLink", new XAttribute("Include", corelib), new XElement("IsTrimmable", true)),
                    new XElement("ReferencePath", new XAttribute("Include", Path.Combine(Path.GetDirectoryName(corelib), "*.dll"))),
                    new XElement("TrimmerRootAssembly", new XAttribute("Include", "App"), new XElement("RootMode", "EntryPoint"))),
                new XElement("Import", new XAttribute("Project", Path.Combine(Path.GetDirectoryName(msbuild), "Sdks", "Microsoft.NET.Sdk", "targets", "Microsoft.NET.Publish.targets"))),
                new XElement("Import", new XAttribute("Project", Path.Combine(Path.GetDirectoryName(tasksAssembly), "build", "Microsoft.NET.ILLink.targets"))),
                new XElement("Target", new XAttribute("Name", "_ComputeManagedAssemblyToLink")),
                new XElement("Target", new XAttribute("Name", "PrepareForILLink")),
                new XElement("Target", new XAttribute("Name", "_HandleFileConflictsForPublish")),
                new XElement("Target", new XAttribute("Name", "Publish"), new XAttribute("DependsOnTargets", "ILLink;PrepareForBundle"),
                    new XElement("ItemGroup",
                        new XElement("_PublishedTypeMap", new XAttribute("Include", "@(ResolvedFileToPublish);@(FilesToBundle)"),
                            new XAttribute("Condition", "'%(Filename)%(Extension)' == 'App.typemaps.xml'"))),
                    new XElement("Error", new XAttribute("Condition", "'@(_PublishedTypeMap)' != ''"),
                        new XAttribute("Text", "The intermediate type map artifact must not be published or bundled.")),
                    new XElement("Copy",
                        new XAttribute("SourceFiles", "@(ResolvedFileToPublish)"),
                        new XAttribute("DestinationFiles", "@(ResolvedFileToPublish->'$(MSBuildProjectDirectory)/publish/%(RelativePath)')"),
                        new XAttribute("SkipUnchangedFiles", "true")))).Save(project);

            await Publish();
            Assert.True(File.Exists(Path.Combine(linkedDirectory, "App.dll")));
            Assert.False(File.Exists(Path.Combine(linkedDirectory, "typemaps.xml")));
            CheckArtifact("kept");
            byte[] original = File.ReadAllBytes(sidecar);
            DateTime written = File.GetLastWriteTimeUtc(sidecar);

            await Publish();
            Assert.Equal(written, File.GetLastWriteTimeUtc(sidecar));
            Assert.Equal(original, File.ReadAllBytes(sidecar));

            await Task.Delay(TimeSpan.FromSeconds(1));
            File.SetLastWriteTimeUtc(input, DateTime.UtcNow);
            await Publish();
            Assert.True(File.GetLastWriteTimeUtc(sidecar) > written);
            Assert.Equal(original, File.ReadAllBytes(sidecar));
            written = File.GetLastWriteTimeUtc(sidecar);
            await Publish();
            Assert.Equal(written, File.GetLastWriteTimeUtc(sidecar));

            File.Delete(sidecar);
            await Publish();
            Assert.Equal(original, File.ReadAllBytes(sidecar));
            CheckArtifact("kept");
            written = File.GetLastWriteTimeUtc(sidecar);
            await Publish();
            Assert.Equal(written, File.GetLastWriteTimeUtc(sidecar));

            await Task.Delay(TimeSpan.FromSeconds(1));
            await CreateApplication(input, corelib, "changed");
            await Publish();
            CheckArtifact("changed");
            Assert.NotEqual(original, File.ReadAllBytes(sidecar));
            written = File.GetLastWriteTimeUtc(sidecar);
            await Publish();
            Assert.Equal(written, File.GetLastWriteTimeUtc(sidecar));

            foreach (bool includeAllContent in new[] { false, true })
            {
                await Publish(publishSingleFile: true, includeAllContent: includeAllContent);
                CheckArtifact("changed");
            }

            await CreateApplication(input, corelib, "invalid\0key");
            File.SetLastWriteTimeUtc(input, written.AddSeconds(-1));
            File.Delete(sidecar);

            await Publish(expectSuccess: false);
            Assert.False(File.Exists(sidecar));
            await Publish(expectSuccess: false);
            Assert.False(File.Exists(sidecar));

            await CreateApplication(input, corelib, "changed");
            await Publish();
            CheckArtifact("changed");

            await Publish(typeMapGenerateXmlFile: false, output: Path.Combine(directory, "disabled"));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(directory, "disabled"), "*.typemaps.xml"));

            void CheckArtifact(string expectedKey)
            {
                Assert.False(File.Exists(publishedSidecar));
                XElement root = XElement.Load(sidecar);
                Assert.Equal("typemaps", root.Name);
                Assert.Equal("1", (string)root.Attribute("version"));
                XElement group = Assert.Single(root.Elements("group"));
                Assert.Equal("Example.Universe, App", (string)group.Attribute("type"));
                Assert.Equal(new[] { "external", "proxy" }, group.Elements().Select(map => map.Name.LocalName));
                XElement external = Assert.Single(group.Elements("external"));
                Assert.Equal(isTrimmable
                    ? new[] { (expectedKey, "Example.Target, App") }
                    : new[] { (expectedKey, "Example.Target, App"), ("trimmed", "Example.Unused, App") },
                    external.Elements("entry").Select(entry => ((string)entry.Attribute("key"), (string)entry.Attribute("value"))));
                XElement proxy = Assert.Single(group.Elements("proxy"));
                Assert.Equal(isTrimmable
                    ? new[] { ("Example.Source, App", "Example.Target, App") }
                    : new[] { ("Example.Source, App", "Example.Target, App"), ("Example.Unused, App", "Example.Unused, App") },
                    proxy.Elements("entry").Select(entry => ((string)entry.Attribute("key"), (string)entry.Attribute("value"))));
                if (!isTrimmable)
                    Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(Path.Combine(linkedDirectory, "App.dll")));
            }

            async Task Publish(bool typeMapGenerateXmlFile = true, string output = null, bool publishSingleFile = false, bool includeAllContent = false, bool expectSuccess = true)
            {
                var arguments = new List<string>
                {
                    msbuild,
                    project,
                    "-t:Publish",
                    "-nologo",
                    "-v:minimal",
                    $"-p:TypeMapGenerateXmlFile={typeMapGenerateXmlFile}",
                    $"-p:PublishSingleFile={publishSingleFile}",
                    $"-p:IncludeAllContentForSelfExtract={includeAllContent}"
                };

                if (output is not null)
                    arguments.Add($"-p:IntermediateLinkDir={output}{Path.DirectorySeparatorChar}");

                (int exitCode, string diagnostics) = await RunTool(directory, arguments);
                if (expectSuccess)
                {
                    Assert.True(exitCode == 0, $"MSBuild failed with exit code {exitCode}.\n{diagnostics}");
                }
                else
                {
                    Assert.True(exitCode != 0, "MSBuild skipped the failed artifact generation and reported success.");
                    Assert.Contains(sidecar, diagnostics, StringComparison.Ordinal);
                }
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task CreateApplication(string path, string corelibPath, string key)
    {
        string source = Path.ChangeExtension(path, ".cs");
        await File.WriteAllTextAsync(source, $$"""
            using System.Runtime.InteropServices;

            [assembly: TypeMap<Example.Universe>({{JsonSerializer.Serialize(key)}}, typeof(Example.Target))]
            [assembly: TypeMap<Example.Universe>("trimmed", typeof(Example.Unused), typeof(Example.Unused))]
            [assembly: TypeMapAssociation<Example.Universe>(typeof(Example.Source), typeof(Example.Target))]
            [assembly: TypeMapAssociation<Example.Universe>(typeof(Example.Unused), typeof(Example.Unused))]

            namespace Example;

            public class Program
            {
                public static void Main()
                {
                    _ = new Source();
                    _ = TypeMapping.GetOrCreateExternalTypeMapping<Universe>();
                    _ = TypeMapping.GetOrCreateProxyTypeMapping<Universe>();
                }
            }

            public class Universe;
            public class Target;
            public class Source;
            public class Unused;
            """);

        string msbuild = (string)AppContext.GetData("ILLink.Tasks.Tests.MSBuildPath");
        string compiler = Path.Combine(Path.GetDirectoryName(msbuild), "Roslyn", "bincore", "csc.dll");
        (int exitCode, string diagnostics) = await RunTool(Path.GetDirectoryName(path), new[]
        {
            compiler, "-nologo", "-noconfig", "-nostdlib", "-target:exe",
            $"-out:{path}", $"-reference:{corelibPath}", source
        });
        Assert.True(exitCode == 0, $"Fixture compilation failed with exit code {exitCode}.\n{diagnostics}");
    }

    private static async Task<(int ExitCode, string Diagnostics)> RunTool(string workingDirectory, IEnumerable<string> arguments)
    {
        string dotnet = (string)AppContext.GetData("ILLink.Tasks.Tests.DotNetHostPath");
        var startInfo = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["DOTNET_HOST_PATH"] = dotnet;
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"Tool timed out.\n{await stdout}\n{await stderr}");
        }

        return (process.ExitCode, await stdout + await stderr);
    }
}
