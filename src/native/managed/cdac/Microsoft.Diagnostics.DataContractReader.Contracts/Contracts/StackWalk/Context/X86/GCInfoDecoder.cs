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
    private const uint byref_OFFSET_FLAG = 0x1;

    private readonly Target _target;

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

    public NoGcRegionTable NoGCRegions { get; set; } = null!;
    public GcSlotTable SlotTable { get; set; } = null!;
    public Dictionary<int, List<BaseGcTransition>> Transitions { get; set; } = [];

    // Number of bytes of stack space that has been pushed for arguments at the current RelativeOffset.
    public uint PushedArgStackDepth { get; set; }

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

        NoGCRegions = new NoGcRegionTable(target, Header, ref offset);

        SlotTable = new GcSlotTable(target, Header, ref offset);

        // Verify the argument table offset is consistent
        Debug.Assert(!Header.HasArgTabOffset || offset.Value == gcInfoAddress.Value + infoHdrSize + Header.ArgTabOffset);
        Transitions = new Dictionary<int, List<BaseGcTransition>>();
        if (Header.Interruptible)
        {
            GetTransitionsFullyInterruptible(ref offset);
        }
        else if (Header.EbpFrame)
        {
            GetTransitionsEbpFrame(ref offset);
        }
        else
        {
            GetTransitionsNoEbp(ref offset);
        }

        CalculateDepth();
    }

    private void CalculateDepth()
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

        PushedArgStackDepth = (uint)(depth * _target.PointerSize);
    }

    private void AddNewTransition(BaseGcTransition transition)
    {
        if (!Transitions.TryGetValue(transition.CodeOffset, out List<BaseGcTransition>? value))
        {
            value = [];
            Transitions[transition.CodeOffset] = value;
        }

        value.Add(transition);
    }

    private void ArgEncoding(ref uint isPop, ref uint argOffs, ref uint argCnt, ref uint curOffs, ref bool isThis, ref bool iptr)
    {
        if (isPop != 0)
        {
            // A Pop of 0, means little-delta

            if (argOffs != 0)
            {
                AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt - argOffs, Action.POP, Header.EbpFrame));
            }
        }
        else
        {
            AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argOffs + 1, Action.PUSH, Header.EbpFrame, isThis, iptr));
            isThis = false;
            iptr = false;
        }
    }

    private static RegMask ThreeBitEncodingToRegMask(byte val) =>
        (val & 0x7) switch
        {
            0x0 => RegMask.EAX,
            0x1 => RegMask.ECX,
            0x2 => RegMask.EDX,
            0x3 => RegMask.EBX,
            0x4 => RegMask.ESP,
            0x5 => RegMask.EBP,
            0x6 => RegMask.ESI,
            0x7 => RegMask.EDI,
            _ => throw new ArgumentOutOfRangeException(nameof(val), $"Not expected register value: {val}"),
        };

    private static RegMask TwoBitEncodingToRegMask(byte val) =>
        (val & 0x3) switch
        {
            0x0 => RegMask.EDI,
            0x1 => RegMask.ESI,
            0x2 => RegMask.EBX,
            0x3 => RegMask.EBP,
            _ => throw new ArgumentOutOfRangeException(nameof(val), $"Not expected register value: {val}"),
        };

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
    /// </summary>
    private void GetTransitionsFullyInterruptible(ref TargetPointer offset)
    {
        uint argCnt = 0;
        bool isThis = false;
        bool iptr = false;
        uint curOffs = 0;

        while (true)
        {
            uint isPop;
            uint argOffs;
            uint val = _target.Read<byte>(offset++);

            if ((val & 0x80) == 0)
            {
                /* A small 'regPtr' encoding */

                curOffs += val & 0x7;

                Action isLive = Action.LIVE;
                if ((val & 0x40) == 0)
                    isLive = Action.DEAD;
                AddNewTransition(new GcTransitionRegister((int)curOffs, ThreeBitEncodingToRegMask((byte)((val >> 3) & 7)), isLive, isThis, iptr));

                isThis = false;
                iptr = false;
                continue;
            }

            /* This is probably an argument push/pop */

            argOffs = (val & 0x38) >> 3;

            /* 6 [110] and 7 [111] are reserved for other encodings */

            if (argOffs < 6)
            {
                /* A small argument encoding */

                curOffs += val & 0x07;
                isPop = val & 0x40;

                ArgEncoding(ref isPop, ref argOffs, ref argCnt, ref curOffs, ref isThis, ref iptr);

                continue;
            }
            else if (argOffs == 6)
            {
                if ((val & 0x40) != 0)
                {
                    curOffs += (((val & 0x07) + 1) << 3);
                }
                else
                {
                    // non-ptr arg push

                    curOffs += (val & 0x07);
                    argCnt++;
                    AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt, Action.PUSH, Header.EbpFrame, false, false, false));
                }

                continue;
            }

            // argOffs was 7 [111] which is reserved for the larger encodings
            switch (val)
            {
                case 0xFF:
                    return;
                case 0xBC:
                    isThis = true;
                    break;
                case 0xBF:
                    iptr = true;
                    break;
                case 0xB8:
                    val = _target.GCDecodeUnsigned(ref offset);
                    curOffs += val;
                    break;
                case 0xF8:
                case 0xFC:
                    isPop = val & 0x04;
                    argOffs = _target.GCDecodeUnsigned(ref offset);
                    ArgEncoding(ref isPop, ref argOffs, ref argCnt, ref curOffs, ref isThis, ref iptr);
                    break;
                case 0xFD:
                    argOffs = _target.GCDecodeUnsigned(ref offset);
                    AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt, Action.KILL, Header.EbpFrame));
                    break;
                case 0xF9:
                    argOffs = _target.GCDecodeUnsigned(ref offset);
                    AddNewTransition(new StackDepthTransition((int)curOffs, (int)argOffs));
                    argCnt += argOffs;
                    break;
                default:
                    throw new BadImageFormatException($"Unexpected special code {val}");
            }
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
    /// </summary>
    private void GetTransitionsEbpFrame(ref TargetPointer offset)
    {
        while (true)
        {
            uint argMask = 0, byrefArgMask = 0;
            uint regMask, byrefRegMask = 0;

            uint argCnt = 0;
            TargetPointer argOffset = offset;
            uint argTabSize;

            uint val, nxt;
            uint curOffs = 0;

            // Get the next byte and check for a 'special' entry
            uint encType = _target.Read<byte>(offset++);
            GcTransitionCall? transition;

            switch (encType)
            {
                default:
                    // A tiny or small call entry
                    val = encType;

                    if ((val & 0x80) == 0x00)
                    {
                        if ((val & 0x0F) != 0)
                        {
                            // A tiny call entry

                            curOffs += (val & 0x0F);
                            regMask = (val & 0x70) >> 4;
                            argMask = 0;
                        }
                        else
                        {
                            RegMask reg;
                            if ((val & 0x10) != 0)
                                reg = RegMask.EDI;
                            else if ((val & 0x20) != 0)
                                reg = RegMask.ESI;
                            else if ((val & 0x40) != 0)
                                reg = RegMask.EBX;
                            else
                                throw new BadImageFormatException("Invalid register");
                            transition = new GcTransitionCall((int)curOffs);
                            transition.CallRegisters.Add(new GcTransitionCall.CallRegister(reg, false));
                            AddNewTransition(transition);

                            continue;
                        }
                    }
                    else
                    {
                        // A small call entry
                        curOffs += (val & 0x7F);
                        val = _target.Read<byte>(offset++);
                        regMask = val >> 5;
                        argMask = val & 0x1F;
                    }
                    break;

                case 0xFD:  // medium encoding
                    argMask = _target.Read<byte>(offset++);
                    val = _target.Read<byte>(offset++);
                    argMask |= (val & 0xF0) << 4;
                    nxt = _target.Read<byte>(offset++);
                    curOffs += (val & 0x0F) + ((nxt & 0x1F) << 4);
                    regMask = nxt >> 5;                   // EBX,ESI,EDI
                    break;

                case 0xF9:  // medium encoding with byrefs
                    curOffs += _target.Read<byte>(offset++);
                    val = _target.Read<byte>(offset++);
                    argMask = val & 0x1F;
                    regMask = val >> 5;
                    val = _target.Read<byte>(offset++);
                    byrefArgMask = val & 0x1F;
                    byrefRegMask = val >> 5;
                    break;
                case 0xFE:  // large encoding
                case 0xFA:  // large encoding with byrefs
                    val = _target.Read<byte>(offset++);
                    regMask = val & 0x7;
                    byrefRegMask = val >> 4;

                    curOffs += _target.Read<uint>(offset);
                    offset += 4;
                    argMask = _target.Read<uint>(offset);
                    offset += 4;

                    if (encType == 0xFA) // read byrefArgMask
                    {
                        byrefArgMask = _target.Read<uint>(offset);
                        offset += 4;
                    }
                    break;
                case 0xFB:  // huge encoding
                    val = _target.Read<byte>(offset++);
                    regMask = val & 0x7;
                    byrefRegMask = val >> 4;
                    curOffs = _target.Read<uint>(offset);
                    offset += 4;
                    argCnt = _target.Read<uint>(offset);
                    offset += 4;
                    argTabSize = _target.Read<uint>(offset);
                    offset += 4;
                    argOffset = offset;
                    offset += argTabSize;
                    break;
                case 0xFF:
                    return;
            }

            /*
                Here we have the following values:

                curOffs      ...    the code offset of the call
                regMask      ...    mask of live pointer register variables
                argMask      ...    bitmask of pushed pointer arguments
                byrefRegMask ...    byref qualifier for regMask
                byrefArgMask ...    byrer qualifier for argMask
            */
            transition = new GcTransitionCall((int)curOffs, Header.EbpFrame, regMask, byrefRegMask);
            AddNewTransition(transition);

            if (argCnt != 0)
            {
                do
                {
                    val = _target.GCDecodeUnsigned(ref argOffset);

                    uint stkOffs = val & ~byref_OFFSET_FLAG;
                    uint lowBit = val & byref_OFFSET_FLAG;
                    transition.PtrArgs.Add(new GcTransitionCall.PtrArg(stkOffs, lowBit));
                }
                while (--argCnt > 0);
            }
            else
            {
                transition.ArgMask = argMask;
                if (byrefArgMask != 0)
                    transition.IArgs = byrefArgMask;
            }
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
    /// </summary>
    private void SaveCallTransition(ref TargetPointer offset, uint val, uint curOffs, uint callRegMask, bool callPndTab, uint callPndTabCnt, uint callPndMask, uint lastSkip, ref uint imask)
    {
        uint iregMask, iargMask;
        iregMask = imask & 0xF;
        iargMask = imask >> 4;

        GcTransitionCall transition = new GcTransitionCall((int)curOffs, Header.EbpFrame, callRegMask, iregMask);
        AddNewTransition(transition);

        if (callPndTab)
        {
            for (int i = 0; i < callPndTabCnt; i++)
            {
                uint pndOffs = _target.GCDecodeUnsigned(ref offset);

                uint stkOffs = val & ~byref_OFFSET_FLAG;
                uint lowBit = val & byref_OFFSET_FLAG;
                Console.WriteLine($"stkOffs: {stkOffs}, lowBit: {lowBit}");

                transition.PtrArgs.Add(new GcTransitionCall.PtrArg(pndOffs, 0));
            }
        }
        else
        {
            if (callPndMask != 0)
                transition.ArgMask = callPndMask;
            if (iargMask != 0)
                transition.IArgs = iargMask;
        }

        Console.WriteLine($"lastSkip: {lastSkip}");
        imask /* = lastSkip  */ = 0;
    }

    private void GetTransitionsNoEbp(ref TargetPointer offset)
    {
        uint curOffs = 0;
        uint lastSkip = 0;
        uint imask = 0;

        for (; ; )
        {
            uint val = _target.Read<byte>(offset++);

            if ((val & 0x80) == 0)
            {
                if ((val & 0x40) == 0)
                {
                    if ((val & 0x20) == 0)
                    {
                        // push    000DDDDD          push one item, 5-bit delta
                        curOffs += val & 0x1F;
                        AddNewTransition(new GcTransitionRegister((int)curOffs, RegMask.ESP, Action.PUSH));
                    }
                    else
                    {
                        // push    00100000 [pushCount]     ESP push multiple items
                        uint pushCount = _target.GCDecodeUnsigned(ref offset);
                        AddNewTransition(new GcTransitionRegister((int)curOffs, RegMask.ESP, Action.PUSH, false, false, (int)pushCount));
                    }
                }
                else
                {
                    uint popSize;
                    uint skip;

                    if ((val & 0x3f) == 0)
                    {
                        //
                        // skip    01000000 [Delta]  Skip arbitrary sized delta
                        //
                        skip = _target.GCDecodeUnsigned(ref offset);
                        curOffs += skip;
                        lastSkip = skip;
                    }
                    else
                    {
                        //  pop     01CCDDDD         pop CC items, 4-bit delta
                        popSize = (val & 0x30) >> 4;
                        skip = val & 0x0f;
                        curOffs += skip;

                        if (popSize > 0)
                        {
                            AddNewTransition(new GcTransitionRegister((int)curOffs, RegMask.ESP, Action.POP, false, false, (int)popSize));
                        }
                        else
                            lastSkip = skip;
                    }
                }
            }
            else
            {
                uint callArgCnt = 0;
                uint callRegMask;
                bool callPndTab = false;
                uint callPndMask = 0;
                uint callPndTabCnt = 0, callPndTabSize = 0;

                switch ((val & 0x70) >> 4)
                {
                    default:
                        //
                        // call    1PPPPPPP          Call Pattern, P=[0..79]
                        //
                        CallPattern.DecodeCallPattern((val & 0x7f), out callArgCnt, out callRegMask, out callPndMask, out lastSkip);
                        curOffs += lastSkip;
                        SaveCallTransition(ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                        AddNewTransition(new StackDepthTransition((int)curOffs, (int)callArgCnt));
                        break;

                    case 5:
                        //
                        // call    1101RRRR DDCCCMMM  Call RegMask=RRRR,ArgCnt=CCC,
                        //                        ArgMask=MMM Delta=commonDelta[DD]
                        //
                        callRegMask = val & 0xf;    // EBP,EBX,ESI,EDI
                        val = _target.Read<byte>(offset++);
                        callPndMask = val & 0x7;
                        callArgCnt = (val >> 3) & 0x7;
                        lastSkip = CallPattern.callCommonDelta[val >> 6];
                        curOffs += lastSkip;
                        SaveCallTransition(ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                        AddNewTransition(new StackDepthTransition((int)curOffs, (int)callArgCnt));
                        break;
                    case 6:
                        //
                        // call    1110RRRR [ArgCnt] [ArgMask]
                        //                          Call ArgCnt,RegMask=RRR,ArgMask
                        //
                        callRegMask = val & 0xf;    // EBP,EBX,ESI,EDI
                        callArgCnt = _target.GCDecodeUnsigned(ref offset);
                        callPndMask = _target.GCDecodeUnsigned(ref offset);
                        SaveCallTransition(ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                        AddNewTransition(new StackDepthTransition((int)curOffs, (int)callArgCnt));
                        break;
                    case 7:
                        switch (val & 0x0C)
                        {
                            case 0x00:
                                //  iptr 11110000 [IPtrMask] Arbitrary Interior Pointer Mask
                                imask = _target.GCDecodeUnsigned(ref offset);
                                AddNewTransition(new IPtrMask((int)curOffs, imask));
                                break;

                            case 0x04:
                                AddNewTransition(new CalleeSavedRegister((int)curOffs, TwoBitEncodingToRegMask((byte)(val & 0x3))));
                                break;

                            case 0x08:
                                val = _target.Read<byte>(offset++);
                                callRegMask = val & 0xF;
                                imask = val >> 4;
                                lastSkip = _target.Read<uint>(offset);
                                offset += 4;
                                curOffs += lastSkip;
                                callArgCnt = _target.Read<uint>(offset);
                                offset += 4;
                                callPndTabCnt = _target.Read<uint>(offset);
                                offset += 4;
                                callPndTabSize = _target.Read<uint>(offset);
                                offset += 4;
                                callPndTab = true;
                                SaveCallTransition(ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                                AddNewTransition(new StackDepthTransition((int)curOffs, (int)callArgCnt));
                                break;
                            case 0x0C:
                                return;
                            default:
                                throw new BadImageFormatException("Invalid GC encoding");
                        }
                        break;
                }
                Console.WriteLine($"CallArgCount: {callArgCnt}");
                Console.WriteLine($"CallPndTabCnt: {callPndTabSize}");
            }
        }
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
