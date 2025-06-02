// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

[Flags]
public enum RegMask
{
    EAX = 0x1,
    ECX = 0x2,
    EDX = 0x4,
    EBX = 0x8,
    ESP = 0x10,
    EBP = 0x20,
    ESI = 0x40,
    EDI = 0x80,

    NONE = 0x00,
    RM_ALL = EAX | ECX | EDX | EBX | ESP | EBP | ESI | EDI,
    RM_CALLEE_SAVED = EBP | EBX | ESI | EDI,
    RM_CALLEE_TRASHED = RM_ALL & ~RM_CALLEE_SAVED,
}

public record GCInfo
{
    private readonly Target _target;

    public uint RelativeOffset { get; set; }
    public uint MethodSize { get; set; }
    public InfoHdr Header { get; set; }

    public bool IsInProlog => PrologOffset != unchecked((uint)-1);
    public uint PrologOffset { get; set; } = unchecked((uint)-1);

    public bool IsInEpilog => EpilogOffset != unchecked((uint)-1);
    public uint EpilogOffset { get; set; } = unchecked((uint)-1);

    public bool HasReversePInvoke => Header.RevPInvokeOffset != InfoHdr.INVALID_REV_PINVOKE_OFFSET;

    public uint RawStackSize { get; set; }


    /// <summary>
    /// Count of the callee-saved registers, excluding the frame pointer.
    /// This does not include EBP for EBP-frames and double-aligned-frames.
    /// </summary>
    public uint SavedRegsCountExclFP { get; set; }
    public RegMask SavedRegsMask { get; set; } = RegMask.NONE;

    public NoGcRegionTable NoGCRegions { get; set; } = null!;
    public GcSlotTable SlotTable { get; set; } = null!;

    private Lazy<Dictionary<int, List<BaseGcTransition>>> _transitions = new();
    public Dictionary<int, List<BaseGcTransition>> Transitions => _transitions.Value;

    // Number of bytes of stack space that has been pushed for arguments at the current RelativeOffset.
    private Lazy<uint> _pushedArgSize;
    public uint PushedArgSize => _pushedArgSize.Value;

    public GCInfo(Target target, TargetPointer gcInfoAddress, uint relativeOffset)
    {
        _target = target;

        TargetPointer offset = gcInfoAddress;
        MethodSize = target.GCDecodeUnsigned(ref offset);
        RelativeOffset = relativeOffset;

        Debug.Assert(relativeOffset >= 0);
        Debug.Assert(relativeOffset <= MethodSize);

        Header = InfoHdr.DecodeHeader(target, ref offset, MethodSize);
        uint infoHdrSize = (uint)(offset.Value - gcInfoAddress.Value);

        // Check if we are in the prolog
        if (relativeOffset < Header.PrologSize)
        {
            PrologOffset = relativeOffset;
        }

        // Check if we are in an epilog
        foreach (uint epilogStart in Header.Epilogs)
        {
            if (relativeOffset > epilogStart && relativeOffset < epilogStart + Header.EpilogSize)
            {
                EpilogOffset = relativeOffset - epilogStart;
            }
        }

        // calculate raw stack size
        uint frameDwordCount = Header.FrameSize;
        RawStackSize = frameDwordCount * (uint)target.PointerSize;

        // calculate callee saved regs
        uint savedRegsCount = 0;
        RegMask savedRegs = RegMask.NONE;

        if (Header.EdiSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EDI;
        }
        if (Header.EsiSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.ESI;
        }
        if (Header.EbxSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EBX;
        }
        if (Header.EbpSaved)
        {
            savedRegsCount++;
            savedRegs |= RegMask.EBP;
        }

        SavedRegsCountExclFP = savedRegsCount;
        SavedRegsMask = savedRegs;
        if (Header.EbpFrame || Header.DoubleAlign)
        {
            Debug.Assert(Header.EbpSaved);
            SavedRegsCountExclFP--;
        }

        // NoGCRegions = new NoGcRegionTable(target, Header, ref offset);

        // SlotTable = new GcSlotTable(target, Header, ref offset);

        // Verify the argument table offset is consistent
        // Debug.Assert(!Header.HasArgTabOffset || offset.Value == gcInfoAddress.Value + infoHdrSize + Header.ArgTabOffset);

        // Lazily initialize transitions. These are not present in all Heap dumps, only if they are required for stack walking.
        _transitions = new(() =>
        {
            // calculate the Argument Table pointer
            TargetPointer argTabPtr = gcInfoAddress.Value + infoHdrSize + Header.ArgTabOffset;
            GCArgTable argTable = new(_target, Header, argTabPtr);
            return argTable.Transitions;
        });

        // Lazily initialize the pushed argument size. This relies on the transitions being calculated first.
        _pushedArgSize = new(CalculateDepth);
    }

    private uint CalculateDepth()
    {
        using StreamWriter outputFile = new StreamWriter("C:\\Users\\maxcharlamb\\OneDrive - Microsoft\\Desktop\\out.txt", true);

        int depth = 0;
        outputFile.WriteLine($"depth: {depth}");
        foreach (int offset in Transitions.Keys.OrderBy(i => i))
        {
            if (offset > RelativeOffset)
                break; // calculate only to current offset
            outputFile.WriteLine($"CodeOffset: {offset:x8}");
            foreach (BaseGcTransition gcTransition in Transitions[offset])
            {
                outputFile.WriteLine(gcTransition);

                switch (gcTransition)
                {
                    case GcTransitionRegister gcTransitionRegister:
                        if (gcTransitionRegister.IsLive == Action.PUSH)
                        {
                            depth += gcTransitionRegister.PushCountOrPopSize;
                        }
                        else if (gcTransitionRegister.IsLive == Action.POP)
                        {
                            depth -= gcTransitionRegister.PushCountOrPopSize;
                        }
                        break;
                    case StackDepthTransition stackDepthTransition:
                        depth += stackDepthTransition.StackDepthChange;
                        break;
                    case GcTransitionPointer gcTransitionPointer:
                        if (gcTransitionPointer.Act == Action.PUSH)
                        {
                            // FEATURE_EH_FUNCLETS implies fullArgInfo
                            // when there is fullArgInfo, the current depth is incremented by the number of pushed arguments
                            depth++;
                        }
                        else if (gcTransitionPointer.Act == Action.POP)
                        {
                            depth -= (int)gcTransitionPointer.ArgOffset;
                        }
                        break;
                    case IPtrMask:
                    case GcTransitionCall:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported gc transition type");
                }
            }
            outputFile.WriteLine($"depth: {depth}");
        }

        return (uint)(depth * _target.PointerSize);
    }
}

public class GCInfoDecoder(Target target)
{
    private readonly Target _target = target;

    // DecodeGCHDrInfo in src/coreclr/vm/gc_unwind_x86.inl
    public TargetPointer DecodeGCHeaderInfo(TargetPointer gcInfoAddress, uint relativeOffset)
    {
        TargetPointer offset = gcInfoAddress;
        uint methodSize = _target.GCDecodeUnsigned(ref offset);

        Debug.Assert(relativeOffset >= 0);
        Debug.Assert(relativeOffset <= methodSize);

        return TargetPointer.Null;

        // InfoHdr infoHdr = InfoHdr.DecodeHeader(_target, ref offset, methodSize);
        // uint infoHdrSize = (uint)(offset.Value - gcInfoAddress.Value);

        // gcInfo = new GCInfo
        // {
        //     RelativeOffset = relativeOffset,
        //     MethodSize = methodSize,
        //     Header = infoHdr,
        // };

        // // Check if we are in the prolog
        // if (relativeOffset < infoHdr.PrologSize)
        // {
        //     gcInfo.PrologOffset = relativeOffset;
        // }

        // // Check if we are in an epilog
        // foreach (uint epilogStart in gcInfo.Header.Epilogs)
        // {
        //     if (relativeOffset > epilogStart && relativeOffset < epilogStart + gcInfo.Header.EpilogSize)
        //     {
        //         gcInfo.EpilogOffset = relativeOffset - epilogStart;
        //     }
        // }

        // // calculate raw stack size
        // uint frameDwordCount = infoHdr.FrameSize;
        // gcInfo.RawStackSize = frameDwordCount * (uint)_target.PointerSize;

        // // calculate callee saved regs
        // uint savedRegsCount = 0;
        // RegMask savedRegs = RegMask.NONE;

        // if (infoHdr.EdiSaved)
        // {
        //     savedRegsCount++;
        //     savedRegs |= RegMask.EDI;
        // }
        // if (infoHdr.EsiSaved)
        // {
        //     savedRegsCount++;
        //     savedRegs |= RegMask.ESI;
        // }
        // if (infoHdr.EbxSaved)
        // {
        //     savedRegsCount++;
        //     savedRegs |= RegMask.EBX;
        // }
        // if (infoHdr.EbpSaved)
        // {
        //     savedRegsCount++;
        //     savedRegs |= RegMask.EBP;
        // }

        // gcInfo.SavedRegsCountExclFP = savedRegsCount;
        // gcInfo.SavedRegsMask = savedRegs;
        // if (infoHdr.EbpFrame || infoHdr.DoubleAlign)
        // {
        //     Debug.Assert(infoHdr.EbpSaved);
        //     gcInfo.SavedRegsCountExclFP--;
        // }

        // gcInfo.NoGCRegions = new NoGcRegionTable(_target, infoHdr, ref offset);

        // gcInfo.SlotTable = new GcSlotTable(_target, infoHdr, ref offset);

        // return TargetPointer.Null;
    }
}
