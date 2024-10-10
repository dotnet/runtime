// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    /// <summary>
    /// Defines the subtypes of the x86_64 <see cref="MachCpuType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    internal enum X8664CpuSubType : uint
    {
        /// <summary>
        /// All x86_64 CPUs
        /// </summary>
        All = 3,

        /// <summary>
        /// Haswell feature subset.
        /// </summary>
        Haswell = 8,
    }
}
