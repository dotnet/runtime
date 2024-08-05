// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WorkloadTests : TestMainJsTestBase
    {
        public WorkloadTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Not applicable on windows")]
        public void FilesInUnixFilesPermissionsXmlExist()
        {
            // not doing any project generation here
            _enablePerTestCleanup = false;

            // find all the UnixFilePermissions ..
            string packsDir = Path.Combine(Path.GetDirectoryName(s_buildEnv.DotNet)!, "packs");
            Assert.True(Directory.Exists(packsDir), $"Could not find packs directory {packsDir}");

            var unixPermFiles = Directory.EnumerateFiles(packsDir, "UnixFilePermissions.xml", new EnumerationOptions { RecurseSubdirectories = true });
            foreach (string unixPermFile in unixPermFiles)
            {
                Assert.True(File.Exists(unixPermFile), $"Could not find {unixPermFile}");
                FileList? list = FileList.Deserialize(unixPermFile);
                if (list == null)
                    throw new Exception($"Could not read unix permissions file {unixPermFile}");

                // File is in <packs>/<packName>/<version>/data/UnixFilePermissions.xml
                // and <FileList><File Path="tools/bin/2to3" Permission="755" />
                string thisPackDir = Path.Combine(Path.GetDirectoryName(unixPermFile)!, "..");
                foreach (FileListFile flf in list.File)
                {
                    if (flf.Path == null)
                        throw new Exception($"Path for FileListFile should not be null. xml: {unixPermFile}");

                    var targetFile = Path.Combine(thisPackDir, flf.Path);
                    Assert.True(File.Exists(targetFile), $"Expected file {targetFile} to exist in the pack, as it is referenced in {unixPermFile}");
                }
            }

            // We don't install the cross compiler pack from nupkg, so we don't
            // have the unixFilePermissions for that
            // Expect just the emscripten ones here for now

            int expectedPermFileCount = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 4 : 5;

            // extract pack names from ./artifacts/bin/dotnet-latest/packs/Microsoft.NET.Runtime.Emscripten.3.1.34.Sdk.linux-x64/8.0.0-rtm.23470.1/data/UnixFilePermissions.xml
            var packNames = unixPermFiles.Select(f => Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(f))))).ToArray();

            Assert.Contains(packNames, name => Regex.IsMatch(name!, "Microsoft\\.NET\\.Runtime\\.Emscripten\\.[0-9\\.]+\\.Cache\\."));
            Assert.Contains(packNames, name => Regex.IsMatch(name!, "Microsoft\\.NET\\.Runtime\\.Emscripten\\.[0-9\\.]+\\.Node\\."));
            Assert.Contains(packNames, name => Regex.IsMatch(name!, "Microsoft\\.NET\\.Runtime\\.Emscripten\\.[0-9\\.]+\\.Sdk\\."));

            // linux doesn't have Emscripten.Python package, so only 2 there
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Assert.Contains(packNames, name => Regex.IsMatch(name!, "Microsoft\\.NET\\.Runtime\\.Emscripten\\.[0-9\\.]+\\.Python\\."));

            Assert.Contains(packNames, name => Regex.IsMatch(name!, "Microsoft.NETCore.App.Runtime.AOT\\..*\\.Cross.browser-wasm"));
        }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class FileList
    {
        private FileListFile[]? fileField;

        [XmlElement("File")]
        public FileListFile[] File
        {
            get => fileField ?? Array.Empty<FileListFile>();
            set => fileField = value;
        }

        public static FileList? Deserialize(string pathToXml)
        {
            var serializer = new XmlSerializer(typeof(FileList));

            using var fs = new FileStream(pathToXml, FileMode.Open, FileAccess.Read);
            var reader = XmlReader.Create(fs);
            FileList? fileList = (FileList?)serializer.Deserialize(reader);
            return fileList;
        }
    }

    // From https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/NugetPackageDownloader/WorkloadUnixFilePermissionsFileList.cs
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class FileListFile
    {
        private string? pathField;

        private string? permissionField;

        [XmlAttribute]
        public string? Path
        {
            get => pathField;
            set => pathField = value;
        }

        [XmlAttribute]
        public string? Permission
        {
            get => permissionField;
            set => permissionField = value;
        }
    }
}
