// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.AppHost;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// TargetInfo: Information about the target for which the single-file bundle is built.
    /// 
    /// Currently the TargetInfo only tracks:
    ///   - the target operating system
    ///   - The target framework
    /// If necessary, the target architecture may be tracked in future.
    /// </summary>

    public class TargetInfo
    {
        public readonly OSPlatform OS;
        public readonly Version FrameworkVersion;
        public readonly uint BundleVersion;
        public readonly BundleOptions DefaultOptions;

        public TargetInfo(OSPlatform? os, Version targetFrameworkVersion)
        {
            OS = os ?? HostOS;
            FrameworkVersion = targetFrameworkVersion ?? net50;

            Debug.Assert(IsLinux || IsOSX || IsWindows);

            if(FrameworkVersion.CompareTo(net50) >= 0)
            {
                BundleVersion = 2u;
                DefaultOptions = BundleOptions.None;
            }
            else if(FrameworkVersion.Major == 3 && (FrameworkVersion.Minor == 0 || FrameworkVersion.Minor == 1))
            {
                BundleVersion = 1u;
                DefaultOptions = BundleOptions.BundleAllContent;
            }
            else
            {
                throw new ArgumentException($"Invalid input: Unsupported Target Framework Version {targetFrameworkVersion}");
            }
        }

        public bool IsNativeBinary(string filePath)
        {
            return IsLinux ? ElfUtils.IsElfImage(filePath) : IsOSX ? MachOUtils.IsMachOImage(filePath) : PEUtils.IsPEImage(filePath);
        }

        public string GetAssemblyName(string hostName)
        {
            // This logic to calculate assembly name from hostName should be removed (and probably moved to test helpers)
            // once the SDK in the correct assembly name.
            return (IsWindows ? Path.GetFileNameWithoutExtension(hostName) : hostName);
        }

        public override string ToString()
        {
            string os = IsWindows ? "win" : IsLinux ? "linux" : "osx";
            return $"OS: {os} FrameworkVersion: {FrameworkVersion}";
        }

        static OSPlatform HostOS => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux :
                                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : OSPlatform.Windows;

        public bool IsLinux => OS.Equals(OSPlatform.Linux);
        public bool IsOSX => OS.Equals(OSPlatform.OSX);
        public bool IsWindows => OS.Equals(OSPlatform.Windows);

        // The .net core 3 apphost doesn't care about semantics of FileType -- all files are extracted at startup.
        // However, the apphost checks that the FileType value is within expected bounds, so set it to the first enumeration.
        public FileType TargetSpecificFileType(FileType fileType) => (BundleVersion == 1) ? FileType.Unknown : fileType;

        // In .net core 3.x, bundle processing happens within the AppHost.
        // Therefore HostFxr and HostPolicy can be bundled within the single-file app.
        // In .net 5, bundle processing happens in HostFxr and HostPolicy libraries.
        // Therefore, these libraries themselves cannot be bundled into the single-file app.
        // This problem is mitigated by statically linking these host components with the AppHost.
        // https://github.com/dotnet/runtime/issues/32823
        public bool ShouldExclude(string relativePath) =>
            (FrameworkVersion.Major != 3) && (relativePath.Equals(HostFxr) || relativePath.Equals(HostPolicy));

        readonly Version net50 = new Version(5, 0);
        string HostFxr => IsWindows ? "hostfxr.dll" : IsLinux ? "libhostfxr.so" : "libhostfxr.dylib";
        string HostPolicy => IsWindows ? "hostpolicy.dll" : IsLinux ? "libhostpolicy.so" : "libhostpolicy.dylib";


    }
}

