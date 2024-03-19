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
                    return emitIns_R_R_R(INS_sve_mov, attr, reg1, reg2, reg3, opt, INS_SCALABLE_OPTS_PREDICATE_MERGE);
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

#endif // TARGET_ARM64
