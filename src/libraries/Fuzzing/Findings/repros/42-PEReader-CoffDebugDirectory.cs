// Finding: PEReader.ReadDebugDirectory assumes the image has a PE optional header (Debug.Assert(PEHeaders.PEHeader != null), then
// PEHeaders.PEHeader.DebugTableDirectory). For a COFF-only image, such as an object file, or any input PEHeaders reads as one,
// PEHeader is null, so Release builds throw NullReferenceException and Debug/Checked builds abort on the assert. Returning an
// empty array (a COFF file has no debug directory) or throwing the documented BadImageFormatException/InvalidOperationException
// would be expected. The 20-byte input below is a valid, empty AMD64 COFF header.
// Run: dotnet run 42-PEReader-CoffDebugDirectory.cs
using System.Reflection.PortableExecutable;

byte[] coff = new byte[20];
BitConverter.TryWriteBytes(coff.AsSpan(0), (ushort)0x8664); // Machine = AMD64, no sections, no optional header
using var reader = new PEReader(new MemoryStream(coff));
Console.WriteLine($"IsCoffOnly = {reader.PEHeaders.IsCoffOnly}, PEHeader is {(reader.PEHeaders.PEHeader is null ? "null" : "present")}");
bool reproduced;
try
{
    Console.WriteLine($"ReadDebugDirectory() returned {reader.ReadDebugDirectory().Length} entries");
    reproduced = false;
}
catch (Exception ex)
{
    Console.WriteLine($"ReadDebugDirectory() threw {ex.GetType().Name}: {ex.Message}");
    reproduced = ex is NullReferenceException;
}

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
