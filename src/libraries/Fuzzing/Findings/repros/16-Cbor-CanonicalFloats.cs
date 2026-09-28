#:package System.Formats.Cbor@11.0.0-rc.1.26425.128
// Observation: in Canonical/Ctap2Canonical mode, CborReader accepts floats that are not in their shortest form, while CborWriter
// in the same mode shortens them, so "canonical" input does not round-trip byte for byte, and distinct float map keys (e.g. two
// NaNs with different payloads) accepted by the reader become duplicate keys the canonical writer rejects.
// Run: dotnet run 16-Cbor-CanonicalFloats.cs
using System.Formats.Cbor;

byte[] input = Convert.FromHexString("FA7F800000"); // +Infinity as single precision; half precision (F97C00) would do
var reader = new CborReader(input, CborConformanceMode.Canonical);
var writer = new CborWriter(CborConformanceMode.Canonical);
writer.WriteSingle(reader.ReadSingle());
byte[] output = writer.Encode();
Console.WriteLine($"Canonical reader accepted {Convert.ToHexString(input)}; Canonical writer re-encodes it as {Convert.ToHexString(output)}");

byte[] map = Convert.FromHexString("A2FA7FC000010AFA7FC0000214"); // { NaN(payload 1): 10, NaN(payload 2): 20 }
var mapReader = new CborReader(map, CborConformanceMode.Canonical);
var mapWriter = new CborWriter(CborConformanceMode.Canonical);
string mapResult;
try
{
    mapWriter.WriteStartMap(mapReader.ReadStartMap());
    for (int i = 0; i < 2; i++)
    {
        mapWriter.WriteSingle(mapReader.ReadSingle());
        mapWriter.WriteUInt64(mapReader.ReadUInt64());
    }

    mapReader.ReadEndMap();
    mapWriter.WriteEndMap();
    mapResult = Convert.ToHexString(mapWriter.Encode());
}
catch (Exception ex) { mapResult = ex.GetType().Name + ": " + ex.Message; }

Console.WriteLine($"Canonical reader accepted map {Convert.ToHexString(map)}; re-encoding it canonically -> {mapResult}");
Console.WriteLine(!input.AsSpan().SequenceEqual(output) ? "REPRODUCED: canonical reader and writer disagree on float encodings." : "NOT REPRODUCED");
