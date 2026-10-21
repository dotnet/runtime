// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace HelixCache;

internal abstract record Command(string CacheDirectory);
internal sealed record LookupCommand(string Manifest, string CacheDirectory, string Output, string? QueueInfo) : Command(CacheDirectory);
internal sealed record StoreCommand(string Manifest, string CacheDirectory, string Results) : Command(CacheDirectory);
internal sealed record PrepareCommand(string CacheDirectory, string Output, string Payload, string Runtime, string Queue, string? QueueInfo) : Command(CacheDirectory);
internal sealed record CollectCommand(string CacheDirectory, string Output, TimeSpan Wait) : Command(CacheDirectory);

internal static class CommandLine
{
    internal const string Usage = """
        Usage: helix-cache lookup submission.json --cache DIRECTORY --output lookup.json [--queue-info queues.json]
               helix-cache store execution.json --results DIRECTORY --cache DIRECTORY
               helix-cache prepare --cache DIRECTORY --output DIRECTORY --payload tests.zip --runtime runtime.zip --queue QUEUE [--queue-info queues.json]
               helix-cache collect --cache DIRECTORY --output DIRECTORY [--wait-seconds SECONDS]
        Only Linux x64 queues and digest-pinned container references are supported.
        """;

    internal static Command Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("lookup" or "store" or "prepare" or "collect"))
        {
            throw new InvalidDataException(Usage);
        }
        bool hasManifest = args[0] is "lookup" or "store";
        int firstOption = hasManifest ? 2 : 1;
        if (args.Length < firstOption ||
            (hasManifest && (string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith("--", StringComparison.Ordinal))))
        {
            throw new InvalidDataException($"Missing manifest for '{args[0]}'.\n{Usage}");
        }
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int i = firstOption; i < args.Length; i += 2)
        {
            string name = args[i];
            if (!name.StartsWith("--", StringComparison.Ordinal) || i + 1 == args.Length ||
                string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Expected an option and value at '{name}'.\n{Usage}");
            }
            if (!options.TryAdd(name, args[i + 1]))
            {
                throw new InvalidDataException($"Duplicate option '{name}'.");
            }
        }

        string cache = Required("--cache");
        Command command = args[0] switch
        {
            "lookup" => new LookupCommand(args[1], cache, Required("--output"), Optional("--queue-info")),
            "store" => new StoreCommand(args[1], cache, Required("--results")),
            "prepare" => new PrepareCommand(cache, Required("--output"), Required("--payload"),
                Required("--runtime"), Required("--queue"), Optional("--queue-info")),
            "collect" => new CollectCommand(cache, Required("--output"), ParseWait(Optional("--wait-seconds"))),
            _ => throw new UnreachableException(),
        };
        if (options.Count > 0)
        {
            throw new InvalidDataException($"Unknown option '{string.Join("', '", options.Keys)}'.");
        }
        return command;

        string Required(string name) => Optional(name) ?? throw new InvalidDataException($"Missing required option '{name}'.\n{Usage}");
        string? Optional(string name) => options.Remove(name, out string? value) ? value : null;
    }

    private static TimeSpan ParseWait(string? value)
    {
        if (value is null)
        {
            return TimeSpan.FromHours(1);
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) ||
            seconds <= 0 || seconds > (uint.MaxValue - 1) / 1000)
        {
            throw new InvalidDataException("--wait-seconds must be a positive number of seconds supported by a cancellation timer.");
        }
        return TimeSpan.FromSeconds(seconds);
    }
}
