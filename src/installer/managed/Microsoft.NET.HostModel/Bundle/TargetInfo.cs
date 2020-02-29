// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.NET.HostModel.AppHost;
using System;
using System.Diagnostics;
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
        public readonly string Framework;
        public readonly uint BundleVersion;
        public readonly BundleOptions DefaultOptions;

        public TargetInfo(OSPlatform? os, string framework)
        {
            OS = os ?? HostOS;
            Framework = framework != null ? framework.ToLower() : "net5";

            Debug.Assert(IsLinux || IsOSX || IsWindows);
            switch (Framework)
            {
                case "netcoreapp3.0":
                case "netcoreapp3.1":
                    BundleVersion = 1u;
                    DefaultOptions = BundleOptions.BundleAllContent;
                    break;

                case "net5":
                    BundleVersion = 2u;
                    DefaultOptions = BundleOptions.None;
                    break;

                default:
                    throw new ArgumentException("Invalid input: Unsupported Target Framework");
            }
        }

        public bool IsNativeBinary(string filePath)
        {
            return IsLinux ? ElfUtils.IsElfImage(filePath) : IsOSX ? MachOUtils.IsMachOImage(filePath) : PEUtils.IsPEImage(filePath);
        }

        public override string ToString()
        {
            string os = IsWindows ? "win" : IsLinux ? "linux" : "osx";
            return string.Format($"{os}-{Framework}");
        }

        static OSPlatform HostOS => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux :
                                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : OSPlatform.Windows;

        public bool IsLinux => OS.Equals(OSPlatform.Linux);
        public bool IsOSX => OS.Equals(OSPlatform.OSX);
        public bool IsWindows => OS.Equals(OSPlatform.Windows);

        // The .net core 3 apphost doesn't care about semantics of FileType -- all files are extracted at startup.
        // However, the apphost checks that the FileType value is within expected bounds, so set it to the first enumeration.
        public FileType TargetSpecificFileType(FileType fileType) => (BundleVersion == 1) ? FileType.Unknown: fileType;
    }
}

