namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryTeamIdHeader
    {
        public uint TeamIdOffset;
    }
}