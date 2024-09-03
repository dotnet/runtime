namespace Melanzana.MachO
{
    public enum MachSectionType : byte
    {
        Regular = 0,
        ZeroFill = 1,
        CStringLiterals = 2,
        FourByteLiterals = 3,
        EightByteLiterals = 4,
        LiteralPointers = 5,
        NonLazySymbolPointers = 6,
        LazySymbolPointers = 7,
        SymbolStubs = 8,
        ModInitFunctionPointers = 9,
        ModTermFunctionPointers = 10,
        Coalesced = 11,
        GBZeroFill = 12,
        Interposing = 13,
        SixteenByteLiterals = 14,
        DTraceObjectFormat = 15,
        LazyDylibSymbolPointers = 16,
        ThreadLocalRegular = 17,
        ThreadLocalZeroFill = 18,
        ThreadLocalVariables = 19,
        ThreadLocalVariablePointers = 20,
        ThreadLocalInitFunctionPointers = 21,
    }
}