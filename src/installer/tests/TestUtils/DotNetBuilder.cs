// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    /// <summary>
    /// Helper class for creating a mock version of a dotnet installation
    /// </summary>
    /// <remarks>
    /// This class uses a mock version of hostpolicy and does not use the product coreclr runtime,
    /// so the mock installation cannot be used to actually run apps.
    /// </remarks>
    public class DotNetBuilder
    {
        private readonly string _path;

        public DotNetBuilder(string basePath, string builtDotnet, string name)
        {
            _path = name == null ? basePath : Path.Combine(basePath, name);
            Directory.CreateDirectory(_path);

            // Prepare the dotnet installation mock

            // ./dotnet.exe - used as a convenient way to load and invoke hostfxr. May change in the future to use test-specific executable
            var builtDotNetCli = new DotNetCli(builtDotnet);
            File.Copy(
                builtDotNetCli.DotnetExecutablePath,
                Path.Combine(_path, Binaries.DotNet.FileName),
                true);

            // ./host/fxr/<version>/hostfxr.dll - this is the component being tested
            SharedFramework.CopyDirectory(
                builtDotNetCli.GreatestVersionHostFxrPath,
                Path.Combine(_path, "host", "fxr", Path.GetFileName(builtDotNetCli.GreatestVersionHostFxrPath)));
        }

        /// <summary>
        /// Add a mock of the Microsoft.NETCore.App framework with the specified version
        /// </summary>
        /// <param name="version">Version to add</param>
        /// <remarks>
        /// Product runtime binaries are not added. All the added mock framework will contain is a mock version of host policy.
        /// </remarks>
        public DotNetBuilder AddMicrosoftNETCoreAppFrameworkMockHostPolicy(string version)
        {
            // ./shared/Microsoft.NETCore.App/<version> - create a mock of the root framework
            string netCoreAppPath = AddFramework(Constants.MicrosoftNETCoreApp, version);

            // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is a mock, will not actually load CoreCLR
            File.Copy(
                Binaries.HostPolicy.MockPath,
                Path.Combine(netCoreAppPath, Binaries.HostPolicy.FileName),
                true);

            return this;
        }

        /// <summary>
        /// Use a mock version of HostFxr.
        /// </summary>
        /// <param name="version">Version to add</param>
        public DotNetBuilder AddMockHostFxr(Version version)
        {
            string hostfxrPath = Path.Combine(_path, "host", "fxr", version.ToString());
            Directory.CreateDirectory(hostfxrPath);

            string mockHostFxrPath = version switch
            {
                { Major: 2, Minor: 2 } => Binaries.HostFxr.MockPath_2_2,
                { Major: 5, Minor: 0 } => Binaries.HostFxr.MockPath_5_0,
                _ => throw new InvalidOperationException($"Unsupported version {version} of mockhostfxr.")
            };

            File.Copy(
                mockHostFxrPath,
                Path.Combine(hostfxrPath, Binaries.HostFxr.FileName),
                true);

            return this;
        }

        /// <summary>
        /// Removes the specified HostFxr version. If no version is set, it'll delete all versions found.
        /// </summary>
        /// <param name="version">Version to remove</param>
        public DotNetBuilder RemoveHostFxr(Version version = null)
        {
            if (version != null)
            {
                new DirectoryInfo(Path.Combine(_path, "host", "fxr", version.ToString())).Delete(recursive: true);
            }
            else
            {
                foreach (var dir in new DirectoryInfo(Path.Combine(_path, "host", "fxr")).GetDirectories())
                {
                    dir.Delete(recursive: true);
                }
            }

            return this;
        }

        /// <summary>
        /// Add a mock of the Microsoft.NETCore.App framework with the specified version
        /// </summary>
        /// <param name="version">Version to add</param>
        /// <param name="customizer">Customizer to customize the framework before it is built</param>
        /// <remarks>
        /// Product runtime binaries are not added. All the added mock framework will contain is hostpolicy,
        /// a mock version of coreclr, and a minimal Microsoft.NETCore.App.deps.json.
        /// </remarks>
        public DotNetBuilder AddMicrosoftNETCoreAppFrameworkMockCoreClr(string version, Action<NetCoreAppBuilder> customizer = null)
        {
            // ./shared/Microsoft.NETCore.App/<version> - create a mock of the root framework
            string netCoreAppPath = AddFramework(Constants.MicrosoftNETCoreApp, version);

            string currentRid = TestContext.TargetRID;

            NetCoreAppBuilder.ForNETCoreApp(Constants.MicrosoftNETCoreApp, currentRid)
                .WithStandardRuntimeFallbacks()
                .WithProject(Constants.MicrosoftNETCoreApp, version, p => p
                    .WithNativeLibraryGroup(null, g => g
                        // ./shared/Microsoft.NETCore.App/<version>/coreclr.dll - this is a mock, will not actually run CoreClr
                        .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder($"runtimes/{currentRid}/native/{Binaries.CoreClr.FileName}"))
                            .CopyFromFile(Binaries.CoreClr.MockPath)
                            .WithFileOnDiskPath(Binaries.CoreClr.FileName))))
                .WithPackage($"runtime.{currentRid}.Microsoft.NETCore.DotNetHostPolicy", version, p => p
                    .WithNativeLibraryGroup(null, g => g
                        // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is the real component and will load CoreClr library
                        .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder($"runtimes/{currentRid}/native/{Binaries.HostPolicy.FileName}"))
                            .CopyFromFile(Binaries.HostPolicy.FilePath)
                            .WithFileOnDiskPath(Binaries.HostPolicy.FileName))))
                .WithCustomizer(customizer)
                .Build(new TestApp(netCoreAppPath, "Microsoft.NETCore.App"));

            return this;
        }

        /// <summary>
        /// Add a mock framework with the specified framework name and version
        /// </summary>
        /// <param name="name">Framework name</param>
        /// <param name="version">Framework version</param>
        /// <param name="runtimeConfigCustomizer">Customization function for the runtime config</param>
        /// <remarks>
        /// The added mock framework will only contain a deps.json and a runtime.config.json file.
        /// </remarks>
        public DotNetBuilder AddFramework(
            string name,
            string version,
            Action<RuntimeConfig> runtimeConfigCustomizer,
            Action<string> frameworkCustomizer = null)
        {
            // ./shared/<name>/<version> - create a mock of the framework
            string path = AddFramework(name, version);

            // ./shared/<name>/<version>/<name>.runtimeconfig.json - runtime config which can be customized
            RuntimeConfig runtimeConfig = new RuntimeConfig(Path.Combine(path, $"{name}.runtimeconfig.json"));
            runtimeConfigCustomizer(runtimeConfig);
            runtimeConfig.Save();

            if (frameworkCustomizer is not null)
                frameworkCustomizer(path);

            return this;
        }

        public DotNetBuilder AddMockSDK(
            string sdkVersion,
            string runtimeVersion)
        {
            string path = Path.Combine(_path, "sdk", sdkVersion);
            Directory.CreateDirectory(path);

            using var _ = File.Create(Path.Combine(path, "dotnet.dll"));

            RuntimeConfig dotnetRuntimeConfig = new RuntimeConfig(Path.Combine(path, "dotnet.runtimeconfig.json"));
            dotnetRuntimeConfig.WithFramework(new RuntimeConfig.Framework("Microsoft.NETCore.App", runtimeVersion));
            dotnetRuntimeConfig.Save();

            return this;
        }

        /// <summary>
        /// Add a minimal mock framework with the specified framework name and version
        /// </summary>
        /// <param name="name">Framework name</param>
        /// <param name="version">Framework version</param>
        /// <returns>Framework directory</returns>
        /// <remarks>
        /// The added mock framework will only contain a deps.json.
        /// </remarks>
        private string AddFramework(string name, string version)
        {
            // ./shared/<name>/<version> - create a mock of effectively the framework
            string path = Path.Combine(_path, "shared", name, version);
            Directory.CreateDirectory(path);

            // ./shared/<name>/<version>/<name>.deps.json - empty file
            File.WriteAllText(Path.Combine(path, $"{name}.deps.json"), string.Empty);

            return path;
        }

        public DotNetCli Build()
        {
            return new DotNetCli(_path);
        }
    }
}
