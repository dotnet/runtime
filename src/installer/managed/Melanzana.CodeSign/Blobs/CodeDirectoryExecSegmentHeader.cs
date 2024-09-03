namespace Melanzana.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    public partial class CodeDirectoryExecSegmentHeader
    {
        public ulong Base;
        public ulong Limit;
        public ExecutableSegmentFlags Flags;
    }
}