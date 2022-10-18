namespace Melanzana.MachO
{
    /// <summary>
    /// Defines the subtypes of the i386 <see cref="MachCpuType"/>.
    /// Defined in <c>machine.h</c>.
    /// </summary>
    /// <remarks>
    /// This enumeration matches version 7195.141.2 of XNU.
    /// </remarks>
    /// <seealso href="https://opensource.apple.com/source/xnu/xnu-7195.141.2/osfmk/mach/machine.h"/>
    public enum MachI386CpuSubType : uint
    {
        /// <summary>
        /// All i386 subtypes
        /// </summary>
        All = 3 + (0 << 4),

        /// <summary>
        /// The Intel 386 processor.
        /// </summary>
        _386 = 3 + (0 << 4),

        /// <summary>
        /// The Intel 486 processor.
        /// </summary>
        _486 = 4 + (0 << 4),

        /// <summary>
        /// The Intel 486SX processor.
        /// </summary>
        _486SX = 4 + (8 << 4),

        /// <summary>
        /// The Intel 586 processor.
        /// </summary>
        _586 = 5 + (0 << 4),

        /// <summary>
        /// The Intel Pentium processor.
        /// </summary>
        Pentium = 5 + (0 << 4),

        /// <summary>
        /// The Intel Pentium Pro processor.
        /// </summary>
        PentiumPro = 6 + (1 << 4),

        /// <summary>
        /// The Intel Pentium II (M3) processor.
        /// </summary>
        PentiumIIM3 = 6 + (3 << 4),

        /// <summary>
        /// The Intel Penium II (M5) processor.
        /// </summary>
        PentiumIIM5 = 6 + (5 << 4),

        /// <summary>
        /// The Intel Celeron processor.
        /// </summary>
        Celeron = 7 + (6 << 4),

        /// <summary>
        /// The Intel Celeron Mobile processor.
        /// </summary>
        CeleronMobile = 7 + (7 << 4),

        /// <summary>
        /// The Intel Pentium 3 processor.
        /// </summary>
        Pentium3 = 8 + (0 << 4),

        /// <summary>
        /// The Intel Pentium 3 M processor.
        /// </summary>
        Pentium3M = 8 + (1 << 4),

        /// <summary>
        /// The Intel Pentium 3 Xeon processor.
        /// </summary>
        Pentium3Xeon = 8 + (2 << 4),

        /// <summary>
        /// The Intel Pentium M processor.
        /// </summary>
        PentiumM = 9 + (0 << 4),

        /// <summary>
        /// The Intel Pentium 4 processor.
        /// </summary>
        Pentium4 = 10 + (0 << 4),

        /// <summary>
        /// The Intel Pentium 4 M processor.
        /// </summary>
        Pentium4M = 10 + (1 << 4),

        /// <summary>
        /// The Intel Itanium processor.
        /// </summary>
        Itanium = 11 + (0 << 4),

        /// <summary>
        /// The Intel Itanium 2 processor.
        /// </summary>
        Itanium2 = 11 + (1 << 4),

        /// <summary>
        /// The Intel Xeon processor.
        /// </summary>
        Xeon = 12 + (0 << 4),

        /// <summary>
        /// The Intel Xeon MP processor.
        /// </summary>
        XeonMP = 12 + (1 << 4),
    }
}
