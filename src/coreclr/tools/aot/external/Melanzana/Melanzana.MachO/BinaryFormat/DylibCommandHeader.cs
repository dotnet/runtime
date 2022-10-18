namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class DylibCommandHeader
    {
        public uint NameOffset { get; set; }
        public uint Timestamp { get; set; }
        public uint CurrentVersion { get; set; }
        public uint CompatibilityVersion { get; set; }
    }
}