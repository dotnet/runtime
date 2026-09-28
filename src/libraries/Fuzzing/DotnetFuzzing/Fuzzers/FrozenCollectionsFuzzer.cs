// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for <see cref="FrozenDictionary{TKey, TValue}"/> and <see cref="FrozenSet{T}"/> against
/// <see cref="Dictionary{TKey, TValue}"/> and <see cref="HashSet{T}"/> with the same comparer. String keys come from a small
/// alphabet (ASCII letters in both cases, ASCII punctuation that differs only in bit 0x20, and non-ASCII letters with case
/// mappings) so that the key analyzer picks every strategy: length buckets, left/right-justified substrings, single chars,
/// full hashing, ordinal and case-insensitive, ASCII and not. Every key, a case-flipped copy of every key, substrings and
/// extra fuzzed strings are looked up through the string API and the ReadOnlySpan&lt;char&gt; alternate lookup. Integer keys
/// cover the integral and dense-range strategies.
/// </summary>
/// <remarks>Input layout: [0] mode (comparer / key type), then keys separated by '|' (0x7C), then '#' (0x23) and extra queries.</remarks>
internal sealed class FrozenCollectionsFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * Small FrozenSets of enums trip Debug.Assert(default(T) is IComparable<T>) in SmallValueTypeComparableFrozenSet.
    // * Non-ASCII lookups in the *CaseInsensitiveAscii* string strategies trip Debug.Assert(Ascii.IsValid(s)).
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Collections.Immutable"];
    public string[] TargetCoreLibPrefixes => [];

    // Bytes 0x00-0x7F map to themselves (with '|' and '#' reserved), the rest to this palette.
    private static readonly char[] s_palette = Array.ConvertAll(new ushort[]
    {
        0x00E9, 0x00C9, 0x00DF, 0x017F, 0x0131, 0x0130, 0x212A, 0x00E5, 0x00C5, 0x212B, 0x03A3, 0x03C3, 0x03C2, 0x0410, 0x0430,
        0xD800, 0xDC00, 0xFFFF, 0x0000, 0x1E9E, 0x01C5, 0x01C4, 0x01C6, 0xFF21, 0xFF41,
    }, c => (char)c);

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        byte mode = bytes[0];
        if ((mode & 0x80) != 0)
        {
            IntegerKeys(mode, bytes.Slice(1));
            return;
        }

        string text = string.Concat(bytes.Slice(1).ToArray().Select(b => b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length].ToString()));
        int hash = text.IndexOf('#');
        string keyText = hash < 0 ? text : text[..hash];
        string queryText = hash < 0 ? "" : text[(hash + 1)..];
        string[] keys = keyText.Split('|').Take(200).ToArray();

        IEqualityComparer<string> comparer = (mode % 4) switch
        {
            0 => StringComparer.Ordinal,
            1 => StringComparer.OrdinalIgnoreCase,
            2 => StringComparer.InvariantCultureIgnoreCase,
            _ => EqualityComparer<string>.Default,
        };

        // Queries: the keys, their case-flipped and ASCII-bit-flipped forms, prefixes/suffixes, and extra fuzzed strings.
        var queries = new List<string>();
        foreach (string key in keys)
        {
            queries.Add(key);
            queries.Add(new string(key.Select(c => char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c)).ToArray()));
            queries.Add(new string(key.Select(c => c < 0x80 ? (char)(c ^ 0x20) : c).ToArray()));
            if (key.Length > 1)
            {
                queries.Add(key[1..]);
                queries.Add(key[..^1]);
                queries.Add(key + key[^1]);
                queries.Add(key[^1] + key);
            }
        }

        queries.AddRange(queryText.Split('|'));
        queries.Add("");

        // Dictionary: build both with "last one wins" for duplicate keys.
        var reference = new Dictionary<string, int>(comparer);
        var pairs = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < keys.Length; i++)
        {
            reference[keys[i]] = i;
            pairs.Add(new KeyValuePair<string, int>(keys[i], i));
        }

        FrozenDictionary<string, int> frozen = pairs.ToFrozenDictionary(comparer);
        string kind = frozen.GetType().Name;
        Check(frozen.Count == reference.Count, () => $"{kind}: Count {frozen.Count}, expected {reference.Count}, keys [{Describe(keys)}]");
        Check(ReferenceEquals(frozen.Comparer, comparer) || (mode % 4) == 3, () => $"{kind}: Comparer isn't the one passed in");
        foreach (KeyValuePair<string, int> pair in frozen)
        {
            Check(reference.TryGetValue(pair.Key, out int value) && value == pair.Value, () => $"{kind}: enumerates ({Escape(pair.Key)}, {pair.Value}), which the reference doesn't have, keys [{Describe(keys)}]");
        }

        Check(frozen.Keys.Length == reference.Count && frozen.Values.Length == reference.Count, () => $"{kind}: Keys/Values lengths are wrong");
        for (int i = 0; i < frozen.Keys.Length; i++)
        {
            Check(reference[frozen.Keys[i]] == frozen.Values[i], () => $"{kind}: Keys[{i}] and Values[{i}] don't belong together");
        }

        FrozenDictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> alternate = default;
        bool hasAlternate = (mode % 4) != 2 && frozen.TryGetAlternateLookup(out alternate);
        Check(hasAlternate || (mode % 4) == 2, () => $"{kind}: no ReadOnlySpan<char> alternate lookup for a {comparer.GetType().Name}");

        FrozenSet<string> set = keys.ToFrozenSet(comparer);
        var referenceSet = new HashSet<string>(keys, comparer);
        string setKind = set.GetType().Name;
        Check(set.Count == referenceSet.Count, () => $"{setKind}: Count {set.Count}, expected {referenceSet.Count}, keys [{Describe(keys)}]");
        FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> setAlternate = default;
        bool hasSetAlternate = (mode % 4) != 2 && set.TryGetAlternateLookup(out setAlternate);

        bool asciiOnlyLookups = !s_strict && (kind.Contains("CaseInsensitiveAscii") || setKind.Contains("CaseInsensitiveAscii"));
        foreach (string query in queries)
        {
            if (asciiOnlyLookups && !System.Text.Ascii.IsValid(query))
            {
                continue;
            }

            bool expected = reference.TryGetValue(query, out int expectedValue);
            bool found = frozen.TryGetValue(query, out int value);
            Check(found == expected && value == expectedValue,
                () => $"{kind}.TryGetValue({Escape(query)}) = {found}/{value}, expected {expected}/{expectedValue}; comparer {comparer.GetType().Name}, keys [{Describe(keys)}]");
            Check(frozen.ContainsKey(query) == expected, () => $"{kind}.ContainsKey({Escape(query)}) disagrees with TryGetValue");
            ref readonly int valueRef = ref frozen.GetValueRefOrNullRef(query);
            Check(Unsafe.IsNullRef(in valueRef) != expected && (!expected || valueRef == expectedValue), () => $"{kind}.GetValueRefOrNullRef({Escape(query)}) is wrong");
            if (hasAlternate)
            {
                bool spanFound = alternate.TryGetValue(query.AsSpan(), out int spanValue);
                Check(spanFound == expected && spanValue == expectedValue,
                    () => $"{kind} alternate lookup TryGetValue({Escape(query)}) = {spanFound}/{spanValue}, expected {expected}/{expectedValue}; keys [{Describe(keys)}]");
                Check(alternate.ContainsKey(query.AsSpan()) == expected, () => $"{kind} alternate lookup ContainsKey({Escape(query)}) disagrees; keys [{Describe(keys)}]");
            }

            bool inSet = set.Contains(query);
            Check(inSet == referenceSet.Contains(query), () => $"{setKind}.Contains({Escape(query)}) = {inSet}, expected {!inSet}; comparer {comparer.GetType().Name}, keys [{Describe(keys)}]");
            bool gotActual = set.TryGetValue(query, out string? actual);
            Check(gotActual == inSet && (!gotActual || comparer.Equals(actual, query)), () => $"{setKind}.TryGetValue({Escape(query)}) returned {gotActual}, {Escape(actual)}");
            if (hasSetAlternate)
            {
                Check(setAlternate.Contains(query.AsSpan()) == inSet, () => $"{setKind} alternate lookup Contains({Escape(query)}) disagrees; keys [{Describe(keys)}]");
            }
        }

        // Set relations against the query list.
        string[] other = queries.Where(q => !asciiOnlyLookups || System.Text.Ascii.IsValid(q)).Take(12).ToArray();
        Check(set.SetEquals(other) == referenceSet.SetEquals(other) && set.Overlaps(other) == referenceSet.Overlaps(other)
            && set.IsSubsetOf(other) == referenceSet.IsSubsetOf(other) && set.IsSupersetOf(other) == referenceSet.IsSupersetOf(other)
            && set.IsProperSubsetOf(other) == referenceSet.IsProperSubsetOf(other) && set.IsProperSupersetOf(other) == referenceSet.IsProperSupersetOf(other),
            () => $"{setKind}: set relations disagree with HashSet for keys [{Describe(keys)}] and [{Describe(other)}]");
    }

    private static void IntegerKeys(byte mode, ReadOnlySpan<byte> data)
    {
        // Keys are small (dense ranges), or 2-byte / 4-byte values, as int, long, short or a small enum-like byte.
        var keys = new List<long>();
        for (int i = 0; i < data.Length && keys.Count < 300;)
        {
            switch (data[i] & 3)
            {
                case 0:
                case 1:
                    keys.Add((sbyte)data[i] >> 2);
                    i += 1;
                    break;
                case 2:
                    keys.Add(i + 2 < data.Length ? (short)(data[i + 1] | (data[i + 2] << 8)) : 0);
                    i += 3;
                    break;
                default:
                    keys.Add(i + 4 < data.Length ? BitConverter.ToInt32(data.Slice(i + 1, 4)) * (long)((mode & 0x40) != 0 ? 1 : 0x1_0000_0001) : int.MinValue);
                    i += 5;
                    break;
            }
        }

        var queries = keys.SelectMany(k => new[] { k, k + 1, k - 1, -k, k + 64 }).Concat([long.MinValue, long.MaxValue, int.MinValue, int.MaxValue, 0]).ToArray();
        switch (mode % 4)
        {
            case 0: Compare(keys.Select(k => (int)k).ToArray(), queries.Select(k => (int)k).ToArray()); break;
            case 1: Compare(keys.ToArray(), queries); break;
            case 2: Compare(keys.Select(k => (short)k).ToArray(), queries.Select(k => (short)k).ToArray()); break;
            default: Compare(keys.Select(k => (DayOfWeek)(k & 0xF)).ToArray(), queries.Select(k => (DayOfWeek)(k & 0xF)).ToArray()); break;
        }
    }

    private static void Compare<T>(T[] keys, T[] queries) where T : notnull
    {
        var reference = new Dictionary<T, int>();
        for (int i = 0; i < keys.Length; i++)
        {
            reference[keys[i]] = i;
        }

        FrozenDictionary<T, int> frozen = keys.Select((k, i) => new KeyValuePair<T, int>(k, i)).ToFrozenDictionary();
        FrozenSet<T> set = typeof(T).IsEnum && !s_strict && reference.Count <= 10 ? keys.ToFrozenSet(new WrappingComparer<T>()) : keys.ToFrozenSet();
        string kind = frozen.GetType().Name, setKind = set.GetType().Name;
        Check(frozen.Count == reference.Count && set.Count == reference.Count, () => $"{kind}/{setKind}: Count {frozen.Count}/{set.Count}, expected {reference.Count}");
        foreach (KeyValuePair<T, int> pair in frozen)
        {
            Check(reference.TryGetValue(pair.Key, out int value) && value == pair.Value, () => $"{kind} enumerates ({pair.Key}, {pair.Value}), keys [{string.Join(",", keys)}]");
        }

        foreach (T query in queries)
        {
            bool expected = reference.TryGetValue(query, out int expectedValue);
            bool found = frozen.TryGetValue(query, out int value);
            Check(found == expected && value == expectedValue, () => $"{kind}.TryGetValue({query}) = {found}/{value}, expected {expected}/{expectedValue}; keys [{string.Join(",", keys)}]");
            Check(set.Contains(query) == expected, () => $"{setKind}.Contains({query}) = {!expected}, expected {expected}; keys [{string.Join(",", keys)}]");
        }
    }

    // A non-default comparer keeps small enum sets off the asserting path.
    private sealed class WrappingComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);
        public int GetHashCode(T obj) => EqualityComparer<T>.Default.GetHashCode(obj!);
    }

    private static string Describe(IEnumerable<string> keys) => string.Join(", ", keys.Take(40).Select(Escape));

    private static string Escape(string? text) =>
        text is null ? "null" : "\"" + string.Concat(text.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}")) + "\"";

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
