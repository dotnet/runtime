namespace Melanzana.MachO
{
    /// <summary>
    /// Defines the subtypes of the ARM 64 <see cref="MachCpuType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    [Flags]
    public enum MachArm64CpuSubType : uint
    {
        /// <summary>
        /// All ARM64 subtypes.
        /// </summary>
        All = MachCpuSubType.All,

        /// <summary>
        /// The ARM64v8 CPU architecture subtype.
        /// </summary>
        V8 = 1,

        /// <summary>
        /// The ARM64e CPU architecture subtype.
        /// </summary>
        E = 2,

        /// <summary>
        /// Pointer authentication with versioned ABI.
        /// </summary>
        PointerAuthenticationWithVersionedAbi = 0x80000000,
    }
}
