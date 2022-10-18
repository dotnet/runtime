namespace Melanzana.MachO
{
    public enum MachFileType : uint
    {
        Object = 1,
        Execute = 2,
        FixedVM = 3,
        Core = 4,
        Preload = 5,
        DynamicLibrary = 6,
        DynamicLinker = 7,
        Bundle = 8,
        DynamicLibraryStub = 9,
        Debug = 10,
        Kext = 11
    }
}
