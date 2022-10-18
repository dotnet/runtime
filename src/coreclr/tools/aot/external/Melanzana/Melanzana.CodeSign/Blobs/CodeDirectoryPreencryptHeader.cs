namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryPreencryptHeader
    {
        public uint HardendRuntimeVersion;
        public uint PrencryptOffset;
    }
}