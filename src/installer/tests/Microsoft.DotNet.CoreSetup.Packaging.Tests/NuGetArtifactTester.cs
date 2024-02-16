// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Packaging.Tests
{
    public class NuGetArtifactTester : IDisposable
    {
        public static NuGetArtifactTester Open(
            RepoDirectoriesProvider dirs,
            string project,
            string id = null)
        {
            var tester = OpenOrNull(dirs, project, id);
            Assert.NotNull(tester);
            return tester;
        }

        public static NuGetArtifactTester OpenOrNull(
            RepoDirectoriesProvider dirs,
            string project,
            string id = null)
        {
            id = id ?? project;

            string nupkgPath = Path.Combine(
                dirs.BaseArtifactsFolder,
                "packages",
                TestContext.Configuration,
                "Shipping",
                $"{id}.{TestContext.MicrosoftNETCoreAppVersion}.nupkg");

            // If the nuspec exists, the nupkg should exist.
            Assert.True(File.Exists(nupkgPath));

            return new NuGetArtifactTester(nupkgPath);
        }

        public PackageIdentity Identity { get; }
        public NuGetVersion PackageVersion { get; }

        private readonly PackageArchiveReader _reader;

        public NuGetArtifactTester(string file)
        {
            _reader = new PackageArchiveReader(ZipFile.Open(file, ZipArchiveMode.Read));
            Identity = _reader.NuspecReader.GetIdentity();
            PackageVersion = _reader.NuspecReader.GetVersion();
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public void IsTargetingPack()
        {
            IsFrameworkPack();

            Assert.NotEmpty(_reader.GetFiles("ref"));
            Assert.Empty(_reader.GetFiles("runtimes"));
            Assert.Empty(_reader.GetFiles("lib"));

            ContainsFrameworkList("FrameworkList.xml");
        }

        public void IsTargetingPackForPlatform()
        {
            IsFrameworkPack();

            HasGoodPlatformManifest();
        }

        public void IsAppHostPack()
        {
            IsRuntimeSpecificPack();
        }

        public void IsRuntimePack()
        {
            IsRuntimeSpecificPack();

            HasOnlyTheseDataFiles(
                "data/RuntimeList.xml");

            ContainsFrameworkList("RuntimeList.xml");
        }

        public void HasOnlyTheseDataFiles(params string[] expectedDataFiles)
        {
            HashSet<string> dataFileSet = _reader.GetFiles("data").ToHashSet();

            Assert.True(
                dataFileSet.SetEquals(expectedDataFiles),
                "Invalid set of data files: " +
                    $"expected '{string.Join(", ", expectedDataFiles)}', " +
                    $"actual '{string.Join(", ", dataFileSet)}'");
        }

        public void HasGoodPlatformManifest()
        {
            string platformManifestContent = ReadEntryContent(
                _reader.GetEntry("data/PlatformManifest.txt"));

            // Sanity: check if the manifest has some content.
            Assert.Contains(".dll", platformManifestContent);

            // Check that the lines contain the package ID where they're supposed to.
            foreach (var parts in platformManifestContent
                .Split('\r', '\n')
                .Select(line => line.Split("|"))
                .Where(parts => parts.Length > 1))
            {
                Assert.True(
                    parts[1] == Identity.Id,
                    $"Platform manifest package id column '{parts[1]}' doesn't match " +
                        $"actual package id '{Identity.Id}'");
            }
        }

        public string ReadEntryContent(string entry)
        {
            return ReadEntryContent(_reader.GetEntry(entry));
        }

        public XDocument ReadEntryXDocument(string entry)
        {
            return ReadEntryXDocument(_reader.GetEntry(entry));
        }

        private void IsFrameworkPack()
        {
            Assert.Empty(_reader.GetPackageDependencies());

            var expectedTypes = new[] { new PackageType("DotnetPlatform", new Version(0, 0)) };
            var types = _reader.GetPackageTypes().ToArray();
            Assert.Equal(expectedTypes, types);
        }

        private void IsRuntimeSpecificPack()
        {
            IsFrameworkPack();

            Assert.Empty(_reader.GetFiles("ref"));
            Assert.NotEmpty(_reader.GetFiles("runtimes"));
            Assert.Empty(_reader.GetFiles("lib"));
        }

        private void ContainsFrameworkList(string name)
        {
            XDocument frameworkList = ReadEntryXDocument(
                _reader.GetEntry($"data/{name}"));

            XElement[] frameworkListFiles = frameworkList
                .Element("FileList")
                .Elements("File")
                .ToArray();

            // Sanity: check if the list has some content.
            Assert.NotEmpty(frameworkListFiles);
        }

        private static string ReadEntryContent(ZipArchiveEntry entry)
        {
            using (var reader = new StreamReader(entry.Open()))
            {
                return reader.ReadToEnd();
            }
        }

        private static XDocument ReadEntryXDocument(ZipArchiveEntry entry)
        {
            return XDocument.Parse(ReadEntryContent(entry));
        }
    }
}
