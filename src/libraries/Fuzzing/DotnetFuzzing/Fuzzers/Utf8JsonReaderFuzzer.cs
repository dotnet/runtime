// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using SharpFuzz;

namespace DotnetFuzzing.Fuzzers;

internal sealed class Utf8JsonReaderFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Text.Json"];

    public string[] TargetCoreLibPrefixes => [];

    delegate T RefFunc<T>(ref Utf8JsonReader reader);

    static Random s_random = default!;

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        const int minLength = 10;
        if (bytes.Length < minLength)
        {
            return;
        }

        // Create a random seed from the last 4 bytes of the input
        int len = bytes.Length;
        int randomSeed = bytes[len - 1] + (bytes[len - 2] << 8) + (bytes[len - 3] << 16) + (bytes[len - 4] << 24);
        s_random = new Random(randomSeed);

        // Remove the 4 bytes used for the random seed
        bytes = bytes.Slice(0, len - 4);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            AllowTrailingCommas = s_random.Next() % 2 == 0,
            Encoder = s_random.Next() % 2 == 0 ? JavaScriptEncoder.Default : JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // TODO: JsonExceptions differ between Span and Sequence paths
            ReadCommentHandling = (JsonCommentHandling)s_random.Next(0, 2),
            NumberHandling = (JsonNumberHandling)s_random.Next(0, 4),
        };

        // Fuzz using ReadOnlySpan<byte>
        var readerSpan = new Utf8JsonReader(bytes, isFinalBlock: true, state: default);

        // Fuzz using ReadOnlySequence<byte>
        var sequence = CreateVariableSegmentSequence(bytes);
        var readerSequence = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);

        //Debugger.Launch();

        var byteArr = bytes.ToArray();
        TestDeserializeAsync<string>(byteArr, sequence, options);
        TestDeserializeAsync<decimal>(byteArr, sequence, options);
        TestDeserializeAsync<byte>(byteArr, sequence, options);
        TestDeserializeAsync<Guid>(byteArr, sequence, options);
        TestDeserializeAsync<DateTime>(byteArr, sequence, options);
        TestDeserializeAsync<DateTimeOffset>(byteArr, sequence, options);
        TestDeserializeAsync<IntEnum>(byteArr, sequence, options);
        TestDeserializeAsync<StringEnum>(byteArr, sequence, options);
        TestDeserializeAsync<bool>(byteArr, sequence, options);
        TestDeserializeAsync<byte[]>(byteArr, sequence, options, (l, r) => l.AsSpan().SequenceEqual(r.AsSpan()));
        TestDeserializeAsync<char>(byteArr, sequence, options);
        TestDeserializeAsync<double>(byteArr, sequence, options);
        TestDeserializeAsync<Int16>(byteArr, sequence, options);
        TestDeserializeAsync<Int32>(byteArr, sequence, options);
        TestDeserializeAsync<Int64>(byteArr, sequence, options);
        TestDeserializeAsync<sbyte>(byteArr, sequence, options);
        TestDeserializeAsync<Single>(byteArr, sequence, options);
        TestDeserializeAsync<TimeSpan>(byteArr, sequence, options);
        TestDeserializeAsync<UInt16>(byteArr, sequence, options);
        TestDeserializeAsync<UInt32>(byteArr, sequence, options);
        TestDeserializeAsync<UInt64>(byteArr, sequence, options);
        TestDeserializeAsync<Uri>(byteArr, sequence, options);
        TestDeserializeAsync<Version>(byteArr, sequence, options);
        TestDeserializeAsync<object>(byteArr, sequence, options, (l, r) => JsonElement.DeepEquals((JsonElement)l!, (JsonElement)r!));
        TestDeserializeAsync<JsonElement>(byteArr, sequence, options, (l, r) => JsonElement.DeepEquals(l, r));
        TestDeserializeAsync<JsonDocument>(byteArr, sequence, options, (l, r) => JsonElement.DeepEquals(l!.RootElement, r!.RootElement));
        TestDeserializeAsync<JsonArray>(byteArr, sequence, options, (l, r) => JsonNode.DeepEquals(l, r));
        TestDeserializeAsync<JsonValue>(byteArr, sequence, options, (l, r) => JsonNode.DeepEquals(l, r));
        TestDeserializeAsync<JsonNode>(byteArr, sequence, options, (l, r) => JsonNode.DeepEquals(l, r));
        TestDeserializeAsync<JsonObject>(byteArr, sequence, options, (l, r) => JsonNode.DeepEquals(l, r));
        TestDeserializeAsync<Memory<byte>>(byteArr, sequence, options, (l, r) => l.Span.SequenceEqual(r.Span));
        TestDeserializeAsync<ReadOnlyMemory<byte>>(byteArr, sequence, options, (l, r) => l.Span.SequenceEqual(r.Span));
        TestDeserializeAsync<DateOnly>(byteArr, sequence, options);
        TestDeserializeAsync<TimeOnly>(byteArr, sequence, options);
        TestDeserializeAsync<Half>(byteArr, sequence, options);
        TestDeserializeAsync<Int128>(byteArr, sequence, options);
        TestDeserializeAsync<UInt128>(byteArr, sequence, options);
        TestDeserializeAsync<List<int>>(byteArr, sequence, options, (l, r) =>
        {
           if (l!.Count != r!.Count)
               return false;

           for (int i = 0; i < l.Count; i++)
           {
               if (l[i] != r[i])
                   return false;
           }
           return true;
        });
        TestDeserializeAsync<Dictionary<string, int>>(byteArr, sequence, options, (l, r) =>
        {
           if (l!.Count != r!.Count)
               return false;

           foreach (var kvp in l)
           {
               if (!r.TryGetValue(kvp.Key, out int value) || value != kvp.Value)
                   return false;
           }
           return true;
        });

        TestRandomPoco(bytes, sequence, options);
        TestRandomPocoWithSetters(bytes, sequence, options);

        while (true)
        {
            if (!Test(ref readerSpan, ref readerSequence,
                static (ref readerSpan) => { return readerSpan.Read(); },
                static (ref readerSequence) => { return readerSequence.Read(); }
            ))
            {
                break;
            }

            if (readerSpan.TokenType != readerSequence.TokenType ||
                !(readerSequence.HasValueSequence ? readerSpan.ValueSpan.SequenceEqual(readerSequence.ValueSequence.ToArray()) :
                readerSpan.ValueSpan.SequenceEqual(readerSequence.ValueSpan)))
            {
                throw new InvalidOperationException("Span and Sequence readers diverged in token or value. " +
                                                    $"Span TokenType: {readerSpan.TokenType}, Value: {Convert.ToHexString(readerSpan.ValueSpan)}{Environment.NewLine}" +
                                                    $"Seq TokenType: {readerSequence.TokenType}, Value: {Convert.ToHexString(readerSequence.ValueSequence.ToArray())}");
            }

            // Example: Test TryGetInt32 after each Read
            if (readerSpan.TokenType == JsonTokenType.Number)
            {
                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { int value; return readerSpan.TryGetInt32(out value) ? value : (int?)null; },
                    static (ref readerSequence) => { int value; return readerSequence.TryGetInt32(out value) ? value : (int?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { byte value; return readerSpan.TryGetByte(out value) ? value : (byte?)null; },
                    static (ref readerSequence) => { byte value; return readerSequence.TryGetByte(out value) ? value : (byte?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { short value; return readerSpan.TryGetInt16(out value) ? value : (short?)null; },
                    static (ref readerSequence) => { short value; return readerSequence.TryGetInt16(out value) ? value : (short?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { long value; return readerSpan.TryGetInt64(out value) ? value : (long?)null; },
                    static (ref readerSequence) => { long value; return readerSequence.TryGetInt64(out value) ? value : (long?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { float value; return readerSpan.TryGetSingle(out value) ? value : (float?)null; },
                    static (ref readerSequence) => { float value; return readerSequence.TryGetSingle(out value) ? value : (float?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { double value; return readerSpan.TryGetDouble(out value) ? value : (double?)null; },
                    static (ref readerSequence) => { double value; return readerSequence.TryGetDouble(out value) ? value : (double?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { decimal value; return readerSpan.TryGetDecimal(out value) ? value : (decimal?)null; },
                    static (ref readerSequence) => { decimal value; return readerSequence.TryGetDecimal(out value) ? value : (decimal?)null; }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { sbyte value; return readerSpan.TryGetSByte(out value) ? value : (sbyte?)null; },
                    static (ref readerSequence) => { sbyte value; return readerSequence.TryGetSByte(out value) ? value : (sbyte?)null; }
                );
            }

            if (readerSpan.TokenType == JsonTokenType.String)
            {
                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) =>
                    {
                        DateTime value;
                        try
                        {
                            readerSpan.TryGetDateTime(out value);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("surrogate"))
                        {
                            // If the string is not a valid DateTime, we expect an exception
                            return (DateTime?)null;
                        }
                        return value;
                    },
                    static (ref readerSequence) =>
                    {
                        DateTime value;
                        try
                        {
                            readerSequence.TryGetDateTime(out value);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("surrogate"))
                        {
                            // If the string is not a valid DateTime, we expect an exception
                            return (DateTime?)null;
                        }
                        return value;
                    }
                );

                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) =>
                    {
                        string? value;
                        try
                        {
                            value = readerSpan.GetString();
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("surrogate"))
                        {
                            // If the string is not a valid DateTime, we expect an exception
                            return null;
                        }
                        return value;
                    },
                    static (ref readerSequence) =>
                    {
                        string? value;
                        try
                        {
                            value = readerSequence.GetString();
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("surrogate"))
                        {
                            // If the string is not a valid DateTime, we expect an exception
                            return null;
                        }
                        return value;
                    }
                );
            }

            if (readerSpan.TokenType == JsonTokenType.True || readerSpan.TokenType == JsonTokenType.False)
            {
                Test(ref readerSpan, ref readerSequence,
                    static (ref readerSpan) => { return readerSpan.GetBoolean(); },
                    static (ref readerSequence) => { return readerSequence.GetBoolean(); }
                );
            }
        }
    }

    private static T Test<T>(
        ref Utf8JsonReader spanReader,
        ref Utf8JsonReader seqReader,
        RefFunc<T> span,
        RefFunc<T> seq,
        Func<T, T, bool>? comparer = null)
    {
        T spanToken = default!;
        T seqToken = default!;
        Exception? spanEx = null, seqEx = null;

        try
        {
            spanToken = span(ref spanReader);
        }
        catch (Exception ex)
        {
            spanEx = ex;
        }

        try
        {
            seqToken = seq(ref seqReader);
        }
        catch (Exception ex)
        {
            seqEx = ex;
        }

        if (CompareExceptions(spanEx, seqEx))
        {
            return default!;
        }

        if (spanToken is null && seqToken is null)
        {
            // Both returned null, consider it a match
            return spanToken;
        }
        else if (spanToken is null || seqToken is null)
        {
            throw new InvalidOperationException("Span and Sequence readers diverged in token availability. " +
                                                $"Span has token: {spanToken}, Sequence has token: {seqToken}");
        }

        try
        {
            if (!comparer?.Invoke(spanToken, seqToken) ?? !spanToken.Equals(seqToken))
            {
                throw new InvalidOperationException("Span and Sequence readers diverged in token availability. " +
                                                    $"Span has token: {spanToken}, Sequence has token: {seqToken}");
            }
        }
        catch (Exception ex)
        {
            if (CompareExceptions(ex, ex))
            {
                return default!;
            }
        }

        if (spanReader.BytesConsumed != seqReader.BytesConsumed ||
            spanReader.TokenStartIndex != seqReader.TokenStartIndex)
        {
            throw new InvalidOperationException("Span and Sequence readers diverged in BytesConsumed or TokenStartIndex. " +
                                                $"Span BytesConsumed: {spanReader.BytesConsumed}, TokenStartIndex: {spanReader.TokenStartIndex}{Environment.NewLine}" +
                                                $"Seq BytesConsumed: {seqReader.BytesConsumed}, TokenStartIndex: {seqReader.TokenStartIndex}");
        }

        return spanToken;
    }

    internal static void TestDeserializeAsync<T>(byte[] bytes, ReadOnlySequence<byte> sequence, JsonSerializerOptions options, Func<T?, T?, bool>? comparer = null)
    {
        Utf8JsonReader readerSpan = new();
        Test(ref readerSpan, ref readerSpan,
            (ref _) => (T?)JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(T), options).GetAwaiter().GetResult(),
            (ref _) => (T?)JsonSerializer.DeserializeAsync(PipeReader.Create(sequence), typeof(T), options).GetAwaiter().GetResult(),
            comparer);
    }

    static readonly Type[] groundTypes = [
        typeof(int),
        typeof(string),
        typeof(decimal),
        typeof(DateTimeOffset),
        typeof(JsonDocument),
        typeof(byte),
        typeof(Guid),
        typeof(DateTime),
        typeof(IntEnum),
        typeof(StringEnum),
        typeof(bool),
        // Add more types as needed
    ];

    void TestRandomPoco(ReadOnlySpan<byte> bytes, ReadOnlySequence<byte> sequence, JsonSerializerOptions options)
    {
        Span<Type> selectedTypes = new Type[6];
        for (int i = 0; i < 6; i++)
            selectedTypes[i] = groundTypes[s_random.Next() % groundTypes.Length];

        Type pocoType = typeof(PocoTemplate<,,,,,>).MakeGenericType(selectedTypes.ToArray());
        Type testerType = typeof(Tester<>).MakeGenericType(pocoType);
        var testerInstance = (ITester)Activator.CreateInstance(testerType)!;

        testerInstance.TestWrapper(bytes, sequence, options);
    }

    void TestRandomPocoWithSetters(ReadOnlySpan<byte> bytes, ReadOnlySequence<byte> sequence, JsonSerializerOptions options)
    {
        Span<Type> selectedTypes = new Type[6];
        for (int i = 0; i < 6; i++)
            selectedTypes[i] = groundTypes[s_random.Next() % groundTypes.Length];

        Type pocoType = typeof(PocoWithSetters<,,,,,>).MakeGenericType(selectedTypes.ToArray());
        Type testerType = typeof(Tester<>).MakeGenericType(pocoType);
        var testerInstance = (ITester)Activator.CreateInstance(testerType)!;

        testerInstance.TestWrapper(bytes, sequence, options);
    }

    private static ReadOnlySequence<byte> CreateVariableSegmentSequence(ReadOnlySpan<byte> bytes)
    {
        // Split into 2-5 segments, but never less than 1 byte per segment
        int segmentCount = s_random.Next(2, Math.Min(6, bytes.Length + 1));

        if (bytes.Length == 0 || bytes.Length < segmentCount)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        int[] segmentSizes = new int[segmentCount];
        int remaining = bytes.Length;

        // Assign at least 1 byte per segment
        for (int i = 0; i < segmentCount; i++)
        {
            int max = remaining - (segmentCount - i - 1);
            segmentSizes[i] = (i == segmentCount - 1) ? max : s_random.Next(1, max);
            remaining -= segmentSizes[i];
        }

        // Build segments
        var buffers = new List<ReadOnlyMemory<byte>>(segmentCount);
        int offset = 0;
        for (int i = 0; i < segmentCount; i++)
        {
            buffers.Add(bytes.Slice(offset, segmentSizes[i]).ToArray());
            offset += segmentSizes[i];
        }

        // Link segments
        ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(buffers[0]);
        if (buffers.Count == 1)
            return sequence;

        var segments = new List<BufferSegment>();
        foreach (var buffer in buffers)
            segments.Add(new BufferSegment(buffer));

        for (int i = 0; i < segments.Count - 1; i++)
            segments[i].SetNext(segments[i + 1]);

        return new ReadOnlySequence<byte>(segments[0], 0, segments[^1], segments[^1].Memory.Length);
    }

    // Helper class for multi-segment ReadOnlySequence
    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }
        public void SetNext(BufferSegment next)
        {
            Next = next;
            next.RunningIndex = RunningIndex + Memory.Length;
        }
    }

    public record PocoTemplate<T1, T2, T3, T4, T5, T6>(
        T1 Property1,
        T2 Property2,
        T3 Property3,
        T4 Property4,
        T5 Property5,
        T6 Property6
    );

    public class PocoWithSetters<T1, T2, T3, T4, T5, T6>
    {
        public T1 Property1 { get; set; } = default!;
        public T2 Property2 { get; set; } = default!;
        public T3 Property3 { get; set; } = default!;
        public T4 Property4 { get; set; } = default!;
        public T5 Property5 { get; set; } = default!;
        public T6 Property6 { get; set; } = default!;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is not PocoWithSetters<T1, T2, T3, T4, T5, T6> other)
                return false;

            return
                EqualityComparer<T1>.Default.Equals(Property1, other.Property1) &&
                EqualityComparer<T2>.Default.Equals(Property2, other.Property2) &&
                EqualityComparer<T3>.Default.Equals(Property3, other.Property3) &&
                EqualityComparer<T4>.Default.Equals(Property4, other.Property4) &&
                EqualityComparer<T5>.Default.Equals(Property5, other.Property5) &&
                EqualityComparer<T6>.Default.Equals(Property6, other.Property6);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Property1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Property1),
                Property2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Property2),
                Property3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Property3),
                Property4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Property4),
                Property5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Property5),
                Property6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Property6)
            );
        }
    }

    public interface ITester
    {
        void TestWrapper(ReadOnlySpan<byte> bytes, ReadOnlySequence<byte> sequence, JsonSerializerOptions options);
    }

    public class Tester<T> : ITester
    {
        public void TestWrapper(ReadOnlySpan<byte> bytes, ReadOnlySequence<byte> sequence, JsonSerializerOptions options)
        {
            TestDeserializeAsync<T>(bytes.ToArray(), sequence, options);
        }
    }

    // true if had exception and processed it, false if no exception, throw if exceptions don't match or are unexpected
    public static bool CompareExceptions(Exception? spanEx, Exception? seqEx)
    {
        if (spanEx is null || seqEx is null)
        {
            if (spanEx is not null)
            {
                throw new InvalidOperationException(
                    $"Span and Sequence readers diverged in exception:{Environment.NewLine}" +
                    $"Span: {spanEx.GetType().Name} '{spanEx.Message}'{Environment.NewLine}" +
                    $"Seq: No Exception");
            }
            else if (seqEx is not null)
            {
                throw new InvalidOperationException(
                    $"Span and Sequence readers diverged in exception:{Environment.NewLine}" +
                    $"Span: No Exception{Environment.NewLine}" +
                    $"Seq: {seqEx.GetType().Name} '{seqEx.Message}");
            }
            else
            {
                // both are null exceptions
                return false;
            }
        }

        if (spanEx.GetType() != seqEx.GetType())
        {
            throw new InvalidOperationException(
                    $"Span and Sequence readers diverged in exception type:{Environment.NewLine}" +
                    $"Span: {spanEx.GetType().Name} '{spanEx.Message}'{Environment.NewLine}" +
                    $"Seq: {seqEx.GetType().Name} '{seqEx.Message}'");
        }

        if (spanEx is JsonException spanJsonEx && seqEx is JsonException seqJsonEx)
        {
            if (//spanJsonEx.LineNumber != seqJsonEx.LineNumber ||
                //spanJsonEx.BytePositionInLine != seqJsonEx.BytePositionInLine ||
                spanJsonEx.Path != seqJsonEx.Path/* ||
                spanJsonEx.Message != seqJsonEx.Message*/)
            {
                throw new InvalidOperationException(
                    $"Span and Sequence readers diverged in exception:{Environment.NewLine}" +
                    $"Span: {spanEx.GetType().Name} '{spanEx.Message}'{Environment.NewLine}" +
                    $"Seq: {seqEx.GetType().Name} '{seqEx.Message}'");
            }
            // If both threw the same exception, consider the test passed for this input
            return true;
        }

        if (spanEx is DecoderFallbackException spanDecoderEx && seqEx is DecoderFallbackException seqDecoderEx)
        {
            if (spanDecoderEx.BytesUnknown.SequenceEqual(seqDecoderEx.BytesUnknown) != true)
            {
                throw new InvalidOperationException(
                    $"Span and Sequence readers diverged in decoder exception:{Environment.NewLine}" +
                    $"Span: {spanDecoderEx.GetType().Name ?? "none"} '{Convert.ToHexString(spanDecoderEx?.BytesUnknown ?? [])}'{Environment.NewLine}" +
                    $"Seq: {seqDecoderEx.GetType().Name ?? "none"} '{Convert.ToHexString(seqDecoderEx?.BytesUnknown ?? [])}'");
            }
            // If both threw the same decoder exception, consider the test passed for this input
            return true;
        }

        if (spanEx is InvalidOperationException spanInvalidEx && seqEx is InvalidOperationException seqInvalidEx)
        {
            if (spanInvalidEx.Message.Contains("surrogate") || seqInvalidEx.Message.Contains("surrogate"))
            {
                if (spanInvalidEx?.Message.Equals(seqInvalidEx?.Message) is not true)
                {
                    throw new InvalidOperationException(
                        $"Span and Sequence readers diverged in surrogate exception:{Environment.NewLine}" +
                        $"Span: '{spanInvalidEx?.Message ?? "null"}'{Environment.NewLine}" +
                        $"Seq: '{seqInvalidEx?.Message ?? "null"}'");
                }

                // If both threw the same exception, consider the test passed for this input
                return true;
            }
            else if (spanInvalidEx.Message.Contains("invalid UTF-8") || seqInvalidEx.Message.Contains("invalid UTF-8"))
            {
                if (spanInvalidEx?.Message.Equals(seqInvalidEx?.Message) is not true)
                {
                    throw new InvalidOperationException(
                        $"Span and Sequence readers diverged in invalid UTF-8 exception:{Environment.NewLine}" +
                        $"Span: '{spanInvalidEx?.Message ?? "null"}'{Environment.NewLine}" +
                        $"Seq: '{seqInvalidEx?.Message ?? "null"}'");
                }

                // If both threw the same exception, consider the test passed for this input
                return true;
            }
            else if (spanInvalidEx.Message.Contains("element cannot be an object or array") ||
                    seqInvalidEx.Message.Contains("element cannot be an object or array"))
            {
                if (spanInvalidEx?.Message.Equals(seqInvalidEx?.Message) is not true)
                {
                    throw new InvalidOperationException(
                        $"Span and Sequence readers diverged in element object/array exception:{Environment.NewLine}" +
                        $"Span: '{spanInvalidEx?.Message ?? "null"}'{Environment.NewLine}" +
                        $"Seq: '{seqInvalidEx?.Message ?? "null"}'");
                }

                // If both threw the same exception, consider the test passed for this input
                return true;
            }
        }

        if (spanEx is ArgumentOutOfRangeException spanRangeEx && seqEx is ArgumentOutOfRangeException seqRangeEx)
        {
            // e.g. 42.1e240071992547409914
            if (spanRangeEx.Message.Contains("exponent") || seqRangeEx.Message.Contains("exponent"))
            {
                if (spanRangeEx?.Message.Equals(seqRangeEx?.Message) is not true)
                {
                    throw new InvalidOperationException(
                        $"Span and Sequence readers diverged in exponent exception:{Environment.NewLine}" +
                        $"Span: '{spanRangeEx?.Message ?? "null"}'{Environment.NewLine}" +
                        $"Seq: '{seqRangeEx?.Message ?? "null"}'");
                }

                // If both threw the same exception, consider the test passed for this input
                return true;
            }
        }

        if (spanEx is ArgumentException spanArgumentEx && seqEx is ArgumentException seqArgumentEx)
        {
            // e.g. {"key":"value", "key":"value"}
            if (spanArgumentEx.Message.Contains("same key") || seqArgumentEx.Message.Contains("same key"))
            {
                if (spanArgumentEx?.Message.Equals(seqArgumentEx?.Message) is not true)
                {
                    throw new InvalidOperationException(
                        $"Span and Sequence readers diverged in exponent exception:{Environment.NewLine}" +
                        $"Span: '{spanArgumentEx?.Message ?? "null"}'{Environment.NewLine}" +
                        $"Seq: '{seqArgumentEx?.Message ?? "null"}'");
                }

                // If both threw the same exception, consider the test passed for this input
                return true;
            }
        }

        throw new InvalidOperationException($"Unexpected error:{Environment.NewLine}" +
            $"Span: '{spanEx?.GetType()}' '{spanEx?.Message ?? "null"}'{Environment.NewLine}" +
            $"Seq: '{seqEx?.GetType()}' '{seqEx?.Message ?? "null"}'");
    }
}

public enum IntEnum
{
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StringEnum
{
}
