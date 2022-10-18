namespace Melanzana.MachO
{
    [Flags]
    public enum MachHeaderFlags : uint
    {
        NoUndefinedReferences = 0x1,
        IncrementalLink = 0x2,
        DynamicLink = 0x4,
        BindAtLoad = 0x8,
        Prebound = 0x10,
        SplitSegments = 0x20,
        LazyInit = 0x40,
        TwoLevel = 0x80,
        ForceFlat = 0x100,
        NoMultiDefs = 0x200,
        NoFixPrebinding = 0x400,
        Prebindable = 0x800,
        AllModsBound = 0x1000,
        SubsectionsViaSymbols = 0x2000,
        Canonical = 0x4000,
        WeakDefines = 0x8000,
        BindsToWeak = 0x10000,
        AllowStackExecution = 0x20000,
        RootSafe = 0x40000,
        SetuidSafe = 0x80000,
        NoReexportedDylibs = 0x100000,
        PIE = 0x200000,
        DeadStrippableDylib = 0x400000,
        HasTlvDescriptors = 0x800000,
        NoHeapExecution = 0x1000000
    }
}
