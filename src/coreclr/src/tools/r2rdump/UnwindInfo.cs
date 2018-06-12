// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace R2RDump
{
    public enum Registers
    {
        RAX = 0,
        RCX = 1,
        RDX = 2,
        RBX = 3,
        RSP = 4,
        RBP = 5,
        RSI = 6,
        RDI = 7,
        R8 = 8,
        R9 = 9,
        R10 = 10,
        R11 = 11,
        R12 = 12,
        R13 = 13,
        R14 = 14,
        R15 = 15,
    }

    struct UnwindCode
    {
        public enum OpCodes
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

        public byte CodeOffset { get; }
        public OpCodes UnwindOp { get; } //4 bits
        public Registers OpInfo { get; } //4 bits

        public byte OffsetLow { get; }
        public byte OffsetHigh { get; } //4 bits

        public ushort FrameOffset { get; }

        public UnwindCode(byte[] image, ref int offset)
        {
            int off = offset;
            CodeOffset = NativeReader.ReadByte(image, ref off);
            byte op = NativeReader.ReadByte(image, ref off);
            UnwindOp = (OpCodes)(op & 15);
            OpInfo = (Registers)(op >> 4);

            OffsetLow = CodeOffset;
            OffsetHigh = (byte)OpInfo;

            FrameOffset = NativeReader.ReadUInt16(image, ref offset);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\t\tCodeOffset: {CodeOffset}");
            sb.AppendLine($"\t\tUnwindOp: {UnwindOp}");
            sb.AppendLine($"\t\tOpInfo: {OpInfo}");
            sb.AppendLine();
            sb.AppendLine($"\t\tOffsetLow: {OffsetLow}");
            sb.AppendLine($"\t\tUnwindOp: {UnwindOp}");
            sb.AppendLine($"\t\tOffsetHigh: {OffsetHigh}");
            sb.AppendLine();
            sb.AppendLine($"\t\tFrameOffset: {FrameOffset}");
            sb.AppendLine($"\t\t--------------------");

            return sb.ToString();
        }
    }

    struct UnwindInfo
    {
        private const int _sizeofUnwindCode = 2;
        private const int _offsetofUnwindCode = 4;

        public byte Version { get; } //3 bits
        public byte Flags { get; } //5 bits
        public byte SizeOfProlog { get; }
        public byte CountOfUnwindCodes { get; }
        public byte FrameRegister { get; } //4 bits
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
            FrameRegister = (byte)(frameRegisterAndOffset & 15);
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
                sb.Append(UnwindCode[i].ToString());
            }
            sb.AppendLine($"\tPersonalityRoutineRVA: 0x{PersonalityRoutineRVA:X8}");
            sb.AppendLine($"\tSize: {Size} bytes");

            return sb.ToString();
        }
    }
}
