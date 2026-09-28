// Finding: MetadataReader reads the metadata root's stream count with BlobReader.ReadInt16() and allocates
// new StreamHeader[streamCount] before any check. A count of 0x8000 or more is negative, so the constructor throws
// OverflowException instead of BadImageFormatException, the exception MetadataReader (and PEReader.GetMetadataReader) documents
// for malformed metadata. Code that opens untrusted assemblies and catches BadImageFormatException crashes on a 24-byte input.
// Run: dotnet run 37-Metadata-NegativeStreamCount.cs
#:property AllowUnsafeBlocks=true
using System.Reflection.Metadata;

byte[] metadata =
[
    (byte)'B', (byte)'S', (byte)'J', (byte)'B', // signature
    1, 0, 1, 0,                                 // major/minor version
    0, 0, 0, 0,                                 // reserved
    4, 0, 0, 0, (byte)'v', (byte)'4', (byte)'.', (byte)'0', // version string length + text
    0, 0,                                       // flags
    0x00, 0x80,                                 // stream count 0x8000 (read as -32768)
];

string outcome;
bool reproduced;
unsafe
{
    fixed (byte* p = metadata)
    {
        try
        {
            _ = new MetadataReader(p, metadata.Length);
            outcome = "succeeded";
            reproduced = false;
        }
        catch (Exception ex)
        {
            outcome = $"threw {ex.GetType().Name}: {ex.Message}";
            reproduced = ex is not BadImageFormatException;
        }
    }
}

Console.WriteLine($"new MetadataReader(stream count 0x8000) {outcome} (expected BadImageFormatException)");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
