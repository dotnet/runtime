// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
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

    // The well-known producers field names emitted for Webcil modules.
    public const string ProducersFieldProcessedBy = "processed-by";
    public const string ProducersFieldSdk = "sdk";

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

        /// <summary>One of <see cref="ProducersFieldProcessedBy"/> or <see cref="ProducersFieldSdk"/>.</summary>
        public string Field { get; }
        public string Name { get; }
        public string Version { get; }
    }

    /// <summary>
    /// Appends a fresh <c>producers</c> custom section to <paramref name="output"/> at the current
    /// position. Each supplied value represents one field with one name/version pair.
    /// </summary>
    public static void WriteProducersSection(Stream output, ProducerValue[] producers)
    {
        if (output is null)
            ThrowArgumentNull(nameof(output));
        if (producers is null)
            ThrowArgumentNull(nameof(producers));
        if (producers.Length == 0)
            throw new ArgumentException("At least one producer is required.", nameof(producers));

        using var payload = new MemoryStream();
        WriteName(payload, ProducersSectionName);
        WriteULEB128(payload, (uint)producers.Length);
        foreach (ProducerValue producer in producers)
        {
            WriteName(payload, producer.Field);
            WriteULEB128(payload, 1);
            WriteName(payload, producer.Name);
            WriteName(payload, producer.Version);
        }

        WriteCustomSection(output, payload.ToArray());
    }

    public static void WriteBuildIdSection(Stream output, byte[] buildId)
    {
        if (output is null)
            ThrowArgumentNull(nameof(output));
        if (buildId is null)
            ThrowArgumentNull(nameof(buildId));
        if (buildId.Length == 0)
            throw new ArgumentException("Build ID cannot be empty.", nameof(buildId));

        using var payload = new MemoryStream();
        WriteName(payload, BuildIdSectionName);
        // build_id payload: length-prefixed sequence of raw bytes.
        WriteULEB128(payload, (uint)buildId.Length);
        payload.Write(buildId, 0, buildId.Length);

        WriteCustomSection(output, payload.ToArray());
    }

    public static byte[] RegenerateBuildId(byte[] moduleBytes)
    {
        if (moduleBytes is null)
            ThrowArgumentNull(nameof(moduleBytes));
        if (moduleBytes.Length < 8 || BinaryPrimitives.ReadUInt32LittleEndian(moduleBytes.AsSpan(0, 4)) != 0x6d736100u)
            throw new InvalidDataException("Not a WebAssembly module (bad magic).");
        if (BinaryPrimitives.ReadUInt32LittleEndian(moduleBytes.AsSpan(4, 4)) != 1)
            throw new InvalidDataException("Unsupported WebAssembly module version.");

        using var module = new MemoryStream(moduleBytes.Length);
        module.Write(moduleBytes, 0, 8);

        int position = 8;
        while (position < moduleBytes.Length)
        {
            int sectionStart = position;
            byte sectionId = moduleBytes[position++];
            uint size = ReadULEB128(moduleBytes, ref position, moduleBytes.Length);
            if (size > int.MaxValue)
                throw new InvalidDataException("WebAssembly section is too large.");
            int payloadEnd = checked(position + (int)size);
            if (payloadEnd > moduleBytes.Length)
                throw new InvalidDataException("Truncated WebAssembly section.");

            bool isBuildId = false;
            if (sectionId == CustomSectionId)
            {
                int namePosition = position;
                uint nameLength = ReadULEB128(moduleBytes, ref namePosition, payloadEnd);
                if (nameLength <= payloadEnd - namePosition)
                    isBuildId = Encoding.UTF8.GetString(moduleBytes, namePosition, (int)nameLength) == BuildIdSectionName;
            }

            if (!isBuildId)
                module.Write(moduleBytes, sectionStart, payloadEnd - sectionStart);

            position = payloadEnd;
        }

        byte[] content = module.ToArray();
        using SHA256 sha256 = SHA256.Create();
        WriteBuildIdSection(module, sha256.ComputeHash(content));
        return module.ToArray();
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

    private static void WriteName(Stream output, string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        WriteULEB128(output, (uint)utf8.Length);
        output.Write(utf8, 0, utf8.Length);
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

    private static uint ReadULEB128(byte[] bytes, ref int position, int end)
    {
        uint value = 0;
        int shift = 0;
        while (true)
        {
            if (position >= end)
                throw new InvalidDataException("Truncated ULEB128 value.");

            byte current = bytes[position++];
            value |= (current & 0x7fu) << shift;
            if ((current & 0x80u) == 0)
                return value;

            shift += 7;
            if (shift >= 35)
                throw new InvalidDataException("ULEB128 value is too large.");
        }
    }
}
