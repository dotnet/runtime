// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    public class MsBuildTargetTest : IDisposable
    {
        private readonly string _tempDir;
        private readonly DirectoryInfo _solutionRoot;
        private readonly ITestOutputHelper _output;

        public MsBuildTargetTest(ITestOutputHelper output)
        {
            _output = output;
            _tempDir = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);

            _solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
            while (_solutionRoot != null)
            {
                if (File.Exists(Path.Combine(_solutionRoot.FullName, "NuGet.config")))
                {
                    break;
                }

                _solutionRoot = _solutionRoot.Parent;
            }

            if (_solutionRoot == null)
            {
                throw new FileNotFoundException("Could not identify solution root");
            }
        }

        [Theory]
        [InlineData(".csproj", ".cs")]
        [InlineData(".fsproj", ".fs")]
        public void GeneratesAssemblyAttributeFile(string projectExt, string sourceExt)
        {
            var testTfm = typeof(MsBuildTargetTest).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(f => f.Key == "TargetFramework")
                .Value;
            var target = Path.Combine(_solutionRoot.FullName, "src", "Configuration", "Config.UserSecrets", "src", "build", "netstandard2.0", "Microsoft.Extensions.Configuration.UserSecrets.targets");
            Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
            var libName = "Microsoft.Extensions.Configuration.UserSecrets.dll";
            File.Copy(Path.Combine(AppContext.BaseDirectory, libName), Path.Combine(_tempDir, libName));
            File.Copy(target, Path.Combine(_tempDir, "obj", $"test{projectExt}.usersecretstest.targets")); // imitates how NuGet will import this target
            var testProj = Path.Combine(_tempDir, "test" + projectExt);
            // should represent a 'dotnet new' project
            File.WriteAllText(testProj, $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <GenerateDocumentationFile>false</GenerateDocumentationFile>
        <Version>1.0.0</Version>
        <InformationalVersion>1.0.0</InformationalVersion>
        <OutputType>Exe</OutputType>
        <UserSecretsId>
            xyz123
        </UserSecretsId>
        <TargetFramework>{testTfm}</TargetFramework>
        <SignAssembly>false</SignAssembly>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""Program.fs"" Condition=""'$(Language)' == 'F#'"" />
        <PackageReference Remove=""Internal.AspNetCore.Sdk"" />
        <Reference Include=""$(MSBuildThisFileDirectory){libName}"" />
    </ItemGroup>
</Project>
");
            _output.WriteLine($"Tempdir = {_tempDir}");

            switch (projectExt)
            {
                case ".csproj":
                    File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "public class Program { public static void Main(){}}");
                    break;
                case ".fsproj":
                    File.WriteAllText(Path.Combine(_tempDir, "Program.fs"), @"
module SomeNamespace.SubNamespace
open System
[<EntryPoint>]
let main argv =
    printfn ""Hello World from F#!""
    0
");
                    break;
            }

            var assemblyInfoFile = Path.Combine(_tempDir, $"obj/Debug/{testTfm}/test.AssemblyInfo" + sourceExt);

            AssertDotNet("restore");

            Assert.False(File.Exists(assemblyInfoFile), $"{assemblyInfoFile} should not exist but does");

            AssertDotNet("build --configuration Debug");

            Assert.True(File.Exists(assemblyInfoFile), $"{assemblyInfoFile} should not exist but does not");
            var contents = File.ReadAllText(assemblyInfoFile);
            Assert.Contains("assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute(\"xyz123\")", contents);
            var lastWrite = new FileInfo(assemblyInfoFile).LastWriteTimeUtc;

            AssertDotNet("build --configuration Debug");
            // asserts that the target doesn't re-generate assembly file. Important for incremental build.
            Assert.Equal(lastWrite, new FileInfo(assemblyInfoFile).LastWriteTimeUtc);
        }

        private void AssertDotNet(string args)
        {
            void LogData(object obj, DataReceivedEventArgs e)
            {
                _output.WriteLine(e.Data ?? string.Empty);
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                UseShellExecute = false,
                WorkingDirectory = _tempDir,
                RedirectStandardOutput = true,
            };
            var process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = processInfo
            };
            process.OutputDataReceived += LogData;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.OutputDataReceived -= LogData;
            Assert.Equal(0, process.ExitCode);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                Console.Error.WriteLine($"Failed to delete '{_tempDir}' during test cleanup");
            }
        }
    }
}
