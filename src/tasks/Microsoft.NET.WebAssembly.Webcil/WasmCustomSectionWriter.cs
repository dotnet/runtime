// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// Writes the tool-conventions <c>producers</c> and <c>build_id</c> custom sections into a
/// WebAssembly module. Both are non-semantic custom sections and can be stripped at any time.
///
/// See:
///   - https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md
///   - https://github.com/WebAssembly/tool-conventions/blob/main/BuildId.md
/// </summary>
public static class WasmCustomSectionWriter
{
    public const string ProducersSectionName = "producers";
    public const string BuildIdSectionName = "build_id";

    // The three well-known producers field names.
    public const string ProducersFieldLanguage = "language";
    public const string ProducersFieldProcessedBy = "processed-by";
    public const string ProducersFieldSdk = "sdk";

    private const uint WasmMagic = 0x6d736100u; // "\0asm"
    private const byte CustomSectionId = 0;

    /// <summary>
    /// A single <c>(name, version)</c> value belonging to a producers field.
    /// </summary>
    public readonly struct ProducerValue
    {
        public ProducerValue(string field, string name, string version)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? string.Empty;
        }

        /// <summary>One of <see cref="ProducersFieldLanguage"/>, <see cref="ProducersFieldProcessedBy"/> or <see cref="ProducersFieldSdk"/>.</summary>
        public string Field { get; }
        public string Name { get; }
        public string Version { get; }
    }

    /// <summary>
    /// Rewrites <paramref name="wasmPath"/> in place, replacing any existing <c>producers</c> and
    /// <c>build_id</c> custom sections with fresh ones built from the supplied data. Existing
    /// producers entries emitted by other tools (clang/LLVM/Emscripten) are preserved and merged.
    /// </summary>
    /// <param name="wasmPath">Path to the WebAssembly module to modify.</param>
    /// <param name="producers">Producers values to add. May be <see langword="null"/> to leave the section untouched.</param>
    /// <param name="buildId">Raw build id bytes. May be <see langword="null"/> to leave the section untouched.</param>
    public static void WriteMetadata(string wasmPath, IEnumerable<ProducerValue>? producers, byte[]? buildId)
    {
        if (wasmPath is null)
            ThrowArgumentNull(nameof(wasmPath));

        byte[] input = File.ReadAllBytes(wasmPath);
        byte[] output = WriteMetadata(input, producers, buildId);
        File.WriteAllBytes(wasmPath, output);
    }

    /// <summary>
    /// Pure-in-memory variant of <see cref="WriteMetadata(string, IEnumerable{ProducerValue}, byte[])"/>.
    /// </summary>
    public static byte[] WriteMetadata(byte[] moduleBytes, IEnumerable<ProducerValue>? producers, byte[]? buildId)
    {
        if (moduleBytes is null)
            ThrowArgumentNull(nameof(moduleBytes));

        // Nothing to do.
        if (producers is null && buildId is null)
            return moduleBytes;

        if (moduleBytes.Length < 8 || BitConverter.ToUInt32(moduleBytes, 0) != WasmMagic)
            throw new InvalidDataException("Not a WebAssembly module (bad magic).");

        uint version = BitConverter.ToUInt32(moduleBytes, 4);
        if (version != 1)
            throw new InvalidDataException($"Unsupported WebAssembly module version {version}.");

        const int bodyStart = 8; // magic (4) + version (4)

        var existingProducers = new List<ProducerValue>();
        var records = new List<(byte id, int recordStart, int recordLength, string? customName)>();

        int pos = bodyStart;
        while (pos < moduleBytes.Length)
        {
            int recordStart = pos;
            byte id = moduleBytes[pos++];
            uint size = ReadULEB128(moduleBytes, ref pos);
            int payloadStart = pos;
            int payloadEnd = payloadStart + (int)size;
            if (payloadEnd > moduleBytes.Length)
                throw new InvalidDataException("Truncated WebAssembly section.");

            string? customName = null;
            if (id == CustomSectionId)
            {
                int p = payloadStart;
                customName = ReadName(moduleBytes, ref p);
                if (customName == ProducersSectionName)
                    ReadProducers(moduleBytes, p, payloadEnd, existingProducers);
            }

            records.Add((id, recordStart, payloadEnd - recordStart, customName));
            pos = payloadEnd;
        }

        // Merge new producers on top of the ones we found (dedupe by field + name, new value wins for version).
        List<ProducerValue>? mergedProducers = null;
        if (producers is not null)
        {
            mergedProducers = new List<ProducerValue>(existingProducers);
            foreach (ProducerValue value in producers)
            {
                int existingIndex = mergedProducers.FindIndex(p =>
                    string.Equals(p.Field, value.Field, StringComparison.Ordinal) &&
                    string.Equals(p.Name, value.Name, StringComparison.Ordinal));
                if (existingIndex >= 0)
                    mergedProducers[existingIndex] = value;
                else
                    mergedProducers.Add(value);
            }
        }

        using var ms = new MemoryStream(moduleBytes.Length + 256);
        ms.Write(moduleBytes, 0, bodyStart);

        foreach ((byte id, int recordStart, int recordLength, string? customName) in records)
        {
            // Drop the sections we are going to (re)write, so we never emit duplicates.
            if (mergedProducers is not null && customName == ProducersSectionName)
                continue;
            if (buildId is not null && customName == BuildIdSectionName)
                continue;

            // Re-emit the full section record (id byte + size + payload) verbatim.
            ms.Write(moduleBytes, recordStart, recordLength);
        }

        if (mergedProducers is not null)
            WriteProducersSection(ms, mergedProducers);

        if (buildId is not null)
            WriteBuildIdSection(ms, buildId);

        return ms.ToArray();
    }

    private static void ReadProducers(byte[] bytes, int pos, int end, List<ProducerValue> into)
    {
        if (pos >= end)
            return;
        uint fieldCount = ReadULEB128(bytes, ref pos);
        for (uint f = 0; f < fieldCount && pos < end; f++)
        {
            string field = ReadName(bytes, ref pos);
            uint valueCount = ReadULEB128(bytes, ref pos);
            for (uint v = 0; v < valueCount && pos < end; v++)
            {
                string name = ReadName(bytes, ref pos);
                string version = ReadName(bytes, ref pos);
                into.Add(new ProducerValue(field, name, version));
            }
        }
    }

    // ------------------------------------------------------------------
    // Section writing helpers
    // ------------------------------------------------------------------

    private static void WriteProducersSection(Stream output, List<ProducerValue> producers)
    {
        // Group values by field, preserving first-seen order.
        var order = new List<string>();
        var byField = new Dictionary<string, List<ProducerValue>>(StringComparer.Ordinal);
        foreach (ProducerValue value in producers)
        {
            if (!byField.TryGetValue(value.Field, out List<ProducerValue>? list))
            {
                list = new List<ProducerValue>();
                byField[value.Field] = list;
                order.Add(value.Field);
            }
            list.Add(value);
        }

        using var payload = new MemoryStream();
        WriteName(payload, ProducersSectionName);
        WriteULEB128(payload, (uint)order.Count);
        foreach (string field in order)
        {
            List<ProducerValue> values = byField[field];
            WriteName(payload, field);
            WriteULEB128(payload, (uint)values.Count);
            foreach (ProducerValue value in values)
            {
                WriteName(payload, value.Name);
                WriteName(payload, value.Version);
            }
        }

        WriteCustomSection(output, payload.ToArray());
    }

    private static void WriteBuildIdSection(Stream output, byte[] buildId)
    {
        using var payload = new MemoryStream();
        WriteName(payload, BuildIdSectionName);
        // build_id payload: length-prefixed sequence of raw bytes.
        WriteULEB128(payload, (uint)buildId.Length);
        payload.Write(buildId, 0, buildId.Length);

        WriteCustomSection(output, payload.ToArray());
    }

    private static void WriteCustomSection(Stream output, byte[] payload)
    {
        output.WriteByte(CustomSectionId);
        WriteULEB128(output, (uint)payload.Length);
        output.Write(payload, 0, payload.Length);
    }

    // ------------------------------------------------------------------
    // LEB128 / name primitives
    // ------------------------------------------------------------------

    [DoesNotReturn]
    private static void ThrowArgumentNull(string paramName) => throw new ArgumentNullException(paramName);

    private static string ReadName(byte[] bytes, ref int pos)
    {
        uint len = ReadULEB128(bytes, ref pos);
        string s = Encoding.UTF8.GetString(bytes, pos, (int)len);
        pos += (int)len;
        return s;
    }

    private static void WriteName(Stream output, string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        WriteULEB128(output, (uint)utf8.Length);
        output.Write(utf8, 0, utf8.Length);
    }

    private static uint ReadULEB128(byte[] bytes, ref int pos)
    {
        uint val = 0;
        int shift = 0;
        while (true)
        {
            byte b = bytes[pos++];
            val |= (b & 0x7fu) << shift;
            if ((b & 0x80u) == 0)
                break;
            shift += 7;
            if (shift >= 35)
                throw new OverflowException("ULEB128 value too large.");
        }
        return val;
    }

    private static void WriteULEB128(Stream output, uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7fu);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            output.WriteByte(b);
        }
        while (value != 0);
    }
}
