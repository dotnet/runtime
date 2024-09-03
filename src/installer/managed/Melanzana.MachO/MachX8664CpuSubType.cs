namespace Melanzana.MachO
{
    /// <summary>
    /// Defines the subtypes of the x86_64 <see cref="MachCpuType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    public enum X8664CpuSubType : uint
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
