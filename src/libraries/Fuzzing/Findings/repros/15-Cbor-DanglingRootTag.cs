#:package System.Formats.Cbor@11.0.0-rc.1.26425.128
// Finding: with AllowMultipleRootLevelValues, a root-level sequence that ends in a tag without its content (e.g. C1) reports
// Finished after ReadTag() instead of throwing, and SkipValue() over it throws InvalidOperationException ("Reader state
// 'Finished' is not at the start of a data item") instead of CborContentException. Single-root readers correctly throw
// CborContentException. (The incremental-reading change notes this as a pre-existing issue.)
// Run: dotnet run 15-Cbor-DanglingRootTag.cs
using System.Formats.Cbor;

bool reproduced = false;
foreach (bool multipleRoots in new[] { false, true })
{
    string readTag, skip;
    try
    {
        var reader = new CborReader(new byte[] { 0xC1 }, CborConformanceMode.Lax, allowMultipleRootLevelValues: multipleRoots);
        reader.ReadTag();
        readTag = "ReadTag() then PeekState() = " + reader.PeekState();
        reproduced |= multipleRoots;
    }
    catch (Exception ex) { readTag = ex.GetType().Name; }

    try
    {
        new CborReader(new byte[] { 0xC1 }, CborConformanceMode.Lax, allowMultipleRootLevelValues: multipleRoots).SkipValue();
        skip = "no exception";
    }
    catch (Exception ex) { skip = ex.GetType().Name; reproduced |= ex is not CborContentException; }

    Console.WriteLine($"allowMultipleRootLevelValues={multipleRoots}: {readTag}; SkipValue() -> {skip}");
}

Console.WriteLine(reproduced ? "REPRODUCED: a dangling root tag is accepted / surfaces as InvalidOperationException." : "NOT REPRODUCED");
