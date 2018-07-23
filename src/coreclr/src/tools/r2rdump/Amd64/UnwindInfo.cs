// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Xml.Serialization;

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

    public enum UnwindFlags
    {
        UNW_FLAG_NHANDLER = 0x0,
        UNW_FLAG_EHANDLER = 0x1,
        UNW_FLAG_UHANDLER = 0x2,
        UNW_FLAG_CHAININFO = 0x4,
    }

    public struct UnwindCode
    {
        [XmlAttribute("Index")]
        public int Index { get; set; }

        public byte CodeOffset { get; set; }
        public UnwindOpCodes UnwindOp { get; set; } //4 bits
        public byte OpInfo { get; set; } //4 bits

        public byte OffsetLow { get; set; }
        public byte OffsetHigh { get; set; } //4 bits

        public uint FrameOffset { get; set; }

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
        }
    }

    public class UnwindInfo : BaseUnwindInfo
    {
        private const int _sizeofUnwindCode = 2;
        private const int _offsetofUnwindCode = 4;

        public byte Version { get; set; } //3 bits
        public byte Flags { get; set; } //5 bits
        public byte SizeOfProlog { get; set; }
        public byte CountOfUnwindCodes { get; set; }
        public Amd64Registers FrameRegister { get; set; } //4 bits
        public byte FrameOffset { get; set; } //4 bits
        public UnwindCode[] UnwindCode { get; set; }
        public uint PersonalityRoutineRVA { get; set; }

        public UnwindInfo() { }

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
                UnwindCode[i] = new UnwindCode(image, i, ref offset);
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
            sb.Append($"\tFlags: 0x{Flags:X8} (");
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

            sb.AppendLine($"\t\tCodeOffset: 0x{UnwindCode[i].CodeOffset:X2}");
            sb.AppendLine($"\t\tUnwindOp: {UnwindCode[i].UnwindOp}({(byte)UnwindCode[i].UnwindOp})");

            switch (UnwindCode[i].UnwindOp)
            {
                case UnwindOpCodes.UWOP_PUSH_NONVOL:
                    sb.AppendLine($"\t\tOpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                    break;
                case UnwindOpCodes.UWOP_ALLOC_LARGE:
                    sb.Append($"\t\tOpInfo: {UnwindCode[i].OpInfo} - ");
                    if (UnwindCode[i].OpInfo == 0)
                    {
                        i++;
                        sb.AppendLine("Scaled small");
                        uint frameOffset = UnwindCode[i].FrameOffset * 8;
                        sb.AppendLine($"\t\tFrameOffset: {UnwindCode[i].FrameOffset} * 8 = {frameOffset} = 0x{frameOffset:X5})");
                    }
                    else if (UnwindCode[i].OpInfo == 1)
                    {
                        i++;
                        sb.AppendLine("Unscaled large");
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"\t\tFrameOffset: 0x{offset:X8})");
                    }
                    else
                    {
                        sb.AppendLine("Unknown");
                    }
                    break;
                case UnwindOpCodes.UWOP_ALLOC_SMALL:
                    int opInfo = UnwindCode[i].OpInfo * 8 + 8;
                    sb.AppendLine($"\t\tOpInfo: {UnwindCode[i].OpInfo} * 8 + 8 = {opInfo} = 0x{opInfo:X2}");
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG:
                    sb.AppendLine($"\t\tOpInfo: Unused({UnwindCode[i].OpInfo})");
                    break;
                case UnwindOpCodes.UWOP_SET_FPREG_LARGE:
                    {
                        sb.AppendLine($"\t\tOpInfo: Unused({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"\t\tScaled Offset: {offset} * 16 = {offset * 16} = 0x{(offset * 16):X8}");
                        if ((UnwindCode[i].FrameOffset & 0xF0000000) != 0)
                        {
                            R2RDump.WriteWarning("Illegal unwindInfo unscaled offset: too large");
                        }
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL:
                    {
                        sb.AppendLine($"\t\tOpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset * 8;
                        sb.AppendLine($"\t\tScaled Offset: {UnwindCode[i].FrameOffset} * 8 = {offset} = 0x{offset:X5}");
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_NONVOL_FAR:
                    {
                        sb.AppendLine($"\t\tOpInfo: {(Amd64Registers)UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"\t\tUnscaled Large Offset: 0x{offset:X8}");
                    }
                    break;
                case UnwindOpCodes.UWOP_SAVE_XMM128:
                    {
                        sb.AppendLine($"\t\tOpInfo: XMM{UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset * 16;
                        sb.AppendLine($"\t\tScaled Offset: {UnwindCode[i].FrameOffset} * 16 = {offset} = 0x{offset:X5}");
                    }
                    break;

                case UnwindOpCodes.UWOP_SAVE_XMM128_FAR:
                    {
                        sb.AppendLine($"\t\tOpInfo: XMM{UnwindCode[i].OpInfo}({UnwindCode[i].OpInfo})");
                        i++;
                        uint offset = UnwindCode[i].FrameOffset;
                        i++;
                        offset = ((UnwindCode[i].FrameOffset << 16) | offset);
                        sb.AppendLine($"\t\tUnscaled Large Offset: 0x{offset:X8}");
                    }
                    break;
                case UnwindOpCodes.UWOP_EPILOG:
                case UnwindOpCodes.UWOP_SPARE_CODE:
                case UnwindOpCodes.UWOP_PUSH_MACHFRAME:
                default:
                    sb.AppendLine($"\t\tOpInfo: {UnwindCode[i].OpInfo}");
                    sb.AppendLine();
                    sb.AppendLine($"\t\tOffsetLow: {UnwindCode[i].OffsetLow}");
                    sb.AppendLine($"\t\tOffsetHigh: {UnwindCode[i].OffsetHigh}");
                    sb.AppendLine();
                    sb.AppendLine($"\t\tFrameOffset: {FrameOffset}");
                    break;
            }
            return sb.ToString();
        }
    }
}
