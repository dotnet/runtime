// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for CoreLib APIs whose fast paths read and write through Unsafe.ReadUnaligned/WriteUnaligned:
/// BitArray, System.Text.Ascii, Convert hex conversion (HexConverter), UnmanagedMemoryAccessor/SafeBuffer, MemoryMarshal and
/// BitConverter span reads, OrdinalIgnoreCase comparison and hashing, and Latin-1 transcoding. Inputs live in guard-paged
/// memory where possible, and every result is compared with a straightforward scalar reference.
/// </summary>
/// <remarks>Input layout: [0] mode, [1..] payload.</remarks>
internal sealed class UnsafeBuffersFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * BitArray.Length keeps stale bits when it grows past its storage.
    // * Convert.FromHexString reports charsConsumed at the invalid char (possibly off by one for non-ASCII pairs), and
    //   NeedMoreData for a trailing non-hex char.
    // Not bugs: DestinationTooSmall may win over InvalidData when the destination fills up first.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } =
        ["System.Collections.BitArray", "System.Text.Ascii", "System.Text.Latin1", "System.HexConverter", "System.Convert", "System.IO.UnmanagedMemory",
         "System.Runtime.InteropServices.SafeBuffer", "System.Globalization.Ordinal", "System.Marvin", "System.BitConverter", "System.Buffers.Binary"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        ReadOnlySpan<byte> payload = bytes.Slice(1);
        switch (bytes[0] % 7)
        {
            case 0: BitArrays(payload); break;
            case 1: AsciiChecks(payload); break;
            case 2: Hex(payload); break;
            case 3: Accessor(payload); break;
            case 4: MemoryMarshalReads(payload); break;
            case 5: OrdinalIgnoreCase(payload); break;
            default: Latin1(payload); break;
        }
    }

    // ---------------------------------------------------------------- BitArray

    private static void BitArrays(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return;
        }

        int length = BinaryPrimitives.ReadUInt16LittleEndian(payload) % 1200;
        int shift = payload[2] % 1300;
        byte op = payload[3];
        ReadOnlySpan<byte> data = payload.Slice(4);
        byte[] first = data.Slice(0, data.Length / 2).ToArray(), second = data.Slice(data.Length / 2).ToArray();

        var a = new BitArray(first) { Length = length };
        var b = new BitArray(second) { Length = length };
        bool[] ra = Bits(first, length), rb = Bits(second, length);
        Same(a, ra, "new BitArray(byte[]) then Length");
        Same(b, rb, "new BitArray(byte[]) then Length");

        switch (op % 9)
        {
            case 0: a.And(b); for (int i = 0; i < length; i++) ra[i] &= rb[i]; break;
            case 1: a.Or(b); for (int i = 0; i < length; i++) ra[i] |= rb[i]; break;
            case 2: a.Xor(b); for (int i = 0; i < length; i++) ra[i] ^= rb[i]; break;
            case 3: a.Not(); for (int i = 0; i < length; i++) ra[i] = !ra[i]; break;
            case 4: a.LeftShift(shift); ra = Enumerable.Range(0, length).Select(i => i - shift >= 0 && ra[i - shift]).ToArray(); break;
            case 5: a.RightShift(shift); ra = Enumerable.Range(0, length).Select(i => i + shift < length && ra[i + shift]).ToArray(); break;
            case 6: a.SetAll(true); ra.AsSpan().Fill(true); break;
            case 7:
                a = new BitArray(ra);
                break;
            default:
                int[] ints = MemoryMarshal.Cast<byte, int>(first.AsSpan(0, first.Length & ~3)).ToArray();
                a = new BitArray(ints) { Length = length };
                ra = Bits(first.AsSpan(0, first.Length & ~3).ToArray(), length);
                break;
        }

        Same(a, ra, $"BitArray op {op % 9} (length {length}, shift {shift})");

        // Growing again must expose zeros, not stale bits.
        int grown = s_strict ? length + (op >> 4) * 7 : length;
        a.Length = grown;
        ra = ra.Concat(new bool[grown - length]).ToArray();
        Same(a, ra, $"BitArray op {op % 9} then Length {length} -> {grown}");

        Check(a.HasAllSet() == ra.All(x => x) && a.HasAnySet() == ra.Any(x => x), () => $"HasAllSet/HasAnySet wrong after op {op % 9}");

        // CopyTo in all three element types, at an offset.
        int offset = op & 3;
        byte[] asBytes = new byte[offset + (grown + 7) / 8];
        a.CopyTo(asBytes, offset);
        int[] asInts = new int[offset + (grown + 31) / 32];
        a.CopyTo(asInts, offset);
        bool[] asBools = new bool[offset + grown];
        a.CopyTo(asBools, offset);
        for (int i = 0; i < grown; i++)
        {
            Check(((asBytes[offset + i / 8] >> (i % 8)) & 1) == (ra[i] ? 1 : 0) && ((asInts[offset + i / 32] >> (i % 32)) & 1) == (ra[i] ? 1 : 0) && asBools[offset + i] == ra[i],
                () => $"CopyTo disagrees at bit {i} after op {op % 9}");
        }

        for (int i = grown; i < ((grown + 7) / 8) * 8; i++)
        {
            Check(((asBytes[offset + i / 8] >> (i % 8)) & 1) == 0, () => $"CopyTo(byte[]) exposes bit {i} past Length {grown} after op {op % 9}");
        }

        for (int i = grown; i < ((grown + 31) / 32) * 32; i++)
        {
            Check(((asInts[offset + i / 32] >> (i % 32)) & 1) == 0, () => $"CopyTo(int[]) exposes bit {i} past Length {grown} after op {op % 9}");
        }
    }

    private static bool[] Bits(byte[] source, int length) =>
        Enumerable.Range(0, length).Select(i => i / 8 < source.Length && ((source[i / 8] >> (i % 8)) & 1) != 0).ToArray();

    private static void Same(BitArray actual, bool[] expected, string what)
    {
        Check(actual.Length == expected.Length, () => $"{what}: Length {actual.Length}, expected {expected.Length}");
        for (int i = 0; i < expected.Length; i++)
        {
            Check(actual[i] == expected[i], () => $"{what}: bit {i} is {actual[i]}, expected {expected[i]}");
        }

        int count = 0;
        foreach (bool bit in actual)
        {
            Check(bit == expected[count++], () => $"{what}: enumerator disagrees at {count - 1}");
        }
    }

    // ---------------------------------------------------------------- Ascii

    private static void AsciiChecks(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2 || payload.Length > 4000)
        {
            return;
        }

        byte control = payload[0];
        ReadOnlySpan<byte> raw = payload.Slice(1);
        using PooledBoundedMemory<byte> bytesMemory = PooledBoundedMemory<byte>.Rent(raw, PoisonPagePlacement.After);
        ReadOnlySpan<byte> input = bytesMemory.Span;
        char[] charArray = new char[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            charArray[i] = raw[i] < 0x80 ? (char)raw[i] : (char)(raw[i] << ((control & 1) != 0 ? 8 : 0));
        }

        using PooledBoundedMemory<char> charsMemory = PooledBoundedMemory<char>.Rent(charArray, PoisonPagePlacement.After);
        ReadOnlySpan<char> chars = charsMemory.Span;

        int firstInvalidByte = raw.IndexOfAnyInRange((byte)0x80, (byte)0xFF);
        int firstInvalidChar = Array.FindIndex(charArray, c => c >= 0x80);
        Check(Ascii.IsValid(input) == (firstInvalidByte < 0) && Ascii.IsValid(chars) == (firstInvalidChar < 0), () => "Ascii.IsValid disagrees with the reference");

        // Case conversion into a destination that may be too small.
        int destinationLength = raw.Length - (control >> 1) % 4;
        destinationLength = Math.Max(0, destinationLength);
        foreach (bool upper in new[] { true, false })
        {
            byte[] byteDestination = new byte[destinationLength];
            OperationStatus status = upper ? Ascii.ToUpper(input, byteDestination, out int written) : Ascii.ToLower(input, byteDestination, out written);
            ExpectConversion(status, written, byteDestination.Select(b => (int)b).ToArray(), raw.ToArray().Select(b => (int)b).ToArray(), firstInvalidByte, destinationLength, upper, "bytes->bytes");

            char[] charDestination = new char[destinationLength];
            status = upper ? Ascii.ToUpper(chars, charDestination, out written) : Ascii.ToLower(chars, charDestination, out written);
            ExpectConversion(status, written, charDestination.Select(c => (int)c).ToArray(), charArray.Select(c => (int)c).ToArray(), firstInvalidChar, destinationLength, upper, "chars->chars");

            status = upper ? Ascii.ToUpper(input, charDestination, out written) : Ascii.ToLower(input, charDestination, out written);
            ExpectConversion(status, written, charDestination.Select(c => (int)c).ToArray(), raw.ToArray().Select(b => (int)b).ToArray(), firstInvalidByte, destinationLength, upper, "bytes->chars");

            status = upper ? Ascii.ToUpper(chars, byteDestination, out written) : Ascii.ToLower(chars, byteDestination, out written);
            ExpectConversion(status, written, byteDestination.Select(b => (int)b).ToArray(), charArray.Select(c => (int)c).ToArray(), firstInvalidChar, destinationLength, upper, "chars->bytes");

            byte[] inPlace = raw.ToArray();
            status = upper ? Ascii.ToUpperInPlace(inPlace, out written) : Ascii.ToLowerInPlace(inPlace, out written);
            ExpectConversion(status, written, inPlace.Select(b => (int)b).ToArray(), raw.ToArray().Select(b => (int)b).ToArray(), firstInvalidByte, raw.Length, upper, "bytes in place");
            char[] inPlaceChars = (char[])charArray.Clone();
            status = upper ? Ascii.ToUpperInPlace(inPlaceChars, out written) : Ascii.ToLowerInPlace(inPlaceChars, out written);
            ExpectConversion(status, written, inPlaceChars.Select(c => (int)c).ToArray(), charArray.Select(c => (int)c).ToArray(), firstInvalidChar, raw.Length, upper, "chars in place");
        }

        // Transcoding.
        char[] utf16 = new char[destinationLength];
        OperationStatus toUtf16 = Ascii.ToUtf16(input, utf16, out int utf16Written);
        ExpectCopy(toUtf16, utf16Written, utf16.Select(c => (int)c).ToArray(), raw.ToArray().Select(b => (int)b).ToArray(), firstInvalidByte, destinationLength, "ToUtf16");
        byte[] fromUtf16 = new byte[destinationLength];
        OperationStatus fromStatus = Ascii.FromUtf16(chars, fromUtf16, out int fromWritten);
        ExpectCopy(fromStatus, fromWritten, fromUtf16.Select(b => (int)b).ToArray(), charArray.Select(c => (int)c).ToArray(), firstInvalidChar, destinationLength, "FromUtf16");

        // Equality between a buffer and a case-flipped / mutated copy.
        byte[] rawArray = raw.ToArray();
        byte[] other = raw.ToArray();
        for (int i = 0; i < other.Length; i++)
        {
            if (char.IsAsciiLetter((char)other[i]) && ((control >> 3) & 1) != 0)
            {
                other[i] ^= 0x20;
            }
        }

        if ((control & 0x40) != 0 && other.Length > 0)
        {
            other[control % other.Length] ^= (byte)(1 << (control & 7));
        }

        char[] otherChars = other.Select(b => (char)b).ToArray();
        bool bothAscii = firstInvalidByte < 0 && Ascii.IsValid(other);
        bool equal = bothAscii && raw.SequenceEqual(other);
        bool equalIgnoreCase = bothAscii && raw.Length == other.Length && Enumerable.Range(0, rawArray.Length).All(i => char.ToLowerInvariant((char)rawArray[i]) == char.ToLowerInvariant((char)other[i]));
        Check(Ascii.Equals(input, other) == equal && Ascii.Equals(input, otherChars) == equal, () => $"Ascii.Equals disagrees ({equal} expected)");
        Check(Ascii.EqualsIgnoreCase(input, other) == equalIgnoreCase && Ascii.EqualsIgnoreCase(input, otherChars) == equalIgnoreCase,
            () => $"Ascii.EqualsIgnoreCase disagrees ({equalIgnoreCase} expected) for {Convert.ToHexString(rawArray)} / {Convert.ToHexString(other)}");
        if (firstInvalidByte < 0)
        {
            char[] rawChars = raw.ToArray().Select(b => (char)b).ToArray();
            Check(Ascii.EqualsIgnoreCase(rawChars, otherChars) == equalIgnoreCase && Ascii.Equals(rawChars, otherChars) == equal, () => "Ascii char/char equality disagrees");
        }

        // Trimming ASCII whitespace.
        static bool IsSpace(int c) => c is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';
        int start = 0, end = raw.Length;
        while (start < end && IsSpace(raw[start])) start++;
        while (end > start && IsSpace(raw[end - 1])) end--;
        Range trimmed = Ascii.Trim(input);
        Check(trimmed.GetOffsetAndLength(raw.Length) == (start, end - start), () => $"Ascii.Trim = {trimmed}, expected {start}..{end}");
    }

    private static void ExpectConversion(OperationStatus status, int written, int[] destination, int[] source, int firstInvalid, int destinationLength, bool upper, string what)
    {
        int convertible = firstInvalid < 0 ? source.Length : firstInvalid;
        OperationStatus expected = destinationLength < convertible ? OperationStatus.DestinationTooSmall : firstInvalid >= 0 ? OperationStatus.InvalidData : OperationStatus.Done;
        int expectedWritten = Math.Min(convertible, destinationLength);
        bool precedence = status == OperationStatus.DestinationTooSmall && expected == OperationStatus.InvalidData && destinationLength < source.Length;
        Check((status == expected || precedence) && written == expectedWritten, () => $"Ascii.{(upper ? "ToUpper" : "ToLower")} {what}: {status}/{written}, expected {expected}/{expectedWritten}");
        for (int i = 0; i < written; i++)
        {
            int c = source[i];
            int e = upper ? (c is >= 'a' and <= 'z' ? c - 32 : c) : (c is >= 'A' and <= 'Z' ? c + 32 : c);
            Check(destination[i] == e, () => $"Ascii.{(upper ? "ToUpper" : "ToLower")} {what}: element {i} is {destination[i]:X}, expected {e:X}");
        }
    }

    private static void ExpectCopy(OperationStatus status, int written, int[] destination, int[] source, int firstInvalid, int destinationLength, string what)
    {
        int convertible = firstInvalid < 0 ? source.Length : firstInvalid;
        OperationStatus expected = destinationLength < convertible ? OperationStatus.DestinationTooSmall : firstInvalid >= 0 ? OperationStatus.InvalidData : OperationStatus.Done;
        int expectedWritten = Math.Min(convertible, destinationLength);
        bool precedence = status == OperationStatus.DestinationTooSmall && expected == OperationStatus.InvalidData && destinationLength < source.Length;
        Check((status == expected || precedence) && written == expectedWritten, () => $"Ascii.{what}: {status}/{written}, expected {expected}/{expectedWritten}");
        for (int i = 0; i < written; i++)
        {
            Check(destination[i] == source[i], () => $"Ascii.{what}: element {i} is {destination[i]:X}, expected {source[i]:X}");
        }
    }

    // ---------------------------------------------------------------- Hex

    private const string HexPalette = "0123456789abcdefABCDEF0123456789";

    private static void Hex(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 1)
        {
            return;
        }

        byte control = payload[0];
        ReadOnlySpan<byte> data = payload.Slice(1);

        // Bytes -> hex text.
        using PooledBoundedMemory<byte> source = PooledBoundedMemory<byte>.Rent(data, PoisonPagePlacement.After);
        string upper = string.Concat(data.ToArray().Select(b => b.ToString("X2")));
        Check(Convert.ToHexString(source.Span) == upper && Convert.ToHexStringLower(source.Span) == upper.ToLowerInvariant(), () => "ToHexString disagrees with the reference");
        int size = Math.Max(0, upper.Length - control % 3);
        char[] destination = new char[size];
        bool ok = Convert.TryToHexString(source.Span, destination, out int written);
        Check(ok == (size >= upper.Length) && (!ok || (written == upper.Length && new string(destination, 0, written) == upper)) && (ok || written == 0), () => $"TryToHexString into {size} chars: {ok}/{written}");
        byte[] utf8Destination = new byte[size];
        ok = Convert.TryToHexStringLower(source.Span, utf8Destination, out written);
        Check(ok == (size >= upper.Length) && (!ok || (written == upper.Length && Encoding.ASCII.GetString(utf8Destination, 0, written) == upper.ToLowerInvariant())) && (ok || written == 0),
            () => $"TryToHexStringLower(UTF-8) into {size} bytes: {ok}/{written}");

        // Hex text (mostly valid, sometimes not) -> bytes.
        char[] text = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            byte b = data[i];
            text[i] = b < 0xF0 ? HexPalette[b % HexPalette.Length] : "g /\0\u00E9\uFF10:@`G"[b % 10];
        }

        using PooledBoundedMemory<char> textMemory = PooledBoundedMemory<char>.Rent(text, PoisonPagePlacement.After);
        byte[] asciiText = text.Select(c => c < 0x80 ? (byte)c : (byte)'?').ToArray();
        using PooledBoundedMemory<byte> utf8Memory = PooledBoundedMemory<byte>.Rent(asciiText, PoisonPagePlacement.After);

        // Reference: decode complete valid pairs; stop at the first invalid char of a pair.
        var expected = new List<byte>();
        int consumed = 0;
        OperationStatus expectedStatus = OperationStatus.Done;
        int destinationSize = (control >> 2) % 2 == 0 ? text.Length / 2 : Math.Max(0, text.Length / 2 - 1 - control % 3);
        while (consumed + 1 < text.Length || consumed < text.Length)
        {
            if (consumed + 1 >= text.Length)
            {
                expectedStatus = IsHex(text[consumed]) ? OperationStatus.NeedMoreData : OperationStatus.InvalidData;
                break;
            }

            if (!IsHex(text[consumed]) || !IsHex(text[consumed + 1]))
            {
                // charsConsumed points at the invalid char, so it includes a valid first nibble (see FINDINGS.md).
                expectedStatus = OperationStatus.InvalidData;
                consumed += IsHex(text[consumed]) ? 1 : 0;
                break;
            }

            if (expected.Count == destinationSize)
            {
                expectedStatus = OperationStatus.DestinationTooSmall;
                break;
            }

            expected.Add(Convert.ToByte(new string(text, consumed, 2), 16));
            consumed += 2;
        }

        byte[] decoded = new byte[destinationSize];
        OperationStatus status = Convert.FromHexString(textMemory.Span, decoded, out int charsConsumed, out int bytesWritten);
        string input = new string(text);
        bool HexOk(OperationStatus actualStatus, int actualConsumed) =>
            (actualStatus == expectedStatus && actualConsumed == consumed)
            || (!s_strict && expectedStatus == OperationStatus.InvalidData && actualStatus == OperationStatus.DestinationTooSmall && expected.Count == destinationSize && actualConsumed == 2 * expected.Count)
            || (!s_strict && expectedStatus == OperationStatus.InvalidData && actualStatus is OperationStatus.InvalidData or OperationStatus.NeedMoreData && Math.Abs(actualConsumed - consumed) <= 1 && actualConsumed >= 2 * expected.Count);
        Check(HexOk(status, charsConsumed) && bytesWritten == expected.Count && decoded.AsSpan(0, bytesWritten).SequenceEqual(expected.ToArray()),
            () => $"FromHexString(\"{Escape(input)}\", {destinationSize} bytes) = {status}/{charsConsumed}/{bytesWritten}, expected {expectedStatus}/{consumed}/{expected.Count}");
        status = Convert.FromHexString(utf8Memory.Span, decoded, out charsConsumed, out bytesWritten);
        bool asciiOnly = text.All(c => c < 0x80);
        Check(!asciiOnly || (HexOk(status, charsConsumed) && bytesWritten == expected.Count && decoded.AsSpan(0, bytesWritten).SequenceEqual(expected.ToArray())),
            () => $"FromHexString(UTF-8 \"{Escape(input)}\", {destinationSize} bytes) = {status}/{charsConsumed}/{bytesWritten}, expected {expectedStatus}/{consumed}/{expected.Count}");

        bool allValid = text.Length % 2 == 0 && text.All(IsHex);
        byte[]? whole = null;
        try
        {
            whole = Convert.FromHexString(input);
        }
        catch (FormatException)
        {
        }

        Check((whole is not null) == allValid && (whole is null || whole.AsSpan().SequenceEqual(allValid ? Convert.FromHexString(input.ToUpperInvariant()) : [])),
            () => $"FromHexString(\"{Escape(input)}\") {(whole is null ? "threw" : "succeeded")}, valid = {allValid}");
    }

    private static bool IsHex(char c) => char.IsAsciiHexDigit(c);

    // ---------------------------------------------------------------- UnmanagedMemoryAccessor / SafeBuffer

    private sealed class PinnedBuffer : SafeBuffer
    {
        public PinnedBuffer(IntPtr pointer, ulong length) : base(ownsHandle: false)
        {
            SetHandle(pointer);
            Initialize(length);
        }

        protected override bool ReleaseHandle() => true;
    }

    private static unsafe void Accessor(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return;
        }

        int capacity = payload[0] % 64;
        int offset = payload[1] % 8;
        ReadOnlySpan<byte> program = payload.Slice(2);
        using BoundedMemory<byte> memory = BoundedMemory.Allocate<byte>(offset + capacity, (program[0] & 1) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After);
        byte[] reference = new byte[offset + capacity];
        new Random(program[0]).NextBytes(reference);
        reference.CopyTo(memory.Span);

        fixed (byte* p = memory.Span)
        {
            using var buffer = new PinnedBuffer((IntPtr)p, (ulong)(offset + capacity));
            using var accessor = new UnmanagedMemoryAccessor(buffer, offset, capacity, FileAccess.ReadWrite);
            for (int i = 1; i + 2 < program.Length; i += 3)
            {
                int op = program[i] % 12;
                long position = (sbyte)program[i + 1];
                int count = program[i + 2] % 20;
                int size = op switch { 0 or 1 => 1, 2 or 3 => 4, 4 or 5 => 8, 6 or 7 => 16, _ => 4 };
                bool fits = position >= 0 && position + size <= capacity;
                Exception? error = null;
                try
                {
                    switch (op)
                    {
                        case 0: Check(accessor.ReadByte(position) == reference[offset + position], () => "ReadByte"); break;
                        case 1: accessor.Write(position, (byte)count); reference[offset + position] = (byte)count; break;
                        case 2: Check(accessor.ReadInt32(position) == BitConverter.ToInt32(reference, (int)(offset + position)), () => "ReadInt32"); break;
                        case 3: accessor.Write(position, count * 0x01010101); BitConverter.TryWriteBytes(reference.AsSpan((int)(offset + position)), count * 0x01010101); break;
                        case 4: Check(accessor.ReadInt64(position) == BitConverter.ToInt64(reference, (int)(offset + position)), () => "ReadInt64"); break;
                        case 5: accessor.Write(position, (long)count << 40); BitConverter.TryWriteBytes(reference.AsSpan((int)(offset + position)), (long)count << 40); break;
                        case 6:
                            accessor.Read(position, out Guid guid);
                            Check(guid == new Guid(reference.AsSpan((int)(offset + position), 16)), () => "Read<Guid>");
                            break;
                        case 7:
                            var value = new Guid(count, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                            accessor.Write(position, ref value);
                            value.TryWriteBytes(reference.AsSpan((int)(offset + position)));
                            break;
                        case 8:
                        case 9:
                        {
                            // Arrays: as many whole elements as fit are transferred.
                            fits = position >= 0 && position < capacity;
                            int available = fits ? (int)Math.Min(count, (capacity - position) / 4) : 0;
                            int[] array = new int[count + 2];
                            if (op == 8)
                            {
                                int read = accessor.ReadArray(position, array, 1, count);
                                Check(read == available, () => $"ReadArray at {position} of {count} ints returned {read}, expected {available} (capacity {capacity})");
                                for (int k = 0; k < read; k++)
                                {
                                    Check(array[1 + k] == BitConverter.ToInt32(reference, (int)(offset + position + 4 * k)), () => "ReadArray contents");
                                }
                            }
                            else
                            {
                                if (fits && count * 4 > capacity - position)
                                {
                                    fits = false; // WriteArray needs room for all of them.
                                }

                                for (int k = 0; k < array.Length; k++) array[k] = k * 0x11111111;
                                accessor.WriteArray(position, array, 1, count);
                                for (int k = 0; k < count; k++)
                                {
                                    BitConverter.TryWriteBytes(reference.AsSpan((int)(offset + position + 4 * k)), array[1 + k]);
                                }
                            }

                            break;
                        }

                        case 10:
                            fits = position >= 0 && position + count <= capacity;
                            byte[] span = new byte[count];
                            buffer.ReadSpan((ulong)(offset + position), span.AsSpan());
                            Check(span.AsSpan().SequenceEqual(reference.AsSpan((int)(offset + position), count)), () => "SafeBuffer.ReadSpan contents");
                            fits = offset + position >= 0 && offset + position + count <= offset + capacity;
                            break;
                        default:
                            decimal d = new decimal(count, 0, 0, (count & 1) != 0, (byte)(count % 29));
                            size = 16;
                            fits = position >= 0 && position + 16 <= capacity;
                            accessor.Write(position, d);
                            Check(accessor.ReadDecimal(position) == d, () => "decimal round trip");
                            int[] bitsOfD = decimal.GetBits(d);
                            for (int k = 0; k < 4; k++)
                            {
                                BitConverter.TryWriteBytes(reference.AsSpan((int)(offset + position + 4 * k)), bitsOfD[k]);
                            }

                            break;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
                {
                    error = ex;
                }

                if (op == 10)
                {
                    // SafeBuffer works on the whole buffer (offset included), not the accessor's view.
                    long absolute = offset + position;
                    fits = absolute >= 0 && absolute + count <= offset + capacity;
                }

                Check((error is null) == fits, () => $"accessor op {op} at {position} (count {count}, capacity {capacity}, offset {offset}) {(error is null ? "succeeded" : "threw " + error.GetType().Name)} but fits = {fits}");
            }

            Check(memory.Span.SequenceEqual(reference), () => "the accessor wrote outside what the reference expected");
        }
    }

    // ---------------------------------------------------------------- MemoryMarshal / BitConverter / BinaryPrimitives

    private static void MemoryMarshalReads(ReadOnlySpan<byte> payload)
    {
        using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(payload, PoisonPagePlacement.After);
        ReadOnlyMemory<byte> all = memory.Memory;
        for (int length = 0; length <= Math.Min(all.Length, 17); length++)
        {
            ReadOnlyMemory<byte> slice = all.Slice(all.Length - length);
            Expect(slice, 4, () => MemoryMarshal.Read<int>(slice.Span), MemoryMarshal.TryRead(slice.Span, out int i32), i32, () => BitConverter.ToInt32(slice.Span), "int");
            Expect(slice, 8, () => MemoryMarshal.Read<long>(slice.Span), MemoryMarshal.TryRead(slice.Span, out long i64), i64, () => BitConverter.ToInt64(slice.Span), "long");
            Expect(slice, 16, () => MemoryMarshal.Read<Guid>(slice.Span), MemoryMarshal.TryRead(slice.Span, out Guid g), g, () => new Guid(slice.Span.Slice(0, 16)), "Guid");
            Expect(slice, 2, () => BinaryPrimitives.ReadInt16BigEndian(slice.Span), BinaryPrimitives.TryReadInt16BigEndian(slice.Span, out short be16), be16,
                () => (short)((slice.Span[0] << 8) | slice.Span[1]), "Int16BigEndian");
            Expect(slice, 16, () => BinaryPrimitives.ReadUInt128LittleEndian(slice.Span), BinaryPrimitives.TryReadUInt128LittleEndian(slice.Span, out UInt128 u128), u128,
                () => new UInt128(BitConverter.ToUInt64(slice.Span.Slice(8)), BitConverter.ToUInt64(slice.Span)), "UInt128LittleEndian");
            Expect(slice, 8, () => BitConverter.ToDouble(slice.Span), BitConverter.TryWriteBytes(new byte[length], 0.0), 0.0, () => 0.0, "ToDouble", compare: false);
        }
    }

    private static void Expect<T>(ReadOnlyMemory<byte> slice, int size, Func<T> read, bool tried, T triedValue, Func<T> reference, string what, bool compare = true)
    {
        bool fits = slice.Length >= size;
        T value = default!;
        bool threw = false;
        try
        {
            value = read();
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        int length = slice.Length;
        Check(threw != fits, () => $"{what} read from {length} bytes {(threw ? "threw" : "succeeded")}");
        if (compare)
        {
            Check(tried == fits, () => $"Try{what} from {length} bytes returned {tried}");
            if (fits)
            {
                T expected = reference();
                Check(EqualityComparer<T>.Default.Equals(value, expected) && EqualityComparer<T>.Default.Equals(triedValue, expected), () => $"{what} from {length} bytes = {value}, expected {expected}");
            }
        }
    }

    // ---------------------------------------------------------------- OrdinalIgnoreCase

    private static readonly string[] s_casePalette =
    [
        "a", "A", "z", "Z", "@", "`", "[", "{", "\u00E9", "\u00C9", "\u00DF", "\u1E9E", "\u0131", "\u0130", "i", "I", "\u017F", "s", "S", "\u212A", "k",
        "\u03A3", "\u03C3", "\u03C2", "\u0410", "\u0430", "\u01C4", "\u01C5", "\u01C6", "\uD801\uDC00", "\uD801\uDC28", "\uD800", "\uDC00", "\uFF21", "\uFF41", "\u0000",
    ];

    private static void OrdinalIgnoreCase(ReadOnlySpan<byte> payload)
    {
        int split = payload.IndexOf((byte)0xFF);
        if (split < 0)
        {
            split = payload.Length / 2;
        }

        string a = MakeString(payload.Slice(0, split));
        string b = MakeString(payload.Slice(Math.Min(split + 1, payload.Length)));
        using PooledBoundedMemory<char> aMemory = PooledBoundedMemory<char>.Rent(a.AsSpan(), PoisonPagePlacement.After);
        using PooledBoundedMemory<char> bMemory = PooledBoundedMemory<char>.Rent(b.AsSpan(), PoisonPagePlacement.After);
        ReadOnlySpan<char> sa = aMemory.Span, sb = bMemory.Span;

        bool equal = string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        int compare = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        int reverse = string.Compare(b, a, StringComparison.OrdinalIgnoreCase);
        Check(equal == (compare == 0) && Math.Sign(compare) == -Math.Sign(reverse), () => $"OrdinalIgnoreCase Equals/Compare disagree for \"{Escape(a)}\" / \"{Escape(b)}\": {equal}, {compare}, {reverse}");
        Check(sa.Equals(sb, StringComparison.OrdinalIgnoreCase) == equal && Math.Sign(sa.CompareTo(sb, StringComparison.OrdinalIgnoreCase)) == Math.Sign(compare),
            () => $"span OrdinalIgnoreCase disagrees with string for \"{Escape(a)}\" / \"{Escape(b)}\"");
        Check(StringComparer.OrdinalIgnoreCase.Equals(a, b) == equal && Math.Sign(StringComparer.OrdinalIgnoreCase.Compare(a, b)) == Math.Sign(compare),
            () => $"StringComparer.OrdinalIgnoreCase disagrees for \"{Escape(a)}\" / \"{Escape(b)}\"");
        if (equal)
        {
            Check(a.GetHashCode(StringComparison.OrdinalIgnoreCase) == b.GetHashCode(StringComparison.OrdinalIgnoreCase)
                && string.GetHashCode(sa, StringComparison.OrdinalIgnoreCase) == string.GetHashCode(sb, StringComparison.OrdinalIgnoreCase)
                && StringComparer.OrdinalIgnoreCase.GetHashCode(a) == StringComparer.OrdinalIgnoreCase.GetHashCode(b),
                () => $"equal strings \"{Escape(a)}\" / \"{Escape(b)}\" have different OrdinalIgnoreCase hash codes");
        }

        Check(a.GetHashCode(StringComparison.OrdinalIgnoreCase) == string.GetHashCode(sa, StringComparison.OrdinalIgnoreCase), () => $"string and span OrdinalIgnoreCase hashes differ for \"{Escape(a)}\"");

        // Search: whatever IndexOf finds must compare equal, and nothing earlier may (for strings without surrogates, where
        // slicing can't split a pair).
        if (b.Length > 0 && !a.Any(char.IsSurrogate) && !b.Any(char.IsSurrogate))
        {
            int index = a.IndexOf(b, StringComparison.OrdinalIgnoreCase);
            int expected = -1;
            for (int i = 0; i + b.Length <= a.Length; i++)
            {
                if (string.Equals(a.Substring(i, b.Length), b, StringComparison.OrdinalIgnoreCase))
                {
                    expected = i;
                    break;
                }
            }

            Check(index == expected && sa.IndexOf(sb, StringComparison.OrdinalIgnoreCase) == expected, () => $"IndexOf(\"{Escape(a)}\", \"{Escape(b)}\", OrdinalIgnoreCase) = {index}, expected {expected}");
            int last = a.LastIndexOf(b, StringComparison.OrdinalIgnoreCase);
            int expectedLast = -1;
            for (int i = a.Length - b.Length; i >= 0; i--)
            {
                if (string.Equals(a.Substring(i, b.Length), b, StringComparison.OrdinalIgnoreCase))
                {
                    expectedLast = i;
                    break;
                }
            }

            Check(last == expectedLast, () => $"LastIndexOf(\"{Escape(a)}\", \"{Escape(b)}\", OrdinalIgnoreCase) = {last}, expected {expectedLast}");
            Check(a.StartsWith(b, StringComparison.OrdinalIgnoreCase) == (a.Length >= b.Length && string.Equals(a[..b.Length], b, StringComparison.OrdinalIgnoreCase))
                && a.EndsWith(b, StringComparison.OrdinalIgnoreCase) == (a.Length >= b.Length && string.Equals(a[^b.Length..], b, StringComparison.OrdinalIgnoreCase)),
                () => $"StartsWith/EndsWith(\"{Escape(a)}\", \"{Escape(b)}\", OrdinalIgnoreCase) disagree with Equals");
        }
    }

    private static string MakeString(ReadOnlySpan<byte> data)
    {
        var builder = new StringBuilder();
        foreach (byte b in data)
        {
            if (b < 0x80)
            {
                builder.Append((char)b);
            }
            else
            {
                builder.Append(s_casePalette[b % s_casePalette.Length]);
            }
        }

        return builder.ToString();
    }

    // ---------------------------------------------------------------- Latin-1

    private static void Latin1(ReadOnlySpan<byte> payload)
    {
        // Bytes -> chars is a widening copy.
        using PooledBoundedMemory<byte> bytesMemory = PooledBoundedMemory<byte>.Rent(payload, PoisonPagePlacement.After);
        string widened = Encoding.Latin1.GetString(bytesMemory.Span);
        byte[] payloadArray = payload.ToArray();
        Check(widened.Length == payloadArray.Length && widened.Select((c, i) => c == payloadArray[i]).All(x => x), () => "Latin1.GetString isn't a widening copy");

        // Chars -> bytes: the vectorized bulk conversion must agree with converting one char (or surrogate pair) at a time.
        char[] chars = new char[payload.Length / 2];
        for (int i = 0; i < chars.Length; i++)
        {
            ushort v = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2 * i));
            chars[i] = (v & 0x8000) != 0 ? (char)(v & 0xFF) : (v & 0x4000) != 0 ? (char)(0xD800 + (v & 0x7FF)) : (char)(v & 0x3FF);
        }

        var expected = new List<byte>();
        for (int i = 0; i < chars.Length; i++)
        {
            int width = char.IsHighSurrogate(chars[i]) && i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]) ? 2 : 1;
            expected.AddRange(Encoding.Latin1.GetBytes(chars, i, width));
            i += width - 1;
        }

        using PooledBoundedMemory<char> charsMemory = PooledBoundedMemory<char>.Rent(chars, PoisonPagePlacement.After);
        byte[] actual = Encoding.Latin1.GetBytes(chars);
        Check(actual.AsSpan().SequenceEqual(expected.ToArray()) && Encoding.Latin1.GetByteCount(charsMemory.Span) == expected.Count,
            () => $"Latin1.GetBytes of {chars.Length} chars gave {Convert.ToHexString(actual)}, one at a time gives {Convert.ToHexString(expected.ToArray())}");
        byte[] exact = new byte[expected.Count];
        Check(Encoding.Latin1.GetBytes(charsMemory.Span, exact) == expected.Count && exact.AsSpan().SequenceEqual(expected.ToArray()), () => "Latin1.GetBytes(span, span) disagrees");
        if (expected.Count > 0)
        {
            bool threw = false;
            try
            {
                Encoding.Latin1.GetBytes(charsMemory.Span, new byte[expected.Count - 1]);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Check(threw, () => "Latin1.GetBytes into a too-small span didn't throw");
        }
    }

    private static string Escape(string text) =>
        string.Concat(text.Take(120).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
