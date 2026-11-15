// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

internal static class Program
{
    private const string ExpectedJson = """{"Id":42,"Name":"hello"}""";

    private static int Main()
    {
        RoundTrip(new ClassModel { Id = 42, Name = "hello" });
        RoundTripStruct<byte>();
        RoundTripStruct<sbyte>();
        RoundTripStruct<short>();
        RoundTripStruct<ushort>();
        RoundTripStruct<int>();
        RoundTripStruct<uint>();
        RoundTripStruct<long>();
        RoundTripStruct<ulong>();
        RoundTripStruct<nint>();
        RoundTripStruct<nuint>();
        RoundTripStruct<bool>();
        RoundTripStruct<char>();
        RoundTripStruct<float>();
        RoundTripStruct<double>();
        RoundTripStruct<decimal>();
        RoundTripStruct<Guid>();

#if CHECK_SIZE
        const long MaxExecutableSize = 2700 * 1024;
        long size = new FileInfo(Environment.ProcessPath!).Length;
        Console.WriteLine($"Executable size: {size} bytes (limit: {MaxExecutableSize} bytes).");
        if (size > MaxExecutableSize)
        {
            Console.WriteLine("Source-generated JSON serialization exceeded its executable size budget.");
            return 1;
        }
#endif

        return 100;
    }

    private static void RoundTripStruct<T>()
    {
        RoundTrip(new StructModel<T> { Id = 42, Name = "hello" });
    }

    private static void RoundTrip<T>(T value)
    {
        if (SmokeContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> typeInfo)
        {
            throw new Exception($"Missing generated metadata for {typeof(T)}.");
        }

        if (JsonSerializer.Serialize(value, typeInfo) != ExpectedJson)
        {
            throw new Exception($"Unexpected serialized JSON for {typeof(T)}.");
        }

        T? deserialized = JsonSerializer.Deserialize(ExpectedJson, typeInfo);
        if (deserialized is null || JsonSerializer.Serialize(deserialized, typeInfo) != ExpectedJson)
        {
            throw new Exception($"Unexpected deserialized value for {typeof(T)}.");
        }
    }
}

internal sealed class ClassModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// Distinct value-type instantiations amplify the cost of retaining unused generic converters.
internal struct StructModel<T>
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

[JsonSerializable(typeof(ClassModel))]
[JsonSerializable(typeof(StructModel<byte>))]
[JsonSerializable(typeof(StructModel<sbyte>))]
[JsonSerializable(typeof(StructModel<short>))]
[JsonSerializable(typeof(StructModel<ushort>))]
[JsonSerializable(typeof(StructModel<int>))]
[JsonSerializable(typeof(StructModel<uint>))]
[JsonSerializable(typeof(StructModel<long>))]
[JsonSerializable(typeof(StructModel<ulong>))]
[JsonSerializable(typeof(StructModel<nint>))]
[JsonSerializable(typeof(StructModel<nuint>))]
[JsonSerializable(typeof(StructModel<bool>))]
[JsonSerializable(typeof(StructModel<char>))]
[JsonSerializable(typeof(StructModel<float>))]
[JsonSerializable(typeof(StructModel<double>))]
[JsonSerializable(typeof(StructModel<decimal>))]
[JsonSerializable(typeof(StructModel<Guid>))]
internal partial class SmokeContext : JsonSerializerContext;
