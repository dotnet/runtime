// Finding: when every key of an OrdinalIgnoreCase FrozenDictionary/FrozenSet is ASCII in the part the key analyzer hashes, the
// collection uses Hashing.GetHashCodeOrdinalIgnoreCaseAscii for lookups too, and that asserts Debug.Assert(Ascii.IsValid(s)). The
// lookup key comes from the caller, so any non-ASCII lookup ("é1" below) aborts Debug/Checked builds of
// System.Collections.Immutable. Release builds return the right answer (no non-ASCII char is OrdinalIgnoreCase-equal to an ASCII
// one), so the fix is to check or drop the assert on the lookup path.
// Run: ./run-on-local-runtime.sh repros/36-Frozen-AsciiHashAssert.cs   (the assert needs a Debug/Checked build)
using System.Collections.Frozen;

var frozen = new[] { "a1", "b1", "c1", "d1", "e1", "f1", "g1" }.ToFrozenDictionary(k => k, k => 0, StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"strategy: {frozen.GetType().Name}");
Console.WriteLine($"ContainsKey(\"A1\") = {frozen.ContainsKey("A1")}");
Console.WriteLine($"ContainsKey(\"é1\") = {frozen.ContainsKey("é1")}   (Debug builds abort here)");
Console.WriteLine(frozen.GetType().Name.Contains("CaseInsensitiveAscii") ? "REPRODUCED (non-ASCII lookup reached the ASCII-only hash; the assert needs a Debug build)" : "NOT REPRODUCED");
