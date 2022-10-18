namespace Melanzana.MachO
{
    /// <summary>
    /// Defines the subtypes of the ARM <see cref="MachCpuType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    public enum MachArmCpuSubType : uint
    {
        /// <summary>
        /// All ARM subtypes.
        /// </summary>
        All = MachCpuSubType.All,

        /// <summary>
        /// The ARMv4T architecture CPU. Part of the ARM7TDMI family.
        /// </summary>
        V4T = 5,

        /// <summary>
        /// The ARMv6 architecture CPU. Part of the ARMv6 family.
        /// </summary>
        V6 = 6,

        /// <summary>
        /// The ARMv5TEJ architecture CPU. Part of the ARM9E family.
        /// </summary>
        V5TEJ = 7,

        /// <summary>
        /// The XScale family of ARMv5TE CPUs.
        /// </summary>
        XSCALE = 8,

        /// <summary>
        /// The ARMv7 CPU
        /// </summary>
        V7 = 9,

        /// <summary>
        /// The ARMv7F CPU.
        /// </summary>
        V7F = 10,

        /// <summary>
        /// The ARMv7S CPU.
        /// </summary>
        V7S = 11,

        /// <summary>
        /// The ARMv7K CPU.
        /// </summary>
        V7K = 12,

        /// <summary>
        /// An ARMv8 architecture CPU.
        /// </summary>
        V8 = 13,

        /// <summary>
        /// The ARMv6-M architecture CPU. Part of the Cortex-M family.
        /// </summary>
        V6M = 14,

        /// <summary>
        /// The ARMv7-M architecture CPU. Part of the Cortex-M family.
        /// </summary>
        V7M = 15,

        /// <summary>
        /// An ARMv7E-M architecture CPU. Part of the Cortex-M family.
        /// </summary>
        V7EM = 16,

        /// <summary>
        /// An ARMv8M architecture CPU.
        /// </summary>
        V8M = 17,
    }
}
