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

        public static readonly Dictionary<string, string> HostPackageSupportedRids = new Dictionary<string, string>()
        {
            // Key: Current platform RID. Value: The actual publishable (non-dummy) package name produced by the build system for this RID.
            { "win7-x64", "win7-x64" },
            { "win7-x86", "win7-x86" },
            { "win8-arm", "win8-arm" },
            { "win10-arm64", "win10-arm64" },
            { "osx.10.10-x64", "osx.10.10-x64" },
            { "osx.10.11-x64", "osx.10.10-x64" },
            { "linux-x64", "linux-x64" },
            { "ubuntu.14.04-x64", "ubuntu.14.04-x64" },
            { "ubuntu.16.04-x64", "ubuntu.16.04-x64" },
            { "ubuntu.14.04-arm", "ubuntu.14.04-arm" },
            { "ubuntu.16.04-arm", "ubuntu.16.04-arm" },
            { "ubuntu.16.10-x64", "ubuntu.16.10-x64" },
            { "centos.7-x64", "rhel.7-x64" },
            { "rhel.7-x64", "rhel.7-x64" },
            { "rhel.7.2-x64", "rhel.7-x64" },
            { "debian.8-x64", "debian.8-x64" },
            { "fedora.23-x64", "fedora.23-x64" },
            { "fedora.24-x64", "fedora.24-x64" },
            { "opensuse.13.2-x64", "opensuse.13.2-x64" },
            { "opensuse.42.1-x64", "opensuse.42.1-x64" }
        };

        [Target(nameof(PrepareTargets.Init),
            nameof(CompileCoreHost),
            nameof(PackagePkgProjects),
            nameof(BuildProjectsForNuGetPackages),
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
            var currentRid = HostPackageSupportedRids[c.BuildContext.Get<string>("TargetRID")];

            var stubPackageBuilder = new StubPackageBuilder(DotNetCli.Stage0, Dirs.Intermediate, Dirs.CorehostDummyPackages);

            foreach (var hostPackage in hostVersion.LatestHostPackages)
            {
                foreach (var rid in HostPackageSupportedRids.Values.Distinct())
                {
                    if (!rid.Equals(currentRid))
                    {
                        var basePackageId = hostPackage.Key;
                        var packageVersion = hostPackage.Value.ToString();

                        var packageId = $"runtime.{rid}.{basePackageId}";

                        stubPackageBuilder.GeneratePackage(packageId, packageVersion);
                    }
                }
            }
            return c.Success();
        }

        private static void GetVersionResourceForAssembly(
            string assemblyName,
            HostVersion.VerInfo hostVer,
            string commitHash,
            string tempRcDirectory)
        {
            var semVer = hostVer.ToString();
            var majorVersion = hostVer.Major;
            var minorVersion = hostVer.Minor;
            var patchVersion = hostVer.Patch;
            var buildNumberMajor = hostVer.VerRsrcBuildMajor;
            var buildNumberMinor = hostVer.VerRsrcBuildMinor;
            var buildDetails = $"{semVer}, {commitHash} built by: {System.Environment.MachineName}, UTC: {DateTime.UtcNow.ToString()}";
            var rcContent = $@"
#include <Windows.h>

#ifndef VER_COMPANYNAME_STR
#define VER_COMPANYNAME_STR         ""Microsoft Corporation""
#endif
#ifndef VER_FILEDESCRIPTION_STR
#define VER_FILEDESCRIPTION_STR     ""{assemblyName}""
#endif
#ifndef VER_INTERNALNAME_STR
#define VER_INTERNALNAME_STR        VER_FILEDESCRIPTION_STR
#endif
#ifndef VER_ORIGINALFILENAME_STR
#define VER_ORIGINALFILENAME_STR    VER_FILEDESCRIPTION_STR
#endif
#ifndef VER_PRODUCTNAME_STR
#define VER_PRODUCTNAME_STR         ""Microsoft\xae .NET Core Framework"";
#endif
#undef VER_PRODUCTVERSION
#define VER_PRODUCTVERSION          {majorVersion},{minorVersion},{patchVersion},{buildNumberMajor}
#undef VER_PRODUCTVERSION_STR
#define VER_PRODUCTVERSION_STR      ""{buildDetails}""
#undef VER_FILEVERSION
#define VER_FILEVERSION             {majorVersion},{minorVersion},{patchVersion},{buildNumberMajor}
#undef VER_FILEVERSION_STR
#define VER_FILEVERSION_STR         ""{majorVersion},{minorVersion},{buildNumberMajor},{buildNumberMinor},{buildDetails}"";
#ifndef VER_LEGALCOPYRIGHT_STR
#define VER_LEGALCOPYRIGHT_STR      ""\xa9 Microsoft Corporation.  All rights reserved."";
#endif
#ifndef VER_DEBUG
#ifdef DEBUG
#define VER_DEBUG                   VS_FF_DEBUG
#else
#define VER_DEBUG                   0
#endif
#endif
";
            var tempRcHdrDir = Path.Combine(tempRcDirectory, assemblyName);
            Mkdirp(tempRcHdrDir);
            var tempRcHdrFile = Path.Combine(tempRcHdrDir, "version_info.h");
            File.WriteAllText(tempRcHdrFile, rcContent);
        }

        public static string GenerateVersionResource(BuildTargetContext c)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            var tempRcDirectory = Path.Combine(Dirs.Intermediate, "hostResourceFiles");
            Rmdir(tempRcDirectory);
            Mkdirp(tempRcDirectory);

            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var commitHash = c.BuildContext.Get<string>("CommitHash");
            foreach (var binary in hostVersion.LatestHostBinaries)
            {
                GetVersionResourceForAssembly(binary.Key, binary.Value, commitHash, tempRcDirectory);
            }

            return tempRcDirectory;
        }

        [Target]
        public static BuildTargetResult CompileCoreHost(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var configuration = c.BuildContext.Get<string>("Configuration");
            string rid = c.BuildContext.Get<string>("TargetRID");
            string platform = c.BuildContext.Get<string>("Platform");
            string crossEnv = c.BuildContext.Get<string>("Cross");
            bool linkPortable = c.BuildContext.Get<bool>("LinkPortable");

            // Generate build files
            var cmakeOut = Path.Combine(Dirs.CorehostLatest, "cmake");

            Rmdir(cmakeOut);
            Mkdirp(cmakeOut);

            // Run the build
            string corehostSrcDir = Path.Combine(c.BuildContext.BuildDirectory, "src", "corehost");
            string commitHash = c.BuildContext.Get<string>("CommitHash");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Create .rc files on Windows.
                var resourceDir = GenerateVersionResource(c);

                if (configuration.Equals("Release"))
                {
                    // Cmake calls it "RelWithDebInfo" in the generated MSBuild
                    configuration = "RelWithDebInfo";
                }

                // Why does Windows directly call cmake but Linux/Mac calls "build.sh" in the corehost dir?
                // See the comment in "src/corehost/build.sh" for details. It doesn't work for some reason.
                List<string> cmakeArgList = new List<string>();

                string cmakeBaseRid, visualStudio, archMacro, arch;
                string ridMacro = $"-DCLI_CMAKE_RUNTIME_ID:STRING={rid}";
                string cmakeHostVer = $"-DCLI_CMAKE_HOST_VER:STRING={hostVersion.LatestHostVersion.ToString()}";
                string cmakeAppHostVer = $"-DCLI_CMAKE_APPHOST_VER:STRING={hostVersion.LatestAppHostVersion.ToString()}";
                string cmakeHostPolicyVer = $"-DCLI_CMAKE_HOST_POLICY_VER:STRING={hostVersion.LatestHostPolicyVersion.ToString()}";
                string cmakeHostFxrVer = $"-DCLI_CMAKE_HOST_FXR_VER:STRING={hostVersion.LatestHostFxrVersion.ToString()}";
                string cmakeCommitHash = $"-DCLI_CMAKE_COMMIT_HASH:STRING={commitHash}";
                string cmakeResourceDir = $"-DCLI_CMAKE_RESOURCE_DIR:STRING={resourceDir}";
                string cmakeExtraArgs = null;

                switch (platform.ToLower())
                {
                    case "x86":
                        cmakeBaseRid = "-DCLI_CMAKE_PKG_RID:STRING=win7-x86";
                        visualStudio = "Visual Studio 14 2015";
                        archMacro = "-DCLI_CMAKE_PLATFORM_ARCH_I386=1";
                        arch = "x86";
                        break;
                    case "arm":
                        cmakeBaseRid = "-DCLI_CMAKE_PKG_RID:STRING=win8-arm";
                        visualStudio = "Visual Studio 14 2015 ARM";
                        archMacro = "-DCLI_CMAKE_PLATFORM_ARCH_ARM=1";
                        cmakeExtraArgs ="-DCMAKE_SYSTEM_VERSION=10.0";
                        arch = "arm";
                        break;
                    case "arm64":
                        cmakeBaseRid = "-DCLI_CMAKE_PKG_RID:STRING=win10-arm64";
                        visualStudio = "Visual Studio 14 2015 Win64";
                        archMacro = "-DCLI_CMAKE_PLATFORM_ARCH_ARM64=1";
                        arch = "arm64";
                        if (Environment.GetEnvironmentVariable("__ToolsetDir") == null)
                        {
                            throw new Exception("Toolset Dir must be set when the Platform is ARM64");
                        }
                        break;
                    case "x64":
                        cmakeBaseRid = "-DCLI_CMAKE_PKG_RID:STRING=win7-x64";
                        visualStudio = "Visual Studio 14 2015 Win64";
                        archMacro = "-DCLI_CMAKE_PLATFORM_ARCH_AMD64=1";
                        arch = "x64";
                        break;
                    default:
                        throw new PlatformNotSupportedException("Target Architecture: " + platform + " is not currently supported.");
                }

                cmakeArgList.Add(corehostSrcDir);
                cmakeArgList.Add(archMacro);
                cmakeArgList.Add(ridMacro);
                cmakeArgList.Add(cmakeHostVer);
                cmakeArgList.Add(cmakeAppHostVer);
                cmakeArgList.Add(cmakeHostFxrVer);
                cmakeArgList.Add(cmakeHostPolicyVer);
                cmakeArgList.Add(cmakeBaseRid);
                cmakeArgList.Add(cmakeCommitHash);
                cmakeArgList.Add(cmakeResourceDir);
                cmakeArgList.Add("-G");
                cmakeArgList.Add(visualStudio);
                
                if (!String.IsNullOrEmpty(cmakeExtraArgs))
                {
                    cmakeArgList.Add(cmakeExtraArgs);
                }

                ExecIn(cmakeOut, "cmake", cmakeArgList);

                var pf32 = RuntimeInformation.OSArchitecture == Architecture.X64 ?
                    Environment.GetEnvironmentVariable("ProgramFiles(x86)") :
                    Environment.GetEnvironmentVariable("ProgramFiles");

                string msbuildPath = Path.Combine(pf32, "MSBuild", "14.0", "Bin", "MSBuild.exe");
                string cmakeOutPath = Path.Combine(cmakeOut, "ALL_BUILD.vcxproj");
                string configParameter = $"/p:Configuration={configuration}";
                if (arch == "arm64")
                    Exec(msbuildPath, cmakeOutPath, configParameter, "/p:useEnv=true");
                else
                    Exec(msbuildPath, cmakeOutPath, configParameter);

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", "exe", configuration, "dotnet.exe"), Path.Combine(Dirs.CorehostLatest, "dotnet.exe"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "exe", configuration, "dotnet.pdb"), Path.Combine(Dirs.CorehostLatest, "dotnet.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "apphost", configuration, "apphost.exe"), Path.Combine(Dirs.CorehostLatest, "apphost.exe"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "apphost", configuration, "apphost.pdb"), Path.Combine(Dirs.CorehostLatest, "apphost.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", configuration, "hostpolicy.dll"), Path.Combine(Dirs.CorehostLatest, "hostpolicy.dll"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", configuration, "hostpolicy.pdb"), Path.Combine(Dirs.CorehostLatest, "hostpolicy.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", configuration, "hostfxr.dll"), Path.Combine(Dirs.CorehostLatest, "hostfxr.dll"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", configuration, "hostfxr.pdb"), Path.Combine(Dirs.CorehostLatest, "hostfxr.pdb"), overwrite: true);
            }
            else
            {
                string arch;
                switch (platform.ToLower())
                {
                    case "x64":
                        arch = "x64";
                        break;
                    case "arm":
                        arch = "arm";
                        break;
                    default:
                        throw new PlatformNotSupportedException("Target Architecture: " + platform + " is not currently supported.");
                }

                // Why does Windows directly call cmake but Linux/Mac calls "build.sh" in the corehost dir?
                // See the comment in "src/corehost/build.sh" for details. It doesn't work for some reason.
                List<string> buildScriptArgList = new List<string>();
                string buildScriptFile  = Path.Combine(corehostSrcDir, "build.sh");

                buildScriptArgList.Add("--arch");
                buildScriptArgList.Add(arch);
                buildScriptArgList.Add("--hostver");
                buildScriptArgList.Add(hostVersion.LatestHostVersion.ToString());
                buildScriptArgList.Add("--apphostver");
                buildScriptArgList.Add(hostVersion.LatestAppHostVersion.ToString());
                buildScriptArgList.Add("--fxrver");
                buildScriptArgList.Add(hostVersion.LatestHostFxrVersion.ToString());
                buildScriptArgList.Add("--policyver");
                buildScriptArgList.Add(hostVersion.LatestHostPolicyVersion.ToString());
                buildScriptArgList.Add("--rid");
                buildScriptArgList.Add(rid);
                buildScriptArgList.Add("--commithash");
                buildScriptArgList.Add(commitHash);

                if (string.Equals(crossEnv, "1"))
                {
                    buildScriptArgList.Add("--cross");
                }

                if (linkPortable)
                {
                    buildScriptArgList.Add("--portableLinux");
                }
                
                ExecIn(cmakeOut, buildScriptFile, buildScriptArgList);

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", "exe", "dotnet"), Path.Combine(Dirs.CorehostLatest, "dotnet"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "apphost", "apphost"), Path.Combine(Dirs.CorehostLatest, "apphost"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", HostArtifactNames.HostPolicyBaseName), Path.Combine(Dirs.CorehostLatest, HostArtifactNames.HostPolicyBaseName), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "fxr", HostArtifactNames.DotnetHostFxrBaseName), Path.Combine(Dirs.CorehostLatest, HostArtifactNames.DotnetHostFxrBaseName), overwrite: true);
            }
            return c.Success();
        }

        [Target]
        public static BuildTargetResult BuildProjectsForNuGetPackages(BuildTargetContext c)
        {
            if (CurrentPlatform.IsWindows)
            {
                var configuration = c.BuildContext.Get<string>("Configuration");

                // build projects for nuget packages
                var packagingOutputDir = Path.Combine(Dirs.Intermediate, "forPackaging");
                Mkdirp(packagingOutputDir);
                foreach (var project in PackageTargets.ProjectsToPack)
                {
                    // Just build them, we'll pack later
                    var packBuildResult = DotNetCli.Stage0.Build(
                        "--build-base-path",
                        packagingOutputDir,
                        "--configuration",
                        configuration,
                        Path.Combine(c.BuildContext.BuildDirectory, "src", project))
                        .Execute();

                    packBuildResult.EnsureSuccessful();
                }
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult GenerateMSbuildPropsFile(BuildTargetContext c)
        {
            var buildVersion = c.BuildContext.Get<BuildVersion>("BuildVersion");
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            string platform = c.BuildContext.Get<string>("Platform");

            var msbuildProps = new StringBuilder();

            msbuildProps.AppendLine(@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">");
            msbuildProps.AppendLine("  <PropertyGroup>");
            msbuildProps.AppendLine($"    <Platform>{platform}</Platform>");
            msbuildProps.AppendLine($"    <DotNetHostBinDir>{Dirs.CorehostLatest}</DotNetHostBinDir>");
            msbuildProps.AppendLine($"    <HostVersion>{hostVersion.LatestHostVersion.WithoutSuffix}</HostVersion>");
            msbuildProps.AppendLine($"    <AppHostVersion>{hostVersion.LatestAppHostVersion.WithoutSuffix}</AppHostVersion>");
            msbuildProps.AppendLine($"    <HostResolverVersion>{hostVersion.LatestHostFxrVersion.WithoutSuffix}</HostResolverVersion>");
            msbuildProps.AppendLine($"    <HostPolicyVersion>{hostVersion.LatestHostPolicyVersion.WithoutSuffix}</HostPolicyVersion>");
            msbuildProps.AppendLine($"    <BuildNumberMajor>{hostVersion.LatestHostBuildMajor}</BuildNumberMajor>");
            msbuildProps.AppendLine($"    <BuildNumberMinor>{hostVersion.LatestHostBuildMinor}</BuildNumberMinor>");
            msbuildProps.AppendLine($"    <PreReleaseLabel>{hostVersion.ReleaseSuffix}</PreReleaseLabel>");
            msbuildProps.AppendLine($"    <EnsureStableVersion>{hostVersion.EnsureStableVersion}</EnsureStableVersion>");
            msbuildProps.AppendLine($"    <NetCoreAppVersion>{buildVersion.ProductionVersion}</NetCoreAppVersion>");
            msbuildProps.AppendLine("  </PropertyGroup>");
            msbuildProps.AppendLine("</Project>");

            File.WriteAllText(Path.Combine(c.BuildContext.BuildDirectory, "pkg", "version.props"), msbuildProps.ToString());

            return c.Success();
        }

        [Target(nameof(GenerateStubHostPackages), nameof(GenerateMSbuildPropsFile))]
        public static BuildTargetResult PackagePkgProjects(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var hostNugetversion = hostVersion.LatestHostVersion.ToString();
            var content = $@"{c.BuildContext["CommitHash"]}{Environment.NewLine}{hostNugetversion}{Environment.NewLine}";
            var pkgDir = Path.Combine(c.BuildContext.BuildDirectory, "pkg");
            string rid = HostPackageSupportedRids[c.BuildContext.Get<string>("TargetRID")];
            File.WriteAllText(Path.Combine(pkgDir, "version.txt"), content);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Exec(Path.Combine(pkgDir, "pack.cmd"));
            }
            else
            {
                List<string> buildScriptArgList = new List<string>();
                string buildScriptFile = Path.Combine(pkgDir, "pack.sh");

                buildScriptArgList.Add("--rid");
                buildScriptArgList.Add(rid);

                Exec(buildScriptFile, buildScriptArgList);
            }

            foreach (var file in Directory.GetFiles(Path.Combine(pkgDir, "bin", "packages"), "*.nupkg"))
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(Dirs.CorehostLocalPackages, fileName), true);

                Console.WriteLine($"Copying package {fileName} to artifacts directory {Dirs.CorehostLocalPackages}.");
            }

            bool fValidateHostPackages = c.BuildContext.Get<bool>("ValidateHostPackages");

            // Validate the generated host packages only if we are building them.
            if (fValidateHostPackages)
            {
                foreach (var item in hostVersion.LatestHostPackages)
                {
                    var fileFilter = $"runtime.{rid}.{item.Key}.{item.Value.ToString()}.nupkg";
                    if (Directory.GetFiles(Dirs.CorehostLocalPackages, fileFilter).Length == 0)
                    {
                        throw new BuildFailureException($"Nupkg for {fileFilter} was not created.");
                    }
                }
            }

            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init))]
        public static BuildTargetResult RestoreLockedCoreHost(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var lockedHostFxrVersion = hostVersion.LockedHostFxrVersion.ToString();
            string currentRid = HostPackageSupportedRids[c.BuildContext.Get<string>("TargetRID")];
            string framework = c.BuildContext.Get<string>("TargetFramework");

            string projectJson = $@"{{
  ""dependencies"": {{
      ""Microsoft.NETCore.DotNetHostResolver"" : ""{lockedHostFxrVersion}""
  }},
  ""frameworks"": {{
      ""{framework}"": {{}}
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
            DotNetCli.Stage0.Publish("--output", Dirs.CorehostLocked, "--no-build", "-r", currentRid)
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();

            return c.Success();
        }

        [Target(nameof(RestoreLockedCoreHost))]
        public static BuildTargetResult PublishSharedFrameworkAndSharedHost(BuildTargetContext c)
        {
            var outputDir = Dirs.SharedFrameworkPublish;
            Utils.DeleteDirectory(outputDir);
            Directory.CreateDirectory(outputDir);

            var dotnetCli = DotNetCli.Stage0;
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            string sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            string sharedFrameworkRid = c.BuildContext.Get<string>("TargetRID");
            string sharedFrameworkTarget = c.BuildContext.Get<string>("TargetFramework");
            var hostFxrVersion = hostVersion.LockedHostFxrVersion.ToString();
            var commitHash = c.BuildContext.Get<string>("CommitHash");

            var sharedFrameworkPublisher = new SharedFrameworkPublisher(
                Dirs.RepoRoot,
                Dirs.CorehostLocked,
                Dirs.CorehostLatest,
                Dirs.CorehostLocalPackages,
                sharedFrameworkNugetVersion,
                sharedFrameworkRid,
                sharedFrameworkTarget);

            sharedFrameworkPublisher.PublishSharedFramework(outputDir, commitHash, dotnetCli, hostFxrVersion);

            sharedFrameworkPublisher.CopyMuxer(outputDir);
            sharedFrameworkPublisher.CopyHostFxrToVersionedDirectory(outputDir, hostFxrVersion);

            return c.Success();
        }
    }
}
