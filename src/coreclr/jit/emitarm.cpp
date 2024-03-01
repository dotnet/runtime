// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitArm.cpp                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_ARM)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

/*****************************************************************************/

const instruction emitJumpKindInstructions[] = {
    INS_nop,

#define JMP_SMALL(en, rev, ins) INS_##ins,
#include "emitjmps.h"
};

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
 * Look up the jump kind for an instruction. It better be a conditional
 * branch instruction with a jump kind!
 */

/*static*/ emitJumpKind emitter::emitInsToJumpKind(instruction ins)
{
    for (unsigned i = 0; i < ArrLen(emitJumpKindInstructions); i++)
    {
        if (ins == emitJumpKindInstructions[i])
        {
            emitJumpKind ret = (emitJumpKind)i;
            assert(EJ_NONE < ret && ret < EJ_COUNT);
            return ret;
        }
    }
    unreached();
}

/*****************************************************************************
 * Reverse the conditional jump
 */

/*static*/ emitJumpKind emitter::emitReverseJumpKind(emitJumpKind jumpKind)
{
    assert(jumpKind < EJ_COUNT);
    return emitReverseJumpKinds[jumpKind];
}

/*****************************************************************************
 *
 *  Return the allocated size (in bytes) of the given instruction descriptor.
 */

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    if (emitIsSmallInsDsc(id))
        return SMALL_IDSC_SIZE;

    assert((unsigned)id->idInsFmt() < emitFmtCount);

    ID_OPS idOp         = (ID_OPS)emitFmtToOps[id->idInsFmt()];
    bool   isCallIns    = (id->idIns() == INS_bl) || (id->idIns() == INS_blx);
    bool   maybeCallIns = (id->idIns() == INS_b) || (id->idIns() == INS_bx);

    // An INS_call instruction may use a "fat" direct/indirect call descriptor
    // except for a local call to a label (i.e. call to a finally).
    // Only ID_OP_CALL and ID_OP_SPEC check for this, so we enforce that the
    // INS_call instruction always uses one of these idOps.

    assert(!isCallIns ||         // either not a call or
           idOp == ID_OP_CALL || // is a direct call
           idOp == ID_OP_SPEC || // is an indirect call
           idOp == ID_OP_JMP);   // is a local call to finally clause

    switch (idOp)
    {
        case ID_OP_NONE:
            break;

        case ID_OP_JMP:
            return sizeof(instrDescJmp);

        case ID_OP_LBL:
            return sizeof(instrDescLbl);

        case ID_OP_CALL:
        case ID_OP_SPEC:
            assert(isCallIns || maybeCallIns);
            if (id->idIsLargeCall())
            {
                /* Must be a "fat" indirect call descriptor */
                return sizeof(instrDescCGCA);
            }
            else
            {
                assert(!id->idIsLargeDsp());
                assert(!id->idIsLargeCns());
                return sizeof(instrDesc);
            }
            break;

        default:
            NO_WAY("unexpected instruction descriptor format");
            break;
    }

    if (id->idInsFmt() == IF_T2_N3)
    {
        assert((id->idIns() == INS_movw) || (id->idIns() == INS_movt));
        return sizeof(instrDescReloc);
    }

    if (id->idIsLargeCns())
    {
        if (id->idIsLargeDsp())
            return sizeof(instrDescCnsDsp);
        else
            return sizeof(instrDescCns);
    }
    else
    {
        if (id->idIsLargeDsp())
            return sizeof(instrDescDsp);
        else
            return sizeof(instrDesc);
    }
}

bool offsetFitsInVectorMem(int disp)
{
    unsigned imm = unsigned_abs(disp);
    return ((imm & 0x03fc) == imm);
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  The following called for each recorded instruction -- use for debugging.
 */
void emitter::emitInsSanityCheck(instrDesc* id)
{
    /* What instruction format have we got? */

    switch (id->idInsFmt())
    {
        case IF_T1_A: // T1_A    ................
        case IF_T2_A: // T2_A    ................ ................
            break;

        case IF_T1_B: // T1_B    ........cccc....                                           cond
        case IF_T2_B: // T2_B    ................ ............iiii                          imm4
            assert(emitGetInsSC(id) < 0x10);
            break;

        case IF_T1_C: // T1_C    .....iiiiinnnddd                       R1  R2              imm5
            assert(isLowRegister(id->idReg1()));
            assert(isLowRegister(id->idReg2()));
            assert(insUnscaleImm(id->idIns(), emitGetInsSC(id)) < 0x20);
            break;

        case IF_T1_D0: // T1_D0   ........Dmmmmddd                       R1* R2*
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_T1_D1: // T1_D1   .........mmmm...                       R1*
            assert(isGeneralRegister(id->idReg1()));
            break;

        case IF_T1_D2: // T1_D2   .........mmmm...                               R3*
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_T1_E: // T1_E    ..........nnnddd                       R1  R2
            assert(isLowRegister(id->idReg1()));
            assert(isLowRegister(id->idReg2()));
            assert(id->idSmallCns() < 0x20);
            break;

        case IF_T1_F: // T1_F    .........iiiiiii                       SP                  imm7
            assert(id->idReg1() == REG_SP);
            assert(id->idOpSize() == EA_4BYTE);
            assert((emitGetInsSC(id) & ~0x1FC) == 0);
            break;

        case IF_T1_G: // T1_G    .......iiinnnddd                       R1  R2              imm3
            assert(isLowRegister(id->idReg1()));
            assert(isLowRegister(id->idReg2()));
            assert(id->idSmallCns() < 0x8);
            break;

        case IF_T1_H: // T1_H    .......mmmnnnddd                       R1  R2  R3
            assert(isLowRegister(id->idReg1()));
            assert(isLowRegister(id->idReg2()));
            assert(isLowRegister(id->idReg3()));
            break;

        case IF_T1_I: // T1_I    ......i.iiiiiddd                       R1                  imm6
            assert(isLowRegister(id->idReg1()));
            break;

        case IF_T1_J0: // T1_J0   .....dddiiiiiiii                       R1                  imm8
            assert(isLowRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T1_J1: // T1_J1   .....dddiiiiiiii                       R1                  <regmask8>
            assert(isLowRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T1_J2: // T1_J2   .....dddiiiiiiii                       R1  SP              imm8
            assert(isLowRegister(id->idReg1()));
            assert(id->idReg2() == REG_SP);
            assert(id->idOpSize() == EA_4BYTE);
            assert((emitGetInsSC(id) & ~0x3FC) == 0);
            break;

        case IF_T1_L0: // T1_L0   ........iiiiiiii                                           imm8
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T1_L1: // T1_L1   .......Rrrrrrrrr                                           <regmask8+2>
            assert(emitGetInsSC(id) < 0x400);
            break;

        case IF_T2_C0: // T2_C0   ...........Snnnn .iiiddddiishmmmm       R1  R2  R3      S, imm5, sh
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(emitGetInsSC(id) < 0x20);
            break;

        case IF_T2_C4: // T2_C4   ...........Snnnn ....dddd....mmmm       R1  R2  R3      S
        case IF_T2_C5: // T2_C5   ............nnnn ....dddd....mmmm       R1  R2  R3
        case IF_T2_G1: // T2_G1   ............nnnn ttttTTTT........       R1  R2  R3
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_T2_C1: // T2_C1   ...........S.... .iiiddddiishmmmm       R1  R2          S, imm5, sh
        case IF_T2_C2: // T2_C2   ...........S.... .iiiddddii..mmmm       R1  R2          S, imm5
        case IF_T2_C8: // T2_C8   ............nnnn .iii....iishmmmm       R1  R2             imm5, sh
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(emitGetInsSC(id) < 0x20);
            break;

        case IF_T2_C6: // T2_C6   ................ ....dddd..iimmmm       R1  R2                   imm2
        case IF_T2_C7: // T2_C7   ............nnnn ..........shmmmm       R1  R2                   imm2
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(emitGetInsSC(id) < 0x4);
            break;

        case IF_T2_C3:  // T2_C3   ...........S.... ....dddd....mmmm       R1  R2          S
        case IF_T2_C9:  // T2_C9   ............nnnn ............mmmm       R1  R2
        case IF_T2_C10: // T2_C10  ............mmmm ....dddd....mmmm       R1  R2
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_T2_D0: // T2_D0   ............nnnn .iiiddddii.wwwww       R1  R2             imm5, imm5
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(emitGetInsSC(id) < 0x400);
            break;

        case IF_T2_D1: // T2_D1   ................ .iiiddddii.wwwww       R1                 imm5, imm5
            assert(isGeneralRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x400);
            break;

        case IF_T2_E0: // T2_E0   ............nnnn tttt......shmmmm       R1  R2  R3               imm2
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            if (id->idIsLclVar())
            {
                assert(isGeneralRegister(codeGen->rsGetRsvdReg()));
            }
            else
            {
                assert(isGeneralRegister(id->idReg3()));
                assert(emitGetInsSC(id) < 0x4);
            }
            break;

        case IF_T2_E1: // T2_E1   ............nnnn tttt............       R1  R2
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_T2_E2: // T2_E2   ................ tttt............       R1
            assert(isGeneralRegister(id->idReg1()));
            break;

        case IF_T2_F1: // T2_F1    ............nnnn ttttdddd....mmmm       R1  R2  R3  R4
        case IF_T2_F2: // T2_F2    ............nnnn aaaadddd....mmmm       R1  R2  R3  R4
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isGeneralRegister(id->idReg4()));
            break;

        case IF_T2_G0: // T2_G0   .......PU.W.nnnn ttttTTTTiiiiiiii       R1  R2  R3         imm8, PUW
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(unsigned_abs(emitGetInsSC(id)) < 0x100);
            break;

        case IF_T2_H0: // T2_H0   ............nnnn tttt.PUWiiiiiiii       R1  R2             imm8, PUW
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(unsigned_abs(emitGetInsSC(id)) < 0x100);
            break;

        case IF_T2_H1: // T2_H1   ............nnnn tttt....iiiiiiii       R1  R2             imm8
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T2_H2: // T2_H2   ............nnnn ........iiiiiiii       R1                 imm8
            assert(isGeneralRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T2_I0: // T2_I0   ..........W.nnnn rrrrrrrrrrrrrrrr       R1              W, imm16
            assert(isGeneralRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x10000);
            break;

        case IF_T2_N: // T2_N    .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            assert(isGeneralRegister(id->idReg1()));
            assert(!id->idIsReloc());
            break;

        case IF_T2_N2: // T2_N2   .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            assert(isGeneralRegister(id->idReg1()));
            assert((size_t)emitGetInsSC(id) < emitDataSize());
            break;

        case IF_T2_N3: // T2_N3   .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            assert(isGeneralRegister(id->idReg1()));
            assert(id->idIsReloc());
            break;

        case IF_T2_I1: // T2_I1   ................ rrrrrrrrrrrrrrrr                          imm16
            assert(emitGetInsSC(id) < 0x10000);
            break;

        case IF_T2_K1: // T2_K1   ............nnnn ttttiiiiiiiiiiii       R1  R2             imm12
        case IF_T2_M0: // T2_M0   .....i......nnnn .iiiddddiiiiiiii       R1  R2             imm12
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(emitGetInsSC(id) < 0x1000);
            break;

        case IF_T2_L0: // T2_L0   .....i.....Snnnn .iiiddddiiiiiiii       R1  R2          S, imm8<<imm4
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isModImmConst(emitGetInsSC(id)));
            break;

        case IF_T2_K4: // T2_K4   ........U....... ttttiiiiiiiiiiii       R1  PC          U, imm12
        case IF_T2_M1: // T2_M1   .....i.......... .iiiddddiiiiiiii       R1  PC             imm12
            assert(isGeneralRegister(id->idReg1()));
            assert(id->idReg2() == REG_PC);
            assert(emitGetInsSC(id) < 0x1000);
            break;

        case IF_T2_K3: // T2_K3   ........U....... ....iiiiiiiiiiii       PC              U, imm12
            assert(id->idReg1() == REG_PC);
            assert(emitGetInsSC(id) < 0x1000);
            break;

        case IF_T2_K2: // T2_K2   ............nnnn ....iiiiiiiiiiii       R1                 imm12
            assert(isGeneralRegister(id->idReg1()));
            assert(emitGetInsSC(id) < 0x1000);
            break;

        case IF_T2_L1: // T2_L1   .....i.....S.... .iiiddddiiiiiiii       R1              S, imm8<<imm4
        case IF_T2_L2: // T2_L2   .....i......nnnn .iii....iiiiiiii       R1                 imm8<<imm4
            assert(isGeneralRegister(id->idReg1()));
            assert(isModImmConst(emitGetInsSC(id)));
            break;

        case IF_T1_J3: // T1_J3   .....dddiiiiiiii                        R1  PC             imm8
            assert(isGeneralRegister(id->idReg1()));
            assert(id->idReg2() == REG_PC);
            assert(emitGetInsSC(id) < 0x100);
            break;

        case IF_T1_K:  // T1_K    ....cccciiiiiiii                        Branch             imm8, cond4
        case IF_T1_M:  // T1_M    .....iiiiiiiiiii                        Branch             imm11
        case IF_T2_J1: // T2_J1   .....Scccciiiiii ..j.jiiiiiiiiiii       Branch             imm20, cond4
        case IF_T2_J2: // T2_J2   .....Siiiiiiiiii ..j.jiiiiiiiiii.       Branch             imm24
        case IF_T2_N1: // T2_N    .....i......iiii .iiiddddiiiiiiii       R1                 imm16
        case IF_T2_J3: // T2_J3   .....Siiiiiiiiii ..j.jiiiiiiiiii.       Call               imm24
        case IF_LARGEJMP:
            break;

        case IF_T2_VFP3:
            if (id->idOpSize() == EA_8BYTE)
            {
                assert(isDoubleReg(id->idReg1()));
                assert(isDoubleReg(id->idReg2()));
                assert(isDoubleReg(id->idReg3()));
            }
            else
            {
                assert(id->idOpSize() == EA_4BYTE);
                assert(isFloatReg(id->idReg1()));
                assert(isFloatReg(id->idReg2()));
                assert(isFloatReg(id->idReg3()));
            }
            break;

        case IF_T2_VFP2:
            assert(isFloatReg(id->idReg1()));
            assert(isFloatReg(id->idReg2()));
            break;

        case IF_T2_VLDST:
            if (id->idOpSize() == EA_8BYTE)
                assert(isDoubleReg(id->idReg1()));
            else
                assert(isFloatReg(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(offsetFitsInVectorMem(emitGetInsSC(id)));
            break;

        case IF_T2_VMOVD:
            assert(id->idOpSize() == EA_8BYTE);
            if (id->idIns() == INS_vmov_d2i)
            {
                assert(isGeneralRegister(id->idReg1()));
                assert(isGeneralRegister(id->idReg2()));
                assert(isDoubleReg(id->idReg3()));
            }
            else
            {
                assert(id->idIns() == INS_vmov_i2d);
                assert(isDoubleReg(id->idReg1()));
                assert(isGeneralRegister(id->idReg2()));
                assert(isGeneralRegister(id->idReg3()));
            }
            break;

        case IF_T2_VMOVS:
            assert(id->idOpSize() == EA_4BYTE);
            if (id->idIns() == INS_vmov_i2f)
            {
                assert(isFloatReg(id->idReg1()));
                assert(isGeneralRegister(id->idReg2()));
            }
            else
            {
                assert(id->idIns() == INS_vmov_f2i);
                assert(isGeneralRegister(id->idReg1()));
                assert(isFloatReg(id->idReg2()));
            }
            break;

        default:
            printf("unexpected format %s\n", emitIfName(id->idInsFmt()));
            assert(!"Unexpected format");
            break;
    }
}
#endif // DEBUG

bool emitter::emitInsMayWriteToGCReg(instrDesc* id)
{
    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    switch (fmt)
    {

        // These are the formats with "destination" or "target" registers:
        case IF_T1_C:
        case IF_T1_D0:
        case IF_T1_E:
        case IF_T1_G:
        case IF_T1_H:
        case IF_T1_J0:
        case IF_T1_J1:
        case IF_T1_J2:
        case IF_T1_J3:
        case IF_T2_C0:
        case IF_T2_C1:
        case IF_T2_C2:
        case IF_T2_C3:
        case IF_T2_C4:
        case IF_T2_C5:
        case IF_T2_C6:
        case IF_T2_C10:
        case IF_T2_D0:
        case IF_T2_D1:
        case IF_T2_F1:
        case IF_T2_F2:
        case IF_T2_L0:
        case IF_T2_L1:
        case IF_T2_M0:
        case IF_T2_M1:
        case IF_T2_N:
        case IF_T2_N1:
        case IF_T2_N2:
        case IF_T2_N3:
        case IF_T2_VFP3:
        case IF_T2_VFP2:
        case IF_T2_VLDST:
        case IF_T2_E0:
        case IF_T2_E1:
        case IF_T2_E2:
        case IF_T2_G0:
        case IF_T2_G1:
        case IF_T2_H0:
        case IF_T2_H1:
        case IF_T2_K1:
        case IF_T2_K4:
            // Some formats with "destination" or "target" registers are actually used for store instructions, for the
            // "source" value written to memory.
            // Similarly, PUSH has a target register, indicating the start of the set of registers to push.  POP
            // *does* write to at least one register, so we do not make that a special case.
            // Various compare/test instructions do not write (except to the flags). Technically "teq" does not need to
            // be
            // be in this list because it has no forms matched above, but I'm putting it here for completeness.
            switch (ins)
            {
                case INS_str:
                case INS_strb:
                case INS_strh:
                case INS_strd:
                case INS_strex:
                case INS_strexb:
                case INS_strexd:
                case INS_strexh:
                case INS_push:
                case INS_cmp:
                case INS_cmn:
                case INS_tst:
                case INS_teq:
                    return false;
                default:
                    return true;
            }
        case IF_T2_VMOVS:
            // VMOV.i2f reads from the integer register. Conversely VMOV.f2i writes to GC pointer-sized
            // integer register that might have previously held GC pointers, so they need to be included.
            assert(id->idGCref() == GCT_NONE);
            return (ins == INS_vmov_f2i);

        case IF_T2_VMOVD:
            // VMOV.i2d reads from the integer registers. Conversely VMOV.d2i writes to GC pointer-sized
            // integer registers that might have previously held GC pointers, so they need to be included.
            assert(id->idGCref() == GCT_NONE);
            return (ins == INS_vmov_d2i);

        default:
            return false;
    }
}

bool emitter::emitInsWritesToLclVarStackLoc(instrDesc* id)
{
    if (!id->idIsLclVar())
        return false;

    instruction ins = id->idIns();

    // This list is related to the list of instructions used to store local vars in emitIns_S_R().
    // We don't accept writing to float local vars.

    switch (ins)
    {
        case INS_strb:
        case INS_strh:
        case INS_str:
            return true;
        default:
            return false;
    }
}

bool emitter::emitInsMayWriteMultipleRegs(instrDesc* id)
{
    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    switch (ins)
    {
        case INS_ldm:
        case INS_ldmdb:
        case INS_smlal:
        case INS_smull:
        case INS_umlal:
        case INS_umull:
        case INS_vmov_d2i:
            return true;
        case INS_pop:
            if (fmt != IF_T2_E2) // T2_E2 is pop single register encoding
            {
                return true;
            }
            return false;
        default:
            return false;
    }
}

/*****************************************************************************
 *
 *  Return a string that represents the given register.
 */

const char* emitter::emitRegName(regNumber reg, emitAttr attr, bool varName) const
{
    assert(reg < REG_COUNT);

    const char* rn = emitComp->compRegVarName(reg, varName, false);

    assert(strlen(rn) >= 1);

    return rn;
}

const char* emitter::emitFloatRegName(regNumber reg, emitAttr attr, bool varName)
{
    assert(reg < REG_COUNT);

    const char* rn = emitComp->compRegVarName(reg, varName, true);

    assert(strlen(rn) >= 1);

    return rn;
}

/*****************************************************************************
 *
 *  Returns the base encoding of the given CPU instruction.
 */

emitter::insFormat emitter::emitInsFormat(instruction ins)
{
    // clang-format off
    const static insFormat insFormats[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                ) fmt,
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) fmt,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) fmt,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) fmt,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) fmt,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) fmt,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) fmt,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) fmt,
        #include "instrs.h"
    };
    // clang-format on

    assert(ins < ArrLen(insFormats));
    assert((insFormats[ins] != IF_NONE));

    return insFormats[ins];
}

// INST_FP is 1
#define LD 2
#define ST 4
#define CMP 8

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST1(id, nm, fp, ldst, fmt, e1                                ) ldst | INST_FP*fp,
    #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) ldst | INST_FP*fp,
    #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) ldst | INST_FP*fp,
    #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) ldst | INST_FP*fp,
    #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) ldst | INST_FP*fp,
    #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) ldst | INST_FP*fp,
    #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) ldst | INST_FP*fp,
    #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) ldst | INST_FP*fp,
    #include "instrs.h"
};
// clang-format on

/*****************************************************************************
 *
 *  Returns true if the instruction is some kind of load instruction
 */

bool emitter::emitInsIsLoad(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & LD) ? true : false;
    else
        return false;
}

/*****************************************************************************
 *
 *  Returns true if the instruction is some kind of compare or test instruction
 */

bool emitter::emitInsIsCompare(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & CMP) ? true : false;
    else
        return false;
}

/*****************************************************************************
 *
 *  Returns true if the instruction is some kind of store instruction
 */

bool emitter::emitInsIsStore(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & ST) ? true : false;
    else
        return false;
}

/*****************************************************************************
 *
 *  Returns true if the instruction is some kind of load/store instruction
 */

bool emitter::emitInsIsLoadOrStore(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & (LD | ST)) ? true : false;
    else
        return false;
}

#undef LD
#undef ST
#undef CMP

/*****************************************************************************
 *
 *  Returns the specific encoding of the given CPU instruction and format
 */

emitter::code_t emitter::emitInsCode(instruction ins, insFormat fmt)
{
    // clang-format off
    const static code_t insCodes1[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                ) e1,
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) e1,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) e1,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) e1,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) e1,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e1,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e1,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e1,
        #include "instrs.h"
    };
    const static code_t insCodes2[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            ) e2,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) e2,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) e2,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) e2,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e2,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e2,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e2,
        #include "instrs.h"
    };
    const static code_t insCodes3[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        ) e3,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) e3,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) e3,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e3,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e3,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e3,
        #include "instrs.h"
    };
    const static code_t insCodes4[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    ) e4,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) e4,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e4,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e4,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e4,
        #include "instrs.h"
    };
    const static code_t insCodes5[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    )
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                ) e5,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e5,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e5,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e5,
        #include "instrs.h"
    };
    const static code_t insCodes6[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    )
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                )
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            ) e6,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e6,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e6,
        #include "instrs.h"
    };
    const static code_t insCodes7[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    )
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                )
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            )
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e7,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e7,
        #include "instrs.h"
    };
    const static code_t insCodes8[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    )
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                )
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            )
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    ) e8,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e8,
        #include "instrs.h"
    };
    const static code_t insCodes9[] =
    {
        #define INST1(id, nm, fp, ldst, fmt, e1                                )
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                            )
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                        )
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                    )
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                )
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6            )
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8    )
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e9,
        #include "instrs.h"
    };
    const static insFormat formatEncode9[9]  = { IF_T1_D0, IF_T1_H,  IF_T1_J0, IF_T1_G,  IF_T2_L0, IF_T2_C0, IF_T1_F,  IF_T1_J2, IF_T1_J3 };
    const static insFormat formatEncode8[8]  = { IF_T1_H,  IF_T1_C,  IF_T2_E0, IF_T2_H0, IF_T2_K1, IF_T2_K4, IF_T1_J2, IF_T1_J3 };
    const static insFormat formatEncode6A[6] = { IF_T1_H,  IF_T1_C,  IF_T2_E0, IF_T2_H0, IF_T2_K1, IF_T2_K4};
    const static insFormat formatEncode6B[6] = { IF_T1_H,  IF_T1_C,  IF_T2_E0, IF_T2_H0, IF_T2_K1, IF_T1_J2 };
    const static insFormat formatEncode5A[5] = { IF_T1_E,  IF_T1_D0, IF_T1_J0, IF_T2_L1, IF_T2_C3 };
    const static insFormat formatEncode5B[5] = { IF_T1_E,  IF_T1_D0, IF_T1_J0, IF_T2_L2, IF_T2_C8 };
    const static insFormat formatEncode4A[4] = { IF_T1_E,  IF_T1_C,  IF_T2_C4, IF_T2_C2 };
    const static insFormat formatEncode4B[4] = { IF_T2_K2, IF_T2_H2, IF_T2_C7, IF_T2_K3 };
    const static insFormat formatEncode4C[4] = { IF_T2_N,  IF_T2_N1, IF_T2_N2, IF_T2_N3 };
    const static insFormat formatEncode3A[3] = { IF_T1_E,  IF_T2_C0, IF_T2_L0 };
    const static insFormat formatEncode3B[3] = { IF_T1_E,  IF_T2_C8, IF_T2_L2 };
    const static insFormat formatEncode3C[3] = { IF_T1_E,  IF_T2_C1, IF_T2_L1 };
    const static insFormat formatEncode3D[3] = { IF_T1_L1, IF_T2_E2, IF_T2_I1 };
    const static insFormat formatEncode3E[3] = { IF_T1_M,  IF_T2_J2, IF_T2_J3 };
    const static insFormat formatEncode2A[2] = { IF_T1_K,  IF_T2_J1 };
    const static insFormat formatEncode2B[2] = { IF_T1_D1, IF_T1_D2 };
    const static insFormat formatEncode2C[2] = { IF_T1_D2, IF_T2_J3 };
    const static insFormat formatEncode2D[2] = { IF_T1_J1, IF_T2_I0 };
    const static insFormat formatEncode2E[2] = { IF_T1_E,  IF_T2_C6 };
    const static insFormat formatEncode2F[2] = { IF_T1_E,  IF_T2_C5 };
    const static insFormat formatEncode2G[2] = { IF_T1_J3, IF_T2_M1 };
    // clang-format on

    code_t    code   = BAD_CODE;
    insFormat insFmt = emitInsFormat(ins);
    bool      found  = false;
    int       index  = 0;

    switch (insFmt)
    {
        case IF_EN9:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN8:
            for (index = 0; index < 8; index++)
            {
                if (fmt == formatEncode8[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN6A:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6A[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN6B:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6B[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN5A:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5A[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN5B:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5B[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN4A:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4A[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN4B:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4B[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN4C:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4C[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN3A:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3A[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN3B:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3B[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN3C:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3C[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN3D:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3D[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN3E:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3E[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN2A:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2A[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN2B:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2B[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN2C:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2C[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN2D:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2D[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN2E:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2E[index])
                {
                    found = true;
                    break;
                }
            }
            break;
        case IF_EN2F:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2F[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        case IF_EN2G:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2G[index])
                {
                    found = true;
                    break;
                }
            }
            break;

        default:
            index = 0;
            found = true;
            break;
    }

    assert(found);

    switch (index)
    {
        case 0:
            assert(ins < ArrLen(insCodes1));
            code = insCodes1[ins];
            break;
        case 1:
            assert(ins < ArrLen(insCodes2));
            code = insCodes2[ins];
            break;
        case 2:
            assert(ins < ArrLen(insCodes3));
            code = insCodes3[ins];
            break;
        case 3:
            assert(ins < ArrLen(insCodes4));
            code = insCodes4[ins];
            break;
        case 4:
            assert(ins < ArrLen(insCodes5));
            code = insCodes5[ins];
            break;
        case 5:
            assert(ins < ArrLen(insCodes6));
            code = insCodes6[ins];
            break;
        case 6:
            assert(ins < ArrLen(insCodes7));
            code = insCodes7[ins];
            break;
        case 7:
            assert(ins < ArrLen(insCodes8));
            code = insCodes8[ins];
            break;
        case 8:
            assert(ins < ArrLen(insCodes9));
            code = insCodes9[ins];
            break;
    }

    assert((code != BAD_CODE));

    return code;
}

/*****************************************************************************
 *
 *  Return the code size of the given instruction format. The 'insSize' return type enum
 *  indicates a 16 bit, 32 bit, or 48 bit instruction.
 */

emitter::insSize emitter::emitInsSize(insFormat insFmt)
{
    if ((insFmt >= IF_T1_A) && (insFmt < IF_T2_A))
        return ISZ_16BIT;

    if ((insFmt >= IF_T2_A) && (insFmt < IF_INVALID))
        return ISZ_32BIT;

    if (insFmt == IF_LARGEJMP)
        return ISZ_48BIT;

    assert(!"Invalid insFormat");
    return ISZ_48BIT;
}

//------------------------------------------------------------------------
// IsMovInstruction: Determines whether a give instruction is a move instruction
//
// Arguments:
//    ins       -- The instruction being checked
//
bool emitter::IsMovInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_mov:
        case INS_sxtb:
        case INS_sxth:
        case INS_uxtb:
        case INS_uxth:
        case INS_vmov:
        case INS_vmov_i2f:
        case INS_vmov_f2i:
        {
            return true;
        }

        default:
        {
            return false;
        }
    }
}

/*****************************************************************************
 *
 *  isModImmConst() returns true when immediate 'val32' can be encoded
 *   using the special modified immediate constant available in Thumb
 */

/*static*/ bool emitter::isModImmConst(int val32)
{
    unsigned uval32 = (unsigned)val32;
    unsigned imm8   = uval32 & 0xff;

    /* encode = 0000x */
    if (imm8 == uval32)
        return true;

    unsigned imm32a = (imm8 << 16) | imm8;
    /* encode = 0001x */
    if (imm32a == uval32)
        return true;

    unsigned imm32b = (imm32a << 8);
    /* encode = 0010x */
    if (imm32b == uval32)
        return true;

    unsigned imm32c = (imm32a | imm32b);
    /* encode = 0011x */
    if (imm32c == uval32)
        return true;

    unsigned mask32 = 0x00000ff;

    unsigned encode = 31; /* 11111 */
    unsigned temp;

    do
    {
        mask32 <<= 1;
        temp = uval32 & ~mask32;
        if (temp == 0)
            return true;
        encode--;
    } while (encode >= 8);

    return false;
}

/*****************************************************************************
 *
 *  encodeModImmConst() returns the special ARM 12-bit immediate encoding.
 *   that is used to encode the immediate.  (4-bits, 8-bits)
 *   If the imm can not be encoded then 0x0BADC0DE is returned.
 */

/*static*/ int emitter::encodeModImmConst(int val32)
{
    unsigned uval32 = (unsigned)val32;
    unsigned imm8   = uval32 & 0xff;
    unsigned encode = imm8 >> 7;
    unsigned imm32a;
    unsigned imm32b;
    unsigned imm32c;
    unsigned mask32;
    unsigned temp;

    /* encode = 0000x */
    if (imm8 == uval32)
    {
        goto DONE;
    }

    imm32a = (imm8 << 16) | imm8;
    /* encode = 0001x */
    if (imm32a == uval32)
    {
        encode += 2;
        goto DONE;
    }

    imm32b = (imm32a << 8);
    /* encode = 0010x */
    if (imm32b == uval32)
    {
        encode += 4;
        goto DONE;
    }

    imm32c = (imm32a | imm32b);
    /* encode = 0011x */
    if (imm32c == uval32)
    {
        encode += 6;
        goto DONE;
    }

    mask32 = 0x00000ff;

    encode = 31; /* 11111 */
    do
    {
        mask32 <<= 1;
        temp = uval32 & ~mask32;
        if (temp == 0)
        {
            imm8 = (uval32 & mask32) >> (32 - encode);
            assert((imm8 & 0x80) != 0);
            goto DONE;
        }
        encode--;
    } while (encode >= 8);

    assert(!"encodeModImmConst failed!");
    return BAD_CODE;

DONE:
    unsigned result = (encode << 7) | (imm8 & 0x7f);
    assert(result <= 0x0fff);
    assert(result >= 0);
    return (int)result;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_alu() returns true when the immediate 'imm'
 *   can be encoded using the 12-bit funky Arm immediate encoding
 */
/*static*/ bool emitter::emitIns_valid_imm_for_alu(int imm)
{
    if (isModImmConst(imm))
        return true;
    return false;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_mov() returns true when the immediate 'imm'
 *   can be encoded using a single mov or mvn instruction.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_mov(int imm)
{
    if ((imm & 0x0000ffff) == imm) // 16-bit immediate
        return true;
    if (isModImmConst(imm)) // funky arm immediate
        return true;
    if (isModImmConst(~imm)) // funky arm immediate via mvn
        return true;
    return false;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_small_mov() returns true when the immediate 'imm'
 *   can be encoded using a single 2-byte mov instruction.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_small_mov(regNumber reg, int imm, insFlags flags)
{
    return isLowRegister(reg) && insSetsFlags(flags) && ((imm & 0x00ff) == imm);
}

/*****************************************************************************
 *
 *  emitins_valid_imm_for_add() returns true when the immediate 'imm'
 *   can be encoded using a single add or sub instruction.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_add(int imm, insFlags flags)
{
    if ((unsigned_abs(imm) <= 0x00000fff) && (flags != INS_FLAGS_SET)) // 12-bit immediate via add/sub
        return true;
    if (isModImmConst(imm)) // funky arm immediate
        return true;
    if (isModImmConst(-imm)) // funky arm immediate via sub
        return true;
    return false;
}

/*****************************************************************************
 *
 *  emitins_valid_imm_for_cmp() returns true if this 'imm'
 *   can be encoded as a input operand to an cmp instruction.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_cmp(int imm, insFlags flags)
{
    if (isModImmConst(imm)) // funky arm immediate
        return true;
    if (isModImmConst(-imm)) // funky arm immediate via sub
        return true;
    return false;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_add_sp() returns true when the immediate 'imm'
 *   can be encoded in "add Rd,SP,i10".
 */
/*static*/ bool emitter::emitIns_valid_imm_for_add_sp(int imm)
{
    if ((imm & 0x03fc) == imm)
        return true;
    return false;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_ldst_offset() returns true when the immediate 'imm'
 *   can be encoded as the offset in a ldr/str instruction.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_ldst_offset(int imm, emitAttr size)
{
    if ((imm & 0x0fff) == imm)
        return true; // encodable using IF_T2_K1
    if (unsigned_abs(imm) <= 0x0ff)
        return true; // encodable using IF_T2_H0
    return false;
}

/*****************************************************************************
 *
 *  emitIns_valid_imm_for_vldst_offset() returns true when the immediate 'imm'
 *   can be encoded as the offset in a vldr/vstr instruction, i.e. when it is
 *   a non-negative multiple of 4 that is less than 1024.
 */
/*static*/ bool emitter::emitIns_valid_imm_for_vldst_offset(int imm)
{
    if ((imm & 0x3fc) == imm)
        return true;
    return false;
}

/*****************************************************************************
 *
 *  Add an instruction with no operands.
 */

void emitter::emitIns(instruction ins)
{
    instrDesc* id  = emitNewInstrSmall(EA_4BYTE);
    insFormat  fmt = emitInsFormat(ins);
    insSize    isz = emitInsSize(fmt);

    assert((fmt == IF_T1_A) || (fmt == IF_T2_A));

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a single immediate value.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, target_ssize_t imm)
{
    insFormat fmt         = IF_NONE;
    bool      hasLR       = false;
    bool      hasPC       = false;
    bool      useT2       = false;
    bool      isSingleBit = false;
    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
#ifdef FEATURE_ITINSTRUCTION
        case INS_it:
        case INS_itt:
        case INS_ite:
        case INS_ittt:
        case INS_itte:
        case INS_itet:
        case INS_itee:
        case INS_itttt:
        case INS_ittte:
        case INS_ittet:
        case INS_ittee:
        case INS_itett:
        case INS_itete:
        case INS_iteet:
        case INS_iteee:
            assert((imm & 0x0F) == imm);
            fmt  = IF_T1_B;
            attr = EA_4BYTE;
            break;
#endif // FEATURE_ITINSTRUCTION

        case INS_push:
            assert((imm & 0xA000) == 0); // Cannot push PC or SP

            if (imm & 0x4000) // Is the LR being pushed?
                hasLR = true;

            goto COMMON_PUSH_POP;

        case INS_pop:
            assert((imm & 0x2000) == 0);      // Cannot pop SP
            assert((imm & 0xC000) != 0xC000); // Cannot pop both PC and LR

            if (imm & 0x8000) // Is the PC being popped?
                hasPC = true;
            if (imm & 0x4000) // Is the LR being popped?
            {
                hasLR = true;
                useT2 = true;
            }

        COMMON_PUSH_POP:

            if (((imm - 1) & imm) == 0) // Is only one or zero bits set in imm?
            {
                if (imm != 0)
                {
                    isSingleBit = true; // only one bits set in imm
                }
            }

            imm &= ~0xE000; // ensure that PC, LR and SP bits are removed from imm

            if (((imm & 0x00ff) == imm) && !useT2)
            {
                // for push {LR,} <reglist8> and pop  {PC,} <regist8> encoding
                fmt = IF_T1_L1;
            }
            else if (!isSingleBit)
            {
                // for other push and pop multiple registers encoding
                fmt = IF_T2_I1;
            }
            else
            {
                // We have to use the Thumb-2 push/pop single register encoding
                if (hasLR)
                {
                    imm |= 0x4000;
                }
                regNumber reg = genRegNumFromMask(imm);
                emitIns_R(ins, attr, reg);
                return;
            }

            //
            // Encode the PC and LR bits as the lowest two bits
            //
            imm <<= 2;
            if (hasPC)
                imm |= 2;
            if (hasLR)
                imm |= 1;

            assert(imm != 0);

            break;

#if 0
    // TODO-ARM-Cleanup: Enable or delete.
    case INS_bkpt:   // Windows uses a different encoding
        if ((imm & 0x0000ffff) == imm)
        {
            fmt = IF_T1_L0;
        }
        else
        {
            assert(!"Instruction cannot be encoded");
        }
        break;
#endif

        case INS_dmb:
        case INS_ism:
            if ((imm & 0x000f) == imm)
            {
                fmt  = IF_T2_B;
                attr = EA_4BYTE;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        default:
            unreached();
    }
    assert((fmt == IF_T1_B) || (fmt == IF_T1_L0) || (fmt == IF_T1_L1) || (fmt == IF_T2_I1) || (fmt == IF_T2_B));

    instrDesc* id  = emitNewInstrSC(attr, imm);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a single register.
 */

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_pop:
        case INS_push:
            if (isLowRegister(reg))
            {
                int regmask = 1 << ((int)reg);
                emitIns_I(ins, attr, regmask);
                return;
            }
            assert(size == EA_PTRSIZE);
            fmt = IF_T2_E2;
            break;

        case INS_vmrs:
            assert(size == EA_PTRSIZE);
            fmt = IF_T2_E2;
            break;

        case INS_bx:
            assert(size == EA_PTRSIZE);
            fmt = IF_T1_D1;
            break;
        case INS_rsb:
        case INS_mvn:
            emitIns_R_R_I(ins, attr, reg, reg, 0);
            return;

        default:
            unreached();
    }
    assert((fmt == IF_T1_D1) || (fmt == IF_T2_E2));

    instrDesc* id  = emitNewInstrSmall(attr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction    ins,
                          emitAttr       attr,
                          regNumber      reg,
                          target_ssize_t imm,
                          insFlags flags /* = INS_FLAGS_DONT_CARE */ DEBUGARG(GenTreeFlags gtFlags))

{
    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_DONT_CARE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_add:
        case INS_sub:
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            if ((reg == REG_SP) && insDoesNotSetFlags(flags) && ((imm & 0x01fc) == imm))
            {
                fmt = IF_T1_F;
                sf  = INS_FLAGS_NOT_SET;
            }
            else if (isLowRegister(reg) && insSetsFlags(flags) && (unsigned_abs(imm) <= 0x00ff))
            {
                if (imm < 0)
                {
                    assert((ins == INS_add) || (ins == INS_sub));
                    if (ins == INS_add)
                        ins = INS_sub;
                    else // ins == INS_sub
                        ins = INS_add;
                    imm     = -imm;
                }
                fmt = IF_T1_J0;
                sf  = INS_FLAGS_SET;
            }
            else
            {
                // otherwise we have to use a Thumb-2 encoding
                emitIns_R_R_I(ins, attr, reg, reg, imm, flags);
                return;
            }
            break;

        case INS_adc:
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            emitIns_R_R_I(ins, attr, reg, reg, imm, flags);
            return;

        case INS_vpush:
        case INS_vpop:
            assert(imm > 0);
            if (attr == EA_8BYTE)
            {
                assert(isDoubleReg(reg));
                assert(imm <= 16);
                imm *= 2;
            }
            else
            {
                assert(attr == EA_4BYTE);
                assert(isFloatReg(reg));
                assert(imm <= 16);
            }
            assert(((reg - REG_F0) + imm) <= 32);
            imm *= 4;

            if (ins == INS_vpush)
                imm = -imm;

            sf  = INS_FLAGS_NOT_SET;
            fmt = IF_T2_VLDST;
            break;

        case INS_stm:
        {
            sf = INS_FLAGS_NOT_SET;

            bool hasLR  = false;
            bool hasPC  = false;
            bool useT2  = false;
            bool onlyT1 = false;

            assert((imm & 0x2000) == 0);      // Cannot pop SP
            assert((imm & 0xC000) != 0xC000); // Cannot pop both PC and LR
            assert((imm & 0xFFFF0000) == 0);  // Can only contain lower 16 bits

            if (imm & 0x8000) // Is the PC being popped?
                hasPC = true;

            if (imm & 0x4000) // Is the LR being pushed?
            {
                hasLR = true;
                useT2 = true;
            }

            if (!isLowRegister(reg))
                useT2 = true;

            if (((imm - 1) & imm) == 0) // Is only one or zero bits set in imm?
            {
                if (((imm == 0) && !hasLR) || // imm has no bits set, but hasLR is set
                    (!hasPC && !hasLR))       // imm has one bit set, and neither of hasPC/hasLR are set
                {
                    onlyT1 = true; // if only one bit is set we must use the T1 encoding
                }
            }

            imm &= ~0xE000; // ensure that PC, LR and SP bits are removed from imm

            if (((imm & 0x00ff) == imm) && !useT2)
            {
                fmt = IF_T1_J1;
            }
            else if (!onlyT1)
            {
                fmt = IF_T2_I0;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
                // We have to use the Thumb-2 str single register encoding
                // reg = genRegNumFromMask(imm);
                // emitIns_R(ins, attr, reg);
                return;
            }

            //
            // Encode the PC and LR bits as the lowest two bits
            //
            if (fmt == IF_T2_I0)
            {
                imm <<= 2;
                if (hasPC)
                    imm |= 2;
                if (hasLR)
                    imm |= 1;
            }
            assert(imm != 0);
        }
        break;

        case INS_and:
        case INS_bic:
        case INS_eor:
        case INS_orr:
        case INS_orn:
        case INS_rsb:
        case INS_sbc:

        case INS_ror:
        case INS_asr:
        case INS_lsl:
        case INS_lsr:
            // use the Reg, Reg, Imm encoding
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            emitIns_R_R_I(ins, attr, reg, reg, imm, flags);
            return;

        case INS_mov:
            assert(!EA_IS_CNS_RELOC(attr));

            if (isLowRegister(reg) && insSetsFlags(flags) && ((imm & 0x00ff) == imm))
            {
                fmt = IF_T1_J0;
                sf  = INS_FLAGS_SET;
            }
            else if (isModImmConst(imm))
            {
                fmt = IF_T2_L1;
                sf  = insMustSetFlags(flags);
            }
            else if (isModImmConst(~imm)) // See if we can use move negated instruction instead
            {
                ins = INS_mvn;
                imm = ~imm;
                fmt = IF_T2_L1;
                sf  = insMustSetFlags(flags);
            }
            else if (insDoesNotSetFlags(flags) && ((imm & 0x0000ffff) == imm))
            {
                // mov => movw instruction
                ins = INS_movw;
                fmt = IF_T2_N;
                sf  = INS_FLAGS_NOT_SET;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_movw:
        case INS_movt:
            assert(!EA_IS_RELOC(attr));
            assert(insDoesNotSetFlags(flags));

            if ((imm & 0x0000ffff) == imm)
            {
                fmt = IF_T2_N;
                sf  = INS_FLAGS_NOT_SET;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_mvn:
            if (isModImmConst(imm))
            {
                fmt = IF_T2_L1;
                sf  = insMustSetFlags(flags);
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_cmp:
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(!EA_IS_CNS_RELOC(attr));
            assert(insSetsFlags(flags));
            sf = INS_FLAGS_SET;
            if (isLowRegister(reg) && ((imm & 0x0ff) == imm))
            {
                fmt = IF_T1_J0;
            }
            else if (isModImmConst(imm))
            {
                fmt = IF_T2_L2;
            }
            else if (isModImmConst(-imm))
            {
                ins = INS_cmn;
                fmt = IF_T2_L2;
                imm = -imm;
            }
            else
            {
                assert(!"emitIns_R_I: immediate doesn't fit into the instruction");
                return;
            }
            break;

        case INS_cmn:
        case INS_tst:
        case INS_teq:
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(insSetsFlags(flags));
            sf = INS_FLAGS_SET;
            if (isModImmConst(imm))
            {
                fmt = IF_T2_L2;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

#ifdef FEATURE_PLI_INSTRUCTION
        case INS_pli:
            assert(insDoesNotSetFlags(flags));
            if ((reg == REG_SP) && (unsigned_abs(imm) <= 0x0fff))
            {
                fmt = IF_T2_K3;
                sf  = INS_FLAGS_NOT_SET;
            }
            FALLTHROUGH;
#endif // FEATURE_PLI_INSTRUCTION

        case INS_pld:
        case INS_pldw:
            assert(insDoesNotSetFlags(flags));
            sf = INS_FLAGS_NOT_SET;
            if ((imm >= 0) && (imm <= 0x0fff))
            {
                fmt = IF_T2_K2;
            }
            else if ((imm < 0) && (-imm <= 0x00ff))
            {
                imm = -imm;
                fmt = IF_T2_H2;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        default:
            unreached();
    }
    assert((fmt == IF_T1_F) || (fmt == IF_T1_J0) || (fmt == IF_T1_J1) || (fmt == IF_T2_H2) || (fmt == IF_T2_I0) ||
           (fmt == IF_T2_K2) || (fmt == IF_T2_K3) || (fmt == IF_T2_L1) || (fmt == IF_T2_L2) || (fmt == IF_T2_M1) ||
           (fmt == IF_T2_N) || (fmt == IF_T2_VLDST));

    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSC(attr, imm);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg);
    INDEBUG(id->idDebugOnlyInfo()->idFlags = gtFlags);

    dispIns(id);
    appendToCurIG(id);
}

void emitter::emitIns_MovRelocatableImmediate(instruction ins, emitAttr attr, regNumber reg, BYTE* addr)
{
    assert(EA_IS_RELOC(attr));
    assert((ins == INS_movw) || (ins == INS_movt));

    insFormat fmt = IF_T2_N3;
    insFlags  sf  = INS_FLAGS_NOT_SET;

    instrDesc* id  = emitNewInstrReloc(attr, addr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
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
//    flags     -- The instructiion flags
//
void emitter::emitIns_Mov(instruction ins,
                          emitAttr    attr,
                          regNumber   dstReg,
                          regNumber   srcReg,
                          bool        canSkip,
                          insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
    assert(IsMovInstruction(ins));

    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;
    insFlags  sf   = INS_FLAGS_DONT_CARE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_mov:
        {
            if (insDoesNotSetFlags(flags))
            {
                if (canSkip && (dstReg == srcReg))
                {
                    // These instructions have no side effect and can be skipped
                    return;
                }
                fmt = IF_T1_D0;
                sf  = INS_FLAGS_NOT_SET;
            }
            else // insSetsFlags(flags)
            {
                sf = INS_FLAGS_SET;
                if (isLowRegister(dstReg) && isLowRegister(srcReg))
                {
                    fmt = IF_T1_E;
                }
                else
                {
                    fmt = IF_T2_C3;
                }
            }
            break;
        }

        case INS_vmov:
        {
            // VM debugging single stepper doesn't support PC register with this instruction.
            assert(dstReg != REG_PC);
            assert(srcReg != REG_PC);

            if (canSkip && (dstReg == srcReg))
            {
                // These instructions have no side effect and can be skipped
                return;
            }

            if (size == EA_8BYTE)
            {
                assert(isDoubleReg(dstReg));
                assert(isDoubleReg(srcReg));
            }
            else
            {
                assert(isFloatReg(dstReg));
                assert(isFloatReg(srcReg));
            }

            fmt = IF_T2_VFP2;
            sf  = INS_FLAGS_NOT_SET;
            break;
        }

        case INS_vmov_i2f:
        {
            // VM debugging single stepper doesn't support PC register with this instruction.
            assert(srcReg != REG_PC);
            assert(isFloatReg(dstReg));
            assert(isGeneralRegister(srcReg));

            fmt = IF_T2_VMOVS;
            sf  = INS_FLAGS_NOT_SET;
            break;
        }

        case INS_vmov_f2i:
        {
            // VM debugging single stepper doesn't support PC register with this instruction.
            assert(dstReg != REG_PC);
            assert(isGeneralRegister(dstReg));
            assert(isFloatReg(srcReg));

            fmt = IF_T2_VMOVS;
            sf  = INS_FLAGS_NOT_SET;
            break;
        }

        case INS_sxtb:
        case INS_uxtb:
        {
            assert(size == EA_4BYTE);
            goto EXTEND_COMMON;
        }

        case INS_sxth:
        case INS_uxth:
        {
            assert(size == EA_4BYTE);

        EXTEND_COMMON:
            if (canSkip && (dstReg == srcReg))
            {
                // There are scenarios such as in genCall where the sign/zero extension should be elided
                return;
            }

            // VM debugging single stepper doesn't support PC register with this instruction.
            assert(dstReg != REG_PC);
            assert(srcReg != REG_PC);
            assert(insDoesNotSetFlags(flags));

            if (isLowRegister(dstReg) && isLowRegister(srcReg))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_NOT_SET;
            }
            else
            {
                // Use the Thumb-2 reg,reg with rotation encoding
                emitIns_R_R_I(ins, attr, dstReg, srcReg, 0, INS_FLAGS_NOT_SET);
                return;
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    assert((fmt == IF_T1_D0) || (fmt == IF_T1_E) || (fmt == IF_T2_C3) || (fmt == IF_T2_VFP2) || (fmt == IF_T2_VMOVS));

    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSmall(attr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(dstReg);
    id->idReg2(srcReg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags /* = INS_FLAGS_DONT_CARE */)

{
    if (IsMovInstruction(ins))
    {
        assert(!"Please use emitIns_Mov() to correctly handle move elision");
        emitIns_Mov(ins, attr, reg1, reg2, /* canSkip */ false, flags);
    }

    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;
    insFlags  sf   = INS_FLAGS_DONT_CARE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_add:
            // VM debugging single stepper doesn't support PC register with this instruction.
            // (but reg2 might be PC for ADD Rn, PC instruction)
            assert(reg1 != REG_PC);
            if (insDoesNotSetFlags(flags))
            {
                fmt = IF_T1_D0;
                sf  = INS_FLAGS_NOT_SET;
                break;
            }
            FALLTHROUGH;

        case INS_sub:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            // Use the Thumb-1 reg,reg,reg encoding
            emitIns_R_R_R(ins, attr, reg1, reg1, reg2, flags);
            return;

        case INS_cmp:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insSetsFlags(flags));
            sf = INS_FLAGS_SET;
            if (isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_E; // both are low registers
            }
            else
            {
                fmt = IF_T1_D0; // one or both are high registers
            }
            break;

        case INS_vcvt_d2i:
        case INS_vcvt_d2u:
        case INS_vcvt_d2f:
            assert(isFloatReg(reg1));
            assert(isDoubleReg(reg2));
            goto VCVT_COMMON;

        case INS_vcvt_f2d:
        case INS_vcvt_u2d:
        case INS_vcvt_i2d:
            assert(isDoubleReg(reg1));
            assert(isFloatReg(reg2));
            goto VCVT_COMMON;

        case INS_vcvt_u2f:
        case INS_vcvt_i2f:
        case INS_vcvt_f2i:
        case INS_vcvt_f2u:
            assert(size == EA_4BYTE);
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            goto VCVT_COMMON;

        case INS_vabs:
        case INS_vsqrt:
        case INS_vcmp:
        case INS_vneg:
            if (size == EA_8BYTE)
            {
                assert(isDoubleReg(reg1));
                assert(isDoubleReg(reg2));
            }
            else
            {
                assert(isFloatReg(reg1));
                assert(isFloatReg(reg2));
            }

        VCVT_COMMON:
            fmt = IF_T2_VFP2;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_vadd:
        case INS_vmul:
        case INS_vsub:
        case INS_vdiv:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            emitIns_R_R_R(ins, attr, reg1, reg1, reg2);
            return;

        case INS_vldr:
        case INS_vstr:
        case INS_ldr:
        case INS_ldrb:
        case INS_ldrsb:
        case INS_ldrh:
        case INS_ldrsh:

        case INS_str:
        case INS_strb:
        case INS_strh:
            emitIns_R_R_I(ins, attr, reg1, reg2, 0);
            return;

        case INS_adc:
        case INS_and:
        case INS_bic:
        case INS_eor:
        case INS_orr:
        case INS_sbc:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            if (insSetsFlags(flags) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_SET;
                break;
            }
            FALLTHROUGH;

        case INS_orn:
            // assert below fired for bug 281892 where the two operands of an OR were
            // the same static field load which got cse'd.
            // there's no reason why this assert would be true in general
            // assert(reg1 != reg2);
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            // Use the Thumb-2 three register encoding
            emitIns_R_R_R_I(ins, attr, reg1, reg1, reg2, 0, flags);
            return;

        case INS_asr:
        case INS_lsl:
        case INS_lsr:
        case INS_ror:
            // assert below fired for bug 296394 where the two operands of an
            // arithmetic right shift were the same local variable
            // there's no reason why this assert would be true in general
            // assert(reg1 != reg2);
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            if (insSetsFlags(flags) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_SET;
            }
            else
            {
                // Use the Thumb-2 three register encoding
                emitIns_R_R_R(ins, attr, reg1, reg1, reg2, flags);
                return;
            }
            break;

        case INS_mul:
            // We will prefer the T2 encoding, unless (flags == INS_FLAGS_SET)
            // The thumb-1 instruction executes much slower as it must always set the flags
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            if (insMustSetFlags(flags) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_SET;
            }
            else
            {
                // Use the Thumb-2 three register encoding
                emitIns_R_R_R(ins, attr, reg1, reg2, reg1, flags);
                return;
            }
            break;

        case INS_mvn:
        case INS_cmn:
        case INS_tst:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            if (insSetsFlags(flags) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_SET;
            }
            else
            {
                // Use the Thumb-2 register with shift encoding
                emitIns_R_R_I(ins, attr, reg1, reg2, 0, flags);
                return;
            }
            break;

        case INS_tbb:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_C9;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_tbh:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_C9;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_clz:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_C10;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrexb:
        case INS_strexb:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_E1;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrexh:
        case INS_strexh:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_E1;
            sf  = INS_FLAGS_NOT_SET;
            break;
        default:
#ifdef DEBUG
            printf("did not expect instruction %s\n", codeGen->genInsName(ins));
#endif
            unreached();
    }

    assert((fmt == IF_T1_D0) || (fmt == IF_T1_E) || (fmt == IF_T2_C3) || (fmt == IF_T2_C9) || (fmt == IF_T2_C10) ||
           (fmt == IF_T2_VFP2) || (fmt == IF_T2_VMOVD) || (fmt == IF_T2_VMOVS) || (fmt == IF_T2_E1));

    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSmall(attr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and two constants.
 */

void emitter::emitIns_R_I_I(
    instruction ins, emitAttr attr, regNumber reg, int imm1, int imm2, insFlags flags /* = INS_FLAGS_DONT_CARE */)

{
    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_DONT_CARE;
    int       imm = 0; // combined immediates

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_bfc:
        {
            assert(reg != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.

            int lsb = imm1;
            int msb = lsb + imm2 - 1;

            assert((lsb >= 0) && (lsb <= 31)); // required for encoding of INS_bfc
            assert((msb >= 0) && (msb <= 31)); // required for encoding of INS_bfc
            assert(msb >= lsb);                // required for encoding of INS_bfc

            imm = (lsb << 5) | msb;

            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_D1;
            sf  = INS_FLAGS_NOT_SET;
        }
        break;

        default:
            unreached();
    }
    assert(fmt == IF_T2_D1);
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSC(attr, imm);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(instruction ins,
                            emitAttr    attr,
                            regNumber   reg1,
                            regNumber   reg2,
                            int         imm,
                            insFlags    flags /* = INS_FLAGS_DONT_CARE */,
                            insOpts     opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;
    insFlags  sf   = INS_FLAGS_DONT_CARE;

    if (ins == INS_lea)
    {
        ins = INS_add;
    }

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_add:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));

            // Can we possibly encode the immediate 'imm' using a Thumb-1 encoding?
            if ((reg2 == REG_SP) && insDoesNotSetFlags(flags) && ((imm & 0x03fc) == imm))
            {
                if ((reg1 == REG_SP) && ((imm & 0x01fc) == imm))
                {
                    // Use Thumb-1 encoding
                    emitIns_R_I(ins, attr, reg1, imm, flags);
                    return;
                }
                else if (isLowRegister(reg1))
                {
                    fmt = IF_T1_J2;
                    sf  = INS_FLAGS_NOT_SET;
                    break;
                }
            }
            FALLTHROUGH;

        case INS_sub:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));

            // Is it just a mov?
            if ((imm == 0) && insDoesNotSetFlags(flags))
            {
                // Is the mov even necessary?
                // Fix 383915 ARM ILGEN
                emitIns_Mov(INS_mov, attr, reg1, reg2, /* canSkip */ true, flags);
                return;
            }
            // Can we encode the immediate 'imm' using a Thumb-1 encoding?
            else if (isLowRegister(reg1) && isLowRegister(reg2) && insSetsFlags(flags) && (unsigned_abs(imm) <= 0x0007))
            {
                if (imm < 0)
                {
                    assert((ins == INS_add) || (ins == INS_sub));
                    if (ins == INS_add)
                        ins = INS_sub;
                    else
                        ins = INS_add;
                    imm     = -imm;
                }
                fmt = IF_T1_G;
                sf  = INS_FLAGS_SET;
            }
            else if ((reg1 == reg2) && isLowRegister(reg1) && insSetsFlags(flags) && (unsigned_abs(imm) <= 0x00ff))
            {
                if (imm < 0)
                {
                    assert((ins == INS_add) || (ins == INS_sub));
                    if (ins == INS_add)
                        ins = INS_sub;
                    else
                        ins = INS_add;
                    imm     = -imm;
                }
                // Use Thumb-1 encoding
                emitIns_R_I(ins, attr, reg1, imm, flags);
                return;
            }
            else if (isModImmConst(imm))
            {
                fmt = IF_T2_L0;
                sf  = insMustSetFlags(flags);
            }
            else if (isModImmConst(-imm))
            {
                assert((ins == INS_add) || (ins == INS_sub));
                ins = (ins == INS_add) ? INS_sub : INS_add;
                imm = -imm;
                fmt = IF_T2_L0;
                sf  = insMustSetFlags(flags);
            }
            else if (insDoesNotSetFlags(flags) && (unsigned_abs(imm) <= 0x0fff))
            {
                if (imm < 0)
                {
                    assert((ins == INS_add) || (ins == INS_sub));
                    ins = (ins == INS_add) ? INS_sub : INS_add;
                    imm = -imm;
                }
                // add/sub => addw/subw instruction
                // Note that even when using the w prefix the immediate is still only 12 bits?
                ins = (ins == INS_add) ? INS_addw : INS_subw;
                fmt = IF_T2_M0;
                sf  = INS_FLAGS_NOT_SET;
            }
            else if (insDoesNotSetFlags(flags) && (reg1 != REG_SP) && (reg1 != REG_PC))
            {
                // movw,movt reg1, imm
                codeGen->instGen_Set_Reg_To_Imm(attr, reg1, (ins == INS_sub ? -1 : 1) * imm);

                // ins reg1, reg2
                emitIns_R_R(INS_add, attr, reg1, reg2);

                return;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_and:
        case INS_bic:
        case INS_orr:
        case INS_orn:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));
            if (isModImmConst(imm))
            {
                fmt = IF_T2_L0;
                sf  = insMustSetFlags(flags);
            }
            else if (isModImmConst(~imm))
            {
                fmt = IF_T2_L0;
                sf  = insMustSetFlags(flags);
                imm = ~imm;

                if (ins == INS_and)
                    ins = INS_bic;
                else if (ins == INS_bic)
                    ins = INS_and;
                else if (ins == INS_orr)
                    ins = INS_orn;
                else if (ins == INS_orn)
                    ins = INS_orr;
                else
                    assert(!"Instruction cannot be encoded");
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_rsb:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));
            if (imm == 0 && isLowRegister(reg1) && isLowRegister(reg2) && insSetsFlags(flags))
            {
                fmt = IF_T1_E;
                sf  = INS_FLAGS_SET;
                break;
            }
            FALLTHROUGH;

        case INS_adc:
        case INS_eor:
        case INS_sbc:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));
            if (isModImmConst(imm))
            {
                fmt = IF_T2_L0;
                sf  = insMustSetFlags(flags);
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_adr:
            assert(insOptsNone(opt));
            assert(insDoesNotSetFlags(flags));
            assert(reg2 == REG_PC);
            sf = INS_FLAGS_NOT_SET;

            if (isLowRegister(reg1) && ((imm & 0x00ff) == imm))
            {
                fmt = IF_T1_J3;
            }
            else if ((imm & 0x0fff) == imm)
            {
                fmt = IF_T2_M1;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        case INS_mvn:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert((imm >= 0) && (imm <= 31)); // required for encoding
            assert(!insOptAnyInc(opt));
            if (imm == 0)
            {
                assert(insOptsNone(opt));
                if (isLowRegister(reg1) && isLowRegister(reg2) && insSetsFlags(flags))
                {
                    // Use the Thumb-1 reg,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg2, flags);
                    return;
                }
            }
            else // imm > 0  &&  imm <= 31
            {
                assert(insOptAnyShift(opt));
            }
            fmt = IF_T2_C1;
            sf  = insMustSetFlags(flags);
            break;

        case INS_cmp:
        case INS_cmn:
        case INS_teq:
        case INS_tst:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insSetsFlags(flags));
            assert((imm >= 0) && (imm <= 31)); // required for encoding
            assert(!insOptAnyInc(opt));
            if (imm == 0)
            {
                assert(insOptsNone(opt));
                if (ins == INS_cmp)
                {
                    // Use the Thumb-1 reg,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg2, flags);
                    return;
                }
                if (((ins == INS_cmn) || (ins == INS_tst)) && isLowRegister(reg1) && isLowRegister(reg2))
                {
                    // Use the Thumb-1 reg,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg2, flags);
                    return;
                }
            }
            else // imm > 0  &&  imm <= 31)
            {
                assert(insOptAnyShift(opt));
                if (insOptsRRX(opt))
                    assert(imm == 1);
            }

            fmt = IF_T2_C8;
            sf  = INS_FLAGS_SET;
            break;

        case INS_ror:
        case INS_asr:
        case INS_lsl:
        case INS_lsr:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));

            // On ARM, the immediate shift count of LSL and ROR must be between 1 and 31. For LSR and ASR, it is between
            // 1 and 32, though we don't ever use 32. Although x86 allows an immediate shift count of 8-bits in
            // instruction encoding, the CPU looks at only the lower 5 bits. As per ECMA, specifying a shift count to
            // the IL SHR, SHL, or SHL.UN instruction that is greater than or equal to the width of the type will yield
            // an undefined value. We choose that undefined value in this case to match x86 behavior, by only using the
            // lower 5 bits of the constant shift count.
            imm &= 0x1f;

            if (imm == 0)
            {
                // Additional Fix 383915 ARM ILGEN
                emitIns_Mov(INS_mov, attr, reg1, reg2, /* canSkip */ !insMustSetFlags(flags), flags);
                return;
            }

            if (insSetsFlags(flags) && (ins != INS_ror) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                fmt = IF_T1_C;
                sf  = INS_FLAGS_SET;
            }
            else
            {
                fmt = IF_T2_C2;
                sf  = insMustSetFlags(flags);
            }
            break;

        case INS_sxtb:
        case INS_uxtb:
            assert(size == EA_4BYTE);
            goto EXTEND_COMMON;

        case INS_sxth:
        case INS_uxth:
            assert(size == EA_4BYTE);
        EXTEND_COMMON:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(insOptsNone(opt));
            assert(insDoesNotSetFlags(flags));
            assert((imm & 0x018) == imm); // required for encoding

            if ((imm == 0) && isLowRegister(reg1) && isLowRegister(reg2))
            {
                // Use Thumb-1 encoding
                emitIns_R_R(ins, attr, reg1, reg2, INS_FLAGS_NOT_SET);
                return;
            }

            fmt = IF_T2_C6;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_pld:
        case INS_pldw:
#ifdef FEATURE_PLI_INSTRUCTION
        case INS_pli:
#endif // FEATURE_PLI_INSTRUCTION
            assert(insOptsNone(opt));
            assert(insDoesNotSetFlags(flags));
            assert((imm & 0x003) == imm); // required for encoding

            fmt = IF_T2_C7;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrb:
        case INS_strb:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));

            if (isLowRegister(reg1) && isLowRegister(reg2) && insOptsNone(opt) && ((imm & 0x001f) == imm))
            {
                fmt = IF_T1_C;
                sf  = INS_FLAGS_NOT_SET;
                break;
            }
            goto COMMON_THUMB2_LDST;

        case INS_ldrsb:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB2_LDST;

        case INS_ldrh:
        case INS_strh:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));

            if (isLowRegister(reg1) && isLowRegister(reg2) && insOptsNone(opt) && ((imm & 0x003e) == imm))
            {
                fmt = IF_T1_C;
                sf  = INS_FLAGS_NOT_SET;
                break;
            }
            goto COMMON_THUMB2_LDST;

        case INS_ldrsh:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB2_LDST;

        case INS_vldr:
        case INS_vstr:
        case INS_vldm:
        case INS_vstm:
            assert(fmt == IF_NONE);
            assert(insDoesNotSetFlags(flags));
            assert(offsetFitsInVectorMem(imm)); // required for encoding
            if (insOptAnyInc(opt))
            {
                if (insOptsPostInc(opt))
                {
                    assert(imm > 0);
                }
                else // insOptsPreDec(opt)
                {
                    assert(imm < 0);
                }
            }
            else
            {
                assert(insOptsNone(opt));
            }

            sf  = INS_FLAGS_NOT_SET;
            fmt = IF_T2_VLDST;
            break;

        case INS_ldr:
        case INS_str:
            assert(size == EA_4BYTE);
            assert(insDoesNotSetFlags(flags));

            // Can we possibly encode the immediate 'imm' using a Thumb-1 encoding?
            if (isLowRegister(reg1) && insOptsNone(opt) && ((imm & 0x03fc) == imm))
            {
                if (reg2 == REG_SP)
                {
                    fmt = IF_T1_J2;
                    sf  = INS_FLAGS_NOT_SET;
                    break;
                }
                else if (reg2 == REG_PC)
                {
                    if (ins == INS_ldr)
                    {
                        fmt = IF_T1_J3;
                        sf  = INS_FLAGS_NOT_SET;
                        break;
                    }
                }
                else if (isLowRegister(reg2))
                {
                    // Only the smaller range 'imm' can be encoded
                    if ((imm & 0x07c) == imm)
                    {
                        fmt = IF_T1_C;
                        sf  = INS_FLAGS_NOT_SET;
                        break;
                    }
                }
            }
        //
        // If we did not find a thumb-1 encoding above
        //

        COMMON_THUMB2_LDST:
            assert(fmt == IF_NONE);
            assert(insDoesNotSetFlags(flags));
            sf = INS_FLAGS_NOT_SET;

            if (insOptAnyInc(opt))
            {
                if (insOptsPostInc(opt))
                    assert(imm > 0);
                else // insOptsPreDec(opt)
                    assert(imm < 0);

                if (unsigned_abs(imm) <= 0x00ff)
                {
                    fmt = IF_T2_H0;
                }
                else
                {
                    assert(!"Instruction cannot be encoded");
                }
            }
            else
            {
                assert(insOptsNone(opt));
                if ((reg2 == REG_PC) && (unsigned_abs(imm) <= 0x0fff))
                {
                    fmt = IF_T2_K4;
                }
                else if ((imm & 0x0fff) == imm)
                {
                    fmt = IF_T2_K1;
                }
                else if (unsigned_abs(imm) <= 0x0ff)
                {
                    fmt = IF_T2_H0;
                }
                else
                {
                    // Load imm into a register
                    regNumber rsvdReg = codeGen->rsGetRsvdReg();
                    codeGen->instGen_Set_Reg_To_Imm(EA_4BYTE, rsvdReg, (ssize_t)imm);
                    emitIns_R_R_R(ins, attr, reg1, reg2, rsvdReg);
                    return;
                }
            }
            break;

        case INS_ldrex:
        case INS_strex:
            assert(insOptsNone(opt));
            assert(insDoesNotSetFlags(flags));
            sf = INS_FLAGS_NOT_SET;

            if ((imm & 0x03fc) == imm)
            {
                fmt = IF_T2_H0;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        default:
            assert(!"Unexpected instruction");
    }
    assert((fmt == IF_T1_C) || (fmt == IF_T1_E) || (fmt == IF_T1_G) || (fmt == IF_T1_J2) || (fmt == IF_T1_J3) ||
           (fmt == IF_T2_C1) || (fmt == IF_T2_C2) || (fmt == IF_T2_C6) || (fmt == IF_T2_C7) || (fmt == IF_T2_C8) ||
           (fmt == IF_T2_H0) || (fmt == IF_T2_H1) || (fmt == IF_T2_K1) || (fmt == IF_T2_K4) || (fmt == IF_T2_L0) ||
           (fmt == IF_T2_M0) || (fmt == IF_T2_VLDST) || (fmt == IF_T2_M1));
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSC(attr, imm);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idInsOpt(opt);
    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(instruction ins,
                            emitAttr    attr,
                            regNumber   reg1,
                            regNumber   reg2,
                            regNumber   reg3,
                            insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;
    insFlags  sf   = INS_FLAGS_DONT_CARE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_add:
            // Encodings do not support SP in the reg3 slot
            if (reg3 == REG_SP)
            {
                // Swap reg2 and reg3
                reg3 = reg2;
                reg2 = REG_SP;
            }
            FALLTHROUGH;

        case INS_sub:
            assert(reg3 != REG_SP);
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC || ins == INS_add); // allow ADD Rn, PC instruction in T2 encoding

            if (isLowRegister(reg1) && isLowRegister(reg2) && isLowRegister(reg3) && insSetsFlags(flags))
            {
                fmt = IF_T1_H;
                sf  = INS_FLAGS_SET;
                break;
            }

            if ((ins == INS_add) && insDoesNotSetFlags(flags))
            {
                if (reg1 == reg2)
                {
                    // Use the Thumb-1 regdest,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg3, flags);
                    return;
                }
                if (reg1 == reg3)
                {
                    // Use the Thumb-1 regdest,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg2, flags);
                    return;
                }
            }

            // Use the Thumb-2 reg,reg,reg with shift encoding
            emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0, flags);
            return;

        case INS_adc:
        case INS_and:
        case INS_bic:
        case INS_eor:
        case INS_orr:
        case INS_sbc:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            if (reg1 == reg2)
            {
                // Try to encode as a Thumb-1 instruction
                emitIns_R_R(ins, attr, reg1, reg3, flags);
                return;
            }
            FALLTHROUGH;

        case INS_orn:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            // Use the Thumb-2 three register encoding, with imm=0
            emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0, flags);
            return;

        case INS_asr:
        case INS_lsl:
        case INS_lsr:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            if (reg1 == reg2 && insSetsFlags(flags) && isLowRegister(reg1) && isLowRegister(reg3))
            {
                // Use the Thumb-1 regdest,reg encoding
                emitIns_R_R(ins, attr, reg1, reg3, flags);
                return;
            }
            FALLTHROUGH;

        case INS_ror:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            fmt = IF_T2_C4;
            sf  = insMustSetFlags(flags);
            break;

        case INS_mul:
            if (insMustSetFlags(flags))
            {
                assert(reg1 !=
                       REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
                assert(reg2 != REG_PC);
                assert(reg3 != REG_PC);

                if ((reg1 == reg2) && isLowRegister(reg1))
                {
                    // Use the Thumb-1 regdest,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg3, flags);
                    return;
                }
                if ((reg1 == reg3) && isLowRegister(reg1))
                {
                    // Use the Thumb-1 regdest,reg encoding
                    emitIns_R_R(ins, attr, reg1, reg2, flags);
                    return;
                }
                else
                {
                    assert(!"Instruction cannot be encoded");
                }
            }

#if !defined(USE_HELPERS_FOR_INT_DIV)
            FALLTHROUGH;
        case INS_sdiv:
        case INS_udiv:
#endif // !USE_HELPERS_FOR_INT_DIV

            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_C5;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrb:
        case INS_strb:
        case INS_ldrsb:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB1_LDST;

        case INS_ldrsh:
        case INS_ldrh:
        case INS_strh:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB1_LDST;

        case INS_ldr:
        case INS_str:
            assert(size == EA_4BYTE);

        COMMON_THUMB1_LDST:
            assert(insDoesNotSetFlags(flags));

            if (isLowRegister(reg1) && isLowRegister(reg2) && isLowRegister(reg3))
            {
                fmt = IF_T1_H;
                sf  = INS_FLAGS_NOT_SET;
            }
            else
            {
                // Use the Thumb-2 reg,reg,reg with shift encoding
                emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0, flags);
                return;
            }
            break;

        case INS_vadd:
        case INS_vmul:
        case INS_vsub:
        case INS_vdiv:
            if (size == EA_8BYTE)
            {
                assert(isDoubleReg(reg1));
                assert(isDoubleReg(reg2));
                assert(isDoubleReg(reg3));
            }
            else
            {
                assert(isFloatReg(reg1));
                assert(isFloatReg(reg2));
                assert(isFloatReg(reg3));
            }
            fmt = IF_T2_VFP3;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_vmov_i2d:
            assert(reg2 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg3 != REG_PC);
            assert(isDoubleReg(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            fmt = IF_T2_VMOVD;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_vmov_d2i:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isDoubleReg(reg3));
            fmt = IF_T2_VMOVD;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrexd:
        case INS_strexd:
            assert(insDoesNotSetFlags(flags));
            fmt = IF_T2_G1;
            sf  = INS_FLAGS_NOT_SET;
            break;

        default:
            unreached();
    }
    assert((fmt == IF_T1_H) || (fmt == IF_T2_C4) || (fmt == IF_T2_C5) || (fmt == IF_T2_VFP3) || (fmt == IF_T2_VMOVD) ||
           (fmt == IF_T2_G1));
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstr(attr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              int         imm1,
                              int         imm2,
                              insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_DONT_CARE;

    int lsb   = imm1;
    int width = imm2;
    int msb   = lsb + width - 1;
    int imm   = 0; /* combined immediate */

    assert((lsb >= 0) && (lsb <= 31));    // required for encodings
    assert((width > 0) && (width <= 32)); // required for encodings
    assert((msb >= 0) && (msb <= 31));    // required for encodings
    assert(msb >= lsb);                   // required for encodings

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_bfi:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);

            assert(insDoesNotSetFlags(flags));
            imm = (lsb << 5) | msb;

            fmt = IF_T2_D0;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_sbfx:
        case INS_ubfx:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);

            assert(insDoesNotSetFlags(flags));
            imm = (lsb << 5) | (width - 1);

            fmt = IF_T2_D0;
            sf  = INS_FLAGS_NOT_SET;
            break;

        default:
            unreached();
    }
    assert((fmt == IF_T2_D0));
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrSC(attr, imm);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers and a constant.
 */

void emitter::emitIns_R_R_R_I(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              regNumber   reg3,
                              int         imm,
                              insFlags    flags /* = INS_FLAGS_DONT_CARE */,
                              insOpts     opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;
    insFlags  sf   = INS_FLAGS_DONT_CARE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {

        case INS_add:
        case INS_sub:
            if (imm == 0)
            {
                if (isLowRegister(reg1) && isLowRegister(reg2) && isLowRegister(reg3) && insSetsFlags(flags))
                {
                    // Use the Thumb-1 reg,reg,reg encoding
                    emitIns_R_R_R(ins, attr, reg1, reg2, reg3, flags);
                    return;
                }
                if ((ins == INS_add) && insDoesNotSetFlags(flags))
                {
                    if (reg1 == reg2)
                    {
                        // Use the Thumb-1 regdest,reg encoding
                        emitIns_R_R(ins, attr, reg1, reg3, flags);
                        return;
                    }
                    if (reg1 == reg3)
                    {
                        // Use the Thumb-1 regdest,reg encoding
                        emitIns_R_R(ins, attr, reg1, reg2, flags);
                        return;
                    }
                }
            }
            FALLTHROUGH;

        case INS_adc:
        case INS_and:
        case INS_bic:
        case INS_eor:
        case INS_orn:
        case INS_orr:
        case INS_sbc:
            assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
            assert(reg2 != REG_PC);
            assert(reg3 != REG_PC);
            assert((imm >= 0) && (imm <= 31)); // required for encoding
            assert(!insOptAnyInc(opt));
            if (imm == 0)
            {
                if (opt == INS_OPTS_LSL) // left shift of zero
                    opt = INS_OPTS_NONE; //           is a nop

                assert(insOptsNone(opt));
                if (isLowRegister(reg1) && isLowRegister(reg2) && isLowRegister(reg3) && insSetsFlags(flags))
                {
                    if (reg1 == reg2)
                    {
                        // Use the Thumb-1 regdest,reg encoding
                        emitIns_R_R(ins, attr, reg1, reg3, flags);
                        return;
                    }
                    if ((reg1 == reg3) && (ins != INS_bic) && (ins != INS_orn) && (ins != INS_sbc))
                    {
                        // Use the Thumb-1 regdest,reg encoding
                        emitIns_R_R(ins, attr, reg1, reg2, flags);
                        return;
                    }
                }
            }
            else // imm > 0  &&  imm <= 31)
            {
                assert(insOptAnyShift(opt));
                if (insOptsRRX(opt))
                    assert(imm == 1);
            }
            fmt = IF_T2_C0;
            sf  = insMustSetFlags(flags);
            break;

        case INS_ldrb:
        case INS_ldrsb:
        case INS_strb:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB2_LDST;

        case INS_ldrh:
        case INS_ldrsh:
        case INS_strh:
            assert(size == EA_4BYTE);
            goto COMMON_THUMB2_LDST;

        case INS_ldr:
        case INS_str:
            assert(size == EA_4BYTE);

        COMMON_THUMB2_LDST:
            assert(insDoesNotSetFlags(flags));
            assert((imm & 0x0003) == imm); // required for encoding

            if ((imm == 0) && insOptsNone(opt) && isLowRegister(reg1) && isLowRegister(reg2) && isLowRegister(reg3))
            {
                // Use the Thumb-1 reg,reg,reg encoding
                emitIns_R_R_R(ins, attr, reg1, reg2, reg3, flags);
                return;
            }
            assert(insOptsNone(opt) || insOptsLSL(opt));
            fmt = IF_T2_E0;
            sf  = INS_FLAGS_NOT_SET;
            break;

        case INS_ldrd:
        case INS_strd:
            assert(insDoesNotSetFlags(flags));
            assert((imm & 0x03) == 0);
            sf = INS_FLAGS_NOT_SET;

            if (insOptAnyInc(opt))
            {
                if (insOptsPostInc(opt))
                    assert(imm > 0);
                else // insOptsPreDec(opt)
                    assert(imm < 0);
            }
            else
            {
                assert(insOptsNone(opt));
            }

            if (unsigned_abs(imm) <= 0x03fc)
            {
                imm >>= 2;
                fmt = IF_T2_G0;
            }
            else
            {
                assert(!"Instruction cannot be encoded");
            }
            break;

        default:
            unreached();
    }
    assert((fmt == IF_T2_C0) || (fmt == IF_T2_E0) || (fmt == IF_T2_G0));
    assert(sf != INS_FLAGS_DONT_CARE);

    // 3-reg ops can't use the small instrdesc
    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(emitInsSize(fmt));

    id->idInsFlags(sf);
    id->idInsOpt(opt);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing four registers.
 */

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4)
{
    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_NOT_SET;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {

        case INS_smull:
        case INS_umull:
        case INS_smlal:
        case INS_umlal:
            assert(reg1 != reg2); // Illegal encoding
            fmt = IF_T2_F1;
            break;
        case INS_mla:
        case INS_mls:
            fmt = IF_T2_F2;
            break;
        default:
            unreached();
    }
    assert((fmt == IF_T2_F1) || (fmt == IF_T2_F2));

    assert(reg1 != REG_PC); // VM debugging single stepper doesn't support PC register with this instruction.
    assert(reg2 != REG_PC);
    assert(reg3 != REG_PC);
    assert(reg4 != REG_PC);

    instrDesc* id  = emitNewInstr(attr);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a static data member operand. If 'size' is 0, the
 *  instruction operates on the address of the static member instead of its
 *  value (e.g. "push offset clsvar", rather than "push dword ptr [clsvar]").
 */

void emitter::emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    NYI("emitIns_C");
}

/*****************************************************************************
 *
 *  Add an instruction referencing stack-based local variable.
 */

void emitter::emitIns_S(instruction ins, emitAttr attr, int varx, int offs)
{
    NYI("emitIns_S");
}

//-------------------------------------------------------------------------------------
// emitIns_R_S: Add an instruction referencing a register and a stack-based local variable.
//
// Arguments:
//    ins      - The instruction to add.
//    attr     - Oeration size.
//    varx     - The variable to generate offset for.
//    offs     - The offset of variable or field in stack.
//    pBaseReg - The base register that is used while calculating the offset. For example, if the offset
//               with "stack pointer" can't be encoded in instruction, "frame pointer" can be used to get
//               the offset of the field. In such case, pBaseReg will store the "fp".
//
// Return Value:
//    The pBaseReg that holds the base register that was used to calculate the offset.
//
void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs, regNumber* pBaseReg)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Load() to select the correct instruction");
    }

    switch (ins)
    {
        case INS_add:
        case INS_ldr:
        case INS_ldrh:
        case INS_ldrb:
        case INS_ldrsh:
        case INS_ldrsb:
        case INS_vldr:
        case INS_vmov:
        case INS_movw:
        case INS_movt:
            break;

        case INS_lea:
            ins = INS_add;
            break;

        default:
            NYI("emitIns_R_S");
            return;
    }

    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_NOT_SET;
    regNumber reg2;
    regNumber baseRegUsed;

    /* Figure out the variable's frame position */
    int      base;
    int      disp;
    unsigned undisp;

    base = emitComp->lvaFrameAddress(varx, emitComp->funCurrentFunc()->funKind != FUNC_ROOT, &reg2, offs,
                                     CodeGen::instIsFP(ins));
    if (pBaseReg != nullptr)
    {
        *pBaseReg = reg2;
    }

    disp   = base + offs;
    undisp = unsigned_abs(disp);

    if (CodeGen::instIsFP(ins))
    {
        // all fp mem ops take 8 bit immediate, multiplied by 4, plus sign
        //
        // Note if undisp is not a multiple of four we will fail later on
        //   when we try to encode this instruction
        // Its better to fail later with a better error message than
        //   to fail here when the RBM_OPT_RSVD is not available
        //
        if (undisp <= 0x03fc)
        {
            fmt = IF_T2_VLDST;
        }
        else
        {
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            emitIns_genStackOffset(rsvdReg, varx, offs, /* isFloatUsage */ true, &baseRegUsed);
            emitIns_R_R(INS_add, EA_4BYTE, rsvdReg, baseRegUsed);
            emitIns_R_R_I(ins, attr, reg1, rsvdReg, 0);
            return;
        }
    }
    else if (emitInsIsLoadOrStore(ins))
    {
        if (isLowRegister(reg1) && (reg2 == REG_SP) && (ins == INS_ldr) && ((disp & 0x03fc) == disp))
        {
            fmt = IF_T1_J2;
        }
        else if (disp >= 0 && disp <= 0x0fff)
        {
            fmt = IF_T2_K1;
        }
        else if (undisp <= 0x0ff)
        {
            fmt = IF_T2_H0;
        }
        else
        {
            // Load disp into a register
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            emitIns_genStackOffset(rsvdReg, varx, offs, /* isFloatUsage */ false, &baseRegUsed);
            fmt = IF_T2_E0;

            // Ensure the baseReg calculated is correct.
            assert(baseRegUsed == reg2);
        }
    }
    else if (ins == INS_add)
    {
        if (isLowRegister(reg1) && (reg2 == REG_SP) && ((disp & 0x03fc) == disp))
        {
            fmt = IF_T1_J2;
        }
        else if (undisp <= 0x0fff)
        {
            if (disp < 0)
            {
                ins  = INS_sub;
                disp = -disp;
            }
            // add/sub => addw/subw instruction
            // Note that even when using the w prefix the immediate is still only 12 bits?
            ins = (ins == INS_add) ? INS_addw : INS_subw;
            fmt = IF_T2_M0;
        }
        else
        {
            // Load disp into a register
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            emitIns_genStackOffset(rsvdReg, varx, offs, /* isFloatUsage */ false, &baseRegUsed);

            // Ensure the baseReg calculated is correct.
            assert(baseRegUsed == reg2);
            emitIns_R_R_R(ins, attr, reg1, reg2, rsvdReg);
            return;
        }
    }
    else if (ins == INS_movw || ins == INS_movt)
    {
        fmt = IF_T2_N;
    }

    assert((fmt == IF_T1_J2) || (fmt == IF_T2_E0) || (fmt == IF_T2_H0) || (fmt == IF_T2_K1) || (fmt == IF_T2_L0) ||
           (fmt == IF_T2_N) || (fmt == IF_T2_VLDST) || (fmt == IF_T2_M0));
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrCns(attr, disp);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idInsOpt(INS_OPTS_NONE);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();
    if (reg2 == REG_FP)
        id->idSetIsLclFPBase();

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

//-------------------------------------------------------------------------------------
// emitIns_genStackOffset: Generate the offset of &varx + offs into a register
//
// Arguments:
//    r            - Register in which offset calculation result is stored.
//    varx         - The variable to generate offset for.
//    offs         - The offset of variable or field in stack.
//    isFloatUsage - True if the instruction being generated is a floating point instruction. This requires using
//                   floating-point offset restrictions. Note that a variable can be non-float, e.g., struct, but
//                   accessed as a float local field.
//    pBaseReg     - The base register that is used while calculating the offset. For example, if the offset with
//                   "stack pointer" can't be encoded in instruction, "frame pointer" can be used to get the offset
//                   of the field. In such case, pBaseReg will store the "fp".
//
// Return Value:
//    The pBaseReg that holds the base register that was used to calculate the offset.
//
void emitter::emitIns_genStackOffset(regNumber r, int varx, int offs, bool isFloatUsage, regNumber* pBaseReg)
{
    regNumber regBase;
    int       base;
    int       disp;

    base =
        emitComp->lvaFrameAddress(varx, emitComp->funCurrentFunc()->funKind != FUNC_ROOT, &regBase, offs, isFloatUsage);
    disp = base + offs;

    emitIns_R_S(INS_movw, EA_4BYTE, r, varx, offs, pBaseReg);

    if ((disp & 0xffff) != disp)
    {
        regNumber regBaseUsedInMovT;
        emitIns_R_S(INS_movt, EA_4BYTE, r, varx, offs, &regBaseUsedInMovT);
        assert(*pBaseReg == regBaseUsedInMovT);
    }
}

/*****************************************************************************
 *
 *  Add an instruction referencing a stack-based local variable and a register
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Store() to select the correct instruction");
    }

    switch (ins)
    {
        case INS_str:
        case INS_strh:
        case INS_strb:
        case INS_vstr:
            break;

        default:
            NYI("emitIns_R_S");
            return;
    }

    insFormat fmt = IF_NONE;
    insFlags  sf  = INS_FLAGS_NOT_SET;
    regNumber reg2;
    regNumber baseRegUsed;

    /* Figure out the variable's frame position */
    int      base;
    int      disp;
    unsigned undisp;

    base = emitComp->lvaFrameAddress(varx, emitComp->funCurrentFunc()->funKind != FUNC_ROOT, &reg2, offs,
                                     CodeGen::instIsFP(ins));

    disp   = base + offs;
    undisp = unsigned_abs(disp);

    if (CodeGen::instIsFP(ins))
    {
        // all fp mem ops take 8 bit immediate, multiplied by 4, plus sign
        //
        // Note if undisp is not a multiple of four we will fail later on
        //   when we try to encode this instruction
        // Its better to fail later with a better error message than
        //   to fail here when the RBM_OPT_RSVD is not available
        //
        if (undisp <= 0x03fc)
        {
            fmt = IF_T2_VLDST;
        }
        else
        {
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            emitIns_genStackOffset(rsvdReg, varx, offs, /* isFloatUsage */ true, &baseRegUsed);

            // Ensure the baseReg calculated is correct.
            assert(baseRegUsed == reg2);
            emitIns_R_R(INS_add, EA_4BYTE, rsvdReg, reg2);
            emitIns_R_R_I(ins, attr, reg1, rsvdReg, 0);
            return;
        }
    }
    else if (isLowRegister(reg1) && (reg2 == REG_SP) && (ins == INS_str) && ((disp & 0x03fc) == disp))
    {
        fmt = IF_T1_J2;
    }
    else if (disp >= 0 && disp <= 0x0fff)
    {
        fmt = IF_T2_K1;
    }
    else if (undisp <= 0x0ff)
    {
        fmt = IF_T2_H0;
    }
    else
    {
        // Load disp into a register
        regNumber rsvdReg = codeGen->rsGetRsvdReg();
        emitIns_genStackOffset(rsvdReg, varx, offs, /* isFloatUsage */ false, &baseRegUsed);
        fmt = IF_T2_E0;

        // Ensure the baseReg calculated is correct.
        assert(baseRegUsed == reg2);
    }
    assert((fmt == IF_T1_J2) || (fmt == IF_T2_E0) || (fmt == IF_T2_H0) || (fmt == IF_T2_VLDST) || (fmt == IF_T2_K1));
    assert(sf != INS_FLAGS_DONT_CARE);

    instrDesc* id  = emitNewInstrCns(attr, disp);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);
    id->idInsFlags(sf);
    id->idInsOpt(INS_OPTS_NONE);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();
    if (reg2 == REG_FP)
        id->idSetIsLclFPBase();
#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing stack-based local variable and an immediate
 */
void emitter::emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val)
{
    NYI("emitIns_S_I");
}

/*****************************************************************************
 *
 *  Add an instruction with a register + static member operands.
 */
void emitter::emitIns_R_C(instruction ins, emitAttr attr, regNumber reg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Load() to select the correct instruction");
    }
    assert(emitInsIsLoad(ins) || (ins == INS_lea));
    if (ins == INS_lea)
    {
        ins = INS_add;
    }

    int     doff = Compiler::eeGetJitDataOffs(fldHnd);
    ssize_t addr = NULL;

    if (doff >= 0)
    {
        NYI_ARM("JitDataOffset static fields");
    }
    else if (fldHnd == FLD_GLOBAL_FS)
    {
        NYI_ARM("Thread-Local-Storage static fields");
    }
    else if (fldHnd == FLD_GLOBAL_DS)
    {
        addr = (ssize_t)offs;
        offs = 0;
    }
    else
    {
        assert(!"Normal statics are expected to be handled in the importer");
    }

    // We can use reg to load the constant address,
    //  as long as it is not a floating point register
    regNumber regTmp = reg;

    if (isFloatReg(regTmp))
    {
        assert(!"emitIns_R_C() cannot be called with floating point target");
        return;
    }

    // Load address into a register
    codeGen->instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regTmp, addr);

    if ((ins != INS_add) || (offs != 0) || (reg != regTmp))
    {
        emitIns_R_R_I(ins, attr, reg, regTmp, offs);
    }
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + register operands.
 */

void emitter::emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs)
{
    assert(!"emitIns_C_R not supported");
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + constant.
 */

void emitter::emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, int offs, ssize_t val)
{
    NYI("emitIns_C_I");
}

/*****************************************************************************
 *
 *  The following adds instructions referencing address modes.
 */

void emitter::emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int offs)
{
    NYI("emitIns_I_AR");
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Load() to select the correct instruction");
    }

    if (ins == INS_lea)
    {
        if (emitter::emitIns_valid_imm_for_add(offs, INS_FLAGS_DONT_CARE))
        {
            emitIns_R_R_I(INS_add, attr, ireg, reg, offs);
        }
        else
        {
            assert(!"emitIns_R_AR: immediate doesn't fit in the instruction");
        }
        return;
    }
    else if (emitInsIsLoad(ins))
    {
        emitIns_R_R_I(ins, attr, ireg, reg, offs);
        return;
    }
    else if ((ins == INS_mov) || (ins == INS_ldr))
    {
        if (EA_SIZE(attr) == EA_4BYTE)
        {
            emitIns_R_R_I(INS_ldr, attr, ireg, reg, offs);
            return;
        }
    }
    else if (ins == INS_vldr)
    {
        emitIns_R_R_I(ins, attr, ireg, reg, offs);
    }
    NYI("emitIns_R_AR");
}

void emitter::emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp)
{
    if (emitInsIsLoad(ins))
    {
        // We can use ireg to load the constant address,
        //  as long as it is not a floating point register
        regNumber regTmp = ireg;

        if (isFloatReg(regTmp))
        {
            assert(!"emitIns_R_AI with floating point reg");
            return;
        }

        codeGen->instGen_Set_Reg_To_Imm(EA_IS_RELOC(attr) ? EA_HANDLE_CNS_RELOC : EA_PTRSIZE, regTmp, disp);
        emitIns_R_R_I(ins, EA_TYPE(attr), ireg, regTmp, 0);
        return;
    }
    NYI("emitIns_R_AI");
}

void emitter::emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Store() to select the correct instruction");
    }
    emitIns_R_R_I(ins, attr, ireg, reg, offs);
}

void emitter::emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Load() to select the correct instruction");
    }

    if (ins == INS_lea)
    {
        emitIns_R_R_R(INS_add, attr, ireg, reg, rg2);
        if (disp != 0)
        {
            emitIns_R_R_I(INS_add, attr, ireg, ireg, disp);
        }
        return;
    }
    else if (emitInsIsLoad(ins))
    {
        if (disp == 0)
        {
            emitIns_R_R_R_I(ins, attr, ireg, reg, rg2, 0, INS_FLAGS_DONT_CARE, INS_OPTS_NONE);
            return;
        }
    }
    assert(!"emitIns_R_ARR: Unexpected instruction");
}

void emitter::emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Store() to select the correct instruction");
    }
    if (emitInsIsStore(ins))
    {
        if (disp == 0)
        {
            emitIns_R_R_R(ins, attr, ireg, reg, rg2);
        }
        else
        {
            emitIns_R_R_R(INS_add, attr, ireg, reg, rg2);
            emitIns_R_R_I(ins, attr, ireg, ireg, disp);
        }
        return;
    }
    assert(!"emitIns_ARR_R: Unexpected instruction");
}

void emitter::emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp)
{
    if (ins == INS_mov)
    {
        assert(!"Please use ins_Load() to select the correct instruction");
    }

    unsigned shift = genLog2(mul);

    if ((ins == INS_lea) || emitInsIsLoad(ins))
    {
        if (ins == INS_lea)
        {
            ins = INS_add;
        }
        if (disp == 0)
        {
            emitIns_R_R_R_I(ins, attr, ireg, reg, rg2, (int)shift, INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
            return;
        }
        else
        {
            bool useForm2     = false;
            bool mustUseForm1 = ((disp % mul) != 0) || (reg == ireg);
            if (!mustUseForm1)
            {
                // If all of the below things are true we can generate a Thumb-1 add instruction
                //  followed by a Thumb-2 add instruction
                // We also useForm1 when reg is a low register since the second instruction
                //  can then always be generated using a Thumb-1 add
                //
                if ((reg >= REG_R8) && (ireg < REG_R8) && (rg2 < REG_R8) && ((disp >> shift) <= 7))
                {
                    useForm2 = true;
                }
            }

            if (useForm2)
            {
                // Form2:
                //     Thumb-1   instruction    add     Rd, Rx, disp>>shift
                //     Thumb-2   instructions   ldr     Rd, Rb, Rd LSL shift
                //
                emitIns_R_R_I(INS_add, EA_4BYTE, ireg, rg2, disp >> shift);
                emitIns_R_R_R_I(ins, attr, ireg, reg, ireg, shift, INS_FLAGS_NOT_SET, INS_OPTS_LSL);
            }
            else
            {
                // Form1:
                //     Thumb-2   instruction    add     Rd, Rb, Rx LSL shift
                //     Thumb-1/2 instructions   ldr     Rd, Rd, disp
                //
                emitIns_R_R_R_I(INS_add, attr, ireg, reg, rg2, shift, INS_FLAGS_NOT_SET, INS_OPTS_LSL);
                emitIns_R_R_I(ins, attr, ireg, ireg, disp);
            }
            return;
        }
    }

    assert(!"emitIns_R_ARX: Unexpected instruction");
}

/*****************************************************************************
 *
 *  Record that a jump instruction uses the short encoding
 *
 */
void emitter::emitSetShortJump(instrDescJmp* id)
{
    if (id->idjKeepLong)
        return;

    if (emitIsCondJump(id))
    {
        id->idInsFmt(IF_T1_K);
    }
    else if (emitIsCmpJump(id))
    {
        // These are always only ever short!
        assert(id->idjShort);
        return;
    }
    else if (emitIsUncondJump(id))
    {
        id->idInsFmt(IF_T1_M);
    }
    else if (emitIsLoadLabel(id))
    {
        return; // Keep long - we don't know the alignment of the target
    }
    else
    {
        assert(!"Unknown instruction in emitSetShortJump()");
    }

    id->idjShort = true;

#if DEBUG_EMIT
    if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
    {
        printf("[8] Converting jump %u to short\n", id->idDebugOnlyInfo()->idNum);
    }
#endif // DEBUG_EMIT

    insSize isz = emitInsSize(id->idInsFmt());
    id->idInsSize(isz);
}

/*****************************************************************************
 *
 *  Record that a jump instruction uses the medium encoding
 *
 */
void emitter::emitSetMediumJump(instrDescJmp* id)
{
    if (id->idjKeepLong)
        return;

#if DEBUG_EMIT
    if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
    {
        printf("[9] Converting jump %u to medium\n", id->idDebugOnlyInfo()->idNum);
    }
#endif // DEBUG_EMIT

    assert(emitIsCondJump(id));
    id->idInsFmt(IF_T2_J1);
    id->idjShort = false;

    insSize isz = emitInsSize(id->idInsFmt());
    id->idInsSize(isz);
}

/*****************************************************************************
 *
 *  Add a jmp instruction.
 *  When dst is NULL, instrCount specifies number of instructions
 *       to jump: positive is forward, negative is backward.
 *  Unconditional branches have two sizes: short and long.
 *  Conditional branches have three sizes: short, medium, and long. A long
 *     branch is a pseudo-instruction that represents two instructions:
 *     a short conditional branch to branch around a large unconditional
 *     branch. Thus, we can handle branch offsets of imm24 instead of just imm20.
 */

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount /* = 0 */)
{
    insFormat fmt = IF_NONE;

    if (dst != NULL)
    {
        assert(dst->HasFlag(BBF_HAS_LABEL));
    }
    else
    {
        assert(instrCount != 0);
    }

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_b:
        case INS_bl:
            fmt = IF_T2_J2; /* Assume the jump will be long */
            break;

        case INS_beq:
        case INS_bne:
        case INS_bhs:
        case INS_blo:
        case INS_bmi:
        case INS_bpl:
        case INS_bvs:
        case INS_bvc:
        case INS_bhi:
        case INS_bls:
        case INS_bge:
        case INS_blt:
        case INS_bgt:
        case INS_ble:
            fmt = IF_LARGEJMP; /* Assume the jump will be long */
            break;

        default:
            unreached();
    }

    instrDescJmp* id  = emitNewInstrJmp();
    insSize       isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsSize(isz);

#ifdef DEBUG
    // Mark the finally call
    if ((ins == INS_bl) && emitComp->compCurBB->KindIs(BBJ_CALLFINALLY))
    {
        id->idDebugOnlyInfo()->idFinallyCall = true;
    }
#endif // DEBUG

    /* Assume the jump will be long */

    id->idjShort = 0;
    if (dst != NULL)
    {
        id->idAddr()->iiaBBlabel = dst;
        id->idjKeepLong          = (ins == INS_bl) || emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

#ifdef DEBUG
        if (emitComp->opts.compLongAddress) // Force long branches
            id->idjKeepLong = 1;
#endif // DEBUG
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

    if (!id->idjKeepLong)
    {
        insGroup* tgt = NULL;

        /* Can we guess at the jump distance? */

        if (dst != NULL)
        {
            tgt = (insGroup*)emitCodeGetCookie(dst);
        }

        if (tgt)
        {
            UNATIVE_OFFSET srcOffs;
            int            jmpDist;

            assert(JMP_SIZE_SMALL == JCC_SIZE_SMALL);

            /* This is a backward jump - figure out the distance */

            srcOffs = emitCurCodeOffset + emitCurIGsize;

            /* Compute the distance estimate */

            jmpDist = srcOffs - tgt->igOffs;
            assert(jmpDist >= 0);
            jmpDist += 4; // Adjustment for ARM PC

            switch (fmt)
            {
                case IF_T2_J2:
                    if (JMP_DIST_SMALL_MAX_NEG <= -jmpDist)
                    {
                        /* This jump surely will be short */
                        emitSetShortJump(id);
                    }
                    break;

                case IF_LARGEJMP:
                    if (JCC_DIST_SMALL_MAX_NEG <= -jmpDist)
                    {
                        /* This jump surely will be short */
                        emitSetShortJump(id);
                    }
                    else if (JCC_DIST_MEDIUM_MAX_NEG <= -jmpDist)
                    {
                        /* This jump surely will be medium */
                        emitSetMediumJump(id);
                    }
                    break;

                default:
                    unreached();
                    break;
            }
        }
    }

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */

void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(dst->HasFlag(BBF_HAS_LABEL));

    insFormat     fmt = IF_NONE;
    instrDescJmp* id;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_adr:
            id  = emitNewInstrLbl();
            fmt = IF_T2_M1;
            break;
        case INS_movt:
        case INS_movw:
            id  = emitNewInstrJmp();
            fmt = IF_T2_N1;
            break;
        default:
            unreached();
    }
    assert((fmt == IF_T2_M1) || (fmt == IF_T2_N1));

    insSize isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idReg1(reg);
    id->idInsFmt(fmt);
    id->idInsSize(isz);

#ifdef DEBUG
    // Mark the catch return
    if (emitComp->compCurBB->KindIs(BBJ_EHCATCHRET))
    {
        id->idDebugOnlyInfo()->idCatchRet = true;
    }
#endif // DEBUG

    id->idAddr()->iiaBBlabel = dst;
    id->idjShort             = false;

    if (ins == INS_adr)
    {
        id->idReg2(REG_PC);
        id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);
    }
    else
    {
        id->idjKeepLong = true;
    }

    /* Record the jump's IG and offset within it */

    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */

    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

    if (emitComp->opts.compReloc)
    {
        // Set the relocation flags - these give hint to zap to perform
        // relocation of the specified 32bit address.
        id->idSetRelocFlags(attr);
    }

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a data label instruction.
 */

void emitter::emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg)
{
    noway_assert((ins == INS_movw) || (ins == INS_movt));

    insFormat  fmt = IF_T2_N2;
    instrDesc* id  = emitNewInstrSC(attr, offs);
    insSize    isz = emitInsSize(fmt);

    id->idIns(ins);
    id->idReg1(reg);
    id->idInsFmt(fmt);
    id->idInsSize(isz);

    if (emitComp->opts.compReloc)
    {
        // Set the relocation flags - these give hint to zap to perform
        // relocation of the specified 32bit address.
        id->idSetRelocFlags(attr);
    }

    dispIns(id);
    appendToCurIG(id);
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(dst->HasFlag(BBF_HAS_LABEL));

    insFormat fmt = IF_NONE;
    switch (ins)
    {
        case INS_cbz:
        case INS_cbnz:
            fmt = IF_T1_I;
            break;
        default:
            unreached();
    }
    assert(fmt == IF_T1_I);

    assert(isLowRegister(reg));

    instrDescJmp* id = emitNewInstrJmp();
    id->idIns(ins);
    id->idInsFmt(IF_T1_I);
    id->idInsSize(emitInsSize(IF_T1_I));
    id->idReg1(reg);

    /* This jump better be short or-else! */
    id->idjShort             = true;
    id->idAddr()->iiaBBlabel = dst;
    id->idjKeepLong          = false;

    /* Record the jump's IG and offset within it */

    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */

    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a call instruction (direct or indirect).
 *      argSize<0 means that the caller will pop the arguments
 *
 * The other arguments are interpreted depending on callType as shown:
 * Unless otherwise specified, ireg,xreg,xmul,disp should have default values.
 *
 * EC_FUNC_TOKEN       : addr is the method address
 * EC_FUNC_ADDR        : addr is the absolute address of the function
 *                       if addr is NULL, it is a recursive call
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 *
 * For ARM xreg, xmul and disp are never used and should always be 0/REG_NA.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,                   // used for pretty printing
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*            addr,
                           int              argSize,
                           emitAttr         retSize,
                           VARSET_VALARG_TP ptrVars,
                           regMaskGpr       gcrefRegs,
                           regMaskGpr       byrefRegs,
                           const DebugInfo& di /* = DebugInfo() */,
                           regNumber        ireg /* = REG_NA */,
                           regNumber        xreg /* = REG_NA */,
                           unsigned         xmul /* = 0     */,
                           ssize_t          disp /* = 0     */,
                           bool             isJump /* = false */)
{
    /* Sanity check the arguments depending on callType */
    assert(emitComp->IsGprRegMask(gcrefRegs));
    assert(emitComp->IsGprRegMask(byrefRegs));
    assert(callType < EC_COUNT);
    assert((callType != EC_FUNC_TOKEN) || (addr != nullptr && ireg == REG_NA));
    assert(callType != EC_INDIR_R || (addr == nullptr && ireg < REG_COUNT));

    // ARM never uses these
    assert(xreg == REG_NA && xmul == 0 && disp == 0);

    // Our stack level should be always greater than the bytes of arguments we push. Just
    // a sanity test.
    assert((unsigned)abs(argSize) <= codeGen->genStackLevel);

    // Trim out any callee-trashed registers from the live set.
    AllRegsMask savedSet = emitGetGCRegsSavedOrModified(methHnd);
    gcrefRegs &= savedSet.gprRegs;
    byrefRegs &= savedSet.gprRegs;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("Call: GCvars=%s ", VarSetOps::ToString(emitComp, ptrVars));
        dumpConvertedVarSet(emitComp, ptrVars);
        printf(", gcrefRegs=");
        printRegMaskInt(gcrefRegs);
        emitDispGprRegSet(gcrefRegs);
        printf(", byrefRegs=");
        printRegMaskInt(byrefRegs);
        emitDispGprRegSet(byrefRegs);
        printf("\n");
    }
#endif

    /* Managed RetVal: emit sequence point for the call */
    if (emitComp->opts.compDbgInfo && di.GetLocation().IsValid())
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
    int argCnt = argSize / REGSIZE_BYTES;

    if (callType == EC_INDIR_R)
    {
        /* Indirect call, virtual calls */

        id = emitNewInstrCallInd(argCnt, 0 /* disp */, ptrVars, gcrefRegs, byrefRegs, retSize);
    }
    else
    {
        /* Helper/static/nonvirtual/function calls (direct or through handle),
           and calls to an absolute addr. */

        assert(callType == EC_FUNC_TOKEN);

        id = emitNewInstrCallDir(argCnt, ptrVars, gcrefRegs, byrefRegs, retSize);
    }

    /* Update the emitter's live GC ref sets */

    VarSetOps::Assign(emitComp, emitThisGCrefVars, ptrVars);
    emitThisGCrefRegs = gcrefRegs;
    emitThisByrefRegs = byrefRegs;

    id->idSetIsNoGC(emitNoGChelper(methHnd));

    /* Set the instruction - special case jumping a function */
    instruction ins;
    insFormat   fmt = IF_NONE;

    /* Record the address: method, indirection, or funcptr */

    if (callType == EC_INDIR_R)
    {
        /* This is an indirect call (either a virtual call or func ptr call) */

        if (isJump)
        {
            ins = INS_bx; // INS_bx  Reg
        }
        else
        {
            ins = INS_blx; // INS_blx Reg
        }
        fmt = IF_T1_D2;

        id->idIns(ins);
        id->idInsFmt(fmt);
        id->idInsSize(emitInsSize(fmt));
        id->idReg3(ireg);
        assert(xreg == REG_NA);
    }
    else
    {
        /* This is a simple direct call: "call helper/method/addr" */

        assert(callType == EC_FUNC_TOKEN);

        // if addr is nullptr then this call is treated as a recursive call.
        assert(addr == nullptr || codeGen->validImmForBL((ssize_t)addr));

        if (isJump)
        {
            ins = INS_b; // INS_b imm24
        }
        else
        {
            ins = INS_bl; // INS_bl imm24
        }

        fmt = IF_T2_J3;

        id->idIns(ins);
        id->idInsFmt(fmt);
        id->idInsSize(emitInsSize(fmt));

        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (emitComp->opts.compReloc)
        {
            // Since this is an indirect call through a pointer and we don't
            // currently pass in emitAttr into this function we have decided
            // to always mark the displacement as being relocatable.

            id->idSetIsDspReloc();
        }
    }

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        if (id->idIsLargeCall())
        {
            printf("[%02u] Rec call GC vars = %s\n", id->idDebugOnlyInfo()->idNum,
                   VarSetOps::ToString(emitComp, ((instrDescCGCA*)id)->idcGCvars));
        }
    }
#endif

    if (m_debugInfoSize > 0)
    {
        INDEBUG(id->idDebugOnlyInfo()->idCallSig = sigInfo);
        id->idDebugOnlyInfo()->idMemCookie       = (size_t)methHnd; // method token
    }

#ifdef LATE_DISASM
    if (addr != nullptr)
    {
        codeGen->getDisAssembler().disSetMethod((size_t)addr, methHnd);
    }
#endif // LATE_DISASM

    dispIns(id);
    appendToCurIG(id);
    emitLastMemBarrier = nullptr; // Cannot optimize away future memory barriers
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register (any-reg) to be used in
 *   a Thumb-1 encoding in the M4 position
 */

inline unsigned insEncodeRegT1_M4(regNumber reg)
{
    assert(reg < REG_STK);

    return reg << 3;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register (any-reg) to be used in
 *   a Thumb-1 encoding in the D4 position
 */

inline unsigned insEncodeRegT1_D4(regNumber reg)
{
    assert(reg < REG_STK);

    return (reg & 0x7) | ((reg & 0x8) << 4);
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register (low-only) to be used in
 *   a Thumb-1 encoding in the M3 position
 */

inline unsigned insEncodeRegT1_M3(regNumber reg)
{
    assert(reg < REG_R8);

    return reg << 6;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register (low-only) to be used in
 *   a Thumb-1 encoding in the N3 position
 */

inline unsigned insEncodeRegT1_N3(regNumber reg)
{
    assert(reg < REG_R8);

    return reg << 3;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register (low-only) to be used in
 *   a Thumb-1 encoding in the D3 position
 */

inline unsigned insEncodeRegT1_D3(regNumber reg)
{
    assert(reg < REG_R8);

    return reg;
}
/*****************************************************************************
 *
 *  Returns an encoding for the specified register (low-only) to be used in
 *   a Thumb-1 encoding in the DI position
 */

inline unsigned insEncodeRegT1_DI(regNumber reg)
{
    assert(reg < REG_R8);

    return reg << 8;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in
 *   a Thumb-2 encoding in the N position
 */

inline unsigned insEncodeRegT2_N(regNumber reg)
{
    assert(reg < REG_STK);

    return reg << 16;
}

inline unsigned floatRegIndex(regNumber reg, int size)
{
    // theoretically this could support quad floats as well but for now...
    assert(size == EA_8BYTE || size == EA_4BYTE);

    if (size == EA_8BYTE)
        assert(emitter::isDoubleReg(reg));
    else
        assert(emitter::isFloatReg(reg));

    unsigned result = reg - REG_F0;

    // the assumption here is that the register F8 also refers to D4
    if (size == EA_8BYTE)
    {
        result >>= 1;
    }

    return result;
}

// variant: SOME arm VFP instructions use the convention that
// for doubles, the split bit holds the msb of the register index
// for singles it holds the lsb
// excerpt : d = if dp_operation then UInt(D:Vd)
// if single  UInt(Vd:D);

inline unsigned floatRegEncoding(unsigned index, int size, bool variant = false)
{
    if (!variant || size == EA_8BYTE)
        return index;
    else
    {
        return ((index & 1) << 4) | (index >> 1);
    }
}

// thumb2 VFP M register encoding
inline unsigned insEncodeRegT2_VectorM(regNumber reg, int size, bool variant)
{
    unsigned enc = floatRegIndex(reg, size);
    enc          = floatRegEncoding(enc, size, variant);
    return ((enc & 0xf) << 0) | ((enc & 0x10) << 1);
}

// thumb2 VFP N register encoding
inline unsigned insEncodeRegT2_VectorN(regNumber reg, int size, bool variant)
{
    unsigned enc = floatRegIndex(reg, size);
    enc          = floatRegEncoding(enc, size, variant);
    return ((enc & 0xf) << 16) | ((enc & 0x10) << 3);
}

// thumb2 VFP D register encoding
inline unsigned insEncodeRegT2_VectorD(regNumber reg, int size, bool variant)
{
    unsigned enc = floatRegIndex(reg, size);
    enc          = floatRegEncoding(enc, size, variant);
    return ((enc & 0xf) << 12) | ((enc & 0x10) << 18);
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in
 *   a Thumb-2 encoding in the T position
 */

inline unsigned insEncodeRegT2_T(regNumber reg)
{
    assert(reg < REG_STK);

    return reg << 12;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in
 *   a Thumb-2 encoding in the D position
 */

inline unsigned insEncodeRegT2_D(regNumber reg)
{
    assert(reg < REG_STK);

    return reg << 8;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register to be used in
 *   a Thumb-2 encoding in the M position
 */

inline unsigned insEncodeRegT2_M(regNumber reg)
{
    assert(reg < REG_STK);

    return reg;
}

/*****************************************************************************
 *
 *  Returns the encoding for the Set Flags bit to be used in a Thumb-2 encoding
 */

unsigned emitter::insEncodeSetFlags(insFlags sf)
{
    if (sf == INS_FLAGS_SET)
        return (1 << 20);
    else
        return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding for the Shift Type bits to be used in a Thumb-2 encoding
 */

unsigned emitter::insEncodeShiftOpts(insOpts opt)
{
    if (opt == INS_OPTS_NONE)
        return 0;
    else if (opt == INS_OPTS_LSL)
        return 0x00;
    else if (opt == INS_OPTS_LSR)
        return 0x10;
    else if (opt == INS_OPTS_ASR)
        return 0x20;
    else if (opt == INS_OPTS_ROR)
        return 0x30;
    else if (opt == INS_OPTS_RRX)
        return 0x30;

    assert(!"Invalid insOpts");
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding for the PUW bits to be used in a T2_G0 Thumb-2 encoding
 */

unsigned emitter::insEncodePUW_G0(insOpts opt, int imm)
{
    unsigned result = 0;

    if (opt != INS_OPTS_LDST_POST_INC)
        result |= (1 << 24); // The P bit

    if (imm >= 0)
        result |= (1 << 23); // The U bit

    if (opt != INS_OPTS_NONE)
        result |= (1 << 21); // The W bits
    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding for the PUW bits to be used in a T2_H0 Thumb-2 encoding
 */

unsigned emitter::insEncodePUW_H0(insOpts opt, int imm)
{
    unsigned result = 0;

    if (opt != INS_OPTS_LDST_POST_INC)
        result |= (1 << 10); // The P bit

    if (imm >= 0)
        result |= (1 << 9); // The U bit

    if (opt != INS_OPTS_NONE)
        result |= (1 << 8); // The W bits

    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding for the Shift Count bits to be used in a Thumb-2 encoding
 */

inline unsigned insEncodeShiftCount(int imm)
{
    unsigned result;

    assert((imm & 0x001F) == imm);
    result = (imm & 0x03) << 6;
    result |= (imm & 0x1C) << 10;

    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding for the immediate use by BFI/BFC Thumb-2 encodings
 */

inline unsigned insEncodeBitFieldImm(int imm)
{
    unsigned result;

    assert((imm & 0x03FF) == imm);
    result = (imm & 0x001f);
    result |= (imm & 0x0060) << 1;
    result |= (imm & 0x0380) << 5;

    return result;
}

/*****************************************************************************
 *
 *  Returns an encoding for the immediate use by MOV/MOVW Thumb-2 encodings
 */

inline unsigned insEncodeImmT2_Mov(int imm)
{
    unsigned result;

    assert((imm & 0x0000ffff) == imm);
    result = (imm & 0x00ff);
    result |= ((imm & 0x0700) << 4);
    result |= ((imm & 0x0800) << 15);
    result |= ((imm & 0xf000) << 4);

    return result;
}

//------------------------------------------------------------------------
// insUnscaleImm: Unscales the immediate operand of a given IF_T1_C instruction.
//
// Arguments:
//    ins - the instruction
//    imm - the immediate value to unscale
//
// Return Value:
//    The unscaled immediate value
//
/*static*/ int emitter::insUnscaleImm(instruction ins, int imm)
{
    switch (ins)
    {
        case INS_ldr:
        case INS_str:
            assert((imm & 0x0003) == 0);
            imm >>= 2;
            break;
        case INS_ldrh:
        case INS_strh:
            assert((imm & 0x0001) == 0);
            imm >>= 1;
            break;
        case INS_ldrb:
        case INS_strb:
        case INS_lsl:
        case INS_lsr:
        case INS_asr:
            // Do nothing
            break;
        default:
            assert(!"Invalid IF_T1_C instruction");
            break;
    }
    return imm;
}

/*****************************************************************************
 *
 *  Emit a Thumb-1 instruction (a 16-bit integer as code)
 */

unsigned emitter::emitOutput_Thumb1Instr(BYTE* dst, code_t code)
{
    unsigned short word1 = code & 0xffff;
    assert(word1 == code);

#ifdef DEBUG
    unsigned short top5bits = (word1 & 0xf800) >> 11;
    assert(top5bits < 29);
#endif

    BYTE* dstRW = dst + writeableOffset;
    MISALIGNED_WR_I2(dstRW, word1);

    return sizeof(short);
}
/*****************************************************************************
 *
 *  Emit a Thumb-2 instruction (two 16-bit integers as code)
 */

unsigned emitter::emitOutput_Thumb2Instr(BYTE* dst, code_t code)
{
    unsigned short word1 = (code >> 16) & 0xffff;
    unsigned short word2 = (code)&0xffff;
    assert((code_t)((word1 << 16) | word2) == code);

#ifdef DEBUG
    unsigned short top5bits = (word1 & 0xf800) >> 11;
    assert(top5bits >= 29);
#endif

    BYTE* dstRW = dst + writeableOffset;
    MISALIGNED_WR_I2(dstRW, word1);
    dstRW += 2;
    MISALIGNED_WR_I2(dstRW, word2);

    return sizeof(short) * 2;
}

/*****************************************************************************
 *
 *  Output a local jump instruction.
 *  Note that this may be invoked to overwrite an existing jump instruction at 'dst'
 *  to handle forward branch patching.
 */

BYTE* emitter::emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* i)
{
    unsigned srcOffs;
    unsigned dstOffs;
    ssize_t  distVal;

    instrDescJmp* id  = (instrDescJmp*)i;
    instruction   ins = id->idIns();
    code_t        code;

    bool loadLabel = false;
    bool isJump    = false;
    bool relAddr   = true; // does the instruction use relative-addressing?

    size_t sdistneg;

    switch (ins)
    {
        default:
            sdistneg = JCC_DIST_SMALL_MAX_NEG;
            isJump   = true;
            break;

        case INS_cbz:
        case INS_cbnz:
            // One size fits all!
            sdistneg = 0;
            isJump   = true;
            break;

        case INS_adr:
            sdistneg  = LBL_DIST_SMALL_MAX_NEG;
            loadLabel = true;
            break;

        case INS_movw:
        case INS_movt:
            sdistneg  = LBL_DIST_SMALL_MAX_NEG;
            relAddr   = false;
            loadLabel = true;
            break;
    }

    /* Figure out the distance to the target */

    srcOffs = emitCurCodeOffs(dst);
    if (id->idAddr()->iiaHasInstrCount())
    {
        assert(ig != NULL);
        int      instrCount = id->idAddr()->iiaGetInstrCount();
        unsigned insNum     = emitFindInsNum(ig, id);
        if (instrCount < 0)
        {
            // Backward branches using instruction count must be within the same instruction group.
            assert(insNum + 1 >= (unsigned)(-instrCount));
        }
        dstOffs = ig->igOffs + emitFindOffset(ig, (insNum + 1 + instrCount));
    }
    else
    {
        dstOffs = id->idAddr()->iiaIGlabel->igOffs;
    }

    if (relAddr)
    {
        if (ins == INS_adr)
        {
            // for adr, the distance is calculated from 4-byte aligned srcOffs.
            distVal = (ssize_t)((emitOffsetToPtr(dstOffs) - (BYTE*)(((size_t)emitOffsetToPtr(srcOffs)) & ~3)) + 1);
        }
        else
        {
            distVal = (ssize_t)(emitOffsetToPtr(dstOffs) - emitOffsetToPtr(srcOffs));
        }
    }
    else
    {
        assert(ins == INS_movw || ins == INS_movt);
        distVal = (ssize_t)emitOffsetToPtr(dstOffs) + 1; // Or in thumb bit
    }

    if (dstOffs <= srcOffs)
    {
/* This is a backward jump - distance is known at this point */

#if DEBUG_EMIT
        if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
        {
            size_t blkOffs = id->idjIG->igOffs;

            if (INTERESTING_JUMP_NUM == 0)
                printf("[3] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
            printf("[3] Jump  block is at %08X - %02X = %08X\n", blkOffs, emitOffsAdj, blkOffs - emitOffsAdj);
            printf("[3] Jump        is at %08X - %02X = %08X\n", srcOffs, emitOffsAdj, srcOffs - emitOffsAdj);
            printf("[3] Label block is at %08X - %02X = %08X\n", dstOffs, emitOffsAdj, dstOffs - emitOffsAdj);
        }
#endif

        // This format only supports forward branches
        noway_assert(id->idInsFmt() != IF_T1_I);

        /* Can we use a short jump? */

        if (isJump && ((unsigned)(distVal - 4) >= (unsigned)sdistneg))
        {
            emitSetShortJump(id);
        }
    }
    else
    {
        /* This is a  forward jump - distance will be an upper limit */

        emitFwdJumps = true;

        /* The target offset will be closer by at least 'emitOffsAdj', but only if this
           jump doesn't cross the hot-cold boundary. */

        if (!emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
        {
            dstOffs -= emitOffsAdj;
            distVal -= emitOffsAdj;
        }

        /* Record the location of the jump for later patching */

        id->idjOffs = dstOffs;

        /* Are we overflowing the id->idjOffs bitfield? */
        if (id->idjOffs != dstOffs)
            IMPL_LIMITATION("Method is too large");

#if DEBUG_EMIT
        if (id->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
        {
            size_t blkOffs = id->idjIG->igOffs;

            if (INTERESTING_JUMP_NUM == 0)
                printf("[4] Jump %u:\n", id->idDebugOnlyInfo()->idNum);
            printf("[4] Jump  block is at %08X\n", blkOffs);
            printf("[4] Jump        is at %08X\n", srcOffs);
            printf("[4] Label block is at %08X - %02X = %08X\n", dstOffs + emitOffsAdj, emitOffsAdj, dstOffs);
        }
#endif
    }

    /* Adjust the offset to emit relative to the end of the instruction */

    if (relAddr)
        distVal -= 4;

#ifdef DEBUG
    if (0 && emitComp->verbose)
    {
        size_t sz          = 4; // Thumb-2 pretends all instructions are 4-bytes long for computing jump offsets?
        int    distValSize = id->idjShort ? 4 : 8;
        printf("; %s jump [%08X/%03u] from %0*X to %0*X: dist = 0x%08X\n", (dstOffs <= srcOffs) ? "Fwd" : "Bwd",
               dspPtr(id), id->idDebugOnlyInfo()->idNum, distValSize, srcOffs + sz, distValSize, dstOffs, distVal);
    }
#endif

    insFormat fmt = id->idInsFmt();

    if (isJump)
    {
        /* What size jump should we use? */

        if (id->idjShort)
        {
            /* Short jump */

            assert(!id->idjKeepLong);
            assert(emitJumpCrossHotColdBoundary(srcOffs, dstOffs) == false);

            assert(JMP_SIZE_SMALL == JCC_SIZE_SMALL);
            assert(JMP_SIZE_SMALL == 2);

            /* For forward jumps, record the address of the distance value */
            id->idjTemp.idjAddr = (distVal > 0) ? dst : NULL;

            dst = emitOutputShortBranch(dst, ins, fmt, distVal, id);
        }
        else
        {
            /* Long  jump */

            /* For forward jumps, record the address of the distance value */
            id->idjTemp.idjAddr = (dstOffs > srcOffs) ? dst : NULL;

            if (fmt == IF_LARGEJMP)
            {
                // This is a pseudo-instruction format representing a large conditional branch, to allow
                // us to get a greater branch target range than we can get by using a straightforward conditional
                // branch. It is encoded as a short conditional branch that branches around a long unconditional
                // branch.
                //
                // Conceptually, we have:
                //
                //      b<cond> L_target
                //
                // The code we emit is:
                //
                //      b<!cond> L_not  // 2 bytes. Note that we reverse the condition.
                //      b L_target      // 4 bytes
                //   L_not:
                //
                // Note that we don't actually insert any blocks: we simply encode "b <!cond> L_not" as a branch with
                // the correct offset. Note also that this works for both integer and floating-point conditions, because
                // the condition inversion takes ordered/unordered into account, preserving NaN behavior. For example,
                // "GT" (greater than) is inverted to "LE" (less than, equal, or unordered).
                //
                // History: previously, we generated:
                //      it<cond>
                //      b L_target
                // but the "it" instruction was deprecated, so we can't use it.

                dst = emitOutputShortBranch(dst,
                                            emitJumpKindToIns(emitReverseJumpKind(
                                                emitInsToJumpKind(ins))), // reverse the conditional instruction
                                            IF_T1_K,
                                            6 - 4, /* 6 bytes from start of this large conditional pseudo-instruction to
                                                      L_not. Jumps are encoded as offset from instr address + 4. */
                                            NULL /* only used for cbz/cbnz */);

                // Now, pretend we've got a normal unconditional branch, and fall through to the code to emit that.
                ins = INS_b;
                fmt = IF_T2_J2;

                // The distVal was computed based on the beginning of the pseudo-instruction, which is
                // the IT. So subtract the size of the IT from the offset, so it is relative to the
                // unconditional branch.
                distVal -= 2;
            }

            code = emitInsCode(ins, fmt);

            if (fmt == IF_T2_J1)
            {
                // Can't use this form for jumps between the hot and cold regions
                assert(!id->idjKeepLong);
                assert(emitJumpCrossHotColdBoundary(srcOffs, dstOffs) == false);

                assert((distVal & 1) == 0);
                assert(distVal >= -1048576);
                assert(distVal <= 1048574);

                if (distVal < 0)
                    code |= 1 << 26;
                code |= ((distVal >> 1) & 0x0007ff);
                code |= (((distVal >> 1) & 0x01f800) << 5);
                code |= (((distVal >> 1) & 0x020000) >> 4);
                code |= (((distVal >> 1) & 0x040000) >> 7);
            }
            else if (fmt == IF_T2_J2)
            {
                assert((distVal & 1) == 0);
                if (emitComp->opts.compReloc && emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
                {
                    // dst isn't an actual final target location, just some intermediate
                    // location.  Thus we cannot make any guarantees about distVal (not
                    // even the direction/sign).  Instead we don't encode any offset and
                    // rely on the relocation to do all the work
                }
                else
                {
                    if ((distVal < CALL_DIST_MAX_NEG) || (distVal > CALL_DIST_MAX_POS))
                        IMPL_LIMITATION("Method is too large");

                    if (distVal < 0)
                        code |= 1 << 26;
                    code |= ((distVal >> 1) & 0x0007ff);
                    code |= (((distVal >> 1) & 0x1ff800) << 5);

                    bool S  = (distVal < 0);
                    bool I1 = ((distVal & 0x00800000) == 0);
                    bool I2 = ((distVal & 0x00400000) == 0);

                    if (S ^ I1)
                        code |= (1 << 13); // J1 bit
                    if (S ^ I2)
                        code |= (1 << 11); // J2 bit
                }
            }
            else
            {
                assert(!"Unknown fmt");
            }

            unsigned instrSize = emitOutput_Thumb2Instr(dst, code);

            if (emitComp->opts.compReloc)
            {
                if (emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
                {
                    assert(id->idjKeepLong);
                    if (emitComp->info.compMatchedVM)
                    {
                        void* target = emitOffsetToPtr(dstOffs);
                        emitRecordRelocation((void*)dst, target, IMAGE_REL_BASED_THUMB_BRANCH24);
                    }
                }
            }

            dst += instrSize;
        }
    }
    else if (loadLabel)
    {
        /* For forward jumps, record the address of the distance value */
        id->idjTemp.idjAddr = (distVal > 0) ? dst : NULL;

        code = emitInsCode(ins, fmt);

        if (fmt == IF_T1_J3)
        {
            assert((dstOffs & 3) == 0); // The target label must be 4-byte aligned
            assert(distVal >= 0);
            assert(distVal <= 1022);
            code |= ((distVal >> 2) & 0xff);

            dst += emitOutput_Thumb1Instr(dst, code);
        }
        else if (fmt == IF_T2_M1)
        {
            assert(distVal >= -4095);
            assert(distVal <= +4095);
            if (distVal < 0)
            {
                code |= 0x00A0 << 16;
                distVal = -distVal;
            }
            assert((distVal & 0x0fff) == distVal);
            code |= (distVal & 0x00ff);
            code |= ((distVal & 0x0700) << 4);

            code |= ((distVal & 0x0800) << 15);
            code |= id->idReg1() << 8;

            dst += emitOutput_Thumb2Instr(dst, code);
        }
        else if (fmt == IF_T2_N1)
        {
            assert(ins == INS_movt || ins == INS_movw);
            code |= insEncodeRegT2_D(id->idReg1());
            ((instrDescJmp*)id)->idjTemp.idjAddr = (dstOffs > srcOffs) ? dst : NULL;

            if (id->idIsReloc())
            {
                dst += emitOutput_Thumb2Instr(dst, code);
                if ((ins == INS_movt) && emitComp->info.compMatchedVM)
                    emitHandlePCRelativeMov32((void*)(dst - 8), (void*)distVal);
            }
            else
            {
                assert(sizeof(size_t) == sizeof(target_size_t));
                target_size_t imm = (target_size_t)distVal;
                if (ins == INS_movw)
                {
                    imm &= 0xffff;
                }
                else
                {
                    imm = (imm >> 16) & 0xffff;
                }
                code |= insEncodeImmT2_Mov(imm);
                dst += emitOutput_Thumb2Instr(dst, code);
            }
        }
        else
        {
            assert(!"Unknown fmt");
        }
    }

    return dst;
}

/*****************************************************************************
 *
 *  Output a short branch instruction.
 */

BYTE* emitter::emitOutputShortBranch(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, instrDescJmp* id)
{
    code_t code;

    code = emitInsCode(ins, fmt);

    if (fmt == IF_T1_K)
    {
        assert((distVal & 1) == 0);
        assert(distVal >= -256);
        assert(distVal <= 254);

        if (distVal < 0)
            code |= 1 << 7;
        code |= ((distVal >> 1) & 0x7f);
    }
    else if (fmt == IF_T1_M)
    {
        assert((distVal & 1) == 0);
        assert(distVal >= -2048);
        assert(distVal <= 2046);

        if (distVal < 0)
            code |= 1 << 10;
        code |= ((distVal >> 1) & 0x3ff);
    }
    else if (fmt == IF_T1_I)
    {
        assert(id != NULL);
        assert(ins == INS_cbz || ins == INS_cbnz);
        assert((distVal & 1) == 0);
        assert(distVal >= 0);
        assert(distVal <= 126);

        code |= ((distVal << 3) & 0x0200);
        code |= ((distVal << 2) & 0x00F8);
        code |= (id->idReg1() & 0x0007);
    }
    else
    {
        assert(!"Unknown fmt");
    }

    dst += emitOutput_Thumb1Instr(dst, code);

    return dst;
}

#ifdef FEATURE_ITINSTRUCTION

/*****************************************************************************
 * The "IT" instruction is deprecated (with a very few exceptions). Don't generate it!
 * Don't delete this code, though, in case we ever want to bring it back.
 *****************************************************************************/

/*****************************************************************************
 *
 *  Output an IT instruction.
 */

BYTE* emitter::emitOutputIT(BYTE* dst, instruction ins, insFormat fmt, code_t condcode)
{
    code_t imm0;
    code_t code, mask, bit;

    code = emitInsCode(ins, fmt);
    code |= (condcode << 4);        // encode firstcond
    imm0 = condcode & 1;            // this is firstcond[0]
    mask = code & 0x0f;             // initialize mask encoded in opcode
    bit  = 0x08;                    // where in mask we are encoding
    while ((mask & (bit - 1)) != 0) // are the remaining bits all zeros?
    {                               //  then we are done
        // otherwise determine the setting of bit
        if ((imm0 == 1) ^ ((bit & mask) != 0))
        {
            code |= bit; // set the current bit
        }
        else
        {
            code &= ~bit; // clear the current bit
        }
        bit >>= 1;
    }
    dst += emitOutput_Thumb1Instr(dst, code);

    return dst;
}

#endif // FEATURE_ITINSTRUCTION

/*****************************************************************************
*
 *  Append the machine code corresponding to the given instruction descriptor
 *  to the code block at '*dp'; the base of the code block is 'bp', and 'ig'
 *  is the instruction group that contains the instruction. Updates '*dp' to
 *  point past the generated code, and returns the size of the instruction
 *  descriptor in bytes.
 */

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    BYTE*         dst           = *dp;
    BYTE*         odst          = dst;
    code_t        code          = 0;
    size_t        sz            = 0;
    instruction   ins           = id->idIns();
    insFormat     fmt           = id->idInsFmt();
    emitAttr      size          = id->idOpSize();
    unsigned char callInstrSize = 0;

#ifdef DEBUG
    bool dspOffs = emitComp->opts.dspGCtbls || !emitComp->opts.disDiffable;
#endif // DEBUG

    assert(REG_NA == (int)REG_NA);

    VARSET_TP GCvars(VarSetOps::UninitVal());

    /* What instruction format have we got? */

    switch (fmt)
    {
        int        imm;
        BYTE*      addr;
        regMaskGpr gcrefRegs;
        regMaskGpr byrefRegs;

        case IF_T1_A: // T1_A    ................
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

#ifdef FEATURE_ITINSTRUCTION
        case IF_T1_B: // T1_B    ........cccc....                                           cond
        {
            assert(id->idGCref() == GCT_NONE);
            target_ssize_t condcode = emitGetInsSC(id);
            dst                     = emitOutputIT(dst, ins, fmt, condcode);
            sz                      = SMALL_IDSC_SIZE;
        }
        break;
#endif // FEATURE_ITINSTRUCTION

        case IF_T1_C: // T1_C    .....iiiiinnnddd                       R1  R2              imm5
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_D3(id->idReg1());
            code |= insEncodeRegT1_N3(id->idReg2());
            imm = insUnscaleImm(ins, imm);
            assert((imm & 0x001f) == imm);
            code |= (imm << 6);
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_D0: // T1_D0   ........Dmmmmddd                       R1* R2*
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_D4(id->idReg1());
            code |= insEncodeRegT1_M4(id->idReg2());
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_E: // T1_E    ..........nnnddd                       R1  R2
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_D3(id->idReg1());
            code |= insEncodeRegT1_N3(id->idReg2());
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_F: // T1_F    .........iiiiiii                       SP                  imm7
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            assert((ins == INS_add) || (ins == INS_sub));
            assert((imm & 0x0003) == 0);
            imm >>= 2;
            assert((imm & 0x007F) == imm);
            code |= imm;
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_G: // T1_G    .......iiinnnddd                       R1  R2              imm3
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_D3(id->idReg1());
            code |= insEncodeRegT1_N3(id->idReg2());
            assert((imm & 0x0007) == imm);
            code |= (imm << 6);
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_H: // T1_H    .......mmmnnnddd                       R1  R2  R3
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_D3(id->idReg1());
            code |= insEncodeRegT1_N3(id->idReg2());
            code |= insEncodeRegT1_M3(id->idReg3());
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_I: // T1_I    ......i.iiiiiddd                       R1                  imm6
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_T1_J0: // T1_J0   .....dddiiiiiiii                       R1                  imm8
        case IF_T1_J1: // T1_J1   .....dddiiiiiiii                       R1                  <regmask8>
        case IF_T1_J2: // T1_J2   .....dddiiiiiiii                       R1  SP              imm8
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_DI(id->idReg1());
            if (fmt == IF_T1_J2)
            {
                assert((ins == INS_add) || (ins == INS_ldr) || (ins == INS_str));
                assert((imm & 0x0003) == 0);
                imm >>= 2;
            }
            assert((imm & 0x00ff) == imm);
            code |= imm;
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T1_L0: // T1_L0   ........iiiiiiii                                           imm8
        case IF_T1_L1: // T1_L1   .......Rrrrrrrrr                                           <regmask8>
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            if (fmt == IF_T1_L1)
            {
                assert((imm & 0x3) != 0x3);
                if (imm & 0x3)
                    code |= 0x0100; //  R bit
                imm >>= 2;
            }
            assert((imm & 0x00ff) == imm);
            code |= imm;
            dst += emitOutput_Thumb1Instr(dst, code);
            break;

        case IF_T2_A: // T2_A    ................ ................
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_B: // T2_B    ................ ............iiii                          imm4
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            assert((imm & 0x000F) == imm);
            code |= imm;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C0: // T2_C0   ...........Snnnn .iiiddddiishmmmm       R1  R2  R3      S, imm5, sh
        case IF_T2_C4: // T2_C4   ...........Snnnn ....dddd....mmmm       R1  R2  R3      S
        case IF_T2_C5: // T2_C5   ............nnnn ....dddd....mmmm       R1  R2  R3
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            code |= insEncodeRegT2_N(id->idReg2());
            code |= insEncodeRegT2_M(id->idReg3());
            if (fmt != IF_T2_C5)
                code |= insEncodeSetFlags(id->idInsFlags());
            if (fmt == IF_T2_C0)
            {
                imm = emitGetInsSC(id);
                code |= insEncodeShiftCount(imm);
                code |= insEncodeShiftOpts(id->idInsOpt());
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C1: // T2_C1   ...........S.... .iiiddddiishmmmm       R1  R2          S, imm5, sh
        case IF_T2_C2: // T2_C2   ...........S.... .iiiddddii..mmmm       R1  R2          S, imm5
        case IF_T2_C6: // T2_C6   ................ ....dddd..iimmmm       R1  R2                   imm2
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            code |= insEncodeRegT2_M(id->idReg2());
            if (fmt == IF_T2_C6)
            {
                assert((imm & 0x0018) == imm);
                code |= (imm << 1);
            }
            else
            {
                code |= insEncodeSetFlags(id->idInsFlags());
                code |= insEncodeShiftCount(imm);
                if (fmt == IF_T2_C1)
                    code |= insEncodeShiftOpts(id->idInsOpt());
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C3: // T2_C3   ...........S.... ....dddd....mmmm       R1  R2          S
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            code |= insEncodeRegT2_M(id->idReg2());
            code |= insEncodeSetFlags(id->idInsFlags());
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C7: // T2_C7   ............nnnn ..........shmmmm       R1  R2                   imm2
        case IF_T2_C8: // T2_C8   ............nnnn .iii....iishmmmm       R1  R2             imm5, sh
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_N(id->idReg1());
            code |= insEncodeRegT2_M(id->idReg2());
            if (fmt == IF_T2_C7)
            {
                assert((imm & 0x0003) == imm);
                code |= (imm << 4);
            }
            else if (fmt == IF_T2_C8)
            {
                code |= insEncodeShiftCount(imm);
                code |= insEncodeShiftOpts(id->idInsOpt());
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C9: // T2_C9   ............nnnn ............mmmm       R1  R2
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_N(id->idReg1());
            code |= insEncodeRegT2_M(id->idReg2());
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_C10: // T2_C10  ............mmmm ....dddd....mmmm       R1  R2
            sz   = SMALL_IDSC_SIZE;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            code |= insEncodeRegT2_M(id->idReg2());
            code |= insEncodeRegT2_N(id->idReg2());
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_D0: // T2_D0   ............nnnn .iiiddddii.wwwww       R1  R2             imm5, imm5
        case IF_T2_D1: // T2_D1   ................ .iiiddddii.wwwww       R1                 imm5, imm5
            sz   = SMALL_IDSC_SIZE;
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            if (fmt == IF_T2_D0)
                code |= insEncodeRegT2_N(id->idReg2());
            code |= insEncodeBitFieldImm(imm);
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_E0: // T2_E0   ............nnnn tttt......shmmmm       R1  R2  R3               imm2
        case IF_T2_E1: // T2_E1   ............nnnn tttt............       R1  R2
        case IF_T2_E2: // T2_E2   ................ tttt............       R1
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_T(id->idReg1());
            if (fmt == IF_T2_E0)
            {
                sz = emitGetInstrDescSize(id);
                code |= insEncodeRegT2_N(id->idReg2());
                if (id->idIsLclVar())
                {
                    code |= insEncodeRegT2_M(codeGen->rsGetRsvdReg());
                    imm = 0;
                }
                else
                {
                    code |= insEncodeRegT2_M(id->idReg3());
                    imm = emitGetInsSC(id);
                    assert((imm & 0x0003) == imm);
                    code |= (imm << 4);
                }
            }
            else
            {
                sz = SMALL_IDSC_SIZE;
                if (fmt != IF_T2_E2)
                {
                    code |= insEncodeRegT2_N(id->idReg2());
                }
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_F1: // T2_F1    ............nnnn ttttdddd....mmmm       R1  R2  R3  R4
            sz = emitGetInstrDescSize(id);
            ;
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_T(id->idReg1());
            code |= insEncodeRegT2_D(id->idReg2());
            code |= insEncodeRegT2_N(id->idReg3());
            code |= insEncodeRegT2_M(id->idReg4());
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_F2: // T2_F2    ............nnnn aaaadddd....mmmm       R1  R2  R3  R4
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            code |= insEncodeRegT2_N(id->idReg2());
            code |= insEncodeRegT2_M(id->idReg3());
            code |= insEncodeRegT2_T(id->idReg4());
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_G0: // T2_G0   .......PU.W.nnnn ttttTTTTiiiiiiii       R1  R2  R3         imm8, PUW
        case IF_T2_G1: // T2_G1   ............nnnn ttttTTTT........       R1  R2  R3
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_T(id->idReg1());
            code |= insEncodeRegT2_D(id->idReg2());
            code |= insEncodeRegT2_N(id->idReg3());
            if (fmt == IF_T2_G0)
            {
                imm = emitGetInsSC(id);
                assert(unsigned_abs(imm) <= 0x00ff);
                code |= abs(imm);
                code |= insEncodePUW_G0(id->idInsOpt(), imm);
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_H0: // T2_H0   ............nnnn tttt.PUWiiiiiiii       R1  R2             imm8, PUW
        case IF_T2_H1: // T2_H1   ............nnnn tttt....iiiiiiii       R1  R2             imm8
        case IF_T2_H2: // T2_H2   ............nnnn ........iiiiiiii       R1                 imm8
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_T(id->idReg1());

            if (fmt != IF_T2_H2)
                code |= insEncodeRegT2_N(id->idReg2());

            if (fmt == IF_T2_H0)
            {
                assert(unsigned_abs(imm) <= 0x00ff);
                code |= insEncodePUW_H0(id->idInsOpt(), imm);
                code |= unsigned_abs(imm);
            }
            else
            {
                assert((imm & 0x00ff) == imm);
                code |= imm;
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_I0: // T2_I0   ..........W.nnnn rrrrrrrrrrrrrrrr       R1              W, imm16
        case IF_T2_I1: // T2_I1   ................ rrrrrrrrrrrrrrrr                          imm16
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            if (fmt == IF_T2_I0)
            {
                code |= insEncodeRegT2_N(id->idReg1());
                code |= (1 << 21); //  W bit
            }
            imm = emitGetInsSC(id);
            assert((imm & 0x3) != 0x3);
            if (imm & 0x2)
                code |= 0x8000; //  PC bit
            if (imm & 0x1)
                code |= 0x4000; //  LR bit
            imm >>= 2;
            assert(imm <= 0x1fff); //  13 bits
            code |= imm;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_K1: // T2_K1   ............nnnn ttttiiiiiiiiiiii       R1  R2             imm12
        case IF_T2_K4: // T2_K4   ........U....... ttttiiiiiiiiiiii       R1  PC          U, imm12
        case IF_T2_K3: // T2_K3   ........U....... ....iiiiiiiiiiii       PC              U, imm12
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            if (fmt != IF_T2_K3)
            {
                code |= insEncodeRegT2_T(id->idReg1());
            }
            if (fmt == IF_T2_K1)
            {
                code |= insEncodeRegT2_N(id->idReg2());
                assert(imm <= 0xfff); //  12 bits
                code |= imm;
            }
            else
            {
                assert(unsigned_abs(imm) <= 0xfff); //  12 bits (signed)
                code |= abs(imm);
                if (imm >= 0)
                    code |= (1 << 23); //  U bit
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_K2: // T2_K2   ............nnnn ....iiiiiiiiiiii       R1                 imm12
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_N(id->idReg1());
            assert(imm <= 0xfff); //  12 bits
            code |= imm;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_L0: // T2_L0   .....i.....Snnnn .iiiddddiiiiiiii       R1  R2          S, imm8<<imm4
        case IF_T2_L1: // T2_L1   .....i.....S.... .iiiddddiiiiiiii       R1              S, imm8<<imm4
        case IF_T2_L2: // T2_L2   .....i......nnnn .iii....iiiiiiii       R1                 imm8<<imm4
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);

            if (fmt == IF_T2_L2)
                code |= insEncodeRegT2_N(id->idReg1());
            else
            {
                code |= insEncodeSetFlags(id->idInsFlags());
                code |= insEncodeRegT2_D(id->idReg1());
                if (fmt == IF_T2_L0)
                    code |= insEncodeRegT2_N(id->idReg2());
            }
            assert(isModImmConst(imm)); // Funky ARM imm encoding
            imm = encodeModImmConst(imm);
            assert(imm <= 0xfff); //  12 bits
            code |= (imm & 0x00ff);
            code |= (imm & 0x0700) << 4;
            code |= (imm & 0x0800) << 15;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_M0: // T2_M0   .....i......nnnn .iiiddddiiiiiiii       R1  R2             imm12
            sz   = emitGetInstrDescSize(id);
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            if (fmt == IF_T2_M0)
                code |= insEncodeRegT2_N(id->idReg2());
            imm = emitGetInsSC(id);
            assert(imm <= 0xfff); //  12 bits
            code |= (imm & 0x00ff);
            code |= (imm & 0x0700) << 4;
            code |= (imm & 0x0800) << 15;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_N: // T2_N    .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            imm = emitGetInsSC(id);
            if (id->idIsLclVar())
            {
                if (ins == INS_movw)
                {
                    imm &= 0xffff;
                }
                else
                {
                    assert(ins == INS_movt);
                    imm = (imm >> 16) & 0xffff;
                }
            }

            assert(!id->idIsReloc());
            code |= insEncodeImmT2_Mov(imm);
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_N2: // T2_N2   .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());
            imm  = emitGetInsSC(id);
            addr = emitConsBlock + imm;
            if (!id->idIsReloc())
            {
                assert(sizeof(size_t) == sizeof(target_size_t));
                imm = (target_size_t)(size_t)addr;
                if (ins == INS_movw)
                {
                    imm &= 0xffff;
                }
                else
                {
                    assert(ins == INS_movt);
                    imm = (imm >> 16) & 0xffff;
                }
                code |= insEncodeImmT2_Mov(imm);
                dst += emitOutput_Thumb2Instr(dst, code);
            }
            else
            {
                assert((ins == INS_movt) || (ins == INS_movw));
                dst += emitOutput_Thumb2Instr(dst, code);
                if ((ins == INS_movt) && emitComp->info.compMatchedVM)
                    emitHandlePCRelativeMov32((void*)(dst - 8), addr);
            }
            break;

        case IF_T2_N3: // T2_N3   .....i......iiii .iiiddddiiiiiiii       R1                 imm16
            sz   = sizeof(instrDescReloc);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_D(id->idReg1());

            assert((ins == INS_movt) || (ins == INS_movw));
            assert(id->idIsReloc());

            addr = emitGetInsRelocValue(id);
            dst += emitOutput_Thumb2Instr(dst, code);
            if ((ins == INS_movt) && emitComp->info.compMatchedVM)
                emitHandlePCRelativeMov32((void*)(dst - 8), addr);
            break;

        case IF_T2_VFP3:
            // these are the binary operators
            // d = n - m
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_VectorN(id->idReg2(), size, true);
            code |= insEncodeRegT2_VectorM(id->idReg3(), size, true);
            code |= insEncodeRegT2_VectorD(id->idReg1(), size, true);
            if (size == EA_8BYTE)
                code |= 1 << 8;
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_VFP2:
        {
            emitAttr srcSize;
            emitAttr dstSize;
            size_t   szCode = 0;

            switch (ins)
            {
                case INS_vcvt_i2d:
                case INS_vcvt_u2d:
                case INS_vcvt_f2d:
                    srcSize = EA_4BYTE;
                    dstSize = EA_8BYTE;
                    break;

                case INS_vcvt_d2i:
                case INS_vcvt_d2u:
                case INS_vcvt_d2f:
                    srcSize = EA_8BYTE;
                    dstSize = EA_4BYTE;
                    break;

                case INS_vmov:
                case INS_vabs:
                case INS_vsqrt:
                case INS_vcmp:
                case INS_vneg:
                    if (id->idOpSize() == EA_8BYTE)
                        szCode |= (1 << 8);
                    FALLTHROUGH;

                default:
                    srcSize = dstSize = id->idOpSize();
                    break;
            }

            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= szCode;
            code |= insEncodeRegT2_VectorD(id->idReg1(), dstSize, true);
            code |= insEncodeRegT2_VectorM(id->idReg2(), srcSize, true);

            dst += emitOutput_Thumb2Instr(dst, code);
            break;
        }

        case IF_T2_VLDST:
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT2_N(id->idReg2());
            code |= insEncodeRegT2_VectorD(id->idReg1(), size, true);

            imm = emitGetInsSC(id);
            if (imm < 0)
                imm = -imm; // bit 23 at 0 means negate
            else
                code |= 1 << 23; // set the positive bit

            // offset is +/- 1020
            assert(!(imm % 4));
            assert(imm >> 10 == 0);
            code |= imm >> 2;
            // bit 8 is set for doubles
            if (id->idOpSize() == EA_8BYTE)
                code |= (1 << 8);
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_VMOVD:
            // 3op assemble a double from two int regs (or back)
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            if (ins == INS_vmov_i2d)
            {
                code |= insEncodeRegT2_VectorM(id->idReg1(), size, true);
                code |= id->idReg2() << 12;
                code |= id->idReg3() << 16;
            }
            else
            {
                assert(ins == INS_vmov_d2i);
                code |= id->idReg1() << 12;
                code |= id->idReg2() << 16;
                code |= insEncodeRegT2_VectorM(id->idReg3(), size, true);
            }
            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T2_VMOVS:
            // 2op assemble a float from one int reg (or back)
            sz   = emitGetInstrDescSize(id);
            code = emitInsCode(ins, fmt);
            if (ins == INS_vmov_f2i)
            {
                code |= insEncodeRegT2_VectorN(id->idReg2(), EA_4BYTE, true);
                code |= id->idReg1() << 12;
            }
            else
            {
                assert(ins == INS_vmov_i2f);
                code |= insEncodeRegT2_VectorN(id->idReg1(), EA_4BYTE, true);
                code |= id->idReg2() << 12;
            }

            dst += emitOutput_Thumb2Instr(dst, code);
            break;

        case IF_T1_J3: // T1_J3   .....dddiiiiiiii                        R1  PC             imm8
        case IF_T2_M1: // T2_M1   .....i.......... .iiiddddiiiiiiii       R1  PC             imm12
            assert(id->idGCref() == GCT_NONE);
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescLbl);
            break;

        case IF_T1_K:  // T1_K    ....cccciiiiiiii                       Branch              imm8, cond4
        case IF_T1_M:  // T1_M    .....iiiiiiiiiii                       Branch              imm11
        case IF_T2_J1: // T2_J1   .....Scccciiiiii ..j.jiiiiiiiiiii      Branch              imm20, cond4
        case IF_T2_J2: // T2_J2   .....Siiiiiiiiii ..j.jiiiiiiiiii.      Branch              imm24
        case IF_T2_N1: // T2_N    .....i......iiii .iiiddddiiiiiiii       R1                 imm16
        case IF_LARGEJMP:
            assert(id->idGCref() == GCT_NONE);
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_T1_D1: // T1_D1   .........mmmm...                       R1*

            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_M4(id->idReg1());
            dst += emitOutput_Thumb1Instr(dst, code);
            sz = SMALL_IDSC_SIZE;
            break;

        case IF_T1_D2: // T1_D2   .........mmmm...                                R3*

            /* Is this a "fat" call descriptor? */

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

            code = emitInsCode(ins, fmt);
            code |= insEncodeRegT1_M4(id->idReg3());
            callInstrSize = SafeCvtAssert<unsigned char>(emitOutput_Thumb1Instr(dst, code));
            dst += callInstrSize;
            goto DONE_CALL;

        case IF_T2_J3: // T2_J3   .....Siiiiiiiiii ..j.jiiiiiiiiii.      Call                imm24

            /* Is this a "fat" call descriptor? */

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

            if (id->idAddr()->iiaAddr == NULL) /* a recursive call */
            {
                addr = emitCodeBlock;
            }
            else
            {
                addr = id->idAddr()->iiaAddr;
            }
            code = emitInsCode(ins, fmt);

            if (id->idIsDspReloc())
            {
                callInstrSize = SafeCvtAssert<unsigned char>(emitOutput_Thumb2Instr(dst, code));
                dst += callInstrSize;
                if (emitComp->info.compMatchedVM)
                    emitRecordRelocation((void*)(dst - 4), addr, IMAGE_REL_BASED_THUMB_BRANCH24);
            }
            else
            {
                addr = (BYTE*)((size_t)addr & ~1); // Clear the lowest bit from target address

                /* Calculate PC relative displacement */
                ptrdiff_t disp = addr - (dst + 4);
                bool      S    = (disp < 0);
                bool      I1   = ((disp & 0x00800000) == 0);
                bool      I2   = ((disp & 0x00400000) == 0);

                if (S)
                    code |= (1 << 26); // S bit
                if (S ^ I1)
                    code |= (1 << 13); // J1 bit
                if (S ^ I2)
                    code |= (1 << 11); // J2 bit

                int immLo = (disp & 0x00000ffe) >> 1;
                int immHi = (disp & 0x003ff000) >> 12;

                code |= (immHi << 16);
                code |= immLo;

                disp = abs(disp);
                assert((disp & 0x00fffffe) == disp);

                callInstrSize = SafeCvtAssert<unsigned char>(emitOutput_Thumb2Instr(dst, code));
                dst += callInstrSize;
            }

        DONE_CALL:

            /* We update the GC info before the call as the variables cannot be
               used by the call. Killing variables before the call helps with
               boundary conditions if the call is CORINFO_HELP_THROW - see bug 50029.
               If we ever track aliased variables (which could be used by the
               call), we would have to keep them alive past the call. */

            emitUpdateLiveGCvars(GCvars, *dp);

#ifdef DEBUG
            // Output any delta in GC variable info, corresponding to the before-call GC var updates done above.
            if (EMIT_GC_VERBOSE || emitComp->opts.disasmWithGC)
            {
                emitDispGCVarDelta();
            }
#endif // DEBUG

            // If the method returns a GC ref, mark R0 appropriately.
            if (id->idGCref() == GCT_GCREF)
                gcrefRegs |= RBM_R0;
            else if (id->idGCref() == GCT_BYREF)
                byrefRegs |= RBM_R0;

            // If the GC register set has changed, report the new set.
            if (gcrefRegs != emitThisGCrefRegs)
                emitUpdateLiveGCregs(GCT_GCREF, gcrefRegs, dst);

            if (byrefRegs != emitThisByrefRegs)
                emitUpdateLiveGCregs(GCT_BYREF, byrefRegs, dst);

            // Some helper calls may be marked as not requiring GC info to be recorded.
            if ((!id->idIsNoGC()))
            {
                // On ARM, as on AMD64, we don't change the stack pointer to push/pop args.
                // So we're not really doing a "stack pop" here (note that "args" is 0), but we use this mechanism
                // to record the call for GC info purposes.  (It might be best to use an alternate call,
                // and protect "emitStackPop" under the EMIT_TRACK_STACK_DEPTH preprocessor variable.)
                emitStackPop(dst, /*isCall*/ true, callInstrSize, /*args*/ 0);

                /* Do we need to record a call location for GC purposes? */

                if (!emitFullGCinfo)
                {
                    emitRecordGCcall(dst, callInstrSize);
                }
            }

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

    // Determine if any registers now hold GC refs, or whether a register that was overwritten held a GC ref.
    // We assume here that "id->idGCref()" is not GC_NONE only if the instruction described by "id" writes a
    // GC ref to register "id->idReg1()".  (It may, apparently, also not be GC_NONE in other cases, such as
    // for stores, but we ignore those cases here.)
    if (emitInsMayWriteToGCReg(id)) // True if "id->idIns()" writes to a register than can hold GC ref.
    {
        // If we ever generate instructions that write to multiple registers (LDM, or POP),
        // then we'd need to more work here to ensure that changes in the status of GC refs are
        // tracked properly.
        if (emitInsMayWriteMultipleRegs(id))
        {
            // We explicitly list the multiple-destination-target instruction that we expect to
            // be emitted outside of the prolog and epilog here.
            switch (ins)
            {
                case INS_smull:
                case INS_umull:
                case INS_smlal:
                case INS_umlal:
                case INS_vmov_d2i:
                    // For each of these, idReg1() and idReg2() are the destination registers.
                    emitGCregDeadUpd(id->idReg1(), dst);
                    emitGCregDeadUpd(id->idReg2(), dst);
                    break;
                default:
                    assert(false); // We need to recognize this multi-target instruction...
            }
        }
        else
        {
            if (id->idGCref() != GCT_NONE)
            {
                emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
            }
            else
            {
                // I also assume that "idReg1" is the destination register of all instructions that write to registers.
                emitGCregDeadUpd(id->idReg1(), dst);
            }
        }
    }

    // Now we determine if the instruction has written to a (local variable) stack location, and either written a GC
    // ref or overwritten one.
    if (emitInsWritesToLclVarStackLoc(id))
    {
        int       varNum = id->idAddr()->iiaLclVar.lvaVarNum();
        unsigned  ofs    = AlignDown(id->idAddr()->iiaLclVar.lvaOffset(), TARGET_POINTER_SIZE);
        regNumber regBase;
        int adr = emitComp->lvaFrameAddress(varNum, true, &regBase, ofs, /* isFloatUsage */ false); // no float GC refs
        if (id->idGCref() != GCT_NONE)
        {
            emitGCvarLiveUpd(adr + ofs, varNum, id->idGCref(), dst DEBUG_ARG(varNum));
        }
        else
        {
            // If the type of the local is a gc ref type, update the liveness.
            var_types vt;
            if (varNum >= 0)
            {
                // "Regular" (non-spill-temp) local.
                vt = var_types(emitComp->lvaTable[varNum].lvType);
            }
            else
            {
                TempDsc* tmpDsc = codeGen->regSet.tmpFindNum(varNum);
                vt              = tmpDsc->tdTempType();
            }
            if (vt == TYP_REF || vt == TYP_BYREF)
                emitGCvarDeadUpd(adr + ofs, dst DEBUG_ARG(varNum));
        }
    }

#ifdef DEBUG
    /* Make sure we set the instruction descriptor size correctly */

    size_t expected = emitSizeOfInsDsc(id);
    assert(sz == expected);

    if (emitComp->opts.disAsm || emitComp->verbose)
    {
        emitDispIns(id, false, dspOffs, true, emitCurCodeOffs(odst), *dp, (dst - *dp), ig);
    }

    if (emitComp->compDebugBreak)
    {
        // set JitEmitPrintRefRegs=1 will print out emitThisGCrefRegs and emitThisByrefRegs
        // at the beginning of this method.
        if (JitConfig.JitEmitPrintRefRegs() != 0)
        {
            printf("Before emitOutputInstr for id->idDebugOnlyInfo()->idNum=0x%02x\n", id->idDebugOnlyInfo()->idNum);
            printf("  emitThisGCrefRegs(0x%p)=", dspPtr(&emitThisGCrefRegs));
            printRegMaskInt(emitThisGCrefRegs);
            emitDispGprRegSet(emitThisGCrefRegs);
            printf("\n");
            printf("  emitThisByrefRegs(0x%p)=", dspPtr(&emitThisByrefRegs));
            printRegMaskInt(emitThisByrefRegs);
            emitDispGprRegSet(emitThisByrefRegs);
            printf("\n");
        }

        // For example, set JitBreakEmitOutputInstr=a6 will break when this method is called for
        // emitting instruction a6, (i.e. IN00a6 in jitdump).
        if ((unsigned)JitConfig.JitBreakEmitOutputInstr() == id->idDebugOnlyInfo()->idNum)
        {
            assert(!"JitBreakEmitOutputInstr reached");
        }
    }

    // Output any delta in GC info.
    if (EMIT_GC_VERBOSE || emitComp->opts.disasmWithGC)
    {
        emitDispGCInfoDelta();
    }
#else
    if (emitComp->opts.disAsm)
    {
        size_t expected = emitSizeOfInsDsc(id);
        assert(sz == expected);
        emitDispIns(id, false, 0, true, emitCurCodeOffs(odst), *dp, (dst - *dp), ig);
    }
#endif

    /* All instructions are expected to generate code */

    assert(*dp != dst);

    *dp = dst;

    return sz;
}

/*****************************************************************************/
/*****************************************************************************/

static bool insAlwaysSetFlags(instruction ins)
{
    bool result = false;
    switch (ins)
    {
        case INS_cmp:
        case INS_cmn:
        case INS_teq:
        case INS_tst:
            result = true;
            break;

        default:
            break;
    }
    return result;
}

/*****************************************************************************
 *
 *  Display the instruction name, optionally the instruction
 *   can add the "s" suffix if it must set the flags.
 */
void emitter::emitDispInst(instruction ins, insFlags flags)
{
    const char* insstr = codeGen->genInsName(ins);
    size_t      len    = strlen(insstr);

    /* Display the instruction name */

    printf("%s", insstr);
    if (insSetsFlags(flags) && !insAlwaysSetFlags(ins))
    {
        printf("s");
        len++;
    }

    //
    // Add at least one space after the instruction name
    // and add spaces until we have reach the normal size of 8
    do
    {
        printf(" ");
        len++;
    } while (len < 8);
}

#define STRICT_ARM_ASM 0

/*****************************************************************************
 *
 *  Display an immediate value
 */
void emitter::emitDispImm(int imm, bool addComma, bool alwaysHex /* =false */, bool isAddrOffset /*= false*/)
{
    if (!alwaysHex && (imm > -1000) && (imm < 1000))
    {
        printf("%d", imm);
    }
    else if ((imm > 0) ||
             (imm == -imm) || // -0x80000000 == 0x80000000. So we don't want to add an extra "-" at the beginning.
             (emitComp->opts.disDiffable && (imm == (int)0xD1FFAB1E))) // Don't display this as negative
    {
        if (isAddrOffset)
        {
            printf("0x%02X", imm);
        }
        else
        {
            printf("0x%02x", imm);
        }
    }
    else
    {
        // val <= -1000
        if (isAddrOffset)
        {
            printf("-0x%02X", -imm);
        }
        else
        {
            printf("-0x%02x", -imm);
        }
    }

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display a relocatable immediate value
 */
void emitter::emitDispReloc(BYTE* addr)
{
    printf("0x%p", dspPtr(addr));
}

/*****************************************************************************
 *
 *  Display an arm condition for the IT instructions
 */
void emitter::emitDispCond(int cond)
{
    const static char* armCond[16] = {"eq", "ne", "hs", "lo", "mi", "pl", "vs", "vc",
                                      "hi", "ls", "ge", "lt", "gt", "le", "AL", "NV"}; // The last two are invalid
    assert(0 <= cond && (unsigned)cond < ArrLen(armCond));
    printf(armCond[cond]);
}

/*****************************************************************************
 *
 *  Display a register range in a range format
 */
void emitter::emitDispRegRange(regNumber reg, int len, emitAttr attr)
{
    printf("{");
    emitDispReg(reg, attr, false);
    if (len > 1)
    {
        printf("-");
        emitDispReg((regNumber)(reg + len - 1), attr, false);
    }
    printf("}");
}

/*****************************************************************************
 *
 *  Display an register mask in a list format
 */
void emitter::emitDispRegmask(int imm, bool encodedPC_LR)
{
    bool printedOne = false;
    bool hasPC;
    bool hasLR;

    if (encodedPC_LR)
    {
        hasPC = (imm & 2) != 0;
        hasLR = (imm & 1) != 0;
        imm >>= 2;
    }
    else
    {
        hasPC = (imm & RBM_PC) != 0;
        hasLR = (imm & RBM_LR) != 0;
        imm &= ~(RBM_PC | RBM_LR);
    }

    regNumber reg = REG_R0;
    unsigned  bit = 1;

    printf("{");
    while (imm != 0)
    {
        if (bit & imm)
        {
            if (printedOne)
                printf(",");
            printf("%s", emitRegName(reg));
            printedOne = true;
            imm -= bit;
        }

        reg = regNumber(reg + 1);
        bit <<= 1;
    }

    if (hasLR)
    {
        if (printedOne)
            printf(",");
        printf("%s", emitRegName(REG_LR));
        printedOne = true;
    }

    if (hasPC)
    {
        if (printedOne)
            printf(",");
        printf("%s", emitRegName(REG_PC));
        printedOne = true;
    }
    printf("}");
}

/*****************************************************************************
 *
 *  Returns the encoding for the Shift Type bits to be used in a Thumb-2 encoding
 */

void emitter::emitDispShiftOpts(insOpts opt)
{
    if (opt == INS_OPTS_LSL)
        printf(" LSL ");
    else if (opt == INS_OPTS_LSR)
        printf(" LSR ");
    else if (opt == INS_OPTS_ASR)
        printf(" ASR ");
    else if (opt == INS_OPTS_ROR)
        printf(" ROR ");
    else if (opt == INS_OPTS_RRX)
        printf(" RRX ");
}

/*****************************************************************************
 *
 *  Display a register
 */
void emitter::emitDispReg(regNumber reg, emitAttr attr, bool addComma)
{
    if (isFloatReg(reg))
    {
        if (attr == EA_8BYTE)
        {
            unsigned regIndex = reg - REG_F0;
            regIndex >>= 1;

            if (regIndex < 10)
            {
                printf("d%c", regIndex + '0');
            }
            else
            {
                assert(regIndex < 100);
                printf("d%c%c", (regIndex / 10) + '0', (regIndex % 10) + '0');
            }
        }
        else
        {
            printf("s%s", emitFloatRegName(reg, attr) + 1);
        }
    }
    else
    {
        printf("%s", emitRegName(reg, attr));
    }

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg]
 */
void emitter::emitDispAddrR(regNumber reg, emitAttr attr)
{
    printf("[");
    emitDispReg(reg, attr, false);
    printf("]");
    emitDispGC(attr);
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + imm]
 */
void emitter::emitDispAddrRI(regNumber reg, int imm, emitAttr attr)
{
    bool regIsSPorFP = (reg == REG_SP) || (reg == REG_FP);

    printf("[");
    emitDispReg(reg, attr, false);
    if (imm != 0)
    {
        if (imm >= 0)
        {
#if STRICT_ARM_ASM
            printf(", ");
#else
            printf("+");
#endif
        }
        emitDispImm(imm, false, true, true);
    }
    printf("]");
    emitDispGC(attr);
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + reg]
 */
void emitter::emitDispAddrRR(regNumber reg1, regNumber reg2, emitAttr attr)
{
    printf("[");
    emitDispReg(reg1, attr, false);
#if STRICT_ARM_ASM
    printf(", ");
#else
    printf("+");
#endif
    emitDispReg(reg2, attr, false);
    printf("]");
    emitDispGC(attr);
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + reg * imm]
 */
void emitter::emitDispAddrRRI(regNumber reg1, regNumber reg2, int imm, emitAttr attr)
{
    printf("[");
    emitDispReg(reg1, attr, false);
#if STRICT_ARM_ASM
    printf(", ");
    emitDispReg(reg2, attr, false);
    if (imm > 0)
    {
        printf(" LSL ");
        emitDispImm(1 << imm, false);
    }
#else
    printf("+");
    if (imm > 0)
    {
        emitDispImm(1 << imm, false);
        printf("*");
    }
    emitDispReg(reg2, attr, false);
#endif
    printf("]");
    emitDispGC(attr);
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + imm]
 */
void emitter::emitDispAddrPUW(regNumber reg, int imm, insOpts opt, emitAttr attr)
{
    bool regIsSPorFP = (reg == REG_SP) || (reg == REG_FP);

    printf("[");
    emitDispReg(reg, attr, false);
    if (insOptAnyInc(opt))
        printf("!");

    if (imm != 0)
    {
        if (imm >= 0)
        {
#if STRICT_ARM_ASM
            printf(", ");
#else
            printf("+");
#endif
        }
        emitDispImm(imm, false, true, true);
    }
    printf("]");

    emitDispGC(attr);
}

/*****************************************************************************
 *
 *  Display the gc-ness of the operand
 */
void emitter::emitDispGC(emitAttr attr)
{
#if 0
    // TODO-ARM-Cleanup: Fix or delete.
    if (attr == EA_GCREF)
        printf(" @gc");
    else if (attr == EA_BYREF)
        printf(" @byref");
#endif
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    if (!emitComp->opts.disCodeBytes)
    {
        return;
    }

    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable)
    {
        if (sz == 2)
        {
            printf("  %04X     ", (*((unsigned short*)code)));
        }
        else if (sz == 4)
        {
            printf("  %04X %04X", (*((unsigned short*)(code + 0))), (*((unsigned short*)(code + 2))));
        }
        else
        {
            assert(sz == 0);

            // At least display the encoding size of the instruction, even if not displaying its actual encoding.
            insSize isz = emitInsSize(id->idInsFmt());
            switch (isz)
            {
                case ISZ_16BIT:
                    printf("  2B");
                    break;
                case ISZ_32BIT:
                    printf("  4B");
                    break;
                case ISZ_48BIT:
                    printf("  6B");
                    break;
                default:
                    unreached();
            }
        }
    }
}

/****************************************************************************
 *
 *  Display the given instruction.
 */

void emitter::emitDispInsHelp(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* code, size_t sz, insGroup* ig)
{
#ifdef DEBUG
    if (EMITVERBOSE)
    {
        unsigned idNum = id->idDebugOnlyInfo()->idNum; // Do not remove this!  It is needed for VisualStudio
                                                       // conditional breakpoints

        printf("IN%04x: ", idNum);
    }
#endif

    if (code == NULL)
        sz = 0;

    if (!isNew && !asmfm && sz)
    {
        doffs = true;
    }

    /* Display the instruction address */

    emitDispInsAddr(code);

    /* Display the instruction offset */

    emitDispInsOffs(offset, doffs);

    BYTE* codeRW = nullptr;
    if (code != nullptr)
    {
        /* Display the instruction hex code */
        assert(((code >= emitCodeBlock) && (code < emitCodeBlock + emitTotalHotCodeSize)) ||
               ((code >= emitColdCodeBlock) && (code < emitColdCodeBlock + emitTotalColdCodeSize)));

        codeRW = code + writeableOffset;
    }

    emitDispInsHex(id, codeRW, sz);

    printf("      ");

    /* Get the instruction and format */

    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    emitDispInst(ins, id->idInsFlags());

    /* If this instruction has just been added, check its size */

    assert(isNew == false || (int)emitSizeOfInsDsc(id) == emitCurIGfreeNext - (BYTE*)id);

    /* Figure out the operand size */
    emitAttr attr;
    if (id->idGCref() == GCT_GCREF)
        attr = EA_GCREF;
    else if (id->idGCref() == GCT_BYREF)
        attr = EA_BYREF;
    else
        attr = id->idOpSize();

    switch (fmt)
    {
        int         imm;
        const char* methodName;

        case IF_T1_A: // None
        case IF_T2_A:
            break;

        case IF_T1_L0: // Imm
        case IF_T2_B:
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_T1_B: // <cond>
            emitDispCond(emitGetInsSC(id));
            break;

        case IF_T1_L1: // <regmask8>
        case IF_T2_I1: // <regmask16>
            emitDispRegmask(emitGetInsSC(id), true);
            break;

        case IF_T2_E2: // Reg
            if (id->idIns() == INS_vmrs)
            {
                if (id->idReg1() != REG_R15)
                {
                    emitDispReg(id->idReg1(), attr, true);
                    printf("FPSCR");
                }
                else
                {
                    printf("APSR, FPSCR");
                }
            }
            else
            {
                emitDispReg(id->idReg1(), attr, false);
            }
            break;

        case IF_T1_D1:
            emitDispReg(id->idReg1(), attr, false);
            break;

        case IF_T1_D2:
            emitDispReg(id->idReg3(), attr, false);
            {
                CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie;
                if (handle != 0)
                {
                    methodName = emitComp->eeGetMethodFullName(handle);
                    printf("\t\t// %s", methodName);
                }
            }
            break;

        case IF_T1_F: // SP, Imm
            emitDispReg(REG_SP, attr, true);
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_T1_J0: // Reg, Imm
        case IF_T2_L1:
        case IF_T2_L2:
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            emitDispImm(imm, false, false);
            break;

        case IF_T2_N:
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            if (emitComp->opts.disDiffable)
                imm = 0xD1FF;
            emitDispImm(imm, false, true);
            break;

        case IF_T2_N3:
            emitDispReg(id->idReg1(), attr, true);
            printf("%s RELOC ", (id->idIns() == INS_movw) ? "LOW" : "HIGH");
            emitDispReloc(emitGetInsRelocValue(id));
            break;

        case IF_T2_N2:
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            {
                dataSection*  jdsc = 0;
                NATIVE_OFFSET offs = 0;

                /* Find the appropriate entry in the data section list */

                for (jdsc = emitConsDsc.dsdList; jdsc; jdsc = jdsc->dsNext)
                {
                    UNATIVE_OFFSET size = jdsc->dsSize;

                    /* Is this a label table? */

                    if (jdsc->dsType == dataSection::blockAbsoluteAddr)
                    {
                        if (offs == imm)
                            break;
                    }

                    offs += size;
                }

                assert(jdsc != NULL);

                if (id->idIsDspReloc())
                {
                    printf("reloc ");
                }
                printf("%s ADDRESS J_M%03u_DS%02u", (id->idIns() == INS_movw) ? "LOW" : "HIGH", emitComp->compMethodID,
                       imm);

                // After the MOVT, dump the table
                if (id->idIns() == INS_movt)
                {
                    unsigned     cnt = jdsc->dsSize / TARGET_POINTER_SIZE;
                    BasicBlock** bbp = (BasicBlock**)jdsc->dsCont;

                    bool isBound = (emitCodeGetCookie(*bbp) != NULL);

                    if (isBound)
                    {
                        printf("\n\n    J_M%03u_DS%02u LABEL   DWORD", emitComp->compMethodID, imm);

                        /* Display the label table (it's stored as "BasicBlock*" values) */

                        do
                        {
                            insGroup* lab;

                            /* Convert the BasicBlock* value to an IG address */

                            lab = (insGroup*)emitCodeGetCookie(*bbp++);
                            assert(lab);

                            printf("\n            DD      %s", emitLabelString(lab));
                        } while (--cnt);
                    }
                }
            }
            break;

        case IF_T2_H2: // [Reg+imm]
        case IF_T2_K2:
            emitDispAddrRI(id->idReg1(), emitGetInsSC(id), attr);
            break;

        case IF_T2_K3: // [PC+imm]
            emitDispAddrRI(REG_PC, emitGetInsSC(id), attr);
            break;

        case IF_T1_J1: // reg, <regmask8>
        case IF_T2_I0: // reg, <regmask16>
            emitDispReg(id->idReg1(), attr, false);
            printf("!, ");
            emitDispRegmask(emitGetInsSC(id), false);
            break;

        case IF_T1_D0: // Reg, Reg
        case IF_T1_E:
        case IF_T2_C3:
        case IF_T2_C9:
        case IF_T2_C10:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, false);
            if (fmt == IF_T1_E && id->idIns() == INS_rsb)
            {
                printf(", 0");
            }
            break;

        case IF_T2_E1: // Reg, [Reg]
            emitDispReg(id->idReg1(), attr, true);
            emitDispAddrR(id->idReg2(), attr);
            break;

        case IF_T2_D1: // Reg, Imm, Imm
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            {
                int lsb  = (imm >> 5) & 0x1f;
                int msb  = imm & 0x1f;
                int imm1 = lsb;
                int imm2 = msb + 1 - lsb;
                emitDispImm(imm1, true);
                emitDispImm(imm2, false);
            }
            break;

        case IF_T1_C: // Reg, Reg, Imm
        case IF_T1_G:
        case IF_T2_C2:
        case IF_T2_H1:
        case IF_T2_K1:
        case IF_T2_L0:
        case IF_T2_M0:
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            if (emitInsIsLoadOrStore(ins))
            {
                emitDispAddrRI(id->idReg2(), imm, attr);
            }
            else
            {
                emitDispReg(id->idReg2(), attr, true);
                emitDispImm(imm, false);
            }
            break;

        case IF_T1_J2:
            emitDispReg(id->idReg1(), attr, true);
            imm = emitGetInsSC(id);
            if (emitInsIsLoadOrStore(ins))
            {
                emitDispAddrRI(REG_SP, imm, attr);
            }
            else
            {
                emitDispReg(REG_SP, attr, true);
                emitDispImm(imm, false);
            }
            break;

        case IF_T2_K4:
            emitDispReg(id->idReg1(), attr, true);
            emitDispAddrRI(REG_PC, emitGetInsSC(id), attr);
            break;

        case IF_T2_C1:
        case IF_T2_C8:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, false);
            imm = emitGetInsSC(id);
            if (id->idInsOpt() == INS_OPTS_RRX)
            {
                emitDispShiftOpts(id->idInsOpt());
                assert(imm == 1);
            }
            else if (imm > 0)
            {
                emitDispShiftOpts(id->idInsOpt());
                emitDispImm(imm, false);
            }
            break;

        case IF_T2_C6:
            imm = emitGetInsSC(id);
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, (imm != 0));
            if (imm != 0)
            {
                emitDispImm(imm, false);
            }
            break;

        case IF_T2_C7:
            emitDispAddrRRI(id->idReg1(), id->idReg2(), emitGetInsSC(id), attr);
            break;

        case IF_T2_H0:
            emitDispReg(id->idReg1(), attr, true);
            emitDispAddrPUW(id->idReg2(), emitGetInsSC(id), id->idInsOpt(), attr);
            break;

        case IF_T1_H: // Reg, Reg, Reg
            emitDispReg(id->idReg1(), attr, true);
            if (emitInsIsLoadOrStore(ins))
            {
                emitDispAddrRR(id->idReg2(), id->idReg3(), attr);
            }
            else
            {
                emitDispReg(id->idReg2(), attr, true);
                emitDispReg(id->idReg3(), attr, false);
            }
            break;

        case IF_T2_C4:
        case IF_T2_C5:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            emitDispReg(id->idReg3(), attr, false);
            break;

        case IF_T2_VFP3:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            emitDispReg(id->idReg3(), attr, false);
            break;

        case IF_T2_VFP2:
            switch (id->idIns())
            {
                case INS_vcvt_d2i:
                case INS_vcvt_d2u:
                case INS_vcvt_d2f:
                    emitDispReg(id->idReg1(), EA_4BYTE, true);
                    emitDispReg(id->idReg2(), EA_8BYTE, false);
                    break;

                case INS_vcvt_i2d:
                case INS_vcvt_u2d:
                case INS_vcvt_f2d:
                    emitDispReg(id->idReg1(), EA_8BYTE, true);
                    emitDispReg(id->idReg2(), EA_4BYTE, false);
                    break;

                // we just use the type on the instruction
                // unless it is an asymmetrical one like the converts
                default:
                    emitDispReg(id->idReg1(), attr, true);
                    emitDispReg(id->idReg2(), attr, false);
                    break;
            }
            break;

        case IF_T2_VLDST:
            imm = emitGetInsSC(id);
            switch (id->idIns())
            {
                case INS_vldr:
                case INS_vstr:
                    emitDispReg(id->idReg1(), attr, true);
                    emitDispAddrPUW(id->idReg2(), imm, id->idInsOpt(), attr);
                    break;

                case INS_vldm:
                case INS_vstm:
                    emitDispReg(id->idReg2(), attr, false);
                    if (insOptAnyInc(id->idInsOpt()))
                        printf("!");
                    printf(", ");
                    emitDispRegRange(id->idReg1(), abs(imm) >> 2, attr);
                    break;

                case INS_vpush:
                case INS_vpop:
                    emitDispRegRange(id->idReg1(), abs(imm) >> 2, attr);
                    break;

                default:
                    unreached();
            }
            break;

        case IF_T2_VMOVD:
            switch (id->idIns())
            {
                case INS_vmov_i2d:
                    emitDispReg(id->idReg1(), attr, true); // EA_8BYTE
                    emitDispReg(id->idReg2(), EA_4BYTE, true);
                    emitDispReg(id->idReg3(), EA_4BYTE, false);
                    break;
                case INS_vmov_d2i:
                    emitDispReg(id->idReg1(), EA_4BYTE, true);
                    emitDispReg(id->idReg2(), EA_4BYTE, true);
                    emitDispReg(id->idReg3(), attr, false); // EA_8BYTE
                    break;
                default:
                    unreached();
            }
            break;

        case IF_T2_VMOVS:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, false);
            break;

        case IF_T2_G1:
            emitDispReg(id->idReg1(), attr, true);
            emitDispAddrRR(id->idReg2(), id->idReg3(), attr);
            break;

        case IF_T2_D0: // Reg, Reg, Imm, Imm
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            imm = emitGetInsSC(id);
            if (ins == INS_bfi)
            {
                int lsb  = (imm >> 5) & 0x1f;
                int msb  = imm & 0x1f;
                int imm1 = lsb;
                int imm2 = msb + 1 - lsb;
                emitDispImm(imm1, true);
                emitDispImm(imm2, false);
            }
            else
            {
                int lsb     = (imm >> 5) & 0x1f;
                int widthm1 = imm & 0x1f;
                int imm1    = lsb;
                int imm2    = widthm1 + 1;
                emitDispImm(imm1, true);
                emitDispImm(imm2, false);
            }
            break;

        case IF_T2_C0: // Reg, Reg, Reg, Imm
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            emitDispReg(id->idReg3(), attr, false);
            imm = emitGetInsSC(id);
            if (id->idInsOpt() == INS_OPTS_RRX)
            {
                emitDispShiftOpts(id->idInsOpt());
                assert(imm == 1);
            }
            else if (imm > 0)
            {
                emitDispShiftOpts(id->idInsOpt());
                emitDispImm(imm, false);
            }
            break;

        case IF_T2_E0:
            emitDispReg(id->idReg1(), attr, true);
            if (id->idIsLclVar())
            {
                emitDispAddrRRI(id->idReg2(), codeGen->rsGetRsvdReg(), 0, attr);
            }
            else
            {
                emitDispAddrRRI(id->idReg2(), id->idReg3(), emitGetInsSC(id), attr);
            }
            break;

        case IF_T2_G0:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            emitDispAddrPUW(id->idReg3(), emitGetInsSC(id), id->idInsOpt(), attr);
            break;

        case IF_T2_F1: // Reg, Reg, Reg, Reg
        case IF_T2_F2:
            emitDispReg(id->idReg1(), attr, true);
            emitDispReg(id->idReg2(), attr, true);
            emitDispReg(id->idReg3(), attr, true);
            emitDispReg(id->idReg4(), attr, false);
            break;

        case IF_T1_J3:
        case IF_T2_M1: // Load Label
            emitDispReg(id->idReg1(), attr, true);
            if (id->idIsBound())
                emitPrintLabel(id->idAddr()->iiaIGlabel);
            else
                printf("L_M%03u_" FMT_BB, emitComp->compMethodID, id->idAddr()->iiaBBlabel->bbNum);
            break;

        case IF_T1_I: // Special Compare-and-branch
            emitDispReg(id->idReg1(), attr, true);
            FALLTHROUGH;

        case IF_T1_K: // Special Branch, conditional
        case IF_T1_M:
            assert(((instrDescJmp*)id)->idjShort);
            printf("SHORT ");
            FALLTHROUGH;

        case IF_T2_N1:
            if (fmt == IF_T2_N1)
            {
                emitDispReg(id->idReg1(), attr, true);
                printf("%s ADDRESS ", (id->idIns() == INS_movw) ? "LOW" : "HIGH");
            }
            FALLTHROUGH;

        case IF_T2_J1:
        case IF_T2_J2:
        case IF_LARGEJMP:
        {
            if (id->idAddr()->iiaHasInstrCount())
            {
                int instrCount = id->idAddr()->iiaGetInstrCount();

                if (ig == NULL)
                {
                    printf("pc%s%d instructions", (instrCount >= 0) ? "+" : "", instrCount);
                }
                else
                {
                    unsigned       insNum  = emitFindInsNum(ig, id);
                    UNATIVE_OFFSET srcOffs = ig->igOffs + emitFindOffset(ig, insNum + 1);
                    UNATIVE_OFFSET dstOffs = ig->igOffs + emitFindOffset(ig, insNum + 1 + instrCount);
                    ssize_t        relOffs = (ssize_t)(emitOffsetToPtr(dstOffs) - emitOffsetToPtr(srcOffs));
                    printf("pc%s%d (%d instructions)", (relOffs >= 0) ? "+" : "", (int)relOffs, (int)instrCount);
                }
            }
            else if (id->idIsBound())
                emitPrintLabel(id->idAddr()->iiaIGlabel);
            else
                printf("L_M%03u_" FMT_BB, emitComp->compMethodID, id->idAddr()->iiaBBlabel->bbNum);
        }
        break;

        case IF_T2_J3:
        {
            methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
            printf("%s", methodName);
        }
        break;

        default:
            printf("unexpected format %s", emitIfName(id->idInsFmt()));
            assert(!"unexpectedFormat");
            break;
    }

    if (id->idDebugOnlyInfo()->idVarRefOffs)
    {
        printf("\t// ");
        emitDispFrameRef(id->idAddr()->iiaLclVar.lvaVarNum(), id->idAddr()->iiaLclVar.lvaOffset(),
                         id->idDebugOnlyInfo()->idVarRefOffs, asmfm);
    }

    printf("\n");
}

/*****************************************************************************
 *
 *  Handles printing of LARGEJMP pseudo-instruction.
 */

void emitter::emitDispLargeJmp(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* code, size_t sz, insGroup* ig)
{
    // Note: don't touch the actual instrDesc. If we accidentally messed it up, it would create a very
    // difficult to find bug.

    inlineInstrDesc<instrDescJmp> idJmp;
    instrDescJmp*                 pidJmp = idJmp.id();

    pidJmp->idIns(emitJumpKindToIns(emitReverseJumpKind(emitInsToJumpKind(id->idIns())))); // reverse the
                                                                                           // conditional
                                                                                           // instruction
    pidJmp->idInsFmt(IF_T1_K);
    pidJmp->idInsSize(emitInsSize(IF_T1_K));
    pidJmp->idjShort = 1;
    pidJmp->idAddr()->iiaSetInstrCount(1);
    pidJmp->idDebugOnlyInfo(id->idDebugOnlyInfo()); // share the idDebugOnlyInfo() field

    size_t bcondSizeOrZero = (code == NULL) ? 0 : 2; // branch is 2 bytes
    emitDispInsHelp(pidJmp, false, doffs, asmfm, offset, code, bcondSizeOrZero,
                    NULL /* force display of pc-relative branch */);

    code += bcondSizeOrZero;
    offset += 2;

    // Next, display the unconditional branch

    // Reset the local instrDesc
    memset(pidJmp, 0, sizeof(instrDescJmp));

    pidJmp->idIns(INS_b);
    pidJmp->idInsFmt(IF_T2_J2);
    pidJmp->idInsSize(emitInsSize(IF_T2_J2));
    pidJmp->idjShort = 0;
    if (id->idIsBound())
    {
        pidJmp->idSetIsBound();
        pidJmp->idAddr()->iiaIGlabel = id->idAddr()->iiaIGlabel;
    }
    else
    {
        pidJmp->idAddr()->iiaBBlabel = id->idAddr()->iiaBBlabel;
    }
    pidJmp->idDebugOnlyInfo(id->idDebugOnlyInfo()); // share the idDebugOnlyInfo() field

    size_t brSizeOrZero = (code == NULL) ? 0 : 4; // unconditional branch is 4 bytes
    emitDispInsHelp(pidJmp, isNew, doffs, asmfm, offset, code, brSizeOrZero, ig);
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
//   ig - The instruction group containing the instruction.
//
void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* code, size_t sz, insGroup* ig)
{
    insFormat fmt = id->idInsFmt();

    /* Special-case IF_LARGEJMP */

    if ((fmt == IF_LARGEJMP) && id->idIsBound())
    {
        // This is a pseudo-instruction format representing a large conditional branch. See the comment
        // in emitter::emitOutputLJ() for the full description.
        //
        // For this pseudo-instruction, we will actually generate:
        //
        //      b<!cond> L_not  // 2 bytes. Note that we reverse the condition.
        //      b L_target      // 4 bytes
        //   L_not:
        //
        // These instructions don't exist in the actual instruction stream, so we need to fake them
        // up to display them.
        emitDispLargeJmp(id, isNew, doffs, asmfm, offset, code, sz, ig);
    }
    else
    {
        emitDispInsHelp(id, isNew, doffs, asmfm, offset, code, sz, ig);
    }
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
#ifdef DEBUG
    printf("[");

    if (varx < 0)
        printf("TEMP_%02u", -varx);
    else
        emitComp->gtDispLclVar(+varx, false);

    if (disp < 0)
        printf("-0x%02x", -disp);
    else if (disp > 0)
        printf("+0x%02x", +disp);

    printf("]");

    if ((varx >= 0) && emitComp->opts.varNames && (((IL_OFFSET)offs) != BAD_IL_OFFSET))
    {
        const char* varName = emitComp->compLocalVarName(varx, offs);

        if (varName)
        {
            printf("'%s", varName);

            if (disp < 0)
                printf("-%d", -disp);
            else if (disp > 0)
                printf("+%d", +disp);

            printf("'");
        }
    }
#endif
}

void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    // Handle unaligned floating point loads/stores
    if ((indir->gtFlags & GTF_IND_UNALIGNED))
    {
        if (indir->OperGet() == GT_STOREIND)
        {
            var_types type = indir->AsStoreInd()->Data()->TypeGet();
            if (type == TYP_FLOAT)
            {
                regNumber tmpReg = indir->GetSingleTempReg();
                emitIns_Mov(INS_vmov_f2i, EA_4BYTE, tmpReg, dataReg, /* canSkip */ false);
                emitInsLoadStoreOp(INS_str, EA_4BYTE, tmpReg, indir, 0);
                return;
            }
            else if (type == TYP_DOUBLE)
            {
                regNumber tmpReg1 = indir->ExtractTempReg();
                regNumber tmpReg2 = indir->GetSingleTempReg();
                emitIns_R_R_R(INS_vmov_d2i, EA_8BYTE, tmpReg1, tmpReg2, dataReg);
                emitInsLoadStoreOp(INS_str, EA_4BYTE, tmpReg1, indir, 0);
                emitInsLoadStoreOp(INS_str, EA_4BYTE, tmpReg2, indir, 4);
                return;
            }
        }
        else if (indir->OperGet() == GT_IND)
        {
            var_types type = indir->TypeGet();
            if (type == TYP_FLOAT)
            {
                regNumber tmpReg = indir->GetSingleTempReg();
                emitInsLoadStoreOp(INS_ldr, EA_4BYTE, tmpReg, indir, 0);
                emitIns_Mov(INS_vmov_i2f, EA_4BYTE, dataReg, tmpReg, /* canSkip */ false);
                return;
            }
            else if (type == TYP_DOUBLE)
            {
                regNumber tmpReg1 = indir->ExtractTempReg();
                regNumber tmpReg2 = indir->GetSingleTempReg();
                emitInsLoadStoreOp(INS_ldr, EA_4BYTE, tmpReg1, indir, 0);
                emitInsLoadStoreOp(INS_ldr, EA_4BYTE, tmpReg2, indir, 4);
                emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, dataReg, tmpReg1, tmpReg2);
                return;
            }
        }
    }

    // Proceed with ordinary loads/stores
    emitInsLoadStoreOp(ins, attr, dataReg, indir, 0);
}

void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir, int offset)
{
    GenTree* addr = indir->Addr();

    if (addr->isContained())
    {
        assert(addr->OperIs(GT_LCL_ADDR, GT_LEA));

        DWORD lsl = 0;

        if (addr->OperGet() == GT_LEA)
        {
            offset += addr->AsAddrMode()->Offset();
            if (addr->AsAddrMode()->gtScale > 0)
            {
                assert(isPow2(addr->AsAddrMode()->gtScale));
                BitScanForward(&lsl, addr->AsAddrMode()->gtScale);
            }
        }

        GenTree* memBase = indir->Base();

        if (indir->HasIndex())
        {
            assert(addr->OperGet() == GT_LEA);

            GenTree* index = indir->Index();

            if (offset != 0)
            {
                regNumber tmpReg = indir->GetSingleTempReg();

                // If the LEA produces a GCREF or BYREF, we need to be careful to mark any temp register
                // computed with the base register as a BYREF.
                GenTreeAddrMode* lea                    = addr->AsAddrMode();
                emitAttr         leaAttr                = emitTypeSize(lea);
                emitAttr         leaBasePartialAddrAttr = EA_IS_GCREF_OR_BYREF(leaAttr) ? EA_BYREF : EA_PTRSIZE;

                if (emitIns_valid_imm_for_add(offset, INS_FLAGS_DONT_CARE))
                {
                    if (lsl > 0)
                    {
                        // Generate code to set tmpReg = base + index*scale
                        emitIns_R_R_R_I(INS_add, leaBasePartialAddrAttr, tmpReg, memBase->GetRegNum(),
                                        index->GetRegNum(), lsl, INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
                    }
                    else // no scale
                    {
                        // Generate code to set tmpReg = base + index
                        emitIns_R_R_R(INS_add, leaBasePartialAddrAttr, tmpReg, memBase->GetRegNum(),
                                      index->GetRegNum());
                    }

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));

                    // Then load/store dataReg from/to [tmpReg + offset]
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, offset);
                }
                else // large offset
                {
                    // First load/store tmpReg with the large offset constant
                    codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);
                    // Then add the base register
                    //      rd = rd + base
                    emitIns_R_R_R(INS_add, leaBasePartialAddrAttr, tmpReg, tmpReg, memBase->GetRegNum());

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));
                    noway_assert(tmpReg != index->GetRegNum());

                    // Then load/store dataReg from/to [tmpReg + index*scale]
                    emitIns_R_R_R_I(ins, attr, dataReg, tmpReg, index->GetRegNum(), lsl, INS_FLAGS_DONT_CARE,
                                    INS_OPTS_LSL);
                }
            }
            else // (offset == 0)
            {
                if (lsl > 0)
                {
                    // Then load/store dataReg from/to [memBase + index*scale]
                    emitIns_R_R_R_I(ins, attr, dataReg, memBase->GetRegNum(), index->GetRegNum(), lsl,
                                    INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
                }
                else // no scale
                {
                    // Then load/store dataReg from/to [memBase + index]
                    emitIns_R_R_R(ins, attr, dataReg, memBase->GetRegNum(), index->GetRegNum());
                }
            }
        }
        else // no Index
        {
            if (addr->OperIs(GT_LCL_ADDR))
            {
                GenTreeLclVarCommon* varNode = addr->AsLclVarCommon();
                unsigned             lclNum  = varNode->GetLclNum();
                unsigned             offset  = varNode->GetLclOffs();
                if (emitInsIsStore(ins))
                {
                    emitIns_S_R(ins, attr, dataReg, lclNum, offset);
                }
                else
                {
                    emitIns_R_S(ins, attr, dataReg, lclNum, offset);
                }
            }
            else if (emitIns_valid_imm_for_ldst_offset(offset, attr))
            {
                // Then load/store dataReg from/to [memBase + offset]
                emitIns_R_R_I(ins, attr, dataReg, memBase->GetRegNum(), offset);
            }
            else
            {
                // We require a tmpReg to hold the offset
                regNumber tmpReg = indir->GetSingleTempReg();

                // First load/store tmpReg with the large offset constant
                codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                // Then load/store dataReg from/to [memBase + tmpReg]
                emitIns_R_R_R(ins, attr, dataReg, memBase->GetRegNum(), tmpReg);
            }
        }
    }
    else
    {
#ifdef DEBUG
        if (addr->OperIs(GT_LCL_ADDR))
        {
            // If the local var is a gcref or byref, the local var better be untracked, because we have
            // no logic here to track local variable lifetime changes, like we do in the contained case
            // above. E.g., for a `str r0,[r1]` for byref `r1` to local `V01`, we won't store the local
            // `V01` and so the emitter can't update the GC lifetime for `V01` if this is a variable birth.
            LclVarDsc* varDsc = emitComp->lvaGetDesc(addr->AsLclVarCommon());
            assert(!varDsc->lvTracked);
        }
#endif // DEBUG

        if (offset != 0)
        {
            assert(emitIns_valid_imm_for_add(offset, INS_FLAGS_DONT_CARE));
            emitIns_R_R_I(ins, attr, dataReg, addr->GetRegNum(), offset);
        }
        else
        {
            emitIns_R_R(ins, attr, dataReg, addr->GetRegNum());
        }
    }
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
    regNumber result = REG_NA;

    // dst can only be a reg
    assert(!dst->isContained());

    // src can be immed or reg
    assert(!src->isContained() || src->isContainedIntOrIImmed());

    // find immed (if any) - it cannot be a dst
    GenTreeIntConCommon* intConst = nullptr;
    if (src->isContainedIntOrIImmed())
    {
        intConst = src->AsIntConCommon();
    }

    if (intConst)
    {
        emitIns_R_I(ins, attr, dst->GetRegNum(), (target_ssize_t)intConst->IconValue());
        return dst->GetRegNum();
    }
    else
    {
        emitIns_R_R(ins, attr, dst->GetRegNum(), src->GetRegNum());
        return dst->GetRegNum();
    }
}

regNumber emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2)
{
    // dst can only be a reg
    assert(!dst->isContained());

    // find immed (if any) - it cannot be a dst
    // Only one src can be an int.
    GenTreeIntConCommon* intConst  = nullptr;
    GenTree*             nonIntReg = nullptr;

    if (varTypeIsFloating(dst))
    {
        // src1 can only be a reg
        assert(!src1->isContained());
        // src2 can only be a reg
        assert(!src2->isContained());
    }
    else // not floating point
    {
        // src2 can be immed or reg
        assert(!src2->isContained() || src2->isContainedIntOrIImmed());

        // Check src2 first as we can always allow it to be a contained immediate
        if (src2->isContainedIntOrIImmed())
        {
            intConst  = src2->AsIntConCommon();
            nonIntReg = src1;
        }
        // Only for commutative operations do we check src1 and allow it to be a contained immediate
        else if (dst->OperIsCommutative())
        {
            // src1 can be immed or reg
            assert(!src1->isContained() || src1->isContainedIntOrIImmed());

            // Check src1 and allow it to be a contained immediate
            if (src1->isContainedIntOrIImmed())
            {
                assert(!src2->isContainedIntOrIImmed());
                intConst  = src1->AsIntConCommon();
                nonIntReg = src2;
            }
        }
        else
        {
            // src1 can only be a reg
            assert(!src1->isContained());
        }
    }

    insFlags flags         = INS_FLAGS_DONT_CARE;
    bool     isMulOverflow = false;
    if (dst->gtOverflowEx())
    {
        if ((ins == INS_add) || (ins == INS_adc) || (ins == INS_sub) || (ins == INS_sbc))
        {
            flags = INS_FLAGS_SET;
        }
        else if (ins == INS_mul)
        {
            isMulOverflow = true;
            assert(intConst == nullptr); // overflow format doesn't support an int constant operand
        }
        else
        {
            assert(!"Invalid ins for overflow check");
        }
    }

    if (dst->gtSetFlags())
    {
        assert((ins == INS_add) || (ins == INS_adc) || (ins == INS_sub) || (ins == INS_sbc) || (ins == INS_and) ||
               (ins == INS_orr) || (ins == INS_eor) || (ins == INS_orn) || (ins == INS_bic));
        flags = INS_FLAGS_SET;
    }

    if (intConst != nullptr)
    {
        emitIns_R_R_I(ins, attr, dst->GetRegNum(), nonIntReg->GetRegNum(), (target_ssize_t)intConst->IconValue(),
                      flags);
    }
    else
    {
        if (isMulOverflow)
        {
            regNumber extraReg = dst->GetSingleTempReg();
            assert(extraReg != dst->GetRegNum());

            if ((dst->gtFlags & GTF_UNSIGNED) != 0)
            {
                // Compute 8 byte result from 4 byte by 4 byte multiplication.
                emitIns_R_R_R_R(INS_umull, EA_4BYTE, dst->GetRegNum(), extraReg, src1->GetRegNum(), src2->GetRegNum());

                // Overflow exists if the result's high word is non-zero.
                emitIns_R_I(INS_cmp, attr, extraReg, 0);
            }
            else
            {
                // Compute 8 byte result from 4 byte by 4 byte multiplication.
                emitIns_R_R_R_R(INS_smull, EA_4BYTE, dst->GetRegNum(), extraReg, src1->GetRegNum(), src2->GetRegNum());

                // Overflow exists if the result's high word is not merely a sign bit.
                emitIns_R_R_I(INS_cmp, attr, extraReg, dst->GetRegNum(), 31, INS_FLAGS_DONT_CARE, INS_OPTS_ASR);
            }
        }
        else
        {
            // We can just do the arithmetic, setting the flags if needed.
            emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum(), flags);
        }
    }

    if (dst->gtOverflowEx())
    {
        assert(!varTypeIsFloating(dst));

        emitJumpKind jumpKind;

        if (dst->OperGet() == GT_MUL)
        {
            jumpKind = EJ_ne;
        }
        else
        {
            bool isUnsignedOverflow = ((dst->gtFlags & GTF_UNSIGNED) != 0);
            jumpKind                = isUnsignedOverflow ? EJ_lo : EJ_vs;
            if (jumpKind == EJ_lo)
            {
                if ((dst->OperGet() != GT_SUB) && (dst->OperGet() != GT_SUB_HI))
                {
                    jumpKind = EJ_hs;
                }
            }
        }

        // Jump to the block which will throw the exception.
        codeGen->genJumpToThrowHlpBlk(jumpKind, SCK_OVERFLOW);
    }

    return dst->GetRegNum();
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
//
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    insExecutionCharacteristics result;

    instruction ins    = id->idIns();
    insFormat   insFmt = id->idInsFmt();

    // ToDo: Calculate actual throughput and latency values
    //
    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
    result.insLatency    = PERFSCORE_LATENCY_1C;

    return result;
}

#endif // defined(DEBUG) || defined(LATE_DISASM)

#endif // defined(TARGET_ARM)
