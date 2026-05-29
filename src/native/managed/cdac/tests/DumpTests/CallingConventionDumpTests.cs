// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Diagnostics.DataContractReader.TestInfrastructure.TestHelpers;

using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Token values from CORCOMPILE_GCREFMAP_TOKENS (corcompile.h).
/// Duplicated here because the canonical type in Contracts is internal.
/// </summary>
internal enum GCRefMapToken
{
    Skip = 0,
    Ref = 1,
    Interior = 2,
    MethodParam = 3,
    TypeParam = 4,
    VASigCookie = 5,
}

/// <summary>
/// Dump-based integration tests that validate <see cref="ICallingConvention.EnumerateArguments"/>
/// against the precomputed GCRefMap in R2R images.
///
/// Strategy: walk all threads' stacks to collect MethodDescs, find each method's R2R
/// entry point, look up its GCRefMap from the import section, then compare the GCRefMap
/// tokens against the output of EnumerateArguments.
/// </summary>
public class CallingConventionDumpTests : DumpTestBase
{
    private readonly ITestOutputHelper _output;

    public CallingConventionDumpTests(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override string DebuggeeName => "StackRefs";

    // Import section layout constants (matches READYTORUN_IMPORT_SECTION in readytorun.h)
    private const int ImportSectionSize = 20;
    private const int SectionVAOffset = 0;
    private const int SectionSizeOffset = 4;
    private const int EntrySizeOffset = 11;
    private const int AuxiliaryDataOffset = 16;

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "CallingConvention contract requires net11.0+")]
    [SkipOnArch("x86", "GCRefMap position computation differs on x86")]
    public void EnumerateArguments_MatchesGCRefMap_ForR2RMethods(TestConfiguration config)
    {
        if (config.R2RMode != "r2r")
            throw new SkipTestException("GCRefMap comparison only applies to R2R dumps");

        InitializeDumpTest(config, "StackRefs", "full");

        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IExecutionManager execMgr = Target.Contracts.ExecutionManager;

        ICallingConvention cc;
        try
        {
            cc = Target.Contracts.CallingConvention;
        }
        catch (NotImplementedException)
        {
            throw new SkipTestException("CallingConvention contract not supported by this runtime");
        }

        int firstGCRefMapSlotOffset = GetFirstGCRefMapSlotOffset();
        int pointerSize = Target.PointerSize;

        // Collect unique MethodDescs from all thread stacks
        HashSet<TargetPointer> methodDescs = CollectMethodDescsFromStacks();
        _output.WriteLine($"Collected {methodDescs.Count} unique MethodDescs from stack walk");

        int totalCompared = 0;
        int totalSkipped = 0;
        List<string> mismatches = [];

        foreach (TargetPointer mdPtr in methodDescs)
        {
            MethodDescHandle mdh;
            string? methodName = null;
            try
            {
                mdh = rts.GetMethodDescHandle(mdPtr);
                methodName = DumpTestHelpers.GetMethodName(Target, mdh);
            }
            catch
            {
                totalSkipped++;
                continue;
            }

            // Get the method's native code entry point
            TargetCodePointer nativeCode;
            try
            {
                nativeCode = rts.GetNativeCode(mdh);
                if (nativeCode == TargetCodePointer.Null)
                {
                    totalSkipped++;
                    continue;
                }
            }
            catch
            {
                totalSkipped++;
                continue;
            }

            // Find the R2R module for this entry point
            TargetPointer r2rModule;
            try
            {
                r2rModule = execMgr.FindReadyToRunModule(nativeCode.AsTargetPointer);
                if (r2rModule == TargetPointer.Null)
                {
                    totalSkipped++;
                    continue;
                }
            }
            catch
            {
                totalSkipped++;
                continue;
            }

            // Find the GCRefMap for this method via import section scan
            TargetPointer gcRefMapBlob = FindGCRefMapForMethod(r2rModule, nativeCode);
            if (gcRefMapBlob == TargetPointer.Null)
            {
                totalSkipped++;
                continue;
            }

            // Decode GCRefMap (crossgen2's ground truth)
            List<(int Pos, GCRefMapToken Token)> expected = DecodeGCRefMapTokens(gcRefMapBlob);

            // Call EnumerateArguments and convert to GCRefMap tokens
            List<(int Pos, GCRefMapToken Token)> actual;
            try
            {
                actual = ConvertArgumentsToGCRefMapTokens(mdh, firstGCRefMapSlotOffset, pointerSize);
            }
            catch (NotImplementedException)
            {
                totalSkipped++;
                continue;
            }
            catch (System.Exception ex)
            {
                totalSkipped++;
                _output.WriteLine($"  [SKIP] {methodName}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            // Compare: filter out Skip tokens (they're implicit gaps)
            List<(int Pos, GCRefMapToken Token)> expectedFiltered = FilterGCTokens(expected);
            List<(int Pos, GCRefMapToken Token)> actualFiltered = FilterGCTokens(actual);

            if (!TokenListsMatch(expectedFiltered, actualFiltered))
            {
                string name = methodName ?? $"MethodDesc@0x{mdPtr.Value:X}";
                string msg = $"MISMATCH: {name}\n" +
                             $"  GCRefMap:  [{FormatTokens(expectedFiltered)}]\n" +
                             $"  EnumArgs:  [{FormatTokens(actualFiltered)}]";
                mismatches.Add(msg);
                _output.WriteLine(msg);
            }
            else
            {
                _output.WriteLine($"  [MATCH] {methodName}: {FormatTokens(expectedFiltered)}");
            }

            totalCompared++;
        }

        _output.WriteLine($"Compared: {totalCompared}, Skipped: {totalSkipped}, Mismatches: {mismatches.Count}");

        Assert.True(totalCompared > 0,
            $"Expected to compare at least 1 method against GCRefMap, but compared {totalCompared} (skipped {totalSkipped})");

        Assert.True(mismatches.Count == 0,
            $"{mismatches.Count} method(s) had GCRefMap mismatches:\n{string.Join("\n", mismatches)}");
    }

    /// <summary>
    /// Walk all threads' stacks and collect unique MethodDesc pointers.
    /// </summary>
    private HashSet<TargetPointer> CollectMethodDescsFromStacks()
    {
        HashSet<TargetPointer> methodDescs = [];

        IThread threadContract = Target.Contracts.Thread;
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IExecutionManager execMgr = Target.Contracts.ExecutionManager;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        TargetPointer currentThreadPtr = storeData.FirstThread;

        while (currentThreadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThreadPtr);
            try
            {
                foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
                {
                    // Get MethodDesc from the frame
                    TargetPointer frameMD = stackWalk.GetMethodDescPtr(frame);
                    if (frameMD != TargetPointer.Null)
                        methodDescs.Add(frameMD);

                    // Also try resolving from the instruction pointer
                    TargetCodePointer ip = stackWalk.GetInstructionPointer(frame);
                    if (ip != TargetCodePointer.Null)
                    {
                        try
                        {
                            CodeBlockHandle? codeBlock = execMgr.GetCodeBlockHandle(ip);
                            if (codeBlock is not null)
                            {
                                TargetPointer md = execMgr.GetMethodDesc(codeBlock.Value);
                                if (md != TargetPointer.Null)
                                    methodDescs.Add(md);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            currentThreadPtr = threadData.NextThread;
        }

        return methodDescs;
    }

    private int GetFirstGCRefMapSlotOffset()
    {
        Target.TypeInfo tbType = Target.GetTypeInfo(DataType.TransitionBlock);
        return tbType.Fields["FirstGCRefMapSlot"].Offset;
    }

    /// <summary>
    /// Find the GCRefMap for a method by scanning the R2R module's import sections
    /// for an entry whose fixed-up slot value matches the method's entry point.
    /// </summary>
    private TargetPointer FindGCRefMapForMethod(TargetPointer modulePtr, TargetCodePointer nativeCode)
    {
        Target.TypeInfo moduleType = Target.GetTypeInfo(DataType.Module);
        TargetPointer r2rInfoPtr = Target.ReadPointer(modulePtr + (ulong)moduleType.Fields["ReadyToRunInfo"].Offset);
        if (r2rInfoPtr == TargetPointer.Null)
            return TargetPointer.Null;

        Target.TypeInfo r2rType = Target.GetTypeInfo(DataType.ReadyToRunInfo);
        uint numImportSections = Target.Read<uint>(r2rInfoPtr + (ulong)r2rType.Fields["NumImportSections"].Offset);
        if (numImportSections == 0)
            return TargetPointer.Null;

        TargetPointer importSections = Target.ReadPointer(r2rInfoPtr + (ulong)r2rType.Fields["ImportSections"].Offset);
        if (importSections == TargetPointer.Null)
            return TargetPointer.Null;

        ulong imageBase = Target.ReadPointer(r2rInfoPtr + (ulong)r2rType.Fields["LoadedImageBase"].Offset).Value;

        // Scan import sections for a slot that contains this entry point
        for (uint si = 0; si < numImportSections; si++)
        {
            TargetPointer sectionAddr = new(importSections.Value + si * ImportSectionSize);
            uint auxDataRva = Target.Read<uint>(sectionAddr + AuxiliaryDataOffset);
            if (auxDataRva == 0)
                continue;

            uint sectionVA = Target.Read<uint>(sectionAddr + SectionVAOffset);
            uint sectionSize = Target.Read<uint>(sectionAddr + SectionSizeOffset);
            byte entrySize = Target.Read<byte>(sectionAddr + EntrySizeOffset);
            if (entrySize == 0)
                continue;

            uint numSlots = sectionSize / entrySize;

            for (uint slotIndex = 0; slotIndex < numSlots; slotIndex++)
            {
                TargetPointer slotAddr = new(imageBase + sectionVA + slotIndex * entrySize);
                try
                {
                    TargetPointer slotValue = Target.ReadPointer(slotAddr);
                    if (slotValue.Value == nativeCode.Value)
                    {
                        return FindGCRefMapForSlot(imageBase, auxDataRva, slotIndex);
                    }
                }
                catch { }
            }
        }

        return TargetPointer.Null;
    }

    private TargetPointer FindGCRefMapForSlot(ulong imageBase, uint auxDataRva, uint slotIndex)
    {
        TargetPointer gcRefMapBase = new(imageBase + auxDataRva);

        const uint GCREFMAP_LOOKUP_STRIDE = 1024;
        uint lookupIndex = slotIndex / GCREFMAP_LOOKUP_STRIDE;
        uint remaining = slotIndex % GCREFMAP_LOOKUP_STRIDE;

        uint lookupOffset = Target.Read<uint>(new TargetPointer(gcRefMapBase.Value + lookupIndex * 4));
        TargetPointer p = new(gcRefMapBase.Value + lookupOffset);

        while (remaining > 0)
        {
            while ((Target.Read<byte>(p) & 0x80) != 0)
                p = new(p.Value + 1);
            p = new(p.Value + 1);
            remaining--;
        }

        return p;
    }

    private List<(int Pos, GCRefMapToken Token)> DecodeGCRefMapTokens(TargetPointer gcRefMapBlob)
    {
        List<(int Pos, GCRefMapToken Token)> tokens = [];
        TargetPointer currentByte = gcRefMapBlob;
        int pendingByte = 0x80;
        int pos = 0;

        int GetBit()
        {
            int x = pendingByte;
            if ((x & 0x80) != 0)
            {
                x = Target.Read<byte>(currentByte);
                currentByte = new TargetPointer(currentByte.Value + 1);
                x |= (x & 0x80) << 7;
            }
            pendingByte = x >> 1;
            return x & 1;
        }

        int GetTwoBit() => GetBit() | (GetBit() << 1);

        int GetInt()
        {
            int result = 0;
            int bit = 0;
            do
            {
                result |= GetBit() << (bit++);
                result |= GetBit() << (bit++);
                result |= GetBit() << (bit++);
            }
            while (GetBit() != 0);
            return result;
        }

        while (pendingByte != 0)
        {
            int curPos = pos;
            int val = GetTwoBit();
            GCRefMapToken token;
            if (val == 3)
            {
                int ext = GetInt();
                if ((ext & 1) == 0)
                {
                    pos += (ext >> 1) + 4;
                    tokens.Add((curPos, GCRefMapToken.Skip));
                    continue;
                }
                else
                {
                    pos++;
                    token = (GCRefMapToken)((ext >> 1) + 3);
                }
            }
            else
            {
                pos++;
                token = (GCRefMapToken)val;
            }
            tokens.Add((curPos, token));
        }

        return tokens;
    }

    private List<(int Pos, GCRefMapToken Token)> ConvertArgumentsToGCRefMapTokens(
        MethodDescHandle mdh, int firstGCRefMapSlotOffset, int pointerSize)
    {
        ICallingConvention cc = Target.Contracts.CallingConvention;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        List<(int Pos, GCRefMapToken Token)> tokens = [];

        foreach (ArgumentLocation arg in cc.EnumerateArguments(mdh))
        {
            int pos = (arg.Offset - firstGCRefMapSlotOffset) / pointerSize;

            if (arg.IsParamType)
            {
                tokens.Add((pos, GCRefMapToken.TypeParam));
                continue;
            }

            if (arg.IsThis)
            {
                tokens.Add((pos, arg.IsValueTypeThis ? GCRefMapToken.Interior : GCRefMapToken.Ref));
                continue;
            }

            switch (arg.ElementType)
            {
                case CorElementType.Class:
                case CorElementType.String:
                case CorElementType.Object:
                case CorElementType.Array:
                case CorElementType.SzArray:
                    tokens.Add((pos, GCRefMapToken.Ref));
                    break;

                case CorElementType.Byref:
                    tokens.Add((pos, GCRefMapToken.Interior));
                    break;

                case CorElementType.ValueType:
                    if (arg.IsPassedByRef)
                    {
                        tokens.Add((pos, GCRefMapToken.Interior));
                    }
                    else
                    {
                        ExpandInlineValueType(rts, arg, firstGCRefMapSlotOffset, pointerSize, tokens);
                    }
                    break;
            }
        }

        return tokens;
    }

    private static void ExpandInlineValueType(
        IRuntimeTypeSystem rts, ArgumentLocation arg,
        int firstGCRefMapSlotOffset, int pointerSize,
        List<(int Pos, GCRefMapToken Token)> tokens)
    {
        TypeHandle th = arg.TypeHandle;
        if (th.IsNull || !rts.ContainsGCPointers(th))
            return;

        foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(th))
        {
            int fieldOffset = (int)seriesOffset - pointerSize;
            int runBytes = (int)seriesSize;

            for (int off = 0; off < runBytes; off += pointerSize)
            {
                int absoluteOffset = arg.Offset + fieldOffset + off;
                int refPos = (absoluteOffset - firstGCRefMapSlotOffset) / pointerSize;
                tokens.Add((refPos, GCRefMapToken.Ref));
            }
        }
    }

    private static List<(int Pos, GCRefMapToken Token)> FilterGCTokens(List<(int Pos, GCRefMapToken Token)> tokens)
    {
        List<(int Pos, GCRefMapToken Token)> filtered = [];
        foreach (var t in tokens)
        {
            if (t.Token != GCRefMapToken.Skip)
                filtered.Add(t);
        }
        return filtered;
    }

    private static bool TokenListsMatch(
        List<(int Pos, GCRefMapToken Token)> a,
        List<(int Pos, GCRefMapToken Token)> b)
    {
        if (a.Count != b.Count)
            return false;

        a.Sort((x, y) => x.Pos.CompareTo(y.Pos));
        b.Sort((x, y) => x.Pos.CompareTo(y.Pos));

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Pos != b[i].Pos || a[i].Token != b[i].Token)
                return false;
        }

        return true;
    }

    private static string FormatTokens(List<(int Pos, GCRefMapToken Token)> tokens)
    {
        if (tokens.Count == 0)
            return "(empty)";

        return string.Join(", ", tokens.ConvertAll(t => $"{t.Token}@{t.Pos}"));
    }
}
