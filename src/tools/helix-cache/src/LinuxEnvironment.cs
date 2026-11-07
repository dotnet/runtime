// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HelixCache;

internal sealed class LinuxEnvironment
{
    private readonly List<HelixQueue> _queues;
    private readonly Dictionary<string, EnvironmentFingerprint> _fingerprints = new(StringComparer.Ordinal);

    internal LinuxEnvironment(List<HelixQueue> queues) => _queues = queues;

    internal static async Task<LinuxEnvironment> LoadAsync(string? queueInfo, HelixClient client, CancellationToken cancellationToken)
    {
        if (queueInfo is not null)
        {
            using FileStream file = File.OpenRead(queueInfo);
            return new LinuxEnvironment(await JsonSerializer.DeserializeAsync(file, HelixJsonContext.Default.ListHelixQueue, cancellationToken)
                ?? throw new InvalidDataException($"Invalid or missing queue metadata: {queueInfo}."));
        }

        return new LinuxEnvironment(await client.GetQueuesAsync(cancellationToken));
    }

    internal EnvironmentFingerprint Resolve(string target)
    {
        if (_fingerprints.TryGetValue(target, out EnvironmentFingerprint? fingerprint))
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
            if (separator < 0 || close <= 1 || close >= host.Length - 1)
            {
                throw new InvalidDataException($"Invalid target queue: {target}.");
            }
            host = host[(close + 1)..];
        }

        if (container is not null)
        {
            int digestStart = container.LastIndexOf("@sha256:", StringComparison.Ordinal);
            if (digestStart <= 0 || !Cache.IsDigest(container[(digestStart + "@sha256:".Length)..]))
            {
                throw new InvalidDataException($"Container reference '{container}' must end in an explicit @sha256: digest.");
            }
        }

        HelixQueue? queue = null;
        foreach (HelixQueue candidate in _queues)
        {
            if (candidate is null)
            {
                throw new InvalidDataException("Helix queue metadata must not contain null.");
            }
            if (string.Equals(candidate.QueueId, host, StringComparison.OrdinalIgnoreCase))
            {
                if (queue is not null)
                {
                    throw new InvalidDataException($"Duplicate target queue: {target}.");
                }
                queue = candidate;
            }
        }

        if (queue is not { Architecture: string architecture, OperatingSystemGroup: string operatingSystem } ||
            !string.Equals(queue.OperatingSystemGroup, "linux", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(queue.Architecture, "AMD64", StringComparison.OrdinalIgnoreCase) ||
            !queue.IsAvailable)
        {
            throw new InvalidDataException($"Queue '{host}' is missing, unavailable, or not a Linux AMD64 queue.");
        }

        fingerprint = new EnvironmentFingerprint
        {
            QueueId = queue.QueueId,
            Architecture = architecture,
            OperatingSystemGroup = operatingSystem,
            Container = container,
            Provisioning = queue.Metadata.Where(p => p.Key is
                "GalleryImage" or "PreInstalledImage" or "PreparedImage" or "Artifacts" or "WorkspacePath")
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
        };
        _fingerprints.Add(target, fingerprint);
        return fingerprint;
    }
}
