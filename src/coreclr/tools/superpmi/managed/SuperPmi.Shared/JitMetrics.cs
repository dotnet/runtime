// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SuperPmi;

// The metrics that the JIT reports through reportMetadata, as listed by src/coreclr/jit/jitmetadatalist.h
// (embedded as a resource, so new metrics don't need changes here).
public sealed unsafe class JitMetrics
{
    public enum MetricType
    {
        Int,
        Int64,
        Double,
    }

    public readonly record struct Metric(string Name, byte[] Utf8Name, MetricType Type);

    public static readonly Metric[] All = Load();

    // Doubles are stored as their bits.
    private readonly long[] _values = new long[All.Length];

    private static Metric[] Load()
    {
        using Stream stream = typeof(JitMetrics).Assembly.GetManifestResourceStream("jitmetadatalist.h");
        using var reader = new StreamReader(stream);

        const string Prefix = "JITMETADATAMETRIC(";
        var metrics = new List<Metric>();
        while (reader.ReadLine() is string line)
        {
            if (!line.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            string[] parts = line.Substring(Prefix.Length).Split(',');
            string name = parts[0].Trim();
            MetricType type = parts[1].Trim() switch
            {
                "int" => MetricType.Int,
                "int64_t" => MetricType.Int64,
                "double" => MetricType.Double,
                string other => throw new InvalidDataException($"Unknown JIT metric type '{other}' for {name}"),
            };
            metrics.Add(new Metric(name, Encoding.UTF8.GetBytes(name), type));
        }
        return metrics.ToArray();
    }

    // The JIT reports metrics in the order they're listed, so the search resumes where the previous one
    // matched; in practice this matches on the first comparison.
    public static int FindIndex(ReadOnlySpan<byte> name, ref int searchStart)
    {
        int index = searchStart;
        for (int i = 0; i < All.Length; i++)
        {
            int candidate = index;
            index = index + 1 == All.Length ? 0 : index + 1;
            if (name.SequenceEqual(All[candidate].Utf8Name))
            {
                searchStart = index;
                return candidate;
            }
        }
        return -1;
    }

    // Stores the value if it has the metric's size.
    public void Store(int index, void* value, nuint length)
    {
        switch (All[index].Type)
        {
            case MetricType.Int when length == sizeof(int):
                _values[index] = *(int*)value;
                break;
            case MetricType.Int64 when length == sizeof(long):
            case MetricType.Double when length == sizeof(double):
                _values[index] = *(long*)value;
                break;
        }
    }

    public double GetDouble(int index) => BitConverter.Int64BitsToDouble(_values[index]);

    public long GetInteger(int index) => _values[index];

    // Formats the value as printf's "%d", "%lld", or "%f", as the details CSV has it.
    public string Format(int index) => All[index].Type switch
    {
        MetricType.Double => FormatDouble(GetDouble(index)),
        _ => _values[index].ToString(CultureInfo.InvariantCulture),
    };

    public static string FormatDouble(double value)
    {
        if (double.IsNaN(value))
            return double.IsNegative(value) ? "-nan" : "nan";
        if (double.IsInfinity(value))
            return value < 0 ? "-inf" : "inf";
        return value.ToString("F6", CultureInfo.InvariantCulture);
    }
}
