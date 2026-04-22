// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;
using Internal.Runtime;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Static assertion helpers for validating R2R images via <see cref="ReadyToRunReader"/>.
/// Use these in <see cref="CrossgenCompilation.Validate"/> callbacks.
/// </summary>
internal static class R2RAssert
{
    /// <summary>
    /// Returns all methods (assembly methods + instance methods) from the reader.
    /// </summary>
    public static List<ReadyToRunMethod> GetAllMethods(ReadyToRunReader reader)
    {
        var methods = new List<ReadyToRunMethod>();
        foreach (var assembly in reader.ReadyToRunAssemblies)
            methods.AddRange(assembly.Methods);
        foreach (var instanceMethod in reader.InstanceMethods)
            methods.Add(instanceMethod.Method);

        return methods;
    }

    /// <summary>
    /// Asserts the R2R image contains a manifest or MSIL assembly reference with the given name.
    /// </summary>
    public static void HasManifestRef(ReadyToRunReader reader, string assemblyName)
    {
        var allRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var globalMetadata = reader.GetGlobalMetadata();
        if (globalMetadata is not null)
        {
            var mdReader = globalMetadata.MetadataReader;
            foreach (var handle in mdReader.AssemblyReferences)
            {
                var assemblyRef = mdReader.GetAssemblyReference(handle);
                allRefs.Add(mdReader.GetString(assemblyRef.Name));
            }
        }

        foreach (var kvp in reader.ManifestReferenceAssemblies)
            allRefs.Add(kvp.Key);

        Assert.True(allRefs.Contains(assemblyName),
            $"Expected assembly reference '{assemblyName}' not found. " +
            $"Found: [{string.Join(", ", allRefs.OrderBy(s => s))}]");
    }

    /// <summary>
    /// Asserts that the CrossModuleInlineInfo section records that <paramref name="inlinerMethodName"/>
    /// inlined <paramref name="inlineeMethodName"/>, and that the inlinee is encoded as a cross-module
    /// reference (ILBody import index, not a local MethodDef RID).
    /// </summary>
    public static void HasCrossModuleInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName)
    {
        var inliningInfo = GetCrossModuleInliningInfoSection(reader);

        var allPairs = new List<string>();
        foreach (var (inlinerName, inlineeName, inlineeKind) in inliningInfo.GetInliningPairs())
        {
            allPairs.Add($"{inlinerName} → {inlineeName} ({inlineeKind})");

            if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(inlineeKind == CrossModuleInliningInfoSection.InlineeReferenceKind.CrossModule,
                    $"Found inlining pair '{inlinerName} → {inlineeName}' but the inlinee is not encoded " +
                    $"as a cross-module reference ({inlineeKind}). Expected ILBody import encoding.");
                return;
            }
        }

        Assert.Fail(
            $"Expected cross-module inlining '{inlineeMethodName}' into '{inlinerMethodName}', but it was not found.\n" +
            $"Recorded inlining pairs:\n  {string.Join("\n  ", allPairs)}");
    }

    /// <summary>
    /// Asserts that the CrossModuleInlineInfo section has an entry for an inlinee matching
    /// <paramref name="inlineeMethodName"/> with cross-module inliners whose resolved names
    /// contain each of the <paramref name="expectedInlinerNames"/>. This validates that
    /// cross-module inliner indices (encoded as absolute ILBody import indices) resolve
    /// to the correct method names.
    /// </summary>
    public static void HasCrossModuleInliners(
        ReadyToRunReader reader,
        string inlineeMethodName,
        params string[] expectedInlinerNames)
    {
        var inliningInfo = GetCrossModuleInliningInfoSection(reader);

        foreach (var entry in inliningInfo.GetEntries())
        {
            string inlineeName = inliningInfo.ResolveMethodName(entry.Inlinee);
            if (!inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                continue;

            var crossModuleInlinerNames = new List<string>();
            foreach (var inliner in entry.Inliners)
            {
                if (inliner.IsCrossModule)
                    crossModuleInlinerNames.Add(inliningInfo.ResolveMethodName(inliner));
            }

            foreach (string expected in expectedInlinerNames)
            {
                Assert.True(
                    crossModuleInlinerNames.Any(n => n.Contains(expected, StringComparison.OrdinalIgnoreCase)),
                    $"Inlinee '{inlineeName}': expected a cross-module inliner matching '{expected}' " +
                    $"but found only:\n  {string.Join("\n  ", crossModuleInlinerNames)}");
            }

            return;
        }

        var allEntries = new List<string>();
        foreach (var (inlinerName, inlineeName, _) in inliningInfo.GetInliningPairs())
            allEntries.Add($"{inlinerName} → {inlineeName}");

        Assert.Fail(
            $"No CrossModuleInlineInfo entry found for inlinee matching '{inlineeMethodName}'.\n" +
            $"All inlining pairs:\n  {string.Join("\n  ", allEntries)}");
    }

    /// <summary>
    /// Asserts that any inlining info section (CrossModuleInlineInfo or InliningInfo2) records that
    /// <paramref name="inlinerMethodName"/> inlined <paramref name="inlineeMethodName"/>.
    /// Does not check whether the encoding is cross-module or local.
    /// </summary>
    public static void HasInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName)
    {
        var foundPairs = new List<string>();

        if (reader.ReadyToRunHeader.Sections.ContainsKey(ReadyToRunSectionType.CrossModuleInlineInfo))
        {
            var inliningInfo = GetCrossModuleInliningInfoSection(reader);
            foreach (var (inlinerName, inlineeName, _) in inliningInfo.GetInliningPairs())
            {
                if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                    inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                foundPairs.Add($"[CXMI] {inlinerName} -> {inlineeName}");
            }
        }

        foreach (var info2 in GetAllInliningInfo2Sections(reader))
        {
            foreach (var (inlinerName, inlineeName) in info2.GetInliningPairs())
            {
                if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                    inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                foundPairs.Add($"[II2] {inlinerName} -> {inlineeName}");
            }
        }

        string pairList = foundPairs.Count > 0
            ? string.Join("\n  ", foundPairs)
            : "(none)";

        Assert.Fail(
            $"Expected inlining '{inlineeMethodName}' into '{inlinerMethodName}', but it was not found.\n" +
            $"Found inlining pairs:\n  {pairList}");
    }

    private static CrossModuleInliningInfoSection GetCrossModuleInliningInfoSection(ReadyToRunReader reader)
    {
        Assert.True(
            reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.CrossModuleInlineInfo, out ReadyToRunSection section),
            "Expected CrossModuleInlineInfo section not found in R2R image.");

        int offset = reader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;

        return new CrossModuleInliningInfoSection(reader, offset, endOffset);
    }

    private static IEnumerable<InliningInfoSection2> GetAllInliningInfo2Sections(ReadyToRunReader reader)
    {
        // InliningInfo2 can appear in the global header
        if (reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.InliningInfo2, out ReadyToRunSection globalSection))
        {
            int offset = reader.GetOffset(globalSection.RelativeVirtualAddress);
            int endOffset = offset + globalSection.Size;
            yield return new InliningInfoSection2(reader, offset, endOffset);
        }

        // In composite images, InliningInfo2 is per-assembly
        if (reader.ReadyToRunAssemblyHeaders is not null)
        {
            for (int asmIndex = 0; asmIndex < reader.ReadyToRunAssemblyHeaders.Count; asmIndex++)
            {
                var asmHeader = reader.ReadyToRunAssemblyHeaders[asmIndex];
                if (asmHeader.Sections.TryGetValue(
                        ReadyToRunSectionType.InliningInfo2, out ReadyToRunSection asmSection))
                {
                    int offset = reader.GetOffset(asmSection.RelativeVirtualAddress);
                    int endOffset = offset + asmSection.Size;
                    uint ownerModuleIndex = reader.Composite
                        ? (uint)(asmIndex + reader.ComponentAssemblyIndexOffset)
                        : 0;
                    yield return new InliningInfoSection2(reader, offset, endOffset, ownerModuleIndex);
                }
            }
        }
    }

    /// <summary>
    /// Asserts the R2R image contains a CrossModuleInlineInfo section with at least one entry.
    /// </summary>
    public static void HasCrossModuleInliningInfo(ReadyToRunReader reader)
    {
        Assert.True(
            reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.CrossModuleInlineInfo, out ReadyToRunSection section),
            "Expected CrossModuleInlineInfo section not found in R2R image.");

        int offset = reader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;
        var inliningInfo = new CrossModuleInliningInfoSection(reader, offset, endOffset);
        string dump = inliningInfo.ToString();

        Assert.True(
            dump.Length > 0,
            "CrossModuleInlineInfo section is present but contains no entries.");
    }

    /// <summary>
    /// Asserts the R2R image contains an [ASYNC] variant entry whose signature contains the given method name.
    /// </summary>
    public static void HasAsyncVariant(ReadyToRunReader reader, string methodName)
    {
        var asyncSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[ASYNC]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        Assert.True(
            asyncSigs.Any(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase)),
            $"Expected [ASYNC] variant for '{methodName}' not found. " +
            $"Async methods: [{string.Join(", ", asyncSigs)}]");
    }

    /// <summary>
    /// Asserts the R2R image contains a [RESUME] stub entry whose signature contains the given method name.
    /// </summary>
    public static void HasResumptionStub(ReadyToRunReader reader, string methodName)
    {
        var resumeSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[RESUME]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        Assert.True(
            resumeSigs.Any(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase)),
            $"Expected [RESUME] stub for '{methodName}' not found. " +
            $"Resume methods: [{string.Join(", ", resumeSigs)}]");
    }

    /// <summary>
    /// Asserts the R2R image contains at least one ContinuationLayout fixup.
    /// </summary>
    public static void HasContinuationLayout(ReadyToRunReader reader)
    {
        HasFixupKind(reader, ReadyToRunFixupKind.ContinuationLayout);
    }

    /// <summary>
    /// Asserts a method whose signature contains <paramref name="methodName"/>
    /// has at least one ContinuationLayout fixup.
    /// </summary>
    public static void HasContinuationLayout(ReadyToRunReader reader, string methodName)
    {
        HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ContinuationLayout, methodName);
    }

    /// <summary>
    /// Asserts the R2R image contains at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static void HasResumptionStubFixup(ReadyToRunReader reader)
    {
        HasFixupKind(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint);
    }

    /// <summary>
    /// Asserts a method whose signature contains <paramref name="methodName"/>
    /// has at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static void HasResumptionStubFixup(ReadyToRunReader reader, string methodName)
    {
        HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, methodName);
    }

    /// <summary>
    /// Asserts the R2R image contains at least one fixup of the given kind.
    /// </summary>
    public static void HasFixupKind(ReadyToRunReader reader, ReadyToRunFixupKind kind)
    {
        var presentKinds = new HashSet<ReadyToRunFixupKind>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null)
                    presentKinds.Add(cell.Signature.FixupKind);
            }
        }

        Assert.True(presentKinds.Contains(kind),
            $"Expected fixup kind '{kind}' not found. " +
            $"Present kinds: [{string.Join(", ", presentKinds)}]");
    }

    /// <summary>
    /// Asserts a method whose signature contains <paramref name="methodName"/>
    /// has at least one fixup of the given kind.
    /// </summary>
    public static void HasFixupKindOnMethod(ReadyToRunReader reader, ReadyToRunFixupKind kind, string methodName)
    {
        var methodsWithFixup = new List<string>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;

            bool hasKind = false;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                {
                    hasKind = true;
                    break;
                }
            }

            if (hasKind)
            {
                methodsWithFixup.Add(method.SignatureString);
                if (method.SignatureString.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                    return;
            }
        }

        Assert.Fail(
            $"Expected fixup kind '{kind}' on method matching '{methodName}', but not found.\n" +
            $"Methods with '{kind}' fixups: [{string.Join(", ", methodsWithFixup)}]");
    }
}

/// <summary>
/// Simple assembly resolver that looks in the same directory as the input image.
/// </summary>
internal sealed class SimpleAssemblyResolver : IAssemblyResolver
{
    private readonly TestPaths _paths;

    public SimpleAssemblyResolver(TestPaths paths)
    {
        _paths = paths;
    }

    public IAssemblyMetadata? FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
    {
        var assemblyRef = metadataReader.GetAssemblyReference(assemblyReferenceHandle);
        string name = metadataReader.GetString(assemblyRef.Name);

        return FindAssembly(name, parentFile);
    }

    public IAssemblyMetadata? FindAssembly(string simpleName, string parentFile)
    {
        string? dir = Path.GetDirectoryName(parentFile);
        if (dir is null)
            return null;

        string candidate = Path.Combine(dir, simpleName + ".dll");
        if (!File.Exists(candidate))
            candidate = Path.Combine(_paths.RuntimePackDir, simpleName + ".dll");

        if (!File.Exists(candidate))
            return null;

        return new SimpleAssemblyMetadata(candidate);
    }
}

/// <summary>
/// Simple assembly metadata wrapper that loads the PE image into memory
/// to avoid holding file handles open (IAssemblyMetadata has no disposal contract,
/// and ReadyToRunReader caches these indefinitely).
/// </summary>
internal sealed class SimpleAssemblyMetadata : IAssemblyMetadata
{
    private readonly PEReader _peReader;

    public SimpleAssemblyMetadata(string path)
    {
        byte[] imageBytes = File.ReadAllBytes(path);
        _peReader = new PEReader(new MemoryStream(imageBytes));
    }

    public PEReader ImageReader => _peReader;

    public MetadataReader MetadataReader => _peReader.GetMetadataReader();
}
