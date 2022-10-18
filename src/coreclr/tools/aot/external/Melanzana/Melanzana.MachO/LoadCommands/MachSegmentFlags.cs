namespace Melanzana.MachO
{
    [Flags]
    public enum MachSegmentFlags : uint
    {
        HighVirtualMemory = 1,
        NoRelocations = 4,
    }
}