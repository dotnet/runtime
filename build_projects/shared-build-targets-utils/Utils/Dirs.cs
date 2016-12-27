using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Cli.Build
{
    public static class Dirs
    {
        public static readonly string RepoRoot = Directory.GetCurrentDirectory();

        public static readonly string DebPackagingConfig = Path.Combine(Dirs.RepoRoot, "packaging", "deb");
        public static readonly string Output = Path.Combine(
            RepoRoot,
            "artifacts",
             Environment.GetEnvironmentVariable("TARGETRID") ?? RuntimeEnvironment.GetRuntimeIdentifier());

        public static readonly string Intermediate = Path.Combine(Output, "intermediate");
        public static readonly string PackagesIntermediate = Path.Combine(Output, "packages/intermediate");
        public static readonly string PackagesNoRID = Path.Combine(RepoRoot, "artifacts", "packages");
        public static readonly string Packages = Path.Combine(Output, "packages");
        public static readonly string Stage1 = Path.Combine(Output, "stage1");
        public static readonly string Stage1Compilation = Path.Combine(Output, "stage1compilation");
        public static readonly string Stage1Symbols = Path.Combine(Output, "stage1symbols");
        public static readonly string Stage2 = Path.Combine(Output, "stage2");
        public static readonly string Stage2Compilation = Path.Combine(Output, "stage2compilation");
        public static readonly string Stage2Symbols = Path.Combine(Output, "stage2symbols");
        public static readonly string CorehostLatest = Path.Combine(Output, "corehost"); // Not using Path.Combine(Output, "corehost", "latest") to keep signing working.
        public static readonly string CorehostLocked = Path.Combine(Output, "corehost", "locked");
        public static readonly string CorehostLocalPackages = Path.Combine(Output, "corehost");
        public static readonly string CorehostDummyPackages = Path.Combine(Output, "corehostdummypackages");
        public static readonly string SharedFrameworkPublish = Path.Combine(Intermediate, "sharedFrameworkPublish");
        public static readonly string TestOutput = Path.Combine(Output, "tests");
        public static readonly string TestArtifacts = Path.Combine(TestOutput, "artifacts");
        public static readonly string TestPackages = Path.Combine(TestOutput, "packages");
        public static readonly string TestPackagesBuild = Path.Combine(TestOutput, "packagesBuild");

        public static readonly string OSXReferenceAssembliesPath = "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks";
        public static readonly string UsrLocalReferenceAssembliesPath = "/usr/local/lib/mono/xbuild-frameworks";
        public static readonly string UsrReferenceAssembliesPath = "/usr/lib/mono/xbuild-frameworks";


        public static string NuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetNuGetPackagesDir();
        public static string PkgNuGetPackages = Path.Combine(Dirs.RepoRoot, "pkg", "packages");

        private static string GetNuGetPackagesDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Dirs.RepoRoot, ".nuget", "packages");
            }
            return Path.Combine(Dirs.RepoRoot, ".nuget", "packages");
        }
    }
}
