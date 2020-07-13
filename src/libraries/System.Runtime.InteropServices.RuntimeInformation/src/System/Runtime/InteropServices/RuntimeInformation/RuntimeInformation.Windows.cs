// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;
        private static readonly object s_lock = new object();
        private static volatile int s_osArch = -1;
        private static volatile int s_processArch = -1;

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return OSPlatform.Windows == osPlatform;
        }

        public static string OSDescription
        {
            get
            {
                string? osDescription = s_osDescription;
                if (osDescription is null)
                {
                    OperatingSystem os = Environment.OSVersion;
                    Version v = os.Version;

                    const string Version = "Microsoft Windows";
                    s_osDescription = osDescription = string.IsNullOrEmpty(os.ServicePack) ?
                        $"{Version} {(uint)v.Major}.{(uint)v.Minor}.{(uint)v.Build}" :
                        $"{Version} {(uint)v.Major}.{(uint)v.Minor}.{(uint)v.Build} {os.ServicePack}";
                }

                return osDescription;
            }
        }

        public static Architecture OSArchitecture
        {
            get
            {
                Debug.Assert(sizeof(Architecture) == sizeof(int));

                if (s_osArch == -1)
                {
                    lock (s_lock)
                    {
                        if (s_osArch == -1)
                        {
                            Interop.Kernel32.GetNativeSystemInfo(out Interop.Kernel32.SYSTEM_INFO sysInfo);

                            s_osArch = (int)Map((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture);
                        }
                    }
                }

                return (Architecture)s_osArch;
            }
        }

        public static Architecture ProcessArchitecture
        {
            get
            {
                Debug.Assert(sizeof(Architecture) == sizeof(int));

                if (s_processArch == -1)
                {
                    lock (s_lock)
                    {
                        if (s_processArch == -1)
                        {
                            Interop.Kernel32.GetSystemInfo(out Interop.Kernel32.SYSTEM_INFO sysInfo);

                            s_processArch = (int)Map((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture);
                        }
                    }
                }

                return (Architecture)s_processArch;
            }
        }

        private static Architecture Map(Interop.Kernel32.ProcessorArchitecture processorArchitecture)
        {
            switch (processorArchitecture)
            {
                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64:
                    return Architecture.Arm64;
                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM:
                    return Architecture.Arm;
                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64:
                    return Architecture.X64;
                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL:
                default:
                    Debug.Assert(processorArchitecture == Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL, "Unidentified Architecture");
                    return Architecture.X86;
            }
        }
    }
}
