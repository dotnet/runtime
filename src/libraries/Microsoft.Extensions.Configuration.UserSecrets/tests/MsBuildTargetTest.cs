// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
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

        [Fact]
        public void GeneratesAssemblyAttributeFile()
        {
            var target = Path.Combine(_solutionRoot.FullName, "src", "Configuration", "Config.UserSecrets", "src", "build", "netstandard2.0", "Microsoft.Extensions.Configuration.UserSecrets.targets");
            Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
            var libName = "Microsoft.Extensions.Configuration.UserSecrets.dll";
            File.Copy(Path.Combine(AppContext.BaseDirectory, libName), Path.Combine(_tempDir, libName));
            File.Copy(target, Path.Combine(_tempDir, "obj", "test.csproj.usersecretstest.targets")); // imitates how NuGet will import this target
            var testProj = Path.Combine(_tempDir, "test.csproj");
            // should represent a 'dotnet new' project
            File.WriteAllText(testProj, $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <GenerateDocumentationFile>false</GenerateDocumentationFile>
        <OutputType>Exe</OutputType>
        <UserSecretsId>
            xyz123
        </UserSecretsId>
        <TargetFramework>netcoreapp2.1</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <Reference Include=""$(MSBuildThisFileDirectory){libName}"" />
    </ItemGroup>
</Project>
");
            _output.WriteLine($"Tempdir = {_tempDir}");
            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "public class Program { public static void Main(){}}");
            var assemblyInfoFile = Path.Combine(_tempDir, "obj/Debug/netcoreapp2.1/UserSecretsAssemblyInfo.cs");

            AssertDotNet("restore");

            Assert.False(File.Exists(assemblyInfoFile), $"{assemblyInfoFile} should not exist but does");

            AssertDotNet("build --configuration Debug");

            Assert.True(File.Exists(assemblyInfoFile), $"{assemblyInfoFile} should not exist but does not");
            var contents = File.ReadAllText(assemblyInfoFile);
            Assert.Contains("[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute(\"xyz123\")]", contents);
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
