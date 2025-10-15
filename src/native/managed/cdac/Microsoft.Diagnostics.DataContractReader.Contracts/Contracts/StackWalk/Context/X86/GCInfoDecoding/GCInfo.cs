// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

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
    private const uint MINIMUM_SUPPORTED_GCINFO_VERSION = 4;
    private const uint MAXIMUM_SUPPORTED_GCINFO_VERSION = 4;

    private readonly Target _target;

    private readonly TargetPointer _gcInfoAddress;
    private readonly uint _infoHdrSize;

    public uint RelativeOffset { get; set; }
    public uint MethodSize { get; set; }
    public InfoHdr Header { get; set; }

    public bool IsInProlog => PrologOffset != unchecked((uint)-1);
    public uint PrologOffset { get; set; } = unchecked((uint)-1);

    public bool IsInEpilog => EpilogOffset != unchecked((uint)-1);
    public uint EpilogOffset { get; set; } = unchecked((uint)-1);

    public uint RawStackSize { get; set; }


    /// <summary>
    /// Count of the callee-saved registers, excluding the frame pointer.
    /// This does not include EBP for EBP-frames and double-aligned-frames.
    /// </summary>
    public uint SavedRegsCountExclFP { get; set; }
    public RegMask SavedRegsMask { get; set; } = RegMask.NONE;

    /// <summary>
    /// GC Transitions indexed by their relative offset.
    /// </summary>
    public ImmutableDictionary<int, List<BaseGcTransition>> Transitions => _transitions.Value;
    private readonly Lazy<ImmutableDictionary<int, List<BaseGcTransition>>> _transitions = new();

    /// <summary>
    /// Number of bytes of stack space that has been pushed for arguments at the current RelativeOffset.
    /// </summary>
    public uint PushedArgSize => _pushedArgSize.Value;
    private readonly Lazy<uint> _pushedArgSize;

    public GCInfo(Target target, TargetPointer gcInfoAddress, uint gcInfoVersion, uint relativeOffset)
    {
        if (gcInfoVersion < MINIMUM_SUPPORTED_GCINFO_VERSION)
        {
            throw new NotSupportedException($"GCInfo version {gcInfoVersion} is not supported. Minimum supported version is {MINIMUM_SUPPORTED_GCINFO_VERSION}.");
        }
        if (gcInfoVersion > MAXIMUM_SUPPORTED_GCINFO_VERSION)
        {
            throw new NotSupportedException($"GCInfo version {gcInfoVersion} is not supported. Maximum supported version is {MAXIMUM_SUPPORTED_GCINFO_VERSION}.");
        }

        _target = target;

        _gcInfoAddress = gcInfoAddress;
        TargetPointer offset = gcInfoAddress;
        MethodSize = target.GCDecodeUnsigned(ref offset);
        RelativeOffset = relativeOffset;

        Debug.Assert(relativeOffset >= 0);
        Debug.Assert(relativeOffset <= MethodSize);

        Header = InfoHdr.DecodeHeader(target, ref offset, MethodSize);
        _infoHdrSize = (uint)(offset.Value - gcInfoAddress.Value);

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

        // Calculate raw stack size
        uint frameDwordCount = Header.FrameSize;
        RawStackSize = frameDwordCount * (uint)target.PointerSize;

        // Calculate callee saved regs
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

        // Lazily decode GC transitions. These values are not present in all Heap dumps. Only when they are required for stack walking.
        // Therefore, we can only read them when they are used by the stack walker.
        _transitions = new(DecodeTransitions);

        // Lazily calculate the pushed argument size. This forces the transitions to be decoded.
        _pushedArgSize = new(CalculatePushedArgSize);
    }

    private ImmutableDictionary<int, List<BaseGcTransition>> DecodeTransitions()
    {
        TargetPointer argTabPtr;
        if (Header.HasArgTabOffset)
        {
            // The GCInfo has an explicit argument table offset
            argTabPtr = _gcInfoAddress + _infoHdrSize + Header.ArgTabOffset;
        }
        else
        {
            // The GCInfo does not have an explicit argument table offset, we need to calculate it
            // from the end of the header. The argument table is located after
            // the NoGCRegions table, the UntrackedVariable table, and the FrameVariableLifetime table.
            argTabPtr = _gcInfoAddress + _infoHdrSize;

            /* Skip over the no GC regions table */
            for (int i = 0; i < Header.NoGCRegionCount; i++)
            {
                // The NoGCRegion table has a variable size, each entry is 2 unsigned integers.
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
            }

            /* Skip over the untracked frame variable table */
            for (int i = 0; i < Header.UntrackedCount; i++)
            {
                // The UntrackedVariable table has a variable size, each entry is 1 signed integer.
                _target.GCDecodeSigned(ref argTabPtr);
            }

            /* Skip over the frame variable lifetime table */
            for (int i = 0; i < Header.VarPtrTableSize; i++)
            {
                // The FrameVariableLifetime table has a variable size, each entry is 3 unsigned integer.
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
                _target.GCDecodeUnsigned(ref argTabPtr);
            }
        }
        GCArgTable argTable = new(_target, Header, argTabPtr);
        return argTable.Transitions.ToImmutableDictionary();
    }

    private uint CalculatePushedArgSize()
    {
        int depth = 0;
        foreach (int offset in Transitions.Keys.OrderBy(i => i))
        {
            if (offset > RelativeOffset)
                break; // calculate only to current offset
            foreach (BaseGcTransition gcTransition in Transitions[offset])
            {
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
        }

        return (uint)(depth * _target.PointerSize);
    }
}
