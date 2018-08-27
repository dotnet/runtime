// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string s_osDescription = null;
        private static object s_osLock = new object();
        private static object s_processLock = new object();
        private static Architecture? s_osArch = null;
        private static Architecture? s_processArch = null;

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return OSPlatform.Windows == osPlatform;
        }

        public static string OSDescription
        {
            get
            {
                if (null == s_osDescription)
                {
#if uap || uapaot // all these are subject to WACK
                    s_osDescription = "Microsoft Windows";
#else
                    s_osDescription = Interop.NtDll.RtlGetVersion();
#endif
                }

                return s_osDescription;
            }
        }

        public static Architecture OSArchitecture
        {
            get
            {
                lock (s_osLock)
                {
                    if (null == s_osArch)
                    {
                        Interop.Kernel32.SYSTEM_INFO sysInfo;
                        Interop.Kernel32.GetNativeSystemInfo(out sysInfo);

                        switch ((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture)
                        {
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64:
                                s_osArch = Architecture.Arm64;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM:
                                s_osArch = Architecture.Arm;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64:
                                s_osArch = Architecture.X64;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL:
                                s_osArch = Architecture.X86;
                                break;
                        }

                    }
                }

                Debug.Assert(s_osArch != null);
                return s_osArch.Value;
            }
        }

        public static Architecture ProcessArchitecture
        {
            get
            {
                lock (s_processLock)
                {
                    if (null == s_processArch)
                    {
                        Interop.Kernel32.SYSTEM_INFO sysInfo;
                        Interop.Kernel32.GetSystemInfo(out sysInfo);

                        switch((Interop.Kernel32.ProcessorArchitecture)sysInfo.wProcessorArchitecture)
                        {
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64:
                                s_processArch = Architecture.Arm64;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM:
                                s_processArch = Architecture.Arm;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64:
                                s_processArch = Architecture.X64;
                                break;
                            case Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_INTEL:
                                s_processArch = Architecture.X86;
                                break;
                        }
                    }
                }

                Debug.Assert(s_processArch != null);
                return s_processArch.Value;
            }
        }
    }
}
