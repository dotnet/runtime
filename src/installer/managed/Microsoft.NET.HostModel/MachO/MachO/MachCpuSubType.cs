// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    /// <summary>
    /// Defines the general flags for CPU subtypes. Depending on the <see cref="MachCpuType"/>,
    /// you should cast the value to a more specialized enumeration, such as <see cref="MachArmCpuSubType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    internal enum MachCpuSubType : uint
    {
        /// <summary>
        /// All subtypes.
        /// </summary>
        All = 0,
    }
}
