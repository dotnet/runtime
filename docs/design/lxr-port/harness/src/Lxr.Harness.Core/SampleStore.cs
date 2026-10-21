// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Lxr.Harness.Core;

/// <summary>
/// Persists every operation's raw timing, not just a summary.
///
/// <para>A summary cannot be re-analysed. If a percentile choice, a warmup boundary or an outlier
/// rule turns out to be wrong, only the raw samples can answer the new question; a stored p99 cannot.
/// The format is deliberately trivial and self-describing, and uses GZip from the shared framework so
/// the harness keeps its zero-package-reference property and still runs on <c>corerun</c>.</para>
///
/// <para>Record layout, little-endian, one per operation: <c>intendedTicks</c> (int64),
/// <c>serviceStartTicks</c> (int64), <c>endTicks</c> (int64), <c>value</c> (int64), <c>phase</c>
/// (int32). Timestamps are raw <see cref="Stopwatch"/> ticks relative to the run start, and the
/// header carries <see cref="Stopwatch.Frequency"/> so they can be converted without guessing.</para>
/// </summary>
public static class SampleStore
{
    private const uint Magic = 0x4C58_5253; // "LXRS"
    private const int FormatVersion = 2;

    public static void WriteOpenLoop(string path, MeasuredRun run, long runStartTimestamp)
    {
        ArgumentNullException.ThrowIfNull(run);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        using FileStream file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gzip);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(Stopwatch.Frequency);
        writer.Write(runStartTimestamp);
        writer.Write(run.RecordCount);

        NativeBuffer<OperationRecord> records = run.Records;
        for (int i = 0; i < run.RecordCount; i++)
        {
            writer.Write(records[i].IntendedTimestamp - runStartTimestamp);
            writer.Write(records[i].ServiceStartTimestamp - runStartTimestamp);
            writer.Write(records[i].DispatchTimestamp - runStartTimestamp);
            writer.Write(records[i].EndTimestamp - runStartTimestamp);
            writer.Write(records[i].Value);
            writer.Write(records[i].Phase);
        }
    }

    public static void WriteQuantumRates(string path, double[] rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        using FileStream file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gzip);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write((long)0);
        writer.Write((long)0);
        writer.Write(rates.Length);
        foreach (double rate in rates)
        {
            writer.Write(rate);
        }
    }

    /// <summary>Reads back open-loop samples, so the retention claim is testable rather than assumed.</summary>
    public static (long Frequency, OperationRecord[] Records) ReadOpenLoop(string path)
    {
        using FileStream file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);

        uint magic = reader.ReadUInt32();
        if (magic != Magic)
        {
            throw new InvalidDataException($"'{path}' is not an LXR harness sample file.");
        }

        int version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"'{path}' has sample format version {version}, expected {FormatVersion}.");
        }

        long frequency = reader.ReadInt64();
        _ = reader.ReadInt64();
        int count = reader.ReadInt32();

        OperationRecord[] records = new OperationRecord[count];
        for (int i = 0; i < count; i++)
        {
            records[i].IntendedTimestamp = reader.ReadInt64();
            records[i].ServiceStartTimestamp = reader.ReadInt64();
            records[i].DispatchTimestamp = reader.ReadInt64();
            records[i].EndTimestamp = reader.ReadInt64();
            records[i].Value = reader.ReadInt64();
            records[i].Phase = reader.ReadInt32();
        }

        return (frequency, records);
    }
}
