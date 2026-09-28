// Finding: BitArray.Length is documented to set new elements to false when it grows. The setter clears the bytes between the old
// and new length only when the new length still fits in the current byte[] storage. When it doesn't, it calls Array.Resize, which
// copies the whole old array, including bytes past the current Length left over from an earlier, longer state. Shrinking a
// BitArray and then growing it past its storage therefore brings back bits that were cut off: below, 768 bits that were removed
// reappear as true. Any bits a caller truncated away (for example, flags from another user) come back.
// Run: dotnet run 40-BitArray-StaleBitsOnGrow.cs
using System.Collections;

byte[] ones = Enumerable.Repeat((byte)0xFF, 100).ToArray();
var bits = new BitArray(ones) { Length = 10 };   // shrink: storage (100 bytes) is kept
bits.Length = 1000;                              // grow past the storage: Array.Resize copies the stale bytes
int set = Enumerable.Range(0, bits.Length).Count(i => bits[i]);
Console.WriteLine($"new BitArray(100 x 0xFF) {{ Length = 10 }}, then Length = 1000: {set} bits set, expected 10");

var small = new BitArray(ones) { Length = 10 };
small.Length = 500;                              // grow within the storage: cleared correctly
Console.WriteLine($"same, but Length = 500 (fits in the storage): {Enumerable.Range(0, small.Length).Count(i => small[i])} bits set, expected 10");

Console.WriteLine(set != 10 ? "REPRODUCED" : "NOT REPRODUCED");
