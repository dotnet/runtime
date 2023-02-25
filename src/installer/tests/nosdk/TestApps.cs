
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using BundleTests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    internal sealed class AppWithSubDirs : TestArtifact
    {
        private const string AppName = nameof(AppWithSubDirs);

        private static RepoDirectoriesProvider s_provider => RepoDirectoriesProvider.Default;

        private readonly bool selfContained;
        private readonly TestApp compiledApp;

        private AppWithSubDirs(bool selfContained)
            : base(GetNewTestArtifactPath(AppName))
        {
            this.selfContained = selfContained;

            // Compile and write out the app to a sub-directory
            compiledApp = new TestApp(Path.Combine(Location, "compiled"), AppName);
            Directory.CreateDirectory(compiledApp.Location);
            WriteAppToDirectory(compiledApp.Location, selfContained);
        }

        public static AppWithSubDirs CreateFrameworkDependent()
            => new AppWithSubDirs(selfContained: false);

        public static AppWithSubDirs CreateSelfContained()
            => new AppWithSubDirs(selfContained: true);

        public string Bundle(BundleOptions options, Version? bundleVersion = null)
        {
            string bundleDirectory = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(Location, "bundle"));
            string singleFile = BundleApp(
                options,
                RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AppName),
                compiledApp.Location,
                bundleDirectory,
                selfContained,
                bundleVersion);

            if (options != BundleOptions.BundleAllContent)
            {
                CopySentenceSubDir(bundleDirectory);
            }

            return singleFile;
        }

        private static string BundleApp(
            BundleOptions options,
            string hostName,
            string srcDir,
            string outDir,
            bool selfContained,
            Version? bundleVersion)
        {
            var rid = s_provider.TargetRID;

            var bundler = new Bundler(
                hostName,
                outDir,
                options,
                BundleHelper.GetTargetOS(rid),
                BundleHelper.GetTargetArch(rid),
                targetFrameworkVersion: bundleVersion,
                macosCodesign: true);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(srcDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach (var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, Path.GetRelativePath(srcDir, file)));
            }

            // If this is a self-contained app, add the runtime assemblies to the bundle
            if (selfContained)
            {
                var runtimeAssemblies = GetRuntimeAssemblies();
                foreach (var asset in runtimeAssemblies)
                {
                    fileSpecs.Add(new FileSpec(asset, Path.GetFileName(asset)));
                }
            }

            return bundler.GenerateBundle(fileSpecs);
        }

        private static ImmutableArray<MetadataReference> _refAssemblies = default;
        public static ImmutableArray<MetadataReference> RefAssemblies
        {
            get
            {
                if (_refAssemblies.IsDefault)
                {
                    var references = Directory.GetFiles(s_provider.RefPackPath, "*.dll")
                        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                        .ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(
                        ref _refAssemblies,
                        references);
                }
                return _refAssemblies;
            }
        }

        private static IEnumerable<string> GetRuntimeAssemblies()
        {
            var runtimePackDir = new DotNetCli(s_provider.BuiltDotnet).GreatestVersionSharedFxPath;
            return Directory.GetFiles(runtimePackDir, "*.dll")
                .Where(f =>
                {
                    using (var fs = File.OpenRead(f))
                    using (var peReader = new PEReader(fs))
                    {
                        return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
                    }
                });
        }

        private void WriteAppToDirectory(string outputDirectory, bool selfContained)
        {
            // Compile the app assembly
            var srcPath = Path.Combine(s_provider.TestAssetsFolder, "TestProjects", AppName, "Program.cs");
            var comp = CSharpCompilation.Create(
                assemblyName: AppName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(File.ReadAllText(srcPath)) },
                references: RefAssemblies);
            var emitResult = comp.Emit(compiledApp.AppDll);
            Assert.True(emitResult.Success);

            var shortVersion = s_provider.Tfm[3..]; // trim "net" from beginning
            var builder = NetCoreAppBuilder.ForNETCoreApp(AppName, s_provider.TargetRID, shortVersion);

            // Update the .runtimeconfig.json
            builder.WithRuntimeConfig(c =>
            {
                c.WithTfm(s_provider.Tfm);
                c = selfContained
                    ? c.WithIncludedFramework(Constants.MicrosoftNETCoreApp, s_provider.MicrosoftNETCoreAppVersion)
                    : c.WithFramework(Constants.MicrosoftNETCoreApp, s_provider.MicrosoftNETCoreAppVersion);
            });

            // Add runtime libraries and assets for generating the .deps.json.
            // All assets are configured to not be on disk as this app is just for bundling purposes.
            // We can grab the runtime assets from their original location and avoid copying everything
            builder.WithProject(AppName, "1.0.0", p => p
                .WithAssemblyGroup(string.Empty, g => g
                    .WithAsset(Path.GetFileName(compiledApp.AppDll), f => f.NotOnDisk())));
            if (selfContained)
            {
                builder = builder
                    .WithRuntimePack($"{Constants.MicrosoftNETCoreApp}.Runtime.{s_provider.TargetRID}", s_provider.MicrosoftNETCoreAppVersion, l => l
                        .WithAssemblyGroup(string.Empty, g =>
                        {
                            foreach (var file in GetRuntimeAssemblies())
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
            builder.Build(compiledApp);

            // Create the apphost for the app
            HostWriter.CreateAppHost(
                GetAppHostPath(selfContained),
                Path.Combine(outputDirectory, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AppName)),
                $"{AppName}.dll",
                windowsGraphicalUserInterface: false,
                assemblyToCopyResourcesFrom: null,
                enableMacOSCodeSign: true);

            // Copy over extra content files
            CopySentenceSubDir(outputDirectory);
        }

        private static string GetAppHostPath(
            bool selfContained)
        {
            string hostName = selfContained ? "singlefilehost" : "apphost";
            return Path.Combine(s_provider.HostArtifacts, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(hostName));
        }

        // Specific to the AppWithSubDirs app
        private static void CopySentenceSubDir(string targetDir)
        {
            TestArtifact.CopyRecursive(
                Path.Combine(
                    s_provider.TestAssetsFolder,
                    "TestProjects",
                    "AppWithSubDirs",
                    "Sentence"),
                Path.Combine(targetDir, "Sentence"),
                overwrite: false);

            BundleHelper.AddLongNameContentToAppWithSubDirs(targetDir);
        }
    }
}
