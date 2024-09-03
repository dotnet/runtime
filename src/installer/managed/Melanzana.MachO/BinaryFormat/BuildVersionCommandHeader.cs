namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class BuildVersionCommandHeader
    {
        public MachPlatform Platform;
        public uint MinimumPlatformVersion { get; set; }
        public uint SdkVersion { get; set; }
        public uint NumberOfTools { get; set; }
    }
}