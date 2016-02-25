// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static readonly object s_osLock = new object();
        private static readonly object s_processLock = new object();
        private static readonly bool s_is64BitProcess = IntPtr.Size == 8;
        private static string s_osPlatformName;
        private static string s_osDescription;
        private static Architecture? s_osArch;
        private static Architecture? s_processArch;

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            string name = s_osPlatformName ?? (s_osPlatformName = Interop.Sys.GetUnixName());
            return osPlatform.Equals(name);
        }

        public static string OSDescription
        {
            get
            {
                if (null == s_osDescription)
                {
                    s_osDescription = Interop.Sys.GetUnixVersion();
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
                        Interop.Sys.ProcessorArchitecture arch = (Interop.Sys.ProcessorArchitecture)Interop.Sys.GetOSArchitecture();
                        switch (arch)
                        {
                            case Interop.Sys.ProcessorArchitecture.ARM:
                                s_osArch = Architecture.Arm;
                                break;

                            case Interop.Sys.ProcessorArchitecture.x64:
                                s_osArch = Architecture.X64;
                                break;

                            case Interop.Sys.ProcessorArchitecture.x86:
                                s_osArch = Architecture.X86;
                                break;

                            case Interop.Sys.ProcessorArchitecture.ARM64:
                                s_osArch = Architecture.Arm64;
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
                        Interop.Sys.ProcessorArchitecture arch = (Interop.Sys.ProcessorArchitecture)Interop.Sys.GetProcessArchitecture();
                        switch (arch)
                        {
                            case Interop.Sys.ProcessorArchitecture.ARM:
                                s_processArch = Architecture.Arm;
                                break;

                            case Interop.Sys.ProcessorArchitecture.x64:
                                s_processArch = Architecture.X64;
                                break;

                            case Interop.Sys.ProcessorArchitecture.x86:
                                s_processArch = Architecture.X86;
                                break;

                            case Interop.Sys.ProcessorArchitecture.ARM64:
                                s_processArch = Architecture.Arm64;
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
