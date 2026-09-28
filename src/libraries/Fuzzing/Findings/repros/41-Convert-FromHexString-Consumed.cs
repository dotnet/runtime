// Observation: the OperationStatus overloads of Convert.FromHexString report charsConsumed inconsistently for invalid input.
// * On InvalidData, charsConsumed is the index of the first invalid char, so for "0A1g" it's 3 although only one byte (two chars)
//   was decoded; a caller resuming at charsConsumed would drop the '1'.
// * When both chars of a pair are invalid and non-ASCII ("００"), charsConsumed is 1, although char 0 is already invalid.
// * A trailing single char is reported as NeedMoreData even when it's not a hex digit ("0Ag"), although no additional input can make
//   it valid; the OperationStatus docs reserve NeedMoreData for input that more data could complete.
// Run: dotnet run 41-Convert-FromHexString-Consumed.cs
using System.Buffers;

bool reproduced = false;
Show("0A1g", OperationStatus.InvalidData, 2);
Show("0Ag1", OperationStatus.InvalidData, 2);
Show("００", OperationStatus.InvalidData, 0);
Show("0Ag", OperationStatus.InvalidData, 2);
Show("0A1", OperationStatus.NeedMoreData, 2);
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Show(string input, OperationStatus expectedStatus, int expectedConsumed)
{
    OperationStatus status = Convert.FromHexString(input, new byte[8], out int consumed, out int written);
    bool ok = status == expectedStatus && consumed == expectedConsumed;
    reproduced |= !ok;
    string shown = string.Concat(input.Select(c => c < 0x80 ? c.ToString() : $"\\u{(int)c:X4}"));
    Console.WriteLine($"FromHexString(\"{shown}\") = {status}, consumed {consumed}, written {written}; expected {expectedStatus}, consumed {expectedConsumed}{(ok ? "" : "   <--")}");
}
