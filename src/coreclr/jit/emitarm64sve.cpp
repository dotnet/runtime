// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitArm64sve.cpp                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARM64

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"

/*****************************************************************************
 *
 *  Add a SVE instruction with a single immediate value.
 */

void emitter::emitInsSve_I(instruction ins, emitAttr attr, ssize_t imm)
{
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    if (ins == INS_sve_setffr)
    {
        fmt  = IF_SVE_DQ_0A;
        attr = EA_PTRSIZE;
        imm  = 0;
    }
    else
    {
        unreached();
    }

    instrDesc* id = emitNewInstrSC(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a single register.
 */

void emitter::emitInsSve_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt /* = INS_OPTS_NONE */)
{
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_aesmc:
        case INS_sve_aesimc:
            opt = INS_OPTS_SCALABLE_B;
            assert(isVectorRegister(reg)); // ddddd
            assert(isScalableVectorSize(attr));
            fmt = IF_SVE_GL_1A;
            break;

        case INS_sve_rdffr:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // DDDD
            fmt = IF_SVE_DH_1A;
            break;

        case INS_sve_pfalse:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // DDDD
            fmt = IF_SVE_DJ_1A;
            break;

        case INS_sve_wrffr:
            opt = INS_OPTS_SCALABLE_B;
            assert(isPredicateRegister(reg)); // NNNN
            fmt = IF_SVE_DR_1A;
            break;

        case INS_sve_ptrue:
            assert(insOptsScalableStandard(opt));
            assert(isHighPredicateRegister(reg));                  // DDD
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DZ_1A;
            break;

        case INS_sve_fmov:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EB_1B;

            // FMOV is a pseudo-instruction for DUP, which is aliased by MOV;
            // MOV is the preferred disassembly
            ins = INS_sve_mov;
            break;

        default:
            unreached();
            break;
    }

    instrDesc* id = emitNewInstrSmall(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);
    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register and a constant.
 */

void emitter::emitInsSve_R_I(instruction     ins,
                             emitAttr        attr,
                             regNumber       reg,
                             ssize_t         imm,
                             insOpts         opt, /* = INS_OPTS_NONE */
                             insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size      = EA_SIZE(attr);
    bool      canEncode = false;
    bool      signedImm = false;
    bool      hasShift  = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bitMaskImm bmi;

        case INS_sve_rdvl:
            assert(insOptsNone(opt));
            assert(size == EA_8BYTE);
            assert(isGeneralRegister(reg)); // ddddd
            assert(isValidSimm<6>(imm));    // iiiiii
            fmt       = IF_SVE_BC_1A;
            canEncode = true;
            break;

        case INS_sve_smax:
        case INS_sve_smin:
            signedImm = true;

            FALLTHROUGH;
        case INS_sve_umax:
        case INS_sve_umin:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (signedImm)
            {
                assert(isValidSimm<8>(imm)); // iiiiiiii
            }
            else
            {
                assert(isValidUimm<8>(imm)); // iiiiiiii
            }

            fmt       = IF_SVE_ED_1A;
            canEncode = true;
            break;

        case INS_sve_mul:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidSimm<8>(imm));                           // iiiiiiii
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt       = IF_SVE_EE_1A;
            canEncode = true;
            break;

        case INS_sve_mov:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            if (sopt == INS_SCALABLE_OPTS_IMM_BITMASK)
            {
                bmi.immNRS = 0;
                canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);

                if (!useMovDisasmForBitMask(imm))
                {
                    ins = INS_sve_dupm;
                }

                imm = bmi.immNRS; // iiiiiiiiiiiii
                assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
                fmt = IF_SVE_BT_1A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

                if (!isValidSimm<8>(imm))
                {
                    // Size specifier must be able to fit a left-shifted immediate
                    assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                    assert(insOptsScalableAtLeastHalf(opt));
                    hasShift = true;
                    imm >>= 8;
                }

                fmt       = IF_SVE_EB_1A;
                canEncode = true;
            }
            break;

        case INS_sve_dup:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (!isValidSimm<8>(imm))
            {
                // Size specifier must be able to fit a left-shifted immediate
                assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            fmt       = IF_SVE_EB_1A;
            canEncode = true;

            // MOV is an alias for DUP, and is always the preferred disassembly.
            ins = INS_sve_mov;
            break;

        case INS_sve_add:
        case INS_sve_sub:
        case INS_sve_sqadd:
        case INS_sve_sqsub:
        case INS_sve_uqadd:
        case INS_sve_uqsub:
        case INS_sve_subr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            if (!isValidUimm<8>(imm))
            {
                // Size specifier must be able to fit left-shifted immediate
                assert((isValidUimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            fmt       = IF_SVE_EC_1A;
            canEncode = true;
            break;

        case INS_sve_and:
        case INS_sve_orr:
        case INS_sve_eor:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_bic:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // AND is an alias for BIC, and is always the preferred disassembly.
            ins = INS_sve_and;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_eon:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // EOR is an alias for EON, and is always the preferred disassembly.
            ins = INS_sve_eor;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_orn:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            // ORR is an alias for ORN, and is always the preferred disassembly.
            ins = INS_sve_orr;
            imm = -imm - 1;

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            imm        = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BS_1A;
            break;

        case INS_sve_dupm:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg)); // ddddd

            bmi.immNRS = 0;
            canEncode  = canEncodeBitMaskImm(imm, optGetSveElemsize(opt), &bmi);
            fmt        = IF_SVE_BT_1A;

            if (useMovDisasmForBitMask(imm))
            {
                ins = INS_sve_mov;
            }

            imm = bmi.immNRS; // iiiiiiiiiiiii
            assert(isValidImmNRS(imm, optGetSveElemsize(opt)));
            break;

        default:
            unreached();
            break;
    }

    assert(canEncode);

    // For encodings with shifted immediates, we need a way to determine if the immediate has been shifted or not.
    // We could just leave the immediate in its unshifted form, and call emitNewInstrSC,
    // but that would allocate unnecessarily large descriptors. Therefore:
    // - For encodings without any shifting, just call emitNewInstrSC.
    // - For unshifted immediates, call emitNewInstrSC.
    //   If it allocates a small descriptor, idHasShift() will always return false.
    //   Else, idHasShift still returns false, as we set the dedicated bit in large descriptors to false.
    // - For immediates that need a shift, call emitNewInstrCns so a normal or large descriptor is used.
    //   idHasShift will always check the dedicated bit, as it is always available. We set this bit to true below.
    instrDesc* id = !hasShift ? emitNewInstrSC(attr, imm) : emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    id->idHasShift(hasShift);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register and a floating point constant.
 */

void emitter::emitInsSve_R_F(
    instruction ins, emitAttr attr, regNumber reg, double immDbl, insOpts opt /* = INS_OPTS_NONE */)
{
    ssize_t   imm       = 0;
    bool      canEncode = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        floatImm8 fpi;

        case INS_sve_fmov:
        case INS_sve_fdup:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg));                         // ddddd
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            fpi.immFPIVal = 0;
            canEncode     = canEncodeFloatImm8(immDbl, &fpi);
            imm           = fpi.immFPIVal;
            fmt           = IF_SVE_EA_1A;

            // FMOV is an alias for FDUP, and is always the preferred disassembly.
            ins = INS_sve_fmov;
            break;

        default:
            unreached();
            break;
    }

    assert(canEncode);

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
 *  Add a SVE instruction referencing two registers
 */

void emitter::emitInsSve_R_R(instruction     ins,
                             emitAttr        attr,
                             regNumber       reg1,
                             regNumber       reg2,
                             insOpts         opt /* = INS_OPTS_NONE */,
                             insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_pmov:
            if (opt != INS_OPTS_SCALABLE_B)
            {
                assert(insOptsScalableStandard(opt));
                return emitInsSve_R_R_I(INS_sve_pmov, attr, reg1, reg2, 0, opt, sopt);
            }
            if (sopt == INS_SCALABLE_OPTS_TO_PREDICATE)
            {
                assert(isPredicateRegister(reg1));
                assert(isVectorRegister(reg2));
                fmt = IF_SVE_CE_2A;
            }
            else if (sopt == INS_SCALABLE_OPTS_TO_VECTOR)
            {
                assert(isVectorRegister(reg1));
                assert(isPredicateRegister(reg2));
                fmt = IF_SVE_CF_2A;
            }
            else
            {
                assert(!"invalid instruction");
            }
            break;

        case INS_sve_movs:
        {
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // nnnn
            fmt = IF_SVE_CZ_4A_A;
            break;
        }

        case INS_sve_mov:
        {
            if (isGeneralRegisterOrSP(reg2))
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                assert(isVectorRegister(reg1));
#ifdef DEBUG
                if (opt == INS_OPTS_SCALABLE_D)
                {
                    assert(size == EA_8BYTE);
                }
                else
                {
                    assert(size == EA_4BYTE);
                }
#endif // DEBUG
                reg2 = encodingSPtoZR(reg2);
                fmt  = IF_SVE_CB_2A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // dddd
                assert(isPredicateRegister(reg2)); // nnnn
                fmt = IF_SVE_CZ_4A_L;
            }
            break;
        }

        case INS_sve_insr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1)); // ddddd
            if (isVectorRegister(reg2))
            {
                fmt = IF_SVE_CC_2A;
            }
            else if (isGeneralRegisterOrZR(reg2))
            {
                fmt = IF_SVE_CD_2A;
            }
            else
            {
                unreached();
            }
            break;

        case INS_sve_pfirst:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            fmt = IF_SVE_DD_2A;
            break;

        case INS_sve_pnext:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));                     // DDDD
            assert(isPredicateRegister(reg2));                     // VVVV
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DF_2A;
            break;

        case INS_sve_punpkhi:
        case INS_sve_punpklo:
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // NNNN
            fmt = IF_SVE_CK_2A;
            break;

        case INS_sve_rdffr:
        case INS_sve_rdffrs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            fmt = IF_SVE_DG_2A;
            break;

        case INS_sve_rev:
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(insOptsScalableStandard(opt));
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_CG_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // NNNN
                fmt = IF_SVE_CJ_2A;
            }
            break;

        case INS_sve_ptest:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // gggg
            assert(isPredicateRegister(reg2)); // NNNN
            fmt = IF_SVE_DI_2A;
            break;

        case INS_sve_cntp:
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsWithVectorLength(sopt));         // l
            assert(isGeneralRegister(reg1));                       // ddddd
            assert(isPredicateRegister(reg2));                     // NNNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DL_2A;
            break;

        case INS_sve_incp:
        case INS_sve_decp:
            assert(isPredicateRegister(reg2)); // MMMM

            if (isGeneralRegister(reg1)) // ddddd
            {
                assert(insOptsScalableStandard(opt)); // xx
                assert(size == EA_8BYTE);
                fmt = IF_SVE_DM_2A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt)); // xx
                assert(isVectorRegister(reg1));          // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_DN_2A;
            }
            break;

        case INS_sve_sqincp:
        case INS_sve_uqincp:
        case INS_sve_sqdecp:
        case INS_sve_uqdecp:
            assert(isPredicateRegister(reg2)); // MMMM

            if (isGeneralRegister(reg1)) // ddddd
            {
                assert(insOptsScalableStandard(opt)); // xx
                assert(isValidGeneralDatasize(size));
                fmt = IF_SVE_DO_2A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt)); // xx
                assert(isVectorRegister(reg1));          // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_DP_2A;
            }
            break;

        case INS_sve_ctermeq:
        case INS_sve_ctermne:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));      // nnnnn
            assert(isGeneralRegister(reg2));      // mmmmm
            assert(isValidGeneralDatasize(size)); // x
            fmt = IF_SVE_DS_2A;
            break;

        case INS_sve_sqcvtn:
        case INS_sve_uqcvtn:
        case INS_sve_sqcvtun:
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isLowVectorRegister(reg2)); // nnnn
            fmt = IF_SVE_FZ_2A;
            break;

        case INS_sve_fcvtn:
        case INS_sve_bfcvtn:
        case INS_sve_fcvtnt:
        case INS_sve_fcvtnb:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isLowVectorRegister(reg2)); // nnnn
            fmt = IF_SVE_HG_2A;
            break;

        case INS_sve_sqxtnb:
        case INS_sve_sqxtnt:
        case INS_sve_uqxtnb:
        case INS_sve_uqxtnt:
        case INS_sve_sqxtunb:
        case INS_sve_sqxtunt:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(optGetSveElemsize(opt) != EA_8BYTE);
            assert(isValidVectorElemsize(optGetSveElemsize(opt)));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_GD_2A;
            break;

        case INS_sve_aese:
        case INS_sve_aesd:
        case INS_sve_sm4e:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
#ifdef DEBUG
            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert(ins == INS_sve_sm4e);
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
            }
#endif // DEBUG
            fmt = IF_SVE_GK_2A;
            break;

        case INS_sve_frecpe:
        case INS_sve_frsqrte:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HF_2A;
            break;

        case INS_sve_sunpkhi:
        case INS_sve_sunpklo:
        case INS_sve_uunpkhi:
        case INS_sve_uunpklo:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_CH_2A;
            break;

        case INS_sve_fexpa:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_BJ_2A;
            break;

        case INS_sve_dup:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrSP(reg2));
#ifdef DEBUG
            if (opt == INS_OPTS_SCALABLE_D)
            {
                assert(size == EA_8BYTE);
            }
            else
            {
                assert(size == EA_4BYTE);
            }
#endif // DEBUG
            reg2 = encodingSPtoZR(reg2);
            fmt  = IF_SVE_CB_2A;

            // DUP is an alias for MOV;
            // MOV is the preferred disassembly
            ins = INS_sve_mov;
            break;

        case INS_sve_bf1cvt:
        case INS_sve_bf1cvtlt:
        case INS_sve_bf2cvt:
        case INS_sve_bf2cvtlt:
        case INS_sve_f1cvt:
        case INS_sve_f1cvtlt:
        case INS_sve_f2cvt:
        case INS_sve_f2cvtlt:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HH_2A;
            unreached(); // not supported yet
            break;

        case INS_sve_movprfx:
            assert(insScalableOptsNone(sopt));
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_BI_2A;
            break;

        case INS_sve_fmov:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_BV_2B;

            // CPY is an alias for FMOV, and MOV is an alias for CPY.
            // Thus, MOV is the preferred disassembly.
            ins = INS_sve_mov;
            break;

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id;

    if (insScalableOptsWithVectorLength(sopt))
    {
        id = emitNewInstr(attr);
        id->idVectorLength4x(sopt == INS_SCALABLE_OPTS_VL_4X);
    }
    else
    {
        id = emitNewInstrSmall(attr);
    }

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
 *  Add a SVE instruction referencing a register and two constants.
 */

void emitter::emitInsSve_R_I_I(
    instruction ins, emitAttr attr, regNumber reg, ssize_t imm1, ssize_t imm2, insOpts opt /* = INS_OPTS_NONE */)
{
    insFormat fmt;
    ssize_t   immOut;

    if (ins == INS_sve_index)
    {
        assert(insOptsScalableStandard(opt));
        assert(isVectorRegister(reg));                         // ddddd
        assert(isValidSimm<5>(imm1));                          // iiiii
        assert(isValidSimm<5>(imm2));                          // iiiii
        assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
        immOut = insEncodeTwoSimm5(imm1, imm2);
        fmt    = IF_SVE_AX_1A;
    }
    else
    {
        unreached();
    }

    instrDesc* id = emitNewInstrSC(attr, immOut);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers and a constant.
 */

void emitter::emitInsSve_R_R_I(instruction     ins,
                               emitAttr        attr,
                               regNumber       reg1,
                               regNumber       reg2,
                               ssize_t         imm,
                               insOpts         opt /* = INS_OPTS_NONE */,
                               insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size     = EA_SIZE(attr);
    bool      hasShift = false;
    insFormat fmt;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        bool isRightShift;

        case INS_sve_asr:
        case INS_sve_lsl:
        case INS_sve_lsr:
        case INS_sve_srshr:
        case INS_sve_sqshl:
        case INS_sve_urshr:
        case INS_sve_sqshlu:
        case INS_sve_uqshl:
        case INS_sve_asrd:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(isScalableVectorSize(size));

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert((ins == INS_sve_asr) || (ins == INS_sve_lsl) || (ins == INS_sve_lsr));
                assert(isVectorRegister(reg1));
                assert(isVectorRegister(reg2));
                fmt = IF_SVE_BF_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isVectorRegister(reg1));       // ddddd
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AM_2A;
            }
            break;

        case INS_sve_xar:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // xiii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xxiii
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimmFrom1<6>(imm)); // x xxiii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_AW_2A;
            break;

        case INS_sve_index:
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isValidSimm<5>(imm));                           // iiiii
            assert(isIntegerRegister(reg2));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_IMM_FIRST)
            {
                fmt = IF_SVE_AY_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_AZ_2A;
            }
            break;

        case INS_sve_addvl:
        case INS_sve_addpl:
            assert(insOptsNone(opt));
            assert(size == EA_8BYTE);
            assert(isGeneralRegisterOrSP(reg1)); // ddddd
            assert(isGeneralRegisterOrSP(reg2)); // nnnnn
            assert(isValidSimm<6>(imm));         // iiiiii
            reg1 = encodingSPtoZR(reg1);
            reg2 = encodingSPtoZR(reg2);
            fmt  = IF_SVE_BB_2A;
            break;

        case INS_sve_mov:
            if (sopt == INS_SCALABLE_OPTS_BROADCAST)
            {
                return emitInsSve_R_R_I(INS_sve_dup, attr, reg1, reg2, imm, opt, sopt);
            }
            FALLTHROUGH;
        case INS_sve_cpy:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));    // DDDDD
            assert(isPredicateRegister(reg2)); // GGGG

            if (!isValidSimm<8>(imm))
            {
                // Size specifier must be able to fit a left-shifted immediate
                assert((isValidSimm_MultipleOf<8, 256>(imm))); // iiiiiiii
                assert(insOptsScalableAtLeastHalf(opt));
                hasShift = true;
                imm >>= 8;
            }

            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                fmt = IF_SVE_BV_2A_J;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BV_2A;
            }

            // MOV is an alias for CPY, and is always the preferred disassembly.
            ins = INS_sve_mov;
            break;

        case INS_sve_dup:
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1)); // DDDDD
            assert(isVectorRegister(reg2)); // GGGG
            assert(isValidBroadcastImm(imm, optGetSveElemsize(opt)));
            fmt = IF_SVE_BW_2A;
            ins = INS_sve_mov; // Set preferred alias for disassembly
            break;

        case INS_sve_pmov:
            if (sopt == INS_SCALABLE_OPTS_TO_PREDICATE)
            {
                assert(isPredicateRegister(reg1));
                assert(isVectorRegister(reg2));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_D:
                        assert(isValidUimm<3>(imm));
                        fmt = IF_SVE_CE_2B;
                        break;
                    case INS_OPTS_SCALABLE_S:
                        assert(isValidUimm<2>(imm));
                        fmt = IF_SVE_CE_2D;
                        break;
                    case INS_OPTS_SCALABLE_H:
                        assert(isValidUimm<1>(imm));
                        fmt = IF_SVE_CE_2C;
                        break;
                    default:
                        unreached();
                }
            }
            else if (sopt == INS_SCALABLE_OPTS_TO_VECTOR)
            {
                assert(isVectorRegister(reg1));
                assert(isPredicateRegister(reg2));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_D:
                        assert(isValidUimm<3>(imm));
                        fmt = IF_SVE_CF_2B;
                        break;
                    case INS_OPTS_SCALABLE_S:
                        assert(isValidUimm<2>(imm));
                        fmt = IF_SVE_CF_2D;
                        break;
                    case INS_OPTS_SCALABLE_H:
                        assert(isValidUimm<1>(imm));
                        fmt = IF_SVE_CF_2C;
                        break;
                    default:
                        unreached();
                }
            }
            else
            {
                unreached();
            }
            break;

        case INS_sve_sqrshrn:
        case INS_sve_sqrshrun:
        case INS_sve_uqrshrn:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isRightShift); // These are always right-shift.
            assert(isValidVectorShiftAmount(imm, EA_4BYTE, isRightShift));
            fmt = IF_SVE_GA_2A;
            break;

        case INS_sve_pext:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));                     // DDDD
            assert(isHighPredicateRegister(reg2));                 // NNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR)
            {
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_DW_2B;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_DW_2A;
            }
            break;

        case INS_sve_sshllb:
        case INS_sve_sshllt:
        case INS_sve_ushllb:
        case INS_sve_ushllt:
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_FR_2A;
            break;

        case INS_sve_sqshrunb:
        case INS_sve_sqshrunt:
        case INS_sve_sqrshrunb:
        case INS_sve_sqrshrunt:
        case INS_sve_shrnb:
        case INS_sve_shrnt:
        case INS_sve_rshrnb:
        case INS_sve_rshrnt:
        case INS_sve_sqshrnb:
        case INS_sve_sqshrnt:
        case INS_sve_sqrshrnb:
        case INS_sve_sqrshrnt:
        case INS_sve_uqshrnb:
        case INS_sve_uqshrnt:
        case INS_sve_uqrshrnb:
        case INS_sve_uqrshrnt:
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x xx

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimmFrom1<3>(imm)); // iii
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimmFrom1<4>(imm)); // x iii
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimmFrom1<5>(imm)); // xx iii
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_GB_2A;
            break;

        case INS_sve_cadd:
        case INS_sve_sqcadd:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation: 0 if 90, 1 if 270
            imm = emitEncodeRotationImm90_or_270(imm); // r
            fmt = IF_SVE_FV_2A;
            break;

        case INS_sve_ftmad:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isValidUimm<3>(imm));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HN_2A;
            break;

        case INS_sve_ldr:
            assert(insOptsNone(opt));
            assert(isScalableVectorSize(size));
            assert(isGeneralRegister(reg2)); // nnnnn
            assert(isValidSimm<9>(imm));     // iii
                                             // iiiiii

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_IE_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isPredicateRegister(reg1));
                fmt = IF_SVE_ID_2A;
            }
            break;

        case INS_sve_str:
            assert(insOptsNone(opt));
            assert(isScalableVectorSize(size));
            assert(isGeneralRegister(reg2)); // nnnnn
            assert(isValidSimm<9>(imm));     // iii
                                             // iiiiii

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_JH_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isPredicateRegister(reg1));
                fmt = IF_SVE_JG_2A;
            }
            break;

        case INS_sve_sli:
        case INS_sve_sri:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_FT_2A;
            break;

        case INS_sve_srsra:
        case INS_sve_ssra:
        case INS_sve_ursra:
        case INS_sve_usra:
            isRightShift = emitInsIsVectorRightShift(ins);
            assert(isValidVectorShiftAmount(imm, optGetSveElemsize(opt), isRightShift));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_FU_2A;
            break;

        case INS_sve_ext:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isValidUimm<8>(imm));    // iiiii iii

            if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
            {
                fmt = IF_SVE_BQ_2A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BQ_2B;
            }
            break;

        case INS_sve_dupq:
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
#ifdef DEBUG
            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    break;
            }
#endif // DEBUG
            fmt = IF_SVE_BX_2A;
            break;

        case INS_sve_extq:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isScalableVectorSize(size));
            assert(isValidUimm<4>(imm));
            fmt = IF_SVE_BY_2A;
            break;

        default:
            unreached();
            break;
    }

    // For encodings with shifted immediates, we need a way to determine if the immediate has been shifted or not.
    // We could just leave the immediate in its unshifted form, and call emitNewInstrSC,
    // but that would allocate unnecessarily large descriptors. Therefore:
    // - For encodings without any shifting, just call emitNewInstrSC.
    // - For unshifted immediates, call emitNewInstrSC.
    //   If it allocates a small descriptor, idHasShift() will always return false.
    //   Else, idHasShift still returns false, as we set the dedicated bit in large descriptors to false.
    // - For immediates that need a shift, call emitNewInstrCns so a normal or large descriptor is used.
    //   idHasShift will always check the dedicated bit, as it is always available. We set this bit to true below.
    instrDesc* id = !hasShift ? emitNewInstrSC(attr, imm) : emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);

    id->idHasShift(hasShift);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers and a floating point constant.
 */

void emitter::emitInsSve_R_R_F(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, double immDbl, insOpts opt /* = INS_OPTS_NONE */)
{
    ssize_t   imm  = 0;
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_fmul:
        case INS_sve_fmaxnm:
        case INS_sve_fadd:
        case INS_sve_fmax:
        case INS_sve_fminnm:
        case INS_sve_fsub:
        case INS_sve_fmin:
        case INS_sve_fsubr:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isScalableVectorSize(size));
            imm = emitEncodeSmallFloatImm(immDbl, ins);
            fmt = IF_SVE_HM_2A;
            break;

        case INS_sve_fmov:
        case INS_sve_fcpy:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            floatImm8 fpi;
            fpi.immFPIVal = 0;
            canEncodeFloatImm8(immDbl, &fpi);
            imm = fpi.immFPIVal;
            fmt = IF_SVE_BU_2A;

            // FMOV is an alias for FCPY, and is always the preferred disassembly.
            ins = INS_sve_fmov;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)

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
 *  Add a SVE instruction referencing three registers.
 */

void emitter::emitInsSve_R_R_R(instruction     ins,
                               emitAttr        attr,
                               regNumber       reg1,
                               regNumber       reg2,
                               regNumber       reg3,
                               insOpts         opt /* = INS_OPTS_NONE */,
                               insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size           = EA_SIZE(attr);
    bool      pmerge         = false;
    bool      vectorLength4x = false;
    insFormat fmt            = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_and:
        case INS_sve_bic:
        case INS_sve_eor:
        case INS_sve_orr:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1)); // mmmmm
            assert(isVectorRegister(reg3)); // ddddd

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isVectorRegister(reg2)); // nnnnn
                fmt = IF_SVE_AU_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AA_3A;
            }
            break;

        case INS_sve_add:
        case INS_sve_sub:
        case INS_sve_subr:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2));
                assert(ins != INS_sve_subr);
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_AB_3A;
            }
            break;

        case INS_sve_addpt:
        case INS_sve_subpt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg3)); // mmmmm

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2)); // nnnnn
                fmt = IF_SVE_AT_3B;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_AB_3B;
            }
            break;

        case INS_sve_sdiv:
        case INS_sve_sdivr:
        case INS_sve_udiv:
        case INS_sve_udivr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AC_3A;
            break;

        case INS_sve_sabd:
        case INS_sve_smax:
        case INS_sve_smin:
        case INS_sve_uabd:
        case INS_sve_umax:
        case INS_sve_umin:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AD_3A;
            break;

        case INS_sve_mul:
        case INS_sve_smulh:
        case INS_sve_umulh:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2));
                fmt = IF_SVE_BD_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2));
                fmt = IF_SVE_AE_3A;
            }
            break;

        case INS_sve_pmul:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_BD_3B;
            break;

        case INS_sve_andv:
        case INS_sve_eorv:
        case INS_sve_orv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AF_3A;
            break;

        case INS_sve_andqv:
        case INS_sve_eorqv:
        case INS_sve_orqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AG_3A;
            break;

        case INS_sve_movprfx:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                pmerge = true;
            }
            fmt = IF_SVE_AH_3A;
            break;

        case INS_sve_saddv:
        case INS_sve_uaddv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWide(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AI_3A;
            break;

        case INS_sve_addqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AJ_3A;
            break;

        case INS_sve_smaxv:
        case INS_sve_sminv:
        case INS_sve_umaxv:
        case INS_sve_uminv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AK_3A;
            break;

        case INS_sve_smaxqv:
        case INS_sve_sminqv:
        case INS_sve_umaxqv:
        case INS_sve_uminqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AL_3A;
            break;

        case INS_sve_asrr:
        case INS_sve_lslr:
        case INS_sve_lsrr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AN_3A;
            break;

        case INS_sve_asr:
        case INS_sve_lsl:
        case INS_sve_lsr:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            if (sopt == INS_SCALABLE_OPTS_WIDE)
            {
                assert(isLowPredicateRegister(reg2));
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_AO_3A;
            }
            else if (sopt == INS_SCALABLE_OPTS_UNPREDICATED_WIDE)
            {
                assert(isVectorRegister(reg2));
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_BG_3A;
            }
            else
            {
                assert(isLowPredicateRegister(reg2));
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_AN_3A;
            }
            break;

        case INS_sve_uzp1:
        case INS_sve_trn1:
        case INS_sve_zip1:
        case INS_sve_uzp2:
        case INS_sve_trn2:
        case INS_sve_zip2:
            assert(insOptsScalable(opt));

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg1)); // ddddd
                assert(isVectorRegister(reg2)); // nnnnn
                assert(isVectorRegister(reg3)); // mmmmm

                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_BR_3B;
                }
                else
                {
                    assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
                    fmt = IF_SVE_BR_3A;
                }
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // NNNN
                assert(isPredicateRegister(reg3)); // MMMM
                fmt = IF_SVE_CI_3A;
            }
            break;

        case INS_sve_tbl:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
            {
                fmt = IF_SVE_BZ_3A_A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_BZ_3A;
            }
            break;

        case INS_sve_tbx:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_BZ_3A;
            break;

        case INS_sve_tbxq:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_CA_3A;
            break;

        case INS_sve_sdot:
        case INS_sve_udot:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                fmt = IF_SVE_EF_3A;
            }
            else
            {
                fmt = IF_SVE_EH_3A;
                assert(insOptsScalableWords(opt));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            }
            break;

        case INS_sve_usdot:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_EI_3A;
            break;

        case INS_sve_smlalb:
        case INS_sve_smlalt:
        case INS_sve_umlalb:
        case INS_sve_umlalt:
        case INS_sve_smlslb:
        case INS_sve_smlslt:
        case INS_sve_umlslb:
        case INS_sve_umlslt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EL_3A;
            break;

        case INS_sve_sqrdmlah:
        case INS_sve_sqrdmlsh:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EM_3A;
            break;

        case INS_sve_sqdmlalbt:
        case INS_sve_sqdmlslbt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EN_3A;
            break;

        case INS_sve_sqdmlalb:
        case INS_sve_sqdmlalt:
        case INS_sve_sqdmlslb:
        case INS_sve_sqdmlslt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EO_3A;
            break;

        case INS_sve_sclamp:
        case INS_sve_uclamp:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EV_3A;
            break;

        case INS_sve_zipq1:
        case INS_sve_zipq2:
        case INS_sve_uzpq1:
        case INS_sve_uzpq2:
        case INS_sve_tblq:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_EX_3A;
            break;

        case INS_sve_saddlb:
        case INS_sve_saddlt:
        case INS_sve_uaddlb:
        case INS_sve_uaddlt:
        case INS_sve_ssublb:
        case INS_sve_ssublt:
        case INS_sve_usublb:
        case INS_sve_usublt:
        case INS_sve_sabdlb:
        case INS_sve_sabdlt:
        case INS_sve_uabdlb:
        case INS_sve_uabdlt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FL_3A;
            break;

        case INS_sve_saddwb:
        case INS_sve_saddwt:
        case INS_sve_uaddwb:
        case INS_sve_uaddwt:
        case INS_sve_ssubwb:
        case INS_sve_ssubwt:
        case INS_sve_usubwb:
        case INS_sve_usubwt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FM_3A;
            break;

        case INS_sve_smullb:
        case INS_sve_smullt:
        case INS_sve_umullb:
        case INS_sve_umullt:
        case INS_sve_sqdmullb:
        case INS_sve_sqdmullt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FN_3A;
            break;

        case INS_sve_pmullb:
        case INS_sve_pmullt:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_Q)
            {
                fmt = IF_SVE_FN_3B;
            }
            else
            {
                assert((opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_D));
                assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
                fmt = IF_SVE_FN_3A;
            }
            break;

        case INS_sve_smmla:
        case INS_sve_usmmla:
        case INS_sve_ummla:
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_FO_3A;
            break;

        case INS_sve_rax1:
        case INS_sve_sm4ekey:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (ins == INS_sve_rax1)
            {
                assert(opt == INS_OPTS_SCALABLE_D);
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
            }

            fmt = IF_SVE_GJ_3A;
            break;

        case INS_sve_fmlalb:
        case INS_sve_fmlalt:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached(); // TODO-SVE: Not yet supported.
                fmt = IF_SVE_GN_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_H);
                fmt = IF_SVE_HB_3A;
            }
            break;

        case INS_sve_fmlslb:
        case INS_sve_fmlslt:
        case INS_sve_bfmlalb:
        case INS_sve_bfmlalt:
        case INS_sve_bfmlslb:
        case INS_sve_bfmlslt:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HB_3A;
            break;

        case INS_sve_bfmmla:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HD_3A;
            break;

        case INS_sve_fmmla:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HD_3A_A;
            break;

        case INS_sve_fmlallbb:
        case INS_sve_fmlallbt:
        case INS_sve_fmlalltb:
        case INS_sve_fmlalltt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_GO_3A;
            break;

        case INS_sve_bfclamp:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_GW_3B;
            break;

        case INS_sve_bfdot:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_HA_3A;
            break;

        case INS_sve_fdot:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                fmt = IF_SVE_HA_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached(); // TODO-SVE: Not yet supported.
                fmt = IF_SVE_HA_3A_E;
            }
            else
            {
                unreached(); // TODO-SVE: Not yet supported.
                assert(insOptsNone(opt));
                fmt = IF_SVE_HA_3A_F;
            }
            break;

        case INS_sve_eorbt:
        case INS_sve_eortb:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FP_3A;
            break;

        case INS_sve_bext:
        case INS_sve_bdep:
        case INS_sve_bgrp:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FQ_3A;
            break;

        case INS_sve_saddlbt:
        case INS_sve_ssublbt:
        case INS_sve_ssubltb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FS_3A;
            break;

        case INS_sve_saba:
        case INS_sve_uaba:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FW_3A;
            break;

        case INS_sve_sabalb:
        case INS_sve_sabalt:
        case INS_sve_uabalb:
        case INS_sve_uabalt:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_FX_3A;
            break;

        case INS_sve_addhnb:
        case INS_sve_addhnt:
        case INS_sve_raddhnb:
        case INS_sve_raddhnt:
        case INS_sve_subhnb:
        case INS_sve_subhnt:
        case INS_sve_rsubhnb:
        case INS_sve_rsubhnt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsScalableWide(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GC_3A;
            break;

        case INS_sve_histseg:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GF_3A;
            break;

        case INS_sve_fclamp:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GW_3A;
            break;

        case INS_sve_clz:
        case INS_sve_cls:
        case INS_sve_cnt:
        case INS_sve_cnot:
        case INS_sve_not:
        case INS_sve_nots:
            if (isPredicateRegister(reg1) && sopt != INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // gggg
                assert(isPredicateRegister(reg3)); // NNNN
                fmt = IF_SVE_CZ_4A;
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isLowPredicateRegister(reg2));
                assert(isVectorRegister(reg3));
                assert(insOptsScalableStandard(opt));
                assert(insScalableOptsNone(sopt));
                fmt = IF_SVE_AP_3A;
            }
            break;

        case INS_sve_fabs:
        case INS_sve_fneg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AP_3A;
            break;

        case INS_sve_abs:
        case INS_sve_neg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxtb:
        case INS_sve_uxtb:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxth:
        case INS_sve_uxth:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_sxtw:
        case INS_sve_uxtw:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_AQ_3A;
            break;

        case INS_sve_index:
            assert(isValidScalarDatasize(size));
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrZR(reg2));
            assert(isGeneralRegisterOrZR(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_BA_3A;
            break;

        case INS_sve_sqdmulh:
        case INS_sve_sqrdmulh:
            assert(isScalableVectorSize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_BE_3A;
            break;

        case INS_sve_ftssel:
            assert(isScalableVectorSize(size));
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_BK_3A;
            break;

        case INS_sve_compact:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CL_3A;
            break;

        case INS_sve_clasta:
        case INS_sve_clastb:
            assert(insOptsScalableStandard(opt));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            if (isGeneralRegister(reg1))
            {
                assert(insScalableOptsNone(sopt));
                assert(isValidScalarDatasize(size));
                fmt = IF_SVE_CO_3A;
            }
            else if (sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR)
            {
                assert(isFloatReg(reg1));
                assert(isValidVectorElemsize(size));
                fmt = IF_SVE_CN_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_CM_3A;
            }
            break;

        case INS_sve_cpy:
        case INS_sve_mov:
            assert(insOptsScalableStandard(opt));
            // TODO-SVE: Following checks can be simplified to check reg1 as predicate register only after adding
            // definitions for predicate registers. Currently, predicate registers P0 to P15 are aliased to simd
            // registers V0 to V15.
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(ins == INS_sve_mov);
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isVectorRegister(reg1)); // ddddd
                assert(isVectorRegister(reg2)); // nnnnn
                assert(isVectorRegister(reg3)); // mmmmm
                fmt = IF_SVE_AU_3A;
                // ORR is an alias for MOV, and is always the preferred disassembly.
                ins = INS_sve_orr;
            }
            else if (isPredicateRegister(reg3) &&
                     (sopt == INS_SCALABLE_OPTS_NONE || sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE))
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // DDDD
                assert(isPredicateRegister(reg2)); // gggg
                assert(isPredicateRegister(reg3)); // NNNN
                fmt = sopt == INS_SCALABLE_OPTS_NONE ? IF_SVE_CZ_4A : IF_SVE_CZ_4A_K;
                // MOV is an alias for CPY, and is always the preferred disassembly.
                ins = INS_sve_mov;
            }
            else if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                assert(isVectorRegister(reg1));
                assert(isPredicateRegister(reg2));
                assert(isVectorRegister(reg3));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_CW_4A;
            }
            else
            {
                assert(isVectorRegister(reg1));
                assert(isLowPredicateRegister(reg2));
                if (isGeneralRegisterOrSP(reg3))
                {
                    assert(insScalableOptsNone(sopt));
                    fmt  = IF_SVE_CQ_3A;
                    reg3 = encodingSPtoZR(reg3);
                }
                else
                {
                    assert(sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
                    assert(isVectorRegister(reg3));
                    fmt = IF_SVE_CP_3A;
                }

                // MOV is an alias for CPY, and is always the preferred disassembly.
                ins = INS_sve_mov;
            }
            break;

        case INS_sve_lasta:
        case INS_sve_lastb:
            assert(insOptsScalableStandard(opt));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            if (isGeneralRegister(reg1))
            {
                assert(insScalableOptsNone(sopt));
                assert(isGeneralRegister(reg1));
                fmt = IF_SVE_CS_3A;
            }
            else if (sopt == INS_SCALABLE_OPTS_WITH_SIMD_SCALAR)
            {
                assert(isVectorRegister(reg1));
                fmt = IF_SVE_CR_3A;
            }
            break;

        case INS_sve_revd:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_CT_3A;
            break;

        case INS_sve_rbit:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revb:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revh:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableWords(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_revw:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_CU_3A;
            break;

        case INS_sve_splice:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            fmt = (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR) ? IF_SVE_CV_3A : IF_SVE_CV_3B;
            break;

        case INS_sve_brka:
        case INS_sve_brkb:
            assert(isPredicateRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isPredicateRegister(reg3));
            assert(insOptsScalableStandard(opt));
            if (sopt == INS_SCALABLE_OPTS_PREDICATE_MERGE)
            {
                pmerge = true;
            }
            fmt = IF_SVE_DB_3A;
            break;

        case INS_sve_brkas:
        case INS_sve_brkbs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isPredicateRegister(reg3));
            fmt = IF_SVE_DB_3B;
            break;

        case INS_sve_brkn:
        case INS_sve_brkns:
            assert(insOptsScalable(opt));
            assert(isPredicateRegister(reg1)); // MMMM
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // NNNN
            fmt = IF_SVE_DC_3A;
            break;

        case INS_sve_cntp:
            assert(size == EA_8BYTE);
            assert(isGeneralRegister(reg1));                       // ddddd
            assert(isPredicateRegister(reg2));                     // gggg
            assert(isPredicateRegister(reg3));                     // NNNN
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_DK_3A;
            break;

        case INS_sve_shadd:
        case INS_sve_shsub:
        case INS_sve_shsubr:
        case INS_sve_srhadd:
        case INS_sve_uhadd:
        case INS_sve_uhsub:
        case INS_sve_uhsubr:
        case INS_sve_urhadd:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_EP_3A;
            break;

        case INS_sve_sadalp:
        case INS_sve_uadalp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_EQ_3A;
            break;

        case INS_sve_addp:
        case INS_sve_smaxp:
        case INS_sve_sminp:
        case INS_sve_umaxp:
        case INS_sve_uminp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_ER_3A;
            break;

        case INS_sve_sqabs:
        case INS_sve_sqneg:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_ES_3A;
            break;

        case INS_sve_urecpe:
        case INS_sve_ursqrte:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_ES_3A;
            break;

        case INS_sve_sqadd:
        case INS_sve_sqsub:
        case INS_sve_uqadd:
        case INS_sve_uqsub:
            assert(isVectorRegister(reg1));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(isScalableVectorSize(size));
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2));
                fmt = IF_SVE_AT_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2));
                fmt = IF_SVE_ET_3A;
            }
            break;

        case INS_sve_sqsubr:
        case INS_sve_suqadd:
        case INS_sve_uqsubr:
        case INS_sve_usqadd:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_ET_3A;
            break;

        case INS_sve_sqrshl:
        case INS_sve_sqrshlr:
        case INS_sve_sqshl:
        case INS_sve_sqshlr:
        case INS_sve_srshl:
        case INS_sve_srshlr:
        case INS_sve_uqrshl:
        case INS_sve_uqrshlr:
        case INS_sve_uqshl:
        case INS_sve_uqshlr:
        case INS_sve_urshl:
        case INS_sve_urshlr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableStandard(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_EU_3A;
            break;

        case INS_sve_fcvtnt:
        case INS_sve_fcvtlt:
            assert(insOptsConvertFloatStepwise(opt));
            FALLTHROUGH;
        case INS_sve_fcvtxnt:
        case INS_sve_bfcvtnt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_GQ_3A;
            break;

        case INS_sve_faddp:
        case INS_sve_fmaxnmp:
        case INS_sve_fmaxp:
        case INS_sve_fminnmp:
        case INS_sve_fminp:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_GR_3A;
            break;

        case INS_sve_faddqv:
        case INS_sve_fmaxnmqv:
        case INS_sve_fminnmqv:
        case INS_sve_fmaxqv:
        case INS_sve_fminqv:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_GS_3A;
            break;

        case INS_sve_fmaxnmv:
        case INS_sve_fmaxv:
        case INS_sve_fminnmv:
        case INS_sve_fminv:
        case INS_sve_faddv:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(isValidVectorElemsizeSveFloat(size));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HE_3A;
            break;

        case INS_sve_fadda:
            assert(isFloatReg(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(isValidVectorElemsizeSveFloat(size));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HJ_3A;
            break;

        case INS_sve_frecps:
        case INS_sve_frsqrts:
        case INS_sve_ftsmul:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_HK_3A;
            break;

        case INS_sve_fadd:
        case INS_sve_fsub:
        case INS_sve_fmul:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2)); // nnnnn
                fmt = IF_SVE_HK_3A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_HL_3A;
            }
            break;

        case INS_sve_fabd:
        case INS_sve_fdiv:
        case INS_sve_fdivr:
        case INS_sve_fmax:
        case INS_sve_fmaxnm:
        case INS_sve_fmin:
        case INS_sve_fminnm:
        case INS_sve_fmulx:
        case INS_sve_fscale:
        case INS_sve_fsubr:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HL_3A;
            break;

        case INS_sve_famax:
        case INS_sve_famin:
            unreached(); // TODO-SVE: Not yet supported.
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HL_3A;
            break;

        case INS_sve_bfmul:
        case INS_sve_bfadd:
        case INS_sve_bfsub:
        case INS_sve_bfmaxnm:
        case INS_sve_bfminnm:
        case INS_sve_bfmax:
        case INS_sve_bfmin:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg3)); // mmmmm

            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                assert(isVectorRegister(reg2)); // nnnnn
                fmt = IF_SVE_HK_3B;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(isLowPredicateRegister(reg2)); // ggg
                fmt = IF_SVE_HL_3B;
            }
            break;

        case INS_sve_bsl:
        case INS_sve_eor3:
        case INS_sve_bcax:
        case INS_sve_bsl1n:
        case INS_sve_bsl2n:
        case INS_sve_nbsl:
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // mmmmm
            assert(isVectorRegister(reg3)); // kkkkk
            fmt = IF_SVE_AV_3A;
            break;

        case INS_sve_frintn:
        case INS_sve_frintm:
        case INS_sve_frintp:
        case INS_sve_frintz:
        case INS_sve_frinta:
        case INS_sve_frintx:
        case INS_sve_frinti:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HQ_3A;
            break;

        case INS_sve_bfcvt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3A;
            break;

        case INS_sve_fcvt:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3B;
            break;

        case INS_sve_fcvtx:
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HO_3C;
            break;

        case INS_sve_fcvtzs:
        case INS_sve_fcvtzu:
            assert(insOptsScalableFloat(opt) || opt == INS_OPTS_H_TO_S || opt == INS_OPTS_H_TO_D ||
                   opt == INS_OPTS_S_TO_D || opt == INS_OPTS_D_TO_S);
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HP_3B;
            break;

        case INS_sve_scvtf:
        case INS_sve_ucvtf:
            assert(insOptsScalableAtLeastHalf(opt) || opt == INS_OPTS_S_TO_H || opt == INS_OPTS_S_TO_D ||
                   opt == INS_OPTS_D_TO_H || opt == INS_OPTS_D_TO_S);
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            fmt = IF_SVE_HS_3A;
            break;

        case INS_sve_frecpx:
        case INS_sve_fsqrt:
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(insOptsScalableFloat(opt));
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_HR_3A;
            break;

        case INS_sve_whilege:
        case INS_sve_whilegt:
        case INS_sve_whilelt:
        case INS_sve_whilele:
        case INS_sve_whilehs:
        case INS_sve_whilehi:
        case INS_sve_whilelo:
        case INS_sve_whilels:
            assert(isGeneralRegister(reg2));                       // nnnnn
            assert(isGeneralRegister(reg3));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            assert(insOptsScalableStandard(opt));

            if (insScalableOptsNone(sopt))
            {
                assert(isPredicateRegister(reg1));    // DDDD
                assert(isValidGeneralDatasize(size)); // X
                fmt = IF_SVE_DT_3A;
            }
            else if (insScalableOptsWithPredicatePair(sopt))
            {
                assert(isLowPredicateRegister(reg1)); // DDD
                assert(size == EA_8BYTE);
                fmt = IF_SVE_DX_3A;
            }
            else
            {
                assert(insScalableOptsWithVectorLength(sopt)); // l
                assert(isHighPredicateRegister(reg1));         // DDD
                assert(size == EA_8BYTE);
                vectorLength4x = (sopt == INS_SCALABLE_OPTS_VL_4X);
                fmt            = IF_SVE_DY_3A;
            }
            break;

        case INS_sve_whilewr:
        case INS_sve_whilerw:
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isGeneralRegister(reg2));   // nnnnn
            assert(size == EA_8BYTE);
            assert(isGeneralRegister(reg3));                       // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_DU_3A;
            break;

        case INS_sve_movs:
            assert(insOptsScalable(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // NNNN
            fmt = IF_SVE_CZ_4A;
            break;

        case INS_sve_adclb:
        case INS_sve_adclt:
        case INS_sve_sbclb:
        case INS_sve_sbclt:
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // x
            fmt = IF_SVE_FY_3A;
            break;

        case INS_sve_mlapt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            fmt = IF_SVE_EW_3A;
            break;

        case INS_sve_madpt:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsNone(opt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // mmmmm
            assert(isVectorRegister(reg3)); // aaaaa
            fmt = IF_SVE_EW_3B;
            break;

        case INS_sve_fcmeq:
        case INS_sve_fcmge:
        case INS_sve_fcmgt:
        case INS_sve_fcmlt:
        case INS_sve_fcmle:
        case INS_sve_fcmne:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isPredicateRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HI_3A;
            break;

        case INS_sve_flogb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_HP_3A;
            break;

        case INS_sve_ld1b:
        case INS_sve_ld1h:
        case INS_sve_ld1w:
        case INS_sve_ld1d:
            return emitIns_R_R_R_I(ins, size, reg1, reg2, reg3, 0, opt);

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);

    if (pmerge)
    {
        id->idPredicateReg2Merge(pmerge);
    }
    else if (vectorLength4x)
    {
        id->idVectorLength4x(vectorLength4x);
    }

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers and a constant.
 *  Do not call this directly. Use 'emitIns_R_R_R_I' instead.
 */

void emitter::emitInsSve_R_R_R_I(instruction     ins,
                                 emitAttr        attr,
                                 regNumber       reg1,
                                 regNumber       reg2,
                                 regNumber       reg3,
                                 ssize_t         imm,
                                 insOpts         opt /* = INS_OPTS_NONE */,
                                 insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size     = EA_SIZE(attr);
    emitAttr  elemsize = EA_UNKNOWN;
    insFormat fmt      = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_adr:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm
            assert(isValidUimm<2>(imm));
            switch (opt)
            {
                case INS_OPTS_SCALABLE_S:
                case INS_OPTS_SCALABLE_D:
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    fmt = IF_SVE_BH_3A;
                    break;
                case INS_OPTS_SCALABLE_D_SXTW:
                    fmt = IF_SVE_BH_3B;
                    break;
                case INS_OPTS_SCALABLE_D_UXTW:
                    fmt = IF_SVE_BH_3B_A;
                    break;
                default:
                    assert(!"invalid instruction");
                    break;
            }
            break;

        case INS_sve_cmpeq:
        case INS_sve_cmpgt:
        case INS_sve_cmpge:
        case INS_sve_cmpne:
        case INS_sve_cmple:
        case INS_sve_cmplt:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isValidSimm<5>(imm));          // iiiii
            fmt = IF_SVE_CY_3A;
            break;

        case INS_sve_cmphi:
        case INS_sve_cmphs:
        case INS_sve_cmplo:
        case INS_sve_cmpls:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isValidUimm<7>(imm));          // iiiii
            fmt = IF_SVE_CY_3B;
            break;

        case INS_sve_sdot:
        case INS_sve_udot:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_B)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_EY_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_EG_3A;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isValidUimm<1>(imm)); // i
                opt = INS_OPTS_SCALABLE_H;
                fmt = IF_SVE_EY_3B;
            }
            break;

        case INS_sve_usdot:
        case INS_sve_sudot:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii
            fmt = IF_SVE_EZ_3A;
            break;

        case INS_sve_mul:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            switch (opt)
            {
                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));                  // iii
                    assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                    fmt = IF_SVE_FD_3A;
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));                  // ii
                    assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                    fmt = IF_SVE_FD_3B;
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm)); // i
                    fmt = IF_SVE_FD_3C;
                    break;

                default:
                    unreached();
                    break;
            }
            break;

        case INS_sve_cdot:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidRot(imm));                               // rr
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation
            imm = emitEncodeRotationImm0_to_270(imm);
            fmt = IF_SVE_EJ_3A;
            break;

        case INS_sve_cmla:
        case INS_sve_sqrdcmlah:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isVectorRegister(reg2));                        // nnnnn
            assert(isVectorRegister(reg3));                        // mmmmm
            assert(isValidRot(imm));                               // rr
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx

            // Convert rot to bitwise representation
            imm = emitEncodeRotationImm0_to_270(imm);
            fmt = IF_SVE_EK_3A;
            break;

        case INS_sve_ld1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalable(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_IH_3A_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_IH_3A;
                }
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 8>(imm)));
                fmt = IF_SVE_IV_3A;
            }
            break;

        case INS_sve_ldff1d:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 8>(imm)));
            fmt = IF_SVE_IV_3A;
            break;

        case INS_sve_ld1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWordsOrQuadwords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IH_3A_F;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 4>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ld1sw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 4>(imm)));
                fmt = IF_SVE_IV_3A;
            }
            break;

        case INS_sve_ldff1sw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 4>(imm)));
            fmt = IF_SVE_IV_3A;
            break;

        case INS_sve_ld1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg3));
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_D;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isValidUimm<5>(imm));
                fmt = IF_SVE_HX_3A_B;
            }
            break;

        case INS_sve_ld1b:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_E;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isValidUimm<5>(imm));
                fmt = IF_SVE_HX_3A_B;
            }
            break;

        case INS_sve_ldff1b:
        case INS_sve_ldff1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isValidUimm<5>(imm));
            fmt = IF_SVE_HX_3A_B;
            break;

        case INS_sve_ld1sh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_F;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 2>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ld1h:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                fmt = IF_SVE_IJ_3A_G;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert((isValidUimm_MultipleOf<5, 2>(imm)));
                fmt = IF_SVE_HX_3A_E;
            }
            break;

        case INS_sve_ldff1h:
        case INS_sve_ldff1sh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 2>(imm)));
            fmt = IF_SVE_HX_3A_E;
            break;

        case INS_sve_ldff1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert((isValidUimm_MultipleOf<5, 4>(imm)));
            fmt = IF_SVE_HX_3A_E;
            break;

        case INS_sve_ldnf1sw:
        case INS_sve_ldnf1d:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A;
            break;

        case INS_sve_ldnf1sh:
        case INS_sve_ldnf1w:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_A;
            break;

        case INS_sve_ldnf1h:
        case INS_sve_ldnf1sb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_B;
            break;

        case INS_sve_ldnf1b:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));
            fmt = IF_SVE_IL_3A_C;
            break;

        case INS_sve_ldnt1b:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ldnt1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ldnt1b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ldnt1h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ldnt1w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ldnt1d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IM_3A;
            break;

        case INS_sve_ld1rqb:
        case INS_sve_ld1rob:
        case INS_sve_ld1rqh:
        case INS_sve_ld1roh:
        case INS_sve_ld1rqw:
        case INS_sve_ld1row:
        case INS_sve_ld1rqd:
        case INS_sve_ld1rod:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld1rqb:
                case INS_sve_ld1rqd:
                case INS_sve_ld1rqh:
                case INS_sve_ld1rqw:
                    assert((isValidSimm_MultipleOf<4, 16>(imm)));
                    break;

                case INS_sve_ld1rob:
                case INS_sve_ld1rod:
                case INS_sve_ld1roh:
                case INS_sve_ld1row:
                    assert((isValidSimm_MultipleOf<4, 32>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_ld1rqb:
                case INS_sve_ld1rob:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ld1rqh:
                case INS_sve_ld1roh:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ld1rqw:
                case INS_sve_ld1row:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ld1rqd:
                case INS_sve_ld1rod:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IO_3A;
            break;

        case INS_sve_ld2q:
        case INS_sve_ld3q:
        case INS_sve_ld4q:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2q:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_ld3q:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_ld4q:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IQ_3A;
            break;

        case INS_sve_ld2b:
        case INS_sve_ld3b:
        case INS_sve_ld4b:
        case INS_sve_ld2h:
        case INS_sve_ld3h:
        case INS_sve_ld4h:
        case INS_sve_ld2w:
        case INS_sve_ld3w:
        case INS_sve_ld4w:
        case INS_sve_ld2d:
        case INS_sve_ld3d:
        case INS_sve_ld4d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld2h:
                case INS_sve_ld2w:
                case INS_sve_ld2d:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_ld3b:
                case INS_sve_ld3h:
                case INS_sve_ld3w:
                case INS_sve_ld3d:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_ld4b:
                case INS_sve_ld4h:
                case INS_sve_ld4w:
                case INS_sve_ld4d:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld3b:
                case INS_sve_ld4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_ld2h:
                case INS_sve_ld3h:
                case INS_sve_ld4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_ld2w:
                case INS_sve_ld3w:
                case INS_sve_ld4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_ld2d:
                case INS_sve_ld3d:
                case INS_sve_ld4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IS_3A;
            break;

        case INS_sve_st2q:
        case INS_sve_st3q:
        case INS_sve_st4q:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2q:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_st3q:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_st4q:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JE_3A;
            break;

        case INS_sve_stnt1b:
        case INS_sve_stnt1h:
        case INS_sve_stnt1w:
        case INS_sve_stnt1d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidSimm<4>(imm));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_stnt1b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_stnt1h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_stnt1w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_stnt1d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JM_3A;
            break;

        case INS_sve_st1w:
        case INS_sve_st1d:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));

                if (opt == INS_OPTS_SCALABLE_Q && (ins == INS_sve_st1d))
                {
                    fmt = IF_SVE_JN_3C_D;
                }
                else
                {
                    if ((ins == INS_sve_st1w) && insOptsScalableWords(opt))
                    {
                        fmt = IF_SVE_JN_3B;
                    }
                    else
                    {
#if DEBUG
                        if (ins == INS_sve_st1w)
                        {
                            assert(opt == INS_OPTS_SCALABLE_Q);
                        }
                        else
                        {
                            assert(opt == INS_OPTS_SCALABLE_D);
                        }
#endif // DEBUG
                        fmt = IF_SVE_JN_3C;
                    }
                }
            }
            else
            {
                assert(isVectorRegister(reg3));
                if ((ins == INS_sve_st1w) && insOptsScalableWords(opt))
                {
                    assert((isValidUimm_MultipleOf<5, 4>(imm)));
                    fmt = IF_SVE_JI_3A_A;
                }
                else
                {
                    assert(ins == INS_sve_st1d);
                    assert((isValidUimm_MultipleOf<5, 8>(imm)));
                    fmt = IF_SVE_JL_3A;
                }
            }
            break;

        case INS_sve_st2b:
        case INS_sve_st3b:
        case INS_sve_st4b:
        case INS_sve_st2h:
        case INS_sve_st3h:
        case INS_sve_st4h:
        case INS_sve_st2w:
        case INS_sve_st3w:
        case INS_sve_st4w:
        case INS_sve_st2d:
        case INS_sve_st3d:
        case INS_sve_st4d:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st2h:
                case INS_sve_st2w:
                case INS_sve_st2d:
                    assert((isValidSimm_MultipleOf<4, 2>(imm)));
                    break;

                case INS_sve_st3b:
                case INS_sve_st3h:
                case INS_sve_st3w:
                case INS_sve_st3d:
                    assert((isValidSimm_MultipleOf<4, 3>(imm)));
                    break;

                case INS_sve_st4b:
                case INS_sve_st4h:
                case INS_sve_st4w:
                case INS_sve_st4d:
                    assert((isValidSimm_MultipleOf<4, 4>(imm)));
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }

            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st3b:
                case INS_sve_st4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    break;

                case INS_sve_st2h:
                case INS_sve_st3h:
                case INS_sve_st4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    break;

                case INS_sve_st2w:
                case INS_sve_st3w:
                case INS_sve_st4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    break;

                case INS_sve_st2d:
                case INS_sve_st3d:
                case INS_sve_st4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_JO_3A;
            break;

        case INS_sve_st1b:
        case INS_sve_st1h:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));

            if (isGeneralRegister(reg3))
            {
                assert(isValidSimm<4>(imm));
                // st1h is reserved for scalable B
                assert((ins == INS_sve_st1h) ? insOptsScalableAtLeastHalf(opt) : insOptsScalableStandard(opt));
                fmt = IF_SVE_JN_3A;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_st1b:
                        assert(isValidUimm<5>(imm));
                        break;

                    case INS_sve_st1h:
                        assert((isValidUimm_MultipleOf<5, 2>(imm)));
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG

                fmt = IF_SVE_JI_3A_A;
            }
            break;

        case INS_sve_fmla:
        case INS_sve_fmls:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_GU_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GU_3B;
            }
            break;

        case INS_sve_bfmla:
        case INS_sve_bfmls:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // i ii
            fmt = IF_SVE_GU_3C;
            break;

        case INS_sve_fmul:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_GX_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GX_3B;
            }
            break;

        case INS_sve_bfmul:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // i ii
            fmt = IF_SVE_GX_3C;
            break;

        case INS_sve_fdot:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii

            if (opt == INS_OPTS_SCALABLE_B)
            {
                unreached();                 // TODO-SVE: Not yet supported.
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_GY_3B_D;
            }
            else if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_GY_3B;
            }
            else
            {
                unreached(); // TODO-SVE: Not yet supported.
                assert(insOptsNone(opt));
                assert(isValidUimm<3>(imm)); // i ii

                // Simplify emitDispInsHelp logic by setting insOpt
                opt = INS_OPTS_SCALABLE_B;
                fmt = IF_SVE_GY_3A;
            }
            break;

        case INS_sve_bfdot:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<2>(imm)); // ii
            fmt = IF_SVE_GY_3B;
            break;

        case INS_sve_mla:
        case INS_sve_mls:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // i ii
                fmt = IF_SVE_FF_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FF_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FF_3C;
            }
            break;

        case INS_sve_smullb:
        case INS_sve_smullt:
        case INS_sve_umullb:
        case INS_sve_umullt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FE_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FE_3B;
            }
            break;

        case INS_sve_smlalb:
        case INS_sve_smlalt:
        case INS_sve_umlalb:
        case INS_sve_umlalt:
        case INS_sve_smlslb:
        case INS_sve_smlslt:
        case INS_sve_umlslb:
        case INS_sve_umlslt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FG_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FG_3B;
            }
            break;

        case INS_sve_sqdmullb:
        case INS_sve_sqdmullt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FH_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // i i
                fmt = IF_SVE_FH_3B;
            }
            break;

        case INS_sve_sqdmulh:
        case INS_sve_sqrdmulh:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FI_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FI_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FI_3C;
            }
            break;

        case INS_sve_sqdmlalb:
        case INS_sve_sqdmlalt:
        case INS_sve_sqdmlslb:
        case INS_sve_sqdmlslt:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // ii i
                fmt = IF_SVE_FJ_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<2>(imm)); // ii
                fmt = IF_SVE_FJ_3B;
            }
            break;

        case INS_sve_sqrdmlah:
        case INS_sve_sqrdmlsh:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<3>(imm));                  // i ii
                fmt = IF_SVE_FK_3A;
            }
            else if (opt == INS_OPTS_SCALABLE_S)
            {
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                assert(isValidUimm<2>(imm));                  // ii
                fmt = IF_SVE_FK_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_FK_3C;
            }
            break;

        case INS_sve_fcadd:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isScalableVectorSize(size));
            imm = emitEncodeRotationImm90_or_270(imm);
            fmt = IF_SVE_GP_3A;
            break;

        case INS_sve_ld1rd:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 8>(imm)));
            fmt = IF_SVE_IC_3A;
            break;

        case INS_sve_ld1rsw:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_D);
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 4>(imm)));
            fmt = IF_SVE_IC_3A;
            break;

        case INS_sve_ld1rsh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 2>(imm)));
            fmt = IF_SVE_IC_3A_A;
            break;

        case INS_sve_ld1rw:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 4>(imm)));
            fmt = IF_SVE_IC_3A_A;
            break;

        case INS_sve_ld1rh:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert((isValidUimm_MultipleOf<6, 2>(imm)));
            fmt = IF_SVE_IC_3A_B;
            break;

        case INS_sve_ld1rsb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidUimm<6>(imm));
            fmt = IF_SVE_IC_3A_B;
            break;

        case INS_sve_ld1rb:
            assert(insScalableOptsNone(sopt));
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isValidUimm<6>(imm));
            fmt = IF_SVE_IC_3A_C;
            break;

        case INS_sve_fmlalb:
        case INS_sve_fmlalt:
        case INS_sve_fmlslb:
        case INS_sve_fmlslt:
        case INS_sve_bfmlalb:
        case INS_sve_bfmlalt:
        case INS_sve_bfmlslb:
        case INS_sve_bfmlslt:
            assert(insScalableOptsNone(sopt));
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmm
            assert((REG_V0 <= reg3) && (reg3 <= REG_V7));
            assert(isValidUimm<3>(imm)); // ii i
            fmt = IF_SVE_GZ_3A;
            break;

        case INS_sve_luti2:
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<3>(imm)); // iii
                fmt = IF_SVE_GG_3B;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isValidUimm<2>(imm)); // ii i
                fmt = IF_SVE_GG_3A;
            }
            unreached();
            break;

        case INS_sve_luti4:
            assert(isVectorRegister(reg1)); // ddddd
            assert(isVectorRegister(reg2)); // nnnnn
            assert(isVectorRegister(reg3)); // mmmmm

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm));

                if (sopt == INS_SCALABLE_OPTS_WITH_VECTOR_PAIR)
                {
                    fmt = IF_SVE_GH_3B;
                }
                else
                {
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_GH_3B_B;
                }
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(insScalableOptsNone(sopt));
                assert(isValidUimm<1>(imm)); // i
                fmt = IF_SVE_GH_3A;
            }
            unreached();
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

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
 *  Add a SVE instruction referencing three registers and two constants.
 */

void emitter::emitInsSve_R_R_R_I_I(instruction ins,
                                   emitAttr    attr,
                                   regNumber   reg1,
                                   regNumber   reg2,
                                   regNumber   reg3,
                                   ssize_t     imm1,
                                   ssize_t     imm2,
                                   insOpts     opt)
{
    insFormat fmt = IF_NONE;
    ssize_t   imm;

    switch (ins)
    {
        case INS_sve_cdot:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_B)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FA_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_H);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FA_3B;
            }
            break;

        case INS_sve_cmla:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FB_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FB_3B;
            }
            break;

        case INS_sve_sqrdcmlah:
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidRot(imm2));          // rr
            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);

            if (opt == INS_OPTS_SCALABLE_H)
            {
                assert(isValidUimm<2>(imm1));                 // ii
                assert((REG_V0 <= reg3) && (reg3 <= REG_V7)); // mmm
                fmt = IF_SVE_FC_3A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_S);
                assert(isValidUimm<1>(imm1)); // i
                fmt = IF_SVE_FC_3B;
            }
            break;

        case INS_sve_fcmla:
            assert(opt == INS_OPTS_SCALABLE_S);
            assert(isVectorRegister(reg1));    // ddddd
            assert(isVectorRegister(reg2));    // nnnnn
            assert(isLowVectorRegister(reg3)); // mmmm
            assert(isValidUimm<1>(imm1));      // i
            assert(isValidRot(imm2));          // rr

            // Convert imm2 from rotation value (0-270) to bitwise representation (0-3)
            imm = (imm1 << 2) | emitEncodeRotationImm0_to_270(imm2);
            fmt = IF_SVE_GV_3A;
            break;

        default:
            unreached();
            break;
    }

    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);
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
 *  Add a SVE instruction referencing four registers.
 *  Do not call this directly. Use 'emitIns_R_R_R_R' instead.
 */

void emitter::emitInsSve_R_R_R_R(instruction     ins,
                                 emitAttr        attr,
                                 regNumber       reg1,
                                 regNumber       reg2,
                                 regNumber       reg3,
                                 regNumber       reg4,
                                 insOpts         opt /* = INS_OPTS_NONE*/,
                                 insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_sel:
            if (sopt == INS_SCALABLE_OPTS_UNPREDICATED)
            {
                if (reg1 == reg4)
                {
                    // mov is a preferred alias for sel
                    return emitInsSve_R_R_R(INS_sve_mov, attr, reg1, reg2, reg3, opt,
                                            INS_SCALABLE_OPTS_PREDICATE_MERGE);
                }

                assert(insOptsScalableStandard(opt));
                assert(isVectorRegister(reg1));    // ddddd
                assert(isPredicateRegister(reg2)); // VVVV
                assert(isVectorRegister(reg3));    // nnnnn
                assert(isVectorRegister(reg4));    // mmmmm
                fmt = IF_SVE_CW_4A;
            }
            else
            {
                assert(opt == INS_OPTS_SCALABLE_B);
                assert(isPredicateRegister(reg1)); // dddd
                assert(isPredicateRegister(reg2)); // gggg
                assert(isPredicateRegister(reg3)); // nnnn
                assert(isPredicateRegister(reg4)); // mmmm
                fmt = IF_SVE_CZ_4A;
            }
            break;

        case INS_sve_cmpeq:
        case INS_sve_cmpgt:
        case INS_sve_cmpge:
        case INS_sve_cmphi:
        case INS_sve_cmphs:
        case INS_sve_cmpne:
        case INS_sve_cmple:
        case INS_sve_cmplo:
        case INS_sve_cmpls:
        case INS_sve_cmplt:
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(attr));   // xx
            if (sopt == INS_SCALABLE_OPTS_WIDE)
            {
                assert(insOptsScalableWide(opt));
                fmt = IF_SVE_CX_4A_A;
            }
            else
            {
                assert(insScalableOptsNone(sopt));
                assert(insOptsScalableStandard(opt));
                fmt = IF_SVE_CX_4A;
            }
            break;

        case INS_sve_and:
        case INS_sve_orr:
        case INS_sve_eor:
        case INS_sve_ands:
        case INS_sve_bic:
        case INS_sve_orn:
        case INS_sve_bics:
        case INS_sve_eors:
        case INS_sve_nor:
        case INS_sve_nand:
        case INS_sve_orrs:
        case INS_sve_orns:
        case INS_sve_nors:
        case INS_sve_nands:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // nnnn
            assert(isPredicateRegister(reg4)); // mmmm
            fmt = IF_SVE_CZ_4A;
            break;

        case INS_sve_brkpa:
        case INS_sve_brkpb:
        case INS_sve_brkpas:
        case INS_sve_brkpbs:
            assert(opt == INS_OPTS_SCALABLE_B);
            assert(isPredicateRegister(reg1)); // dddd
            assert(isPredicateRegister(reg2)); // gggg
            assert(isPredicateRegister(reg3)); // nnnn
            assert(isPredicateRegister(reg4)); // mmmm
            fmt = IF_SVE_DA_4A;
            break;

        case INS_sve_fcmeq:
        case INS_sve_fcmge:
        case INS_sve_facge:
        case INS_sve_fcmgt:
        case INS_sve_facgt:
        case INS_sve_fcmlt:
        case INS_sve_fcmle:
        case INS_sve_fcmne:
        case INS_sve_fcmuo:
        case INS_sve_facle:
        case INS_sve_faclt:
            assert(insOptsScalableFloat(opt));
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isScalableVectorSize(attr));   // xx
            fmt = IF_SVE_HT_4A;
            break;

        case INS_sve_match:
        case INS_sve_nmatch:
            assert(insOptsScalableAtMaxHalf(opt));
            assert(isPredicateRegister(reg1));    // DDDD
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(attr));   // xx
            fmt = IF_SVE_GE_4A;
            break;

        case INS_sve_mla:
        case INS_sve_mls:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // nnnnn
            assert(isVectorRegister(reg4));       // mmmmm
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_AR_4A;
            break;

        case INS_sve_histcnt:
            assert(insOptsScalableWords(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isLowPredicateRegister(reg2));                  // ggg
            assert(isVectorRegister(reg3));                        // nnnnn
            assert(isVectorRegister(reg4));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_GI_4A;
            break;

        case INS_sve_fmla:
        case INS_sve_fmls:
        case INS_sve_fnmla:
        case INS_sve_fnmls:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));                        // ddddd
            assert(isLowPredicateRegister(reg2));                  // ggg
            assert(isVectorRegister(reg3));                        // nnnnn
            assert(isVectorRegister(reg4));                        // mmmmm
            assert(isValidVectorElemsize(optGetSveElemsize(opt))); // xx
            fmt = IF_SVE_HU_4A;
            break;

        case INS_sve_mad:
        case INS_sve_msb:
            assert(insOptsScalableStandard(opt));
            assert(isVectorRegister(reg1));       // ddddd
            assert(isLowPredicateRegister(reg2)); // ggg
            assert(isVectorRegister(reg3));       // mmmmm
            assert(isVectorRegister(reg4));       // aaaaa
            assert(isScalableVectorSize(size));
            fmt = IF_SVE_AS_4A;
            break;

        case INS_sve_st1b:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));

            if (insOptsScalableStandard(opt))
            {
                if (isGeneralRegister(reg4))
                {
                    fmt = IF_SVE_JD_4A;
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    fmt = IF_SVE_JK_4B;
                }
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        fmt = IF_SVE_JK_4A_B;
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        fmt = IF_SVE_JK_4A;
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1h:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (insOptsScalableStandard(opt))
            {
                if (sopt == INS_SCALABLE_OPTS_LSL_N)
                {
                    if (isGeneralRegister(reg4))
                    {
                        // st1h is reserved for scalable B
                        assert((ins == INS_sve_st1h) ? insOptsScalableAtLeastHalf(opt) : true);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        fmt = IF_SVE_JD_4A;
                    }
                    else
                    {
                        assert(isVectorRegister(reg4));
                        fmt = IF_SVE_JJ_4B;
                    }
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_JJ_4B_E;
                }
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_D;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A;
                        }
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_C;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A_B;
                        }
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1w:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (insOptsScalableStandard(opt))
            {
                if (sopt == INS_SCALABLE_OPTS_LSL_N)
                {
                    if (isGeneralRegister(reg4))
                    {
                        fmt = IF_SVE_JD_4B;
                    }
                    else
                    {
                        assert(isVectorRegister(reg4));
                        fmt = IF_SVE_JJ_4B;
                    }
                }
                else
                {
                    assert(isVectorRegister(reg4));
                    assert(insScalableOptsNone(sopt));
                    fmt = IF_SVE_JJ_4B_E;
                }
            }
            else if (opt == INS_OPTS_SCALABLE_Q)
            {
                assert(isGeneralRegister(reg4));
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                fmt = IF_SVE_JD_4C;
            }
            else
            {
                assert(insOptsScalable32bitExtends(opt));
                assert(isVectorRegister(reg4));
                switch (opt)
                {
                    case INS_OPTS_SCALABLE_S_UXTW:
                    case INS_OPTS_SCALABLE_S_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_D;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A;
                        }
                        break;

                    case INS_OPTS_SCALABLE_D_UXTW:
                    case INS_OPTS_SCALABLE_D_SXTW:
                        if (insScalableOptsNone(sopt))
                        {
                            fmt = IF_SVE_JJ_4A_C;
                        }
                        else
                        {
                            assert(sopt == INS_SCALABLE_OPTS_MOD_N);
                            fmt = IF_SVE_JJ_4A_B;
                        }
                        break;

                    default:
                        assert(!"Invalid options for scalable");
                        break;
                }
            }
            break;

        case INS_sve_st1d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    fmt = IF_SVE_JD_4C_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_JD_4C;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (opt == INS_OPTS_SCALABLE_D)
                {
                    if (sopt == INS_SCALABLE_OPTS_LSL_N)
                    {
                        fmt = IF_SVE_JJ_4B;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_JJ_4B_C;
                    }
                }
                else
                {
                    assert(insOptsScalable32bitExtends(opt));
                    switch (opt)
                    {
                        case INS_OPTS_SCALABLE_D_UXTW:
                        case INS_OPTS_SCALABLE_D_SXTW:
                            if (sopt == INS_SCALABLE_OPTS_MOD_N)
                            {
                                fmt = IF_SVE_JJ_4A;
                            }
                            else
                            {
                                assert(insScalableOptsNone(sopt));
                                fmt = IF_SVE_JJ_4A_B;
                            }
                            break;

                        default:
                            assert(!"Invalid options for scalable");
                            break;
                    }
                }
            }
            break;

        case INS_sve_ld1b:
        case INS_sve_ld1sb:
        case INS_sve_ldff1b:
        case INS_sve_ldff1sb:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));
            assert(insScalableOptsNone(sopt));

            if (isGeneralRegisterOrZR(reg4))
            {
                switch (ins)
                {
                    case INS_sve_ldff1b:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IG_4A_E;
                        break;

                    case INS_sve_ldff1sb:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IG_4A_D;
                        break;

                    case INS_sve_ld1sb:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IK_4A_F;
                        break;

                    case INS_sve_ld1b:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IK_4A_H;
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (insOptsScalableDoubleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HW_4A;
                }
                else if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HW_4A_A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_HW_4B;
                }
            }
            break;

        case INS_sve_ld1h:
        case INS_sve_ld1sh:
        case INS_sve_ldff1h:
        case INS_sve_ldff1sh:
        case INS_sve_ld1w:
        case INS_sve_ldff1w:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegisterOrZR(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);

                switch (ins)
                {
                    case INS_sve_ldff1h:
                        assert(insOptsScalableStandard(opt));
                        fmt = IF_SVE_IG_4A_G;
                        break;

                    case INS_sve_ldff1sh:
                    case INS_sve_ldff1w:
                        assert(insOptsScalableWords(opt));
                        fmt = IF_SVE_IG_4A_F;
                        break;

                    case INS_sve_ld1w:
                        assert(insOptsScalableWordsOrQuadwords(opt));
                        fmt = IF_SVE_II_4A_H;
                        break;

                    case INS_sve_ld1sh:
                        assert(insOptsScalableWords(opt));
                        fmt = IF_SVE_IK_4A_G;
                        break;

                    case INS_sve_ld1h:
                        assert(insOptsScalableAtLeastHalf(opt));
                        fmt = IF_SVE_IK_4A_I;
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
            }
            else
            {
                assert(isVectorRegister(reg4));

                if (insOptsScalableDoubleWord32bitExtends(opt))
                {
                    if (sopt == INS_SCALABLE_OPTS_MOD_N)
                    {
                        fmt = IF_SVE_HW_4A_A;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4A_B;
                    }
                }
                else if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    if (sopt == INS_SCALABLE_OPTS_MOD_N)
                    {
                        fmt = IF_SVE_HW_4A;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4A_C;
                    }
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    if (sopt == INS_SCALABLE_OPTS_LSL_N)
                    {
                        fmt = IF_SVE_HW_4B;
                    }
                    else
                    {
                        assert(insScalableOptsNone(sopt));
                        fmt = IF_SVE_HW_4B_D;
                    }
                }
            }
            break;

        case INS_sve_ld1d:
        case INS_sve_ld1sw:
        case INS_sve_ldff1d:
        case INS_sve_ldff1sw:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isScalableVectorSize(size));

            if (isGeneralRegisterOrZR(reg4))
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);

                if (opt == INS_OPTS_SCALABLE_Q)
                {
                    assert(reg4 != REG_ZR);
                    assert(ins == INS_sve_ld1d);
                    fmt = IF_SVE_II_4A_B;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);

                    switch (ins)
                    {
                        case INS_sve_ldff1d:
                        case INS_sve_ldff1sw:
                            fmt = IF_SVE_IG_4A;
                            break;

                        case INS_sve_ld1d:
                            assert(reg4 != REG_ZR);
                            fmt = IF_SVE_II_4A;
                            break;

                        case INS_sve_ld1sw:
                            assert(reg4 != REG_ZR);
                            fmt = IF_SVE_IK_4A;
                            break;

                        default:
                            assert(!"Invalid instruction");
                            break;
                    }
                }
            }
            else if (insOptsScalableDoubleWord32bitExtends(opt))
            {
                assert(isVectorRegister(reg4));

                if (sopt == INS_SCALABLE_OPTS_MOD_N)
                {
                    fmt = IF_SVE_IU_4A;
                }
                else
                {
                    assert(insScalableOptsNone(sopt));

                    if (ins == INS_sve_ld1d)
                    {
                        fmt = IF_SVE_IU_4A_C;
                    }
                    else
                    {
                        fmt = IF_SVE_IU_4A_A;
                    }
                }
            }
            else if (sopt == INS_SCALABLE_OPTS_LSL_N)
            {
                assert(isVectorRegister(reg4));
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_IU_4B;
            }
            else
            {
                assert(isVectorRegister(reg4));
                assert(opt == INS_OPTS_SCALABLE_D);
                assert(insScalableOptsNone(sopt));

                if (ins == INS_sve_ld1d)
                {
                    fmt = IF_SVE_IU_4B_D;
                }
                else
                {
                    fmt = IF_SVE_IU_4B_B;
                }
            }
            break;

        case INS_sve_ldnt1b:
        case INS_sve_ldnt1h:
        case INS_sve_ldnt1w:
        case INS_sve_ldnt1d:
        case INS_sve_ldnt1sb:
        case INS_sve_ldnt1sh:
        case INS_sve_ldnt1sw:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg4));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_ldnt1b:
                        assert(opt == INS_OPTS_SCALABLE_B);
                        assert(insScalableOptsNone(sopt));
                        break;

                    case INS_sve_ldnt1h:
                        assert(opt == INS_OPTS_SCALABLE_H);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_ldnt1w:
                        assert(opt == INS_OPTS_SCALABLE_S);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_ldnt1d:
                        assert(opt == INS_OPTS_SCALABLE_D);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG

                fmt = IF_SVE_IN_4A;
            }
            else if ((ins == INS_sve_ldnt1d) || (ins == INS_sve_ldnt1sw))
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(insScalableOptsNone(sopt));
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_IX_4A;
            }
            else
            {
                assert(insOptsScalableWords(opt));
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(insScalableOptsNone(sopt));

                if (opt == INS_OPTS_SCALABLE_S)
                {
                    fmt = IF_SVE_IF_4A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_IF_4A_A;
                }
            }
            break;

        case INS_sve_ld1rob:
        case INS_sve_ld1roh:
        case INS_sve_ld1row:
        case INS_sve_ld1rod:
        case INS_sve_ld1rqb:
        case INS_sve_ld1rqh:
        case INS_sve_ld1rqw:
        case INS_sve_ld1rqd:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld1rob:
                case INS_sve_ld1rqb:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_ld1roh:
                case INS_sve_ld1rqh:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld1row:
                case INS_sve_ld1rqw:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld1rod:
                case INS_sve_ld1rqd:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IP_4A;
            break;

        case INS_sve_ld1q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isGeneralRegisterOrZR(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_IW_4A;
            break;

        case INS_sve_ld2q:
        case INS_sve_ld3q:
        case INS_sve_ld4q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(sopt == INS_SCALABLE_OPTS_LSL_N);
            fmt = IF_SVE_IR_4A;
            break;

        case INS_sve_ld2b:
        case INS_sve_ld3b:
        case INS_sve_ld4b:
        case INS_sve_ld2h:
        case INS_sve_ld3h:
        case INS_sve_ld4h:
        case INS_sve_ld2w:
        case INS_sve_ld3w:
        case INS_sve_ld4w:
        case INS_sve_ld2d:
        case INS_sve_ld3d:
        case INS_sve_ld4d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_ld2b:
                case INS_sve_ld3b:
                case INS_sve_ld4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_ld2h:
                case INS_sve_ld3h:
                case INS_sve_ld4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld2w:
                case INS_sve_ld3w:
                case INS_sve_ld4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_ld2d:
                case INS_sve_ld3d:
                case INS_sve_ld4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG

            fmt = IF_SVE_IT_4A;
            break;

        case INS_sve_st1q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isGeneralRegisterOrZR(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            assert(insScalableOptsNone(sopt));
            fmt = IF_SVE_IY_4A;
            break;

        case INS_sve_stnt1b:
        case INS_sve_stnt1h:
        case INS_sve_stnt1w:
        case INS_sve_stnt1d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isScalableVectorSize(size));

            if (isGeneralRegister(reg3))
            {
                assert(isGeneralRegister(reg4));
#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_stnt1b:
                        assert(opt == INS_OPTS_SCALABLE_B);
                        assert(insScalableOptsNone(sopt));
                        break;

                    case INS_sve_stnt1h:
                        assert(opt == INS_OPTS_SCALABLE_H);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_stnt1w:
                        assert(opt == INS_OPTS_SCALABLE_S);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    case INS_sve_stnt1d:
                        assert(opt == INS_OPTS_SCALABLE_D);
                        assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG
                fmt = IF_SVE_JB_4A;
            }
            else
            {
                assert(isVectorRegister(reg3));
                assert(isGeneralRegisterOrZR(reg4));
                assert(isScalableVectorSize(size));
                assert(insScalableOptsNone(sopt));

                if (opt == INS_OPTS_SCALABLE_S)
                {
                    fmt = IF_SVE_IZ_4A;
                }
                else
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    if (ins == INS_sve_stnt1d)
                    {
                        fmt = IF_SVE_JA_4A;
                    }
                    else
                    {
                        fmt = IF_SVE_IZ_4A_A;
                    }
                }
            }
            break;

        case INS_sve_st2b:
        case INS_sve_st3b:
        case INS_sve_st4b:
        case INS_sve_st2h:
        case INS_sve_st3h:
        case INS_sve_st4h:
        case INS_sve_st2w:
        case INS_sve_st3w:
        case INS_sve_st4w:
        case INS_sve_st2d:
        case INS_sve_st3d:
        case INS_sve_st4d:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));

#ifdef DEBUG
            switch (ins)
            {
                case INS_sve_st2b:
                case INS_sve_st3b:
                case INS_sve_st4b:
                    assert(opt == INS_OPTS_SCALABLE_B);
                    assert(insScalableOptsNone(sopt));
                    break;

                case INS_sve_st2h:
                case INS_sve_st3h:
                case INS_sve_st4h:
                    assert(opt == INS_OPTS_SCALABLE_H);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_st2w:
                case INS_sve_st3w:
                case INS_sve_st4w:
                    assert(opt == INS_OPTS_SCALABLE_S);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                case INS_sve_st2d:
                case INS_sve_st3d:
                case INS_sve_st4d:
                    assert(opt == INS_OPTS_SCALABLE_D);
                    assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                    break;

                default:
                    assert(!"Invalid instruction");
                    break;
            }
#endif // DEBUG
            fmt = IF_SVE_JC_4A;
            break;

        case INS_sve_st2q:
        case INS_sve_st3q:
        case INS_sve_st4q:
            assert(isVectorRegister(reg1));
            assert(isPredicateRegister(reg2));
            assert(isGeneralRegister(reg3));
            assert(isGeneralRegister(reg4));
            assert(isScalableVectorSize(size));
            assert(opt == INS_OPTS_SCALABLE_Q);
            fmt = IF_SVE_JF_4A;
            break;

        case INS_sve_bfmla:
        case INS_sve_bfmls:
            assert(opt == INS_OPTS_SCALABLE_H);
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            fmt = IF_SVE_HU_4B;
            break;

        case INS_sve_fmad:
        case INS_sve_fmsb:
        case INS_sve_fnmad:
        case INS_sve_fnmsb:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(insScalableOptsNone(sopt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            fmt = IF_SVE_HV_4A;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    // Use aliases.
    switch (ins)
    {
        case INS_sve_cmple:
            std::swap(reg3, reg4);
            ins = INS_sve_cmpge;
            break;
        case INS_sve_cmplo:
            std::swap(reg3, reg4);
            ins = INS_sve_cmphi;
            break;
        case INS_sve_cmpls:
            std::swap(reg3, reg4);
            ins = INS_sve_cmphs;
            break;
        case INS_sve_cmplt:
            std::swap(reg3, reg4);
            ins = INS_sve_cmpgt;
            break;
        case INS_sve_facle:
            std::swap(reg3, reg4);
            ins = INS_sve_facge;
            break;
        case INS_sve_faclt:
            std::swap(reg3, reg4);
            ins = INS_sve_facgt;
            break;
        case INS_sve_fcmle:
            std::swap(reg3, reg4);
            ins = INS_sve_fcmge;
            break;
        case INS_sve_fcmlt:
            std::swap(reg3, reg4);
            ins = INS_sve_fcmgt;
            break;
        default:
            break;
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing four registers and a constant.
 */

void emitter::emitInsSve_R_R_R_R_I(instruction ins,
                                   emitAttr    attr,
                                   regNumber   reg1,
                                   regNumber   reg2,
                                   regNumber   reg3,
                                   regNumber   reg4,
                                   ssize_t     imm,
                                   insOpts     opt /* = INS_OPT_NONE*/)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_fcmla:
            assert(insOptsScalableAtLeastHalf(opt));
            assert(isVectorRegister(reg1));
            assert(isLowPredicateRegister(reg2));
            assert(isVectorRegister(reg3));
            assert(isVectorRegister(reg4));
            assert(isScalableVectorSize(size));
            imm = emitEncodeRotationImm0_to_270(imm);
            fmt = IF_SVE_GT_4A;
            break;

        case INS_sve_psel:
            unreached(); // TODO-SVE: Not yet supported.
            assert(insOptsScalableStandard(opt));
            assert(isPredicateRegister(reg1)); // DDDD
            assert(isPredicateRegister(reg2)); // NNNN
            assert(isPredicateRegister(reg3)); // MMMM
            assert(isGeneralRegister(reg4));   // vv
            assert((REG_R12 <= reg4) && (reg4 <= REG_R15));

            switch (opt)
            {
                case INS_OPTS_SCALABLE_B:
                    assert(isValidUimm<4>(imm));
                    break;

                case INS_OPTS_SCALABLE_H:
                    assert(isValidUimm<3>(imm));
                    break;

                case INS_OPTS_SCALABLE_S:
                    assert(isValidUimm<2>(imm));
                    break;

                case INS_OPTS_SCALABLE_D:
                    assert(isValidUimm<1>(imm));
                    break;

                default:
                    unreached();
                    break;
            }

            fmt = IF_SVE_DV_4A;
            break;

        default:
            unreached();
            break;
    }
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idReg4(reg4);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register, a SVE Pattern.
 */

void emitter::emitIns_R_PATTERN(
    instruction ins, emitAttr attr, regNumber reg1, insOpts opt, insSvePattern pattern /* = SVE_PATTERN_ALL*/)
{
    insFormat fmt = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_ptrue:
        case INS_sve_ptrues:
            assert(isPredicateRegister(reg1));
            assert(isScalableVectorSize(attr));
            assert(insOptsScalableStandard(opt));
            fmt = IF_SVE_DE_1A;
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idInsOpt(opt);
    id->idSvePattern(pattern);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing a register, a SVE Pattern and an immediate.
 */

void emitter::emitIns_R_PATTERN_I(instruction   ins,
                                  emitAttr      attr,
                                  regNumber     reg1,
                                  insSvePattern pattern,
                                  ssize_t       imm,
                                  insOpts       opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_cntb:
        case INS_sve_cntd:
        case INS_sve_cnth:
        case INS_sve_cntw:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));  // ddddd
            assert(isValidUimmFrom1<4>(imm)); // iiii
            assert(size == EA_8BYTE);
            fmt = IF_SVE_BL_1A;
            break;

        case INS_sve_incd:
        case INS_sve_inch:
        case INS_sve_incw:
        case INS_sve_decd:
        case INS_sve_dech:
        case INS_sve_decw:
            assert(isValidUimmFrom1<4>(imm)); // iiii

            if (insOptsNone(opt))
            {
                assert(isGeneralRegister(reg1)); // ddddd
                assert(size == EA_8BYTE);
                fmt = IF_SVE_BM_1A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt));
                assert(isVectorRegister(reg1)); // ddddd
                fmt = IF_SVE_BN_1A;
            }
            break;

        case INS_sve_incb:
        case INS_sve_decb:
            assert(isGeneralRegister(reg1));  // ddddd
            assert(isValidUimmFrom1<4>(imm)); // iiii
            assert(size == EA_8BYTE);
            fmt = IF_SVE_BM_1A;
            break;

        case INS_sve_sqincb:
        case INS_sve_uqincb:
        case INS_sve_sqdecb:
        case INS_sve_uqdecb:
            assert(insOptsNone(opt));
            assert(isGeneralRegister(reg1));      // ddddd
            assert(isValidUimmFrom1<4>(imm));     // iiii
            assert(isValidGeneralDatasize(size)); // X
            fmt = IF_SVE_BO_1A;
            break;

        case INS_sve_sqinch:
        case INS_sve_uqinch:
        case INS_sve_sqdech:
        case INS_sve_uqdech:
        case INS_sve_sqincw:
        case INS_sve_uqincw:
        case INS_sve_sqdecw:
        case INS_sve_uqdecw:
        case INS_sve_sqincd:
        case INS_sve_uqincd:
        case INS_sve_sqdecd:
        case INS_sve_uqdecd:
            assert(isValidUimmFrom1<4>(imm)); // iiii

            if (insOptsNone(opt))
            {
                assert(isGeneralRegister(reg1));      // ddddd
                assert(isValidGeneralDatasize(size)); // X
                fmt = IF_SVE_BO_1A;
            }
            else
            {
                assert(insOptsScalableAtLeastHalf(opt));
                assert(isVectorRegister(reg1)); // ddddd
                assert(isScalableVectorSize(size));
                fmt = IF_SVE_BP_1A;
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsFmt(fmt);
    id->idInsOpt(opt);
    id->idOpSize(size);

    id->idReg1(reg1);
    id->idSvePattern(pattern);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing three registers and a SVE 'prfop'.
 */

void emitter::emitIns_PRFOP_R_R_R(instruction     ins,
                                  emitAttr        attr,
                                  insSvePrfop     prfop,
                                  regNumber       reg1,
                                  regNumber       reg2,
                                  regNumber       reg3,
                                  insOpts         opt /* = INS_OPTS_NONE */,
                                  insScalableOpts sopt /* = INS_SCALABLE_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_prfb:
            assert(insScalableOptsNone(sopt));
            assert(isLowPredicateRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isScalableVectorSize(size));

            if (insOptsScalable32bitExtends(opt))
            {
                assert(isVectorRegister(reg3));

                if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HY_3A;
                }
                else
                {
                    assert(insOptsScalableDoubleWord32bitExtends(opt));
                    fmt = IF_SVE_HY_3A_A;
                }
            }
            else if (isVectorRegister(reg3))
            {
                assert(opt == INS_OPTS_SCALABLE_D);
                fmt = IF_SVE_HY_3B;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isGeneralRegister(reg3));
                fmt = IF_SVE_IB_3A;
            }
            break;

        case INS_sve_prfh:
        case INS_sve_prfw:
        case INS_sve_prfd:
            assert(isLowPredicateRegister(reg1));
            assert(isGeneralRegister(reg2));
            assert(isScalableVectorSize(size));

            if (sopt == INS_SCALABLE_OPTS_MOD_N)
            {
                if (insOptsScalableSingleWord32bitExtends(opt))
                {
                    fmt = IF_SVE_HY_3A;
                }
                else
                {
                    assert(insOptsScalableDoubleWord32bitExtends(opt));
                    fmt = IF_SVE_HY_3A_A;
                }
            }
            else
            {
                assert(sopt == INS_SCALABLE_OPTS_LSL_N);
                if (isVectorRegister(reg3))
                {
                    assert(opt == INS_OPTS_SCALABLE_D);
                    fmt = IF_SVE_HY_3B;
                }
                else
                {
                    assert(insOptsNone(opt));
                    assert(isGeneralRegister(reg3));
                    fmt = IF_SVE_IB_3A;
                }
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsOpt(opt);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idSvePrfop(prfop);

    dispIns(id);
    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add a SVE instruction referencing two registers, a SVE 'prfop' and an immediate.
 */

void emitter::emitIns_PRFOP_R_R_I(instruction ins,
                                  emitAttr    attr,
                                  insSvePrfop prfop,
                                  regNumber   reg1,
                                  regNumber   reg2,
                                  int         imm,
                                  insOpts     opt /* = INS_OPTS_NONE */)
{
    emitAttr  size = EA_SIZE(attr);
    insFormat fmt  = IF_NONE;

    /* Figure out the encoding format of the instruction */
    switch (ins)
    {
        case INS_sve_prfb:
        case INS_sve_prfh:
        case INS_sve_prfw:
        case INS_sve_prfd:
            assert(isLowPredicateRegister(reg1));
            assert(isScalableVectorSize(size));

            if (isVectorRegister(reg2))
            {
                assert(insOptsScalableWords(opt));

#ifdef DEBUG
                switch (ins)
                {
                    case INS_sve_prfb:
                        assert(isValidUimm<5>(imm));
                        break;

                    case INS_sve_prfh:
                        assert((isValidUimm_MultipleOf<5, 2>(imm)));
                        break;

                    case INS_sve_prfw:
                        assert((isValidUimm_MultipleOf<5, 4>(imm)));
                        break;

                    case INS_sve_prfd:
                        assert((isValidUimm_MultipleOf<5, 8>(imm)));
                        break;

                    default:
                        assert(!"Invalid instruction");
                        break;
                }
#endif // DEBUG
                fmt = IF_SVE_HZ_2A_B;
            }
            else
            {
                assert(insOptsNone(opt));
                assert(isGeneralRegister(reg2));
                assert(isValidSimm<6>(imm));
                fmt = IF_SVE_IA_2A;
            }
            break;

        default:
            unreached();
            break;

    } // end switch (ins)
    assert(fmt != IF_NONE);

    instrDesc* id = emitNewInstrCns(attr, imm);

    id->idIns(ins);
    id->idInsOpt(opt);
    id->idInsFmt(fmt);

    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idSvePrfop(prfop);

    dispIns(id);
    appendToCurIG(id);
}

#endif // TARGET_ARM64
