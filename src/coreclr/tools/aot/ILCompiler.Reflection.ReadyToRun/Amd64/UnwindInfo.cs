// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.Amd64
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_OP_CODES
    /// </summary>
    public enum UnwindOpCodes
    {
        UWOP_PUSH_NONVOL = 0,
        UWOP_ALLOC_LARGE,
        UWOP_ALLOC_SMALL,
        UWOP_SET_FPREG,
        UWOP_SAVE_NONVOL,
        UWOP_SAVE_NONVOL_FAR,
        UWOP_EPILOG,
        UWOP_SPARE_CODE,
        UWOP_SAVE_XMM128,
        UWOP_SAVE_XMM128_FAR,
        UWOP_PUSH_MACHFRAME,
        UWOP_SET_FPREG_LARGE,
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_OP_CODES
    /// </summary>
    public enum UnwindFlags
    {
        UNW_FLAG_NHANDLER = 0x0,
        UNW_FLAG_EHANDLER = 0x1,
        UNW_FLAG_UHANDLER = 0x2,
        UNW_FLAG_CHAININFO = 0x4,
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_CODE
    /// </summary>
    public class UnwindCode
    {
        public byte CodeOffset { get; set; }
        public UnwindOpCodes UnwindOp { get; set; } //4 bits

        public byte OpInfo { get; set; } //4 bits
        public string OpInfoStr { get; set; } //4 bits

        public byte OffsetLow { get; set; }
        public byte OffsetHigh { get; set; } //4 bits

        public int FrameOffset { get; set; }
        public int NextFrameOffset { get; set; }

        public bool IsOpInfo { get; set; }

        public UnwindCode() { }

        /// <summary>
        /// Unwind code parsing is based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/jit/unwindamd64.cpp">src\jit\unwindamd64.cpp</a> DumpUnwindInfo
        /// </summary>
        public UnwindCode(byte[] image, ref int frameOffset, ref int offset)
        {
            CodeOffset = NativeReader.ReadByte(image, ref offset);
            byte op = NativeReader.ReadByte(image, ref offset);
            UnwindOp = (UnwindOpCodes)(op & 15);
            OpInfo = (byte)(op >> 4);

            OffsetLow = CodeOffset;
            OffsetHigh = OpInfo;

            FrameOffset = frameOffset;

            switch (UnwindOp)
            {
                case UnwindOpCodes.UWOP_PUSH_NONVOL:
                    OpInfoStr = $"{(Registers)OpInfo}({OpInfo})";
                    break;
                case UnwindOpCodes.UWOP_ALLOC_LARGE:
                    OpInfoStr = $"{OpInfo} - ";
                    if (OpInfo == 0)
                    {
                        OpInfoStr += "Scaled small";
                        NextFrameOffset = 8 * NativeReader.ReadUInt16(image, ref offset);
                    }
                    else if (OpInfo == 1)
                    {
                        OpInfoStr += "Unscaled large";
                        uint nextOffset = NativeReader.ReadUInt16(image, ref offset);
                        NextFrameOffset = (int)((uint)(NativeReader.ReadUInt16(image, ref offset) << 16) | nextOffset);
                    }
                    else
                    {
                        throw new BadImageFormatException();
                    }
                    break;
                case UnwindOpCodes.UWOP_ALLOC_SMALL:
                    int opInfo = OpInfo * 8 + 8;
                    OpInfoStr = $"{opInfo}";
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG:
                    OpInfoStr = $"Unused({OpInfo})";
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG_LARGE:
                    {
                        OpInfoStr = $"Unused({OpInfo})";
                        uint nextOffset = NativeReader.ReadUInt16(image, ref offset);
                        nextOffset = ((uint)(NativeReader.ReadUInt16(image, ref offset) << 16) | nextOffset);
                        NextFrameOffset = (int)nextOffset * 16;
                        if ((NextFrameOffset & 0xF0000000) != 0)
                        {
                            throw new BadImageFormatException("Warning: Illegal unwindInfo unscaled offset: too large");
                        }
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL:
                    {
                        OpInfoStr = $"{(Registers)OpInfo}({OpInfo})";
                        NextFrameOffset = NativeReader.ReadUInt16(image, ref offset) * 8;
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL_FAR:
                    {
                        OpInfoStr = $"{(Registers)OpInfo}({OpInfo})";
                        uint nextOffset = NativeReader.ReadUInt16(image, ref offset);
                        NextFrameOffset = (int)((uint)(NativeReader.ReadUInt16(image, ref offset) << 16) | nextOffset);
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128:
                    {
                        OpInfoStr = $"XMM{OpInfo}({OpInfo})";
                        NextFrameOffset = (int)NativeReader.ReadUInt16(image, ref offset) * 16;
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128_FAR:
                    {
                        OpInfoStr = $"XMM{OpInfo}({OpInfo})";
                        uint nextOffset = NativeReader.ReadUInt16(image, ref offset);
                        NextFrameOffset = (int)((uint)(NativeReader.ReadUInt16(image, ref offset) << 16) | nextOffset);
                    }
                    break;
                default:
                    throw new NotImplementedException(UnwindOp.ToString());
            }

            NextFrameOffset = frameOffset;
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_INFO
    /// </summary>
    public class UnwindInfo : BaseUnwindInfo
    {
        private const int _sizeofUnwindCode = 2;
        private const int _offsetofUnwindCode = 4;

        public byte Version { get; set; } //3 bits
        public byte Flags { get; set; } //5 bits
        public byte SizeOfProlog { get; set; }
        public byte CountOfUnwindCodes { get; set; }
        public Registers FrameRegister { get; set; } //4 bits
        public byte FrameOffset { get; set; } //4 bits
        public Dictionary<int, int> CodeOffsetToUnwindCodeIndex { get; set; }
        public List<UnwindCode> UnwindCodes { get; set; }
        public uint PersonalityRoutineRVA { get; set; }

        public UnwindInfo() { }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/zap/zapcode.cpp">ZapUnwindData::Save</a>
        /// </summary>
        public UnwindInfo(byte[] image, int offset)
        {
            byte versionAndFlags = NativeReader.ReadByte(image, ref offset);
            Version = (byte)(versionAndFlags & 7);
            Flags = (byte)(versionAndFlags >> 3);
            SizeOfProlog = NativeReader.ReadByte(image, ref offset);
            CountOfUnwindCodes = NativeReader.ReadByte(image, ref offset);
            byte frameRegisterAndOffset = NativeReader.ReadByte(image, ref offset);
            FrameRegister = (Registers)(frameRegisterAndOffset & 15);
            FrameOffset = (byte)(frameRegisterAndOffset >> 4);

            UnwindCodes = new List<UnwindCode>(CountOfUnwindCodes);
            CodeOffsetToUnwindCodeIndex = new Dictionary<int, int>();
            int frameOffset = FrameOffset;
            int sizeOfUnwindCodes = CountOfUnwindCodes * _sizeofUnwindCode;
            int endOffset = offset + sizeOfUnwindCodes;
            while (offset < endOffset)
            {
                UnwindCode unwindCode = new UnwindCode(image, ref frameOffset, ref offset);
                CodeOffsetToUnwindCodeIndex.Add(unwindCode.CodeOffset, UnwindCodes.Count);
                UnwindCodes.Add(unwindCode);
            }

            Size = _offsetofUnwindCode + sizeOfUnwindCodes;
            int alignmentPad = -Size & 3;
            Size += alignmentPad + sizeof(uint);

            // Personality routine RVA must be at 4-aligned address
            offset += alignmentPad;
            PersonalityRoutineRVA = NativeReader.ReadUInt32(image, ref offset);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"    Version: {Version}");
            sb.Append($"    Flags: 0x{Flags:X8} (");
            if (Flags == (byte)UnwindFlags.UNW_FLAG_NHANDLER)
            {
                sb.Append(" UNW_FLAG_NHANDLER");
            }
            else
            {
                if ((Flags & (byte)UnwindFlags.UNW_FLAG_EHANDLER) != 0)
                    sb.Append(" UNW_FLAG_EHANDLER");
                if ((Flags & (byte)UnwindFlags.UNW_FLAG_UHANDLER) != 0)
                    sb.Append(" UNW_FLAG_UHANDLER");
                if ((Flags & (byte)UnwindFlags.UNW_FLAG_CHAININFO) != 0)
                    sb.Append(" UNW_FLAG_CHAININFO");
            }
            sb.AppendLine(" )");

            sb.AppendLine($"    SizeOfProlog: {SizeOfProlog}");
            sb.AppendLine($"    CountOfUnwindCodes: {CountOfUnwindCodes}");
            sb.AppendLine($"    FrameRegister: {FrameRegister}");
            sb.AppendLine($"    FrameOffset: {FrameOffset}");
            sb.AppendLine($"    Unwind Codes:");
            sb.AppendLine($"        ------------------");
            foreach (UnwindCode unwindCode in UnwindCodes)
            {
                sb.AppendLine($"        CodeOffset: 0x{unwindCode.CodeOffset:X2}");
                sb.AppendLine($"        UnwindOp: {unwindCode.UnwindOp}({(byte)unwindCode.UnwindOp})");
                sb.AppendLine($"        OpInfo: {unwindCode.OpInfoStr}");
                if (unwindCode.NextFrameOffset != -1)
                {
                    sb.AppendLine($"        FrameOffset: {unwindCode.NextFrameOffset}");
                }
                sb.AppendLine($"        ------------------");
            }
            sb.AppendLine($"        PersonalityRoutineRVA: 0x{PersonalityRoutineRVA:X8}");
            sb.AppendLine($"        Size: {Size} bytes");

            return sb.ToString();
        }

    }
}
