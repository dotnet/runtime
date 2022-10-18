namespace Melanzana.MachO
{
    [Flags]
    public enum MachSymbolDescriptor : ushort
    {
        ReferenceTypeMask = 0xf,
        UndefinedNonLazy = 0,
        UndefinedLazy = 1,
        Defined = 2,
        PrivateDefined = 3,
        PrivateUndefinedNonLazy = 4,
        PrivateUndefinedLazy = 5,

        ReferencedDynamically = 0x10,
        NoDeadStrip = 0x20,
        WeakReference = 0x40,
        WeakDefinition = 0x80,
    }
}
