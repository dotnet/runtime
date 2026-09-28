#:package System.Formats.Cbor@11.0.0-rc.1.26425.128
// Finding: a major type 7 item with reserved additional information 28-30 (bytes 0xFC-0xFE) is malformed CBOR, but
// CborReader.PeekState() reports SimpleValue and ReadSimpleValue()/SkipValue() then throw InvalidOperationException instead of
// CborContentException, so callers that only catch CborContentException for untrusted input crash.
// Run: dotnet run 14-Cbor-ReservedSimpleValue.cs
using System.Formats.Cbor;

bool reproduced = false;
foreach (byte[] data in new byte[][] { [0xFC], [0x81, 0xFD] })
{
    var reader = new CborReader(data, CborConformanceMode.Lax);
    string state = reader.PeekState().ToString();
    string result;
    try
    {
        if (data.Length == 1) { reader.ReadSimpleValue(); } else { reader.SkipValue(); }
        result = "no exception";
    }
    catch (Exception ex)
    {
        result = ex.GetType().Name + ": " + ex.Message;
        reproduced |= ex is not CborContentException;
    }

    Console.WriteLine($"{Convert.ToHexString(data)}: PeekState = {state}; {(data.Length == 1 ? "ReadSimpleValue" : "SkipValue")} -> {result}");
}

Console.WriteLine(reproduced ? "REPRODUCED: malformed input surfaces as InvalidOperationException." : "NOT REPRODUCED");
