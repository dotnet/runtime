// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Json;

namespace DotnetFuzzing.Fuzzers;

internal sealed class JsonDocumentFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Text.Json"];
    public string[] TargetCoreLibPrefixes => [];
    public string Dictionary => "json.dict";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        // The first byte is used to select various options.
        // The rest of the input is used as the UTF-8 JSON payload.
        byte optionsByte = bytes[0];
        bytes = bytes.Slice(1);

        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = (optionsByte & 1) != 0,
            CommentHandling = (optionsByte & 2) != 0 ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
        };

        using var poisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);

        try
        {
            JsonDocument.Parse(poisonAfter.Memory, options);
        }
        catch (JsonException) { }
    }
}
