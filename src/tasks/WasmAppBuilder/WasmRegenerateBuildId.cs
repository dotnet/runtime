// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>Assigns an optimized Wasm module and its optional source map a content-based build ID.</summary>
public sealed class WasmRegenerateBuildId : Task
{
    [Required]
    public string WasmPath { get; set; } = string.Empty;

    [Required]
    public string WorkingDirectory { get; set; } = string.Empty;

    public string WasmOptArguments { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            byte[] buildId = RegenerateBuildId(WasmPath);
            if (GetSourceMapPath(WasmOptArguments) is string sourceMapPath)
            {
                string path = Path.Combine(WorkingDirectory, sourceMapPath);
                if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject sourceMap)
                    throw new InvalidDataException($"The Wasm source map '{path}' is not a JSON object.");

                sourceMap["debugId"] = BitConverter.ToString(buildId).Replace("-", "").ToLowerInvariant();
                File.WriteAllText(path, sourceMap.ToJsonString());
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            Log.LogError($"Could not regenerate the build ID for '{WasmPath}': {ex.Message}");
            return false;
        }
    }

    private static byte[] RegenerateBuildId(string path)
    {
        byte[] module = File.ReadAllBytes(path);
        ReadOnlySpan<byte> header = "\0asm\u0001\0\0\0"u8;
        byte[] sectionName = "build_id"u8.ToArray();
        if (!module.AsSpan().StartsWith(header))
            throw new InvalidDataException("Expected a WebAssembly version 1 module.");

        int buildIdStart = module.Length;
        int buildIdEnd = module.Length;
        int position = header.Length;
        while (position < module.Length)
        {
            int sectionStart = position;
            byte sectionId = module[position++];
            uint size = ReadULEB128(module, ref position, module.Length);
            if (size > module.Length - position)
                throw new InvalidDataException("Truncated WebAssembly section.");

            int sectionEnd = position + (int)size;
            if (sectionId == 0)
            {
                uint nameLength = ReadULEB128(module, ref position, sectionEnd);
                if (nameLength > sectionEnd - position)
                    throw new InvalidDataException("Truncated WebAssembly custom section name.");

                if (module.AsSpan(position, (int)nameLength).SequenceEqual(sectionName))
                {
                    if (buildIdStart != module.Length)
                        throw new InvalidDataException("Expected at most one WebAssembly build_id section.");

                    buildIdStart = sectionStart;
                    buildIdEnd = sectionEnd;
                }
            }
            else if (buildIdStart != module.Length)
            {
                // Binaryen emits build_id after the standard sections. Removing it must not shift code or data.
                throw new InvalidDataException("Expected the WebAssembly build_id after the standard sections.");
            }

            position = sectionEnd;
        }

        int trailingLength = module.Length - buildIdEnd;
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(module, 0, buildIdStart);
        hash.AppendData(module, buildIdEnd, trailingLength);
        byte[] buildId = hash.GetHashAndReset();

        using FileStream output = File.OpenWrite(path);
        output.Position = buildIdStart;
        output.Write(module, buildIdEnd, trailingLength);
        output.WriteByte(0); // custom section
        // All lengths fit into a single ULEB128 byte.
        output.WriteByte((byte)(1 + sectionName.Length + 1 + buildId.Length));
        output.WriteByte((byte)sectionName.Length);
        output.Write(sectionName, 0, sectionName.Length);
        output.WriteByte((byte)buildId.Length);
        output.Write(buildId, 0, buildId.Length);
        output.SetLength(output.Position);
        return buildId;
    }

    private static uint ReadULEB128(byte[] module, ref int position, int end)
    {
        uint value = 0;
        for (int shift = 0; shift < 35 && position < end; shift += 7)
        {
            byte current = module[position++];
            if (shift == 28 && current > 0x0f)
                break;

            value |= (uint)(current & 0x7f) << shift;
            if ((current & 0x80) == 0)
                return value;
        }

        throw new InvalidDataException("Invalid WebAssembly ULEB128 length.");
    }

    private static string? GetSourceMapPath(string arguments)
    {
        if (arguments.IndexOf("--output-source-map", StringComparison.Ordinal) < 0 &&
            arguments.IndexOf("-osm", StringComparison.Ordinal) < 0)
            return null;

        // Match arguments as passed to Exec, including paths quoted for the command shell.
        string pattern = Path.DirectorySeparatorChar == '\\'
            ? """(?:"(?<text>[^"]*)"|(?<text>[^\s"]+))+"""
            : """(?:"(?<text>[^"]*)"|'(?<text>[^']*)'|\\(?<text>.)|(?<text>[^\s"'\\]+))+""";
        string? path = null;
        bool readPath = false;
        foreach (Match match in Regex.Matches(arguments, pattern))
        {
            var token = new StringBuilder(match.Length);
            foreach (Capture part in match.Groups["text"].Captures)
                token.Append(part.Value);

            string argument = token.ToString();
            if (readPath)
            {
                path = argument;
                readPath = false;
                continue;
            }

            int equals = argument.IndexOf('=');
            string option = equals < 0 ? argument : argument.Substring(0, equals);
            if (option is "--output-source-map" or "-osm")
            {
                readPath = equals < 0;
                if (!readPath)
                    path = argument.Substring(equals + 1);
            }
        }

        return path;
    }
}
