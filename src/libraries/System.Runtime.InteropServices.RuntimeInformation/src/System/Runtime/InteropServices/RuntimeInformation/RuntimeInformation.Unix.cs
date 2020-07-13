// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osPlatformName;
        private static string? s_osDescription;
        private static readonly Architecture s_osArch = Map((Interop.Sys.ProcessorArchitecture)Interop.Sys.GetOSArchitecture());
        private static readonly Architecture s_processArch = Map((Interop.Sys.ProcessorArchitecture)Interop.Sys.GetProcessArchitecture());

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            string name = s_osPlatformName ??= Interop.Sys.GetUnixName();
            return osPlatform.Equals(name);
        }

        public static string OSDescription => s_osDescription ??= Interop.Sys.GetUnixVersion();

        public static Architecture OSArchitecture => s_osArch;

        public static Architecture ProcessArchitecture => s_processArch;

        private static Architecture Map(Interop.Sys.ProcessorArchitecture arch)
        {
            switch (arch)
            {
                case Interop.Sys.ProcessorArchitecture.ARM:
                    return Architecture.Arm;
                case Interop.Sys.ProcessorArchitecture.x64:
                    return Architecture.X64;
                case Interop.Sys.ProcessorArchitecture.ARM64:
                    return Architecture.Arm64;
                case Interop.Sys.ProcessorArchitecture.x86:
                default:
                    Debug.Assert(arch == Interop.Sys.ProcessorArchitecture.x86, "Unidentified Architecture");
                    return Architecture.X86;
            }
        }
    }
}
