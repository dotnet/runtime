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

        public static unsafe Architecture OSArchitecture
        {
            get
            {
                int osArch = s_osArchPlusOne - 1;

                if (osArch < 0)
                {
                    // If we are running an x64 process on a non-x64 windows machine, we will report x64 as OS architecutre.
                    //
                    // IsWow64Process2 is only available on Windows 10+, so we will perform run-time introspection via indirect load
                    if (NativeLibrary.TryGetExport(NativeLibrary.Load(Interop.Libraries.Kernel32), "IsWow64Process2", out IntPtr isWow64Process2Ptr))
                    {
                        ushort processMachine, nativeMachine;
                        var isWow64Process2 = (delegate* unmanaged<IntPtr, ushort*, ushort*, int>)isWow64Process2Ptr;
                        if (isWow64Process2(Interop.Kernel32.GetCurrentProcess(), &processMachine, &nativeMachine) != 0)
                        {
                            osArch = (int)MapMachineConstant(nativeMachine);
                        }
                        else
                        {
                            Debug.Fail("Call to IsWow64Process2() failed unexpectedly. Falling back to ProcessArchitecture");
                            osArch = (int)ProcessArchitecture;
                        }
                    }
                    else
                    {
                        Interop.Kernel32.SYSTEM_INFO sysInfo;
                        Interop.Kernel32.GetNativeSystemInfo(&sysInfo);

                        osArch = (int)Map(sysInfo.wProcessorArchitecture);
                    }

                    s_osArchPlusOne = osArch + 1;

                    Debug.Assert(osArch >= 0);
                }

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

        private static Architecture MapMachineConstant(ushort processMachine)
        {
            switch (processMachine)
            {
                case 0x01C4: // IMAGE_FILE_MACHINE_ARMNT
                    return Architecture.Arm;

                case 0x8664: // IMAGE_FILE_MACHINE_AMD64
                    return Architecture.X64;

                case 0xAA64: // IMAGE_FILE_MACHINE_ARM64
                    return Architecture.Arm64;

                case 0x014C: // IMAGE_FILE_MACHINE_I386
                    return Architecture.X86;

                default: // IMAGE_FILE_MACHINE_UNKNOWN etc.
                    Debug.Fail("Unidentified OS Architecture. Falling back to ProcessArchitecture");
                    return ProcessArchitecture;
            }
        }
    }
}
