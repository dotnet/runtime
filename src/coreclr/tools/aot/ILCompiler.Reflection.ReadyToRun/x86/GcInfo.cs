// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.x86
{
    public class GcInfo : BaseGcInfo
    {
        const uint byref_OFFSET_FLAG = 0x1;

        public InfoHdrSmall Header { get; set; }
        public GcSlotTable SlotTable { get; set; }

        public GcInfo() { }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
        /// </summary>
        public GcInfo(byte[] image, int offset)
        {
            Offset = offset;

            CodeLength = (int)NativeReader.DecodeUnsignedGc(image, ref offset);

            Header = InfoHdrDecoder.DecodeHeader(image, ref offset, CodeLength);

            SlotTable = new GcSlotTable(image, Header, ref offset);

            Transitions = new Dictionary<int, List<BaseGcTransition>>();
            if (Header.Interruptible)
            {
                GetTransitionsFullyInterruptible(image, ref offset);
            }
            else if (Header.EbpFrame)
            {
                GetTransitionsEbpFrame(image, ref offset);
            }
            else
            {
                GetTransitionsNoEbp(image, ref offset);
            }

            Size = offset - Offset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    CodeLength: {CodeLength} bytes");
            sb.AppendLine($"    InfoHdr:");
            sb.AppendLine($"{Header}");
            sb.AppendLine($"{SlotTable}");

            sb.AppendLine($"    Size: {Size} bytes");

            return sb.ToString();
        }

        public string GetRegisterName(int registerNumber)
        {
            return ((x86.Registers)registerNumber).ToString();
        }

        private void AddNewTransition(BaseGcTransition transition)
        {
            if (!Transitions.ContainsKey(transition.CodeOffset))
            {
                Transitions[transition.CodeOffset] = new List<BaseGcTransition>();
            }
            Transitions[transition.CodeOffset].Add(transition);
        }

        private void ArgEncoding(byte[] image, ref uint isPop, ref uint argOffs, ref uint argCnt, ref uint curOffs, ref bool isThis, ref bool iptr)
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

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
        /// </summary>
        private void GetTransitionsFullyInterruptible(byte[] image, ref int offset)
        {
            uint argCnt = 0;
            bool isThis = false;
            bool iptr = false;
            uint curOffs = 0;

            while (true)
            {
                uint isPop;
                uint argOffs;
                uint val = image[offset++];

                if ((val & 0x80) == 0)
                {
                    /* A small 'regPtr' encoding */

                    curOffs += val & 0x7;

                    Action isLive = Action.LIVE;
                    if ((val & 0x40) == 0)
                        isLive = Action.DEAD;
                    AddNewTransition(new GcTransitionRegister((int)curOffs, (Registers)((val >> 3) & 7), isLive, isThis, iptr));

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

                    curOffs += (val & 0x07);
                    isPop = (val & 0x40);

                    ArgEncoding(image, ref isPop, ref argOffs, ref argCnt, ref curOffs, ref isThis, ref iptr);

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
                        val = NativeReader.DecodeUnsignedGc(image, ref offset);
                        curOffs += val;
                        break;
                    case 0xF8:
                    case 0xFC:
                        isPop = val & 0x04;
                        argOffs = NativeReader.DecodeUnsignedGc(image, ref offset);
                        ArgEncoding(image, ref isPop, ref argOffs, ref argCnt, ref curOffs, ref isThis, ref iptr);
                        break;
                    case 0xFD:
                        argOffs = NativeReader.DecodeUnsignedGc(image, ref offset);
                        AddNewTransition(new GcTransitionPointer((int)curOffs, argOffs, argCnt, Action.KILL, Header.EbpFrame));
                        break;
                    case 0xF9:
                        argOffs = NativeReader.DecodeUnsignedGc(image, ref offset);
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
        private void GetTransitionsEbpFrame(byte[] image, ref int offset)
        {
            while (true)
            {
                uint argMask = 0, byrefArgMask = 0;
                uint regMask, byrefRegMask = 0;

                uint argCnt = 0;
                int argOffset = offset;
                uint argTabSize;

                uint val, nxt;
                uint curOffs = 0;

                // Get the next byte and check for a 'special' entry
                uint encType = image[offset++];
                GcTransitionCall transition = null;

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
                                Registers reg;
                                if ((val & 0x10) != 0)
                                    reg = Registers.EDI;
                                else if ((val & 0x20) != 0)
                                    reg = Registers.ESI;
                                else if ((val & 0x40) != 0)
                                    reg = Registers.EBX;
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
                            val = image[offset++];
                            regMask = val >> 5;
                            argMask = val & 0x1F;
                        }
                        break;

                    case 0xFD:  // medium encoding
                        argMask = image[offset++];
                        val = image[offset++];
                        argMask |= (val & 0xF0) << 4;
                        nxt = image[offset++];
                        curOffs += (val & 0x0F) + ((nxt & 0x1F) << 4);
                        regMask = nxt >> 5;                   // EBX,ESI,EDI
                        break;

                    case 0xF9:  // medium encoding with byrefs
                        curOffs += image[offset++];
                        val = image[offset++];
                        argMask = val & 0x1F;
                        regMask = val >> 5;
                        val = image[offset++];
                        byrefArgMask = val & 0x1F;
                        byrefRegMask = val >> 5;
                        break;
                    case 0xFE:  // large encoding
                    case 0xFA:  // large encoding with byrefs
                        val = image[offset++];
                        regMask = val & 0x7;
                        byrefRegMask = val >> 4;

                        curOffs += NativeReader.ReadUInt32(image, ref offset);
                        argMask = NativeReader.ReadUInt32(image, ref offset);
                        if (encType == 0xFA) // read byrefArgMask
                        {
                            byrefArgMask = NativeReader.ReadUInt32(image, ref offset);
                        }
                        break;
                    case 0xFB:  // huge encoding
                        val = image[offset++];
                        regMask = val & 0x7;
                        byrefRegMask = val >> 4;
                        curOffs = NativeReader.ReadUInt32(image, ref offset);
                        argCnt = NativeReader.ReadUInt32(image, ref offset);
                        argTabSize = NativeReader.ReadUInt32(image, ref offset);
                        argOffset = offset;
                        offset += (int)argTabSize;
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
                        val = NativeReader.DecodeUnsignedGc(image, ref argOffset);

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
        private void SaveCallTransition(byte[] image, ref int offset, uint val, uint curOffs, uint callRegMask, bool callPndTab, uint callPndTabCnt, uint callPndMask, uint lastSkip, ref uint imask)
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
                    uint pndOffs = NativeReader.DecodeUnsignedGc(image, ref offset);

                    uint stkOffs = val & ~byref_OFFSET_FLAG;
                    uint lowBit = val & byref_OFFSET_FLAG;
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

            imask = lastSkip = 0;
        }

        private void GetTransitionsNoEbp(byte[] image, ref int offset)
        {
            uint curOffs = 0;
            uint lastSkip = 0;
            uint imask = 0;

            for (; ; )
            {
                uint val = image[offset++];

                if ((val & 0x80) == 0)
                {
                    if ((val & 0x40) == 0)
                    {
                        if ((val & 0x20) == 0)
                        {
                            // push    000DDDDD          push one item, 5-bit delta
                            curOffs += val & 0x1F;
                            AddNewTransition(new GcTransitionRegister((int)curOffs, Registers.ESP, Action.PUSH));
                        }
                        else
                        {
                            // push    00100000 [pushCount]     ESP push multiple items
                            uint pushCount = NativeReader.DecodeUnsignedGc(image, ref offset);
                            AddNewTransition(new GcTransitionRegister((int)curOffs, Registers.ESP, Action.PUSH, false, false, (int)pushCount));
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
                            skip = NativeReader.DecodeUnsignedGc(image, ref offset);
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
                                AddNewTransition(new GcTransitionRegister((int)curOffs, Registers.ESP, Action.POP, false, false, (int)popSize));
                            }
                            else
                                lastSkip = skip;
                        }
                    }
                }
                else
                {
                    uint callArgCnt;
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
                            SaveCallTransition(image, ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                            break;

                        case 5:
                            //
                            // call    1101RRRR DDCCCMMM  Call RegMask=RRRR,ArgCnt=CCC,
                            //                        ArgMask=MMM Delta=commonDelta[DD]
                            //
                            callRegMask = val & 0xf;    // EBP,EBX,ESI,EDI
                            val = image[offset++];
                            callPndMask = (val & 0x7);
                            callArgCnt = (val >> 3) & 0x7;
                            lastSkip = CallPattern.callCommonDelta[val >> 6];
                            curOffs += lastSkip;
                            SaveCallTransition(image, ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                            break;
                        case 6:
                            //
                            // call    1110RRRR [ArgCnt] [ArgMask]
                            //                          Call ArgCnt,RegMask=RRR,ArgMask
                            //
                            callRegMask = val & 0xf;    // EBP,EBX,ESI,EDI
                            callArgCnt = NativeReader.DecodeUnsignedGc(image, ref offset);
                            callPndMask = NativeReader.DecodeUnsignedGc(image, ref offset);
                            SaveCallTransition(image, ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                            break;
                        case 7:
                            switch (val & 0x0C)
                            {
                                case 0x00:
                                    //  iptr 11110000 [IPtrMask] Arbitrary Interior Pointer Mask
                                    imask = NativeReader.DecodeUnsignedGc(image, ref offset);
                                    AddNewTransition(new IPtrMask((int)curOffs, imask));
                                    break;

                                case 0x04:
                                    AddNewTransition(new CalleeSavedRegister((int)curOffs, (CalleeSavedRegisters)(val & 0x3)));
                                    break;

                                case 0x08:
                                    val = image[offset++];
                                    callRegMask = val & 0xF;
                                    imask = val >> 4;
                                    lastSkip = NativeReader.ReadUInt32(image, ref offset);
                                    curOffs += lastSkip;
                                    callArgCnt = NativeReader.ReadUInt32(image, ref offset);
                                    callPndTabCnt = NativeReader.ReadUInt32(image, ref offset);
                                    callPndTabSize = NativeReader.ReadUInt32(image, ref offset);
                                    callPndTab = true;
                                    SaveCallTransition(image, ref offset, val, curOffs, callRegMask, callPndTab, callPndTabCnt, callPndMask, lastSkip, ref imask);
                                    break;
                                case 0x0C:
                                    return;
                                default:
                                    throw new BadImageFormatException("Invalid GC encoding");
                            }
                            break;
                    }
                }
            }
        }
    }
}
