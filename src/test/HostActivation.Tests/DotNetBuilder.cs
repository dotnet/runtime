// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
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
        private readonly RepoDirectoriesProvider _repoDirectories;

        public DotNetBuilder(string basePath, string builtDotnet, string name)
        {
            _path = Path.Combine(basePath, name);
            Directory.CreateDirectory(_path);

            _repoDirectories = new RepoDirectoriesProvider(builtDotnet: _path);

            // Prepare the dotnet installation mock

            // ./dotnet.exe - used as a convenient way to load and invoke hostfxr. May change in the future to use test-specific executable
            var builtDotNetCli = new DotNetCli(builtDotnet);
            File.Copy(
                builtDotNetCli.DotnetExecutablePath,
                Path.Combine(_path, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("dotnet")),
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
            string netCoreAppPath = Path.Combine(_path, "shared", "Microsoft.NETCore.App", version);
            Directory.CreateDirectory(netCoreAppPath);

            // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is a mock, will not actually load CoreCLR
            string mockHostPolicyFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
            File.Copy(
                Path.Combine(_repoDirectories.Artifacts, "corehost_test", mockHostPolicyFileName),
                Path.Combine(netCoreAppPath, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy")),
                true);

            return this;
        }

        /// <summary>
        /// Add a mock of the Microsoft.NETCore.App framework with the specified version
        /// </summary>
        /// <param name="version">Version to add</param>
        /// <remarks>
        /// Product runtime binaries are not added. All the added mock framework will contain is hostpolicy,
        /// a mock version of coreclr, and a minimal Microsoft.NETCore.App.deps.json.
        /// </remarks>
        public DotNetBuilder AddMicrosoftNETCoreAppFrameworkMockCoreClr(string version)
        {
            // ./shared/Microsoft.NETCore.App/<version> - create a mock of the root framework
            string netCoreAppPath = Path.Combine(_path, "shared", "Microsoft.NETCore.App", version);
            Directory.CreateDirectory(netCoreAppPath);

            string hostPolicyFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            string coreclrFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("coreclr");
            string mockCoreclrFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr");

            string netCoreAppPathDepsJson = Path.Combine(netCoreAppPath, "Microsoft.NETCore.App.deps.json");

            string currentRid = _repoDirectories.TargetRID;

            NetCoreAppBuilder.ForNETCoreApp("Microsoft.NETCore.App", currentRid)
                .WithStandardRuntimeFallbacks()
                .WithProject("Microsoft.NETCore.App", version, p => p
                    .WithNativeLibraryGroup(null, g => g
                        // ./shared/Microsoft.NETCore.App/<version>/coreclr.dll - this is a mock, will not actually run CoreClr
                        .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder($"runtimes/{currentRid}/native/{coreclrFileName}"))
                            .CopyFromFile(Path.Combine(_repoDirectories.Artifacts, "corehost_test", mockCoreclrFileName))
                            .WithFileOnDiskPath(coreclrFileName))))
                .WithPackage($"runtime.{currentRid}.Microsoft.NETCore.DotNetHostPolicy", version, p => p
                    .WithNativeLibraryGroup(null, g => g
                        // ./shared/Microsoft.NETCore.App/<version>/hostpolicy.dll - this is the real component and will load CoreClr library
                        .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder($"runtimes/{currentRid}/native/{hostPolicyFileName}"))
                            .CopyFromFile(Path.Combine(_repoDirectories.Artifacts, "corehost", hostPolicyFileName))
                            .WithFileOnDiskPath(hostPolicyFileName))))
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
        /// The added mock framework will only contain a runtime.config.json file.
        /// </remarks>
        public DotNetBuilder AddFramework(
            string name,
            string version,
            Action<RuntimeConfig> runtimeConfigCustomizer)
        {
            // ./shared/<name>/<version> - create a mock of effectively empty non-root framework
            string path = Path.Combine(_path, "shared", name, version);
            Directory.CreateDirectory(path);

            // ./shared/<name>/<version>/<name>.runtimeconfig.json - runtime config which can be customized
            RuntimeConfig runtimeConfig = new RuntimeConfig(Path.Combine(path, name + ".runtimeconfig.json"));
            runtimeConfigCustomizer(runtimeConfig);
            runtimeConfig.Save();

            return this;
        }

        public DotNetCli Build()
        {
            return new DotNetCli(_path);
        }
    }
}
