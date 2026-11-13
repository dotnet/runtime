// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;

namespace ILCompiler;

// #DeferredWasmCallRewriting
//
// Finalize speculative direct calls once callee compilation results and
// direct-call policy are known, without asking the JIT to regenerate code.
// Each candidate is a fixed-width frame-identity constant followed by a
// call or return_call. Its DeferredWasmCallNode retains the compiled target,
// fallback import, and signature needed if the call must become indirect.
//
// If every candidate can remain direct, only retarget the call relocations;
// preserve the original bytes, frame ranges, and debug offsets. Otherwise,
// replace each rejected call's entire bundle with a sequence that loads the
// fallback PEP, passes it as the final argument, and calls its entrypoint
// indirectly. Argument evaluation and receiver null checks remain outside
// the replaced bundle.
//
// The rebuilding path records nonoverlapping Edits in original-code
// coordinates. Sort them, compute their output positions, and copy unchanged
// spans around the replacements. Replace relocations inside edited spans,
// rebase surviving relocations, update frame ranges and body-size prefixes,
// and map raw debug offsets before their final encoding.
//
// scratchLocals assigns one appended i32 PEP temporary per body, including
// funclets. activationImports preserves the first import-time activation
// token for each target, which need not match a surviving call site's token.
// _fixups and _fixupsInitialized lazily seed and reuse a per-method set for
// deduplicating added precode fixups; Rewrite resets them between methods.
//
// Call selection also records activation requirements independently of the
// final code relocations. Rewriting precedes precode-fixup minimization and
// final dependency publication; see #PrecodeFixupMinimization.
internal sealed class DeferredWasmCallRewriter(ReadyToRunCodegenCompilation compilation)
{
    private const int BodySizePrefixLength = 5;
    private const int RelocInstructionLength = 6;
    private const int DeferredCallLength = 2 * RelocInstructionLength;
    private readonly HashSet<ISymbolNode> _fixups = new();
    private bool _fixupsInitialized;

    // A replacement span in original-code coordinates, with relocations relative
    // to Bytes. NewOffset is assigned after sorting and accumulating size changes.
    private sealed class Edit(int offset, int length, byte[] bytes, Relocation[] relocations)
    {
        internal readonly int Offset = offset;
        internal readonly int Length = length;
        internal readonly byte[] Bytes = bytes;
        internal readonly Relocation[] Relocations = relocations;
        internal int NewOffset;
    }

    // Finalize one method's deferred calls and metadata, clearing reusable fixup
    // state even if rewriting fails.
    internal void Rewrite(
        MethodWithGCInfo method, OffsetMapping[] locations = null, NativeVarInfo[] variables = null,
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports = null)
    {
        Debug.Assert(_fixups.Count == 0);
        try
        {
            RewriteCore(method, locations, variables, activationImports);
        }
        finally
        {
            _fixups.Clear();
            _fixupsInitialized = false;
        }
    }

    // Code preparation canonicalizes the direct target's import-time token to its compiled
    // body. Indirect calls retain the fallback's exact invocation context.
    // Keep eager imports reachable and deduplicate added precode entries.
    private void SelectCall(
        MethodWithGCInfo caller, DeferredWasmCallNode call, bool useDirectCall, bool isEager,
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports)
    {
        MethodWithToken activationToken = useDirectCall
            ? (activationImports is null ? call.Fallback : activationImports[call.Target]).MethodWithToken
            : default;
        Import import = useDirectCall
            ? (Import)(isEager
                ? compilation.SymbolNodeFactory.EagerReadyToRunMethodEntry(activationToken)
                : compilation.SymbolNodeFactory.PrecodeReadyToRunMethodEntry(activationToken))
            : call.Fallback;

        if (import.Table.IsEager)
        {
            caller.AddDeferredWasmDependency(import);
        }
        else if (import.EmitPrecode)
        {
            if (!_fixupsInitialized)
            {
                _fixups.UnionWith(caller.Fixups);
                _fixupsInitialized = true;
            }
            if (_fixups.Add(import))
            {
                caller.Fixups.Add(import);
            }
        }
    }

    // Take the relocation-only fast path only if every deferred call can remain
    // direct. Otherwise rebuild the body before changing any call relocation.
    private void RewriteCore(
        MethodWithGCInfo method, OffsetMapping[] locations, NativeVarInfo[] variables,
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports)
    {
        ObjectNode.ObjectData original = method.GetData(compilation.NodeFactory, relocsOnly: false);
        foreach (Relocation relocation in original.Relocs.AsSpan())
        {
            if (relocation.Target is DeferredWasmCallNode call &&
                !compilation.DeferredWasmCalls.CanUseDirectCall(call.Method, out _))
            {
                RewriteWithIndirectCalls(method, original, locations, variables, activationImports);
                return;
            }
        }

        foreach (ref Relocation relocation in original.Relocs.AsSpan())
        {
            if (relocation.Target is DeferredWasmCallNode call)
            {
                Debug.Assert(relocation.RelocType == RelocType.WASM_FUNCTION_INDEX_LEB);
                AssertDeferredCall(original.Data, relocation.Offset);
                bool direct = compilation.DeferredWasmCalls.CanUseDirectCall(call.Method, out DeferredWasmMethodFinalizationInfo info);
                Debug.Assert(direct);
                SelectCall(method, call, useDirectCall: true, info.IsEager, activationImports);
                relocation = new Relocation(relocation.RelocType, relocation.Offset, call.Target);
            }
        }
        method.FinalizeDeferredWasmCode(original, method.FrameInfos, locations, variables);
    }

    // Plan call-bundle and local-declaration replacements against the original
    // body, then apply them together so every relocation, frame boundary, and
    // debug offset uses the same original-to-final coordinate mapping.
    private void RewriteWithIndirectCalls(
        MethodWithGCInfo method, ObjectNode.ObjectData original, OffsetMapping[] locations, NativeVarInfo[] variables,
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports)
    {
        NodeFactory factory = compilation.NodeFactory;
        Relocation[] originalRelocations = original.Relocs ?? [];
        FrameInfo[] frames = method.FrameInfos;
        var edits = new List<Edit>();
        var scratchLocals = new Dictionary<int, int>();

        foreach (Relocation relocation in originalRelocations)
        {
            if (relocation.Target is DeferredWasmCallNode call)
            {
                Debug.Assert(relocation.RelocType == RelocType.WASM_FUNCTION_INDEX_LEB);
                AssertDeferredCall(original.Data, relocation.Offset);
                bool direct = compilation.DeferredWasmCalls.CanUseDirectCall(call.Method, out DeferredWasmMethodFinalizationInfo info);
                SelectCall(method, call, direct, info.IsEager, activationImports);
                if (direct)
                {
                    continue;
                }

                int frameIndex = 0;
                while (relocation.Offset >= frames[frameIndex].EndOffset)
                {
                    frameIndex++;
                }
                int instructionOffset = relocation.Offset - RelocInstructionLength - 1;
                Debug.Assert(instructionOffset > frames[frameIndex].StartOffset);
                if (!scratchLocals.TryGetValue(frameIndex, out int scratchLocal))
                {
                    scratchLocal = AddScratchLocal(method, original.Data, frameIndex, edits);
                    scratchLocals.Add(frameIndex, scratchLocal);
                }

                ISymbolNode imageBase = factory.GetWellKnownWasmGlobalSymbol(new Utf8String(WasmWellKnownGlobalSymbolNode.ImageBaseName));
                bool tailCall = original.Data[relocation.Offset - 1] == (byte)WasmExprKind.ReturnCall;
                edits.Add(EncodeEdit(instructionOffset, DeferredCallLength,
                    new WasmLEBConstantReloc(WasmExprKind.GlobalGet, imageBase, RelocType.WASM_GLOBAL_INDEX_LEB),
                    I32.LoadWithRVAOffset(call.Fallback), Local.Tee(scratchLocal), Local.Get(scratchLocal), I32.Load(0),
                    new WasmIndirectCallInstruction(
                        tailCall ? WasmExprKind.ReturnCallIndirect : WasmExprKind.CallIndirect,
                        call.Signature, 0)));
            }
        }

        edits.Sort(static (left, right) => left.Offset.CompareTo(right.Offset));
        int delta = 0;
        int previousEnd = 0;
        foreach (Edit edit in edits)
        {
            Debug.Assert(edit.Offset >= previousEnd);
            edit.NewOffset = checked(edit.Offset + delta);
            delta = checked(delta + edit.Bytes.Length - edit.Length);
            previousEnd = checked(edit.Offset + edit.Length);
        }

        byte[] code = edits.Count == 0 ? original.Data : new byte[checked(original.Data.Length + delta)];
        var relocations = new List<Relocation>(originalRelocations.Length + edits.Count);
        int oldPosition = 0;
        int newPosition = 0;
        foreach (Edit edit in edits)
        {
            int unchangedLength = edit.Offset - oldPosition;
            original.Data.AsSpan(oldPosition, unchangedLength).CopyTo(code.AsSpan(newPosition));
            edit.Bytes.CopyTo(code, edit.NewOffset);
            foreach (Relocation relocation in edit.Relocations)
            {
                relocations.Add(new Relocation(relocation.RelocType, edit.NewOffset + relocation.Offset, relocation.Target));
            }
            oldPosition = edit.Offset + edit.Length;
            newPosition = edit.NewOffset + edit.Bytes.Length;
        }
        if (edits.Count != 0)
        {
            original.Data.AsSpan(oldPosition).CopyTo(code.AsSpan(newPosition));
        }

        int editIndex = 0;
        foreach (Relocation relocation in originalRelocations)
        {
            while (editIndex < edits.Count && edits[editIndex].Offset + edits[editIndex].Length <= relocation.Offset)
            {
                editIndex++;
            }
            if (editIndex < edits.Count && edits[editIndex].Offset <= relocation.Offset)
            {
                continue;
            }

            ISymbolNode target = relocation.Target is DeferredWasmCallNode call ? call.Target : relocation.Target;
            Debug.Assert(target is not DeferredWasmCallNode);
            relocations.Add(new Relocation(relocation.RelocType, MapOffset(relocation.Offset), target));
        }
        relocations.Sort(static (left, right) => left.Offset.CompareTo(right.Offset));

        var newFrames = new FrameInfo[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            FrameInfo frame = frames[i];
            int start = MapOffset(frame.StartOffset);
            int end = MapOffset(frame.EndOffset);
            newFrames[i] = new FrameInfo(frame.Flags, start, end, frame.BlobData);
            if (edits.Count != 0)
            {
                DwarfHelper.WritePaddedULEB128(code.AsSpan(start, BodySizePrefixLength), checked((ulong)(end - start - BodySizePrefixLength)));
            }
        }

        method.FinalizeDeferredWasmCode(
            new ObjectNode.ObjectData(code, relocations.ToArray(), original.Alignment, original.DefinedSymbols),
            newFrames, locations, variables, MapOffset);

        // Translate an original offset using the last edit at or before it. Bound
        // offsets inside replaced spans by the replacement's length; offsets after
        // a replacement inherit its cumulative size change.
        int MapOffset(int offset)
        {
            int low = 0;
            int high = edits.Count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (edits[middle].Offset <= offset)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }
            if (high < 0)
            {
                return offset;
            }
            Edit edit = edits[high];
            return offset < edit.Offset + edit.Length
                ? edit.NewOffset + Math.Min(offset - edit.Offset, edit.Bytes.Length)
                : checked(offset + edit.NewOffset - edit.Offset + edit.Bytes.Length - edit.Length);
        }
    }

    // Append a non-GC i32 local so fallback calls can reuse the loaded PEP without
    // renumbering JIT locals. Return its index after accounting for the main
    // method's or funclet's parameters.
    private static int AddScratchLocal(MethodWithGCInfo method, byte[] code, int frameIndex, List<Edit> edits)
    {
        int start = method.FrameInfos[frameIndex].StartOffset + BodySizePrefixLength;
        int position = start;
        int groupCount = ReadUnsigned(code, ref position);
        int groupsStart = position;
        int localCount = 0;
        int lastGroupStart = position;
        int lastGroupCount = 0;
        byte lastGroupType = 0;
        for (int i = 0; i < groupCount; i++)
        {
            lastGroupStart = position;
            lastGroupCount = ReadUnsigned(code, ref position);
            localCount = checked(localCount + lastGroupCount);
            lastGroupType = code[position++];
        }

        bool extendLastGroup = groupCount != 0 && lastGroupType == (byte)WasmValueType.I32;
        int newGroupCount = groupCount + (extendLastGroup ? 0 : 1);
        int prefixSize = checked((int)DwarfHelper.SizeOfULEB128((ulong)newGroupCount));
        int copiedLength = (extendLastGroup ? lastGroupStart : position) - groupsStart;
        int appendedCount = extendLastGroup ? checked(lastGroupCount + 1) : 1;
        int appendedSize = checked((int)DwarfHelper.SizeOfULEB128((ulong)appendedCount) + 1);
        byte[] declarations = new byte[checked(prefixSize + copiedLength + appendedSize)];
        DwarfHelper.WriteULEB128(declarations, (ulong)newGroupCount);
        code.AsSpan(groupsStart, copiedLength).CopyTo(declarations.AsSpan(prefixSize));
        DwarfHelper.WriteULEB128(declarations.AsSpan(prefixSize + copiedLength), (ulong)appendedCount);
        declarations[^1] = (byte)WasmValueType.I32;
        edits.Add(new Edit(start, position - start, declarations, []));

        int parameterCount = frameIndex == 0
            ? WasmLowering.GetSignature(method).FuncType.Params.Types.Length
            : method.GetFuncletKinds()[frameIndex - 1] is FuncletKind.CatchOrFilterHandler or FuncletKind.Filter ? 3 : 2;
        return checked(parameterCount + localCount);
    }

    // Decode a count from the locals prefix, leaving the cursor at the next field.
    private static int ReadUnsigned(byte[] code, ref int position)
    {
        int value = checked((int)DwarfHelper.ReadULEB128(code.AsSpan(position), out int count));
        position += count;
        return value;
    }

    // Check the instruction-boundary assumptions needed to replace the identity
    // constant and call as one self-contained bundle.
    [Conditional("DEBUG")]
    private static void AssertDeferredCall(byte[] code, int callOffset)
    {
        Debug.Assert(callOffset >= RelocInstructionLength + 1);
        Debug.Assert(code[callOffset - RelocInstructionLength - 1] == (byte)WasmExprKind.I32Const);
        Debug.Assert(code[callOffset - 1] is (byte)WasmExprKind.Call or (byte)WasmExprKind.ReturnCall);
    }

    // Encode a replacement independently of its final position, keeping its
    // relocations relative to the replacement for later rebasing.
    private static Edit EncodeEdit(int offset, int length, params ReadOnlySpan<WasmExpr> instructions)
    {
        int byteCount = 0;
        int relocationCount = 0;
        foreach (WasmExpr instruction in instructions)
        {
            byteCount = checked(byteCount + instruction.EncodeSize());
            relocationCount = checked(relocationCount + instruction.EncodeRelocationCount());
        }
        byte[] bytes = new byte[byteCount];
        var relocations = new Relocation[relocationCount];
        int position = 0;
        int relocationPosition = 0;
        foreach (WasmExpr instruction in instructions)
        {
            int count = instruction.EncodeRelocations(relocations.AsSpan(relocationPosition));
            for (int i = relocationPosition; i < relocationPosition + count; i++)
            {
                Relocation relocation = relocations[i];
                relocations[i] = new Relocation(relocation.RelocType, position + relocation.Offset, relocation.Target);
            }
            relocationPosition += count;
            position += instruction.Encode(bytes.AsSpan(position));
        }
        return new Edit(offset, length, bytes, relocations);
    }
}
