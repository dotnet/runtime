// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace R2RDump
{
    struct UnwindCode
    {
        public byte CodeOffset { get; }
        public byte UnwindOp { get; } //4 bits
        public byte OpInfo { get; } //4 bits

        public byte OffsetLow { get; }
        public byte OffsetHigh { get; } //4 bits

        public ushort FrameOffset { get; }

        public UnwindCode(byte[] image, ref int offset)
        {
            int off = offset;
            CodeOffset = NativeReader.ReadByte(image, ref off);
            byte op = NativeReader.ReadByte(image, ref off);
            UnwindOp = (byte)(op & 15);
            OpInfo = (byte)(op >> 4);

            OffsetLow = CodeOffset;
            OffsetHigh = OpInfo;

            FrameOffset = NativeReader.ReadUInt16(image, ref offset);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string tab2 = new string(' ', 8);
            string tab3 = new string(' ', 12);

            sb.AppendLine($"{tab2}{{");
            sb.AppendLine($"{tab3}CodeOffset: {CodeOffset}");
            sb.AppendLine($"{tab3}UnwindOp: {UnwindOp}");
            sb.AppendLine($"{tab3}OpInfo: {OpInfo}");
            sb.AppendLine($"{tab2}}}");
            sb.AppendLine($"{tab2}{{");
            sb.AppendLine($"{tab3}OffsetLow: {OffsetLow}");
            sb.AppendLine($"{tab3}UnwindOp: {UnwindOp}");
            sb.AppendLine($"{tab3}OffsetHigh: {OffsetHigh}");
            sb.AppendLine($"{tab2}}}");
            sb.AppendLine($"{tab2}FrameOffset: {FrameOffset}");
            sb.AppendLine($"{tab2}------------------");

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

            Size = _offsetofUnwindCode + CountOfUnwindCodes * _sizeofUnwindCode + sizeof(uint);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string tab = "    ";

            sb.AppendLine($"{tab}Version: {Version}");
            sb.AppendLine($"{tab}Flags: 0x{Flags:X8}");
            sb.AppendLine($"{tab}SizeOfProlog: {SizeOfProlog}");
            sb.AppendLine($"{tab}CountOfUnwindCodes: {CountOfUnwindCodes}");
            sb.AppendLine($"{tab}FrameRegister: {FrameRegister}");
            sb.AppendLine($"{tab}FrameOffset: {FrameOffset}");
            sb.AppendLine($"{tab}Unwind Codes:");
            sb.AppendLine($"{tab}{tab}------------------");
            for (int i = 0; i < CountOfUnwindCodes; i++)
            {
                sb.Append(UnwindCode[i].ToString());
            }
            sb.AppendLine($"{tab}PersonalityRoutineRVA: 0x{PersonalityRoutineRVA:X8}");
            sb.AppendLine($"{tab}Size: {Size}");

            return sb.ToString();
        }
    }
}
