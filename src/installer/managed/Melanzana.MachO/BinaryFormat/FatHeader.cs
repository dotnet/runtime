namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class FatHeader
    {
        public uint NumberOfFatArchitectures { get; set; }
    }
}