// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestApp : TestArtifact
    {
        public string AppDll { get; private set; }
        public string AppExe { get; private set; }
        public string DepsJson { get; private set; }
        public string RuntimeConfigJson { get; private set; }
        public string RuntimeDevConfigJson { get; private set; }
        public string HostPolicyDll { get; private set; }
        public string HostFxrDll { get; private set; }
        public string CoreClrDll { get; private set; }

        public string AssemblyName { get; }

        public TestApp(string basePath, string assemblyName = null)
            : base(basePath)
        {
            AssemblyName = assemblyName ?? Name;
            LoadAssets();
        }

        private TestApp(TestApp source)
            : base(source)
        {
            AssemblyName = source.AssemblyName;
            LoadAssets();
        }

        public static TestApp CreateEmpty(string name)
        {
            var (location, parentPath) = GetNewTestArtifactPath(name);
            return new TestApp(location)
            {
                DirectoryToDelete = parentPath
            };
        }

        public void PopulateFrameworkDependent(string fxName, string fxVersion, Action<NetCoreAppBuilder> customizer = null)
        {
            var builder = NetCoreAppBuilder.PortableForNETCoreApp(this);

            // Update the .runtimeconfig.json
            builder.WithRuntimeConfig(c => c.WithFramework(fxName, fxVersion));

            // Add main project assembly
            builder.WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));

            builder.WithCustomizer(customizer);

            // Write out the app
            builder.Build(this);
        }

        public enum MockedComponent
        {
            None,       // Product components
            CoreClr,    // Mock coreclr
            HostPolicy, // Mock hostpolicy
        }

        public void PopulateSelfContained(MockedComponent mock, Action<NetCoreAppBuilder> customizer = null)
        {
            var builder = NetCoreAppBuilder.ForNETCoreApp(Name, RepoDirectoriesProvider.Default.TargetRID);

            // Update the .runtimeconfig.json
            builder.WithRuntimeConfig(c =>
                c.WithIncludedFramework(Constants.MicrosoftNETCoreApp, RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion));

            // Add main project assembly
            builder.WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));

            // Add runtime libraries and assets
            builder.WithRuntimePack($"{Constants.MicrosoftNETCoreApp}.Runtime.{RepoDirectoriesProvider.Default.TargetRID}", RepoDirectoriesProvider.Default.MicrosoftNETCoreAppVersion, l =>
            {
                if (mock == MockedComponent.None)
                {
                    // All product components
                    var (assemblies, nativeLibraries) = GetRuntimeFiles();
                    l.WithAssemblyGroup(string.Empty, g =>
                    {
                        foreach (var file in assemblies)
                        {
                            var fileVersion = FileVersionInfo.GetVersionInfo(file).FileVersion;
                            var asmVersion = System.Reflection.AssemblyName.GetAssemblyName(file).Version!.ToString();
                            g.WithAsset(Path.GetFileName(file),
                                f => f.WithVersion(asmVersion, fileVersion!).CopyFromFile(file));
                        }
                    });
                    l.WithNativeLibraryGroup(string.Empty, g =>
                    {
                        // ./hostfxr - real component and will load hostpolicy
                        g.WithAsset(Binaries.HostFxr.FileName,
                            f => f.CopyFromFile(Binaries.HostFxr.FilePath));

                        foreach (var file in nativeLibraries)
                        {
                            g.WithAsset(Path.GetFileName(file),
                                f => f.CopyFromFile(file));
                        }
                    });
                }
                else if (mock == MockedComponent.CoreClr)
                {
                    l.WithNativeLibraryGroup(string.Empty, g => g
                        // ./hostfxr - real component and will load hostpolicy
                        .WithAsset(Binaries.HostFxr.FileName,
                            f => f.CopyFromFile(Binaries.HostFxr.FilePath))
                        // ./hostpolicy - real component and will load coreclr
                        .WithAsset(Binaries.HostPolicy.FileName,
                            f => f.CopyFromFile(Binaries.HostPolicy.FilePath))
                        // ./coreclr - mocked component
                        .WithAsset(Binaries.CoreClr.FileName,
                            f => f.CopyFromFile(Binaries.CoreClr.MockPath)));
                }
                else if (mock == MockedComponent.HostPolicy)
                {
                    l.WithNativeLibraryGroup(string.Empty, g => g
                        // ./hostfxr - real component and will load hostpolicy
                        .WithAsset(Binaries.HostFxr.FileName,
                            f => f.CopyFromFile(Binaries.HostFxr.FilePath))
                        // ./hostpolicy - mocked component
                        .WithAsset(Binaries.HostPolicy.FileName,
                            f => f.CopyFromFile(Binaries.HostPolicy.MockPath)));
                }
            });

            builder.WithCustomizer(customizer);

            // Write out the app
            builder.Build(this);
        }

        public TestApp Copy()
        {
            return new TestApp(this);
        }

        private void LoadAssets()
        {
            Directory.CreateDirectory(Location);
            AppDll = Path.Combine(Location, $"{AssemblyName}.dll");
            AppExe = Path.Combine(Location, Binaries.GetExeFileNameForCurrentPlatform(AssemblyName));
            DepsJson = Path.Combine(Location, $"{AssemblyName}.deps.json");
            RuntimeConfigJson = Path.Combine(Location, $"{AssemblyName}.runtimeconfig.json");
            RuntimeDevConfigJson = Path.Combine(Location, $"{AssemblyName}.runtimeconfig.dev.json");
            HostPolicyDll = Path.Combine(Location, Binaries.HostPolicy.FileName);
            HostFxrDll = Path.Combine(Location, Binaries.HostFxr.FileName);
            CoreClrDll = Path.Combine(Location, Binaries.CoreClr.FileName);
        }

        private static (IEnumerable<string> Assemblies, IEnumerable<string> NativeLibraries) GetRuntimeFiles()
        {
            var runtimePackDir = new DotNetCli(RepoDirectoriesProvider.Default.BuiltDotnet).GreatestVersionSharedFxPath;
            var assemblies = Directory.GetFiles(runtimePackDir, "*.dll").Where(f => IsAssembly(f));

            (string prefix, string suffix) = Binaries.GetSharedLibraryPrefixSuffix();
            var nativeLibraries = Directory.GetFiles(runtimePackDir, $"{prefix}*{suffix}").Where(f => !IsAssembly(f));

            return (assemblies, nativeLibraries);

            static bool IsAssembly(string filePath)
            {
                if (Path.GetExtension(filePath) != ".dll")
                    return false;

                using (var fs = File.OpenRead(filePath))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(fs))
                {
                    return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
                }
            }
        }
    }
}
