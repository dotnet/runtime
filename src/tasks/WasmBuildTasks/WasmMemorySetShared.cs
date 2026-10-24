// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Patches a wasm binary's memory definition to set the shared flag.
/// This enables SharedArrayBuffer usage at runtime without requiring
/// the native build to use threaded Emscripten libraries.
/// Handles both imported memory (import section) and locally-defined
/// memory (memory section), e.g. when memory is exported.
/// </summary>
public class WasmMemorySetShared : Task
{
    [Required]
    public string WasmFilePath { get; set; } = string.Empty;

    public override bool Execute()
    {
        if (!File.Exists(WasmFilePath))
        {
            Log.LogError($"Wasm file not found: {WasmFilePath}");
            return false;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(WasmFilePath);
            if (!PatchMemorySharedFlag(bytes))
                return false;

            File.WriteAllBytes(WasmFilePath, bytes);
            Log.LogMessage(MessageImportance.High, $"Patched shared memory flag in {WasmFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to patch wasm file: {ex.Message}");
            return false;
        }
    }

    private bool PatchMemorySharedFlag(byte[] bytes)
    {
        // Validate wasm header: magic + version
        if (bytes.Length < 8
            || bytes[0] != 0x00 || bytes[1] != 0x61 || bytes[2] != 0x73 || bytes[3] != 0x6D
            || bytes[4] != 0x01 || bytes[5] != 0x00 || bytes[6] != 0x00 || bytes[7] != 0x00)
        {
            Log.LogError("Not a valid wasm binary");
            return false;
        }

        int pos = 8;
        while (pos < bytes.Length)
        {
            byte sectionId = bytes[pos++];
            int sectionSize = ReadULEB128(bytes, ref pos);
            int sectionEnd = pos + sectionSize;

            if (sectionId == 2) // Import section
            {
                int importCount = ReadULEB128(bytes, ref pos);
                for (int i = 0; i < importCount; i++)
                {
                    // Skip module name
                    int nameLen = ReadULEB128(bytes, ref pos);
                    pos += nameLen;
                    // Skip field name
                    nameLen = ReadULEB128(bytes, ref pos);
                    pos += nameLen;
                    // Import kind
                    byte kind = bytes[pos++];

                    if (kind == 0x02) // Memory import
                    {
                        SetSharedBit(bytes, ref pos);
                        return true;
                    }

                    // Skip other import types
                    SkipImportType(bytes, ref pos, kind);
                }
            }
            else if (sectionId == 5) // Memory section
            {
                int memCount = ReadULEB128(bytes, ref pos);
                if (memCount > 0)
                {
                    SetSharedBit(bytes, ref pos);
                    return true;
                }
            }
            else
            {
                pos = sectionEnd;
                continue;
            }

            pos = sectionEnd;
        }

        Log.LogError("No memory definition found in wasm binary (neither imported nor locally defined)");
        return false;
    }

    private void SetSharedBit(byte[] bytes, ref int pos)
    {
        // pos points to the limits flags byte
        // Bit 0 = has_max, Bit 1 = shared
        bytes[pos] |= 0x02;
        Log.LogMessage(MessageImportance.Low, $"Set shared bit at offset {pos}, flags: 0x{bytes[pos]:X2}");
    }

    private static void SkipImportType(byte[] bytes, ref int pos, byte kind)
    {
        switch (kind)
        {
            case 0x00: // Function: typeidx (uleb128)
                ReadULEB128(bytes, ref pos);
                break;
            case 0x01: // Table: elemtype (byte) + limits
                pos++; // elemtype
                SkipLimits(bytes, ref pos);
                break;
            case 0x02: // Memory: limits
                SkipLimits(bytes, ref pos);
                break;
            case 0x03: // Global: valtype (byte) + mutability (byte)
                pos += 2;
                break;
        }
    }

    private static void SkipLimits(byte[] bytes, ref int pos)
    {
        byte flags = bytes[pos++];
        ReadULEB128(bytes, ref pos); // initial
        if ((flags & 0x01) != 0)
            ReadULEB128(bytes, ref pos); // maximum
    }

    private static int ReadULEB128(byte[] bytes, ref int pos)
    {
        int result = 0;
        int shift = 0;
        byte b;
        do
        {
            b = bytes[pos++];
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }
}
