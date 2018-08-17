// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump.x86
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a> InfoHdrSmall
    /// </summary>
    public struct InfoHdrSmall
    {
        private const uint INVALID_GS_COOKIE_OFFSET = 0;
        private const uint INVALID_SYNC_OFFSET = 0;

        public uint PrologSize { get; set; }
        public uint EpilogSize { get; set; }
        public byte EpilogCount { get; set; }
        public bool EpilogAtEnd { get; set; }
        public bool EdiSaved { get; set; } // which callee-saved regs are pushed onto stack
        public bool EsiSaved { get; set; }
        public bool EbxSaved { get; set; }
        public bool EbpSaved { get; set; }
        public bool EbpFrame { get; set; } // locals accessed relative to ebp
        public bool Interruptible { get; set; } // is intr. at all points (except prolog/epilog), not just call-sites
        public bool DoubleAlign { get; set; } // uses double-aligned stack (ebpFrame will be false)
        public bool Security { get; set; } // has slot for security object
        public bool Handlers { get; set; } // has callable handlers
        public bool Localloc { get; set; } // uses localloc
        public bool EditNcontinue { get; set; } // was JITed in EnC mode
        public bool Varargs { get; set; } // function uses varargs calling convention
        public bool ProfCallbacks { get; set; }
        public byte GenericsContext { get; set; }// function reports a generics context parameter is present
        public byte GenericsContextIsMethodDesc { get; set; }
        public ReturnKinds ReturnKind { get; set; } // Available GcInfo v2 onwards, previously undefined 
        public ushort ArgCount { get; set; }
        public uint FrameSize { get; set; }
        public uint UntrackedCnt { get; set; }
        public uint VarPtrTableSize { get; set; }

        public uint GsCookieOffset { get; set; }
        public uint SyncStartOffset { get; set; }
        public uint SyncEndOffset { get; set; }
        public uint RevPInvokeOffset { get; set; }

        public bool HasArgTabOffset { get; set; }
        public uint ArgTabOffset { get; set; }
        [XmlIgnore]
        public List<int> Epilogs { get; set; }

        public InfoHdrSmall(uint prologSize, uint epilogSize, byte epilogCount, byte epilogAtEnd, byte ediSaved, byte esiSaved, byte ebxSaved, byte ebpSaved, byte ebpFrame,
            byte interruptible, byte doubleAlign, byte security, byte handlers, byte localloc, byte editNcontinue, byte varargs, byte profCallbacks,
            byte genericsContext, byte genericsContextIsMethodDesc, byte returnKind, ushort argCount, uint frameSize, uint untrackedCnt, uint varPtrTableSize)
        {
            PrologSize = prologSize;
            EpilogSize = epilogSize;
            EpilogCount = epilogCount;
            EpilogAtEnd = epilogAtEnd == 1;
            EdiSaved = ediSaved == 1;
            EsiSaved = esiSaved == 1;
            EbxSaved = ebxSaved == 1;
            EbpSaved = ebpSaved == 1;
            EbpFrame = ebpFrame == 1;
            Interruptible = interruptible == 1;
            DoubleAlign = doubleAlign == 1;
            Security = security == 1;
            Handlers = handlers == 1;
            Localloc = localloc == 1;
            EditNcontinue = editNcontinue == 1;
            Varargs = varargs == 1;
            ProfCallbacks = profCallbacks == 1;
            GenericsContext = genericsContext;
            GenericsContextIsMethodDesc = genericsContextIsMethodDesc;
            ReturnKind = (ReturnKinds)returnKind;
            ArgCount = argCount;
            FrameSize = frameSize;
            UntrackedCnt = untrackedCnt;
            VarPtrTableSize = varPtrTableSize;

            GsCookieOffset = 0;
            SyncStartOffset = 0;
            SyncEndOffset = 0;
            RevPInvokeOffset = 0;

            HasArgTabOffset = false;
            ArgTabOffset = 0;
            Epilogs = null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\t\tPrologSize: {PrologSize}");
            sb.AppendLine($"\t\tEpilogSize: {EpilogSize}");
            sb.AppendLine($"\t\tEpilogCount: {EpilogCount}");
            sb.Append("\t\tEpilogAtEnd: ");
            sb.AppendLine(EpilogAtEnd ? "yes" : "no");

            sb.Append($"\t\tCallee-saved regs  = ");
            if (EdiSaved) sb.Append("EDI");
            if (EsiSaved) sb.Append("ESI");
            if (EbxSaved) sb.Append("EBX");
            if (EbpSaved) sb.Append("EBP");
            sb.AppendLine();

            sb.Append($"\t\tEbpFrame: ");
            sb.AppendLine(EbpFrame ? "yes" : "no");
            sb.Append($"\t\tFully Interruptible: ");
            sb.AppendLine(Interruptible ? "yes" : "no");
            sb.Append($"\t\tDoubleAlign: ");
            sb.AppendLine(DoubleAlign ? "yes" : "no");
            sb.AppendLine($"\t\tArguments Size: {ArgCount} DWORDs");
            sb.AppendLine($"\t\tStack Frame Size: {FrameSize} DWORDs");
            sb.AppendLine($"\t\tUntrackedCnt: {UntrackedCnt}");
            sb.AppendLine($"\t\tVarPtrTableSize: {VarPtrTableSize}");

            if (Security) sb.AppendLine($"\t\tSecurity Check Obj: yes");
            if (Handlers) sb.AppendLine($"\t\tHandlers: yes");
            if (Localloc) sb.AppendLine($"\t\tLocalloc: yes");
            if (EditNcontinue) sb.AppendLine($"\t\tEditNcontinue: yes");
            if (Varargs) sb.AppendLine($"\t\tVarargs: yes");
            if (ProfCallbacks) sb.AppendLine($"\t\tProfCallbacks: yes");

            sb.AppendLine($"\t\tGenericsContext: {GenericsContext}");
            sb.AppendLine($"\t\tGenericsContextIsMethodDesc: {GenericsContextIsMethodDesc}");
            sb.AppendLine($"\t\tReturnKind: {ReturnKind}");
            sb.AppendLine($"\t\tRevPInvokeOffset: {RevPInvokeOffset}");

            if (GsCookieOffset != INVALID_GS_COOKIE_OFFSET)
            {
                sb.Append("\t\tGuardStack cookie = [");
                sb.Append(EbpFrame ? "EBP-" : "ESP+");
                sb.AppendLine($"{GsCookieOffset}]\n");
            }
            if (SyncStartOffset != INVALID_GS_COOKIE_OFFSET)
            {
                sb.AppendLine($"\t\tSync region = [{SyncStartOffset},{SyncEndOffset}]");
            }

            sb.Append($"\t\tEpilogs:");
            foreach (int epilog in Epilogs)
            {
                sb.AppendLine($" {epilog}");
            }
            if (HasArgTabOffset)
                sb.AppendLine($"\t\tArgTabOffset: {ArgTabOffset}");

            return sb.ToString();
        }
    };

    public class InfoHdrDecoder {

        private const uint HAS_VARPTR = 0xFFFFFFFF;
        private const uint HAS_UNTRACKED = 0xFFFFFFFF;
        private const uint HAS_GS_COOKIE_OFFSET = 0xFFFFFFFF;
        private const uint HAS_SYNC_OFFSET = 0xFFFFFFFF;
        private const uint HAS_REV_PINVOKE_FRAME_OFFSET = 0xFFFFFFFF;
        private const uint YES = HAS_VARPTR;

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a> GetInfoHdr
        /// </summary>
        public static InfoHdrSmall GetInfoHdr(byte encoding)
        {
            return _infoHdrShortcut[encoding];
        }

        /// <summary>
        /// Initialize the GcInfo header
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> DecodeHeader and <a href="https://github.com/dotnet/coreclr/blob/master/src/gcdump/i386/gcdumpx86.cpp">GCDump::DumpInfoHdr</a>
        /// </summary>
        public static InfoHdrSmall DecodeHeader(byte[] image, ref int offset, int codeLength)
        {
            byte nextByte = image[offset++];
            byte encoding = (byte)(nextByte & 0x7f);
            InfoHdrSmall header = GetInfoHdr(encoding);
            while ((nextByte & (uint)InfoHdrAdjustConstants.MORE_BYTES_TO_FOLLOW) != 0)
            {
                nextByte = image[offset++];
                encoding = (byte)(nextByte & (uint)InfoHdrAdjustConstants.ADJ_ENCODING_MAX);

                if (encoding < (uint)InfoHdrAdjust.NEXT_FOUR_START)
                {
                    if (encoding < (uint)InfoHdrAdjust.SET_ARGCOUNT)
                    {
                        header.FrameSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_FRAMESIZE);
                    }
                    else if (encoding < (uint)InfoHdrAdjust.SET_PROLOGSIZE)
                    {
                        header.ArgCount = (byte)(encoding - (uint)InfoHdrAdjust.SET_ARGCOUNT);
                    }
                    else if (encoding < (uint)InfoHdrAdjust.SET_EPILOGSIZE)
                    {
                        header.PrologSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_PROLOGSIZE);
                    }
                    else if (encoding < (uint)InfoHdrAdjust.SET_EPILOGCNT)
                    {
                        header.EpilogSize = (byte)(encoding - (uint)InfoHdrAdjust.SET_EPILOGSIZE);
                    }
                    else if (encoding < (uint)InfoHdrAdjust.SET_UNTRACKED)
                    {
                        header.EpilogCount = (byte)((encoding - (uint)InfoHdrAdjust.SET_EPILOGCNT) / 2);
                        header.EpilogAtEnd = ((encoding - (uint)InfoHdrAdjust.SET_EPILOGCNT) & 1) == 1 ? true : false;
                    }
                    else if (encoding < (uint)InfoHdrAdjust.FIRST_FLIP)
                    {
                        header.UntrackedCnt = (byte)(encoding - (uint)InfoHdrAdjust.SET_UNTRACKED);
                    }
                    else
                    {
                        switch (encoding)
                        {
                            case (byte)InfoHdrAdjust.FLIP_EDI_SAVED:
                                header.EdiSaved = !header.EdiSaved;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_ESI_SAVED:
                                header.EsiSaved = !header.EsiSaved;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_EBX_SAVED:
                                header.EbxSaved = !header.EbxSaved;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_EBP_SAVED:
                                header.EbpSaved = !header.EbpSaved;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_EBP_FRAME:
                                header.EbpFrame = !header.EbpFrame;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_INTERRUPTIBLE:
                                header.Interruptible = !header.Interruptible;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_DOUBLE_ALIGN:
                                header.DoubleAlign = !header.DoubleAlign;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_SECURITY:
                                header.Security = !header.Security;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_HANDLERS:
                                header.Handlers = !header.Handlers;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_LOCALLOC:
                                header.Localloc = !header.Localloc;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_EDITnCONTINUE:
                                header.EditNcontinue = !header.EditNcontinue;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_VAR_PTR_TABLE_SZ:
                                header.VarPtrTableSize ^= HAS_VARPTR;
                                break;
                            case (byte)InfoHdrAdjust.FFFF_UNTRACKED_CNT:
                                header.UntrackedCnt = HAS_UNTRACKED;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_VARARGS:
                                header.Varargs = !header.Varargs;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_PROF_CALLBACKS:
                                header.ProfCallbacks = !header.ProfCallbacks;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_HAS_GENERICS_CONTEXT:
                                header.GenericsContext ^= 1;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_GENERICS_CONTEXT_IS_METHODDESC:
                                header.GenericsContextIsMethodDesc ^= 1;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_HAS_GS_COOKIE:
                                header.GsCookieOffset ^= HAS_GS_COOKIE_OFFSET;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_SYNC:
                                header.SyncStartOffset ^= HAS_SYNC_OFFSET;
                                break;
                            case (byte)InfoHdrAdjust.FLIP_REV_PINVOKE_FRAME:
                                header.RevPInvokeOffset ^= HAS_REV_PINVOKE_FRAME_OFFSET;
                                break;

                            case (byte)InfoHdrAdjust.NEXT_OPCODE:
                                encoding = (byte)(image[offset++] & (int)InfoHdrAdjustConstants.ADJ_ENCODING_MAX);
                                // encoding here always corresponds to codes in InfoHdrAdjust2 set

                                if (encoding < (int)InfoHdrAdjustConstants.SET_RET_KIND_MAX)
                                {
                                    header.ReturnKind = (ReturnKinds)encoding;
                                }
                                else
                                {
                                    throw new BadImageFormatException("Unexpected gcinfo header encoding");
                                }
                                break;
                            default:
                                throw new BadImageFormatException("Unexpected gcinfo header encoding");
                        }
                    }
                }
                else
                {
                    byte lowBits;
                    switch (encoding >> 4)
                    {
                        case 5:
                            lowBits = (byte)(encoding & 0xf);
                            header.FrameSize <<= 4;
                            header.FrameSize += lowBits;
                            break;
                        case 6:
                            lowBits = (byte)(encoding & 0xf);
                            header.ArgCount <<= 4;
                            header.ArgCount += lowBits;
                            break;
                        case 7:
                            if ((encoding & 0x8) == 0)
                            {
                                lowBits = (byte)(encoding & 0x7);
                                header.PrologSize <<= 3;
                                header.PrologSize += lowBits;
                            }
                            else
                            {
                                lowBits = (byte)(encoding & 0x7);
                                header.EpilogSize <<= 3;
                                header.EpilogSize += lowBits;
                            }
                            break;
                        default:
                            throw new BadImageFormatException("Unexpected gcinfo header encoding");
                    }
                }
            }

            if (header.UntrackedCnt == HAS_UNTRACKED)
            {
                header.HasArgTabOffset = true;
                header.UntrackedCnt = NativeReader.DecodeUnsignedGc(image, ref offset);
            }
            if (header.VarPtrTableSize == HAS_VARPTR)
            {
                header.HasArgTabOffset = true;
                header.VarPtrTableSize = NativeReader.DecodeUnsignedGc(image, ref offset);
            }
            if (header.GsCookieOffset == HAS_GS_COOKIE_OFFSET)
            {
                header.GsCookieOffset = NativeReader.DecodeUnsignedGc(image, ref offset);
            }
            if (header.SyncStartOffset == HAS_SYNC_OFFSET)
            {
                header.SyncStartOffset = NativeReader.DecodeUnsignedGc(image, ref offset);
                header.SyncEndOffset = NativeReader.DecodeUnsignedGc(image, ref offset);
            }
            if (header.RevPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET)
            {
                header.RevPInvokeOffset = NativeReader.DecodeUnsignedGc(image, ref offset);
            }

            header.Epilogs = new List<int>();
            if (header.EpilogCount > 1 || (header.EpilogCount != 0 && !header.EpilogAtEnd))
            {
                uint offs = 0;

                for (int i = 0; i < header.EpilogCount; i++)
                {
                    offs = NativeReader.DecodeUDelta(image, ref offset, offs);
                    header.Epilogs.Add((int)offs);
                }
            }
            else
            {
                if (header.EpilogCount != 0)
                    header.Epilogs.Add(codeLength - (int)header.EpilogSize);
            }

            if (header.HasArgTabOffset)
            {
                header.ArgTabOffset = NativeReader.DecodeUnsignedGc(image, ref offset);
            }

            return header;
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> infoHdrShortcut
        /// </summary>
        private static InfoHdrSmall[] _infoHdrShortcut = {
            new InfoHdrSmall(  0,  1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1139  00
            new InfoHdrSmall(  0,  1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //  128738  01
            new InfoHdrSmall(  0,  1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3696  02
            new InfoHdrSmall(  0,  1, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     402  03
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    4259  04
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    3379  05
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //    2058  06
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          ),  //     728  07
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     984  08
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3,  0,  0,  0          ),  //     606  09
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  0,  0          ),  //    1110  0a
            new InfoHdrSmall(  0,  3, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4,  0,  1,  0          ),  //     414  0b
            new InfoHdrSmall(  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1553  0c
            new InfoHdrSmall(  1,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         ),  //     584  0d
            new InfoHdrSmall(  1,  2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    2182  0e
            new InfoHdrSmall(  1,  2, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3445  0f
            new InfoHdrSmall(  1,  2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1369  10
            new InfoHdrSmall(  1,  2, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     515  11
            new InfoHdrSmall(  1,  2, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //   21127  12
            new InfoHdrSmall(  1,  2, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3517  13
            new InfoHdrSmall(  1,  2, 3, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     750  14
            new InfoHdrSmall(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1876  15
            new InfoHdrSmall(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1665  16
            new InfoHdrSmall(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     729  17
            new InfoHdrSmall(  1,  4, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     484  18
            new InfoHdrSmall(  1,  4, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     331  19
            new InfoHdrSmall(  2,  3, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     361  1a
            new InfoHdrSmall(  2,  3, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     964  1b
            new InfoHdrSmall(  2,  3, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3713  1c
            new InfoHdrSmall(  2,  3, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     466  1d
            new InfoHdrSmall(  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1325  1e
            new InfoHdrSmall(  2,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     712  1f
            new InfoHdrSmall(  2,  3, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     588  20
            new InfoHdrSmall(  2,  3, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //   20542  21
            new InfoHdrSmall(  2,  3, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    3802  22
            new InfoHdrSmall(  2,  3, 3, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     798  23
            new InfoHdrSmall(  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1900  24
            new InfoHdrSmall(  2,  5, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     385  25
            new InfoHdrSmall(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1617  26
            new InfoHdrSmall(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1743  27
            new InfoHdrSmall(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     909  28
            new InfoHdrSmall(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  1,  0          ),  //     602  29
            new InfoHdrSmall(  2,  5, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     352  2a
            new InfoHdrSmall(  2,  6, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     657  2b
            new InfoHdrSmall(  2,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //    1283  2c
            new InfoHdrSmall(  2,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1286  2d
            new InfoHdrSmall(  3,  4, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1495  2e
            new InfoHdrSmall(  3,  4, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1989  2f
            new InfoHdrSmall(  3,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1154  30
            new InfoHdrSmall(  3,  4, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    9300  31
            new InfoHdrSmall(  3,  4, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     392  32
            new InfoHdrSmall(  3,  4, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1720  33
            new InfoHdrSmall(  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1246  34
            new InfoHdrSmall(  3,  6, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     800  35
            new InfoHdrSmall(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1179  36
            new InfoHdrSmall(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //    1368  37
            new InfoHdrSmall(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     349  38
            new InfoHdrSmall(  3,  6, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  2,  0          ),  //     505  39
            new InfoHdrSmall(  3,  6, 2, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     629  3a
            new InfoHdrSmall(  3,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  2, YES         ),  //     365  3b
            new InfoHdrSmall(  4,  5, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     487  3c
            new InfoHdrSmall(  4,  5, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    1752  3d
            new InfoHdrSmall(  4,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1959  3e
            new InfoHdrSmall(  4,  5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    2436  3f
            new InfoHdrSmall(  4,  5, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     861  40
            new InfoHdrSmall(  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1459  41
            new InfoHdrSmall(  4,  7, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     950  42
            new InfoHdrSmall(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //    1491  43
            new InfoHdrSmall(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  1,  0          ),  //     879  44
            new InfoHdrSmall(  4,  7, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  0,  0,  0          ),  //     408  45
            new InfoHdrSmall(  5,  4, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //    4870  46
            new InfoHdrSmall(  5,  6, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     359  47
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //     915  48
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          ),  //     412  49
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1288  4a
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //    1591  4b
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0, YES         ),  //     361  4c
            new InfoHdrSmall(  5,  6, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  1,  0,  0          ),  //     623  4d
            new InfoHdrSmall(  5,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  1,  0,  0          ),  //    1239  4e
            new InfoHdrSmall(  6,  0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     457  4f
            new InfoHdrSmall(  6,  0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     606  50
            new InfoHdrSmall(  6,  4, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //    1073  51
            new InfoHdrSmall(  6,  4, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //     508  52
            new InfoHdrSmall(  6,  6, 1, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     330  53
            new InfoHdrSmall(  6,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //    1709  54
            new InfoHdrSmall(  6,  7, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    1164  55
            new InfoHdrSmall(  7,  4, 1, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0,  0          ),  //     556  56
            new InfoHdrSmall(  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //     529  57
            new InfoHdrSmall(  7,  5, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //    1423  58
            new InfoHdrSmall(  7,  8, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //    2455  59
            new InfoHdrSmall(  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //     956  5a
            new InfoHdrSmall(  7,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0, YES         ),  //    1399  5b
            new InfoHdrSmall(  7,  8, 2, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0, YES         ),  //     587  5c
            new InfoHdrSmall(  7, 10, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         ),  //     743  5d
            new InfoHdrSmall(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  0,  0          ),  //    1004  5e
            new InfoHdrSmall(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  2,  1, YES         ),  //     487  5f
            new InfoHdrSmall(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  2,  0,  0          ),  //     337  60
            new InfoHdrSmall(  7, 10, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  3,  0, YES         ),  //     361  61
            new InfoHdrSmall(  8,  3, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     560  62
            new InfoHdrSmall(  8,  6, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    1377  63
            new InfoHdrSmall(  9,  4, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     877  64
            new InfoHdrSmall(  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  0,  0          ),  //    3041  65
            new InfoHdrSmall(  9,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //     349  66
            new InfoHdrSmall( 10,  5, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          ),  //    2061  67
            new InfoHdrSmall( 10,  5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  0          ),  //     577  68
            new InfoHdrSmall( 11,  6, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  4,  1,  0          ),  //    1195  69
            new InfoHdrSmall( 12,  5, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0,  0          ),  //     491  6a
            new InfoHdrSmall( 13,  8, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  9,  0, YES         ),  //     627  6b
            new InfoHdrSmall( 13,  8, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  2,  1,  0          ),  //    1099  6c
            new InfoHdrSmall( 13, 10, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2,  6,  1, YES         ),  //     488  6d
            new InfoHdrSmall( 14,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     574  6e
            new InfoHdrSmall( 16,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //    1281  6f
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0, YES         ),  //    1881  70
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     339  71
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  3,  0,  0          ),  //    2594  72
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0,  0          ),  //     339  73
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //    2107  74
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //    2372  75
            new InfoHdrSmall( 16,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  6,  0, YES         ),  //    1078  76
            new InfoHdrSmall( 16,  7, 2, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  4,  0, YES         ),  //     384  77
            new InfoHdrSmall( 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1,  4,  1, YES         ),  //    1541  78
            new InfoHdrSmall( 16,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2,  4,  1, YES         ),  //     975  79
            new InfoHdrSmall( 19,  7, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //     546  7a
            new InfoHdrSmall( 24,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  5,  0, YES         ),  //     675  7b
            new InfoHdrSmall( 45,  9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,  0,  0,  0          ),  //     902  7c
            new InfoHdrSmall( 51,  7, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13,  0, YES         ),  //     432  7d
            new InfoHdrSmall( 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1,  0, YES         ),  //     361  7e
            new InfoHdrSmall( 51,  7, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11,  0,  0          ),  //     703  7f
        };
    }
}
