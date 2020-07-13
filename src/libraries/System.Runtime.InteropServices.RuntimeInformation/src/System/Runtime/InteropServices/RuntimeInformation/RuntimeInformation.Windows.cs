// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;
        private static readonly object s_osLock = new object();
        private static readonly object s_processLock = new object();
        private static int s_osArch = -1;
        private static int s_processArch = -1;

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
                    lock (s_osLock)
                    {
                        if (s_osArch == -1)
                        {
                            Interop.Kernel32.SYSTEM_INFO sysInfo;
                            Interop.Kernel32.GetNativeSystemInfo(out sysInfo);

                            switch ((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture)
                            {
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64:
                                    s_osArch = (int)Architecture.Arm64;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM:
                                    s_osArch = (int)Architecture.Arm;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64:
                                    s_osArch = (int)Architecture.X64;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL:
                                    s_osArch = (int)Architecture.X86;
                                    break;
                            }
                        }
                    }
                }

                Debug.Assert(s_osArch != -1);
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
                    lock (s_processLock)
                    {
                        if (s_processArch == -1)
                        {
                            Interop.Kernel32.SYSTEM_INFO sysInfo;
                            Interop.Kernel32.GetSystemInfo(out sysInfo);

                            switch ((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture)
                            {
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64:
                                    s_processArch = (int)Architecture.Arm64;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM:
                                    s_processArch = (int)Architecture.Arm;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64:
                                    s_processArch = (int)Architecture.X64;
                                    break;
                                case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL:
                                    s_processArch = (int)Architecture.X86;
                                    break;
                            }
                        }
                    }
                }

                Debug.Assert(s_processArch != -1);
                return (Architecture)s_processArch;
            }
        }
    }
}
