// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace R2RDump.Amd64
{
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

    struct UnwindCode
    {
        public byte CodeOffset { get; }
        public UnwindOpCodes UnwindOp { get; } //4 bits
        public byte OpInfo { get; } //4 bits

        public byte OffsetLow { get; }
        public byte OffsetHigh { get; } //4 bits

        public uint FrameOffset { get; }

        public UnwindCode(byte[] image, ref int offset)
        {
            int off = offset;
            CodeOffset = NativeReader.ReadByte(image, ref off);
            byte op = NativeReader.ReadByte(image, ref off);
            UnwindOp = (UnwindOpCodes)(op & 15);
            OpInfo = (byte)(op >> 4);

            OffsetLow = CodeOffset;
            OffsetHigh = OpInfo;

            FrameOffset = NativeReader.ReadUInt16(image, ref offset);
        }
    }

    struct UnwindInfo : BaseUnwindInfo
    {
        private const int _sizeofUnwindCode = 2;
        private const int _offsetofUnwindCode = 4;

        public byte Version { get; } //3 bits
        public byte Flags { get; } //5 bits
        public byte SizeOfProlog { get; }
        public byte CountOfUnwindCodes { get; }
        public Amd64Registers FrameRegister { get; } //4 bits
        public byte FrameOffset { get; } //4 bits
        public UnwindCode[] UnwindCode { get; }
        public uint PersonalityRoutineRVA { get; }
        public int Size { get; }

        public UnwindInfo(byte[] image, int offset)
        {
            byte versionAndFlags = NativeReader.ReadByte(image, ref offset);
            Version = (byte)(versionAndFlags & 7);
            Flags = (byte)(versionAndFlags >> 3);
            SizeOfProlog = NativeReader.ReadByte(image, ref offset);
            CountOfUnwindCodes = NativeReader.ReadByte(image, ref offset);
            byte frameRegisterAndOffset = NativeReader.ReadByte(image, ref offset);
            FrameRegister = (Amd64Registers)(frameRegisterAndOffset & 15);
            FrameOffset = (byte)(frameRegisterAndOffset >> 4);

            UnwindCode = new UnwindCode[CountOfUnwindCodes];
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                UnwindCode[i] = new UnwindCode(image, ref offset);
            }

            PersonalityRoutineRVA = NativeReader.ReadUInt32(image, ref offset);

            Size = _offsetofUnwindCode + CountOfUnwindCodes * _sizeofUnwindCode;
            int alignmentPad = ((Size + sizeof(int) - 1) & ~(sizeof(int) - 1)) - Size;
            Size += (alignmentPad + sizeof(uint));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\tVersion: {Version}");
            sb.AppendLine($"\tFlags: 0x{Flags:X8}");
            sb.AppendLine($"\tSizeOfProlog: {SizeOfProlog}");
            sb.AppendLine($"\tCountOfUnwindCodes: {CountOfUnwindCodes}");
            sb.AppendLine($"\tFrameRegister: {FrameRegister}");
            sb.AppendLine($"\tFrameOffset: {FrameOffset}");
            sb.AppendLine($"\tUnwind Codes:");
            sb.AppendLine($"\t\t------------------");
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                sb.Append(GetUnwindCode(ref i));
                sb.AppendLine($"\t\t------------------");
            }
            sb.AppendLine($"\tPersonalityRoutineRVA: 0x{PersonalityRoutineRVA:X8}");
            sb.AppendLine($"\tSize: {Size} bytes");

            return sb.ToString();
        }

        private string GetUnwindCode(ref int i)
        {
            StringBuilder sb = new StringBuilder();
            string tab2 = new string(' ', 8);

            sb.AppendLine($"{tab2}CodeOffset: 0x{UnwindCode[i].CodeOffset:X2}");
            sb.AppendLine($"{tab2}UnwindOp: {UnwindCode[i].UnwindOp}({(byte)UnwindCode[i].UnwindOp})");

            switch (UnwindCode[i].UnwindOp)
            {
                case UnwindOpCodes.UWOP_PUSH_NONVOL:
                    sb.AppendLine($"{tab2}OpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                    break;
                case UnwindOpCodes.UWOP_ALLOC_LARGE:
                    sb.Append($"{tab2}OpInfo: {UnwindCode[i].OpInfo} - ");
                    if (UnwindCode[i].OpInfo == 0)
                    {
                        i++;
                        sb.AppendLine("Scaled small");
                        uint frameOffset = UnwindCode[i].FrameOffset * 8;
                        sb.AppendLine($"{tab2}FrameOffset: {UnwindCode[i].FrameOffset} * 8 = {frameOffset} = 0x{frameOffset:X5})");
                    }
                    else if (UnwindCode[i].OpInfo == 1)
                    {
                        i++;
                        sb.AppendLine("Unscaled large");
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"{tab2}FrameOffset: 0x{offset:X8})");
                    }
                    else
                    {
                        sb.AppendLine("Unknown");
                    }
                    break;
                case UnwindOpCodes.UWOP_ALLOC_SMALL:
                    int opInfo = UnwindCode[i].OpInfo * 8 + 8;
                    sb.AppendLine($"{tab2}OpInfo: {UnwindCode[i].OpInfo} * 8 + 8 = {opInfo} = 0x{opInfo:X2}");
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG:
                    sb.AppendLine($"{tab2}OpInfo: Unused({UnwindCode[i].OpInfo})");
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG_LARGE:
                    {
                        sb.AppendLine($"{tab2}OpInfo: Unused({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"{tab2}Scaled Offset: {offset} * 16 = {offset * 16} = 0x{(offset * 16):X8}");
                        if ((UnwindCode[i].FrameOffset & 0xF0000000) != 0)
                        {
                            R2RDump.WriteWarning("Illegal unwindInfo unscaled offset: too large");
                        }
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL:
                    {
                        sb.AppendLine($"{tab2}OpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset * 8;
                        sb.AppendLine($"{tab2}Scaled Offset: {UnwindCode[i].FrameOffset} * 8 = {offset} = 0x{offset:X5}");
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL_FAR:
                    {
                        sb.AppendLine($"{tab2}OpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"{tab2}Unscaled Large Offset: 0x{offset:X8}");
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128:
                    {
                        sb.AppendLine($"{tab2}OpInfo: XMM{UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset * 16;
                        sb.AppendLine($"{tab2}Scaled Offset: {UnwindCode[i].FrameOffset} * 16 = {offset} = 0x{offset:X5}");
                    }
                    break;

                case UnwindOpCodes.UWOP_SAVE_XMM128_FAR:
                    {
                        sb.AppendLine($"{tab2}OpInfo: XMM{UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"{tab2}Unscaled Large Offset: 0x{offset:X8}");
                    }
                    break;
                case UnwindOpCodes.UWOP_EPILOG:
                case UnwindOpCodes.UWOP_SPARE_CODE:
                case UnwindOpCodes.UWOP_PUSH_MACHFRAME:
                default:
                    sb.AppendLine($"{tab2}OpInfo: {UnwindCode[i].OpInfo}");
                    sb.AppendLine();
                    sb.AppendLine($"{tab2}OffsetLow: {UnwindCode[i].OffsetLow}");
                    sb.AppendLine($"{tab2}OffsetHigh: {UnwindCode[i].OffsetHigh}");
                    sb.AppendLine();
                    sb.AppendLine($"{tab2}FrameOffset: {FrameOffset}");
                    break;
            }
            return sb.ToString();
        }
    }
}
