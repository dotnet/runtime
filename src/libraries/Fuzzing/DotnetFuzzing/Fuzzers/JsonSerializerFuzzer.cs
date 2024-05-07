// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Json;

namespace DotnetFuzzing.Fuzzers;

// Based on https://github.com/Metalnem/dotnet-fuzzers/blob/main/src/JsonSerializerFuzzer/Program.cs
internal sealed class JsonSerializerFuzzer : IFuzzer
{
    public string BlameAlias => "mizupan";
    public string[] TargetAssemblies => ["System.Text.Json"];
    public string[] TargetCoreLibPrefixes => [];
    public string Dictionary => "json.dict";

    private static readonly ArrayBufferWriter<byte> s_writeBuffer = new();
    private static readonly Utf8JsonWriter s_writer = new(s_writeBuffer);

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        using var poisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);

        Item? item = null;
        try
        {
            item = JsonSerializer.Deserialize<Item>(poisonAfter.Span);
        }
        catch (JsonException) { }

        if (item is not null)
        {
            // Test that we can roundtrip the serialization.
            s_writeBuffer.ResetWrittenCount();
            s_writer.Reset();

            JsonSerializer.Serialize(s_writer, item);

            Item? item2 = JsonSerializer.Deserialize<Item>(s_writeBuffer.WrittenSpan);
            item.AssertEqual(item2);
        }
    }

    private sealed class Item
    {
        public byte A { get; set; }
        public int B { get; set; }
        public double C { get; set; }
        public DateTime D { get; set; }
        public string? E { get; set; }
        public short[]? F { get; set; }
        public Item? G { get; set; }
        public Dictionary<string, int>? H { get; set; }
        public List<char>? I { get; set; }

        public void AssertEqual(Item? other)
        {
            ArgumentNullException.ThrowIfNull(other);

            Assert.Equal(A, other.A);
            Assert.Equal(B, other.B);
            Assert.Equal(C, other.C);
            Assert.Equal(D, other.D);
            Assert.Equal(E, other.E);
            Assert.SequenceEqual<short>(F.AsSpan(), other.F.AsSpan());
            Assert.Equal(G is null, other.G is null);
            G?.AssertEqual(other.G);
            Assert.SequenceEqual(H, other.H);
            Assert.SequenceEqual(I, other.I);
        }
    }
}
