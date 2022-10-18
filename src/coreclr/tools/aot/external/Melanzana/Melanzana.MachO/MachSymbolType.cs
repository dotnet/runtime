namespace Melanzana.MachO
{
    [Flags]
    public enum MachSymbolType : byte
    {
        Stab = 0xe0,
        PrivateExternal = 0x10,

        TypeMask = 0xe,

        Undefined = 0,
        External = 1,
        Section = 0xe,
        Prebound = 0xc,
        Indirect = 0xa,
    }
}
