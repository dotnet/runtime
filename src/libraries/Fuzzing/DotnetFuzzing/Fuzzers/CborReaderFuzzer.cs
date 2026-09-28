// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="CborReader"/> in all conformance modes and in incremental (non-final block) mode.
/// Checks that:
/// * the reader only throws <see cref="CborContentException"/> while following its own <see cref="CborReader.PeekState"/>;
/// * reading the input in fuzzer-chosen chunks through <see cref="CborReader.SlideData"/> produces exactly the same tokens
///   and outcome as reading it in one go;
/// * the conformance modes nest (whatever Strict accepts, Lax accepts identically; whatever the canonical modes accept,
///   Strict accepts identically);
/// * input accepted by a canonical mode re-encodes byte for byte through <see cref="CborWriter"/> in that mode.
/// </summary>
/// <remarks>Input layout: [0] flags (bit0 multiple root values), [1] chunking seed, [2] walk-decision seed, [3..] CBOR data.</remarks>
internal sealed class CborReaderFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * Major type 7 with reserved additional information 28-30 (0xFC-0xFE): PeekState reports SimpleValue and
    //   ReadSimpleValue then throws InvalidOperationException instead of the reader throwing CborContentException.
    // * With AllowMultipleRootLevelValues, a root-level sequence ending in a dangling tag reports Finished although it isn't
    //   well-formed, and SkipValue over it throws InvalidOperationException ("... is not at the start of a data item").
    // * Canonical/Ctap2Canonical readers accept floats that are not in their shortest form, which the canonical writer
    //   re-encodes shorter, so canonical inputs containing floats don't round-trip byte for byte.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Formats.Cbor"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 3)
        {
            return;
        }

        bool multipleRoots = (bytes[0] & 1) != 0;
        byte chunkSeed = bytes[1];
        byte decisionSeed = bytes[2];
        byte[] data = bytes.Slice(3).ToArray();

        var results = new Dictionary<CborConformanceMode, Outcome>();
        foreach (CborConformanceMode mode in (ReadOnlySpan<CborConformanceMode>)[CborConformanceMode.Lax, CborConformanceMode.Strict, CborConformanceMode.Canonical, CborConformanceMode.Ctap2Canonical])
        {
            var reader = new CborReader(data, new CborReaderOptions { ConformanceMode = mode, AllowMultipleRootLevelValues = multipleRoots });
            results[mode] = Walk(reader, decisionSeed, incremental: null);
        }

        // Conformance modes nest.
        CheckSubset(results[CborConformanceMode.Strict], results[CborConformanceMode.Lax], "Strict", "Lax");
        CheckSubset(results[CborConformanceMode.Canonical], results[CborConformanceMode.Strict], "Canonical", "Strict");
        CheckSubset(results[CborConformanceMode.Ctap2Canonical], results[CborConformanceMode.Strict], "Ctap2Canonical", "Strict");

        // Canonical encodings round-trip exactly.
        foreach (CborConformanceMode mode in (ReadOnlySpan<CborConformanceMode>)[CborConformanceMode.Canonical, CborConformanceMode.Ctap2Canonical])
        {
            if (results[mode].Success && results[mode].Trace.Count > 0)
            {
                byte[] reencoded;
                int consumed;
                bool hasFloats, danglingTag;
                try
                {
                    (reencoded, consumed, hasFloats, danglingTag) = Reencode(data, mode, multipleRoots);
                }
                catch (InvalidOperationException) when (!s_strict)
                {
                    // The canonical writer shortens floats, which can collapse distinct float keys (e.g. NaNs with
                    // different payloads) into duplicates.
                    continue;
                }

                if (!s_strict && (hasFloats || danglingTag))
                {
                    continue;
                }

                Check(reencoded.AsSpan().SequenceEqual(data.AsSpan(0, consumed)), $"{mode} input re-encodes differently: input {Convert.ToHexString(data, 0, consumed)}, re-encoded {Convert.ToHexString(reencoded)}");
            }
        }

        // Incremental reading (Lax only) must match the one-shot Lax read token for token.
        var chunks = new Feeder(data, chunkSeed);
        var incrementalReader = new CborReader(chunks.Start(), new CborReaderOptions { ConformanceMode = CborConformanceMode.Lax, AllowMultipleRootLevelValues = multipleRoots }, isFinalBlock: chunks.Final);
        Outcome incremental = Walk(incrementalReader, decisionSeed, chunks);
        Outcome oneShot = results[CborConformanceMode.Lax];
        // When both fail, the incremental reader may get further: it can't reject a declared length that exceeds the buffer
        // until the data is actually missing.
        bool consistent = incremental.Success == oneShot.Success &&
            (incremental.Success ? incremental.Trace.SequenceEqual(oneShot.Trace) : incremental.Trace.Take(oneShot.Trace.Count).SequenceEqual(oneShot.Trace));
        Check(consistent,
            $"incremental read differs from one-shot read.\n  one-shot:    {oneShot}\n  incremental: {incremental}\n  chunks: [{string.Join(", ", chunks.Sizes)}] data {Convert.ToHexString(data)}");
    }

    private sealed record Outcome(List<string> Trace, bool Success, string? Error)
    {
        public override string ToString() => $"{(Success ? "success" : "error " + Error)} after [{string.Join(" ", Trace)}]";
    }

    private static void CheckSubset(Outcome stricter, Outcome laxer, string stricterName, string laxerName)
    {
        if (stricter.Success)
        {
            Check(laxer.Success && laxer.Trace.SequenceEqual(stricter.Trace), $"{stricterName} accepts but {laxerName} differs.\n  {stricterName}: {stricter}\n  {laxerName}: {laxer}");
        }
    }

    /// <summary>Feeds the data to an incremental reader in pseudo-random chunk sizes, carrying the unread bytes forward.</summary>
    private sealed class Feeder(byte[] data, byte seed)
    {
        private int _fed;
        private uint _state = (uint)seed * 2654435761u + 1;
        private ReadOnlyMemory<byte> _buffer;

        public List<int> Sizes { get; } = [];
        public bool Final => _fed >= data.Length;

        public ReadOnlyMemory<byte> Start() => _buffer = NextBuffer(ReadOnlyMemory<byte>.Empty);

        /// <summary>Slides the reader's unread bytes followed by the next chunk into the reader.</summary>
        public void Feed(CborReader reader)
        {
            ReadOnlyMemory<byte> unread = _buffer.Slice(_buffer.Length - reader.BytesRemaining);
            _buffer = NextBuffer(unread);
            reader.SlideData(_buffer, isFinalBlock: Final);
        }

        private byte[] NextBuffer(ReadOnlyMemory<byte> unread)
        {
            _state = _state * 1103515245 + 12345;
            int size = Math.Min((int)((_state >> 16) % 9), data.Length - _fed);
            Sizes.Add(size);

            byte[] buffer = new byte[unread.Length + size];
            unread.CopyTo(buffer);
            data.AsSpan(_fed, size).CopyTo(buffer.AsSpan(unread.Length));
            _fed += size;
            return buffer;
        }
    }

    private static Outcome Walk(CborReader reader, byte decisionSeed, Feeder? incremental)
    {
        var trace = new List<string>();
        int steps = 0;

        while (true)
        {
            CborReaderState state;
            try
            {
                state = reader.PeekState();
            }
            catch (CborContentException ex)
            {
                return new Outcome(trace, false, "peek: " + ex.Message);
            }

            if (state == CborReaderState.NeedsMoreData)
            {
                Check(incremental is not null, $"a final-block reader reported NeedsMoreData after [{string.Join(" ", trace)}]");
                Check(!incremental!.Final, $"NeedsMoreData after the final block after [{string.Join(" ", trace)}]");
                incremental.Feed(reader);
                continue;
            }

            if (state == CborReaderState.Finished)
            {
                return new Outcome(trace, true, null);
            }

            // Deterministic walk decisions, independent of chunking: sometimes skip a value or read it encoded.
            int decision = (decisionSeed * 31 + steps * 17) & 31;
            steps++;

            bool isValueStart = state is not (CborReaderState.EndArray or CborReaderState.EndMap or CborReaderState.EndIndefiniteLengthByteString or CborReaderState.EndIndefiniteLengthTextString);
            try
            {
                if (isValueStart && decision == 0)
                {
                    RetryOnTruncation(reader, incremental, r => r.SkipValue());
                    trace.Add("skip");
                    continue;
                }

                if (isValueStart && decision == 1)
                {
                    byte[] encoded = [];
                    RetryOnTruncation(reader, incremental, r => encoded = r.ReadEncodedValue().ToArray());
                    trace.Add("enc:" + Convert.ToHexString(encoded));
                    continue;
                }

                trace.Add(ReadToken(reader, state));
            }
            catch (CborContentException ex)
            {
                return new Outcome(trace, false, $"{state}: {ex.Message}");
            }
            catch (InvalidOperationException ex) when (!s_strict && ex.Source != "DotnetFuzzing" &&
                (ex.Message.Contains("does not encode a simple value", StringComparison.Ordinal) || ex.Message.Contains("is not at the start of a data item", StringComparison.Ordinal)))
            {
                return new Outcome(trace, false, $"{state}: {ex.Message}");
            }
            catch (Exception ex) when (ex is not InvalidOperationException { Source: "DotnetFuzzing" })
            {
                throw new InvalidOperationException($"CborReader ({reader.ConformanceMode}, incremental {incremental is not null}) threw {ex.GetType().Name} reading state {state} after [{string.Join(" ", trace)}]: {ex.Message}", ex) { Source = "DotnetFuzzing" };
            }
        }
    }

    /// <summary>
    /// Multi-token reads throw on truncation in incremental mode and restore the reader, so the caller slides in more data and
    /// retries. Once the final block is in, a failure is a real content error.
    /// </summary>
    private static void RetryOnTruncation(CborReader reader, Feeder? feeder, Action<CborReader> read)
    {
        while (true)
        {
            try
            {
                read(reader);
                return;
            }
            catch (CborContentException) when (feeder is not null && !feeder.Final)
            {
                feeder.Feed(reader);
            }
        }
    }

    private static string ReadToken(CborReader reader, CborReaderState state) => state switch
    {
        CborReaderState.UnsignedInteger => "u" + reader.ReadUInt64(),
        CborReaderState.NegativeInteger => "n" + reader.ReadCborNegativeIntegerRepresentation(),
        CborReaderState.ByteString => "b" + Convert.ToHexString(reader.ReadByteString()),
        CborReaderState.TextString => "t" + Convert.ToHexString(Encoding.UTF8.GetBytes(reader.ReadTextString())),
        CborReaderState.StartIndefiniteLengthByteString => Do(reader.ReadStartIndefiniteLengthByteString, "b["),
        CborReaderState.EndIndefiniteLengthByteString => Do(reader.ReadEndIndefiniteLengthByteString, "]b"),
        CborReaderState.StartIndefiniteLengthTextString => Do(reader.ReadStartIndefiniteLengthTextString, "t["),
        CborReaderState.EndIndefiniteLengthTextString => Do(reader.ReadEndIndefiniteLengthTextString, "]t"),
        CborReaderState.StartArray => "a" + (reader.ReadStartArray()?.ToString() ?? "_"),
        CborReaderState.EndArray => Do(reader.ReadEndArray, "]a"),
        CborReaderState.StartMap => "m" + (reader.ReadStartMap()?.ToString() ?? "_"),
        CborReaderState.EndMap => Do(reader.ReadEndMap, "]m"),
        CborReaderState.Tag => "tag" + (ulong)reader.ReadTag(),
        CborReaderState.SimpleValue => "s" + (byte)reader.ReadSimpleValue(),
        CborReaderState.HalfPrecisionFloat => "h" + BitConverter.HalfToUInt16Bits(reader.ReadHalf()).ToString("X4"),
        CborReaderState.SinglePrecisionFloat => "f" + BitConverter.SingleToUInt32Bits(reader.ReadSingle()).ToString("X8"),
        CborReaderState.DoublePrecisionFloat => "d" + BitConverter.DoubleToUInt64Bits(reader.ReadDouble()).ToString("X16"),
        CborReaderState.Null => Do(reader.ReadNull, "null"),
        CborReaderState.Boolean => reader.ReadBoolean() ? "true" : "false",
        _ => throw new InvalidOperationException($"unexpected reader state {state}") { Source = "DotnetFuzzing" },
    };

    private static string Do(Action action, string token)
    {
        action();
        return token;
    }

    private static (byte[] Encoded, int Consumed, bool HasFloats, bool DanglingTag) Reencode(byte[] data, CborConformanceMode mode, bool multipleRoots)
    {
        var reader = new CborReader(data, new CborReaderOptions { ConformanceMode = mode, AllowMultipleRootLevelValues = multipleRoots });
        var writer = new CborWriter(mode, convertIndefiniteLengthEncodings: false, allowMultipleRootLevelValues: multipleRoots);
        bool hasFloats = false;
        CborReaderState previous = CborReaderState.Undefined;
        while (true)
        {
            CborReaderState state = reader.PeekState();
            hasFloats |= state is CborReaderState.HalfPrecisionFloat or CborReaderState.SinglePrecisionFloat or CborReaderState.DoublePrecisionFloat;
            if (state == CborReaderState.Finished && previous == CborReaderState.Tag)
            {
                return ([], 0, hasFloats, DanglingTag: true);
            }

            previous = state;
            switch (state)
            {
                case CborReaderState.Finished: return (writer.Encode(), data.Length - reader.BytesRemaining, hasFloats, false);
                case CborReaderState.UnsignedInteger: writer.WriteUInt64(reader.ReadUInt64()); break;
                case CborReaderState.NegativeInteger: writer.WriteCborNegativeIntegerRepresentation(reader.ReadCborNegativeIntegerRepresentation()); break;
                case CborReaderState.ByteString: writer.WriteByteString(reader.ReadByteString()); break;
                case CborReaderState.TextString: writer.WriteTextString(reader.ReadTextString()); break;
                case CborReaderState.StartIndefiniteLengthByteString: reader.ReadStartIndefiniteLengthByteString(); writer.WriteStartIndefiniteLengthByteString(); break;
                case CborReaderState.EndIndefiniteLengthByteString: reader.ReadEndIndefiniteLengthByteString(); writer.WriteEndIndefiniteLengthByteString(); break;
                case CborReaderState.StartIndefiniteLengthTextString: reader.ReadStartIndefiniteLengthTextString(); writer.WriteStartIndefiniteLengthTextString(); break;
                case CborReaderState.EndIndefiniteLengthTextString: reader.ReadEndIndefiniteLengthTextString(); writer.WriteEndIndefiniteLengthTextString(); break;
                case CborReaderState.StartArray: writer.WriteStartArray(reader.ReadStartArray()); break;
                case CborReaderState.EndArray: reader.ReadEndArray(); writer.WriteEndArray(); break;
                case CborReaderState.StartMap: writer.WriteStartMap(reader.ReadStartMap()); break;
                case CborReaderState.EndMap: reader.ReadEndMap(); writer.WriteEndMap(); break;
                case CborReaderState.Tag: writer.WriteTag(reader.ReadTag()); break;
                case CborReaderState.SimpleValue: writer.WriteSimpleValue(reader.ReadSimpleValue()); break;
                case CborReaderState.HalfPrecisionFloat: writer.WriteHalf(reader.ReadHalf()); break;
                case CborReaderState.SinglePrecisionFloat: writer.WriteSingle(reader.ReadSingle()); break;
                case CborReaderState.DoublePrecisionFloat: writer.WriteDouble(reader.ReadDouble()); break;
                case CborReaderState.Null: reader.ReadNull(); writer.WriteNull(); break;
                case CborReaderState.Boolean: writer.WriteBoolean(reader.ReadBoolean()); break;
                default: throw new InvalidOperationException($"unexpected state {state}");
            }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message) { Source = "DotnetFuzzing" };
        }
    }
}
