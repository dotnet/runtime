namespace Melanzana.MachO.BinaryFormat
{
    public interface IMachHeader
    {
        MachCpuType CpuType { get; set; }
        uint CpuSubType { get; set; }
        MachFileType FileType { get; set; }
        uint NumberOfCommands { get; set; }
        uint SizeOfCommands { get; set; }
        MachHeaderFlags Flags { get; set; }
    }
}