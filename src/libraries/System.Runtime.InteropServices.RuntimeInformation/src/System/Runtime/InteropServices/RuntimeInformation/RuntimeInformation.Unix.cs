// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static readonly object s_lock = new object();
        private static string? s_osPlatformName;
        private static string? s_osDescription;
        private static volatile int s_osArch = -1;
        private static volatile int s_processArch = -1;

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            string name = s_osPlatformName ??= Interop.Sys.GetUnixName();
            return osPlatform.Equals(name);
        }

        public static string OSDescription => s_osDescription ??= Interop.Sys.GetUnixVersion();

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
                            Interop.Sys.ProcessorArchitecture arch = (Interop.Sys.ProcessorArchitecture)Interop.Sys.GetOSArchitecture();
                            switch (arch)
                            {
                                case Interop.Sys.ProcessorArchitecture.ARM:
                                    s_osArch = (int)Architecture.Arm;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.x64:
                                    s_osArch = (int)Architecture.X64;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.x86:
                                    s_osArch = (int)Architecture.X86;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.ARM64:
                                    s_osArch = (int)Architecture.Arm64;
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
                    lock (s_lock)
                    {
                        if (s_processArch == -1)
                        {
                            Interop.Sys.ProcessorArchitecture arch = (Interop.Sys.ProcessorArchitecture)Interop.Sys.GetProcessArchitecture();
                            switch (arch)
                            {
                                case Interop.Sys.ProcessorArchitecture.ARM:
                                    s_processArch = (int)Architecture.Arm;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.x64:
                                    s_processArch = (int)Architecture.X64;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.x86:
                                    s_processArch = (int)Architecture.X86;
                                    break;

                                case Interop.Sys.ProcessorArchitecture.ARM64:
                                    s_processArch = (int)Architecture.Arm64;
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
