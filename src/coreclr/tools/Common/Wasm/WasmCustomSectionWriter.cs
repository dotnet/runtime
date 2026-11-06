// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    /// Appends fresh <c>producers</c> and/or <c>build_id</c> custom sections to <paramref name="output"/>
    /// at the current position. The caller is responsible for ensuring these are written after the
    /// module's <c>name</c> section (as the tool-conventions spec requires for <c>producers</c>). This is
    /// the low-level primitive used by producers that build their own module bytes (e.g. the webcil
    /// wasm wrapper and the ReadyToRun wasm object writer).
    /// </summary>
    /// <param name="output">Stream positioned at the end of the module body.</param>
    /// <param name="producers">Producers values to write. May be <see langword="null"/>/empty to skip.</param>
    /// <param name="buildId">Raw build id bytes. May be <see langword="null"/>/empty to skip.</param>
    public static void AppendMetadataSections(Stream output, IEnumerable<ProducerValue>? producers, byte[]? buildId)
    {
        if (output is null)
            ThrowArgumentNull(nameof(output));

        if (producers is not null)
        {
            var list = new List<ProducerValue>(producers);
            if (list.Count > 0)
                WriteProducersSection(output, list);
        }

        if (buildId is not null && buildId.Length > 0)
            WriteBuildIdSection(output, buildId);
    }

    /// <summary>
    /// Replaces any existing <c>build_id</c> custom section in <paramref name="moduleBytes"/>.
    /// </summary>
    public static byte[] ReplaceBuildId(byte[] moduleBytes, byte[] buildId)
    {
        if (moduleBytes is null)
            ThrowArgumentNull(nameof(moduleBytes));
        if (buildId is null)
            ThrowArgumentNull(nameof(buildId));
        if (buildId.Length == 0)
            throw new ArgumentException("Build ID cannot be empty.", nameof(buildId));

        if (moduleBytes.Length < 8 || BinaryPrimitives.ReadUInt32LittleEndian(moduleBytes.AsSpan(0, 4)) != WasmMagic)
            throw new InvalidDataException("Not a WebAssembly module (bad magic).");

        uint version = BinaryPrimitives.ReadUInt32LittleEndian(moduleBytes.AsSpan(4, 4));
        if (version != 1)
            throw new InvalidDataException($"Unsupported WebAssembly module version {version}.");

        const int bodyStart = 8; // magic (4) + version (4)

        using var output = new MemoryStream(moduleBytes.Length + buildId.Length + 32);
        output.Write(moduleBytes, 0, bodyStart);

        int pos = bodyStart;
        while (pos < moduleBytes.Length)
        {
            int recordStart = pos;
            byte id = moduleBytes[pos++];
            uint size = ReadULEB128(moduleBytes, ref pos, moduleBytes.Length);
            if (size > int.MaxValue)
                throw new InvalidDataException("WebAssembly section is too large.");

            int payloadStart = pos;
            int payloadEnd = checked(payloadStart + (int)size);
            if (payloadEnd > moduleBytes.Length)
                throw new InvalidDataException("Truncated WebAssembly section.");

            bool isBuildId = false;
            if (id == CustomSectionId)
            {
                int p = payloadStart;
                isBuildId = ReadName(moduleBytes, ref p, payloadEnd) == BuildIdSectionName;
            }

            if (!isBuildId)
                output.Write(moduleBytes, recordStart, payloadEnd - recordStart);

            pos = payloadEnd;
        }

        WriteBuildIdSection(output, buildId);
        return output.ToArray();
    }

    public static byte[]? ParseBuildId(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        string hex = value!.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        if (hex.Length == 0 || (hex.Length % 2) != 0)
            throw InvalidBuildId(value);

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
#if NETFRAMEWORK
            bool parsed = byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]);
#else
            bool parsed = byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]);
#endif
            if (!parsed)
                throw InvalidBuildId(value);
        }

        return bytes;
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

    private static ArgumentException InvalidBuildId(string value)
        => new($"WasmNativeBuildId '{value}' is not a valid hex string.");

    private static string ReadName(byte[] bytes, ref int pos, int end)
    {
        uint len = ReadULEB128(bytes, ref pos, end);
        if (len > int.MaxValue || len > end - pos)
            throw new InvalidDataException("Truncated WebAssembly name.");

        string s = Encoding.UTF8.GetString(bytes, pos, checked((int)len));
        pos += checked((int)len);
        return s;
    }

    private static void WriteName(Stream output, string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        WriteULEB128(output, (uint)utf8.Length);
        output.Write(utf8, 0, utf8.Length);
    }

    private static uint ReadULEB128(byte[] bytes, ref int pos, int end)
    {
        uint val = 0;
        int shift = 0;
        while (true)
        {
            if (pos >= end)
                throw new InvalidDataException("Truncated ULEB128 value.");

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
