namespace Melanzana.CodeSign.Blobs
{
    public enum CodeDirectoryVersion : int
    {
        Baseline = 0x20001,
        SupportsScatter = 0x20100,
        SupportsTeamId = 0x20200,
        SupportsCodeLimit64 = 0x20300,
        SupportsExecSegment = 0x20400,
        SupportsPreEncrypt = 0x20500,
        HighestVersion = SupportsExecSegment, // TODO: We don't support pre-encryption yet
    }
}
