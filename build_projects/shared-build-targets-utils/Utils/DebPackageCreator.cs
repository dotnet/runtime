// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class DebPackageCreator
    {
        private static readonly string s_dotnetDebToolPackageId = "dotnet-deb-tool";
        private static readonly string s_toolConsumerProjectName = "dotnet-deb-tool-consumer";
        private static readonly string s_debianConfigJsonFileName = "debian_config.json";

        private DotNetCli _dotnet;
        private string _intermediateDirectory;
        private string _dotnetDebToolVersion;
        private string _dotnetDebToolPackageSource;
        private string _consumingProjectDirectory;

        public DebPackageCreator(
            DotNetCli dotnet,
            string intermediateDirectory,
            string dotnetDebToolVersion = "1.0.1-*",
            string dotnetDebToolPackageSource = null)
        {
            _dotnet = dotnet;
            _intermediateDirectory = intermediateDirectory;
            _dotnetDebToolVersion = dotnetDebToolVersion;
            _dotnetDebToolPackageSource = dotnetDebToolPackageSource;

            _consumingProjectDirectory = Path.Combine(_intermediateDirectory, s_toolConsumerProjectName);

            InitializeDotnetDebTool();
        }

        public void CreateDeb(
            string debianConfigJsonFile,
            string packageName,
            string packageVersion,
            string inputBinariesDirectory,
            Dictionary<string, string> debianConfigVariables,
            string outputFile,
            string manpagesDirectory = null)
        {
            string debIntermediatesDirectory = Path.Combine(_intermediateDirectory, packageName, packageVersion);
            if (Directory.Exists(debIntermediatesDirectory))
            {
                FS.Rmdir(debIntermediatesDirectory);
            }
            FS.Mkdirp(debIntermediatesDirectory);

            string layoutDirectory = Path.Combine(debIntermediatesDirectory, "debianLayoutDirectory");
            var debianLayoutDirectories = new DebianLayoutDirectories(layoutDirectory);

            CreateEmptyDebianLayout(debianLayoutDirectories);
            CopyFilesToDebianLayout(
                debianLayoutDirectories, 
                debianConfigJsonFile, 
                inputBinariesDirectory, 
                manpagesDirectory);
            ReplaceDebianConfigJsonVariables(debianLayoutDirectories, debianConfigVariables);
            CreateDebianPackage(debianLayoutDirectories, debIntermediatesDirectory, outputFile, packageName, packageVersion);
        }

        private void CreateEmptyDebianLayout(DebianLayoutDirectories layoutDirectories)
        {
            if (Directory.Exists(layoutDirectories.LayoutDirectory))
            {
                FS.Rmdir(layoutDirectories.LayoutDirectory);
            }
            Directory.CreateDirectory(layoutDirectories.LayoutDirectory);

            Directory.CreateDirectory(layoutDirectories.AbsolutePlacement);
            Directory.CreateDirectory(layoutDirectories.PackageRoot);
            Directory.CreateDirectory(layoutDirectories.Samples);
            Directory.CreateDirectory(layoutDirectories.Docs);
        }

        private void CopyFilesToDebianLayout(
            DebianLayoutDirectories layoutDirectories,
            string debianConfigFile,
            string inputBinariesDirectory,
            string manpagesDirectory)
        {
            FS.CopyRecursive(inputBinariesDirectory, layoutDirectories.PackageRoot);

            if (manpagesDirectory != null)
            {
                FS.CopyRecursive(manpagesDirectory, layoutDirectories.Docs);
            }

            File.Copy(debianConfigFile,
                Path.Combine(layoutDirectories.LayoutDirectory, s_debianConfigJsonFileName));
        }

        private void ReplaceDebianConfigJsonVariables(
            DebianLayoutDirectories debianLayoutDirectories, 
            Dictionary<string, string> debianConfigVariables)
        {
            var debianConfigFile = Path.Combine(debianLayoutDirectories.LayoutDirectory, s_debianConfigJsonFileName);
            var debianConfigFileContents = File.ReadAllText(debianConfigFile);

            foreach (var variable in debianConfigVariables)
            {
                var variableToken = $"%{variable.Key}%";
                debianConfigFileContents = debianConfigFileContents.Replace(variableToken, variable.Value);
            }
            File.WriteAllText(debianConfigFile, debianConfigFileContents);
        }

        private void CreateDebianPackage(
            DebianLayoutDirectories debianLayoutDirectories,
            string intermediatesOutputDirectory,
            string outputFile,
            string packageName,
            string packageVersion)
        {
            _dotnet.Exec("deb-tool",
                "-i", debianLayoutDirectories.LayoutDirectory,
                "-o", intermediatesOutputDirectory,
                "-n", packageName,
                "-v", packageVersion)
                .WorkingDirectory(_consumingProjectDirectory)
                .Execute()
                .EnsureSuccessful();

            var debianFile = Directory.EnumerateFiles(intermediatesOutputDirectory, "*.deb").First();
            File.Copy(debianFile, outputFile, true);
        }

        private void InitializeDotnetDebTool()
        {
            CreateAndRestoreToolConsumingProject();
        }

        private void CreateAndRestoreToolConsumingProject()
        {
            if (Directory.Exists(_consumingProjectDirectory))
            {
                FS.Rmdir(_consumingProjectDirectory);
            }
            FS.Mkdirp(_consumingProjectDirectory);

            var projectJsonFile = Path.Combine(_consumingProjectDirectory, "project.json");
            File.WriteAllText(projectJsonFile, GetDotnetDebProjectJsonContents());

            Command restore = _dotnetDebToolPackageSource == null 
                ? _dotnet.Restore("--disable-parallel")
                : _dotnet.Restore("-f", $"{_dotnetDebToolPackageSource}", "--disable-parallel");

            restore
                .WorkingDirectory(Path.GetDirectoryName(projectJsonFile))
                .Execute()
                .EnsureSuccessful();
        }

        private string GetDotnetDebProjectJsonContents()
        {
            var projectJson = new StringBuilder();
            projectJson.Append("{");
            projectJson.Append($"  \"version\": \"1.0.0-*\",");
            projectJson.Append($"  \"name\": \"{s_toolConsumerProjectName}\",");
            projectJson.Append("  \"frameworks\": { \"netcoreapp1.0\": { } },");
            projectJson.Append($"  \"tools\": {{ \"{s_dotnetDebToolPackageId}\": \"{_dotnetDebToolVersion}\" }},");
            projectJson.Append("}");

            return projectJson.ToString();
        }

        private class DebianLayoutDirectories
        {
            private string _layoutDirectory;

            public DebianLayoutDirectories(string layoutDirectory)
            {
                _layoutDirectory = layoutDirectory;
            }

            public string LayoutDirectory => _layoutDirectory;
            public string PackageRoot => Path.Combine(_layoutDirectory, "package_root");
            public string AbsolutePlacement => Path.Combine(_layoutDirectory, "$");
            public string Samples => Path.Combine(_layoutDirectory, "samples");
            public string Docs => Path.Combine(_layoutDirectory, "docs");
        }
    }
}
