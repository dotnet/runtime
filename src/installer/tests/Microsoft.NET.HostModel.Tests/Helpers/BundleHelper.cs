// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace BundleTests.Helpers
{
    public static class BundleHelper
    {
        public const string DotnetBundleExtractBaseEnvVariable = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";
        public const string CoreServicingEnvVariable = "CORE_SERVICING";

        public static string GetHostPath(TestProjectFixture fixture)
        {
            return Path.Combine(GetPublishPath(fixture), GetHostName(fixture));
        }

        public static string GetAppPath(TestProjectFixture fixture)
        {
            return Path.Combine(GetPublishPath(fixture), GetAppName(fixture));
        }

        public static string GetDepsJsonPath(TestProjectFixture fixture)
        {
            return Path.Combine(GetPublishPath(fixture), $"{GetAppBaseName(fixture)}.deps.json");
        }

        public static string GetPublishedSingleFilePath(TestProjectFixture fixture)
        {
            return GetHostPath(fixture);
        }

        public static string GetHostName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppExe);
        }

        public static string GetAppName(TestProjectFixture fixture)
        {
            return Path.GetFileName(fixture.TestProject.AppDll);
        }

        public static string GetAppBaseName(TestProjectFixture fixture)
        {
            return Path.GetFileNameWithoutExtension(GetAppName(fixture));
        }

        public static string[] GetBundledFiles(TestProjectFixture fixture)
        {
            string appBaseName = GetAppBaseName(fixture);
            return new string[] { $"{appBaseName}.dll", $"{appBaseName}.deps.json", $"{appBaseName}.runtimeconfig.json" };
        }

        public static string[] GetExtractedFiles(TestProjectFixture fixture, BundleOptions bundleOptions)
        {
            switch (bundleOptions & ~BundleOptions.EnableCompression)
            {
                case BundleOptions.None:
                case BundleOptions.BundleOtherFiles:
                case BundleOptions.BundleSymbolFiles:
                    throw new ArgumentException($"Bundle option {bundleOptions} doesn't extract any files to disk.");

                case BundleOptions.BundleAllContent:
                    return Directory.GetFiles(GetPublishPath(fixture))
                        .Select(f => Path.GetFileName(f))
                        .Except(GetFilesNeverExtracted(fixture)).ToArray();

                case BundleOptions.BundleNativeBinaries:
                    return new string[] { Path.GetFileName(fixture.TestProject.CoreClrDll) };

                default:
                    throw new ArgumentException("Unsupported bundle option.");
            }
        }

        public static string[] GetFilesNeverExtracted(TestProjectFixture fixture)
        {
            string appBaseName = GetAppBaseName(fixture);
            return new string[] { $"{appBaseName}",
                                  $"{appBaseName}.dll",
                                  $"{appBaseName}.exe",
                                  $"{appBaseName}.pdb",
                                  $"{appBaseName}.runtimeconfig.dev.json",
                                  Path.GetFileName(fixture.TestProject.HostFxrDll),
                                  Path.GetFileName(fixture.TestProject.HostPolicyDll) };
        }

        public static string GetPublishPath(TestProjectFixture fixture)
        {
            return Path.Combine(fixture.TestProject.ProjectDirectory, "publish");
        }

        public static DirectoryInfo GetBundleDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(Path.Combine(fixture.TestProject.ProjectDirectory, "bundle"));
        }

        public static string  GetExtractionRootPath(TestProjectFixture fixture)
        {
            return Path.Combine(fixture.TestProject.ProjectDirectory, "extract");
        }

        public static DirectoryInfo GetExtractionRootDir(TestProjectFixture fixture)
        {
            return Directory.CreateDirectory(GetExtractionRootPath(fixture));
        }

        public static string GetExtractionPath(TestProjectFixture fixture, Bundler bundler)
        {
            return Path.Combine(GetExtractionRootPath(fixture), GetAppBaseName(fixture), bundler.BundleManifest.BundleID);

        }

        public static DirectoryInfo GetExtractionDir(TestProjectFixture fixture, Bundler bundler)
        {
            return new DirectoryInfo(GetExtractionPath(fixture, bundler));
        }

        public static OSPlatform GetTargetOS(string runtimeIdentifier)
        {
            return runtimeIdentifier.Split('-')[0] switch {
                "win" => OSPlatform.Windows,
                "osx" => OSPlatform.OSX,
                "linux" => OSPlatform.Linux,
                _ => throw new ArgumentException(nameof(runtimeIdentifier))
            };
        }

        public static Architecture GetTargetArch(string runtimeIdentifier)
        {
            return runtimeIdentifier.EndsWith("-x64") || runtimeIdentifier.Contains("-x64-") ? Architecture.X64 :
                   runtimeIdentifier.EndsWith("-x86") || runtimeIdentifier.Contains("-x86-") ? Architecture.X86 :
                   runtimeIdentifier.EndsWith("-arm64") || runtimeIdentifier.Contains("-arm64-") ? Architecture.Arm64 :
                   runtimeIdentifier.EndsWith("-arm") || runtimeIdentifier.Contains("-arm-") ? Architecture.Arm :
                   throw new ArgumentException(nameof (runtimeIdentifier));
        }

        /// Generate a bundle containind the (embeddable) files in sourceDir
        public static string GenerateBundle(Bundler bundler, string sourceDir, string outputDir, bool copyExludedFiles=true)
        {
            // Convert sourceDir to absolute path
            sourceDir = Path.GetFullPath(sourceDir);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(sourceDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, Path.GetRelativePath(sourceDir, file)));
            }

            var singleFile = bundler.GenerateBundle(fileSpecs);

            if (copyExludedFiles)
            {
                foreach (var spec in fileSpecs)
                {
                    if (spec.Excluded)
                    {
                        var outputFilePath = Path.Combine(outputDir, spec.BundleRelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                        File.Copy(spec.SourcePath, outputFilePath, true);
                    }
                }
            }

            return singleFile;
        }

        // Bundle to a single-file
        // In several tests, the single-file bundle is created explicitly using Bundle API
        // instead of the SDK via /p:PublishSingleFile=true.
        // This is necessary when the test needs the latest changes in the AppHost, 
        // which may not (yet) be available in the SDK.
        public static Bundler BundleApp(TestProjectFixture fixture,
                                        out string singleFile,
                                        BundleOptions options = BundleOptions.None,
                                        Version targetFrameworkVersion = null,
                                        bool copyExcludedFiles = true)
        {
            var hostName = GetHostName(fixture);
            string publishPath = GetPublishPath(fixture);
            var bundleDir = GetBundleDir(fixture);
            var targetOS = GetTargetOS(fixture.CurrentRid);
            var targetArch = GetTargetArch(fixture.CurrentRid);

            var bundler = new Bundler(hostName, bundleDir.FullName, options, targetOS, targetArch, targetFrameworkVersion, macosCodesign: true);
            singleFile = GenerateBundle(bundler, publishPath, bundleDir.FullName, copyExcludedFiles);

            return bundler;
        }

        public static string BundleApp(TestProjectFixture fixture,
                                       BundleOptions options = BundleOptions.None,
                                       Version targetFrameworkVersion = null)
        {
            string singleFile;
            BundleApp(fixture, out singleFile, options, targetFrameworkVersion);
            return singleFile;
        }

        public static Bundler Bundle(TestProjectFixture fixture, BundleOptions options = BundleOptions.None)
        {
            string singleFile;
            return BundleApp(fixture, out singleFile, options, copyExcludedFiles:false);
        }

        public static void AddLongNameContentToAppWithSubDirs(TestProjectFixture fixture)
        {
            // For tests using the AppWithSubDirs, One of the sub-directories with a really long name
            // is generated during test-runs rather than being checked in as a test asset.
            // This prevents git-clone of the repo from failing if long-file-name support is not enabled on windows.
            var longDirName = "This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation";
            var longDirPath = Path.Combine(fixture.TestProject.ProjectDirectory, "Sentence", longDirName);
            Directory.CreateDirectory(longDirPath);
            using (var writer = File.CreateText(Path.Combine(longDirPath, "word")))
            {
                writer.Write(".");
            }
        }

        public static void AddEmptyContentToApp(TestProjectFixture fixture)
        {
            XDocument projectDoc = XDocument.Load(fixture.TestProject.ProjectFile);
            projectDoc.Root.Add(
                new XElement("ItemGroup",
                    new XElement("Content",
                        new XAttribute("Include", "empty.txt"),
                        new XElement("CopyToOutputDirectory", "PreserveNewest"))));
            projectDoc.Save(fixture.TestProject.ProjectFile);
            File.WriteAllBytes(Path.Combine(fixture.TestProject.Location, "empty.txt"), new byte[0]);
        }

    }
}
