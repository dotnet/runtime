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
