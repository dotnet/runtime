// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;
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
    public void R2R_WithActivePayload_ReadsPayloadAndTableSize()
    {
        byte[] wasm = BuildWebcilInWasm(payloadSize: 0x00ABCDEF, tableSize: 0x42, activePayload: true);

        using var stream = new MemoryStream(wasm);
        bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out int payloadSize, out int tableSize, out string? failureReason);

        Assert.True(ok, failureReason);
        Assert.Equal(0x00ABCDEF, payloadSize);
        Assert.Equal(0x42, tableSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(128)]
    public void R2R_WithActivePayload_WebcilReaderReadsMetadata(int? memoryIndex)
    {
        using var directory = new TempDirectory();
        string assemblyPath = typeof(object).Assembly.Location;
        string webcilPath = Path.Combine(directory.Path, "System.Private.CoreLib.webcil");
        WebcilConverter converter = WebcilConverter.FromPortableExecutable(assemblyPath, webcilPath, webcilVersion: 1);
        converter.WrapInWebAssembly = false;
        converter.ConvertToWebcil();

        byte[] payload = File.ReadAllBytes(webcilPath);
        byte[] wasm = BuildWebcilInWasm(payload, tableSize: 1, activePayload: true, memoryIndex: memoryIndex);

        using var stream = new MemoryStream(wasm);
        using var reader = new WebcilReader(stream);
        MetadataReader metadataReader = reader.GetMetadataReader();

        Assert.Equal(
            typeof(object).Assembly.GetName().Name,
            metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name));
        Assert.Equal(
            typeof(object).Module.ModuleVersionId,
            metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid));
    }

    [Theory]
    [InlineData(0, 0, 1, new[] { "getWebcilPayload", "getWebcilSize" })]
    [InlineData(1, 2, 0, new[] { "getWebcilSize" })]
    public void WebcilConverter_WrapsPayloadForRuntime(
        int payloadVersion,
        int expectedWrapperVersion,
        byte expectedPayloadSegmentMode,
        string[] expectedFunctionExports)
    {
        using var directory = new TempDirectory();
        string wasmPath = Path.Combine(directory.Path, "System.Private.CoreLib.wasm");
        WebcilConverter.FromPortableExecutable(typeof(object).Assembly.Location, wasmPath, payloadVersion).ConvertToWebcil();
        byte[] wasm = File.ReadAllBytes(wasmPath);

        WrapperShape shape = ReadWrapperShape(wasm);
        Assert.Equal(expectedWrapperVersion, shape.WrapperVersion);
        Assert.Equal(expectedPayloadSegmentMode, shape.PayloadSegmentMode);
        Assert.Equal(expectedFunctionExports, shape.FunctionExports);
        if (expectedPayloadSegmentMode == 0)
        {
            // global.get of the imported __memory_base, the only imported global.
            Assert.Equal(new byte[] { 0x23, 0x00, 0x0b }, shape.PayloadOffsetExpr);
            Assert.Equal(new[] { "__memory_base" }, shape.GlobalImports);
        }
        else
        {
            Assert.Empty(shape.GlobalImports);
        }

        using (var stream = new MemoryStream(wasm))
        {
            Assert.True(WebcilReader.TryReadWebcilInWasmSizes(stream, out int payloadSize, out int tableSize, out string? failureReason), failureReason);
            Assert.Equal(shape.PayloadLength, payloadSize);
            Assert.Equal(0, tableSize);
        }

        using (var stream = new MemoryStream(wasm))
        using (var reader = new WebcilReader(stream))
        {
            MetadataReader metadataReader = reader.GetMetadataReader();
            Assert.Equal(
                typeof(object).Module.ModuleVersionId,
                metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid));
        }
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

    [Theory]
    [InlineData("netstandard.dll", false, true, false, true)]
    [InlineData("System.Private.CoreLib.dll", true, false, false, true)]
    [InlineData("netstandard.dll", false, false, false, false)]
    [InlineData("System.Private.CoreLib.dll", true, true, false, false)]
    [InlineData("netstandard.dll", false, false, true, true)]
    [InlineData("System.Private.CoreLib.dll", true, true, true, true)]
    public void ConvertDllsToWebcil_RespectsAssemblyAndOutputFlavor(
        string assemblyName,
        bool usePrebuiltR2R,
        bool outputUsesR2R,
        bool forceWithStamp,
        bool expectReplacement)
    {
        using var directory = new TempDirectory();
        string assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, assemblyName);
        string assemblyBaseName = Path.GetFileNameWithoutExtension(assemblyName);
        string prebuiltDirectory = Path.Combine(directory.Path, "prebuilt");
        string outputDirectory = Path.Combine(directory.Path, "output");
        string intermediateDirectory = Path.Combine(directory.Path, "intermediate");
        Directory.CreateDirectory(prebuiltDirectory);

        bool hasILCode;
        using (FileStream stream = File.OpenRead(assemblyPath))
        using (var peReader = new PEReader(stream))
        {
            MetadataReader metadataReader = peReader.GetMetadataReader();
            hasILCode = metadataReader.MethodDefinitions.Any(handle => metadataReader.GetMethodDefinition(handle).RelativeVirtualAddress > 0);
        }
        Assert.Equal(usePrebuiltR2R, hasILCode);

        byte[] invalidPrebuiltImage = { 0xde, 0xad, 0xbe, 0xef };
        File.WriteAllBytes(Path.Combine(prebuiltDirectory, assemblyBaseName + ".wasm"), invalidPrebuiltImage);
        string outputPath = Path.Combine(outputDirectory, assemblyBaseName + ".wasm");
        Directory.CreateDirectory(outputDirectory);
        byte[] existingOutput = BuildWebcilInWasm(payloadSize: 4, tableSize: outputUsesR2R ? 1 : null);
        File.WriteAllBytes(outputPath, existingOutput);
        File.SetLastWriteTimeUtc(outputPath, DateTime.UtcNow.AddMinutes(1));
        string conversionStamp = Path.Combine(directory.Path, "conversion.stamp");
        if (forceWithStamp)
        {
            File.WriteAllText(conversionStamp, "changed");
            File.SetLastWriteTimeUtc(conversionStamp, DateTime.UtcNow.AddMinutes(2));
        }

        var candidate = new TaskItem(assemblyPath);
        candidate.SetMetadata("RelativePath", assemblyName);

        var task = new ConvertDllsToWebcil
        {
            BuildEngine = new TestBuildEngine(),
            Candidates = [candidate],
            ConversionStamp = forceWithStamp ? conversionStamp : null,
            IntermediateOutputPath = intermediateDirectory,
            IsEnabled = true,
            OutputPath = outputDirectory,
            PrebuiltR2RDirectory = prebuiltDirectory,
        };
        Assert.True(task.Execute());
        Assert.Equal(expectReplacement ? new[] { outputPath } : Array.Empty<string>(), task.FilesToTouch);

        byte[] actualOutput = File.ReadAllBytes(outputPath);
        if (!expectReplacement)
        {
            Assert.True(existingOutput.SequenceEqual(actualOutput));
            return;
        }

        if (usePrebuiltR2R)
        {
            Assert.True(invalidPrebuiltImage.SequenceEqual(actualOutput));
            return;
        }

        Assert.False(existingOutput.SequenceEqual(actualOutput));
        using FileStream output = File.OpenRead(outputPath);
        Assert.True(WebcilReader.TryReadWebcilInWasmSizes(output, out _, out int tableSize, out string? failureReason), failureReason);
        Assert.Equal(0, tableSize);
    }

    [Fact]
    public void ConvertDllsToWebcil_StagesR2RWebcilWithDllExtension()
    {
        using var directory = new TempDirectory();
        string prebuiltDirectory = Path.Combine(directory.Path, "prebuilt");
        string outputDirectory = Path.Combine(directory.Path, "output");
        Directory.CreateDirectory(prebuiltDirectory);

        byte[] r2rWebcil = BuildWebcilInWasm(payloadSize: 4, tableSize: 1);
        string candidatePath = Path.Combine(prebuiltDirectory, "R2RAssembly.dll");
        File.WriteAllBytes(candidatePath, r2rWebcil);

        var candidate = new TaskItem(candidatePath);
        candidate.SetMetadata("RelativePath", "R2RAssembly.dll");

        var task = new ConvertDllsToWebcil
        {
            BuildEngine = new TestBuildEngine(),
            Candidates = [candidate],
            IntermediateOutputPath = Path.Combine(directory.Path, "intermediate"),
            IsEnabled = true,
            OutputPath = outputDirectory,
            PrebuiltR2RDirectory = prebuiltDirectory,
        };

        Assert.True(task.Execute());
        Assert.True(r2rWebcil.SequenceEqual(File.ReadAllBytes(Path.Combine(outputDirectory, "R2RAssembly.wasm"))));
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, 0)]
    [InlineData(true, 128)]
    public void ConvertDllsToWebcil_FallsBackToIL_WhenPrebuiltMvidMismatches(bool wrapInWebcil, int? memoryIndex)
    {
        // A prebuilt R2R image whose MVID differs from the candidate must never be staged: it would fail-fast
        // at load against the current version bubble. Use two real assemblies with distinct MVIDs.
        string candidatePath = typeof(System.Console).Assembly.Location;
        string mismatchedAssembly = typeof(object).Assembly.Location;
        Assert.True(File.Exists(candidatePath), $"Candidate assembly not found: '{candidatePath}'.");
        Assert.True(File.Exists(mismatchedAssembly), $"Mismatched assembly not found: '{mismatchedAssembly}'.");

        using var directory = new TempDirectory();
        string prebuiltDirectory = Path.Combine(directory.Path, "prebuilt");
        string outputDirectory = Path.Combine(directory.Path, "output");
        Directory.CreateDirectory(prebuiltDirectory);
        string prebuiltPath = Path.Combine(prebuiltDirectory, "System.Console.wasm");
        if (wrapInWebcil)
        {
            string payloadPath = Path.Combine(directory.Path, "payload.webcil");
            WebcilConverter converter = WebcilConverter.FromPortableExecutable(mismatchedAssembly, payloadPath, webcilVersion: 1);
            converter.WrapInWebAssembly = false;
            converter.ConvertToWebcil();
            File.WriteAllBytes(prebuiltPath, BuildWebcilInWasm(
                File.ReadAllBytes(payloadPath), tableSize: 1, activePayload: true, memoryIndex: memoryIndex));
        }
        else
        {
            File.Copy(mismatchedAssembly, prebuiltPath);
        }

        var candidate = new TaskItem(candidatePath);
        candidate.SetMetadata("RelativePath", "System.Console.dll");

        var task = new ConvertDllsToWebcil
        {
            BuildEngine = new TestBuildEngine(),
            Candidates = [candidate],
            IntermediateOutputPath = Path.Combine(directory.Path, "intermediate"),
            IsEnabled = true,
            OutputPath = outputDirectory,
            PrebuiltR2RDirectory = prebuiltDirectory,
        };

        Assert.True(task.Execute());

        // The output must be a freshly converted IL webcil (no R2R table), not the mismatched prebuilt.
        using FileStream output = File.OpenRead(Path.Combine(outputDirectory, "System.Console.wasm"));
        Assert.True(WebcilReader.TryReadWebcilInWasmSizes(output, out _, out int tableSize, out string? failureReason), failureReason);
        Assert.Equal(0, tableSize);
    }

    private const byte SectionCustom = 0x00;
    private const byte SectionData = 0x0b;

    // Builds a minimal webcil-in-wasm module: a data section with segment 0 holding payloadSize
    // (and, for R2R, tableSize) followed by a payload segment, mirroring the real layout.
    private static byte[] BuildWebcilInWasm(int payloadSize, int? tableSize, bool activePayload = false)
        => BuildWebcilInWasm(new byte[] { 0xde, 0xad, 0xbe, 0xef }, tableSize, activePayload, payloadSize);

    private static byte[] BuildWebcilInWasm(byte[] payload, int? tableSize, bool activePayload = false, int? payloadSize = null, int? memoryIndex = null)
    {
        var sizes = new List<byte>();
        WriteUInt32LE(sizes, (uint)(payloadSize ?? payload.Length));
        if (tableSize is int ts)
            WriteUInt32LE(sizes, (uint)ts);

        var body = new List<byte>();
        WriteULEB(body, 2); // two segments: sizes, then payload

        body.Add(0x01); // passive
        WriteULEB(body, (uint)sizes.Count);
        body.AddRange(sizes);

        if (activePayload)
        {
            body.Add(memoryIndex.HasValue ? (byte)0x02 : (byte)0x00); // active
            if (memoryIndex is int index)
                WriteULEB(body, (uint)index);
            body.Add(0x23); // global.get
            WriteULEB(body, 1); // __memory_base
            body.Add(0x0B); // end
        }
        else
        {
            body.Add(0x01); // passive
        }
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

    private sealed record WrapperShape(
        int WrapperVersion,
        byte PayloadSegmentMode,
        byte[] PayloadOffsetExpr,
        int PayloadLength,
        string[] FunctionExports,
        string[] GlobalImports);

    // Reads the parts of a converter-produced wrapper that distinguish the passive and self-installing forms.
    private static WrapperShape ReadWrapperShape(byte[] wasm)
    {
        int wrapperVersion = -1;
        byte payloadSegmentMode = 0xff;
        byte[] payloadOffsetExpr = Array.Empty<byte>();
        int payloadLength = -1;
        var functionExports = new List<string>();
        var globalImports = new List<string>();
        var exportedGlobals = new Dictionary<uint, string>();
        var definedGlobalValues = new List<int>();

        int offset = 8;
        while (offset < wasm.Length)
        {
            byte sectionId = wasm[offset++];
            int sectionEnd = checked((int)ReadULEB(wasm, ref offset) + offset);
            switch (sectionId)
            {
                case 2: // Import
                    for (uint i = ReadULEB(wasm, ref offset); i > 0; i--)
                    {
                        ReadName(wasm, ref offset);
                        string name = ReadName(wasm, ref offset);
                        byte kind = wasm[offset++];
                        if (kind == 2) // memory: flags, min
                        {
                            byte flags = wasm[offset++];
                            ReadULEB(wasm, ref offset);
                            if ((flags & 1) != 0)
                                ReadULEB(wasm, ref offset);
                        }
                        else
                        {
                            Assert.Equal((byte)3, kind);
                            offset += 2; // valtype, mutability
                            globalImports.Add(name);
                        }
                    }
                    break;
                case 6: // Global
                    for (uint i = ReadULEB(wasm, ref offset); i > 0; i--)
                    {
                        offset += 2; // valtype, mutability
                        Assert.Equal((byte)0x41, wasm[offset++]); // i32.const
                        definedGlobalValues.Add((int)ReadULEB(wasm, ref offset));
                        Assert.Equal((byte)0x0b, wasm[offset++]);
                    }
                    break;
                case 7: // Export
                    for (uint i = ReadULEB(wasm, ref offset); i > 0; i--)
                    {
                        string name = ReadName(wasm, ref offset);
                        byte kind = wasm[offset++];
                        uint index = ReadULEB(wasm, ref offset);
                        if (kind == 0)
                            functionExports.Add(name);
                        else if (kind == 3)
                            exportedGlobals[index] = name;
                    }
                    break;
                case 11: // Data
                    Assert.Equal(2u, ReadULEB(wasm, ref offset));
                    Assert.Equal((byte)1, wasm[offset++]); // segment 0 stays passive
                    int sizesLength = (int)ReadULEB(wasm, ref offset);
                    offset += sizesLength;
                    payloadSegmentMode = wasm[offset++];
                    if (payloadSegmentMode == 0)
                    {
                        int exprStart = offset;
                        while (wasm[offset++] != 0x0b)
                        {
                        }
                        payloadOffsetExpr = wasm.AsSpan(exprStart, offset - exprStart).ToArray();
                    }
                    payloadLength = (int)ReadULEB(wasm, ref offset);
                    Assert.Equal(sectionEnd, offset + payloadLength);
                    break;
            }
            offset = sectionEnd;
        }

        foreach ((uint index, string name) in exportedGlobals)
        {
            if (name == "webcilVersion")
                wrapperVersion = definedGlobalValues[checked((int)index - globalImports.Count)];
        }

        functionExports.Sort(StringComparer.Ordinal);
        return new WrapperShape(wrapperVersion, payloadSegmentMode, payloadOffsetExpr, payloadLength, functionExports.ToArray(), globalImports.ToArray());
    }

    private static string ReadName(byte[] wasm, ref int offset)
    {
        int length = (int)ReadULEB(wasm, ref offset);
        string name = System.Text.Encoding.UTF8.GetString(wasm, offset, length);
        offset += length;
        return name;
    }

    private static uint ReadULEB(byte[] wasm, ref int offset)
    {
        uint value = 0;
        int shift = 0;
        byte b;
        do
        {
            b = wasm[offset++];
            value |= (uint)(b & 0x7f) << shift;
            shift += 7;
        }
        while ((b & 0x80) != 0);
        return value;
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

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class TestBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
            => throw new NotSupportedException();

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e) => Assert.Fail(e.Message ?? "Build error");

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
        }

        public void LogWarningEvent(BuildWarningEventArgs e) => Assert.Fail(e.Message ?? "Build warning");
    }
}
