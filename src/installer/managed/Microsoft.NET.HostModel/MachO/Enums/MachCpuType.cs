// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/xnu/blob/xnu-11215.1.10/osfmk/mach/machine.h#L131-L156
/// </summary>
internal enum MachCpuType : uint
{
    // Architecture mask bits.
    Abi64 = 0x01000000,       // CPU_ARCH_ABI64: 64-bit ABI
    Abi64_32 = 0x02000000,    // CPU_ARCH_ABI64_32: ABI for 64-bit hardware with 32-bit types

    // Base CPU type.
    Arm = 12,                 // CPU_TYPE_ARM

    // Combined CPU types.
    Arm64 = Arm | Abi64,      // CPU_TYPE_ARM64
    Arm64_32 = Arm | Abi64_32, // CPU_TYPE_ARM64_32
}
