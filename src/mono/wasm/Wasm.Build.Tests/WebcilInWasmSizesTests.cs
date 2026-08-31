// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.NET.WebAssembly.Webcil;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests;

[TestCategory("no-workload")]
public class WebcilInWasmSizesTests
{
    [Fact]
    public void NonR2R_ReadsPayloadSize_WithZeroTableSize()
    {
        byte[] wasm = BuildWebcilInWasm(payloadSize: 0x1234, tableSize: null);

        using var stream = new MemoryStream(wasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out int payloadSize, out int tableSize, out string? failureReason);

        Assert.True(ok, failureReason);
        Assert.Equal(0x1234, payloadSize);
        Assert.Equal(0, tableSize);
    }

    [Fact]
    public void R2R_ReadsPayloadAndTableSize()
    {
        byte[] wasm = BuildWebcilInWasm(payloadSize: 0x00ABCDEF, tableSize: 0x42);

        using var stream = new MemoryStream(wasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out int payloadSize, out int tableSize, out string? failureReason);

        Assert.True(ok, failureReason);
        Assert.Equal(0x00ABCDEF, payloadSize);
        Assert.Equal(0x42, tableSize);
    }

    [Fact]
    public void NotAWasmModule_Fails()
    {
        byte[] notWasm = { 0x7f, 0x45, 0x4c, 0x46, 0x00, 0x00, 0x00, 0x00 };

        using var stream = new MemoryStream(notWasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out _, out string? failureReason);

        Assert.False(ok);
        Assert.NotNull(failureReason);
    }

    [Fact]
    public void DataSegmentTooSmall_Fails()
    {
        // A passive data segment 0 that is smaller than the 4-byte payload size.
        var body = new List<byte>();
        WriteULEB(body, 1); // one segment
        body.Add(0x01); // passive
        WriteULEB(body, 2); // only 2 bytes of data
        body.Add(0x00);
        body.Add(0x00);
        byte[] wasm = WrapModule(SectionData, body);

        using var stream = new MemoryStream(wasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out _, out string? failureReason);

        Assert.False(ok);
        Assert.NotNull(failureReason);
    }

    [Fact]
    public void NoDataSection_Fails()
    {
        // A module with only a custom section and no data section.
        var body = new List<byte> { 0x00 }; // custom section: zero-length name
        byte[] wasm = WrapModule(SectionCustom, body);

        using var stream = new MemoryStream(wasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out _, out string? failureReason);

        Assert.False(ok);
        Assert.NotNull(failureReason);
    }

    private const byte SectionCustom = 0x00;
    private const byte SectionData = 0x0b;

    // Builds a minimal webcil-in-wasm module: a data section with segment 0 holding payloadSize
    // (and, for R2R, tableSize) followed by a payload segment, mirroring the real layout.
    private static byte[] BuildWebcilInWasm(int payloadSize, int? tableSize)
    {
        var sizes = new List<byte>();
        WriteUInt32LE(sizes, (uint)payloadSize);
        if (tableSize is int ts)
            WriteUInt32LE(sizes, (uint)ts);

        var body = new List<byte>();
        WriteULEB(body, 2); // two segments: sizes, then payload

        body.Add(0x01); // passive
        WriteULEB(body, (uint)sizes.Count);
        body.AddRange(sizes);

        byte[] payload = { 0xde, 0xad, 0xbe, 0xef };
        body.Add(0x01); // passive
        WriteULEB(body, (uint)payload.Length);
        body.AddRange(payload);

        return WrapModule(SectionData, body);
    }

    private static byte[] WrapModule(byte sectionCode, List<byte> sectionBody)
    {
        var module = new List<byte> { 0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00 };
        module.Add(sectionCode);
        WriteULEB(module, (uint)sectionBody.Count);
        module.AddRange(sectionBody);
        return module.ToArray();
    }

    private static void WriteUInt32LE(List<byte> buffer, uint value)
    {
        buffer.Add((byte)(value & 0xff));
        buffer.Add((byte)((value >> 8) & 0xff));
        buffer.Add((byte)((value >> 16) & 0xff));
        buffer.Add((byte)((value >> 24) & 0xff));
    }

    private static void WriteULEB(List<byte> buffer, uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            buffer.Add(b);
        }
        while (value != 0);
    }
}
