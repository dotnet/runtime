// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace HelixCache;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using CancellationTokenSource cancellation = new();
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            if (args is ["--help" or "-h"] or ["lookup" or "store" or "prepare" or "collect", "--help" or "-h"])
            {
                Console.WriteLine(CommandLine.Usage);
                return 0;
            }
            Command command = CommandLine.Parse(args);
            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException("This prototype requires a Linux host.");
            }
            await ExecuteAsync(command, cancellation.Token);
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.Error.WriteLine("helix-cache: Cancelled.");
            return 130;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or
            HttpRequestException or TaskCanceledException or XmlException or ArgumentException or
            OverflowException or PlatformNotSupportedException or TimeoutException)
        {
            Console.Error.WriteLine($"helix-cache: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    private static async Task ExecuteAsync(Command command, CancellationToken cancellationToken)
    {
        Cache cache = new(command.CacheDirectory);
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };
        HelixClient helix = new(http);
        switch (command)
        {
            case LookupCommand lookup:
            {
                string manifest = Path.GetFullPath(lookup.Manifest);
                using FileStream input = File.OpenRead(manifest);
                Submission submission = await JsonSerializer.DeserializeAsync(input, CacheJsonContext.Default.Submission, cancellationToken)
                    ?? throw new InvalidDataException($"Missing submission: {manifest}.");
                submission.Validate();
                LinuxEnvironment environment = await LinuxEnvironment.LoadAsync(lookup.QueueInfo, helix, cancellationToken);
                LookupResult result = cache.Lookup(submission, Path.GetDirectoryName(manifest)!, environment);
                WriteLookup(lookup.Output, result);
                break;
            }
            case StoreCommand store:
            {
                using FileStream input = File.OpenRead(store.Manifest);
                ExecutionRecord execution = await JsonSerializer.DeserializeAsync(input, CacheJsonContext.Default.ExecutionRecord, cancellationToken)
                    ?? throw new InvalidDataException($"Missing execution record: {store.Manifest}.");
                cache.Store(execution, store.Results);
                Console.Error.WriteLine($"Stored {execution.Key}.");
                break;
            }
            case PrepareCommand prepare:
            {
                LinuxEnvironment environment = await LinuxEnvironment.LoadAsync(prepare.QueueInfo, helix, cancellationToken);
                LookupEntry entry = ShadowValidation.Prepare(cache, prepare.Output, prepare.Payload, prepare.Runtime, prepare.Queue, environment);
                Console.WriteLine($"Helix cache shadow: {entry.Status.ToString().ToLowerInvariant()} {entry.Execution.Key}; execution will NOT be skipped.");
                break;
            }
            case CollectCommand collect:
            {
                ShadowSummary summary = await ShadowValidation.CollectAsync(cache, collect.Output, collect.Wait, helix, cancellationToken);
                Console.WriteLine(JsonSerializer.Serialize(summary, CacheJsonContext.Default.ShadowSummary));
                break;
            }
        }
    }

    private static void WriteLookup(string path, LookupResult result)
    {
        string output = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (FileStream file = File.Create(temporary))
            {
                JsonSerializer.Serialize(file, result, CacheJsonContext.Default.LookupResult);
                file.Flush(flushToDisk: true);
            }
            File.Move(temporary, output, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
