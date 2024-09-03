namespace Melanzana.MachO
{
    public enum MachCpuType : uint
    {
        Vax = 1,
        M68k = 6,
        X86 = 7,
        X86_64 = X86 | Architecture64,
        M98k = 10,
        PaRisc = 11,
        Arm = 12,
        Arm64 = Arm | Architecture64,
        M88k = 13,
        Sparc = 14,
        I860 = 15,
        PowerPC = 18,
        PowerPC64 = PowerPC | Architecture64,
        Architecture64 = 0x1000000,
    }
}
