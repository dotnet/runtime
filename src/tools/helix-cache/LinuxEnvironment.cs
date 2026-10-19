// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HelixCache;

internal sealed class LinuxEnvironment : IDisposable
{
    private readonly JsonDocument _queues;
    private readonly Dictionary<string, JsonElement> _fingerprints = new(StringComparer.Ordinal);

    private LinuxEnvironment(JsonDocument queues) => _queues = queues;

    internal static async Task<LinuxEnvironment> LoadAsync(string? queueInfo)
    {
        if (queueInfo is not null)
        {
            using FileStream file = File.OpenRead(queueInfo);
            return new LinuxEnvironment(await JsonDocument.ParseAsync(file));
        }

        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(60) };
        using Stream stream = await client.GetStreamAsync("https://helix.dot.net/api/2019-06-17/info/queues");
        return new LinuxEnvironment(await JsonDocument.ParseAsync(stream));
    }

    internal JsonElement Resolve(string target)
    {
        if (_fingerprints.TryGetValue(target, out JsonElement fingerprint))
        {
            return fingerprint;
        }

        // Matches Arcade JobSender's (alias)host@container syntax; retain a digest's second '@'.
        // https://github.com/dotnet/arcade/blob/7103cc604358625941ee1d105e662f0d697085c6/src/Microsoft.DotNet.Helix/JobSender/JobDefinition.cs#L374-L399
        int separator = target.IndexOf('@');
        string host = separator < 0 ? target : target[..separator];
        string? container = separator < 0 ? null : target[(separator + 1)..];
        if (host.StartsWith('('))
        {
            int close = host.IndexOf(')');
            JsonFile.Require(separator >= 0 && close > 1 && close < host.Length - 1, nameof(target));
            host = host[(close + 1)..];
        }

        if (container is not null)
        {
            int digestStart = container.LastIndexOf("@sha256:", StringComparison.Ordinal);
            if (digestStart <= 0 || !Cache.IsDigest(container[(digestStart + "@sha256:".Length)..]))
            {
                throw new InvalidDataException(SR.Get("Container", container));
            }
        }

        JsonElement? queue = null;
        foreach (JsonElement candidate in _queues.RootElement.EnumerateArray())
        {
            if (string.Equals(candidate.GetProperty("QueueId").GetString(), host, StringComparison.OrdinalIgnoreCase))
            {
                JsonFile.Require(queue is null, nameof(target));
                queue = candidate;
            }
        }

        if (queue is not JsonElement info ||
            !string.Equals(info.GetProperty("OperatingSystemGroup").GetString(), "linux", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(info.GetProperty("Architecture").GetString(), "AMD64", StringComparison.OrdinalIgnoreCase) ||
            !info.GetProperty("IsAvailable").GetBoolean())
        {
            throw new InvalidDataException(SR.Get("Queue", host));
        }

        using MemoryStream buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            foreach (string property in new[] { "QueueId", "Architecture", "OperatingSystemGroup", "GalleryImage", "PreInstalledImage", "PreparedImage", "Artifacts", "WorkspacePath" })
            {
                if (info.TryGetProperty(property, out JsonElement value))
                {
                    writer.WritePropertyName(property);
                    JsonFile.WriteCanonical(writer, value);
                }
            }
            writer.WriteString("Container", container);
            writer.WriteEndObject();
        }
        using JsonDocument document = JsonDocument.Parse(buffer.ToArray());
        fingerprint = document.RootElement.Clone();
        _fingerprints.Add(target, fingerprint);
        return fingerprint;
    }

    public void Dispose() => _queues.Dispose();
}
