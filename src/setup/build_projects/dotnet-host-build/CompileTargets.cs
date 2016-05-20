using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.DotNet.Cli.Build;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using static Microsoft.DotNet.Cli.Build.FS;

namespace Microsoft.DotNet.Host.Build
{
    public class CompileTargets
    {
        public static readonly bool IsWinx86 = CurrentPlatform.IsWindows && CurrentArchitecture.Isx86;
        public const string SharedFrameworkName = "Microsoft.NETCore.App";

        public static string HostPackagePlatformRid => HostPackageSupportedRids[
                             (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
                             ? $"win7-{RuntimeEnvironment.RuntimeArchitecture}"
                             : RuntimeEnvironment.GetRuntimeIdentifier()];

        public static readonly Dictionary<string, string> HostPackageSupportedRids = new Dictionary<string, string>()
        {
            // Key: Current platform RID. Value: The actual publishable (non-dummy) package name produced by the build system for this RID.
            { "win7-x64", "win7-x64" },
            { "win7-x86", "win7-x86" },
            { "osx.10.10-x64", "osx.10.10-x64" },
            { "osx.10.11-x64", "osx.10.10-x64" },
            { "ubuntu.14.04-x64", "ubuntu.14.04-x64" },
            { "centos.7-x64", "rhel.7-x64" },
            { "rhel.7-x64", "rhel.7-x64" },
            { "rhel.7.2-x64", "rhel.7-x64" },
            { "debian.8-x64", "debian.8-x64" }
        };

        [Target(nameof(PrepareTargets.Init), 
            nameof(CompileCoreHost), 
            nameof(PackagePkgProjects),
            nameof(RestoreLockedCoreHost),
            nameof(PublishSharedFrameworkAndSharedHost))]
        public static BuildTargetResult Compile(BuildTargetContext c)
        {
            return c.Success();
        }

        // We need to generate stub host packages so we can restore our standalone test assets against the metapackage 
        // we built earlier in the build
        // https://github.com/dotnet/cli/issues/2438
        [Target]
        public static BuildTargetResult GenerateStubHostPackages(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var currentRid = HostPackagePlatformRid;

            var stubPackageBuilder = new StubPackageBuilder(DotNetCli.Stage0, Dirs.Intermediate, Dirs.CorehostDummyPackages);

            foreach (var hostPackage in hostVersion.LatestHostPackages)
            {
                foreach (var rid in HostPackageSupportedRids.Values.Distinct())
                {
                    if (!rid.Equals(currentRid))
                    {
                        var basePackageId = hostPackage.Key;
                        var packageVersion = hostPackage.Value;

                        var packageId = $"runtime.{rid}.{basePackageId}";

                        stubPackageBuilder.GeneratePackage(packageId, packageVersion);
                    }
                }
            }
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CompileCoreHost(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");

            // Generate build files
            var cmakeOut = Path.Combine(Dirs.CorehostLatest, "cmake");

            Rmdir(cmakeOut);
            Mkdirp(cmakeOut);

            var configuration = c.BuildContext.Get<string>("Configuration");

            // Run the build
            string rid = DotNetCli.Stage0.GetRuntimeId();
            string corehostSrcDir = Path.Combine(c.BuildContext.BuildDirectory, "src", "corehost");
            string commitHash = c.BuildContext.Get<string>("CommitHash");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Why does Windows directly call cmake but Linux/Mac calls "build.sh" in the corehost dir?
                // See the comment in "src/corehost/build.sh" for details. It doesn't work for some reason.
                var visualStudio = IsWinx86 ? "Visual Studio 14 2015" : "Visual Studio 14 2015 Win64";
                var archMacro = IsWinx86 ? "-DCLI_CMAKE_PLATFORM_ARCH_I386=1" : "-DCLI_CMAKE_PLATFORM_ARCH_AMD64=1";
                var ridMacro = $"-DCLI_CMAKE_RUNTIME_ID:STRING={rid}";
                var arch = IsWinx86 ? "x86" : "x64";
                var baseSupportedRid = $"win7-{arch}";
                var cmakeHostVer = $"-DCLI_CMAKE_HOST_VER:STRING={hostVersion.LatestHostVersion}";
                var cmakeHostPolicyVer = $"-DCLI_CMAKE_HOST_POLICY_VER:STRING={hostVersion.LatestHostPolicyVersion}";
                var cmakeHostFxrVer = $"-DCLI_CMAKE_HOST_FXR_VER:STRING={hostVersion.LatestHostFxrVersion}";
                var cmakeBaseRid = $"-DCLI_CMAKE_PKG_RID:STRING={baseSupportedRid}";
                var cmakeCommitHash = $"-DCLI_CMAKE_COMMIT_HASH:STRING={commitHash}";

                ExecIn(cmakeOut, "cmake",
                    corehostSrcDir,
                    archMacro,
                    ridMacro,
                    cmakeHostVer,
                    cmakeHostFxrVer,
                    cmakeHostPolicyVer,
                    cmakeBaseRid,
                    cmakeCommitHash,
                    "-G",
                    visualStudio);

                var pf32 = RuntimeInformation.OSArchitecture == Architecture.X64 ?
                    Environment.GetEnvironmentVariable("ProgramFiles(x86)") :
                    Environment.GetEnvironmentVariable("ProgramFiles");

                if (configuration.Equals("Release"))
                {
                    // Cmake calls it "RelWithDebInfo" in the generated MSBuild
                    configuration = "RelWithDebInfo";
                }

                Exec(Path.Combine(pf32, "MSBuild", "14.0", "Bin", "MSBuild.exe"),
                    Path.Combine(cmakeOut, "ALL_BUILD.vcxproj"),
                    $"/p:Configuration={configuration}");

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", configuration, "dotnet.exe"), Path.Combine(Dirs.CorehostLatest, "dotnet.exe"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", configuration, "dotnet.pdb"), Path.Combine(Dirs.CorehostLatest, "dotnet.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", configuration, "hostpolicy.dll"), Path.Combine(Dirs.CorehostLatest, "hostpolicy.dll"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", configuration, "hostpolicy.pdb"), Path.Combine(Dirs.CorehostLatest, "hostpolicy.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", configuration, "hostfxr.dll"), Path.Combine(Dirs.CorehostLatest, "hostfxr.dll"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", configuration, "hostfxr.pdb"), Path.Combine(Dirs.CorehostLatest, "hostfxr.pdb"), overwrite: true);
            }
            else
            {
                ExecIn(cmakeOut, Path.Combine(c.BuildContext.BuildDirectory, "src", "corehost", "build.sh"),
                        "--arch",
                        "x64",
                        "--hostver",
                        hostVersion.LatestHostVersion,
                        "--fxrver",
                        hostVersion.LatestHostFxrVersion,
                        "--policyver",
                        hostVersion.LatestHostPolicyVersion,
                        "--rid",
                        rid,
                        "--commithash",
                        commitHash);

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", "dotnet"), Path.Combine(Dirs.CorehostLatest, "dotnet"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", HostArtifactNames.HostPolicyBaseName), Path.Combine(Dirs.CorehostLatest, HostArtifactNames.HostPolicyBaseName), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", HostArtifactNames.DotnetHostFxrBaseName), Path.Combine(Dirs.CorehostLatest, HostArtifactNames.DotnetHostFxrBaseName), overwrite: true);
            }
            return c.Success();
        }

        [Target(nameof(CompileTargets.GenerateStubHostPackages))]
        public static BuildTargetResult PackagePkgProjects(BuildTargetContext c)
        {
            var arch = IsWinx86 ? "x86" : "x64";
            
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var hostNugetversion = hostVersion.LatestHostVersion;
            var content = $@"{c.BuildContext["CommitHash"]}{Environment.NewLine}{hostNugetversion}{Environment.NewLine}";
            var pkgDir = Path.Combine(c.BuildContext.BuildDirectory, "pkg");
            File.WriteAllText(Path.Combine(pkgDir, "version.txt"), content);

            if (CurrentPlatform.IsWindows)
            {
                Command.Create(Path.Combine(pkgDir, "pack.cmd"))
                    // Workaround to arg escaping adding backslashes for arguments to .cmd scripts.
                    .Environment("__WorkaroundCliCoreHostBuildArch", arch)
                    .Environment("__WorkaroundCliCoreHostBinDir", Dirs.CorehostLatest)
                    .Environment("__WorkaroundCliCoreHostPolicyVer", hostVersion.LatestHostPolicyVersionNoSuffix)
                    .Environment("__WorkaroundCliCoreHostFxrVer", hostVersion.LatestHostFxrVersionNoSuffix)
                    .Environment("__WorkaroundCliCoreHostVer", hostVersion.LatestHostVersionNoSuffix)
                    .Environment("__WorkaroundCliCoreHostBuildMajor", hostVersion.LatestHostBuildMajor)
                    .Environment("__WorkaroundCliCoreHostBuildMinor", hostVersion.LatestHostBuildMinor)
                    .Environment("__WorkaroundCliCoreHostVersionTag", hostVersion.LatestHostPrerelease)
                    .ForwardStdOut()
                    .ForwardStdErr()
                    .Execute()
                    .EnsureSuccessful();
            }
            else
            {
                Exec(Path.Combine(pkgDir, "pack.sh"),
                    "--arch",
                    "x64",
                    "--hostbindir",
                    Dirs.CorehostLatest,
                    "--policyver",
                    hostVersion.LatestHostPolicyVersionNoSuffix,
                    "--fxrver",
                    hostVersion.LatestHostFxrVersionNoSuffix,
                    "--hostver",
                    hostVersion.LatestHostVersionNoSuffix,
                    "--build-major",
                    hostVersion.LatestHostBuildMajor,
                    "--build-minor",
                    hostVersion.LatestHostBuildMinor,
                    "--vertag",
                    hostVersion.LatestHostPrerelease);
            }
            foreach (var file in Directory.GetFiles(Path.Combine(pkgDir, "bin", "packages"), "*.nupkg"))
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(Dirs.CorehostLocalPackages, fileName), true);

                Console.WriteLine($"Copying package {fileName} to artifacts directory {Dirs.CorehostLocalPackages}.");
            }
            foreach (var item in hostVersion.LatestHostPackages)
            {
                var fileFilter = $"runtime.{HostPackagePlatformRid}.{item.Key}.{item.Value}.nupkg";
                if (Directory.GetFiles(Dirs.CorehostLocalPackages, fileFilter).Length == 0)
                {
                    throw new BuildFailureException($"Nupkg for {fileFilter} was not created.");
                }
            }
            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init))]
        public static BuildTargetResult RestoreLockedCoreHost(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var lockedHostFxrVersion = hostVersion.LockedHostFxrVersion;

            var currentRid = HostPackagePlatformRid;

            string projectJson = $@"{{
  ""dependencies"": {{
      ""Microsoft.NETCore.DotNetHostResolver"" : ""{lockedHostFxrVersion}""
  }},
  ""frameworks"": {{
      ""netcoreapp1.0"": {{}}
  }},
  ""runtimes"": {{
      ""{currentRid}"": {{}}
  }}
}}";
            var tempPjDirectory = Path.Combine(Dirs.Intermediate, "lockedHostTemp");
            FS.Rmdir(tempPjDirectory);
            Directory.CreateDirectory(tempPjDirectory);
            var tempPjFile = Path.Combine(tempPjDirectory, "project.json");
            File.WriteAllText(tempPjFile, projectJson);

            DotNetCli.Stage0.Restore("--verbosity", "verbose",
                    "--fallbacksource", Dirs.CorehostLocalPackages,
                    "--fallbacksource", Dirs.CorehostDummyPackages)
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();

            // Clean out before publishing locked binaries
            FS.Rmdir(Dirs.CorehostLocked);

            // Use specific RIDS for non-backward compatible platforms.
            (CurrentPlatform.IsWindows
                ? DotNetCli.Stage0.Publish("--output", Dirs.CorehostLocked, "--no-build")
                : DotNetCli.Stage0.Publish("--output", Dirs.CorehostLocked, "--no-build", "-r", currentRid))
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishSharedFrameworkAndSharedHost(BuildTargetContext c)
        {
            var outputDir = Dirs.SharedFrameworkPublish;
            var dotnetCli = DotNetCli.Stage0;
            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var commitHash = c.BuildContext.Get<string>("CommitHash");

            var sharedFrameworkPublisher = new SharedFrameworkPublisher(
                Dirs.RepoRoot,
                Dirs.CorehostLocked,
                Dirs.CorehostLatest,
                Dirs.CorehostLocalPackages,
                sharedFrameworkNugetVersion);

            sharedFrameworkPublisher.PublishSharedFramework(outputDir, commitHash, dotnetCli);
            sharedFrameworkPublisher.CopySharedHostArtifacts(outputDir);

            return c.Success();
        }
    }
}
