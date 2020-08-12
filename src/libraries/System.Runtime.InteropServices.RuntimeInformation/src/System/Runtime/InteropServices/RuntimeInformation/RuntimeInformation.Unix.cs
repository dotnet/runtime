// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;

        public static string OSDescription => s_osDescription ??= Interop.Sys.GetUnixVersion();

        public static Architecture OSArchitecture { get; } = Map((Interop.Sys.ProcessorArchitecture)Interop.Sys.GetOSArchitecture());

        public static Architecture ProcessArchitecture { get; } = Map((Interop.Sys.ProcessorArchitecture)Interop.Sys.GetProcessArchitecture());

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
                case Interop.Sys.ProcessorArchitecture.WASM:
                    return Architecture.Wasm;
                case Interop.Sys.ProcessorArchitecture.x86:
                default:
                    Debug.Assert(arch == Interop.Sys.ProcessorArchitecture.x86, "Unidentified Architecture");
                    return Architecture.X86;
            }
        }
    }
}
