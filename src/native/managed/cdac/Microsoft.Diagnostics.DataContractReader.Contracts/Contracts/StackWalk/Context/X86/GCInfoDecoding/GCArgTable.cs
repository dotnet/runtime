// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

public class GCArgTable
{
    private const uint byref_OFFSET_FLAG = 0x1;

    private readonly Target _target;
    private readonly InfoHdr _header;

    public Dictionary<int, List<BaseGcTransition>> Transitions { get; private set; } = [];

    public GCArgTable(Target target, InfoHdr header, TargetPointer argTablePtr)
    {
        _target = target;
        _header = header;

        TargetPointer offset = argTablePtr;
        if (header.Interruptible)
        {
            GetTransitionsFullyInterruptible(ref offset);
        }
        else if (_header.EbpFrame)
        {
            GetTransitionsEbpFrame(ref offset);
        }
        else
        {
            GetTransitionsNoEbp(ref offset);
        }
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
                AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt - argOffs, Action.POP, _header.EbpFrame));
            }
        }
        else
        {
            AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argOffs + 1, Action.PUSH, _header.EbpFrame, isThis, iptr));
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
                    AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt, Action.PUSH, _header.EbpFrame, false, false, false));
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
                    AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt, Action.KILL, _header.EbpFrame));
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
            transition = new GcTransitionCall((int)curOffs, _header.EbpFrame, regMask, byrefRegMask);
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

        GcTransitionCall transition = new GcTransitionCall((int)curOffs, _header.EbpFrame, callRegMask, iregMask);
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
                        lastSkip = CallPattern.CallCommonDelta[(int)(val >> 6)];
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
