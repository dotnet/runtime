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

internal static class DeferredWasmCallRewriter
{
    private const int BodySizePrefixLength = 5;
    private const int RelocInstructionLength = 6;

    private sealed class Edit(int offset, int length, byte[] bytes, Relocation[] relocations)
    {
        internal readonly int Offset = offset;
        internal readonly int Length = length;
        internal readonly byte[] Bytes = bytes;
        internal readonly Relocation[] Relocations = relocations;
        internal int NewOffset;
    }

    internal static void Rewrite(ReadyToRunCodegenCompilation compilation, MethodWithGCInfo method)
    {
        ObjectNode.ObjectData original = method.GetData(compilation.NodeFactory, relocsOnly: false);
        foreach (Relocation relocation in original.Relocs.AsSpan())
        {
            if (relocation.Target is DeferredWasmCallNode call &&
                !compilation.CanUseDirectCall(call.Method, out _))
            {
                RewriteWithIndirectCalls(compilation, method, original);
                return;
            }
        }

        foreach (ref Relocation relocation in original.Relocs.AsSpan())
        {
            if (relocation.Target is DeferredWasmCallNode call)
            {
                Debug.Assert(relocation.RelocType == RelocType.WASM_FUNCTION_INDEX_LEB);
                bool direct = compilation.CanUseDirectCall(call.Method, out ReadyToRunMethodCompilationInfo info);
                Debug.Assert(direct);
                compilation.SelectDeferredWasmCall(method, call, useDirectCall: true, info.IsEager);
                relocation = new Relocation(relocation.RelocType, relocation.Offset, call.Target);
            }
        }
        method.FinalizeDeferredWasmCode(original, method.FrameInfos);
    }

    private static void RewriteWithIndirectCalls(
        ReadyToRunCodegenCompilation compilation, MethodWithGCInfo method, ObjectNode.ObjectData original)
    {
        NodeFactory factory = compilation.NodeFactory;
        Relocation[] originalRelocations = original.Relocs ?? [];
        FrameInfo[] frames = method.FrameInfos;
        var edits = new List<Edit>();
        var frameIdentities = new Dictionary<MethodWithGCInfo, Stack<int>>();
        var scratchLocals = new Dictionary<int, int>();
        var directTargets = new Dictionary<int, MethodWithGCInfo>();

        foreach (Relocation relocation in originalRelocations)
        {
            if (relocation.RelocType == RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB &&
                relocation.Target is WasmMethodFrameIdentityNode identity)
            {
                if (!frameIdentities.TryGetValue(identity.Method, out Stack<int> offsets))
                {
                    offsets = new Stack<int>();
                    frameIdentities.Add(identity.Method, offsets);
                }
                offsets.Push(relocation.Offset);
            }
            else if (relocation.Target is DeferredWasmCallNode call)
            {
                Debug.Assert(relocation.RelocType == RelocType.WASM_FUNCTION_INDEX_LEB);
                bool direct = compilation.CanUseDirectCall(call.Method, out ReadyToRunMethodCompilationInfo info);
                compilation.SelectDeferredWasmCall(method, call, direct, info.IsEager);
                Debug.Assert(frameIdentities.TryGetValue(call.Target, out Stack<int> pending) && pending.Count != 0);
                int identityOffset = frameIdentities[call.Target].Pop();
                if (direct)
                {
                    directTargets.Add(relocation.Offset, call.Target);
                    continue;
                }

                int frameIndex = 0;
                while (relocation.Offset >= frames[frameIndex].EndOffset)
                {
                    frameIndex++;
                }
                Debug.Assert(identityOffset > frames[frameIndex].StartOffset);
                if (!scratchLocals.TryGetValue(frameIndex, out int scratchLocal))
                {
                    scratchLocal = AddScratchLocal(method, original.Data, frameIndex, edits);
                    scratchLocals.Add(frameIndex, scratchLocal);
                }

                ISymbolNode imageBase = factory.GetWellKnownWasmGlobalSymbol(new Utf8String(WasmWellKnownGlobalSymbolNode.ImageBaseName));
                edits.Add(EncodeEdit(identityOffset - 1, RelocInstructionLength,
                    new WasmLEBConstantReloc(WasmExprKind.GlobalGet, imageBase, RelocType.WASM_GLOBAL_INDEX_LEB),
                    I32.LoadWithRVAOffset(call.Fallback), Local.Tee(scratchLocal)));

                bool tailCall = original.Data[relocation.Offset - 1] == (byte)WasmExprKind.ReturnCall;
                edits.Add(EncodeEdit(relocation.Offset - 1, RelocInstructionLength,
                    Local.Get(scratchLocal), I32.Load(0),
                    new WasmIndirectCallInstruction(
                        tailCall ? WasmExprKind.ReturnCallIndirect : WasmExprKind.CallIndirect,
                        method.GetDeferredWasmCallSignature(relocation.Offset), 0)));
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

            ISymbolNode target = directTargets.TryGetValue(relocation.Offset, out MethodWithGCInfo directTarget)
                ? directTarget : relocation.Target;
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
            newFrames, MapOffset);

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

        // Appending a non-GC local preserves every JIT-assigned index and keeps the PEP
        // live across the null check without loading it twice.
        int parameterCount = frameIndex == 0
            ? WasmLowering.GetSignature(method).FuncType.Params.Types.Length
            : method.GetFuncletKinds()[frameIndex - 1] is FuncletKind.CatchOrFilterHandler or FuncletKind.Filter ? 3 : 2;
        return checked(parameterCount + localCount);
    }

    private static int ReadUnsigned(byte[] code, ref int position)
    {
        int value = checked((int)DwarfHelper.ReadULEB128(code.AsSpan(position), out int count));
        position += count;
        return value;
    }

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
