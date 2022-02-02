// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;
        private static volatile int s_osArchPlusOne;

        public static string OSDescription
        {
            get
            {
                string? osDescription = s_osDescription;
                if (osDescription is null)
                {
                    OperatingSystem os = Environment.OSVersion;
                    Version v = os.Version;

                    Span<char> stackBuffer = stackalloc char[256];
                    const string Version = "Microsoft Windows";
                    s_osDescription = osDescription = string.IsNullOrEmpty(os.ServicePack) ?
                        string.Create(null, stackBuffer, $"{Version} {(uint)v.Major}.{(uint)v.Minor}.{(uint)v.Build}") :
                        string.Create(null, stackBuffer, $"{Version} {(uint)v.Major}.{(uint)v.Minor}.{(uint)v.Build} {os.ServicePack}");
                }

                return osDescription;
            }
        }

        public static Architecture OSArchitecture
        {
            get
            {
                int osArch = s_osArchPlusOne - 1;

                if (osArch < 0)
                {
                    Interop.Kernel32.SYSTEM_INFO sysInfo;
                    unsafe
                    {
                        Interop.Kernel32.GetNativeSystemInfo(&sysInfo);
                    }
                    osArch = (int)Map(sysInfo.wProcessorArchitecture);

                    s_osArchPlusOne = osArch + 1;
                }

                Debug.Assert(osArch >= 0);
                return (Architecture)osArch;
            }
        }

        private static Architecture Map(int processorArchitecture)
        {
            switch (processorArchitecture)
            {
                case Interop.Kernel32.PROCESSOR_ARCHITECTURE_ARM64:
                    return Architecture.Arm64;
                case Interop.Kernel32.PROCESSOR_ARCHITECTURE_ARM:
                    return Architecture.Arm;
                case Interop.Kernel32.PROCESSOR_ARCHITECTURE_AMD64:
                    return Architecture.X64;
                case Interop.Kernel32.PROCESSOR_ARCHITECTURE_INTEL:
                default:
                    Debug.Assert(processorArchitecture == Interop.Kernel32.PROCESSOR_ARCHITECTURE_INTEL, "Unidentified Architecture");
                    return Architecture.X86;
            }
        }
    }
}
