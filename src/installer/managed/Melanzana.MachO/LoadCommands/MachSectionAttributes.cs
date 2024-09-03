namespace Melanzana.MachO
{
    [Flags]
    public enum MachSectionAttributes : uint
    {
        LocalRelocations = 0x100,
        ExternalRelocations = 0x200,
        SomeInstructions = 0x400,
        Debug = 0x2000000,
        SelfModifyingCode = 0x4000000,
        LiveSupport = 0x8000000,
        NoDeadStrip = 0x10000000,
        StripStaticSymbols = 0x20000000,
        NoTableOfContents = 0x40000000,
        PureInstructions = 0x80000000,
    }
}