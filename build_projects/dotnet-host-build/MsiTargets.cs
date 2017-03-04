using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Microsoft.DotNet.Cli.Build;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Host.Build
{
    public class MsiTargets
    {
        private const string ENGINE = "engine.exe";

        private const string WixVersion = "3.10.2";

        private static string WixRoot
        {
            get
            {
                return Path.Combine(Dirs.Output, $"WixTools.{WixVersion}");
            }
        }

        private static string HostFxrMsi { get; set; }

        private static string SharedHostMsi { get; set; }

        private static string SharedFrameworkMsi { get; set; }

        private static string SharedFrameworkBundle { get; set; }

        private static string SharedFrameworkEngine { get; set; }

        private static string MsiVersion { get; set; }

        private static string DisplayVersion { get; set; }

        // Processor Architecture of MSI's contents
        private static string TargetArch { get; set; }

        // Processor Architecture of MSI itself
        private static string MSIBuildArch { get; set; }

        private static void AcquireWix(BuildTargetContext c)
        {
            if (File.Exists(Path.Combine(WixRoot, "candle.exe")))
            {
                return;
            }

            Directory.CreateDirectory(WixRoot);

            c.Info("Downloading WixTools..");

            DownloadFile($"https://dotnetcli.blob.core.windows.net/build/wix/wix.{WixVersion}.zip", Path.Combine(WixRoot, "WixTools.zip"));

            c.Info("Extracting WixTools..");
            ZipFile.ExtractToDirectory(Path.Combine(WixRoot, "WixTools.zip"), WixRoot);
        }

        private static void DownloadFile(string uri, string destinationPath)
        {
            using (var httpClient = new HttpClient())
            {
                var getTask = httpClient.GetStreamAsync(uri);

                using (var outStream = File.OpenWrite(destinationPath))
                {
                    getTask.Result.CopyTo(outStream);
                }
            }
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult InitMsi(BuildTargetContext c)
        {

            SharedFrameworkBundle = c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkInstallerFile");
            SharedHostMsi = Path.ChangeExtension(c.BuildContext.Get<string>("SharedHostInstallerFile"), "msi");
            HostFxrMsi = Path.ChangeExtension(c.BuildContext.Get<string>("HostFxrInstallerFile"), "msi");
            SharedFrameworkMsi = Path.ChangeExtension(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"), "msi");
            SharedFrameworkEngine = GetEngineName(SharedFrameworkBundle);

            var buildVersion = c.BuildContext.Get<BuildVersion>("BuildVersion");
            MsiVersion = buildVersion.GenerateMsiVersion();
            DisplayVersion = buildVersion.SimpleVersion;

            TargetArch = c.BuildContext.Get<string>("Platform");

            MSIBuildArch = CurrentArchitecture.Current.ToString();
            
            // If we are building the MSI for Arm32 or Arm64, then build it as x86 or x64 for now.
            if (String.Compare(TargetArch, "arm", true) == 0)
            {
                MSIBuildArch = "x86";
            }
            else if (String.Compare(TargetArch, "arm64", true) == 0)
            {
                MSIBuildArch = "x64";
            }

            AcquireWix(c);
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi),
        nameof(GenerateDotnetSharedHostMsi),
        nameof(GenerateDotnetHostFxrMsi),
        nameof(GenerateDotnetSharedFrameworkMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateMsis(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi),
        nameof(GenerateSharedFxBundle))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateBundles(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateDotnetSharedHostMsi(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var hostMsiVersion = hostVersion.LockedHostVersion.GenerateMsiVersion();            
            var hostNugetVersion = hostVersion.LockedHostVersion.ToString();
            var inputDir = c.BuildContext.Get<string>("SharedHostPublishRoot");
            var wixObjRoot = Path.Combine(Dirs.Output, "obj", "wix", "sharedhost");
            var sharedHostBrandName = $"'{Monikers.GetSharedHostBrandName(c)}'";
            var upgradeCode = Utils.GenerateGuidFromName(SharedHostMsi).ToString().ToUpper();

            if (Directory.Exists(wixObjRoot))
            {
                Utils.DeleteDirectory(wixObjRoot);
            }
            Directory.CreateDirectory(wixObjRoot);

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "host", "generatemsi.ps1"),
                inputDir, SharedHostMsi, WixRoot, sharedHostBrandName, hostMsiVersion, hostNugetVersion, MSIBuildArch, TargetArch, wixObjRoot, upgradeCode)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateDotnetHostFxrMsi(BuildTargetContext c)
        {
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion");
            var hostFxrMsiVersion = hostVersion.LockedHostFxrVersion.GenerateMsiVersion();            
            var hostFxrNugetVersion = hostVersion.LockedHostFxrVersion.ToString();
            var inputDir = c.BuildContext.Get<string>("HostFxrPublishRoot");
            var wixObjRoot = Path.Combine(Dirs.Output, "obj", "wix", "hostfxr");
            var hostFxrBrandName = $"'{Monikers.GetHostFxrBrandName(c)}'";
            var upgradeCode = Utils.GenerateGuidFromName(HostFxrMsi).ToString().ToUpper();

            if (Directory.Exists(wixObjRoot))
            {
                Utils.DeleteDirectory(wixObjRoot);
            }
            Directory.CreateDirectory(wixObjRoot);

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "hostfxr", "generatemsi.ps1"),
                inputDir, HostFxrMsi, WixRoot, hostFxrBrandName, hostFxrMsiVersion, hostFxrNugetVersion, MSIBuildArch, TargetArch, wixObjRoot, upgradeCode)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateDotnetSharedFrameworkMsi(BuildTargetContext c)
        {
            var inputDir = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            var sharedFrameworkNuGetName = Monikers.SharedFrameworkName;
            var sharedFrameworkNuGetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var msiVerison = sharedFrameworkNuGetVersion.Split('-')[0];
            var upgradeCode = Utils.GenerateGuidFromName(SharedFrameworkMsi).ToString().ToUpper();
            var wixObjRoot = Path.Combine(Dirs.Output, "obj", "wix", "sharedframework");
            var sharedFxBrandName = $"'{Monikers.GetSharedFxBrandName(c)}'";

            if (Directory.Exists(wixObjRoot))
            {
                Utils.DeleteDirectory(wixObjRoot);
            }
            Directory.CreateDirectory(wixObjRoot);

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "sharedframework", "generatemsi.ps1"),
                inputDir, SharedFrameworkMsi, WixRoot, sharedFxBrandName, msiVerison, sharedFrameworkNuGetName, sharedFrameworkNuGetVersion, upgradeCode, MSIBuildArch, TargetArch, wixObjRoot)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }
        
        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateSharedFxBundle(BuildTargetContext c)
        {
            var sharedFrameworkNuGetName = Monikers.SharedFrameworkName;
            var sharedFrameworkNuGetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var upgradeCode = Utils.GenerateGuidFromName(SharedFrameworkBundle).ToString().ToUpper();
            var sharedFxBrandName = $"'{Monikers.GetSharedFxBrandName(c)}'";

            Cmd("powershell", "-NoProfile", "-NoLogo",
                Path.Combine(Dirs.RepoRoot, "packaging", "windows", "sharedframework", "generatebundle.ps1"),
                SharedFrameworkMsi, SharedHostMsi, HostFxrMsi, SharedFrameworkBundle, WixRoot, sharedFxBrandName, MsiVersion, DisplayVersion, sharedFrameworkNuGetName, sharedFrameworkNuGetVersion, upgradeCode, TargetArch, MSIBuildArch)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        private static string GetEngineName(string bundle)
        {
            var engine = $"{Path.GetFileNameWithoutExtension(bundle)}-{ENGINE}";
            return Path.Combine(Path.GetDirectoryName(bundle), engine);
        }

        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult ExtractEngineFromBundle(BuildTargetContext c)
        {
            ExtractEngineFromBundleHelper(SharedFrameworkBundle, SharedFrameworkEngine);
            return c.Success();
        }

        [Target(nameof(MsiTargets.InitMsi))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult ReattachEngineToBundle(BuildTargetContext c)
        {
            ReattachEngineToBundleHelper(SharedFrameworkBundle, SharedFrameworkEngine);
            return c.Success();
        }

        private static void ExtractEngineFromBundleHelper(string bundle, string engine)
        {
            Cmd($"{WixRoot}\\insignia.exe", "-ib", bundle, "-o", engine)
                    .Execute()
                    .EnsureSuccessful();
        }

        private static void ReattachEngineToBundleHelper(string bundle, string engine)
        {
            Cmd($"{WixRoot}\\insignia.exe", "-ab", engine, bundle, "-o", bundle)
                    .Execute()
                    .EnsureSuccessful();

            File.Delete(engine);
        }
    }
}
