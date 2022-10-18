namespace Melanzana.MachO
{
    public enum MachRelocationType : byte
    {
        GenericVanilla = 0,
        GenericPair = 1,
        GenericSectionDiff = 2,
        GenericPreboundLazyPtr = 3,
        GenericLocalSectionDiff = 4,
        GenericTlv = 5,

        X86_64Unsigned = 0,
        X86_64Signed = 1,
        X86_64Branch = 2,
        X86_64GotLoad = 3,
        X86_64Got = 4,
        X86_64Subtractor = 5,
        X86_64Signed1 = 6,
        X86_64Signed2 = 7,
        X86_64Signed4 = 8,
        X86_64Tlv = 9,

        Arm64Unsigned = 0,
        Arm64Subtractor = 1,
        Arm64Branch26 = 2,
        Arm64Page21 = 3,
        Arm64PageOffset21 = 4,
        Arm64GotLoadPage21 = 5,
        Arm64GotLoadPageOffset21 = 6,
        Arm64PointerToGot = 7,
        Arm64TlvpLoadPage21 = 8,
        Arm64TlvpLoadPageOffset21 = 9,
        Arm64Addend = 10,
    }
}
