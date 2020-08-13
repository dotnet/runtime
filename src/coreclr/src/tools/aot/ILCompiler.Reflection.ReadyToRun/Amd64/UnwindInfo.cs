// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.Amd64
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_OP_CODES
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
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_OP_CODES
    /// </summary>
    public enum UnwindFlags
    {
        UNW_FLAG_NHANDLER = 0x0,
        UNW_FLAG_EHANDLER = 0x1,
        UNW_FLAG_UHANDLER = 0x2,
        UNW_FLAG_CHAININFO = 0x4,
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_CODE
    /// </summary>
    public class UnwindCode
    {
        public int Index { get; set; }

        public byte CodeOffset { get; set; }
        public UnwindOpCodes UnwindOp { get; set; } //4 bits

        public byte OpInfo { get; set; } //4 bits
        public string OpInfoStr { get; set; } //4 bits

        public byte OffsetLow { get; set; }
        public byte OffsetHigh { get; set; } //4 bits

        public uint FrameOffset { get; set; }
        public int NextFrameOffset { get; set; }

        public bool IsOpInfo { get; set; }

        public UnwindCode() { }

        public UnwindCode(byte[] image, int index, ref int offset)
        {
            Index = index;

            int off = offset;
            CodeOffset = NativeReader.ReadByte(image, ref off);
            byte op = NativeReader.ReadByte(image, ref off);
            UnwindOp = (UnwindOpCodes)(op & 15);
            OpInfo = (byte)(op >> 4);

            OffsetLow = CodeOffset;
            OffsetHigh = OpInfo;

            FrameOffset = NativeReader.ReadUInt16(image, ref offset);
            NextFrameOffset = -1;

            if (UnwindOp == UnwindOpCodes.UWOP_ALLOC_LARGE)
            {
                uint codedSize;
                if (OpInfo == 0)
                {
                    codedSize = NativeReader.ReadUInt16(image, ref offset);
                }
                else if (OpInfo == 1)
                {
                    codedSize = NativeReader.ReadUInt32(image, ref offset);
                }
            }

            IsOpInfo = false;
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/win64unwind.h">src\inc\win64unwind.h</a> _UNWIND_INFO
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
        public UnwindCode[] UnwindCodeArray { get; set; }
        public Dictionary<int, UnwindCode> UnwindCodes { get; set; }
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

            UnwindCodeArray = new UnwindCode[CountOfUnwindCodes];
            UnwindCodes = new Dictionary<int, UnwindCode>();
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                UnwindCodeArray[i] = new UnwindCode(image, i, ref offset);
            }
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                ParseUnwindCode(ref i);
                Debug.Assert(!UnwindCodes.ContainsKey(UnwindCodeArray[i].CodeOffset));
                UnwindCodes.Add(UnwindCodeArray[i].CodeOffset, UnwindCodeArray[i]);
            }

            Size = _offsetofUnwindCode + CountOfUnwindCodes * _sizeofUnwindCode;
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
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                if (!UnwindCodeArray[i].IsOpInfo)
                    continue;
                sb.AppendLine($"        CodeOffset: 0x{UnwindCodeArray[i].CodeOffset:X2}");
                sb.AppendLine($"        UnwindOp: {UnwindCodeArray[i].UnwindOp}({(byte)UnwindCodeArray[i].UnwindOp})");
                sb.AppendLine($"        OpInfo: {UnwindCodeArray[i].OpInfoStr}");
                if (UnwindCodeArray[i].NextFrameOffset != -1)
                {
                    sb.AppendLine($"        FrameOffset: {UnwindCodeArray[i].NextFrameOffset}");
                }
                sb.AppendLine($"        ------------------");
            }
            sb.AppendLine($"        PersonalityRoutineRVA: 0x{PersonalityRoutineRVA:X8}");
            sb.AppendLine($"        Size: {Size} bytes");

            return sb.ToString();
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/jit/unwindamd64.cpp">src\jit\unwindamd64.cpp</a> DumpUnwindInfo
        /// </summary>
        private void ParseUnwindCode(ref int i)
        {
            UnwindCode code = UnwindCodeArray[i];
            code.IsOpInfo = true;
            switch (code.UnwindOp)
            {
                case UnwindOpCodes.UWOP_PUSH_NONVOL:
                    code.OpInfoStr = $"{(Registers)code.OpInfo}({code.OpInfo})";
                    break;
                case UnwindOpCodes.UWOP_ALLOC_LARGE:
                    code.OpInfoStr = $"{code.OpInfo} - ";
                    if (code.OpInfo == 0)
                    {
                        i++;
                        UnwindCodeArray[i].OpInfoStr += "Scaled small";
                        code.NextFrameOffset = (int)UnwindCodeArray[i].FrameOffset * 8;
                    }
                    else if (code.OpInfo == 1)
                    {
                        i++;
                        UnwindCodeArray[i].OpInfoStr += "Unscaled large";
                        uint offset = UnwindCodeArray[i].FrameOffset;
                        i++;
                        offset = ((UnwindCodeArray[i].FrameOffset << 16) | offset);
                        code.NextFrameOffset = (int)offset;
                    }
                    else
                    {
                        code.OpInfoStr += "Unknown";
                    }
                    break;
                case UnwindOpCodes.UWOP_ALLOC_SMALL:
                    int opInfo = code.OpInfo * 8 + 8;
                    code.OpInfoStr = $"{opInfo}";
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG:
                    code.OpInfoStr = $"Unused({code.OpInfo})";
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG_LARGE:
                    {
                        code.OpInfoStr = $"Unused({code.OpInfo})";
                        i++;
                        uint offset = UnwindCodeArray[i].FrameOffset;
                        i++;
                        offset = ((UnwindCodeArray[i].FrameOffset << 16) | offset);
                        code.NextFrameOffset = (int)offset * 16;
                        if ((UnwindCodeArray[i].FrameOffset & 0xF0000000) != 0)
                        {
                            throw new BadImageFormatException("Warning: Illegal unwindInfo unscaled offset: too large");
                        }
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL:
                    {
                        code.OpInfoStr = $"{(Registers)code.OpInfo}({code.OpInfo})";
                        i++;
                        uint offset = UnwindCodeArray[i].FrameOffset * 8;
                        code.NextFrameOffset = (int)offset;
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL_FAR:
                    {
                        code.OpInfoStr = $"{(Registers)code.OpInfo}({code.OpInfo})";
                        i++;
                        uint offset = UnwindCodeArray[i].FrameOffset;
                        i++;
                        offset = ((UnwindCodeArray[i].FrameOffset << 16) | offset);
                        code.NextFrameOffset = (int)offset;
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128:
                    {
                        code.OpInfoStr = $"XMM{code.OpInfo}({code.OpInfo})";
                        i++;
                        uint offset = UnwindCodeArray[i].FrameOffset * 16;
                        code.NextFrameOffset = (int)offset;
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128_FAR:
                    {
                        code.OpInfoStr = $"XMM{code.OpInfo}({code.OpInfo})";
                        i++;
                        uint offset = UnwindCodeArray[i].FrameOffset;
                        i++;
                        offset = ((UnwindCodeArray[i].FrameOffset << 16) | offset);
                        code.NextFrameOffset = (int)offset;
                    }
                    break;
            }
        }
    }
}
