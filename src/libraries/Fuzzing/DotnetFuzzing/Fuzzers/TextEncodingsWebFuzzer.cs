// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the HTML, JavaScript and URL encoders in System.Text.Encodings.Web, with the built-in instances and with encoders
/// created from fuzzed <see cref="TextEncoderSettings"/>. All ways of encoding (string, TextWriter, span with and without
/// isFinalBlock and small destinations, UTF-8) must agree; FindFirstCharacterToEncode and FindFirstCharacterToEncodeUtf8 must
/// match a per-scalar reference built on WillEncode; WillEncode may only be false for code points the settings allow; and the
/// output must decode back to the input (lone surrogates become U+FFFD).
/// </summary>
/// <remarks>
/// Input layout: [0] encoder selector, [1] settings program length, then the settings program (3 bytes per operation), then
/// the text (mapped through a palette that favours ASCII boundaries, surrogates and other special code points), or raw
/// UTF-8 when bit 7 of [0] is set.
/// </remarks>
internal sealed class TextEncodingsWebFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers).
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Text.Encodings.Web"];
    public string[] TargetCoreLibPrefixes => [];

    private static readonly char[] s_palette = Array.ConvertAll(new ushort[]
    {
        0xD800, 0xDBFF, 0xDC00, 0xDFFF, 0xD83D, 0xDE00, 0xFFFD, 0xFFFE,
        0xFFFF, 0x2028, 0x2029, 0x00A0, 0x0085, 0x00FF, 0x0100, 0x07FF,
        0x0800, 0xE000, 0xFEFF, 0x3000, 0x00E9, 0x4E2D, 0x0000, 0x007F,
        0x0080, 0x009F, 0x200B, 0x061C, 0x0378, 0xFDD0, 0xABCD, 0x1234,
    }, c => (char)c);

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        byte selector = bytes[0];
        int programLength = Math.Min(bytes[1] % 8 * 3, bytes.Length - 2);
        ReadOnlySpan<byte> program = bytes.Slice(2, programLength);
        ReadOnlySpan<byte> payload = bytes.Slice(2 + programLength);

        bool[]? allowed = null;
        TextEncoder encoder;
        int kind = selector % 3;
        if ((selector & 0x18) == 0)
        {
            // Built-in instances.
            encoder = kind switch
            {
                0 => HtmlEncoder.Default,
                1 => (selector & 0x20) != 0 ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping : JavaScriptEncoder.Default,
                _ => UrlEncoder.Default,
            };
            if (encoder != JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
            {
                allowed = new bool[0x10000];
                allowed.AsSpan(0x20, 0x5F).Fill(true); // Basic Latin
            }
        }
        else
        {
            (TextEncoderSettings settings, allowed) = BuildSettings(program);
            encoder = kind switch
            {
                0 => HtmlEncoder.Create(settings),
                1 => JavaScriptEncoder.Create(settings),
                _ => UrlEncoder.Create(settings),
            };
        }

        if ((selector & 0x80) != 0)
        {
            Utf8Checks(encoder, payload);
            return;
        }

        var builder = new StringBuilder(payload.Length);
        for (int i = 0; i < payload.Length; i++)
        {
            byte b = payload[i];
            if (b < 0x80)
            {
                builder.Append((char)b);
            }
            else if (b < 0xF0)
            {
                builder.Append(s_palette[b % s_palette.Length]);
            }
            else if (i + 2 < payload.Length)
            {
                builder.Append((char)(payload[i + 1] | (payload[i + 2] << 8)));
                i += 2;
            }

            if (b == 0xEF)
            {
                builder.Append(builder[^1], 40); // Long runs reach the vectorized paths.
            }
        }

        string text = builder.ToString();
        Utf16Checks(encoder, text, allowed, selector);
    }

    private static (TextEncoderSettings Settings, bool[] Allowed) BuildSettings(ReadOnlySpan<byte> program)
    {
        var settings = new TextEncoderSettings();
        bool[] allowed = new bool[0x10000];
        for (int i = 0; i + 2 < program.Length; i += 3)
        {
            int op = program[i] & 7;
            char first = (char)((program[i + 1] << 8) | (program[i] & 0xF8));
            int length = program[i + 2] * ((program[i] & 0x80) != 0 ? 64 : 1);
            length = Math.Min(length, 0x10000 - first);
            switch (op)
            {
                case 0:
                case 1:
                    settings.AllowRange(UnicodeRange.Create(first, (char)(first + Math.Max(length, 1) - 1)));
                    allowed.AsSpan(first, Math.Max(length, 1)).Fill(true);
                    break;
                case 2:
                    settings.ForbidRange(UnicodeRange.Create(first, (char)(first + Math.Max(length, 1) - 1)));
                    allowed.AsSpan(first, Math.Max(length, 1)).Clear();
                    break;
                case 3:
                    settings.AllowCharacter(first);
                    allowed[first] = true;
                    break;
                case 4:
                    settings.ForbidCharacter(first);
                    allowed[first] = false;
                    break;
                case 5:
                    settings.AllowRange(UnicodeRanges.BasicLatin);
                    allowed.AsSpan(0, 0x80).Fill(true);
                    break;
                case 6:
                    settings.AllowRange(UnicodeRanges.All);
                    allowed.AsSpan().Fill(true);
                    break;
                default:
                    settings.AllowCodePoints(Enumerable.Range(first, Math.Min(length, 300)));
                    allowed.AsSpan(first, Math.Min(length, 300)).Fill(true);
                    break;
            }
        }

        int[] expected = Enumerable.Range(0, 0x10000).Where(c => allowed[c]).ToArray();
        int[] actual = settings.GetAllowedCodePoints().Order().ToArray();
        Check(actual.SequenceEqual(expected), () => $"GetAllowedCodePoints returned {actual.Length} code points, expected {expected.Length}; first difference at {FirstDifference(actual, expected)}");
        return (settings, allowed);
    }

    private static unsafe void Utf16Checks(TextEncoder encoder, string text, bool[]? allowed, byte selector)
    {
        string name = encoder.GetType().Name;
        string encoded = encoder.Encode(text);

        // Reference for FindFirstCharacterToEncode: the first lone surrogate or scalar that WillEncode.
        int expectedIndex = -1;
        for (int i = 0; i < text.Length; i++)
        {
            int scalar;
            int width = 1;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                scalar = char.ConvertToUtf32(text[i], text[i + 1]);
                width = 2;
            }
            else if (char.IsSurrogate(text[i]))
            {
                expectedIndex = i;
                break;
            }
            else
            {
                scalar = text[i];
            }

            if (encoder.WillEncode(scalar))
            {
                expectedIndex = i;
                break;
            }

            i += width - 1;
        }

        int index;
        fixed (char* p = text)
        {
            index = encoder.FindFirstCharacterToEncode(p, text.Length);
        }

        Check(index == expectedIndex, () => $"{name}.FindFirstCharacterToEncode = {index}, expected {expectedIndex} for {Describe(text)}");
        Check(index == -1 ? encoded == text : encoded.AsSpan().StartsWith(text.AsSpan(0, index)) && encoded != text,
            () => $"{name}.Encode({Describe(text)}) = {Describe(encoded)}, but FindFirstCharacterToEncode = {index}");

        // WillEncode may only be false for code points the settings allow.
        if (allowed is not null)
        {
            foreach (char c in text.Where(c => !char.IsSurrogate(c)))
            {
                Check(encoder.WillEncode(c) || allowed[c], () => $"{name}.WillEncode(U+{(int)c:X4}) is false, but the settings don't allow it");
            }
        }

        // TextWriter overloads.
        var writer = new StringWriter();
        encoder.Encode(writer, text);
        Check(writer.ToString() == encoded, () => $"{name}.Encode(TextWriter, string) = {Describe(writer.ToString())}, Encode(string) = {Describe(encoded)}");
        if (text.Length > 2)
        {
            int start = selector % (text.Length - 1);
            int count = text.Length - start - 1;
            writer = new StringWriter();
            encoder.Encode(writer, text.ToCharArray(), start, count);
            string expectedSlice = encoder.Encode(text.Substring(start, count));
            Check(writer.ToString() == expectedSlice, () => $"{name}.Encode(TextWriter, char[], {start}, {count}) disagrees with Encode(string) of the substring for {Describe(text)}");
        }

        // Span overload in one go, then in chunks with small destinations and isFinalBlock = false.
        char[] destination = new char[text.Length * encoder.MaxOutputCharactersPerInputCharacter + 1];
        OperationStatus status = encoder.Encode(text, destination, out int consumed, out int written);
        Check(status == OperationStatus.Done && consumed == text.Length && destination.AsSpan(0, written).SequenceEqual(encoded),
            () => $"{name}.Encode(span) returned {status}, consumed {consumed}, wrote {Describe(new string(destination, 0, written))}; Encode(string) = {Describe(encoded)}");
        Check(encoded.Length <= text.Length * encoder.MaxOutputCharactersPerInputCharacter,
            () => $"{name} produced {encoded.Length} chars for {text.Length}, more than MaxOutputCharactersPerInputCharacter = {encoder.MaxOutputCharactersPerInputCharacter}");

        string chunked = EncodeInChunks(encoder, text, 1 + selector % 7, 1 + (selector >> 3) % 13);
        Check(chunked == encoded, () => $"{name} chunked Encode(span) = {Describe(chunked)}, Encode(string) = {Describe(encoded)} for {Describe(text)}");

        // UTF-8: the same text through EncodeUtf8 (lone surrogates are already U+FFFD in the UTF-8 form).
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        byte[] utf8Destination = new byte[utf8.Length * encoder.MaxOutputCharactersPerInputCharacter * 3 + 1];
        status = encoder.EncodeUtf8(utf8, utf8Destination, out consumed, out written);
        string utf8Encoded = Encoding.UTF8.GetString(utf8Destination, 0, written);
        string replaced = Encoding.UTF8.GetString(utf8);
        string encodedReplaced = encoder.Encode(replaced);
        Check(status == OperationStatus.Done && consumed == utf8.Length && utf8Encoded == encodedReplaced,
            () => $"{name}.EncodeUtf8 returned {status}, consumed {consumed}/{utf8.Length}, produced {Describe(utf8Encoded)}; Encode(string) = {Describe(encodedReplaced)}");
        int utf8Index = encoder.FindFirstCharacterToEncodeUtf8(utf8);
        int expectedUtf8Index = FirstUtf8IndexToEncode(encoder, utf8);
        Check(utf8Index == expectedUtf8Index, () => $"{name}.FindFirstCharacterToEncodeUtf8 = {utf8Index}, expected {expectedUtf8Index} for {Describe(text)}");

        // Round trip through a decoder.
        string decoded = encoder switch
        {
            HtmlEncoder => WebUtility.HtmlDecode(encoded),
            JavaScriptEncoder => JsonString(encoded) ?? $"(invalid JSON string: {Describe(encoded)})",
            _ => Uri.UnescapeDataString(encoded),
        };
        Check(decoded == replaced, () => $"{name}.Encode({Describe(text)}) = {Describe(encoded)}, which decodes to {Describe(decoded)}");
    }

    private static string? JsonString(string encoded)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse("\"" + encoded + "\"");
            return document.RootElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string EncodeInChunks(TextEncoder encoder, string text, int chunk, int destinationSize)
    {
        var result = new StringBuilder();
        char[] destination = new char[destinationSize];
        int position = 0, take = chunk, guard = 0;
        while (position < text.Length)
        {
            Check(++guard < 100_000, () => "chunked encoding made no progress");
            take = Math.Min(take, text.Length - position);
            bool final = position + take == text.Length;
            OperationStatus status = encoder.Encode(text.AsSpan(position, take), destination, out int consumed, out int written, final);
            Check(consumed >= 0 && consumed <= take && written >= 0 && written <= destination.Length, () => $"Encode(span) consumed {consumed} of {take}, wrote {written} into {destination.Length}");
            result.Append(destination, 0, written);
            position += consumed;
            switch (status)
            {
                case OperationStatus.Done:
                    Check(consumed == take, () => $"Encode(span) returned Done but consumed {consumed} of {take}");
                    take = chunk;
                    break;
                case OperationStatus.NeedMoreData:
                    Check(!final && take - consumed == 1 && char.IsHighSurrogate(text[position]), () => $"Encode(span) returned NeedMoreData with {take - consumed} chars left (final = {final})");
                    take = take - consumed + chunk;
                    break;
                case OperationStatus.DestinationTooSmall:
                    if (consumed == 0 && written == 0)
                    {
                        destination = new char[destination.Length * 2];
                    }

                    take -= consumed;
                    break;
                default:
                    Check(false, () => $"Encode(span) returned {status}");
                    break;
            }
        }

        return result.ToString();
    }

    private static void Utf8Checks(TextEncoder encoder, ReadOnlySpan<byte> utf8)
    {
        // Arbitrary (possibly invalid) UTF-8: invalid sequences are replaced by U+FFFD with the same maximal-subpart rules as
        // Encoding.UTF8, and FindFirstCharacterToEncodeUtf8 stops at the first invalid or encoded scalar.
        string name = encoder.GetType().Name;
        string hex = Convert.ToHexString(utf8);
        // Invalid sequences always come out as an escaped U+FFFD, even when U+FFFD itself is allowed; a lone surrogate
        // does the same in the UTF-16 path, so use one to stand in for each invalid sequence.
        var builder = new StringBuilder();
        for (int i = 0; i < utf8.Length;)
        {
            OperationStatus decodeStatus = Rune.DecodeFromUtf8(utf8.Slice(i), out Rune rune, out int length);
            builder.Append(decodeStatus == OperationStatus.Done ? rune.ToString() : "\uDC00");
            i += length;
        }

        string expected = encoder.Encode(builder.ToString());
        byte[] destination = new byte[utf8.Length * encoder.MaxOutputCharactersPerInputCharacter * 3 + 16];
        OperationStatus status = encoder.EncodeUtf8(utf8, destination, out int consumed, out int written);
        string actual = Encoding.UTF8.GetString(destination, 0, written);
        Check(status == OperationStatus.Done && consumed == utf8.Length && actual == expected,
            () => $"{name}.EncodeUtf8({hex}) returned {status}, consumed {consumed}, produced {Describe(actual)}; expected {Describe(expected)}");

        int expectedIndex = FirstUtf8IndexToEncode(encoder, utf8);
        int index = encoder.FindFirstCharacterToEncodeUtf8(utf8);
        Check(index == expectedIndex, () => $"{name}.FindFirstCharacterToEncodeUtf8({hex}) = {index}, expected {expectedIndex}");

        // Chunked with isFinalBlock = false: split multi-byte sequences must come back as NeedMoreData.
        var result = new List<byte>();
        byte[] small = new byte[16];
        int position = 0, guard = 0;
        int step = Math.Max(1, utf8.Length / 5);
        while (position < utf8.Length)
        {
            Check(++guard < 100_000, () => "chunked UTF-8 encoding made no progress");
            int take = Math.Min(step, utf8.Length - position);
            bool final = position + take == utf8.Length;
            status = encoder.EncodeUtf8(utf8.Slice(position, take), small, out consumed, out written, final);
            result.AddRange(small.AsSpan(0, written).ToArray());
            position += consumed;
            if (status == OperationStatus.NeedMoreData)
            {
                Check(!final && take - consumed <= 3, () => $"EncodeUtf8 returned NeedMoreData with {take - consumed} bytes left (final = {final})");
                step = take - consumed + 1;
            }
            else if (status == OperationStatus.DestinationTooSmall)
            {
                if (consumed == 0 && written == 0)
                {
                    small = new byte[small.Length * 2];
                }
            }
            else
            {
                Check(status == OperationStatus.Done && consumed == take, () => $"EncodeUtf8 returned {status}, consumed {consumed} of {take}");
                step = Math.Max(1, utf8.Length / 5);
            }
        }

        string chunked = Encoding.UTF8.GetString(result.ToArray());
        Check(chunked == expected, () => $"{name} chunked EncodeUtf8({hex}) = {Describe(chunked)}, expected {Describe(expected)}");
    }

    private static int FirstUtf8IndexToEncode(TextEncoder encoder, ReadOnlySpan<byte> utf8)
    {
        for (int i = 0; i < utf8.Length;)
        {
            if (Rune.DecodeFromUtf8(utf8.Slice(i), out Rune rune, out int length) != OperationStatus.Done || encoder.WillEncode(rune.Value))
            {
                return i;
            }

            i += length;
        }

        return -1;
    }

    private static int FirstDifference(int[] a, int[] b)
    {
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            if (a[i] != b[i])
            {
                return Math.Min(a[i], b[i]);
            }
        }

        return a.Length < b.Length ? b[a.Length] : a.Length > b.Length ? a[b.Length] : -1;
    }

    private static string Describe(string text) =>
        "\"" + string.Concat(text.Take(160).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}")) + (text.Length > 160 ? $"...\" ({text.Length} chars)" : "\"");

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
