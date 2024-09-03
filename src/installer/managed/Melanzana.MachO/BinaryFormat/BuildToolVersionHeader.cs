namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class BuildToolVersionHeader
    {
        public MachBuildTool BuildTool;
        public uint Version { get; set; }
    }
}