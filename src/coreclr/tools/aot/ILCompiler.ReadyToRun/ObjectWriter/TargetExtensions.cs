// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Per-OS machine overrides. Corresponds to CoreCLR constants
    /// IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE.
    /// </summary>
    public enum MachineOSOverride : ushort
    {
        Windows = 0,
        Linux = 0x7B79,
        Apple = 0x4644,
        FreeBSD = 0xADC4,
        NetBSD = 0x1993,
        SunOS = 0x1992,
    }

    /// <summary>
    /// Constants for emission of Windows PE file mostly copied from CoreCLR pewriter.cpp.
    /// </summary>
    public static class PEHeaderConstants
    {
        public const int SectionAlignment = 0x1000;

        public const byte MajorLinkerVersion = 11;
        public const byte MinorLinkerVersion = 0;

        public const byte MajorOperatingSystemVersion = 4;
        public const byte MinorOperatingSystemVersion = 0;

        public const ushort MajorImageVersion = 0;
        public const ushort MinorImageVersion = 0;

        public const ushort MajorSubsystemVersion = 4;
        public const ushort MinorSubsystemVersion = 0;
    }

    public static class PE32HeaderConstants
    {
        public const uint ImageBase = 0x0040_0000;

        public const uint SizeOfStackReserve = 0x100000;
        public const uint SizeOfStackCommit = 0x1000;
        public const uint SizeOfHeapReserve = 0x100000;
        public const uint SizeOfHeapCommit = 0x1000;
    }

    public static class PE64HeaderConstants
    {
        // Default base addresses used by Roslyn
        public const ulong ExeImageBase = 0x1_4000_0000;
        public const ulong DllImageBase = 0x1_8000_0000;

        public const ulong SizeOfStackReserve = 0x400000;
        public const ulong SizeOfStackCommit = 0x4000;
        public const ulong SizeOfHeapReserve = 0x100000;
        public const ulong SizeOfHeapCommit = 0x2000;
    }

    public static class TargetExtensions
    {
        /// <summary>
        /// Calculate machine ID based on compilation target architecture.
        /// </summary>
        /// <param name="target">Compilation target environment specification</param>
        /// <returns></returns>
        public static Machine MachineFromTarget(this TargetDetails target)
        {
            switch (target.Architecture)
            {
                case Internal.TypeSystem.TargetArchitecture.X64:
                    return Machine.Amd64;

                case Internal.TypeSystem.TargetArchitecture.X86:
                    return Machine.I386;

                case Internal.TypeSystem.TargetArchitecture.ARM64:
                    return Machine.Arm64;

                case Internal.TypeSystem.TargetArchitecture.ARM:
                    return Machine.ArmThumb2;

                case Internal.TypeSystem.TargetArchitecture.LoongArch64:
                    return Machine.LoongArch64;

                case Internal.TypeSystem.TargetArchitecture.RiscV64:
                    return Machine.RiscV64;

                default:
                    throw new NotImplementedException(target.Architecture.ToString());
            }
        }

        /// <summary>
        /// Determine OS machine override for the target operating system.
        /// </summary>
        public static MachineOSOverride MachineOSOverrideFromTarget(this TargetDetails target)
        {
            switch (target.OperatingSystem)
            {
                case TargetOS.Windows:
                    return MachineOSOverride.Windows;

                case TargetOS.Linux:
                    return MachineOSOverride.Linux;

                case TargetOS.OSX:
                    return MachineOSOverride.Apple;

                case TargetOS.FreeBSD:
                    return MachineOSOverride.FreeBSD;

                case TargetOS.NetBSD:
                    return MachineOSOverride.NetBSD;

                default:
                    throw new NotImplementedException(target.OperatingSystem.ToString());
            }
        }
    }
}
