// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.NET.HostModel.Bundle;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class SingleFileTestApp : TestArtifact
    {
        public string AppName { get; }
        public string NonBundledLocation => builtApp.Location;

        private readonly TestApp builtApp;
        private readonly bool selfContained;

        public SingleFileTestApp(string appName, bool selfContained, string location)
            : base(location)
        {
            AppName = appName;
            this.selfContained = selfContained;

            builtApp = new TestApp(Path.Combine(Location, "builtApp"), AppName);
            Directory.CreateDirectory(builtApp.Location);
            PopulateBuiltAppDirectory();
        }

        /// <summary>
        /// Create a framework-dependent single-file test app from pre-built output of <paramref name="appName"/>.
        /// </summary>
        /// <param name="appName">Name of pre-built app</param>
        /// <returns>
        /// The <paramref name="appName"/> is expected to be in <see cref="TestContext.TestAssetsOutput"/>
        /// and have been built as framework-dependent
        /// </returns>
        public static SingleFileTestApp CreateFrameworkDependent(string appName)
            => Create(appName, selfContained: false);

        /// <summary>
        /// Create a self-contained single-file test app from pre-built output of <paramref name="appName"/>.
        /// </summary>
        /// <param name="appName">Name of pre-built app</param>
        /// <returns>
        /// The <paramref name="appName"/> is expected to be in <see cref="TestContext.TestAssetsOutput"/>
        /// and have been built as framework-dependent
        /// </returns>
        public static SingleFileTestApp CreateSelfContained(string appName)
            => Create(appName, selfContained: true);

        private static SingleFileTestApp Create(string appName, bool selfContained)
        {
            var (location, parentPath) = GetNewTestArtifactPath(appName);
            return new SingleFileTestApp(appName, selfContained, location)
            {
                DirectoryToDelete = parentPath
            };
        }

        public static IReadOnlyList<FileSpec> GetRuntimeFilesToBundle()
        {
            var runtimeAssemblies = Binaries.GetRuntimeFiles().Assemblies;
            List<FileSpec> fileSpecs = new List<FileSpec>();
            foreach (var asset in runtimeAssemblies)
            {
                fileSpecs.Add(new FileSpec(asset, Path.GetFileName(asset)));
            }

            fileSpecs.Sort((a, b) => string.CompareOrdinal(a.BundleRelativePath, b.BundleRelativePath));
            return fileSpecs;
        }

        public string Bundle(BundleOptions options = BundleOptions.None, Version? bundleVersion = null)
        {
            return Bundle(options, out _, bundleVersion);
        }

        public string Bundle(BundleOptions options, out Manifest manifest, Version? bundleVersion = null)
        {
            string bundleDirectory = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(Location, "bundle"));
            var bundler = new Bundler(
                Binaries.GetExeFileNameForCurrentPlatform(AppName),
                bundleDirectory,
                options,
                targetFrameworkVersion: bundleVersion,
                macosCodesign: true);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(builtApp.Location, searchPattern: "*", searchOption: SearchOption.AllDirectories);
            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, Path.GetRelativePath(builtApp.Location, file)));
            }

            // If this is a self-contained app, add the runtime assemblies to the bundle
            if (selfContained)
            {
                fileSpecs.AddRange(GetRuntimeFilesToBundle());
            }

            // Sort the file specs to keep the bundle construction deterministic.
            fileSpecs.Sort((a, b) => string.CompareOrdinal(a.BundleRelativePath, b.BundleRelativePath));
            var singleFile = bundler.GenerateBundle(fileSpecs);

            // Copy excluded files to the bundle directory. This mimics the SDK behaviour where
            // files excluded by the bundler are copied to the publish directory.
            foreach (FileSpec spec in fileSpecs)
            {
                if (!spec.Excluded)
                    continue;

                var outputFilePath = Path.Combine(bundleDirectory, spec.BundleRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                File.Copy(spec.SourcePath, outputFilePath, true);
            }

            manifest = bundler.BundleManifest;
            return singleFile;
        }

        public string GetNewExtractionRootPath()
        {
            return SharedFramework.CalculateUniqueTestDirectory(Path.Combine(Location, "extract"));
        }

        public DirectoryInfo GetExtractionDir(string root, Manifest manifest)
        {
            return new DirectoryInfo(Path.Combine(root, Name, manifest.BundleID));
        }

        private void PopulateBuiltAppDirectory()
        {
            // Copy the compiled app output - the app is expected to have been built as framework-dependent
            TestArtifact.CopyRecursive(
                Path.Combine(TestContext.TestAssetsOutput, AppName),
                builtApp.Location);

            // Remove any runtimeconfig.json or deps.json - we will be creating new ones
            File.Delete(builtApp.RuntimeConfigJson);
            File.Delete(builtApp.DepsJson);

            var shortVersion = TestContext.Tfm[3..]; // trim "net" from beginning
            var builder = NetCoreAppBuilder.ForNETCoreApp(AppName, TestContext.TargetRID, shortVersion);

            // Update the .runtimeconfig.json
            builder.WithRuntimeConfig(c =>
            {
                c.WithTfm(TestContext.Tfm);
                c = selfContained
                    ? c.WithIncludedFramework(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion)
                    : c.WithFramework(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion);
            });

            // Add runtime libraries and assets for generating the .deps.json.
            // Native libraries are excluded - matches DropFromSingleFile setting in RuntimeList.xml.
            // All assets are configured to not be on disk as this app is just for bundling purposes.
            // We can grab the runtime assets from their original location and avoid copying everything
            builder.WithProject(AppName, "1.0.0", p => p
                .WithAssemblyGroup(string.Empty, g => g
                    .WithAsset(Path.GetFileName(builtApp.AppDll), f => f.NotOnDisk())));
            if (selfContained)
            {
                builder.WithRuntimePack($"{Constants.MicrosoftNETCoreApp}.Runtime.{TestContext.TargetRID}", TestContext.MicrosoftNETCoreAppVersion, l => l
                    .WithAssemblyGroup(string.Empty, g =>
                    {
                        foreach (var file in Binaries.GetRuntimeFiles().Assemblies)
                        {
                            var fileVersion = FileVersionInfo.GetVersionInfo(file).FileVersion;
                            var asmVersion = AssemblyName.GetAssemblyName(file).Version!.ToString();
                            g.WithAsset(
                                Path.GetFileName(file),
                                f => f.WithVersion(asmVersion, fileVersion!).NotOnDisk());
                        }
                    }));
            }

            // Write out the app
            builder.Build(builtApp);

            // Create the apphost for the app
            if (selfContained)
            {
                builtApp.CreateSingleFileHost();
            }
            else
            {
                builtApp.CreateAppHost();
            }
        }
    }
}
