// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitX86.cpp                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_XARCH)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

bool emitter::IsSSEInstruction(instruction ins)
{
    return (ins >= INS_FIRST_SSE_INSTRUCTION) && (ins <= INS_LAST_SSE_INSTRUCTION);
}

bool emitter::IsSSEOrAVXInstruction(instruction ins)
{
    return (ins >= INS_FIRST_SSE_INSTRUCTION) && (ins <= INS_LAST_AVX_INSTRUCTION);
}

bool emitter::IsAVXOnlyInstruction(instruction ins)
{
    return (ins >= INS_FIRST_AVX_INSTRUCTION) && (ins <= INS_LAST_AVX_INSTRUCTION);
}

bool emitter::IsFMAInstruction(instruction ins)
{
    return (ins >= INS_FIRST_FMA_INSTRUCTION) && (ins <= INS_LAST_FMA_INSTRUCTION);
}

bool emitter::IsAVXVNNIInstruction(instruction ins)
{
    return (ins >= INS_FIRST_AVXVNNI_INSTRUCTION) && (ins <= INS_LAST_AVXVNNI_INSTRUCTION);
}

bool emitter::IsBMIInstruction(instruction ins)
{
    return (ins >= INS_FIRST_BMI_INSTRUCTION) && (ins <= INS_LAST_BMI_INSTRUCTION);
}

regNumber emitter::getBmiRegNumber(instruction ins)
{
    switch (ins)
    {
        case INS_blsi:
        {
            return (regNumber)3;
        }

        case INS_blsmsk:
        {
            return (regNumber)2;
        }

        case INS_blsr:
        {
            return (regNumber)1;
        }

        default:
        {
            assert(IsBMIInstruction(ins));
            return REG_NA;
        }
    }
}

regNumber emitter::getSseShiftRegNumber(instruction ins)
{
    switch (ins)
    {
        case INS_psrldq:
        {
            return (regNumber)3;
        }

        case INS_pslldq:
        {
            return (regNumber)7;
        }

        case INS_psrld:
        case INS_psrlw:
        case INS_psrlq:
        {
            return (regNumber)2;
        }

        case INS_pslld:
        case INS_psllw:
        case INS_psllq:
        {
            return (regNumber)6;
        }

        case INS_psrad:
        case INS_psraw:
        {
            return (regNumber)4;
        }

        default:
        {
            assert(!"Invalid instruction for SSE2 instruction of the form: opcode reg, immed8");
            return REG_NA;
        }
    }
}

bool emitter::IsAVXInstruction(instruction ins) const
{
    return UseVEXEncoding() && IsSSEOrAVXInstruction(ins);
}

// Returns true if the AVX instruction is a binary operator that requires 3 operands.
// When we emit an instruction with only two operands, we will duplicate the destination
// as a source.
// TODO-XArch-Cleanup: This is a temporary solution for now. Eventually this needs to
// be formalized by adding an additional field to instruction table to
// to indicate whether a 3-operand instruction.
bool emitter::IsDstDstSrcAVXInstruction(instruction ins)
{
    return ((CodeGenInterface::instInfo[ins] & INS_Flags_IsDstDstSrcAVXInstruction) != 0) && IsAVXInstruction(ins);
}

// Returns true if the AVX instruction requires 3 operands that duplicate the source
// register in the vvvv field.
// TODO-XArch-Cleanup: This is a temporary solution for now. Eventually this needs to
// be formalized by adding an additional field to instruction table to
// to indicate whether a 3-operand instruction.
bool emitter::IsDstSrcSrcAVXInstruction(instruction ins)
{
    return ((CodeGenInterface::instInfo[ins] & INS_Flags_IsDstSrcSrcAVXInstruction) != 0) && IsAVXInstruction(ins);
}

//------------------------------------------------------------------------
// HasRegularWideForm: Many x86/x64 instructions follow a regular encoding scheme where the
// byte-sized version of an instruction has the lowest bit of the opcode cleared
// while the 32-bit version of the instruction (taking potential prefixes to
// override operand size) has the lowest bit set. This function returns true if
// the instruction follows this format.
//
// Note that this bit is called `w` in the encoding table in Section B.2 of
// Volume 2 of the Intel Architecture Software Developer Manual.
//
// Arguments:
//    ins - instruction to test
//
// Return Value:
//    true if instruction has a regular form where the 'w' bit needs to be set.
bool emitter::HasRegularWideForm(instruction ins)
{
    return ((CodeGenInterface::instInfo[ins] & INS_FLAGS_Has_Wbit) != 0);
}

//------------------------------------------------------------------------
// HasRegularWideImmediateForm: As above in HasRegularWideForm, many instructions taking
// immediates have a regular form used to encode whether the instruction takes a sign-extended
// 1-byte immediate or a (in 64-bit sign-extended) 4-byte immediate, by respectively setting and
// clearing the second lowest bit.
//
// Note that this bit is called `s` in the encoding table in Section B.2 of
// Volume 2 of the Intel Architecture Software Developer Manual.
//
// Arguments:
//    ins - instruction to test
//
// Return Value:
//    true if instruction has a regular wide immediate form where the 's' bit needs to set.
bool emitter::HasRegularWideImmediateForm(instruction ins)
{
    return ((CodeGenInterface::instInfo[ins] & INS_FLAGS_Has_Sbit) != 0);
}

//------------------------------------------------------------------------
// DoesWriteZeroFlag: check if the instruction write the
//     ZF flag.
//
// Arguments:
//    ins - instruction to test
//
// Return Value:
//    true if instruction writes the ZF flag, false otherwise.
//
bool emitter::DoesWriteZeroFlag(instruction ins)
{
    return (CodeGenInterface::instInfo[ins] & Writes_ZF) != 0;
}

//------------------------------------------------------------------------
// DoesWriteSignFlag: check if the instruction writes the
//     SF flag.
//
// Arguments:
//    ins - instruction to test
//
// Return Value:
//    true if instruction writes the SF flag, false otherwise.
//
bool emitter::DoesWriteSignFlag(instruction ins)
{
    return (CodeGenInterface::instInfo[ins] & Writes_SF) != 0;
}

//------------------------------------------------------------------------
// DoesResetOverflowAndCarryFlags: check if the instruction resets the
//     OF and CF flag to 0.
//
// Arguments:
//    ins - instruction to test
//
// Return Value:
//    true if instruction resets the OF and CF flag, false otherwise.
//
bool emitter::DoesResetOverflowAndCarryFlags(instruction ins)
{
    return (CodeGenInterface::instInfo[ins] & (Resets_OF | Resets_CF)) == (Resets_OF | Resets_CF);
}

//------------------------------------------------------------------------
// IsFlagsAlwaysModified: check if the instruction guarantee to modify any flags.
//
// Arguments:
//    id - instruction to test
//
// Return Value:
//    false, if instruction is guaranteed to not modify any flag.
//    true, if instruction will modify some flag.
//
bool emitter::IsFlagsAlwaysModified(instrDesc* id)
{
    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    if (fmt == IF_RRW_SHF)
    {
        if (id->idIsLargeCns())
        {
            return true;
        }
        else if (id->idSmallCns() == 0)
        {
            switch (ins)
            {
                // If shift-amount for below instructions is 0, then flags are unaffected.
                case INS_rcl_N:
                case INS_rcr_N:
                case INS_rol_N:
                case INS_ror_N:
                case INS_shl_N:
                case INS_shr_N:
                case INS_sar_N:
                    return false;
                default:
                    return true;
            }
        }
    }
    else if (fmt == IF_RRW)
    {
        switch (ins)
        {
            // If shift-amount for below instructions is 0, then flags are unaffected.
            // So, to be conservative, do not optimize if the instruction has register
            // as the shift-amount operand.
            case INS_rcl:
            case INS_rcr:
            case INS_rol:
            case INS_ror:
            case INS_shl:
            case INS_shr:
            case INS_sar:
                return false;
            default:
                return true;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// AreUpper32BitsZero: check if some previously emitted
//     instruction set the upper 32 bits of reg to zero.
//
// Arguments:
//    reg - register of interest
//
// Return Value:
//    true if previous instruction zeroed reg's upper 32 bits.
//    false if it did not, or if we can't safely determine.
//
// Notes:
//    Currently only looks back one instruction.
//
//    movsx eax, ... might seem viable but we always encode this
//    instruction with a 64 bit destination. See TakesRexWPrefix.

bool emitter::AreUpper32BitsZero(regNumber reg)
{
    // If there are no instructions in this IG, we can look back at
    // the previous IG's instructions if this IG is an extension.
    //
    if ((emitCurIGinsCnt == 0) && ((emitCurIG->igFlags & IGF_EXTEND) == 0))
    {
        return false;
    }

    instrDesc* id  = emitLastIns;
    insFormat  fmt = id->idInsFmt();

    // This isn't meant to be a comprehensive check. Just look for what
    // seems to be common.
    switch (fmt)
    {
        case IF_RWR_CNS:
        case IF_RRW_CNS:
        case IF_RRW_SHF:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
        case IF_RWR_MRD:
        case IF_RWR_SRD:
        case IF_RWR_ARD:

            // Bail if not writing to the right register
            if (id->idReg1() != reg)
            {
                return false;
            }

            // Bail if movsx, we always have movsx sign extend to 8 bytes
            if (id->idIns() == INS_movsx)
            {
                return false;
            }

#ifdef TARGET_AMD64
            if (id->idIns() == INS_movsxd)
            {
                return false;
            }
#endif

            // movzx always zeroes the upper 32 bits.
            if (id->idIns() == INS_movzx)
            {
                return true;
            }

            // Else rely on operation size.
            return (id->idOpSize() == EA_4BYTE);

        default:
            break;
    }

    return false;
}

//------------------------------------------------------------------------
// AreFlagsSetToZeroCmp: Checks if the previous instruction set the SZ, and optionally OC, flags to
//                       the same values as if there were a compare to 0
//
// Arguments:
//    reg     - register of interest
//    opSize  - size of register
//    treeOps - type of tree node operation
//
// Return Value:
//    true if the previous instruction set the flags for reg
//    false if not, or if we can't safely determine
//
// Notes:
//    Currently only looks back one instruction.
bool emitter::AreFlagsSetToZeroCmp(regNumber reg, emitAttr opSize, genTreeOps treeOps)
{
    assert(reg != REG_NA);

    if (!emitComp->opts.OptimizationEnabled())
    {
        return false;
    }

    // Don't look back across IG boundaries (possible control flow)
    if (emitCurIGinsCnt == 0 && ((emitCurIG->igFlags & IGF_EXTEND) == 0))
    {
        return false;
    }

    instrDesc*  id      = emitLastIns;
    instruction lastIns = id->idIns();
    insFormat   fmt     = id->idInsFmt();

    // make sure op1 is a reg
    switch (fmt)
    {
        case IF_RWR_CNS:
        case IF_RRW_CNS:
        case IF_RRW_SHF:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
        case IF_RWR_MRD:
        case IF_RWR_SRD:
        case IF_RRW_SRD:
        case IF_RWR_ARD:
        case IF_RRW_ARD:
        case IF_RWR:
        case IF_RRD:
        case IF_RRW:
        case IF_RWR_RRD_RRD:
        case IF_RWR_RRD_MRD:
        case IF_RWR_RRD_ARD:
        case IF_RWR_RRD_SRD:
            break;
        default:
            return false;
    }

    if (id->idReg1() != reg)
    {
        return false;
    }

    // Certain instruction like and, or and xor modifies exactly same flags
    // as "test" instruction.
    // They reset OF and CF to 0 and modifies SF, ZF and PF.
    if (DoesResetOverflowAndCarryFlags(lastIns))
    {
        return id->idOpSize() == opSize;
    }

    if ((treeOps == GT_EQ) || (treeOps == GT_NE))
    {
        if (DoesWriteZeroFlag(lastIns) && IsFlagsAlwaysModified(id))
        {
            return id->idOpSize() == opSize;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// AreFlagsSetToForSignJumpOpt: checks if the previous instruction set the SF if the tree
//                              node qualifies for a jg/jle to jns/js optimization
//
// Arguments:
//    reg     - register of interest
//    opSize  - size of register
//    relop   - relational tree node
//
// Return Value:
//    true if the tree node qualifies for the jg/jle to jns/js optimization
//    false if not, or if we can't safely determine
//
// Notes:
//    Currently only looks back one instruction.
bool emitter::AreFlagsSetForSignJumpOpt(regNumber reg, emitAttr opSize, GenTree* relop)
{
    assert(reg != REG_NA);

    if (!emitComp->opts.OptimizationEnabled())
    {
        return false;
    }

    // Don't look back across IG boundaries (possible control flow)
    if (emitCurIGinsCnt == 0 && ((emitCurIG->igFlags & IGF_EXTEND) == 0))
    {
        return false;
    }

    instrDesc*  id      = emitLastIns;
    instruction lastIns = id->idIns();
    insFormat   fmt     = id->idInsFmt();

    // make sure op1 is a reg
    switch (fmt)
    {
        case IF_RWR_CNS:
        case IF_RRW_CNS:
        case IF_RRW_SHF:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
        case IF_RWR_MRD:
        case IF_RWR_SRD:
        case IF_RRW_SRD:
        case IF_RWR_ARD:
        case IF_RRW_ARD:
        case IF_RWR:
        case IF_RRD:
        case IF_RRW:
            break;
        default:
            return false;
    }

    if (id->idReg1() != reg)
    {
        return false;
    }

    // If we have a GT_GE/GT_LT which generates an jge/jl, and the previous instruction
    // sets the SF, we can omit a test instruction and check for jns/js.
    if ((relop->OperGet() == GT_GE || relop->OperGet() == GT_LT) && !GenCondition::FromRelop(relop).IsUnsigned())
    {
        if (DoesWriteSignFlag(lastIns) && IsFlagsAlwaysModified(id))
        {
            return id->idOpSize() == opSize;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// IsDstSrcImmAvxInstruction: Checks if the instruction has a "reg, reg/mem, imm" or
//                            "reg/mem, reg, imm" form for the legacy, VEX, and EVEX
//                            encodings.
//
// Arguments:
//    instruction -- processor instruction to check
//
// Return Value:
//    true if instruction has a "reg, reg/mem, imm" or "reg/mem, reg, imm" encoding
//    form for the legacy, VEX, and EVEX encodings.
//
//    That is, the instruction takes two operands, one of which is immediate, and it
//    does not need to encode any data in the VEX.vvvv field.
//
static bool IsDstSrcImmAvxInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_aeskeygenassist:
        case INS_extractps:
        case INS_pextrb:
        case INS_pextrw:
        case INS_pextrd:
        case INS_pextrq:
        case INS_pshufd:
        case INS_pshufhw:
        case INS_pshuflw:
        case INS_roundpd:
        case INS_roundps:
            return true;
        default:
            return false;
    }
}

// -------------------------------------------------------------------
// Is4ByteSSEInstruction: Returns true if the SSE instruction is a 4-byte opcode.
//
// Arguments:
//    ins  -  instruction
//
// Note that this should be true for any of the instructions in instrsXArch.h
// that use the SSE38 or SSE3A macro but returns false if the VEX encoding is
// in use, since that encoding does not require an additional byte.
bool emitter::Is4ByteSSEInstruction(instruction ins)
{
    return !UseVEXEncoding() && EncodedBySSE38orSSE3A(ins);
}

// Returns true if this instruction requires a VEX prefix
// All AVX instructions require a VEX prefix
bool emitter::TakesVexPrefix(instruction ins) const
{
    // special case vzeroupper as it requires 2-byte VEX prefix
    // special case the fencing, movnti and the prefetch instructions as they never take a VEX prefix
    switch (ins)
    {
        case INS_lfence:
        case INS_mfence:
        case INS_movnti:
        case INS_prefetchnta:
        case INS_prefetcht0:
        case INS_prefetcht1:
        case INS_prefetcht2:
        case INS_sfence:
        case INS_vzeroupper:
            return false;
        default:
            break;
    }

    return IsAVXInstruction(ins);
}

// Add base VEX prefix without setting W, R, X, or B bits
// L bit will be set based on emitter attr.
//
// 2-byte VEX prefix = C5 <R,vvvv,L,pp>
// 3-byte VEX prefix = C4 <R,X,B,m-mmmm> <W,vvvv,L,pp>
//  - R, X, B, W - bits to express corresponding REX prefixes
//  - m-mmmmm (5-bit)
//    0-00001 - implied leading 0F opcode byte
//    0-00010 - implied leading 0F 38 opcode bytes
//    0-00011 - implied leading 0F 3A opcode bytes
//    Rest    - reserved for future use and usage of them will uresult in Undefined instruction exception
//
// - vvvv (4-bits) - register specifier in 1's complement form; must be 1111 if unused
// - L - scalar or AVX-128 bit operations (L=0),  256-bit operations (L=1)
// - pp (2-bits) - opcode extension providing equivalent functionality of a SIMD size prefix
//                 these prefixes are treated mandatory when used with escape opcode 0Fh for
//                 some SIMD instructions
//   00  - None   (0F    - packed float)
//   01  - 66     (66 0F - packed double)
//   10  - F3     (F3 0F - scalar float
//   11  - F2     (F2 0F - scalar double)
#define DEFAULT_3BYTE_VEX_PREFIX 0xC4E07800000000ULL
#define DEFAULT_3BYTE_VEX_PREFIX_MASK 0xFFFFFF00000000ULL
#define LBIT_IN_3BYTE_VEX_PREFIX 0x00000400000000ULL
emitter::code_t emitter::AddVexPrefix(instruction ins, code_t code, emitAttr attr)
{
    // The 2-byte VEX encoding is preferred when possible, but actually emitting
    // it depends on a number of factors that we may not know until much later.
    //
    // In order to handle this "easily", we just carry the 3-byte encoding all
    // the way through and "fix-up" the encoding when the VEX prefix is actually
    // emitted, by simply checking that all the requirements were met.

    // Only AVX instructions require VEX prefix
    assert(IsAVXInstruction(ins));

    // Shouldn't have already added VEX prefix
    assert(!hasVexPrefix(code));

    assert((code & DEFAULT_3BYTE_VEX_PREFIX_MASK) == 0);

    code |= DEFAULT_3BYTE_VEX_PREFIX;

    if (attr == EA_32BYTE)
    {
        // Set L bit to 1 in case of instructions that operate on 256-bits.
        code |= LBIT_IN_3BYTE_VEX_PREFIX;
    }

    return code;
}

// Returns true if this instruction, for the given EA_SIZE(attr), will require a REX.W prefix
bool emitter::TakesRexWPrefix(instruction ins, emitAttr attr)
{
    // Because the current implementation of AVX does not have a way to distinguish between the register
    // size specification (128 vs. 256 bits) and the operand size specification (32 vs. 64 bits), where both are
    // required, the instruction must be created with the register size attribute (EA_16BYTE or EA_32BYTE),
    // and here we must special case these by the opcode.
    switch (ins)
    {
        case INS_vpermpd:
        case INS_vpermq:
        case INS_vpsrlvq:
        case INS_vpsllvq:
        case INS_pinsrq:
        case INS_pextrq:
        case INS_vfmadd132pd:
        case INS_vfmadd213pd:
        case INS_vfmadd231pd:
        case INS_vfmadd132sd:
        case INS_vfmadd213sd:
        case INS_vfmadd231sd:
        case INS_vfmaddsub132pd:
        case INS_vfmaddsub213pd:
        case INS_vfmaddsub231pd:
        case INS_vfmsubadd132pd:
        case INS_vfmsubadd213pd:
        case INS_vfmsubadd231pd:
        case INS_vfmsub132pd:
        case INS_vfmsub213pd:
        case INS_vfmsub231pd:
        case INS_vfmsub132sd:
        case INS_vfmsub213sd:
        case INS_vfmsub231sd:
        case INS_vfnmadd132pd:
        case INS_vfnmadd213pd:
        case INS_vfnmadd231pd:
        case INS_vfnmadd132sd:
        case INS_vfnmadd213sd:
        case INS_vfnmadd231sd:
        case INS_vfnmsub132pd:
        case INS_vfnmsub213pd:
        case INS_vfnmsub231pd:
        case INS_vfnmsub132sd:
        case INS_vfnmsub213sd:
        case INS_vfnmsub231sd:
        case INS_vpmaskmovq:
        case INS_vpgatherdq:
        case INS_vpgatherqq:
        case INS_vgatherdpd:
        case INS_vgatherqpd:
            return true;
        default:
            break;
    }

#ifdef TARGET_AMD64
    // movsx should always sign extend out to 8 bytes just because we don't track
    // whether the dest should be 4 bytes or 8 bytes (attr indicates the size
    // of the source, not the dest).
    // A 4-byte movzx is equivalent to an 8 byte movzx, so it is not special
    // cased here.
    if (ins == INS_movsx)
    {
        return true;
    }

    if (EA_SIZE(attr) != EA_8BYTE)
    {
        return false;
    }

    if (IsSSEOrAVXInstruction(ins))
    {
        switch (ins)
        {
            case INS_movd: // TODO-Cleanup: replace with movq, https://github.com/dotnet/runtime/issues/47943.
            case INS_andn:
            case INS_bextr:
            case INS_blsi:
            case INS_blsmsk:
            case INS_blsr:
            case INS_bzhi:
            case INS_cvttsd2si:
            case INS_cvttss2si:
            case INS_cvtsd2si:
            case INS_cvtss2si:
            case INS_cvtsi2sd:
            case INS_cvtsi2ss:
            case INS_movnti:
            case INS_mulx:
            case INS_pdep:
            case INS_pext:
            case INS_rorx:
                return true;
            default:
                return false;
        }
    }

    // TODO-XArch-Cleanup: Better way to not emit REX.W when we don't need it, than just testing all these
    // opcodes...
    // These are all the instructions that default to 8-byte operand without the REX.W bit
    // With 1 special case: movzx because the 4 byte version still zeros-out the hi 4 bytes
    // so we never need it
    if ((ins != INS_push) && (ins != INS_pop) && (ins != INS_movq) && (ins != INS_movzx) && (ins != INS_push_hide) &&
        (ins != INS_pop_hide) && (ins != INS_ret) && (ins != INS_call) && (ins != INS_tail_i_jmp) &&
        !((ins >= INS_i_jmp) && (ins <= INS_l_jg)))
    {
        return true;
    }
    else
    {
        return false;
    }
#else  //! TARGET_AMD64 = TARGET_X86
    return false;
#endif //! TARGET_AMD64
}

// Returns true if using this register will require a REX.* prefix.
// Since XMM registers overlap with YMM registers, this routine
// can also be used to know whether a YMM register if the
// instruction in question is AVX.
bool IsExtendedReg(regNumber reg)
{
#ifdef TARGET_AMD64
    return ((reg >= REG_R8) && (reg <= REG_R15)) || ((reg >= REG_XMM8) && (reg <= REG_XMM15));
#else
    // X86 JIT operates in 32-bit mode and hence extended reg are not available.
    return false;
#endif
}

// Returns true if using this register, for the given EA_SIZE(attr), will require a REX.* prefix
bool IsExtendedReg(regNumber reg, emitAttr attr)
{
#ifdef TARGET_AMD64
    // Not a register, so doesn't need a prefix
    if (reg > REG_XMM15)
    {
        return false;
    }

    // Opcode field only has 3 bits for the register, these high registers
    // need a 4th bit, that comes from the REX prefix (eiter REX.X, REX.R, or REX.B)
    if (IsExtendedReg(reg))
    {
        return true;
    }

    if (EA_SIZE(attr) != EA_1BYTE)
    {
        return false;
    }

    // There are 12 one byte registers addressible 'below' r8b:
    //     al, cl, dl, bl, ah, ch, dh, bh, spl, bpl, sil, dil.
    // The first 4 are always addressible, the last 8 are divided into 2 sets:
    //     ah,  ch,  dh,  bh
    //          -- or --
    //     spl, bpl, sil, dil
    // Both sets are encoded exactly the same, the difference is the presence
    // of a REX prefix, even a REX prefix with no other bits set (0x40).
    // So in order to get to the second set we need a REX prefix (but no bits).
    //
    // TODO-AMD64-CQ: if we ever want to start using the first set, we'll need a different way of
    // encoding/tracking/encoding registers.
    return (reg >= REG_RSP);
#else
    // X86 JIT operates in 32-bit mode and hence extended reg are not available.
    return false;
#endif
}

// Since XMM registers overlap with YMM registers, this routine
// can also used to know whether a YMM register in case of AVX instructions.
bool IsXMMReg(regNumber reg)
{
#ifdef TARGET_AMD64
    return (reg >= REG_XMM0) && (reg <= REG_XMM15);
#else  // !TARGET_AMD64
    return (reg >= REG_XMM0) && (reg <= REG_XMM7);
#endif // !TARGET_AMD64
}

// Returns bits to be encoded in instruction for the given register.
unsigned RegEncoding(regNumber reg)
{
    static_assert((REG_XMM0 & 0x7) == 0, "bad XMMBASE");
    return (unsigned)(reg & 0x7);
}

// Utility routines that abstract the logic of adding REX.W, REX.R, REX.X, REX.B and REX prefixes
// SSE2: separate 1-byte prefix gets added before opcode.
// AVX:  specific bits within VEX prefix need to be set in bit-inverted form.
emitter::code_t emitter::AddRexWPrefix(instruction ins, code_t code)
{
    if (UseVEXEncoding() && IsAVXInstruction(ins))
    {
        if (TakesVexPrefix(ins))
        {
            // W-bit is available only in 3-byte VEX prefix that starts with byte C4.
            assert(hasVexPrefix(code));

            // W-bit is the only bit that is added in non bit-inverted form.
            return emitter::code_t(code | 0x00008000000000ULL);
        }
    }
#ifdef TARGET_AMD64
    return emitter::code_t(code | 0x4800000000ULL);
#else
    assert(!"UNREACHED");
    return code;
#endif
}

#ifdef TARGET_AMD64

emitter::code_t emitter::AddRexRPrefix(instruction ins, code_t code)
{
    if (UseVEXEncoding() && IsAVXInstruction(ins))
    {
        if (TakesVexPrefix(ins))
        {
            // R-bit is supported by both 2-byte and 3-byte VEX prefix
            assert(hasVexPrefix(code));

            // R-bit is added in bit-inverted form.
            return code & 0xFF7FFFFFFFFFFFULL;
        }
    }

    return code | 0x4400000000ULL;
}

emitter::code_t emitter::AddRexXPrefix(instruction ins, code_t code)
{
    if (UseVEXEncoding() && IsAVXInstruction(ins))
    {
        if (TakesVexPrefix(ins))
        {
            // X-bit is available only in 3-byte VEX prefix that starts with byte C4.
            assert(hasVexPrefix(code));

            // X-bit is added in bit-inverted form.
            return code & 0xFFBFFFFFFFFFFFULL;
        }
    }

    return code | 0x4200000000ULL;
}

emitter::code_t emitter::AddRexBPrefix(instruction ins, code_t code)
{
    if (UseVEXEncoding() && IsAVXInstruction(ins))
    {
        if (TakesVexPrefix(ins))
        {
            // B-bit is available only in 3-byte VEX prefix that starts with byte C4.
            assert(hasVexPrefix(code));

            // B-bit is added in bit-inverted form.
            return code & 0xFFDFFFFFFFFFFFULL;
        }
    }

    return code | 0x4100000000ULL;
}

// Adds REX prefix (0x40) without W, R, X or B bits set
emitter::code_t emitter::AddRexPrefix(instruction ins, code_t code)
{
    assert(!UseVEXEncoding() || !IsAVXInstruction(ins));
    return code | 0x4000000000ULL;
}

#endif // TARGET_AMD64

bool isPrefix(BYTE b)
{
    assert(b != 0);    // Caller should check this
    assert(b != 0x67); // We don't use the address size prefix
    assert(b != 0x65); // The GS segment override prefix is emitted separately
    assert(b != 0x64); // The FS segment override prefix is emitted separately
    assert(b != 0xF0); // The lock prefix is emitted separately
    assert(b != 0x2E); // We don't use the CS segment override prefix
    assert(b != 0x3E); // Or the DS segment override prefix
    assert(b != 0x26); // Or the ES segment override prefix
    assert(b != 0x36); // Or the SS segment override prefix

    // That just leaves the size prefixes used in SSE opcodes:
    //      Scalar Double  Scalar Single  Packed Double
    return ((b == 0xF2) || (b == 0xF3) || (b == 0x66));
}

// Outputs VEX prefix (in case of AVX instructions) and REX.R/X/W/B otherwise.
unsigned emitter::emitOutputRexOrVexPrefixIfNeeded(instruction ins, BYTE* dst, code_t& code)
{
    if (hasVexPrefix(code))
    {
        // Only AVX instructions should have a VEX prefix
        assert(UseVEXEncoding() && IsAVXInstruction(ins));
        code_t vexPrefix = (code >> 32) & 0x00FFFFFF;
        code &= 0x00000000FFFFFFFFLL;

        WORD leadingBytes = 0;
        BYTE check        = (code >> 24) & 0xFF;
        if (check != 0)
        {
            // 3-byte opcode: with the bytes ordered as 0x2211RM33 or
            // 4-byte opcode: with the bytes ordered as 0x22114433
            // check for a prefix in the 11 position
            BYTE sizePrefix = (code >> 16) & 0xFF;
            if ((sizePrefix != 0) && isPrefix(sizePrefix))
            {
                // 'pp' bits in byte2 of VEX prefix allows us to encode SIMD size prefixes as two bits
                //
                //   00  - None   (0F    - packed float)
                //   01  - 66     (66 0F - packed double)
                //   10  - F3     (F3 0F - scalar float
                //   11  - F2     (F2 0F - scalar double)
                switch (sizePrefix)
                {
                    case 0x66:
                        if (IsBMIInstruction(ins))
                        {
                            switch (ins)
                            {
                                case INS_rorx:
                                case INS_pdep:
                                case INS_mulx:
                                {
                                    vexPrefix |= 0x03;
                                    break;
                                }

                                case INS_pext:
                                {
                                    vexPrefix |= 0x02;
                                    break;
                                }

                                default:
                                {
                                    vexPrefix |= 0x00;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            vexPrefix |= 0x01;
                        }
                        break;
                    case 0xF3:
                        vexPrefix |= 0x02;
                        break;
                    case 0xF2:
                        vexPrefix |= 0x03;
                        break;
                    default:
                        assert(!"unrecognized SIMD size prefix");
                        unreached();
                }

                // Now the byte in the 22 position must be an escape byte 0F
                leadingBytes = check;
                assert(leadingBytes == 0x0F);

                // Get rid of both sizePrefix and escape byte
                code &= 0x0000FFFFLL;

                // Check the byte in the 33 position to see if it is 3A or 38.
                // In such a case escape bytes must be 0x0F3A or 0x0F38
                check = code & 0xFF;
                if (check == 0x3A || check == 0x38)
                {
                    leadingBytes = (leadingBytes << 8) | check;
                    code &= 0x0000FF00LL;
                }
            }
        }
        else
        {
            // 2-byte opcode with the bytes ordered as 0x0011RM22
            // the byte in position 11 must be an escape byte.
            leadingBytes = (code >> 16) & 0xFF;
            assert(leadingBytes == 0x0F || leadingBytes == 0x00);
            code &= 0xFFFF;
        }

        // If there is an escape byte it must be 0x0F or 0x0F3A or 0x0F38
        // m-mmmmm bits in byte 1 of VEX prefix allows us to encode these
        // implied leading bytes. 0x0F is supported by both the 2-byte and
        // 3-byte encoding. While 0x0F3A and 0x0F38 are only supported by
        // the 3-byte version.

        switch (leadingBytes)
        {
            case 0x00:
                // there is no leading byte
                break;
            case 0x0F:
                vexPrefix |= 0x0100;
                break;
            case 0x0F38:
                vexPrefix |= 0x0200;
                break;
            case 0x0F3A:
                vexPrefix |= 0x0300;
                break;
            default:
                assert(!"encountered unknown leading bytes");
                unreached();
        }

        // At this point
        //     VEX.2211RM33 got transformed as VEX.0000RM33
        //     VEX.0011RM22 got transformed as VEX.0000RM22
        //
        // Now output VEX prefix leaving the 4-byte opcode

        // The 2-byte VEX encoding, requires that the X and B-bits are set (these
        // bits are inverted from the REX values so set means off), the W-bit is
        // not set (this bit is not inverted), and that the m-mmmm bits are 0-0001
        // (the 2-byte VEX encoding only supports the 0x0F leading byte). When these
        // conditions are met, we can change byte-0 from 0xC4 to 0xC5 and then
        // byte-1 is the logical-or of bit 7 from byte-1 and bits 0-6 from byte 2
        // from the 3-byte VEX encoding.
        //
        // Given the above, the check can be reduced to a simple mask and comparison.
        // * 0xFFFF7F80 is a mask that ignores any bits whose value we don't care about:
        //   * R can be set or unset              (0x7F ignores bit 7)
        //   * vvvv can be any value              (0x80 ignores bits 3-6)
        //   * L can be set or unset              (0x80 ignores bit 2)
        //   * pp can be any value                (0x80 ignores bits 0-1)
        // * 0x00C46100 is a value that signifies the requirements listed above were met:
        //   * We must be a three-byte VEX opcode (0x00C4)
        //   * X and B must be set                (0x61 validates bits 5-6)
        //   * m-mmmm must be 0-00001             (0x61 validates bits 0-4)
        //   * W must be unset                    (0x00 validates bit 7)
        if ((vexPrefix & 0xFFFF7F80) == 0x00C46100)
        {
            // Encoding optimization calculation is not done while estimating the instruction
            // size and thus over-predict instruction size by 1 byte.
            // If there are IGs that will be aligned, do not optimize encoding so the
            // estimated alignment sizes are accurate.
            if (emitCurIG->igNum > emitLastAlignedIgNum)
            {
                emitOutputByte(dst, 0xC5);
                emitOutputByte(dst + 1, ((vexPrefix >> 8) & 0x80) | (vexPrefix & 0x7F));
                return 2;
            }
        }

        emitOutputByte(dst, ((vexPrefix >> 16) & 0xFF));
        emitOutputByte(dst + 1, ((vexPrefix >> 8) & 0xFF));
        emitOutputByte(dst + 2, vexPrefix & 0xFF);
        return 3;
    }

#ifdef TARGET_AMD64
    if (code > 0x00FFFFFFFFLL)
    {
        BYTE prefix = (code >> 32) & 0xFF;
        noway_assert(prefix >= 0x40 && prefix <= 0x4F);
        code &= 0x00000000FFFFFFFFLL;

        // TODO-AMD64-Cleanup: when we remove the prefixes (just the SSE opcodes right now)
        // we can remove this code as well

        // The REX prefix is required to come after all other prefixes.
        // Some of our 'opcodes' actually include some prefixes, if that
        // is the case, shift them over and place the REX prefix after
        // the other prefixes, and emit any prefix that got moved out.
        BYTE check = (code >> 24) & 0xFF;
        if (check == 0)
        {
            // 3-byte opcode: with the bytes ordered as 0x00113322
            // check for a prefix in the 11 position
            check = (code >> 16) & 0xFF;
            if (check != 0 && isPrefix(check))
            {
                // Swap the rex prefix and whatever this prefix is
                code = (((DWORD)prefix << 16) | (code & 0x0000FFFFLL));
                // and then emit the other prefix
                return emitOutputByte(dst, check);
            }
        }
        else
        {
            // 4-byte opcode with the bytes ordered as 0x22114433
            // first check for a prefix in the 11 position
            BYTE check2 = (code >> 16) & 0xFF;
            if (isPrefix(check2))
            {
                assert(!isPrefix(check)); // We currently don't use this, so it is untested
                if (isPrefix(check))
                {
                    // 3 prefixes were rex = rr, check = c1, check2 = c2 encoded as 0xrrc1c2XXXX
                    // Change to c2rrc1XXXX, and emit check2 now
                    code = (((code_t)prefix << 24) | ((code_t)check << 16) | (code & 0x0000FFFFLL));
                }
                else
                {
                    // 2 prefixes were rex = rr, check2 = c2 encoded as 0xrrXXc2XXXX, (check is part of the opcode)
                    // Change to c2XXrrXXXX, and emit check2 now
                    code = (((code_t)check << 24) | ((code_t)prefix << 16) | (code & 0x0000FFFFLL));
                }
                return emitOutputByte(dst, check2);
            }
        }

        return emitOutputByte(dst, prefix);
    }
#endif // TARGET_AMD64

    return 0;
}

#ifdef TARGET_AMD64
/*****************************************************************************
 * Is the last instruction emitted a call instruction?
 */
bool emitter::emitIsLastInsCall()
{
    if ((emitLastIns != nullptr) && (emitLastIns->idIns() == INS_call))
    {
        return true;
    }

    return false;
}

/*****************************************************************************
 * We're about to create an epilog. If the last instruction we output was a 'call',
 * then we need to insert a NOP, to allow for proper exception-handling behavior.
 */
void emitter::emitOutputPreEpilogNOP()
{
    if (emitIsLastInsCall())
    {
        emitIns(INS_nop);
    }
}

#endif // TARGET_AMD64

// Size of rex prefix in bytes
unsigned emitter::emitGetRexPrefixSize(instruction ins)
{
    // In case of AVX instructions, REX prefixes are part of VEX prefix.
    // And hence requires no additional byte to encode REX prefixes.
    if (IsAVXInstruction(ins))
    {
        return 0;
    }

    // If not AVX, then we would need 1-byte to encode REX prefix.
    return 1;
}

// Size of vex prefix in bytes
unsigned emitter::emitGetVexPrefixSize(instruction ins, emitAttr attr)
{
    if (IsAVXInstruction(ins))
    {
        return 3;
    }

    // If not AVX, then we don't need to encode vex prefix.
    return 0;
}

//------------------------------------------------------------------------
// emitGetAdjustedSize: Determines any size adjustment needed for a given instruction based on the current
// configuration.
//
// Arguments:
//    ins   -- The instruction being emitted
//    attr  -- The emit attribute
//    code  -- The current opcode and any known prefixes
unsigned emitter::emitGetAdjustedSize(instruction ins, emitAttr attr, code_t code)
{
    unsigned adjustedSize = 0;

    if (IsAVXInstruction(ins))
    {
        // VEX prefix encodes some bytes of the opcode and as a result, overall size of the instruction reduces.
        // Therefore, to estimate the size adding VEX prefix size and size of instruction opcode bytes will always
        // overstimate.
        // Instead this routine will adjust the size of VEX prefix based on the number of bytes of opcode it encodes so
        // that
        // instruction size estimate will be accurate.
        // Basically this  will decrease the vexPrefixSize, so that opcodeSize + vexPrefixAdjustedSize will be the right
        // size.
        //
        // rightOpcodeSize + vexPrefixSize
        //  = (opcodeSize - ExtrabytesSize) + vexPrefixSize
        //  = opcodeSize + (vexPrefixSize - ExtrabytesSize)
        //  = opcodeSize + vexPrefixAdjustedSize

        unsigned vexPrefixAdjustedSize = emitGetVexPrefixSize(ins, attr);
        assert(vexPrefixAdjustedSize == 3);

        // In this case, opcode will contains escape prefix at least one byte,
        // vexPrefixAdjustedSize should be minus one.
        vexPrefixAdjustedSize -= 1;

        // Get the fourth byte in Opcode.
        // If this byte is non-zero, then we should check whether the opcode contains SIMD prefix or not.
        BYTE check = (code >> 24) & 0xFF;
        if (check != 0)
        {
            // 3-byte opcode: with the bytes ordered as 0x2211RM33 or
            // 4-byte opcode: with the bytes ordered as 0x22114433
            // Simd prefix is at the first byte.
            BYTE sizePrefix = (code >> 16) & 0xFF;
            if (sizePrefix != 0 && isPrefix(sizePrefix))
            {
                vexPrefixAdjustedSize -= 1;
            }

            // If the opcode size is 4 bytes, then the second escape prefix is at fourth byte in opcode.
            // But in this case the opcode has not counted R\M part.
            // opcodeSize + VexPrefixAdjustedSize - ExtraEscapePrefixSize + ModR\MSize
            //=opcodeSize + VexPrefixAdjustedSize -1 + 1
            //=opcodeSize + VexPrefixAdjustedSize
            // So although we may have second byte escape prefix, we won't decrease vexPrefixAdjustedSize.
        }

        adjustedSize = vexPrefixAdjustedSize;
    }
    else if (Is4ByteSSEInstruction(ins))
    {
        // The 4-Byte SSE instructions require one additional byte to hold the ModRM byte
        adjustedSize++;
    }
    else
    {
        if (ins == INS_crc32)
        {
            // Adjust code size for CRC32 that has 4-byte opcode but does not use SSE38 or EES3A encoding.
            adjustedSize++;
        }

        if ((attr == EA_2BYTE) && (ins != INS_movzx) && (ins != INS_movsx))
        {
            // Most 16-bit operand instructions will need a 0x66 prefix.
            adjustedSize++;
        }
    }

    return adjustedSize;
}

//
//------------------------------------------------------------------------
// emitGetPrefixSize: Get size of rex or vex prefix emitted in code
//
// Arguments:
//    code                  -- The current opcode and any known prefixes
//    includeRexPrefixSize  -- If Rex Prefix size should be included or not
//
unsigned emitter::emitGetPrefixSize(code_t code, bool includeRexPrefixSize)
{
    if (hasVexPrefix(code))
    {
        return 3;
    }

    if (includeRexPrefixSize && hasRexPrefix(code))
    {
        return 1;
    }

    return 0;
}

#ifdef TARGET_X86
/*****************************************************************************
 *
 *  Record a non-empty stack
 */

void emitter::emitMarkStackLvl(unsigned stackLevel)
{
    assert(int(stackLevel) >= 0);
    assert(emitCurStackLvl == 0);
    assert(emitCurIG->igStkLvl == 0);
    assert(emitCurIGfreeNext == emitCurIGfreeBase);

    assert(stackLevel && stackLevel % sizeof(int) == 0);

    emitCurStackLvl = emitCurIG->igStkLvl = stackLevel;

    if (emitMaxStackDepth < emitCurStackLvl)
    {
        JITDUMP("Upping emitMaxStackDepth from %d to %d\n", emitMaxStackDepth, emitCurStackLvl);
        emitMaxStackDepth = emitCurStackLvl;
    }
}
#endif

/*****************************************************************************
 *
 *  Get hold of the address mode displacement value for an indirect call.
 */

inline ssize_t emitter::emitGetInsCIdisp(instrDesc* id)
{
    if (id->idIsLargeCall())
    {
        return ((instrDescCGCA*)id)->idcDisp;
    }
    else
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCns());

        return id->idAddr()->iiaAddrMode.amDisp;
    }
}

/** ***************************************************************************
 *
 *  The following table is used by the instIsFP()/instUse/DefFlags() helpers.
 */

// clang-format off
const insFlags      CodeGenInterface::instInfo[] =
{
    #define INST0(id, nm, um, mr,                 flags) static_cast<insFlags>(flags),
    #define INST1(id, nm, um, mr,                 flags) static_cast<insFlags>(flags),
    #define INST2(id, nm, um, mr, mi,             flags) static_cast<insFlags>(flags),
    #define INST3(id, nm, um, mr, mi, rm,         flags) static_cast<insFlags>(flags),
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) static_cast<insFlags>(flags),
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) static_cast<insFlags>(flags),
    #include "instrs.h"
    #undef  INST0
    #undef  INST1
    #undef  INST2
    #undef  INST3
    #undef  INST4
    #undef  INST5
};
// clang-format on

/*****************************************************************************
 *
 *  Initialize the table used by emitInsModeFormat().
 */

// clang-format off
const BYTE          emitter::emitInsModeFmtTab[] =
{
    #define INST0(id, nm, um, mr,                 flags) um,
    #define INST1(id, nm, um, mr,                 flags) um,
    #define INST2(id, nm, um, mr, mi,             flags) um,
    #define INST3(id, nm, um, mr, mi, rm,         flags) um,
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) um,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) um,
    #include "instrs.h"
    #undef  INST0
    #undef  INST1
    #undef  INST2
    #undef  INST3
    #undef  INST4
    #undef  INST5
};
// clang-format on

#ifdef DEBUG
unsigned const emitter::emitInsModeFmtCnt = ArrLen(emitInsModeFmtTab);
#endif

/*****************************************************************************
 *
 *  Combine the given base format with the update mode of the instuction.
 */

inline emitter::insFormat emitter::emitInsModeFormat(instruction ins, insFormat base)
{
    assert(IF_RRD + IUM_RD == IF_RRD);
    assert(IF_RRD + IUM_WR == IF_RWR);
    assert(IF_RRD + IUM_RW == IF_RRW);

    return (insFormat)(base + emitInsUpdateMode(ins));
}

// This is a helper we need due to Vs Whidbey #254016 in order to distinguish
// if we can not possibly be updating an integer register. This is not the best
// solution, but the other ones (see bug) are going to be much more complicated.
bool emitter::emitInsCanOnlyWriteSSE2OrAVXReg(instrDesc* id)
{
    instruction ins = id->idIns();

    if (!IsSSEOrAVXInstruction(ins))
    {
        return false;
    }

    switch (ins)
    {
        case INS_andn:
        case INS_bextr:
        case INS_blsi:
        case INS_blsmsk:
        case INS_blsr:
        case INS_bzhi:
        case INS_cvttsd2si:
        case INS_cvttss2si:
        case INS_cvtsd2si:
        case INS_cvtss2si:
        case INS_extractps:
        case INS_movd:
        case INS_movmskpd:
        case INS_movmskps:
        case INS_mulx:
        case INS_pdep:
        case INS_pext:
        case INS_pmovmskb:
        case INS_pextrb:
        case INS_pextrd:
        case INS_pextrq:
        case INS_pextrw:
        case INS_pextrw_sse41:
        case INS_rorx:
        {
            // These SSE instructions write to a general purpose integer register.
            return false;
        }

        default:
        {
            return true;
        }
    }
}

/*****************************************************************************
 *
 *  Returns the base encoding of the given CPU instruction.
 */

inline size_t insCode(instruction ins)
{
    // clang-format off
    const static
    size_t          insCodes[] =
    {
        #define INST0(id, nm, um, mr,                 flags) mr,
        #define INST1(id, nm, um, mr,                 flags) mr,
        #define INST2(id, nm, um, mr, mi,             flags) mr,
        #define INST3(id, nm, um, mr, mi, rm,         flags) mr,
        #define INST4(id, nm, um, mr, mi, rm, a4,     flags) mr,
        #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) mr,
        #include "instrs.h"
        #undef  INST0
        #undef  INST1
        #undef  INST2
        #undef  INST3
        #undef  INST4
        #undef  INST5
    };
    // clang-format on

    assert((unsigned)ins < ArrLen(insCodes));
    assert((insCodes[ins] != BAD_CODE));

    return insCodes[ins];
}

/*****************************************************************************
 *
 *  Returns the "AL/AX/EAX, imm" accumulator encoding of the given instruction.
 */

inline size_t insCodeACC(instruction ins)
{
    // clang-format off
    const static
    size_t          insCodesACC[] =
    {
        #define INST0(id, nm, um, mr,                 flags)
        #define INST1(id, nm, um, mr,                 flags)
        #define INST2(id, nm, um, mr, mi,             flags)
        #define INST3(id, nm, um, mr, mi, rm,         flags)
        #define INST4(id, nm, um, mr, mi, rm, a4,     flags) a4,
        #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) a4,
        #include "instrs.h"
        #undef  INST0
        #undef  INST1
        #undef  INST2
        #undef  INST3
        #undef  INST4
        #undef  INST5
    };
    // clang-format on

    assert((unsigned)ins < ArrLen(insCodesACC));
    assert((insCodesACC[ins] != BAD_CODE));

    return insCodesACC[ins];
}

/*****************************************************************************
 *
 *  Returns the "register" encoding of the given CPU instruction.
 */

inline size_t insCodeRR(instruction ins)
{
    // clang-format off
    const static
    size_t          insCodesRR[] =
    {
        #define INST0(id, nm, um, mr,                 flags)
        #define INST1(id, nm, um, mr,                 flags)
        #define INST2(id, nm, um, mr, mi,             flags)
        #define INST3(id, nm, um, mr, mi, rm,         flags)
        #define INST4(id, nm, um, mr, mi, rm, a4,     flags)
        #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) rr,
        #include "instrs.h"
        #undef  INST0
        #undef  INST1
        #undef  INST2
        #undef  INST3
        #undef  INST4
        #undef  INST5
    };
    // clang-format on

    assert((unsigned)ins < ArrLen(insCodesRR));
    assert((insCodesRR[ins] != BAD_CODE));

    return insCodesRR[ins];
}

// clang-format off
const static
size_t          insCodesRM[] =
{
    #define INST0(id, nm, um, mr,                 flags)
    #define INST1(id, nm, um, mr,                 flags)
    #define INST2(id, nm, um, mr, mi,             flags)
    #define INST3(id, nm, um, mr, mi, rm,         flags) rm,
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) rm,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) rm,
    #include "instrs.h"
    #undef  INST0
    #undef  INST1
    #undef  INST2
    #undef  INST3
    #undef  INST4
    #undef  INST5
};
// clang-format on

// Returns true iff the give CPU instruction has an RM encoding.
inline bool hasCodeRM(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesRM));
    return ((insCodesRM[ins] != BAD_CODE));
}

/*****************************************************************************
 *
 *  Returns the "reg, [r/m]" encoding of the given CPU instruction.
 */

inline size_t insCodeRM(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesRM));
    assert((insCodesRM[ins] != BAD_CODE));

    return insCodesRM[ins];
}

// clang-format off
const static
size_t          insCodesMI[] =
{
    #define INST0(id, nm, um, mr,                 flags)
    #define INST1(id, nm, um, mr,                 flags)
    #define INST2(id, nm, um, mr, mi,             flags) mi,
    #define INST3(id, nm, um, mr, mi, rm,         flags) mi,
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) mi,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) mi,
    #include "instrs.h"
    #undef  INST0
    #undef  INST1
    #undef  INST2
    #undef  INST3
    #undef  INST4
    #undef  INST5
};
// clang-format on

// Returns true iff the give CPU instruction has an MI encoding.
inline bool hasCodeMI(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesMI));
    return ((insCodesMI[ins] != BAD_CODE));
}

/*****************************************************************************
 *
 *  Returns the "[r/m], 32-bit icon" encoding of the given CPU instruction.
 */

inline size_t insCodeMI(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesMI));
    assert((insCodesMI[ins] != BAD_CODE));

    return insCodesMI[ins];
}

// clang-format off
const static
size_t          insCodesMR[] =
{
    #define INST0(id, nm, um, mr,                 flags)
    #define INST1(id, nm, um, mr,                 flags) mr,
    #define INST2(id, nm, um, mr, mi,             flags) mr,
    #define INST3(id, nm, um, mr, mi, rm,         flags) mr,
    #define INST4(id, nm, um, mr, mi, rm, a4,     flags) mr,
    #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) mr,
    #include "instrs.h"
    #undef  INST0
    #undef  INST1
    #undef  INST2
    #undef  INST3
    #undef  INST4
    #undef  INST5
};
// clang-format on

// Returns true iff the give CPU instruction has an MR encoding.
inline bool hasCodeMR(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesMR));
    return ((insCodesMR[ins] != BAD_CODE));
}

/*****************************************************************************
 *
 *  Returns the "[r/m], reg" or "[r/m]" encoding of the given CPU instruction.
 */

inline size_t insCodeMR(instruction ins)
{
    assert((unsigned)ins < ArrLen(insCodesMR));
    assert((insCodesMR[ins] != BAD_CODE));

    return insCodesMR[ins];
}

// Return true if the instruction uses the SSE38 or SSE3A macro in instrsXArch.h.
bool emitter::EncodedBySSE38orSSE3A(instruction ins)
{
    const size_t SSE38 = 0x0F660038;
    const size_t SSE3A = 0x0F66003A;
    const size_t MASK  = 0xFFFF00FF;

    size_t insCode = 0;

    if (!IsSSEOrAVXInstruction(ins))
    {
        return false;
    }

    if (hasCodeRM(ins))
    {
        insCode = insCodeRM(ins);
    }
    else if (hasCodeMI(ins))
    {
        insCode = insCodeMI(ins);
    }
    else if (hasCodeMR(ins))
    {
        insCode = insCodeMR(ins);
    }

    insCode &= MASK;
    return insCode == SSE38 || insCode == SSE3A;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in the bit0-2
 *  part of an opcode.
 */

inline unsigned emitter::insEncodeReg012(instruction ins, regNumber reg, emitAttr size, code_t* code)
{
    assert(reg < REG_STK);

#ifdef TARGET_AMD64
    // Either code is not NULL or reg is not an extended reg.
    // If reg is an extended reg, instruction needs to be prefixed with 'REX'
    // which would require code != NULL.
    assert(code != nullptr || !IsExtendedReg(reg));

    if (IsExtendedReg(reg))
    {
        *code = AddRexBPrefix(ins, *code); // REX.B
    }
    else if ((EA_SIZE(size) == EA_1BYTE) && (reg > REG_RBX) && (code != nullptr))
    {
        // We are assuming that we only use/encode SPL, BPL, SIL and DIL
        // not the corresponding AH, CH, DH, or BH
        *code = AddRexPrefix(ins, *code); // REX
    }
#endif // TARGET_AMD64

    unsigned regBits = RegEncoding(reg);

    assert(regBits < 8);
    return regBits;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in the bit3-5
 *  part of an opcode.
 */

inline unsigned emitter::insEncodeReg345(instruction ins, regNumber reg, emitAttr size, code_t* code)
{
    assert(reg < REG_STK);

#ifdef TARGET_AMD64
    // Either code is not NULL or reg is not an extended reg.
    // If reg is an extended reg, instruction needs to be prefixed with 'REX'
    // which would require code != NULL.
    assert(code != nullptr || !IsExtendedReg(reg));

    if (IsExtendedReg(reg))
    {
        *code = AddRexRPrefix(ins, *code); // REX.R
    }
    else if ((EA_SIZE(size) == EA_1BYTE) && (reg > REG_RBX) && (code != nullptr))
    {
        // We are assuming that we only use/encode SPL, BPL, SIL and DIL
        // not the corresponding AH, CH, DH, or BH
        *code = AddRexPrefix(ins, *code); // REX
    }
#endif // TARGET_AMD64

    unsigned regBits = RegEncoding(reg);

    assert(regBits < 8);
    return (regBits << 3);
}

/***********************************************************************************
 *
 *  Returns modified AVX opcode with the specified register encoded in bits 3-6 of
 *  byte 2 of VEX prefix.
 */
inline emitter::code_t emitter::insEncodeReg3456(instruction ins, regNumber reg, emitAttr size, code_t code)
{
    assert(reg < REG_STK);
    assert(IsAVXInstruction(ins));
    assert(hasVexPrefix(code));

    // Get 4-bit register encoding
    // RegEncoding() gives lower 3 bits
    // IsExtendedReg() gives MSB.
    code_t regBits = RegEncoding(reg);
    if (IsExtendedReg(reg))
    {
        regBits |= 0x08;
    }

    // VEX prefix encodes register operand in 1's complement form
    // Shift count = 4-bytes of opcode + 0-2 bits
    assert(regBits <= 0xF);
    regBits <<= 35;
    return code ^ regBits;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in the bit3-5
 *  part of an SIB byte (unshifted).
 *  Used exclusively to generate the REX.X bit and truncate the register.
 */

inline unsigned emitter::insEncodeRegSIB(instruction ins, regNumber reg, code_t* code)
{
    assert(reg < REG_STK);

#ifdef TARGET_AMD64
    // Either code is not NULL or reg is not an extended reg.
    // If reg is an extended reg, instruction needs to be prefixed with 'REX'
    // which would require code != NULL.
    assert(code != nullptr || reg < REG_R8 || (reg >= REG_XMM0 && reg < REG_XMM8));

    if (IsExtendedReg(reg))
    {
        *code = AddRexXPrefix(ins, *code); // REX.X
    }
    unsigned regBits = RegEncoding(reg);
#else  // !TARGET_AMD64
    unsigned regBits = reg;
#endif // !TARGET_AMD64

    assert(regBits < 8);
    return regBits;
}

/*****************************************************************************
 *
 *  Returns the "[r/m]" opcode with the mod/RM field set to register.
 */

inline emitter::code_t emitter::insEncodeMRreg(instruction ins, code_t code)
{
    // If Byte 4 (which is 0xFF00) is 0, that's where the RM encoding goes.
    // Otherwise, it will be placed after the 4 byte encoding.
    if ((code & 0xFF00) == 0)
    {
        assert((code & 0xC000) == 0);
        code |= 0xC000;
    }

    return code;
}

/*****************************************************************************
 *
 *  Returns the given "[r/m]" opcode with the mod/RM field set to register.
 */

inline emitter::code_t emitter::insEncodeRMreg(instruction ins, code_t code)
{
    // If Byte 4 (which is 0xFF00) is 0, that's where the RM encoding goes.
    // Otherwise, it will be placed after the 4 byte encoding.
    if ((code & 0xFF00) == 0)
    {
        assert((code & 0xC000) == 0);
        code |= 0xC000;
    }
    return code;
}

/*****************************************************************************
 *
 *  Returns the "byte ptr [r/m]" opcode with the mod/RM field set to
 *  the given register.
 */

inline emitter::code_t emitter::insEncodeMRreg(instruction ins, regNumber reg, emitAttr size, code_t code)
{
    assert((code & 0xC000) == 0);
    code |= 0xC000;
    unsigned regcode = insEncodeReg012(ins, reg, size, &code) << 8;
    code |= regcode;
    return code;
}

/*****************************************************************************
 *
 *  Returns the "byte ptr [r/m], icon" opcode with the mod/RM field set to
 *  the given register.
 */

inline emitter::code_t emitter::insEncodeMIreg(instruction ins, regNumber reg, emitAttr size, code_t code)
{
    assert((code & 0xC000) == 0);
    code |= 0xC000;
    unsigned regcode = insEncodeReg012(ins, reg, size, &code) << 8;
    code |= regcode;
    return code;
}

/*****************************************************************************
 *
 *  Returns true iff the given instruction does not have a "[r/m], icon" form, but *does* have a
 *  "reg,reg,imm8" form.
 */
inline bool insNeedsRRIb(instruction ins)
{
    // If this list gets longer, use a switch or a table.
    return ins == INS_imul;
}

/*****************************************************************************
 *
 *  Returns the "reg,reg,imm8" opcode with both the reg's set to the
 *  the given register.
 */
inline emitter::code_t emitter::insEncodeRRIb(instruction ins, regNumber reg, emitAttr size)
{
    assert(size == EA_4BYTE); // All we handle for now.
    assert(insNeedsRRIb(ins));
    // If this list gets longer, use a switch, or a table lookup.
    code_t   code    = 0x69c0;
    unsigned regcode = insEncodeReg012(ins, reg, size, &code);
    // We use the same register as source and destination.  (Could have another version that does both regs...)
    code |= regcode;
    code |= (regcode << 3);
    return code;
}

/*****************************************************************************
 *
 *  Returns the "+reg" opcode with the the given register set into the low
 *  nibble of the opcode
 */

inline emitter::code_t emitter::insEncodeOpreg(instruction ins, regNumber reg, emitAttr size)
{
    code_t   code    = insCodeRR(ins);
    unsigned regcode = insEncodeReg012(ins, reg, size, &code);
    code |= regcode;
    return code;
}

/*****************************************************************************
 *
 *  Return the 'SS' field value for the given index scale factor.
 */

inline unsigned emitter::insSSval(unsigned scale)
{
    assert(scale == 1 || scale == 2 || scale == 4 || scale == 8);

    const static BYTE scales[] = {
        0x00, // 1
        0x40, // 2
        0xFF, // 3
        0x80, // 4
        0xFF, // 5
        0xFF, // 6
        0xFF, // 7
        0xC0, // 8
    };

    return scales[scale - 1];
}

const instruction emitJumpKindInstructions[] = {INS_nop,

#define JMP_SMALL(en, rev, ins) INS_##ins,
#include "emitjmps.h"

                                                INS_call};

const emitJumpKind emitReverseJumpKinds[] = {
    EJ_NONE,

#define JMP_SMALL(en, rev, ins) EJ_##rev,
#include "emitjmps.h"
};

/*****************************************************************************
 * Look up the instruction for a jump kind
 */

/*static*/ instruction emitter::emitJumpKindToIns(emitJumpKind jumpKind)
{
    assert((unsigned)jumpKind < ArrLen(emitJumpKindInstructions));
    return emitJumpKindInstructions[jumpKind];
}

/*****************************************************************************
 * Reverse the conditional jump
 */

/* static */ emitJumpKind emitter::emitReverseJumpKind(emitJumpKind jumpKind)
{
    assert(jumpKind < EJ_COUNT);
    return emitReverseJumpKinds[jumpKind];
}

/*****************************************************************************
 * The size for these instructions is less than EA_4BYTE,
 * but the target register need not be byte-addressable
 */

inline bool emitInstHasNoCode(instruction ins)
{
    if (ins == INS_align)
    {
        return true;
    }

    return false;
}

/*****************************************************************************
 * When encoding instructions that operate on byte registers
 * we have to ensure that we use a low register (EAX, EBX, ECX or EDX)
 * otherwise we will incorrectly encode the instruction
 */

bool emitter::emitVerifyEncodable(instruction ins, emitAttr size, regNumber reg1, regNumber reg2 /* = REG_NA */)
{
#if CPU_HAS_BYTE_REGS
    if (size != EA_1BYTE) // Not operating on a byte register is fine
    {
        return true;
    }

    if ((ins != INS_movsx) && // These three instructions support high register
        (ins != INS_movzx)    // encodings for reg1
#ifdef FEATURE_HW_INTRINSICS
        && (ins != INS_crc32)
#endif
            )
    {
        // reg1 must be a byte-able register
        if ((genRegMask(reg1) & RBM_BYTE_REGS) == 0)
        {
            return false;
        }
    }
    // if reg2 is not REG_NA then reg2 must be a byte-able register
    if ((reg2 != REG_NA) && ((genRegMask(reg2) & RBM_BYTE_REGS) == 0))
    {
        return false;
    }
#endif
    // The instruction can be encoded
    return true;
}

//------------------------------------------------------------------------
// emitInsSize: Estimate the size (in bytes of generated code) of the given instruction.
//
// Arguments:
//    code  -- The current opcode and any known prefixes
//    includeRexPrefixSize  -- If Rex Prefix size should be included or not
//
inline UNATIVE_OFFSET emitter::emitInsSize(code_t code, bool includeRexPrefixSize)
{
    UNATIVE_OFFSET size = (code & 0xFF000000) ? 4 : (code & 0x00FF0000) ? 3 : 2;
#ifdef TARGET_AMD64
    size += emitGetPrefixSize(code, includeRexPrefixSize);
#endif
    return size;
}

//------------------------------------------------------------------------
// emitInsSizeRR: Determines the code size for an instruction encoding that does not have any addressing modes
//
// Arguments:
//    ins   -- The instruction being emitted
//    code  -- The current opcode and any known prefixes
inline UNATIVE_OFFSET emitter::emitInsSizeRR(instrDesc* id, code_t code)
{
    assert(id->idIns() != INS_invalid);

    instruction ins  = id->idIns();
    emitAttr    attr = id->idOpSize();

    UNATIVE_OFFSET sz = emitGetAdjustedSize(ins, attr, code);

    bool includeRexPrefixSize = true;
    // REX prefix
    if (TakesRexWPrefix(ins, attr) || IsExtendedReg(id->idReg1(), attr) || IsExtendedReg(id->idReg2(), attr) ||
        (!id->idIsSmallDsc() && (IsExtendedReg(id->idReg3(), attr) || IsExtendedReg(id->idReg4(), attr))))
    {
        sz += emitGetRexPrefixSize(ins);
        includeRexPrefixSize = !IsAVXInstruction(ins);
    }

    sz += emitInsSize(code, includeRexPrefixSize);

    return sz;
}

//------------------------------------------------------------------------
// emitInsSizeRR: Determines the code size for an instruction encoding that does not have any addressing modes and
// includes an immediate value
//
// Arguments:
//    ins   -- The instruction being emitted
//    code  -- The current opcode and any known prefixes
//    val   -- The immediate value to encode
inline UNATIVE_OFFSET emitter::emitInsSizeRR(instrDesc* id, code_t code, int val)
{
    instruction    ins       = id->idIns();
    UNATIVE_OFFSET valSize   = EA_SIZE_IN_BYTES(id->idOpSize());
    bool           valInByte = ((signed char)val == val) && (ins != INS_mov) && (ins != INS_test);

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(valSize <= sizeof(INT32) || !id->idIsCnsReloc());
#endif // TARGET_AMD64

    if (valSize > sizeof(INT32))
    {
        valSize = sizeof(INT32);
    }

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte
        assert(valSize == sizeof(INT32));
    }

    if (valInByte)
    {
        valSize = sizeof(char);
    }
    else
    {
        assert(!IsSSEOrAVXInstruction(ins));
    }

    return valSize + emitInsSizeRR(id, code);
}

inline UNATIVE_OFFSET emitter::emitInsSizeRR(instruction ins, regNumber reg1, regNumber reg2, emitAttr attr)
{
    emitAttr size = EA_SIZE(attr);

    // If Byte 4 (which is 0xFF00) is zero, that's where the RM encoding goes.
    // Otherwise, it will be placed after the 4 byte encoding, making the total 5 bytes.
    // This would probably be better expressed as a different format or something?
    code_t code = insCodeRM(ins);

    UNATIVE_OFFSET sz = emitGetAdjustedSize(ins, size, insCodeRM(ins));

    bool includeRexPrefixSize = true;
    // REX prefix
    if (!hasRexPrefix(code))
    {
        if ((TakesRexWPrefix(ins, size) && ((ins != INS_xor) || (reg1 != reg2))) || IsExtendedReg(reg1, attr) ||
            IsExtendedReg(reg2, attr))
        {
            sz += emitGetRexPrefixSize(ins);
            includeRexPrefixSize = false;
        }
    }

    if ((code & 0xFF00) != 0)
    {
        sz += IsSSEOrAVXInstruction(ins) ? emitInsSize(code, includeRexPrefixSize) : 5;
    }
    else
    {
        sz += emitInsSize(insEncodeRMreg(ins, code), includeRexPrefixSize);
    }

    return sz;
}

/*****************************************************************************/

inline UNATIVE_OFFSET emitter::emitInsSizeSV(code_t code, int var, int dsp)
{
    UNATIVE_OFFSET size = emitInsSize(code, /* includeRexPrefixSize */ true);
    UNATIVE_OFFSET offs;
    bool           offsIsUpperBound = true;
    bool           EBPbased         = true;

    /*  Is this a temporary? */

    if (var < 0)
    {
        /* An address off of ESP takes an extra byte */

        if (!emitHasFramePtr)
        {
            size++;
        }

        // The offset is already assigned. Find the temp.
        TempDsc* tmp = codeGen->regSet.tmpFindNum(var, RegSet::TEMP_USAGE_USED);
        if (tmp == nullptr)
        {
            // It might be in the free lists, if we're working on zero initializing the temps.
            tmp = codeGen->regSet.tmpFindNum(var, RegSet::TEMP_USAGE_FREE);
        }
        assert(tmp != nullptr);
        offs = tmp->tdTempOffs();

        // We only care about the magnitude of the offset here, to determine instruction size.
        if (emitComp->isFramePointerUsed())
        {
            if ((int)offs < 0)
            {
                offs = -(int)offs;
            }
        }
        else
        {
            // SP-based offsets must already be positive.
            assert((int)offs >= 0);
        }
    }
    else
    {

        /* Get the frame offset of the (non-temp) variable */

        offs = dsp + emitComp->lvaFrameAddress(var, &EBPbased);

        /* An address off of ESP takes an extra byte */

        if (!EBPbased)
        {
            ++size;
        }

        /* Is this a stack parameter reference? */

        if ((emitComp->lvaIsParameter(var)
#if !defined(TARGET_AMD64) || defined(UNIX_AMD64_ABI)
             && !emitComp->lvaIsRegArgument(var)
#endif // !TARGET_AMD64 || UNIX_AMD64_ABI
                 ) ||
            (static_cast<unsigned>(var) == emitComp->lvaRetAddrVar))
        {
            /* If no EBP frame, arguments and ret addr are off of ESP, above temps */

            if (!EBPbased)
            {
                assert((int)offs >= 0);

                offsIsUpperBound = false; // since #temps can increase
                offs += emitMaxTmpSize;
            }
        }
        else
        {
            /* Locals off of EBP are at negative offsets */

            if (EBPbased)
            {
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
                // If localloc is not used, then ebp chaining is done and hence
                // offset of locals will be at negative offsets, Otherwise offsets
                // will be positive.  In future, when RBP gets positioned in the
                // middle of the frame so as to optimize instruction encoding size,
                // the below asserts needs to be modified appropriately.
                // However, for Unix platforms, we always do frame pointer chaining,
                // so offsets from the frame pointer will always be negative.
                if (emitComp->compLocallocUsed || emitComp->opts.compDbgEnC)
                {
                    noway_assert((int)offs >= 0);
                }
                else
#endif
                {
                    // Dev10 804810 - failing this assert can lead to bad codegen and runtime crashes
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef UNIX_AMD64_ABI
                    const LclVarDsc* varDsc         = emitComp->lvaGetDesc(var);
                    bool             isRegPassedArg = varDsc->lvIsParam && varDsc->lvIsRegArg;
                    // Register passed args could have a stack offset of 0.
                    noway_assert((int)offs < 0 || isRegPassedArg || emitComp->opts.IsOSR());
#else  // !UNIX_AMD64_ABI

                    // OSR transitioning to RBP frame currently can have mid-frame FP
                    noway_assert(((int)offs < 0) || emitComp->opts.IsOSR());
#endif // !UNIX_AMD64_ABI
                }

                assert(emitComp->lvaTempsHaveLargerOffsetThanVars());

                // lvaInlinedPInvokeFrameVar and lvaStubArgumentVar are placed below the temps
                if (unsigned(var) == emitComp->lvaInlinedPInvokeFrameVar ||
                    unsigned(var) == emitComp->lvaStubArgumentVar)
                {
                    offs -= emitMaxTmpSize;
                }

                if ((int)offs < 0)
                {
                    // offset is negative
                    return size + ((int(offs) >= SCHAR_MIN) ? sizeof(char) : sizeof(int));
                }
#ifdef TARGET_AMD64
                // This case arises for localloc frames
                else
                {
                    return size + ((offs <= SCHAR_MAX) ? sizeof(char) : sizeof(int));
                }
#endif
            }

            if (emitComp->lvaTempsHaveLargerOffsetThanVars() == false)
            {
                offs += emitMaxTmpSize;
            }
        }
    }

    assert((int)offs >= 0);

#if !FEATURE_FIXED_OUT_ARGS

    /* Are we addressing off of ESP? */

    if (!emitHasFramePtr)
    {
        /* Adjust the effective offset if necessary */

        if (emitCntStackDepth)
            offs += emitCurStackLvl;

        // we could (and used to) check for the special case [sp] here but the stack offset
        // estimator was off, and there is very little harm in overestimating for such a
        // rare case.
    }

#endif // !FEATURE_FIXED_OUT_ARGS

//  printf("lcl = %04X, tmp = %04X, stk = %04X, offs = %04X\n",
//         emitLclSize, emitMaxTmpSize, emitCurStackLvl, offs);

#ifdef TARGET_AMD64
    bool useSmallEncoding = (SCHAR_MIN <= (int)offs) && ((int)offs <= SCHAR_MAX);
#else
    bool useSmallEncoding = (offs <= size_t(SCHAR_MAX));
#endif

    // If it is ESP based, and the offset is zero, we will not encode the disp part.
    if (!EBPbased && offs == 0)
    {
        return size;
    }
    else
    {
        return size + (useSmallEncoding ? sizeof(char) : sizeof(int));
    }
}

inline UNATIVE_OFFSET emitter::emitInsSizeSV(instrDesc* id, code_t code, int var, int dsp)
{
    assert(id->idIns() != INS_invalid);
    instruction    ins      = id->idIns();
    emitAttr       attrSize = id->idOpSize();
    UNATIVE_OFFSET prefix   = emitGetAdjustedSize(ins, attrSize, code);

    // REX prefix
    if (TakesRexWPrefix(ins, attrSize) || IsExtendedReg(id->idReg1(), attrSize) ||
        IsExtendedReg(id->idReg2(), attrSize))
    {
        prefix += emitGetRexPrefixSize(ins);
    }

    return prefix + emitInsSizeSV(code, var, dsp);
}

inline UNATIVE_OFFSET emitter::emitInsSizeSV(instrDesc* id, code_t code, int var, int dsp, int val)
{
    assert(id->idIns() != INS_invalid);
    instruction    ins       = id->idIns();
    emitAttr       attrSize  = id->idOpSize();
    UNATIVE_OFFSET valSize   = EA_SIZE_IN_BYTES(attrSize);
    UNATIVE_OFFSET prefix    = emitGetAdjustedSize(ins, attrSize, code);
    bool           valInByte = ((signed char)val == val) && (ins != INS_mov) && (ins != INS_test);

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(valSize <= sizeof(int) || !id->idIsCnsReloc());
#endif // TARGET_AMD64

    if (valSize > sizeof(int))
    {
        valSize = sizeof(int);
    }

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte
        assert(valSize == sizeof(int));
    }

    if (valInByte)
    {
        valSize = sizeof(char);
    }
    else
    {
        assert(!IsSSEOrAVXInstruction(ins));
    }

    // 64-bit operand instructions will need a REX.W prefix
    if (TakesRexWPrefix(ins, attrSize) || IsExtendedReg(id->idReg1(), attrSize) ||
        IsExtendedReg(id->idReg2(), attrSize))
    {
        prefix += emitGetRexPrefixSize(ins);
    }

    return prefix + valSize + emitInsSizeSV(code, var, dsp);
}

/*****************************************************************************/

static bool baseRegisterRequiresSibByte(regNumber base)
{
#ifdef TARGET_AMD64
    return base == REG_ESP || base == REG_R12;
#else
    return base == REG_ESP;
#endif
}

static bool baseRegisterRequiresDisplacement(regNumber base)
{
#ifdef TARGET_AMD64
    return base == REG_EBP || base == REG_R13;
#else
    return base == REG_EBP;
#endif
}

UNATIVE_OFFSET emitter::emitInsSizeAM(instrDesc* id, code_t code)
{
    assert(id->idIns() != INS_invalid);
    instruction ins      = id->idIns();
    emitAttr    attrSize = id->idOpSize();
    /* The displacement field is in an unusual place for (tail-)calls */
    ssize_t        dsp = (ins == INS_call) || (ins == INS_tail_i_jmp) ? emitGetInsCIdisp(id) : emitGetInsAmdAny(id);
    bool           dspInByte = ((signed char)dsp == (ssize_t)dsp);
    bool           dspIsZero = (dsp == 0);
    UNATIVE_OFFSET size;

    // Note that the values in reg and rgx are used in this method to decide
    // how many bytes will be needed by the address [reg+rgx+cns]
    // this includes the prefix bytes when reg or rgx are registers R8-R15
    regNumber reg;
    regNumber rgx;

    // The idAddr field is a union and only some of the instruction formats use the iiaAddrMode variant
    // these are IF_AWR_*, IF_ARD_*, IF_ARW_* and IF_*_ARD
    // ideally these should really be the only idInsFmts that we see here
    //  but we have some outliers to deal with:
    //     emitIns_R_L adds IF_RWR_LABEL and calls emitInsSizeAM
    //     emitInsRMW adds IF_MRW_CNS, IF_MRW_RRD, IF_MRW_SHF, and calls emitInsSizeAM

    switch (id->idInsFmt())
    {
        case IF_RWR_LABEL:
        case IF_MRW_CNS:
        case IF_MRW_RRD:
        case IF_MRW_SHF:
            reg = REG_NA;
            rgx = REG_NA;
            break;

        default:
            reg = id->idAddr()->iiaAddrMode.amBaseReg;
            rgx = id->idAddr()->iiaAddrMode.amIndxReg;
            break;
    }

    if (id->idIsDspReloc())
    {
        dspInByte = false; // relocs can't be placed in a byte
        dspIsZero = false; // relocs won't always be zero
    }

    if (code & 0xFF000000)
    {
        size = 4;
    }
    else if (code & 0x00FF0000)
    {
        // BT supports 16 bit operands and this code doesn't handle the necessary 66 prefix.
        assert(ins != INS_bt);

        assert((attrSize == EA_4BYTE) || (attrSize == EA_PTRSIZE)    // Only for x64
               || (attrSize == EA_16BYTE) || (attrSize == EA_32BYTE) // only for x64
               || (ins == INS_movzx) || (ins == INS_movsx)
               // The prefetch instructions are always 3 bytes and have part of their modr/m byte hardcoded
               || isPrefetch(ins));
        size = 3;
    }
    else
    {
        size = 2;
    }

    size += emitGetAdjustedSize(ins, attrSize, code);

    if (hasRexPrefix(code))
    {
        // REX prefix
        size += emitGetRexPrefixSize(ins);
    }
    else if (TakesRexWPrefix(ins, attrSize))
    {
        // REX.W prefix
        size += emitGetRexPrefixSize(ins);
    }
    else if (IsExtendedReg(reg, EA_PTRSIZE) || IsExtendedReg(rgx, EA_PTRSIZE) ||
             ((ins != INS_call) && (IsExtendedReg(id->idReg1(), attrSize) || IsExtendedReg(id->idReg2(), attrSize))))
    {
        // Should have a REX byte
        size += emitGetRexPrefixSize(ins);
    }

    if (rgx == REG_NA)
    {
        /* The address is of the form "[reg+disp]" */

        if (reg == REG_NA)
        {
            /* The address is of the form "[disp]" */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
            // Special case: "mov eax, [disp]" and "mov [disp], eax" can use a smaller 1-byte encoding.
            if ((ins == INS_mov) && (id->idReg1() == REG_EAX) &&
                ((id->idInsFmt() == IF_RWR_ARD) || (id->idInsFmt() == IF_AWR_RRD)))
            {
                // Amd64: this is one case where addr can be 64-bit in size. This is currently unused.
                // If this ever changes, this code will need to be updated to add "sizeof(INT64)" to "size".
                assert((size == 2) || ((size == 3) && (id->idOpSize() == EA_2BYTE)));
                size--;
            }
#endif

            size += sizeof(INT32);

#ifdef TARGET_AMD64
            // If id is not marked for reloc, add 1 additional byte for SIB that follows disp32
            if (!id->idIsDspReloc())
            {
                size++;
            }
#endif
            return size;
        }

        // If this is just "call reg", we're done.
        if (id->idIsCallRegPtr())
        {
            assert(ins == INS_call || ins == INS_tail_i_jmp);
            assert(dsp == 0);
            return size;
        }

        // If the base register is ESP (or R12 on 64-bit systems), a SIB byte must be used.
        if (baseRegisterRequiresSibByte(reg))
        {
            size++;
        }

        // If the base register is EBP (or R13 on 64-bit systems), a displacement is required.
        // Otherwise, the displacement can be elided if it is zero.
        if (dspIsZero && !baseRegisterRequiresDisplacement(reg))
        {
            return size;
        }

        /* Does the offset fit in a byte? */

        if (dspInByte)
        {
            size += sizeof(char);
        }
        else
        {
            size += sizeof(INT32);
        }
    }
    else
    {
        /* An index register is present */

        size++;

        /* Is the index value scaled? */

        if (emitDecodeScale(id->idAddr()->iiaAddrMode.amScale) > 1)
        {
            /* Is there a base register? */

            if (reg != REG_NA)
            {
                /* The address is "[reg + {2/4/8} * rgx + icon]" */

                if (dspIsZero && !baseRegisterRequiresDisplacement(reg))
                {
                    /* The address is "[reg + {2/4/8} * rgx]" */
                }
                else
                {
                    /* The address is "[reg + {2/4/8} * rgx + disp]" */

                    if (dspInByte)
                    {
                        size += sizeof(char);
                    }
                    else
                    {
                        size += sizeof(int);
                    }
                }
            }
            else
            {
                /* The address is "[{2/4/8} * rgx + icon]" */

                size += sizeof(INT32);
            }
        }
        else
        {
            // When we are using the SIB or VSIB format with EBP or R13 as a base, we must emit at least
            // a 1 byte displacement (this is a special case in the encoding to allow for the case of no
            // base register at all). In order to avoid this when we have no scaling, we can reverse the
            // registers so that we don't have to add that extra byte. However, we can't do that if the
            // index register is a vector, such as for a gather instruction.
            //
            if (dspIsZero && baseRegisterRequiresDisplacement(reg) && !baseRegisterRequiresDisplacement(rgx) &&
                !isFloatReg(rgx))
            {
                // Swap reg and rgx, such that reg is not EBP/R13.
                regNumber tmp                       = reg;
                id->idAddr()->iiaAddrMode.amBaseReg = reg = rgx;
                id->idAddr()->iiaAddrMode.amIndxReg = rgx = tmp;
            }

            /* The address is "[reg+rgx+dsp]" */

            if (dspIsZero && !baseRegisterRequiresDisplacement(reg))
            {
                /* This is [reg+rgx]" */
            }
            else
            {
                /* This is [reg+rgx+dsp]" */

                if (dspInByte)
                {
                    size += sizeof(char);
                }
                else
                {
                    size += sizeof(int);
                }
            }
        }
    }

    return size;
}

inline UNATIVE_OFFSET emitter::emitInsSizeAM(instrDesc* id, code_t code, int val)
{
    assert(id->idIns() != INS_invalid);
    instruction    ins       = id->idIns();
    UNATIVE_OFFSET valSize   = EA_SIZE_IN_BYTES(id->idOpSize());
    bool           valInByte = ((signed char)val == val) && (ins != INS_mov) && (ins != INS_test);

    // We should never generate BT mem,reg because it has poor performance. BT mem,imm might be useful
    // but it requires special handling of the immediate value (it is always encoded in a byte).
    // Let's not complicate things until this is needed.
    assert(ins != INS_bt);

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(valSize <= sizeof(INT32) || !id->idIsCnsReloc());
#endif // TARGET_AMD64

    if (valSize > sizeof(INT32))
    {
        valSize = sizeof(INT32);
    }

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte
        assert(valSize == sizeof(INT32));
    }

    if (valInByte)
    {
        valSize = sizeof(char);
    }
    else
    {
        assert(!IsSSEOrAVXInstruction(ins));
    }

    return valSize + emitInsSizeAM(id, code);
}

inline UNATIVE_OFFSET emitter::emitInsSizeCV(instrDesc* id, code_t code)
{
    assert(id->idIns() != INS_invalid);
    instruction ins      = id->idIns();
    emitAttr    attrSize = id->idOpSize();

    // It is assumed that all addresses for the "M" format
    // can be reached via RIP-relative addressing.
    UNATIVE_OFFSET size = sizeof(INT32);

    size += emitGetAdjustedSize(ins, attrSize, code);

    bool includeRexPrefixSize = true;

    // 64-bit operand instructions will need a REX.W prefix
    if (TakesRexWPrefix(ins, attrSize) || IsExtendedReg(id->idReg1(), attrSize) ||
        IsExtendedReg(id->idReg2(), attrSize))
    {
        size += emitGetRexPrefixSize(ins);
        includeRexPrefixSize = false;
    }

    return size + emitInsSize(code, includeRexPrefixSize);
}

inline UNATIVE_OFFSET emitter::emitInsSizeCV(instrDesc* id, code_t code, int val)
{
    instruction    ins       = id->idIns();
    UNATIVE_OFFSET valSize   = EA_SIZE_IN_BYTES(id->idOpSize());
    bool           valInByte = ((signed char)val == val) && (ins != INS_mov) && (ins != INS_test);

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(valSize <= sizeof(INT32) || !id->idIsCnsReloc());
#endif // TARGET_AMD64

    if (valSize > sizeof(INT32))
    {
        valSize = sizeof(INT32);
    }

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte
        assert(valSize == sizeof(INT32));
    }

    if (valInByte)
    {
        valSize = sizeof(char);
    }
    else
    {
        assert(!IsSSEOrAVXInstruction(ins));
    }

    return valSize + emitInsSizeCV(id, code);
}

/*****************************************************************************
 *
 *  Allocate instruction descriptors for instructions with address modes.
 */

inline emitter::instrDesc* emitter::emitNewInstrAmd(emitAttr size, ssize_t dsp)
{
    if (dsp < AM_DISP_MIN || dsp > AM_DISP_MAX)
    {
        instrDescAmd* id = emitAllocInstrAmd(size);

        id->idSetIsLargeDsp();
#ifdef DEBUG
        id->idAddr()->iiaAddrMode.amDisp = AM_DISP_BIG_VAL;
#endif
        id->idaAmdVal = dsp;

        return id;
    }
    else
    {
        instrDesc* id = emitAllocInstr(size);

        id->idAddr()->iiaAddrMode.amDisp = dsp;
        assert(id->idAddr()->iiaAddrMode.amDisp == dsp); // make sure the value fit

        return id;
    }
}

/*****************************************************************************
 *
 *  Set the displacement field in an instruction. Only handles instrDescAmd type.
 */

inline void emitter::emitSetAmdDisp(instrDescAmd* id, ssize_t dsp)
{
    if (dsp < AM_DISP_MIN || dsp > AM_DISP_MAX)
    {
        id->idSetIsLargeDsp();
#ifdef DEBUG
        id->idAddr()->iiaAddrMode.amDisp = AM_DISP_BIG_VAL;
#endif
        id->idaAmdVal = dsp;
    }
    else
    {
        id->idSetIsSmallDsp();
        id->idAddr()->iiaAddrMode.amDisp = dsp;
        assert(id->idAddr()->iiaAddrMode.amDisp == dsp); // make sure the value fit
    }
}

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for an instruction that uses both
 *  an address mode displacement and a constant.
 */

emitter::instrDesc* emitter::emitNewInstrAmdCns(emitAttr size, ssize_t dsp, int cns)
{
    if (dsp >= AM_DISP_MIN && dsp <= AM_DISP_MAX)
    {
        instrDesc* id                    = emitNewInstrCns(size, cns);
        id->idAddr()->iiaAddrMode.amDisp = dsp;
        assert(id->idAddr()->iiaAddrMode.amDisp == dsp); // make sure the value fit

        return id;
    }
    else
    {
        if (instrDesc::fitsInSmallCns(cns))
        {
            instrDescAmd* id = emitAllocInstrAmd(size);

            id->idSetIsLargeDsp();
#ifdef DEBUG
            id->idAddr()->iiaAddrMode.amDisp = AM_DISP_BIG_VAL;
#endif
            id->idaAmdVal = dsp;

            id->idSmallCns(cns);

            return id;
        }
        else
        {
            instrDescCnsAmd* id = emitAllocInstrCnsAmd(size);

            id->idSetIsLargeCns();
            id->idacCnsVal = cns;

            id->idSetIsLargeDsp();
#ifdef DEBUG
            id->idAddr()->iiaAddrMode.amDisp = AM_DISP_BIG_VAL;
#endif
            id->idacAmdVal = dsp;

            return id;
        }
    }
}

/*****************************************************************************
 *
 *  Add a NOP instruction of the given size.
 */

void emitter::emitIns_Nop(unsigned size)
{
    assert(size <= MAX_ENCODED_SIZE);

    instrDesc* id = emitNewInstr();
    id->idIns(INS_nop);
    id->idInsFmt(IF_NONE);
    id->idCodeSize(size);

    dispIns(id);
    emitCurIGsize += size;
}

/*****************************************************************************
 *
 *  Add an instruction with no operands.
 */
void emitter::emitIns(instruction ins)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id   = emitNewInstr();
    code_t         code = insCodeMR(ins);

#ifdef DEBUG
    {
        // We cannot have #ifdef inside macro expansion.
        bool assertCond =
            (ins == INS_cdq || ins == INS_int3 || ins == INS_lock || ins == INS_leave || ins == INS_movsb ||
             ins == INS_movsd || ins == INS_movsp || ins == INS_nop || ins == INS_r_movsb || ins == INS_r_movsd ||
             ins == INS_r_movsp || ins == INS_r_stosb || ins == INS_r_stosd || ins == INS_r_stosp || ins == INS_ret ||
             ins == INS_sahf || ins == INS_stosb || ins == INS_stosd || ins == INS_stosp
             // These instructions take zero operands
             || ins == INS_vzeroupper || ins == INS_lfence || ins == INS_mfence || ins == INS_sfence ||
             ins == INS_pause);

        assert(assertCond);
    }
#endif // DEBUG

    assert(!hasRexPrefix(code)); // Can't have a REX bit with no operands, right?

    if (code & 0xFF000000)
    {
        sz = 2; // TODO-XArch-Bug?: Shouldn't this be 4? Or maybe we should assert that we don't see this case.
    }
    else if (code & 0x00FF0000)
    {
        sz = 3;
    }
    else if (code & 0x0000FF00)
    {
        sz = 2;
    }
    else
    {
        sz = 1;
    }

    // vzeroupper includes its 2-byte VEX prefix in its MR code.
    assert((ins != INS_vzeroupper) || (sz == 3));

    insFormat fmt = IF_NONE;

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

// Add an instruction with no operands, but whose encoding depends on the size
// (Only CDQ/CQO/CWDE/CDQE currently)
void emitter::emitIns(instruction ins, emitAttr attr)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id   = emitNewInstr(attr);
    code_t         code = insCodeMR(ins);
    assert((ins == INS_cdq) || (ins == INS_cwde));
    assert((code & 0xFFFFFF00) == 0);
    sz = 1;

    insFormat fmt = IF_NONE;

    sz += emitGetAdjustedSize(ins, attr, code);
    if (TakesRexWPrefix(ins, attr))
    {
        sz += emitGetRexPrefixSize(ins);
    }

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitMapFmtForIns: map the instruction format based on the instruction.
// Shift-by-a-constant instructions have a special format.
//
// Arguments:
//    fmt - the instruction format to map
//    ins - the instruction
//
// Returns:
//    The mapped instruction format.
//
emitter::insFormat emitter::emitMapFmtForIns(insFormat fmt, instruction ins)
{
    switch (ins)
    {
        case INS_rol_N:
        case INS_ror_N:
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
        {
            switch (fmt)
            {
                case IF_RRW_CNS:
                    return IF_RRW_SHF;
                case IF_MRW_CNS:
                    return IF_MRW_SHF;
                case IF_SRW_CNS:
                    return IF_SRW_SHF;
                case IF_ARW_CNS:
                    return IF_ARW_SHF;
                default:
                    unreached();
            }
        }

        default:
            return fmt;
    }
}

//------------------------------------------------------------------------
// emitMapFmtAtoM: map the address mode formats ARD, ARW, and AWR to their direct address equivalents.
//
// Arguments:
//    fmt - the instruction format to map
//
// Returns:
//    The mapped instruction format.
//
emitter::insFormat emitter::emitMapFmtAtoM(insFormat fmt)
{
    switch (fmt)
    {
        case IF_ARD:
            return IF_MRD;
        case IF_AWR:
            return IF_MWR;
        case IF_ARW:
            return IF_MRW;

        case IF_RRD_ARD:
            return IF_RRD_MRD;
        case IF_RWR_ARD:
            return IF_RWR_MRD;
        case IF_RWR_ARD_CNS:
            return IF_RWR_MRD_CNS;
        case IF_RRW_ARD:
            return IF_RRW_MRD;
        case IF_RRW_ARD_CNS:
            return IF_RRW_MRD_CNS;
        case IF_RWR_RRD_ARD:
            return IF_RWR_RRD_MRD;
        case IF_RWR_RRD_ARD_CNS:
            return IF_RWR_RRD_MRD_CNS;
        case IF_RWR_RRD_ARD_RRD:
            return IF_RWR_RRD_MRD_RRD;

        case IF_ARD_RRD:
            return IF_MRD_RRD;
        case IF_AWR_RRD:
            return IF_MWR_RRD;
        case IF_ARW_RRD:
            return IF_MRW_RRD;

        case IF_ARD_CNS:
            return IF_MRD_CNS;
        case IF_AWR_CNS:
            return IF_MWR_CNS;
        case IF_ARW_CNS:
            return IF_MRW_CNS;

        case IF_AWR_RRD_CNS:
            return IF_MWR_RRD_CNS;

        case IF_ARW_SHF:
            return IF_MRW_SHF;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// emitHandleMemOp: For a memory operand, fill in the relevant fields of the instrDesc.
//
// Arguments:
//    indir - the memory operand.
//    id - the instrDesc to fill in.
//    fmt - the instruction format to use. This must be one of the ARD, AWR, or ARW formats. If necessary (such as for
//          GT_CLS_VAR_ADDR), this function will map it to the correct format.
//    ins - the instruction we are generating. This might affect the instruction format we choose.
//
// Assumptions:
//    The correctly sized instrDesc must already be created, e.g., via emitNewInstrAmd() or emitNewInstrAmdCns();
//
// Post-conditions:
//    For base address of int constant:
//        -- the caller must have added the int constant base to the instrDesc when creating it via
//           emitNewInstrAmdCns().
//    For simple address modes (base + scale * index + offset):
//        -- the base register, index register, and scale factor are set.
//        -- the caller must have added the addressing mode offset int constant to the instrDesc when creating it via
//           emitNewInstrAmdCns().
//
//    The instruction format is set.
//
//    idSetIsDspReloc() is called if necessary.
//
void emitter::emitHandleMemOp(GenTreeIndir* indir, instrDesc* id, insFormat fmt, instruction ins)
{
    assert(fmt != IF_NONE);

    GenTree* memBase = indir->Base();

    if ((memBase != nullptr) && memBase->isContained() && (memBase->OperGet() == GT_CLS_VAR_ADDR))
    {
        CORINFO_FIELD_HANDLE fldHnd = memBase->AsClsVar()->gtClsVarHnd;

        // Static always need relocs
        if (!jitStaticFldIsGlobAddr(fldHnd))
        {
            // "CLS_VAR_ADDR"s (currently only used for data section constants, which
            // are addressable IP relative) are, by contract, always relocatable.
            id->idSetIsDspReloc();
        }

        id->idAddr()->iiaFieldHnd = fldHnd;
        id->idInsFmt(emitMapFmtForIns(emitMapFmtAtoM(fmt), ins));
    }
    else if ((memBase != nullptr) && memBase->IsCnsIntOrI() && memBase->isContained())
    {
        // Absolute addresses marked as contained should fit within the base of addr mode.
        assert(memBase->AsIntConCommon()->FitsInAddrBase(emitComp));

        // If we reach here, either:
        // - we are not generating relocatable code, (typically the non-AOT JIT case)
        // - the base address is a handle represented by an integer constant,
        // - the base address is a constant zero, or
        // - the base address is a constant that fits into the memory instruction (this can happen on x86).
        //   This last case is captured in the FitsInAddrBase method which is used by Lowering to determine that it can
        //   be contained.
        //
        assert(!emitComp->opts.compReloc || memBase->IsIconHandle() || memBase->IsIntegralConst(0) ||
               memBase->AsIntConCommon()->FitsInAddrBase(emitComp));

        if (memBase->AsIntConCommon()->AddrNeedsReloc(emitComp))
        {
            id->idSetIsDspReloc();
        }

        id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
        id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;
        id->idAddr()->iiaAddrMode.amScale   = emitter::OPSZ1; // for completeness

        id->idInsFmt(emitMapFmtForIns(fmt, ins));

        // Absolute address must have already been set in the instrDesc constructor.
        assert(emitGetInsAmdAny(id) == memBase->AsIntConCommon()->IconValue());
    }
    else
    {
        regNumber amBaseReg = REG_NA;
        if (memBase != nullptr)
        {
            assert(!memBase->isContained());
            amBaseReg = memBase->GetRegNum();
            assert(amBaseReg != REG_NA);
        }

        regNumber amIndxReg = REG_NA;
        if (indir->HasIndex())
        {
            GenTree* index = indir->Index();
            assert(!index->isContained());
            amIndxReg = index->GetRegNum();
            assert(amIndxReg != REG_NA);
        }

        assert((amBaseReg != REG_NA) || (amIndxReg != REG_NA) || (indir->Offset() != 0)); // At least one should be set.
        id->idAddr()->iiaAddrMode.amBaseReg = amBaseReg;
        id->idAddr()->iiaAddrMode.amIndxReg = amIndxReg;
        id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(indir->Scale());

        id->idInsFmt(emitMapFmtForIns(fmt, ins));

        // disp must have already been set in the instrDesc constructor.
        assert(emitGetInsAmdAny(id) == indir->Offset()); // make sure "disp" is stored properly
    }
}

// Takes care of storing all incoming register parameters
// into its corresponding shadow space (defined by the x64 ABI)
void emitter::spillIntArgRegsToShadowSlots()
{
    unsigned       argNum;
    instrDesc*     id;
    UNATIVE_OFFSET sz;

    assert(emitComp->compGeneratingProlog);

    for (argNum = 0; argNum < MAX_REG_ARG; ++argNum)
    {
        regNumber argReg = intArgRegs[argNum];

        // The offsets for the shadow space start at RSP + 8
        // (right before the caller return address)
        int offset = (argNum + 1) * EA_PTRSIZE;

        id = emitNewInstrAmd(EA_PTRSIZE, offset);
        id->idIns(INS_mov);
        id->idInsFmt(IF_AWR_RRD);
        id->idAddr()->iiaAddrMode.amBaseReg = REG_SPBASE;
        id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;
        id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(1);

        // The offset has already been set in the intrDsc ctor,
        // make sure we got it right.
        assert(emitGetInsAmdAny(id) == ssize_t(offset));

        id->idReg1(argReg);
        sz = emitInsSizeAM(id, insCodeMR(INS_mov));
        id->idCodeSize(sz);
        emitCurIGsize += sz;
    }
}

//------------------------------------------------------------------------
// emitInsLoadInd: Emits a "mov reg, [mem]" (or a variant such as "movzx" or "movss")
// instruction for a GT_IND node.
//
// Arguments:
//    ins - the instruction to emit
//    attr - the instruction operand size
//    dstReg - the destination register
//    mem - the GT_IND node
//
void emitter::emitInsLoadInd(instruction ins, emitAttr attr, regNumber dstReg, GenTreeIndir* mem)
{
    assert(mem->OperIs(GT_IND, GT_NULLCHECK));

    GenTree* addr = mem->Addr();

    if (addr->OperGet() == GT_CLS_VAR_ADDR)
    {
        emitIns_R_C(ins, attr, dstReg, addr->AsClsVar()->gtClsVarHnd, 0);
        return;
    }

    if (addr->OperIsLocalAddr())
    {
        GenTreeLclVarCommon* varNode = addr->AsLclVarCommon();
        unsigned             offset  = varNode->GetLclOffs();
        emitIns_R_S(ins, attr, dstReg, varNode->GetLclNum(), offset);

        // Updating variable liveness after instruction was emitted.
        // TODO-Review: it appears that this call to genUpdateLife does nothing because it
        // returns quickly when passed GT_LCL_VAR_ADDR or GT_LCL_FLD_ADDR. Below, emitInsStoreInd
        // had similar code that replaced `varNode` with `mem` (to fix a GC hole). It might be
        // appropriate to do that here as well, but doing so showed no asm diffs, so it's not
        // clear when this scenario gets hit, at least for GC refs.
        codeGen->genUpdateLife(varNode);
        return;
    }

    assert(addr->OperIsAddrMode() || (addr->IsCnsIntOrI() && addr->isContained()) || !addr->isContained());
    ssize_t    offset = mem->Offset();
    instrDesc* id     = emitNewInstrAmd(attr, offset);
    id->idIns(ins);
    id->idReg1(dstReg);
    emitHandleMemOp(mem, id, IF_RWR_ARD, ins);
    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);
    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitInsStoreInd: Emits a "mov [mem], reg/imm" (or a variant such as "movss")
// instruction for a GT_STOREIND node.
//
// Arguments:
//    ins - the instruction to emit
//    attr - the instruction operand size
//    mem - the GT_STOREIND node
//
void emitter::emitInsStoreInd(instruction ins, emitAttr attr, GenTreeStoreInd* mem)
{
    assert(mem->OperIs(GT_STOREIND));

    GenTree* addr = mem->Addr();
    GenTree* data = mem->Data();

    if (addr->OperGet() == GT_CLS_VAR_ADDR)
    {
        if (data->isContainedIntOrIImmed())
        {
            emitIns_C_I(ins, attr, addr->AsClsVar()->gtClsVarHnd, 0, (int)data->AsIntConCommon()->IconValue());
        }
        else
        {
            assert(!data->isContained());
            emitIns_C_R(ins, attr, addr->AsClsVar()->gtClsVarHnd, data->GetRegNum(), 0);
        }
        return;
    }

    if (addr->OperIsLocalAddr())
    {
        GenTreeLclVarCommon* varNode = addr->AsLclVarCommon();
        unsigned             offset  = varNode->GetLclOffs();
        if (data->isContainedIntOrIImmed())
        {
            emitIns_S_I(ins, attr, varNode->GetLclNum(), offset, (int)data->AsIntConCommon()->IconValue());
        }
        else
        {
            assert(!data->isContained());
            emitIns_S_R(ins, attr, data->GetRegNum(), varNode->GetLclNum(), offset);
        }

        // Updating variable liveness after instruction was emitted
        codeGen->genUpdateLife(mem);
        return;
    }

    ssize_t        offset = mem->Offset();
    UNATIVE_OFFSET sz;
    instrDesc*     id;

    if (data->isContainedIntOrIImmed())
    {
        int icon = (int)data->AsIntConCommon()->IconValue();
        id       = emitNewInstrAmdCns(attr, offset, icon);
        id->idIns(ins);
        emitHandleMemOp(mem, id, IF_AWR_CNS, ins);
        sz = emitInsSizeAM(id, insCodeMI(ins), icon);
        id->idCodeSize(sz);
    }
    else
    {
        assert(!data->isContained());
        id = emitNewInstrAmd(attr, offset);
        id->idIns(ins);
        emitHandleMemOp(mem, id, IF_AWR_RRD, ins);
        id->idReg1(data->GetRegNum());
        sz = emitInsSizeAM(id, insCodeMR(ins));
        id->idCodeSize(sz);
    }

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitInsStoreLcl: Emits a "mov [mem], reg/imm" (or a variant such as "movss")
// instruction for a GT_STORE_LCL_VAR node.
//
// Arguments:
//    ins - the instruction to emit
//    attr - the instruction operand size
//    varNode - the GT_STORE_LCL_VAR node
//
void emitter::emitInsStoreLcl(instruction ins, emitAttr attr, GenTreeLclVarCommon* varNode)
{
    assert(varNode->OperIs(GT_STORE_LCL_VAR));
    assert(varNode->GetRegNum() == REG_NA); // stack store

    GenTree* data = varNode->gtGetOp1();
    codeGen->inst_set_SV_var(varNode);

    if (data->isContainedIntOrIImmed())
    {
        emitIns_S_I(ins, attr, varNode->GetLclNum(), 0, (int)data->AsIntConCommon()->IconValue());
    }
    else
    {
        assert(!data->isContained());
        emitIns_S_R(ins, attr, data->GetRegNum(), varNode->GetLclNum(), 0);
    }

    // Updating variable liveness after instruction was emitted
    codeGen->genUpdateLife(varNode);
}

//------------------------------------------------------------------------
// emitInsBinary: Emits an instruction for a node which takes two operands
//
// Arguments:
//    ins - the instruction to emit
//    attr - the instruction operand size
//    dst - the destination and first source operand
//    src - the second source operand
//
// Assumptions:
//  i) caller of this routine needs to call genConsumeReg()
// ii) caller of this routine needs to call genProduceReg()
regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
    // We can only have one memory operand and only src can be a constant operand
    // However, the handling for a given operand type (mem, cns, or other) is fairly
    // consistent regardless of whether they are src or dst. As such, we will find
    // the type of each operand and only check them against src/dst where relevant.

    GenTree* memOp   = nullptr;
    GenTree* cnsOp   = nullptr;
    GenTree* otherOp = nullptr;

    if (dst->isContained() || (dst->isLclField() && (dst->GetRegNum() == REG_NA)) || dst->isUsedFromSpillTemp())
    {
        // dst can only be a modrm
        // dst on 3opImul isn't really the dst
        assert(dst->isUsedFromMemory() || (dst->GetRegNum() == REG_NA) || instrIs3opImul(ins));
        assert(!src->isUsedFromMemory());

        memOp = dst;

        if (src->isContained())
        {
            assert(src->IsCnsIntOrI());
            cnsOp = src;
        }
        else
        {
            otherOp = src;
        }
    }
    else if (src->isContained() || src->isUsedFromSpillTemp())
    {
        assert(!dst->isUsedFromMemory());
        otherOp = dst;

        if ((src->IsCnsIntOrI() || src->IsCnsFltOrDbl()) && !src->isUsedFromSpillTemp())
        {
            assert(!src->isUsedFromMemory() || src->IsCnsFltOrDbl());
            cnsOp = src;
        }
        else
        {
            assert(src->isUsedFromMemory());
            memOp = src;
        }
    }

    // At this point, we either have a memory operand or we don't.
    //
    // If we don't then the logic is very simple and  we will either be emitting a
    // `reg, immed` instruction (if src is a cns) or a `reg, reg` instruction otherwise.
    //
    // If we do have a memory operand, the logic is a bit more complicated as we need
    // to do different things depending on the type of memory operand. These types include:
    //  * Spill temp
    //  * Indirect access
    //    * Local variable
    //    * Class variable
    //    * Addressing mode [base + index * scale + offset]
    //  * Local field
    //  * Local variable
    //
    // Most of these types (except Indirect: Class variable and Indirect: Addressing mode)
    // give us a a local variable number and an offset and access memory on the stack
    //
    // Indirect: Class variable is used for access static class variables and gives us a handle
    // to the memory location we read from
    //
    // Indirect: Addressing mode is used for the remaining memory accesses and will give us
    // a base address, an index, a scale, and an offset. These are combined to let us easily
    // access the given memory location.
    //
    // In all of the memory access cases, we determine which form to emit (e.g. `reg, [mem]`
    // or `[mem], reg`) by comparing memOp to src to determine which `emitIns_*` method needs
    // to be called. The exception is for the `[mem], immed` case (for Indirect: Class variable)
    // where only src can be the immediate.

    if (memOp != nullptr)
    {
        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (memOp->isUsedFromSpillTemp())
        {
            assert(memOp->IsRegOptional());

            tmpDsc = codeGen->getSpillTempDsc(memOp);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            codeGen->regSet.tmpRlsTemp(tmpDsc);
        }
        else if (memOp->isIndir())
        {
            GenTreeIndir* memIndir = memOp->AsIndir();
            GenTree*      memBase  = memIndir->gtOp1;

            switch (memBase->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(memBase->isContained());
                    varNum = memBase->AsLclVarCommon()->GetLclNum();
                    offset = memBase->AsLclVarCommon()->GetLclOffs();

                    // Ensure that all the GenTreeIndir values are set to their defaults.
                    assert(!memIndir->HasIndex());
                    assert(memIndir->Scale() == 1);
                    assert(memIndir->Offset() == 0);

                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    if (memOp == src)
                    {
                        assert(otherOp == dst);
                        assert(cnsOp == nullptr);

                        if (instrHasImplicitRegPairDest(ins))
                        {
                            // src is a class static variable
                            // dst is implicit - RDX:RAX
                            emitIns_C(ins, attr, memBase->AsClsVar()->gtClsVarHnd, 0);
                        }
                        else
                        {
                            // src is a class static variable
                            // dst is a register
                            emitIns_R_C(ins, attr, dst->GetRegNum(), memBase->AsClsVar()->gtClsVarHnd, 0);
                        }
                    }
                    else
                    {
                        assert(memOp == dst);

                        if (cnsOp != nullptr)
                        {
                            assert(cnsOp == src);
                            assert(otherOp == nullptr);
                            assert(src->IsCnsIntOrI());

                            // src is an contained immediate
                            // dst is a class static variable
                            emitIns_C_I(ins, attr, memBase->AsClsVar()->gtClsVarHnd, 0,
                                        (int)src->AsIntConCommon()->IconValue());
                        }
                        else
                        {
                            assert(otherOp == src);

                            // src is a register
                            // dst is a class static variable
                            emitIns_C_R(ins, attr, memBase->AsClsVar()->gtClsVarHnd, src->GetRegNum(), 0);
                        }
                    }

                    return dst->GetRegNum();
                }

                default: // Addressing mode [base + index * scale + offset]
                {
                    instrDesc* id = nullptr;

                    if (cnsOp != nullptr)
                    {
                        assert(memOp == dst);
                        assert(cnsOp == src);
                        assert(otherOp == nullptr);
                        assert(src->IsCnsIntOrI());

                        id = emitNewInstrAmdCns(attr, memIndir->Offset(), (int)src->AsIntConCommon()->IconValue());
                    }
                    else
                    {
                        ssize_t offset = memIndir->Offset();
                        id             = emitNewInstrAmd(attr, offset);
                        id->idIns(ins);

                        GenTree* regTree = (memOp == src) ? dst : src;

                        // there must be one non-contained op
                        assert(!regTree->isContained());
                        id->idReg1(regTree->GetRegNum());
                    }
                    assert(id != nullptr);

                    id->idIns(ins); // Set the instruction.

                    // Determine the instruction format
                    insFormat fmt = IF_NONE;

                    if (memOp == src)
                    {
                        assert(cnsOp == nullptr);
                        assert(otherOp == dst);

                        if (instrHasImplicitRegPairDest(ins))
                        {
                            fmt = emitInsModeFormat(ins, IF_ARD);
                        }
                        else
                        {
                            fmt = emitInsModeFormat(ins, IF_RRD_ARD);
                        }
                    }
                    else
                    {
                        assert(memOp == dst);

                        if (cnsOp != nullptr)
                        {
                            assert(cnsOp == src);
                            assert(otherOp == nullptr);
                            assert(src->IsCnsIntOrI());

                            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
                        }
                        else
                        {
                            assert(otherOp == src);
                            fmt = emitInsModeFormat(ins, IF_ARD_RRD);
                        }
                    }
                    assert(fmt != IF_NONE);
                    emitHandleMemOp(memIndir, id, fmt, ins);

                    // Determine the instruction size
                    UNATIVE_OFFSET sz = 0;

                    if (memOp == src)
                    {
                        assert(otherOp == dst);
                        assert(cnsOp == nullptr);

                        if (instrHasImplicitRegPairDest(ins))
                        {
                            sz = emitInsSizeAM(id, insCode(ins));
                        }
                        else
                        {
                            sz = emitInsSizeAM(id, insCodeRM(ins));
                        }
                    }
                    else
                    {
                        assert(memOp == dst);

                        if (cnsOp != nullptr)
                        {
                            assert(memOp == dst);
                            assert(cnsOp == src);
                            assert(otherOp == nullptr);

                            sz = emitInsSizeAM(id, insCodeMI(ins), (int)src->AsIntConCommon()->IconValue());
                        }
                        else
                        {
                            assert(otherOp == src);
                            sz = emitInsSizeAM(id, insCodeMR(ins));
                        }
                    }
                    assert(sz != 0);

                    id->idCodeSize(sz);

                    dispIns(id);
                    emitCurIGsize += sz;

                    return (memOp == src) ? dst->GetRegNum() : REG_NA;
                }
            }
        }
        else
        {
            switch (memOp->OperGet())
            {
                case GT_LCL_FLD:
                case GT_STORE_LCL_FLD:
                    varNum = memOp->AsLclFld()->GetLclNum();
                    offset = memOp->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(memOp->IsRegOptional() ||
                           !emitComp->lvaTable[memOp->AsLclVar()->GetLclNum()].lvIsRegCandidate());
                    varNum = memOp->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                default:
                    unreached();
                    break;
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        if (memOp == src)
        {
            assert(otherOp == dst);
            assert(cnsOp == nullptr);

            if (instrHasImplicitRegPairDest(ins))
            {
                // src is a stack based local variable
                // dst is implicit - RDX:RAX
                emitIns_S(ins, attr, varNum, offset);
            }
            else
            {
                // src is a stack based local variable
                // dst is a register
                emitIns_R_S(ins, attr, dst->GetRegNum(), varNum, offset);
            }
        }
        else
        {
            assert(memOp == dst);
            assert((dst->GetRegNum() == REG_NA) || dst->IsRegOptional());

            if (cnsOp != nullptr)
            {
                assert(cnsOp == src);
                assert(otherOp == nullptr);
                assert(src->IsCnsIntOrI());

                // src is an contained immediate
                // dst is a stack based local variable
                emitIns_S_I(ins, attr, varNum, offset, (int)src->AsIntConCommon()->IconValue());
            }
            else
            {
                assert(otherOp == src);
                assert(!src->isContained());

                // src is a register
                // dst is a stack based local variable
                emitIns_S_R(ins, attr, src->GetRegNum(), varNum, offset);
            }
        }
    }
    else if (cnsOp != nullptr) // reg, immed
    {
        assert(cnsOp == src);
        assert(otherOp == dst);

        if (src->IsCnsIntOrI())
        {
            assert(!dst->isContained());
            GenTreeIntConCommon* intCns = src->AsIntConCommon();
            emitIns_R_I(ins, attr, dst->GetRegNum(), intCns->IconValue());
        }
        else
        {
            assert(src->IsCnsFltOrDbl());
            GenTreeDblCon* dblCns = src->AsDblCon();

            CORINFO_FIELD_HANDLE hnd = emitFltOrDblConst(dblCns->gtDconVal, emitTypeSize(dblCns));
            emitIns_R_C(ins, attr, dst->GetRegNum(), hnd, 0);
        }
    }
    else // reg, reg
    {
        assert(otherOp == nullptr);
        assert(!src->isContained() && !dst->isContained());

        if (instrHasImplicitRegPairDest(ins))
        {
            emitIns_R(ins, attr, src->GetRegNum());
        }
        else
        {
            emitIns_R_R(ins, attr, dst->GetRegNum(), src->GetRegNum());
        }
    }

    return dst->GetRegNum();
}

//------------------------------------------------------------------------
// emitInsRMW: Emit logic for Read-Modify-Write binary instructions.
//
// Responsible for emitting a single instruction that will perform an operation of the form:
//      *addr = *addr <BinOp> src
// For example:
//      ADD [RAX], RCX
//
// Arguments:
//    ins - instruction to generate
//    attr - emitter attribute for instruction
//    storeInd - indir for RMW addressing mode
//    src - source operand of instruction
//
// Assumptions:
//    Lowering has taken care of recognizing the StoreInd pattern of:
//          StoreInd( AddressTree, BinOp( Ind ( AddressTree ), Operand ) )
//    The address to store is already sitting in a register.
//
// Notes:
//    This is a no-produce operation, meaning that no register output will
//    be produced for future use in the code stream.
//
void emitter::emitInsRMW(instruction ins, emitAttr attr, GenTreeStoreInd* storeInd, GenTree* src)
{
    GenTree* addr = storeInd->Addr();
    addr          = addr->gtSkipReloadOrCopy();
    assert(addr->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_LEA, GT_CLS_VAR_ADDR, GT_CNS_INT));

    instrDesc*     id = nullptr;
    UNATIVE_OFFSET sz;

    ssize_t offset = 0;
    if (addr->OperGet() != GT_CLS_VAR_ADDR)
    {
        offset = storeInd->Offset();
    }

    if (src->isContainedIntOrIImmed())
    {
        GenTreeIntConCommon* intConst = src->AsIntConCommon();
        int                  iconVal  = (int)intConst->IconValue();
        switch (ins)
        {
            case INS_rcl_N:
            case INS_rcr_N:
            case INS_rol_N:
            case INS_ror_N:
            case INS_shl_N:
            case INS_shr_N:
            case INS_sar_N:
                iconVal &= 0x7F;
                break;
            default:
                break;
        }

        if (addr->isContained() && addr->OperIsLocalAddr())
        {
            GenTreeLclVarCommon* lclVar = addr->AsLclVarCommon();
            emitIns_S_I(ins, attr, lclVar->GetLclNum(), lclVar->GetLclOffs(), iconVal);
            return;
        }
        else
        {
            id = emitNewInstrAmdCns(attr, offset, iconVal);
            emitHandleMemOp(storeInd, id, IF_ARW_CNS, ins);
            id->idIns(ins);
            sz = emitInsSizeAM(id, insCodeMI(ins), iconVal);
        }
    }
    else
    {
        assert(!src->isContained()); // there must be one non-contained src

        // ind, reg
        id = emitNewInstrAmd(attr, offset);
        emitHandleMemOp(storeInd, id, IF_ARW_RRD, ins);
        id->idReg1(src->GetRegNum());
        id->idIns(ins);
        sz = emitInsSizeAM(id, insCodeMR(ins));
    }

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitInsRMW: Emit logic for Read-Modify-Write unary instructions.
//
// Responsible for emitting a single instruction that will perform an operation of the form:
//      *addr = UnaryOp *addr
// For example:
//      NOT [RAX]
//
// Arguments:
//    ins - instruction to generate
//    attr - emitter attribute for instruction
//    storeInd - indir for RMW addressing mode
//
// Assumptions:
//    Lowering has taken care of recognizing the StoreInd pattern of:
//          StoreInd( AddressTree, UnaryOp( Ind ( AddressTree ) ) )
//    The address to store is already sitting in a register.
//
// Notes:
//    This is a no-produce operation, meaning that no register output will
//    be produced for future use in the code stream.
//
void emitter::emitInsRMW(instruction ins, emitAttr attr, GenTreeStoreInd* storeInd)
{
    GenTree* addr = storeInd->Addr();
    addr          = addr->gtSkipReloadOrCopy();
    assert(addr->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_CLS_VAR_ADDR, GT_LEA, GT_CNS_INT));

    ssize_t offset = 0;
    if (addr->OperGet() != GT_CLS_VAR_ADDR)
    {
        offset = storeInd->Offset();
    }

    if (addr->isContained() && addr->OperIsLocalAddr())
    {
        GenTreeLclVarCommon* lclVar = addr->AsLclVarCommon();
        emitIns_S(ins, attr, lclVar->GetLclNum(), lclVar->GetLclOffs());
        return;
    }

    instrDesc* id = emitNewInstrAmd(attr, offset);
    emitHandleMemOp(storeInd, id, IF_ARW, ins);
    id->idIns(ins);
    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction referencing a single register.
 */

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    emitAttr size = EA_SIZE(attr);

    assert(size <= EA_PTRSIZE);
    noway_assert(emitVerifyEncodable(ins, size, reg));

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrSmall(attr);

    switch (ins)
    {
        case INS_inc:
        case INS_dec:
#ifdef TARGET_AMD64

            sz = 2; // x64 has no 1-byte opcode (it is the same encoding as the REX prefix)

#else // !TARGET_AMD64

            if (size == EA_1BYTE)
                sz = 2; // Use the long form as the small one has no 'w' bit
            else
                sz = 1; // Use short form

#endif // !TARGET_AMD64

            break;

        case INS_pop:
        case INS_pop_hide:
        case INS_push:
        case INS_push_hide:

            /* We don't currently push/pop small values */

            assert(size == EA_PTRSIZE);

            sz = 1;
            break;

        default:

            /* All the sixteen INS_setCCs are contiguous. */

            if (INS_seto <= ins && ins <= INS_setg)
            {
                // Rough check that we used the endpoints for the range check

                assert(INS_seto + 0xF == INS_setg);

                // The caller must specify EA_1BYTE for 'attr'

                assert(attr == EA_1BYTE);

                /* We expect this to always be a 'big' opcode */

                assert(insEncodeMRreg(ins, reg, attr, insCodeMR(ins)) & 0x00FF0000);

                size = attr;

                sz = 3;
                break;
            }
            else
            {
                sz = 2;
                break;
            }
    }
    insFormat fmt = emitInsModeFormat(ins, IF_RRD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);

    // Vex bytes
    sz += emitGetAdjustedSize(ins, attr, insEncodeMRreg(ins, reg, attr, insCodeMR(ins)));

    // REX byte
    if (IsExtendedReg(reg, attr) || TakesRexWPrefix(ins, attr))
    {
        sz += emitGetRexPrefixSize(ins);
    }

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t val DEBUGARG(GenTreeFlags gtFlags))
{
    emitAttr size = EA_SIZE(attr);

    // Allow emitting SSE2/AVX SIMD instructions of R_I form that can specify EA_16BYTE or EA_32BYTE
    assert(size <= EA_PTRSIZE || IsSSEOrAVXInstruction(ins));

    noway_assert(emitVerifyEncodable(ins, size, reg));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(size < EA_8BYTE || ins == INS_mov || ((int)val == val && !EA_IS_CNS_RELOC(attr)));
#endif

    UNATIVE_OFFSET sz;
    instrDesc*     id;
    insFormat      fmt       = emitInsModeFormat(ins, IF_RRD_CNS);
    bool           valInByte = ((signed char)val == (target_ssize_t)val) && (ins != INS_mov) && (ins != INS_test);

    // BT reg,imm might be useful but it requires special handling of the immediate value
    // (it is always encoded in a byte). Let's not complicate things until this is needed.
    assert(ins != INS_bt);

    // Figure out the size of the instruction
    switch (ins)
    {
        case INS_mov:
#ifdef TARGET_AMD64
            // mov reg, imm64 is equivalent to mov reg, imm32 if the high order bits are all 0
            // and this isn't a reloc constant.
            if (((size > EA_4BYTE) && (0 == (val & 0xFFFFFFFF00000000LL))) && !EA_IS_CNS_RELOC(attr))
            {
                attr = size = EA_4BYTE;
            }

            if (size > EA_4BYTE)
            {
                sz = 9; // Really it is 10, but we'll add one more later
                break;
            }
#endif // TARGET_AMD64
            sz = 5;
            break;

        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_RRW_SHF;
            sz  = 3;
            val &= 0x7F;
            valInByte = true; // shift amount always placed in a byte
            break;

        default:

            if (EA_IS_CNS_RELOC(attr))
            {
                valInByte = false; // relocs can't be placed in a byte
            }

            if (valInByte)
            {
                if (IsSSEOrAVXInstruction(ins))
                {
                    bool includeRexPrefixSize = true;
                    // Do not get the RexSize() but just decide if it will be included down further and if yes,
                    // do not include it again.
                    if (IsExtendedReg(reg, attr) || TakesRexWPrefix(ins, size) || instrIsExtendedReg3opImul(ins))
                    {
                        includeRexPrefixSize = false;
                    }

                    sz = emitInsSize(insCodeMI(ins), includeRexPrefixSize);
                    sz += 1;
                }
                else if (size == EA_1BYTE && reg == REG_EAX && !instrIs3opImul(ins))
                {
                    sz = 2;
                }
                else
                {
                    sz = 3;
                }
            }
            else
            {
                assert(!IsSSEOrAVXInstruction(ins));

                if (reg == REG_EAX && !instrIs3opImul(ins))
                {
                    sz = 1;
                }
                else
                {
                    sz = 2;
                }

#ifdef TARGET_AMD64
                if (size > EA_4BYTE)
                {
                    // We special-case anything that takes a full 8-byte constant.
                    sz += 4;
                }
                else
#endif // TARGET_AMD64
                {
                    sz += EA_SIZE_IN_BYTES(attr);
                }
            }
            break;
    }

    sz += emitGetAdjustedSize(ins, attr, insCodeMI(ins));

    // Do we need a REX prefix for AMD64? We need one if we are using any extended register (REX.R), or if we have a
    // 64-bit sized operand (REX.W). Note that IMUL in our encoding is special, with a "built-in", implicit, target
    // register. So we also need to check if that built-in register is an extended register.
    if (IsExtendedReg(reg, attr) || TakesRexWPrefix(ins, size) || instrIsExtendedReg3opImul(ins))
    {
        sz += emitGetRexPrefixSize(ins);
    }

    id = emitNewInstrSC(attr, val);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);
    id->idCodeSize(sz);
    INDEBUG(id->idDebugOnlyInfo()->idFlags = gtFlags);

    dispIns(id);
    emitCurIGsize += sz;

    if (reg == REG_ESP)
    {
        emitAdjustStackDepth(ins, val);
    }
}

/*****************************************************************************
 *
 *  Add an instruction referencing an integer constant.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t val)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id;
    bool           valInByte = ((signed char)val == (target_ssize_t)val);

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    if (EA_IS_CNS_RELOC(attr))
    {
        valInByte = false; // relocs can't be placed in a byte
    }

    switch (ins)
    {
        case INS_loop:
        case INS_jge:
            sz = 2;
            break;

        case INS_ret:
            sz = 3;
            break;

        case INS_push_hide:
        case INS_push:
            sz = valInByte ? 2 : 5;
            break;

        default:
            NO_WAY("unexpected instruction");
    }

    id = emitNewInstrSC(attr, val);
    id->idIns(ins);
    id->idInsFmt(IF_CNS);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

/*****************************************************************************
 *
 *  Add a "jump through a table" instruction.
 */

void emitter::emitIns_IJ(emitAttr attr, regNumber reg, unsigned base)
{
    assert(EA_SIZE(attr) == EA_4BYTE);

    UNATIVE_OFFSET    sz  = 3 + 4;
    const instruction ins = INS_i_jmp;

    if (IsExtendedReg(reg, attr))
    {
        sz += emitGetRexPrefixSize(ins);
    }

    instrDesc* id = emitNewInstrAmd(attr, base);

    id->idIns(ins);
    id->idInsFmt(IF_ARD);
    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = reg;
    id->idAddr()->iiaAddrMode.amScale   = emitter::OPSZP;

#ifdef DEBUG
    id->idDebugOnlyInfo()->idMemCookie = base;
#endif

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with a static data member operand. If 'size' is 0, the
 *  instruction operates on the address of the static member instead of its
 *  value (e.g. "push offset clsvar", rather than "push dword ptr [clsvar]").
 */

void emitter::emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id;

    /* Are we pushing the offset of the class variable? */

    if (EA_IS_OFFSET(attr))
    {
        assert(ins == INS_push);
        sz = 1 + TARGET_POINTER_SIZE;

        id = emitNewInstrDsp(EA_1BYTE, offs);
        id->idIns(ins);
        id->idInsFmt(IF_MRD_OFF);
    }
    else
    {
        insFormat fmt = emitInsModeFormat(ins, IF_MRD);

        id = emitNewInstrDsp(attr, offs);
        id->idIns(ins);
        id->idInsFmt(fmt);
        sz = emitInsSizeCV(id, insCodeMR(ins));
    }

    if (TakesRexWPrefix(ins, attr))
    {
        // REX.W prefix
        sz += emitGetRexPrefixSize(ins);
    }

    id->idAddr()->iiaFieldHnd = fldHnd;

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

//------------------------------------------------------------------------
// emitIns_A: Emit an instruction with one memory ("[address mode]") operand.
//
// Arguments:
//    ins   - The instruction to emit
//    attr  - The corresponding emit attribute
//    indir - The memory operand, represented as an indirection tree
//
void emitter::emitIns_A(instruction ins, emitAttr attr, GenTreeIndir* indir)
{
    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmd(attr, offs);
    insFormat  fmt  = emitInsModeFormat(ins, IF_ARD);

    id->idIns(ins);
    emitHandleMemOp(indir, id, fmt, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

//------------------------------------------------------------------------
// IsMovInstruction: Determines whether a give instruction is a move instruction
//
// Arguments:
//    ins       -- The instruction being checked
//
// Return Value:
//    true if the instruction is a qualifying move instruction; otherwise, false
//
// Remarks:
//    This methods covers most kinds of two operand move instructions that copy a
//    value between two registers. It does not cover all move-like instructions
//    and so doesn't currently cover things like movsb/movsw/movsd/movsq or cmovcc
//    and doesn't currently cover cases where a value is read/written from memory.
//
//    The reason it doesn't cover all instructions was namely to limit the scope
//    of the initial change to that which was impactful to move elision so that
//    it could be centrally managed and optimized. It may be beneficial to support
//    the other move instructions in the future but that may require more extensive
//    changes to ensure relevant codegen/emit paths flow and check things correctly.
bool emitter::IsMovInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_mov:
        case INS_movapd:
        case INS_movaps:
        case INS_movd:
        case INS_movdqa:
        case INS_movdqu:
        case INS_movsdsse2:
        case INS_movss:
        case INS_movsx:
        case INS_movupd:
        case INS_movups:
        case INS_movzx:
        {
            return true;
        }

#if defined(TARGET_AMD64)
        case INS_movq:
        case INS_movsxd:
        {
            return true;
        }
#endif // TARGET_AMD64

        default:
        {
            return false;
        }
    }
}

//------------------------------------------------------------------------
// IsJccInstruction: Determine if an instruction is a conditional jump instruction.
//
// Arguments:
//    ins       -- The instruction being checked
//
// Return Value:
//    true if the instruction qualifies; otherwise, false
//
bool emitter::IsJccInstruction(instruction ins)
{
    return ((ins >= INS_jo) && (ins <= INS_jg)) || ((ins >= INS_l_jo) && (ins <= INS_l_jg));
}

//------------------------------------------------------------------------
// IsJmpInstruction: Determine if an instruction is a jump instruction but NOT a conditional jump instruction.
//
// Arguments:
//    ins       -- The instruction being checked
//
// Return Value:
//    true if the instruction qualifies; otherwise, false
//
bool emitter::IsJmpInstruction(instruction ins)
{
    return (ins == INS_i_jmp) || (ins == INS_jmp) || (ins == INS_l_jmp) || (ins == INS_tail_i_jmp);
}

// TODO-XArch-CQ: There are places where the fact that an instruction zero-extends
// is not an important detail, such as when "regular" floating-point code is generated
//
// This differs from cases like HWIntrinsics that deal with the entire vector and so
// they need to be "aware" that a given move impacts the upper-bits.
//
// Ideally we can detect this difference, likely via canIgnoreSideEffects, and allow
// the below optimizations for those scenarios as well.

// Track whether the instruction has a zero/sign-extension or clearing of the upper-bits as a side-effect
bool emitter::HasSideEffect(instruction ins, emitAttr size)
{
    bool hasSideEffect = false;

    switch (ins)
    {
        case INS_mov:
        {
            // non EA_PTRSIZE moves may zero-extend the source
            hasSideEffect = (size != EA_PTRSIZE);
            break;
        }

        case INS_movapd:
        case INS_movaps:
        case INS_movdqa:
        case INS_movdqu:
        case INS_movupd:
        case INS_movups:
        {
            // non EA_32BYTE moves clear the upper bits under VEX encoding
            hasSideEffect = UseVEXEncoding() && (size != EA_32BYTE);
            break;
        }

        case INS_movd:
        {
            // Clears the upper bits
            hasSideEffect = true;
            break;
        }

        case INS_movsdsse2:
        case INS_movss:
        {
            // Clears the upper bits under VEX encoding
            hasSideEffect = UseVEXEncoding();
            break;
        }

        case INS_movsx:
        case INS_movzx:
        {
            // Sign/Zero-extends the source
            hasSideEffect = true;
            break;
        }

#if defined(TARGET_AMD64)
        case INS_movq:
        {
            // Clears the upper bits
            hasSideEffect = true;
            break;
        }

        case INS_movsxd:
        {
            // Sign-extends the source
            hasSideEffect = true;
            break;
        }
#endif // TARGET_AMD64

        default:
        {
            unreached();
        }
    }

    return hasSideEffect;
}

//----------------------------------------------------------------------------------------
// IsRedundantMov:
//    Check if the current `mov` instruction is redundant and can be omitted.
//    A `mov` is redundant in following 3 cases:
//
//    1. Move to same register on TARGET_AMD64
//       (Except 4-byte movement like "mov eax, eax" which zeros out upper bits of eax register)
//
//         mov rax, rax
//
//    2. Move that is identical to last instruction emitted.
//
//         mov rax, rbx  # <-- last instruction
//         mov rax, rbx  # <-- current instruction can be omitted.
//
//    3. Opposite Move as that of last instruction emitted.
//
//         mov rax, rbx  # <-- last instruction
//         mov rbx, rax  # <-- current instruction can be omitted.
//
// Arguments:
//                 ins  - The current instruction
//                 fmt  - The current format
//                 size - Operand size of current instruction
//                 dst  - The current destination
//                 src  - The current source
// canIgnoreSideEffects - The move can be skipped as it doesn't represent special semantics
//
// Return Value:
//    true if the move instruction is redundant; otherwise, false.

bool emitter::IsRedundantMov(
    instruction ins, insFormat fmt, emitAttr size, regNumber dst, regNumber src, bool canIgnoreSideEffects)
{
    assert(IsMovInstruction(ins));

    if (canIgnoreSideEffects && (dst == src))
    {
        // These elisions used to be explicit even when optimizations were disabled

        // Some instructions have a side effect and shouldn't be skipped
        // however existing codepaths were skipping these instructions in
        // certain scenarios and so we skip them as well for back-compat
        // when canIgnoreSideEffects is true (see below for which have a
        // side effect).
        //
        // Long term, these paths should be audited and should likely be
        // replaced with copies rather than extensions.
        return true;
    }

    if (!emitComp->opts.OptimizationEnabled())
    {
        // The remaining move elisions should only happen if optimizations are enabled
        return false;
    }

    // Skip optimization if current instruction creates a GC live value.
    if (EA_IS_GCREF_OR_BYREF(size))
    {
        return false;
    }

    bool hasSideEffect = HasSideEffect(ins, size);

    // Check if we are already in the correct register and don't have a side effect
    if ((dst == src) && !hasSideEffect)
    {
        JITDUMP("\n -- suppressing mov because src and dst is same register and the mov has no side-effects.\n");
        return true;
    }

    bool isFirstInstrInBlock = (emitCurIGinsCnt == 0) && ((emitCurIG->igFlags & IGF_EXTEND) == 0);

    // TODO-XArch-CQ: Certain instructions, such as movaps vs movups, are equivalent in
    // functionality even if their actual identifier differs and we should optimize these

    if (isFirstInstrInBlock ||               // Don't optimize if instruction is the first instruction in IG.
        (emitLastIns == nullptr) ||          // or if a last instruction doesn't exist
        (emitLastIns->idIns() != ins) ||     // or if the instruction is different from the last instruction
        (emitLastIns->idOpSize() != size) || // or if the operand size is different from the last instruction
        (emitLastIns->idInsFmt() != fmt))    // or if the format is different from the last instruction
    {
        return false;
    }

    regNumber lastDst = emitLastIns->idReg1();
    regNumber lastSrc = emitLastIns->idReg2();

    // Check if we did same move in last instruction, side effects don't matter since they already happened
    if ((lastDst == dst) && (lastSrc == src))
    {
        JITDUMP("\n -- suppressing mov because last instruction already moved from src to dst register.\n");
        return true;
    }

    // Check if we did a switched mov in the last instruction  and don't have a side effect
    if ((lastDst == src) && (lastSrc == dst) && !hasSideEffect)
    {
        JITDUMP("\n -- suppressing mov because last instruction already moved from dst to src register and the mov has "
                "no side-effects.\n");
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// EmitMovsxAsCwde: try to emit "movsxd rax, eax" and "movsx eax, ax" as
//                  "cdqe" and "cwde" as a code size optimization.
//
// Arguments:
//    ins  - The instruction for the original mov
//    size - The size of the original mov
//    dst  - The destination register for the original mov
//    src  - The source register for the original mov
//
// Return Value:
//    "true" if the optimization succeded, in which case the instruction can be
//    counted as emitted, "false" otherwise.
//
bool emitter::EmitMovsxAsCwde(instruction ins, emitAttr size, regNumber dst, regNumber src)
{
    if ((src == REG_EAX) && (src == dst))
    {
#ifdef TARGET_64BIT
        // "movsxd rax, eax".
        if ((ins == INS_movsxd) && (size == EA_4BYTE))
        {
            // "cdqe".
            emitIns(INS_cwde, EA_8BYTE);
            return true;
        }
#endif
        // "movsx eax, ax".
        if ((ins == INS_movsx) && (size == EA_2BYTE))
        {
            // "cwde".
            emitIns(INS_cwde, EA_4BYTE);
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// emitIns_Mov: Emits a move instruction
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    dstReg    -- The destination register
//    srcReg    -- The source register
//    canSkip   -- true if the move can be elided when dstReg == srcReg, otherwise false
//
void emitter::emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip)
{
    // Only move instructions can use emitIns_Mov
    assert(IsMovInstruction(ins));

#if DEBUG
    switch (ins)
    {
        case INS_mov:
        case INS_movsx:
        case INS_movzx:
        {
            assert(isGeneralRegister(dstReg) && isGeneralRegister(srcReg));
            break;
        }

        case INS_movapd:
        case INS_movaps:
        case INS_movdqa:
        case INS_movdqu:
        case INS_movsdsse2:
        case INS_movss:
        case INS_movupd:
        case INS_movups:
        {
            assert(isFloatReg(dstReg) && isFloatReg(srcReg));
            break;
        }

        case INS_movd:
        {
            assert(isFloatReg(dstReg) != isFloatReg(srcReg));
            break;
        }

#if defined(TARGET_AMD64)
        case INS_movq:
        {
            assert(isFloatReg(dstReg) && isFloatReg(srcReg));
            break;
        }

        case INS_movsxd:
        {
            assert(isGeneralRegister(dstReg) && isGeneralRegister(srcReg));
            break;
        }
#endif // TARGET_AMD64

        default:
        {
            unreached();
        }
    }
#endif

    emitAttr size = EA_SIZE(attr);

    assert(size <= EA_32BYTE);
    noway_assert(emitVerifyEncodable(ins, size, dstReg, srcReg));

    insFormat fmt = emitInsModeFormat(ins, IF_RRD_RRD);

    if (IsRedundantMov(ins, fmt, attr, dstReg, srcReg, canSkip))
    {
        return;
    }

    if (EmitMovsxAsCwde(ins, size, dstReg, srcReg))
    {
        return;
    }

    UNATIVE_OFFSET sz = emitInsSizeRR(ins, dstReg, srcReg, attr);

    instrDesc* id = emitNewInstrSmall(attr);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(dstReg);
    id->idReg2(srcReg);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with two register operands.
 */

void emitter::emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2)
{
    if (IsMovInstruction(ins))
    {
        assert(!"Please use emitIns_Mov() to correctly handle move elision");
        emitIns_Mov(ins, attr, reg1, reg2, /* canSkip */ false);
    }

    emitAttr size = EA_SIZE(attr);

    assert(size <= EA_32BYTE);
    noway_assert(emitVerifyEncodable(ins, size, reg1, reg2));

    UNATIVE_OFFSET sz = emitInsSizeRR(ins, reg1, reg2, attr);

    /* Special case: "XCHG" uses a different format */
    insFormat fmt = (ins == INS_xchg) ? IF_RRW_RRW : emitInsModeFormat(ins, IF_RRD_RRD);

    instrDesc* id = emitNewInstrSmall(attr);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with two register operands and an integer constant.
 */

void emitter::emitIns_R_R_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int ival)
{
#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    instrDesc* id = emitNewInstrSC(attr, ival);

    id->idIns(ins);
    id->idInsFmt(IF_RRW_RRW_CNS);
    id->idReg1(reg1);
    id->idReg2(reg2);

    code_t code = 0;

    switch (ins)
    {
        case INS_pextrb:
        case INS_pextrd:
        case INS_pextrq:
        case INS_pextrw_sse41:
        case INS_extractps:
        case INS_vextractf128:
        case INS_vextracti128:
        case INS_shld:
        case INS_shrd:
        {
            code = insCodeMR(ins);
            break;
        }

        case INS_psrldq:
        case INS_pslldq:
        {
            code = insCodeMI(ins);
            break;
        }

        default:
        {
            code = insCodeRM(ins);
            break;
        }
    }

    UNATIVE_OFFSET sz = emitInsSizeRR(id, code, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_AR(instruction ins, emitAttr attr, regNumber base, int offs)
{
    assert(ins == INS_prefetcht0 || ins == INS_prefetcht1 || ins == INS_prefetcht2 || ins == INS_prefetchnta);

    instrDesc* id = emitNewInstrAmd(attr, offs);

    id->idIns(ins);

    id->idInsFmt(IF_ARD);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitIns_AR_R_R: emits the code for an instruction that takes a base memory register, two register operands
//                 and that does not return a value
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op2Reg    -- The register of the second operand
//    op3Reg    -- The register of the third operand
//    base      -- The base register used for the memory address (first operand)
//    offs      -- The offset from base
//
void emitter::emitIns_AR_R_R(
    instruction ins, emitAttr attr, regNumber op2Reg, regNumber op3Reg, regNumber base, int offs)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstrAmd(attr, offs);

    id->idIns(ins);
    id->idReg1(op2Reg);
    id->idReg2(op3Reg);

    id->idInsFmt(IF_AWR_RRD_RRD);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_A(instruction ins, emitAttr attr, regNumber reg1, GenTreeIndir* indir)
{
    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmd(attr, offs);

    id->idIns(ins);
    id->idReg1(reg1);

    emitHandleMemOp(indir, id, IF_RRW_ARD, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_A_I(instruction ins, emitAttr attr, regNumber reg1, GenTreeIndir* indir, int ival)
{
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg1));
    assert(IsSSEOrAVXInstruction(ins));

    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(reg1);

    emitHandleMemOp(indir, id, IF_RRW_ARD_CNS, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_AR_I(instruction ins, emitAttr attr, regNumber reg1, regNumber base, int offs, int ival)
{
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg1));
    assert(IsSSEOrAVXInstruction(ins));

    instrDesc* id = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(reg1);

    id->idInsFmt(IF_RRW_ARD_CNS);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_C_I(
    instruction ins, emitAttr attr, regNumber reg1, CORINFO_FIELD_HANDLE fldHnd, int offs, int ival)
{
    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg1));
    assert(IsSSEOrAVXInstruction(ins));

    instrDesc* id = emitNewInstrCnsDsp(attr, ival, offs);

    id->idIns(ins);
    id->idInsFmt(IF_RRW_MRD_CNS);
    id->idReg1(reg1);
    id->idAddr()->iiaFieldHnd = fldHnd;

    UNATIVE_OFFSET sz = emitInsSizeCV(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_S_I(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs, int ival)
{
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg1));
    assert(IsSSEOrAVXInstruction(ins));

    instrDesc* id = emitNewInstrCns(attr, ival);

    id->idIns(ins);
    id->idInsFmt(IF_RRW_SRD_CNS);
    id->idReg1(reg1);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeRM(ins), varx, offs, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_A(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, GenTreeIndir* indir)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmd(attr, offs);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    emitHandleMemOp(indir, id, IF_RWR_RRD_ARD, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_AR(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber base, int offs)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstrAmd(attr, offs);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    id->idInsFmt(IF_RWR_RRD_ARD);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// IsAVX2GatherInstruction: return true if the instruction is AVX2 Gather
//
// Arguments:
//    ins - the instruction to check
// Return Value:
//    true if the instruction is AVX2 Gather
//
bool IsAVX2GatherInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_vpgatherdd:
        case INS_vpgatherdq:
        case INS_vpgatherqd:
        case INS_vpgatherqq:
        case INS_vgatherdps:
        case INS_vgatherdpd:
        case INS_vgatherqps:
        case INS_vgatherqpd:
            return true;
        default:
            return false;
    }
}

//------------------------------------------------------------------------
// emitIns_R_AR_R: Emits an AVX2 Gather instructions
//
// Arguments:
//    ins - the instruction to emit
//    attr - the instruction operand size
//    reg1 - the destination and first source operand
//    reg2 - the mask operand (encoded in VEX.vvvv)
//    base - the base register of address to load
//    index - the index register of VSIB
//    scale - the scale number of VSIB
//    offs - the offset added to the memory address from base
//
void emitter::emitIns_R_AR_R(instruction ins,
                             emitAttr    attr,
                             regNumber   reg1,
                             regNumber   reg2,
                             regNumber   base,
                             regNumber   index,
                             int         scale,
                             int         offs)
{
    assert(IsAVX2GatherInstruction(ins));

    instrDesc* id = emitNewInstrAmd(attr, offs);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    id->idInsFmt(IF_RWR_ARD_RRD);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = index;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeSize((emitAttr)scale);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_C(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    instrDesc* id = emitNewInstrDsp(attr, offs);

    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_MRD);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaFieldHnd = fldHnd;

    UNATIVE_OFFSET sz = emitInsSizeCV(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
*
*  Add an instruction with three register operands.
*/

void emitter::emitIns_R_R_R(instruction ins, emitAttr attr, regNumber targetReg, regNumber reg1, regNumber reg2)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstr(attr);
    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_RRD);
    id->idReg1(targetReg);
    id->idReg2(reg1);
    id->idReg3(reg2);

    UNATIVE_OFFSET sz = emitInsSizeRR(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_S(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int varx, int offs)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_SRD);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeRM(ins), varx, offs);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_A_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, GenTreeIndir* indir, int ival, insFormat fmt)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    emitHandleMemOp(indir, id, fmt, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_AR_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber base, int offs, int ival)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);

    id->idInsFmt(IF_RWR_RRD_ARD_CNS);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_C_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, CORINFO_FIELD_HANDLE fldHnd, int offs, int ival)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    instrDesc* id = emitNewInstrCnsDsp(attr, ival, offs);

    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_MRD_CNS);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaFieldHnd = fldHnd;

    UNATIVE_OFFSET sz = emitInsSizeCV(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/**********************************************************************************
* emitIns_R_R_R_I: Add an instruction with three register operands and an immediate.
*
* Arguments:
*    ins       - the instruction to add
*    attr      - the emitter attribute for instruction
*    targetReg - the target (destination) register
*    reg1      - the first source register
*    reg2      - the second source register
*    ival      - the immediate value
*/

void emitter::emitIns_R_R_R_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber reg1, regNumber reg2, int ival)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstrCns(attr, ival);
    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_RRD_CNS);
    id->idReg1(targetReg);
    id->idReg2(reg1);
    id->idReg3(reg2);

    code_t code = 0;

    switch (ins)
    {
        case INS_pextrb:
        case INS_pextrd:
        case INS_pextrq:
        case INS_pextrw_sse41:
        case INS_extractps:
        case INS_vextractf128:
        case INS_vextracti128:
        {
            code = insCodeMR(ins);
            break;
        }

        case INS_psrldq:
        case INS_pslldq:
        {
            code = insCodeMI(ins);
            break;
        }

        default:
        {
            code = insCodeRM(ins);
            break;
        }
    }

    UNATIVE_OFFSET sz = emitInsSizeRR(id, code, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_S_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int varx, int offs, int ival)
{
    assert(IsSSEOrAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins));

    instrDesc* id = emitNewInstrCns(attr, ival);

    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_SRD_CNS);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeRM(ins), varx, offs, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// encodeXmmRegAsIval: Encodes a XMM register into imm[7:4] for use by a SIMD instruction
//
// Arguments
//    opReg -- The register being encoded
//
// Returns:
//    opReg encoded in imm[7:4]
static int encodeXmmRegAsIval(regNumber opReg)
{
    // AVX/AVX2 supports 4-reg format for vblendvps/vblendvpd/vpblendvb,
    // which encodes the fourth register into imm8[7:4]
    assert(opReg >= XMMBASE);
    int ival = (opReg - XMMBASE) << 4;

    assert((ival >= 0) && (ival <= 255));
    return (int8_t)ival;
}

//------------------------------------------------------------------------
// emitIns_R_R_A_R: emits the code for an instruction that takes a register operand, a GenTreeIndir address,
//                  another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    indir     -- The GenTreeIndir used for the memory address
//
// Remarks:
//    op2 is built from indir
//
void emitter::emitIns_R_R_A_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, GenTreeIndir* indir)
{
    assert(isAvxBlendv(ins));
    assert(UseVEXEncoding());

    int        ival = encodeXmmRegAsIval(op3Reg);
    ssize_t    offs = indir->Offset();
    instrDesc* id   = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(targetReg);
    id->idReg2(op1Reg);

    emitHandleMemOp(indir, id, IF_RWR_RRD_ARD_RRD, ins);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitIns_R_R_AR_R: emits the code for an instruction that takes a register operand, a base memory
//                   register, another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operands
//    op3Reg    -- The register of the third operand
//    base      -- The base register used for the memory address
//    offs      -- The offset added to the memory address from base
//
// Remarks:
//    op2 is built from base + offs
//
void emitter::emitIns_R_R_AR_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, regNumber base, int offs)
{
    assert(isAvxBlendv(ins));
    assert(UseVEXEncoding());

    int        ival = encodeXmmRegAsIval(op3Reg);
    instrDesc* id   = emitNewInstrAmdCns(attr, offs, ival);

    id->idIns(ins);
    id->idReg1(targetReg);
    id->idReg2(op1Reg);

    id->idInsFmt(IF_RWR_RRD_ARD_RRD);
    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitIns_R_R_C_R: emits the code for an instruction that takes a register operand, a field handle +
//                  offset,  another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    fldHnd    -- The CORINFO_FIELD_HANDLE used for the memory address
//    offs      -- The offset added to the memory address from fldHnd
//
// Remarks:
//    op2 is built from fldHnd + offs
//
void emitter::emitIns_R_R_C_R(instruction          ins,
                              emitAttr             attr,
                              regNumber            targetReg,
                              regNumber            op1Reg,
                              regNumber            op3Reg,
                              CORINFO_FIELD_HANDLE fldHnd,
                              int                  offs)
{
    assert(isAvxBlendv(ins));
    assert(UseVEXEncoding());

    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    int        ival = encodeXmmRegAsIval(op3Reg);
    instrDesc* id   = emitNewInstrCnsDsp(attr, ival, offs);

    id->idIns(ins);
    id->idReg1(targetReg);
    id->idReg2(op1Reg);

    id->idInsFmt(IF_RWR_RRD_MRD_RRD);
    id->idAddr()->iiaFieldHnd = fldHnd;

    UNATIVE_OFFSET sz = emitInsSizeCV(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

//------------------------------------------------------------------------
// emitIns_R_R_R_S: emits the code for a instruction that takes a register operand, a variable index +
//                  offset, another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    varx      -- The variable index used for the memory address
//    offs      -- The offset added to the memory address from varx
//
// Remarks:
//    op2 is built from varx + offs
//
void emitter::emitIns_R_R_S_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, int varx, int offs)
{
    assert(isAvxBlendv(ins));
    assert(UseVEXEncoding());

    int        ival = encodeXmmRegAsIval(op3Reg);
    instrDesc* id   = emitNewInstrCns(attr, ival);

    id->idIns(ins);
    id->idReg1(targetReg);
    id->idReg2(op1Reg);

    id->idInsFmt(IF_RWR_RRD_SRD_RRD);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeRM(ins), varx, offs, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber reg1, regNumber reg2, regNumber reg3)
{
    assert(isAvxBlendv(ins));
    assert(UseVEXEncoding());

    int        ival = encodeXmmRegAsIval(reg3);
    instrDesc* id   = emitNewInstrCns(attr, ival);

    id->idIns(ins);
    id->idInsFmt(IF_RWR_RRD_RRD_RRD);
    id->idReg1(targetReg);
    id->idReg2(reg1);
    id->idReg3(reg2);
    id->idReg4(reg3);

    UNATIVE_OFFSET sz = emitInsSizeRR(id, insCodeRM(ins), ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with a register + static member operands.
 */
void emitter::emitIns_R_C(instruction ins, emitAttr attr, regNumber reg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    emitAttr size = EA_SIZE(attr);

    assert(size <= EA_32BYTE);
    noway_assert(emitVerifyEncodable(ins, size, reg));

    UNATIVE_OFFSET sz;
    instrDesc*     id;

    // Are we MOV'ing the offset of the class variable into EAX?
    if (EA_IS_OFFSET(attr))
    {
        id = emitNewInstrDsp(EA_1BYTE, offs);
        id->idIns(ins);
        id->idInsFmt(IF_RWR_MRD_OFF);
        id->idReg1(reg);

        assert(ins == INS_mov && reg == REG_EAX);

        // Special case: "mov eax, [addr]" is smaller
        sz = 1 + TARGET_POINTER_SIZE;
    }
    else
    {
        insFormat fmt = emitInsModeFormat(ins, IF_RRD_MRD);

        id = emitNewInstrDsp(attr, offs);
        id->idIns(ins);
        id->idInsFmt(fmt);
        id->idReg1(reg);

#ifdef TARGET_X86
        // Special case: "mov eax, [addr]" is smaller.
        // This case is not enabled for amd64 as it always uses RIP relative addressing
        // and it results in smaller instruction size than encoding 64-bit addr in the
        // instruction.
        if (ins == INS_mov && reg == REG_EAX)
        {
            sz = 1 + TARGET_POINTER_SIZE;
            if (size == EA_2BYTE)
                sz += 1;
        }
        else
#endif // TARGET_X86
        {
            sz = emitInsSizeCV(id, insCodeRM(ins));
        }

        // Special case: mov reg, fs:[ddd]
        if (fldHnd == FLD_GLOBAL_FS)
        {
            sz += 1;
        }
    }

    id->idCodeSize(sz);

    id->idAddr()->iiaFieldHnd = fldHnd;

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + register operands.
 */

void emitter::emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs)
{
    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    emitAttr size = EA_SIZE(attr);

#if defined(TARGET_X86)
    // For x86 it is valid to storeind a double sized operand in an xmm reg to memory
    assert(size <= EA_8BYTE);
#else
    assert(size <= EA_PTRSIZE);
#endif

    noway_assert(emitVerifyEncodable(ins, size, reg));

    instrDesc* id  = emitNewInstrDsp(attr, offs);
    insFormat  fmt = emitInsModeFormat(ins, IF_MRD_RRD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);

    UNATIVE_OFFSET sz;

#ifdef TARGET_X86
    // Special case: "mov [addr], EAX" is smaller.
    // This case is not enable for amd64 as it always uses RIP relative addressing
    // and it will result in smaller instruction size than encoding 64-bit addr in
    // the instruction.
    if (ins == INS_mov && reg == REG_EAX)
    {
        sz = 1 + TARGET_POINTER_SIZE;

        if (size == EA_2BYTE)
            sz += 1;

        // REX prefix
        if (TakesRexWPrefix(ins, attr) || IsExtendedReg(reg, attr))
        {
            sz += emitGetRexPrefixSize(ins);
        }
    }
    else
#endif // TARGET_X86
    {
        sz = emitInsSizeCV(id, insCodeMR(ins));
    }

    // Special case: mov reg, fs:[ddd]
    if (fldHnd == FLD_GLOBAL_FS)
    {
        sz += 1;
    }

    id->idCodeSize(sz);

    id->idAddr()->iiaFieldHnd = fldHnd;

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + constant.
 */

void emitter::emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, int offs, int val)
{
    // Static always need relocs
    if (!jitStaticFldIsGlobAddr(fldHnd))
    {
        attr = EA_SET_FLG(attr, EA_DSP_RELOC_FLG);
    }

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_MRW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_MRD_CNS);
            break;
    }

    instrDesc* id = emitNewInstrCnsDsp(attr, val, offs);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idAddr()->iiaFieldHnd = fldHnd;

    code_t         code = insCodeMI(ins);
    UNATIVE_OFFSET sz   = emitInsSizeCV(id, code, val);

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_J_S(instruction ins, emitAttr attr, BasicBlock* dst, int varx, int offs)
{
    assert(ins == INS_mov);
    assert(dst->bbFlags & BBF_HAS_LABEL);

    instrDescLbl* id = emitNewInstrLbl();

    id->idIns(ins);
    id->idInsFmt(IF_SWR_LABEL);
    id->idAddr()->iiaBBlabel = dst;

    /* The label reference is always long */

    id->idjShort    = 0;
    id->idjKeepLong = 1;

    /* Record the current IG and offset within it */

    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this instruction to this IG's jump list */

    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

    UNATIVE_OFFSET sz = sizeof(INT32) + emitInsSizeSV(id, insCodeMI(ins), varx, offs);
    id->dstLclVar.initLclVarAddr(varx, offs);
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

#ifndef TARGET_AMD64
    // Storing the address of a basicBlock will need a reloc
    // as the instruction uses the absolute address,
    // not a relative address.
    //
    // On Amd64, Absolute code addresses should always go through a reloc to
    // to be encoded as RIP rel32 offset.
    if (emitComp->opts.compReloc)
#endif
    {
        id->idSetIsDspReloc();
    }

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */
void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(ins == INS_lea);
    assert(dst->bbFlags & BBF_HAS_LABEL);

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idReg1(reg);
    id->idInsFmt(IF_RWR_LABEL);
    id->idOpSize(EA_SIZE(attr)); // emitNewInstrJmp() sets the size (incorrectly) to EA_1BYTE
    id->idAddr()->iiaBBlabel = dst;

    /* The label reference is always long */

    id->idjShort    = 0;
    id->idjKeepLong = 1;

    /* Record the current IG and offset within it */

    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this instruction to this IG's jump list */

    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#ifdef DEBUG
    // Mark the catch return
    if (emitComp->compCurBB->bbJumpKind == BBJ_EHCATCHRET)
    {
        id->idDebugOnlyInfo()->idCatchRet = true;
    }
#endif // DEBUG

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    // Set the relocation flags - these give hint to zap to perform
    // relocation of the specified 32bit address.
    //
    // Note the relocation flags influence the size estimate.
    id->idSetRelocFlags(attr);

    UNATIVE_OFFSET sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  The following adds instructions referencing address modes.
 */

void emitter::emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_ARW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
            break;
    }

    /*
    Useful if you want to trap moves with 0 constant
    if (ins == INS_mov && val == 0 && EA_SIZE(attr) >= EA_4BYTE)
    {
        printf("MOV 0\n");
    }
    */

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmdCns(attr, disp, val);
    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = reg;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMI(ins), val);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_I_AI(instruction ins, emitAttr attr, int val, ssize_t disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_ARW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
            break;
    }

    /*
    Useful if you want to trap moves with 0 constant
    if (ins == INS_mov && val == 0 && EA_SIZE(attr) >= EA_4BYTE)
    {
        printf("MOV 0\n");
    }
    */

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmdCns(attr, disp, val);
    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMI(ins), val);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber reg, regNumber base, int disp)
{
    emitIns_R_ARX(ins, attr, reg, base, REG_NA, 1, disp);
}

void emitter::emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE) && (ireg != REG_NA));
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), ireg));

    UNATIVE_OFFSET sz;
    instrDesc*     id  = emitNewInstrAmd(attr, disp);
    insFormat      fmt = emitInsModeFormat(ins, IF_RRD_ARD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(ireg);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_AR_R(instruction ins, emitAttr attr, regNumber reg, regNumber base, cnsval_ssize_t disp)
{
    emitIns_ARX_R(ins, attr, reg, base, REG_NA, 1, disp);
}

//------------------------------------------------------------------------
// emitIns_S_R_I: emits the code for an instruction that takes a stack operand,
//                a register operand, and an immediate.
//
// Arguments:
//    ins       - The instruction being emitted
//    attr      - The emit attribute
//    varNum    - The varNum of the stack operand
//    offs      - The offset for the stack operand
//    reg       - The register operand
//    ival      - The immediate value
//
void emitter::emitIns_S_R_I(instruction ins, emitAttr attr, int varNum, int offs, regNumber reg, int ival)
{
    // This is only used for INS_vextracti128 and INS_vextractf128, and for these 'ival' must be 0 or 1.
    assert(ins == INS_vextracti128 || ins == INS_vextractf128);
    assert((ival == 0) || (ival == 1));
    instrDesc* id = emitNewInstrAmdCns(attr, 0, ival);

    id->idIns(ins);
    id->idInsFmt(IF_SWR_RRD_CNS);
    id->idReg1(reg);
    id->idAddr()->iiaLclVar.initLclVarAddr(varNum, offs);
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeMR(ins), varNum, offs, ival);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_A_R_I(instruction ins, emitAttr attr, GenTreeIndir* indir, regNumber reg, int imm)
{
    assert((ins == INS_vextracti128) || (ins == INS_vextractf128));
    assert(attr == EA_32BYTE);
    assert(reg != REG_NA);

    instrDesc* id = emitNewInstrAmdCns(attr, indir->Offset(), imm);
    id->idIns(ins);
    id->idReg1(reg);
    emitHandleMemOp(indir, id, IF_AWR_RRD_CNS, ins);
    UNATIVE_OFFSET size = emitInsSizeAM(id, insCodeMR(ins), imm);
    id->idCodeSize(size);
    dispIns(id);
    emitCurIGsize += size;
}

void emitter::emitIns_AI_R(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmd(attr, disp);
    insFormat      fmt;

    if (ireg == REG_NA)
    {
        fmt = emitInsModeFormat(ins, IF_ARD);
    }
    else
    {
        fmt = emitInsModeFormat(ins, IF_ARD_RRD);

        assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));
        noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), ireg));

        id->idReg1(ireg);
    }

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = REG_NA;

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

void emitter::emitIns_I_ARR(instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, int disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_ARW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
            break;
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmdCns(attr, disp, val);
    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = reg;
    id->idAddr()->iiaAddrMode.amIndxReg = rg2;
    id->idAddr()->iiaAddrMode.amScale   = emitter::OPSZ1;

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMI(ins), val);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_ARR(instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, int disp)
{
    emitIns_R_ARX(ins, attr, reg, base, index, 1, disp);
}

void emitter::emitIns_ARR_R(instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, int disp)
{
    emitIns_ARX_R(ins, attr, reg, base, index, 1, disp);
}

void emitter::emitIns_I_ARX(
    instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, unsigned mul, int disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_ARW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
            break;
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmdCns(attr, disp, val);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = reg;
    id->idAddr()->iiaAddrMode.amIndxReg = rg2;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(mul);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMI(ins), val);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, unsigned scale, int disp)
{
    assert(!CodeGen::instIsFP(ins) && (EA_SIZE(attr) <= EA_32BYTE) && (reg != REG_NA));
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg));

    if ((ins == INS_lea) && (reg == base) && (index == REG_NA) && (disp == 0))
    {
        // Maybe the emitter is not the common place for this optimization, but it's a better choke point
        // for all the emitIns(ins, tree), we would have to be analyzing at each call site
        //
        return;
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id  = emitNewInstrAmd(attr, disp);
    insFormat      fmt = emitInsModeFormat(ins, IF_RRD_ARD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);

    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = index;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(scale);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_ARX_R(
    instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, unsigned scale, cnsval_ssize_t disp)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmd(attr, disp);
    insFormat      fmt;

    if (reg == REG_NA)
    {
        fmt = emitInsModeFormat(ins, IF_ARD);
    }
    else
    {
        fmt = emitInsModeFormat(ins, IF_ARD_RRD);

        noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), reg));
        assert(!CodeGen::instIsFP(ins) && (EA_SIZE(attr) <= EA_32BYTE));

        id->idReg1(reg);
    }

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = base;
    id->idAddr()->iiaAddrMode.amIndxReg = index;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(scale);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

void emitter::emitIns_I_AX(instruction ins, emitAttr attr, int val, regNumber reg, unsigned mul, int disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_ARW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_ARD_CNS);
            break;
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmdCns(attr, disp, val);
    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = reg;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(mul);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMI(ins), val);
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_AX(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp)
{
    assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE) && (ireg != REG_NA));
    noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), ireg));

    UNATIVE_OFFSET sz;
    instrDesc*     id  = emitNewInstrAmd(attr, disp);
    insFormat      fmt = emitInsModeFormat(ins, IF_RRD_ARD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(ireg);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = reg;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(mul);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeRM(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_AX_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstrAmd(attr, disp);
    insFormat      fmt;

    if (ireg == REG_NA)
    {
        fmt = emitInsModeFormat(ins, IF_ARD);
    }
    else
    {
        fmt = emitInsModeFormat(ins, IF_ARD_RRD);
        noway_assert(emitVerifyEncodable(ins, EA_SIZE(attr), ireg));
        assert((CodeGen::instIsFP(ins) == false) && (EA_SIZE(attr) <= EA_8BYTE));

        id->idReg1(ireg);
    }

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
    id->idAddr()->iiaAddrMode.amIndxReg = reg;
    id->idAddr()->iiaAddrMode.amScale   = emitEncodeScale(mul);

    assert(emitGetInsAmdAny(id) == disp); // make sure "disp" is stored properly

    sz = emitInsSizeAM(id, insCodeMR(ins));
    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_I: emits the code for an instruction that takes a register operand, an immediate operand
//                     and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    ival      -- The immediate value
//
// Notes:
//    This will handle the required register copy if 'op1Reg' and 'targetReg' are not the same, and
//    the 3-operand format is not available.
//    This is not really SIMD-specific, but is currently only used in that context, as that's
//    where we frequently need to handle the case of generating 3-operand or 2-operand forms
//    depending on what target ISA is supported.
//
void emitter::emitIns_SIMD_R_R_I(instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int ival)
{
    if (UseVEXEncoding() || IsDstSrcImmAvxInstruction(ins))
    {
        emitIns_R_R_I(ins, attr, targetReg, op1Reg, ival);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_I(ins, attr, targetReg, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_A: emits the code for a SIMD instruction that takes a register operand, a GenTreeIndir address,
//                     and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    indir     -- The GenTreeIndir used for the memory address
//
void emitter::emitIns_SIMD_R_R_A(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTreeIndir* indir)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_A(ins, attr, targetReg, op1Reg, indir);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_A(ins, attr, targetReg, indir);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_AR: emits the code for a SIMD instruction that takes a register operand, a base memory register,
//                      and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    base      -- The base register used for the memory address
//    offset    -- The memory offset
//
void emitter::emitIns_SIMD_R_R_AR(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber base, int offset)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_AR(ins, attr, targetReg, op1Reg, base, offset);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_AR(ins, attr, targetReg, base, offset);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_C: emits the code for a SIMD instruction that takes a register operand, a field handle + offset,
//                     and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    fldHnd    -- The CORINFO_FIELD_HANDLE used for the memory address
//    offs      -- The offset added to the memory address from fldHnd
//
void emitter::emitIns_SIMD_R_R_C(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_C(ins, attr, targetReg, op1Reg, fldHnd, offs);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_C(ins, attr, targetReg, fldHnd, offs);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R: emits the code for a SIMD instruction that takes two register operands, and that returns a
//                     value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//
void emitter::emitIns_SIMD_R_R_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_R(ins, attr, targetReg, op1Reg, op2Reg);
    }
    else
    {
        // Ensure we aren't overwriting op2
        assert((op2Reg != targetReg) || (op1Reg == targetReg));

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);

        if (IsMovInstruction(ins))
        {
            emitIns_Mov(ins, attr, targetReg, op2Reg, /* canSkip */ false);
        }
        else
        {
            emitIns_R_R(ins, attr, targetReg, op2Reg);
        }
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_S: emits the code for a SIMD instruction that takes a register operand, a variable index + offset,
//                     and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    varx      -- The variable index used for the memory address
//    offs      -- The offset added to the memory address from varx
//
void emitter::emitIns_SIMD_R_R_S(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int varx, int offs)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_S(ins, attr, targetReg, op1Reg, varx, offs);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_S(ins, attr, targetReg, varx, offs);
    }
}

#ifdef FEATURE_HW_INTRINSICS
//------------------------------------------------------------------------
// emitIns_SIMD_R_R_A_I: emits the code for a SIMD instruction that takes a register operand, a GenTreeIndir address,
//                       an immediate operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    indir     -- The GenTreeIndir used for the memory address
//    ival      -- The immediate value
//
void emitter::emitIns_SIMD_R_R_A_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTreeIndir* indir, int ival)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_A_I(ins, attr, targetReg, op1Reg, indir, ival, IF_RWR_RRD_ARD_CNS);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_A_I(ins, attr, targetReg, indir, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_AR_I: emits the code for a SIMD instruction that takes a register operand, a base memory register,
//                        an immediate operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    base      -- The base register used for the memory address
//    ival      -- The immediate value
//
void emitter::emitIns_SIMD_R_R_AR_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber base, int ival)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_AR_I(ins, attr, targetReg, op1Reg, base, 0, ival);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_AR_I(ins, attr, targetReg, base, 0, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_C_I: emits the code for a SIMD instruction that takes a register operand, a field handle + offset,
//                       an immediate operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    fldHnd    -- The CORINFO_FIELD_HANDLE used for the memory address
//    offs      -- The offset added to the memory address from fldHnd
//    ival      -- The immediate value
//
void emitter::emitIns_SIMD_R_R_C_I(instruction          ins,
                                   emitAttr             attr,
                                   regNumber            targetReg,
                                   regNumber            op1Reg,
                                   CORINFO_FIELD_HANDLE fldHnd,
                                   int                  offs,
                                   int                  ival)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_C_I(ins, attr, targetReg, op1Reg, fldHnd, offs, ival);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_C_I(ins, attr, targetReg, fldHnd, offs, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_I: emits the code for a SIMD instruction that takes two register operands, an immediate operand,
//                       and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//    ival      -- The immediate value
//
void emitter::emitIns_SIMD_R_R_R_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, int ival)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_R_I(ins, attr, targetReg, op1Reg, op2Reg, ival);
    }
    else
    {
        // Ensure we aren't overwriting op2
        assert((op2Reg != targetReg) || (op1Reg == targetReg));

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_R_I(ins, attr, targetReg, op2Reg, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_S_I: emits the code for a SIMD instruction that takes a register operand, a variable index + offset,
//                       an imediate operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    varx      -- The variable index used for the memory address
//    offs      -- The offset added to the memory address from varx
//    ival      -- The immediate value
//
void emitter::emitIns_SIMD_R_R_S_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int varx, int offs, int ival)
{
    if (UseVEXEncoding())
    {
        emitIns_R_R_S_I(ins, attr, targetReg, op1Reg, varx, offs, ival);
    }
    else
    {
        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_S_I(ins, attr, targetReg, varx, offs, ival);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_A: emits the code for a SIMD instruction that takes two register operands, a GenTreeIndir address,
//                       and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//    indir     -- The GenTreeIndir used for the memory address
//
void emitter::emitIns_SIMD_R_R_R_A(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTreeIndir* indir)
{
    assert(IsFMAInstruction(ins) || IsAVXVNNIInstruction(ins));
    assert(UseVEXEncoding());

    // Ensure we aren't overwriting op2
    assert((op2Reg != targetReg) || (op1Reg == targetReg));

    emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
    emitIns_R_R_A(ins, attr, targetReg, op2Reg, indir);
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_AR: emits the code for a SIMD instruction that takes two register operands, a base memory
//                        register, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operands
//    op2Reg    -- The register of the second operand
//    base      -- The base register used for the memory address
//
void emitter::emitIns_SIMD_R_R_R_AR(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, regNumber base)
{
    assert(IsFMAInstruction(ins));
    assert(UseVEXEncoding());

    // Ensure we aren't overwriting op2
    assert((op2Reg != targetReg) || (op1Reg == targetReg));

    emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
    emitIns_R_R_AR(ins, attr, targetReg, op2Reg, base, 0);
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_C: emits the code for a SIMD instruction that takes two register operands, a field handle +
//                       offset, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//    fldHnd    -- The CORINFO_FIELD_HANDLE used for the memory address
//    offs      -- The offset added to the memory address from fldHnd
//
void emitter::emitIns_SIMD_R_R_R_C(instruction          ins,
                                   emitAttr             attr,
                                   regNumber            targetReg,
                                   regNumber            op1Reg,
                                   regNumber            op2Reg,
                                   CORINFO_FIELD_HANDLE fldHnd,
                                   int                  offs)
{
    assert(IsFMAInstruction(ins));
    assert(UseVEXEncoding());

    // Ensure we aren't overwriting op2
    assert((op2Reg != targetReg) || (op1Reg == targetReg));

    emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
    emitIns_R_R_C(ins, attr, targetReg, op2Reg, fldHnd, offs);
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_R: emits the code for a SIMD instruction that takes three register operands, and that returns a
//                     value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//    op3Reg    -- The register of the second operand
//
void emitter::emitIns_SIMD_R_R_R_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, regNumber op3Reg)
{
    if (IsFMAInstruction(ins) || IsAVXVNNIInstruction(ins))
    {
        assert(UseVEXEncoding());

        // Ensure we aren't overwriting op2 or op3
        assert((op2Reg != targetReg) || (op1Reg == targetReg));
        assert((op3Reg != targetReg) || (op1Reg == targetReg));

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_R_R(ins, attr, targetReg, op2Reg, op3Reg);
    }
    else if (UseVEXEncoding())
    {
        assert(isAvxBlendv(ins) || isSse41Blendv(ins));

        // convert SSE encoding of SSE4.1 instructions to VEX encoding
        switch (ins)
        {
            case INS_blendvps:
                ins = INS_vblendvps;
                break;
            case INS_blendvpd:
                ins = INS_vblendvpd;
                break;
            case INS_pblendvb:
                ins = INS_vpblendvb;
                break;
            default:
                break;
        }
        emitIns_R_R_R_R(ins, attr, targetReg, op1Reg, op2Reg, op3Reg);
    }
    else
    {
        assert(isSse41Blendv(ins));

        // Ensure we aren't overwriting op1 or op2
        assert((op1Reg != REG_XMM0) || (op3Reg == REG_XMM0));
        assert((op2Reg != REG_XMM0) || (op3Reg == REG_XMM0));

        // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
        emitIns_Mov(INS_movaps, attr, REG_XMM0, op3Reg, /* canSkip */ true);

        // Ensure we aren't overwriting op2 or oop3 (which should be REG_XMM0)
        assert((op2Reg != targetReg) || (op1Reg == targetReg));
        assert(targetReg != REG_XMM0);

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_R(ins, attr, targetReg, op2Reg);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_R_S: emits the code for a SIMD instruction that takes two register operands, a variable index +
//                       offset, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op2Reg    -- The register of the second operand
//    varx      -- The variable index used for the memory address
//    offs      -- The offset added to the memory address from varx
//
void emitter::emitIns_SIMD_R_R_R_S(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, int varx, int offs)
{
    assert(IsFMAInstruction(ins) || IsAVXVNNIInstruction(ins));
    assert(UseVEXEncoding());

    // Ensure we aren't overwriting op2
    assert((op2Reg != targetReg) || (op1Reg == targetReg));

    emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
    emitIns_R_R_S(ins, attr, targetReg, op2Reg, varx, offs);
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_A_R: emits the code for a SIMD instruction that takes a register operand, a GenTreeIndir address,
//                       another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    indir     -- The GenTreeIndir used for the memory address
//
void emitter::emitIns_SIMD_R_R_A_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, GenTreeIndir* indir)
{
    if (UseVEXEncoding())
    {
        assert(isAvxBlendv(ins) || isSse41Blendv(ins));

        // convert SSE encoding of SSE4.1 instructions to VEX encoding
        switch (ins)
        {
            case INS_blendvps:
            {
                ins = INS_vblendvps;
                break;
            }

            case INS_blendvpd:
            {
                ins = INS_vblendvpd;
                break;
            }

            case INS_pblendvb:
            {
                ins = INS_vpblendvb;
                break;
            }

            default:
            {
                break;
            }
        }

        emitIns_R_R_A_R(ins, attr, targetReg, op1Reg, op3Reg, indir);
    }
    else
    {
        assert(isSse41Blendv(ins));

        // Ensure we aren't overwriting op1
        assert(op1Reg != REG_XMM0);

        // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
        emitIns_Mov(INS_movaps, attr, REG_XMM0, op3Reg, /* canSkip */ true);

        // Ensure we aren't overwriting op3 (which should be REG_XMM0)
        assert(targetReg != REG_XMM0);

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_A(ins, attr, targetReg, indir);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_AR_R: emits the code for a SIMD instruction that takes a register operand, a base memory
//                        register, another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operands
//    op3Reg    -- The register of the third operand
//    base      -- The base register used for the memory address
//
void emitter::emitIns_SIMD_R_R_AR_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, regNumber base)
{
    if (UseVEXEncoding())
    {
        assert(isAvxBlendv(ins) || isSse41Blendv(ins));

        // convert SSE encoding of SSE4.1 instructions to VEX encoding
        switch (ins)
        {
            case INS_blendvps:
            {
                ins = INS_vblendvps;
                break;
            }

            case INS_blendvpd:
            {
                ins = INS_vblendvpd;
                break;
            }

            case INS_pblendvb:
            {
                ins = INS_vpblendvb;
                break;
            }

            default:
            {
                break;
            }
        }

        emitIns_R_R_AR_R(ins, attr, targetReg, op1Reg, op3Reg, base, 0);
    }
    else
    {
        assert(isSse41Blendv(ins));

        // Ensure we aren't overwriting op1
        assert(op1Reg != REG_XMM0);

        // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
        emitIns_Mov(INS_movaps, attr, REG_XMM0, op3Reg, /* canSkip */ true);

        // Ensure we aren't overwriting op3 (which should be REG_XMM0)
        assert(targetReg != REG_XMM0);

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_AR(ins, attr, targetReg, base, 0);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_C_R: emits the code for a SIMD instruction that takes a register operand, a field handle +
//                       offset,  another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    fldHnd    -- The CORINFO_FIELD_HANDLE used for the memory address
//    offs      -- The offset added to the memory address from fldHnd
//
void emitter::emitIns_SIMD_R_R_C_R(instruction          ins,
                                   emitAttr             attr,
                                   regNumber            targetReg,
                                   regNumber            op1Reg,
                                   regNumber            op3Reg,
                                   CORINFO_FIELD_HANDLE fldHnd,
                                   int                  offs)
{
    if (UseVEXEncoding())
    {
        assert(isAvxBlendv(ins) || isSse41Blendv(ins));

        // convert SSE encoding of SSE4.1 instructions to VEX encoding
        switch (ins)
        {
            case INS_blendvps:
            {
                ins = INS_vblendvps;
                break;
            }

            case INS_blendvpd:
            {
                ins = INS_vblendvpd;
                break;
            }

            case INS_pblendvb:
            {
                ins = INS_vpblendvb;
                break;
            }

            default:
            {
                break;
            }
        }

        emitIns_R_R_C_R(ins, attr, targetReg, op1Reg, op3Reg, fldHnd, offs);
    }
    else
    {
        assert(isSse41Blendv(ins));

        // Ensure we aren't overwriting op1
        assert(op1Reg != REG_XMM0);

        // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
        emitIns_Mov(INS_movaps, attr, REG_XMM0, op3Reg, /* canSkip */ true);

        // Ensure we aren't overwriting op3 (which should be REG_XMM0)
        assert(targetReg != REG_XMM0);

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_C(ins, attr, targetReg, fldHnd, offs);
    }
}

//------------------------------------------------------------------------
// emitIns_SIMD_R_R_S_R: emits the code for a SIMD instruction that takes a register operand, a variable index +
//                       offset, another register operand, and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    targetReg -- The target register
//    op1Reg    -- The register of the first operand
//    op3Reg    -- The register of the third operand
//    varx      -- The variable index used for the memory address
//    offs      -- The offset added to the memory address from varx
//
void emitter::emitIns_SIMD_R_R_S_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, int varx, int offs)
{
    if (UseVEXEncoding())
    {
        assert(isAvxBlendv(ins) || isSse41Blendv(ins));

        // convert SSE encoding of SSE4.1 instructions to VEX encoding
        switch (ins)
        {
            case INS_blendvps:
            {
                ins = INS_vblendvps;
                break;
            }

            case INS_blendvpd:
            {
                ins = INS_vblendvpd;
                break;
            }

            case INS_pblendvb:
            {
                ins = INS_vpblendvb;
                break;
            }

            default:
            {
                break;
            }
        }

        emitIns_R_R_S_R(ins, attr, targetReg, op1Reg, op3Reg, varx, offs);
    }
    else
    {
        assert(isSse41Blendv(ins));

        // Ensure we aren't overwriting op1
        assert(op1Reg != REG_XMM0);

        // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
        emitIns_Mov(INS_movaps, attr, REG_XMM0, op3Reg, /* canSkip */ true);

        // Ensure we aren't overwriting op3 (which should be REG_XMM0)
        assert(targetReg != REG_XMM0);

        emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
        emitIns_R_S(ins, attr, targetReg, varx, offs);
    }
}
#endif // FEATURE_HW_INTRINSICS

/*****************************************************************************
 *
 *  The following add instructions referencing stack-based local variables.
 */

void emitter::emitIns_S(instruction ins, emitAttr attr, int varx, int offs)
{
    UNATIVE_OFFSET sz;
    instrDesc*     id  = emitNewInstr(attr);
    insFormat      fmt = emitInsModeFormat(ins, IF_SRD);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

    sz = emitInsSizeSV(id, insCodeMR(ins), varx, offs);
    id->idCodeSize(sz);

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif
    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

//----------------------------------------------------------------------------------------
// IsRedundantStackMov:
//    Check if the current `mov` instruction is redundant and can be omitted when dealing with Load/Store from stack.
//    A `mov` is redundant in following 2 cases:
//
//    1. Move that is identical to last instruction emitted.
//
//         vmovapd  xmmword ptr [V01 rbp-20H], xmm0  # <-- last instruction
//         vmovapd  xmmword ptr [V01 rbp-20H], xmm0  # <-- current instruction can be omitted.
//
//    2. Opposite Move as that of last instruction emitted.
//
//         vmovupd  ymmword ptr[V01 rbp-50H], ymm0  # <-- last instruction
//         vmovupd  ymm0, ymmword ptr[V01 rbp-50H]  # <-- current instruction can be omitted.
//
// Arguments:
//                 ins  - The current instruction
//                 fmt  - The current format
//                 size - Operand size of current instruction
//                 ireg - The current source/destination register
//                 varx - The variable index used for the memory address
//                 offs - The offset added to the memory address from varx
//
// Return Value:
//    true if the move instruction is redundant; otherwise, false.

bool emitter::IsRedundantStackMov(instruction ins, insFormat fmt, emitAttr size, regNumber ireg, int varx, int offs)
{
    assert(IsMovInstruction(ins));
    assert((fmt == IF_SWR_RRD) || (fmt == IF_RWR_SRD));
    if (!emitComp->opts.OptimizationEnabled())
    {
        // The remaining move elisions should only happen if optimizations are enabled
        return false;
    }

    // Skip optimization if current instruction creates a GC live value.
    if (EA_IS_GCREF_OR_BYREF(size))
    {
        return false;
    }

    bool hasSideEffect = HasSideEffect(ins, size);

    bool isFirstInstrInBlock = (emitCurIGinsCnt == 0) && ((emitCurIG->igFlags & IGF_EXTEND) == 0);
    // TODO-XArch-CQ: Certain instructions, such as movaps vs movups, are equivalent in
    // functionality even if their actual identifier differs and we should optimize these

    if (isFirstInstrInBlock ||             // Don't optimize if instruction is the first instruction in IG.
        (emitLastIns == nullptr) ||        // or if a last instruction doesn't exist
        (emitLastIns->idIns() != ins) ||   // or if the instruction is different from the last instruction
        (emitLastIns->idOpSize() != size)) // or if the operand size is different from the last instruction
    {
        return false;
    }

    // Don't optimize if the last instruction is also not a Load/Store.
    if (!((emitLastIns->idInsFmt() == IF_SWR_RRD) || (emitLastIns->idInsFmt() == IF_RWR_SRD)))
    {
        return false;
    }

    regNumber lastReg1 = emitLastIns->idReg1();
    int       varNum   = emitLastIns->idAddr()->iiaLclVar.lvaVarNum();
    int       lastOffs = emitLastIns->idAddr()->iiaLclVar.lvaOffset();

    // Check if the last instruction and current instructions use the same register and local memory.
    if (varNum == varx && lastReg1 == ireg && lastOffs == offs)
    {
        // Check if we did a switched mov in the last instruction  and don't have a side effect
        if ((((emitLastIns->idInsFmt() == IF_RWR_SRD) && (fmt == IF_SWR_RRD)) ||
             ((emitLastIns->idInsFmt() == IF_SWR_RRD) && (fmt == IF_RWR_SRD))) &&
            !hasSideEffect) // or if the format is different from the last instruction
        {
            JITDUMP("\n -- suppressing mov because last instruction already moved from dst to src and the mov has "
                    "no side-effects.\n");
            return true;
        }
        // Check if we did same move in last instruction, side effects don't matter since they already happened
        if (emitLastIns->idInsFmt() == fmt)
        {
            JITDUMP("\n -- suppressing mov because last instruction already moved from src to dst.\n");
            return true;
        }
    }
    return false;
}

void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs)
{
    insFormat fmt = emitInsModeFormat(ins, IF_SRD_RRD);
    if (IsMovInstruction(ins) && IsRedundantStackMov(ins, fmt, attr, ireg, varx, offs))
    {
        return;
    }

    UNATIVE_OFFSET sz;
    instrDesc*     id = emitNewInstr(attr);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(ireg);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

    sz = emitInsSizeSV(id, insCodeMR(ins), varx, offs);

#ifdef TARGET_X86
    if (attr == EA_1BYTE)
    {
        assert(isByteReg(ireg));
    }
#endif

    id->idCodeSize(sz);
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif
    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs)
{
    emitAttr size = EA_SIZE(attr);
    noway_assert(emitVerifyEncodable(ins, size, ireg));
    insFormat fmt = emitInsModeFormat(ins, IF_RRD_SRD);

    if (IsMovInstruction(ins) && IsRedundantStackMov(ins, fmt, attr, ireg, varx, offs))
    {
        return;
    }

    instrDesc*     id = emitNewInstr(attr);
    UNATIVE_OFFSET sz;
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(ireg);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

    sz = emitInsSizeSV(id, insCodeRM(ins), varx, offs);
    id->idCodeSize(sz);
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif
    dispIns(id);
    emitCurIGsize += sz;
}

void emitter::emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val)
{
#ifdef TARGET_AMD64
    // mov reg, imm64 is the only opcode which takes a full 8 byte immediate
    // all other opcodes take a sign-extended 4-byte immediate
    noway_assert(EA_SIZE(attr) < EA_8BYTE || !EA_IS_CNS_RELOC(attr));
#endif

    insFormat fmt;

    switch (ins)
    {
        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            assert(val != 1);
            fmt = IF_SRW_SHF;
            val &= 0x7F;
            break;

        default:
            fmt = emitInsModeFormat(ins, IF_SRD_CNS);
            break;
    }

    instrDesc* id = emitNewInstrCns(attr, val);
    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);

    UNATIVE_OFFSET sz = emitInsSizeSV(id, insCodeMI(ins), varx, offs, val);
    id->idCodeSize(sz);
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif
    dispIns(id);
    emitCurIGsize += sz;
}

/*****************************************************************************
 *
 *  Record that a jump instruction uses the short encoding
 *
 */
void emitter::emitSetShortJump(instrDescJmp* id)
{
    if (id->idjKeepLong)
    {
        return;
    }

    id->idjShort = true;
}

/*****************************************************************************
 *
 *  Add a jmp instruction.
 *  When dst is NULL, instrCount specifies number of instructions
 *       to jump: positive is forward, negative is backward.
 */

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount /* = 0 */)
{
    UNATIVE_OFFSET sz;
    instrDescJmp*  id = emitNewInstrJmp();

    if (dst != nullptr)
    {
        assert(dst->bbFlags & BBF_HAS_LABEL);
        assert(instrCount == 0);
    }
    else
    {
        /* Only allow non-label jmps in prolog */
        assert(emitPrologIG);
        assert(emitPrologIG == emitCurIG);
        assert(instrCount != 0);
    }

    id->idIns(ins);
    id->idInsFmt(IF_LABEL);

#ifdef DEBUG
    // Mark the finally call
    if (ins == INS_call && emitComp->compCurBB->bbJumpKind == BBJ_CALLFINALLY)
    {
        id->idDebugOnlyInfo()->idFinallyCall = true;
    }
#endif // DEBUG

    id->idjShort = 0;
    if (dst != nullptr)
    {
        /* Assume the jump will be long */
        id->idAddr()->iiaBBlabel = dst;
        id->idjKeepLong          = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);
    }
    else
    {
        id->idAddr()->iiaSetInstrCount(instrCount);
        id->idjKeepLong = false;
        /* This jump must be short */
        emitSetShortJump(id);
        id->idSetIsBound();
    }

    /* Record the jump's IG and offset within it */

    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */

    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    /* Figure out the max. size of the jump/call instruction */

    if (ins == INS_call)
    {
        sz = CALL_INST_SIZE;
    }
    else if (ins == INS_push || ins == INS_push_hide)
    {
        // Pushing the address of a basicBlock will need a reloc
        // as the instruction uses the absolute address,
        // not a relative address
        if (emitComp->opts.compReloc)
        {
            id->idSetIsDspReloc();
        }
        sz = PUSH_INST_SIZE;
    }
    else
    {
        insGroup* tgt = nullptr;

        if (dst != nullptr)
        {
            /* This is a jump - assume the worst */
            sz = (ins == INS_jmp) ? JMP_SIZE_LARGE : JCC_SIZE_LARGE;
            /* Can we guess at the jump distance? */
            tgt = (insGroup*)emitCodeGetCookie(dst);
        }
        else
        {
            sz = JMP_SIZE_SMALL;
        }

        if (tgt)
        {
            int            extra;
            UNATIVE_OFFSET srcOffs;
            int            jmpDist;

            assert(JMP_SIZE_SMALL == JCC_SIZE_SMALL);

            /* This is a backward jump - figure out the distance */

            srcOffs = emitCurCodeOffset + emitCurIGsize + JMP_SIZE_SMALL;

            /* Compute the distance estimate */

            jmpDist = srcOffs - tgt->igOffs;
            assert((int)jmpDist > 0);

            /* How much beyond the max. short distance does the jump go? */

            extra = jmpDist + JMP_DIST_SMALL_MAX_NEG;

#if DEBUG_EMIT
            if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[0] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
                }
                printf("[0] Jump source is at %08X\n", srcOffs);
                printf("[0] Label block is at %08X\n", tgt->igOffs);
                printf("[0] Jump  distance  - %04X\n", jmpDist);
                if (extra > 0)
                {
                    printf("[0] Distance excess = %d  \n", extra);
                }
            }
#endif

            if (extra <= 0 && !id->idjKeepLong)
            {
                /* Wonderful - this jump surely will be short */

                emitSetShortJump(id);
                sz = JMP_SIZE_SMALL;
            }
        }
#if DEBUG_EMIT
        else
        {
            if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[0] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
                }
                printf("[0] Jump source is at %04X/%08X\n", emitCurIGsize,
                       emitCurCodeOffset + emitCurIGsize + JMP_SIZE_SMALL);
                printf("[0] Label block is unknown\n");
            }
        }
#endif
    }

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

    emitAdjustStackDepthPushPop(ins);
}

#if !FEATURE_FIXED_OUT_ARGS

//------------------------------------------------------------------------
// emitAdjustStackDepthPushPop: Adjust the current and maximum stack depth.
//
// Arguments:
//    ins - the instruction. Only INS_push and INS_pop adjust the stack depth.
//
// Notes:
//    1. Alters emitCurStackLvl and possibly emitMaxStackDepth.
//    2. emitCntStackDepth must be set (0 in prolog/epilog, one DWORD elsewhere)
//
void emitter::emitAdjustStackDepthPushPop(instruction ins)
{
    if (ins == INS_push)
    {
        emitCurStackLvl += emitCntStackDepth;

        if (emitMaxStackDepth < emitCurStackLvl)
        {
            JITDUMP("Upping emitMaxStackDepth from %d to %d\n", emitMaxStackDepth, emitCurStackLvl);
            emitMaxStackDepth = emitCurStackLvl;
        }
    }
    else if (ins == INS_pop)
    {
        emitCurStackLvl -= emitCntStackDepth;
        assert((int)emitCurStackLvl >= 0);
    }
}

//------------------------------------------------------------------------
// emitAdjustStackDepth: Adjust the current and maximum stack depth.
//
// Arguments:
//    ins - the instruction. Only INS_add and INS_sub adjust the stack depth.
//          It is assumed that the add/sub is on the stack pointer.
//    val - the number of bytes to add to or subtract from the stack pointer.
//
// Notes:
//    1. Alters emitCurStackLvl and possibly emitMaxStackDepth.
//    2. emitCntStackDepth must be set (0 in prolog/epilog, one DWORD elsewhere)
//
void emitter::emitAdjustStackDepth(instruction ins, ssize_t val)
{
    // If we're in the prolog or epilog, or otherwise not tracking the stack depth, just return.
    if (emitCntStackDepth == 0)
        return;

    if (ins == INS_sub)
    {
        S_UINT32 newStackLvl(emitCurStackLvl);
        newStackLvl += S_UINT32(val);
        noway_assert(!newStackLvl.IsOverflow());

        emitCurStackLvl = newStackLvl.Value();

        if (emitMaxStackDepth < emitCurStackLvl)
        {
            JITDUMP("Upping emitMaxStackDepth from %d to %d\n", emitMaxStackDepth, emitCurStackLvl);
            emitMaxStackDepth = emitCurStackLvl;
        }
    }
    else if (ins == INS_add)
    {
        S_UINT32 newStackLvl = S_UINT32(emitCurStackLvl) - S_UINT32(val);
        noway_assert(!newStackLvl.IsOverflow());

        emitCurStackLvl = newStackLvl.Value();
    }
}

#endif // EMIT_TRACK_STACK_DEPTH

/*****************************************************************************
 *
 *  Add a call instruction (direct or indirect).
 *      argSize<0 means that the caller will pop the arguments
 *
 * The other arguments are interpreted depending on callType as shown:
 * Unless otherwise specified, ireg,xreg,xmul,disp should have default values.
 *
 * EC_FUNC_TOKEN       : addr is the method address
 * EC_FUNC_TOKEN_INDIR : addr is the indirect method address
 * EC_FUNC_ADDR        : addr is the absolute address of the function
 * EC_FUNC_VIRTUAL     : "call [ireg+disp]"
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 * EC_INDIR_SR         : "call lcl<disp>" (eg. call [ebp-8]).
 * EC_INDIR_C          : "call clsVar<disp>" (eg. call [clsVarAddr])
 * EC_INDIR_ARD        : "call [ireg+xreg*xmul+disp]"
 *
 */

// clang-format off
void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*                 addr,
                           ssize_t               argSize,
                           emitAttr              retSize
                           MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                           VARSET_VALARG_TP      ptrVars,
                           regMaskTP             gcrefRegs,
                           regMaskTP             byrefRegs,
                           const DebugInfo&      di,
                           regNumber             ireg,
                           regNumber             xreg,
                           unsigned              xmul,
                           ssize_t               disp,
                           bool                  isJump)
// clang-format on
{
    /* Sanity check the arguments depending on callType */

    assert(callType < EC_COUNT);
    assert((callType != EC_FUNC_TOKEN && callType != EC_FUNC_TOKEN_INDIR) ||
           (addr != nullptr && ireg == REG_NA && xreg == REG_NA && xmul == 0 && disp == 0));
    assert(callType != EC_INDIR_R || (addr == nullptr && ireg < REG_COUNT && xreg == REG_NA && xmul == 0 && disp == 0));
    assert(callType != EC_INDIR_ARD || (addr == nullptr));

    // Our stack level should be always greater than the bytes of arguments we push. Just
    // a sanity test.
    assert((unsigned)abs((signed)argSize) <= codeGen->genStackLevel);

    // Trim out any callee-trashed registers from the live set.
    regMaskTP savedSet = emitGetGCRegsSavedOrModified(methHnd);
    gcrefRegs &= savedSet;
    byrefRegs &= savedSet;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("\t\t\t\t\t\t\tCall: GCvars=%s ", VarSetOps::ToString(emitComp, ptrVars));
        dumpConvertedVarSet(emitComp, ptrVars);
        printf(", gcrefRegs=");
        printRegMaskInt(gcrefRegs);
        emitDispRegSet(gcrefRegs);
        printf(", byrefRegs=");
        printRegMaskInt(byrefRegs);
        emitDispRegSet(byrefRegs);
        printf("\n");
    }
#endif

    /* Managed RetVal: emit sequence point for the call */
    if (emitComp->opts.compDbgInfo && di.IsValid())
    {
        codeGen->genIPmappingAdd(IPmappingDscKind::Normal, di, false);
    }

    /*
        We need to allocate the appropriate instruction descriptor based
        on whether this is a direct/indirect call, and whether we need to
        record an updated set of live GC variables.

        The stats for a ton of classes is as follows:

            Direct call w/o  GC vars        220,216
            Indir. call w/o  GC vars        144,781

            Direct call with GC vars          9,440
            Indir. call with GC vars          5,768
     */

    instrDesc* id;

    assert(argSize % REGSIZE_BYTES == 0);
    int argCnt = (int)(argSize / (int)REGSIZE_BYTES); // we need a signed-divide

    if ((callType == EC_INDIR_R) || (callType == EC_INDIR_ARD))
    {
        /* Indirect call, virtual calls */

        id = emitNewInstrCallInd(argCnt, disp, ptrVars, gcrefRegs, byrefRegs,
                                 retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize));
    }
    else
    {
        // Helper/static/nonvirtual/function calls (direct or through handle),
        // and calls to an absolute addr.

        assert(callType == EC_FUNC_TOKEN || callType == EC_FUNC_TOKEN_INDIR);

        id = emitNewInstrCallDir(argCnt, ptrVars, gcrefRegs, byrefRegs,
                                 retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize));
    }

    /* Update the emitter's live GC ref sets */

    VarSetOps::Assign(emitComp, emitThisGCrefVars, ptrVars);
    emitThisGCrefRegs = gcrefRegs;
    emitThisByrefRegs = byrefRegs;

    /* Set the instruction - special case jumping a function (tail call) */
    instruction ins = INS_call;

    if (isJump)
    {
        if (callType == EC_FUNC_TOKEN)
        {
            ins = INS_l_jmp;
        }
        else
        {
            ins = INS_tail_i_jmp;
        }
    }
    id->idIns(ins);

    id->idSetIsNoGC(emitNoGChelper(methHnd));

    UNATIVE_OFFSET sz;

    // Record the address: method, indirection, or funcptr
    if ((callType == EC_INDIR_R) || (callType == EC_INDIR_ARD))
    {
        // This is an indirect call/jmp (either a virtual call or func ptr call)

        if (callType == EC_INDIR_R) // call reg
        {
            id->idSetIsCallRegPtr();
        }

        // The function is "ireg" if id->idIsCallRegPtr(),
        // else [ireg+xmul*xreg+disp]

        id->idInsFmt(IF_ARD);

        id->idAddr()->iiaAddrMode.amBaseReg = ireg;
        id->idAddr()->iiaAddrMode.amIndxReg = xreg;
        id->idAddr()->iiaAddrMode.amScale   = xmul ? emitEncodeScale(xmul) : emitter::OPSZ1;

        code_t code = insCodeMR(ins);
        if (ins == INS_tail_i_jmp)
        {
            // Tailcall with addressing mode/register needs to be rex.w
            // prefixed to be recognized as part of epilog by unwinder.
            code = AddRexWPrefix(ins, code);
        }

        sz = emitInsSizeAM(id, code);

        if (ireg == REG_NA && xreg == REG_NA)
        {
            if (codeGen->genCodeIndirAddrNeedsReloc(disp))
            {
                id->idSetIsDspReloc();
            }
#ifdef TARGET_AMD64
            else
            {
                // An absolute indir address that doesn't need reloc should fit within 32-bits
                // to be encoded as offset relative to zero.  This addr mode requires an extra
                // SIB byte
                noway_assert((size_t) static_cast<int>(reinterpret_cast<intptr_t>(addr)) == (size_t)addr);
                sz++;
            }
#endif // TARGET_AMD64
        }
    }
    else if (callType == EC_FUNC_TOKEN_INDIR)
    {
        // call/jmp [method_addr]

        assert(addr != nullptr);

        id->idInsFmt(IF_METHPTR);
        id->idAddr()->iiaAddr = (BYTE*)addr;
        sz                    = 6;

        // Since this is an indirect call through a pointer and we don't
        // currently pass in emitAttr into this function, we query codegen
        // whether addr needs a reloc.
        if (codeGen->genCodeIndirAddrNeedsReloc((size_t)addr))
        {
            id->idSetIsDspReloc();
        }
#ifdef TARGET_AMD64
        else
        {
            // An absolute indir address that doesn't need reloc should fit within 32-bits
            // to be encoded as offset relative to zero.  This addr mode requires an extra
            // SIB byte
            noway_assert((size_t) static_cast<int>(reinterpret_cast<intptr_t>(addr)) == (size_t)addr);
            sz++;
        }
#endif // TARGET_AMD64
    }
    else
    {
        // This is a simple direct call/jmp: call/jmp helper/method/addr

        assert(callType == EC_FUNC_TOKEN);

        assert(addr != nullptr);

        id->idInsFmt(IF_METHOD);
        sz = 5;

        id->idAddr()->iiaAddr = (BYTE*)addr;

        // Direct call to a method and no addr indirection is needed.
        if (codeGen->genCodeAddrNeedsReloc((size_t)addr))
        {
            id->idSetIsDspReloc();
        }
    }

#ifdef DEBUG
    if (emitComp->verbose && 0)
    {
        if (id->idIsLargeCall())
        {
            printf("[%02u] Rec call GC vars = %s\n", id->idDebugOnlyInfo()->idNum,
                   VarSetOps::ToString(emitComp, ((instrDescCGCA*)id)->idcGCvars));
        }
    }

    id->idDebugOnlyInfo()->idMemCookie = (size_t)methHnd; // method token
    id->idDebugOnlyInfo()->idCallSig   = sigInfo;
#endif // DEBUG

#ifdef LATE_DISASM
    if (addr != nullptr)
    {
        codeGen->getDisAssembler().disSetMethod((size_t)addr, methHnd);
    }
#endif // LATE_DISASM

    id->idCodeSize(sz);

    dispIns(id);
    emitCurIGsize += sz;

#if !FEATURE_FIXED_OUT_ARGS

    /* The call will pop the arguments */

    if (emitCntStackDepth && argSize > 0)
    {
        noway_assert((ssize_t)emitCurStackLvl >= argSize);
        emitCurStackLvl -= (int)argSize;
        assert((int)emitCurStackLvl >= 0);
    }

#endif // !FEATURE_FIXED_OUT_ARGS
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  The following called for each recorded instruction -- use for debugging.
 */
void emitter::emitInsSanityCheck(instrDesc* id)
{
    // make certain you only try to put relocs on things that can have them.
    ID_OPS idOp = (ID_OPS)emitFmtToOps[id->idInsFmt()];
    if ((idOp == ID_OP_SCNS) && id->idIsLargeCns())
    {
        idOp = ID_OP_CNS;
    }

    if (id->idIsDspReloc())
    {
        assert(idOp == ID_OP_NONE || idOp == ID_OP_AMD || idOp == ID_OP_DSP || idOp == ID_OP_DSP_CNS ||
               idOp == ID_OP_AMD_CNS || idOp == ID_OP_SPEC || idOp == ID_OP_CALL || idOp == ID_OP_JMP ||
               idOp == ID_OP_LBL);
    }

    if (id->idIsCnsReloc())
    {
        assert(idOp == ID_OP_CNS || idOp == ID_OP_AMD_CNS || idOp == ID_OP_DSP_CNS || idOp == ID_OP_SPEC ||
               idOp == ID_OP_CALL || idOp == ID_OP_JMP);
    }
}
#endif

/*****************************************************************************
 *
 *  Return the allocated size (in bytes) of the given instruction descriptor.
 */

size_t emitter::emitSizeOfInsDsc(instrDesc* id)
{
    if (emitIsScnsInsDsc(id))
    {
        return SMALL_IDSC_SIZE;
    }

    assert((unsigned)id->idInsFmt() < emitFmtCount);

    ID_OPS idOp = (ID_OPS)emitFmtToOps[id->idInsFmt()];

    // An INS_call instruction may use a "fat" direct/indirect call descriptor
    // except for a local call to a label (i.e. call to a finally)
    // Only ID_OP_CALL and ID_OP_SPEC check for this, so we enforce that the
    //  INS_call instruction always uses one of these idOps

    if (id->idIns() == INS_call)
    {
        assert(idOp == ID_OP_CALL || // is a direct   call
               idOp == ID_OP_SPEC || // is a indirect call
               idOp == ID_OP_JMP);   // is a local call to finally clause
    }

    switch (idOp)
    {
        case ID_OP_NONE:
#if FEATURE_LOOP_ALIGN
            if (id->idIns() == INS_align)
            {
                return sizeof(instrDescAlign);
            }
#endif
            break;

        case ID_OP_LBL:
            return sizeof(instrDescLbl);

        case ID_OP_JMP:
            return sizeof(instrDescJmp);

        case ID_OP_CALL:
        case ID_OP_SPEC:
            if (id->idIsLargeCall())
            {
                /* Must be a "fat" indirect call descriptor */
                return sizeof(instrDescCGCA);
            }

            FALLTHROUGH;

        case ID_OP_SCNS:
        case ID_OP_CNS:
        case ID_OP_DSP:
        case ID_OP_DSP_CNS:
            if (id->idIsLargeCns())
            {
                if (id->idIsLargeDsp())
                {
                    return sizeof(instrDescCnsDsp);
                }
                else
                {
                    return sizeof(instrDescCns);
                }
            }
            else
            {
                if (id->idIsLargeDsp())
                {
                    return sizeof(instrDescDsp);
                }
                else
                {
                    return sizeof(instrDesc);
                }
            }
        case ID_OP_AMD:
        case ID_OP_AMD_CNS:
            if (id->idIsLargeCns())
            {
                if (id->idIsLargeDsp())
                {
                    return sizeof(instrDescCnsAmd);
                }
                else
                {
                    return sizeof(instrDescCns);
                }
            }
            else
            {
                if (id->idIsLargeDsp())
                {
                    return sizeof(instrDescAmd);
                }
                else
                {
                    return sizeof(instrDesc);
                }
            }

        default:
            NO_WAY("unexpected instruction descriptor format");
            break;
    }

    return sizeof(instrDesc);
}

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************
 *
 *  Return a string that represents the given register.
 */

const char* emitter::emitRegName(regNumber reg, emitAttr attr, bool varName)
{
    static char          rb[2][128];
    static unsigned char rbc = 0;

    const char* rn = emitComp->compRegVarName(reg, varName);

#ifdef TARGET_AMD64
    char suffix = '\0';

    switch (EA_SIZE(attr))
    {
        case EA_32BYTE:
            return emitYMMregName(reg);

        case EA_16BYTE:
            return emitXMMregName(reg);

        case EA_8BYTE:
            if ((REG_XMM0 <= reg) && (reg <= REG_XMM15))
            {
                return emitXMMregName(reg);
            }
            break;

        case EA_4BYTE:
            if ((REG_XMM0 <= reg) && (reg <= REG_XMM15))
            {
                return emitXMMregName(reg);
            }

            if (reg > REG_R15)
            {
                break;
            }

            if (reg > REG_RDI)
            {
                suffix = 'd';
                goto APPEND_SUFFIX;
            }
            rbc        = (rbc + 1) % 2;
            rb[rbc][0] = 'e';
            rb[rbc][1] = rn[1];
            rb[rbc][2] = rn[2];
            rb[rbc][3] = 0;
            rn         = rb[rbc];
            break;

        case EA_2BYTE:
            if (reg > REG_RDI)
            {
                suffix = 'w';
                goto APPEND_SUFFIX;
            }
            rn++;
            break;

        case EA_1BYTE:
            if (reg > REG_RDI)
            {
                suffix = 'b';
            APPEND_SUFFIX:
                rbc        = (rbc + 1) % 2;
                rb[rbc][0] = rn[0];
                rb[rbc][1] = rn[1];
                if (rn[2])
                {
                    assert(rn[3] == 0);
                    rb[rbc][2] = rn[2];
                    rb[rbc][3] = suffix;
                    rb[rbc][4] = 0;
                }
                else
                {
                    rb[rbc][2] = suffix;
                    rb[rbc][3] = 0;
                }
            }
            else
            {
                rbc        = (rbc + 1) % 2;
                rb[rbc][0] = rn[1];
                if (reg < 4)
                {
                    rb[rbc][1] = 'l';
                    rb[rbc][2] = 0;
                }
                else
                {
                    rb[rbc][1] = rn[2];
                    rb[rbc][2] = 'l';
                    rb[rbc][3] = 0;
                }
            }

            rn = rb[rbc];
            break;

        default:
            break;
    }
#endif // TARGET_AMD64

#ifdef TARGET_X86
    assert(strlen(rn) >= 3);

    switch (EA_SIZE(attr))
    {
        case EA_32BYTE:
            return emitYMMregName(reg);

        case EA_16BYTE:
            return emitXMMregName(reg);

        case EA_8BYTE:
            if ((REG_XMM0 <= reg) && (reg <= REG_XMM7))
            {
                return emitXMMregName(reg);
            }
            break;

        case EA_4BYTE:
            if ((REG_XMM0 <= reg) && (reg <= REG_XMM7))
            {
                return emitXMMregName(reg);
            }
            break;

        case EA_2BYTE:
            rn++;
            break;

        case EA_1BYTE:
            rbc        = (rbc + 1) % 2;
            rb[rbc][0] = rn[1];
            rb[rbc][1] = 'l';
            strcpy_s(&rb[rbc][2], sizeof(rb[0]) - 2, rn + 3);

            rn = rb[rbc];
            break;

        default:
            break;
    }
#endif // TARGET_X86

#if 0
    // The following is useful if you want register names to be tagged with * or ^ representing gcref or byref, respectively,
    // however it's possibly not interesting most of the time.
    if (EA_IS_GCREF(attr) || EA_IS_BYREF(attr))
    {
        if (rn != rb[rbc])
        {
            rbc = (rbc+1)%2;
            strcpy_s(rb[rbc], sizeof(rb[rbc]), rn);
            rn = rb[rbc];
        }

        if (EA_IS_GCREF(attr))
        {
            strcat_s(rb[rbc], sizeof(rb[rbc]), "*");
        }
        else if (EA_IS_BYREF(attr))
        {
            strcat_s(rb[rbc], sizeof(rb[rbc]), "^");
        }
    }
#endif // 0

    return rn;
}

/*****************************************************************************
 *
 *  Return a string that represents the given XMM register.
 */

const char* emitter::emitXMMregName(unsigned reg)
{
    static const char* const regNames[] = {
#define REGDEF(name, rnum, mask, sname) "x" sname,
#include "register.h"
    };

    assert(reg < REG_COUNT);
    assert(reg < ArrLen(regNames));

    return regNames[reg];
}

/*****************************************************************************
 *
 *  Return a string that represents the given YMM register.
 */

const char* emitter::emitYMMregName(unsigned reg)
{
    static const char* const regNames[] = {
#define REGDEF(name, rnum, mask, sname) "y" sname,
#include "register.h"
    };

    assert(reg < REG_COUNT);
    assert(reg < ArrLen(regNames));

    return regNames[reg];
}

/*****************************************************************************
 *
 *  Display a static data member reference.
 */

void emitter::emitDispClsVar(CORINFO_FIELD_HANDLE fldHnd, ssize_t offs, bool reloc /* = false */)
{
    int doffs;

    /* Filter out the special case of fs:[offs] */

    // Munge any pointers if we want diff-able disassembly
    if (emitComp->opts.disDiffable)
    {
        ssize_t top12bits = (offs >> 20);
        if ((top12bits != 0) && (top12bits != -1))
        {
            offs = 0xD1FFAB1E;
        }
    }

    if (fldHnd == FLD_GLOBAL_FS)
    {
        printf("FS:[0x%04X]", offs);
        return;
    }

    if (fldHnd == FLD_GLOBAL_DS)
    {
        printf("[0x%04X]", offs);
        return;
    }

    printf("[");

    doffs = Compiler::eeGetJitDataOffs(fldHnd);

    if (reloc)
    {
        printf("reloc ");
    }

    if (doffs >= 0)
    {
        if (doffs & 1)
        {
            printf("@CNS%02u", doffs - 1);
        }
        else
        {
            printf("@RWD%02u", doffs);
        }

        if (offs)
        {
            printf("%+Id", offs);
        }
    }
    else
    {
        printf("classVar[%#x]", emitComp->dspPtr(fldHnd));

        if (offs)
        {
            printf("%+Id", offs);
        }
    }

    printf("]");

    if (emitComp->opts.varNames && offs < 0)
    {
        printf("'%s", emitComp->eeGetFieldName(fldHnd));
        if (offs)
        {
            printf("%+Id", offs);
        }
        printf("'");
    }
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    int  addr;
    bool bEBP;

    printf("[");

    if (!asmfm || emitComp->lvaDoneFrameLayout == Compiler::NO_FRAME_LAYOUT)
    {
        if (varx < 0)
        {
            printf("TEMP_%02u", -varx);
        }
        else
        {
            printf("V%02u", +varx);
        }

        if (disp < 0)
        {
            printf("-0x%X", -disp);
        }
        else if (disp > 0)
        {
            printf("+0x%X", +disp);
        }
    }

    if (emitComp->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT)
    {
        if (!asmfm)
        {
            printf(" ");
        }

        addr = emitComp->lvaFrameAddress(varx, &bEBP) + disp;

        if (bEBP)
        {
            printf(STR_FPBASE);

            if (addr < 0)
            {
                printf("-%02XH", -addr);
            }
            else if (addr > 0)
            {
                printf("+%02XH", addr);
            }
        }
        else
        {
            /* Adjust the offset by amount currently pushed on the stack */

            printf(STR_SPBASE);

            if (addr < 0)
            {
                printf("-%02XH", -addr);
            }
            else if (addr > 0)
            {
                printf("+%02XH", addr);
            }

#if !FEATURE_FIXED_OUT_ARGS

            if (emitCurStackLvl)
                printf("+%02XH", emitCurStackLvl);

#endif // !FEATURE_FIXED_OUT_ARGS
        }
    }

    printf("]");

    if (varx >= 0 && emitComp->opts.varNames)
    {
        const char* varName = emitComp->compLocalVarName(varx, offs);

        if (varName)
        {
            printf("'%s", varName);

            if (disp < 0)
            {
                printf("-%d", -disp);
            }
            else if (disp > 0)
            {
                printf("+%d", +disp);
            }

            printf("'");
        }
    }
}

/*****************************************************************************
 *
 *  Display a reloc value
 *  If we are formatting for a diffable assembly listing don't print the hex value
 *  since it will prevent us from doing assembly diffs
 */
void emitter::emitDispReloc(ssize_t value)
{
    if (emitComp->opts.disAsm && emitComp->opts.disDiffable)
    {
        printf("(reloc)");
    }
    else
    {
        printf("(reloc 0x%Ix)", emitComp->dspPtr(value));
    }
}

/*****************************************************************************
 *
 *  Display an address mode.
 */

void emitter::emitDispAddrMode(instrDesc* id, bool noDetail)
{
    bool    nsep = false;
    ssize_t disp;

    unsigned     jtno = 0;
    dataSection* jdsc = nullptr;

    /* The displacement field is in an unusual place for calls */

    disp = (id->idIns() == INS_call) || (id->idIns() == INS_tail_i_jmp) ? emitGetInsCIdisp(id) : emitGetInsAmdAny(id);

    /* Display a jump table label if this is a switch table jump */

    if (id->idIns() == INS_i_jmp)
    {
        UNATIVE_OFFSET offs = 0;

        /* Find the appropriate entry in the data section list */

        for (jdsc = emitConsDsc.dsdList, jtno = 0; jdsc; jdsc = jdsc->dsNext)
        {
            UNATIVE_OFFSET size = jdsc->dsSize;

            /* Is this a label table? */

            if (size & 1)
            {
                size--;
                jtno++;

                if (offs == id->idDebugOnlyInfo()->idMemCookie)
                {
                    break;
                }
            }

            offs += size;
        }

        /* If we've found a matching entry then is a table jump */

        if (jdsc)
        {
            if (id->idIsDspReloc())
            {
                printf("reloc ");
            }
            printf("J_M%03u_DS%02u", emitComp->compMethodID, id->idDebugOnlyInfo()->idMemCookie);

            disp -= id->idDebugOnlyInfo()->idMemCookie;
        }
    }

    bool frameRef = false;

    printf("[");

    if (id->idAddr()->iiaAddrMode.amBaseReg != REG_NA)
    {
        printf("%s", emitRegName(id->idAddr()->iiaAddrMode.amBaseReg));
        nsep = true;
        if (id->idAddr()->iiaAddrMode.amBaseReg == REG_ESP)
        {
            frameRef = true;
        }
        else if (emitComp->isFramePointerUsed() && id->idAddr()->iiaAddrMode.amBaseReg == REG_EBP)
        {
            frameRef = true;
        }
    }

    if (id->idAddr()->iiaAddrMode.amIndxReg != REG_NA)
    {
        size_t scale = emitDecodeScale(id->idAddr()->iiaAddrMode.amScale);

        if (nsep)
        {
            printf("+");
        }
        if (scale > 1)
        {
            printf("%u*", scale);
        }
        printf("%s", emitRegName(id->idAddr()->iiaAddrMode.amIndxReg));
        nsep = true;
    }

    if ((id->idIsDspReloc()) && (id->idIns() != INS_i_jmp))
    {
        if (nsep)
        {
            printf("+");
        }
        emitDispReloc(disp);
    }
    else
    {
        // Munge any pointers if we want diff-able disassembly
        // It's assumed to be a pointer when disp is outside of the range (-1M, +1M); top bits are not 0 or -1
        if (!frameRef && emitComp->opts.disDiffable && (static_cast<size_t>((disp >> 20) + 1) > 1))
        {
            if (nsep)
            {
                printf("+");
            }
            printf("D1FFAB1EH");
        }
        else if (disp > 0)
        {
            if (nsep)
            {
                printf("+");
            }
            if (frameRef)
            {
                printf("%02XH", disp);
            }
            else if (disp < 1000)
            {
                printf("%d", disp);
            }
            else if (disp <= 0xFFFF)
            {
                printf("%04XH", disp);
            }
            else
            {
                printf("%08XH", disp);
            }
        }
        else if (disp < 0)
        {
            if (frameRef)
            {
                printf("-%02XH", -disp);
            }
            else if (disp > -1000)
            {
                printf("-%d", -disp);
            }
            else if (disp >= -0xFFFF)
            {
                printf("-%04XH", -disp);
            }
            else if (disp < -0xFFFFFF)
            {
                if (nsep)
                {
                    printf("+");
                }
                printf("%08XH", disp);
            }
            else
            {
                printf("-%08XH", -disp);
            }
        }
        else if (!nsep)
        {
            printf("%04XH", disp);
        }
    }

    printf("]");

    // pretty print string if it looks like one
    if ((id->idGCref() == GCT_GCREF) && (id->idIns() == INS_mov) && (id->idAddr()->iiaAddrMode.amBaseReg == REG_NA))
    {
        const WCHAR* str = emitComp->eeGetCPString(disp);
        if (str != nullptr)
        {
            printf("      '%S'", str);
        }
    }

    if (jdsc && !noDetail)
    {
        unsigned     cnt = (jdsc->dsSize - 1) / TARGET_POINTER_SIZE;
        BasicBlock** bbp = (BasicBlock**)jdsc->dsCont;

#ifdef TARGET_AMD64
#define SIZE_LETTER "Q"
#else
#define SIZE_LETTER "D"
#endif
        printf("\n\n    J_M%03u_DS%02u LABEL   " SIZE_LETTER "WORD", emitComp->compMethodID, jtno);

        /* Display the label table (it's stored as "BasicBlock*" values) */

        do
        {
            insGroup* lab;

            /* Convert the BasicBlock* value to an IG address */

            lab = (insGroup*)emitCodeGetCookie(*bbp++);
            assert(lab);

            printf("\n            D" SIZE_LETTER "      %s", emitLabelString(lab));
        } while (--cnt);
    }
}

/*****************************************************************************
 *
 *  If the given instruction is a shift, display the 2nd operand.
 */

void emitter::emitDispShift(instruction ins, int cnt)
{
    switch (ins)
    {
        case INS_rcl_1:
        case INS_rcr_1:
        case INS_rol_1:
        case INS_ror_1:
        case INS_shl_1:
        case INS_shr_1:
        case INS_sar_1:
            printf(", 1");
            break;

        case INS_rcl:
        case INS_rcr:
        case INS_rol:
        case INS_ror:
        case INS_shl:
        case INS_shr:
        case INS_sar:
            printf(", cl");
            break;

        case INS_rcl_N:
        case INS_rcr_N:
        case INS_rol_N:
        case INS_ror_N:
        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
            printf(", %d", cnt);
            break;

        default:
            break;
    }
}

/*****************************************************************************
 *
 *  Display (optionally) the bytes for the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable)
    {
#ifdef TARGET_AMD64
        // how many bytes per instruction we format for
        const size_t digits = 10;
#else // TARGET_X86
        const size_t digits = 6;
#endif
        printf(" ");
        for (unsigned i = 0; i < sz; i++)
        {
            printf("%02X", (*((BYTE*)(code + i))));
        }

        if (sz < digits)
        {
            printf("%.*s", 2 * (digits - sz), "                         ");
        }
    }
}

//--------------------------------------------------------------------
// emitDispIns: Dump the given instruction to jitstdout.
//
// Arguments:
//   id - The instruction
//   isNew - Whether the instruction is newly generated (before encoding).
//   doffs - If true, always display the passed-in offset.
//   asmfm - Whether the instruction should be displayed in assembly format.
//           If false some additional information may be printed for the instruction.
//   offset - The offset of the instruction. Only displayed if doffs is true or if
//            !isNew && !asmfm.
//   code - Pointer to the actual code, used for displaying the address and encoded bytes
//          if turned on.
//   sz - The size of the instruction, used to display the encoded bytes.
//   ig - The instruction group containing the instruction. Not used on xarch.
//
void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* code, size_t sz, insGroup* ig)
{
    emitAttr    attr;
    const char* sstr;

    instruction ins = id->idIns();

    if (emitComp->verbose)
    {
        unsigned idNum = id->idDebugOnlyInfo()->idNum;
        printf("IN%04x: ", idNum);
    }

#define ID_INFO_DSP_RELOC ((bool)(id->idIsDspReloc()))

    /* Display a constant value if the instruction references one */

    if (!isNew)
    {
        switch (id->idInsFmt())
        {
            int offs;

            case IF_MRD_RRD:
            case IF_MWR_RRD:
            case IF_MRW_RRD:

            case IF_RRD_MRD:
            case IF_RWR_MRD:
            case IF_RRW_MRD:

            case IF_MRD_CNS:
            case IF_MWR_CNS:
            case IF_MRW_CNS:
            case IF_MRW_SHF:

            case IF_MRD:
            case IF_MWR:
            case IF_MRW:

            case IF_MRD_OFF:

                /* Is this actually a reference to a data section? */

                offs = Compiler::eeGetJitDataOffs(id->idAddr()->iiaFieldHnd);

                if (offs >= 0)
                {
                    void* addr;

                    /* Display a data section reference */

                    assert((unsigned)offs < emitConsDsc.dsdOffs);
                    addr = emitConsBlock ? emitConsBlock + offs : nullptr;

#if 0
                // TODO-XArch-Cleanup: Fix or remove this code.
                /* Is the operand an integer or floating-point value? */

                bool isFP = false;

                if  (CodeGen::instIsFP(id->idIns()))
                {
                    switch (id->idIns())
                    {
                    case INS_fild:
                    case INS_fildl:
                        break;

                    default:
                        isFP = true;
                        break;
                    }
                }

                if (offs & 1)
                    printf("@CNS%02u", offs);
                else
                    printf("@RWD%02u", offs);

                printf("      ");

                if  (addr)
                {
                    addr = 0;
                    // TODO-XArch-Bug?:
                    //          This was busted by switching the order
                    //          in which we output the code block vs.
                    //          the data blocks -- when we get here,
                    //          the data block has not been filled in
                    //          yet, so we'll display garbage.

                    if  (isFP)
                    {
                        if  (id->idOpSize() == EA_4BYTE)
                            printf("DF      %f \n", addr ? *(float   *)addr : 0);
                        else
                            printf("DQ      %lf\n", addr ? *(double  *)addr : 0);
                    }
                    else
                    {
                        if  (id->idOpSize() <= EA_4BYTE)
                            printf("DD      %d \n", addr ? *(int     *)addr : 0);
                        else
                            printf("DQ      %D \n", addr ? *(__int64 *)addr : 0);
                    }
                }
#endif
                }
                break;

            default:
                break;
        }
    }

    // printf("[F=%s] "   , emitIfName(id->idInsFmt()));
    // printf("INS#%03u: ", id->idDebugOnlyInfo()->idNum);
    // printf("[S=%02u] " , emitCurStackLvl); if (isNew) printf("[M=%02u] ", emitMaxStackDepth);
    // printf("[S=%02u] " , emitCurStackLvl/sizeof(INT32));
    // printf("[A=%08X] " , emitSimpleStkMask);
    // printf("[A=%08X] " , emitSimpleByrefStkMask);
    // printf("[L=%02u] " , id->idCodeSize());

    if (!isNew && !asmfm)
    {
        doffs = true;
    }

    /* Display the instruction address */

    emitDispInsAddr(code);

    /* Display the instruction offset */

    emitDispInsOffs(offset, doffs);

    if (code != nullptr)
    {
        /* Display the instruction hex code */
        assert(((code >= emitCodeBlock) && (code < emitCodeBlock + emitTotalHotCodeSize)) ||
               ((code >= emitColdCodeBlock) && (code < emitColdCodeBlock + emitTotalColdCodeSize)));

        emitDispInsHex(id, code + writeableOffset, sz);
    }

    /* Display the instruction name */

    sstr = codeGen->genInsDisplayName(id);
    printf(" %-9s", sstr);

#ifndef HOST_UNIX
    if (strnlen_s(sstr, 10) >= 9)
#else  // HOST_UNIX
    if (strnlen(sstr, 10) >= 9)
#endif // HOST_UNIX
    {
        // Make sure there's at least one space after the instruction name, for very long instruction names.
        printf(" ");
    }

    /* By now the size better be set to something */

    assert(id->idCodeSize() || emitInstHasNoCode(ins));

    /* Figure out the operand size */

    if (id->idGCref() == GCT_GCREF)
    {
        attr = EA_GCREF;
        sstr = "gword ptr ";
    }
    else if (id->idGCref() == GCT_BYREF)
    {
        attr = EA_BYREF;
        sstr = "bword ptr ";
    }
    else
    {
        attr = id->idOpSize();
        sstr = codeGen->genSizeStr(emitGetMemOpSize(id));

        if (ins == INS_lea)
        {
#ifdef TARGET_AMD64
            assert((attr == EA_4BYTE) || (attr == EA_8BYTE));
#else
            assert(attr == EA_4BYTE);
#endif
            sstr = "";
        }
    }

    /* Now see what instruction format we've got */

    // First print the implicit register usage
    if (instrHasImplicitRegPairDest(ins))
    {
        printf("%s:%s, ", emitRegName(REG_EDX, id->idOpSize()), emitRegName(REG_EAX, id->idOpSize()));
    }
    else if (instrIs3opImul(ins))
    {
        regNumber tgtReg = inst3opImulReg(ins);
        printf("%s, ", emitRegName(tgtReg, id->idOpSize()));
    }

    switch (id->idInsFmt())
    {
        ssize_t     val;
        ssize_t     offs;
        CnsVal      cnsVal;
        const char* methodName;

        case IF_CNS:
            val = emitGetInsSC(id);
#ifdef TARGET_AMD64
            // no 8-byte immediates allowed here!
            assert((val >= (ssize_t)0xFFFFFFFF80000000LL) && (val <= 0x000000007FFFFFFFLL));
#endif
            if (id->idIsCnsReloc())
            {
                emitDispReloc(val);
            }
            else
            {
            PRINT_CONSTANT:
                ssize_t srcVal = val;
                // Munge any pointers if we want diff-able disassembly
                if (emitComp->opts.disDiffable)
                {
                    ssize_t top14bits = (val >> 18);
                    if ((top14bits != 0) && (top14bits != -1))
                    {
                        val = 0xD1FFAB1E;
                    }
                }
                if ((val > -1000) && (val < 1000))
                {
                    printf("%d", val);
                }
                else if ((val > 0) || (val < -0xFFFFFF))
                {
                    printf("0x%IX", val);
                }
                else
                { // (val < 0)
                    printf("-0x%IX", -val);
                }
                emitDispCommentForHandle(srcVal, id->idDebugOnlyInfo()->idFlags);
            }
            break;

        case IF_ARD:
        case IF_AWR:
        case IF_ARW:

            if (id->idIsCallRegPtr())
            {
                printf("%s", emitRegName(id->idAddr()->iiaAddrMode.amBaseReg));
            }
            else
            {
                // GC ref bit is for the return value for calls, do not print it before the address mode
                if ((ins != INS_call) && (ins != INS_tail_i_jmp))
                {
                    printf("%s", sstr);
                }

                emitDispAddrMode(id, isNew);
                emitDispShift(ins);
            }

            if ((ins == INS_call) || (ins == INS_tail_i_jmp))
            {
                assert(id->idInsFmt() == IF_ARD);

                /* Ignore indirect calls */

                if (id->idDebugOnlyInfo()->idMemCookie == 0)
                {
                    break;
                }

                assert(id->idDebugOnlyInfo()->idMemCookie);

                if (id->idIsCallRegPtr())
                {
                    printf(" ; ");
                }

                /* This is a virtual call */

                methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
                printf("%s", methodName);
            }
            break;

        case IF_RRD_ARD:
        case IF_RWR_ARD:
        case IF_RRW_ARD:
#ifdef TARGET_AMD64
            if (ins == INS_movsxd)
            {
                attr = EA_8BYTE;
            }
            else
#endif
                if (ins == INS_movsx || ins == INS_movzx)
            {
                attr = EA_PTRSIZE;
            }
            else if ((ins == INS_crc32) && (attr != EA_8BYTE))
            {
                // The idReg1 is always 4 bytes, but the size of idReg2 can vary.
                // This logic ensures that we print `crc32 eax, bx` instead of `crc32 ax, bx`
                attr = EA_4BYTE;
            }
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            emitDispAddrMode(id);
            break;

        case IF_RRW_ARD_CNS:
        case IF_RWR_ARD_CNS:
        {
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            emitDispAddrMode(id);
            emitGetInsAmdCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }

            break;
        }

        case IF_AWR_RRD_CNS:
        {
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            // vextracti/f128 extracts 128-bit data, so we fix sstr as "xmm ptr"
            sstr = codeGen->genSizeStr(EA_ATTR(16));
            printf(sstr);
            emitDispAddrMode(id);
            printf(", %s", emitRegName(id->idReg1(), attr));

            emitGetInsAmdCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }

            break;
        }

        case IF_RWR_RRD_ARD:
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            emitDispAddrMode(id);
            break;

        case IF_RWR_ARD_RRD:
            if (ins == INS_vpgatherqd || ins == INS_vgatherqps)
            {
                attr = EA_16BYTE;
            }
            sstr = codeGen->genSizeStr(EA_ATTR(4));
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            emitDispAddrMode(id);
            printf(", %s", emitRegName(id->idReg2(), attr));
            break;

        case IF_RWR_RRD_ARD_CNS:
        {
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            emitDispAddrMode(id);
            emitGetInsAmdCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }

            break;
        }

        case IF_RWR_RRD_ARD_RRD:
        {
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(id->idReg2(), attr));
            emitDispAddrMode(id);

            emitGetInsAmdCns(id, &cnsVal);
            val = (cnsVal.cnsVal >> 4) + XMMBASE;
            printf(", %s", emitRegName((regNumber)val, attr));
            break;
        }

        case IF_ARD_RRD:
        case IF_AWR_RRD:
        case IF_ARW_RRD:

            printf("%s", sstr);
            emitDispAddrMode(id);
            printf(", %s", emitRegName(id->idReg1(), attr));
            break;

        case IF_AWR_RRD_RRD:
        {
            printf("%s", sstr);
            emitDispAddrMode(id);
            printf(", %s", emitRegName(id->idReg1(), attr));
            printf(", %s", emitRegName(id->idReg2(), attr));
            break;
        }

        case IF_ARD_CNS:
        case IF_AWR_CNS:
        case IF_ARW_CNS:
        case IF_ARW_SHF:

            printf("%s", sstr);
            emitDispAddrMode(id);
            emitGetInsAmdCns(id, &cnsVal);
            val = cnsVal.cnsVal;
#ifdef TARGET_AMD64
            // no 8-byte immediates allowed here!
            assert((val >= (ssize_t)0xFFFFFFFF80000000LL) && (val <= 0x000000007FFFFFFFLL));
#endif
            if (id->idInsFmt() == IF_ARW_SHF)
            {
                emitDispShift(ins, (BYTE)val);
            }
            else
            {
                printf(", ");
                if (cnsVal.cnsReloc)
                {
                    emitDispReloc(val);
                }
                else
                {
                    goto PRINT_CONSTANT;
                }
            }
            break;

        case IF_SRD:
        case IF_SWR:
        case IF_SRW:

            printf("%s", sstr);

#if !FEATURE_FIXED_OUT_ARGS
            if (ins == INS_pop)
                emitCurStackLvl -= sizeof(int);
#endif

            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

#if !FEATURE_FIXED_OUT_ARGS
            if (ins == INS_pop)
                emitCurStackLvl += sizeof(int);
#endif

            emitDispShift(ins);
            break;

        case IF_SRD_RRD:
        case IF_SWR_RRD:
        case IF_SRW_RRD:

            printf("%s", sstr);

            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

            printf(", %s", emitRegName(id->idReg1(), attr));
            break;

        case IF_SRD_CNS:
        case IF_SWR_CNS:
        case IF_SRW_CNS:
        case IF_SRW_SHF:

            printf("%s", sstr);

            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

            emitGetInsCns(id, &cnsVal);
            val = cnsVal.cnsVal;
#ifdef TARGET_AMD64
            // no 8-byte immediates allowed here!
            assert((val >= (ssize_t)0xFFFFFFFF80000000LL) && (val <= 0x000000007FFFFFFFLL));
#endif
            if (id->idInsFmt() == IF_SRW_SHF)
            {
                emitDispShift(ins, (BYTE)val);
            }
            else
            {
                printf(", ");
                if (cnsVal.cnsReloc)
                {
                    emitDispReloc(val);
                }
                else
                {
                    goto PRINT_CONSTANT;
                }
            }
            break;

        case IF_SWR_RRD_CNS:
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            assert(UseVEXEncoding());
            emitGetInsAmdCns(id, &cnsVal);

            printf("%s", sstr);

            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

            printf(", %s", emitRegName(id->idReg1(), attr));

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;

        case IF_RRD_SRD:
        case IF_RWR_SRD:
        case IF_RRW_SRD:
#ifdef TARGET_AMD64
            if (ins == INS_movsxd)
            {
                attr = EA_8BYTE;
            }
            else
#endif
                if (ins == INS_movsx || ins == INS_movzx)
            {
                attr = EA_PTRSIZE;
            }
            else if ((ins == INS_crc32) && (attr != EA_8BYTE))
            {
                // The idReg1 is always 4 bytes, but the size of idReg2 can vary.
                // This logic ensures that we print `crc32 eax, bx` instead of `crc32 ax, bx`
                attr = EA_4BYTE;
            }

            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

            break;

        case IF_RRW_SRD_CNS:
        case IF_RWR_SRD_CNS:
        {
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);
            emitGetInsCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;
        }

        case IF_RWR_RRD_SRD:
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);
            break;

        case IF_RWR_RRD_SRD_CNS:
        {
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);
            emitGetInsCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;
        }

        case IF_RWR_RRD_SRD_RRD:
        {
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(id->idReg2(), attr));
            emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                             id->idDebugOnlyInfo()->idVarRefOffs, asmfm);

            emitGetInsCns(id, &cnsVal);
            val = (cnsVal.cnsVal >> 4) + XMMBASE;
            printf(", %s", emitRegName((regNumber)val, attr));
            break;
        }

        case IF_RRD_RRD:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
            if (ins == INS_pmovmskb)
            {
                printf("%s, %s", emitRegName(id->idReg1(), EA_4BYTE), emitRegName(id->idReg2(), attr));
            }
            else if ((ins == INS_cvtsi2ss) || (ins == INS_cvtsi2sd))
            {
                printf(" %s, %s", emitRegName(id->idReg1(), EA_16BYTE), emitRegName(id->idReg2(), attr));
            }
            else if ((ins == INS_cvttsd2si) || (ins == INS_cvtss2si) || (ins == INS_cvtsd2si) || (ins == INS_cvttss2si))
            {
                printf(" %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), EA_16BYTE));
            }
#ifdef TARGET_AMD64
            else if (ins == INS_movsxd)
            {
                printf("%s, %s", emitRegName(id->idReg1(), EA_8BYTE), emitRegName(id->idReg2(), EA_4BYTE));
            }
#endif // TARGET_AMD64
            else if (ins == INS_movsx || ins == INS_movzx)
            {
                printf("%s, %s", emitRegName(id->idReg1(), EA_PTRSIZE), emitRegName(id->idReg2(), attr));
            }
            else if (ins == INS_bt)
            {
                // INS_bt operands are reversed. Display them in the normal order.
                printf("%s, %s", emitRegName(id->idReg2(), attr), emitRegName(id->idReg1(), attr));
            }
#ifdef FEATURE_HW_INTRINSICS
            else if (ins == INS_crc32 && attr != EA_8BYTE)
            {
                // The idReg1 is always 4 bytes, but the size of idReg2 can vary.
                // This logic ensures that we print `crc32 eax, bx` instead of `crc32 ax, bx`
                printf("%s, %s", emitRegName(id->idReg1(), EA_4BYTE), emitRegName(id->idReg2(), attr));
            }
#endif // FEATURE_HW_INTRINSICS
            else
            {
                printf("%s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr));
            }
            break;

        case IF_RRW_RRW:
            assert(ins == INS_xchg);
            printf("%s,", emitRegName(id->idReg1(), attr));
            printf(" %s", emitRegName(id->idReg2(), attr));
            break;

        case IF_RWR_RRD_RRD:
        {
            assert(IsAVXInstruction(ins));
            assert(IsThreeOperandAVXInstruction(ins));
            regNumber reg2 = id->idReg2();
            regNumber reg3 = id->idReg3();
            if (ins == INS_bextr || ins == INS_bzhi)
            {
                // BMI bextr and bzhi encodes the reg2 in VEX.vvvv and reg3 in modRM,
                // which is different from most of other instructions
                regNumber tmp = reg2;
                reg2          = reg3;
                reg3          = tmp;
            }
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(reg2, attr));
            printf("%s", emitRegName(reg3, attr));
            break;
        }

        case IF_RWR_RRD_RRD_CNS:
            assert(IsAVXInstruction(ins));
            assert(IsThreeOperandAVXInstruction(ins));
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(id->idReg2(), attr));

            switch (ins)
            {
                case INS_vinsertf128:
                case INS_vinserti128:
                {
                    attr = EA_16BYTE;
                    break;
                }

                case INS_pinsrb:
                case INS_pinsrw:
                case INS_pinsrd:
                {
                    attr = EA_4BYTE;
                    break;
                }

                case INS_pinsrq:
                {
                    attr = EA_8BYTE;
                    break;
                }

                default:
                {
                    break;
                }
            }

            printf("%s, ", emitRegName(id->idReg3(), attr));
            val = emitGetInsSC(id);
            goto PRINT_CONSTANT;
            break;
        case IF_RWR_RRD_RRD_RRD:
            assert(IsAVXOnlyInstruction(ins));
            assert(UseVEXEncoding());
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(id->idReg2(), attr));
            printf("%s, ", emitRegName(id->idReg3(), attr));
            printf("%s", emitRegName(id->idReg4(), attr));
            break;
        case IF_RRW_RRW_CNS:
        {
            emitAttr tgtAttr = attr;

            switch (ins)
            {
                case INS_vextractf128:
                case INS_vextracti128:
                {
                    tgtAttr = EA_16BYTE;
                    break;
                }

                case INS_extractps:
                case INS_pextrb:
                case INS_pextrw:
                case INS_pextrw_sse41:
                case INS_pextrd:
                {
                    tgtAttr = EA_4BYTE;
                    break;
                }

                case INS_pextrq:
                {
                    tgtAttr = EA_8BYTE;
                    break;
                }

                case INS_pinsrb:
                case INS_pinsrw:
                case INS_pinsrd:
                {
                    attr = EA_4BYTE;
                    break;
                }

                case INS_pinsrq:
                {
                    attr = EA_8BYTE;
                    break;
                }

                default:
                {
                    break;
                }
            }

            printf("%s,", emitRegName(id->idReg1(), tgtAttr));
            printf(" %s", emitRegName(id->idReg2(), attr));
            val = emitGetInsSC(id);
#ifdef TARGET_AMD64
            // no 8-byte immediates allowed here!
            assert((val >= (ssize_t)0xFFFFFFFF80000000LL) && (val <= 0x000000007FFFFFFFLL));
#endif
            printf(", ");
            if (id->idIsCnsReloc())
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;
        }

        case IF_RRD:
        case IF_RWR:
        case IF_RRW:
            printf("%s", emitRegName(id->idReg1(), attr));
            emitDispShift(ins);
            break;

        case IF_RRW_SHF:
            printf("%s", emitRegName(id->idReg1(), attr));
            emitDispShift(ins, (BYTE)emitGetInsSC(id));
            break;

        case IF_RRD_MRD:
        case IF_RWR_MRD:
        case IF_RRW_MRD:

            if (ins == INS_movsx || ins == INS_movzx)
            {
                attr = EA_PTRSIZE;
            }
#ifdef TARGET_AMD64
            else if (ins == INS_movsxd)
            {
                attr = EA_PTRSIZE;
            }
#endif
            else if ((ins == INS_crc32) && (attr != EA_8BYTE))
            {
                // The idReg1 is always 4 bytes, but the size of idReg2 can vary.
                // This logic ensures that we print `crc32 eax, bx` instead of `crc32 ax, bx`
                attr = EA_4BYTE;
            }
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            break;

        case IF_RRW_MRD_CNS:
        case IF_RWR_MRD_CNS:
        {
            printf("%s, %s", emitRegName(id->idReg1(), attr), sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            emitGetInsDcmCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;
        }

        case IF_MWR_RRD_CNS:
        {
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            // vextracti/f128 extracts 128-bit data, so we fix sstr as "xmm ptr"
            sstr = codeGen->genSizeStr(EA_ATTR(16));
            printf(sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            printf(", %s", emitRegName(id->idReg1(), attr));
            emitGetInsDcmCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }

            break;
        }

        case IF_RWR_RRD_MRD:
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            break;

        case IF_RWR_RRD_MRD_CNS:
        {
            printf("%s, %s, %s", emitRegName(id->idReg1(), attr), emitRegName(id->idReg2(), attr), sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            emitGetInsDcmCns(id, &cnsVal);

            val = cnsVal.cnsVal;
            printf(", ");

            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;
        }

        case IF_RWR_RRD_MRD_RRD:
        {
            printf("%s, ", emitRegName(id->idReg1(), attr));
            printf("%s, ", emitRegName(id->idReg2(), attr));

            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);

            emitGetInsDcmCns(id, &cnsVal);
            val = (cnsVal.cnsVal >> 4) + XMMBASE;
            printf(", %s", emitRegName((regNumber)val, attr));
            break;
        }

        case IF_RWR_MRD_OFF:

            printf("%s, %s", emitRegName(id->idReg1(), attr), "offset");
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            break;

        case IF_MRD_RRD:
        case IF_MWR_RRD:
        case IF_MRW_RRD:

            printf("%s", sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            printf(", %s", emitRegName(id->idReg1(), attr));
            break;

        case IF_MRD_CNS:
        case IF_MWR_CNS:
        case IF_MRW_CNS:
        case IF_MRW_SHF:

            printf("%s", sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            emitGetInsDcmCns(id, &cnsVal);
            val = cnsVal.cnsVal;
#ifdef TARGET_AMD64
            // no 8-byte immediates allowed here!
            assert((val >= (ssize_t)0xFFFFFFFF80000000LL) && (val <= 0x000000007FFFFFFFLL));
#endif
            if (cnsVal.cnsReloc)
            {
                emitDispReloc(val);
            }
            else if (id->idInsFmt() == IF_MRW_SHF)
            {
                emitDispShift(ins, (BYTE)val);
            }
            else
            {
                printf(", ");
                goto PRINT_CONSTANT;
            }
            break;

        case IF_MRD:
        case IF_MWR:
        case IF_MRW:

            printf("%s", sstr);
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            emitDispShift(ins);
            break;

        case IF_MRD_OFF:

            printf("offset ");
            offs = emitGetInsDsp(id);
            emitDispClsVar(id->idAddr()->iiaFieldHnd, offs, ID_INFO_DSP_RELOC);
            break;

        case IF_RRD_CNS:
        case IF_RWR_CNS:
        case IF_RRW_CNS:
            printf("%s, ", emitRegName(id->idReg1(), attr));
            val = emitGetInsSC(id);
            if (id->idIsCnsReloc())
            {
                emitDispReloc(val);
            }
            else
            {
                goto PRINT_CONSTANT;
            }
            break;

        case IF_LABEL:
        case IF_RWR_LABEL:
        case IF_SWR_LABEL:

            if (ins == INS_lea)
            {
                printf("%s, ", emitRegName(id->idReg1(), attr));
            }
            else if (ins == INS_mov)
            {
                /* mov   dword ptr [frame.callSiteReturnAddress], label */
                assert(id->idInsFmt() == IF_SWR_LABEL);
                instrDescLbl* idlbl = (instrDescLbl*)id;

                emitDispFrameRef(idlbl->dstLclVar.lvaVarNum(), idlbl->dstLclVar.lvaOffset(), 0, asmfm);

                printf(", ");
            }

            if (((instrDescJmp*)id)->idjShort)
            {
                printf("SHORT ");
            }

            if (id->idIsBound())
            {
                if (id->idAddr()->iiaHasInstrCount())
                {
                    printf("%3d instr", id->idAddr()->iiaGetInstrCount());
                }
                else
                {
                    emitPrintLabel(id->idAddr()->iiaIGlabel);
                }
            }
            else
            {
                printf("L_M%03u_" FMT_BB, emitComp->compMethodID, id->idAddr()->iiaBBlabel->bbNum);
            }
            break;

        case IF_METHOD:
        case IF_METHPTR:
            methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);

            if (id->idInsFmt() == IF_METHPTR)
            {
                printf("[");
            }

            printf("%s", methodName);

            if (id->idInsFmt() == IF_METHPTR)
            {
                printf("]");
            }

            break;

        case IF_NONE:
#if FEATURE_LOOP_ALIGN
            if (ins == INS_align)
            {
                instrDescAlign* alignInstrId = (instrDescAlign*)id;
                printf("[%d bytes", alignInstrId->idCodeSize());
                // targetIG is only set for 1st of the series of align instruction
                if ((alignInstrId->idaLoopHeadPredIG != nullptr) && (alignInstrId->loopHeadIG() != nullptr))
                {
                    printf(" for IG%02u", alignInstrId->loopHeadIG()->igNum);
                }
                printf("]");
            }
#endif
            break;

        default:
            printf("unexpected format %s", emitIfName(id->idInsFmt()));
            assert(!"unexpectedFormat");
            break;
    }

    if (sz != 0 && sz != id->idCodeSize() && (!asmfm || emitComp->verbose))
    {
        // Code size in the instrDesc is different from the actual code size we've been given!
        printf(" (ECS:%d, ACS:%d)", id->idCodeSize(), sz);
    }

    printf("\n");
}

/*****************************************************************************/
#endif

/*****************************************************************************
 *
 *  Output nBytes bytes of NOP instructions
 */

static BYTE* emitOutputNOP(BYTE* dstRW, size_t nBytes)
{
    assert(nBytes <= 15);

#ifndef TARGET_AMD64
    // TODO-X86-CQ: when VIA C3 CPU's are out of circulation, switch to the
    // more efficient real NOP: 0x0F 0x1F +modR/M
    // Also can't use AMD recommended, multiple size prefixes (i.e. 0x66 0x66 0x90 for 3 byte NOP)
    // because debugger and msdis don't like it, so maybe VIA doesn't either
    // So instead just stick to repeating single byte nops

    switch (nBytes)
    {
        case 15:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 14:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 13:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 12:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 11:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 10:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 9:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 8:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 7:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 6:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 5:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 4:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 3:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 2:
            *dstRW++ = 0x90;
            FALLTHROUGH;
        case 1:
            *dstRW++ = 0x90;
            break;
        case 0:
            break;
    }
#else  // TARGET_AMD64
    switch (nBytes)
    {
        case 2:
            *dstRW++ = 0x66;
            FALLTHROUGH;
        case 1:
            *dstRW++ = 0x90;
            break;
        case 0:
            break;
        case 3:
            *dstRW++ = 0x0F;
            *dstRW++ = 0x1F;
            *dstRW++ = 0x00;
            break;
        case 4:
            *dstRW++ = 0x0F;
            *dstRW++ = 0x1F;
            *dstRW++ = 0x40;
            *dstRW++ = 0x00;
            break;
        case 6:
            *dstRW++ = 0x66;
            FALLTHROUGH;
        case 5:
            *dstRW++ = 0x0F;
            *dstRW++ = 0x1F;
            *dstRW++ = 0x44;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            break;
        case 7:
            *dstRW++ = 0x0F;
            *dstRW++ = 0x1F;
            *dstRW++ = 0x80;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            break;
        case 15:
            // More than 3 prefixes is slower than just 2 NOPs
            dstRW = emitOutputNOP(emitOutputNOP(dstRW, 7), 8);
            break;
        case 14:
            // More than 3 prefixes is slower than just 2 NOPs
            dstRW = emitOutputNOP(emitOutputNOP(dstRW, 7), 7);
            break;
        case 13:
            // More than 3 prefixes is slower than just 2 NOPs
            dstRW = emitOutputNOP(emitOutputNOP(dstRW, 5), 8);
            break;
        case 12:
            // More than 3 prefixes is slower than just 2 NOPs
            dstRW = emitOutputNOP(emitOutputNOP(dstRW, 4), 8);
            break;
        case 11:
            *dstRW++ = 0x66;
            FALLTHROUGH;
        case 10:
            *dstRW++ = 0x66;
            FALLTHROUGH;
        case 9:
            *dstRW++ = 0x66;
            FALLTHROUGH;
        case 8:
            *dstRW++ = 0x0F;
            *dstRW++ = 0x1F;
            *dstRW++ = 0x84;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            *dstRW++ = 0x00;
            break;
    }
#endif // TARGET_AMD64

    return dstRW;
}

//--------------------------------------------------------------------
// emitOutputAlign: Outputs NOP to align the loop
//
// Arguments:
//   ig - Current instruction group
//   id - align instruction that holds amount of padding (NOPs) to add
//   dst - Destination buffer
//
// Return Value:
//   None.
//
// Notes:
//   Amount of padding needed to align the loop is already calculated. This
//   method extracts that information and inserts suitable NOP instructions.
//
BYTE* emitter::emitOutputAlign(insGroup* ig, instrDesc* id, BYTE* dst)
{
    instrDescAlign* alignInstr = (instrDescAlign*)id;

#ifdef DEBUG
    // For cases where 'align' was placed behing a 'jmp' in an IG that does not
    // immediately preced the loop IG, we do not know in advance the offset of
    // IG having loop. For such cases, skip the padding calculation validation.
    bool validatePadding = !alignInstr->isPlacedAfterJmp;
#endif

    // Candidate for loop alignment
    assert(codeGen->ShouldAlignLoops());
    assert(ig->endsWithAlignInstr());

    unsigned paddingToAdd = id->idCodeSize();

    // Either things are already aligned or align them here.
    assert(!validatePadding || (paddingToAdd == 0) ||
           (((size_t)dst & (emitComp->opts.compJitAlignLoopBoundary - 1)) != 0));

    // Padding amount should not exceed the alignment boundary
    assert(0 <= paddingToAdd && paddingToAdd < emitComp->opts.compJitAlignLoopBoundary);

#ifdef DEBUG
    if (validatePadding)
    {
        unsigned paddingNeeded =
            emitCalculatePaddingForLoopAlignment(((instrDescAlign*)id)->idaIG->igNext, (size_t)dst, true);

        // For non-adaptive, padding size is spread in multiple instructions, so don't bother checking
        if (emitComp->opts.compJitAlignLoopAdaptive)
        {
            assert(paddingToAdd == paddingNeeded);
        }
    }

    emitComp->loopsAligned++;
#endif

    BYTE* dstRW = dst + writeableOffset;

#ifdef DEBUG
    // Under STRESS_EMITTER, if this is the 'align' before the 'jmp' instruction,
    // then add "int3" instruction. Since int3 takes 1 byte, we would only add
    // it if paddingToAdd >= 1 byte.

    if (emitComp->compStressCompile(Compiler::STRESS_EMITTER, 50) && alignInstr->isPlacedAfterJmp && paddingToAdd >= 1)
    {
        size_t int3Code = insCodeMR(INS_BREAKPOINT);
        // There is no good way to squeeze in "int3" as well as display it
        // in the disassembly because there is no corresponding instrDesc for
        // it. As such, leave it as is, the "0xCC" bytecode will be seen next
        // to the nop instruction in disasm.
        // e.g. CC                   align    [1 bytes for IG29]
        //
        // if (emitComp->opts.disAsm)
        //{
        //    emitDispInsAddr(dstRW);

        //    emitDispInsOffs(0, false);

        //    printf("                      %-9s  ; stress-mode injected interrupt\n", "int3");
        //}
        dstRW += emitOutputByte(dstRW, int3Code);
        paddingToAdd -= 1;
    }
#endif

    dstRW = emitOutputNOP(dstRW, paddingToAdd);
    return dstRW - writeableOffset;
}

/*****************************************************************************
 *
 *  Output an instruction involving an address mode.
 */

BYTE* emitter::emitOutputAM(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc)
{
    regNumber reg;
    regNumber rgx;
    ssize_t   dsp;
    bool      dspInByte;
    bool      dspIsZero;
    bool      isMoffset = false;

    instruction ins  = id->idIns();
    emitAttr    size = id->idOpSize();
    size_t      opsz = EA_SIZE_IN_BYTES(size);

    // Get the base/index registers
    reg = id->idAddr()->iiaAddrMode.amBaseReg;
    rgx = id->idAddr()->iiaAddrMode.amIndxReg;

    // For INS_call the instruction size is actually the return value size
    if ((ins == INS_call) || (ins == INS_tail_i_jmp))
    {
        if (ins == INS_tail_i_jmp)
        {
            // tail call with addressing mode (or through register) needs rex.w
            // prefix to be recognized by unwinder as part of epilog.
            code = AddRexWPrefix(ins, code);
        }

        // Special case: call via a register
        if (id->idIsCallRegPtr())
        {
            code = insEncodeMRreg(ins, reg, EA_PTRSIZE, code);
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);
            dst += emitOutputWord(dst, code);
            goto DONE;
        }

        // The displacement field is in an unusual place for calls
        dsp = emitGetInsCIdisp(id);

#ifdef TARGET_AMD64

        // Compute the REX prefix if it exists
        if (IsExtendedReg(reg, EA_PTRSIZE))
        {
            insEncodeReg012(ins, reg, EA_PTRSIZE, &code);
            // TODO-Cleanup: stop casting RegEncoding() back to a regNumber.
            reg = (regNumber)RegEncoding(reg);
        }

        if (IsExtendedReg(rgx, EA_PTRSIZE))
        {
            insEncodeRegSIB(ins, rgx, &code);
            // TODO-Cleanup: stop casting RegEncoding() back to a regNumber.
            rgx = (regNumber)RegEncoding(rgx);
        }

        // And emit the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

#endif // TARGET_AMD64

        goto GOT_DSP;
    }

    // `addc` is used for two kinds if instructions
    // 1. ins like ADD that can have reg/mem and const versions both and const version needs to modify the opcode for
    // large constant operand (e.g., imm32)
    // 2. certain SSE/AVX ins have const operand as control bits that is always 1-Byte (imm8) even if `size` > 1-Byte
    if (addc && (size > EA_1BYTE))
    {
        ssize_t cval = addc->cnsVal;

        // Does the constant fit in a byte?
        // SSE/AVX do not need to modify opcode
        if ((signed char)cval == cval && addc->cnsReloc == false && ins != INS_mov && ins != INS_test)
        {
            if (id->idInsFmt() != IF_ARW_SHF && !IsSSEOrAVXInstruction(ins))
            {
                code |= 2;
            }

            opsz = 1;
        }
    }
#ifdef TARGET_X86
    else
    {
        // Special case: "mov eax, [addr]" and "mov [addr], eax"
        // Amd64: this is one case where addr can be 64-bit in size.  This is
        // currently unused or not enabled on amd64 as it always uses RIP
        // relative addressing which results in smaller instruction size.
        if ((ins == INS_mov) && (id->idReg1() == REG_EAX) && (reg == REG_NA) && (rgx == REG_NA))
        {
            switch (id->idInsFmt())
            {
                case IF_RWR_ARD:

                    assert(code == (insCodeRM(ins) | (insEncodeReg345(ins, REG_EAX, EA_PTRSIZE, NULL) << 8)));

                    code &= ~((code_t)0xFFFFFFFF);
                    code |= 0xA0;
                    isMoffset = true;
                    break;

                case IF_AWR_RRD:

                    assert(code == (insCodeMR(ins) | (insEncodeReg345(ins, REG_EAX, EA_PTRSIZE, NULL) << 8)));

                    code &= ~((code_t)0xFFFFFFFF);
                    code |= 0xA2;
                    isMoffset = true;
                    break;

                default:
                    break;
            }
        }
    }
#endif // TARGET_X86

    // Emit VEX prefix if required
    // There are some callers who already add VEX prefix and call this routine.
    // Therefore, add VEX prefix is one is not already present.
    code = AddVexPrefixIfNeededAndNotPresent(ins, code, size);

    // For this format, moves do not support a third operand, so we only need to handle the binary ops.
    if (TakesVexPrefix(ins))
    {
        if (IsDstDstSrcAVXInstruction(ins))
        {
            regNumber src1 = REG_NA;

            switch (id->idInsFmt())
            {
                case IF_RWR_RRD_ARD:
                case IF_RWR_ARD_RRD:
                case IF_RWR_RRD_ARD_CNS:
                case IF_RWR_RRD_ARD_RRD:
                {
                    src1 = id->idReg2();
                    break;
                }

                default:
                {
                    src1 = id->idReg1();
                    break;
                }
            }

            // encode source operand reg in 'vvvv' bits in 1's complement form
            code = insEncodeReg3456(ins, src1, size, code);
        }
        else if (IsDstSrcSrcAVXInstruction(ins))
        {
            code = insEncodeReg3456(ins, id->idReg2(), size, code);
        }
    }

    // Emit the REX prefix if required
    if (TakesRexWPrefix(ins, size))
    {
        code = AddRexWPrefix(ins, code);
    }

    if (IsExtendedReg(reg, EA_PTRSIZE))
    {
        insEncodeReg012(ins, reg, EA_PTRSIZE, &code);
        // TODO-Cleanup: stop casting RegEncoding() back to a regNumber.
        reg = (regNumber)RegEncoding(reg);
    }

    if (IsExtendedReg(rgx, EA_PTRSIZE))
    {
        insEncodeRegSIB(ins, rgx, &code);
        // TODO-Cleanup: stop casting RegEncoding() back to a regNumber.
        rgx = (regNumber)RegEncoding(rgx);
    }

    // Special case emitting AVX instructions
    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
    {
        if ((ins == INS_crc32) && (size > EA_1BYTE))
        {
            code |= 0x0100;

            if (size == EA_2BYTE)
            {
                dst += emitOutputByte(dst, 0x66);
            }
        }

        regNumber reg345 = REG_NA;
        if (IsBMIInstruction(ins))
        {
            reg345 = getBmiRegNumber(ins);
        }
        if (reg345 == REG_NA)
        {
            switch (id->idInsFmt())
            {
                case IF_AWR_RRD_RRD:
                {
                    reg345 = id->idReg2();
                    break;
                }

                default:
                {
                    reg345 = id->idReg1();
                    break;
                }
            }
        }
        unsigned regcode = insEncodeReg345(ins, reg345, size, &code);

        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        if (UseVEXEncoding() && (ins != INS_crc32))
        {
            // Emit last opcode byte
            // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
            assert((code & 0xFF) == 0);
            dst += emitOutputByte(dst, (code >> 8) & 0xFF);
        }
        else
        {
            dst += emitOutputWord(dst, code >> 16);
            dst += emitOutputWord(dst, code & 0xFFFF);
        }

        code = regcode;
    }
    // Is this a 'big' opcode?
    else if (code & 0xFF000000)
    {
        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Output the highest word of the opcode
        // We need to check again as in case of AVX instructions leading opcode bytes are stripped off
        // and encoded as part of VEX prefix.
        if (code & 0xFF000000)
        {
            dst += emitOutputWord(dst, code >> 16);
            code &= 0x0000FFFF;
        }
    }
    else if (code & 0x00FF0000)
    {
        // BT supports 16 bit operands and this code doesn't handle the necessary 66 prefix.
        assert(ins != INS_bt);

        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Output the highest byte of the opcode
        if (code & 0x00FF0000)
        {
            dst += emitOutputByte(dst, code >> 16);
            code &= 0x0000FFFF;
        }

        // Use the large version if this is not a byte. This trick will not
        // work in case of SSE2 and AVX instructions.
        if ((size != EA_1BYTE) && HasRegularWideForm(ins))
        {
            code |= 0x1;
        }
    }
    else if (CodeGen::instIsFP(ins))
    {
        assert(size == EA_4BYTE || size == EA_8BYTE);
        if (size == EA_8BYTE)
        {
            code += 4;
        }
    }
    else if (!IsSSEInstruction(ins) && !IsAVXInstruction(ins))
    {
        /* Is the operand size larger than a byte? */

        switch (size)
        {
            case EA_1BYTE:
                break;

            case EA_2BYTE:

                /* Output a size prefix for a 16-bit operand */

                dst += emitOutputByte(dst, 0x66);

                FALLTHROUGH;

            case EA_4BYTE:
#ifdef TARGET_AMD64
            case EA_8BYTE:
#endif

                /* Set the 'w' bit to get the large version */

                code |= 0x1;
                break;

#ifdef TARGET_X86
            case EA_8BYTE:

                /* Double operand - set the appropriate bit */

                code |= 0x04;
                break;

#endif // TARGET_X86

            default:
                NO_WAY("unexpected size");
                break;
        }
    }

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    // Get the displacement value
    dsp = emitGetInsAmdAny(id);

GOT_DSP:

    dspInByte = ((signed char)dsp == (ssize_t)dsp);
    dspIsZero = (dsp == 0);

    if (id->idIsDspReloc())
    {
        dspInByte = false; // relocs can't be placed in a byte
    }

    if (isMoffset)
    {
#ifdef TARGET_AMD64
        // This code path should never be hit on amd64 since it always uses RIP relative addressing.
        // In future if ever there is a need to enable this special case, also enable the logic
        // that sets isMoffset to true on amd64.
        unreached();
#else // TARGET_X86

        dst += emitOutputByte(dst, code);
        dst += emitOutputSizeT(dst, dsp);

        if (id->idIsDspReloc())
        {
            emitRecordRelocation((void*)(dst - TARGET_POINTER_SIZE), (void*)dsp, IMAGE_REL_BASED_MOFFSET);
        }

#endif // TARGET_X86
    }
    // Is there a [scaled] index component?
    else if (rgx == REG_NA)
    {
        // The address is of the form "[reg+disp]"
        switch (reg)
        {
            case REG_NA:
            {
                if (id->idIsDspReloc())
                {
                    INT32 addlDelta = 0;

                    // The address is of the form "[disp]"
                    // On x86 - disp is relative to zero
                    // On Amd64 - disp is relative to RIP
                    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                    {
                        dst += emitOutputByte(dst, code | 0x05);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x0500);
                    }

                    if (addc)
                    {
                        // It is of the form "ins [disp], imm" or "ins reg, [disp], imm"
                        // For emitting relocation, we also need to take into account of the
                        // additional bytes of code emitted for immed val.

                        ssize_t cval = addc->cnsVal;

#ifdef TARGET_AMD64
                        // all these opcodes only take a sign-extended 4-byte immediate
                        noway_assert(opsz < 8 || ((int)cval == cval && !addc->cnsReloc));
#else  // TARGET_X86
                        noway_assert(opsz <= 4);
#endif // TARGET_X86

                        switch (opsz)
                        {
                            case 0:
                            case 4:
                            case 8:
                                addlDelta = -4;
                                break;
                            case 2:
                                addlDelta = -2;
                                break;
                            case 1:
                                addlDelta = -1;
                                break;

                            default:
                                assert(!"unexpected operand size");
                                unreached();
                        }
                    }

#ifdef TARGET_AMD64
                    // We emit zero on Amd64, to avoid the assert in emitOutputLong()
                    dst += emitOutputLong(dst, 0);
#else
                    dst += emitOutputLong(dst, dsp);
#endif
                    emitRecordRelocationWithAddlDelta((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_DISP32,
                                                      addlDelta);
                }
                else
                {
#ifdef TARGET_X86
                    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                    {
                        dst += emitOutputByte(dst, code | 0x05);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x0500);
                    }
#else  // TARGET_AMD64
                    // Amd64: addr fits within 32-bits and can be encoded as a displacement relative to zero.
                    // This addr mode should never be used while generating relocatable ngen code nor if
                    // the addr can be encoded as pc-relative address.
                    noway_assert(!emitComp->opts.compReloc);
                    noway_assert(codeGen->genAddrRelocTypeHint((size_t)dsp) != IMAGE_REL_BASED_REL32);
                    noway_assert((int)dsp == dsp);

                    // This requires, specifying a SIB byte after ModRM byte.
                    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                    {
                        dst += emitOutputByte(dst, code | 0x04);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x0400);
                    }
                    dst += emitOutputByte(dst, 0x25);
#endif // TARGET_AMD64
                    dst += emitOutputLong(dst, dsp);
                }
                break;
            }

            case REG_EBP:
            {
                if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                {
                    // Does the offset fit in a byte?
                    if (dspInByte)
                    {
                        dst += emitOutputByte(dst, code | 0x45);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputByte(dst, code | 0x85);
                        dst += emitOutputLong(dst, dsp);

                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
                else
                {
                    // Does the offset fit in a byte?
                    if (dspInByte)
                    {
                        dst += emitOutputWord(dst, code | 0x4500);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x8500);
                        dst += emitOutputLong(dst, dsp);

                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
                break;
            }

            case REG_ESP:
            {
                if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                {
                    // Is the offset 0 or does it at least fit in a byte?
                    if (dspIsZero)
                    {
                        dst += emitOutputByte(dst, code | 0x04);
                        dst += emitOutputByte(dst, 0x24);
                    }
                    else if (dspInByte)
                    {
                        dst += emitOutputByte(dst, code | 0x44);
                        dst += emitOutputByte(dst, 0x24);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputByte(dst, code | 0x84);
                        dst += emitOutputByte(dst, 0x24);
                        dst += emitOutputLong(dst, dsp);
                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
                else
                {
                    // Is the offset 0 or does it at least fit in a byte?
                    if (dspIsZero)
                    {
                        dst += emitOutputWord(dst, code | 0x0400);
                        dst += emitOutputByte(dst, 0x24);
                    }
                    else if (dspInByte)
                    {
                        dst += emitOutputWord(dst, code | 0x4400);
                        dst += emitOutputByte(dst, 0x24);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x8400);
                        dst += emitOutputByte(dst, 0x24);
                        dst += emitOutputLong(dst, dsp);
                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
                break;
            }

            default:
            {
                if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                {
                    // Put the register in the opcode
                    code |= insEncodeReg012(ins, reg, EA_PTRSIZE, nullptr);

                    // Is there a displacement?
                    if (dspIsZero)
                    {
                        // This is simply "[reg]"
                        dst += emitOutputByte(dst, code);
                    }
                    else
                    {
                        // This is [reg + dsp]" -- does the offset fit in a byte?
                        if (dspInByte)
                        {
                            dst += emitOutputByte(dst, code | 0x40);
                            dst += emitOutputByte(dst, dsp);
                        }
                        else
                        {
                            dst += emitOutputByte(dst, code | 0x80);
                            dst += emitOutputLong(dst, dsp);
                            if (id->idIsDspReloc())
                            {
                                emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                            }
                        }
                    }
                }
                else
                {
                    // Put the register in the opcode
                    code |= insEncodeReg012(ins, reg, EA_PTRSIZE, nullptr) << 8;

                    // Is there a displacement?
                    if (dspIsZero)
                    {
                        // This is simply "[reg]"
                        dst += emitOutputWord(dst, code);
                    }
                    else
                    {
                        // This is [reg + dsp]" -- does the offset fit in a byte?
                        if (dspInByte)
                        {
                            dst += emitOutputWord(dst, code | 0x4000);
                            dst += emitOutputByte(dst, dsp);
                        }
                        else
                        {
                            dst += emitOutputWord(dst, code | 0x8000);
                            dst += emitOutputLong(dst, dsp);
                            if (id->idIsDspReloc())
                            {
                                emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                            }
                        }
                    }
                }

                break;
            }
        }
    }
    else
    {
        unsigned regByte;

        // We have a scaled index operand
        unsigned mul = emitDecodeScale(id->idAddr()->iiaAddrMode.amScale);

        // Is the index operand scaled?
        if (mul > 1)
        {
            // Is there a base register?
            if (reg != REG_NA)
            {
                // The address is "[reg + {2/4/8} * rgx + icon]"
                regByte = insEncodeReg012(ins, reg, EA_PTRSIZE, nullptr) |
                          insEncodeReg345(ins, rgx, EA_PTRSIZE, nullptr) | insSSval(mul);

                if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                {
                    // Emit [ebp + {2/4/8} * rgz] as [ebp + {2/4/8} * rgx + 0]
                    if (dspIsZero && reg != REG_EBP)
                    {
                        // The address is "[reg + {2/4/8} * rgx]"
                        dst += emitOutputByte(dst, code | 0x04);
                        dst += emitOutputByte(dst, regByte);
                    }
                    else
                    {
                        // The address is "[reg + {2/4/8} * rgx + disp]"
                        if (dspInByte)
                        {
                            dst += emitOutputByte(dst, code | 0x44);
                            dst += emitOutputByte(dst, regByte);
                            dst += emitOutputByte(dst, dsp);
                        }
                        else
                        {
                            dst += emitOutputByte(dst, code | 0x84);
                            dst += emitOutputByte(dst, regByte);
                            dst += emitOutputLong(dst, dsp);
                            if (id->idIsDspReloc())
                            {
                                emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                            }
                        }
                    }
                }
                else
                {
                    // Emit [ebp + {2/4/8} * rgz] as [ebp + {2/4/8} * rgx + 0]
                    if (dspIsZero && reg != REG_EBP)
                    {
                        // The address is "[reg + {2/4/8} * rgx]"
                        dst += emitOutputWord(dst, code | 0x0400);
                        dst += emitOutputByte(dst, regByte);
                    }
                    else
                    {
                        // The address is "[reg + {2/4/8} * rgx + disp]"
                        if (dspInByte)
                        {
                            dst += emitOutputWord(dst, code | 0x4400);
                            dst += emitOutputByte(dst, regByte);
                            dst += emitOutputByte(dst, dsp);
                        }
                        else
                        {
                            dst += emitOutputWord(dst, code | 0x8400);
                            dst += emitOutputByte(dst, regByte);
                            dst += emitOutputLong(dst, dsp);
                            if (id->idIsDspReloc())
                            {
                                emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                            }
                        }
                    }
                }
            }
            else
            {
                // The address is "[{2/4/8} * rgx + icon]"
                regByte = insEncodeReg012(ins, REG_EBP, EA_PTRSIZE, nullptr) |
                          insEncodeReg345(ins, rgx, EA_PTRSIZE, nullptr) | insSSval(mul);

                if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
                {
                    dst += emitOutputByte(dst, code | 0x04);
                }
                else
                {
                    dst += emitOutputWord(dst, code | 0x0400);
                }

                dst += emitOutputByte(dst, regByte);

                // Special case: jump through a jump table
                if (ins == INS_i_jmp)
                {
                    dsp += (size_t)emitConsBlock;
                }

                dst += emitOutputLong(dst, dsp);
                if (id->idIsDspReloc())
                {
                    emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                }
            }
        }
        else
        {
            // The address is "[reg+rgx+dsp]"
            regByte = insEncodeReg012(ins, reg, EA_PTRSIZE, nullptr) | insEncodeReg345(ins, rgx, EA_PTRSIZE, nullptr);

            if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
            {
                if (dspIsZero && reg != REG_EBP)
                {
                    // This is [reg+rgx]"
                    dst += emitOutputByte(dst, code | 0x04);
                    dst += emitOutputByte(dst, regByte);
                }
                else
                {
                    // This is [reg+rgx+dsp]" -- does the offset fit in a byte?
                    if (dspInByte)
                    {
                        dst += emitOutputByte(dst, code | 0x44);
                        dst += emitOutputByte(dst, regByte);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputByte(dst, code | 0x84);
                        dst += emitOutputByte(dst, regByte);
                        dst += emitOutputLong(dst, dsp);
                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
            }
            else
            {
                if (dspIsZero && reg != REG_EBP)
                {
                    // This is [reg+rgx]"
                    dst += emitOutputWord(dst, code | 0x0400);
                    dst += emitOutputByte(dst, regByte);
                }
                else
                {
                    // This is [reg+rgx+dsp]" -- does the offset fit in a byte?
                    if (dspInByte)
                    {
                        dst += emitOutputWord(dst, code | 0x4400);
                        dst += emitOutputByte(dst, regByte);
                        dst += emitOutputByte(dst, dsp);
                    }
                    else
                    {
                        dst += emitOutputWord(dst, code | 0x8400);
                        dst += emitOutputByte(dst, regByte);
                        dst += emitOutputLong(dst, dsp);
                        if (id->idIsDspReloc())
                        {
                            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)dsp, IMAGE_REL_BASED_HIGHLOW);
                        }
                    }
                }
            }
        }
    }

    // Now generate the constant value, if present
    if (addc)
    {
        ssize_t cval = addc->cnsVal;

#ifdef TARGET_AMD64
        // all these opcodes only take a sign-extended 4-byte immediate
        noway_assert(opsz < 8 || ((int)cval == cval && !addc->cnsReloc));
#endif

        switch (opsz)
        {
            case 0:
            case 4:
            case 8:
                dst += emitOutputLong(dst, cval);
                break;
            case 2:
                dst += emitOutputWord(dst, cval);
                break;
            case 1:
                dst += emitOutputByte(dst, cval);
                break;

            default:
                assert(!"unexpected operand size");
        }

        if (addc->cnsReloc)
        {
            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)(size_t)cval, IMAGE_REL_BASED_HIGHLOW);
            assert(opsz == 4);
        }
    }

DONE:

    // Does this instruction operate on a GC ref value?
    if (id->idGCref())
    {
        switch (id->idInsFmt())
        {
            case IF_ARD:
            case IF_AWR:
            case IF_ARW:
                break;

            case IF_RRD_ARD:
                break;

            case IF_RWR_ARD:
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
                break;

            case IF_RRW_ARD:
                // Mark the destination register as holding a GCT_BYREF
                assert(id->idGCref() == GCT_BYREF && (ins == INS_add || ins == INS_sub));
                emitGCregLiveUpd(GCT_BYREF, id->idReg1(), dst);
                break;

            case IF_ARD_RRD:
            case IF_AWR_RRD:
                break;

            case IF_AWR_RRD_RRD:
                break;

            case IF_ARD_CNS:
            case IF_AWR_CNS:
                break;

            case IF_ARW_RRD:
            case IF_ARW_CNS:
                assert(id->idGCref() == GCT_BYREF && (ins == INS_add || ins == INS_sub));
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }

        // mul can never produce a GC ref
        assert(!instrIs3opImul(ins));
        assert(ins != INS_mulEAX && ins != INS_imulEAX);
    }
    else
    {
        if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
        {
            switch (id->idInsFmt())
            {
                case IF_RWR_ARD:
                case IF_RRW_ARD:
                case IF_RWR_RRD_ARD:
                    emitGCregDeadUpd(id->idReg1(), dst);
                    break;
                default:
                    break;
            }

            if (ins == INS_mulEAX || ins == INS_imulEAX)
            {
                emitGCregDeadUpd(REG_EAX, dst);
                emitGCregDeadUpd(REG_EDX, dst);
            }

            // For the three operand imul instruction the target register
            // is encoded in the opcode

            if (instrIs3opImul(ins))
            {
                regNumber tgtReg = inst3opImulReg(ins);
                emitGCregDeadUpd(tgtReg, dst);
            }
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction involving a stack frame value.
 */

BYTE* emitter::emitOutputSV(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc)
{
    int  adr;
    int  dsp;
    bool EBPbased;
    bool dspInByte;
    bool dspIsZero;

    instruction ins  = id->idIns();
    emitAttr    size = id->idOpSize();
    size_t      opsz = EA_SIZE_IN_BYTES(size);

    assert(ins != INS_imul || id->idReg1() == REG_EAX || size == EA_4BYTE || size == EA_8BYTE);

    // `addc` is used for two kinds if instructions
    // 1. ins like ADD that can have reg/mem and const versions both and const version needs to modify the opcode for
    // large constant operand (e.g., imm32)
    // 2. certain SSE/AVX ins have const operand as control bits that is always 1-Byte (imm8) even if `size` > 1-Byte
    if (addc && (size > EA_1BYTE))
    {
        ssize_t cval = addc->cnsVal;

        // Does the constant fit in a byte?
        // SSE/AVX do not need to modify opcode
        if ((signed char)cval == cval && addc->cnsReloc == false && ins != INS_mov && ins != INS_test)
        {
            if ((id->idInsFmt() != IF_SRW_SHF) && (id->idInsFmt() != IF_RRW_SRD_CNS) &&
                (id->idInsFmt() != IF_RWR_RRD_SRD_CNS) && !IsSSEOrAVXInstruction(ins))
            {
                code |= 2;
            }

            opsz = 1;
        }
    }

    // Add VEX prefix if required.
    // There are some callers who already add VEX prefix and call this routine.
    // Therefore, add VEX prefix is one is not already present.
    code = AddVexPrefixIfNeededAndNotPresent(ins, code, size);

    // Compute the REX prefix
    if (TakesRexWPrefix(ins, size))
    {
        code = AddRexWPrefix(ins, code);
    }

    // Special case emitting AVX instructions
    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
    {
        if ((ins == INS_crc32) && (size > EA_1BYTE))
        {
            code |= 0x0100;

            if (size == EA_2BYTE)
            {
                dst += emitOutputByte(dst, 0x66);
            }
        }

        regNumber reg345 = REG_NA;
        if (IsBMIInstruction(ins))
        {
            reg345 = getBmiRegNumber(ins);
        }
        if (reg345 == REG_NA)
        {
            reg345 = id->idReg1();
        }
        else
        {
            code = insEncodeReg3456(ins, id->idReg1(), size, code);
        }
        unsigned regcode = insEncodeReg345(ins, reg345, size, &code);

        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        if (UseVEXEncoding() && (ins != INS_crc32))
        {
            // Emit last opcode byte
            // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
            assert((code & 0xFF) == 0);
            dst += emitOutputByte(dst, (code >> 8) & 0xFF);
        }
        else
        {
            dst += emitOutputWord(dst, code >> 16);
            dst += emitOutputWord(dst, code & 0xFFFF);
        }

        code = regcode;
    }
    // Is this a 'big' opcode?
    else if (code & 0xFF000000)
    {
        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Output the highest word of the opcode
        // We need to check again because in case of AVX instructions the leading
        // escape byte(s) (e.g. 0x0F) will be encoded as part of VEX prefix.
        if (code & 0xFF000000)
        {
            dst += emitOutputWord(dst, code >> 16);
            code &= 0x0000FFFF;
        }
    }
    else if (code & 0x00FF0000)
    {
        // BT supports 16 bit operands and this code doesn't add the necessary 66 prefix.
        assert(ins != INS_bt);

        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Output the highest byte of the opcode.
        // We need to check again because in case of AVX instructions the leading
        // escape byte(s) (e.g. 0x0F) will be encoded as part of VEX prefix.
        if (code & 0x00FF0000)
        {
            dst += emitOutputByte(dst, code >> 16);
            code &= 0x0000FFFF;
        }

        // Use the large version if this is not a byte
        if ((size != EA_1BYTE) && HasRegularWideForm(ins))
        {
            code |= 0x1;
        }
    }
    else if (CodeGen::instIsFP(ins))
    {
        assert(size == EA_4BYTE || size == EA_8BYTE);

        if (size == EA_8BYTE)
        {
            code += 4;
        }
    }
    else if (!IsSSEInstruction(ins) && !IsAVXInstruction(ins))
    {
        // Is the operand size larger than a byte?
        switch (size)
        {
            case EA_1BYTE:
                break;

            case EA_2BYTE:
                // Output a size prefix for a 16-bit operand
                dst += emitOutputByte(dst, 0x66);
                FALLTHROUGH;

            case EA_4BYTE:
#ifdef TARGET_AMD64
            case EA_8BYTE:
#endif // TARGET_AMD64

                /* Set the 'w' size bit to indicate 32-bit operation
                 * Note that incrementing "code" for INS_call (0xFF) would
                 * overflow, whereas setting the lower bit to 1 just works out
                 */

                code |= 0x01;
                break;

#ifdef TARGET_X86
            case EA_8BYTE:

                // Double operand - set the appropriate bit.
                // I don't know what a legitimate reason to end up in this case would be
                // considering that FP is taken care of above...
                // what is an instruction that takes a double which is not covered by the
                // above instIsFP? Of the list in instrsxarch, only INS_fprem
                code |= 0x04;
                NO_WAY("bad 8 byte op");
                break;
#endif // TARGET_X86

            default:
                NO_WAY("unexpected size");
                break;
        }
    }

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    // Figure out the variable's frame position
    int varNum = id->idAddr()->iiaLclVar.lvaVarNum();

    adr = emitComp->lvaFrameAddress(varNum, &EBPbased);
    dsp = adr + id->idAddr()->iiaLclVar.lvaOffset();

    dspInByte = ((signed char)dsp == (int)dsp);
    dspIsZero = (dsp == 0);

    // for stack varaibles the dsp should never be a reloc
    assert(id->idIsDspReloc() == 0);

    if (EBPbased)
    {
        // EBP-based variable: does the offset fit in a byte?
        if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
        {
            if (dspInByte)
            {
                dst += emitOutputByte(dst, code | 0x45);
                dst += emitOutputByte(dst, dsp);
            }
            else
            {
                dst += emitOutputByte(dst, code | 0x85);
                dst += emitOutputLong(dst, dsp);
            }
        }
        else
        {
            if (dspInByte)
            {
                dst += emitOutputWord(dst, code | 0x4500);
                dst += emitOutputByte(dst, dsp);
            }
            else
            {
                dst += emitOutputWord(dst, code | 0x8500);
                dst += emitOutputLong(dst, dsp);
            }
        }
    }
    else
    {

#if !FEATURE_FIXED_OUT_ARGS
        // Adjust the offset by the amount currently pushed on the CPU stack
        dsp += emitCurStackLvl;
#endif

        dspInByte = ((signed char)dsp == (int)dsp);
        dspIsZero = (dsp == 0);

        // Does the offset fit in a byte?
        if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
        {
            if (dspInByte)
            {
                if (dspIsZero)
                {
                    dst += emitOutputByte(dst, code | 0x04);
                    dst += emitOutputByte(dst, 0x24);
                }
                else
                {
                    dst += emitOutputByte(dst, code | 0x44);
                    dst += emitOutputByte(dst, 0x24);
                    dst += emitOutputByte(dst, dsp);
                }
            }
            else
            {
                dst += emitOutputByte(dst, code | 0x84);
                dst += emitOutputByte(dst, 0x24);
                dst += emitOutputLong(dst, dsp);
            }
        }
        else
        {
            if (dspInByte)
            {
                if (dspIsZero)
                {
                    dst += emitOutputWord(dst, code | 0x0400);
                    dst += emitOutputByte(dst, 0x24);
                }
                else
                {
                    dst += emitOutputWord(dst, code | 0x4400);
                    dst += emitOutputByte(dst, 0x24);
                    dst += emitOutputByte(dst, dsp);
                }
            }
            else
            {
                dst += emitOutputWord(dst, code | 0x8400);
                dst += emitOutputByte(dst, 0x24);
                dst += emitOutputLong(dst, dsp);
            }
        }
    }

    // Now generate the constant value, if present
    if (addc)
    {
        ssize_t cval = addc->cnsVal;

#ifdef TARGET_AMD64
        // all these opcodes only take a sign-extended 4-byte immediate
        noway_assert(opsz < 8 || ((int)cval == cval && !addc->cnsReloc));
#endif

        switch (opsz)
        {
            case 0:
            case 4:
            case 8:
                dst += emitOutputLong(dst, cval);
                break;
            case 2:
                dst += emitOutputWord(dst, cval);
                break;
            case 1:
                dst += emitOutputByte(dst, cval);
                break;

            default:
                assert(!"unexpected operand size");
        }

        if (addc->cnsReloc)
        {
            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)(size_t)cval, IMAGE_REL_BASED_HIGHLOW);
            assert(opsz == 4);
        }
    }

    // Does this instruction operate on a GC ref value?
    if (id->idGCref())
    {
        // Factor in the sub-variable offset
        adr += AlignDown(id->idAddr()->iiaLclVar.lvaOffset(), TARGET_POINTER_SIZE);

        switch (id->idInsFmt())
        {
            case IF_SRD:
                // Read  stack                    -- no change
                break;

            case IF_SWR: // Stack Write (So we need to update GC live for stack var)
                // Write stack                    -- GC var may be born
                emitGCvarLiveUpd(adr, varNum, id->idGCref(), dst DEBUG_ARG(varNum));
                break;

            case IF_SRD_CNS:
                // Read  stack                    -- no change
                break;

            case IF_SWR_CNS:
                // Write stack                    -- no change
                break;

            case IF_SRD_RRD:
            case IF_RRD_SRD:
                // Read  stack   , read  register -- no change
                break;

            case IF_RWR_SRD: // Register Write, Stack Read (So we need to update GC live for register)

                // Read  stack   , write register -- GC reg may be born
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
                break;

            case IF_SWR_RRD: // Stack Write, Register Read (So we need to update GC live for stack var)
                // Read  register, write stack    -- GC var may be born
                emitGCvarLiveUpd(adr, varNum, id->idGCref(), dst DEBUG_ARG(varNum));
                break;

            case IF_RRW_SRD: // Register Read/Write, Stack Read (So we need to update GC live for register)

                // reg could have been a GCREF as GCREF + int=BYREF
                //                             or BYREF+/-int=BYREF
                assert(id->idGCref() == GCT_BYREF && (ins == INS_add || ins == INS_sub));
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
                break;

            case IF_SRW_CNS:
            case IF_SRW_RRD:
            // += -= of a byref, no change

            case IF_SRW:
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }
    }
    else
    {
        if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
        {
            switch (id->idInsFmt())
            {
                case IF_RWR_SRD: // Register Write, Stack Read
                case IF_RRW_SRD: // Register Read/Write, Stack Read
                case IF_RWR_RRD_SRD:
                    emitGCregDeadUpd(id->idReg1(), dst);
                    break;
                default:
                    break;
            }

            if (ins == INS_mulEAX || ins == INS_imulEAX)
            {
                emitGCregDeadUpd(REG_EAX, dst);
                emitGCregDeadUpd(REG_EDX, dst);
            }

            // For the three operand imul instruction the target register
            // is encoded in the opcode

            if (instrIs3opImul(ins))
            {
                regNumber tgtReg = inst3opImulReg(ins);
                emitGCregDeadUpd(tgtReg, dst);
            }
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction with a static data member (class variable).
 */

BYTE* emitter::emitOutputCV(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc)
{
    BYTE*                addr;
    CORINFO_FIELD_HANDLE fldh;
    ssize_t              offs;
    int                  doff;

    emitAttr    size      = id->idOpSize();
    size_t      opsz      = EA_SIZE_IN_BYTES(size);
    instruction ins       = id->idIns();
    bool        isMoffset = false;

    // Get hold of the field handle and offset
    fldh = id->idAddr()->iiaFieldHnd;
    offs = emitGetInsDsp(id);

    // Special case: mov reg, fs:[ddd]
    if (fldh == FLD_GLOBAL_FS)
    {
        dst += emitOutputByte(dst, 0x64);
    }

    // Compute VEX prefix
    // Some of its callers already add VEX prefix and then call this routine.
    // Therefore add VEX prefix is not already present.
    code = AddVexPrefixIfNeededAndNotPresent(ins, code, size);

    // Compute the REX prefix
    if (TakesRexWPrefix(ins, size))
    {
        code = AddRexWPrefix(ins, code);
    }

    // `addc` is used for two kinds if instructions
    // 1. ins like ADD that can have reg/mem and const versions both and const version needs to modify the opcode for
    // large constant operand (e.g., imm32)
    // 2. certain SSE/AVX ins have const operand as control bits that is always 1-Byte (imm8) even if `size` > 1-Byte
    if (addc && (size > EA_1BYTE))
    {
        ssize_t cval = addc->cnsVal;
        // Does the constant fit in a byte?
        if ((signed char)cval == cval && addc->cnsReloc == false && ins != INS_mov && ins != INS_test)
        {
            // SSE/AVX do not need to modify opcode
            if (id->idInsFmt() != IF_MRW_SHF && !IsSSEOrAVXInstruction(ins))
            {
                code |= 2;
            }

            opsz = 1;
        }
    }
#ifdef TARGET_X86
    else
    {
        // Special case: "mov eax, [addr]" and "mov [addr], eax"
        // Amd64: this is one case where addr can be 64-bit in size.  This is
        // currently unused or not enabled on amd64 as it always uses RIP
        // relative addressing which results in smaller instruction size.
        if (ins == INS_mov && id->idReg1() == REG_EAX)
        {
            switch (id->idInsFmt())
            {
                case IF_RWR_MRD:

                    assert(code == (insCodeRM(ins) | (insEncodeReg345(ins, REG_EAX, EA_PTRSIZE, NULL) << 8) | 0x0500));

                    code &= ~((code_t)0xFFFFFFFF);
                    code |= 0xA0;
                    isMoffset = true;
                    break;

                case IF_MWR_RRD:

                    assert(code == (insCodeMR(ins) | (insEncodeReg345(ins, REG_EAX, EA_PTRSIZE, NULL) << 8) | 0x0500));

                    code &= ~((code_t)0xFFFFFFFF);
                    code |= 0xA2;
                    isMoffset = true;
                    break;

                default:
                    break;
            }
        }
    }
#endif // TARGET_X86

    // Special case emitting AVX instructions
    if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
    {
        if ((ins == INS_crc32) && (size > EA_1BYTE))
        {
            code |= 0x0100;

            if (size == EA_2BYTE)
            {
                dst += emitOutputByte(dst, 0x66);
            }
        }

        regNumber reg345 = REG_NA;
        if (IsBMIInstruction(ins))
        {
            reg345 = getBmiRegNumber(ins);
        }
        if (reg345 == REG_NA)
        {
            reg345 = id->idReg1();
        }
        else
        {
            code = insEncodeReg3456(ins, id->idReg1(), size, code);
        }
        unsigned regcode = insEncodeReg345(ins, reg345, size, &code);

        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        if (UseVEXEncoding() && (ins != INS_crc32))
        {
            // Emit last opcode byte
            // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
            assert((code & 0xFF) == 0);
            dst += emitOutputByte(dst, (code >> 8) & 0xFF);
        }
        else
        {
            dst += emitOutputWord(dst, code >> 16);
            dst += emitOutputWord(dst, code & 0xFFFF);
        }

        // Emit Mod,R/M byte
        dst += emitOutputByte(dst, regcode | 0x05);
        code = 0;
    }
    // Is this a 'big' opcode?
    else if (code & 0xFF000000)
    {
        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Output the highest word of the opcode.
        // Check again since AVX instructions encode leading opcode bytes as part of VEX prefix.
        if (code & 0xFF000000)
        {
            dst += emitOutputWord(dst, code >> 16);
        }
        code &= 0x0000FFFF;
    }
    else if (code & 0x00FF0000)
    {
        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        // Check again as VEX prefix would have encoded leading opcode byte
        if (code & 0x00FF0000)
        {
            dst += emitOutputByte(dst, code >> 16);
            code &= 0x0000FFFF;
        }

        if (size != EA_1BYTE && HasRegularWideForm(ins))
        {
            code |= 0x1;
        }
    }
    else if (CodeGen::instIsFP(ins))
    {
        assert(size == EA_4BYTE || size == EA_8BYTE);

        if (size == EA_8BYTE)
        {
            code += 4;
        }
    }
    else
    {
        // Is the operand size larger than a byte?
        switch (size)
        {
            case EA_1BYTE:
                break;

            case EA_2BYTE:
                // Output a size prefix for a 16-bit operand
                dst += emitOutputByte(dst, 0x66);
                FALLTHROUGH;

            case EA_4BYTE:
#ifdef TARGET_AMD64
            case EA_8BYTE:
#endif
                // Set the 'w' bit to get the large version
                code |= 0x1;
                break;

#ifdef TARGET_X86
            case EA_8BYTE:
                // Double operand - set the appropriate bit
                code |= 0x04;
                break;
#endif // TARGET_X86

            default:
                assert(!"unexpected size");
        }
    }

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    if (code)
    {
        if (id->idInsFmt() == IF_MRD_OFF || id->idInsFmt() == IF_RWR_MRD_OFF || isMoffset)
        {
            dst += emitOutputByte(dst, code);
        }
        else
        {
            dst += emitOutputWord(dst, code);
        }
    }

    // Do we have a constant or a static data member?
    doff = Compiler::eeGetJitDataOffs(fldh);
    if (doff >= 0)
    {
        addr = emitConsBlock + doff;

#ifdef DEBUG
        int byteSize = EA_SIZE_IN_BYTES(emitGetMemOpSize(id));

        // Check that the offset is properly aligned (i.e. the ddd in [ddd])
        // When SMALL_CODE is set, we only expect 4-byte alignment, otherwise
        // we expect the same alignment as the size of the constant.

        assert((emitChkAlign == false) || (ins == INS_lea) ||
               ((emitComp->compCodeOpt() == Compiler::SMALL_CODE) && (((size_t)addr & 3) == 0)) ||
               (((size_t)addr & (byteSize - 1)) == 0));
#endif // DEBUG
    }
    else
    {
        // Special case: mov reg, fs:[ddd] or mov reg, [ddd]
        if (jitStaticFldIsGlobAddr(fldh))
        {
            addr = nullptr;
        }
        else
        {
            addr = (BYTE*)emitComp->info.compCompHnd->getFieldAddress(fldh, nullptr);
            if (addr == nullptr)
            {
                NO_WAY("could not obtain address of static field");
            }
        }
    }

    BYTE* target = (addr + offs);

    if (!isMoffset)
    {
        INT32 addlDelta = 0;

        if (addc)
        {
            // It is of the form "ins [disp], imm" or "ins reg, [disp], imm"
            // For emitting relocation, we also need to take into account of the
            // additional bytes of code emitted for immed val.

            ssize_t cval = addc->cnsVal;

#ifdef TARGET_AMD64
            // all these opcodes only take a sign-extended 4-byte immediate
            noway_assert(opsz < 8 || ((int)cval == cval && !addc->cnsReloc));
#else  // TARGET_X86
            noway_assert(opsz <= 4);
#endif // TARGET_X86

            switch (opsz)
            {
                case 0:
                case 4:
                case 8:
                    addlDelta = -4;
                    break;
                case 2:
                    addlDelta = -2;
                    break;
                case 1:
                    addlDelta = -1;
                    break;

                default:
                    assert(!"unexpected operand size");
                    unreached();
            }
        }

#ifdef TARGET_AMD64
        // All static field and data section constant accesses should be marked as relocatable
        noway_assert(id->idIsDspReloc());
        dst += emitOutputLong(dst, 0);
#else  // TARGET_X86
        dst += emitOutputLong(dst, (int)(ssize_t)target);
#endif // TARGET_X86

        if (id->idIsDspReloc())
        {
            emitRecordRelocationWithAddlDelta((void*)(dst - sizeof(int)), target, IMAGE_REL_BASED_DISP32, addlDelta);
        }
    }
    else
    {
#ifdef TARGET_AMD64
        // This code path should never be hit on amd64 since it always uses RIP relative addressing.
        // In future if ever there is a need to enable this special case, also enable the logic
        // that sets isMoffset to true on amd64.
        unreached();
#else // TARGET_X86

        dst += emitOutputSizeT(dst, (ssize_t)target);

        if (id->idIsDspReloc())
        {
            emitRecordRelocation((void*)(dst - TARGET_POINTER_SIZE), target, IMAGE_REL_BASED_MOFFSET);
        }

#endif // TARGET_X86
    }

    // Now generate the constant value, if present
    if (addc)
    {
        ssize_t cval = addc->cnsVal;

#ifdef TARGET_AMD64
        // all these opcodes only take a sign-extended 4-byte immediate
        noway_assert(opsz < 8 || ((int)cval == cval && !addc->cnsReloc));
#endif

        switch (opsz)
        {
            case 0:
            case 4:
            case 8:
                dst += emitOutputLong(dst, cval);
                break;
            case 2:
                dst += emitOutputWord(dst, cval);
                break;
            case 1:
                dst += emitOutputByte(dst, cval);
                break;

            default:
                assert(!"unexpected operand size");
        }
        if (addc->cnsReloc)
        {
            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)(size_t)cval, IMAGE_REL_BASED_HIGHLOW);
            assert(opsz == 4);
        }
    }

    // Does this instruction operate on a GC ref value?
    if (id->idGCref())
    {
        switch (id->idInsFmt())
        {
            case IF_MRD:
            case IF_MRW:
            case IF_MWR:
                break;

            case IF_RRD_MRD:
                break;

            case IF_RWR_MRD:
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
                break;

            case IF_MRD_RRD:
            case IF_MWR_RRD:
            case IF_MRW_RRD:
                break;

            case IF_MRD_CNS:
            case IF_MWR_CNS:
            case IF_MRW_CNS:
                break;

            case IF_RRW_MRD:

                assert(id->idGCref() == GCT_BYREF);
                assert(ins == INS_add || ins == INS_sub);

                // Mark it as holding a GCT_BYREF
                emitGCregLiveUpd(GCT_BYREF, id->idReg1(), dst);
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }
    }
    else
    {
        if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
        {
            switch (id->idInsFmt())
            {
                case IF_RWR_MRD:
                case IF_RRW_MRD:
                case IF_RWR_RRD_MRD:
                    emitGCregDeadUpd(id->idReg1(), dst);
                    break;
                default:
                    break;
            }

            if (ins == INS_mulEAX || ins == INS_imulEAX)
            {
                emitGCregDeadUpd(REG_EAX, dst);
                emitGCregDeadUpd(REG_EDX, dst);
            }

            // For the three operand imul instruction the target register
            // is encoded in the opcode

            if (instrIs3opImul(ins))
            {
                regNumber tgtReg = inst3opImulReg(ins);
                emitGCregDeadUpd(tgtReg, dst);
            }
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction with one register operand.
 */

BYTE* emitter::emitOutputR(BYTE* dst, instrDesc* id)
{
    code_t code;

    instruction ins  = id->idIns();
    regNumber   reg  = id->idReg1();
    emitAttr    size = id->idOpSize();

    // We would to update GC info correctly
    assert(!IsSSEInstruction(ins));
    assert(!IsAVXInstruction(ins));

    // Get the 'base' opcode
    switch (ins)
    {
        case INS_inc:
        case INS_dec:

#ifdef TARGET_AMD64
            if (true)
#else
            if (size == EA_1BYTE)
#endif
            {
                assert(INS_inc_l == INS_inc + 1);
                assert(INS_dec_l == INS_dec + 1);

                // Can't use the compact form, use the long form
                ins = (instruction)(ins + 1);
                if (size == EA_2BYTE)
                {
                    // Output a size prefix for a 16-bit operand
                    dst += emitOutputByte(dst, 0x66);
                }

                code = insCodeRR(ins);
                if (size != EA_1BYTE)
                {
                    // Set the 'w' bit to get the large version
                    code |= 0x1;
                }

                if (TakesRexWPrefix(ins, size))
                {
                    code = AddRexWPrefix(ins, code);
                }

                // Register...
                unsigned regcode = insEncodeReg012(ins, reg, size, &code);

                // Output the REX prefix
                dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

                dst += emitOutputWord(dst, code | (regcode << 8));
            }
            else
            {
                if (size == EA_2BYTE)
                {
                    // Output a size prefix for a 16-bit operand
                    dst += emitOutputByte(dst, 0x66);
                }
                dst += emitOutputByte(dst, insCodeRR(ins) | insEncodeReg012(ins, reg, size, nullptr));
            }
            break;

        case INS_pop:
        case INS_pop_hide:
        case INS_push:
        case INS_push_hide:

            assert(size == EA_PTRSIZE);
            code = insEncodeOpreg(ins, reg, size);

            assert(!TakesVexPrefix(ins));
            assert(!TakesRexWPrefix(ins, size));

            // Output the REX prefix
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

            dst += emitOutputByte(dst, code);
            break;

        case INS_bswap:
        {
            assert(size >= EA_4BYTE && size <= EA_PTRSIZE); // 16-bit BSWAP is undefined

            // The Intel instruction set reference for BSWAP states that extended registers
            // should be enabled via REX.R, but per Vol. 2A, Sec. 2.2.1.2 (see also Figure 2-7),
            // REX.B should instead be used if the register is encoded in the opcode byte itself.
            // Therefore the default logic of insEncodeReg012 is correct for this case.

            code = insCodeRR(ins);

            if (TakesRexWPrefix(ins, size))
            {
                code = AddRexWPrefix(ins, code);
            }

            // Register...
            unsigned regcode = insEncodeReg012(ins, reg, size, &code);

            // Output the REX prefix
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

            dst += emitOutputWord(dst, code | (regcode << 8));
            break;
        }

        case INS_seto:
        case INS_setno:
        case INS_setb:
        case INS_setae:
        case INS_sete:
        case INS_setne:
        case INS_setbe:
        case INS_seta:
        case INS_sets:
        case INS_setns:
        case INS_setp:
        case INS_setnp:
        case INS_setl:
        case INS_setge:
        case INS_setle:
        case INS_setg:

            assert(id->idGCref() == GCT_NONE);
            assert(size == EA_1BYTE);

            code = insEncodeMRreg(ins, reg, EA_1BYTE, insCodeMR(ins));

            // Output the REX prefix
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

            // We expect this to always be a 'big' opcode
            assert(code & 0x00FF0000);

            dst += emitOutputByte(dst, code >> 16);
            dst += emitOutputWord(dst, code & 0x0000FFFF);

            break;

        case INS_mulEAX:
        case INS_imulEAX:

            // Kill off any GC refs in EAX or EDX
            emitGCregDeadUpd(REG_EAX, dst);
            emitGCregDeadUpd(REG_EDX, dst);

            FALLTHROUGH;

        default:

            assert(id->idGCref() == GCT_NONE);

            code = insEncodeMRreg(ins, reg, size, insCodeMR(ins));

            if (size != EA_1BYTE)
            {
                // Set the 'w' bit to get the large version
                code |= 0x1;

                if (size == EA_2BYTE)
                {
                    // Output a size prefix for a 16-bit operand
                    dst += emitOutputByte(dst, 0x66);
                }
            }

            code = AddVexPrefixIfNeeded(ins, code, size);

            if (TakesRexWPrefix(ins, size))
            {
                code = AddRexWPrefix(ins, code);
            }

            // Output the REX prefix
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

            dst += emitOutputWord(dst, code);
            break;
    }

    // Are we writing the register? if so then update the GC information
    switch (id->idInsFmt())
    {
        case IF_RRD:
            break;
        case IF_RWR:
            if (id->idGCref())
            {
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
            }
            else
            {
                emitGCregDeadUpd(id->idReg1(), dst);
            }
            break;
        case IF_RRW:
        {
#ifdef DEBUG
            regMaskTP regMask = genRegMask(reg);
#endif
            if (id->idGCref())
            {
                assert(ins == INS_inc || ins == INS_dec || ins == INS_inc_l || ins == INS_dec_l);
                // We would like to assert that the reg must currently be holding either a gcref or a byref.
                // However, we can see cases where a LCLHEAP generates a non-gcref value into a register,
                // and the first instruction we generate after the LCLHEAP is an `inc` that is typed as
                // byref. We'll properly create the byref gcinfo when this happens.
                //     assert((emitThisGCrefRegs | emitThisByrefRegs) & regMask);
                assert(id->idGCref() == GCT_BYREF);
                // Mark it as holding a GCT_BYREF
                emitGCregLiveUpd(GCT_BYREF, id->idReg1(), dst);
            }
            else
            {
                // Can't use RRW to trash a GC ref.  It's OK for unverifiable code
                // to trash Byrefs.
                assert((emitThisGCrefRegs & regMask) == 0);
            }
        }
        break;
        default:
#ifdef DEBUG
            emitDispIns(id, false, false, false);
#endif
            assert(!"unexpected instruction format");
            break;
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction with two register operands.
 */

BYTE* emitter::emitOutputRR(BYTE* dst, instrDesc* id)
{
    code_t code;

    instruction ins  = id->idIns();
    regNumber   reg1 = id->idReg1();
    regNumber   reg2 = id->idReg2();
    emitAttr    size = id->idOpSize();

    if (IsSSEOrAVXInstruction(ins))
    {
        assert((ins != INS_movd) || (isFloatReg(reg1) != isFloatReg(reg2)));

        if ((ins != INS_movd) || isFloatReg(reg1))
        {
            code = insCodeRM(ins);
        }
        else
        {
            code = insCodeMR(ins);
        }
        code = AddVexPrefixIfNeeded(ins, code, size);
        code = insEncodeRMreg(ins, code);

        if (TakesRexWPrefix(ins, size))
        {
            code = AddRexWPrefix(ins, code);
        }
    }
    else if ((ins == INS_movsx) || (ins == INS_movzx) || (insIsCMOV(ins)))
    {
        assert(hasCodeRM(ins) && !hasCodeMI(ins) && !hasCodeMR(ins));
        code = insCodeRM(ins);
        code = AddVexPrefixIfNeeded(ins, code, size);
        code = insEncodeRMreg(ins, code) | (int)(size == EA_2BYTE);
#ifdef TARGET_AMD64

        assert((size < EA_4BYTE) || (insIsCMOV(ins)));
        if ((size == EA_8BYTE) || (ins == INS_movsx))
        {
            code = AddRexWPrefix(ins, code);
        }
    }
    else if (ins == INS_movsxd)
    {
        assert(hasCodeRM(ins) && !hasCodeMI(ins) && !hasCodeMR(ins));
        code = insCodeRM(ins);
        code = AddVexPrefixIfNeeded(ins, code, size);
        code = insEncodeRMreg(ins, code);

#endif // TARGET_AMD64
    }
#ifdef FEATURE_HW_INTRINSICS
    else if ((ins == INS_bsf) || (ins == INS_bsr) || (ins == INS_crc32) || (ins == INS_lzcnt) || (ins == INS_popcnt) ||
             (ins == INS_tzcnt))
    {
        assert(hasCodeRM(ins) && !hasCodeMI(ins) && !hasCodeMR(ins));
        code = insCodeRM(ins);
        code = AddVexPrefixIfNeeded(ins, code, size);
        code = insEncodeRMreg(ins, code);
        if ((ins == INS_crc32) && (size > EA_1BYTE))
        {
            code |= 0x0100;
        }

        if (size == EA_2BYTE)
        {
            assert(ins == INS_crc32);
            dst += emitOutputByte(dst, 0x66);
        }
        else if (size == EA_8BYTE)
        {
            code = AddRexWPrefix(ins, code);
        }
    }
#endif // FEATURE_HW_INTRINSICS
    else
    {
        assert(!TakesVexPrefix(ins));
        code = insCodeMR(ins);
        code = insEncodeMRreg(ins, code);

        if (ins != INS_test)
        {
            code |= 2;
        }

        switch (size)
        {
            case EA_1BYTE:
                noway_assert(RBM_BYTE_REGS & genRegMask(reg1));
                noway_assert(RBM_BYTE_REGS & genRegMask(reg2));
                break;

            case EA_2BYTE:
                // Output a size prefix for a 16-bit operand
                dst += emitOutputByte(dst, 0x66);
                FALLTHROUGH;

            case EA_4BYTE:
                // Set the 'w' bit to get the large version
                code |= 0x1;
                break;

#ifdef TARGET_AMD64
            case EA_8BYTE:
                // TODO-AMD64-CQ: Better way to not emit REX.W when we don't need it
                // Don't need to zero out the high bits explicitly
                if ((ins != INS_xor) || (reg1 != reg2))
                {
                    code = AddRexWPrefix(ins, code);
                }
                else
                {
                    id->idOpSize(EA_4BYTE);
                }

                // Set the 'w' bit to get the large version
                code |= 0x1;
                break;

#endif // TARGET_AMD64

            default:
                assert(!"unexpected size");
        }
    }

    regNumber regFor012Bits = reg2;
    regNumber regFor345Bits = REG_NA;
    if (IsBMIInstruction(ins))
    {
        regFor345Bits = getBmiRegNumber(ins);
    }
    if (regFor345Bits == REG_NA)
    {
        regFor345Bits = reg1;
    }
    if (ins == INS_movd)
    {
        assert(isFloatReg(reg1) != isFloatReg(reg2));
        if (isFloatReg(reg2))
        {
            std::swap(regFor012Bits, regFor345Bits);
        }
    }

    unsigned regCode = insEncodeReg345(ins, regFor345Bits, size, &code);
    regCode |= insEncodeReg012(ins, regFor012Bits, size, &code);

    if (TakesVexPrefix(ins))
    {
        // In case of AVX instructions that take 3 operands, we generally want to encode reg1
        // as first source.  In this case, reg1 is both a source and a destination.
        // The exception is the "merge" 3-operand case, where we have a move instruction, such
        // as movss, and we want to merge the source with itself.
        //
        // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
        // now we use the single source as source1 and source2.
        if (IsDstDstSrcAVXInstruction(ins))
        {
            // encode source/dest operand reg in 'vvvv' bits in 1's complement form
            code = insEncodeReg3456(ins, reg1, size, code);
        }
        else if (IsDstSrcSrcAVXInstruction(ins))
        {
            // encode source operand reg in 'vvvv' bits in 1's complement form
            code = insEncodeReg3456(ins, reg2, size, code);
        }
    }

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    if (code & 0xFF000000)
    {
        // Output the highest word of the opcode
        dst += emitOutputWord(dst, code >> 16);
        code &= 0x0000FFFF;

        if (Is4ByteSSEInstruction(ins))
        {
            // Output 3rd byte of the opcode
            dst += emitOutputByte(dst, code);
            code &= 0xFF00;
        }
    }
    else if (code & 0x00FF0000)
    {
        dst += emitOutputByte(dst, code >> 16);
        code &= 0x0000FFFF;
    }

    // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
    if ((code & 0xFF00) == 0xC000)
    {
        dst += emitOutputWord(dst, code | (regCode << 8));
    }
    else if ((code & 0xFF) == 0x00)
    {
        // This case happens for some SSE/AVX instructions only
        assert(IsAVXInstruction(ins) || Is4ByteSSEInstruction(ins));

        dst += emitOutputByte(dst, (code >> 8) & 0xFF);
        dst += emitOutputByte(dst, (0xC0 | regCode));
    }
    else
    {
        dst += emitOutputWord(dst, code);
        dst += emitOutputByte(dst, (0xC0 | regCode));
    }

    // Does this instruction operate on a GC ref value?
    if (id->idGCref())
    {
        switch (id->idInsFmt())
        {
            case IF_RRD_RRD:
                break;

            case IF_RWR_RRD:

                if (emitSyncThisObjReg != REG_NA && emitIGisInProlog(emitCurIG) && reg2 == (int)REG_ARG_0)
                {
                    // We're relocating "this" in the prolog
                    assert(emitComp->lvaIsOriginalThisArg(0));
                    assert(emitComp->lvaTable[0].lvRegister);
                    assert(emitComp->lvaTable[0].GetRegNum() == reg1);

                    if (emitFullGCinfo)
                    {
                        emitGCregLiveSet(id->idGCref(), genRegMask(reg1), dst, true);
                        break;
                    }
                    else
                    {
                        /* If emitFullGCinfo==false, the we don't use any
                           regPtrDsc's and so explictly note the location
                           of "this" in GCEncode.cpp
                         */
                    }
                }

                emitGCregLiveUpd(id->idGCref(), reg1, dst);
                break;

            case IF_RRW_RRD:

                switch (id->idIns())
                {
                    /*
                        This must be one of the following cases:

                        xor reg, reg        to assign NULL

                        and r1 , r2         if (ptr1 && ptr2) ...
                        or  r1 , r2         if (ptr1 || ptr2) ...

                        add r1 , r2         to compute a normal byref
                        sub r1 , r2         to compute a strange byref (VC only)

                    */
                    case INS_xor:
                        assert(reg1 == reg2);
                        emitGCregLiveUpd(id->idGCref(), reg1, dst);
                        break;

                    case INS_or:
                    case INS_and:
                        emitGCregDeadUpd(reg1, dst);
                        break;

                    case INS_add:
                    case INS_sub:
                        assert(id->idGCref() == GCT_BYREF);

#if 0
#ifdef DEBUG
                        // Due to elided register moves, we can't have the following assert.
                        // For example, consider:
                        //    t85 = LCL_VAR byref V01 arg1 rdx (last use) REG rdx
                        //        /--*  t85    byref
                        //        *  STORE_LCL_VAR byref  V40 tmp31 rdx REG rdx
                        // Here, V01 is type `long` on entry, then is stored as a byref. But because
                        // the register allocator assigned the same register, no instruction was
                        // generated, and we only (currently) make gcref/byref changes in emitter GC info
                        // when an instruction is generated. We still generate correct GC info, as this
                        // instruction, if writing a GC ref even through reading a long, will go live here.
                        // These situations typically occur due to unsafe casting, such as with Span<T>.

                        regMaskTP regMask;
                        regMask = genRegMask(reg1) | genRegMask(reg2);

                        // r1/r2 could have been a GCREF as GCREF + int=BYREF
                        //                               or BYREF+/-int=BYREF
                        assert(((regMask & emitThisGCrefRegs) && (ins == INS_add)) ||
                               ((regMask & emitThisByrefRegs) && (ins == INS_add || ins == INS_sub)));
#endif // DEBUG
#endif // 0

                        // Mark r1 as holding a byref
                        emitGCregLiveUpd(GCT_BYREF, reg1, dst);
                        break;

                    default:
#ifdef DEBUG
                        emitDispIns(id, false, false, false);
#endif
                        assert(!"unexpected GC reg update instruction");
                }

                break;

            case IF_RRW_RRW:
                // This must be "xchg reg1, reg2"
                assert(id->idIns() == INS_xchg);

                // If we got here, the GC-ness of the registers doesn't match, so we have to "swap" them in the GC
                // register pointer mask.

                GCtype gc1, gc2;

                gc1 = emitRegGCtype(reg1);
                gc2 = emitRegGCtype(reg2);

                if (gc1 != gc2)
                {
                    // Kill the GC-info about the GC registers

                    if (needsGC(gc1))
                    {
                        emitGCregDeadUpd(reg1, dst);
                    }

                    if (needsGC(gc2))
                    {
                        emitGCregDeadUpd(reg2, dst);
                    }

                    // Now, swap the info

                    if (needsGC(gc1))
                    {
                        emitGCregLiveUpd(gc1, reg2, dst);
                    }

                    if (needsGC(gc2))
                    {
                        emitGCregLiveUpd(gc2, reg1, dst);
                    }
                }
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }
    }
    else
    {
        if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
        {
            switch (id->idInsFmt())
            {
                case IF_RRD_CNS:
                    // INS_mulEAX can not be used with any of these formats
                    assert(ins != INS_mulEAX && ins != INS_imulEAX);

                    // For the three operand imul instruction the target
                    // register is encoded in the opcode

                    if (instrIs3opImul(ins))
                    {
                        regNumber tgtReg = inst3opImulReg(ins);
                        emitGCregDeadUpd(tgtReg, dst);
                    }
                    break;

                case IF_RWR_RRD:
                case IF_RRW_RRD:
                case IF_RWR_RRD_RRD:
                    emitGCregDeadUpd(reg1, dst);
                    break;

                default:
                    break;
            }
        }
    }

    return dst;
}

BYTE* emitter::emitOutputRRR(BYTE* dst, instrDesc* id)
{
    code_t code;

    instruction ins = id->idIns();
    assert(IsAVXInstruction(ins));
    assert(IsThreeOperandAVXInstruction(ins) || isAvxBlendv(ins));
    regNumber targetReg = id->idReg1();
    regNumber src1      = id->idReg2();
    regNumber src2      = id->idReg3();
    emitAttr  size      = id->idOpSize();

    code = insCodeRM(ins);
    code = AddVexPrefixIfNeeded(ins, code, size);
    code = insEncodeRMreg(ins, code);

    if (TakesRexWPrefix(ins, size))
    {
        code = AddRexWPrefix(ins, code);
    }

    unsigned regCode = insEncodeReg345(ins, targetReg, size, &code);
    regCode |= insEncodeReg012(ins, src2, size, &code);
    // encode source operand reg in 'vvvv' bits in 1's complement form
    code = insEncodeReg3456(ins, src1, size, code);

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    // Is this a 'big' opcode?
    if (code & 0xFF000000)
    {
        // Output the highest word of the opcode
        dst += emitOutputWord(dst, code >> 16);
        code &= 0x0000FFFF;
    }
    else if (code & 0x00FF0000)
    {
        dst += emitOutputByte(dst, code >> 16);
        code &= 0x0000FFFF;
    }

    // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
    if ((code & 0xFF00) == 0xC000)
    {
        dst += emitOutputWord(dst, code | (regCode << 8));
    }
    else if ((code & 0xFF) == 0x00)
    {
        // This case happens for AVX instructions only
        assert(IsAVXInstruction(ins));

        dst += emitOutputByte(dst, (code >> 8) & 0xFF);
        dst += emitOutputByte(dst, (0xC0 | regCode));
    }
    else
    {
        dst += emitOutputWord(dst, code);
        dst += emitOutputByte(dst, (0xC0 | regCode));
    }

    noway_assert(!id->idGCref());

    if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
    {
        switch (id->idInsFmt())
        {
            case IF_RWR_RRD_RRD:
            case IF_RWR_RRD_RRD_CNS:
            case IF_RWR_RRD_RRD_RRD:
                emitGCregDeadUpd(id->idReg1(), dst);
                break;

            default:
                break;
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction with a register and constant operands.
 */

BYTE* emitter::emitOutputRI(BYTE* dst, instrDesc* id)
{
    code_t      code;
    emitAttr    size      = id->idOpSize();
    instruction ins       = id->idIns();
    regNumber   reg       = id->idReg1();
    ssize_t     val       = emitGetInsSC(id);
    bool        valInByte = ((signed char)val == (target_ssize_t)val) && (ins != INS_mov) && (ins != INS_test);

    // BT reg,imm might be useful but it requires special handling of the immediate value
    // (it is always encoded in a byte). Let's not complicate things until this is needed.
    assert(ins != INS_bt);

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte
    }

    noway_assert(emitVerifyEncodable(ins, size, reg));

    if (IsSSEOrAVXInstruction(ins))
    {
        // Handle SSE2 instructions of the form "opcode reg, immed8"

        assert(id->idGCref() == GCT_NONE);
        assert(valInByte);

        // The left and right shifts use the same encoding, and are distinguished by the Reg/Opcode field.
        regNumber regOpcode = getSseShiftRegNumber(ins);

        // Get the 'base' opcode.
        code = insCodeMI(ins);
        code = AddVexPrefixIfNeeded(ins, code, size);
        code = insEncodeMIreg(ins, reg, size, code);
        assert(code & 0x00FF0000);
        if (TakesVexPrefix(ins))
        {
            // The 'vvvv' bits encode the destination register, which for this case (RI)
            // is the same as the source.
            code = insEncodeReg3456(ins, reg, size, code);
        }

        unsigned regcode = (insEncodeReg345(ins, regOpcode, size, &code) | insEncodeReg012(ins, reg, size, &code)) << 8;

        // Output the REX prefix
        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        if (code & 0xFF000000)
        {
            dst += emitOutputWord(dst, code >> 16);
        }
        else if (code & 0xFF0000)
        {
            dst += emitOutputByte(dst, code >> 16);
        }

        dst += emitOutputWord(dst, code | regcode);

        dst += emitOutputByte(dst, val);

        return dst;
    }

    // The 'mov' opcode is special
    if (ins == INS_mov)
    {
        code = insCodeACC(ins);
        assert(code < 0x100);

        code |= 0x08; // Set the 'w' bit
        unsigned regcode = insEncodeReg012(ins, reg, size, &code);
        code |= regcode;

        // This is INS_mov and will not take VEX prefix
        assert(!TakesVexPrefix(ins));

        if (TakesRexWPrefix(ins, size))
        {
            code = AddRexWPrefix(ins, code);
        }

        dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

        dst += emitOutputByte(dst, code);
        if (size == EA_4BYTE)
        {
            dst += emitOutputLong(dst, val);
        }
#ifdef TARGET_AMD64
        else
        {
            assert(size == EA_PTRSIZE);
            dst += emitOutputSizeT(dst, val);
        }
#endif

        if (id->idIsCnsReloc())
        {
            emitRecordRelocation((void*)(dst - (unsigned)EA_SIZE(size)), (void*)(size_t)val, IMAGE_REL_BASED_MOFFSET);
        }

        goto DONE;
    }

    // Decide which encoding is the shortest
    bool useSigned, useACC;

    if (reg == REG_EAX && !instrIs3opImul(ins))
    {
        if (size == EA_1BYTE || (ins == INS_test))
        {
            // For al, ACC encoding is always the smallest
            useSigned = false;
            useACC    = true;
        }
        else
        {
            /* For ax/eax, we avoid ACC encoding for small constants as we
             * can emit the small constant and have it sign-extended.
             * For big constants, the ACC encoding is better as we can use
             * the 1 byte opcode
             */

            if (valInByte)
            {
                // avoid using ACC encoding
                useSigned = true;
                useACC    = false;
            }
            else
            {
                useSigned = false;
                useACC    = true;
            }
        }
    }
    else
    {
        useACC = false;

        if (valInByte)
        {
            useSigned = true;
        }
        else
        {
            useSigned = false;
        }
    }

    // "test" has no 's' bit
    if (!HasRegularWideImmediateForm(ins))
    {
        useSigned = false;
    }

    // Get the 'base' opcode
    if (useACC)
    {
        assert(!useSigned);
        code = insCodeACC(ins);
    }
    else
    {
        assert(!useSigned || valInByte);

        // Some instructions (at least 'imul') do not have a
        // r/m, immed form, but do have a dstReg,srcReg,imm8 form.
        if (valInByte && useSigned && insNeedsRRIb(ins))
        {
            code = insEncodeRRIb(ins, reg, size);
        }
        else
        {
            code = insCodeMI(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeMIreg(ins, reg, size, code);
        }
    }

    switch (size)
    {
        case EA_1BYTE:
            break;

        case EA_2BYTE:
            // Output a size prefix for a 16-bit operand
            dst += emitOutputByte(dst, 0x66);
            FALLTHROUGH;

        case EA_4BYTE:
            // Set the 'w' bit to get the large version
            code |= 0x1;
            break;

#ifdef TARGET_AMD64
        case EA_8BYTE:
            /* Set the 'w' bit to get the large version */
            /* and the REX.W bit to get the really large version */

            code = AddRexWPrefix(ins, code);
            code |= 0x1;
            break;
#endif

        default:
            assert(!"unexpected size");
    }

    // Output the REX prefix
    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

    // Does the value fit in a sign-extended byte?
    // Important!  Only set the 's' bit when we have a size larger than EA_1BYTE.
    // Note: A sign-extending immediate when (size == EA_1BYTE) is invalid in 64-bit mode.

    if (useSigned && (size > EA_1BYTE))
    {
        // We can just set the 's' bit, and issue an immediate byte

        code |= 0x2; // Set the 's' bit to use a sign-extended immediate byte.
        dst += emitOutputWord(dst, code);
        dst += emitOutputByte(dst, val);
    }
    else
    {
        // Can we use an accumulator (EAX) encoding?
        if (useACC)
        {
            dst += emitOutputByte(dst, code);
        }
        else
        {
            dst += emitOutputWord(dst, code);
        }

        switch (size)
        {
            case EA_1BYTE:
                dst += emitOutputByte(dst, val);
                break;
            case EA_2BYTE:
                dst += emitOutputWord(dst, val);
                break;
            case EA_4BYTE:
                dst += emitOutputLong(dst, val);
                break;
#ifdef TARGET_AMD64
            case EA_8BYTE:
                dst += emitOutputLong(dst, val);
                break;
#endif // TARGET_AMD64
            default:
                break;
        }

        if (id->idIsCnsReloc())
        {
            emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)(size_t)val, IMAGE_REL_BASED_HIGHLOW);
            assert(size == EA_4BYTE);
        }
    }

DONE:

    // Does this instruction operate on a GC ref value?
    if (id->idGCref())
    {
        switch (id->idInsFmt())
        {
            case IF_RRD_CNS:
                break;

            case IF_RWR_CNS:
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
                break;

            case IF_RRW_CNS:
                assert(id->idGCref() == GCT_BYREF);

#ifdef DEBUG
                regMaskTP regMask;
                regMask = genRegMask(reg);
                // FIXNOW review the other places and relax the assert there too

                // The reg must currently be holding either a gcref or a byref
                // GCT_GCREF+int = GCT_BYREF, and GCT_BYREF+/-int = GCT_BYREF
                if (emitThisGCrefRegs & regMask)
                {
                    assert(ins == INS_add);
                }
                if (emitThisByrefRegs & regMask)
                {
                    assert(ins == INS_add || ins == INS_sub);
                }
#endif
                // Mark it as holding a GCT_BYREF
                emitGCregLiveUpd(GCT_BYREF, id->idReg1(), dst);
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }

        // mul can never produce a GC ref
        assert(!instrIs3opImul(ins));
        assert(ins != INS_mulEAX && ins != INS_imulEAX);
    }
    else
    {
        switch (id->idInsFmt())
        {
            case IF_RRD_CNS:
                // INS_mulEAX can not be used with any of these formats
                assert(ins != INS_mulEAX && ins != INS_imulEAX);

                // For the three operand imul instruction the target
                // register is encoded in the opcode

                if (instrIs3opImul(ins))
                {
                    regNumber tgtReg = inst3opImulReg(ins);
                    emitGCregDeadUpd(tgtReg, dst);
                }
                break;

            case IF_RRW_CNS:
            case IF_RWR_CNS:
                assert(!instrIs3opImul(ins));

                emitGCregDeadUpd(id->idReg1(), dst);
                break;

            default:
#ifdef DEBUG
                emitDispIns(id, false, false, false);
#endif
                assert(!"unexpected GC ref instruction format");
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output an instruction with a constant operand.
 */

BYTE* emitter::emitOutputIV(BYTE* dst, instrDesc* id)
{
    code_t      code;
    instruction ins       = id->idIns();
    emitAttr    size      = id->idOpSize();
    ssize_t     val       = emitGetInsSC(id);
    bool        valInByte = ((signed char)val == (target_ssize_t)val);

    // We would to update GC info correctly
    assert(!IsSSEInstruction(ins));
    assert(!IsAVXInstruction(ins));

#ifdef TARGET_AMD64
    // all these opcodes take a sign-extended 4-byte immediate, max
    noway_assert(size < EA_8BYTE || ((int)val == val && !id->idIsCnsReloc()));
#endif

    if (id->idIsCnsReloc())
    {
        valInByte = false; // relocs can't be placed in a byte

        // Of these instructions only the push instruction can have reloc
        assert(ins == INS_push || ins == INS_push_hide);
    }

    switch (ins)
    {
        case INS_jge:
            assert((val >= -128) && (val <= 127));
            dst += emitOutputByte(dst, insCode(ins));
            dst += emitOutputByte(dst, val);
            break;

        case INS_loop:
            assert((val >= -128) && (val <= 127));
            dst += emitOutputByte(dst, insCodeMI(ins));
            dst += emitOutputByte(dst, val);
            break;

        case INS_ret:
            assert(val);
            dst += emitOutputByte(dst, insCodeMI(ins));
            dst += emitOutputWord(dst, val);
            break;

        case INS_push_hide:
        case INS_push:
            code = insCodeMI(ins);

            // Does the operand fit in a byte?
            if (valInByte)
            {
                dst += emitOutputByte(dst, code | 2);
                dst += emitOutputByte(dst, val);
            }
            else
            {
                if (TakesRexWPrefix(ins, size))
                {
                    code = AddRexWPrefix(ins, code);
                    dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);
                }

                dst += emitOutputByte(dst, code);
                dst += emitOutputLong(dst, val);
                if (id->idIsCnsReloc())
                {
                    emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)(size_t)val, IMAGE_REL_BASED_HIGHLOW);
                }
            }
            break;

        default:
            assert(!"unexpected instruction");
    }

    // GC tracking for "push"es is done by "emitStackPush", for all other instructions
    // that can reach here we do not expect (and do not handle) GC refs.
    assert((ins == INS_push) || !id->idGCref());

    return dst;
}

/*****************************************************************************
 *
 *  Output a local jump instruction.
 *  This function also handles non-jumps that have jump-like characteristics, like RIP-relative LEA of a label that
 *  needs to get bound to an actual address and processed by branch shortening.
 */

BYTE* emitter::emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* i)
{
    unsigned srcOffs;
    unsigned dstOffs;
    BYTE*    srcAddr;
    BYTE*    dstAddr;
    ssize_t  distVal;

    instrDescJmp* id  = (instrDescJmp*)i;
    instruction   ins = id->idIns();
    bool          jmp;
    bool          relAddr = true; // does the instruction use relative-addressing?

    // SSE/AVX doesnt make any sense here
    assert(!IsSSEInstruction(ins));
    assert(!IsAVXInstruction(ins));

    size_t ssz;
    size_t lsz;

    switch (ins)
    {
        default:
            ssz = JCC_SIZE_SMALL;
            lsz = JCC_SIZE_LARGE;
            jmp = true;
            break;

        case INS_jmp:
            ssz = JMP_SIZE_SMALL;
            lsz = JMP_SIZE_LARGE;
            jmp = true;
            break;

        case INS_call:
            ssz = lsz = CALL_INST_SIZE;
            jmp       = false;
            break;

        case INS_push_hide:
        case INS_push:
            ssz = lsz = 5;
            jmp       = false;
            relAddr   = false;
            break;

        case INS_mov:
        case INS_lea:
            ssz = lsz = id->idCodeSize();
            jmp       = false;
            relAddr   = false;
            break;
    }

    // Figure out the distance to the target
    srcOffs = emitCurCodeOffs(dst);
    srcAddr = emitOffsetToPtr(srcOffs);

    if (id->idAddr()->iiaHasInstrCount())
    {
        assert(ig != nullptr);
        int      instrCount = id->idAddr()->iiaGetInstrCount();
        unsigned insNum     = emitFindInsNum(ig, id);
        if (instrCount < 0)
        {
            // Backward branches using instruction count must be within the same instruction group.
            assert(insNum + 1 >= (unsigned)(-instrCount));
        }
        dstOffs = ig->igOffs + emitFindOffset(ig, (insNum + 1 + instrCount));
        dstAddr = emitOffsetToPtr(dstOffs);
    }
    else
    {
        dstOffs = id->idAddr()->iiaIGlabel->igOffs;
        dstAddr = emitOffsetToPtr(dstOffs);
        if (!relAddr)
        {
            srcAddr = nullptr;
        }
    }

    distVal = (ssize_t)(dstAddr - srcAddr);

    if (dstOffs <= srcOffs)
    {
        // This is a backward jump - distance is known at this point
        CLANG_FORMAT_COMMENT_ANCHOR;

#if DEBUG_EMIT
        if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
        {
            size_t blkOffs = id->idjIG->igOffs;

            if (INTERESTING_JUMP_NUM == 0)
            {
                printf("[3] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
            }
            printf("[3] Jump  block is at %08X - %02X = %08X\n", blkOffs, emitOffsAdj, blkOffs - emitOffsAdj);
            printf("[3] Jump        is at %08X - %02X = %08X\n", srcOffs, emitOffsAdj, srcOffs - emitOffsAdj);
            printf("[3] Label block is at %08X - %02X = %08X\n", dstOffs, emitOffsAdj, dstOffs - emitOffsAdj);
        }
#endif

        // Can we use a short jump?
        if (jmp && distVal - ssz >= (size_t)JMP_DIST_SMALL_MAX_NEG)
        {
            emitSetShortJump(id);
        }
    }
    else
    {
        // This is a  forward jump - distance will be an upper limit
        emitFwdJumps = true;

        // The target offset will be closer by at least 'emitOffsAdj', but only if this
        // jump doesn't cross the hot-cold boundary.
        if (!emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
        {
            dstOffs -= emitOffsAdj;
            distVal -= emitOffsAdj;
        }

        // Record the location of the jump for later patching
        id->idjOffs = dstOffs;

        // Are we overflowing the id->idjOffs bitfield?
        if (id->idjOffs != dstOffs)
        {
            IMPL_LIMITATION("Method is too large");
        }

#if DEBUG_EMIT
        if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
        {
            size_t blkOffs = id->idjIG->igOffs;

            if (INTERESTING_JUMP_NUM == 0)
            {
                printf("[4] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
            }
            printf("[4] Jump  block is at %08X\n", blkOffs);
            printf("[4] Jump        is at %08X\n", srcOffs);
            printf("[4] Label block is at %08X - %02X = %08X\n", dstOffs + emitOffsAdj, emitOffsAdj, dstOffs);
        }
#endif

        // Can we use a short jump?
        if (jmp && distVal - ssz <= (size_t)JMP_DIST_SMALL_MAX_POS)
        {
            emitSetShortJump(id);
        }
    }

    // Adjust the offset to emit relative to the end of the instruction
    if (relAddr)
    {
        distVal -= id->idjShort ? ssz : lsz;
    }

#ifdef DEBUG
    if (0 && emitComp->verbose)
    {
        size_t sz          = id->idjShort ? ssz : lsz;
        int    distValSize = id->idjShort ? 4 : 8;
        printf("; %s jump [%08X/%03u] from %0*X to %0*X: dist = %08XH\n", (dstOffs <= srcOffs) ? "Fwd" : "Bwd",
               emitComp->dspPtr(id), id->idDebugOnlyInfo()->idNum, distValSize, srcOffs + sz, distValSize, dstOffs,
               distVal);
    }
#endif

    // What size jump should we use?
    if (id->idjShort)
    {
        // Short jump
        assert(!id->idjKeepLong);
        assert(emitJumpCrossHotColdBoundary(srcOffs, dstOffs) == false);

        assert(JMP_SIZE_SMALL == JCC_SIZE_SMALL);
        assert(JMP_SIZE_SMALL == 2);

        assert(jmp);

        if (id->idCodeSize() != JMP_SIZE_SMALL)
        {
#if DEBUG_EMIT || defined(DEBUG)
            int offsShrinkage = id->idCodeSize() - JMP_SIZE_SMALL;
            if (INDEBUG(emitComp->verbose ||)(id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM ||
                                              INTERESTING_JUMP_NUM == 0))
            {
                printf("; NOTE: size of jump [%08p] mis-predicted by %d bytes\n", dspPtr(id), offsShrinkage);
            }
#endif
        }

        dst += emitOutputByte(dst, insCode(ins));

        // For forward jumps, record the address of the distance value
        id->idjTemp.idjAddr = (distVal > 0) ? dst : nullptr;

        dst += emitOutputByte(dst, distVal);
    }
    else
    {
        code_t code;

        // Long  jump
        if (jmp)
        {
            // clang-format off
            assert(INS_jmp + (INS_l_jmp - INS_jmp) == INS_l_jmp);
            assert(INS_jo  + (INS_l_jmp - INS_jmp) == INS_l_jo);
            assert(INS_jb  + (INS_l_jmp - INS_jmp) == INS_l_jb);
            assert(INS_jae + (INS_l_jmp - INS_jmp) == INS_l_jae);
            assert(INS_je  + (INS_l_jmp - INS_jmp) == INS_l_je);
            assert(INS_jne + (INS_l_jmp - INS_jmp) == INS_l_jne);
            assert(INS_jbe + (INS_l_jmp - INS_jmp) == INS_l_jbe);
            assert(INS_ja  + (INS_l_jmp - INS_jmp) == INS_l_ja);
            assert(INS_js  + (INS_l_jmp - INS_jmp) == INS_l_js);
            assert(INS_jns + (INS_l_jmp - INS_jmp) == INS_l_jns);
            assert(INS_jp  + (INS_l_jmp - INS_jmp) == INS_l_jp);
            assert(INS_jnp + (INS_l_jmp - INS_jmp) == INS_l_jnp);
            assert(INS_jl  + (INS_l_jmp - INS_jmp) == INS_l_jl);
            assert(INS_jge + (INS_l_jmp - INS_jmp) == INS_l_jge);
            assert(INS_jle + (INS_l_jmp - INS_jmp) == INS_l_jle);
            assert(INS_jg  + (INS_l_jmp - INS_jmp) == INS_l_jg);
            // clang-format on

            code = insCode((instruction)(ins + (INS_l_jmp - INS_jmp)));
        }
        else if (ins == INS_push || ins == INS_push_hide)
        {
            assert(insCodeMI(INS_push) == 0x68);
            code = 0x68;
        }
        else if (ins == INS_mov)
        {
            // Make it look like IF_SWR_CNS so that emitOutputSV emits the r/m32 for us
            insFormat tmpInsFmt   = id->idInsFmt();
            insGroup* tmpIGlabel  = id->idAddr()->iiaIGlabel;
            bool      tmpDspReloc = id->idIsDspReloc();

            id->idInsFmt(IF_SWR_CNS);
            id->idAddr()->iiaLclVar = ((instrDescLbl*)id)->dstLclVar;
            id->idSetIsDspReloc(false);

            dst = emitOutputSV(dst, id, insCodeMI(ins));

            // Restore id fields with original values
            id->idInsFmt(tmpInsFmt);
            id->idAddr()->iiaIGlabel = tmpIGlabel;
            id->idSetIsDspReloc(tmpDspReloc);
            code = 0xCC;
        }
        else if (ins == INS_lea)
        {
            // Make an instrDesc that looks like IF_RWR_ARD so that emitOutputAM emits the r/m32 for us.
            // We basically are doing what emitIns_R_AI does.
            // TODO-XArch-Cleanup: revisit this.
            instrDescAmd  idAmdStackLocal;
            instrDescAmd* idAmd = &idAmdStackLocal;
            *(instrDesc*)idAmd  = *(instrDesc*)id; // copy all the "core" fields
            memset((BYTE*)idAmd + sizeof(instrDesc), 0,
                   sizeof(instrDescAmd) - sizeof(instrDesc)); // zero out the tail that wasn't copied

            idAmd->idInsFmt(IF_RWR_ARD);
            idAmd->idAddr()->iiaAddrMode.amBaseReg = REG_NA;
            idAmd->idAddr()->iiaAddrMode.amIndxReg = REG_NA;
            emitSetAmdDisp(idAmd, distVal); // set the displacement
            idAmd->idSetIsDspReloc(id->idIsDspReloc());
            assert(emitGetInsAmdAny(idAmd) == distVal); // make sure "disp" is stored properly

            UNATIVE_OFFSET sz = emitInsSizeAM(idAmd, insCodeRM(ins));
            idAmd->idCodeSize(sz);

            code = insCodeRM(ins);
            code |= (insEncodeReg345(ins, id->idReg1(), EA_PTRSIZE, &code) << 8);

            dst = emitOutputAM(dst, idAmd, code, nullptr);

            code = 0xCC;

            // For forward jumps, record the address of the distance value
            // Hard-coded 4 here because we already output the displacement, as the last thing.
            id->idjTemp.idjAddr = (dstOffs > srcOffs) ? (dst - 4) : nullptr;

            // We're done
            return dst;
        }
        else
        {
            code = 0xE8;
        }

        if (ins != INS_mov)
        {
            dst += emitOutputByte(dst, code);

            if (code & 0xFF00)
            {
                dst += emitOutputByte(dst, code >> 8);
            }
        }

        // For forward jumps, record the address of the distance value
        id->idjTemp.idjAddr = (dstOffs > srcOffs) ? dst : nullptr;

        dst += emitOutputLong(dst, distVal);

#ifndef TARGET_AMD64 // all REL32 on AMD have to go through recordRelocation
        if (emitComp->opts.compReloc)
#endif
        {
            if (!relAddr)
            {
                emitRecordRelocation((void*)(dst - sizeof(INT32)), (void*)distVal, IMAGE_REL_BASED_HIGHLOW);
            }
            else if (emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
            {
                assert(id->idjKeepLong);
                emitRecordRelocation((void*)(dst - sizeof(INT32)), dst + distVal, IMAGE_REL_BASED_REL32);
            }
        }
    }

    // Local calls kill all registers
    if (ins == INS_call && (emitThisGCrefRegs | emitThisByrefRegs))
    {
        emitGCregDeadUpdMask(emitThisGCrefRegs | emitThisByrefRegs, dst);
    }

    return dst;
}

/*****************************************************************************
 *
 *  Append the machine code corresponding to the given instruction descriptor
 *  to the code block at '*dp'; the base of the code block is 'bp', and 'ig'
 *  is the instruction group that contains the instruction. Updates '*dp' to
 *  point past the generated code, and returns the size of the instruction
 *  descriptor in bytes.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    assert(emitIssuing);

    BYTE*         dst           = *dp;
    size_t        sz            = sizeof(instrDesc);
    instruction   ins           = id->idIns();
    unsigned char callInstrSize = 0;

#ifdef DEBUG
    bool dspOffs = emitComp->opts.dspGCtbls;
#endif // DEBUG

    emitAttr size = id->idOpSize();

    assert(REG_NA == (int)REG_NA);

    assert(ins != INS_imul || size >= EA_4BYTE);                  // Has no 'w' bit
    assert(instrIs3opImul(id->idIns()) == 0 || size >= EA_4BYTE); // Has no 'w' bit

    VARSET_TP GCvars(VarSetOps::UninitVal());

    // What instruction format have we got?
    switch (id->idInsFmt())
    {
        code_t   code;
        unsigned regcode;
        int      args;
        CnsVal   cnsVal;

        BYTE* addr;
        bool  recCall;

        regMaskTP gcrefRegs;
        regMaskTP byrefRegs;

        /********************************************************************/
        /*                        No operands                               */
        /********************************************************************/
        case IF_NONE:
            // the loop alignment pseudo instruction
            if (ins == INS_align)
            {
                sz = sizeof(instrDescAlign);
                // IG can be marked as not needing alignment after emitting align instruction
                // In such case, skip outputting alignment.
                if (ig->endsWithAlignInstr())
                {
                    dst = emitOutputAlign(ig, id, dst);
                }
#ifdef DEBUG
                else
                {
                    // If the IG is not marked as need alignment, then the code size
                    // should be zero i.e. no padding needed.
                    assert(id->idCodeSize() == 0);
                }
#endif
                break;
            }

            if (ins == INS_nop)
            {
                BYTE* dstRW = dst + writeableOffset;
                dstRW       = emitOutputNOP(dstRW, id->idCodeSize());
                dst         = dstRW - writeableOffset;
                break;
            }

            // the cdq instruction kills the EDX register implicitly
            if (ins == INS_cdq)
            {
                emitGCregDeadUpd(REG_EDX, dst);
            }

            assert(id->idGCref() == GCT_NONE);

            code = insCodeMR(ins);

#ifdef TARGET_AMD64
            // Support only scalar AVX instructions and hence size is hard coded to 4-byte.
            code = AddVexPrefixIfNeeded(ins, code, EA_4BYTE);

            if (((ins == INS_cdq) || (ins == INS_cwde)) && TakesRexWPrefix(ins, id->idOpSize()))
            {
                code = AddRexWPrefix(ins, code);
            }
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);
#endif
            // Is this a 'big' opcode?
            if (code & 0xFF000000)
            {
                // The high word and then the low word
                dst += emitOutputWord(dst, code >> 16);
                code &= 0x0000FFFF;
                dst += emitOutputWord(dst, code);
            }
            else if (code & 0x00FF0000)
            {
                // The high byte and then the low word
                dst += emitOutputByte(dst, code >> 16);
                code &= 0x0000FFFF;
                dst += emitOutputWord(dst, code);
            }
            else if (code & 0xFF00)
            {
                // The 2 byte opcode
                dst += emitOutputWord(dst, code);
            }
            else
            {
                // The 1 byte opcode
                dst += emitOutputByte(dst, code);
            }

            break;

        /********************************************************************/
        /*                Simple constant, local label, method              */
        /********************************************************************/

        case IF_CNS:
            dst = emitOutputIV(dst, id);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_LABEL:
        case IF_RWR_LABEL:
        case IF_SWR_LABEL:
            assert(id->idGCref() == GCT_NONE);
            assert(id->idIsBound());

            // TODO-XArch-Cleanup: handle IF_RWR_LABEL in emitOutputLJ() or change it to emitOutputAM()?
            dst = emitOutputLJ(ig, dst, id);
            sz  = (id->idInsFmt() == IF_SWR_LABEL ? sizeof(instrDescLbl) : sizeof(instrDescJmp));
            break;

        case IF_METHOD:
        case IF_METHPTR:
            // Assume we'll be recording this call
            recCall = true;

            // Get hold of the argument count and field Handle
            args = emitGetInsCDinfo(id);

            // Is this a "fat" call descriptor?
            if (id->idIsLargeCall())
            {
                instrDescCGCA* idCall = (instrDescCGCA*)id;
                gcrefRegs             = idCall->idcGcrefRegs;
                byrefRegs             = idCall->idcByrefRegs;
                VarSetOps::Assign(emitComp, GCvars, idCall->idcGCvars);
                sz = sizeof(instrDescCGCA);
            }
            else
            {
                assert(!id->idIsLargeDsp());
                assert(!id->idIsLargeCns());

                gcrefRegs = emitDecodeCallGCregs(id);
                byrefRegs = 0;
                VarSetOps::AssignNoCopy(emitComp, GCvars, VarSetOps::MakeEmpty(emitComp));
                sz = sizeof(instrDesc);
            }

            addr = (BYTE*)id->idAddr()->iiaAddr;
            assert(addr != nullptr);

            // Some helpers don't get recorded in GC tables
            if (id->idIsNoGC())
            {
                recCall = false;
            }

            // What kind of a call do we have here?
            if (id->idInsFmt() == IF_METHPTR)
            {
                // This is call indirect via a method pointer
                assert((ins == INS_call) || (ins == INS_tail_i_jmp));

                code = insCodeMR(ins);

                if (id->idIsDspReloc())
                {
                    dst += emitOutputWord(dst, code | 0x0500);
#ifdef TARGET_AMD64
                    dst += emitOutputLong(dst, 0);
#else
                    dst += emitOutputLong(dst, (int)(ssize_t)addr);
#endif
                    emitRecordRelocation((void*)(dst - sizeof(int)), addr, IMAGE_REL_BASED_DISP32);
                }
                else
                {
#ifdef TARGET_X86
                    dst += emitOutputWord(dst, code | 0x0500);
#else  // TARGET_AMD64
                    // Amd64: addr fits within 32-bits and can be encoded as a displacement relative to zero.
                    // This addr mode should never be used while generating relocatable ngen code nor if
                    // the addr can be encoded as pc-relative address.
                    noway_assert(!emitComp->opts.compReloc);
                    noway_assert(codeGen->genAddrRelocTypeHint((size_t)addr) != IMAGE_REL_BASED_REL32);
                    noway_assert(static_cast<int>(reinterpret_cast<intptr_t>(addr)) == (ssize_t)addr);

                    // This requires, specifying a SIB byte after ModRM byte.
                    dst += emitOutputWord(dst, code | 0x0400);
                    dst += emitOutputByte(dst, 0x25);
#endif // TARGET_AMD64
                    dst += emitOutputLong(dst, static_cast<int>(reinterpret_cast<intptr_t>(addr)));
                }
                goto DONE_CALL;
            }

            // Else
            // This is call direct where we know the target, thus we can
            // use a direct call; the target to jump to is in iiaAddr.
            assert(id->idInsFmt() == IF_METHOD);

            // Output the call opcode followed by the target distance
            dst += (ins == INS_l_jmp) ? emitOutputByte(dst, insCode(ins)) : emitOutputByte(dst, insCodeMI(ins));

            ssize_t offset;
#ifdef TARGET_AMD64
            // All REL32 on Amd64 go through recordRelocation.  Here we will output zero to advance dst.
            offset = 0;
            assert(id->idIsDspReloc());
#else
            // Calculate PC relative displacement.
            // Although you think we should be using sizeof(void*), the x86 and x64 instruction set
            // only allow a 32-bit offset, so we correctly use sizeof(INT32)
            offset = addr - (dst + sizeof(INT32));
#endif

            dst += emitOutputLong(dst, offset);

            if (id->idIsDspReloc())
            {
                emitRecordRelocation((void*)(dst - sizeof(INT32)), addr, IMAGE_REL_BASED_REL32);
            }

        DONE_CALL:

            /* We update the variable (not register) GC info before the call as the variables cannot be
               used by the call. Killing variables before the call helps with
               boundary conditions if the call is CORINFO_HELP_THROW - see bug 50029.
               If we ever track aliased variables (which could be used by the
               call), we would have to keep them alive past the call.
             */
            assert(FitsIn<unsigned char>(dst - *dp));
            callInstrSize = static_cast<unsigned char>(dst - *dp);

            // Note the use of address `*dp`, the call instruction address, instead of `dst`, the post-call-instruction
            // address.
            emitUpdateLiveGCvars(GCvars, *dp);

#ifdef DEBUG
            // Output any delta in GC variable info, corresponding to the before-call GC var updates done above.
            if (EMIT_GC_VERBOSE || emitComp->opts.disasmWithGC)
            {
                emitDispGCVarDelta();
            }
#endif // DEBUG

            // If the method returns a GC ref, mark EAX appropriately
            if (id->idGCref() == GCT_GCREF)
            {
                gcrefRegs |= RBM_EAX;
            }
            else if (id->idGCref() == GCT_BYREF)
            {
                byrefRegs |= RBM_EAX;
            }

#ifdef UNIX_AMD64_ABI
            // If is a multi-register return method is called, mark RDX appropriately (for System V AMD64).
            if (id->idIsLargeCall())
            {
                instrDescCGCA* idCall = (instrDescCGCA*)id;
                if (idCall->idSecondGCref() == GCT_GCREF)
                {
                    gcrefRegs |= RBM_RDX;
                }
                else if (idCall->idSecondGCref() == GCT_BYREF)
                {
                    byrefRegs |= RBM_RDX;
                }
            }
#endif // UNIX_AMD64_ABI

            // If the GC register set has changed, report the new set
            if (gcrefRegs != emitThisGCrefRegs)
            {
                emitUpdateLiveGCregs(GCT_GCREF, gcrefRegs, dst);
            }

            if (byrefRegs != emitThisByrefRegs)
            {
                emitUpdateLiveGCregs(GCT_BYREF, byrefRegs, dst);
            }

            if (recCall || args)
            {
                // For callee-pop, all arguments will be popped  after the call.
                // For caller-pop, any GC arguments will go dead after the call.

                assert(callInstrSize != 0);

                if (args >= 0)
                {
                    emitStackPop(dst, /*isCall*/ true, callInstrSize, args);
                }
                else
                {
                    emitStackKillArgs(dst, -args, callInstrSize);
                }
            }

            // Do we need to record a call location for GC purposes?
            if (!emitFullGCinfo && recCall)
            {
                assert(callInstrSize != 0);
                emitRecordGCcall(dst, callInstrSize);
            }

#ifdef DEBUG
            if (ins == INS_call)
            {
                emitRecordCallSite(emitCurCodeOffs(*dp), id->idDebugOnlyInfo()->idCallSig,
                                   (CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
            }
#endif // DEBUG

            break;

        /********************************************************************/
        /*                      One register operand                        */
        /********************************************************************/

        case IF_RRD:
        case IF_RWR:
        case IF_RRW:
            dst = emitOutputR(dst, id);
            sz  = SMALL_IDSC_SIZE;
            break;

        /********************************************************************/
        /*                 Register and register/constant                   */
        /********************************************************************/

        case IF_RRW_SHF:
            code = insCodeMR(ins);
            // Emit the VEX prefix if it exists
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeMRreg(ins, id->idReg1(), size, code);

            // set the W bit
            if (size != EA_1BYTE)
            {
                code |= 1;
            }

            // Emit the REX prefix if it exists
            if (TakesRexWPrefix(ins, size))
            {
                code = AddRexWPrefix(ins, code);
            }

            // Output a size prefix for a 16-bit operand
            if (size == EA_2BYTE)
            {
                dst += emitOutputByte(dst, 0x66);
            }

            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);
            dst += emitOutputWord(dst, code);
            dst += emitOutputByte(dst, emitGetInsSC(id));
            sz = emitSizeOfInsDsc(id);

            // Update GC info.
            assert(!id->idGCref());
            emitGCregDeadUpd(id->idReg1(), dst);
            break;

        case IF_RRD_RRD:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
        case IF_RRW_RRW:
            dst = emitOutputRR(dst, id);
            sz  = SMALL_IDSC_SIZE;
            break;

        case IF_RRD_CNS:
        case IF_RWR_CNS:
        case IF_RRW_CNS:
            dst = emitOutputRI(dst, id);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_RWR_RRD_RRD:
            dst = emitOutputRRR(dst, id);
            sz  = emitSizeOfInsDsc(id);
            break;
        case IF_RWR_RRD_RRD_CNS:
        case IF_RWR_RRD_RRD_RRD:
            dst = emitOutputRRR(dst, id);
            sz  = emitSizeOfInsDsc(id);
            dst += emitOutputByte(dst, emitGetInsSC(id));
            break;

        case IF_RRW_RRW_CNS:
            assert(id->idGCref() == GCT_NONE);

            // Get the 'base' opcode (it's a big one)
            // Also, determine which operand goes where in the ModRM byte.
            regNumber mReg;
            regNumber rReg;
            if (hasCodeMR(ins))
            {
                code = insCodeMR(ins);
                // Emit the VEX prefix if it exists
                code = AddVexPrefixIfNeeded(ins, code, size);
                code = insEncodeMRreg(ins, code);
                mReg = id->idReg1();
                rReg = id->idReg2();
            }
            else if (hasCodeMI(ins))
            {
                code = insCodeMI(ins);

                // Emit the VEX prefix if it exists
                code = AddVexPrefixIfNeeded(ins, code, size);

                assert((code & 0xC000) == 0);
                code |= 0xC000;

                mReg = id->idReg2();

                // The left and right shifts use the same encoding, and are distinguished by the Reg/Opcode field.
                rReg = getSseShiftRegNumber(ins);
            }
            else
            {
                code = insCodeRM(ins);
                // Emit the VEX prefix if it exists
                code = AddVexPrefixIfNeeded(ins, code, size);
                code = insEncodeRMreg(ins, code);
                mReg = id->idReg2();
                rReg = id->idReg1();
            }
            assert(code & 0x00FF0000);

            if (TakesRexWPrefix(ins, size))
            {
                code = AddRexWPrefix(ins, code);
            }

            if (TakesVexPrefix(ins))
            {
                if (IsDstDstSrcAVXInstruction(ins))
                {
                    // Encode source/dest operand reg in 'vvvv' bits in 1's complement form
                    // This code will have to change when we support 3 operands.
                    // For now, we always overload this source with the destination (always reg1).
                    // (Though we will need to handle the few ops that can have the 'vvvv' bits as destination,
                    // e.g. pslldq, when/if we support those instructions with 2 registers.)
                    // (see x64 manual Table 2-9. Instructions with a VEX.vvvv destination)
                    code = insEncodeReg3456(ins, id->idReg1(), size, code);
                }
                else if (IsDstSrcSrcAVXInstruction(ins))
                {
                    // This is a "merge" move instruction.
                    // Encode source operand reg in 'vvvv' bits in 1's complement form
                    code = insEncodeReg3456(ins, id->idReg2(), size, code);
                }
            }

            regcode = (insEncodeReg345(ins, rReg, size, &code) | insEncodeReg012(ins, mReg, size, &code));

            // Output the REX prefix
            dst += emitOutputRexOrVexPrefixIfNeeded(ins, dst, code);

            if (code & 0xFF000000)
            {
                // Output the highest word of the opcode
                dst += emitOutputWord(dst, code >> 16);
                code &= 0x0000FFFF;

                if (Is4ByteSSEInstruction(ins))
                {
                    // Output 3rd byte of the opcode
                    dst += emitOutputByte(dst, code);
                    code &= 0xFF00;
                }
            }
            else if (code & 0x00FF0000)
            {
                dst += emitOutputByte(dst, code >> 16);
                code &= 0x0000FFFF;
            }

            // TODO-XArch-CQ: Right now support 4-byte opcode instructions only
            if ((code & 0xFF00) == 0xC000)
            {
                dst += emitOutputWord(dst, code | (regcode << 8));
            }
            else if ((code & 0xFF) == 0x00)
            {
                // This case happens for some SSE/AVX instructions only
                assert(IsAVXInstruction(ins) || Is4ByteSSEInstruction(ins));

                dst += emitOutputByte(dst, (code >> 8) & 0xFF);
                dst += emitOutputByte(dst, (0xC0 | regcode));
            }
            else
            {
                dst += emitOutputWord(dst, code);
                dst += emitOutputByte(dst, (0xC0 | regcode));
            }

            dst += emitOutputByte(dst, emitGetInsSC(id));
            sz = emitSizeOfInsDsc(id);

            // Kill any GC ref in the destination register if necessary.
            if (!emitInsCanOnlyWriteSSE2OrAVXReg(id))
            {
                emitGCregDeadUpd(id->idReg1(), dst);
            }
            break;

        /********************************************************************/
        /*                      Address mode operand                        */
        /********************************************************************/

        case IF_ARD:
        case IF_AWR:
        case IF_ARW:

            dst = emitCodeWithInstructionSize(dst, emitOutputAM(dst, id, insCodeMR(ins)), &callInstrSize);

            switch (ins)
            {
                case INS_call:

                IND_CALL:
                    // Get hold of the argument count and method handle
                    args = emitGetInsCIargs(id);

                    // Is this a "fat" call descriptor?
                    if (id->idIsLargeCall())
                    {
                        instrDescCGCA* idCall = (instrDescCGCA*)id;

                        gcrefRegs = idCall->idcGcrefRegs;
                        byrefRegs = idCall->idcByrefRegs;
                        VarSetOps::Assign(emitComp, GCvars, idCall->idcGCvars);
                        sz = sizeof(instrDescCGCA);
                    }
                    else
                    {
                        assert(!id->idIsLargeDsp());
                        assert(!id->idIsLargeCns());

                        gcrefRegs = emitDecodeCallGCregs(id);
                        byrefRegs = 0;
                        VarSetOps::AssignNoCopy(emitComp, GCvars, VarSetOps::MakeEmpty(emitComp));
                        sz = sizeof(instrDesc);
                    }

                    recCall = true;

                    goto DONE_CALL;

                default:
                    sz = emitSizeOfInsDsc(id);
                    break;
            }
            break;

        case IF_RRW_ARD_CNS:
        case IF_RWR_ARD_CNS:
            assert(IsSSEOrAVXInstruction(ins));
            emitGetInsAmdCns(id, &cnsVal);
            code = insCodeRM(ins);

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputAM(dst, id, code, &cnsVal);
            }
            else
            {
                code    = AddVexPrefixIfNeeded(ins, code, size);
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputAM(dst, id, code | regcode, &cnsVal);
            }

            sz = emitSizeOfInsDsc(id);
            break;

        case IF_AWR_RRD_CNS:
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            assert(UseVEXEncoding());
            emitGetInsAmdCns(id, &cnsVal);
            code = insCodeMR(ins);
            dst  = emitOutputAM(dst, id, code, &cnsVal);
            sz   = emitSizeOfInsDsc(id);
            break;

        case IF_RRD_ARD:
        case IF_RWR_ARD:
        case IF_RRW_ARD:
        case IF_RWR_RRD_ARD:
        {
            code = insCodeRM(ins);
            if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
            {
                dst = emitOutputAM(dst, id, code);
            }
            else
            {
                code    = AddVexPrefixIfNeeded(ins, code, size);
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputAM(dst, id, code | regcode);
            }
            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_ARD_RRD:
        {
            assert(IsAVX2GatherInstruction(ins));
            code = insCodeRM(ins);
            dst  = emitOutputAM(dst, id, code);
            sz   = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_RRD_ARD_CNS:
        case IF_RWR_RRD_ARD_RRD:
        {
            assert(IsSSEOrAVXInstruction(ins));
            emitGetInsAmdCns(id, &cnsVal);
            code = insCodeRM(ins);
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputAM(dst, id, code, &cnsVal);
            }
            else
            {
                code    = AddVexPrefixIfNeeded(ins, code, size);
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputAM(dst, id, code | regcode, &cnsVal);
            }
            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_ARD_RRD:
        case IF_AWR_RRD:
        case IF_ARW_RRD:
            code    = insCodeMR(ins);
            code    = AddVexPrefixIfNeeded(ins, code, size);
            regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
            dst     = emitOutputAM(dst, id, code | regcode);
            sz      = emitSizeOfInsDsc(id);
            break;

        case IF_AWR_RRD_RRD:
        {
            code = insCodeMR(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            dst  = emitOutputAM(dst, id, code);
            sz   = emitSizeOfInsDsc(id);
            break;
        }

        case IF_ARD_CNS:
        case IF_AWR_CNS:
        case IF_ARW_CNS:
            emitGetInsAmdCns(id, &cnsVal);
            dst = emitOutputAM(dst, id, insCodeMI(ins), &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_ARW_SHF:
            emitGetInsAmdCns(id, &cnsVal);
            dst = emitOutputAM(dst, id, insCodeMR(ins), &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        /********************************************************************/
        /*                      Stack-based operand                         */
        /********************************************************************/

        case IF_SRD:
        case IF_SWR:
        case IF_SRW:

            assert(ins != INS_pop_hide);
            if (ins == INS_pop)
            {
                // The offset in "pop [ESP+xxx]" is relative to the new ESP value
                CLANG_FORMAT_COMMENT_ANCHOR;

#if !FEATURE_FIXED_OUT_ARGS
                emitCurStackLvl -= sizeof(int);
#endif
                dst = emitOutputSV(dst, id, insCodeMR(ins));

#if !FEATURE_FIXED_OUT_ARGS
                emitCurStackLvl += sizeof(int);
#endif
                break;
            }

            dst = emitCodeWithInstructionSize(dst, emitOutputSV(dst, id, insCodeMR(ins)), &callInstrSize);

            if (ins == INS_call)
            {
                goto IND_CALL;
            }

            break;

        case IF_SRD_CNS:
        case IF_SWR_CNS:
        case IF_SRW_CNS:
            emitGetInsCns(id, &cnsVal);
            dst = emitOutputSV(dst, id, insCodeMI(ins), &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_SRW_SHF:
            emitGetInsCns(id, &cnsVal);
            dst = emitOutputSV(dst, id, insCodeMR(ins), &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_SWR_RRD_CNS:
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            assert(UseVEXEncoding());
            emitGetInsAmdCns(id, &cnsVal);
            code = insCodeMR(ins);
            dst  = emitOutputSV(dst, id, insCodeMR(ins), &cnsVal);
            sz   = emitSizeOfInsDsc(id);
            break;

        case IF_RRW_SRD_CNS:
        case IF_RWR_SRD_CNS:
            assert(IsSSEOrAVXInstruction(ins));
            emitGetInsCns(id, &cnsVal);
            code = insCodeRM(ins);

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputSV(dst, id, code, &cnsVal);
            }
            else
            {
                code = AddVexPrefixIfNeeded(ins, code, size);

                // In case of AVX instructions that take 3 operands, encode reg1 as first source.
                // Note that reg1 is both a source and a destination.
                //
                // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
                // now we use the single source as source1 and source2.
                // For this format, moves do not support a third operand, so we only need to handle the binary ops.
                if (IsDstDstSrcAVXInstruction(ins))
                {
                    // encode source operand reg in 'vvvv' bits in 1's complement form
                    code = insEncodeReg3456(ins, id->idReg1(), size, code);
                }

                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputSV(dst, id, code | regcode, &cnsVal);
            }

            sz = emitSizeOfInsDsc(id);
            break;

        case IF_RRD_SRD:
        case IF_RWR_SRD:
        case IF_RRW_SRD:
        {
            code = insCodeRM(ins);

            // 4-byte AVX instructions are special cased inside emitOutputSV
            // since they do not have space to encode ModRM byte.
            if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
            {
                dst = emitOutputSV(dst, id, code);
            }
            else
            {
                code = AddVexPrefixIfNeeded(ins, code, size);

                if (IsDstDstSrcAVXInstruction(ins))
                {
                    // encode source operand reg in 'vvvv' bits in 1's complement form
                    code = insEncodeReg3456(ins, id->idReg1(), size, code);
                }

                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputSV(dst, id, code | regcode);
            }

            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_RRD_SRD:
        {
            // This should only be called on AVX instructions
            assert(IsAVXInstruction(ins));

            code = insCodeRM(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeReg3456(ins, id->idReg2(), size,
                                    code); // encode source operand reg in 'vvvv' bits in 1's complement form

            // 4-byte AVX instructions are special cased inside emitOutputSV
            // since they do not have space to encode ModRM byte.
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputSV(dst, id, code);
            }
            else
            {
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputSV(dst, id, code | regcode);
            }
            break;
        }

        case IF_RWR_RRD_SRD_CNS:
        case IF_RWR_RRD_SRD_RRD:
        {
            // This should only be called on AVX instructions
            assert(IsAVXInstruction(ins));
            emitGetInsCns(id, &cnsVal);

            code = insCodeRM(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeReg3456(ins, id->idReg2(), size,
                                    code); // encode source operand reg in 'vvvv' bits in 1's complement form

            // 4-byte AVX instructions are special cased inside emitOutputSV
            // since they do not have space to encode ModRM byte.
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputSV(dst, id, code, &cnsVal);
            }
            else
            {
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputSV(dst, id, code | regcode, &cnsVal);
            }

            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_SRD_RRD:
        case IF_SWR_RRD:
        case IF_SRW_RRD:
            code = insCodeMR(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);

            // In case of AVX instructions that take 3 operands, encode reg1 as first source.
            // Note that reg1 is both a source and a destination.
            //
            // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
            // now we use the single source as source1 and source2.
            // For this format, moves do not support a third operand, so we only need to handle the binary ops.
            if (IsDstDstSrcAVXInstruction(ins))
            {
                // encode source operand reg in 'vvvv' bits in 1's complement form
                code = insEncodeReg3456(ins, id->idReg1(), size, code);
            }

            regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
            dst     = emitOutputSV(dst, id, code | regcode);
            break;

        /********************************************************************/
        /*                    Direct memory address                         */
        /********************************************************************/

        case IF_MRD:
        case IF_MRW:
        case IF_MWR:

            noway_assert(ins != INS_call);
            dst = emitOutputCV(dst, id, insCodeMR(ins) | 0x0500);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_MRD_OFF:
            dst = emitOutputCV(dst, id, insCodeMI(ins));
            break;

        case IF_RRW_MRD_CNS:
        case IF_RWR_MRD_CNS:
            assert(IsSSEOrAVXInstruction(ins));
            emitGetInsDcmCns(id, &cnsVal);
            code = insCodeRM(ins);

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputCV(dst, id, code, &cnsVal);
            }
            else
            {
                code = AddVexPrefixIfNeeded(ins, code, size);

                // In case of AVX instructions that take 3 operands, encode reg1 as first source.
                // Note that reg1 is both a source and a destination.
                //
                // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
                // now we use the single source as source1 and source2.
                // For this format, moves do not support a third operand, so we only need to handle the binary ops.
                if (IsDstDstSrcAVXInstruction(ins))
                {
                    // encode source operand reg in 'vvvv' bits in 1's complement form
                    code = insEncodeReg3456(ins, id->idReg1(), size, code);
                }

                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputCV(dst, id, code | regcode | 0x0500, &cnsVal);
            }

            sz = emitSizeOfInsDsc(id);
            break;

        case IF_MWR_RRD_CNS:
            assert(ins == INS_vextracti128 || ins == INS_vextractf128);
            assert(UseVEXEncoding());
            emitGetInsDcmCns(id, &cnsVal);
            code = insCodeMR(ins);
            // only AVX2 vextracti128 and AVX vextractf128 can reach this path,
            // they do not need VEX.vvvv to encode the register operand
            dst = emitOutputCV(dst, id, code, &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_RRD_MRD:
        case IF_RWR_MRD:
        case IF_RRW_MRD:
        {
            code = insCodeRM(ins);

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins) || (ins == INS_crc32))
            {
                dst = emitOutputCV(dst, id, code);
            }
            else
            {
                code = AddVexPrefixIfNeeded(ins, code, size);

                if (IsDstDstSrcAVXInstruction(ins))
                {
                    // encode source operand reg in 'vvvv' bits in 1's complement form
                    code = insEncodeReg3456(ins, id->idReg1(), size, code);
                }

                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputCV(dst, id, code | regcode | 0x0500);
            }

            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_RRD_MRD:
        {
            // This should only be called on AVX instructions
            assert(IsAVXInstruction(ins));

            code = insCodeRM(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeReg3456(ins, id->idReg2(), size,
                                    code); // encode source operand reg in 'vvvv' bits in 1's complement form

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputCV(dst, id, code);
            }
            else
            {
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputCV(dst, id, code | regcode | 0x0500);
            }
            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_RRD_MRD_CNS:
        case IF_RWR_RRD_MRD_RRD:
        {
            // This should only be called on AVX instructions
            assert(IsAVXInstruction(ins));
            emitGetInsCns(id, &cnsVal);

            code = insCodeRM(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);
            code = insEncodeReg3456(ins, id->idReg2(), size,
                                    code); // encode source operand reg in 'vvvv' bits in 1's complement form

            // Special case 4-byte AVX instructions
            if (EncodedBySSE38orSSE3A(ins))
            {
                dst = emitOutputCV(dst, id, code, &cnsVal);
            }
            else
            {
                regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
                dst     = emitOutputCV(dst, id, code | regcode | 0x0500, &cnsVal);
            }
            sz = emitSizeOfInsDsc(id);
            break;
        }

        case IF_RWR_MRD_OFF:
            code = insCode(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);

            // In case of AVX instructions that take 3 operands, encode reg1 as first source.
            // Note that reg1 is both a source and a destination.
            //
            // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
            // now we use the single source as source1 and source2.
            // For this format, moves do not support a third operand, so we only need to handle the binary ops.
            if (IsDstDstSrcAVXInstruction(ins))
            {
                // encode source operand reg in 'vvvv' bits in 1's complement form
                code = insEncodeReg3456(ins, id->idReg1(), size, code);
            }

            regcode = insEncodeReg012(id->idIns(), id->idReg1(), size, &code);
            dst     = emitOutputCV(dst, id, code | 0x30 | regcode);
            sz      = emitSizeOfInsDsc(id);
            break;

        case IF_MRD_RRD:
        case IF_MWR_RRD:
        case IF_MRW_RRD:
            code = insCodeMR(ins);
            code = AddVexPrefixIfNeeded(ins, code, size);

            // In case of AVX instructions that take 3 operands, encode reg1 as first source.
            // Note that reg1 is both a source and a destination.
            //
            // TODO-XArch-CQ: Eventually we need to support 3 operand instruction formats. For
            // now we use the single source as source1 and source2.
            // For this format, moves do not support a third operand, so we only need to handle the binary ops.
            if (IsDstDstSrcAVXInstruction(ins))
            {
                // encode source operand reg in 'vvvv' bits in 1's complement form
                code = insEncodeReg3456(ins, id->idReg1(), size, code);
            }

            regcode = (insEncodeReg345(ins, id->idReg1(), size, &code) << 8);
            dst     = emitOutputCV(dst, id, code | regcode | 0x0500);
            sz      = emitSizeOfInsDsc(id);
            break;

        case IF_MRD_CNS:
        case IF_MWR_CNS:
        case IF_MRW_CNS:
            emitGetInsDcmCns(id, &cnsVal);
            dst = emitOutputCV(dst, id, insCodeMI(ins) | 0x0500, &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        case IF_MRW_SHF:
            emitGetInsDcmCns(id, &cnsVal);
            dst = emitOutputCV(dst, id, insCodeMR(ins) | 0x0500, &cnsVal);
            sz  = emitSizeOfInsDsc(id);
            break;

        /********************************************************************/
        /*                            oops                                  */
        /********************************************************************/

        default:

#ifdef DEBUG
            printf("unexpected format %s\n", emitIfName(id->idInsFmt()));
            assert(!"don't know how to encode this instruction");
#endif
            break;
    }

    // Make sure we set the instruction descriptor size correctly
    assert(sz == emitSizeOfInsDsc(id));

#if !FEATURE_FIXED_OUT_ARGS
    bool updateStackLevel = !emitIGisInProlog(ig) && !emitIGisInEpilog(ig);

#if defined(FEATURE_EH_FUNCLETS)
    updateStackLevel = updateStackLevel && !emitIGisInFuncletProlog(ig) && !emitIGisInFuncletEpilog(ig);
#endif // FEATURE_EH_FUNCLETS

    // Make sure we keep the current stack level up to date
    if (updateStackLevel)
    {
        switch (ins)
        {
            case INS_push:
                // Please note: {INS_push_hide,IF_LABEL} is used to push the address of the
                // finally block for calling it locally for an op_leave.
                emitStackPush(dst, id->idGCref());
                break;

            case INS_pop:
                emitStackPop(dst, false, /*callInstrSize*/ 0, 1);
                break;

            case INS_sub:
                // Check for "sub ESP, icon"
                if (ins == INS_sub && id->idInsFmt() == IF_RRW_CNS && id->idReg1() == REG_ESP)
                {
                    assert((size_t)emitGetInsSC(id) < 0x00000000FFFFFFFFLL);
                    emitStackPushN(dst, (unsigned)(emitGetInsSC(id) / TARGET_POINTER_SIZE));
                }
                break;

            case INS_add:
                // Check for "add ESP, icon"
                if (ins == INS_add && id->idInsFmt() == IF_RRW_CNS && id->idReg1() == REG_ESP)
                {
                    assert((size_t)emitGetInsSC(id) < 0x00000000FFFFFFFFLL);
                    emitStackPop(dst, /*isCall*/ false, /*callInstrSize*/ 0,
                                 (unsigned)(emitGetInsSC(id) / TARGET_POINTER_SIZE));
                }
                break;

            default:
                break;
        }
    }

#endif // !FEATURE_FIXED_OUT_ARGS

    assert((int)emitCurStackLvl >= 0);

    // Only epilog "instructions" and some pseudo-instrs
    // are allowed not to generate any code

    assert(*dp != dst || emitInstHasNoCode(ins));

#ifdef DEBUG
    if (emitComp->opts.disAsm || emitComp->verbose)
    {
        emitDispIns(id, false, dspOffs, true, emitCurCodeOffs(*dp), *dp, (dst - *dp));
    }
#endif

#if FEATURE_LOOP_ALIGN
    // Only compensate over-estimated instructions if emitCurIG is before
    // the last IG that needs alignment.
    if (emitCurIG->igNum <= emitLastAlignedIgNum)
    {
        int diff = id->idCodeSize() - ((UNATIVE_OFFSET)(dst - *dp));
        assert(diff >= 0);
        if (diff != 0)
        {

#ifdef DEBUG
            // should never over-estimate align instruction
            assert(id->idIns() != INS_align);
            JITDUMP("Added over-estimation compensation: %d\n", diff);

            if (emitComp->opts.disAsm)
            {
                emitDispInsAddr(dst);
                printf("\t\t  ;; NOP compensation instructions of %d bytes.\n", diff);
            }
#endif

            BYTE* dstRW = dst + writeableOffset;
            dstRW       = emitOutputNOP(dstRW, diff);
            dst         = dstRW - writeableOffset;
        }
        assert((id->idCodeSize() - ((UNATIVE_OFFSET)(dst - *dp))) == 0);
    }
#endif

#ifdef DEBUG
    if (emitComp->compDebugBreak)
    {
        // set JitEmitPrintRefRegs=1 will print out emitThisGCrefRegs and emitThisByrefRegs
        // at the beginning of this method.
        if (JitConfig.JitEmitPrintRefRegs() != 0)
        {
            printf("Before emitOutputInstr for id->idDebugOnlyInfo()->idNum=0x%02x\n", id->idDebugOnlyInfo()->idNum);
            printf("  emitThisGCrefRegs(0x%p)=", emitComp->dspPtr(&emitThisGCrefRegs));
            printRegMaskInt(emitThisGCrefRegs);
            emitDispRegSet(emitThisGCrefRegs);
            printf("\n");
            printf("  emitThisByrefRegs(0x%p)=", emitComp->dspPtr(&emitThisByrefRegs));
            printRegMaskInt(emitThisByrefRegs);
            emitDispRegSet(emitThisByrefRegs);
            printf("\n");
        }

        // For example, set JitBreakEmitOutputInstr=a6 will break when this method is called for
        // emitting instruction a6, (i.e. IN00a6 in jitdump).
        if ((unsigned)JitConfig.JitBreakEmitOutputInstr() == id->idDebugOnlyInfo()->idNum)
        {
            assert(!"JitBreakEmitOutputInstr reached");
        }
    }
#endif

    *dp = dst;

#ifdef DEBUG
    if (ins == INS_mulEAX || ins == INS_imulEAX)
    {
        // INS_mulEAX has implicit target of Edx:Eax. Make sure
        // that we detected this cleared its GC-status.

        assert(((RBM_EAX | RBM_EDX) & (emitThisGCrefRegs | emitThisByrefRegs)) == 0);
    }

    if (instrIs3opImul(ins))
    {
        // The target of the 3-operand imul is implicitly encoded. Make sure
        // that we detected the implicit register and cleared its GC-status.

        regMaskTP regMask = genRegMask(inst3opImulReg(ins));
        assert((regMask & (emitThisGCrefRegs | emitThisByrefRegs)) == 0);
    }

    // Output any delta in GC info.
    if (EMIT_GC_VERBOSE || emitComp->opts.disasmWithGC)
    {
        emitDispGCInfoDelta();
    }
#endif

    return sz;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

emitter::insFormat emitter::getMemoryOperation(instrDesc* id)
{
    insFormat   result = IF_NONE;
    instruction ins    = id->idIns();
    insFormat   insFmt = id->idInsFmt();

    if (ins == INS_lea)
    {
        // an INS_lea instruction doesn't actually read memory
        insFmt = IF_NONE;
    }

    switch (insFmt)
    {
        case IF_NONE:
        case IF_LABEL:
        case IF_RWR_LABEL:
        case IF_METHOD:
        case IF_CNS:

        case IF_RRD:
        case IF_RWR:
        case IF_RRW:
        case IF_RRD_CNS:
        case IF_RWR_CNS:
        case IF_RRW_CNS:
        case IF_RRW_SHF:
        case IF_RRD_RRD:
        case IF_RWR_RRD:
        case IF_RRW_RRD:
        case IF_RRW_RRW:
        case IF_RRW_RRW_CNS:
        case IF_RWR_RRD_RRD:
        case IF_RWR_RRD_RRD_CNS:
        case IF_RWR_RRD_RRD_RRD:
            // none, or register only
            result = IF_NONE;
            break;

        case IF_ARD:
        case IF_RRD_ARD:
        case IF_RWR_ARD:
        case IF_RRW_ARD:
        case IF_RWR_ARD_CNS:
        case IF_RWR_RRD_ARD:
        case IF_RRW_ARD_CNS:
        case IF_RWR_ARD_RRD:
        case IF_RWR_RRD_ARD_CNS:
        case IF_RWR_RRD_ARD_RRD:
        case IF_ARD_CNS:
        case IF_ARD_RRD:
            // Address [reg+reg*scale+cns] - read
            result = IF_ARD;
            break;

        case IF_AWR:
        case IF_AWR_RRD:
        case IF_AWR_CNS:
        case IF_AWR_RRD_CNS:
        case IF_AWR_RRD_RRD:
            // Address [reg+reg*scale+cns] - write
            result = IF_AWR;
            break;

        case IF_ARW:
        case IF_ARW_RRD:
        case IF_ARW_CNS:
        case IF_ARW_SHF:
            // Address [reg+reg*scale+cns] - read and write
            result = IF_ARW;
            break;

        case IF_MRD:
        case IF_MRD_CNS:
        case IF_MRD_OFF:
        case IF_MRD_RRD:
        case IF_RRD_MRD:
        case IF_RRW_MRD:
        case IF_RWR_MRD:
        case IF_RWR_MRD_CNS:
        case IF_RWR_MRD_OFF:
        case IF_RWR_RRD_MRD:
        case IF_RRW_MRD_CNS:
        case IF_RWR_RRD_MRD_CNS:
        case IF_RWR_RRD_MRD_RRD:
        case IF_METHPTR:
            // Address [cns] - read
            result = IF_MRD;
            break;

        case IF_MWR:
        case IF_MWR_CNS:
        case IF_MWR_RRD:
        case IF_MWR_RRD_CNS:
            // Address [cns] - write
            result = IF_MWR;
            break;

        case IF_MRW:
        case IF_MRW_CNS:
        case IF_MRW_RRD:
        case IF_MRW_SHF:
            // Address [cns] - read and write
            result = IF_MWR;
            break;

        case IF_SRD:
        case IF_SRD_CNS:
        case IF_SRD_RRD:

        case IF_RRD_SRD:
        case IF_RRW_SRD:
        case IF_RWR_SRD:
        case IF_RWR_SRD_CNS:
        case IF_RWR_RRD_SRD:
        case IF_RRW_SRD_CNS:
        case IF_RWR_RRD_SRD_CNS:
        case IF_RWR_RRD_SRD_RRD:
            // Stack [RSP] - read
            result = IF_SRD;
            break;

        case IF_SWR:
        case IF_SWR_CNS:
        case IF_SWR_RRD:
        case IF_SWR_RRD_CNS:
        case IF_SWR_LABEL:
            // Stack [RSP] - write
            result = IF_SWR;
            break;

        case IF_SRW:
        case IF_SRW_CNS:
        case IF_SRW_RRD:
        case IF_SRW_SHF:
            // Stack [RSP] - read and write
            result = IF_SWR;
            break;

        default:
            result = IF_NONE;
            break;
    }
    return result;
}

#if defined(DEBUG) || defined(LATE_DISASM)

//----------------------------------------------------------------------------------------
// getInsExecutionCharacteristics:
//    Returns the current instruction execution characteristics
//
// Arguments:
//    id  - The current instruction descriptor to be evaluated
//
// Return Value:
//    A struct containing the current instruction execution characteristics
//
// Notes:
//    The instruction latencies and throughput values returned by this function
//    are for the Intel Skylake-X processor and are from either:
//      1.  Agner.org - https://www.agner.org/optimize/instruction_tables.pdf
//      2.  uops.info - https://uops.info/table.html
//
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    insExecutionCharacteristics result;
    instruction                 ins    = id->idIns();
    insFormat                   insFmt = id->idInsFmt();
    emitAttr                    opSize = id->idOpSize();
    insFormat                   memFmt = getMemoryOperation(id);
    unsigned                    memAccessKind;

    result.insThroughput = PERFSCORE_THROUGHPUT_ILLEGAL;
    result.insLatency    = PERFSCORE_LATENCY_ILLEGAL;

    // Model the memory latency
    switch (memFmt)
    {
        // Model a read from stack location, possible def to use latency from L0 cache
        case IF_SRD:
            result.insLatency = PERFSCORE_LATENCY_RD_STACK;
            memAccessKind     = PERFSCORE_MEMORY_READ;
            break;

        case IF_SWR:
            result.insLatency = PERFSCORE_LATENCY_WR_STACK;
            memAccessKind     = PERFSCORE_MEMORY_WRITE;
            break;

        case IF_SRW:
            result.insLatency = PERFSCORE_LATENCY_RD_WR_STACK;
            memAccessKind     = PERFSCORE_MEMORY_READ_WRITE;
            break;

        // Model a read from a constant location, possible def to use latency from L0 cache
        case IF_MRD:
            result.insLatency = PERFSCORE_LATENCY_RD_CONST_ADDR;
            memAccessKind     = PERFSCORE_MEMORY_READ;
            break;

        case IF_MWR:
            result.insLatency = PERFSCORE_LATENCY_WR_CONST_ADDR;
            memAccessKind     = PERFSCORE_MEMORY_WRITE;
            break;

        case IF_MRW:
            result.insLatency = PERFSCORE_LATENCY_RD_WR_CONST_ADDR;
            memAccessKind     = PERFSCORE_MEMORY_READ_WRITE;
            break;

        // Model a read from memory location, possible def to use latency from L0 or L1 cache
        case IF_ARD:
            result.insLatency = PERFSCORE_LATENCY_RD_GENERAL;
            memAccessKind     = PERFSCORE_MEMORY_READ;
            break;

        case IF_AWR:
            result.insLatency = PERFSCORE_LATENCY_WR_GENERAL;
            memAccessKind     = PERFSCORE_MEMORY_WRITE;
            break;

        case IF_ARW:
            result.insLatency = PERFSCORE_LATENCY_RD_WR_GENERAL;
            memAccessKind     = PERFSCORE_MEMORY_READ_WRITE;
            break;

        case IF_NONE:
            result.insLatency = PERFSCORE_LATENCY_ZERO;
            memAccessKind     = PERFSCORE_MEMORY_NONE;
            break;

        default:
            assert(!"Unhandled insFmt for switch (memFmt)");
            result.insLatency = PERFSCORE_LATENCY_ZERO;
            memAccessKind     = PERFSCORE_MEMORY_NONE;
            break;
    }
    result.insMemoryAccessKind = memAccessKind;

    switch (ins)
    {
        case INS_align:
#if FEATURE_LOOP_ALIGN
            if ((id->idCodeSize() == 0) || ((instrDescAlign*)id)->isPlacedAfterJmp)
            {
                // Either we're not going to generate 'align' instruction, or the 'align'
                // instruction is placed immediately after unconditional jmp.
                // In both cases, don't count for PerfScore.

                result.insThroughput = PERFSCORE_THROUGHPUT_ZERO;
                result.insLatency    = PERFSCORE_LATENCY_ZERO;
                break;
            }
#endif
            FALLTHROUGH;

        case INS_nop:
        case INS_int3:
            assert(memFmt == IF_NONE);
            result.insThroughput = PERFSCORE_THROUGHPUT_4X;
            result.insLatency    = PERFSCORE_LATENCY_ZERO;
            break;

        case INS_push:
        case INS_push_hide:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            if (insFmt == IF_RRD) // push  reg
            {
                // For pushes (stack writes) we assume that the full latency will be covered
                result.insLatency = PERFSCORE_LATENCY_ZERO;
            }
            break;

        case INS_pop:
        case INS_pop_hide:
            if (insFmt == IF_RWR) // pop   reg
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                // For pops (stack reads) we assume that the full latency will be covered
                result.insLatency = PERFSCORE_LATENCY_ZERO;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_inc:
        case INS_dec:
        case INS_neg:
        case INS_not:
            if (memFmt == IF_NONE)
            {
                // ins   reg
                result.insThroughput = PERFSCORE_THROUGHPUT_4X;
                result.insLatency    = PERFSCORE_LATENCY_1C;
            }
            else
            {
                // ins   mem
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                // no additional R/W latency
            }
            break;

#ifdef TARGET_AMD64
        case INS_movsxd:
#endif
        case INS_mov:
        case INS_movsx:
        case INS_movzx:
        case INS_cwde:
        case INS_cmp:
        case INS_test:
            if (memFmt == IF_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_4X;
            }
            else if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                if (ins == INS_cmp || ins == INS_test)
                {
                    result.insLatency += PERFSCORE_LATENCY_1C;
                }
                else if (ins == INS_movsx
#ifdef TARGET_AMD64
                         || ins == INS_movsxd
#endif
                         )
                {
                    result.insLatency += PERFSCORE_LATENCY_2C;
                }
            }
            else // writes
            {
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
                assert(ins == INS_mov);
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_adc:
        case INS_sbb:
            if (memAccessKind != PERFSCORE_MEMORY_READ_WRITE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += PERFSCORE_LATENCY_1C;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                // no additional R/W latency
            }
            break;

        case INS_add:
        case INS_sub:
        case INS_and:
        case INS_or:
        case INS_xor:
            if (memFmt == IF_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_4X;
                result.insLatency    = PERFSCORE_LATENCY_1C;
            }
            else if (memAccessKind == PERFSCORE_MEMORY_READ_WRITE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                // no additional R/W latency
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += PERFSCORE_LATENCY_1C;
            }
            break;

        case INS_lea:
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_2X; // one or two components
            result.insLatency    = PERFSCORE_LATENCY_1C;

            if (insFmt == IF_RWR_LABEL)
            {
                // RIP relative addressing
                //
                // - throughput is only 1 per cycle
                //
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            else if (insFmt != IF_RWR_SRD)
            {
                if (id->idAddr()->iiaAddrMode.amIndxReg != REG_NA)
                {
                    regNumber baseReg = id->idAddr()->iiaAddrMode.amBaseReg;
                    if (baseReg != REG_NA)
                    {
                        ssize_t dsp = emitGetInsAmdAny(id);

                        if ((dsp != 0) || baseRegisterRequiresDisplacement(baseReg))
                        {
                            // three components
                            //
                            // - throughput is only 1 per cycle
                            //
                            result.insThroughput = PERFSCORE_THROUGHPUT_1C;

                            if (baseRegisterRequiresDisplacement(baseReg) || id->idIsDspReloc())
                            {
                                // Increased Latency for these cases
                                //  - see https://reviews.llvm.org/D32277
                                //
                                result.insLatency = PERFSCORE_LATENCY_3C;
                            }
                        }
                    }
                }
            }

            break;

        case INS_imul_AX:
        case INS_imul_BX:
        case INS_imul_CX:
        case INS_imul_DX:
        case INS_imul_BP:
        case INS_imul_SI:
        case INS_imul_DI:
#ifdef TARGET_AMD64
        case INS_imul_08:
        case INS_imul_09:
        case INS_imul_10:
        case INS_imul_11:
        case INS_imul_12:
        case INS_imul_13:
        case INS_imul_14:
        case INS_imul_15:
#endif // TARGET_AMD64
        case INS_imul:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_3C;
            break;

        case INS_mulEAX:
        case INS_imulEAX:
            // uops.info: mul/imul rdx:rax,reg latency is 3 only if the low half of the result is needed, but in that
            // case codegen uses imul reg,reg instruction form (except for unsigned overflow checks, which are rare)
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_div:
            // The integer divide instructions have long latencies
            if (opSize == EA_8BYTE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_52C;
                result.insLatency    = PERFSCORE_LATENCY_62C;
            }
            else
            {
                assert(opSize == EA_4BYTE);
                result.insThroughput = PERFSCORE_THROUGHPUT_6C;
                result.insLatency    = PERFSCORE_LATENCY_26C;
            }
            break;

        case INS_idiv:
            // The integer divide instructions have long latenies
            if (opSize == EA_8BYTE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_57C;
                result.insLatency    = PERFSCORE_LATENCY_69C;
            }
            else
            {
                assert(opSize == EA_4BYTE);
                result.insThroughput = PERFSCORE_THROUGHPUT_6C;
                result.insLatency    = PERFSCORE_LATENCY_26C;
            }
            break;

        case INS_cdq:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_1C;
            break;

        case INS_shl:
        case INS_shr:
        case INS_sar:
        case INS_ror:
        case INS_rol:
            switch (insFmt)
            {
                case IF_RRW_CNS:
                    // ins   reg, cns
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    result.insLatency    = PERFSCORE_LATENCY_1C;
                    break;

                case IF_MRW_CNS:
                case IF_SRW_CNS:
                case IF_ARW_CNS:
                    // ins   [mem], cns
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency += PERFSCORE_LATENCY_1C;
                    break;

                case IF_RRW:
                    // ins   reg, cl
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    result.insLatency    = PERFSCORE_LATENCY_2C;
                    break;

                case IF_MRW:
                case IF_SRW:
                case IF_ARW:
                    // ins   [mem], cl
                    result.insThroughput = PERFSCORE_THROUGHPUT_4C;
                    result.insLatency += PERFSCORE_LATENCY_2C;
                    break;

                default:
                    // unhandled instruction insFmt combination
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case INS_shl_1:
        case INS_shr_1:
        case INS_sar_1:
            result.insLatency += PERFSCORE_LATENCY_1C;
            switch (insFmt)
            {
                case IF_RRW:
                    // ins   reg, 1
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                case IF_MRW:
                case IF_SRW:
                case IF_ARW:
                    // ins   [mem], 1
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                default:
                    // unhandled instruction insFmt combination
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case INS_ror_1:
        case INS_rol_1:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_1C;
            break;

        case INS_shl_N:
        case INS_shr_N:
        case INS_sar_N:
        case INS_ror_N:
        case INS_rol_N:
            result.insLatency += PERFSCORE_LATENCY_1C;
            switch (insFmt)
            {
                case IF_RRW_SHF:
                    // ins   reg, cns
                    result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                    break;

                case IF_MRW_SHF:
                case IF_SRW_SHF:
                case IF_ARW_SHF:
                    // ins   [mem], cns
                    result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                    break;

                default:
                    // unhandled instruction insFmt combination
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case INS_rcr:
        case INS_rcl:
            result.insThroughput = PERFSCORE_THROUGHPUT_6C;
            result.insLatency += PERFSCORE_LATENCY_6C;
            break;

        case INS_rcr_1:
        case INS_rcl_1:
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_2C;
            break;

        case INS_shld:
        case INS_shrd:
            result.insLatency += PERFSCORE_LATENCY_3C;
            if (insFmt == IF_RRW_RRW_CNS)
            {
                // ins   reg, reg, cns
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            else
            {
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE); // _SHF form never emitted
                result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            }
            break;

        case INS_bt:
            result.insLatency += PERFSCORE_LATENCY_1C;
            if ((insFmt == IF_RRD_RRD) || (insFmt == IF_RRD_CNS))
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_seto:
        case INS_setno:
        case INS_setb:
        case INS_setae:
        case INS_sete:
        case INS_setne:
        case INS_setbe:
        case INS_seta:
        case INS_sets:
        case INS_setns:
        case INS_setp:
        case INS_setnp:
        case INS_setl:
        case INS_setge:
        case INS_setle:
        case INS_setg:
            result.insLatency += PERFSCORE_LATENCY_1C;
            if (insFmt == IF_RRD)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_jo:
        case INS_jno:
        case INS_jb:
        case INS_jae:
        case INS_je:
        case INS_jne:
        case INS_jbe:
        case INS_ja:
        case INS_js:
        case INS_jns:
        case INS_jp:
        case INS_jnp:
        case INS_jl:
        case INS_jge:
        case INS_jle:
        case INS_jg:
            // conditional branch
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_BRANCH_COND;
            break;

        case INS_jmp:
        case INS_l_jmp:
            // branch to a constant address
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_BRANCH_DIRECT;
            break;

        case INS_tail_i_jmp:
        case INS_i_jmp:
            // branch to register
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_BRANCH_INDIRECT;
            break;

        case INS_call:
            // uops.info
            result.insLatency = PERFSCORE_LATENCY_ZERO;
            switch (insFmt)
            {
                case IF_LABEL:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case IF_METHOD:
                    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                    break;

                case IF_METHPTR:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    break;

                case IF_SRD:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    break;

                case IF_ARD:
                    result.insThroughput = PERFSCORE_THROUGHPUT_3C;
                    break;

                default:
                    // unhandled instruction, insFmt combination
                    perfScoreUnhandledInstruction(id, &result);
                    break;
            }
            break;

        case INS_ret:
            if (insFmt == IF_CNS)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            }
            else
            {
                assert(insFmt == IF_NONE);
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_lock:
            result.insThroughput = PERFSCORE_THROUGHPUT_13C;
            break;

        case INS_xadd:
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_1C;
            break;

        case INS_cmpxchg:
            result.insThroughput = PERFSCORE_THROUGHPUT_5C;
            break;

        case INS_xchg:
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            if (memFmt == IF_NONE)
            {
                result.insLatency = PERFSCORE_LATENCY_1C;
            }
            else
            {
                result.insLatency = PERFSCORE_LATENCY_23C;
            }
            break;

#ifdef TARGET_X86
        case INS_fld:
        case INS_fstp:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                result.insLatency = PERFSCORE_LATENCY_1C;
            }
            break;
#endif // TARGET_X86

#ifdef TARGET_AMD64
        case INS_movsq:
        case INS_stosq:
#endif // TARGET_AMD64
        case INS_movsd:
        case INS_stosd:
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            break;

#ifdef TARGET_AMD64
        case INS_r_movsq:
        case INS_r_stosq:
#endif // TARGET_AMD64
        case INS_r_movsd:
        case INS_r_movsb:
        case INS_r_stosd:
        case INS_r_stosb:
            // Actually variable sized: rep stosd, used to zero frame slots
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_25C;
            break;

        case INS_movd:
        case INS_movq: // only MOVQ xmm, xmm is different (emitted by Sse2.MoveScalar, should use MOVDQU instead)
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                // movd   r32, xmm   or  xmm, r32
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency    = PERFSCORE_LATENCY_3C;
            }
            else if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                // movd   xmm, m32
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += PERFSCORE_LATENCY_2C;
            }
            else
            {
                // movd   m32, xmm
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency += PERFSCORE_LATENCY_2C;
            }
            break;

        case INS_movdqa:
        case INS_movdqu:
        case INS_movaps:
        case INS_movups:
        case INS_movapd:
        case INS_movupd:
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                // ins   reg, reg
                result.insThroughput = PERFSCORE_THROUGHPUT_4X;
                result.insLatency    = PERFSCORE_LATENCY_ZERO;
            }
            else if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                // ins   reg, mem
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_2C;
            }
            else
            {
                // ins   mem, reg
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency += PERFSCORE_LATENCY_2C;
            }
            break;

        case INS_movhps:
        case INS_movhpd:
        case INS_movlps:
        case INS_movlpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                result.insLatency += PERFSCORE_LATENCY_3C;
            }
            else
            {
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
                result.insLatency += PERFSCORE_LATENCY_2C;
            }
            break;

        case INS_movhlps:
        case INS_movlhps:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_1C;
            break;

        case INS_movntdq:
        case INS_movnti:
        case INS_movntps:
        case INS_movntpd:
            assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_400C; // Intel microcode issue with these instuctions
            break;

        case INS_maskmovdqu:
            result.insThroughput = PERFSCORE_THROUGHPUT_6C;
            result.insLatency    = PERFSCORE_LATENCY_400C; // Intel microcode issue with these instuctions
            break;

        case INS_movntdqa:
            assert(memAccessKind == PERFSCORE_MEMORY_READ);
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_2C;
            break;

        case INS_vzeroupper:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            // insLatency is zero and is set when we Model the memory latency
            break;

        case INS_movss:
        case INS_movsdsse2:
        case INS_movddup:
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency    = PERFSCORE_LATENCY_1C;
            }
            else if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_2C;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency += PERFSCORE_LATENCY_2C;
            }
            break;

        case INS_lddqu:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_2C;
            break;

        case INS_comiss:
        case INS_comisd:
        case INS_ucomiss:
        case INS_ucomisd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_3C;
            break;

        case INS_addsd:
        case INS_addss:
        case INS_addpd:
        case INS_addps:
        case INS_subsd:
        case INS_subss:
        case INS_subpd:
        case INS_subps:
        case INS_cvttps2dq:
        case INS_cvtps2dq:
        case INS_cvtdq2ps:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_haddps:
        case INS_haddpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency += PERFSCORE_LATENCY_6C;
            break;

        case INS_mulss:
        case INS_mulsd:
        case INS_mulps:
        case INS_mulpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_divss:
        case INS_divps:
            result.insThroughput = PERFSCORE_THROUGHPUT_3C;
            result.insLatency += PERFSCORE_LATENCY_11C;
            break;

        case INS_divsd:
        case INS_divpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_4C;
            result.insLatency += PERFSCORE_LATENCY_13C;
            break;

        case INS_sqrtss:
        case INS_sqrtps:
            result.insThroughput = PERFSCORE_THROUGHPUT_3C;
            result.insLatency += PERFSCORE_LATENCY_12C;
            break;

        case INS_sqrtsd:
        case INS_sqrtpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_4C;
            result.insLatency += PERFSCORE_LATENCY_13C;
            break;

        case INS_rcpps:
        case INS_rcpss:
        case INS_rsqrtss:
        case INS_rsqrtps:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_roundpd:
        case INS_roundps:
        case INS_roundsd:
        case INS_roundss:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_8C;
            break;

        case INS_cvttsd2si:
        case INS_cvtsd2si:
        case INS_cvtsi2sd:
        case INS_cvtsi2ss:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_7C;
            break;

        case INS_cvttss2si:
        case INS_cvtss2si:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += opSize == EA_8BYTE ? PERFSCORE_LATENCY_8C : PERFSCORE_LATENCY_7C;
            break;

        case INS_cvtss2sd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_5C;
            break;

        case INS_paddb:
        case INS_psubb:
        case INS_paddw:
        case INS_psubw:
        case INS_paddd:
        case INS_psubd:
        case INS_paddq:
        case INS_psubq:
        case INS_paddsb:
        case INS_psubsb:
        case INS_paddsw:
        case INS_psubsw:
        case INS_paddusb:
        case INS_psubusb:
        case INS_paddusw:
        case INS_psubusw:
        case INS_pand:
        case INS_pandn:
        case INS_por:
        case INS_pxor:
        case INS_andpd:
        case INS_andps:
        case INS_andnpd:
        case INS_andnps:
        case INS_orpd:
        case INS_orps:
        case INS_xorpd:
        case INS_xorps:
        case INS_blendps:
        case INS_blendpd:
        case INS_vpblendd:
            result.insLatency += PERFSCORE_LATENCY_1C;
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_3X;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            }
            break;

        case INS_andn:
        case INS_pcmpeqb:
        case INS_pcmpeqw:
        case INS_pcmpeqd:
        case INS_pcmpeqq:
        case INS_pcmpgtb:
        case INS_pcmpgtw:
        case INS_pcmpgtd:
        case INS_pavgb:
        case INS_pavgw:
        case INS_pminub:
        case INS_pminsb:
        case INS_pminuw:
        case INS_pminsw:
        case INS_pminud:
        case INS_pminsd:
        case INS_pmaxub:
        case INS_pmaxsb:
        case INS_pmaxuw:
        case INS_pmaxsw:
        case INS_pmaxsd:
        case INS_pmaxud:
        case INS_pabsb:
        case INS_pabsw:
        case INS_pabsd:
        case INS_psignb:
        case INS_psignw:
        case INS_psignd:
        case INS_vpsravd:
        case INS_blendvps:
        case INS_blendvpd:
        case INS_pblendvb:
        case INS_vpsllvd:
        case INS_vpsllvq:
        case INS_vpsrlvd:
        case INS_vpsrlvq:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += PERFSCORE_LATENCY_1C;
            break;

        case INS_pslld:
        case INS_psllw:
        case INS_psllq:
        case INS_psrlw:
        case INS_psrld:
        case INS_psrlq:
        case INS_psrad:
        case INS_psraw:
            if (insFmt == IF_RWR_CNS)
            {
                result.insLatency    = PERFSCORE_LATENCY_1C;
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            }
            else
            {
                result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_4C : PERFSCORE_LATENCY_2C;
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            }
            break;

        case INS_blsi:
        case INS_blsmsk:
        case INS_blsr:
        case INS_bzhi:
        case INS_rorx:
            result.insLatency += PERFSCORE_LATENCY_1C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case INS_bextr:
            result.insLatency += PERFSCORE_LATENCY_2C;
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case INS_packuswb:
        case INS_packusdw:
        case INS_packsswb:
        case INS_packssdw:
        case INS_unpcklps:
        case INS_unpckhps:
        case INS_unpcklpd:
        case INS_unpckhpd:
        case INS_punpckldq:
        case INS_punpcklwd:
        case INS_punpcklbw:
        case INS_punpckhdq:
        case INS_punpckhwd:
        case INS_punpckhbw:
        case INS_punpcklqdq:
        case INS_punpckhqdq:
        case INS_pshufb:
        case INS_pshufd:
        case INS_pshuflw:
        case INS_pshufhw:
        case INS_shufps:
        case INS_shufpd:
        case INS_pblendw:
        case INS_movsldup:
        case INS_movshdup:
        case INS_insertps:
        case INS_palignr:
        case INS_vpermilps:
        case INS_vpermilpd:
        case INS_vpermilpsvar:
        case INS_vpermilpdvar:
        case INS_pslldq:
        case INS_psrldq:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_1C;
            break;

        case INS_vblendvps:
        case INS_vblendvpd:
        case INS_vpblendvb:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_2C;
            break;

        case INS_bswap:
            if (opSize == EA_8BYTE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency    = PERFSCORE_LATENCY_2C;
            }
            else
            {
                assert(opSize == EA_4BYTE);
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency    = PERFSCORE_LATENCY_1C;
            }
            break;

        case INS_pmovmskb:
        case INS_movmskpd:
        case INS_movmskps:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            if (opSize == EA_32BYTE)
            {
                result.insLatency += ins == INS_pmovmskb ? PERFSCORE_LATENCY_4C : PERFSCORE_LATENCY_5C;
            }
            else
            {
                result.insLatency += PERFSCORE_LATENCY_3C;
            }
            break;

        case INS_bsf:
        case INS_bsr:
        case INS_lzcnt:
        case INS_tzcnt:
        case INS_popcnt:
        case INS_crc32:
        case INS_pdep:
        case INS_pext:
        case INS_pcmpgtq:
        case INS_psadbw:
        case INS_vpermps:
        case INS_vpermpd:
        case INS_vpermd:
        case INS_vpermq:
        case INS_vperm2i128:
        case INS_vperm2f128:
        case INS_vextractf128:
        case INS_vextracti128:
        case INS_vinsertf128:
        case INS_vinserti128:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_3C;
            break;

        case INS_pextrb:
        case INS_pextrd:
        case INS_pextrw:
        case INS_pextrq:
        case INS_pextrw_sse41:
        case INS_addsubps:
        case INS_addsubpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_pmovsxbw:
        case INS_pmovsxbd:
        case INS_pmovsxbq:
        case INS_pmovsxwd:
        case INS_pmovsxwq:
        case INS_pmovsxdq:
        case INS_pmovzxbw:
        case INS_pmovzxbd:
        case INS_pmovzxbq:
        case INS_pmovzxwd:
        case INS_pmovzxwq:
        case INS_pmovzxdq:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_1C;
            break;

        case INS_phaddw:
        case INS_phaddd:
        case INS_phaddsw:
        case INS_phsubw:
        case INS_phsubsw:
        case INS_phsubd:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency += PERFSCORE_LATENCY_3C;
            break;

        case INS_cmpps:
        case INS_cmppd:
        case INS_cmpss:
        case INS_cmpsd:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency    = PERFSCORE_LATENCY_4C;
            break;

        case INS_mulx:
        case INS_maxps:
        case INS_maxpd:
        case INS_maxss:
        case INS_maxsd:
        case INS_minps:
        case INS_minpd:
        case INS_minss:
        case INS_minsd:
        case INS_phminposuw:
        case INS_extractps:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_ptest:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_6C : PERFSCORE_LATENCY_4C;
            break;

        case INS_mpsadbw:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_pmullw:
        case INS_pmulhw:
        case INS_pmulhuw:
        case INS_pmulhrsw:
        case INS_pmuldq:
        case INS_pmuludq:
        case INS_pmaddwd:
        case INS_pmaddubsw:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += PERFSCORE_LATENCY_5C;
            break;

        case INS_cvtsd2ss:
        case INS_cvtps2pd:
        case INS_cvtpd2dq:
        case INS_cvtdq2pd:
        case INS_cvtpd2ps:
        case INS_cvttpd2dq:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_7C : PERFSCORE_LATENCY_5C;
            break;

        case INS_vtestps:
        case INS_vtestpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_5C : PERFSCORE_LATENCY_3C;
            break;

        case INS_hsubps:
        case INS_hsubpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency += PERFSCORE_LATENCY_6C;
            break;

        case INS_pclmulqdq:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_7C;
            break;

        case INS_pmulld:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_10C;
            break;

        case INS_vpbroadcastb:
        case INS_vpbroadcastw:
        case INS_vpbroadcastd:
        case INS_vpbroadcastq:
        case INS_vbroadcasti128:
        case INS_vbroadcastf128:
        case INS_vbroadcastss:
        case INS_vbroadcastsd:
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency    = opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_1C;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_3C : PERFSCORE_LATENCY_2C;
                if (ins == INS_vpbroadcastb || ins == INS_vpbroadcastw)
                {
                    result.insLatency += PERFSCORE_LATENCY_1C;
                }
            }
            break;

        case INS_pinsrb:
        case INS_pinsrw:
        case INS_pinsrd:
        case INS_pinsrq:
            if (memAccessKind == PERFSCORE_MEMORY_NONE)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2C;
                result.insLatency    = PERFSCORE_LATENCY_4C;
            }
            else
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency += PERFSCORE_LATENCY_3C;
            }
            break;

        case INS_dppd:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency    = PERFSCORE_LATENCY_9C;
            break;

        case INS_dpps:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency    = PERFSCORE_LATENCY_13C;
            break;

        case INS_vfmadd132pd:
        case INS_vfmadd213pd:
        case INS_vfmadd231pd:
        case INS_vfmadd132ps:
        case INS_vfmadd213ps:
        case INS_vfmadd231ps:
        case INS_vfmadd132sd:
        case INS_vfmadd213sd:
        case INS_vfmadd231sd:
        case INS_vfmadd132ss:
        case INS_vfmadd213ss:
        case INS_vfmadd231ss:
        case INS_vfmaddsub132pd:
        case INS_vfmaddsub213pd:
        case INS_vfmaddsub231pd:
        case INS_vfmaddsub132ps:
        case INS_vfmaddsub213ps:
        case INS_vfmaddsub231ps:
        case INS_vfmsubadd132pd:
        case INS_vfmsubadd213pd:
        case INS_vfmsubadd231pd:
        case INS_vfmsubadd132ps:
        case INS_vfmsubadd213ps:
        case INS_vfmsubadd231ps:
        case INS_vfmsub132pd:
        case INS_vfmsub213pd:
        case INS_vfmsub231pd:
        case INS_vfmsub132ps:
        case INS_vfmsub213ps:
        case INS_vfmsub231ps:
        case INS_vfmsub132sd:
        case INS_vfmsub213sd:
        case INS_vfmsub231sd:
        case INS_vfmsub132ss:
        case INS_vfmsub213ss:
        case INS_vfmsub231ss:
        case INS_vfnmadd132pd:
        case INS_vfnmadd213pd:
        case INS_vfnmadd231pd:
        case INS_vfnmadd132ps:
        case INS_vfnmadd213ps:
        case INS_vfnmadd231ps:
        case INS_vfnmadd132sd:
        case INS_vfnmadd213sd:
        case INS_vfnmadd231sd:
        case INS_vfnmadd132ss:
        case INS_vfnmadd213ss:
        case INS_vfnmadd231ss:
        case INS_vfnmsub132pd:
        case INS_vfnmsub213pd:
        case INS_vfnmsub231pd:
        case INS_vfnmsub132ps:
        case INS_vfnmsub213ps:
        case INS_vfnmsub231ps:
        case INS_vfnmsub132sd:
        case INS_vfnmsub213sd:
        case INS_vfnmsub231sd:
        case INS_vfnmsub132ss:
        case INS_vfnmsub213ss:
        case INS_vfnmsub231ss:
        case INS_vpdpbusd:  // will be populated when the HW becomes publicly available
        case INS_vpdpwssd:  // will be populated when the HW becomes publicly available
        case INS_vpdpbusds: // will be populated when the HW becomes publicly available
        case INS_vpdpwssds: // will be populated when the HW becomes publicly available
            // uops.info
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_vmaskmovpd:
        case INS_vmaskmovps:
        case INS_vpmaskmovd:
        case INS_vpmaskmovq:

            if (memAccessKind == PERFSCORE_MEMORY_READ)
            {
                result.insThroughput = PERFSCORE_THROUGHPUT_2X;
                result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_4C : PERFSCORE_LATENCY_3C;
            }
            else
            {
                assert(memAccessKind == PERFSCORE_MEMORY_WRITE);
                result.insThroughput = PERFSCORE_THROUGHPUT_1C;
                result.insLatency += PERFSCORE_LATENCY_12C;
            }
            break;

        case INS_vpgatherdd:
        case INS_vgatherdps:
            result.insThroughput = PERFSCORE_THROUGHPUT_4C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_13C : PERFSCORE_LATENCY_11C;
            break;

        case INS_vpgatherdq:
        case INS_vpgatherqd:
        case INS_vpgatherqq:
        case INS_vgatherdpd:
        case INS_vgatherqps:
        case INS_vgatherqpd:
            result.insThroughput = PERFSCORE_THROUGHPUT_4C;
            result.insLatency += opSize == EA_32BYTE ? PERFSCORE_LATENCY_11C : PERFSCORE_LATENCY_9C;
            break;

        case INS_aesdec:
        case INS_aesdeclast:
        case INS_aesenc:
        case INS_aesenclast:
            result.insThroughput = PERFSCORE_THROUGHPUT_1C;
            result.insLatency += PERFSCORE_LATENCY_4C;
            break;

        case INS_aesimc:
            result.insThroughput = PERFSCORE_THROUGHPUT_2C;
            result.insLatency += PERFSCORE_LATENCY_8C;
            break;

        case INS_aeskeygenassist:
            result.insThroughput = PERFSCORE_THROUGHPUT_13C;
            result.insLatency += PERFSCORE_LATENCY_7C;
            break;

        case INS_lfence:
            result.insThroughput = PERFSCORE_THROUGHPUT_4C;
            break;

        case INS_sfence:
            result.insThroughput = PERFSCORE_THROUGHPUT_6C;
            break;

        case INS_mfence:
            result.insThroughput = PERFSCORE_THROUGHPUT_33C;
            break;

        case INS_prefetcht0:
        case INS_prefetcht1:
        case INS_prefetcht2:
        case INS_prefetchnta:
            result.insThroughput = PERFSCORE_THROUGHPUT_2X;
            break;

        case INS_pause:
        {
            result.insLatency    = PERFSCORE_LATENCY_140C;
            result.insThroughput = PERFSCORE_THROUGHPUT_140C;
            break;
        }

        default:
            // unhandled instruction insFmt combination
            perfScoreUnhandledInstruction(id, &result);
            break;
    }

    return result;
}

#endif // defined(DEBUG) || defined(LATE_DISASM)

/*****************************************************************************/
/*****************************************************************************/

#endif // defined(TARGET_XARCH)
