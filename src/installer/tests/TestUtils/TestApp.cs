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
using Microsoft.NET.HostModel.AppHost;

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

        /// <summary>
        /// Create a test app from pre-built output of <paramref name="appName"/>.
        /// </summary>
        /// <param name="appName">Name of pre-built app</param>
        /// <param name="assetRelativePath">Path to asset - relative to the directory containing all pre-built assets</param>
        /// <returns>
        /// If <paramref name="assetRelativePath"/> is <c>null</c>, <paramref name="appName"/> is used as the relative path.
        /// </returns>
        public static TestApp CreateFromBuiltAssets(string appName, string assetRelativePath = null)
        {
            assetRelativePath = assetRelativePath ?? appName;
            TestApp app = CreateEmpty(appName);
            TestArtifact.CopyRecursive(
                Path.Combine(TestContext.TestAssetsOutput, assetRelativePath),
                app.Location);
            return app;
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

        public void CreateAppHost(bool isWindowsGui = false, bool copyResources = true)
            => CreateAppHost(Binaries.AppHost.FilePath, isWindowsGui, copyResources);

        public void CreateSingleFileHost(bool isWindowsGui = false, bool copyResources = true)
            => CreateAppHost(Binaries.SingleFileHost.FilePath, isWindowsGui, copyResources);

        public void CreateAppHost(string hostSourcePath, bool isWindowsGui = false, bool copyResources = true)
        {
            // Use the live-built apphost and HostModel to create the apphost to run
            HostWriter.CreateAppHost(
                hostSourcePath,
                AppExe,
                Path.GetFileName(AppDll),
                windowsGraphicalUserInterface: isWindowsGui,
                assemblyToCopyResourcesFrom: copyResources ? AppDll : null);
        }

        public enum MockedComponent
        {
            None,       // Product components
            CoreClr,    // Mock coreclr
            HostPolicy, // Mock hostpolicy
        }

        public void PopulateSelfContained(MockedComponent mock, Action<NetCoreAppBuilder> customizer = null)
        {
            var builder = NetCoreAppBuilder.ForNETCoreApp(Name, TestContext.TargetRID);

            // Update the .runtimeconfig.json - add included framework and remove any existing NETCoreApp framework
            builder.WithRuntimeConfig(c =>
                c.WithIncludedFramework(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion)
                    .RemoveFramework(Constants.MicrosoftNETCoreApp));

            // Add main project assembly
            builder.WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));

            // Add runtime libraries and assets
            builder.WithRuntimePack($"{Constants.MicrosoftNETCoreApp}.Runtime.{TestContext.TargetRID}", TestContext.MicrosoftNETCoreAppVersion, l =>
            {
                if (mock == MockedComponent.None)
                {
                    // All product components
                    var (assemblies, nativeLibraries) = Binaries.GetRuntimeFiles();
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
    }
}
