namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class VersionMinCommandHeader
    {
        public uint MinimumPlatformVersion { get; set; }
        public uint SdkVersion { get; set; }
    }
}