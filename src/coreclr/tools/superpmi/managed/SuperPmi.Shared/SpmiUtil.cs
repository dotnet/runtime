// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Internal.TypeSystem;

namespace SuperPmi;

public static unsafe class SpmiUtil
{
    private static TargetArchitecture s_targetArchitecture = Helpers.GetTargetArchitecture(null);

    public static TargetArchitecture GetSpmiTargetArchitecture() => s_targetArchitecture;

    public static void SetSpmiTargetArchitecture(TargetArchitecture architecture) => s_targetArchitecture = architecture;

    public static bool IsSpmiTarget32Bit() => s_targetArchitecture is TargetArchitecture.X86 or TargetArchitecture.ARM or TargetArchitecture.Wasm32;

    public static bool IsSpmiTarget64Bit() => s_targetArchitecture is TargetArchitecture.X64 or TargetArchitecture.ARM64
        or TargetArchitecture.LoongArch64 or TargetArchitecture.RiscV64;

    public static uint SpmiTargetPointerSize() => IsSpmiTarget64Bit() ? 8u : 4u;

    public static bool BreakOnDebugBreakorAV { get; set; }

    public static bool BreakOnException { get; set; }

    // The handler for unexpected conditions, with a unique number per call site.
    public static void DebugBreakorAV(int val)
    {
        if (Debugger.IsAttached && (val == 0 || BreakOnDebugBreakorAV))
            Debugger.Break();

        ErrorHandling.LogException(ExceptionCodes.DebugBreakorAV + (uint)val, $"DebugBreak or AV Exception {val}");
    }

    // Instruction encoding helpers, as in src/coreclr/utilcode/util.cpp.

    public static void PutArm64Rel28(uint* pCode, int imm28)
        => *pCode = (*pCode & 0xFC000000) | (uint)((imm28 >> 2) & 0x03FFFFFF);

    public static void PutArm64Rel21(uint* pCode, int imm21)
    {
        int immlo = imm21 & 0x03;
        int immhi = (imm21 & 0x1FFFFC) >> 2;
        *pCode = (*pCode & 0x9F00001F) | (uint)((immlo << 29) | (immhi << 5));
    }

    public static void PutArm64Rel12(uint* pCode, int imm12) => *pCode = (*pCode & 0xFFC003FF) | (uint)(imm12 << 10);

    // For a 64-bit LDR the encoded immediate is scaled by the access size (8).
    public static void PutArm64Rel12Ldr(uint* pCode, int imm12) => *pCode = (*pCode & 0xFFC003FF) | (uint)((imm12 >> 3) << 10);

    public static void PutThumb2Imm16(ushort* p, ushort imm16)
    {
        int opcode0 = p[0] & ~((0xf000 >> 12) | (0x0800 >> 1));
        int opcode1 = p[1] & ~((0x0700 << 4) | (0x00ff << 0));
        opcode0 |= (imm16 & 0xf000) >> 12;
        opcode0 |= (imm16 & 0x0800) >> 1;
        opcode1 |= (imm16 & 0x0700) << 4;
        opcode1 |= (imm16 & 0x00ff) << 0;
        p[0] = (ushort)opcode0;
        p[1] = (ushort)opcode1;
    }

    public static void PutThumb2Mov32(ushort* p, uint imm32)
    {
        PutThumb2Imm16(p, (ushort)imm32);
        PutThumb2Imm16(p + 2, (ushort)(imm32 >> 16));
    }

    public static void PutThumb2BlRel24(ushort* p, int imm24)
    {
        uint opcode0 = p[0] & 0xF800u;
        uint opcode1 = p[1] & 0xD000u;

        uint s = (uint)(imm24 & 0x1000000) >> 24;
        uint j1 = (((uint)(imm24 & 0x0800000) >> 23) ^ s ^ 1);
        uint j2 = (((uint)(imm24 & 0x0400000) >> 22) ^ s ^ 1);

        opcode0 |= ((uint)(imm24 & 0x03FF000) >> 12) | (s << 10);
        opcode1 |= ((uint)(imm24 & 0x0000FFE) >> 1) | (j1 << 13) | (j2 << 11);

        p[0] = (ushort)opcode0;
        p[1] = (ushort)opcode1;
    }

    // Decodes arm64 mov / movk instructions, which the near differ uses to recognize addresses built by a
    // mov/movk sequence. For example:
    //    d29ff600 mov     x0, #65456
    //    f2ab8640 movk    x0, #23602, lsl #16
    public static bool GetArm64MovConstant(uint* p, out uint reg, out uint con)
    {
        uint instr = *p;
        bool result = (instr & 0xffe00000) == 0xd2800000;
        reg = result ? instr & 0x1f : 0;
        con = result ? (instr >> 5) & 0xffff : 0;
        return result;
    }

    public static bool GetArm64MovkConstant(uint* p, out uint reg, out uint con, out uint shift)
    {
        uint instr = *p;
        bool result = (instr & 0xff800000) == 0xf2800000;
        reg = result ? instr & 0x1f : 0;
        con = result ? (instr >> 5) & 0xffff : 0;
        shift = result ? ((instr >> 21) & 0x3) * 16 : 0;
        return result;
    }

    public static void PutArm64MovkConstant(uint* p, uint con) => *p = (*p & ~(0xffffu << 5)) | ((con & 0xffff) << 5);

    // Decodes Arm32 (Thumb-2) movw / movt instructions. For example:
    //    4b f2 33 40    movw    r0, #46131
    //    c0 f2 79 30    movt    r0, #889
    // This decodes "MOV (immediate)" encoding T3 (ARM manual A8.8.102), and "MOVT" encoding T1 (A8.8.106).
    public static bool GetArm32MovwConstant(uint* p, out uint reg, out uint con) => GetArm32MovConstant(p, 0xf2400000, out reg, out con);

    public static bool GetArm32MovtConstant(uint* p, out uint reg, out uint con) => GetArm32MovConstant(p, 0xf2C00000, out reg, out con);

    private static bool GetArm32MovConstant(uint* p, uint pattern, out uint reg, out uint con)
    {
        reg = 0;
        con = 0;
        if (!Is32BitThumb2Instruction((ushort*)p))
            return false;

        // A Thumb-2 instruction is one or two 16-bit words (in little-endian format).
        uint instr = ((uint)((ushort*)p)[0] << 16) | ((ushort*)p)[1];
        if ((instr & 0xfbf08000) != pattern)
            return false;

        reg = (instr & 0xf00) >> 8;
        con = ExtractArm32MovImm(instr);
        return true;
    }

    public static bool Is32BitThumb2Instruction(ushort* p) => (*p & 0xf800) >= 0xe800;

    public static uint ExtractArm32MovImm(uint instr)
    {
        uint imm4 = (instr >> 16) & 0xf;
        uint i = (instr >> 26) & 0x1;
        uint imm3 = (instr >> 12) & 0x7;
        uint imm8 = instr & 0xff;
        return (imm4 << 12) | (i << 11) | (imm3 << 8) | imm8;
    }

    // Sets the constant field of a movt instruction; 'con' must be a 16-bit constant.
    public static void PutArm32MovtConstant(uint* p, uint con)
    {
        uint imm4 = (con >> 12) & 0xf;
        uint i = (con >> 11) & 0x1;
        uint imm3 = (con >> 8) & 0x7;
        uint imm8 = con & 0xff;
        uint instr = ((uint)((ushort*)p)[0] << 16) | ((ushort*)p)[1];
        instr = (instr & 0xfbf08f00) | (imm4 << 16) | (i << 26) | (imm3 << 12) | imm8;
        ((ushort*)p)[0] = (ushort)(instr >> 16);
        ((ushort*)p)[1] = (ushort)instr;
    }

    // Extracts the PC-relative offset from an auipc + I-type or S-type adder (addi/load/store/jalr).
    public static long GetRiscV64AuipcCombo(uint* pCode, bool isStype)
    {
        long hi20 = ((int)pCode[0] >> 12) << 12;
        uint instr = pCode[1];
        long lo12 = ((int)instr >> 25) << 5; // the top 7 bits are in the same spot
        lo12 |= (instr >> (isStype ? 7 : 20)) & 0x1F;
        return hi20 + lo12;
    }

    // Deposits the PC-relative offset into an auipc + I-type or S-type adder (addi/load/store/jalr).
    public static void PutRiscV64AuipcCombo(uint* pCode, long offset, bool isStype)
    {
        int lo12 = (int)((offset << (64 - 12)) >> (64 - 12));
        int hi20 = (int)(offset - lo12);

        // Replace the existing immediate bits, since RISC-V relocation placeholders may already carry addends.
        pCode[0] &= 0x00000FFF;
        pCode[0] |= (uint)hi20 & 0xFFFFF000;

        uint lo12Bits = (uint)lo12 & 0xFFF;
        if (isStype)
        {
            pCode[1] &= 0x01FFF07F;
            pCode[1] |= (lo12Bits & 0xFE0) << 20;
            pCode[1] |= (lo12Bits & 0x01F) << 7;
        }
        else
        {
            pCode[1] &= 0x000FFFFF;
            pCode[1] |= lo12Bits << 20;
        }
    }
}
