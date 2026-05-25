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
    /// Returns true if the R2R image contains a manifest or MSIL assembly reference with the given name.
    /// </summary>
    public static bool HasManifestRef(ReadyToRunReader reader, string assemblyName, out string diagnostic)
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

        bool found = allRefs.Contains(assemblyName);
        diagnostic = found
            ? $"Found manifest/MSIL ref '{assemblyName}'."
            : $"Expected assembly reference '{assemblyName}' not found. " +
              $"Found: [{string.Join(", ", allRefs.OrderBy(s => s))}]";
        return found;
    }

    /// <summary>
    /// Returns true if the CrossModuleInlineInfo section records that <paramref name="inlinerMethodName"/>
    /// inlined <paramref name="inlineeMethodName"/> and the inlinee is encoded as a cross-module
    /// reference (ILBody import index, not a local MethodDef RID). If a pair matches but the
    /// encoding is wrong, returns false with the mismatch described in <paramref name="diagnostic"/>.
    /// </summary>
    public static bool HasCrossModuleInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName, out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

        var allPairs = new List<string>();
        foreach (var (inlinerName, inlineeName, inlineeKind) in inliningInfo.GetInliningPairs())
        {
            allPairs.Add($"{inlinerName} → {inlineeName} ({inlineeKind})");

            if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
            {
                if (inlineeKind == CrossModuleInliningInfoSection.InlineeReferenceKind.CrossModule)
                {
                    diagnostic = $"Found cross-module inlining '{inlinerName} → {inlineeName}'.";
                    return true;
                }

                diagnostic =
                    $"Found inlining pair '{inlinerName} → {inlineeName}' but the inlinee is not encoded " +
                    $"as a cross-module reference ({inlineeKind}). Expected ILBody import encoding.";
                return false;
            }
        }

        diagnostic =
            $"Expected cross-module inlining '{inlineeMethodName}' into '{inlinerMethodName}', but it was not found.\n" +
            $"Recorded inlining pairs:\n  {string.Join("\n  ", allPairs)}";
        return false;
    }

    /// <summary>
    /// Returns true if the CrossModuleInlineInfo section has an entry for an inlinee matching
    /// <paramref name="inlineeMethodName"/> with cross-module inliners whose resolved names
    /// contain each of the <paramref name="expectedInlinerNames"/>. This validates that
    /// cross-module inliner indices (encoded as absolute ILBody import indices) resolve
    /// to the correct method names.
    /// </summary>
    public static bool HasCrossModuleInliners(
        ReadyToRunReader reader,
        string inlineeMethodName,
        IEnumerable<string> expectedInlinerNames,
        out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

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
                if (!crossModuleInlinerNames.Any(n => n.Contains(expected, StringComparison.OrdinalIgnoreCase)))
                {
                    diagnostic =
                        $"Inlinee '{inlineeName}': expected a cross-module inliner matching '{expected}' " +
                        $"but found only:\n  {string.Join("\n  ", crossModuleInlinerNames)}";
                    return false;
                }
            }

            diagnostic =
                $"Inlinee '{inlineeName}' has all expected cross-module inliners: " +
                $"[{string.Join(", ", expectedInlinerNames)}]";
            return true;
        }

        var allEntries = new List<string>();
        foreach (var (inlinerName, inlineeName, _) in inliningInfo.GetInliningPairs())
            allEntries.Add($"{inlinerName} → {inlineeName}");

        diagnostic =
            $"No CrossModuleInlineInfo entry found for inlinee matching '{inlineeMethodName}'.\n" +
            $"All inlining pairs:\n  {string.Join("\n  ", allEntries)}";
        return false;
    }

    /// <summary>
    /// Returns true if any inlining info section (CrossModuleInlineInfo or InliningInfo2) records
    /// that <paramref name="inlinerMethodName"/> inlined <paramref name="inlineeMethodName"/>.
    /// Does not check whether the encoding is cross-module or local.
    /// </summary>
    public static bool HasInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName, out string diagnostic)
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
                    diagnostic = $"Found inlining '{inlinerName} -> {inlineeName}' in CrossModuleInlineInfo.";
                    return true;
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
                    diagnostic = $"Found inlining '{inlinerName} -> {inlineeName}' in InliningInfo2.";
                    return true;
                }
                foundPairs.Add($"[II2] {inlinerName} -> {inlineeName}");
            }
        }

        string pairList = foundPairs.Count > 0 ? string.Join("\n  ", foundPairs) : "(none)";
        diagnostic =
            $"Inlining '{inlineeMethodName}' into '{inlinerMethodName}' not found.\n" +
            $"Inlining pairs in image:\n  {pairList}";
        return false;
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

    private static bool TryGetCrossModuleInliningInfoSection(ReadyToRunReader reader, out CrossModuleInliningInfoSection inliningInfo, out string diagnostic)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.CrossModuleInlineInfo, out ReadyToRunSection section))
        {
            inliningInfo = null!;
            diagnostic = "Expected CrossModuleInlineInfo section not found in R2R image.";
            return false;
        }

        int offset = reader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;
        inliningInfo = new CrossModuleInliningInfoSection(reader, offset, endOffset);
        diagnostic = "";
        return true;
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
    /// Returns true if the R2R image contains a CrossModuleInlineInfo section with at least one entry.
    /// </summary>
    public static bool HasCrossModuleInliningInfo(ReadyToRunReader reader, out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

        string dump = inliningInfo.ToString();
        if (dump.Length == 0)
        {
            diagnostic = "CrossModuleInlineInfo section is present but contains no entries.";
            return false;
        }

        diagnostic = $"CrossModuleInlineInfo contains entries:\n{dump}";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains exactly one [ASYNC] variant entry whose signature contains the given method name.
    /// Returns false if no match is found or if more than one [ASYNC] method signature matches the search token.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// </summary>
    public static bool HasAsyncVariant(ReadyToRunReader reader, string methodName, out string diagnostic)
    {
        var asyncSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[ASYNC]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        var matchingSigs = asyncSigs
            .Where(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingSigs.Count == 0)
        {
            diagnostic = $"Expected [ASYNC] variant for '{methodName}' not found. " +
                $"Async methods: [{string.Join(", ", asyncSigs)}]";
            return false;
        }

        if (matchingSigs.Count > 1)
        {
            diagnostic = $"Expected exactly one [ASYNC] variant matching '{methodName}', " +
                $"but found {matchingSigs.Count}: [{string.Join(", ", matchingSigs)}]";
            return false;
        }

        diagnostic = $"Found [ASYNC] variant for '{methodName}'.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains a [RESUME] stub entry whose signature contains the given method name.
    /// </summary>
    public static bool HasResumptionStub(ReadyToRunReader reader, string methodName, out string diagnostic)
    {
        var resumeSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[RESUME]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        bool found = resumeSigs.Any(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase));
        diagnostic = found
            ? $"Found [RESUME] stub for '{methodName}'."
            : $"Expected [RESUME] stub for '{methodName}' not found. " +
              $"Resume methods: [{string.Join(", ", resumeSigs)}]";
        return found;
    }

    /// <summary>
    /// Returns true if every [ASYNC] method with a ResumptionStubEntryPoint fixup is followed
    /// immediately in emitted code by its [RESUME] stub.
    /// </summary>
    public static bool AsyncMethodsWithResumptionStubsAreAdjacent(ReadyToRunReader reader, out string diagnostic)
    {
        var methodsByRva = GetAllMethods(reader)
            .Select(method => (Method: method, EntryPointRva: GetEntryPointRva(method)))
            .Where(entry => entry.EntryPointRva >= 0)
            .OrderBy(entry => entry.EntryPointRva)
            .ToList();

        var failures = new List<string>();
        int checkedMethodCount = 0;
        for (int i = 0; i < methodsByRva.Count; i++)
        {
            ReadyToRunMethod method = methodsByRva[i].Method;
            if (!HasSignaturePrefix(method, "[ASYNC]") ||
                !HasFixupKind(method, ReadyToRunFixupKind.ResumptionStubEntryPoint))
            {
                continue;
            }

            checkedMethodCount++;
            if (i + 1 == methodsByRva.Count)
            {
                failures.Add($"'{method.SignatureString}' at RVA 0x{methodsByRva[i].EntryPointRva:X} is the final method.");
                continue;
            }

            ReadyToRunMethod nextMethod = methodsByRva[i + 1].Method;
            if (!HasSignaturePrefix(nextMethod, "[RESUME]") ||
                StripAsyncMethodPrefix(nextMethod.SignatureString) != StripAsyncMethodPrefix(method.SignatureString))
            {
                failures.Add(
                    $"'{method.SignatureString}' at RVA 0x{methodsByRva[i].EntryPointRva:X} " +
                    $"is followed by '{nextMethod.SignatureString}' at RVA 0x{methodsByRva[i + 1].EntryPointRva:X}.");
            }
        }

        if (checkedMethodCount == 0)
        {
            diagnostic = "No [ASYNC] methods with ResumptionStubEntryPoint fixups were found.";
            return false;
        }

        if (failures.Count != 0)
        {
            diagnostic =
                $"Expected each [ASYNC] method with a ResumptionStubEntryPoint fixup to be followed by its [RESUME] stub, " +
                $"but found {failures.Count} mismatch(es):\n  {string.Join("\n  ", failures)}";
            return false;
        }

        diagnostic = $"Found {checkedMethodCount} [ASYNC] method(s), each followed by its [RESUME] stub.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains at least one ContinuationLayout fixup.
    /// </summary>
    public static bool HasContinuationLayout(ReadyToRunReader reader, out string diagnostic)
        => HasFixupKind(reader, ReadyToRunFixupKind.ContinuationLayout, out diagnostic);

    /// <summary>
    /// Returns true if a method whose signature contains <paramref name="methodName"/>
    /// has at least one ContinuationLayout fixup.
    /// </summary>
    public static bool HasContinuationLayout(ReadyToRunReader reader, string methodName, out string diagnostic)
        => HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ContinuationLayout, methodName, out diagnostic);

    /// <summary>
    /// Returns true if the R2R image contains at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static bool HasResumptionStubFixup(ReadyToRunReader reader, out string diagnostic)
        => HasFixupKind(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, out diagnostic);

    /// <summary>
    /// Returns true if a method whose signature contains <paramref name="methodName"/>
    /// has at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static bool HasResumptionStubFixup(ReadyToRunReader reader, string methodName, out string diagnostic)
        => HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, methodName, out diagnostic);

    /// <summary>
    /// Returns true if exactly one method whose signature contains <paramref name="methodName"/>
    /// has at least one fixup of <paramref name="kind"/>, and that method has exactly
    /// <paramref name="expectedCount"/> fixups of that kind.
    /// Returns false if no match is found, if more than one method matches the search token, or if
    /// the fixup count differs from <paramref name="expectedCount"/>.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// Useful for ensuring fixups are properly deduplicated.
    /// </summary>
    public static bool HasFixupKindCountOnMethod(ReadyToRunReader reader, ReadyToRunFixupKind kind, string methodName, int expectedCount, out string diagnostic)
    {
        var matchingMethods = new List<(string Signature, int Count)>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;
            if (!method.SignatureString.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                continue;

            int count = 0;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                    count++;
            }

            // Only consider methods that have at least one fixup of this kind so we
            // don't false-fail on co-named thunks that legitimately have none.
            if (count > 0)
                matchingMethods.Add((method.SignatureString, count));
        }

        if (matchingMethods.Count == 0)
        {
            diagnostic = $"No method matching '{methodName}' was found with any '{kind}' fixup.";
            return false;
        }

        if (matchingMethods.Count > 1)
        {
            diagnostic = $"Expected exactly one method matching '{methodName}' with '{kind}' fixup, " +
                $"but found {matchingMethods.Count}: [{string.Join(", ", matchingMethods.Select(m => m.Signature))}]";
            return false;
        }

        var (signature, fixupCount) = matchingMethods[0];
        if (fixupCount != expectedCount)
        {
            diagnostic = $"Expected exactly {expectedCount} '{kind}' fixup(s) on method '{signature}', but found {fixupCount}.";
            return false;
        }

        diagnostic = $"Found exactly {expectedCount} '{kind}' fixup(s) on method '{signature}'.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains at least one fixup of the given kind.
    /// </summary>
    public static bool HasFixupKind(ReadyToRunReader reader, ReadyToRunFixupKind kind, out string diagnostic)
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

        bool found = presentKinds.Contains(kind);
        diagnostic = found
            ? $"Found fixup kind '{kind}'."
            : $"Expected fixup kind '{kind}' not found. " +
              $"Present kinds: [{string.Join(", ", presentKinds)}]";
        return found;
    }

    private static bool HasFixupKind(ReadyToRunMethod method, ReadyToRunFixupKind kind)
    {
        if (method.Fixups is null)
            return false;

        foreach (var cell in method.Fixups)
        {
            if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                return true;
        }

        return false;
    }

    private static bool HasSignaturePrefix(ReadyToRunMethod method, string prefix)
        => method.SignaturePrefixes is not null && method.SignaturePrefixes.Contains(prefix);

    private static int GetEntryPointRva(ReadyToRunMethod method)
    {
        foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
        {
            if (runtimeFunction.Id == method.EntryPointRuntimeFunctionId)
                return runtimeFunction.StartAddress;
        }

        return -1;
    }

    private static string StripAsyncMethodPrefix(string signature)
    {
        const string AsyncPrefix = "[ASYNC] ";
        const string ResumePrefix = "[RESUME] ";
        if (signature.StartsWith(AsyncPrefix, StringComparison.Ordinal))
            return signature.Substring(AsyncPrefix.Length);

        if (signature.StartsWith(ResumePrefix, StringComparison.Ordinal))
            return signature.Substring(ResumePrefix.Length);

        return signature;
    }

    /// <summary>
    /// Returns true if exactly one method whose signature contains <paramref name="methodName"/>
    /// has at least one fixup of the given kind.
    /// Fails if no match is found or if more than one method matches the search token.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// </summary>
    public static bool HasFixupKindOnMethod(ReadyToRunReader reader, ReadyToRunFixupKind kind, string methodName, out string diagnostic)
    {
        var matchingMethods = new List<string>();
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
                    matchingMethods.Add(method.SignatureString);
            }
        }

        if (matchingMethods.Count == 0)
        {
            diagnostic =
                $"Expected fixup kind '{kind}' on method matching '{methodName}', but not found.\n" +
                $"Methods with '{kind}' fixups: [{string.Join(", ", methodsWithFixup)}]";
            return false;
        }

        if (matchingMethods.Count > 1)
        {
            diagnostic =
                $"Expected exactly one method matching '{methodName}' with fixup kind '{kind}', " +
                $"but found {matchingMethods.Count}: [{string.Join(", ", matchingMethods)}]";
            return false;
        }

        diagnostic = $"Found '{kind}' fixup on method matching '{methodName}'.";
        return true;
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

    public void GetSectionData(int relativeVirtualAddress, Action<BlobReader> action) => action(_peReader.GetSectionData(relativeVirtualAddress).GetReader());

    public MetadataReader MetadataReader => _peReader.GetMetadataReader();
}
