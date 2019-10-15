using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JitBench
{
    public class DotNetSetup
    {
        public DotNetSetup(string dotNetDirPath)
        {
            DotNetDirPath = dotNetDirPath;
            OS = DefaultOSPlatform;
            Architecture = RuntimeInformation.OSArchitecture;
            AzureFeed = DefaultAzureFeed;
        }

        public string DotNetDirPath { get; set; }
        public string FrameworkVersion { get; set; }
        public string SdkVersion { get; set; }
        public OSPlatform OS { get; set; }
        public Architecture Architecture { get; set; }
        public string AzureFeed { get; set; }
        public string PrivateRuntimeBinaryDirPath { get; set; }

        public DotNetSetup WithFrameworkVersion(string version)
        {
            FrameworkVersion = version;
            return this;
        }

        public DotNetSetup WithSdkVersion(string version)
        {
            SdkVersion = version;
            return this;
        }

        public DotNetSetup WithArchitecture(Architecture architecture)
        {
            Architecture = architecture;
            return this;
        }

        public DotNetSetup WithOS(OSPlatform os)
        {
            OS = os;
            return this;
        }

        public DotNetSetup WithPrivateRuntimeBinaryOverlay(string privateRuntimeBinaryDirPath)
        {
            PrivateRuntimeBinaryDirPath = privateRuntimeBinaryDirPath;
            return this;
        }

        public string GetFrameworkDownloadLink()
        {
            if(FrameworkVersion == null)
            {
                return null;
            }
            else
            {
                return GetFrameworkDownloadLink(AzureFeed, FrameworkVersion, OS, Architecture);
            }
        }

        public string GetSDKDownloadLink()
        {
            if(SdkVersion == null)
            {
                return null;
            }
            else
            {
                return GetSDKDownloadLink(AzureFeed, SdkVersion, OS, Architecture);
            }
        }

        async public Task<DotNetInstallation> Run(ITestOutputHelper output)
        {
            using (var acquireOutput = new IndentedTestOutputHelper("Acquiring DotNet", output))
            {
                string remoteSdkPath = GetSDKDownloadLink();
                if(remoteSdkPath != null)
                {
                    await FileTasks.DownloadAndUnzip(remoteSdkPath, DotNetDirPath, acquireOutput);
                }
                string remoteRuntimePath = GetFrameworkDownloadLink();
                if(remoteRuntimePath != null)
                {
                    await FileTasks.DownloadAndUnzip(remoteRuntimePath, DotNetDirPath, acquireOutput);

                    // the SDK may have included another runtime version, but to help prevent mistakes
                    // where a test might run against a different version than we intended all other
                    // versions will be deleted.
                    string mnappDirPath = Path.Combine(DotNetDirPath, "shared", "Microsoft.NETCore.App");
                    foreach (string dir in Directory.GetDirectories(mnappDirPath))
                    {
                        string versionDir = Path.GetFileName(dir);
                        if (versionDir != FrameworkVersion)
                        {
                            FileTasks.DeleteDirectory(dir, acquireOutput);
                        }
                    }
                }
                string actualFrameworkVersion = FrameworkVersion;
                if (actualFrameworkVersion == null)
                {
                    //if Framework version is being infered from an SDK then snoop the filesystem to see what got installed
                    foreach (string dirPath in Directory.EnumerateDirectories(Path.Combine(DotNetDirPath, "shared", "Microsoft.NETCore.App")))
                    {
                        actualFrameworkVersion = Path.GetFileName(dirPath);
                        break;
                    }
                }


                DotNetInstallation result = new DotNetInstallation(DotNetDirPath, actualFrameworkVersion, SdkVersion, Architecture);
                acquireOutput.WriteLine("Dotnet path: " + result.DotNetExe);
                if (!File.Exists(result.DotNetExe))
                {
                    throw new FileNotFoundException(result.DotNetExe + " not found");
                }
                if (result.SdkVersion != null)
                {
                    if (!Directory.Exists(result.SdkDir))
                    {
                        throw new DirectoryNotFoundException("Sdk directory " + result.SdkDir + " not found");
                    }
                }
                if (result.FrameworkVersion != null)
                {
                    if (!Directory.Exists(result.FrameworkDir))
                    {
                        throw new DirectoryNotFoundException("Framework directory " + result.FrameworkDir + " not found");
                    }

                    //overlay private binaries if needed
                    if (PrivateRuntimeBinaryDirPath != null)
                    {
                        foreach (string fileName in GetPrivateRuntimeOverlayBinaryNames(OS))
                        {
                            string backupPath = Path.Combine(result.FrameworkDir, fileName + ".original");
                            string overwritePath = Path.Combine(result.FrameworkDir, fileName);
                            string privateBinPath = Path.Combine(PrivateRuntimeBinaryDirPath, fileName);
                            if (!File.Exists(backupPath))
                            {
                                File.Copy(overwritePath, backupPath);
                            }
                            if (!File.Exists(privateBinPath))
                            {
                                throw new FileNotFoundException("Private binary " + privateBinPath + " not found");
                            }
                            File.Copy(privateBinPath, overwritePath, true);
                        }
                    }
                }
                return result;
            }
        }

        public static string DefaultFrameworkVersion {  get { return "2.0.0"; } }

        public static string DefaultAzureFeed { get { return "https://dotnetcli.azureedge.net/dotnet"; } }

        public static OSPlatform DefaultOSPlatform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return OSPlatform.Windows;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return OSPlatform.Linux;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return OSPlatform.OSX;
                throw new Exception("Unable to detect current OS");
            }
        }

        public static string GetNormalizedOSName(OSPlatform os)
        {
            if (os == OSPlatform.Windows)
            {
                return "win";
            }
            else if (os == OSPlatform.Linux)
            {
                return "linux";
            }
            else if (os == OSPlatform.OSX)
            {
                return "osx";
            }
            else
            {
                throw new Exception("OS " + os + " wasn't recognized. No normalized name for dotnet download is available");
            }
        }

        

        public static string GetRuntimeDownloadLink(string version, Architecture arch)
        {
            return GetFrameworkDownloadLink(DefaultAzureFeed, version, DefaultOSPlatform, arch);
        }

        public static string GetFrameworkDownloadLink(string azureFeed, string version, OSPlatform os, Architecture arch)
        {
            return GetRuntimeDownloadLink(azureFeed, version, GetNormalizedOSName(os), DotNetInstallation.GetNormalizedArchitectureName(arch));
        }

        public static string GetRuntimeDownloadLink(string azureFeed, string version, string os, string arch)
        {
            return string.Format("{0}/Runtime/{1}/dotnet-runtime-{1}-{2}-{3}.{4}", azureFeed, version, os, arch, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz");
        }

        public static string GetSDKDownloadLink(string version, Architecture arch)
        {
            return GetSDKDownloadLink(DefaultAzureFeed, version, DefaultOSPlatform, arch);
        }

        public static string GetSDKDownloadLink(string azureFeed, string version, OSPlatform os, Architecture arch)
        {
            return GetSDKDownloadLink(azureFeed, version, GetNormalizedOSName(os), DotNetInstallation.GetNormalizedArchitectureName(arch));
        }

        public static string GetSDKDownloadLink(string azureFeed, string version, string os, string arch)
        {
            return string.Format("{0}/Sdk/{1}/dotnet-sdk-{1}-{2}-{3}.{4}", azureFeed, version, os, arch, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz");
        }

        public static string GetTargetFrameworkMonikerForFrameworkVersion(string runtimeVersion)
        {
            if (runtimeVersion.StartsWith("3.0"))
            {
                return "netcoreapp3.0";
            }
            else if (runtimeVersion.StartsWith("2.2"))
            {
                return "netcoreapp2.2";
            }
            else if (runtimeVersion.StartsWith("2.1"))
            {
                return "netcoreapp2.1";
            }
            else if (runtimeVersion.StartsWith("2.0"))
            {
                return "netcoreapp2.0";
            }
            else
            {
                throw new NotSupportedException("Version " + runtimeVersion + " doesn't have a known TFM");
            }
        }

        public static string GetCompatibleDefaultSDKVersionForRuntimeVersion(string runtimeVersion)
        {
            return GetCompatibleDefaultSDKVersionForRuntimeTFM(
                GetTargetFrameworkMonikerForFrameworkVersion(runtimeVersion));
        }

        public static string GetCompatibleDefaultSDKVersionForRuntimeTFM(string targetFrameworkMoniker)
        {
            if (targetFrameworkMoniker == "netcoreapp3.0")
            {
                return "3.0.100-alpha1-009410";
            }
            else if (targetFrameworkMoniker == "netcoreapp2.2")
            {
                return "2.2.100-preview2-009404";
            }
            else if (targetFrameworkMoniker == "netcoreapp2.1")
            {
                return "2.2.0-preview1-007558";
            }
            else if (targetFrameworkMoniker == "netcoreapp2.0")
            {
                return "2.0.9";
            }
            else
            {
                throw new Exception("No compatible SDK version has been designated for TFM: " + targetFrameworkMoniker);
            }
        }

        public static string[] GetPrivateRuntimeOverlayBinaryNames(OSPlatform os)
        {
            return new string[]
            {
                GetNativeDllNameConvention("coreclr", os),
                GetNativeDllNameConvention("clrjit", os),
                GetNativeDllNameConvention("mscordaccore", os),
                GetNativeDllNameConvention("mscordbi", os),
                GetNativeDllNameConvention("sos", os),
                "sos.NETCore.dll",
                GetNativeDllNameConvention("clretwrc", os),
                "System.Private.CoreLib.dll",
                "mscorrc.debug.dll",
                "mscorrc.dll"
            };
        }

        private static string GetNativeDllNameConvention(string baseName, OSPlatform os)
        {
            if(os == OSPlatform.Windows)
            {
                return baseName + ".dll";
            }
            else
            {
                return "lib" + baseName;
            }
        }


    }

    public class DotNetInstallation
    {
        public DotNetInstallation(string dotNetDir, string frameworkVersion, string sdkVersion, Architecture architecture)
        {
            DotNetDir = dotNetDir;
            FrameworkVersion = frameworkVersion;
            SdkVersion = sdkVersion;
            Architecture = GetNormalizedArchitectureName(architecture);
            DotNetExe = Path.Combine(DotNetDir, "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
            if(frameworkVersion != null)
            {
                FrameworkDir = GetFrameworkDir(dotNetDir, frameworkVersion);
            }
            if(sdkVersion != null)
            {
                SdkDir = GetSDKDir(dotNetDir, sdkVersion);
            }
        }

        public string DotNetExe { get; } 
        public string DotNetDir { get; }
        public string FrameworkDir { get; }
        public string FrameworkVersion { get; }
        public string SdkDir { get; }
        public string SdkVersion { get; }
        public string Architecture { get; }

        public static string GetMNAppDir(string dotNetDir)
        {
            return Path.Combine(dotNetDir, "shared", "Microsoft.NETCore.App");
        }

        public static string GetFrameworkDir(string dotNetDir, string frameworkVersion)
        {
            return Path.Combine(GetMNAppDir(dotNetDir), frameworkVersion);
        }

        public static string GetSDKDir(string dotNetDir, string sdkVersion)
        {
            return Path.Combine(dotNetDir, "sdk", sdkVersion);
        }

        public static string GetNormalizedArchitectureName(Architecture arch)
        {
            if (arch == System.Runtime.InteropServices.Architecture.X64)
            {
                return "x64";
            }
            else if (arch == System.Runtime.InteropServices.Architecture.X86)
            {
                return "x86";
            }
            else
            {
                throw new Exception("Architecture " + arch + " wasn't recognized. No normalized name for dotnet download is available");
            }
        }
    }

}
