// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string s_osDescription = null;
        private static readonly bool s_is64BitProcess = IntPtr.Size == 8;
        private static object s_osLock = new object();
        private static object s_processLock = new object();
        private static Architecture? s_osArch = null;
        private static Architecture? s_processArch = null;

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
                        Interop.Sys.ProcessorArchitecture arch = (Interop.Sys.ProcessorArchitecture)Interop.Sys.GetUnixArchitecture();
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
                        if (Architecture.Arm == OSArchitecture)
                        {
                            s_processArch = Architecture.Arm;
                        }
                        else if (s_is64BitProcess)
                        {
                            s_processArch = Architecture.X64;
                        }
                        else
                        {
                            s_processArch = Architecture.X86;
                        }
                    }
                }

                Debug.Assert(s_processArch != null);
                return s_processArch.Value;
            }
        }
    }
}
