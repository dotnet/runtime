// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;
using Microsoft.NET.WebAssembly.Webcil;
using Xunit;
using SL = Microsoft.Build.Logging.StructuredLogger;

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

    [Fact]
    public void WasiR2RComposition_SeparatesFunctionAndTableSlotCounts()
    {
        using var directory = new TempDirectory();
        string compositePath = Path.Combine(directory.Path, "composite.wasm");
        File.WriteAllBytes(compositePath, BuildWasiR2RComposite(
            functionCount: 2,
            functionIndices: [1, 0, 1, 1, 0]));

        WasiR2RComposition.InspectComposite(
            compositePath,
            out int functionCount,
            out int tableSlotCount,
            out int payloadSize);

        Assert.Equal(2, functionCount);
        Assert.Equal(5, tableSlotCount);
        Assert.Equal(4, payloadSize);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecompressedRuntimePack_ProducesAndRegistersRuntimeAssets(bool endpointFingerprint)
    {
        using var pack = new PrecompressedTestPack();
        Assert.Equal(5, pack.Producer.CompressedFiles.Length - 1);
        Assert.Equal(4, pack.Producer.ReplacedFiles.Length);
        Assert.All(pack.Producer.ReplacedFiles, file => Assert.False(File.Exists(file.ItemSpec)));
        Assert.True(File.Exists(pack.AssemblyPath));
        Assert.True(File.Exists(pack.FacadePath));
        Assert.True(File.Exists(Path.Combine(pack.Root, "runtimes/browser-wasm/native/dotnet.js")));

        var native = new ResolvePrecompressedWasmRuntimeAssets
        {
            BuildEngine = new TestBuildEngine(),
            RuntimePackDirectory = pack.Root,
            OutputPath = Path.Combine(pack.Directory, "native"),
            RuntimeAssets = [new TaskItem(pack.NativePath)],
            CopyLocalAssets = [new TaskItem(pack.NativePath)]
        };
        Assert.True(native.Execute());
        Assert.Equal(native.UpdatedRuntimeAssets[0].ItemSpec, native.UpdatedCopyLocalAssets[0].ItemSpec);
        Assert.Equal(pack.NativeBytes, File.ReadAllBytes(native.UpdatedRuntimeAssets[0].ItemSpec));

        ResolvePrecompressedWasmAssemblies assemblies = pack.Resolve();
        Assert.True(assemblies.Execute());
        Assert.Equal(2, assemblies.Representations.Length);
        Assert.Empty(assemblies.ConversionCandidates);
        Assert.Equal(pack.ReadyToRunBytes, File.ReadAllBytes(Path.Combine(pack.Directory, "webcil", "System.Console.wasm")));

        DateTime timestamp = File.GetLastWriteTimeUtc(Path.Combine(pack.Directory, "webcil", "System.Console.wasm"));
        Assert.True(pack.Resolve().Execute());
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(Path.Combine(pack.Directory, "webcil", "System.Console.wasm")));

        var assets = new List<ITaskItem> { native.UpdatedRuntimeAssets[0] };
        foreach (ITaskItem candidate in assemblies.UpdatedCandidates.Where(c => c.GetMetadata("Integrity").Length != 0))
            assets.Add(new TaskItem(candidate.GetMetadata("WebcilOutputPath"), candidate.CloneCustomMetadata()));
        foreach (ITaskItem asset in assets)
        {
            asset.SetMetadata("RelativePath", "_framework/" + Path.GetFileName(asset.ItemSpec));
            asset.SetMetadata("BasePath", "client");
            asset.SetMetadata("SourceType", "Computed");
        }
        ITaskItem first = assets[0];
        first.SetMetadata("FileLength", pack.NativeBytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        first.SetMetadata("LastWriteTime", "2001-01-01T00:00:00Z");
        string fingerprinted = "_framework/dotnet.native." + first.GetMetadata("Fingerprint") + ".wasm";
        var endpoint = new TaskItem("client/" + fingerprinted);
        endpoint.SetMetadata("AssetFile", first.ItemSpec);
        endpoint.SetMetadata("EndpointProperties", "[{\"Name\":\"fingerprint\",\"Value\":\"" + first.GetMetadata("Fingerprint") + "\"}]");
        if (!endpointFingerprint)
            first.SetMetadata("RelativePath", fingerprinted);

        var register = new DefinePrecompressedWasmAssets
        {
            BuildEngine = new TestBuildEngine(),
            Assets = assets.ToArray(),
            Endpoints = endpointFingerprint ? [endpoint] : [],
            Representations = native.Representations.Concat(assemblies.Representations).ToArray()
        };
        Assert.True(register.Execute());
        Assert.Equal(3, register.GzipAssets.Length);
        ITaskItem gzip = Assert.Single(register.GzipAssets.Where(a => a.GetMetadata("RelatedAsset") == first.ItemSpec));
        string expectedRelative = endpointFingerprint
            ? "_framework/dotnet.native#[.{fingerprint=" + first.GetMetadata("Fingerprint") + "}]!.wasm"
            : fingerprinted;
        Assert.Equal(expectedRelative + "." + gzip.GetMetadata("Fingerprint") + ".gz", gzip.GetMetadata("RelativePath"));
        Assert.Equal("Framework", gzip.GetMetadata("SourceType"));
        Assert.Equal("client", gzip.GetMetadata("BasePath"));
        Assert.Equal("Alternative", gzip.GetMetadata("AssetRole"));
        Assert.Equal("Content-Encoding", gzip.GetMetadata("AssetTraitName"));
        Assert.Equal("gzip", gzip.GetMetadata("AssetTraitValue"));
        Assert.Empty(gzip.GetMetadata("FileLength"));
        Assert.Empty(gzip.GetMetadata("LastWriteTime"));
        Assert.NotEqual(first.GetMetadata("Integrity"), gzip.GetMetadata("Integrity"));
        Assert.NotEqual(first.GetMetadata("Fingerprint"), gzip.GetMetadata("Fingerprint"));
        Assert.Equal(Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(gzip.ItemSpec))), gzip.GetMetadata("Integrity"));
        using var stream = new GZipStream(File.OpenRead(gzip.ItemSpec), CompressionMode.Decompress);
        using var decoded = new MemoryStream();
        stream.CopyTo(decoded);
        Assert.Equal(pack.NativeBytes, decoded.ToArray());
    }

    [Theory]
    [InlineData("NuGetPackageId", "another.package")]
    [InlineData("NuGetPackageVersion", "2.0.0")]
    [InlineData("PathInPackage", "other/System.Console.dll")]
    public void PrecompressedRuntimePack_DoesNotReuseDifferentInputs(string metadata, string value)
    {
        using var pack = new PrecompressedTestPack();
        ResolvePrecompressedWasmAssemblies task = pack.Resolve();
        task.Candidates[0].SetMetadata(metadata, value);
        Assert.True(task.Execute());
        Assert.Equal(pack.AssemblyPath, Assert.Single(task.ConversionCandidates).ItemSpec);
        Assert.Equal("runtimes/browser-wasm/lib/net12.0/netstandard.dll", Assert.Single(task.Representations).GetMetadata("SourcePath"));
    }

    [Theory]
    [InlineData(true, 1, 1)]
    [InlineData(false, 0, 2)]
    public void PrecompressedRuntimePack_FallsBackWhenR2ROrWebcilVersionChanges(bool disableR2R, int version, int expectedConversions)
    {
        using var pack = new PrecompressedTestPack();
        ResolvePrecompressedWasmAssemblies task = pack.Resolve();
        task.WebcilVersion = version;
        if (disableR2R)
            task.PrebuiltR2RDirectory = "";
        Assert.True(task.Execute());
        Assert.Equal(expectedConversions, task.ConversionCandidates.Length);
    }

    [Theory]
    [InlineData("Integrity", "Fingerprint")]
    [InlineData("GzipIntegrity", "GzipFingerprint")]
    public void PrecompressedRuntimePack_ReusesManifestHashes(string integrityName, string fingerprintName)
    {
        using var pack = new PrecompressedTestPack();
        ResolvePrecompressedWasmAssemblies initial = pack.Resolve();
        Assert.True(initial.Execute());
        string manifestPath = Path.Combine(pack.Root, "data", "PrecompressedAssets.xml");
        XElement manifest = XElement.Load(manifestPath);
        string integrity = Convert.ToBase64String(new byte[32]);
        const string Fingerprint = "0123456789";
        foreach (XElement asset in manifest.Elements("Asset"))
        {
            asset.SetAttributeValue(integrityName, integrity);
            asset.SetAttributeValue(fingerprintName, Fingerprint);
        }
        manifest.Save(manifestPath);

        var native = new ResolvePrecompressedWasmRuntimeAssets
        {
            BuildEngine = new TestBuildEngine(),
            RuntimePackDirectory = pack.Root,
            OutputPath = Path.Combine(pack.Directory, "native"),
            RuntimeAssets = [new TaskItem(pack.NativePath)]
        };
        Assert.True(native.Execute());
        Assert.Equal(pack.NativeBytes, File.ReadAllBytes(Assert.Single(native.UpdatedRuntimeAssets).ItemSpec));
        ResolvePrecompressedWasmAssemblies assemblies = pack.Resolve();
        Assert.True(assemblies.Execute());
        Assert.Empty(assemblies.ConversionCandidates);
        Assert.Equal(pack.ReadyToRunBytes, File.ReadAllBytes(Path.Combine(pack.Directory, "webcil", "System.Console.wasm")));
        Assert.All(native.Representations.Concat(assemblies.Representations), asset =>
        {
            Assert.Equal(integrity, asset.GetMetadata(integrityName));
            Assert.Equal(Fingerprint, asset.GetMetadata(fingerprintName));
        });
        Assert.NotEqual(initial.SelectionStamp, assemblies.SelectionStamp);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecompressedRuntimePack_SelectionStampIsSafeForMsbuild(bool emptySelection)
    {
        using var pack = new PrecompressedTestPack();
        string manifestPath = Path.Combine(pack.Root, "data", "PrecompressedAssets.xml");
        XElement manifest = XElement.Load(manifestPath);
        XElement asset = Assert.Single(manifest.Elements("Asset").Where(a => (string?)a.Attribute("Kind") == "ReadyToRun"));
        string original = (string)asset.Attribute("GzipPath")!;
        const string EncodedName = "System.Console;$(Selection)%(Identity)@(Items).\u03b2.wasm.gz";
        string changed = original.Substring(0, original.LastIndexOf('/') + 1) + EncodedName;
        File.Move(Path.Combine(pack.Root, original), Path.Combine(pack.Root, changed));
        asset.SetAttributeValue("GzipPath", changed);
        manifest.Save(manifestPath);

        SL.Build build = RunPrecompressedTarget(pack, "_ComputeWasmBuildCandidates;_WriteWasmBuildWebcilStamp", "-p:EmptySelection=" + emptySelection);
        string selection = GetSelectionStamp(build);
        Assert.Equal(32, Convert.FromBase64String(selection).Length);
        Assert.Equal(emptySelection, selection == Convert.ToBase64String(SHA256.HashData(Array.Empty<byte>())));
        string stamp = Path.Combine(pack.Directory, "obj", "wasm-webcil-build.stamp");
        Assert.Contains("_WasmPrecompressedSelectionStamp=" + selection, File.ReadAllLines(stamp));
        DateTime timestamp = File.GetLastWriteTimeUtc(stamp);

        SL.Build repeated = RunPrecompressedTarget(pack, "_ComputeWasmBuildCandidates;_WriteWasmBuildWebcilStamp", "-p:EmptySelection=" + emptySelection);
        Assert.Equal(selection, GetSelectionStamp(repeated));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(stamp));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("truncated")]
    [InlineData("mvid")]
    public void PrecompressedRuntimePack_InvalidatesChangedRepresentations(string change)
    {
        using var pack = new PrecompressedTestPack();
        ResolvePrecompressedWasmAssemblies initial = pack.Resolve();
        Assert.True(initial.Execute());
        string compressed = initial.Representations.Single(a => a.GetMetadata("Kind") == "ReadyToRun").ItemSpec;
        if (change == "missing")
            File.Delete(compressed);
        else if (change == "truncated")
            File.WriteAllBytes(compressed, new byte[new FileInfo(compressed).Length - 1]);
        else
            File.Copy(typeof(object).Assembly.Location, pack.AssemblyPath, overwrite: true);
        ResolvePrecompressedWasmAssemblies changed = pack.Resolve();
        Assert.True(changed.Execute());
        Assert.Equal(pack.AssemblyPath, Assert.Single(changed.ConversionCandidates).ItemSpec);
        Assert.NotEqual(initial.SelectionStamp, changed.SelectionStamp);
        Assert.Empty(changed.ConversionCandidates[0].GetMetadata("Integrity"));
    }

    [Fact]
    public void PrecompressedRuntimePack_ReportsTruncatedNativeAsset()
    {
        using var pack = new PrecompressedTestPack();
        string compressed = Path.Combine(pack.Root, "precompressed/runtimes/browser-wasm/native/dotnet.native.wasm.gz");
        File.WriteAllBytes(compressed, new byte[new FileInfo(compressed).Length - 1]);
        var engine = new TestBuildEngine { AllowErrors = true };
        var task = new ResolvePrecompressedWasmRuntimeAssets
        {
            BuildEngine = engine,
            RuntimePackDirectory = pack.Root,
            OutputPath = Path.Combine(pack.Directory, "native"),
            IncludeReadyToRun = true,
            RuntimeAssets = [new TaskItem(pack.NativePath), new TaskItem(pack.ReadyToRunPath)]
        };
        Assert.False(task.Execute());
        Assert.Contains("Cannot read gzip-only runtime asset", Assert.Single(engine.Errors));
        Assert.False(File.Exists(pack.NativePath));
        Assert.False(File.Exists(pack.ReadyToRunPath));
    }

    [Fact]
    public void PrecompressedRuntimePack_ReturnsCompressedPackageFiles()
    {
        using var pack = new PrecompressedTestPack(precompress: false);
        SL.Build build = RunPrecompressedTarget(pack, "CheckProducerOutputs", "-p:ProducerTest=true");
        SL.Task caller = Assert.Single(build.FindChildrenRecursive<SL.Task>(task => task.Name == "MSBuild"));
        string[] files = GetTaskOutputItems(caller, "_ReturnedPackageFiles").Select(Path.GetFullPath).ToArray();
        Assert.Contains(files, path => path.EndsWith("PrecompressedAssets.xml", StringComparison.Ordinal));
        Assert.Contains(files, path => path.EndsWith("dotnet.native.wasm.gz", StringComparison.Ordinal));
        Assert.Contains(files, path => path.EndsWith("System.Console.wasm.gz", StringComparison.Ordinal));
        Assert.DoesNotContain(Path.GetFullPath(pack.NativePath), files);
        Assert.DoesNotContain(Path.GetFullPath(pack.ReadyToRunPath), files);
        Assert.Contains(Path.GetFullPath(pack.AssemblyPath), files);
        Assert.Contains(Path.GetFullPath(pack.FacadePath), files);
    }

    [Fact]
    public void PrecompressedRuntimePack_PreservesCacheAcrossNativeAndReadyToRunPhases()
    {
        using var pack = new PrecompressedTestPack();
        string output = Path.Combine(pack.Directory, "native");
        string cache = Path.Combine(output, "precompressed.assets.cache");
        Resolve(includeReadyToRun: false);
        Resolve(includeReadyToRun: true);
        string populated = File.ReadAllText(cache);

        Resolve(includeReadyToRun: false);
        Assert.Equal(populated, File.ReadAllText(cache));
        Resolve(includeReadyToRun: true);
        Assert.Equal(populated, File.ReadAllText(cache));

        void Resolve(bool includeReadyToRun)
        {
            var task = new ResolvePrecompressedWasmRuntimeAssets
            {
                BuildEngine = new TestBuildEngine(),
                RuntimePackDirectory = pack.Root,
                OutputPath = output,
                IncludeReadyToRun = includeReadyToRun
            };
            Assert.True(task.Execute());
        }
    }

    [Theory]
    [InlineData("Configuration", "Release", false)]
    [InlineData("PublishReadyToRun", "true", true)]
    [InlineData("BlazorIcuDataFileName", "icudt.dat", true)]
    [InlineData("WasmEnableWebcil", "false", false)]
    [InlineData("WasmPerformanceInstrumentation", "all", false)]
    public void PrecompressedRuntimePack_TargetsPreserveRequiredInputs(string property, string value, bool expectedRawR2R)
    {
        using var pack = new PrecompressedTestPack();
        SL.Build build = RunPrecompressedTarget(pack, "_ComputeWasmBuildCandidates", "-p:" + property + "=" + value);
        Assert.Empty(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ResolvePrecompressedWasmAssemblies)));
        SL.Task[] materializers = build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ResolvePrecompressedWasmRuntimeAssets)).ToArray();
        Assert.Equal(expectedRawR2R ? 2 : 1, materializers.Length);
        string native = Assert.Single(GetTaskOutputItems(materializers[0], "_WasmPrecompressedRuntimeAssets"));
        Assert.Equal(native, Assert.Single(GetTaskOutputItems(materializers[0], "_WasmPrecompressedCopyLocalAssets")));
        Assert.Equal(pack.NativeBytes, File.ReadAllBytes(native));
        string r2r = Path.Combine(Path.GetDirectoryName(native)!, "r2r", "System.Console.wasm");
        Assert.Equal(expectedRawR2R, File.Exists(r2r));
        if (expectedRawR2R)
            Assert.Equal(pack.ReadyToRunBytes, File.ReadAllBytes(r2r));
        if (property == "BlazorIcuDataFileName")
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(native)!, value)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecompressedRuntimePack_HostedClientsHaveSeparateEncodedAssets(bool optionalFingerprint)
    {
        using var pack = new PrecompressedTestPack();
        SL.Build build = RunPrecompressedTarget(pack, "CheckHostedStaticAssets",
            "-p:TestOptionalFingerprint=" + optionalFingerprint, "-p:TestAppAsset=true");
        SL.Task[] compressors = build.FindChildrenRecursive<SL.Task>(task => task.Name == "GZipCompress").ToArray();
        Assert.Equal(2, compressors.Length);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack.Directory, "hosted.json")));
        JsonElement[] assets = document.RootElement.GetProperty("Assets").EnumerateArray().ToArray();
        JsonElement[] gzip = assets.Where(a => a.GetProperty("AssetTraitValue").GetString() == "gzip").ToArray();
        Assert.Equal(4, gzip.Length);
        Assert.Equal(2, gzip.Count(a => Path.GetFileName(a.GetProperty("RelatedAsset").GetString()) == "app.js"));
        gzip = gzip.Where(a => Path.GetFileName(a.GetProperty("RelatedAsset").GetString()) == "dotnet.native.wasm").ToArray();
        Assert.Equal(2, gzip.Length);
        Assert.NotEqual(gzip[0].GetProperty("Identity").GetString(), gzip[1].GetProperty("Identity").GetString());
        byte[] packedGzip = File.ReadAllBytes(Path.Combine(pack.Root, "precompressed/runtimes/browser-wasm/native/dotnet.native.wasm.gz"));
        foreach (JsonElement asset in gzip)
        {
            string path = asset.GetProperty("Identity").GetString()!;
            Assert.StartsWith(pack.Directory, path);
            Assert.False(path.StartsWith(pack.Root, StringComparison.Ordinal));
            Assert.Equal(packedGzip, File.ReadAllBytes(path));
            Assert.Equal(new FileInfo(path).Length, asset.GetProperty("FileLength").GetInt64());
            string original = asset.GetProperty("RelatedAsset").GetString()!;
            JsonElement primary = Assert.Single(assets.Where(a => a.GetProperty("Identity").GetString() == original));
            string fingerprint = "." + primary.GetProperty("Fingerprint").GetString() + ".";
            JsonElement endpoint = Assert.Single(document.RootElement.GetProperty("Endpoints").EnumerateArray()
                .Where(e => e.GetProperty("AssetFile").GetString() == path &&
                    e.GetProperty("Route").GetString()!.Contains(fingerprint, StringComparison.Ordinal) &&
                    e.GetProperty("Selectors").EnumerateArray().Any(s => s.GetProperty("Value").GetString() == "gzip")));
            string source = asset.GetProperty("SourceId").GetString()!;
            Assert.StartsWith(source + "/", endpoint.GetProperty("Route").GetString());
            JsonElement length = Assert.Single(endpoint.GetProperty("ResponseHeaders").EnumerateArray()
                .Where(h => h.GetProperty("Name").GetString() == "Content-Length"));
            Assert.Equal(new FileInfo(path).Length.ToString(System.Globalization.CultureInfo.InvariantCulture), length.GetProperty("Value").GetString());
            if (optionalFingerprint)
            {
                Assert.Contains(document.RootElement.GetProperty("Endpoints").EnumerateArray(), e =>
                    e.GetProperty("AssetFile").GetString() == path &&
                    e.GetProperty("Route").GetString() == source + "/_framework/dotnet.native.wasm" &&
                    e.GetProperty("Selectors").EnumerateArray().Any(s => s.GetProperty("Value").GetString() == "gzip"));
            }
        }
    }

    [Fact]
    public void PrecompressedRuntimePack_DisabledBuildCompressionDoesNotRegisterGzip()
    {
        using var pack = new PrecompressedTestPack();
        SL.Build build = RunPrecompressedTarget(pack, "CheckStaticAssets", "-p:DisableBuildCompression=true",
            "-p:PackageId=Client", "-p:StaticWebAssetBasePath=Client");
        Assert.Empty(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(DefinePrecompressedWasmAssets)));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack.Directory, "staticwebassets.json")));
        JsonElement[] assets = document.RootElement.GetProperty("Assets").EnumerateArray().ToArray();
        Assert.NotEmpty(assets);
        Assert.DoesNotContain(assets, asset => asset.GetProperty("AssetTraitValue").GetString() == "gzip");
    }

    [Fact]
    public void PrecompressedRuntimePack_DowngradeDoesNotReuseStaleGzip()
    {
        using var pack = new PrecompressedTestPack();
        string manifestPath = Path.Combine(pack.Root, "data", "PrecompressedAssets.xml");
        byte[] manifest = File.ReadAllBytes(manifestPath);
        string gzipPath = Path.Combine(pack.Root, "precompressed/runtimes/browser-wasm/native/dotnet.native.wasm.gz");
        byte[] originalGzip = File.ReadAllBytes(gzipPath);
        File.WriteAllBytes(pack.NativePath, pack.NativeBytes.Concat(new byte[] { 0, 1, 0 }).ToArray());
        var native = new TaskItem(pack.NativePath);
        native.SetMetadata("TargetPath", "runtimes/browser-wasm/native");
        var producer = new PrecompressWasmRuntimeAssets
        {
            BuildEngine = new TestBuildEngine(), Files = [native], OutputPath = pack.Root,
            PackageId = pack.Producer.PackageId, PackageVersion = pack.Producer.PackageVersion
        };
        Assert.True(producer.Execute());
        File.Delete(pack.NativePath);
        File.SetLastWriteTimeUtc(gzipPath, new DateTime(2002, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        RunPrecompressedTarget(pack, "CheckStaticAssets", "-p:PackageId=Client", "-p:StaticWebAssetBasePath=Client");
        using JsonDocument newer = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack.Directory, "staticwebassets.json")));
        string newerGzip = Assert.Single(newer.RootElement.GetProperty("Assets").EnumerateArray()
            .Where(asset => asset.GetProperty("AssetTraitValue").GetString() == "gzip")).GetProperty("Identity").GetString()!;
        Assert.Equal(File.ReadAllBytes(gzipPath), File.ReadAllBytes(newerGzip));

        File.WriteAllBytes(manifestPath, manifest);
        File.WriteAllBytes(gzipPath, originalGzip);
        File.SetLastWriteTimeUtc(gzipPath, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        RunPrecompressedTarget(pack, "CheckStaticAssets", "-p:PackageId=Client", "-p:StaticWebAssetBasePath=Client");
        using JsonDocument older = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack.Directory, "staticwebassets.json")));
        JsonElement gzip = Assert.Single(older.RootElement.GetProperty("Assets").EnumerateArray()
            .Where(asset => asset.GetProperty("AssetTraitValue").GetString() == "gzip"));
        string olderGzip = gzip.GetProperty("Identity").GetString()!;
        Assert.Equal(originalGzip, File.ReadAllBytes(olderGzip));
        Assert.Equal(Convert.ToBase64String(SHA256.HashData(originalGzip)), gzip.GetProperty("Integrity").GetString());
    }

    [Theory]
    [InlineData("WasmBuildNative")]
    [InlineData("RunAOTCompilation")]
    [InlineData("_IsPublishing")]
    public void PrecompressedRuntimePack_RechecksLateConfigurationChanges(string property)
    {
        using var pack = new PrecompressedTestPack();
        SL.Build build = RunPrecompressedTarget(pack, "_ComputeWasmBuildCandidates;_StageSelectedAssembly", "-p:LateExclusion=" + property);
        Assert.Empty(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ResolvePrecompressedWasmAssemblies)));
        Assert.Single(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ConvertDllsToWebcil)));
        AssertStagedReadyToRun(pack, expected: true);
    }

    [Theory]
    [InlineData("WasmBuildNative", "true", true)]
    [InlineData("RunAOTCompilation", "true", true)]
    [InlineData("Configuration", "Release", false)]
    [InlineData("_WasmUsePrebuiltReadyToRun", "false", false)]
    [InlineData("_IsPublishing", "true", false)]
    public void PrecompressedRuntimePack_PreservesTheCoreClrReadyToRunSelection(string property, string value, bool expectedPackStaging)
    {
        using var pack = new PrecompressedTestPack();
        SL.Build build = RunPrecompressedTarget(pack, "_ComputeWasmBuildCandidates;_StageSelectedAssembly", "-p:" + property + "=" + value);
        Assert.Single(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ConvertDllsToWebcil)));
        Assert.Equal(expectedPackStaging ? 2 : 1,
            build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ResolvePrecompressedWasmRuntimeAssets)).Count());
        AssertStagedReadyToRun(pack, expectedPackStaging);
    }

    private static string GetSelectionStamp(SL.Build build)
    {
        SL.Task resolver = Assert.Single(build.FindChildrenRecursive<SL.Task>(task => task.Name == nameof(ResolvePrecompressedWasmAssemblies)));
        return Assert.Single(resolver.FindChildrenRecursive<SL.Property>(property => property.Name == "_WasmPrecompressedSelectionStamp")).Value;
    }

    private static string[] GetTaskOutputItems(SL.Task task, string itemName)
    {
        SL.Folder outputs = task.FindChild<SL.Folder>("OutputItems");
        Assert.NotNull(outputs);
        SL.NamedNode items = outputs.FindChild<SL.NamedNode>(itemName);
        Assert.NotNull(items);
        return items.Children.OfType<SL.Item>().Select(item => item.Text).ToArray();
    }

    private static void AssertStagedReadyToRun(PrecompressedTestPack pack, bool expected)
    {
        string path = Path.Combine(pack.Directory, "staged-webcil", "System.Console.wasm");
        using FileStream stream = File.OpenRead(path);
        Assert.True(WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int table, out string? reason), reason);
        Assert.Equal(expected, table > 0);
        if (expected)
            Assert.Equal(pack.ReadyToRunBytes, File.ReadAllBytes(path));
    }

    private static SL.Build RunPrecompressedTarget(PrecompressedTestPack pack, string target, params string[] properties)
    {
        string? dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (string.IsNullOrEmpty(dotnet))
            dotnet = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        Assert.True(File.Exists(dotnet), $"Cannot find the SDK host at '{dotnet}'.");
        var startInfo = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = pack.Directory, RedirectStandardOutput = true, RedirectStandardError = true
        };
        string logDirectory = string.IsNullOrEmpty(EnvironmentVariables.TestLogPath)
            ? Environment.CurrentDirectory : Path.GetFullPath(EnvironmentVariables.TestLogPath);
        Directory.CreateDirectory(logDirectory);
        string binlog = Path.Combine(logDirectory, "precompressed-" + Guid.NewGuid().ToString("N") + ".binlog");
        foreach (string argument in new[]
        {
            "msbuild", Path.Combine(AppContext.BaseDirectory, "data", "PrecompressedRuntimePackTests.proj"), "-nologo", "-t:" + target, "-bl:" + binlog,
            "-p:PackDirectory=" + pack.Root, "-p:TestDirectory=" + pack.Directory,
            "-p:TaskAssembly=" + typeof(PrecompressWasmRuntimeAssets).Assembly.Location,
            "-p:_WasmPrecompressionTasksAssembly=" + typeof(PrecompressWasmRuntimeAssets).Assembly.Location
        }.Concat(properties))
            startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo)!;
        System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
        System.Threading.Tasks.Task<string> error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"Binlog: {binlog}{Environment.NewLine}" +
            output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        SL.Build build = SL.BinaryLog.ReadBuild(binlog);
        SL.BuildAnalyzer.AnalyzeBuild(build);
        return build;
    }

    private sealed class PrecompressedTestPack : IDisposable
    {
        private readonly TempDirectory _directory = new();
        public string Directory => _directory.Path;
        public string Root { get; }
        public string AssemblyPath { get; }
        public string FacadePath { get; }
        public string NativePath { get; }
        public string ReadyToRunPath { get; }
        public byte[] NativeBytes { get; } = [0, 0x61, 0x73, 0x6d, 1, 0, 0, 0];
        public byte[] ReadyToRunBytes { get; }
        public PrecompressWasmRuntimeAssets Producer { get; }

        public PrecompressedTestPack(bool precompress = true)
        {
            Root = Path.Combine(Directory, "pack");
            string native = Path.Combine(Root, "runtimes/browser-wasm/native");
            string managed = Path.Combine(Root, "runtimes/browser-wasm/lib/net12.0");
            System.IO.Directory.CreateDirectory(Path.Combine(native, "r2r"));
            System.IO.Directory.CreateDirectory(managed);
            AssemblyPath = Path.Combine(managed, "System.Console.dll");
            FacadePath = Path.Combine(managed, "netstandard.dll");
            NativePath = Path.Combine(native, "dotnet.native.wasm");
            ReadyToRunPath = Path.Combine(native, "r2r/System.Console.wasm");
            File.Copy(typeof(Console).Assembly.Location, AssemblyPath);
            File.Copy(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll"), FacadePath);
            string payload = Path.Combine(Directory, "payload.webcil");
            WebcilConverter converter = WebcilConverter.FromPortableExecutable(AssemblyPath, payload, webcilVersion: 1);
            converter.WrapInWebAssembly = false;
            converter.ConvertToWebcil();
            ReadyToRunBytes = BuildWebcilInWasm(File.ReadAllBytes(payload), tableSize: 1);
            File.WriteAllBytes(ReadyToRunPath, ReadyToRunBytes);
            File.WriteAllBytes(NativePath, NativeBytes);
            File.WriteAllText(Path.Combine(native, "dotnet.js"), "dynamic boot configuration");
            File.WriteAllText(Path.Combine(native, "dotnet.runtime.js"), "export const runtime = 1;");
            File.WriteAllText(Path.Combine(native, "icudt.dat"), "ICU data");
            ITaskItem[] files = new[] { AssemblyPath, FacadePath, NativePath, ReadyToRunPath, Path.Combine(native, "dotnet.js"), Path.Combine(native, "dotnet.runtime.js"), Path.Combine(native, "icudt.dat") }
                .Select(path =>
                {
                    var item = new TaskItem(path);
                    item.SetMetadata("TargetPath", Path.GetRelativePath(Root, Path.GetDirectoryName(path)!));
                    return (ITaskItem)item;
                }).ToArray();
            Producer = new PrecompressWasmRuntimeAssets
            {
                BuildEngine = new TestBuildEngine(), Files = files, OutputPath = Root,
                PackageId = "Microsoft.NETCore.App.Runtime.browser-wasm", PackageVersion = "1.0.0"
            };
            if (precompress)
            {
                Assert.True(Producer.Execute());
                foreach (ITaskItem file in Producer.ReplacedFiles)
                    File.Delete(file.ItemSpec);
            }
        }

        public ResolvePrecompressedWasmAssemblies Resolve()
        {
            string output = Path.Combine(Directory, "webcil");
            return new ResolvePrecompressedWasmAssemblies
            {
                BuildEngine = new TestBuildEngine(), RuntimePackDirectory = Root, OutputPath = output,
                PrebuiltR2RDirectory = Path.Combine(Root, "runtimes/browser-wasm/native/r2r"), WebcilVersion = 1,
                Candidates = new[] { AssemblyPath, FacadePath }.Select(path =>
                {
                    var item = new TaskItem(path);
                    item.SetMetadata("NuGetPackageId", Producer.PackageId);
                    item.SetMetadata("NuGetPackageVersion", Producer.PackageVersion);
                    item.SetMetadata("PathInPackage", Path.GetRelativePath(Root, path).Replace('\\', '/'));
                    item.SetMetadata("WebcilOutputPath", Path.Combine(output, Path.GetFileNameWithoutExtension(path) + ".wasm"));
                    return (ITaskItem)item;
                }).ToArray()
            };
        }

        public void Dispose() => _directory.Dispose();
    }

    private const byte SectionCustom = 0x00;
    private const byte SectionType = 0x01;
    private const byte SectionFunction = 0x03;
    private const byte SectionExport = 0x07;
    private const byte SectionElement = 0x09;
    private const byte SectionCode = 0x0a;
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

    private static byte[] BuildWasiR2RComposite(int functionCount, uint[] functionIndices)
    {
        var module = new List<byte> { 0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00 };

        AddSection(module, SectionType,
        [
            0x01, // one type
            0x60, // function
            0x00, // no parameters
            0x00, // no results
        ]);

        var functions = new List<byte>();
        WriteULEB(functions, (uint)functionCount);
        for (int i = 0; i < functionCount; i++)
            WriteULEB(functions, 0);
        AddSection(module, SectionFunction, functions);

        var exports = new List<byte>();
        WriteULEB(exports, 1);
        WriteName(exports, "patchWebcilHeader");
        exports.Add(0x00); // function
        WriteULEB(exports, 0);
        AddSection(module, SectionExport, exports);

        var elements = new List<byte>();
        WriteULEB(elements, 1);
        WriteULEB(elements, 0); // active table-0 function-index segment
        elements.Add(0x41); // i32.const
        WriteULEB(elements, 1);
        elements.Add(0x0b); // end
        WriteULEB(elements, (uint)functionIndices.Length);
        foreach (uint functionIndex in functionIndices)
            WriteULEB(elements, functionIndex);
        AddSection(module, SectionElement, elements);

        var code = new List<byte>();
        WriteULEB(code, (uint)functionCount);
        for (int i = 0; i < functionCount; i++)
        {
            WriteULEB(code, 2);
            code.Add(0x00); // no locals
            code.Add(0x0b); // end
        }
        AddSection(module, SectionCode, code);

        var data = new List<byte>();
        WriteULEB(data, 1);
        WriteULEB(data, 0); // active memory-0 segment
        data.Add(0x41); // i32.const
        WriteULEB(data, 0);
        data.Add(0x0b); // end
        WriteULEB(data, 4);
        data.AddRange([0x57, 0x62, 0x49, 0x4c]);
        AddSection(module, SectionData, data);

        return module.ToArray();
    }

    private static void AddSection(List<byte> module, byte sectionCode, List<byte> sectionBody)
    {
        module.Add(sectionCode);
        WriteULEB(module, (uint)sectionBody.Count);
        module.AddRange(sectionBody);
    }

    private static byte[] WrapModule(byte sectionCode, List<byte> sectionBody)
    {
        var module = new List<byte> { 0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00 };
        AddSection(module, sectionCode, sectionBody);
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

    private static void WriteName(List<byte> buffer, string value)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteULEB(buffer, (uint)bytes.Length);
        buffer.AddRange(bytes);
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
        public string Path { get; } = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class TestBuildEngine : IBuildEngine
    {
        public bool AllowErrors { get; init; }
        public List<string> Errors { get; } = [];
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
            => throw new NotSupportedException();

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            if (!AllowErrors)
                Assert.Fail(e.Message ?? "Build error");
            Errors.Add(e.Message ?? "Build error");
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
        }

        public void LogWarningEvent(BuildWarningEventArgs e) => Assert.Fail(e.Message ?? "Build warning");
    }
}
