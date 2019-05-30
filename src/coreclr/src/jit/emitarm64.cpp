// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitArm64.cpp                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(_TARGET_ARM64_)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

/* static */ bool emitter::strictArmAsm = true;

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

size_t emitter::emitSizeOfInsDsc(instrDesc* id)
{
    if (emitIsScnsInsDsc(id))
        return SMALL_IDSC_SIZE;

    assert((unsigned)id->idInsFmt() < emitFmtCount);

    ID_OPS idOp      = (ID_OPS)emitFmtToOps[id->idInsFmt()];
    bool   isCallIns = (id->idIns() == INS_bl) || (id->idIns() == INS_blr) || (id->idIns() == INS_b_tail) ||
                     (id->idIns() == INS_br_tail);
    bool maybeCallIns = (id->idIns() == INS_b) || (id->idIns() == INS_br);

    switch (idOp)
    {
        case ID_OP_NONE:
            break;

        case ID_OP_JMP:
            return sizeof(instrDescJmp);

        case ID_OP_CALL:
            assert(isCallIns || maybeCallIns);
            if (id->idIsLargeCall())
            {
                /* Must be a "fat" call descriptor */
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
        instruction ins;
        emitAttr    elemsize;
        emitAttr    datasize;
        emitAttr    dstsize;
        emitAttr    srcsize;
        ssize_t     imm;
        unsigned    immShift;
        ssize_t     index;
        ssize_t     index2;

        case IF_BI_0A: // BI_0A   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
            break;

        case IF_BI_0B: // BI_0B   ......iiiiiiiiii iiiiiiiiiiii....               simm19:00
            break;

        case IF_LARGEJMP:
        case IF_LARGEADR:
        case IF_LARGELDC:
            break;

        case IF_BI_0C: // BI_0C   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
            break;

        case IF_BI_1A: // BI_1A   ......iiiiiiiiii iiiiiiiiiiittttt      Rt       simm19:00
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            break;

        case IF_BI_1B: // BI_1B   B.......bbbbbiii iiiiiiiiiiittttt      Rt imm6, simm14:00
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_BR_1A: // BR_1A   ................ ......nnnnn.....         Rn
            assert(isGeneralRegister(id->idReg1()));
            break;

        case IF_BR_1B: // BR_1B   ................ ......nnnnn.....         Rn
            assert(isGeneralRegister(id->idReg3()));
            break;

        case IF_LS_1A: // LS_1A   .X......iiiiiiii iiiiiiiiiiittttt      Rt    PC imm(1MB)
            assert(isGeneralRegister(id->idReg1()) || isVectorRegister(id->idReg1()));
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_LS_2A:                                // LS_2A   .X.......X...... ......nnnnnttttt      Rt Rn
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2())); // SP
            assert(emitGetInsSC(id) == 0);
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_LS_2B: // LS_2B   .X.......Xiiiiii iiiiiinnnnnttttt      Rt Rn    imm(0-4095)
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2())); // SP
            assert(isValidUimm12(emitGetInsSC(id)));
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_LS_2C: // LS_2C   .X.......X.iiiii iiiiPPnnnnnttttt      Rt Rn    imm(-256..+255) no/pre/post inc
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2())); // SP
            assert(emitGetInsSC(id) >= -0x100);
            assert(emitGetInsSC(id) < 0x100);
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            break;

        case IF_LS_3A: // LS_3A   .X.......X.mmmmm oooS..nnnnnttttt      Rt Rn Rm ext(Rm) LSL {}
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2())); // SP
            if (id->idIsLclVar())
            {
                assert(isGeneralRegister(codeGen->rsGetRsvdReg()));
            }
            else
            {
                assert(isGeneralRegister(id->idReg3()));
            }
            assert(insOptsLSExtend(id->idInsOpt()));
            break;

        case IF_LS_3B: // LS_3B   X............... .aaaaannnnnttttt      Rt Ra Rn
            assert((isValidGeneralDatasize(id->idOpSize()) && isIntegerRegister(id->idReg1())) ||
                   (isValidVectorLSPDatasize(id->idOpSize()) && isVectorRegister(id->idReg1())));
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2()) || // ZR
                   isVectorRegister(id->idReg2()));
            assert(isIntegerRegister(id->idReg3())); // SP
            assert(emitGetInsSC(id) == 0);
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_LS_3C: // LS_3C   X.........iiiiii iaaaaannnnnttttt      Rt Ra Rn imm(im7,sh)
            assert((isValidGeneralDatasize(id->idOpSize()) && isIntegerRegister(id->idReg1())) ||
                   (isValidVectorLSPDatasize(id->idOpSize()) && isVectorRegister(id->idReg1())));
            assert(isIntegerRegister(id->idReg1()) || // ZR
                   isVectorRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2()) || // ZR
                   isVectorRegister(id->idReg2()));
            assert(isIntegerRegister(id->idReg3())); // SP
            assert(emitGetInsSC(id) >= -0x40);
            assert(emitGetInsSC(id) < 0x40);
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            break;

        case IF_LS_3D: // LS_3D   .X.......X.mmmmm ......nnnnnttttt      Wm Rt Rn
            assert(isIntegerRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2()));
            assert(isIntegerRegister(id->idReg3()));
            assert(emitGetInsSC(id) == 0);
            assert(!id->idIsLclVar());
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_LS_3E: // LS_3E   .X.........mmmmm ......nnnnnttttt      Rm Rt Rn ARMv8.1 LSE Atomics
            assert(isIntegerRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2()));
            assert(isIntegerRegister(id->idReg3()));
            assert(emitGetInsSC(id) == 0);
            assert(!id->idIsLclVar());
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_DI_1A: // DI_1A   X.......shiiiiii iiiiiinnnnn.....         Rn    imm(i12,sh)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidUimm12(emitGetInsSC(id)));
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL12(id->idInsOpt()));
            break;

        case IF_DI_1B: // DI_1B   X........hwiiiii iiiiiiiiiiiddddd      Rd       imm(i16,hw)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidImmHWVal(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DI_1C: // DI_1C   X........Nrrrrrr ssssssnnnnn.....         Rn    imm(N,r,s)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidImmNRS(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DI_1D: // DI_1D   X........Nrrrrrr ssssss.....ddddd      Rd       imm(N,r,s)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isValidImmNRS(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DI_1E: // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd       simm21
            assert(isGeneralRegister(id->idReg1()));
            break;

        case IF_DI_1F: // DI_1F   X..........iiiii cccc..nnnnn.nzcv      Rn imm5  nzcv cond
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidImmCondFlagsImm5(emitGetInsSC(id)));
            break;

        case IF_DI_2A: // DI_2A   X.......shiiiiii iiiiiinnnnnddddd      Rd Rn    imm(i12,sh)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isIntegerRegister(id->idReg2())); // SP
            assert(isValidUimm12(emitGetInsSC(id)));
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL12(id->idInsOpt()));
            break;

        case IF_DI_2B: // DI_2B   X.........Xnnnnn ssssssnnnnnddddd      Rd Rn    imm(0-63)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DI_2C: // DI_2C   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imm(N,r,s)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmNRS(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DI_2D: // DI_2D   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imr, imms   (N,r,s)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmNRS(emitGetInsSC(id), id->idOpSize()));
            break;

        case IF_DR_1D: // DR_1D   X............... cccc.......ddddd      Rd       cond
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isValidImmCond(emitGetInsSC(id)));
            break;

        case IF_DR_2A: // DR_2A   X..........mmmmm ......nnnnn.....         Rn Rm
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_DR_2B: // DR_2B   X.......sh.mmmmm ssssssnnnnn.....         Rn Rm {LSL,LSR,ASR,ROR} imm(0-63)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // ZR
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            if (!insOptsNone(id->idInsOpt()))
            {
                if (id->idIns() == INS_tst) // tst allows ROR, cmp/cmn don't
                {
                    assert(insOptsAnyShift(id->idInsOpt()));
                }
                else
                {
                    assert(insOptsAluShift(id->idInsOpt()));
                }
            }
            assert(insOptsNone(id->idInsOpt()) || (emitGetInsSC(id) > 0));
            break;

        case IF_DR_2C: // DR_2C   X..........mmmmm ooosssnnnnn.....         Rn Rm ext(Rm) LSL imm(0-4)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isGeneralRegister(id->idReg2()));
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL(id->idInsOpt()) || insOptsAnyExtend(id->idInsOpt()));
            assert(emitGetInsSC(id) >= 0);
            assert(emitGetInsSC(id) <= 4);
            if (insOptsLSL(id->idInsOpt()))
            {
                assert(emitGetInsSC(id) > 0);
            }
            break;

        case IF_DR_2D: // DR_2D   X..........nnnnn cccc..nnnnnmmmmm      Rd Rn    cond
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmCond(emitGetInsSC(id)));
            break;

        case IF_DR_2E: // DR_2E   X..........mmmmm ...........ddddd      Rd    Rm
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isIntegerRegister(id->idReg2())); // ZR
            break;

        case IF_DR_2F: // DR_2F   X.......sh.mmmmm ssssss.....ddddd      Rd    Rm {LSL,LSR,ASR} imm(0-63)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            assert(insOptsNone(id->idInsOpt()) || insOptsAluShift(id->idInsOpt()));
            assert(insOptsNone(id->idInsOpt()) || (emitGetInsSC(id) > 0));
            break;

        case IF_DR_2G: // DR_2G   X............... ......nnnnnddddd      Rd    Rm
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isIntegerRegister(id->idReg2())); // SP
            break;

        case IF_DR_2H: // DR_2H   X........X...... ......nnnnnddddd      Rd Rn
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_DR_2I: // DR_2I   X..........mmmmm cccc..nnnnn.nzcv      Rn Rm    nzcv cond
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isValidImmCondFlags(emitGetInsSC(id)));
            break;

        case IF_DR_2J: // DR_2J   ................ ......nnnnnddddd      Sd Sn    (sha1h)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DR_3A: // DR_3A   X..........mmmmm ......nnnnnmmmmm      Rd Rn Rm
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isIntegerRegister(id->idReg2())); // SP
            if (id->idIsLclVar())
            {
                assert(isGeneralRegister(codeGen->rsGetRsvdReg()));
            }
            else
            {
                assert(isGeneralRegister(id->idReg3()));
            }
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_DR_3B: // DR_3B   X.......sh.mmmmm ssssssnnnnnddddd      Rd Rn Rm {LSL,LSR,ASR,ROR} imm(0-63)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            assert(insOptsNone(id->idInsOpt()) || insOptsAnyShift(id->idInsOpt()));
            assert(insOptsNone(id->idInsOpt()) || (emitGetInsSC(id) > 0));
            break;

        case IF_DR_3C: // DR_3C   X..........mmmmm ooosssnnnnnddddd      Rd Rn Rm ext(Rm) LSL imm(0-4)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isIntegerRegister(id->idReg1())); // SP
            assert(isIntegerRegister(id->idReg2())); // SP
            assert(isGeneralRegister(id->idReg3()));
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL(id->idInsOpt()) || insOptsAnyExtend(id->idInsOpt()));
            assert(emitGetInsSC(id) >= 0);
            assert(emitGetInsSC(id) <= 4);
            if (insOptsLSL(id->idInsOpt()))
            {
                assert((emitGetInsSC(id) > 0) ||
                       (id->idReg2() == REG_ZR)); // REG_ZR encodes SP and we allow a shift of zero
            }
            break;

        case IF_DR_3D: // DR_3D   X..........mmmmm cccc..nnnnnmmmmm      Rd Rn Rm cond
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isValidImmCond(emitGetInsSC(id)));
            break;

        case IF_DR_3E: // DR_3E   X........X.mmmmm ssssssnnnnnddddd      Rd Rn Rm imm(0-63)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isValidImmShift(emitGetInsSC(id), id->idOpSize()));
            assert(insOptsNone(id->idInsOpt()));
            break;

        case IF_DR_4A: // DR_4A   X..........mmmmm .aaaaannnnnddddd      Rd Rn Rm Ra
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isGeneralRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            assert(isGeneralRegister(id->idReg3()));
            assert(isGeneralRegister(id->idReg4()));
            break;

        case IF_DV_1A: // DV_1A   .........X.iiiii iii........ddddd      Vd imm8    (fmov - immediate scalar)
            assert(insOptsNone(id->idInsOpt()));
            elemsize = id->idOpSize();
            assert(isValidVectorElemsizeFloat(elemsize));
            assert(isVectorRegister(id->idReg1()));
            assert(isValidUimm8(emitGetInsSC(id)));
            break;

        case IF_DV_1B: // DV_1B   .QX..........iii cmod..iiiiiddddd      Vd imm8    (immediate vector)
            ins      = id->idIns();
            imm      = emitGetInsSC(id) & 0x0ff;
            immShift = (emitGetInsSC(id) & 0x700) >> 8;
            assert(immShift >= 0);
            datasize = id->idOpSize();
            assert(isValidVectorDatasize(datasize));
            assert(isValidArrangement(datasize, id->idInsOpt()));
            elemsize = optGetElemsize(id->idInsOpt());
            if (ins == INS_fmov)
            {
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(id->idInsOpt() != INS_OPTS_1D); // Reserved encoding
                assert(immShift == 0);
            }
            else
            {
                assert(isValidVectorElemsize(elemsize));
                assert((immShift != 4) && (immShift != 7)); // always invalid values
                if (ins != INS_movi)                        // INS_mvni, INS_orr, INS_bic
                {
                    assert((elemsize != EA_1BYTE) && (elemsize != EA_8BYTE)); // only H or S
                    if (elemsize == EA_2BYTE)
                    {
                        assert(immShift < 2);
                    }
                    else // (elemsize == EA_4BYTE)
                    {
                        if (ins != INS_mvni)
                        {
                            assert(immShift < 4);
                        }
                    }
                }
            }
            assert(isVectorRegister(id->idReg1()));
            assert(isValidUimm8(imm));
            break;

        case IF_DV_1C: // DV_1C   .........X...... ......nnnnn.....      Vn #0.0    (fcmp - with zero)
            assert(insOptsNone(id->idInsOpt()));
            elemsize = id->idOpSize();
            assert(isValidVectorElemsizeFloat(elemsize));
            assert(isVectorRegister(id->idReg1()));
            break;

        case IF_DV_2A: // DV_2A   .Q.......X...... ......nnnnnddddd      Vd Vn      (fabs, fcvt - vector)
        case IF_DV_2M: // DV_2M   .Q......XX...... ......nnnnnddddd      Vd Vn      (abs, neg   - vector)
        case IF_DV_2P: // DV_2P   ................ ......nnnnnddddd      Vd Vn      (aes*, sha1su1)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2N: // DV_2N   .........iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - scalar)
            assert(id->idOpSize() == EA_8BYTE);
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isValidImmShift(emitGetInsSC(id), EA_8BYTE));
            break;

        case IF_DV_2O: // DV_2O   .Q.......iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - vector)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            elemsize = optGetElemsize(id->idInsOpt());
            assert(isValidImmShift(emitGetInsSC(id), elemsize));
            break;

        case IF_DV_2B: // DV_2B   .Q.........iiiii ......nnnnnddddd      Rd Vn[]  (umov/smov    - to general)
            elemsize = id->idOpSize();
            index    = emitGetInsSC(id);
            assert(insOptsNone(id->idInsOpt()));
            assert(isValidVectorIndex(EA_16BYTE, elemsize, index));
            assert(isValidVectorElemsize(elemsize));
            assert(isGeneralRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2C: // DV_2C   .Q.........iiiii ......nnnnnddddd      Vd Rn    (dup/ins - vector from general)
            if (id->idIns() == INS_dup)
            {
                datasize = id->idOpSize();
                assert(isValidVectorDatasize(datasize));
                assert(isValidArrangement(datasize, id->idInsOpt()));
                elemsize = optGetElemsize(id->idInsOpt());
            }
            else // INS_ins
            {
                datasize = EA_16BYTE;
                elemsize = id->idOpSize();
                assert(isValidVectorElemsize(elemsize));
            }
            assert(isVectorRegister(id->idReg1()));
            assert(isGeneralRegisterOrZR(id->idReg2()));
            break;

        case IF_DV_2D: // DV_2D   .Q.........iiiii ......nnnnnddddd      Vd Vn[]  (dup - vector)
            datasize = id->idOpSize();
            assert(isValidVectorDatasize(datasize));
            assert(isValidArrangement(datasize, id->idInsOpt()));
            elemsize = optGetElemsize(id->idInsOpt());
            index    = emitGetInsSC(id);
            assert(isValidVectorIndex(datasize, elemsize, index));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2E: // DV_2E   ...........iiiii ......nnnnnddddd      Vd Vn[]  (dup - scalar)
            elemsize = id->idOpSize();
            index    = emitGetInsSC(id);
            assert(isValidVectorIndex(EA_16BYTE, elemsize, index));
            assert(isValidVectorElemsize(elemsize));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2F: // DV_2F   ...........iiiii .jjjj.nnnnnddddd      Vd[] Vn[] (ins - element)
            imm      = emitGetInsSC(id);
            index    = (imm >> 4) & 0xf;
            index2   = imm & 0xf;
            elemsize = id->idOpSize();
            assert(isValidVectorElemsize(elemsize));
            assert(isValidVectorIndex(EA_16BYTE, elemsize, index));
            assert(isValidVectorIndex(EA_16BYTE, elemsize, index2));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2L: // DV_2L   ........XX...... ......nnnnnddddd      Vd Vn      (abs, neg - scalar)
            assert(id->idOpSize() == EA_8BYTE); // only type D is supported
            __fallthrough;

        case IF_DV_2G: // DV_2G   .........X...... ......nnnnnddddd      Vd Vn      (fmov, fcvtXX - register)
        case IF_DV_2K: // DV_2K   .........X.mmmmm ......nnnnn.....      Vn Vm      (fcmp)
            assert(insOptsNone(id->idInsOpt()));
            assert(isValidVectorElemsizeFloat(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2H: // DV_2H   X........X...... ......nnnnnddddd      Rd Vn      (fmov/fcvtXX - to general)
            assert(insOptsConvertFloatToInt(id->idInsOpt()));
            dstsize = optGetDstsize(id->idInsOpt());
            srcsize = optGetSrcsize(id->idInsOpt());
            assert(isValidGeneralDatasize(dstsize));
            assert(isValidVectorElemsizeFloat(srcsize));
            assert(dstsize == id->idOpSize());
            assert(isGeneralRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_2I: // DV_2I   X........X...... ......nnnnnddddd      Vd Rn      (fmov/Xcvtf - from general)
            assert(insOptsConvertIntToFloat(id->idInsOpt()));
            dstsize = optGetDstsize(id->idInsOpt());
            srcsize = optGetSrcsize(id->idInsOpt());
            assert(isValidGeneralDatasize(srcsize));
            assert(isValidVectorElemsizeFloat(dstsize));
            assert(dstsize == id->idOpSize());
            assert(isVectorRegister(id->idReg1()));
            assert(isGeneralRegister(id->idReg2()));
            break;

        case IF_DV_2J: // DV_2J   ........SS.....D D.....nnnnnddddd      Vd Vn      (fcvt)
            assert(insOptsConvertFloatToFloat(id->idInsOpt()));
            dstsize = optGetDstsize(id->idInsOpt());
            srcsize = optGetSrcsize(id->idInsOpt());
            assert(isValidVectorFcvtsize(srcsize));
            assert(isValidVectorFcvtsize(dstsize));
            assert(dstsize == id->idOpSize());
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            break;

        case IF_DV_3A: // DV_3A   .Q......XX.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            elemsize = optGetElemsize(id->idInsOpt());
            ins      = id->idIns();
            if (ins == INS_mul)
            {
                assert(elemsize != EA_8BYTE); // can't use 2D or 1D
            }
            else if (ins == INS_pmul)
            {
                assert(elemsize == EA_1BYTE); // only supports 8B or 16B
            }
            break;

        case IF_DV_3AI: // DV_3AI  .Q......XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            elemsize = optGetElemsize(id->idInsOpt());
            assert(isValidVectorIndex(EA_16BYTE, elemsize, emitGetInsSC(id)));
            // Only has encodings for H or S elemsize
            assert((elemsize == EA_2BYTE) || (elemsize == EA_4BYTE));
            break;

        case IF_DV_3B: // DV_3B   .Q.......X.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_DV_3BI: // DV_3BI  .Q.......XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            elemsize = optGetElemsize(id->idInsOpt());
            assert(isValidVectorIndex(id->idOpSize(), elemsize, emitGetInsSC(id)));
            break;

        case IF_DV_3C: // DV_3C   .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_DV_3D: // DV_3D   .........X.mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
            assert(isValidScalarDatasize(id->idOpSize()));
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_DV_3DI: // DV_3DI  .........XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by elem)
            assert(isValidScalarDatasize(id->idOpSize()));
            assert(insOptsNone(id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            elemsize = id->idOpSize();
            assert(isValidVectorIndex(EA_16BYTE, elemsize, emitGetInsSC(id)));
            break;

        case IF_DV_3E: // DV_3E   ...........mmmmm ......nnnnnddddd      Vd Vn Vm  (scalar)
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idOpSize() == EA_8BYTE);
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_DV_3F: // DV_3F   ...........mmmmm ......nnnnnddddd      Vd Vn Vm
            assert(isValidVectorDatasize(id->idOpSize()));
            assert(isValidArrangement(id->idOpSize(), id->idInsOpt()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            break;

        case IF_DV_4A: // DR_4A   .........X.mmmmm .aaaaannnnnddddd      Rd Rn Rm Ra (scalar)
            assert(isValidGeneralDatasize(id->idOpSize()));
            assert(isVectorRegister(id->idReg1()));
            assert(isVectorRegister(id->idReg2()));
            assert(isVectorRegister(id->idReg3()));
            assert(isVectorRegister(id->idReg4()));
            break;

        case IF_SN_0A: // SN_0A   ................ ................
        case IF_SI_0A: // SI_0A   ...........iiiii iiiiiiiiiii.....               imm16
        case IF_SI_0B: // SI_0B   ................ ....bbbb........               imm4 - barrier
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

        // These are the formats with "destination" registers:

        case IF_DI_1B: // DI_1B   X........hwiiiii iiiiiiiiiiiddddd      Rd       imm(i16,hw)
        case IF_DI_1D: // DI_1D   X........Nrrrrrr ssssss.....ddddd      Rd       imm(N,r,s)
        case IF_DI_1E: // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd       simm21

        case IF_DI_2A: // DI_2A   X.......shiiiiii iiiiiinnnnnddddd      Rd Rn    imm(i12,sh)
        case IF_DI_2B: // DI_2B   X.........Xnnnnn ssssssnnnnnddddd      Rd Rn    imm(0-63)
        case IF_DI_2C: // DI_2C   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imm(N,r,s)
        case IF_DI_2D: // DI_2D   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imr, imms   (N,r,s)

        case IF_DR_1D: // DR_1D   X............... cccc.......ddddd      Rd       cond

        case IF_DR_2D: // DR_2D   X..........nnnnn cccc..nnnnnddddd      Rd Rn    cond
        case IF_DR_2E: // DR_2E   X..........mmmmm ...........ddddd      Rd    Rm
        case IF_DR_2F: // DR_2F   X.......sh.mmmmm ssssss.....ddddd      Rd    Rm {LSL,LSR,ASR} imm(0-63)
        case IF_DR_2G: // DR_2G   X............... ......nnnnnddddd      Rd Rn
        case IF_DR_2H: // DR_2H   X........X...... ......nnnnnddddd      Rd Rn

        case IF_DR_3A: // DR_3A   X..........mmmmm ......nnnnnddddd      Rd Rn Rm
        case IF_DR_3B: // DR_3B   X.......sh.mmmmm ssssssnnnnnddddd      Rd Rn Rm {LSL,LSR,ASR} imm(0-63)
        case IF_DR_3C: // DR_3C   X..........mmmmm xxxsssnnnnnddddd      Rd Rn Rm ext(Rm) LSL imm(0-4)
        case IF_DR_3D: // DR_3D   X..........mmmmm cccc..nnnnnddddd      Rd Rn Rm cond
        case IF_DR_3E: // DR_3E   X........X.mmmmm ssssssnnnnnddddd      Rd Rn Rm imm(0-63)
        case IF_DV_3F: // DV_3F   ...........mmmmm ......nnnnnddddd      Vd Vn Vm (vector) - Vd both source and dest

        case IF_DR_4A: // DR_4A   X..........mmmmm .aaaaannnnnddddd      Rd Rn Rm Ra

        case IF_DV_2B: // DV_2B   .Q.........iiiii ......nnnnnddddd      Rd Vn[]    (umov - to general)
        case IF_DV_2H: // DV_2H   X........X...... ......nnnnnddddd      Rd Vn      (fmov - to general)

            return true;

        case IF_DV_2C: // DV_2C   .Q.........iiiii ......nnnnnddddd      Vd Rn      (dup/ins - vector from general)
        case IF_DV_2D: // DV_2D   .Q.........iiiii ......nnnnnddddd      Vd Vn[]    (dup - vector)
        case IF_DV_2E: // DV_2E   ...........iiiii ......nnnnnddddd      Vd Vn[]    (dup - scalar)
        case IF_DV_2F: // DV_2F   ...........iiiii .jjjj.nnnnnddddd      Vd[] Vn[]  (ins - element)
        case IF_DV_2G: // DV_2G   .........X...... ......nnnnnddddd      Vd Vn      (fmov, fcvtXX - register)
        case IF_DV_2I: // DV_2I   X........X...... ......nnnnnddddd      Vd Rn      (fmov - from general)
        case IF_DV_2J: // DV_2J   ........SS.....D D.....nnnnnddddd      Vd Vn      (fcvt)
        case IF_DV_2K: // DV_2K   .........X.mmmmm ......nnnnn.....      Vn Vm      (fcmp)
        case IF_DV_2L: // DV_2L   ........XX...... ......nnnnnddddd      Vd Vn      (abs, neg - scalar)
        case IF_DV_2M: // DV_2M   .Q......XX...... ......nnnnnddddd      Vd Vn      (abs, neg - vector)
        case IF_DV_2P: // DV_2P   ................ ......nnnnnddddd      Vd Vn      (aes*, sha1su1) - Vd both source and
                       // destination

        case IF_DV_3A:  // DV_3A   .Q......XX.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
        case IF_DV_3AI: // DV_3AI  .Q......XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector)
        case IF_DV_3B:  // DV_3B   .Q.......X.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
        case IF_DV_3BI: // DV_3BI  .Q.......XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
        case IF_DV_3C:  // DV_3C   .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
        case IF_DV_3D:  // DV_3D   .........X.mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
        case IF_DV_3DI: // DV_3DI  .........XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by elem)
        case IF_DV_3E:  // DV_3E   ...........mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
        case IF_DV_4A:  // DV_4A   .........X.mmmmm .aaaaannnnnddddd      Vd Va Vn Vm (scalar)
            // Tracked GC pointers cannot be placed into the SIMD registers.
            return false;

        // These are the load/store formats with "target" registers:

        case IF_LS_1A: // LS_1A   XX...V..iiiiiiii iiiiiiiiiiittttt      Rt    PC imm(1MB)
        case IF_LS_2A: // LS_2A   .X.......X...... ......nnnnnttttt      Rt Rn
        case IF_LS_2B: // LS_2B   .X.......Xiiiiii iiiiiinnnnnttttt      Rt Rn    imm(0-4095)
        case IF_LS_2C: // LS_2C   .X.......X.iiiii iiiiP.nnnnnttttt      Rt Rn    imm(-256..+255) pre/post inc
        case IF_LS_3A: // LS_3A   .X.......X.mmmmm xxxS..nnnnnttttt      Rt Rn Rm ext(Rm) LSL {}
        case IF_LS_3B: // LS_3B   X............... .aaaaannnnnttttt      Rt Ra Rn
        case IF_LS_3C: // LS_3C   X.........iiiiii iaaaaannnnnttttt      Rt Ra Rn imm(im7,sh)
        case IF_LS_3D: // LS_3D   .X.......X.mmmmm ......nnnnnttttt      Wm Rt Rn

            // For the Store instructions the "target" register is actually a "source" value

            if (emitInsIsStore(ins))
            {
                return false;
            }
            else
            {
                assert(emitInsIsLoad(ins));
                return true;
            }

        case IF_LS_3E: // LS_3E   .X.........mmmmm ......nnnnnttttt      Rm Rt Rn ARMv8.1 LSE Atomics
            // ARMv8.1 Atomics
            assert(emitInsIsStore(ins));
            assert(emitInsIsLoad(ins));
            return true;

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
        case INS_stur:
        case INS_sturb:
        case INS_sturh:
            return true;
        default:
            return false;
    }
}

bool emitter::emitInsWritesToLclVarStackLocPair(instrDesc* id)
{
    if (!id->idIsLclVar())
        return false;

    instruction ins = id->idIns();

    // This list is related to the list of instructions used to store local vars in emitIns_S_S_R_R().
    // We don't accept writing to float local vars.

    switch (ins)
    {
        case INS_stnp:
        case INS_stp:
            return true;
        default:
            return false;
    }
}

bool emitter::emitInsMayWriteMultipleRegs(instrDesc* id)
{
    instruction ins = id->idIns();

    switch (ins)
    {
        case INS_ldp:
        case INS_ldpsw:
        case INS_ldnp:
            return true;
        default:
            return false;
    }
}

// Takes an instrDesc 'id' and uses the instruction 'ins' to determine the
// size of the target register that is written or read by the instruction.
// Note that even if EA_4BYTE is returned a load instruction will still
// always zero the upper 4 bytes of the target register.
// This method is required so that we can distinguish between loads that are
// sign-extending as they can have two different sizes for their target register.
// Additionally for instructions like 'ldr' and 'str' these can load/store
// either 4 byte or 8 bytes to/from the target register.
// By convention the small unsigned load instructions are considered to write
// a 4 byte sized target register, though since these also zero the upper 4 bytes
// they could equally be considered to write the unsigned value to full 8 byte register.
//
emitAttr emitter::emitInsTargetRegSize(instrDesc* id)
{
    instruction ins    = id->idIns();
    emitAttr    result = EA_UNKNOWN;

    // This is used to determine the size of the target registers for a load/store instruction

    switch (ins)
    {
        case INS_ldxrb:
        case INS_ldarb:
        case INS_ldaxrb:
        case INS_stxrb:
        case INS_stlrb:
        case INS_stlxrb:
        case INS_ldrb:
        case INS_strb:
        case INS_ldurb:
        case INS_sturb:
            result = EA_4BYTE;
            break;

        case INS_ldxrh:
        case INS_ldarh:
        case INS_ldaxrh:
        case INS_stxrh:
        case INS_stlrh:
        case INS_stlxrh:
        case INS_ldrh:
        case INS_strh:
        case INS_ldurh:
        case INS_sturh:
            result = EA_4BYTE;
            break;

        case INS_ldrsb:
        case INS_ldursb:
        case INS_ldrsh:
        case INS_ldursh:
            if (id->idOpSize() == EA_8BYTE)
                result = EA_8BYTE;
            else
                result = EA_4BYTE;
            break;

        case INS_ldrsw:
        case INS_ldursw:
        case INS_ldpsw:
            result = EA_8BYTE;
            break;

        case INS_ldp:
        case INS_stp:
        case INS_ldnp:
        case INS_stnp:
            result = id->idOpSize();
            break;

        case INS_ldxr:
        case INS_ldar:
        case INS_ldaxr:
        case INS_stxr:
        case INS_stlr:
        case INS_stlxr:
        case INS_ldr:
        case INS_str:
        case INS_ldur:
        case INS_stur:
            result = id->idOpSize();
            break;

        default:
            NO_WAY("unexpected instruction");
            break;
    }
    return result;
}

// Takes an instrDesc and uses the instruction to determine the 'size' of the
// data that is loaded from memory.
//
emitAttr emitter::emitInsLoadStoreSize(instrDesc* id)
{
    instruction ins    = id->idIns();
    emitAttr    result = EA_UNKNOWN;

    // The 'result' returned is the 'size' of the data that is loaded from memory.

    switch (ins)
    {
        case INS_ldarb:
        case INS_stlrb:
        case INS_ldrb:
        case INS_strb:
        case INS_ldurb:
        case INS_sturb:
        case INS_ldrsb:
        case INS_ldursb:
            result = EA_1BYTE;
            break;

        case INS_ldarh:
        case INS_stlrh:
        case INS_ldrh:
        case INS_strh:
        case INS_ldurh:
        case INS_sturh:
        case INS_ldrsh:
        case INS_ldursh:
            result = EA_2BYTE;
            break;

        case INS_ldrsw:
        case INS_ldursw:
        case INS_ldpsw:
            result = EA_4BYTE;
            break;

        case INS_ldp:
        case INS_stp:
        case INS_ldnp:
        case INS_stnp:
            result = id->idOpSize();
            break;

        case INS_ldar:
        case INS_stlr:
        case INS_ldr:
        case INS_str:
        case INS_ldur:
        case INS_stur:
            result = id->idOpSize();
            break;

        default:
            NO_WAY("unexpected instruction");
            break;
    }
    return result;
}

/*****************************************************************************/
#ifdef DEBUG

// clang-format off
static const char * const  xRegNames[] =
{
    #define REGDEF(name, rnum, mask, xname, wname) xname,
    #include "register.h"
};

static const char * const  wRegNames[] =
{
    #define REGDEF(name, rnum, mask, xname, wname) wname,
    #include "register.h"
};

static const char * const  vRegNames[] =
{
    "v0",  "v1",  "v2",  "v3",  "v4", 
    "v5",  "v6",  "v7",  "v8",  "v9", 
    "v10", "v11", "v12", "v13", "v14", 
    "v15", "v16", "v17", "v18", "v19", 
    "v20", "v21", "v22", "v23", "v24", 
    "v25", "v26", "v27", "v28", "v29",
    "v30", "v31"
};

static const char * const  qRegNames[] =
{
    "q0",  "q1",  "q2",  "q3",  "q4", 
    "q5",  "q6",  "q7",  "q8",  "q9", 
    "q10", "q11", "q12", "q13", "q14", 
    "q15", "q16", "q17", "q18", "q19", 
    "q20", "q21", "q22", "q23", "q24", 
    "q25", "q26", "q27", "q28", "q29",
    "q30", "q31"
};

static const char * const  hRegNames[] =
{
    "h0",  "h1",  "h2",  "h3",  "h4", 
    "h5",  "h6",  "h7",  "h8",  "h9", 
    "h10", "h11", "h12", "h13", "h14", 
    "h15", "h16", "h17", "h18", "h19", 
    "h20", "h21", "h22", "h23", "h24", 
    "h25", "h26", "h27", "h28", "h29",
    "h30", "h31"
};
static const char * const  bRegNames[] =
{
    "b0",  "b1",  "b2",  "b3",  "b4", 
    "b5",  "b6",  "b7",  "b8",  "b9", 
    "b10", "b11", "b12", "b13", "b14", 
    "b15", "b16", "b17", "b18", "b19", 
    "b20", "b21", "b22", "b23", "b24", 
    "b25", "b26", "b27", "b28", "b29",
    "b30", "b31"
};
// clang-format on

/*****************************************************************************
 *
 *  Return a string that represents the given register.
 */

const char* emitter::emitRegName(regNumber reg, emitAttr size, bool varName)
{
    assert(reg < REG_COUNT);

    const char* rn = nullptr;

    if (size == EA_8BYTE)
    {
        rn = xRegNames[reg];
    }
    else if (size == EA_4BYTE)
    {
        rn = wRegNames[reg];
    }
    else if (isVectorRegister(reg))
    {
        if (size == EA_16BYTE)
        {
            rn = qRegNames[reg - REG_V0];
        }
        else if (size == EA_2BYTE)
        {
            rn = hRegNames[reg - REG_V0];
        }
        else if (size == EA_1BYTE)
        {
            rn = bRegNames[reg - REG_V0];
        }
    }

    assert(rn != nullptr);

    return rn;
}

/*****************************************************************************
 *
 *  Return a string that represents the given register.
 */

const char* emitter::emitVectorRegName(regNumber reg)
{
    assert((reg >= REG_V0) && (reg <= REG_V31));

    int index = (int)reg - (int)REG_V0;

    return vRegNames[index];
}
#endif // DEBUG

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
    #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) ldst | INST_FP*fp,
    #include "instrs.h"
};
// clang-format on

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
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9) e9,
        #include "instrs.h"
    };
    // clang-format on

    const static insFormat formatEncode9[9] = {IF_DR_2E, IF_DR_2G, IF_DI_1B, IF_DI_1D, IF_DV_3C,
                                               IF_DV_2B, IF_DV_2C, IF_DV_2E, IF_DV_2F};
    const static insFormat formatEncode6A[6] = {IF_DR_3A, IF_DR_3B, IF_DR_3C, IF_DI_2A, IF_DV_3A, IF_DV_3E};
    const static insFormat formatEncode5A[5] = {IF_LS_2A, IF_LS_2B, IF_LS_2C, IF_LS_3A, IF_LS_1A};
    const static insFormat formatEncode5B[5] = {IF_DV_2G, IF_DV_2H, IF_DV_2I, IF_DV_1A, IF_DV_1B};
    const static insFormat formatEncode5C[5] = {IF_DR_3A, IF_DR_3B, IF_DI_2C, IF_DV_3C, IF_DV_1B};
    const static insFormat formatEncode4A[4] = {IF_LS_2A, IF_LS_2B, IF_LS_2C, IF_LS_3A};
    const static insFormat formatEncode4B[4] = {IF_DR_3A, IF_DR_3B, IF_DR_3C, IF_DI_2A};
    const static insFormat formatEncode4C[4] = {IF_DR_2A, IF_DR_2B, IF_DR_2C, IF_DI_1A};
    const static insFormat formatEncode4D[4] = {IF_DV_3B, IF_DV_3D, IF_DV_3BI, IF_DV_3DI};
    const static insFormat formatEncode4E[4] = {IF_DR_3A, IF_DR_3B, IF_DI_2C, IF_DV_3C};
    const static insFormat formatEncode4F[4] = {IF_DR_3A, IF_DR_3B, IF_DV_3C, IF_DV_1B};
    const static insFormat formatEncode4G[4] = {IF_DR_2E, IF_DR_2F, IF_DV_2M, IF_DV_2L};
    const static insFormat formatEncode4H[4] = {IF_DV_3E, IF_DV_3A, IF_DV_2L, IF_DV_2M};
    const static insFormat formatEncode4I[4] = {IF_DV_3D, IF_DV_3B, IF_DV_2G, IF_DV_2A};
    const static insFormat formatEncode3A[3] = {IF_DR_3A, IF_DR_3B, IF_DI_2C};
    const static insFormat formatEncode3B[3] = {IF_DR_2A, IF_DR_2B, IF_DI_1C};
    const static insFormat formatEncode3C[3] = {IF_DR_3A, IF_DR_3B, IF_DV_3C};
    const static insFormat formatEncode3D[3] = {IF_DV_2C, IF_DV_2D, IF_DV_2E};
    const static insFormat formatEncode3E[3] = {IF_DV_3B, IF_DV_3BI, IF_DV_3DI};
    const static insFormat formatEncode3F[3] = {IF_DV_2A, IF_DV_2G, IF_DV_2H};
    const static insFormat formatEncode3G[3] = {IF_DV_2A, IF_DV_2G, IF_DV_2I};
    const static insFormat formatEncode3H[3] = {IF_DR_3A, IF_DV_3A, IF_DV_3AI};
    const static insFormat formatEncode3I[3] = {IF_DR_2E, IF_DR_2F, IF_DV_2M};
    const static insFormat formatEncode2A[2] = {IF_DR_2E, IF_DR_2F};
    const static insFormat formatEncode2B[2] = {IF_DR_3A, IF_DR_3B};
    const static insFormat formatEncode2C[2] = {IF_DR_3A, IF_DI_2D};
    const static insFormat formatEncode2D[2] = {IF_DR_3A, IF_DI_2B};
    const static insFormat formatEncode2E[2] = {IF_LS_3B, IF_LS_3C};
    const static insFormat formatEncode2F[2] = {IF_DR_2I, IF_DI_1F};
    const static insFormat formatEncode2G[2] = {IF_DV_3B, IF_DV_3D};
    const static insFormat formatEncode2H[2] = {IF_DV_2C, IF_DV_2F};
    const static insFormat formatEncode2I[2] = {IF_DV_2K, IF_DV_1C};
    const static insFormat formatEncode2J[2] = {IF_DV_2A, IF_DV_2G};
    const static insFormat formatEncode2K[2] = {IF_DV_2M, IF_DV_2L};
    const static insFormat formatEncode2L[2] = {IF_DR_2G, IF_DV_2M};
    const static insFormat formatEncode2M[2] = {IF_DV_3A, IF_DV_3AI};
    const static insFormat formatEncode2N[2] = {IF_DV_2N, IF_DV_2O};
    const static insFormat formatEncode2O[2] = {IF_DV_3E, IF_DV_3A};
    const static insFormat formatEncode2P[2] = {IF_DV_2G, IF_DV_3B};

    code_t    code           = BAD_CODE;
    insFormat insFmt         = emitInsFormat(ins);
    bool      encoding_found = false;
    int       index          = -1;

    switch (insFmt)
    {
        case IF_EN9:
            for (index = 0; index < 9; index++)
            {
                if (fmt == formatEncode9[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN6A:
            for (index = 0; index < 6; index++)
            {
                if (fmt == formatEncode6A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN5A:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN5B:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN5C:
            for (index = 0; index < 5; index++)
            {
                if (fmt == formatEncode5C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4A:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4B:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4C:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4D:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4E:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4F:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4G:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4H:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4H[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN4I:
            for (index = 0; index < 4; index++)
            {
                if (fmt == formatEncode4I[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3A:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3B:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3C:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3D:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3E:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3F:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3G:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3H:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3H[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN3I:
            for (index = 0; index < 3; index++)
            {
                if (fmt == formatEncode3I[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2A:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2A[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2B:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2B[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2C:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2C[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2D:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2D[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2E:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2E[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2F:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2F[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2G:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2G[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2H:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2H[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2I:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2I[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2J:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2J[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2K:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2K[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2L:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2L[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2M:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2M[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2N:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2N[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2O:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2O[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_EN2P:
            for (index = 0; index < 2; index++)
            {
                if (fmt == formatEncode2P[index])
                {
                    encoding_found = true;
                    break;
                }
            }
            break;

        case IF_BI_0A:
        case IF_BI_0B:
        case IF_BI_0C:
        case IF_BI_1A:
        case IF_BI_1B:
        case IF_BR_1A:
        case IF_BR_1B:
        case IF_LS_1A:
        case IF_LS_2A:
        case IF_LS_2B:
        case IF_LS_2C:
        case IF_LS_3A:
        case IF_LS_3B:
        case IF_LS_3C:
        case IF_LS_3D:
        case IF_LS_3E:
        case IF_DI_1A:
        case IF_DI_1B:
        case IF_DI_1C:
        case IF_DI_1D:
        case IF_DI_1E:
        case IF_DI_1F:
        case IF_DI_2A:
        case IF_DI_2B:
        case IF_DI_2C:
        case IF_DI_2D:
        case IF_DR_1D:
        case IF_DR_2A:
        case IF_DR_2B:
        case IF_DR_2C:
        case IF_DR_2D:
        case IF_DR_2E:
        case IF_DR_2F:
        case IF_DR_2G:
        case IF_DR_2H:
        case IF_DR_2I:
        case IF_DR_2J:
        case IF_DR_3A:
        case IF_DR_3B:
        case IF_DR_3C:
        case IF_DR_3D:
        case IF_DR_3E:
        case IF_DR_4A:
        case IF_DV_1A:
        case IF_DV_1B:
        case IF_DV_1C:
        case IF_DV_2A:
        case IF_DV_2B:
        case IF_DV_2C:
        case IF_DV_2D:
        case IF_DV_2E:
        case IF_DV_2F:
        case IF_DV_2G:
        case IF_DV_2H:
        case IF_DV_2I:
        case IF_DV_2J:
        case IF_DV_2K:
        case IF_DV_2L:
        case IF_DV_2M:
        case IF_DV_2N:
        case IF_DV_2O:
        case IF_DV_2P:
        case IF_DV_3A:
        case IF_DV_3AI:
        case IF_DV_3B:
        case IF_DV_3BI:
        case IF_DV_3C:
        case IF_DV_3D:
        case IF_DV_3DI:
        case IF_DV_3E:
        case IF_DV_3F:
        case IF_DV_4A:
        case IF_SN_0A:
        case IF_SI_0A:
        case IF_SI_0B:

            index          = 0;
            encoding_found = true;
            break;

        default:

            encoding_found = false;
            break;
    }

    assert(encoding_found);

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

// true if this 'imm' can be encoded as a input operand to a mov instruction
/*static*/ bool emitter::emitIns_valid_imm_for_mov(INT64 imm, emitAttr size)
{
    // Check for "MOV (wide immediate)".
    if (canEncodeHalfwordImm(imm, size))
        return true;

    // Next try the ones-complement form of 'halfword immediate' imm(i16,hw),
    // namely "MOV (inverted wide immediate)".
    ssize_t notOfImm = NOT_helper(imm, getBitWidth(size));
    if (canEncodeHalfwordImm(notOfImm, size))
        return true;

    // Finally try "MOV (bitmask immediate)" imm(N,r,s)
    if (canEncodeBitMaskImm(imm, size))
        return true;

    return false;
}

// true if this 'imm' can be encoded as a input operand to a vector movi instruction
/*static*/ bool emitter::emitIns_valid_imm_for_movi(INT64 imm, emitAttr elemsize)
{
    if (elemsize == EA_8BYTE)
    {
        UINT64 uimm = imm;
        while (uimm != 0)
        {
            INT64 loByte = uimm & 0xFF;
            if ((loByte == 0) || (loByte == 0xFF))
            {
                uimm >>= 8;
            }
            else
            {
                return false;
            }
        }
        assert(uimm == 0);
        return true;
    }
    else
    {
        // First try the standard 'byteShifted immediate' imm(i8,bySh)
        if (canEncodeByteShiftedImm(imm, elemsize, true))
            return true;

        // Next try the ones-complement form of the 'immediate' imm(i8,bySh)
        ssize_t notOfImm = NOT_helper(imm, getBitWidth(elemsize));
        if (canEncodeByteShiftedImm(notOfImm, elemsize, true))
            return true;
    }
    return false;
}

// true if this 'imm' can be encoded as a input operand to a fmov instruction
/*static*/ bool emitter::emitIns_valid_imm_for_fmov(double immDbl)
{
    if (canEncodeFloatImm8(immDbl))
        return true;

    return false;
}

// true if this 'imm' can be encoded as a input operand to an add instruction
/*static*/ bool emitter::emitIns_valid_imm_for_add(INT64 imm, emitAttr size)
{
    if (unsigned_abs(imm) <= 0x0fff)
        return true;
    else if (canEncodeWithShiftImmBy12(imm)) // Try the shifted by 12 encoding
        return true;

    return false;
}

// true if this 'imm' can be encoded as a input operand to an non-add/sub alu instruction
/*static*/ bool emitter::emitIns_valid_imm_for_cmp(INT64 imm, emitAttr size)
{
    return emitIns_valid_imm_for_add(imm, size);
}

// true if this 'imm' can be encoded as a input operand to an non-add/sub alu instruction
/*static*/ bool emitter::emitIns_valid_imm_for_alu(INT64 imm, emitAttr size)
{
    if (canEncodeBitMaskImm(imm, size))
        return true;

    return false;
}

// true if this 'imm' can be encoded as the offset in a ldr/str instruction
/*static*/ bool emitter::emitIns_valid_imm_for_ldst_offset(INT64 imm, emitAttr attr)
{
    if (imm == 0)
        return true; // Encodable using IF_LS_2A

    if ((imm >= -256) && (imm <= 255))
        return true; // Encodable using IF_LS_2C (or possibly IF_LS_2B)

    if (imm < 0)
        return false; // not encodable

    emitAttr size  = EA_SIZE(attr);
    unsigned scale = NaturalScale_helper(size);
    ssize_t  mask  = size - 1; // the mask of low bits that must be zero to encode the immediate

    if (((imm & mask) == 0) && ((imm >> scale) < 0x1000))
        return true; // Encodable using IF_LS_2B

    return false; // not encodable
}

/************************************************************************
 *
 *   A helper method to return the natural scale for an EA 'size'
 */

/*static*/ unsigned emitter::NaturalScale_helper(emitAttr size)
{
    assert(size == EA_1BYTE || size == EA_2BYTE || size == EA_4BYTE || size == EA_8BYTE || size == EA_16BYTE);

    unsigned result = 0;
    unsigned utemp  = (unsigned)size;

    // Compute log base 2 of utemp (aka 'size')
    while (utemp > 1)
    {
        result++;
        utemp >>= 1;
    }

    return result;
}

/************************************************************************
 *
 *  A helper method to perform a Rotate-Right shift operation
 *  the source is 'value' and it is rotated right by 'sh' bits
 *  'value' is considered to be a fixed size 'width' set of bits.
 *
 *  Example
 *      value is '00001111', sh is 2 and width is 8
 *     result is '11000011'
 */

/*static*/ UINT64 emitter::ROR_helper(UINT64 value, unsigned sh, unsigned width)
{
    assert(width <= 64);
    // Check that 'value' fits in 'width' bits
    assert((width == 64) || (value < (1ULL << width)));
    // We don't support shifts >= width
    assert(sh < width);

    UINT64 result;

    unsigned rsh = sh;
    unsigned lsh = width - rsh;

    result = (value >> rsh);
    result |= (value << lsh);

    if (width < 64)
    {
        // mask off any extra bits that we got from the left shift
        result &= ((1ULL << width) - 1);
    }
    return result;
}
/************************************************************************
 *
 *  A helper method to perform a 'NOT' bitwise complement operation.
 *  'value' is considered to be a fixed size 'width' set of bits.
 *
 *  Example
 *      value is '01001011', and width is 8
 *     result is '10110100'
 */

/*static*/ UINT64 emitter::NOT_helper(UINT64 value, unsigned width)
{
    assert(width <= 64);

    UINT64 result = ~value;

    if (width < 64)
    {
        // Check that 'value' fits in 'width' bits. Don't consider "sign" bits above width.
        UINT64 maxVal       = 1ULL << width;
        UINT64 lowBitsMask  = maxVal - 1;
        UINT64 signBitsMask = ~lowBitsMask | (1ULL << (width - 1)); // The high bits must be set, and the top bit
                                                                    // (sign bit) must be set.
        assert((value < maxVal) || ((value & signBitsMask) == signBitsMask));

        // mask off any extra bits that we got from the complement operation
        result &= lowBitsMask;
    }

    return result;
}

/************************************************************************
 *
 *  A helper method to perform a bit Replicate operation
 *  the source is 'value' with a fixed size 'width' set of bits.
 *  value is replicated to fill out 32 or 64 bits as determined by 'size'.
 *
 *  Example
 *      value is '11000011' (0xE3), width is 8 and size is EA_8BYTE
 *     result is '11000011 11000011 11000011 11000011 11000011 11000011 11000011 11000011'
 *               0xE3E3E3E3E3E3E3E3
 */

/*static*/ UINT64 emitter::Replicate_helper(UINT64 value, unsigned width, emitAttr size)
{
    assert(emitter::isValidGeneralDatasize(size));

    unsigned immWidth = (size == EA_8BYTE) ? 64 : 32;
    assert(width <= immWidth);

    UINT64   result     = value;
    unsigned filledBits = width;

    while (filledBits < immWidth)
    {
        value <<= width;
        result |= value;
        filledBits += width;
    }
    return result;
}

/************************************************************************
 *
 *  Convert an imm(N,r,s) into a 64-bit immediate
 *  inputs 'bmImm' a bitMaskImm struct
 *         'size' specifies the size of the result (64 or 32 bits)
 */

/*static*/ INT64 emitter::emitDecodeBitMaskImm(const emitter::bitMaskImm bmImm, emitAttr size)
{
    assert(isValidGeneralDatasize(size)); // Only EA_4BYTE or EA_8BYTE forms

    unsigned N = bmImm.immN; // read the N,R and S values from the 'bitMaskImm' encoding
    unsigned R = bmImm.immR;
    unsigned S = bmImm.immS;

    unsigned elemWidth = 64; // used when N == 1

    if (N == 0) // find the smaller elemWidth when N == 0
    {
        // Scan S for the highest bit not set
        elemWidth = 32;
        for (unsigned bitNum = 5; bitNum > 0; bitNum--)
        {
            unsigned oneBit = elemWidth;
            if ((S & oneBit) == 0)
                break;
            elemWidth /= 2;
        }
    }
    else
    {
        assert(size == EA_8BYTE);
    }

    unsigned maskSR = elemWidth - 1;

    S &= maskSR;
    R &= maskSR;

    // encoding for S is one less than the number of consecutive one bits
    S++; // Number of consecutive ones to generate in 'welem'

    // At this point:
    //
    //    'elemWidth' is the number of bits that we will use for the ROR and Replicate operations
    //    'S'         is the number of consecutive 1 bits for the immediate
    //    'R'         is the number of bits that we will Rotate Right the immediate
    //    'size'      selects the final size of the immedate that we return (64 or 32 bits)

    assert(S < elemWidth); // 'elemWidth' consecutive one's is a reserved encoding

    UINT64 welem;
    UINT64 wmask;

    welem = (1ULL << S) - 1;

    wmask = ROR_helper(welem, R, elemWidth);
    wmask = Replicate_helper(wmask, elemWidth, size);

    return wmask;
}

/*****************************************************************************
 *
 *  Check if an immediate can use the left shifted by 12 bits encoding
 */

/*static*/ bool emitter::canEncodeWithShiftImmBy12(INT64 imm)
{
    if (imm < 0)
    {
        imm = -imm; // convert to unsigned
    }

    if (imm < 0)
    {
        return false; // Must be MIN_INT64
    }

    if ((imm & 0xfff) != 0) // Now the low 12 bits all have to be zero
    {
        return false;
    }

    imm >>= 12; // shift right by 12 bits

    return (imm <= 0x0fff); // Does it fit in 12 bits
}

/*****************************************************************************
 *
 *  Normalize the 'imm' so that the upper bits, as defined by 'size' are zero
 */

/*static*/ INT64 emitter::normalizeImm64(INT64 imm, emitAttr size)
{
    unsigned immWidth = getBitWidth(size);
    INT64    result   = imm;

    if (immWidth < 64)
    {
        // Check that 'imm' fits in 'immWidth' bits. Don't consider "sign" bits above width.
        INT64 maxVal      = 1LL << immWidth;
        INT64 lowBitsMask = maxVal - 1;
        INT64 hiBitsMask  = ~lowBitsMask;
        INT64 signBitsMask =
            hiBitsMask | (1LL << (immWidth - 1)); // The high bits must be set, and the top bit (sign bit) must be set.
        assert((imm < maxVal) || ((imm & signBitsMask) == signBitsMask));

        // mask off the hiBits
        result &= lowBitsMask;
    }
    return result;
}

/*****************************************************************************
 *
 *  Normalize the 'imm' so that the upper bits, as defined by 'size' are zero
 */

/*static*/ INT32 emitter::normalizeImm32(INT32 imm, emitAttr size)
{
    unsigned immWidth = getBitWidth(size);
    INT32    result   = imm;

    if (immWidth < 32)
    {
        // Check that 'imm' fits in 'immWidth' bits. Don't consider "sign" bits above width.
        INT32 maxVal       = 1 << immWidth;
        INT32 lowBitsMask  = maxVal - 1;
        INT32 hiBitsMask   = ~lowBitsMask;
        INT32 signBitsMask = hiBitsMask | (1 << (immWidth - 1)); // The high bits must be set, and the top bit
                                                                 // (sign bit) must be set.
        assert((imm < maxVal) || ((imm & signBitsMask) == signBitsMask));

        // mask off the hiBits
        result &= lowBitsMask;
    }
    return result;
}

/************************************************************************
 *
 *  returns true if 'imm' of 'size bits (32/64) can be encoded
 *  using the ARM64 'bitmask immediate' form.
 *  When a non-null value is passed for 'wbBMI' then this method
 *  writes back the 'N','S' and 'R' values use to encode this immediate
 *
 */

/*static*/ bool emitter::canEncodeBitMaskImm(INT64 imm, emitAttr size, emitter::bitMaskImm* wbBMI)
{
    assert(isValidGeneralDatasize(size)); // Only EA_4BYTE or EA_8BYTE forms

    unsigned immWidth = (size == EA_8BYTE) ? 64 : 32;
    unsigned maxLen   = (size == EA_8BYTE) ? 6 : 5;

    imm = normalizeImm64(imm, size);

    // Starting with len=1, elemWidth is 2 bits
    //               len=2, elemWidth is 4 bits
    //               len=3, elemWidth is 8 bits
    //               len=4, elemWidth is 16 bits
    //               len=5, elemWidth is 32 bits
    // (optionally)  len=6, elemWidth is 64 bits
    //
    for (unsigned len = 1; (len <= maxLen); len++)
    {
        unsigned elemWidth = 1 << len;
        UINT64   elemMask  = ((UINT64)-1) >> (64 - elemWidth);
        UINT64   tempImm   = (UINT64)imm;        // A working copy of 'imm' that we can mutate
        UINT64   elemVal   = tempImm & elemMask; // The low 'elemWidth' bits of 'imm'

        // Check for all 1's or 0's as these can't be encoded
        if ((elemVal == 0) || (elemVal == elemMask))
            continue;

        // 'checkedBits' is the count of bits that are known to match 'elemVal' when replicated
        unsigned checkedBits = elemWidth; // by definition the first 'elemWidth' bits match

        // Now check to see if each of the next bits match...
        //
        while (checkedBits < immWidth)
        {
            tempImm >>= elemWidth;

            UINT64 nextElem = tempImm & elemMask;
            if (nextElem != elemVal)
            {
                // Not matching, exit this loop and checkedBits will not be equal to immWidth
                break;
            }

            // The 'nextElem' is matching, so increment 'checkedBits'
            checkedBits += elemWidth;
        }

        // Did the full immediate contain bits that can be formed by repeating 'elemVal'?
        if (checkedBits == immWidth)
        {
            // We are not quite done, since the only values that we can encode as a
            // 'bitmask immediate' are those that can be formed by starting with a
            // bit string of 0*1* that is rotated by some number of bits.
            //
            // We check to see if 'elemVal' can be formed using these restrictions.
            //
            // Observation:
            // Rotating by one bit any value that passes these restrictions
            // can be xor-ed with the original value and will result it a string
            // of bits that have exactly two 1 bits: 'elemRorXor'
            // Further the distance between the two one bits tells us the value
            // of S and the location of the 1 bits tells us the value of R
            //
            // Some examples:   (immWidth is 8)
            //
            // S=4,R=0   S=5,R=3   S=3,R=6
            // elemVal:        00001111  11100011  00011100
            // elemRor:        10000111  11110001  00001110
            // elemRorXor:     10001000  00010010  00010010
            //      compute S  45678---  ---5678-  ---3210-
            //      compute R  01234567  ---34567  ------67

            UINT64 elemRor    = ROR_helper(elemVal, 1, elemWidth); // Rotate 'elemVal' Right by one bit
            UINT64 elemRorXor = elemVal ^ elemRor;                 // Xor elemVal and elemRor

            // If we only have a two-bit change in elemROR then we can form a mask for this value
            unsigned bitCount = 0;
            UINT64   oneBit   = 0x1;
            unsigned R        = elemWidth; // R is shift count for ROR (rotate right shift)
            unsigned S        = 0;         // S is number of consecutive one bits
            int      incr     = -1;

            // Loop over the 'elemWidth' bits in 'elemRorXor'
            //
            for (unsigned bitNum = 0; bitNum < elemWidth; bitNum++)
            {
                if (incr == -1)
                {
                    R--; // We decrement R by one whenever incr is -1
                }
                if (bitCount == 1)
                {
                    S += incr; // We incr/decr S, after we find the first one bit in 'elemRorXor'
                }

                // Is this bit position a 1 bit in 'elemRorXor'?
                //
                if (oneBit & elemRorXor)
                {
                    bitCount++;
                    // Is this the first 1 bit that we found in 'elemRorXor'?
                    if (bitCount == 1)
                    {
                        // Does this 1 bit represent a transition to zero bits?
                        bool toZeros = ((oneBit & elemVal) != 0);
                        if (toZeros)
                        {
                            // S :: Count down from elemWidth
                            S    = elemWidth;
                            incr = -1;
                        }
                        else // this 1 bit represent a transition to one bits.
                        {
                            // S :: Count up from zero
                            S    = 0;
                            incr = +1;
                        }
                    }
                    else // bitCount > 1
                    {
                        // We found the second (or third...) 1 bit in 'elemRorXor'
                        incr = 0; // stop decrementing 'R'

                        if (bitCount > 2)
                        {
                            // More than 2 transitions from 0/1 in 'elemVal'
                            // This means that 'elemVal' can't be encoded
                            // using a 'bitmask immediate'.
                            //
                            // Furthermore, it will continue to fail
                            // with any larger 'len' that we try.
                            // so just return false.
                            //
                            return false;
                        }
                    }
                }

                // shift oneBit left by one bit to test the next position
                oneBit <<= 1;
            }

            // We expect that bitCount will always be two at this point
            // but just in case return false for any bad cases.
            //
            assert(bitCount == 2);
            if (bitCount != 2)
                return false;

            // Perform some sanity checks on the values of 'S' and 'R'
            assert(S > 0);
            assert(S < elemWidth);
            assert(R < elemWidth);

            // Does the caller want us to return the N,R,S encoding values?
            //
            if (wbBMI != nullptr)
            {

                // The encoding used for S is one less than the
                //  number of consecutive one bits
                S--;

                if (len == 6)
                {
                    wbBMI->immN = 1;
                }
                else
                {
                    wbBMI->immN = 0;
                    // The encoding used for 'S' here is a bit peculiar.
                    //
                    // The upper bits need to be complemented, followed by a zero bit
                    // then the value of 'S-1'
                    //
                    unsigned upperBitsOfS = 64 - (1 << (len + 1));
                    S |= upperBitsOfS;
                }
                wbBMI->immR = R;
                wbBMI->immS = S;

                // Verify that what we are returning is correct.
                assert(imm == emitDecodeBitMaskImm(*wbBMI, size));
            }
            // Tell the caller that we can successfully encode this immediate
            // using a 'bitmask immediate'.
            //
            return true;
        }
    }
    return false;
}

/************************************************************************
 *
 *  Convert a 64-bit immediate into its 'bitmask immediate' representation imm(N,r,s)
 */

/*static*/ emitter::bitMaskImm emitter::emitEncodeBitMaskImm(INT64 imm, emitAttr size)
{
    emitter::bitMaskImm result;
    result.immNRS = 0;

    bool canEncode = canEncodeBitMaskImm(imm, size, &result);
    assert(canEncode);

    return result;
}

/************************************************************************
 *
 *  Convert an imm(i16,hw) into a 32/64-bit immediate
 *  inputs 'hwImm' a halfwordImm struct
 *         'size' specifies the size of the result (64 or 32 bits)
 */

/*static*/ INT64 emitter::emitDecodeHalfwordImm(const emitter::halfwordImm hwImm, emitAttr size)
{
    assert(isValidGeneralDatasize(size)); // Only EA_4BYTE or EA_8BYTE forms

    unsigned hw  = hwImm.immHW;
    INT64    val = (INT64)hwImm.immVal;

    assert((hw <= 1) || (size == EA_8BYTE));

    INT64 result = val << (16 * hw);
    return result;
}

/************************************************************************
 *
 *  returns true if 'imm' of 'size' bits (32/64) can be encoded
 *  using the ARM64 'halfword immediate' form.
 *  When a non-null value is passed for 'wbHWI' then this method
 *  writes back the 'immHW' and 'immVal' values use to encode this immediate
 *
 */

/*static*/ bool emitter::canEncodeHalfwordImm(INT64 imm, emitAttr size, emitter::halfwordImm* wbHWI)
{
    assert(isValidGeneralDatasize(size)); // Only EA_4BYTE or EA_8BYTE forms

    unsigned immWidth = (size == EA_8BYTE) ? 64 : 32;
    unsigned maxHW    = (size == EA_8BYTE) ? 4 : 2;

    // setup immMask to a (EA_4BYTE) 0x00000000_FFFFFFFF or (EA_8BYTE) 0xFFFFFFFF_FFFFFFFF
    const UINT64 immMask = ((UINT64)-1) >> (64 - immWidth);
    const INT64  mask16  = (INT64)0xFFFF;

    imm = normalizeImm64(imm, size);

    // Try each of the valid hw shift sizes
    for (unsigned hw = 0; (hw < maxHW); hw++)
    {
        INT64 curMask   = mask16 << (hw * 16); // Represents the mask of the bits in the current halfword
        INT64 checkBits = immMask & ~curMask;

        // Excluding the current halfword (using ~curMask)
        //  does the immediate have zero bits in every other bit that we care about?
        //  note we care about all 64-bits for EA_8BYTE
        //  and we care about the lowest 32 bits for EA_4BYTE
        //
        if ((imm & checkBits) == 0)
        {
            // Does the caller want us to return the imm(i16,hw) encoding values?
            //
            if (wbHWI != nullptr)
            {
                INT64 val     = ((imm & curMask) >> (hw * 16)) & mask16;
                wbHWI->immHW  = hw;
                wbHWI->immVal = val;

                // Verify that what we are returning is correct.
                assert(imm == emitDecodeHalfwordImm(*wbHWI, size));
            }
            // Tell the caller that we can successfully encode this immediate
            // using a 'halfword immediate'.
            //
            return true;
        }
    }
    return false;
}

/************************************************************************
 *
 *  Convert a 64-bit immediate into its 'halfword immediate' representation imm(i16,hw)
 */

/*static*/ emitter::halfwordImm emitter::emitEncodeHalfwordImm(INT64 imm, emitAttr size)
{
    emitter::halfwordImm result;
    result.immHWVal = 0;

    bool canEncode = canEncodeHalfwordImm(imm, size, &result);
    assert(canEncode);

    return result;
}

/************************************************************************
 *
 *  Convert an imm(i8,sh) into a 16/32-bit immediate
 *  inputs 'bsImm' a byteShiftedImm struct
 *         'size' specifies the size of the result (16 or 32 bits)
 */

/*static*/ INT32 emitter::emitDecodeByteShiftedImm(const emitter::byteShiftedImm bsImm, emitAttr size)
{
    bool     onesShift = (bsImm.immOnes == 1);
    unsigned bySh      = bsImm.immBY;         // Num Bytes to shift 0,1,2,3
    INT32    val       = (INT32)bsImm.immVal; // 8-bit immediate
    INT32    result    = val;

    if (bySh > 0)
    {
        assert((size == EA_2BYTE) || (size == EA_4BYTE)); // Only EA_2BYTE or EA_4BYTE forms
        if (size == EA_2BYTE)
        {
            assert(bySh < 2);
        }
        else
        {
            assert(bySh < 4);
        }

        result <<= (8 * bySh);

        if (onesShift)
        {
            result |= ((1 << (8 * bySh)) - 1);
        }
    }
    return result;
}

/************************************************************************
 *
 *  returns true if 'imm' of 'size' bits (16/32) can be encoded
 *  using the ARM64 'byteShifted immediate' form.
 *  When a non-null value is passed for 'wbBSI' then this method
 *  writes back the 'immBY' and 'immVal' values use to encode this immediate
 *
 */

/*static*/ bool emitter::canEncodeByteShiftedImm(INT64                    imm,
                                                 emitAttr                 size,
                                                 bool                     allow_MSL,
                                                 emitter::byteShiftedImm* wbBSI)
{
    bool     canEncode = false;
    bool     onesShift = false; // true if we use the shifting ones variant
    unsigned bySh      = 0;     // number of bytes to shift: 0, 1, 2, 3
    unsigned imm8      = 0;     // immediate to use in the encoding

    imm = normalizeImm64(imm, size);

    if (size == EA_1BYTE)
    {
        imm8 = (unsigned)imm;
        assert(imm8 < 0x100);
        canEncode = true;
    }
    else if (size == EA_8BYTE)
    {
        imm8 = (unsigned)imm;
        assert(imm8 < 0x100);
        canEncode = true;
    }
    else
    {
        assert((size == EA_2BYTE) || (size == EA_4BYTE)); // Only EA_2BYTE or EA_4BYTE forms

        unsigned immWidth = (size == EA_4BYTE) ? 32 : 16;
        unsigned maxBY    = (size == EA_4BYTE) ? 4 : 2;

        // setup immMask to a (EA_2BYTE) 0x0000FFFF or (EA_4BYTE) 0xFFFFFFFF
        const UINT32 immMask = ((UINT32)-1) >> (32 - immWidth);
        const INT32  mask8   = (INT32)0xFF;

        // Try each of the valid by shift sizes
        for (bySh = 0; (bySh < maxBY); bySh++)
        {
            INT32 curMask   = mask8 << (bySh * 8); // Represents the mask of the bits in the current byteShifted
            INT32 checkBits = immMask & ~curMask;
            INT32 immCheck  = (imm & checkBits);

            // Excluding the current byte (using ~curMask)
            //  does the immediate have zero bits in every other bit that we care about?
            //  or can be use the shifted one variant?
            //  note we care about all 32-bits for EA_4BYTE
            //  and we care about the lowest 16 bits for EA_2BYTE
            //
            if (immCheck == 0)
            {
                canEncode = true;
            }
            if (allow_MSL)
            {
                if ((bySh == 1) && (immCheck == 0xFF))
                {
                    canEncode = true;
                    onesShift = true;
                }
                else if ((bySh == 2) && (immCheck == 0xFFFF))
                {
                    canEncode = true;
                    onesShift = true;
                }
            }
            if (canEncode)
            {
                imm8 = (unsigned)(((imm & curMask) >> (bySh * 8)) & mask8);
                break;
            }
        }
    }

    if (canEncode)
    {
        // Does the caller want us to return the imm(i8,bySh) encoding values?
        //
        if (wbBSI != nullptr)
        {
            wbBSI->immOnes = onesShift;
            wbBSI->immBY   = bySh;
            wbBSI->immVal  = imm8;

            // Verify that what we are returning is correct.
            assert(imm == emitDecodeByteShiftedImm(*wbBSI, size));
        }
        // Tell the caller that we can successfully encode this immediate
        // using a 'byteShifted immediate'.
        //
        return true;
    }
    return false;
}

/************************************************************************
 *
 *  Convert a 32-bit immediate into its 'byteShifted immediate' representation imm(i8,by)
 */

/*static*/ emitter::byteShiftedImm emitter::emitEncodeByteShiftedImm(INT64 imm, emitAttr size, bool allow_MSL)
{
    emitter::byteShiftedImm result;
    result.immBSVal = 0;

    bool canEncode = canEncodeByteShiftedImm(imm, size, allow_MSL, &result);
    assert(canEncode);

    return result;
}

/************************************************************************
 *
 *  Convert a 'float 8-bit immediate' into a double.
 *  inputs 'fpImm' a floatImm8 struct
 */

/*static*/ double emitter::emitDecodeFloatImm8(const emitter::floatImm8 fpImm)
{
    unsigned sign  = fpImm.immSign;
    unsigned exp   = fpImm.immExp ^ 0x4;
    unsigned mant  = fpImm.immMant + 16;
    unsigned scale = 16 * 8;

    while (exp > 0)
    {
        scale /= 2;
        exp--;
    }

    double result = ((double)mant) / ((double)scale);
    if (sign == 1)
    {
        result = -result;
    }

    return result;
}

/************************************************************************
 *
 *  returns true if the 'immDbl' can be encoded using the 'float 8-bit immediate' form.
 *  also returns the encoding if wbFPI is non-null
 *
 */

/*static*/ bool emitter::canEncodeFloatImm8(double immDbl, emitter::floatImm8* wbFPI)
{
    bool   canEncode = false;
    double val       = immDbl;

    int sign = 0;
    if (val < 0.0)
    {
        val  = -val;
        sign = 1;
    }

    int exp = 0;
    while ((val < 1.0) && (exp >= -4))
    {
        val *= 2.0;
        exp--;
    }
    while ((val >= 2.0) && (exp <= 5))
    {
        val *= 0.5;
        exp++;
    }
    exp += 3;
    val *= 16.0;
    int ival = (int)val;

    if ((exp >= 0) && (exp <= 7))
    {
        if (val == (double)ival)
        {
            canEncode = true;

            if (wbFPI != nullptr)
            {
                ival -= 16;
                assert((ival >= 0) && (ival <= 15));

                wbFPI->immSign = sign;
                wbFPI->immExp  = exp ^ 0x4;
                wbFPI->immMant = ival;
                unsigned imm8  = wbFPI->immFPIVal;
                assert((imm8 >= 0) && (imm8 <= 0xff));
            }
        }
    }

    return canEncode;
}

/************************************************************************
 *
 *  Convert a double into its 'float 8-bit immediate' representation
 */

/*static*/ emitter::floatImm8 emitter::emitEncodeFloatImm8(double immDbl)
{
    emitter::floatImm8 result;
    result.immFPIVal = 0;

    bool canEncode = canEncodeFloatImm8(immDbl, &result);
    assert(canEncode);

    return result;
}

/*****************************************************************************
 *
 *  For the given 'ins' returns the reverse instruction
 *  if one exists, otherwise returns INS_INVALID
 */

/*static*/ instruction emitter::insReverse(instruction ins)
{
    switch (ins)
    {
        case INS_add:
            return INS_sub;
        case INS_adds:
            return INS_subs;

        case INS_sub:
            return INS_add;
        case INS_subs:
            return INS_adds;

        case INS_cmp:
            return INS_cmn;
        case INS_cmn:
            return INS_cmp;

        case INS_ccmp:
            return INS_ccmn;
        case INS_ccmn:
            return INS_ccmp;

        default:
            return INS_invalid;
    }
}

/*****************************************************************************
 *
 *  For the given 'datasize' and 'elemsize', make the proper arrangement option
 *  returns the insOpts that specifies the vector register arrangement
 *  if one does not exist returns INS_OPTS_NONE
 */

/*static*/ insOpts emitter::optMakeArrangement(emitAttr datasize, emitAttr elemsize)
{
    insOpts result = INS_OPTS_NONE;

    if (datasize == EA_8BYTE)
    {
        switch (elemsize)
        {
            case EA_1BYTE:
                result = INS_OPTS_8B;
                break;
            case EA_2BYTE:
                result = INS_OPTS_4H;
                break;
            case EA_4BYTE:
                result = INS_OPTS_2S;
                break;
            case EA_8BYTE:
                result = INS_OPTS_1D;
                break;
            default:
                unreached();
                break;
        }
    }
    else if (datasize == EA_16BYTE)
    {
        switch (elemsize)
        {
            case EA_1BYTE:
                result = INS_OPTS_16B;
                break;
            case EA_2BYTE:
                result = INS_OPTS_8H;
                break;
            case EA_4BYTE:
                result = INS_OPTS_4S;
                break;
            case EA_8BYTE:
                result = INS_OPTS_2D;
                break;
            default:
                unreached();
                break;
        }
    }
    return result;
}

/*****************************************************************************
 *
 *  For the given 'datasize' and arrangement 'opts'
 *  returns true is the pair spcifies a valid arrangement
 */
/*static*/ bool emitter::isValidArrangement(emitAttr datasize, insOpts opt)
{
    if (datasize == EA_8BYTE)
    {
        if ((opt == INS_OPTS_8B) || (opt == INS_OPTS_4H) || (opt == INS_OPTS_2S) || (opt == INS_OPTS_1D))
        {
            return true;
        }
    }
    else if (datasize == EA_16BYTE)
    {
        if ((opt == INS_OPTS_16B) || (opt == INS_OPTS_8H) || (opt == INS_OPTS_4S) || (opt == INS_OPTS_2D))
        {
            return true;
        }
    }
    return false;
}

//  For the given 'arrangement' returns the 'datasize' specified by the vector register arrangement
//  asserts and returns EA_UNKNOWN if an invalid 'arrangement' value is passed
//
/*static*/ emitAttr emitter::optGetDatasize(insOpts arrangement)
{
    if ((arrangement == INS_OPTS_8B) || (arrangement == INS_OPTS_4H) || (arrangement == INS_OPTS_2S) ||
        (arrangement == INS_OPTS_1D))
    {
        return EA_8BYTE;
    }
    else if ((arrangement == INS_OPTS_16B) || (arrangement == INS_OPTS_8H) || (arrangement == INS_OPTS_4S) ||
             (arrangement == INS_OPTS_2D))
    {
        return EA_16BYTE;
    }
    else
    {
        assert(!" invalid 'arrangement' value");
        return EA_UNKNOWN;
    }
}

//  For the given 'arrangement' returns the 'elemsize' specified by the vector register arrangement
//  asserts and returns EA_UNKNOWN if an invalid 'arrangement' value is passed
//
/*static*/ emitAttr emitter::optGetElemsize(insOpts arrangement)
{
    if ((arrangement == INS_OPTS_8B) || (arrangement == INS_OPTS_16B))
    {
        return EA_1BYTE;
    }
    else if ((arrangement == INS_OPTS_4H) || (arrangement == INS_OPTS_8H))
    {
        return EA_2BYTE;
    }
    else if ((arrangement == INS_OPTS_2S) || (arrangement == INS_OPTS_4S))
    {
        return EA_4BYTE;
    }
    else if ((arrangement == INS_OPTS_1D) || (arrangement == INS_OPTS_2D))
    {
        return EA_8BYTE;
    }
    else
    {
        assert(!" invalid 'arrangement' value");
        return EA_UNKNOWN;
    }
}

//  For the given 'arrangement' returns the 'widen-arrangement' specified by the vector register arrangement
//  asserts and returns INS_OPTS_NONE if an invalid 'arrangement' value is passed
//
/*static*/ insOpts emitter::optWidenElemsize(insOpts arrangement)
{
    if ((arrangement == INS_OPTS_8B) || (arrangement == INS_OPTS_16B))
    {
        return INS_OPTS_8H;
    }
    else if ((arrangement == INS_OPTS_4H) || (arrangement == INS_OPTS_8H))
    {
        return INS_OPTS_4S;
    }
    else if ((arrangement == INS_OPTS_2S) || (arrangement == INS_OPTS_4S))
    {
        return INS_OPTS_2D;
    }
    else
    {
        assert(!" invalid 'arrangement' value");
        return INS_OPTS_NONE;
    }
}

//  For the given 'conversion' returns the 'dstsize' specified by the conversion option
/*static*/ emitAttr emitter::optGetDstsize(insOpts conversion)
{
    switch (conversion)
    {
        case INS_OPTS_S_TO_8BYTE:
        case INS_OPTS_D_TO_8BYTE:
        case INS_OPTS_4BYTE_TO_D:
        case INS_OPTS_8BYTE_TO_D:
        case INS_OPTS_S_TO_D:
        case INS_OPTS_H_TO_D:

            return EA_8BYTE;

        case INS_OPTS_S_TO_4BYTE:
        case INS_OPTS_D_TO_4BYTE:
        case INS_OPTS_4BYTE_TO_S:
        case INS_OPTS_8BYTE_TO_S:
        case INS_OPTS_D_TO_S:
        case INS_OPTS_H_TO_S:

            return EA_4BYTE;

        case INS_OPTS_S_TO_H:
        case INS_OPTS_D_TO_H:

            return EA_2BYTE;

        default:
            assert(!" invalid 'conversion' value");
            return EA_UNKNOWN;
    }
}

//  For the given 'conversion' returns the 'srcsize' specified by the conversion option
/*static*/ emitAttr emitter::optGetSrcsize(insOpts conversion)
{
    switch (conversion)
    {
        case INS_OPTS_D_TO_8BYTE:
        case INS_OPTS_D_TO_4BYTE:
        case INS_OPTS_8BYTE_TO_D:
        case INS_OPTS_8BYTE_TO_S:
        case INS_OPTS_D_TO_S:
        case INS_OPTS_D_TO_H:

            return EA_8BYTE;

        case INS_OPTS_S_TO_8BYTE:
        case INS_OPTS_S_TO_4BYTE:
        case INS_OPTS_4BYTE_TO_S:
        case INS_OPTS_4BYTE_TO_D:
        case INS_OPTS_S_TO_D:
        case INS_OPTS_S_TO_H:

            return EA_4BYTE;

        case INS_OPTS_H_TO_S:
        case INS_OPTS_H_TO_D:

            return EA_2BYTE;

        default:
            assert(!" invalid 'conversion' value");
            return EA_UNKNOWN;
    }
}

//    For the given 'size' and 'index' returns true if it specifies a valid index for a vector register of 'size'
/*static*/ bool emitter::isValidVectorIndex(emitAttr datasize, emitAttr elemsize, ssize_t index)
{
    assert(isValidVectorDatasize(datasize));
    assert(isValidVectorElemsize(elemsize));

    bool result = false;
    if (index >= 0)
    {
        if (datasize == EA_8BYTE)
        {
            switch (elemsize)
            {
                case EA_1BYTE:
                    result = (index < 8);
                    break;
                case EA_2BYTE:
                    result = (index < 4);
                    break;
                case EA_4BYTE:
                    result = (index < 2);
                    break;
                case EA_8BYTE:
                    result = (index < 1);
                    break;
                default:
                    unreached();
                    break;
            }
        }
        else if (datasize == EA_16BYTE)
        {
            switch (elemsize)
            {
                case EA_1BYTE:
                    result = (index < 16);
                    break;
                case EA_2BYTE:
                    result = (index < 8);
                    break;
                case EA_4BYTE:
                    result = (index < 4);
                    break;
                case EA_8BYTE:
                    result = (index < 2);
                    break;
                default:
                    unreached();
                    break;
            }
        }
    }
    return result;
}

/*****************************************************************************
 *
 *  Add an instruction with no operands.
 */

void emitter::emitIns(instruction ins)
{
    instrDesc* id  = emitNewInstrSmall(EA_8BYTE);
    insFormat  fmt = emitInsFormat(ins);

    assert(fmt == IF_SN_0A);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a single immediate value.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, ssize_t imm)
{
    insFormat fmt = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_brk:
            if ((imm & 0x0000ffff) == imm)
            {
                fmt = IF_SI_0A;
            }
            else
            {
                assert(!"Instruction cannot be encoded: IF_SI_0A");
            }
            break;
        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a single register.
 */

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    insFormat  fmt = IF_NONE;
    instrDesc* id  = nullptr;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_br:
        case INS_ret:
            assert(isGeneralRegister(reg));
            id = emitNewInstrSmall(attr);
            id->idReg1(reg);
            fmt = IF_BR_1A;
            break;

        default:
            unreached();
    }

    assert(fmt != IF_NONE);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    emitAttr  size      = EA_SIZE(attr);
    emitAttr  elemsize  = EA_UNKNOWN;
    insFormat fmt       = IF_NONE;
    bool      canEncode = false;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bitMaskImm     bmi;
        halfwordImm    hwi;
        byteShiftedImm bsi;
        ssize_t        notOfImm;

        case INS_tst:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg));
            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, size, &bmi);
            if (canEncode)
            {
                imm = bmi.immNRS;
                assert(isValidImmNRS(imm, size));
                fmt = IF_DI_1C;
            }
            break;

        case INS_movk:
        case INS_movn:
        case INS_movz:
            assert(isValidGeneralDatasize(size));
            assert(insOptsNone(opt)); // No LSL here (you must use emitIns_R_I_I if a shift is needed)
            assert(isGeneralRegister(reg));
            assert(isValidUimm16(imm));

            hwi.immHW  = 0;
            hwi.immVal = imm;
            assert(imm == emitDecodeHalfwordImm(hwi, size));

            imm       = hwi.immHWVal;
            canEncode = true;
            fmt       = IF_DI_1B;
            break;

        case INS_mov:
            assert(isValidGeneralDatasize(size));
            assert(insOptsNone(opt)); // No explicit LSL here
            // We will automatically determine the shift based upon the imm

            // First try the standard 'halfword immediate' imm(i16,hw)
            hwi.immHWVal = 0;
            canEncode    = canEncodeHalfwordImm(imm, size, &hwi);
            if (canEncode)
            {
                // uses a movz encoding
                assert(isGeneralRegister(reg));
                imm = hwi.immHWVal;
                assert(isValidImmHWVal(imm, size));
                fmt = IF_DI_1B;
                break;
            }

            // Next try the ones-complement form of 'halfword immediate' imm(i16,hw)
            notOfImm  = NOT_helper(imm, getBitWidth(size));
            canEncode = canEncodeHalfwordImm(notOfImm, size, &hwi);
            if (canEncode)
            {
                assert(isGeneralRegister(reg));
                imm = hwi.immHWVal;
                ins = INS_movn; // uses a movn encoding
                assert(isValidImmHWVal(imm, size));
                fmt = IF_DI_1B;
                break;
            }

            // Finally try the 'bitmask immediate' imm(N,r,s)
            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, size, &bmi);
            if (canEncode)
            {
                assert(isGeneralRegisterOrSP(reg));
                reg = encodingSPtoZR(reg);
                imm = bmi.immNRS;
                assert(isValidImmNRS(imm, size));
                fmt = IF_DI_1D;
                break;
            }
            else
            {
                assert(!"Instruction cannot be encoded: mov imm");
            }

            break;

        case INS_movi:
            assert(isValidVectorDatasize(size));
            assert(isVectorRegister(reg));
            if (insOptsNone(opt) && (size == EA_8BYTE))
            {
                opt = INS_OPTS_1D;
            }
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);

            if (elemsize == EA_8BYTE)
            {
                size_t   uimm = imm;
                ssize_t  imm8 = 0;
                unsigned pos  = 0;
                canEncode     = true;
                while (uimm != 0)
                {
                    INT64 loByte = uimm & 0xFF;
                    if (((loByte == 0) || (loByte == 0xFF)) && (pos < 8))
                    {
                        if (loByte == 0xFF)
                        {
                            imm8 |= (ssize_t{1} << pos);
                        }
                        uimm >>= 8;
                        pos++;
                    }
                    else
                    {
                        canEncode = false;
                        break;
                    }
                }
                imm = imm8;
                assert(isValidUimm8(imm));
                fmt = IF_DV_1B;
                break;
            }
            else
            {
                // Vector operation

                // No explicit LSL/MSL is used for the immediate
                // We will automatically determine the shift based upon the value of imm

                // First try the standard 'byteShifted immediate' imm(i8,bySh)
                bsi.immBSVal = 0;
                canEncode    = canEncodeByteShiftedImm(imm, elemsize, true, &bsi);
                if (canEncode)
                {
                    imm = bsi.immBSVal;
                    assert(isValidImmBSVal(imm, size));
                    fmt = IF_DV_1B;
                    break;
                }

                // Next try the ones-complement form of the 'immediate' imm(i8,bySh)
                if ((elemsize == EA_2BYTE) || (elemsize == EA_4BYTE)) // Only EA_2BYTE or EA_4BYTE forms
                {
                    notOfImm  = NOT_helper(imm, getBitWidth(elemsize));
                    canEncode = canEncodeByteShiftedImm(notOfImm, elemsize, true, &bsi);
                    if (canEncode)
                    {
                        imm = bsi.immBSVal;
                        ins = INS_mvni; // uses a mvni encoding
                        assert(isValidImmBSVal(imm, size));
                        fmt = IF_DV_1B;
                        break;
                    }
                }
            }
            break;

        case INS_orr:
        case INS_bic:
        case INS_mvni:
            assert(isValidVectorDatasize(size));
            assert(isVectorRegister(reg));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert((elemsize == EA_2BYTE) || (elemsize == EA_4BYTE)); // Only EA_2BYTE or EA_4BYTE forms

            // Vector operation

            // No explicit LSL/MSL is used for the immediate
            // We will automatically determine the shift based upon the value of imm

            // First try the standard 'byteShifted immediate' imm(i8,bySh)
            bsi.immBSVal = 0;
            canEncode    = canEncodeByteShiftedImm(imm, elemsize,
                                                (ins == INS_mvni), // mvni supports the ones shifting variant (aka MSL)
                                                &bsi);
            if (canEncode)
            {
                imm = bsi.immBSVal;
                assert(isValidImmBSVal(imm, size));
                fmt = IF_DV_1B;
                break;
            }
            break;

        case INS_cmp:
        case INS_cmn:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg));

            if (unsigned_abs(imm) <= 0x0fff)
            {
                if (imm < 0)
                {
                    ins = insReverse(ins);
                    imm = -imm;
                }
                assert(isValidUimm12(imm));
                canEncode = true;
                fmt       = IF_DI_1A;
            }
            else if (canEncodeWithShiftImmBy12(imm)) // Try the shifted by 12 encoding
            {
                // Encoding will use a 12-bit left shift of the immediate
                opt = INS_OPTS_LSL12;
                if (imm < 0)
                {
                    ins = insReverse(ins);
                    imm = -imm;
                }
                assert((imm & 0xfff) == 0);
                imm >>= 12;
                assert(isValidUimm12(imm));
                canEncode = true;
                fmt       = IF_DI_1A;
            }
            else
            {
                assert(!"Instruction cannot be encoded: IF_DI_1A");
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(canEncode);
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a floating point constant.
 */

void emitter::emitIns_R_F(
    instruction ins, emitAttr attr, regNumber reg, double immDbl, insOpts opt /* = INS_OPTS_NONE */)

{
    emitAttr  size      = EA_SIZE(attr);
    emitAttr  elemsize  = EA_UNKNOWN;
    insFormat fmt       = IF_NONE;
    ssize_t   imm       = 0;
    bool      canEncode = false;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        floatImm8 fpi;

        case INS_fcmp:
        case INS_fcmpe:
            assert(insOptsNone(opt));
            assert(isValidVectorElemsizeFloat(size));
            assert(isVectorRegister(reg));
            if (immDbl == 0.0)
            {
                canEncode = true;
                fmt       = IF_DV_1C;
            }
            break;

        case INS_fmov:
            assert(isVectorRegister(reg));
            fpi.immFPIVal = 0;
            canEncode     = canEncodeFloatImm8(immDbl, &fpi);

            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding

                if (canEncode)
                {
                    imm = fpi.immFPIVal;
                    assert((imm >= 0) && (imm <= 0xff));
                    fmt = IF_DV_1B;
                }
            }
            else
            {
                // Scalar operation
                assert(insOptsNone(opt));
                assert(isValidVectorElemsizeFloat(size));

                if (canEncode)
                {
                    imm = fpi.immFPIVal;
                    assert((imm >= 0) && (imm <= 0xff));
                    fmt = IF_DV_1A;
                }
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(canEncode);
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt /* = INS_OPTS_NONE */)
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_mov:
            assert(insOptsNone(opt));
            // Is the mov even necessary?
            if (reg1 == reg2)
            {
                // A mov with a EA_4BYTE has the side-effect of clearing the upper bits
                // So only eliminate mov instructions that are not clearing the upper bits
                //
                if (isGeneralRegisterOrSP(reg1) && (size == EA_8BYTE))
                {
                    return;
                }
                else if (isVectorRegister(reg1) && (size == EA_16BYTE))
                {
                    return;
                }
            }

            // Check for the 'mov' aliases for the vector registers
            if (isVectorRegister(reg1))
            {
                if (isVectorRegister(reg2) && isValidVectorDatasize(size))
                {
                    return emitIns_R_R_R(INS_mov, size, reg1, reg2, reg2);
                }
                else
                {
                    return emitIns_R_R_I(INS_mov, size, reg1, reg2, 0);
                }
            }
            else
            {
                if (isVectorRegister(reg2))
                {
                    assert(isGeneralRegister(reg1));
                    return emitIns_R_R_I(INS_mov, size, reg1, reg2, 0);
                }
            }

            // Is this a MOV to/from SP instruction?
            if ((reg1 == REG_SP) || (reg2 == REG_SP))
            {
                assert(isGeneralRegisterOrSP(reg1));
                assert(isGeneralRegisterOrSP(reg2));
                reg1 = encodingSPtoZR(reg1);
                reg2 = encodingSPtoZR(reg2);
                fmt  = IF_DR_2G;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isGeneralRegister(reg1));
                assert(isGeneralRegisterOrZR(reg2));
                fmt = IF_DR_2E;
            }
            break;

        case INS_dup:
            // Vector operation
            assert(insOptsAnyArrangement(opt));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            fmt = IF_DV_2C;
            break;

        case INS_abs:
        case INS_not:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            if (ins == INS_not)
            {
                assert(isValidVectorDatasize(size));
                // Bitwise behavior is independent of element size, but is always encoded as 1 Byte
                opt = optMakeArrangement(size, EA_1BYTE);
            }
            if (insOptsNone(opt))
            {
                // Scalar operation
                assert(size == EA_8BYTE); // Only type D is supported
                fmt = IF_DV_2L;
            }
            else
            {
                // Vector operation
                assert(insOptsAnyArrangement(opt));
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                fmt      = IF_DV_2M;
            }
            break;

        case INS_mvn:
        case INS_neg:
            if (isVectorRegister(reg1))
            {
                assert(isVectorRegister(reg2));
                if (ins == INS_mvn)
                {
                    assert(isValidVectorDatasize(size));
                    // Bitwise behavior is independent of element size, but is always encoded as 1 Byte
                    opt = optMakeArrangement(size, EA_1BYTE);
                }
                if (insOptsNone(opt))
                {
                    // Scalar operation
                    assert(size == EA_8BYTE); // Only type D is supported
                    fmt = IF_DV_2L;
                }
                else
                {
                    // Vector operation
                    assert(isValidVectorDatasize(size));
                    assert(isValidArrangement(size, opt));
                    elemsize = optGetElemsize(opt);
                    fmt      = IF_DV_2M;
                }
                break;
            }
            __fallthrough;

        case INS_negs:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            fmt = IF_DR_2E;
            break;

        case INS_sxtw:
            assert(size == EA_8BYTE);
            __fallthrough;

        case INS_sxtb:
        case INS_sxth:
        case INS_uxtb:
        case INS_uxth:
            assert(insOptsNone(opt));
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            fmt = IF_DR_2H;
            break;

        case INS_sxtl:
        case INS_sxtl2:
        case INS_uxtl:
        case INS_uxtl2:
            return emitIns_R_R_I(ins, size, reg1, reg2, 0, opt);

        case INS_cls:
        case INS_clz:
        case INS_rbit:
        case INS_rev16:
        case INS_rev32:
        case INS_cnt:
            if (isVectorRegister(reg1))
            {
                assert(isVectorRegister(reg2));
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                if ((ins == INS_cls) || (ins == INS_clz))
                {
                    assert(elemsize != EA_8BYTE); // No encoding for type D
                }
                else if (ins == INS_rev32)
                {
                    assert((elemsize == EA_2BYTE) || (elemsize == EA_1BYTE));
                }
                else
                {
                    assert(elemsize == EA_1BYTE); // Only supports 8B or 16B
                }
                fmt = IF_DV_2M;
                break;
            }
            if (ins == INS_cnt)
            {
                // Doesn't have general register version(s)
                break;
            }

            __fallthrough;

        case INS_rev:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            if (ins == INS_rev32)
            {
                assert(size == EA_8BYTE);
            }
            else
            {
                assert(isValidGeneralDatasize(size));
            }
            fmt = IF_DR_2G;
            break;

        case INS_addv:
        case INS_saddlv:
        case INS_smaxv:
        case INS_sminv:
        case INS_uaddlv:
        case INS_umaxv:
        case INS_uminv:
        case INS_rev64:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(elemsize != EA_8BYTE); // No encoding for type D
            fmt = IF_DV_2M;
            break;

        case INS_xtn:
        case INS_xtn2:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            // size is determined by instruction
            if (ins == INS_xtn)
            {
                assert(size == EA_8BYTE);
            }
            else // ins == INS_xtn2
            {
                assert(size == EA_16BYTE);
            }
            assert(elemsize != EA_8BYTE); // Narrowing must not end with 8 byte data
            fmt = IF_DV_2M;
            break;

        case INS_ldar:
        case INS_ldaxr:
        case INS_ldxr:
        case INS_stlr:
            assert(isValidGeneralDatasize(size));

            __fallthrough;

        case INS_ldarb:
        case INS_ldaxrb:
        case INS_ldxrb:
        case INS_ldarh:
        case INS_ldaxrh:
        case INS_ldxrh:
        case INS_stlrb:
        case INS_stlrh:
            assert(isValidGeneralLSDatasize(size));
            assert(isGeneralRegisterOrZR(reg1));
            assert(isGeneralRegisterOrSP(reg2));
            assert(insOptsNone(opt));

            reg2 = encodingSPtoZR(reg2);

            fmt = IF_LS_2A;
            break;

        case INS_ldr:
        case INS_ldrb:
        case INS_ldrh:
        case INS_ldrsb:
        case INS_ldrsh:
        case INS_ldrsw:
        case INS_str:
        case INS_strb:
        case INS_strh:

        case INS_cmp:
        case INS_cmn:
        case INS_tst:
            assert(insOptsNone(opt));
            emitIns_R_R_I(ins, attr, reg1, reg2, 0, INS_OPTS_NONE);
            return;

        case INS_staddb:
            emitIns_R_R_R(INS_ldaddb, attr, reg1, REG_ZR, reg2);
            return;
        case INS_staddlb:
            emitIns_R_R_R(INS_ldaddlb, attr, reg1, REG_ZR, reg2);
            return;
        case INS_staddh:
            emitIns_R_R_R(INS_ldaddh, attr, reg1, REG_ZR, reg2);
            return;
        case INS_staddlh:
            emitIns_R_R_R(INS_ldaddlh, attr, reg1, REG_ZR, reg2);
            return;
        case INS_stadd:
            emitIns_R_R_R(INS_ldadd, attr, reg1, REG_ZR, reg2);
            return;
        case INS_staddl:
            emitIns_R_R_R(INS_ldaddl, attr, reg1, REG_ZR, reg2);
            return;

        case INS_fmov:
            assert(isValidVectorElemsizeFloat(size));

            // Is the mov even necessary?
            if (reg1 == reg2)
            {
                return;
            }

            if (isVectorRegister(reg1))
            {
                if (isVectorRegister(reg2))
                {
                    assert(insOptsNone(opt));
                    fmt = IF_DV_2G;
                }
                else
                {
                    assert(isGeneralRegister(reg2));

                    // if the optional conversion specifier is not present we calculate it
                    if (opt == INS_OPTS_NONE)
                    {
                        opt = (size == EA_4BYTE) ? INS_OPTS_4BYTE_TO_S : INS_OPTS_8BYTE_TO_D;
                    }
                    assert(insOptsConvertIntToFloat(opt));

                    fmt = IF_DV_2I;
                }
            }
            else
            {
                assert(isGeneralRegister(reg1));
                assert(isVectorRegister(reg2));

                // if the optional conversion specifier is not present we calculate it
                if (opt == INS_OPTS_NONE)
                {
                    opt = (size == EA_4BYTE) ? INS_OPTS_S_TO_4BYTE : INS_OPTS_D_TO_8BYTE;
                }
                assert(insOptsConvertFloatToInt(opt));

                fmt = IF_DV_2H;
            }
            break;

        case INS_fcmp:
        case INS_fcmpe:
            assert(insOptsNone(opt));
            assert(isValidVectorElemsizeFloat(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            fmt = IF_DV_2K;
            break;

        case INS_fcvtns:
        case INS_fcvtnu:
        case INS_fcvtas:
        case INS_fcvtau:
        case INS_fcvtps:
        case INS_fcvtpu:
        case INS_fcvtms:
        case INS_fcvtmu:
        case INS_fcvtzs:
        case INS_fcvtzu:
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_2A;
            }
            else
            {
                // Scalar operation
                assert(isVectorRegister(reg2));
                if (isVectorRegister(reg1))
                {
                    assert(insOptsNone(opt));
                    assert(isValidVectorElemsizeFloat(size));
                    fmt = IF_DV_2G;
                }
                else
                {
                    assert(isGeneralRegister(reg1));
                    assert(insOptsConvertFloatToInt(opt));
                    assert(isValidVectorElemsizeFloat(size));
                    fmt = IF_DV_2H;
                }
            }
            break;

        case INS_fcvtl:
        case INS_fcvtl2:
        case INS_fcvtn:
        case INS_fcvtn2:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidVectorDatasize(size));
            assert(insOptsNone(opt));
            assert(size == EA_8BYTE); // Narrowing from Double or Widening to Double (Half not supported)
            fmt = IF_DV_2G;
            break;

        case INS_scvtf:
        case INS_ucvtf:
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_2A;
            }
            else
            {
                // Scalar operation
                assert(isVectorRegister(reg1));
                if (isVectorRegister(reg2))
                {
                    assert(insOptsNone(opt));
                    assert(isValidVectorElemsizeFloat(size));
                    fmt = IF_DV_2G;
                }
                else
                {
                    assert(isGeneralRegister(reg2));
                    assert(insOptsConvertIntToFloat(opt));
                    assert(isValidVectorElemsizeFloat(size));
                    fmt = IF_DV_2I;
                }
            }
            break;

        case INS_fabs:
        case INS_fneg:
        case INS_fsqrt:
        case INS_frinta:
        case INS_frinti:
        case INS_frintm:
        case INS_frintn:
        case INS_frintp:
        case INS_frintx:
        case INS_frintz:
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_2A;
            }
            else
            {
                // Scalar operation
                assert(insOptsNone(opt));
                assert(isValidVectorElemsizeFloat(size));
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                fmt = IF_DV_2G;
            }
            break;

        case INS_faddp:
            // Scalar operation
            assert(insOptsNone(opt));
            assert(isValidVectorElemsizeFloat(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            fmt = IF_DV_2G;
            break;

        case INS_fcvt:
            assert(insOptsConvertFloatToFloat(opt));
            assert(isValidVectorFcvtsize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            fmt = IF_DV_2J;
            break;

        case INS_cmeq:
        case INS_cmge:
        case INS_cmgt:
        case INS_cmle:
        case INS_cmlt:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));

            if (isValidVectorDatasize(size))
            {
                // Vector operation
                assert(insOptsAnyArrangement(opt));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                fmt      = IF_DV_2M;
            }
            else
            {
                NYI("Untested");
                // Scalar operation
                assert(size == EA_8BYTE); // Only Double supported
                fmt = IF_DV_2L;
            }
            break;

        case INS_fcmeq:
        case INS_fcmge:
        case INS_fcmgt:
        case INS_fcmle:
        case INS_fcmlt:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));

            if (isValidVectorDatasize(size))
            {
                // Vector operation
                assert(insOptsAnyArrangement(opt));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert((elemsize == EA_8BYTE) || (elemsize == EA_4BYTE)); // Only Double/Float supported
                assert(opt != INS_OPTS_1D);                               // Reserved encoding
                fmt = IF_DV_2A;
            }
            else
            {
                NYI("Untested");
                // Scalar operation
                assert((size == EA_8BYTE) || (size == EA_4BYTE)); // Only Double/Float supported
                fmt = IF_DV_2G;
            }
            break;
        case INS_aesd:
        case INS_aese:
        case INS_aesmc:
        case INS_aesimc:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidVectorDatasize(size));
            elemsize = optGetElemsize(opt);
            assert(elemsize == EA_1BYTE);
            fmt = IF_DV_2P;
            break;

        case INS_sha1h:
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            fmt = IF_DR_2J;
            break;

        case INS_sha256su0:
        case INS_sha1su1:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidVectorDatasize(size));
            elemsize = optGetElemsize(opt);
            assert(elemsize == EA_4BYTE);
            fmt = IF_DV_2P;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSmall(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

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
    instruction ins, emitAttr attr, regNumber reg, ssize_t imm1, ssize_t imm2, insOpts opt /* = INS_OPTS_NONE */)
{
    emitAttr  size   = EA_SIZE(attr);
    insFormat fmt    = IF_NONE;
    size_t    immOut = 0; // composed from imm1 and imm2 and stored in the instrDesc

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bool        canEncode;
        halfwordImm hwi;

        case INS_mov:
            ins = INS_movz; // INS_mov with LSL is an alias for INS_movz LSL
            __fallthrough;

        case INS_movk:
        case INS_movn:
        case INS_movz:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg));
            assert(isValidUimm16(imm1));
            assert(insOptsLSL(opt)); // Must be INS_OPTS_LSL

            if (size == EA_8BYTE)
            {
                assert((imm2 == 0) || (imm2 == 16) || // shift amount: 0, 16, 32 or 48
                       (imm2 == 32) || (imm2 == 48));
            }
            else // EA_4BYTE
            {
                assert((imm2 == 0) || (imm2 == 16)); // shift amount: 0 or 16
            }

            hwi.immHWVal = 0;

            switch (imm2)
            {
                case 0:
                    hwi.immHW = 0;
                    canEncode = true;
                    break;

                case 16:
                    hwi.immHW = 1;
                    canEncode = true;
                    break;

                case 32:
                    hwi.immHW = 2;
                    canEncode = true;
                    break;

                case 48:
                    hwi.immHW = 3;
                    canEncode = true;
                    break;

                default:
                    canEncode = false;
            }

            if (canEncode)
            {
                hwi.immVal = imm1;

                immOut = hwi.immHWVal;
                assert(isValidImmHWVal(immOut, size));
                fmt = IF_DI_1B;
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, immOut);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    emitAttr  size       = EA_SIZE(attr);
    emitAttr  elemsize   = EA_UNKNOWN;
    insFormat fmt        = IF_NONE;
    bool      isLdSt     = false;
    bool      isSIMD     = false;
    bool      isAddSub   = false;
    bool      setFlags   = false;
    unsigned  scale      = 0;
    bool      unscaledOp = false;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bool       canEncode;
        bitMaskImm bmi;

        case INS_mov:
            // Check for the 'mov' aliases for the vector registers
            assert(insOptsNone(opt));
            assert(isValidVectorElemsize(size));
            elemsize = size;
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));

            if (isVectorRegister(reg1))
            {
                if (isGeneralRegisterOrZR(reg2))
                {
                    fmt = IF_DV_2C; // Alias for 'ins'
                    break;
                }
                else if (isVectorRegister(reg2))
                {
                    fmt = IF_DV_2E; // Alias for 'dup'
                    break;
                }
            }
            else // isGeneralRegister(reg1)
            {
                assert(isGeneralRegister(reg1));
                if (isVectorRegister(reg2))
                {
                    fmt = IF_DV_2B; // Alias for 'umov'
                    break;
                }
            }
            assert(!" invalid INS_mov operands");
            break;

        case INS_lsl:
        case INS_lsr:
        case INS_asr:
            assert(insOptsNone(opt));
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isValidImmShift(imm, size));
            fmt = IF_DI_2D;
            break;

        case INS_ror:
            assert(insOptsNone(opt));
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isValidImmShift(imm, size));
            fmt = IF_DI_2B;
            break;

        case INS_sshr:
        case INS_ssra:
        case INS_srshr:
        case INS_srsra:
        case INS_shl:
        case INS_ushr:
        case INS_usra:
        case INS_urshr:
        case INS_ursra:
        case INS_sri:
        case INS_sli:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsize(elemsize));
                assert(isValidImmShift(imm, elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_2O;
                break;
            }
            else
            {
                // Scalar operation
                assert(insOptsNone(opt));
                assert(size == EA_8BYTE); // only supported size
                assert(isValidImmShift(imm, size));
                fmt = IF_DV_2N;
            }
            break;

        case INS_sxtl:
        case INS_uxtl:
            assert(imm == 0);
            __fallthrough;

        case INS_shrn:
        case INS_rshrn:
        case INS_sshll:
        case INS_ushll:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            // Vector operation
            assert(size == EA_8BYTE);
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(elemsize != EA_8BYTE); // Reserved encodings
            assert(isValidVectorElemsize(elemsize));
            assert(isValidImmShift(imm, elemsize));
            fmt = IF_DV_2O;
            break;

        case INS_sxtl2:
        case INS_uxtl2:
            assert(imm == 0);
            __fallthrough;

        case INS_shrn2:
        case INS_rshrn2:
        case INS_sshll2:
        case INS_ushll2:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            // Vector operation
            assert(size == EA_16BYTE);
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(elemsize != EA_8BYTE); // Reserved encodings
            assert(isValidVectorElemsize(elemsize));
            assert(isValidImmShift(imm, elemsize));
            fmt = IF_DV_2O;
            break;

        case INS_mvn:
        case INS_neg:
        case INS_negs:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));

            if (imm == 0)
            {
                assert(insOptsNone(opt)); // a zero imm, means no alu shift kind

                fmt = IF_DR_2E;
            }
            else
            {
                if (ins == INS_mvn)
                {
                    assert(insOptsAnyShift(opt)); // a non-zero imm, must select shift kind
                }
                else // neg or negs
                {
                    assert(insOptsAluShift(opt)); // a non-zero imm, must select shift kind, can't use ROR
                }
                assert(isValidImmShift(imm, size));
                fmt = IF_DR_2F;
            }
            break;

        case INS_tst:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegisterOrZR(reg1));
            assert(isGeneralRegister(reg2));

            if (insOptsAnyShift(opt))
            {
                assert(isValidImmShift(imm, size) && (imm != 0));
                fmt = IF_DR_2B;
            }
            else
            {
                assert(insOptsNone(opt)); // a zero imm, means no alu shift kind
                assert(imm == 0);
                fmt = IF_DR_2A;
            }
            break;

        case INS_cmp:
        case INS_cmn:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegisterOrSP(reg1));
            assert(isGeneralRegister(reg2));

            reg1 = encodingSPtoZR(reg1);
            if (insOptsAnyExtend(opt))
            {
                assert((imm >= 0) && (imm <= 4));

                fmt = IF_DR_2C;
            }
            else if (imm == 0)
            {
                assert(insOptsNone(opt)); // a zero imm, means no alu shift kind

                fmt = IF_DR_2A;
            }
            else
            {
                assert(insOptsAnyShift(opt)); // a non-zero imm, must select shift kind
                assert(isValidImmShift(imm, size));
                fmt = IF_DR_2B;
            }
            break;

        case INS_ands:
        case INS_and:
        case INS_eor:
        case INS_orr:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg2));
            if (ins == INS_ands)
            {
                assert(isGeneralRegister(reg1));
            }
            else
            {
                assert(isGeneralRegisterOrSP(reg1));
                reg1 = encodingSPtoZR(reg1);
            }

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, size, &bmi);
            if (canEncode)
            {
                imm = bmi.immNRS;
                assert(isValidImmNRS(imm, size));
                fmt = IF_DI_2C;
            }
            break;

        case INS_dup: // by element, imm selects the element of reg2
            assert(isVectorRegister(reg1));
            if (isVectorRegister(reg2))
            {
                if (insOptsAnyArrangement(opt))
                {
                    // Vector operation
                    assert(isValidVectorDatasize(size));
                    assert(isValidArrangement(size, opt));
                    elemsize = optGetElemsize(opt);
                    assert(isValidVectorElemsize(elemsize));
                    assert(isValidVectorIndex(size, elemsize, imm));
                    assert(opt != INS_OPTS_1D); // Reserved encoding
                    fmt = IF_DV_2D;
                    break;
                }
                else
                {
                    // Scalar operation
                    assert(insOptsNone(opt));
                    elemsize = size;
                    assert(isValidVectorElemsize(elemsize));
                    assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
                    fmt = IF_DV_2E;
                    break;
                }
            }
            __fallthrough;

        case INS_ins: // (MOV from general)
            assert(insOptsNone(opt));
            assert(isValidVectorElemsize(size));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            elemsize = size;
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            fmt = IF_DV_2C;
            break;

        case INS_umov: // (MOV to general)
            assert(insOptsNone(opt));
            assert(isValidVectorElemsize(size));
            assert(isGeneralRegister(reg1));
            assert(isVectorRegister(reg2));
            elemsize = size;
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            fmt = IF_DV_2B;
            break;

        case INS_smov:
            assert(insOptsNone(opt));
            assert(isValidVectorElemsize(size));
            assert(size != EA_8BYTE); // no encoding, use INS_umov
            assert(isGeneralRegister(reg1));
            assert(isVectorRegister(reg2));
            elemsize = size;
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            fmt = IF_DV_2B;
            break;

        case INS_add:
        case INS_sub:
            setFlags = false;
            isAddSub = true;
            break;

        case INS_adds:
        case INS_subs:
            setFlags = true;
            isAddSub = true;
            break;

        case INS_ldrsb:
        case INS_ldursb:
            // 'size' specifies how we sign-extend into 4 or 8 bytes of the target register
            assert(isValidGeneralDatasize(size));
            unscaledOp = (ins == INS_ldursb);
            scale      = 0;
            isLdSt     = true;
            break;

        case INS_ldrsh:
        case INS_ldursh:
            // 'size' specifies how we sign-extend into 4 or 8 bytes of the target register
            assert(isValidGeneralDatasize(size));
            unscaledOp = (ins == INS_ldursh);
            scale      = 1;
            isLdSt     = true;
            break;

        case INS_ldrsw:
        case INS_ldursw:
            // 'size' specifies how we sign-extend into 4 or 8 bytes of the target register
            assert(size == EA_8BYTE);
            unscaledOp = (ins == INS_ldursw);
            scale      = 2;
            isLdSt     = true;
            break;

        case INS_ldrb:
        case INS_strb:
            // size is ignored
            unscaledOp = false;
            scale      = 0;
            isLdSt     = true;
            break;

        case INS_ldurb:
        case INS_sturb:
            // size is ignored
            unscaledOp = true;
            scale      = 0;
            isLdSt     = true;
            break;

        case INS_ldrh:
        case INS_strh:
            // size is ignored
            unscaledOp = false;
            scale      = 1;
            isLdSt     = true;
            break;

        case INS_ldurh:
        case INS_sturh:
            // size is ignored
            unscaledOp = true;
            scale      = 0;
            isLdSt     = true;
            break;

        case INS_ldr:
        case INS_str:
            // Is the target a vector register?
            if (isVectorRegister(reg1))
            {
                assert(isValidVectorLSDatasize(size));
                assert(isGeneralRegisterOrSP(reg2));
                isSIMD = true;
            }
            else
            {
                assert(isValidGeneralDatasize(size));
            }
            unscaledOp = false;
            scale      = NaturalScale_helper(size);
            isLdSt     = true;
            break;

        case INS_ldur:
        case INS_stur:
            // Is the target a vector register?
            if (isVectorRegister(reg1))
            {
                assert(isValidVectorLSDatasize(size));
                assert(isGeneralRegisterOrSP(reg2));
                isSIMD = true;
            }
            else
            {
                assert(isValidGeneralDatasize(size));
            }
            unscaledOp = true;
            scale      = 0;
            isLdSt     = true;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    if (isLdSt)
    {
        assert(!isAddSub);

        if (isSIMD)
        {
            assert(isValidVectorLSDatasize(size));
            assert(isVectorRegister(reg1));
            assert((scale >= 0) && (scale <= 4));
        }
        else
        {
            assert(isValidGeneralLSDatasize(size));
            assert(isGeneralRegisterOrZR(reg1));
            assert((scale >= 0) && (scale <= 3));
        }

        assert(isGeneralRegisterOrSP(reg2));

        // Load/Store reserved encodings:
        if (insOptsIndexed(opt))
        {
            assert(reg1 != reg2);
        }

        reg2 = encodingSPtoZR(reg2);

        ssize_t mask = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate
        if (imm == 0)
        {
            assert(insOptsNone(opt)); // PRE/POST Index doesn't make sense with an immediate of zero

            fmt = IF_LS_2A;
        }
        else if (insOptsIndexed(opt) || unscaledOp || (imm < 0) || ((imm & mask) != 0))
        {
            if ((imm >= -256) && (imm <= 255))
            {
                fmt = IF_LS_2C;
            }
            else
            {
                assert(!"Instruction cannot be encoded: IF_LS_2C");
            }
        }
        else if (imm > 0)
        {
            assert(insOptsNone(opt));
            assert(!unscaledOp);

            if (((imm & mask) == 0) && ((imm >> scale) < 0x1000))
            {
                imm >>= scale; // The immediate is scaled by the size of the ld/st

                fmt = IF_LS_2B;
            }
            else
            {
                assert(!"Instruction cannot be encoded: IF_LS_2B");
            }
        }
    }
    else if (isAddSub)
    {
        assert(!isLdSt);
        assert(insOptsNone(opt));

        if (setFlags) // Can't encode SP with setFlags
        {
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
        }
        else
        {
            assert(isGeneralRegisterOrSP(reg1));
            assert(isGeneralRegisterOrSP(reg2));

            // Is it just a mov?
            if (imm == 0)
            {
                // Is the mov even necessary?
                if (reg1 != reg2)
                {
                    emitIns_R_R(INS_mov, attr, reg1, reg2);
                }
                return;
            }

            reg1 = encodingSPtoZR(reg1);
            reg2 = encodingSPtoZR(reg2);
        }

        if (unsigned_abs(imm) <= 0x0fff)
        {
            if (imm < 0)
            {
                ins = insReverse(ins);
                imm = -imm;
            }
            assert(isValidUimm12(imm));
            fmt = IF_DI_2A;
        }
        else if (canEncodeWithShiftImmBy12(imm)) // Try the shifted by 12 encoding
        {
            // Encoding will use a 12-bit left shift of the immediate
            opt = INS_OPTS_LSL12;
            if (imm < 0)
            {
                ins = insReverse(ins);
                imm = -imm;
            }
            assert((imm & 0xfff) == 0);
            imm >>= 12;
            assert(isValidUimm12(imm));
            fmt = IF_DI_2A;
        }
        else
        {
            assert(!"Instruction cannot be encoded: IF_DI_2A");
        }
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
*
*  Add an instruction referencing two registers and a constant.
*  Also checks for a large immediate that needs a second instruction
*  and will load it in reg1
*
*  - Supports instructions: add, adds, sub, subs, and, ands, eor and orr
*  - Requires that reg1 is a general register and not SP or ZR
*  - Requires that reg1 != reg2
*/
void emitter::emitIns_R_R_Imm(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm)
{
    assert(isGeneralRegister(reg1));
    assert(reg1 != reg2);

    bool immFits = true;

    switch (ins)
    {
        case INS_add:
        case INS_adds:
        case INS_sub:
        case INS_subs:
            immFits = emitter::emitIns_valid_imm_for_add(imm, attr);
            break;

        case INS_ands:
        case INS_and:
        case INS_eor:
        case INS_orr:
            immFits = emitter::emitIns_valid_imm_for_alu(imm, attr);
            break;

        default:
            assert(!"Unsupported instruction in emitIns_R_R_Imm");
    }

    if (immFits)
    {
        emitIns_R_R_I(ins, attr, reg1, reg2, imm);
    }
    else
    {
        // Load 'imm' into the reg1 register
        // then issue:   'ins'  reg1, reg2, reg1
        //
        codeGen->instGen_Set_Reg_To_Imm(attr, reg1, imm);
        emitIns_R_R_R(ins, attr, reg1, reg2, reg1);
    }
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt) /* = INS_OPTS_NONE */
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_lsl:
        case INS_lsr:
        case INS_asr:
        case INS_ror:
        case INS_adc:
        case INS_adcs:
        case INS_sbc:
        case INS_sbcs:
        case INS_udiv:
        case INS_sdiv:
        case INS_mneg:
        case INS_smull:
        case INS_smnegl:
        case INS_smulh:
        case INS_umull:
        case INS_umnegl:
        case INS_umulh:
        case INS_lslv:
        case INS_lsrv:
        case INS_asrv:
        case INS_rorv:
            assert(insOptsNone(opt));
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            fmt = IF_DR_3A;
            break;

        case INS_mul:
            if (insOptsNone(opt))
            {
                // general register
                assert(isValidGeneralDatasize(size));
                assert(isGeneralRegister(reg1));
                assert(isGeneralRegister(reg2));
                assert(isGeneralRegister(reg3));
                fmt = IF_DR_3A;
                break;
            }
            __fallthrough;

        case INS_mla:
        case INS_mls:
        case INS_pmul:
            assert(insOptsAnyArrangement(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            if (ins == INS_pmul)
            {
                assert(elemsize == EA_1BYTE); // only supports 8B or 16B
            }
            else // INS_mul, INS_mla, INS_mls
            {
                assert(elemsize != EA_8BYTE); // can't use 2D or 1D
            }
            fmt = IF_DV_3A;
            break;

        case INS_add:
        case INS_sub:
            if (isVectorRegister(reg1))
            {
                assert(isVectorRegister(reg2));
                assert(isVectorRegister(reg3));

                if (insOptsAnyArrangement(opt))
                {
                    // Vector operation
                    assert(opt != INS_OPTS_1D); // Reserved encoding
                    assert(isValidVectorDatasize(size));
                    assert(isValidArrangement(size, opt));
                    fmt = IF_DV_3A;
                }
                else
                {
                    // Scalar operation
                    assert(insOptsNone(opt));
                    assert(size == EA_8BYTE);
                    fmt = IF_DV_3E;
                }
                break;
            }
            __fallthrough;

        case INS_adds:
        case INS_subs:
            emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0, INS_OPTS_NONE);
            return;

        case INS_cmeq:
        case INS_cmge:
        case INS_cmgt:
        case INS_cmhi:
        case INS_cmhs:
        case INS_ctst:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));

            if (isValidVectorDatasize(size))
            {
                // Vector operation
                assert(insOptsAnyArrangement(opt));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                fmt      = IF_DV_3A;
            }
            else
            {
                NYI("Untested");
                // Scalar operation
                assert(size == EA_8BYTE); // Only Double supported
                fmt = IF_DV_3E;
            }
            break;

        case INS_fcmeq:
        case INS_fcmge:
        case INS_fcmgt:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));

            if (isValidVectorDatasize(size))
            {
                // Vector operation
                assert(insOptsAnyArrangement(opt));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert((elemsize == EA_8BYTE) || (elemsize == EA_4BYTE)); // Only Double/Float supported
                assert(opt != INS_OPTS_1D);                               // Reserved encoding
                fmt = IF_DV_3B;
            }
            else
            {
                NYI("Untested");
                // Scalar operation
                assert((size == EA_8BYTE) || (size == EA_4BYTE)); // Only Double/Float supported
                fmt = IF_DV_3D;
            }
            break;

        case INS_saba:
        case INS_sabd:
        case INS_smax:
        case INS_smin:
        case INS_uaba:
        case INS_uabd:
        case INS_umax:
        case INS_umin:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsAnyArrangement(opt));

            // Vector operation
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(elemsize != EA_8BYTE); // can't use 2D or 1D

            fmt = IF_DV_3A;
            break;

        case INS_mov:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(reg2 == reg3);
            assert(isValidVectorDatasize(size));
            // INS_mov is an alias for INS_orr (vector register)
            if (opt == INS_OPTS_NONE)
            {
                elemsize = EA_1BYTE;
                opt      = optMakeArrangement(size, elemsize);
            }
            assert(isValidArrangement(size, opt));
            fmt = IF_DV_3C;
            break;

        case INS_and:
        case INS_bic:
        case INS_eor:
        case INS_orr:
        case INS_orn:
            if (isVectorRegister(reg1))
            {
                assert(isValidVectorDatasize(size));
                assert(isVectorRegister(reg2));
                assert(isVectorRegister(reg3));
                if (opt == INS_OPTS_NONE)
                {
                    elemsize = EA_1BYTE;
                    opt      = optMakeArrangement(size, elemsize);
                }
                assert(isValidArrangement(size, opt));
                fmt = IF_DV_3C;
                break;
            }
            __fallthrough;

        case INS_ands:
        case INS_bics:
        case INS_eon:
            emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0, INS_OPTS_NONE);
            return;

        case INS_bsl:
        case INS_bit:
        case INS_bif:
            assert(isValidVectorDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            if (opt == INS_OPTS_NONE)
            {
                elemsize = EA_1BYTE;
                opt      = optMakeArrangement(size, elemsize);
            }
            assert(isValidArrangement(size, opt));
            fmt = IF_DV_3C;
            break;

        case INS_fadd:
        case INS_fsub:
        case INS_fdiv:
        case INS_fmax:
        case INS_fmin:
        case INS_fabd:
        case INS_fmul:
        case INS_fmulx:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_3B;
            }
            else
            {
                // Scalar operation
                assert(insOptsNone(opt));
                assert(isValidScalarDatasize(size));
                fmt = IF_DV_3D;
            }
            break;

        case INS_fnmul:
            // Scalar operation
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isValidScalarDatasize(size));
            fmt = IF_DV_3D;
            break;

        case INS_faddp:
        case INS_fmla:
        case INS_fmls:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsAnyArrangement(opt)); // no scalar encoding, use 4-operand 'fmadd' or 'fmsub'

            // Vector operation
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(isValidVectorElemsizeFloat(elemsize));
            assert(opt != INS_OPTS_1D); // Reserved encoding
            fmt = IF_DV_3B;
            break;

        case INS_ldr:
        case INS_ldrb:
        case INS_ldrh:
        case INS_ldrsb:
        case INS_ldrsh:
        case INS_ldrsw:
        case INS_str:
        case INS_strb:
        case INS_strh:
            emitIns_R_R_R_Ext(ins, attr, reg1, reg2, reg3, opt);
            return;

        case INS_ldp:
        case INS_ldpsw:
        case INS_ldnp:
        case INS_stp:
        case INS_stnp:
            emitIns_R_R_R_I(ins, attr, reg1, reg2, reg3, 0);
            return;

        case INS_stxr:
        case INS_stxrb:
        case INS_stxrh:
        case INS_stlxr:
        case INS_stlxrb:
        case INS_stlxrh:
            assert(isGeneralRegisterOrZR(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert(isGeneralRegisterOrSP(reg3));
            fmt = IF_LS_3D;
            break;

        case INS_casb:
        case INS_casab:
        case INS_casalb:
        case INS_caslb:
        case INS_cash:
        case INS_casah:
        case INS_casalh:
        case INS_caslh:
        case INS_cas:
        case INS_casa:
        case INS_casal:
        case INS_casl:
        case INS_ldaddb:
        case INS_ldaddab:
        case INS_ldaddalb:
        case INS_ldaddlb:
        case INS_ldaddh:
        case INS_ldaddah:
        case INS_ldaddalh:
        case INS_ldaddlh:
        case INS_ldadd:
        case INS_ldadda:
        case INS_ldaddal:
        case INS_ldaddl:
        case INS_swpb:
        case INS_swpab:
        case INS_swpalb:
        case INS_swplb:
        case INS_swph:
        case INS_swpah:
        case INS_swpalh:
        case INS_swplh:
        case INS_swp:
        case INS_swpa:
        case INS_swpal:
        case INS_swpl:
            assert(isGeneralRegisterOrZR(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert(isGeneralRegisterOrSP(reg3));
            fmt = IF_LS_3E;
            break;

        case INS_sha256h:
        case INS_sha256h2:
        case INS_sha256su1:
        case INS_sha1su0:
        case INS_sha1c:
        case INS_sha1p:
        case INS_sha1m:
            assert(isValidVectorDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            if (opt == INS_OPTS_NONE)
            {
                elemsize = EA_4BYTE;
                opt      = optMakeArrangement(size, elemsize);
            }
            assert(isValidArrangement(size, opt));
            fmt = IF_DV_3F;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

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
                              ssize_t     imm,
                              insOpts     opt /* = INS_OPTS_NONE */,
                              emitAttr    attrReg2 /* = EA_UNKNOWN */)
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;
    bool      isLdSt   = false;
    bool      isSIMD   = false;
    bool      isAddSub = false;
    bool      setFlags = false;
    unsigned  scale    = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_extr:
            assert(insOptsNone(opt));
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidImmShift(imm, size));
            fmt = IF_DR_3E;
            break;

        case INS_and:
        case INS_ands:
        case INS_eor:
        case INS_orr:
        case INS_bic:
        case INS_bics:
        case INS_eon:
        case INS_orn:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidImmShift(imm, size));
            if (imm == 0)
            {
                assert(insOptsNone(opt)); // a zero imm, means no shift kind
                fmt = IF_DR_3A;
            }
            else
            {
                assert(insOptsAnyShift(opt)); // a non-zero imm, must select shift kind
                fmt = IF_DR_3B;
            }
            break;

        case INS_fmul: // by element, imm[0..3] selects the element of reg3
        case INS_fmla:
        case INS_fmls:
        case INS_fmulx:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            if (insOptsAnyArrangement(opt))
            {
                // Vector operation
                assert(isValidVectorDatasize(size));
                assert(isValidArrangement(size, opt));
                elemsize = optGetElemsize(opt);
                assert(isValidVectorElemsizeFloat(elemsize));
                assert(isValidVectorIndex(size, elemsize, imm));
                assert(opt != INS_OPTS_1D); // Reserved encoding
                fmt = IF_DV_3BI;
            }
            else
            {
                // Scalar operation
                assert(insOptsNone(opt));
                assert(isValidScalarDatasize(size));
                elemsize = size;
                assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
                fmt = IF_DV_3DI;
            }
            break;

        case INS_mul: // by element, imm[0..7] selects the element of reg3
        case INS_mla:
        case INS_mls:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            // Vector operation
            assert(insOptsAnyArrangement(opt));
            assert(isValidVectorDatasize(size));
            assert(isValidArrangement(size, opt));
            elemsize = optGetElemsize(opt);
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            // Only has encodings for H or S elemsize
            assert((elemsize == EA_2BYTE) || (elemsize == EA_4BYTE));
            // Only has encodings for V0..V15
            if ((elemsize == EA_2BYTE) && (reg3 >= REG_V16))
            {
                noway_assert(!"Invalid reg3");
            }
            fmt = IF_DV_3AI;
            break;

        case INS_add:
        case INS_sub:
            setFlags = false;
            isAddSub = true;
            break;

        case INS_adds:
        case INS_subs:
            setFlags = true;
            isAddSub = true;
            break;

        case INS_ldpsw:
            scale  = 2;
            isLdSt = true;
            break;

        case INS_ldnp:
        case INS_stnp:
            assert(insOptsNone(opt)); // Can't use Pre/Post index on these two instructions
            __fallthrough;

        case INS_ldp:
        case INS_stp:
            // Is the target a vector register?
            if (isVectorRegister(reg1))
            {
                scale  = NaturalScale_helper(size);
                isSIMD = true;
            }
            else
            {
                scale = (size == EA_8BYTE) ? 3 : 2;
            }
            isLdSt = true;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    if (isLdSt)
    {
        assert(!isAddSub);
        assert(isGeneralRegisterOrSP(reg3));
        assert(insOptsNone(opt) || insOptsIndexed(opt));

        if (isSIMD)
        {
            assert(isValidVectorLSPDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert((scale >= 2) && (scale <= 4));
        }
        else
        {
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegisterOrZR(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert((scale == 2) || (scale == 3));
        }

        // Load/Store Pair reserved encodings:
        if (emitInsIsLoad(ins))
        {
            assert(reg1 != reg2);
        }
        if (insOptsIndexed(opt))
        {
            assert(reg1 != reg3);
            assert(reg2 != reg3);
        }

        reg3 = encodingSPtoZR(reg3);

        ssize_t mask = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate
        if (imm == 0)
        {
            assert(insOptsNone(opt)); // PRE/POST Index doesn't make sense with an immediate of zero

            fmt = IF_LS_3B;
        }
        else
        {
            if ((imm & mask) == 0)
            {
                imm >>= scale; // The immediate is scaled by the size of the ld/st

                if ((imm >= -64) && (imm <= 63))
                {
                    fmt = IF_LS_3C;
                }
            }
#ifdef DEBUG
            if (fmt != IF_LS_3C)
            {
                assert(!"Instruction cannot be encoded: IF_LS_3C");
            }
#endif
        }
    }
    else if (isAddSub)
    {
        bool reg2IsSP = (reg2 == REG_SP);
        assert(!isLdSt);
        assert(isValidGeneralDatasize(size));
        assert(isGeneralRegister(reg3));

        if (setFlags || insOptsAluShift(opt)) // Can't encode SP in reg1 with setFlags or AluShift option
        {
            assert(isGeneralRegisterOrZR(reg1));
        }
        else
        {
            assert(isGeneralRegisterOrSP(reg1));
            reg1 = encodingSPtoZR(reg1);
        }

        if (insOptsAluShift(opt)) // Can't encode SP in reg2 with AluShift option
        {
            assert(isGeneralRegister(reg2));
        }
        else
        {
            assert(isGeneralRegisterOrSP(reg2));
            reg2 = encodingSPtoZR(reg2);
        }

        if (insOptsAnyExtend(opt))
        {
            assert((imm >= 0) && (imm <= 4));

            fmt = IF_DR_3C;
        }
        else if (insOptsAluShift(opt))
        {
            // imm should be non-zero and in [1..63]
            assert(isValidImmShift(imm, size) && (imm != 0));
            fmt = IF_DR_3B;
        }
        else if (imm == 0)
        {
            assert(insOptsNone(opt));

            if (reg2IsSP)
            {
                // To encode the SP register as reg2 we must use the IF_DR_3C encoding
                // and also specify a LSL of zero (imm == 0)
                opt = INS_OPTS_LSL;
                fmt = IF_DR_3C;
            }
            else
            {
                fmt = IF_DR_3A;
            }
        }
        else
        {
            assert(!"Instruction cannot be encoded: Add/Sub IF_DR_3A");
        }
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    // Record the attribute for the second register in the pair
    id->idGCrefReg2(GCT_NONE);
    if (attrReg2 != EA_UNKNOWN)
    {
        // Record the attribute for the second register in the pair
        assert((fmt == IF_LS_3B) || (fmt == IF_LS_3C));
        if (EA_IS_GCREF(attrReg2))
        {
            id->idGCrefReg2(GCT_GCREF);
        }
        else if (EA_IS_BYREF(attrReg2))
        {
            id->idGCrefReg2(GCT_BYREF);
        }
    }

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers, with an extend option
 */

void emitter::emitIns_R_R_R_Ext(instruction ins,
                                emitAttr    attr,
                                regNumber   reg1,
                                regNumber   reg2,
                                regNumber   reg3,
                                insOpts     opt,         /* = INS_OPTS_NONE */
                                int         shiftAmount) /* = -1 -- unset   */
{
    emitAttr  size   = EA_SIZE(attr);
    insFormat fmt    = IF_NONE;
    bool      isSIMD = false;
    int       scale  = -1;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_ldrb:
        case INS_ldrsb:
        case INS_strb:
            scale = 0;
            break;

        case INS_ldrh:
        case INS_ldrsh:
        case INS_strh:
            scale = 1;
            break;

        case INS_ldrsw:
            scale = 2;
            break;

        case INS_ldr:
        case INS_str:
            // Is the target a vector register?
            if (isVectorRegister(reg1))
            {
                assert(isValidVectorLSDatasize(size));
                scale  = NaturalScale_helper(size);
                isSIMD = true;
            }
            else
            {
                assert(isValidGeneralDatasize(size));
                scale = (size == EA_8BYTE) ? 3 : 2;
            }

            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(scale != -1);
    assert(insOptsLSExtend(opt));

    if (isSIMD)
    {
        assert(isValidVectorLSDatasize(size));
        assert(isVectorRegister(reg1));
    }
    else
    {
        assert(isValidGeneralLSDatasize(size));
        assert(isGeneralRegisterOrZR(reg1));
    }

    assert(isGeneralRegisterOrSP(reg2));
    assert(isGeneralRegister(reg3));

    // Load/Store reserved encodings:
    if (insOptsIndexed(opt))
    {
        assert(reg1 != reg2);
    }

    if (shiftAmount == -1)
    {
        shiftAmount = insOptsLSL(opt) ? scale : 0;
    }
    assert((shiftAmount == scale) || (shiftAmount == 0));

    reg2 = encodingSPtoZR(reg2);
    fmt  = IF_LS_3A;

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg3Scaled(shiftAmount == scale);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2)
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;
    size_t    immOut   = 0; // composed from imm1 and imm2 and stored in the instrDesc

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        int        lsb;
        int        width;
        bitMaskImm bmi;

        case INS_bfm:
        case INS_sbfm:
        case INS_ubfm:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isValidImmShift(imm1, size));
            assert(isValidImmShift(imm2, size));
            bmi.immNRS = 0;
            bmi.immN   = (size == EA_8BYTE);
            bmi.immR   = imm1;
            bmi.immS   = imm2;
            immOut     = bmi.immNRS;
            fmt        = IF_DI_2D;
            break;

        case INS_bfi:
        case INS_sbfiz:
        case INS_ubfiz:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            lsb   = getBitWidth(size) - imm1;
            width = imm2 - 1;
            assert(isValidImmShift(lsb, size));
            assert(isValidImmShift(width, size));
            bmi.immNRS = 0;
            bmi.immN   = (size == EA_8BYTE);
            bmi.immR   = lsb;
            bmi.immS   = width;
            immOut     = bmi.immNRS;
            fmt        = IF_DI_2D;
            break;

        case INS_bfxil:
        case INS_sbfx:
        case INS_ubfx:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            lsb   = imm1;
            width = imm2 + imm1 - 1;
            assert(isValidImmShift(lsb, size));
            assert(isValidImmShift(width, size));
            bmi.immNRS = 0;
            bmi.immN   = (size == EA_8BYTE);
            bmi.immR   = imm1;
            bmi.immS   = imm2 + imm1 - 1;
            immOut     = bmi.immNRS;
            fmt        = IF_DI_2D;
            break;

        case INS_mov:
        case INS_ins:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            elemsize = size;
            assert(isValidVectorElemsize(elemsize));
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm1));
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm2));
            immOut = (imm1 << 4) + imm2;
            fmt    = IF_DV_2F;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(attr, immOut);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);

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
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_madd:
        case INS_msub:
        case INS_smaddl:
        case INS_smsubl:
        case INS_umaddl:
        case INS_umsubl:
            assert(isValidGeneralDatasize(size));
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            fmt = IF_DR_4A;
            break;

        case INS_fmadd:
        case INS_fmsub:
        case INS_fnmadd:
        case INS_fnmsub:
            // Scalar operation
            assert(isValidScalarDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            fmt = IF_DV_4A;
            break;

        case INS_invalid:
            fmt = IF_NONE;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a condition code
 */

void emitter::emitIns_R_COND(instruction ins, emitAttr attr, regNumber reg, insCond cond)
{
    insFormat    fmt = IF_NONE;
    condFlagsImm cfi;
    cfi.immCFVal = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_cset:
        case INS_csetm:
            assert(isGeneralRegister(reg));
            cfi.cond = cond;
            fmt      = IF_DR_1D;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);
    assert(isValidImmCond(cfi.immCFVal));

    instrDesc* id = emitNewInstrSC(attr, cfi.immCFVal);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a condition code
 */

void emitter::emitIns_R_R_COND(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insCond cond)
{
    insFormat    fmt = IF_NONE;
    condFlagsImm cfi;
    cfi.immCFVal = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_cinc:
        case INS_cinv:
        case INS_cneg:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            cfi.cond = cond;
            fmt      = IF_DR_2D;
            break;
        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);
    assert(isValidImmCond(cfi.immCFVal));

    instrDesc* id = emitNewInstrSC(attr, cfi.immCFVal);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a condition code
 */

void emitter::emitIns_R_R_R_COND(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insCond cond)
{
    insFormat    fmt = IF_NONE;
    condFlagsImm cfi;
    cfi.immCFVal = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_csel:
        case INS_csinc:
        case INS_csinv:
        case INS_csneg:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isGeneralRegister(reg3));
            cfi.cond = cond;
            fmt      = IF_DR_3D;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

    assert(fmt != IF_NONE);
    assert(isValidImmCond(cfi.immCFVal));

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idSmallCns(cfi.immCFVal);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers the flags and a condition code
 */

void emitter::emitIns_R_R_FLAGS_COND(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insCflags flags, insCond cond)
{
    insFormat    fmt = IF_NONE;
    condFlagsImm cfi;
    cfi.immCFVal = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_ccmp:
        case INS_ccmn:
            assert(isGeneralRegister(reg1));
            assert(isGeneralRegister(reg2));
            cfi.flags = flags;
            cfi.cond  = cond;
            fmt       = IF_DR_2I;
            break;
        default:
            unreached();
            break;
    } // end switch (ins)

    assert(fmt != IF_NONE);
    assert(isValidImmCondFlags(cfi.immCFVal));

    instrDesc* id = emitNewInstrSC(attr, cfi.immCFVal);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register, an immediate, the flags and a condition code
 */

void emitter::emitIns_R_I_FLAGS_COND(
    instruction ins, emitAttr attr, regNumber reg, int imm, insCflags flags, insCond cond)
{
    insFormat    fmt = IF_NONE;
    condFlagsImm cfi;
    cfi.immCFVal = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_ccmp:
        case INS_ccmn:
            assert(isGeneralRegister(reg));
            if (imm < 0)
            {
                ins = insReverse(ins);
                imm = -imm;
            }
            if ((imm >= 0) && (imm <= 31))
            {
                cfi.imm5  = imm;
                cfi.flags = flags;
                cfi.cond  = cond;
                fmt       = IF_DI_1F;
            }
            else
            {
                assert(!"Instruction cannot be encoded: ccmp/ccmn imm5");
            }
            break;
        default:
            unreached();
            break;
    } // end switch (ins)

    assert(fmt != IF_NONE);
    assert(isValidImmCondFlagsImm5(cfi.immCFVal));

    instrDesc* id = emitNewInstrSC(attr, cfi.immCFVal);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a memory barrier instruction with a 'barrier' immediate
 */

void emitter::emitIns_BARR(instruction ins, insBarrier barrier)
{
    insFormat fmt = IF_NONE;
    ssize_t   imm = 0;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_dsb:
        case INS_dmb:
        case INS_isb:

            fmt = IF_SI_0B;
            imm = (ssize_t)barrier;
            break;
        default:
            unreached();
            break;
    } // end switch (ins)

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrSC(EA_8BYTE, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

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

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a stack-based local variable.
 */
void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    emitAttr  size  = EA_SIZE(attr);
    insFormat fmt   = IF_NONE;
    int       disp  = 0;
    unsigned  scale = 0;

    assert(offs >= 0);

    // TODO-ARM64-CQ: use unscaled loads?
    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_strb:
        case INS_ldrb:
        case INS_ldrsb:
            scale = 0;
            break;

        case INS_strh:
        case INS_ldrh:
        case INS_ldrsh:
            scale = 1;
            break;

        case INS_ldrsw:
            scale = 2;
            break;

        case INS_str:
        case INS_ldr:
            assert(isValidGeneralDatasize(size) || isValidVectorDatasize(size));
            scale = genLog2(EA_SIZE_IN_BYTES(size));
            break;

        case INS_lea:
            assert(size == EA_8BYTE);
            scale = 0;
            break;

        default:
            NYI("emitIns_R_S"); // FP locals?
            return;

    } // end switch (ins)

    /* Figure out the variable's frame position */
    ssize_t imm;
    int     base;
    bool    FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    disp = base + offs;
    assert((scale >= 0) && (scale <= 4));

    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;
    reg2           = encodingSPtoZR(reg2);

    if (ins == INS_lea)
    {
        if (disp >= 0)
        {
            ins = INS_add;
            imm = disp;
        }
        else
        {
            ins = INS_sub;
            imm = -disp;
        }

        if (imm <= 0x0fff)
        {
            fmt = IF_DI_2A; // add reg1,reg2,#disp
        }
        else
        {
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, rsvdReg, imm);
            fmt = IF_DR_3A; // add reg1,reg2,rsvdReg
        }
    }
    else
    {
        bool    useRegForImm = false;
        ssize_t mask         = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate

        imm = disp;
        if (imm == 0)
        {
            fmt = IF_LS_2A;
        }
        else if ((imm < 0) || ((imm & mask) != 0))
        {
            if ((imm >= -256) && (imm <= 255))
            {
                fmt = IF_LS_2C;
            }
            else
            {
                useRegForImm = true;
            }
        }
        else if (imm > 0)
        {
            if (((imm & mask) == 0) && ((imm >> scale) < 0x1000))
            {
                imm >>= scale; // The immediate is scaled by the size of the ld/st

                fmt = IF_LS_2B;
            }
            else
            {
                useRegForImm = true;
            }
        }

        if (useRegForImm)
        {
            regNumber rsvdReg = codeGen->rsGetRsvdReg();
            codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, rsvdReg, imm);
            fmt = IF_LS_3A;
        }
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two register and consectutive stack-based local variable slots.
 */
void emitter::emitIns_R_R_S_S(
    instruction ins, emitAttr attr1, emitAttr attr2, regNumber reg1, regNumber reg2, int varx, int offs)
{
    assert((ins == INS_ldp) || (ins == INS_ldnp));
    assert(EA_8BYTE == EA_SIZE(attr1));
    assert(EA_8BYTE == EA_SIZE(attr2));
    assert(isGeneralRegisterOrZR(reg1));
    assert(isGeneralRegisterOrZR(reg2));
    assert(offs >= 0);

    insFormat      fmt   = IF_LS_3B;
    int            disp  = 0;
    const unsigned scale = 3;

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    disp = base + offs;

    // TODO-ARM64-CQ: with compLocallocUsed, should we use REG_SAVED_LOCALLOC_SP instead?
    regNumber reg3 = FPbased ? REG_FPBASE : REG_SPBASE;
    reg3           = encodingSPtoZR(reg3);

    bool    useRegForAdr = true;
    ssize_t imm          = disp;
    ssize_t mask         = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate
    if (imm == 0)
    {
        useRegForAdr = false;
    }
    else
    {
        if ((imm & mask) == 0)
        {
            ssize_t immShift = imm >> scale; // The immediate is scaled by the size of the ld/st

            if ((immShift >= -64) && (immShift <= 63))
            {
                fmt          = IF_LS_3C;
                useRegForAdr = false;
                imm          = immShift;
            }
        }
    }

    if (useRegForAdr)
    {
        regNumber rsvd = codeGen->rsGetRsvdReg();
        emitIns_R_R_Imm(INS_add, EA_PTRSIZE, rsvd, reg3, imm);
        reg3 = rsvd;
        imm  = 0;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr1, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    // Record the attribute for the second register in the pair
    if (EA_IS_GCREF(attr2))
    {
        id->idGCrefReg2(GCT_GCREF);
    }
    else if (EA_IS_BYREF(attr2))
    {
        id->idGCrefReg2(GCT_BYREF);
    }
    else
    {
        id->idGCrefReg2(GCT_NONE);
    }

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing a stack-based local variable and a register
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    assert(offs >= 0);
    emitAttr  size          = EA_SIZE(attr);
    insFormat fmt           = IF_NONE;
    int       disp          = 0;
    unsigned  scale         = 0;
    bool      isVectorStore = false;

    // TODO-ARM64-CQ: use unscaled loads?
    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_strb:
            scale = 0;
            assert(isGeneralRegisterOrZR(reg1));
            break;

        case INS_strh:
            scale = 1;
            assert(isGeneralRegisterOrZR(reg1));
            break;

        case INS_str:
            if (isGeneralRegisterOrZR(reg1))
            {
                assert(isValidGeneralDatasize(size));
                scale = (size == EA_8BYTE) ? 3 : 2;
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isValidVectorLSDatasize(size));
                scale         = NaturalScale_helper(size);
                isVectorStore = true;
            }
            break;

        default:
            NYI("emitIns_S_R"); // FP locals?
            return;

    } // end switch (ins)

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    disp = base + offs;
    assert(scale >= 0);
    if (isVectorStore)
    {
        assert(scale <= 4);
    }
    else
    {
        assert(scale <= 3);
    }

    // TODO-ARM64-CQ: with compLocallocUsed, should we use REG_SAVED_LOCALLOC_SP instead?
    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;
    reg2           = encodingSPtoZR(reg2);

    bool    useRegForImm = false;
    ssize_t imm          = disp;
    ssize_t mask         = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate
    if (imm == 0)
    {
        fmt = IF_LS_2A;
    }
    else if ((imm < 0) || ((imm & mask) != 0))
    {
        if ((imm >= -256) && (imm <= 255))
        {
            fmt = IF_LS_2C;
        }
        else
        {
            useRegForImm = true;
        }
    }
    else if (imm > 0)
    {
        if (((imm & mask) == 0) && ((imm >> scale) < 0x1000))
        {
            imm >>= scale; // The immediate is scaled by the size of the ld/st

            fmt = IF_LS_2B;
        }
        else
        {
            useRegForImm = true;
        }
    }

    if (useRegForImm)
    {
        // The reserved register is not stored in idReg3() since that field overlaps with iiaLclVar.
        // It is instead implicit when idSetIsLclVar() is set, with this encoding format.
        regNumber rsvdReg = codeGen->rsGetRsvdReg();
        codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, rsvdReg, imm);
        fmt = IF_LS_3A;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();

#ifdef DEBUG
    id->idDebugOnlyInfo()->idVarRefOffs = emitVarRefOffs;
#endif

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing consecutive stack-based local variable slots and two registers
 */
void emitter::emitIns_S_S_R_R(
    instruction ins, emitAttr attr1, emitAttr attr2, regNumber reg1, regNumber reg2, int varx, int offs)
{
    assert((ins == INS_stp) || (ins == INS_stnp));
    assert(EA_8BYTE == EA_SIZE(attr1));
    assert(EA_8BYTE == EA_SIZE(attr2));
    assert(isGeneralRegisterOrZR(reg1));
    assert(isGeneralRegisterOrZR(reg2));
    assert(offs >= 0);

    insFormat      fmt   = IF_LS_3B;
    int            disp  = 0;
    const unsigned scale = 3;

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    disp = base + offs;

    // TODO-ARM64-CQ: with compLocallocUsed, should we use REG_SAVED_LOCALLOC_SP instead?
    regNumber reg3 = FPbased ? REG_FPBASE : REG_SPBASE;

    bool    useRegForAdr = true;
    ssize_t imm          = disp;
    ssize_t mask         = (1 << scale) - 1; // the mask of low bits that must be zero to encode the immediate
    if (imm == 0)
    {
        useRegForAdr = false;
    }
    else
    {
        if ((imm & mask) == 0)
        {
            ssize_t immShift = imm >> scale; // The immediate is scaled by the size of the ld/st

            if ((immShift >= -64) && (immShift <= 63))
            {
                fmt          = IF_LS_3C;
                useRegForAdr = false;
                imm          = immShift;
            }
        }
    }

    if (useRegForAdr)
    {
        regNumber rsvd = codeGen->rsGetRsvdReg();
        emitIns_R_R_Imm(INS_add, EA_PTRSIZE, rsvd, reg3, imm);
        reg3 = rsvd;
        imm  = 0;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr1, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);

    // Record the attribute for the second register in the pair
    if (EA_IS_GCREF(attr2))
    {
        id->idGCrefReg2(GCT_GCREF);
    }
    else if (EA_IS_BYREF(attr2))
    {
        id->idGCrefReg2(GCT_BYREF);
    }
    else
    {
        id->idGCrefReg2(GCT_NONE);
    }

    reg3 = encodingSPtoZR(reg3);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();

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
 *  Constant is stored into JIT data which is adjacent to code.
 *  No relocation is needed. PC-relative offset will be encoded directly into instruction.
 *
 */
void emitter::emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber addrReg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    assert(offs >= 0);
    assert(instrDesc::fitsInSmallCns(offs));

    emitAttr      size = EA_SIZE(attr);
    insFormat     fmt  = IF_NONE;
    instrDescJmp* id   = emitNewInstrJmp();

    switch (ins)
    {
        case INS_adr:
            // This is case to get address to the constant data.
            fmt = IF_LARGEADR;
            assert(isGeneralRegister(reg));
            assert(isValidGeneralDatasize(size));
            break;

        case INS_ldr:
            fmt = IF_LARGELDC;
            if (isVectorRegister(reg))
            {
                assert(isValidScalarDatasize(size));
                // For vector (float/double) register, we should have an integer address reg to
                // compute long address which consists of page address and page offset.
                // For integer constant, this is not needed since the dest reg can be used to
                // compute address as well as contain the final contents.
                assert(isGeneralRegister(reg) || (addrReg != REG_NA));
            }
            else
            {
                assert(isGeneralRegister(reg));
                assert(isValidGeneralDatasize(size));
            }
            break;
        default:
            unreached();
    }

    assert(fmt != IF_NONE);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);
    id->idSmallCns(offs);
    id->idOpSize(size);
    id->idAddr()->iiaFieldHnd = fldHnd;
    id->idSetIsBound(); // We won't patch address since we will know the exact distance once JIT code and data are
                        // allocated together.

    id->idReg1(reg); // destination register that will get the constant value.
    if (addrReg != REG_NA)
    {
        id->idReg2(addrReg); // integer register to compute long address (used for vector dest when we end up with long
                             // address)
    }
    id->idjShort = false; // Assume loading constant from long address

    // Keep it long if it's in cold code.
    id->idjKeepLong = emitComp->fgIsBlockCold(emitComp->compCurBB);

#ifdef DEBUG
    if (emitComp->opts.compLongAddress)
        id->idjKeepLong = 1;
#endif // DEBUG

    // If it's possible to be shortened, then put it in jump list
    // to be revisited by emitJumpDistBind.
    if (!id->idjKeepLong)
    {
        /* Record the jump's IG and offset within it */
        id->idjIG   = emitCurIG;
        id->idjOffs = emitCurIGsize;

        /* Append this jump to this IG's jump list */
        id->idjNext      = emitCurIGjmpList;
        emitCurIGjmpList = id;

#if EMITTER_STATS
        emitTotalIGjmps++;
#endif
    }

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + constant.
 */

void emitter::emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, ssize_t offs, ssize_t val)
{
    NYI("emitIns_C_I");
}

/*****************************************************************************
 *
 *  Add an instruction with a static member + register operands.
 */

void emitter::emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs)
{
    assert(!"emitIns_C_R not supported for RyuJIT backend");
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    NYI("emitIns_R_AR");
}

// This computes address from the immediate which is relocatable.
void emitter::emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t addr)
{
    assert(EA_IS_RELOC(attr));
    emitAttr      size    = EA_SIZE(attr);
    insFormat     fmt     = IF_DI_1E;
    bool          needAdd = false;
    instrDescJmp* id      = emitNewInstrJmp();

    switch (ins)
    {
        case INS_adrp:
            // This computes page address.
            // page offset is needed using add.
            needAdd = true;
            break;
        case INS_adr:
            break;
        default:
            unreached();
    }

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(INS_OPTS_NONE);
    id->idOpSize(size);
    id->idAddr()->iiaAddr = (BYTE*)addr;
    id->idReg1(ireg);
    id->idSetIsDspReloc();

    dispIns(id);
    appendToCurIG(id);

    if (needAdd)
    {
        // add reg, reg, imm
        ins           = INS_add;
        fmt           = IF_DI_2A;
        instrDesc* id = emitNewInstr(attr);
        assert(id->idIsReloc());

        id->idIns(ins);
        id->idInsFmt(fmt);
        id->idInsOpt(INS_OPTS_NONE);
        id->idOpSize(size);
        id->idAddr()->iiaAddr = (BYTE*)addr;
        id->idReg1(ireg);
        id->idReg2(ireg);

        dispIns(id);
        appendToCurIG(id);
    }
}

void emitter::emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    NYI("emitIns_AR_R");
}

void emitter::emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp)
{
    NYI("emitIns_R_ARR");
}

void emitter::emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp)
{
    NYI("emitIns_R_ARR");
}

void emitter::emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp)
{
    NYI("emitIns_R_ARR");
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

    insFormat fmt = IF_NONE;
    if (emitIsCondJump(id))
    {
        switch (id->idIns())
        {
            case INS_cbz:
            case INS_cbnz:
                fmt = IF_BI_1A;
                break;
            case INS_tbz:
            case INS_tbnz:
                fmt = IF_BI_1B;
                break;
            default:
                fmt = IF_BI_0B;
                break;
        }
    }
    else if (emitIsLoadLabel(id))
    {
        fmt = IF_DI_1E;
    }
    else if (emitIsLoadConstant(id))
    {
        fmt = IF_LS_1A;
    }
    else
    {
        unreached();
    }

    id->idInsFmt(fmt);
    id->idjShort = true;
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */

void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(dst->bbFlags & BBF_JMP_TARGET);

    insFormat fmt = IF_NONE;

    switch (ins)
    {
        case INS_adr:
            fmt = IF_LARGEADR;
            break;
        default:
            unreached();
    }

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idjShort             = false;
    id->idAddr()->iiaBBlabel = dst;
    id->idReg1(reg);
    id->idOpSize(EA_PTRSIZE);

#ifdef DEBUG
    // Mark the catch return
    if (emitComp->compCurBB->bbJumpKind == BBJ_EHCATCHRET)
    {
        id->idDebugOnlyInfo()->idCatchRet = true;
    }
#endif // DEBUG

    id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

#ifdef DEBUG
    if (emitComp->opts.compLongAddress)
        id->idjKeepLong = 1;
#endif // DEBUG

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
 *  Add a data label instruction.
 */

void emitter::emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg)
{
    NYI("emitIns_R_D");
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert((ins == INS_cbz) || (ins == INS_cbnz));

    assert(dst != nullptr);
    assert((dst->bbFlags & BBF_JMP_TARGET) != 0);

    insFormat fmt = IF_LARGEJMP;

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);
    id->idjShort = false;
    id->idOpSize(EA_SIZE(attr));

    id->idAddr()->iiaBBlabel = dst;
    id->idjKeepLong          = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

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

void emitter::emitIns_J_R_I(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg, int imm)
{
    assert((ins == INS_tbz) || (ins == INS_tbnz));

    assert(dst != nullptr);
    assert((dst->bbFlags & BBF_JMP_TARGET) != 0);
    assert((EA_SIZE(attr) == EA_4BYTE) || (EA_SIZE(attr) == EA_8BYTE));
    assert(imm < ((EA_SIZE(attr) == EA_4BYTE) ? 32 : 64));

    insFormat fmt = IF_LARGEJMP;

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idReg1(reg);
    id->idjShort = false;
    id->idSmallCns(imm);
    id->idOpSize(EA_SIZE(attr));

    id->idAddr()->iiaBBlabel = dst;
    id->idjKeepLong          = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

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

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    insFormat fmt = IF_NONE;

    if (dst != nullptr)
    {
        assert(dst->bbFlags & BBF_JMP_TARGET);
    }
    else
    {
        assert(instrCount != 0);
    }

    /* Figure out the encoding format of the instruction */

    bool idjShort = false;
    switch (ins)
    {
        case INS_bl_local:
        case INS_b:
            // Unconditional jump is a single form.
            idjShort = true;
            fmt      = IF_BI_0A;
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
            // Assume conditional jump is long.
            fmt = IF_LARGEJMP;
            break;

        default:
            unreached();
            break;
    }

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idjShort = idjShort;

#ifdef DEBUG
    // Mark the finally call
    if (ins == INS_bl_local && emitComp->compCurBB->bbJumpKind == BBJ_CALLFINALLY)
    {
        id->idDebugOnlyInfo()->idFinallyCall = true;
    }
#endif // DEBUG

    if (dst != nullptr)
    {
        id->idAddr()->iiaBBlabel = dst;

        // Skip unconditional jump that has a single form.
        // TODO-ARM64-NYI: enable hot/cold splittingNYI.
        // The target needs to be relocated.
        if (!idjShort)
        {
            id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);

#ifdef DEBUG
            if (emitComp->opts.compLongAddress) // Force long branches
                id->idjKeepLong = 1;
#endif // DEBUG
        }
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
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 *
 * For ARM xreg, xmul and disp are never used and should always be 0/REG_NA.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*            addr,
                           ssize_t          argSize,
                           emitAttr         retSize,
                           emitAttr         secondRetSize,
                           VARSET_VALARG_TP ptrVars,
                           regMaskTP        gcrefRegs,
                           regMaskTP        byrefRegs,
                           IL_OFFSETX       ilOffset /* = BAD_IL_OFFSET */,
                           regNumber        ireg /* = REG_NA */,
                           regNumber        xreg /* = REG_NA */,
                           unsigned         xmul /* = 0     */,
                           ssize_t          disp /* = 0     */,
                           bool             isJump /* = false */)
{
    /* Sanity check the arguments depending on callType */

    assert(callType < EC_COUNT);
    assert((callType != EC_FUNC_TOKEN && callType != EC_FUNC_ADDR) ||
           (ireg == REG_NA && xreg == REG_NA && xmul == 0 && disp == 0));
    assert(callType < EC_INDIR_R || addr == NULL);
    assert(callType != EC_INDIR_R || (ireg < REG_COUNT && xreg == REG_NA && xmul == 0 && disp == 0));

    // ARM never uses these
    assert(xreg == REG_NA && xmul == 0 && disp == 0);

    // Our stack level should be always greater than the bytes of arguments we push. Just
    // a sanity test.
    assert((unsigned)abs(argSize) <= codeGen->genStackLevel);

    // Trim out any callee-trashed registers from the live set.
    regMaskTP savedSet = emitGetGCRegsSavedOrModified(methHnd);
    gcrefRegs &= savedSet;
    byrefRegs &= savedSet;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("Call: GCvars=%s ", VarSetOps::ToString(emitComp, ptrVars));
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
    if (emitComp->opts.compDbgInfo && ilOffset != BAD_IL_OFFSET)
    {
        codeGen->genIPmappingAdd(ilOffset, false);
    }

    /*
        We need to allocate the appropriate instruction descriptor based
        on whether this is a direct/indirect call, and whether we need to
        record an updated set of live GC variables.
     */
    instrDesc* id;

    assert(argSize % REGSIZE_BYTES == 0);
    int argCnt = (int)(argSize / (int)REGSIZE_BYTES);

    if (callType >= EC_INDIR_R)
    {
        /* Indirect call, virtual calls */

        assert(callType == EC_INDIR_R);

        id = emitNewInstrCallInd(argCnt, disp, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
    }
    else
    {
        /* Helper/static/nonvirtual/function calls (direct or through handle),
           and calls to an absolute addr. */

        assert(callType == EC_FUNC_TOKEN || callType == EC_FUNC_ADDR);

        id = emitNewInstrCallDir(argCnt, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
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

    if (callType > EC_FUNC_ADDR)
    {
        /* This is an indirect call (either a virtual call or func ptr call) */

        switch (callType)
        {
            case EC_INDIR_R: // the address is in a register

                id->idSetIsCallRegPtr();

                if (isJump)
                {
                    ins = INS_br_tail; // INS_br_tail  Reg
                }
                else
                {
                    ins = INS_blr; // INS_blr Reg
                }
                fmt = IF_BR_1B;

                id->idIns(ins);
                id->idInsFmt(fmt);

                id->idReg3(ireg);
                assert(xreg == REG_NA);
                break;

            default:
                NO_WAY("unexpected instruction");
                break;
        }
    }
    else
    {
        /* This is a simple direct call: "call helper/method/addr" */

        assert(callType == EC_FUNC_TOKEN || callType == EC_FUNC_ADDR);

        assert(addr != NULL);

        if (isJump)
        {
            ins = INS_b_tail; // INS_b_tail imm28
        }
        else
        {
            ins = INS_bl; // INS_bl imm28
        }
        fmt = IF_BI_0C;

        id->idIns(ins);
        id->idInsFmt(fmt);

        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (callType == EC_FUNC_ADDR)
        {
            id->idSetIsCallAddr();
        }

        if (emitComp->opts.compReloc)
        {
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

    id->idDebugOnlyInfo()->idMemCookie = (size_t)methHnd; // method token
    id->idDebugOnlyInfo()->idCallSig   = sigInfo;
#endif // DEBUG

#ifdef LATE_DISASM
    if (addr != nullptr)
    {
        codeGen->getDisAssembler().disSetMethod((size_t)addr, methHnd);
    }
#endif // LATE_DISASM

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Returns true if 'imm' is valid Cond encoding
 */

/*static*/ bool emitter::isValidImmCond(ssize_t imm)
{
    // range check the ssize_t value, to make sure it is a small unsigned value
    // and that only the bits in the cfi.cond are set
    if ((imm < 0) || (imm > 0xF))
        return false;

    condFlagsImm cfi;
    cfi.immCFVal = (unsigned)imm;

    return (cfi.cond <= INS_COND_LE); // Don't allow 14 & 15 (AL & NV).
}

/*****************************************************************************
 *
 *  Returns true if 'imm' is valid Cond/Flags encoding
 */

/*static*/ bool emitter::isValidImmCondFlags(ssize_t imm)
{
    // range check the ssize_t value, to make sure it is a small unsigned value
    // and that only the bits in the cfi.cond or cfi.flags are set
    if ((imm < 0) || (imm > 0xFF))
        return false;

    condFlagsImm cfi;
    cfi.immCFVal = (unsigned)imm;

    return (cfi.cond <= INS_COND_LE); // Don't allow 14 & 15 (AL & NV).
}

/*****************************************************************************
 *
 *  Returns true if 'imm' is valid Cond/Flags/Imm5 encoding
 */

/*static*/ bool emitter::isValidImmCondFlagsImm5(ssize_t imm)
{
    // range check the ssize_t value, to make sure it is a small unsigned value
    // and that only the bits in the cfi.cond, cfi.flags or cfi.imm5 are set
    if ((imm < 0) || (imm > 0x1FFF))
        return false;

    condFlagsImm cfi;
    cfi.immCFVal = (unsigned)imm;

    return (cfi.cond <= INS_COND_LE); // Don't allow 14 & 15 (AL & NV).
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Rd' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Rd(regNumber reg)
{
    assert(isIntegerRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Rt' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Rt(regNumber reg)
{
    assert(isIntegerRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Rn' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Rn(regNumber reg)
{
    assert(isIntegerRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 5;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Rm' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Rm(regNumber reg)
{
    assert(isIntegerRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 16;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Ra' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Ra(regNumber reg)
{
    assert(isIntegerRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 10;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Vd' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Vd(regNumber reg)
{
    assert(emitter::isVectorRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg - (emitter::code_t)REG_V0;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Vt' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Vt(regNumber reg)
{
    assert(emitter::isVectorRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg - (emitter::code_t)REG_V0;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Vn' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Vn(regNumber reg)
{
    assert(emitter::isVectorRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg - (emitter::code_t)REG_V0;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 5;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Vm' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Vm(regNumber reg)
{
    assert(emitter::isVectorRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg - (emitter::code_t)REG_V0;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 16;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified register used in the 'Va' position
 */

/*static*/ emitter::code_t emitter::insEncodeReg_Va(regNumber reg)
{
    assert(emitter::isVectorRegister(reg));
    emitter::code_t ureg = (emitter::code_t)reg - (emitter::code_t)REG_V0;
    assert((ureg >= 0) && (ureg <= 31));
    return ureg << 10;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified condition code.
 */

/*static*/ emitter::code_t emitter::insEncodeCond(insCond cond)
{
    emitter::code_t uimm = (emitter::code_t)cond;
    return uimm << 12;
}

/*****************************************************************************
 *
 *  Returns an encoding for the condition code with the lowest bit inverted (marked by invert(<cond>) in the
 *  architecture manual).
 */

/*static*/ emitter::code_t emitter::insEncodeInvertedCond(insCond cond)
{
    emitter::code_t uimm = (emitter::code_t)cond;
    uimm ^= 1; // invert the lowest bit
    return uimm << 12;
}

/*****************************************************************************
 *
 *  Returns an encoding for the specified flags.
 */

/*static*/ emitter::code_t emitter::insEncodeFlags(insCflags flags)
{
    emitter::code_t uimm = (emitter::code_t)flags;
    return uimm;
}

/*****************************************************************************
 *
 *  Returns the encoding for the Shift Count bits to be used for Arm64 encodings
 */

/*static*/ emitter::code_t emitter::insEncodeShiftCount(ssize_t imm, emitAttr size)
{
    assert((imm & 0x003F) == imm);
    assert(((imm & 0x0020) == 0) || (size == EA_8BYTE));

    return (emitter::code_t)imm << 10;
}

/*****************************************************************************
 *
 *  Returns the encoding to select a 64-bit datasize for an Arm64 instruction
 */

/*static*/ emitter::code_t emitter::insEncodeDatasize(emitAttr size)
{
    if (size == EA_8BYTE)
    {
        return 0x80000000; // set the bit at location 31
    }
    else
    {
        assert(size == EA_4BYTE);
        return 0;
    }
}

/*****************************************************************************
 *
 *  Returns the encoding to select the datasize for the general load/store Arm64 instructions
 *
 */

/*static*/ emitter::code_t emitter::insEncodeDatasizeLS(emitter::code_t code, emitAttr size)
{
    bool exclusive = ((code & 0x35000000) == 0);
    bool atomic    = ((code & 0x31200C00) == 0x30200000);

    if ((code & 0x00800000) && !exclusive && !atomic) // Is this a sign-extending opcode? (i.e. ldrsw, ldrsh, ldrsb)
    {
        if ((code & 0x80000000) == 0) // Is it a ldrsh or ldrsb and not ldrsw ?
        {
            if (EA_SIZE(size) != EA_8BYTE) // Do we need to encode the 32-bit Rt size bit?
            {
                return 0x00400000; // set the bit at location 22
            }
        }
    }
    else if (code & 0x80000000) // Is this a ldr/str/ldur/stur opcode?
    {
        if (EA_SIZE(size) == EA_8BYTE) // Do we need to encode the 64-bit size bit?
        {
            return 0x40000000; // set the bit at location 30
        }
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the datasize for the vector load/store Arm64 instructions
 *
 */

/*static*/ emitter::code_t emitter::insEncodeDatasizeVLS(emitter::code_t code, emitAttr size)
{
    code_t result = 0;

    // Check bit 29
    if ((code & 0x20000000) == 0)
    {
        // LDR literal

        if (size == EA_16BYTE)
        {
            // set the operation size in bit 31
            result = 0x80000000;
        }
        else if (size == EA_8BYTE)
        {
            // set the operation size in bit 30
            result = 0x40000000;
        }
        else
        {
            assert(size == EA_4BYTE);
            // no bits are set
            result = 0x00000000;
        }
    }
    else
    {
        // LDR non-literal

        if (size == EA_16BYTE)
        {
            // The operation size in bits 31 and 30 are zero
            // Bit 23 specifies a 128-bit Load/Store
            result = 0x00800000;
        }
        else if (size == EA_8BYTE)
        {
            // set the operation size in bits 31 and 30
            result = 0xC0000000;
        }
        else if (size == EA_4BYTE)
        {
            // set the operation size in bit 31
            result = 0x80000000;
        }
        else if (size == EA_2BYTE)
        {
            // set the operation size in bit 30
            result = 0x40000000;
        }
        else
        {
            assert(size == EA_1BYTE);
            // The operation size in bits 31 and 30 are zero
            result = 0x00000000;
        }
    }

    // Or in bit 26 to indicate a Vector register is used as 'target'
    result |= 0x04000000;

    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the datasize for the vector load/store Arm64 instructions
 *
 */

/*static*/ emitter::code_t emitter::insEncodeDatasizeVPLS(emitter::code_t code, emitAttr size)
{
    code_t result = 0;

    if (size == EA_16BYTE)
    {
        // The operation size in bits 31 and 30 are zero
        // Bit 23 specifies a 128-bit Load/Store
        result = 0x80000000;
    }
    else if (size == EA_8BYTE)
    {
        // set the operation size in bits 31 and 30
        result = 0x40000000;
    }
    else if (size == EA_4BYTE)
    {
        // set the operation size in bit 31
        result = 0x00000000;
    }

    // Or in bit 26 to indicate a Vector register is used as 'target'
    result |= 0x04000000;

    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding to set the size bit and the N bits for a 'bitfield' instruction
 *
 */

/*static*/ emitter::code_t emitter::insEncodeDatasizeBF(emitter::code_t code, emitAttr size)
{
    // is bit 30 equal to 0?
    if ((code & 0x40000000) == 0) // is the opcode one of extr, sxtb, sxth or sxtw
    {
        if (size == EA_8BYTE) // Do we need to set the sf and N bits?
        {
            return 0x80400000; // set the sf-bit at location 31 and the N-bit at location 22
        }
    }
    return 0; // don't set any bits
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 64/128-bit datasize for an Arm64 vector instruction
 */

/*static*/ emitter::code_t emitter::insEncodeVectorsize(emitAttr size)
{
    if (size == EA_16BYTE)
    {
        return 0x40000000; // set the bit at location 30
    }
    else
    {
        assert(size == EA_8BYTE);
        return 0;
    }
}

/*****************************************************************************
 *
 *  Returns the encoding to select 'index' for an Arm64 vector elem instruction
 */
/*static*/ emitter::code_t emitter::insEncodeVectorIndex(emitAttr elemsize, ssize_t index)
{
    code_t bits = (code_t)index;
    if (elemsize == EA_1BYTE)
    {
        bits <<= 1;
        bits |= 1;
    }
    else if (elemsize == EA_2BYTE)
    {
        bits <<= 2;
        bits |= 2;
    }
    else if (elemsize == EA_4BYTE)
    {
        bits <<= 3;
        bits |= 4;
    }
    else
    {
        assert(elemsize == EA_8BYTE);
        bits <<= 4;
        bits |= 8;
    }
    assert((bits >= 1) && (bits <= 0x1f));

    return (bits << 16); // bits at locations [20,19,18,17,16]
}

/*****************************************************************************
 *
 *  Returns the encoding to select 'index2' for an Arm64 'ins' elem instruction
 */
/*static*/ emitter::code_t emitter::insEncodeVectorIndex2(emitAttr elemsize, ssize_t index2)
{
    code_t bits = (code_t)index2;
    if (elemsize == EA_1BYTE)
    {
        // bits are correct
    }
    else if (elemsize == EA_2BYTE)
    {
        bits <<= 1;
    }
    else if (elemsize == EA_4BYTE)
    {
        bits <<= 2;
    }
    else
    {
        assert(elemsize == EA_8BYTE);
        bits <<= 3;
    }
    assert((bits >= 0) && (bits <= 0xf));

    return (bits << 11); // bits at locations [14,13,12,11]
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 'index' for an Arm64 'mul' by elem instruction
 */
/*static*/ emitter::code_t emitter::insEncodeVectorIndexLMH(emitAttr elemsize, ssize_t index)
{
    code_t bits = 0;

    if (elemsize == EA_2BYTE)
    {
        assert((index >= 0) && (index <= 7));
        if (index & 0x4)
        {
            bits |= (1 << 11); // set bit 11 'H'
        }
        if (index & 0x2)
        {
            bits |= (1 << 21); // set bit 21 'L'
        }
        if (index & 0x1)
        {
            bits |= (1 << 20); // set bit 20 'M'
        }
    }
    else if (elemsize == EA_4BYTE)
    {
        assert((index >= 0) && (index <= 3));
        if (index & 0x2)
        {
            bits |= (1 << 11); // set bit 11 'H'
        }
        if (index & 0x1)
        {
            bits |= (1 << 21); // set bit 21 'L'
        }
    }
    else
    {
        assert(!"Invalid 'elemsize' value");
    }

    return bits;
}

/*****************************************************************************
 *
 *   Returns the encoding to shift by 'shift' for an Arm64 vector or scalar instruction
 */

/*static*/ emitter::code_t emitter::insEncodeVectorShift(emitAttr size, ssize_t shift)
{
    assert(shift < getBitWidth(size));

    code_t imm = (code_t)(getBitWidth(size) + shift);

    return imm << 16;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 vector instruction
 */

/*static*/ emitter::code_t emitter::insEncodeElemsize(emitAttr size)
{
    if (size == EA_8BYTE)
    {
        return 0x00C00000; // set the bit at location 23 and 22
    }
    else if (size == EA_4BYTE)
    {
        return 0x00800000; // set the bit at location 23
    }
    else if (size == EA_2BYTE)
    {
        return 0x00400000; // set the bit at location 22
    }
    assert(size == EA_1BYTE);
    return 0x00000000;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the 4/8 byte elemsize for an Arm64 float vector instruction
 */

/*static*/ emitter::code_t emitter::insEncodeFloatElemsize(emitAttr size)
{
    if (size == EA_8BYTE)
    {
        return 0x00400000; // set the bit at location 22
    }
    assert(size == EA_4BYTE);
    return 0x00000000;
}

// Returns the encoding to select the index for an Arm64 float vector by elem instruction
/*static*/ emitter::code_t emitter::insEncodeFloatIndex(emitAttr elemsize, ssize_t index)
{
    code_t result = 0x00000000;
    if (elemsize == EA_8BYTE)
    {
        assert((index >= 0) && (index <= 1));
        if (index == 1)
        {
            result |= 0x00000800; // 'H' - set the bit at location 11
        }
    }
    else
    {
        assert(elemsize == EA_4BYTE);
        assert((index >= 0) && (index <= 3));
        if (index & 2)
        {
            result |= 0x00000800; // 'H' - set the bit at location 11
        }
        if (index & 1)
        {
            result |= 0x00200000; // 'L' - set the bit at location 21
        }
    }
    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding to select the fcvt operation for Arm64 instructions
 */
/*static*/ emitter::code_t emitter::insEncodeConvertOpt(insFormat fmt, insOpts conversion)
{
    code_t result = 0;
    switch (conversion)
    {
        case INS_OPTS_S_TO_D: // Single to Double
            assert(fmt == IF_DV_2J);
            result = 0x00008000; // type=00, opc=01
            break;

        case INS_OPTS_D_TO_S: // Double to Single
            assert(fmt == IF_DV_2J);
            result = 0x00400000; // type=01, opc=00
            break;

        case INS_OPTS_H_TO_S: // Half to Single
            assert(fmt == IF_DV_2J);
            result = 0x00C00000; // type=11, opc=00
            break;

        case INS_OPTS_H_TO_D: // Half to Double
            assert(fmt == IF_DV_2J);
            result = 0x00C08000; // type=11, opc=01
            break;

        case INS_OPTS_S_TO_H: // Single to Half
            assert(fmt == IF_DV_2J);
            result = 0x00018000; // type=00, opc=11
            break;

        case INS_OPTS_D_TO_H: // Double to Half
            assert(fmt == IF_DV_2J);
            result = 0x00418000; // type=01, opc=11
            break;

        case INS_OPTS_S_TO_4BYTE: // Single to INT32
            assert(fmt == IF_DV_2H);
            result = 0x00000000; // sf=0, type=00
            break;

        case INS_OPTS_D_TO_4BYTE: // Double to INT32
            assert(fmt == IF_DV_2H);
            result = 0x00400000; // sf=0, type=01
            break;

        case INS_OPTS_S_TO_8BYTE: // Single to INT64
            assert(fmt == IF_DV_2H);
            result = 0x80000000; // sf=1, type=00
            break;

        case INS_OPTS_D_TO_8BYTE: // Double to INT64
            assert(fmt == IF_DV_2H);
            result = 0x80400000; // sf=1, type=01
            break;

        case INS_OPTS_4BYTE_TO_S: // INT32 to Single
            assert(fmt == IF_DV_2I);
            result = 0x00000000; // sf=0, type=00
            break;

        case INS_OPTS_4BYTE_TO_D: // INT32 to Double
            assert(fmt == IF_DV_2I);
            result = 0x00400000; // sf=0, type=01
            break;

        case INS_OPTS_8BYTE_TO_S: // INT64 to Single
            assert(fmt == IF_DV_2I);
            result = 0x80000000; // sf=1, type=00
            break;

        case INS_OPTS_8BYTE_TO_D: // INT64 to Double
            assert(fmt == IF_DV_2I);
            result = 0x80400000; // sf=1, type=01
            break;

        default:
            assert(!"Invalid 'conversion' value");
            break;
    }
    return result;
}

/*****************************************************************************
 *
 *  Returns the encoding to have the Rn register be updated Pre/Post indexed
 *  or not updated
 */

/*static*/ emitter::code_t emitter::insEncodeIndexedOpt(insOpts opt)
{
    assert(emitter::insOptsNone(opt) || emitter::insOptsIndexed(opt));

    if (emitter::insOptsIndexed(opt))
    {
        if (emitter::insOptsPostIndex(opt))
        {
            return 0x00000400; // set the bit at location 10
        }
        else
        {
            assert(emitter::insOptsPreIndex(opt));
            return 0x00000C00; // set the bit at location 10 and 11
        }
    }
    else
    {
        assert(emitter::insOptsNone(opt));
        return 0; // bits 10 and 11 are zero
    }
}

/*****************************************************************************
 *
 *  Returns the encoding for a ldp/stp instruction to have the Rn register
 *  be updated Pre/Post indexed or not updated
 */

/*static*/ emitter::code_t emitter::insEncodePairIndexedOpt(instruction ins, insOpts opt)
{
    assert(emitter::insOptsNone(opt) || emitter::insOptsIndexed(opt));

    if ((ins == INS_ldnp) || (ins == INS_stnp))
    {
        assert(emitter::insOptsNone(opt));
        return 0; // bits 23 and 24 are zero
    }
    else
    {
        if (emitter::insOptsIndexed(opt))
        {
            if (emitter::insOptsPostIndex(opt))
            {
                return 0x00800000; // set the bit at location 23
            }
            else
            {
                assert(emitter::insOptsPreIndex(opt));
                return 0x01800000; // set the bit at location 24 and 23
            }
        }
        else
        {
            assert(emitter::insOptsNone(opt));
            return 0x01000000; // set the bit at location 24
        }
    }
}

/*****************************************************************************
 *
 *  Returns the encoding to apply a Shift Type on the Rm register
 */

/*static*/ emitter::code_t emitter::insEncodeShiftType(insOpts opt)
{
    if (emitter::insOptsNone(opt))
    {
        // None implies the we encode LSL (with a zero immediate)
        opt = INS_OPTS_LSL;
    }
    assert(emitter::insOptsAnyShift(opt));

    emitter::code_t option = (emitter::code_t)opt - (emitter::code_t)INS_OPTS_LSL;
    assert(option <= 3);

    return option << 22; // bits 23, 22
}

/*****************************************************************************
 *
 *  Returns the encoding to apply a 12 bit left shift to the immediate
 */

/*static*/ emitter::code_t emitter::insEncodeShiftImm12(insOpts opt)
{
    if (emitter::insOptsLSL12(opt))
    {
        return 0x00400000; // set the bit at location 22
    }
    return 0;
}

/*****************************************************************************
 *
 *  Returns the encoding to have the Rm register use an extend operation
 */

/*static*/ emitter::code_t emitter::insEncodeExtend(insOpts opt)
{
    if (emitter::insOptsNone(opt) || (opt == INS_OPTS_LSL))
    {
        // None or LSL implies the we encode UXTX
        opt = INS_OPTS_UXTX;
    }
    assert(emitter::insOptsAnyExtend(opt));

    emitter::code_t option = (emitter::code_t)opt - (emitter::code_t)INS_OPTS_UXTB;
    assert(option <= 7);

    return option << 13; // bits 15,14,13
}

/*****************************************************************************
 *
 *  Returns the encoding to scale the Rm register by {0,1,2,3,4}
 *  when using an extend operation
 */

/*static*/ emitter::code_t emitter::insEncodeExtendScale(ssize_t imm)
{
    assert((imm >= 0) && (imm <= 4));

    return (emitter::code_t)imm << 10; // bits 12,11,10
}

/*****************************************************************************
 *
 *  Returns the encoding to have the Rm register be auto scaled by the ld/st size
 */

/*static*/ emitter::code_t emitter::insEncodeReg3Scale(bool isScaled)
{
    if (isScaled)
    {
        return 0x00001000; // set the bit at location 12
    }
    else
    {
        return 0;
    }
}

BYTE* emitter::emitOutputLoadLabel(BYTE* dst, BYTE* srcAddr, BYTE* dstAddr, instrDescJmp* id)
{
    instruction ins    = id->idIns();
    insFormat   fmt    = id->idInsFmt();
    regNumber   dstReg = id->idReg1();
    if (id->idjShort)
    {
        // adr x, [rel addr] --  compute address: current addr(ip) + rel addr.
        assert(ins == INS_adr);
        assert(fmt == IF_DI_1E);
        ssize_t distVal = (ssize_t)(dstAddr - srcAddr);
        dst             = emitOutputShortAddress(dst, ins, fmt, distVal, dstReg);
    }
    else
    {
        // adrp x, [rel page addr] -- compute page address: current page addr + rel page addr
        assert(fmt == IF_LARGEADR);
        ssize_t relPageAddr = computeRelPageAddr((size_t)dstAddr, (size_t)srcAddr);
        dst                 = emitOutputShortAddress(dst, INS_adrp, IF_DI_1E, relPageAddr, dstReg);

        // add x, x, page offs -- compute address = page addr + page offs
        ssize_t imm12 = (ssize_t)dstAddr & 0xFFF; // 12 bits
        assert(isValidUimm12(imm12));
        code_t code =
            emitInsCode(INS_add, IF_DI_2A);  // DI_2A  X0010001shiiiiii iiiiiinnnnnddddd   1100 0000   imm(i12, sh)
        code |= insEncodeDatasize(EA_8BYTE); // X
        code |= ((code_t)imm12 << 10);       // iiiiiiiiiiii
        code |= insEncodeReg_Rd(dstReg);     // ddddd
        code |= insEncodeReg_Rn(dstReg);     // nnnnn
        dst += emitOutput_Instr(dst, code);
    }
    return dst;
}

/*****************************************************************************
 *
 *  Output a local jump or other instruction with a pc-relative immediate.
 *  Note that this may be invoked to overwrite an existing jump instruction at 'dst'
 *  to handle forward branch patching.
 */

BYTE* emitter::emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* i)
{
    instrDescJmp* id = (instrDescJmp*)i;

    unsigned srcOffs;
    unsigned dstOffs;
    BYTE*    srcAddr;
    BYTE*    dstAddr;
    ssize_t  distVal;

    // Set default ins/fmt from id.
    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    bool loadLabel    = false;
    bool isJump       = false;
    bool loadConstant = false;

    switch (ins)
    {
        default:
            isJump = true;
            break;

        case INS_tbz:
        case INS_tbnz:
        case INS_cbz:
        case INS_cbnz:
            isJump = true;
            break;

        case INS_ldr:
        case INS_ldrsw:
            loadConstant = true;
            break;

        case INS_adr:
        case INS_adrp:
            loadLabel = true;
            break;
    }

    /* Figure out the distance to the target */

    srcOffs = emitCurCodeOffs(dst);
    srcAddr = emitOffsetToPtr(srcOffs);

    if (id->idAddr()->iiaIsJitDataOffset())
    {
        assert(loadConstant || loadLabel);
        int doff = id->idAddr()->iiaGetJitDataOffset();
        assert(doff >= 0);
        ssize_t imm = emitGetInsSC(id);
        assert((imm >= 0) && (imm < 0x1000)); // 0x1000 is arbitrary, currently 'imm' is always 0

        unsigned dataOffs = (unsigned)(doff + imm);
        assert(dataOffs < emitDataSize());
        dstAddr = emitDataOffsetToPtr(dataOffs);

        regNumber dstReg  = id->idReg1();
        regNumber addrReg = dstReg; // an integer register to compute long address.
        emitAttr  opSize  = id->idOpSize();

        if (loadConstant)
        {
            if (id->idjShort)
            {
                // ldr x/v, [rel addr] -- load constant from current addr(ip) + rel addr.
                assert(ins == INS_ldr);
                assert(fmt == IF_LS_1A);
                distVal = (ssize_t)(dstAddr - srcAddr);
                dst     = emitOutputShortConstant(dst, ins, fmt, distVal, dstReg, opSize);
            }
            else
            {
                // adrp x, [rel page addr] -- compute page address: current page addr + rel page addr
                assert(fmt == IF_LARGELDC);
                ssize_t relPageAddr = computeRelPageAddr((size_t)dstAddr, (size_t)srcAddr);
                if (isVectorRegister(dstReg))
                {
                    // Update addrReg with the reserved integer register
                    // since we cannot use dstReg (vector) to load constant directly from memory.
                    addrReg = id->idReg2();
                    assert(isGeneralRegister(addrReg));
                }
                ins = INS_adrp;
                fmt = IF_DI_1E;
                dst = emitOutputShortAddress(dst, ins, fmt, relPageAddr, addrReg);

                // ldr x, [x, page offs] -- load constant from page address + page offset into integer register.
                ssize_t imm12 = (ssize_t)dstAddr & 0xFFF; // 12 bits
                assert(isValidUimm12(imm12));
                ins = INS_ldr;
                fmt = IF_LS_2B;
                dst = emitOutputShortConstant(dst, ins, fmt, imm12, addrReg, opSize);

                // fmov v, d -- copy constant in integer register to vector register.
                // This is needed only for vector constant.
                if (addrReg != dstReg)
                {
                    //  fmov    Vd,Rn                DV_2I  X00111100X100111 000000nnnnnddddd   1E27 0000   Vd,Rn
                    //  (scalar, from general)
                    assert(isVectorRegister(dstReg) && isGeneralRegister(addrReg));
                    ins         = INS_fmov;
                    fmt         = IF_DV_2I;
                    code_t code = emitInsCode(ins, fmt);

                    code |= insEncodeReg_Vd(dstReg);  // ddddd
                    code |= insEncodeReg_Rn(addrReg); // nnnnn
                    if (id->idOpSize() == EA_8BYTE)
                    {
                        code |= 0x80400000; // X ... X
                    }
                    dst += emitOutput_Instr(dst, code);
                }
            }
        }
        else
        {
            assert(loadLabel);
            dst = emitOutputLoadLabel(dst, srcAddr, dstAddr, id);
        }

        return dst;
    }

    assert(loadLabel || isJump);

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
        dstAddr = emitOffsetToPtr(dstOffs);
    }
    else
    {
        dstOffs = id->idAddr()->iiaIGlabel->igOffs;
        dstAddr = emitOffsetToPtr(dstOffs);
    }

    distVal = (ssize_t)(dstAddr - srcAddr);

    if (dstOffs <= srcOffs)
    {
#if DEBUG_EMIT
        /* This is a backward jump - distance is known at this point */

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

#ifdef DEBUG
    if (0 && emitComp->verbose)
    {
        size_t sz          = 4;
        int    distValSize = id->idjShort ? 4 : 8;
        printf("; %s jump [%08X/%03u] from %0*X to %0*X: dist = %08XH\n", (dstOffs <= srcOffs) ? "Fwd" : "Bwd",
               dspPtr(id), id->idDebugOnlyInfo()->idNum, distValSize, srcOffs + sz, distValSize, dstOffs, distVal);
    }
#endif

    /* For forward jumps, record the address of the distance value */
    id->idjTemp.idjAddr = (distVal > 0) ? dst : NULL;

    if (emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
    {
        assert(!id->idjShort);
        NYI_ARM64("Relocation Support for long address");
    }

    assert(insOptsNone(id->idInsOpt()));

    if (isJump)
    {
        if (id->idjShort)
        {
            // Short conditional/unconditional jump
            assert(!id->idjKeepLong);
            assert(emitJumpCrossHotColdBoundary(srcOffs, dstOffs) == false);
            assert((fmt == IF_BI_0A) || (fmt == IF_BI_0B) || (fmt == IF_BI_1A) || (fmt == IF_BI_1B));
        }
        else
        {
            // Long conditional jump
            assert(fmt == IF_LARGEJMP);
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
            //      b<!cond> L_not  // 4 bytes. Note that we reverse the condition.
            //      b L_target      // 4 bytes
            //   L_not:
            //
            // Note that we don't actually insert any blocks: we simply encode "b <!cond> L_not" as a branch with
            // the correct offset. Note also that this works for both integer and floating-point conditions, because
            // the condition inversion takes ordered/unordered into account, preserving NaN behavior. For example,
            // "GT" (greater than) is inverted to "LE" (less than, equal, or unordered).

            instruction reverseIns;
            insFormat   reverseFmt;

            switch (ins)
            {
                case INS_cbz:
                    reverseIns = INS_cbnz;
                    reverseFmt = IF_BI_1A;
                    break;
                case INS_cbnz:
                    reverseIns = INS_cbz;
                    reverseFmt = IF_BI_1A;
                    break;
                case INS_tbz:
                    reverseIns = INS_tbnz;
                    reverseFmt = IF_BI_1B;
                    break;
                case INS_tbnz:
                    reverseIns = INS_tbz;
                    reverseFmt = IF_BI_1B;
                    break;
                default:
                    reverseIns = emitJumpKindToIns(emitReverseJumpKind(emitInsToJumpKind(ins)));
                    reverseFmt = IF_BI_0B;
            }

            dst =
                emitOutputShortBranch(dst,
                                      reverseIns, // reverse the conditional instruction
                                      reverseFmt,
                                      8, /* 8 bytes from start of this large conditional pseudo-instruction to L_not. */
                                      id);

            // Now, pretend we've got a normal unconditional branch, and fall through to the code to emit that.
            ins = INS_b;
            fmt = IF_BI_0A;

            // The distVal was computed based on the beginning of the pseudo-instruction,
            // So subtract the size of the conditional branch so that it is relative to the
            // unconditional branch.
            distVal -= 4;
        }

        dst = emitOutputShortBranch(dst, ins, fmt, distVal, id);
    }
    else if (loadLabel)
    {
        dst = emitOutputLoadLabel(dst, srcAddr, dstAddr, id);
    }

    return dst;
}

/*****************************************************************************
*
*  Output a short branch instruction.
*/
BYTE* emitter::emitOutputShortBranch(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, instrDescJmp* id)
{
    code_t code = emitInsCode(ins, fmt);

    ssize_t loBits = (distVal & 3);
    noway_assert(loBits == 0);
    distVal >>= 2; // branch offset encodings are scaled by 4.

    if (fmt == IF_BI_0A)
    {
        // INS_b or INS_bl_local
        noway_assert(isValidSimm26(distVal));
        distVal &= 0x3FFFFFFLL;
        code |= distVal;
    }
    else if (fmt == IF_BI_0B) // BI_0B   01010100iiiiiiii iiiiiiiiiiiXXXXX      simm19:00
    {
        // INS_beq, INS_bne, etc...
        noway_assert(isValidSimm19(distVal));
        distVal &= 0x7FFFFLL;
        code |= distVal << 5;
    }
    else if (fmt == IF_BI_1A) // BI_1A   X.......iiiiiiii iiiiiiiiiiittttt      Rt simm19:00
    {
        // INS_cbz or INS_cbnz
        assert(id != nullptr);
        code |= insEncodeDatasize(id->idOpSize()); // X
        code |= insEncodeReg_Rt(id->idReg1());     // ttttt

        noway_assert(isValidSimm19(distVal));
        distVal &= 0x7FFFFLL; // 19 bits
        code |= distVal << 5;
    }
    else if (fmt == IF_BI_1B) // BI_1B   B.......bbbbbiii iiiiiiiiiiittttt      Rt imm6, simm14:00
    {
        // INS_tbz or INS_tbnz
        assert(id != nullptr);
        ssize_t imm = emitGetInsSC(id);
        assert(isValidImmShift(imm, id->idOpSize()));

        if (imm & 0x20) // test bit 32-63 ?
        {
            code |= 0x80000000; // B
        }
        code |= ((imm & 0x1F) << 19);          // bbbbb
        code |= insEncodeReg_Rt(id->idReg1()); // ttttt

        noway_assert(isValidSimm14(distVal));
        distVal &= 0x3FFFLL; // 14 bits
        code |= distVal << 5;
    }
    else
    {
        assert(!"Unknown fmt for emitOutputShortBranch");
    }

    dst += emitOutput_Instr(dst, code);

    return dst;
}

/*****************************************************************************
*
*  Output a short address instruction.
*/
BYTE* emitter::emitOutputShortAddress(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, regNumber reg)
{
    ssize_t loBits = (distVal & 3);
    distVal >>= 2;

    code_t code = emitInsCode(ins, fmt);
    if (fmt == IF_DI_1E) // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd simm21
    {
        // INS_adr or INS_adrp
        code |= insEncodeReg_Rd(reg); // ddddd

        noway_assert(isValidSimm19(distVal));
        distVal &= 0x7FFFFLL; // 19 bits
        code |= distVal << 5;
        code |= loBits << 29; //  2 bits
    }
    else
    {
        assert(!"Unknown fmt for emitOutputShortAddress");
    }

    dst += emitOutput_Instr(dst, code);

    return dst;
}

/*****************************************************************************
*
*  Output a short constant instruction.
*/
BYTE* emitter::emitOutputShortConstant(
    BYTE* dst, instruction ins, insFormat fmt, ssize_t imm, regNumber reg, emitAttr opSize)
{
    code_t code = emitInsCode(ins, fmt);

    if (fmt == IF_LS_1A)
    {
        // LS_1A   XX...V..iiiiiiii iiiiiiiiiiittttt      Rt simm21
        // INS_ldr or INS_ldrsw (PC-Relative)

        ssize_t loBits = (imm & 3);
        noway_assert(loBits == 0);
        ssize_t distVal = imm >>= 2; // load offset encodings are scaled by 4.

        noway_assert(isValidSimm19(distVal));

        // Is the target a vector register?
        if (isVectorRegister(reg))
        {
            code |= insEncodeDatasizeVLS(code, opSize); // XX V
            code |= insEncodeReg_Vt(reg);               // ttttt
        }
        else
        {
            assert(isGeneralRegister(reg));
            // insEncodeDatasizeLS is not quite right for this case.
            // So just specialize it.
            if ((ins == INS_ldr) && (opSize == EA_8BYTE))
            {
                // set the operation size in bit 30
                code |= 0x40000000;
            }

            code |= insEncodeReg_Rt(reg); // ttttt
        }

        distVal &= 0x7FFFFLL; // 19 bits
        code |= distVal << 5;
    }
    else if (fmt == IF_LS_2B)
    {
        //  ldr     Rt,[Xn+pimm12]       LS_2B  1X11100101iiiiii iiiiiinnnnnttttt   B940 0000   imm(0-4095<<{2,3})
        // INS_ldr or INS_ldrsw (PC-Relative)
        noway_assert(isValidUimm12(imm));
        assert(isGeneralRegister(reg));

        if (opSize == EA_8BYTE)
        {
            // insEncodeDatasizeLS is not quite right for this case.
            // So just specialize it.
            if (ins == INS_ldr)
            {
                // set the operation size in bit 30
                code |= 0x40000000;
            }
            // Low 3 bits should be 0 -- 8 byte JIT data should be aligned on 8 byte.
            assert((imm & 7) == 0);
            imm >>= 3;
        }
        else
        {
            assert(opSize == EA_4BYTE);
            // Low 2 bits should be 0 -- 4 byte aligned data.
            assert((imm & 3) == 0);
            imm >>= 2;
        }

        code |= insEncodeReg_Rt(reg); // ttttt
        code |= insEncodeReg_Rn(reg); // nnnnn
        code |= imm << 10;
    }
    else
    {
        assert(!"Unknown fmt for emitOutputShortConstant");
    }

    dst += emitOutput_Instr(dst, code);

    return dst;
}
/*****************************************************************************
 *
 *  Output a call instruction.
 */

unsigned emitter::emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code)
{
    const unsigned char callInstrSize = sizeof(code_t); // 4 bytes
    regMaskTP           gcrefRegs;
    regMaskTP           byrefRegs;

    VARSET_TP GCvars(VarSetOps::UninitVal());

    // Is this a "fat" call descriptor?
    if (id->idIsLargeCall())
    {
        instrDescCGCA* idCall = (instrDescCGCA*)id;
        gcrefRegs             = idCall->idcGcrefRegs;
        byrefRegs             = idCall->idcByrefRegs;
        VarSetOps::Assign(emitComp, GCvars, idCall->idcGCvars);
    }
    else
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCns());

        gcrefRegs = emitDecodeCallGCregs(id);
        byrefRegs = 0;
        VarSetOps::AssignNoCopy(emitComp, GCvars, VarSetOps::MakeEmpty(emitComp));
    }

    /* We update the GC info before the call as the variables cannot be
        used by the call. Killing variables before the call helps with
        boundary conditions if the call is CORINFO_HELP_THROW - see bug 50029.
        If we ever track aliased variables (which could be used by the
        call), we would have to keep them alive past the call. */

    emitUpdateLiveGCvars(GCvars, dst);

    // Now output the call instruction and update the 'dst' pointer
    //
    unsigned outputInstrSize = emitOutput_Instr(dst, code);
    dst += outputInstrSize;

    // All call instructions are 4-byte in size on ARM64
    //
    assert(outputInstrSize == callInstrSize);

    // If the method returns a GC ref, mark INTRET (R0) appropriately.
    if (id->idGCref() == GCT_GCREF)
    {
        gcrefRegs |= RBM_INTRET;
    }
    else if (id->idGCref() == GCT_BYREF)
    {
        byrefRegs |= RBM_INTRET;
    }

    // If is a multi-register return method is called, mark INTRET_1 (X1) appropriately
    if (id->idIsLargeCall())
    {
        instrDescCGCA* idCall = (instrDescCGCA*)id;
        if (idCall->idSecondGCref() == GCT_GCREF)
        {
            gcrefRegs |= RBM_INTRET_1;
        }
        else if (idCall->idSecondGCref() == GCT_BYREF)
        {
            byrefRegs |= RBM_INTRET_1;
        }
    }

    // If the GC register set has changed, report the new set.
    if (gcrefRegs != emitThisGCrefRegs)
    {
        emitUpdateLiveGCregs(GCT_GCREF, gcrefRegs, dst);
    }
    // If the Byref register set has changed, report the new set.
    if (byrefRegs != emitThisByrefRegs)
    {
        emitUpdateLiveGCregs(GCT_BYREF, byrefRegs, dst);
    }

    // Some helper calls may be marked as not requiring GC info to be recorded.
    if ((!id->idIsNoGC()))
    {
        // On ARM64, as on AMD64, we don't change the stack pointer to push/pop args.
        // So we're not really doing a "stack pop" here (note that "args" is 0), but we use this mechanism
        // to record the call for GC info purposes.  (It might be best to use an alternate call,
        // and protect "emitStackPop" under the EMIT_TRACK_STACK_DEPTH preprocessor variable.)
        emitStackPop(dst, /*isCall*/ true, callInstrSize, /*args*/ 0);

        // Do we need to record a call location for GC purposes?
        //
        if (!emitFullGCinfo)
        {
            emitRecordGCcall(dst, callInstrSize);
        }
    }
    return callInstrSize;
}

/*****************************************************************************
 *
 *  Emit a 32-bit Arm64 instruction
 */

/*static*/ unsigned emitter::emitOutput_Instr(BYTE* dst, code_t code)
{
    assert(sizeof(code_t) == 4);
    *((code_t*)dst) = code;

    return sizeof(code_t);
}

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
    BYTE*       dst  = *dp;
    BYTE*       odst = dst;
    code_t      code = 0;
    size_t      sz   = emitGetInstrDescSize(id); // TODO-ARM64-Cleanup: on ARM, this is set in each case. why?
    instruction ins  = id->idIns();
    insFormat   fmt  = id->idInsFmt();
    emitAttr    size = id->idOpSize();

#ifdef DEBUG
#if DUMP_GC_TABLES
    bool dspOffs = emitComp->opts.dspGCtbls;
#else
    bool dspOffs = !emitComp->opts.disDiffable;
#endif
#endif // DEBUG

    assert(REG_NA == (int)REG_NA);

    /* What instruction format have we got? */

    switch (fmt)
    {
        ssize_t  imm;
        ssize_t  index;
        ssize_t  index2;
        unsigned cmode;
        unsigned immShift;
        emitAttr elemsize;
        emitAttr datasize;

        case IF_BI_0A: // BI_0A   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
        case IF_BI_0B: // BI_0B   ......iiiiiiiiii iiiiiiiiiii.....               simm19:00
        case IF_LARGEJMP:
            assert(id->idGCref() == GCT_NONE);
            assert(id->idIsBound());
            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_BI_0C: // BI_0C   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
            code = emitInsCode(ins, fmt);
            sz   = id->idIsLargeCall() ? sizeof(instrDescCGCA) : sizeof(instrDesc);
            dst += emitOutputCall(ig, dst, id, code);
            // Always call RecordRelocation so that we wire in a JumpStub when we don't reach
            emitRecordRelocation(odst, id->idAddr()->iiaAddr, IMAGE_REL_ARM64_BRANCH26);
            break;

        case IF_BI_1A: // BI_1A   ......iiiiiiiiii iiiiiiiiiiittttt      Rt       simm19:00
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_BI_1B: // BI_1B   B.......bbbbbiii iiiiiiiiiiittttt      Rt imm6, simm14:00
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_BR_1A: // BR_1A   ................ ......nnnnn.....         Rn
            assert(insOptsNone(id->idInsOpt()));
            assert((ins == INS_ret) || (ins == INS_br));
            code = emitInsCode(ins, fmt);
            code |= insEncodeReg_Rn(id->idReg1()); // nnnnn

            dst += emitOutput_Instr(dst, code);
            break;

        case IF_BR_1B: // BR_1B   ................ ......nnnnn.....         Rn
            assert(insOptsNone(id->idInsOpt()));
            assert((ins == INS_br_tail) || (ins == INS_blr));
            code = emitInsCode(ins, fmt);
            code |= insEncodeReg_Rn(id->idReg3()); // nnnnn

            sz = id->idIsLargeCall() ? sizeof(instrDescCGCA) : sizeof(instrDesc);
            dst += emitOutputCall(ig, dst, id, code);
            break;

        case IF_LS_1A: // LS_1A   XX...V..iiiiiiii iiiiiiiiiiittttt      Rt    PC imm(1MB)
        case IF_LARGELDC:
            assert(insOptsNone(id->idInsOpt()));
            assert(id->idIsBound());

            dst = emitOutputLJ(ig, dst, id);
            sz  = sizeof(instrDescJmp);
            break;

        case IF_LS_2A: // LS_2A   .X.......X...... ......nnnnnttttt      Rt Rn
            assert(insOptsNone(id->idInsOpt()));
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                 // clear the size bits
                code |= insEncodeDatasizeVLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());              // ttttt
            }
            else
            {
                code |= insEncodeDatasizeLS(code, id->idOpSize()); // .X.......X
                code |= insEncodeReg_Rt(id->idReg1());             // ttttt
            }
            code |= insEncodeReg_Rn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_2B: // LS_2B   .X.......Xiiiiii iiiiiinnnnnttttt      Rt Rn    imm(0-4095)
            assert(insOptsNone(id->idInsOpt()));
            imm = emitGetInsSC(id);
            assert(isValidUimm12(imm));
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                 // clear the size bits
                code |= insEncodeDatasizeVLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());              // ttttt
            }
            else
            {
                code |= insEncodeDatasizeLS(code, id->idOpSize()); // .X.......X
                code |= insEncodeReg_Rt(id->idReg1());             // ttttt
            }
            code |= ((code_t)imm << 10);           // iiiiiiiiiiii
            code |= insEncodeReg_Rn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_2C: // LS_2C   .X.......X.iiiii iiiiPPnnnnnttttt      Rt Rn    imm(-256..+255) no/pre/post inc
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            imm = emitGetInsSC(id);
            assert((imm >= -256) && (imm <= 255)); // signed 9 bits
            imm &= 0x1ff;                          // force into unsigned 9 bit representation
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                 // clear the size bits
                code |= insEncodeDatasizeVLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());              // ttttt
            }
            else
            {
                code |= insEncodeDatasizeLS(code, id->idOpSize()); // .X.......X
                code |= insEncodeReg_Rt(id->idReg1());             // ttttt
            }
            code |= insEncodeIndexedOpt(id->idInsOpt()); // PP
            code |= ((code_t)imm << 12);                 // iiiiiiiii
            code |= insEncodeReg_Rn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_3A: // LS_3A   .X.......X.mmmmm oooS..nnnnnttttt      Rt Rn Rm ext(Rm) LSL {}
            assert(insOptsLSExtend(id->idInsOpt()));
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                 // clear the size bits
                code |= insEncodeDatasizeVLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());              // ttttt
            }
            else
            {
                code |= insEncodeDatasizeLS(code, id->idOpSize()); // .X.......X
                code |= insEncodeReg_Rt(id->idReg1());             // ttttt
            }
            code |= insEncodeExtend(id->idInsOpt()); // ooo
            code |= insEncodeReg_Rn(id->idReg2());   // nnnnn
            if (id->idIsLclVar())
            {
                code |= insEncodeReg_Rm(codeGen->rsGetRsvdReg()); // mmmmm
            }
            else
            {
                code |= insEncodeReg3Scale(id->idReg3Scaled()); // S
                code |= insEncodeReg_Rm(id->idReg3());          // mmmmm
            }
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_3B: // LS_3B   X............... .aaaaannnnnddddd      Rd Ra Rn
            assert(insOptsNone(id->idInsOpt()));
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                  // clear the size bits
                code |= insEncodeDatasizeVPLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());               // ttttt
                code |= insEncodeReg_Va(id->idReg2());               // aaaaa
            }
            else
            {
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rt(id->idReg1());     // ttttt
                code |= insEncodeReg_Ra(id->idReg2());     // aaaaa
            }
            code |= insEncodeReg_Rn(id->idReg3()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_3C: // LS_3C   X......PP.iiiiii iaaaaannnnnddddd      Rd Ra Rn imm(im7,sh)
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            imm = emitGetInsSC(id);
            assert((imm >= -64) && (imm <= 63)); // signed 7 bits
            imm &= 0x7f;                         // force into unsigned 7 bit representation
            code = emitInsCode(ins, fmt);
            // Is the target a vector register?
            if (isVectorRegister(id->idReg1()))
            {
                code &= 0x3FFFFFFF;                                  // clear the size bits
                code |= insEncodeDatasizeVPLS(code, id->idOpSize()); // XX
                code |= insEncodeReg_Vt(id->idReg1());               // ttttt
                code |= insEncodeReg_Va(id->idReg2());               // aaaaa
            }
            else
            {
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rt(id->idReg1());     // ttttt
                code |= insEncodeReg_Ra(id->idReg2());     // aaaaa
            }
            code |= insEncodePairIndexedOpt(ins, id->idInsOpt()); // PP
            code |= ((code_t)imm << 15);                          // iiiiiiiii
            code |= insEncodeReg_Rn(id->idReg3());                // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_3D: // LS_3D   .X.......X.mmmmm ......nnnnnttttt      Wm Rt Rn
            code = emitInsCode(ins, fmt);
            // Arm64 store exclusive unpredictable cases
            assert(id->idReg1() != id->idReg2());
            assert(id->idReg1() != id->idReg3());
            code |= insEncodeDatasizeLS(code, id->idOpSize()); // X
            code |= insEncodeReg_Rm(id->idReg1());             // mmmmm
            code |= insEncodeReg_Rt(id->idReg2());             // ttttt
            code |= insEncodeReg_Rn(id->idReg3());             // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_LS_3E: // LS_3E   .X.........mmmmm ......nnnnnttttt      Rm Rt Rn ARMv8.1 LSE Atomics
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasizeLS(code, id->idOpSize()); // X
            code |= insEncodeReg_Rm(id->idReg1());             // mmmmm
            code |= insEncodeReg_Rt(id->idReg2());             // ttttt
            code |= insEncodeReg_Rn(id->idReg3());             // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_1A: // DI_1A   X.......shiiiiii iiiiiinnnnn.....         Rn    imm(i12,sh)
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL12(id->idInsOpt()));
            imm = emitGetInsSC(id);
            assert(isValidUimm12(imm));
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize());   // X
            code |= insEncodeShiftImm12(id->idInsOpt()); // sh
            code |= ((code_t)imm << 10);                 // iiiiiiiiiiii
            code |= insEncodeReg_Rn(id->idReg1());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_1B: // DI_1B   X........hwiiiii iiiiiiiiiiiddddd      Rd       imm(i16,hw)
            imm = emitGetInsSC(id);
            assert(isValidImmHWVal(imm, id->idOpSize()));
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= ((code_t)imm << 5);                // hwiiiii iiiiiiiiiii
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_1C: // DI_1C   X........Nrrrrrr ssssssnnnnn.....         Rn    imm(N,r,s)
            imm = emitGetInsSC(id);
            assert(isValidImmNRS(imm, id->idOpSize()));
            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 10);               // Nrrrrrrssssss
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rn(id->idReg1());     // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_1D: // DI_1D   X........Nrrrrrr ssssss.....ddddd      Rd       imm(N,r,s)
            imm = emitGetInsSC(id);
            assert(isValidImmNRS(imm, id->idOpSize()));
            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 10);               // Nrrrrrrssssss
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_1E: // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd       simm21
        case IF_LARGEADR:
            assert(insOptsNone(id->idInsOpt()));
            if (id->idIsReloc())
            {
                code = emitInsCode(ins, fmt);
                code |= insEncodeReg_Rd(id->idReg1()); // ddddd
                dst += emitOutput_Instr(dst, code);
                emitRecordRelocation(odst, id->idAddr()->iiaAddr, IMAGE_REL_ARM64_PAGEBASE_REL21);
            }
            else
            {
                // Local jmp/load case which does not need a relocation.
                assert(id->idIsBound());
                dst = emitOutputLJ(ig, dst, id);
            }
            sz = sizeof(instrDescJmp);
            break;

        case IF_DI_1F: // DI_1F   X..........iiiii cccc..nnnnn.nzcv      Rn imm5  nzcv cond
            imm = emitGetInsSC(id);
            assert(isValidImmCondFlagsImm5(imm));
            {
                condFlagsImm cfi;
                cfi.immCFVal = (unsigned)imm;
                code         = emitInsCode(ins, fmt);
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rn(id->idReg1());     // nnnnn
                code |= ((code_t)cfi.imm5 << 16);          // iiiii
                code |= insEncodeFlags(cfi.flags);         // nzcv
                code |= insEncodeCond(cfi.cond);           // cccc
                dst += emitOutput_Instr(dst, code);
            }
            break;

        case IF_DI_2A: // DI_2A   X.......shiiiiii iiiiiinnnnnddddd      Rd Rn    imm(i12,sh)
            assert(insOptsNone(id->idInsOpt()) || insOptsLSL12(id->idInsOpt()));
            imm = emitGetInsSC(id);
            assert(isValidUimm12(imm));
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize());   // X
            code |= insEncodeShiftImm12(id->idInsOpt()); // sh
            code |= ((code_t)imm << 10);                 // iiiiiiiiiiii
            code |= insEncodeReg_Rd(id->idReg1());       // ddddd
            code |= insEncodeReg_Rn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);

            if (id->idIsReloc())
            {
                assert(sz == sizeof(instrDesc));
                assert(id->idAddr()->iiaAddr != nullptr);
                emitRecordRelocation(odst, id->idAddr()->iiaAddr, IMAGE_REL_ARM64_PAGEOFFSET_12A);
            }
            break;

        case IF_DI_2B: // DI_2B   X.........Xnnnnn ssssssnnnnnddddd      Rd Rn    imm(0-63)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert(isValidImmShift(imm, id->idOpSize()));
            code |= insEncodeDatasizeBF(code, id->idOpSize()); // X........X
            code |= insEncodeReg_Rd(id->idReg1());             // ddddd
            code |= insEncodeReg_Rn(id->idReg2());             // nnnnn
            code |= insEncodeReg_Rm(id->idReg2());             // Reg2 also in mmmmm
            code |= insEncodeShiftCount(imm, id->idOpSize());  // ssssss
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_2C: // DI_2C   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imm(N,r,s)
            imm = emitGetInsSC(id);
            assert(isValidImmNRS(imm, id->idOpSize()));
            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 10);               // Nrrrrrrssssss
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DI_2D: // DI_2D   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imr, imms   (N,r,s)
            if (ins == INS_asr || ins == INS_lsl || ins == INS_lsr)
            {
                imm = emitGetInsSC(id);
                assert(isValidImmShift(imm, id->idOpSize()));

                // Shift immediates are aliases of the SBFM/UBFM instructions
                // that actually take 2 registers and 2 constants,
                // Since we stored the shift immediate value
                // we need to calculate the N,R and S values here.

                bitMaskImm bmi;
                bmi.immNRS = 0;

                bmi.immN = (size == EA_8BYTE) ? 1 : 0;
                bmi.immR = imm;
                bmi.immS = (size == EA_8BYTE) ? 0x3f : 0x1f;

                // immR and immS are now set correctly for INS_asr and INS_lsr
                // but for INS_lsl we have to adjust the values for immR and immS
                //
                if (ins == INS_lsl)
                {
                    bmi.immR = -imm & bmi.immS;
                    bmi.immS = bmi.immS - imm;
                }

                // setup imm with the proper 13 bit value N:R:S
                //
                imm = bmi.immNRS;
            }
            else
            {
                // The other instructions have already have encoded N,R and S values
                imm = emitGetInsSC(id);
            }
            assert(isValidImmNRS(imm, id->idOpSize()));

            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 10);               // Nrrrrrrssssss
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_1D: // DR_1D   X............... cccc.......ddddd      Rd       cond
            imm = emitGetInsSC(id);
            assert(isValidImmCond(imm));
            {
                condFlagsImm cfi;
                cfi.immCFVal = (unsigned)imm;
                code         = emitInsCode(ins, fmt);
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rd(id->idReg1());     // ddddd
                code |= insEncodeInvertedCond(cfi.cond);   // cccc
                dst += emitOutput_Instr(dst, code);
            }
            break;

        case IF_DR_2A: // DR_2A   X..........mmmmm ......nnnnn.....         Rn Rm
            assert(insOptsNone(id->idInsOpt()));
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rn(id->idReg1());     // nnnnn
            code |= insEncodeReg_Rm(id->idReg2());     // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2B: // DR_2B   X.......sh.mmmmm ssssssnnnnn.....         Rn Rm {LSL,LSR,ASR,ROR} imm(0-63)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert(isValidImmShift(imm, id->idOpSize()));
            code |= insEncodeDatasize(id->idOpSize());        // X
            code |= insEncodeShiftType(id->idInsOpt());       // sh
            code |= insEncodeShiftCount(imm, id->idOpSize()); // ssssss
            code |= insEncodeReg_Rn(id->idReg1());            // nnnnn
            code |= insEncodeReg_Rm(id->idReg2());            // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2C: // DR_2C   X..........mmmmm ooosssnnnnn.....         Rn Rm ext(Rm) LSL imm(0-4)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert((imm >= 0) && (imm <= 4));          // imm [0..4]
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeExtend(id->idInsOpt());   // ooo
            code |= insEncodeExtendScale(imm);         // sss
            code |= insEncodeReg_Rn(id->idReg1());     // nnnnn
            code |= insEncodeReg_Rm(id->idReg2());     // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2D: // DR_2D   X..........nnnnn cccc..nnnnnddddd      Rd Rn    cond
            imm = emitGetInsSC(id);
            assert(isValidImmCond(imm));
            {
                condFlagsImm cfi;
                cfi.immCFVal = (unsigned)imm;
                code         = emitInsCode(ins, fmt);
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rd(id->idReg1());     // ddddd
                code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
                code |= insEncodeReg_Rm(id->idReg2());     // mmmmm
                code |= insEncodeInvertedCond(cfi.cond);   // cccc
                dst += emitOutput_Instr(dst, code);
            }
            break;

        case IF_DR_2E: // DR_2E   X..........mmmmm ...........ddddd      Rd    Rm
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rm(id->idReg2());     // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2F: // DR_2F   X.......sh.mmmmm ssssss.....ddddd      Rd    Rm {LSL,LSR,ASR} imm(0-63)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert(isValidImmShift(imm, id->idOpSize()));
            code |= insEncodeDatasize(id->idOpSize());        // X
            code |= insEncodeShiftType(id->idInsOpt());       // sh
            code |= insEncodeShiftCount(imm, id->idOpSize()); // ssssss
            code |= insEncodeReg_Rd(id->idReg1());            // ddddd
            code |= insEncodeReg_Rm(id->idReg2());            // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2G: // DR_2G   X............... .....xnnnnnddddd      Rd Rn
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            if (ins == INS_rev)
            {
                if (size == EA_8BYTE)
                {
                    code |= 0x00000400; // x - bit at location 10
                }
            }
            code |= insEncodeReg_Rd(id->idReg1()); // ddddd
            code |= insEncodeReg_Rn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2H: // DR_2H   X........X...... ......nnnnnddddd      Rd Rn
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasizeBF(code, id->idOpSize()); // X........X
            code |= insEncodeReg_Rd(id->idReg1());             // ddddd
            code |= insEncodeReg_Rn(id->idReg2());             // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_2I: // DR_2I   X..........mmmmm cccc..nnnnn.nzcv      Rn Rm    nzcv cond
            imm = emitGetInsSC(id);
            assert(isValidImmCondFlags(imm));
            {
                condFlagsImm cfi;
                cfi.immCFVal = (unsigned)imm;
                code         = emitInsCode(ins, fmt);
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rn(id->idReg1());     // nnnnn
                code |= insEncodeReg_Rm(id->idReg2());     // mmmmm
                code |= insEncodeFlags(cfi.flags);         // nzcv
                code |= insEncodeCond(cfi.cond);           // cccc
                dst += emitOutput_Instr(dst, code);
            }
            break;

        case IF_DR_2J: // DR_2J   ................ ......nnnnnddddd      Sd Sn   (sha1h)
            code = emitInsCode(ins, fmt);
            code |= insEncodeReg_Vd(id->idReg1()); // ddddd
            code |= insEncodeReg_Vn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_3A: // DR_3A   X..........mmmmm ......nnnnnmmmmm      Rd Rn Rm
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
            if (id->idIsLclVar())
            {
                code |= insEncodeReg_Rm(codeGen->rsGetRsvdReg()); // mmmmm
            }
            else
            {
                code |= insEncodeReg_Rm(id->idReg3()); // mmmmm
            }
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_3B: // DR_3B   X.......sh.mmmmm ssssssnnnnnddddd      Rd Rn Rm {LSL,LSR,ASR} imm(0-63)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert(isValidImmShift(imm, id->idOpSize()));
            code |= insEncodeDatasize(id->idOpSize());        // X
            code |= insEncodeReg_Rd(id->idReg1());            // ddddd
            code |= insEncodeReg_Rn(id->idReg2());            // nnnnn
            code |= insEncodeReg_Rm(id->idReg3());            // mmmmm
            code |= insEncodeShiftType(id->idInsOpt());       // sh
            code |= insEncodeShiftCount(imm, id->idOpSize()); // ssssss
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_3C: // DR_3C   X..........mmmmm ooosssnnnnnddddd      Rd Rn Rm ext(Rm) LSL imm(0-4)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert((imm >= 0) && (imm <= 4));          // imm [0..4]
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeExtend(id->idInsOpt());   // ooo
            code |= insEncodeExtendScale(imm);         // sss
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
            code |= insEncodeReg_Rm(id->idReg3());     // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_3D: // DR_3D   X..........mmmmm cccc..nnnnnddddd      Rd Rn Rm cond
            imm = emitGetInsSC(id);
            assert(isValidImmCond(imm));
            {
                condFlagsImm cfi;
                cfi.immCFVal = (unsigned)imm;
                code         = emitInsCode(ins, fmt);
                code |= insEncodeDatasize(id->idOpSize()); // X
                code |= insEncodeReg_Rd(id->idReg1());     // ddddd
                code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
                code |= insEncodeReg_Rm(id->idReg3());     // mmmmm
                code |= insEncodeCond(cfi.cond);           // cccc
                dst += emitOutput_Instr(dst, code);
            }
            break;

        case IF_DR_3E: // DR_3E   X........X.mmmmm ssssssnnnnnddddd      Rd Rn Rm imm(0-63)
            code = emitInsCode(ins, fmt);
            imm  = emitGetInsSC(id);
            assert(isValidImmShift(imm, id->idOpSize()));
            code |= insEncodeDatasizeBF(code, id->idOpSize()); // X........X
            code |= insEncodeReg_Rd(id->idReg1());             // ddddd
            code |= insEncodeReg_Rn(id->idReg2());             // nnnnn
            code |= insEncodeReg_Rm(id->idReg3());             // mmmmm
            code |= insEncodeShiftCount(imm, id->idOpSize());  // ssssss
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DR_4A: // DR_4A   X..........mmmmm .aaaaannnnnmmmmm      Rd Rn Rm Ra
            code = emitInsCode(ins, fmt);
            code |= insEncodeDatasize(id->idOpSize()); // X
            code |= insEncodeReg_Rd(id->idReg1());     // ddddd
            code |= insEncodeReg_Rn(id->idReg2());     // nnnnn
            code |= insEncodeReg_Rm(id->idReg3());     // mmmmm
            code |= insEncodeReg_Ra(id->idReg4());     // aaaaa
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_1A: // DV_1A   .........X.iiiii iii........ddddd      Vd imm8    (fmov - immediate scalar)
            imm      = emitGetInsSC(id);
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeFloatElemsize(elemsize); // X
            code |= ((code_t)imm << 13);              // iiiii iii
            code |= insEncodeReg_Vd(id->idReg1());    // ddddd
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_1B: // DV_1B   .QX..........iii cmod..iiiiiddddd      Vd imm8    (immediate vector)
            imm      = emitGetInsSC(id) & 0x0ff;
            immShift = (emitGetInsSC(id) & 0x700) >> 8;
            elemsize = optGetElemsize(id->idInsOpt());
            cmode    = 0;
            switch (elemsize)
            { // cmode
                case EA_1BYTE:
                    cmode = 0xE; // 1110
                    break;
                case EA_2BYTE:
                    cmode = 0x8;
                    cmode |= (immShift << 1); // 10x0
                    break;
                case EA_4BYTE:
                    if (immShift < 4)
                    {
                        cmode = 0x0;
                        cmode |= (immShift << 1); // 0xx0
                    }
                    else // MSL
                    {
                        cmode = 0xC;
                        if (immShift & 2)
                            cmode |= 1; // 110x
                    }
                    break;
                case EA_8BYTE:
                    cmode = 0xE; // 1110
                    break;
                default:
                    unreached();
                    break;
            }

            code = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            if ((ins == INS_fmov) || (ins == INS_movi))
            {
                if (elemsize == EA_8BYTE)
                {
                    code |= 0x20000000; // X
                }
            }
            if (ins != INS_fmov)
            {
                assert((cmode >= 0) && (cmode <= 0xF));
                code |= (cmode << 12); // cmod
            }
            code |= (((code_t)imm >> 5) << 16);    // iii
            code |= (((code_t)imm & 0x1f) << 5);   // iiiii
            code |= insEncodeReg_Vd(id->idReg1()); // ddddd
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_1C: // DV_1C   .........X...... ......nnnnn.....      Vn #0.0    (fcmp - with zero)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeFloatElemsize(elemsize); // X
            code |= insEncodeReg_Vn(id->idReg1());    // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2A: // DV_2A   .Q.......X...... ......nnnnnddddd      Vd Vn      (fabs, fcvt - vector)
            elemsize = optGetElemsize(id->idInsOpt());
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeFloatElemsize(elemsize);    // X
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2B: // DV_2B   .Q.........iiiii ......nnnnnddddd      Rd Vn[] (umov/smov    - to general)
            elemsize = id->idOpSize();
            index    = emitGetInsSC(id);
            datasize = (elemsize == EA_8BYTE) ? EA_16BYTE : EA_8BYTE;
            if (ins == INS_smov)
            {
                datasize = EA_16BYTE;
            }
            code = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(datasize);         // Q
            code |= insEncodeVectorIndex(elemsize, index); // iiiii
            code |= insEncodeReg_Rd(id->idReg1());         // ddddd
            code |= insEncodeReg_Vn(id->idReg2());         // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2C: // DV_2C   .Q.........iiiii ......nnnnnddddd      Vd Rn   (dup/ins - vector from general)
            if (ins == INS_dup)
            {
                datasize = id->idOpSize();
                elemsize = optGetElemsize(id->idInsOpt());
                index    = 0;
            }
            else // INS_ins
            {
                datasize = EA_16BYTE;
                elemsize = id->idOpSize();
                index    = emitGetInsSC(id);
            }
            code = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(datasize);         // Q
            code |= insEncodeVectorIndex(elemsize, index); // iiiii
            code |= insEncodeReg_Vd(id->idReg1());         // ddddd
            code |= insEncodeReg_Rn(id->idReg2());         // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2D: // DV_2D   .Q.........iiiii ......nnnnnddddd      Vd Vn[]   (dup - vector)
            index    = emitGetInsSC(id);
            elemsize = optGetElemsize(id->idInsOpt());
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize());   // Q
            code |= insEncodeVectorIndex(elemsize, index); // iiiii
            code |= insEncodeReg_Vd(id->idReg1());         // ddddd
            code |= insEncodeReg_Vn(id->idReg2());         // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2E: // DV_2E   ...........iiiii ......nnnnnddddd      Vd Vn[]   (dup - scalar)
            index    = emitGetInsSC(id);
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorIndex(elemsize, index); // iiiii
            code |= insEncodeReg_Vd(id->idReg1());         // ddddd
            code |= insEncodeReg_Vn(id->idReg2());         // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2F: // DV_2F   ...........iiiii .jjjj.nnnnnddddd      Vd[] Vn[] (ins - element)
            elemsize = id->idOpSize();
            imm      = emitGetInsSC(id);
            index    = (imm >> 4) & 0xf;
            index2   = imm & 0xf;
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorIndex(elemsize, index);   // iiiii
            code |= insEncodeVectorIndex2(elemsize, index2); // jjjj
            code |= insEncodeReg_Vd(id->idReg1());           // ddddd
            code |= insEncodeReg_Vn(id->idReg2());           // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2G: // DV_2G   .........X...... ......nnnnnddddd      Vd Vn      (fmov,fcvtXX - register)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeFloatElemsize(elemsize); // X
            code |= insEncodeReg_Vd(id->idReg1());    // ddddd
            code |= insEncodeReg_Vn(id->idReg2());    // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2H: // DV_2H   X........X...... ......nnnnnddddd      Rd Vn      (fmov - to general)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeConvertOpt(fmt, id->idInsOpt()); // X   X
            code |= insEncodeReg_Rd(id->idReg1());            // ddddd
            code |= insEncodeReg_Vn(id->idReg2());            // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2I: // DV_2I   X........X...... ......nnnnnddddd      Vd Rn      (fmov - from general)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeConvertOpt(fmt, id->idInsOpt()); // X   X
            code |= insEncodeReg_Vd(id->idReg1());            // ddddd
            code |= insEncodeReg_Rn(id->idReg2());            // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2J: // DV_2J   ........SS.....D D.....nnnnnddddd      Vd Vn      (fcvt)
            code = emitInsCode(ins, fmt);
            code |= insEncodeConvertOpt(fmt, id->idInsOpt()); // SS DD
            code |= insEncodeReg_Vd(id->idReg1());            // ddddd
            code |= insEncodeReg_Vn(id->idReg2());            // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2K: // DV_2K   .........X.mmmmm ......nnnnn.....      Vn Vm      (fcmp)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeFloatElemsize(elemsize); // X
            code |= insEncodeReg_Vn(id->idReg1());    // nnnnn
            code |= insEncodeReg_Vm(id->idReg2());    // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2L: // DV_2L   ........XX...... ......nnnnnddddd      Vd Vn      (abs, neg - scalar)
            elemsize = id->idOpSize();
            code     = emitInsCode(ins, fmt);
            code |= insEncodeElemsize(elemsize);   // XX
            code |= insEncodeReg_Vd(id->idReg1()); // ddddd
            code |= insEncodeReg_Vn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2M: // DV_2M   .Q......XX...... ......nnnnnddddd      Vd Vn      (abs, neg   - vector)
            elemsize = optGetElemsize(id->idInsOpt());
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeElemsize(elemsize);         // XX
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2N: // DV_2N   .........iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - scalar)
            imm  = emitGetInsSC(id);
            code = emitInsCode(ins, fmt);
            code |= insEncodeVectorShift(EA_8BYTE, imm); // iiiiiii
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2O: // DV_2O   .Q.......iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - vector)
            imm      = emitGetInsSC(id);
            elemsize = optGetElemsize(id->idInsOpt());
            code     = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeVectorShift(elemsize, imm); // iiiiiii
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_2P: // DV_2P   ............... ......nnnnnddddd      Vd Vn      (aes*, sha1su1)
            elemsize = optGetElemsize(id->idInsOpt());
            code     = emitInsCode(ins, fmt);
            code |= insEncodeReg_Vd(id->idReg1()); // ddddd
            code |= insEncodeReg_Vn(id->idReg2()); // nnnnn
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3A: // DV_3A   .Q......XX.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            code     = emitInsCode(ins, fmt);
            elemsize = optGetElemsize(id->idInsOpt());
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeElemsize(elemsize);         // XX
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());       // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3AI: // DV_3AI  .Q......XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector)
            code     = emitInsCode(ins, fmt);
            imm      = emitGetInsSC(id);
            elemsize = optGetElemsize(id->idInsOpt());
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            code |= insEncodeVectorsize(id->idOpSize());    // Q
            code |= insEncodeElemsize(elemsize);            // XX
            code |= insEncodeVectorIndexLMH(elemsize, imm); // LM H
            code |= insEncodeReg_Vd(id->idReg1());          // ddddd
            code |= insEncodeReg_Vn(id->idReg2());          // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());          // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3B: // DV_3B   .Q.......X.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            code     = emitInsCode(ins, fmt);
            elemsize = optGetElemsize(id->idInsOpt());
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeFloatElemsize(elemsize);    // X
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());       // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3BI: // DV_3BI  .Q.......XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
            code     = emitInsCode(ins, fmt);
            imm      = emitGetInsSC(id);
            elemsize = optGetElemsize(id->idInsOpt());
            assert(isValidVectorIndex(id->idOpSize(), elemsize, imm));
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeFloatElemsize(elemsize);    // X
            code |= insEncodeFloatIndex(elemsize, imm);  // L H
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());       // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3C: // DV_3C   .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
            code = emitInsCode(ins, fmt);
            code |= insEncodeVectorsize(id->idOpSize()); // Q
            code |= insEncodeReg_Vd(id->idReg1());       // ddddd
            code |= insEncodeReg_Vn(id->idReg2());       // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());       // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3D: // DV_3D   .........X.mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
            code = emitInsCode(ins, fmt);
            code |= insEncodeFloatElemsize(id->idOpSize()); // X
            code |= insEncodeReg_Vd(id->idReg1());          // ddddd
            code |= insEncodeReg_Vn(id->idReg2());          // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());          // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3DI: // DV_3DI  .........XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by elem)
            code     = emitInsCode(ins, fmt);
            imm      = emitGetInsSC(id);
            elemsize = id->idOpSize();
            assert(isValidVectorIndex(EA_16BYTE, elemsize, imm));
            code |= insEncodeFloatElemsize(elemsize);   // X
            code |= insEncodeFloatIndex(elemsize, imm); // L H
            code |= insEncodeReg_Vd(id->idReg1());      // ddddd
            code |= insEncodeReg_Vn(id->idReg2());      // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());      // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_3E: // DV_3E   ...........mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
        case IF_DV_3F: // DV_3F   ...........mmmmm ......nnnnnddddd      Vd Vn Vm   (vector) - source dest regs overlap
            code = emitInsCode(ins, fmt);
            code |= insEncodeReg_Vd(id->idReg1()); // ddddd
            code |= insEncodeReg_Vn(id->idReg2()); // nnnnn
            code |= insEncodeReg_Vm(id->idReg3()); // mmmmm
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_DV_4A: // DV_4A   .........X.mmmmm .aaaaannnnnddddd      Vd Va Vn Vm (scalar)
            code     = emitInsCode(ins, fmt);
            elemsize = id->idOpSize();
            code |= insEncodeFloatElemsize(elemsize); // X
            code |= insEncodeReg_Vd(id->idReg1());    // ddddd
            code |= insEncodeReg_Vn(id->idReg2());    // nnnnn
            code |= insEncodeReg_Vm(id->idReg3());    // mmmmm
            code |= insEncodeReg_Va(id->idReg4());    // aaaaa
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SN_0A: // SN_0A   ................ ................
            code = emitInsCode(ins, fmt);
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SI_0A: // SI_0A   ...........iiiii iiiiiiiiiii.....               imm16
            imm = emitGetInsSC(id);
            assert(isValidUimm16(imm));
            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 5); // iiiii iiiiiiiiiii
            dst += emitOutput_Instr(dst, code);
            break;

        case IF_SI_0B: // SI_0B   ................ ....bbbb........               imm4 - barrier
            imm = emitGetInsSC(id);
            assert((imm >= 0) && (imm <= 15));
            code = emitInsCode(ins, fmt);
            code |= ((code_t)imm << 8); // bbbb
            dst += emitOutput_Instr(dst, code);
            break;

        default:
            assert(!"Unexpected format");
            break;
    }

    // Determine if any registers now hold GC refs, or whether a register that was overwritten held a GC ref.
    // We assume here that "id->idGCref()" is not GC_NONE only if the instruction described by "id" writes a
    // GC ref to register "id->idReg1()".  (It may, apparently, also not be GC_NONE in other cases, such as
    // for stores, but we ignore those cases here.)
    if (emitInsMayWriteToGCReg(id)) // True if "id->idIns()" writes to a register than can hold GC ref.
    {
        // We assume that "idReg1" is the primary destination register for all instructions
        if (id->idGCref() != GCT_NONE)
        {
            emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst);
        }
        else
        {
            emitGCregDeadUpd(id->idReg1(), dst);
        }

        if (emitInsMayWriteMultipleRegs(id))
        {
            // INS_ldp etc...
            // "idReg2" is the secondary destination register
            if (id->idGCrefReg2() != GCT_NONE)
            {
                emitGCregLiveUpd(id->idGCrefReg2(), id->idReg2(), dst);
            }
            else
            {
                emitGCregDeadUpd(id->idReg2(), dst);
            }
        }
    }

    // Now we determine if the instruction has written to a (local variable) stack location, and either written a GC
    // ref or overwritten one.
    if (emitInsWritesToLclVarStackLoc(id) || emitInsWritesToLclVarStackLocPair(id))
    {
        int      varNum = id->idAddr()->iiaLclVar.lvaVarNum();
        unsigned ofs    = AlignDown(id->idAddr()->iiaLclVar.lvaOffset(), TARGET_POINTER_SIZE);
        bool     FPbased;
        int      adr = emitComp->lvaFrameAddress(varNum, &FPbased);
        if (id->idGCref() != GCT_NONE)
        {
            emitGCvarLiveUpd(adr + ofs, varNum, id->idGCref(), dst);
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
                emitGCvarDeadUpd(adr + ofs, dst);
        }
        if (emitInsWritesToLclVarStackLocPair(id))
        {
            unsigned ofs2 = ofs + TARGET_POINTER_SIZE;
            if (id->idGCrefReg2() != GCT_NONE)
            {
                emitGCvarLiveUpd(adr + ofs2, varNum, id->idGCrefReg2(), dst);
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
                    emitGCvarDeadUpd(adr + ofs2, dst);
            }
        }
    }

#ifdef DEBUG
    /* Make sure we set the instruction descriptor size correctly */

    size_t expected = emitSizeOfInsDsc(id);
    assert(sz == expected);

    if (emitComp->opts.disAsm || emitComp->opts.dspEmit || emitComp->verbose)
    {
        emitDispIns(id, false, dspOffs, true, emitCurCodeOffs(odst), *dp, (dst - *dp), ig);
    }

    if (emitComp->compDebugBreak)
    {
        // For example, set JitBreakEmitOutputInstr=a6 will break when this method is called for
        // emitting instruction a6, (i.e. IN00a6 in jitdump).
        if ((unsigned)JitConfig.JitBreakEmitOutputInstr() == id->idDebugOnlyInfo()->idNum)
        {
            assert(!"JitBreakEmitOutputInstr reached");
        }
    }
#endif

    /* All instructions are expected to generate code */

    assert(*dp != dst);

    *dp = dst;

    return sz;
}

/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG

/*****************************************************************************
 *
 *  Display the instruction name
 */
void emitter::emitDispInst(instruction ins)
{
    const char* insstr = codeGen->genInsName(ins);
    size_t      len    = strlen(insstr);

    /* Display the instruction name */

    printf("%s", insstr);

    //
    // Add at least one space after the instruction name
    // and add spaces until we have reach the normal size of 8
    do
    {
        printf(" ");
        len++;
    } while (len < 8);
}

/*****************************************************************************
 *
 *  Display an immediate value
 */
void emitter::emitDispImm(ssize_t imm, bool addComma, bool alwaysHex /* =false */)
{
    if (strictArmAsm)
    {
        printf("#");
    }

    // Munge any pointers if we want diff-able disassembly.
    // Since some may be emitted as partial words, print as diffable anything that has
    // significant bits beyond the lowest 8-bits.
    if (emitComp->opts.disDiffable)
    {
        ssize_t top56bits = (imm >> 8);
        if ((top56bits != 0) && (top56bits != -1))
            imm = 0xD1FFAB1E;
    }

    if (!alwaysHex && (imm > -1000) && (imm < 1000))
    {
        printf("%d", imm);
    }
    else
    {
        if ((imm < 0) && ((imm & 0xFFFFFFFF00000000LL) == 0xFFFFFFFF00000000LL))
        {
            printf("-");
            imm = -imm;
        }

        if ((imm & 0xFFFFFFFF00000000LL) != 0)
        {
            printf("0x%llx", imm);
        }
        else
        {
            printf("0x%02x", imm);
        }
    }

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display a float zero constant
 */
void emitter::emitDispFloatZero()
{
    if (strictArmAsm)
    {
        printf("#");
    }
    printf("0.0");
}

/*****************************************************************************
 *
 *  Display an encoded float constant value
 */
void emitter::emitDispFloatImm(ssize_t imm8)
{
    assert((0 <= imm8) && (imm8 <= 0x0ff));
    if (strictArmAsm)
    {
        printf("#");
    }

    floatImm8 fpImm;
    fpImm.immFPIVal = (unsigned)imm8;
    double result   = emitDecodeFloatImm8(fpImm);

    printf("%.4f", result);
}

/*****************************************************************************
 *
 *  Display an immediate that is optionally LSL12.
 */
void emitter::emitDispImmOptsLSL12(ssize_t imm, insOpts opt)
{
    if (!strictArmAsm && insOptsLSL12(opt))
    {
        imm <<= 12;
    }
    emitDispImm(imm, false);
    if (strictArmAsm && insOptsLSL12(opt))
    {
        printf(", LSL #12");
    }
}

/*****************************************************************************
 *
 *  Display an ARM64 condition code for the conditional instructions
 */
void emitter::emitDispCond(insCond cond)
{
    const static char* armCond[16] = {"eq", "ne", "hs", "lo", "mi", "pl", "vs", "vc",
                                      "hi", "ls", "ge", "lt", "gt", "le", "AL", "NV"}; // The last two are invalid
    unsigned imm = (unsigned)cond;
    assert((0 <= imm) && (imm < ArrLen(armCond)));
    printf(armCond[imm]);
}

/*****************************************************************************
 *
 *  Display an ARM64 flags for the conditional instructions
 */
void emitter::emitDispFlags(insCflags flags)
{
    const static char* armFlags[16] = {"0", "v",  "c",  "cv",  "z",  "zv",  "zc",  "zcv",
                                       "n", "nv", "nc", "ncv", "nz", "nzv", "nzc", "nzcv"};
    unsigned imm = (unsigned)flags;
    assert((0 <= imm) && (imm < ArrLen(armFlags)));
    printf(armFlags[imm]);
}

/*****************************************************************************
 *
 *  Display an ARM64 'barrier' for the memory barrier instructions
 */
void emitter::emitDispBarrier(insBarrier barrier)
{
    const static char* armBarriers[16] = {"#0", "oshld", "oshst", "osh", "#4",  "nshld", "nshst", "nsh",
                                          "#8", "ishld", "ishst", "ish", "#12", "ld",    "st",    "sy"};
    unsigned imm = (unsigned)barrier;
    assert((0 <= imm) && (imm < ArrLen(armBarriers)));
    printf(armBarriers[imm]);
}

/*****************************************************************************
 *
 *  Prints the encoding for the Shift Type encoding
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
    else if (opt == INS_OPTS_MSL)
        printf(" MSL ");
    else
        assert(!"Bad value");
}

/*****************************************************************************
 *
 *  Prints the encoding for the Extend Type encoding
 */

void emitter::emitDispExtendOpts(insOpts opt)
{
    if (opt == INS_OPTS_UXTB)
        printf("UXTB");
    else if (opt == INS_OPTS_UXTH)
        printf("UXTH");
    else if (opt == INS_OPTS_UXTW)
        printf("UXTW");
    else if (opt == INS_OPTS_UXTX)
        printf("UXTX");
    else if (opt == INS_OPTS_SXTB)
        printf("SXTB");
    else if (opt == INS_OPTS_SXTH)
        printf("SXTH");
    else if (opt == INS_OPTS_SXTW)
        printf("SXTW");
    else if (opt == INS_OPTS_SXTX)
        printf("SXTX");
    else
        assert(!"Bad value");
}

/*****************************************************************************
 *
 *  Prints the encoding for the Extend Type encoding in loads/stores
 */

void emitter::emitDispLSExtendOpts(insOpts opt)
{
    if (opt == INS_OPTS_LSL)
        printf("LSL");
    else if (opt == INS_OPTS_UXTW)
        printf("UXTW");
    else if (opt == INS_OPTS_UXTX)
        printf("UXTX");
    else if (opt == INS_OPTS_SXTW)
        printf("SXTW");
    else if (opt == INS_OPTS_SXTX)
        printf("SXTX");
    else
        assert(!"Bad value");
}

/*****************************************************************************
 *
 *  Display a register
 */
void emitter::emitDispReg(regNumber reg, emitAttr attr, bool addComma)
{
    emitAttr size = EA_SIZE(attr);
    printf(emitRegName(reg, size));

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display a vector register with an arrangement suffix
 */
void emitter::emitDispVectorReg(regNumber reg, insOpts opt, bool addComma)
{
    assert(isVectorRegister(reg));
    printf(emitVectorRegName(reg));
    emitDispArrangement(opt);

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display an vector register index suffix
 */
void emitter::emitDispVectorRegIndex(regNumber reg, emitAttr elemsize, ssize_t index, bool addComma)
{
    assert(isVectorRegister(reg));
    printf(emitVectorRegName(reg));

    switch (elemsize)
    {
        case EA_1BYTE:
            printf(".b");
            break;
        case EA_2BYTE:
            printf(".h");
            break;
        case EA_4BYTE:
            printf(".s");
            break;
        case EA_8BYTE:
            printf(".d");
            break;
        default:
            assert(!"invalid elemsize");
            break;
    }

    printf("[%d]", index);

    if (addComma)
        printf(", ");
}

/*****************************************************************************
 *
 *  Display an arrangement suffix
 */
void emitter::emitDispArrangement(insOpts opt)
{
    const char* str = "???";

    switch (opt)
    {
        case INS_OPTS_8B:
            str = "8b";
            break;
        case INS_OPTS_16B:
            str = "16b";
            break;
        case INS_OPTS_4H:
            str = "4h";
            break;
        case INS_OPTS_8H:
            str = "8h";
            break;
        case INS_OPTS_2S:
            str = "2s";
            break;
        case INS_OPTS_4S:
            str = "4s";
            break;
        case INS_OPTS_1D:
            str = "1d";
            break;
        case INS_OPTS_2D:
            str = "2d";
            break;

        default:
            assert(!"Invalid insOpt for vector register");
    }
    printf(".");
    printf(str);
}

/*****************************************************************************
 *
 *  Display a register with an optional shift operation
 */
void emitter::emitDispShiftedReg(regNumber reg, insOpts opt, ssize_t imm, emitAttr attr)
{
    emitAttr size = EA_SIZE(attr);
    assert((imm & 0x003F) == imm);
    assert(((imm & 0x0020) == 0) || (size == EA_8BYTE));

    printf(emitRegName(reg, size));

    if (imm > 0)
    {
        if (strictArmAsm)
        {
            printf(",");
        }
        emitDispShiftOpts(opt);
        emitDispImm(imm, false);
    }
}

/*****************************************************************************
 *
 *  Display a register with an optional extend and scale operations
 */
void emitter::emitDispExtendReg(regNumber reg, insOpts opt, ssize_t imm)
{
    assert((imm >= 0) && (imm <= 4));
    assert(insOptsNone(opt) || insOptsAnyExtend(opt) || (opt == INS_OPTS_LSL));

    // size is based on the extend option, not the instr size.
    emitAttr size = insOpts32BitExtend(opt) ? EA_4BYTE : EA_8BYTE;

    if (strictArmAsm)
    {
        if (insOptsNone(opt))
        {
            emitDispReg(reg, size, false);
        }
        else
        {
            emitDispReg(reg, size, true);
            if (opt == INS_OPTS_LSL)
                printf("LSL");
            else
                emitDispExtendOpts(opt);
            if ((imm > 0) || (opt == INS_OPTS_LSL))
            {
                printf(" ");
                emitDispImm(imm, false);
            }
        }
    }
    else // !strictArmAsm
    {
        if (insOptsNone(opt))
        {
            emitDispReg(reg, size, false);
        }
        else
        {
            if (opt != INS_OPTS_LSL)
            {
                emitDispExtendOpts(opt);
                printf("(");
                emitDispReg(reg, size, false);
                printf(")");
            }
        }
        if (imm > 0)
        {
            printf("*");
            emitDispImm(ssize_t{1} << imm, false);
        }
    }
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + imm]
 */
void emitter::emitDispAddrRI(regNumber reg, insOpts opt, ssize_t imm)
{
    reg = encodingZRtoSP(reg); // ZR (R31) encodes the SP register

    if (strictArmAsm)
    {
        printf("[");

        emitDispReg(reg, EA_8BYTE, false);

        if (!insOptsPostIndex(opt) && (imm != 0))
        {
            printf(",");
            emitDispImm(imm, false);
        }
        printf("]");

        if (insOptsPreIndex(opt))
        {
            printf("!");
        }
        else if (insOptsPostIndex(opt))
        {
            printf(",");
            emitDispImm(imm, false);
        }
    }
    else // !strictArmAsm
    {
        printf("[");

        const char* operStr = "++";
        if (imm < 0)
        {
            operStr = "--";
            imm     = -imm;
        }

        if (insOptsPreIndex(opt))
        {
            printf(operStr);
        }

        emitDispReg(reg, EA_8BYTE, false);

        if (insOptsPostIndex(opt))
        {
            printf(operStr);
        }

        if (insOptsIndexed(opt))
        {
            printf(", ");
        }
        else
        {
            printf("%c", operStr[1]);
        }
        emitDispImm(imm, false);
        printf("]");
    }
}

/*****************************************************************************
 *
 *  Display an addressing operand [reg + extended reg]
 */
void emitter::emitDispAddrRRExt(regNumber reg1, regNumber reg2, insOpts opt, bool isScaled, emitAttr size)
{
    reg1 = encodingZRtoSP(reg1); // ZR (R31) encodes the SP register

    unsigned scale = 0;
    if (isScaled)
    {
        scale = NaturalScale_helper(size);
    }

    printf("[");

    if (strictArmAsm)
    {
        emitDispReg(reg1, EA_8BYTE, true);
        emitDispExtendReg(reg2, opt, scale);
    }
    else // !strictArmAsm
    {
        emitDispReg(reg1, EA_8BYTE, false);
        printf("+");
        emitDispExtendReg(reg2, opt, scale);
    }

    printf("]");
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable)
    {
        if (sz == 4)
        {
            printf("  %08X    ", (*((code_t*)code)));
        }
        else
        {
            assert(sz == 0);
            printf("              ");
        }
    }
}

/****************************************************************************
 *
 *  Display the given instruction.
 */

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    if (EMITVERBOSE)
    {
        unsigned idNum =
            id->idDebugOnlyInfo()->idNum; // Do not remove this!  It is needed for VisualStudio conditional breakpoints

        printf("IN%04x: ", idNum);
    }

    if (pCode == NULL)
        sz = 0;

    if (!emitComp->opts.dspEmit && !isNew && !asmfm && sz)
        doffs = true;

    /* Display the instruction offset */

    emitDispInsOffs(offset, doffs);

    /* Display the instruction hex code */

    emitDispInsHex(id, pCode, sz);

    printf("      ");

    /* Get the instruction and format */

    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    emitDispInst(ins);

    /* If this instruction has just been added, check its size */

    assert(isNew == false || (int)emitSizeOfInsDsc(id) == emitCurIGfreeNext - (BYTE*)id);

    /* Figure out the operand size */
    emitAttr size = id->idOpSize();
    emitAttr attr = size;
    if (id->idGCref() == GCT_GCREF)
        attr = EA_GCREF;
    else if (id->idGCref() == GCT_BYREF)
        attr = EA_BYREF;

    switch (fmt)
    {
        code_t       code;
        ssize_t      imm;
        int          doffs;
        bool         isExtendAlias;
        bitMaskImm   bmi;
        halfwordImm  hwi;
        condFlagsImm cfi;
        unsigned     scale;
        unsigned     immShift;
        bool         hasShift;
        ssize_t      offs;
        const char*  methodName;
        emitAttr     elemsize;
        emitAttr     datasize;
        emitAttr     srcsize;
        emitAttr     dstsize;
        ssize_t      index;
        ssize_t      index2;

        case IF_BI_0A: // BI_0A   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
        case IF_BI_0B: // BI_0B   ......iiiiiiiiii iiiiiiiiiii.....               simm19:00
        case IF_LARGEJMP:
        {
            if (fmt == IF_LARGEJMP)
            {
                printf("(LARGEJMP)");
            }
            if (id->idAddr()->iiaHasInstrCount())
            {
                int instrCount = id->idAddr()->iiaGetInstrCount();

                if (ig == nullptr)
                {
                    printf("pc%s%d instructions", (instrCount >= 0) ? "+" : "", instrCount);
                }
                else
                {
                    unsigned       insNum  = emitFindInsNum(ig, id);
                    UNATIVE_OFFSET srcOffs = ig->igOffs + emitFindOffset(ig, insNum + 1);
                    UNATIVE_OFFSET dstOffs = ig->igOffs + emitFindOffset(ig, insNum + 1 + instrCount);
                    ssize_t        relOffs = (ssize_t)(emitOffsetToPtr(dstOffs) - emitOffsetToPtr(srcOffs));
                    printf("pc%s%d (%d instructions)", (relOffs >= 0) ? "+" : "", relOffs, instrCount);
                }
            }
            else if (id->idIsBound())
            {
                printf("G_M%03u_IG%02u", Compiler::s_compMethodsCount, id->idAddr()->iiaIGlabel->igNum);
            }
            else
            {
                printf("L_M%03u_" FMT_BB, Compiler::s_compMethodsCount, id->idAddr()->iiaBBlabel->bbNum);
            }
        }
        break;

        case IF_BI_0C: // BI_0C   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00
            if (id->idIsCallAddr())
            {
                offs       = (ssize_t)id->idAddr()->iiaAddr;
                methodName = "";
            }
            else
            {
                offs       = 0;
                methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
            }

            if (offs)
            {
                if (id->idIsDspReloc())
                    printf("reloc ");
                printf("%08X", offs);
            }
            else
            {
                printf("%s", methodName);
            }
            break;

        case IF_BI_1A: // BI_1A   ......iiiiiiiiii iiiiiiiiiiittttt      Rt       simm19:00
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg1(), size, true);
            if (id->idIsBound())
            {
                printf("G_M%03u_IG%02u", Compiler::s_compMethodsCount, id->idAddr()->iiaIGlabel->igNum);
            }
            else
            {
                printf("L_M%03u_" FMT_BB, Compiler::s_compMethodsCount, id->idAddr()->iiaBBlabel->bbNum);
            }
            break;

        case IF_BI_1B: // BI_1B   B.......bbbbbiii iiiiiiiiiiittttt      Rt imm6, simm14:00
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg1(), size, true);
            emitDispImm(emitGetInsSC(id), true);
            if (id->idIsBound())
            {
                printf("G_M%03u_IG%02u", Compiler::s_compMethodsCount, id->idAddr()->iiaIGlabel->igNum);
            }
            else
            {
                printf("L_M%03u_" FMT_BB, Compiler::s_compMethodsCount, id->idAddr()->iiaBBlabel->bbNum);
            }
            break;

        case IF_BR_1A: // BR_1A   ................ ......nnnnn.....         Rn
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg1(), size, false);
            break;

        case IF_BR_1B: // BR_1B   ................ ......nnnnn.....         Rn
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg3(), size, false);
            break;

        case IF_LS_1A: // LS_1A   XX...V..iiiiiiii iiiiiiiiiiittttt      Rt    PC imm(1MB)
        case IF_DI_1E: // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd       simm21
        case IF_LARGELDC:
        case IF_LARGEADR:
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg1(), size, true);
            imm = emitGetInsSC(id);

            /* Is this actually a reference to a data section? */
            if (fmt == IF_LARGEADR)
            {
                printf("(LARGEADR)");
            }
            else if (fmt == IF_LARGELDC)
            {
                printf("(LARGELDC)");
            }

            printf("[");
            if (id->idAddr()->iiaIsJitDataOffset())
            {
                doffs = Compiler::eeGetJitDataOffs(id->idAddr()->iiaFieldHnd);
                /* Display a data section reference */

                if (doffs & 1)
                    printf("@CNS%02u", doffs - 1);
                else
                    printf("@RWD%02u", doffs);

                if (imm != 0)
                    printf("%+Id", imm);
            }
            else
            {
                assert(imm == 0);
                if (id->idIsReloc())
                {
                    printf("RELOC ");
                    emitDispImm((ssize_t)id->idAddr()->iiaAddr, false);
                }
                else if (id->idIsBound())
                {
                    printf("G_M%03u_IG%02u", Compiler::s_compMethodsCount, id->idAddr()->iiaIGlabel->igNum);
                }
                else
                {
                    printf("L_M%03u_" FMT_BB, Compiler::s_compMethodsCount, id->idAddr()->iiaBBlabel->bbNum);
                }
            }
            printf("]");
            break;

        case IF_LS_2A: // LS_2A   .X.......X...... ......nnnnnttttt      Rt Rn
            assert(insOptsNone(id->idInsOpt()));
            assert(emitGetInsSC(id) == 0);
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg2(), id->idInsOpt(), 0);
            break;

        case IF_LS_2B: // LS_2B   .X.......Xiiiiii iiiiiinnnnnttttt      Rt Rn    imm(0-4095)
            assert(insOptsNone(id->idInsOpt()));
            imm   = emitGetInsSC(id);
            scale = NaturalScale_helper(emitInsLoadStoreSize(id));
            imm <<= scale; // The immediate is scaled by the size of the ld/st
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg2(), id->idInsOpt(), imm);
            break;

        case IF_LS_2C: // LS_2C   .X.......X.iiiii iiiiPPnnnnnttttt      Rt Rn    imm(-256..+255) no/pre/post inc
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            imm = emitGetInsSC(id);
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg2(), id->idInsOpt(), imm);
            break;

        case IF_LS_3A: // LS_3A   .X.......X.mmmmm oooS..nnnnnttttt      Rt Rn Rm ext(Rm) LSL {}
            assert(insOptsLSExtend(id->idInsOpt()));
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            if (id->idIsLclVar())
            {
                emitDispAddrRRExt(id->idReg2(), codeGen->rsGetRsvdReg(), id->idInsOpt(), false, size);
            }
            else
            {
                emitDispAddrRRExt(id->idReg2(), id->idReg3(), id->idInsOpt(), id->idReg3Scaled(), size);
            }
            break;

        case IF_LS_3B: // LS_3B   X............... .aaaaannnnnddddd      Rt Ra Rn
            assert(insOptsNone(id->idInsOpt()));
            assert(emitGetInsSC(id) == 0);
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            emitDispReg(id->idReg2(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg3(), id->idInsOpt(), 0);
            break;

        case IF_LS_3C: // LS_3C   X.........iiiiii iaaaaannnnnddddd      Rt Ra Rn imm(im7,sh)
            assert(insOptsNone(id->idInsOpt()) || insOptsIndexed(id->idInsOpt()));
            imm   = emitGetInsSC(id);
            scale = NaturalScale_helper(emitInsLoadStoreSize(id));
            imm <<= scale;
            emitDispReg(id->idReg1(), emitInsTargetRegSize(id), true);
            emitDispReg(id->idReg2(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg3(), id->idInsOpt(), imm);
            break;

        case IF_LS_3D: // LS_3D   .X.......X.mmmmm ......nnnnnttttt      Wm Rt Rn
            assert(insOptsNone(id->idInsOpt()));
            emitDispReg(id->idReg1(), EA_4BYTE, true);
            emitDispReg(id->idReg2(), emitInsTargetRegSize(id), true);
            emitDispAddrRI(id->idReg3(), id->idInsOpt(), 0);
            break;

        case IF_LS_3E: // LS_3E   .X.........mmmmm ......nnnnnttttt      Rm Rt Rn ARMv8.1 LSE Atomics
            assert(insOptsNone(id->idInsOpt()));
            assert((EA_SIZE(size) == 4) || (EA_SIZE(size) == 8));
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispAddrRI(id->idReg3(), id->idInsOpt(), 0);
            break;

        case IF_DI_1A: // DI_1A   X.......shiiiiii iiiiiinnnnn.....      Rn       imm(i12,sh)
            emitDispReg(id->idReg1(), size, true);
            emitDispImmOptsLSL12(emitGetInsSC(id), id->idInsOpt());
            break;

        case IF_DI_1B: // DI_1B   X........hwiiiii iiiiiiiiiiiddddd      Rd       imm(i16,hw)
            emitDispReg(id->idReg1(), size, true);
            hwi.immHWVal = (unsigned)emitGetInsSC(id);
            if (ins == INS_mov)
            {
                emitDispImm(emitDecodeHalfwordImm(hwi, size), false);
            }
            else // movz, movn, movk
            {
                emitDispImm(hwi.immVal, false);
                if (hwi.immHW != 0)
                {
                    emitDispShiftOpts(INS_OPTS_LSL);
                    emitDispImm(hwi.immHW * 16, false);
                }
            }
            break;

        case IF_DI_1C: // DI_1C   X........Nrrrrrr ssssssnnnnn.....         Rn    imm(N,r,s)
            emitDispReg(id->idReg1(), size, true);
            bmi.immNRS = (unsigned)emitGetInsSC(id);
            emitDispImm(emitDecodeBitMaskImm(bmi, size), false);
            break;

        case IF_DI_1D: // DI_1D   X........Nrrrrrr ssssss.....ddddd      Rd       imm(N,r,s)
            emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
            bmi.immNRS = (unsigned)emitGetInsSC(id);
            emitDispImm(emitDecodeBitMaskImm(bmi, size), false);
            break;

        case IF_DI_2A: // DI_2A   X.......shiiiiii iiiiiinnnnnddddd      Rd Rn    imm(i12,sh)
            if ((ins == INS_add) || (ins == INS_sub))
            {
                emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
                emitDispReg(encodingZRtoSP(id->idReg2()), size, true);
            }
            else
            {
                emitDispReg(id->idReg1(), size, true);
                emitDispReg(id->idReg2(), size, true);
            }
            emitDispImmOptsLSL12(emitGetInsSC(id), id->idInsOpt());
            break;

        case IF_DI_2B: // DI_2B   X........X.nnnnn ssssssnnnnnddddd      Rd Rn    imm(0-63)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_DI_2C: // DI_2C   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imm(N,r,s)
            if (ins == INS_ands)
            {
                emitDispReg(id->idReg1(), size, true);
            }
            else
            {
                emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
            }
            emitDispReg(id->idReg2(), size, true);
            bmi.immNRS = (unsigned)emitGetInsSC(id);
            emitDispImm(emitDecodeBitMaskImm(bmi, size), false);
            break;

        case IF_DI_2D: // DI_2D   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imr, ims   (N,r,s)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);

            imm        = emitGetInsSC(id);
            bmi.immNRS = (unsigned)imm;

            switch (ins)
            {
                case INS_bfm:
                case INS_sbfm:
                case INS_ubfm:
                    emitDispImm(bmi.immR, true);
                    emitDispImm(bmi.immS, false);
                    break;

                case INS_bfi:
                case INS_sbfiz:
                case INS_ubfiz:
                    emitDispImm(getBitWidth(size) - bmi.immR, true);
                    emitDispImm(bmi.immS + 1, false);
                    break;

                case INS_bfxil:
                case INS_sbfx:
                case INS_ubfx:
                    emitDispImm(bmi.immR, true);
                    emitDispImm(bmi.immS - bmi.immR + 1, false);
                    break;

                case INS_asr:
                case INS_lsr:
                case INS_lsl:
                    emitDispImm(imm, false);
                    break;

                default:
                    assert(!"Unexpected instruction in IF_DI_2D");
            }

            break;

        case IF_DI_1F: // DI_1F   X..........iiiii cccc..nnnnn.nzcv      Rn imm5  nzcv cond
            emitDispReg(id->idReg1(), size, true);
            cfi.immCFVal = (unsigned)emitGetInsSC(id);
            emitDispImm(cfi.imm5, true);
            emitDispFlags(cfi.flags);
            printf(",");
            emitDispCond(cfi.cond);
            break;

        case IF_DR_1D: // DR_1D   X............... cccc.......mmmmm      Rd       cond
            emitDispReg(id->idReg1(), size, true);
            cfi.immCFVal = (unsigned)emitGetInsSC(id);
            emitDispCond(cfi.cond);
            break;

        case IF_DR_2A: // DR_2A   X..........mmmmm ......nnnnn.....         Rn Rm
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, false);
            break;

        case IF_DR_2B: // DR_2B   X.......sh.mmmmm ssssssnnnnn.....         Rn Rm {LSL,LSR,ASR,ROR} imm(0-63)
            emitDispReg(id->idReg1(), size, true);
            emitDispShiftedReg(id->idReg2(), id->idInsOpt(), emitGetInsSC(id), size);
            break;

        case IF_DR_2C: // DR_2C   X..........mmmmm ooosssnnnnn.....         Rn Rm ext(Rm) LSL imm(0-4)
            emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
            imm = emitGetInsSC(id);
            emitDispExtendReg(id->idReg2(), id->idInsOpt(), imm);
            break;

        case IF_DR_2D: // DR_2D   X..........nnnnn cccc..nnnnnddddd      Rd Rn    cond
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            cfi.immCFVal = (unsigned)emitGetInsSC(id);
            emitDispCond(cfi.cond);
            break;

        case IF_DR_2E: // DR_2E   X..........mmmmm ...........ddddd      Rd    Rm
        case IF_DR_2J: // DR_2J   ................ ......nnnnnddddd      Sd    Sn
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, false);
            break;

        case IF_DR_2F: // DR_2F   X.......sh.mmmmm ssssss.....ddddd      Rd    Rm {LSL,LSR,ASR} imm(0-63)
            emitDispReg(id->idReg1(), size, true);
            emitDispShiftedReg(id->idReg2(), id->idInsOpt(), emitGetInsSC(id), size);
            break;

        case IF_DR_2G: // DR_2G   X............... ......nnnnnddddd      Rd Rn
            emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
            emitDispReg(encodingZRtoSP(id->idReg2()), size, false);
            break;

        case IF_DR_2H: // DR_2H   X........X...... ......nnnnnddddd      Rd Rn
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, false);
            break;

        case IF_DR_2I: // DR_2I   X..........mmmmm cccc..nnnnn.nzcv      Rn Rm    nzcv cond
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            cfi.immCFVal = (unsigned)emitGetInsSC(id);
            emitDispFlags(cfi.flags);
            printf(",");
            emitDispCond(cfi.cond);
            break;

        case IF_DR_3A: // DR_3A   X..........mmmmm ......nnnnnmmmmm      Rd Rn Rm
            if ((ins == INS_add) || (ins == INS_sub))
            {
                emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
                emitDispReg(encodingZRtoSP(id->idReg2()), size, true);
            }
            else if ((ins == INS_smull) || (ins == INS_smulh))
            {
                // Rd is always 8 bytes
                emitDispReg(id->idReg1(), EA_8BYTE, true);

                // Rn, Rm effective size depends on instruction type
                size = (ins == INS_smulh) ? EA_8BYTE : EA_4BYTE;
                emitDispReg(id->idReg2(), size, true);
            }
            else
            {
                emitDispReg(id->idReg1(), size, true);
                emitDispReg(id->idReg2(), size, true);
            }
            if (id->idIsLclVar())
            {
                emitDispReg(codeGen->rsGetRsvdReg(), size, false);
            }
            else
            {
                emitDispReg(id->idReg3(), size, false);
            }

            break;

        case IF_DR_3B: // DR_3B   X.......sh.mmmmm ssssssnnnnnddddd      Rd Rn Rm {LSL,LSR,ASR} imm(0-63)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispShiftedReg(id->idReg3(), id->idInsOpt(), emitGetInsSC(id), size);
            break;

        case IF_DR_3C: // DR_3C   X..........mmmmm ooosssnnnnnddddd      Rd Rn Rm ext(Rm) LSL imm(0-4)
            emitDispReg(encodingZRtoSP(id->idReg1()), size, true);
            emitDispReg(encodingZRtoSP(id->idReg2()), size, true);
            imm = emitGetInsSC(id);
            emitDispExtendReg(id->idReg3(), id->idInsOpt(), imm);
            break;

        case IF_DR_3D: // DR_3D   X..........mmmmm cccc..nnnnnmmmmm      Rd Rn Rm cond
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispReg(id->idReg3(), size, true);
            cfi.immCFVal = (unsigned)emitGetInsSC(id);
            emitDispCond(cfi.cond);
            break;

        case IF_DR_3E: // DR_3E   X........X.mmmmm ssssssnnnnnddddd      Rd Rn Rm imm(0-63)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispReg(id->idReg3(), size, true);
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_DR_4A: // DR_4A   X..........mmmmm .aaaaannnnnmmmmm      Rd Rn Rm Ra
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispReg(id->idReg3(), size, true);
            emitDispReg(id->idReg4(), size, false);
            break;

        case IF_DV_1A: // DV_1A   .........X.iiiii iii........ddddd      Vd imm8 (fmov - immediate scalar)
            elemsize = id->idOpSize();
            emitDispReg(id->idReg1(), elemsize, true);
            emitDispFloatImm(emitGetInsSC(id));
            break;

        case IF_DV_1B: // DV_1B   .QX..........iii cmod..iiiiiddddd      Vd imm8 (immediate vector)
            imm      = emitGetInsSC(id) & 0x0ff;
            immShift = (emitGetInsSC(id) & 0x700) >> 8;
            hasShift = (immShift != 0);
            elemsize = optGetElemsize(id->idInsOpt());
            if (id->idInsOpt() == INS_OPTS_1D)
            {
                assert(elemsize == size);
                emitDispReg(id->idReg1(), size, true);
            }
            else
            {
                emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            }
            if (ins == INS_fmov)
            {
                emitDispFloatImm(imm);
                assert(hasShift == false);
            }
            else
            {
                if (elemsize == EA_8BYTE)
                {
                    assert(ins == INS_movi);
                    ssize_t       imm64 = 0;
                    const ssize_t mask8 = 0xFF;
                    for (unsigned b = 0; b < 8; b++)
                    {
                        if (imm & (ssize_t{1} << b))
                        {
                            imm64 |= (mask8 << (b * 8));
                        }
                    }
                    emitDispImm(imm64, hasShift, true);
                }
                else
                {
                    emitDispImm(imm, hasShift, true);
                }
                if (hasShift)
                {
                    insOpts  opt   = (immShift & 0x4) ? INS_OPTS_MSL : INS_OPTS_LSL;
                    unsigned shift = (immShift & 0x3) * 8;
                    emitDispShiftOpts(opt);
                    emitDispImm(shift, false);
                }
            }
            break;

        case IF_DV_1C: // DV_1C   .........X...... ......nnnnn.....      Vn #0.0 (fcmp - with zero)
            elemsize = id->idOpSize();
            emitDispReg(id->idReg1(), elemsize, true);
            emitDispFloatZero();
            break;

        case IF_DV_2A: // DV_2A   .Q.......X...... ......nnnnnddddd      Vd Vn   (fabs, fcvt - vector)
        case IF_DV_2M: // DV_2M   .Q......XX...... ......nnnnnddddd      Vd Vn   (abs, neg   - vector)
        case IF_DV_2P: // DV_2P   ................ ......nnnnnddddd      Vd Vn   (aes*, sha1su1)
            emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            emitDispVectorReg(id->idReg2(), id->idInsOpt(), false);
            break;

        case IF_DV_2N: // DV_2N   .........iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - scalar)
            elemsize = id->idOpSize();
            emitDispReg(id->idReg1(), elemsize, true);
            emitDispReg(id->idReg2(), elemsize, true);
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_DV_2O: // DV_2O   .Q.......iiiiiii ......nnnnnddddd      Vd Vn imm   (shift - vector)
            imm = emitGetInsSC(id);
            // Do we have a sxtl or uxtl instruction?
            isExtendAlias = ((ins == INS_sxtl) || (ins == INS_sxtl2) || (ins == INS_uxtl) || (ins == INS_uxtl2));
            code          = emitInsCode(ins, fmt);
            if (code & 0x00008000) // widen/narrow opcodes
            {
                if (code & 0x00002000) // SHL opcodes
                {
                    emitDispVectorReg(id->idReg1(), optWidenElemsize(id->idInsOpt()), true);
                    emitDispVectorReg(id->idReg2(), id->idInsOpt(), !isExtendAlias);
                }
                else // SHR opcodes
                {
                    emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
                    emitDispVectorReg(id->idReg2(), optWidenElemsize(id->idInsOpt()), !isExtendAlias);
                }
            }
            else
            {
                emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
                emitDispVectorReg(id->idReg2(), id->idInsOpt(), !isExtendAlias);
            }
            // Print the immediate unless we have a sxtl or uxtl instruction
            if (!isExtendAlias)
            {
                emitDispImm(imm, false);
            }
            break;

        case IF_DV_2B: // DV_2B   .Q.........iiiii ......nnnnnddddd      Rd Vn[] (umov/smov    - to general)
            srcsize = id->idOpSize();
            index   = emitGetInsSC(id);
            if (ins == INS_smov)
            {
                dstsize = EA_8BYTE;
            }
            else // INS_umov or INS_mov
            {
                dstsize = (srcsize == EA_8BYTE) ? EA_8BYTE : EA_4BYTE;
            }
            emitDispReg(id->idReg1(), dstsize, true);
            emitDispVectorRegIndex(id->idReg2(), srcsize, index, false);
            break;

        case IF_DV_2C: // DV_2C   .Q.........iiiii ......nnnnnddddd      Vd Rn   (dup/ins - vector from general)
            if (ins == INS_dup)
            {
                datasize = id->idOpSize();
                assert(isValidVectorDatasize(datasize));
                assert(isValidArrangement(datasize, id->idInsOpt()));
                elemsize = optGetElemsize(id->idInsOpt());
                emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            }
            else // INS_ins
            {
                elemsize = id->idOpSize();
                index    = emitGetInsSC(id);
                assert(isValidVectorElemsize(elemsize));
                emitDispVectorRegIndex(id->idReg1(), elemsize, index, true);
            }
            emitDispReg(id->idReg2(), (elemsize == EA_8BYTE) ? EA_8BYTE : EA_4BYTE, false);
            break;

        case IF_DV_2D: // DV_2D   .Q.........iiiii ......nnnnnddddd      Vd Vn[]   (dup - vector)
            datasize = id->idOpSize();
            assert(isValidVectorDatasize(datasize));
            assert(isValidArrangement(datasize, id->idInsOpt()));
            elemsize = optGetElemsize(id->idInsOpt());
            index    = emitGetInsSC(id);
            emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            emitDispVectorRegIndex(id->idReg2(), elemsize, index, false);
            break;

        case IF_DV_2E: // DV_2E   ...........iiiii ......nnnnnddddd      Vd Vn[]   (dup - scalar)
            elemsize = id->idOpSize();
            index    = emitGetInsSC(id);
            emitDispReg(id->idReg1(), elemsize, true);
            emitDispVectorRegIndex(id->idReg2(), elemsize, index, false);
            break;

        case IF_DV_2F: // DV_2F   ...........iiiii .jjjj.nnnnnddddd      Vd[] Vn[] (ins - element)
            imm      = emitGetInsSC(id);
            index    = (imm >> 4) & 0xf;
            index2   = imm & 0xf;
            elemsize = id->idOpSize();
            emitDispVectorRegIndex(id->idReg1(), elemsize, index, true);
            emitDispVectorRegIndex(id->idReg2(), elemsize, index2, false);
            break;

        case IF_DV_2G: // DV_2G   .........X...... ......nnnnnddddd      Vd Vn      (fmov, fcvtXX - register)
        case IF_DV_2K: // DV_2K   .........X.mmmmm ......nnnnn.....      Vn Vm      (fcmp)
        case IF_DV_2L: // DV_2L   ........XX...... ......nnnnnddddd      Vd Vn      (abs, neg - scalar)
            elemsize = id->idOpSize();
            emitDispReg(id->idReg1(), elemsize, true);
            emitDispReg(id->idReg2(), elemsize, false);
            break;

        case IF_DV_2H: // DV_2H   X........X...... ......nnnnnddddd      Rd Vn      (fmov, fcvtXX - to general)
        case IF_DV_2I: // DV_2I   X........X...... ......nnnnnddddd      Vd Rn      (fmov, Xcvtf - from general)
        case IF_DV_2J: // DV_2J   ........SS.....D D.....nnnnnddddd      Vd Vn      (fcvt)
            dstsize = optGetDstsize(id->idInsOpt());
            srcsize = optGetSrcsize(id->idInsOpt());

            emitDispReg(id->idReg1(), dstsize, true);
            emitDispReg(id->idReg2(), srcsize, false);
            break;

        case IF_DV_3A: // DV_3A   .Q......XX.mmmmm ......nnnnnddddd      Vd Vn Vm  (vector)
        case IF_DV_3B: // DV_3B   .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm  (vector)
            emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            emitDispVectorReg(id->idReg2(), id->idInsOpt(), true);
            emitDispVectorReg(id->idReg3(), id->idInsOpt(), false);
            break;

        case IF_DV_3C: // DV_3C   .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm  (vector)
            emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            if (ins != INS_mov)
            {
                emitDispVectorReg(id->idReg2(), id->idInsOpt(), true);
            }
            emitDispVectorReg(id->idReg3(), id->idInsOpt(), false);
            break;

        case IF_DV_3AI: // DV_3AI  .Q......XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
        case IF_DV_3BI: // DV_3BI  .Q........Lmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by elem)
            emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
            emitDispVectorReg(id->idReg2(), id->idInsOpt(), true);
            elemsize = optGetElemsize(id->idInsOpt());
            emitDispVectorRegIndex(id->idReg3(), elemsize, emitGetInsSC(id), false);
            break;

        case IF_DV_3D: // DV_3D   .........X.mmmmm ......nnnnnddddd      Vd Vn Vm  (scalar)
        case IF_DV_3E: // DV_3E   ...........mmmmm ......nnnnnddddd      Vd Vn Vm  (scalar)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispReg(id->idReg3(), size, false);
            break;

        case IF_DV_3F: // DV_3F   ..........mmmmm ......nnnnnddddd       Vd Vn Vm (vector)
            if ((ins == INS_sha1c) || (ins == INS_sha1m) || (ins == INS_sha1p))
            {
                // Qd, Sn, Vm (vector)
                emitDispReg(id->idReg1(), size, true);
                emitDispReg(id->idReg2(), EA_4BYTE, true);
                emitDispVectorReg(id->idReg3(), id->idInsOpt(), false);
            }
            else if ((ins == INS_sha256h) || (ins == INS_sha256h2))
            {
                // Qd Qn Vm (vector)
                emitDispReg(id->idReg1(), size, true);
                emitDispReg(id->idReg2(), size, true);
                emitDispVectorReg(id->idReg3(), id->idInsOpt(), false);
            }
            else // INS_sha1su0, INS_sha256su1
            {
                // Vd, Vn, Vm   (vector)
                emitDispVectorReg(id->idReg1(), id->idInsOpt(), true);
                emitDispVectorReg(id->idReg2(), id->idInsOpt(), true);
                emitDispVectorReg(id->idReg3(), id->idInsOpt(), false);
            }
            break;

        case IF_DV_3DI: // DV_3DI  .........XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by elem)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            elemsize = size;
            emitDispVectorRegIndex(id->idReg3(), elemsize, emitGetInsSC(id), false);
            break;

        case IF_DV_4A: // DV_4A   .........X.mmmmm .aaaaannnnnddddd      Vd Va Vn Vm (scalar)
            emitDispReg(id->idReg1(), size, true);
            emitDispReg(id->idReg2(), size, true);
            emitDispReg(id->idReg3(), size, true);
            emitDispReg(id->idReg4(), size, false);
            break;

        case IF_SN_0A: // SN_0A   ................ ................
            break;

        case IF_SI_0A: // SI_0A   ...........iiiii iiiiiiiiiii.....               imm16
            emitDispImm(emitGetInsSC(id), false);
            break;

        case IF_SI_0B: // SI_0B   ................ ....bbbb........               imm4 - barrier
            emitDispBarrier((insBarrier)emitGetInsSC(id));
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
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
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

    if (varx >= 0 && emitComp->opts.varNames)
    {
        LclVarDsc*  varDsc;
        const char* varName;

        assert((unsigned)varx < emitComp->lvaCount);
        varDsc  = emitComp->lvaTable + varx;
        varName = emitComp->compLocalVarName(varx, offs);

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
}

#endif // DEBUG

// Generate code for a load or store operation with a potentially complex addressing mode
// This method handles the case of a GT_IND with contained GT_LEA op1 of the x86 form [base + index*sccale + offset]
// Since Arm64 does not directly support this complex of an addressing mode
// we may generates up to three instructions for this for Arm64
//
void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    GenTree* addr = indir->Addr();

    if (addr->isContained())
    {
        assert(addr->OperGet() == GT_LCL_VAR_ADDR || addr->OperGet() == GT_LEA);

        int   offset = 0;
        DWORD lsl    = 0;

        if (addr->OperGet() == GT_LEA)
        {
            offset = addr->AsAddrMode()->Offset();
            if (addr->AsAddrMode()->gtScale > 0)
            {
                assert(isPow2(addr->AsAddrMode()->gtScale));
                BitScanForward(&lsl, addr->AsAddrMode()->gtScale);
            }
        }

        GenTree* memBase = indir->Base();

        if (indir->HasIndex())
        {
            GenTree* index = indir->Index();

            if (offset != 0)
            {
                regNumber tmpReg = indir->GetSingleTempReg();

                emitAttr addType = varTypeIsGC(memBase) ? EA_BYREF : EA_PTRSIZE;

                if (emitIns_valid_imm_for_add(offset, EA_8BYTE))
                {
                    if (lsl > 0)
                    {
                        // Generate code to set tmpReg = base + index*scale
                        emitIns_R_R_R_I(INS_add, addType, tmpReg, memBase->gtRegNum, index->gtRegNum, lsl,
                                        INS_OPTS_LSL);
                    }
                    else // no scale
                    {
                        // Generate code to set tmpReg = base + index
                        emitIns_R_R_R(INS_add, addType, tmpReg, memBase->gtRegNum, index->gtRegNum);
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
                    emitIns_R_R_R(INS_add, addType, tmpReg, tmpReg, memBase->gtRegNum);

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));
                    noway_assert(tmpReg != index->gtRegNum);

                    // Then load/store dataReg from/to [tmpReg + index*scale]
                    emitIns_R_R_R_I(ins, attr, dataReg, tmpReg, index->gtRegNum, lsl, INS_OPTS_LSL);
                }
            }
            else // (offset == 0)
            {
                if (lsl > 0)
                {
                    // Then load/store dataReg from/to [memBase + index*scale]
                    emitIns_R_R_R_I(ins, attr, dataReg, memBase->gtRegNum, index->gtRegNum, lsl, INS_OPTS_LSL);
                }
                else // no scale
                {
                    // Then load/store dataReg from/to [memBase + index]
                    emitIns_R_R_R(ins, attr, dataReg, memBase->gtRegNum, index->gtRegNum);
                }
            }
        }
        else // no Index register
        {
            if (emitIns_valid_imm_for_ldst_offset(offset, emitTypeSize(indir->TypeGet())))
            {
                // Then load/store dataReg from/to [memBase + offset]
                emitIns_R_R_I(ins, attr, dataReg, memBase->gtRegNum, offset);
            }
            else
            {
                // We require a tmpReg to hold the offset
                regNumber tmpReg = indir->GetSingleTempReg();

                // First load/store tmpReg with the large offset constant
                codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                // Then load/store dataReg from/to [memBase + tmpReg]
                emitIns_R_R_R(ins, attr, dataReg, memBase->gtRegNum, tmpReg);
            }
        }
    }
    else // addr is not contained, so we evaluate it into a register
    {
        // Then load/store dataReg from/to [addrReg]
        emitIns_R_R(ins, attr, dataReg, addr->gtRegNum);
    }
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
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
        emitIns_R_I(ins, attr, dst->gtRegNum, intConst->IconValue());
        return dst->gtRegNum;
    }
    else
    {
        emitIns_R_R(ins, attr, dst->gtRegNum, src->gtRegNum);
        return dst->gtRegNum;
    }
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

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

    bool isMulOverflow = false;
    if (dst->gtOverflowEx())
    {
        if ((ins == INS_add) || (ins == INS_adds))
        {
            ins = INS_adds;
        }
        else if ((ins == INS_sub) || (ins == INS_subs))
        {
            ins = INS_subs;
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
    if (intConst != nullptr)
    {
        emitIns_R_R_I(ins, attr, dst->gtRegNum, nonIntReg->gtRegNum, intConst->IconValue());
    }
    else
    {
        if (isMulOverflow)
        {
            regNumber extraReg = dst->GetSingleTempReg();
            assert(extraReg != dst->gtRegNum);

            if ((dst->gtFlags & GTF_UNSIGNED) != 0)
            {
                if (attr == EA_4BYTE)
                {
                    // Compute 8 byte results from 4 byte by 4 byte multiplication.
                    emitIns_R_R_R(INS_umull, EA_8BYTE, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);

                    // Get the high result by shifting dst.
                    emitIns_R_R_I(INS_lsr, EA_8BYTE, extraReg, dst->gtRegNum, 32);
                }
                else
                {
                    assert(attr == EA_8BYTE);
                    // Compute the high result.
                    emitIns_R_R_R(INS_umulh, attr, extraReg, src1->gtRegNum, src2->gtRegNum);

                    // Now multiply without skewing the high result.
                    emitIns_R_R_R(ins, attr, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);
                }

                // zero-sign bit comparison to detect overflow.
                emitIns_R_I(INS_cmp, attr, extraReg, 0);
            }
            else
            {
                int bitShift = 0;
                if (attr == EA_4BYTE)
                {
                    // Compute 8 byte results from 4 byte by 4 byte multiplication.
                    emitIns_R_R_R(INS_smull, EA_8BYTE, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);

                    // Get the high result by shifting dst.
                    emitIns_R_R_I(INS_lsr, EA_8BYTE, extraReg, dst->gtRegNum, 32);

                    bitShift = 31;
                }
                else
                {
                    assert(attr == EA_8BYTE);
                    // Save the high result in a temporary register.
                    emitIns_R_R_R(INS_smulh, attr, extraReg, src1->gtRegNum, src2->gtRegNum);

                    // Now multiply without skewing the high result.
                    emitIns_R_R_R(ins, attr, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);

                    bitShift = 63;
                }

                // Sign bit comparison to detect overflow.
                emitIns_R_R_I(INS_cmp, attr, extraReg, dst->gtRegNum, bitShift, INS_OPTS_ASR);
            }
        }
        else
        {
            // We can just multiply.
            emitIns_R_R_R(ins, attr, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);
        }
    }

    if (dst->gtOverflowEx())
    {
        assert(!varTypeIsFloating(dst));
        codeGen->genCheckOverflow(dst);
    }

    return dst->gtRegNum;
}

#endif // defined(_TARGET_ARM64_)
