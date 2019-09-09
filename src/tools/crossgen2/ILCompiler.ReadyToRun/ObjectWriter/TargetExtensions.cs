// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
