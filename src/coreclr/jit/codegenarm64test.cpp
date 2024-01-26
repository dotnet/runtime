// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        Arm64 Code Generator Tests                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_ARM64) && defined(DEBUG)

#include "emit.h"
#include "codegen.h"

/*****************************************************************************
 * Unit tests for the general instructions.
 */
void CodeGen::genArm64EmitterUnitTestsGeneral()
{
    emitter* theEmitter = GetEmitter();

    //
    // Loads/Stores basic general register
    //

    genDefineTempLabel(genCreateTempLabel());

    // ldr/str Xt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrh, EA_2BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strh, EA_2BYTE, REG_R8, REG_R9);

    // ldr/str Wt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_4BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrh, EA_2BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_4BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strh, EA_2BYTE, REG_R8, REG_R9);

    theEmitter->emitIns_R_R(INS_ldrsb, EA_4BYTE, REG_R8, REG_R9); // target Wt
    theEmitter->emitIns_R_R(INS_ldrsh, EA_4BYTE, REG_R8, REG_R9); // target Wt
    theEmitter->emitIns_R_R(INS_ldrsb, EA_8BYTE, REG_R8, REG_R9); // target Xt
    theEmitter->emitIns_R_R(INS_ldrsh, EA_8BYTE, REG_R8, REG_R9); // target Xt
    theEmitter->emitIns_R_R(INS_ldrsw, EA_8BYTE, REG_R8, REG_R9); // target Xt

    theEmitter->emitIns_R_R_I(INS_ldurb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldurh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sturb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sturh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursb, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursh, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_stur, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_stur, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursw, EA_8BYTE, REG_R8, REG_R9, 1);

    // SP and ZR tests
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_R8, REG_SP, 1);
    theEmitter->emitIns_R_R_I(INS_ldurb, EA_8BYTE, REG_ZR, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldurh, EA_8BYTE, REG_ZR, REG_SP, 1);

    // scaled
    theEmitter->emitIns_R_R_I(INS_ldrb, EA_1BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldrh, EA_2BYTE, REG_R8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 8);

    // pre-/post-indexed (unscaled)
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_PRE_INDEX);

    // ldar/stlr Rt, [reg]
    theEmitter->emitIns_R_R(INS_ldar, EA_8BYTE, REG_R9, REG_R8);
    theEmitter->emitIns_R_R(INS_ldar, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_ldarb, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_ldarh, EA_4BYTE, REG_R5, REG_R12);

    theEmitter->emitIns_R_R(INS_stlr, EA_8BYTE, REG_R9, REG_R8);
    theEmitter->emitIns_R_R(INS_stlr, EA_4BYTE, REG_R7, REG_R13);
    theEmitter->emitIns_R_R(INS_stlrb, EA_4BYTE, REG_R5, REG_R14);
    theEmitter->emitIns_R_R(INS_stlrh, EA_4BYTE, REG_R3, REG_R15);

    // ldapr Rt, [reg]
    theEmitter->emitIns_R_R(INS_ldapr, EA_8BYTE, REG_R9, REG_R8);
    theEmitter->emitIns_R_R(INS_ldapr, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_ldaprb, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_ldaprh, EA_4BYTE, REG_R5, REG_R12);

    // ldaxr Rt, [reg]
    theEmitter->emitIns_R_R(INS_ldaxr, EA_8BYTE, REG_R9, REG_R8);
    theEmitter->emitIns_R_R(INS_ldaxr, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_ldaxrb, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_ldaxrh, EA_4BYTE, REG_R5, REG_R12);

    // ldxr Rt, [reg]
    theEmitter->emitIns_R_R(INS_ldxr, EA_8BYTE, REG_R9, REG_R8);
    theEmitter->emitIns_R_R(INS_ldxr, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_ldxrb, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_ldxrh, EA_4BYTE, REG_R5, REG_R12);

    // stxr Ws, Rt, [reg]
    theEmitter->emitIns_R_R_R(INS_stxr, EA_8BYTE, REG_R1, REG_R9, REG_R8);
    theEmitter->emitIns_R_R_R(INS_stxr, EA_4BYTE, REG_R3, REG_R7, REG_R13);
    theEmitter->emitIns_R_R_R(INS_stxrb, EA_4BYTE, REG_R8, REG_R5, REG_R14);
    theEmitter->emitIns_R_R_R(INS_stxrh, EA_4BYTE, REG_R12, REG_R3, REG_R15);

    // stlxr Ws, Rt, [reg]
    theEmitter->emitIns_R_R_R(INS_stlxr, EA_8BYTE, REG_R1, REG_R9, REG_R8);
    theEmitter->emitIns_R_R_R(INS_stlxr, EA_4BYTE, REG_R3, REG_R7, REG_R13);
    theEmitter->emitIns_R_R_R(INS_stlxrb, EA_4BYTE, REG_R8, REG_R5, REG_R14);
    theEmitter->emitIns_R_R_R(INS_stlxrh, EA_4BYTE, REG_R12, REG_R3, REG_R15);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld1, EA_8BYTE, REG_V0, REG_R1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld1, EA_16BYTE, REG_V2, REG_R3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld1, EA_8BYTE, REG_V4, REG_R5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld1, EA_16BYTE, REG_V6, REG_R7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld1, EA_8BYTE, REG_V8, REG_R9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld1, EA_16BYTE, REG_V10, REG_R11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld1, EA_8BYTE, REG_V12, REG_R13, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld1, EA_16BYTE, REG_V14, REG_R15, INS_OPTS_2D);

    // ld1 {Vt, Vt2}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_8BYTE, REG_V0, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_16BYTE, REG_V3, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_8BYTE, REG_V6, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_16BYTE, REG_V9, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_8BYTE, REG_V12, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_16BYTE, REG_V15, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_8BYTE, REG_V18, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld1_2regs, EA_16BYTE, REG_V21, REG_R23, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_8BYTE, REG_V0, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_16BYTE, REG_V4, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_8BYTE, REG_V8, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_16BYTE, REG_V12, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_8BYTE, REG_V16, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_16BYTE, REG_V20, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_8BYTE, REG_V24, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld1_3regs, EA_16BYTE, REG_V28, REG_SP, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_8BYTE, REG_V0, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_16BYTE, REG_V5, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_8BYTE, REG_V10, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_16BYTE, REG_V15, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_8BYTE, REG_V20, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_16BYTE, REG_V25, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_8BYTE, REG_V30, REG_R2, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld1_4regs, EA_16BYTE, REG_V3, REG_R7, INS_OPTS_2D);

    // ld2 {Vt, Vt2}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld2, EA_8BYTE, REG_V0, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld2, EA_16BYTE, REG_V3, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld2, EA_8BYTE, REG_V6, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld2, EA_16BYTE, REG_V9, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld2, EA_8BYTE, REG_V12, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld2, EA_16BYTE, REG_V15, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld2, EA_16BYTE, REG_V18, REG_R20, INS_OPTS_2D);

    // ld3 {Vt, Vt2, Vt3}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld3, EA_8BYTE, REG_V0, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld3, EA_16BYTE, REG_V4, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld3, EA_8BYTE, REG_V8, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld3, EA_16BYTE, REG_V12, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld3, EA_8BYTE, REG_V16, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld3, EA_16BYTE, REG_V20, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld3, EA_16BYTE, REG_V24, REG_R27, INS_OPTS_2D);

    // ld4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld4, EA_8BYTE, REG_V0, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld4, EA_16BYTE, REG_V5, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld4, EA_8BYTE, REG_V10, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld4, EA_16BYTE, REG_V15, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld4, EA_8BYTE, REG_V20, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld4, EA_16BYTE, REG_V25, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld4, EA_16BYTE, REG_V30, REG_R2, INS_OPTS_2D);

    // st1 {Vt}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st1, EA_8BYTE, REG_V0, REG_R1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st1, EA_16BYTE, REG_V2, REG_R3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st1, EA_8BYTE, REG_V4, REG_R5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st1, EA_16BYTE, REG_V6, REG_R7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st1, EA_8BYTE, REG_V8, REG_R9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st1, EA_16BYTE, REG_V10, REG_R11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st1, EA_8BYTE, REG_V12, REG_R13, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_st1, EA_16BYTE, REG_V14, REG_R15, INS_OPTS_2D);

    // st1 {Vt, Vt2}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_8BYTE, REG_V0, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_16BYTE, REG_V3, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_8BYTE, REG_V6, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_16BYTE, REG_V9, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_8BYTE, REG_V12, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_16BYTE, REG_V15, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_8BYTE, REG_V18, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_st1_2regs, EA_16BYTE, REG_V21, REG_R23, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_8BYTE, REG_V0, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_16BYTE, REG_V4, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_8BYTE, REG_V8, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_16BYTE, REG_V12, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_8BYTE, REG_V16, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_16BYTE, REG_V20, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_8BYTE, REG_V24, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_st1_3regs, EA_16BYTE, REG_V28, REG_SP, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_8BYTE, REG_V0, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_16BYTE, REG_V5, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_8BYTE, REG_V10, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_16BYTE, REG_V15, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_8BYTE, REG_V20, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_16BYTE, REG_V25, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_8BYTE, REG_V30, REG_R2, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_st1_4regs, EA_16BYTE, REG_V3, REG_R7, INS_OPTS_2D);

    // st2 {Vt, Vt2}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st2, EA_8BYTE, REG_V0, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st2, EA_16BYTE, REG_V3, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st2, EA_8BYTE, REG_V6, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st2, EA_16BYTE, REG_V9, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st2, EA_8BYTE, REG_V12, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st2, EA_16BYTE, REG_V15, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st2, EA_16BYTE, REG_V18, REG_R20, INS_OPTS_2D);

    // st3 {Vt, Vt2, Vt3}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st3, EA_8BYTE, REG_V0, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st3, EA_16BYTE, REG_V4, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st3, EA_8BYTE, REG_V8, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st3, EA_16BYTE, REG_V12, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st3, EA_8BYTE, REG_V16, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st3, EA_16BYTE, REG_V20, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st3, EA_16BYTE, REG_V24, REG_R27, INS_OPTS_2D);

    // st4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_st4, EA_8BYTE, REG_V0, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_st4, EA_16BYTE, REG_V5, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_st4, EA_8BYTE, REG_V10, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_st4, EA_16BYTE, REG_V15, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_st4, EA_8BYTE, REG_V20, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_st4, EA_16BYTE, REG_V25, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_st4, EA_16BYTE, REG_V30, REG_R2, INS_OPTS_2D);

    // ld1r {Vt}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld1r, EA_8BYTE, REG_V0, REG_R1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld1r, EA_16BYTE, REG_V2, REG_R3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld1r, EA_8BYTE, REG_V4, REG_R5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld1r, EA_16BYTE, REG_V6, REG_R7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld1r, EA_8BYTE, REG_V8, REG_R9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld1r, EA_16BYTE, REG_V10, REG_R11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld1r, EA_8BYTE, REG_V12, REG_R13, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld1r, EA_16BYTE, REG_V14, REG_R15, INS_OPTS_2D);

    // ld2r {Vt, Vt2}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld2r, EA_8BYTE, REG_V0, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld2r, EA_16BYTE, REG_V3, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld2r, EA_8BYTE, REG_V6, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld2r, EA_16BYTE, REG_V9, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld2r, EA_8BYTE, REG_V12, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld2r, EA_16BYTE, REG_V15, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld2r, EA_8BYTE, REG_V18, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld2r, EA_16BYTE, REG_V21, REG_R23, INS_OPTS_2D);

    // ld3r {Vt, Vt2, Vt3}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld3r, EA_8BYTE, REG_V0, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld3r, EA_16BYTE, REG_V4, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld3r, EA_8BYTE, REG_V8, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld3r, EA_16BYTE, REG_V12, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld3r, EA_8BYTE, REG_V16, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld3r, EA_16BYTE, REG_V20, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld3r, EA_8BYTE, REG_V24, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld3r, EA_16BYTE, REG_V28, REG_SP, INS_OPTS_2D);

    // ld4r {Vt, Vt2, Vt3, Vt4}, [Xn|SP]
    theEmitter->emitIns_R_R(INS_ld4r, EA_8BYTE, REG_V0, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_ld4r, EA_16BYTE, REG_V5, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_ld4r, EA_8BYTE, REG_V10, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_ld4r, EA_16BYTE, REG_V15, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_ld4r, EA_8BYTE, REG_V20, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ld4r, EA_16BYTE, REG_V25, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ld4r, EA_8BYTE, REG_V30, REG_R2, INS_OPTS_1D);
    theEmitter->emitIns_R_R(INS_ld4r, EA_16BYTE, REG_V3, REG_R7, INS_OPTS_2D);

    // tbl Vd, {Vt}, Vm
    theEmitter->emitIns_R_R_R(INS_tbl, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbl, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbx Vd, {Vt}, Vm
    theEmitter->emitIns_R_R_R(INS_tbx, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbx, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbl Vd, {Vt, Vt2}, Vm
    theEmitter->emitIns_R_R_R(INS_tbl_2regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbl_2regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbx Vd, {Vt, Vt2}, Vm
    theEmitter->emitIns_R_R_R(INS_tbx_2regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbx_2regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbl Vd, {Vt, Vt2, Vt3}, Vm
    theEmitter->emitIns_R_R_R(INS_tbl_3regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbl_3regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbx Vd, {Vt, Vt2, Vt3}, Vm
    theEmitter->emitIns_R_R_R(INS_tbx_3regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbx_3regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbl Vd, {Vt, Vt2, Vt3, Vt4}, Vm
    theEmitter->emitIns_R_R_R(INS_tbl_4regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbl_4regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    // tbx Vd, {Vt, Vt2, Vt3, Vt4}, Vm
    theEmitter->emitIns_R_R_R(INS_tbx_4regs, EA_8BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_tbx_4regs, EA_16BYTE, REG_V0, REG_V1, REG_V6, INS_OPTS_16B);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld1, EA_8BYTE, REG_V0, REG_R1, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_16BYTE, REG_V3, REG_R4, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_8BYTE, REG_V6, REG_R7, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_16BYTE, REG_V9, REG_R10, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_8BYTE, REG_V12, REG_R13, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_16BYTE, REG_V15, REG_R16, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_8BYTE, REG_V18, REG_R19, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld1, EA_16BYTE, REG_V21, REG_R22, REG_R23, INS_OPTS_2D);

    // ld1 {Vt, Vt2}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_8BYTE, REG_V0, REG_R2, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_16BYTE, REG_V4, REG_R6, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_8BYTE, REG_V8, REG_R10, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_16BYTE, REG_V12, REG_R14, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_8BYTE, REG_V16, REG_R18, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_16BYTE, REG_V20, REG_R22, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_8BYTE, REG_V24, REG_R26, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld1_2regs, EA_16BYTE, REG_V28, REG_SP, REG_R30, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_8BYTE, REG_V0, REG_R3, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_16BYTE, REG_V5, REG_R8, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_8BYTE, REG_V10, REG_R13, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_16BYTE, REG_V15, REG_R18, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_8BYTE, REG_V20, REG_R23, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_16BYTE, REG_V25, REG_R28, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_8BYTE, REG_V30, REG_R0, REG_R1, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld1_3regs, EA_16BYTE, REG_V2, REG_R5, REG_R6, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_8BYTE, REG_V0, REG_R4, REG_R5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_16BYTE, REG_V6, REG_R10, REG_R11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_8BYTE, REG_V12, REG_R16, REG_R17, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_16BYTE, REG_V18, REG_R22, REG_R23, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_8BYTE, REG_V24, REG_R28, REG_R29, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_16BYTE, REG_V30, REG_R2, REG_R3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_8BYTE, REG_V4, REG_R8, REG_R9, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld1_4regs, EA_16BYTE, REG_V10, REG_R14, REG_R15, INS_OPTS_2D);

    // ld2 {Vt, Vt2}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld2, EA_8BYTE, REG_V0, REG_R2, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_16BYTE, REG_V4, REG_R6, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_8BYTE, REG_V8, REG_R10, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_16BYTE, REG_V12, REG_R14, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_8BYTE, REG_V16, REG_R18, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_16BYTE, REG_V20, REG_R22, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld2, EA_16BYTE, REG_V24, REG_R26, REG_R27, INS_OPTS_2D);

    // ld3 {Vt, Vt2, Vt3}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld3, EA_8BYTE, REG_V0, REG_R3, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_16BYTE, REG_V5, REG_R8, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_8BYTE, REG_V10, REG_R13, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_16BYTE, REG_V15, REG_R18, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_8BYTE, REG_V20, REG_R23, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_16BYTE, REG_V25, REG_R28, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld3, EA_16BYTE, REG_V30, REG_R0, REG_R1, INS_OPTS_2D);

    // ld4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld4, EA_8BYTE, REG_V0, REG_R4, REG_R5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_16BYTE, REG_V6, REG_R10, REG_R11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_8BYTE, REG_V12, REG_R16, REG_R17, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_16BYTE, REG_V18, REG_R22, REG_R23, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_8BYTE, REG_V24, REG_R28, REG_R29, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_16BYTE, REG_V30, REG_R2, REG_R3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld4, EA_16BYTE, REG_V4, REG_R8, REG_R9, INS_OPTS_2D);

    // st1 {Vt}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st1, EA_8BYTE, REG_V0, REG_R1, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st1, EA_16BYTE, REG_V3, REG_R4, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st1, EA_8BYTE, REG_V6, REG_R7, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st1, EA_16BYTE, REG_V9, REG_R10, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st1, EA_8BYTE, REG_V12, REG_R13, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st1, EA_16BYTE, REG_V15, REG_R16, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st1, EA_8BYTE, REG_V18, REG_R19, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_st1, EA_16BYTE, REG_V21, REG_R22, REG_R23, INS_OPTS_2D);

    // st1 {Vt, Vt2}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_8BYTE, REG_V0, REG_R2, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_16BYTE, REG_V4, REG_R6, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_8BYTE, REG_V8, REG_R10, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_16BYTE, REG_V12, REG_R14, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_8BYTE, REG_V16, REG_R18, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_16BYTE, REG_V20, REG_R22, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_8BYTE, REG_V24, REG_R26, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_st1_2regs, EA_16BYTE, REG_V28, REG_SP, REG_R30, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_8BYTE, REG_V0, REG_R3, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_16BYTE, REG_V5, REG_R8, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_8BYTE, REG_V10, REG_R13, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_16BYTE, REG_V15, REG_R18, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_8BYTE, REG_V20, REG_R23, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_16BYTE, REG_V25, REG_R28, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_8BYTE, REG_V30, REG_R0, REG_R1, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_st1_3regs, EA_16BYTE, REG_V2, REG_R5, REG_R6, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_8BYTE, REG_V0, REG_R4, REG_R5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_16BYTE, REG_V6, REG_R10, REG_R11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_8BYTE, REG_V12, REG_R16, REG_R17, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_16BYTE, REG_V18, REG_R22, REG_R23, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_8BYTE, REG_V24, REG_R28, REG_R29, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_16BYTE, REG_V30, REG_R2, REG_R3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_8BYTE, REG_V4, REG_R8, REG_R9, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_st1_4regs, EA_16BYTE, REG_V10, REG_R14, REG_R15, INS_OPTS_2D);

    // st2 {Vt, Vt2}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st2, EA_8BYTE, REG_V0, REG_R2, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st2, EA_16BYTE, REG_V4, REG_R6, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st2, EA_8BYTE, REG_V8, REG_R10, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st2, EA_16BYTE, REG_V12, REG_R14, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st2, EA_8BYTE, REG_V16, REG_R18, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st2, EA_16BYTE, REG_V20, REG_R22, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st2, EA_16BYTE, REG_V24, REG_R26, REG_R27, INS_OPTS_2D);

    // st3 {Vt, Vt2, Vt3}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st3, EA_8BYTE, REG_V0, REG_R3, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st3, EA_16BYTE, REG_V5, REG_R8, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st3, EA_8BYTE, REG_V10, REG_R13, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st3, EA_16BYTE, REG_V15, REG_R18, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st3, EA_8BYTE, REG_V20, REG_R23, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st3, EA_16BYTE, REG_V25, REG_R28, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st3, EA_16BYTE, REG_V30, REG_R0, REG_R1, INS_OPTS_2D);

    // st4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_st4, EA_8BYTE, REG_V0, REG_R4, REG_R5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_st4, EA_16BYTE, REG_V6, REG_R10, REG_R11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_st4, EA_8BYTE, REG_V12, REG_R16, REG_R17, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_st4, EA_16BYTE, REG_V18, REG_R22, REG_R23, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_st4, EA_8BYTE, REG_V24, REG_R28, REG_R29, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_st4, EA_16BYTE, REG_V30, REG_R2, REG_R3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_st4, EA_16BYTE, REG_V4, REG_R8, REG_R9, INS_OPTS_2D);

    // ld1r {Vt}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_8BYTE, REG_V0, REG_R1, REG_R2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_16BYTE, REG_V3, REG_R4, REG_R5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_8BYTE, REG_V6, REG_R7, REG_R8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_16BYTE, REG_V9, REG_R10, REG_R11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_8BYTE, REG_V12, REG_R13, REG_R14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_16BYTE, REG_V15, REG_R16, REG_R17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_8BYTE, REG_V18, REG_R19, REG_R20, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld1r, EA_16BYTE, REG_V21, REG_R22, REG_R23, INS_OPTS_2D);

    // ld2r {Vt, Vt2}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_8BYTE, REG_V0, REG_R2, REG_R3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_16BYTE, REG_V4, REG_R6, REG_R7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_8BYTE, REG_V8, REG_R10, REG_R11, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_16BYTE, REG_V12, REG_R14, REG_R15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_8BYTE, REG_V16, REG_R18, REG_R19, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_16BYTE, REG_V20, REG_R22, REG_R23, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_8BYTE, REG_V24, REG_R26, REG_R27, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld2r, EA_16BYTE, REG_V28, REG_SP, REG_R30, INS_OPTS_2D);

    // ld3r {Vt, Vt2, Vt3}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_8BYTE, REG_V0, REG_R3, REG_R4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_16BYTE, REG_V5, REG_R8, REG_R9, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_8BYTE, REG_V10, REG_R13, REG_R14, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_16BYTE, REG_V15, REG_R18, REG_R19, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_8BYTE, REG_V20, REG_R23, REG_R24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_16BYTE, REG_V25, REG_R28, REG_R29, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_8BYTE, REG_V30, REG_R0, REG_R1, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld3r, EA_16BYTE, REG_V2, REG_R5, REG_R6, INS_OPTS_2D);

    // ld4r {Vt, Vt2, Vt3, Vt4}, [Xn|SP], Xm
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_8BYTE, REG_V0, REG_R4, REG_R5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_16BYTE, REG_V6, REG_R10, REG_R11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_8BYTE, REG_V12, REG_R16, REG_R17, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_16BYTE, REG_V18, REG_R22, REG_R23, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_8BYTE, REG_V24, REG_R28, REG_R29, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_16BYTE, REG_V30, REG_R2, REG_R3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_8BYTE, REG_V4, REG_R8, REG_R9, INS_OPTS_1D);
    theEmitter->emitIns_R_R_R(INS_ld4r, EA_16BYTE, REG_V10, REG_R14, REG_R15, INS_OPTS_2D);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld1, EA_8BYTE, REG_V0, REG_R1, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_16BYTE, REG_V2, REG_R3, 16, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_8BYTE, REG_V4, REG_R5, 8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_16BYTE, REG_V6, REG_R7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_8BYTE, REG_V8, REG_R9, 8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_16BYTE, REG_V10, REG_R11, 16, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_8BYTE, REG_V12, REG_R13, 8, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_16BYTE, REG_V14, REG_R15, 16, INS_OPTS_2D);

    // ld1 {Vt, Vt2}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_8BYTE, REG_V0, REG_R2, 16, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_16BYTE, REG_V3, REG_R5, 32, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_8BYTE, REG_V6, REG_R8, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_16BYTE, REG_V9, REG_R11, 32, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_8BYTE, REG_V12, REG_R14, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_16BYTE, REG_V15, REG_R17, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_8BYTE, REG_V18, REG_R20, 16, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld1_2regs, EA_16BYTE, REG_V21, REG_R23, 32, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_8BYTE, REG_V0, REG_R3, 24, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_16BYTE, REG_V4, REG_R7, 48, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_8BYTE, REG_V8, REG_R11, 24, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_16BYTE, REG_V12, REG_R15, 48, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_8BYTE, REG_V16, REG_R19, 24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_16BYTE, REG_V20, REG_R23, 48, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_8BYTE, REG_V24, REG_R27, 24, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld1_3regs, EA_16BYTE, REG_V28, REG_SP, 48, INS_OPTS_2D);

    // ld1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_8BYTE, REG_V0, REG_R4, 32, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_16BYTE, REG_V5, REG_R9, 64, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_8BYTE, REG_V10, REG_R14, 32, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_16BYTE, REG_V15, REG_R19, 64, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_8BYTE, REG_V20, REG_R24, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_16BYTE, REG_V25, REG_R29, 64, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_8BYTE, REG_V30, REG_R2, 32, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld1_4regs, EA_16BYTE, REG_V3, REG_R7, 64, INS_OPTS_2D);

    // ld2 {Vt, Vt2}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld2, EA_8BYTE, REG_V0, REG_R2, 16, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_16BYTE, REG_V3, REG_R5, 32, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_8BYTE, REG_V6, REG_R8, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_16BYTE, REG_V9, REG_R11, 32, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_8BYTE, REG_V12, REG_R14, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_16BYTE, REG_V15, REG_R17, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_16BYTE, REG_V18, REG_R20, 32, INS_OPTS_2D);

    // ld3 {Vt, Vt2, Vt3}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld3, EA_8BYTE, REG_V0, REG_R3, 24, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_16BYTE, REG_V4, REG_R7, 48, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_8BYTE, REG_V8, REG_R11, 24, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_16BYTE, REG_V12, REG_R15, 48, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_8BYTE, REG_V16, REG_R19, 24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_16BYTE, REG_V20, REG_R23, 48, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_16BYTE, REG_V24, REG_R27, 48, INS_OPTS_2D);

    // ld4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld4, EA_8BYTE, REG_V0, REG_R4, 32, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_16BYTE, REG_V5, REG_R9, 64, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_8BYTE, REG_V10, REG_R14, 32, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_16BYTE, REG_V15, REG_R19, 64, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_8BYTE, REG_V20, REG_R24, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_16BYTE, REG_V25, REG_R29, 64, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_16BYTE, REG_V30, REG_R2, 64, INS_OPTS_2D);

    // st1 {Vt}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st1, EA_8BYTE, REG_V0, REG_R1, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st1, EA_16BYTE, REG_V2, REG_R3, 16, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st1, EA_8BYTE, REG_V4, REG_R5, 8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st1, EA_16BYTE, REG_V6, REG_R7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st1, EA_8BYTE, REG_V8, REG_R9, 8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st1, EA_16BYTE, REG_V10, REG_R11, 16, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st1, EA_8BYTE, REG_V12, REG_R13, 8, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_st1, EA_16BYTE, REG_V14, REG_R15, 16, INS_OPTS_2D);

    // st1 {Vt, Vt2}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_8BYTE, REG_V0, REG_R2, 16, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_16BYTE, REG_V3, REG_R5, 32, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_8BYTE, REG_V6, REG_R8, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_16BYTE, REG_V9, REG_R11, 32, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_8BYTE, REG_V12, REG_R14, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_16BYTE, REG_V15, REG_R17, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_8BYTE, REG_V18, REG_R20, 16, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_st1_2regs, EA_16BYTE, REG_V21, REG_R23, 32, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_8BYTE, REG_V0, REG_R3, 24, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_16BYTE, REG_V4, REG_R7, 48, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_8BYTE, REG_V8, REG_R11, 24, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_16BYTE, REG_V12, REG_R15, 48, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_8BYTE, REG_V16, REG_R19, 24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_16BYTE, REG_V20, REG_R23, 48, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_8BYTE, REG_V24, REG_R27, 24, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_st1_3regs, EA_16BYTE, REG_V28, REG_SP, 48, INS_OPTS_2D);

    // st1 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_8BYTE, REG_V0, REG_R4, 32, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_16BYTE, REG_V5, REG_R9, 64, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_8BYTE, REG_V10, REG_R14, 32, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_16BYTE, REG_V15, REG_R19, 64, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_8BYTE, REG_V20, REG_R24, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_16BYTE, REG_V25, REG_R29, 64, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_8BYTE, REG_V30, REG_R2, 32, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_st1_4regs, EA_16BYTE, REG_V3, REG_R7, 64, INS_OPTS_2D);

    // st2 {Vt, Vt2}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st2, EA_8BYTE, REG_V0, REG_R2, 16, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st2, EA_16BYTE, REG_V3, REG_R5, 32, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st2, EA_8BYTE, REG_V6, REG_R8, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st2, EA_16BYTE, REG_V9, REG_R11, 32, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st2, EA_8BYTE, REG_V12, REG_R14, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st2, EA_16BYTE, REG_V15, REG_R17, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st2, EA_16BYTE, REG_V18, REG_R20, 32, INS_OPTS_2D);

    // st3 {Vt, Vt2, Vt3}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st3, EA_8BYTE, REG_V0, REG_R3, 24, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st3, EA_16BYTE, REG_V4, REG_R7, 48, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st3, EA_8BYTE, REG_V8, REG_R11, 24, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st3, EA_16BYTE, REG_V12, REG_R15, 48, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st3, EA_8BYTE, REG_V16, REG_R19, 24, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st3, EA_16BYTE, REG_V20, REG_R23, 48, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st3, EA_16BYTE, REG_V24, REG_R27, 48, INS_OPTS_2D);

    // st4 {Vt, Vt2, Vt3, Vt4}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_st4, EA_8BYTE, REG_V0, REG_R4, 32, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_st4, EA_16BYTE, REG_V5, REG_R9, 64, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_st4, EA_8BYTE, REG_V10, REG_R14, 32, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_st4, EA_16BYTE, REG_V15, REG_R19, 64, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_st4, EA_8BYTE, REG_V20, REG_R24, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_st4, EA_16BYTE, REG_V25, REG_R29, 64, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_st4, EA_16BYTE, REG_V30, REG_R2, 64, INS_OPTS_2D);

    // ld1r {Vt}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_8BYTE, REG_V0, REG_R1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_16BYTE, REG_V2, REG_R3, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_8BYTE, REG_V4, REG_R5, 2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_16BYTE, REG_V6, REG_R7, 2, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_8BYTE, REG_V8, REG_R9, 4, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_16BYTE, REG_V10, REG_R11, 4, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_8BYTE, REG_V12, REG_R13, 8, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld1r, EA_16BYTE, REG_V14, REG_R15, 8, INS_OPTS_2D);

    // ld2r {Vt, Vt2}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_8BYTE, REG_V0, REG_R2, 2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_16BYTE, REG_V3, REG_R5, 2, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_8BYTE, REG_V6, REG_R8, 4, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_16BYTE, REG_V9, REG_R11, 4, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_8BYTE, REG_V12, REG_R14, 8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_16BYTE, REG_V15, REG_R17, 8, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_8BYTE, REG_V18, REG_R20, 16, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld2r, EA_16BYTE, REG_V21, REG_R23, 16, INS_OPTS_2D);

    // ld3r {Vt, Vt2, Vt3}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_8BYTE, REG_V0, REG_R3, 3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_16BYTE, REG_V4, REG_R7, 3, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_8BYTE, REG_V8, REG_R11, 6, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_16BYTE, REG_V12, REG_R15, 6, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_8BYTE, REG_V16, REG_R19, 12, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_16BYTE, REG_V20, REG_R23, 12, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_8BYTE, REG_V24, REG_R27, 24, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld3r, EA_16BYTE, REG_V28, REG_SP, 24, INS_OPTS_2D);

    // ld4r {Vt, Vt2, Vt3, Vt4}, [Xn|SP], #imm
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_8BYTE, REG_V0, REG_R4, 4, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_16BYTE, REG_V5, REG_R9, 4, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_8BYTE, REG_V10, REG_R14, 8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_16BYTE, REG_V15, REG_R19, 8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_8BYTE, REG_V20, REG_R24, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_16BYTE, REG_V25, REG_R29, 16, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_8BYTE, REG_V30, REG_R2, 32, INS_OPTS_1D);
    theEmitter->emitIns_R_R_I(INS_ld4r, EA_16BYTE, REG_V3, REG_R7, 32, INS_OPTS_2D);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_ld1, EA_1BYTE, REG_V0, REG_R1, 3);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_2BYTE, REG_V2, REG_R3, 2);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_4BYTE, REG_V4, REG_R5, 1);
    theEmitter->emitIns_R_R_I(INS_ld1, EA_8BYTE, REG_V6, REG_R7, 0);

    // ld2 {Vt, Vt2}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_ld2, EA_1BYTE, REG_V0, REG_R2, 4);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_2BYTE, REG_V3, REG_R5, 3);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_4BYTE, REG_V6, REG_R8, 2);
    theEmitter->emitIns_R_R_I(INS_ld2, EA_8BYTE, REG_V9, REG_R11, 1);

    // ld3 {Vt, Vt2, Vt3}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_ld3, EA_1BYTE, REG_V0, REG_R3, 5);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_2BYTE, REG_V4, REG_R7, 4);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_4BYTE, REG_V8, REG_R11, 3);
    theEmitter->emitIns_R_R_I(INS_ld3, EA_8BYTE, REG_V12, REG_R15, 0);

    // ld4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_ld4, EA_1BYTE, REG_V0, REG_R4, 6);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_2BYTE, REG_V5, REG_R9, 5);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_4BYTE, REG_V10, REG_R14, 0);
    theEmitter->emitIns_R_R_I(INS_ld4, EA_8BYTE, REG_V15, REG_R19, 1);

    // st1 {Vt}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_st1, EA_1BYTE, REG_V0, REG_R1, 7);
    theEmitter->emitIns_R_R_I(INS_st1, EA_2BYTE, REG_V2, REG_R3, 6);
    theEmitter->emitIns_R_R_I(INS_st1, EA_4BYTE, REG_V4, REG_R5, 1);
    theEmitter->emitIns_R_R_I(INS_st1, EA_8BYTE, REG_V6, REG_R7, 0);

    // st2 {Vt, Vt2}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_st2, EA_1BYTE, REG_V0, REG_R2, 8);
    theEmitter->emitIns_R_R_I(INS_st2, EA_2BYTE, REG_V3, REG_R5, 7);
    theEmitter->emitIns_R_R_I(INS_st2, EA_4BYTE, REG_V6, REG_R8, 2);
    theEmitter->emitIns_R_R_I(INS_st2, EA_8BYTE, REG_V9, REG_R11, 1);

    // st3 {Vt, Vt2, Vt3}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_st3, EA_1BYTE, REG_V0, REG_R3, 9);
    theEmitter->emitIns_R_R_I(INS_st3, EA_2BYTE, REG_V4, REG_R7, 0);
    theEmitter->emitIns_R_R_I(INS_st3, EA_4BYTE, REG_V8, REG_R11, 3);
    theEmitter->emitIns_R_R_I(INS_st3, EA_8BYTE, REG_V12, REG_R15, 0);

    // st4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP]
    theEmitter->emitIns_R_R_I(INS_st4, EA_1BYTE, REG_V0, REG_R4, 10);
    theEmitter->emitIns_R_R_I(INS_st4, EA_2BYTE, REG_V5, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_st4, EA_4BYTE, REG_V10, REG_R14, 0);
    theEmitter->emitIns_R_R_I(INS_st4, EA_8BYTE, REG_V15, REG_R19, 1);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_ld1, EA_1BYTE, REG_V0, REG_R1, REG_R2, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld1, EA_2BYTE, REG_V3, REG_R4, REG_R5, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld1, EA_4BYTE, REG_V6, REG_R7, REG_R8, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld1, EA_8BYTE, REG_V9, REG_R10, REG_R11, 0, INS_OPTS_POST_INDEX);

    // ld2 {Vt, Vt2}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_ld2, EA_1BYTE, REG_V0, REG_R2, REG_R3, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld2, EA_2BYTE, REG_V4, REG_R6, REG_R7, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld2, EA_4BYTE, REG_V8, REG_R10, REG_R11, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld2, EA_8BYTE, REG_V12, REG_R14, REG_R15, 1, INS_OPTS_POST_INDEX);

    // ld3 {Vt, Vt2, Vt3}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_ld3, EA_1BYTE, REG_V0, REG_R3, REG_R4, 5, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld3, EA_2BYTE, REG_V5, REG_R8, REG_R9, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld3, EA_4BYTE, REG_V10, REG_R13, REG_R14, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld3, EA_8BYTE, REG_V15, REG_R18, REG_R19, 0, INS_OPTS_POST_INDEX);

    // ld4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_ld4, EA_1BYTE, REG_V0, REG_R4, REG_R5, 6, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld4, EA_2BYTE, REG_V6, REG_R10, REG_R11, 5, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld4, EA_4BYTE, REG_V12, REG_R16, REG_R17, 0, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ld4, EA_8BYTE, REG_V18, REG_R22, REG_R23, 1, INS_OPTS_POST_INDEX);

    // st1 {Vt}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_st1, EA_1BYTE, REG_V0, REG_R1, REG_R2, 7, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st1, EA_2BYTE, REG_V3, REG_R4, REG_R5, 6, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st1, EA_4BYTE, REG_V6, REG_R7, REG_R8, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st1, EA_8BYTE, REG_V9, REG_R10, REG_R11, 0, INS_OPTS_POST_INDEX);

    // st2 {Vt, Vt2}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_st2, EA_1BYTE, REG_V0, REG_R2, REG_R3, 8, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st2, EA_2BYTE, REG_V4, REG_R6, REG_R7, 7, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st2, EA_4BYTE, REG_V8, REG_R10, REG_R11, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st2, EA_8BYTE, REG_V12, REG_R14, REG_R15, 1, INS_OPTS_POST_INDEX);

    // st3 {Vt, Vt2, Vt3}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_st3, EA_1BYTE, REG_V0, REG_R3, REG_R4, 9, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st3, EA_2BYTE, REG_V5, REG_R8, REG_R9, 0, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st3, EA_4BYTE, REG_V10, REG_R13, REG_R14, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st3, EA_8BYTE, REG_V15, REG_R18, REG_R19, 0, INS_OPTS_POST_INDEX);

    // st4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP], Xm
    theEmitter->emitIns_R_R_R_I(INS_st4, EA_1BYTE, REG_V0, REG_R4, REG_R5, 10, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st4, EA_2BYTE, REG_V6, REG_R10, REG_R11, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st4, EA_4BYTE, REG_V12, REG_R16, REG_R17, 0, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_st4, EA_8BYTE, REG_V18, REG_R22, REG_R23, 1, INS_OPTS_POST_INDEX);

    //
    // Loads to and Stores from one, two, three, or four SIMD&FP registers
    //

    genDefineTempLabel(genCreateTempLabel());

    // ld1 {Vt}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_ld1, EA_1BYTE, REG_V0, REG_R1, 3, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld1, EA_2BYTE, REG_V2, REG_R3, 2, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld1, EA_4BYTE, REG_V4, REG_R5, 1, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld1, EA_8BYTE, REG_V6, REG_R7, 0, 8, INS_OPTS_POST_INDEX);

    // ld2 {Vt, Vt2}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_ld2, EA_1BYTE, REG_V0, REG_R2, 4, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld2, EA_2BYTE, REG_V3, REG_R5, 3, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld2, EA_4BYTE, REG_V6, REG_R8, 2, 8, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld2, EA_8BYTE, REG_V9, REG_R11, 1, 16, INS_OPTS_POST_INDEX);

    // ld3 {Vt, Vt2, Vt3}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_ld3, EA_1BYTE, REG_V0, REG_R3, 5, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld3, EA_2BYTE, REG_V4, REG_R7, 4, 6, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld3, EA_4BYTE, REG_V8, REG_R11, 3, 12, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld3, EA_8BYTE, REG_V12, REG_R15, 0, 24, INS_OPTS_POST_INDEX);

    // ld4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_ld4, EA_1BYTE, REG_V0, REG_R4, 6, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld4, EA_2BYTE, REG_V5, REG_R9, 5, 8, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld4, EA_4BYTE, REG_V10, REG_R14, 0, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_ld4, EA_8BYTE, REG_V15, REG_R19, 1, 32, INS_OPTS_POST_INDEX);

    // st1 {Vt}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_st1, EA_1BYTE, REG_V0, REG_R1, 3, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st1, EA_2BYTE, REG_V2, REG_R3, 2, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st1, EA_4BYTE, REG_V4, REG_R5, 1, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st1, EA_8BYTE, REG_V6, REG_R7, 0, 8, INS_OPTS_POST_INDEX);

    // st2 {Vt, Vt2}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_st2, EA_1BYTE, REG_V0, REG_R2, 4, 2, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st2, EA_2BYTE, REG_V3, REG_R5, 3, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st2, EA_4BYTE, REG_V6, REG_R8, 2, 8, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st2, EA_8BYTE, REG_V9, REG_R11, 1, 16, INS_OPTS_POST_INDEX);

    // st3 {Vt, Vt2, Vt3}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_st3, EA_1BYTE, REG_V0, REG_R3, 5, 3, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st3, EA_2BYTE, REG_V4, REG_R7, 4, 6, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st3, EA_4BYTE, REG_V8, REG_R11, 3, 12, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st3, EA_8BYTE, REG_V12, REG_R15, 0, 24, INS_OPTS_POST_INDEX);

    // st4 {Vt, Vt2, Vt3, Vt4}[#index], [Xn|SP], #imm
    theEmitter->emitIns_R_R_I_I(INS_st4, EA_1BYTE, REG_V0, REG_R4, 6, 4, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st4, EA_2BYTE, REG_V5, REG_R9, 5, 8, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st4, EA_4BYTE, REG_V10, REG_R14, 0, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I_I(INS_st4, EA_8BYTE, REG_V15, REG_R19, 1, 32, INS_OPTS_POST_INDEX);

    //
    // Compares
    //

    genDefineTempLabel(genCreateTempLabel());

    // cmp reg, reg
    theEmitter->emitIns_R_R(INS_cmp, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_cmn, EA_8BYTE, REG_R8, REG_R9);

    // cmp reg, imm
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 4095);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 1 << 12);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 4095 << 12);

    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 4095);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 1 << 12);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 4095 << 12);

    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, -1);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, -0xfff);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0xffffffffff800000LL);

    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, -1);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, -0xfff);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0xffffffffff800000LL);

    // R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_R1, REG_R12);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_R2, REG_R13);
    theEmitter->emitIns_R_R(INS_rbit, EA_8BYTE, REG_R3, REG_R14);
    theEmitter->emitIns_R_R(INS_rev, EA_8BYTE, REG_R4, REG_R15);
    theEmitter->emitIns_R_R(INS_rev16, EA_8BYTE, REG_R5, REG_R0);
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_R6, REG_R1);

    theEmitter->emitIns_R_R(INS_cls, EA_4BYTE, REG_R7, REG_R2);
    theEmitter->emitIns_R_R(INS_clz, EA_4BYTE, REG_R8, REG_R3);
    theEmitter->emitIns_R_R(INS_rbit, EA_4BYTE, REG_R9, REG_R4);
    theEmitter->emitIns_R_R(INS_rev, EA_4BYTE, REG_R10, REG_R5);
    theEmitter->emitIns_R_R(INS_rev16, EA_4BYTE, REG_R11, REG_R6);

    //
    // R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // mov reg, imm(i16,hw)
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000000000001234);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000000043210000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000567800000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x8765000000000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFFFFFFFFFF1234);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFFFFFF4321FFFF);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFF5678FFFFFFFF);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x8765FFFFFFFFFFFF);

    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x00001234);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x87650000);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0xFFFF1234);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x4567FFFF);

    // mov reg, imm(N,r,s)
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x00FFFFF000000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x6666666666666666);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_SP, 0x7FFF00007FFF0000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x5555555555555555);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xE003E003E003E003);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0707070707070707);

    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x00FFFFF0);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x66666666);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x03FFC000);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x55555555);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0xE003E003);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x07070707);

    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0xE003E003E003E003);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x00FFFFF000000000);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x6666666666666666);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x0707070707070707);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x7FFF00007FFF0000);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x5555555555555555);

    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0xE003E003);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x00FFFFF0);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x66666666);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x07070707);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0xFFF00000);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x55555555);

    //
    // R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    // tst reg, reg
    theEmitter->emitIns_R_R(INS_tst, EA_8BYTE, REG_R7, REG_R10);

    // mov reg, reg
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_R7, REG_R10, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_R8, REG_SP, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_SP, REG_R9, /* canSkip */ false);

    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_R4, REG_R12);
    theEmitter->emitIns_R_R(INS_negs, EA_8BYTE, REG_R3, REG_R13);

    theEmitter->emitIns_Mov(INS_mov, EA_4BYTE, REG_R7, REG_R10, /* canSkip */ false);
    theEmitter->emitIns_R_R(INS_mvn, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_neg, EA_4BYTE, REG_R4, REG_R12);
    theEmitter->emitIns_R_R(INS_negs, EA_4BYTE, REG_R3, REG_R13);

    theEmitter->emitIns_Mov(INS_sxtb, EA_8BYTE, REG_R7, REG_R10, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_sxth, EA_8BYTE, REG_R5, REG_R11, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_sxtw, EA_8BYTE, REG_R4, REG_R12, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_uxtb, EA_8BYTE, REG_R3, REG_R13, /* canSkip */ false); // map to Wt
    theEmitter->emitIns_Mov(INS_uxth, EA_8BYTE, REG_R2, REG_R14, /* canSkip */ false); // map to Wt

    theEmitter->emitIns_Mov(INS_sxtb, EA_4BYTE, REG_R7, REG_R10, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_sxth, EA_4BYTE, REG_R5, REG_R11, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_uxtb, EA_4BYTE, REG_R3, REG_R13, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_uxth, EA_4BYTE, REG_R2, REG_R14, /* canSkip */ false);

    //
    // R_I_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // mov reg, imm(i16,hw)
    theEmitter->emitIns_R_I_I(INS_mov, EA_8BYTE, REG_R8, 0x1234, 0, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_mov, EA_8BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);

    theEmitter->emitIns_R_I_I(INS_movk, EA_8BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movn, EA_8BYTE, REG_R8, 0x5678, 32, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movz, EA_8BYTE, REG_R8, 0x8765, 48, INS_OPTS_LSL);

    theEmitter->emitIns_R_I_I(INS_movk, EA_4BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movn, EA_4BYTE, REG_R8, 0x5678, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movz, EA_4BYTE, REG_R8, 0x8765, 16, INS_OPTS_LSL);

    //
    // R_R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_I(INS_lsl, EA_8BYTE, REG_R0, REG_R0, 1);
    theEmitter->emitIns_R_R_I(INS_lsl, EA_4BYTE, REG_R9, REG_R3, 18);
    theEmitter->emitIns_R_R_I(INS_lsr, EA_8BYTE, REG_R7, REG_R0, 37);
    theEmitter->emitIns_R_R_I(INS_lsr, EA_4BYTE, REG_R0, REG_R1, 2);
    theEmitter->emitIns_R_R_I(INS_asr, EA_8BYTE, REG_R2, REG_R3, 53);
    theEmitter->emitIns_R_R_I(INS_asr, EA_4BYTE, REG_R9, REG_R3, 18);

    theEmitter->emitIns_R_R_I(INS_and, EA_8BYTE, REG_R2, REG_R3, 0x5555555555555555);
    theEmitter->emitIns_R_R_I(INS_ands, EA_8BYTE, REG_R1, REG_R5, 0x6666666666666666);
    theEmitter->emitIns_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, 0x0707070707070707);
    theEmitter->emitIns_R_R_I(INS_orr, EA_8BYTE, REG_SP, REG_R3, 0xFFFC000000000000);
    theEmitter->emitIns_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, 0xE003E003);

    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 31);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 32);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 63);

    theEmitter->emitIns_R_R_I(INS_ror, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ror, EA_4BYTE, REG_R8, REG_R9, 31);

    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    //
    // R_R_I cmp/txt
    //

    // cmp
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0);

    // CMP (shifted register)
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 33, INS_OPTS_ASR);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 21, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 22, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 23, INS_OPTS_ASR);

    // TST (shifted register)
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 33, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 34, INS_OPTS_ROR);

    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 21, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 22, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 23, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 24, INS_OPTS_ROR);

    // CMP (extended register)
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTW); // "cmp x8, x9, UXTW"; msdis
                                                                                    // disassembles this "cmp x8,x9",
                                                                                    // which looks like an msdis issue.
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTX);

    // CMP 64-bit (extended register) and left shift
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 2, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 3, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 4, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 2, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 3, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 4, INS_OPTS_SXTX);

    // CMP 32-bit (extended register) and left shift
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 2, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 4, INS_OPTS_UXTW);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 2, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 4, INS_OPTS_SXTW);

    //
    // R_R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_lsl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsr, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asr, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ror, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adc, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adcs, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbc, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbcs, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_udiv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sdiv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mneg, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smull, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smnegl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smulh, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umull, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umnegl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umulh, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lslv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsrv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asrv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_rorv, EA_8BYTE, REG_R8, REG_R9, REG_R10);

    theEmitter->emitIns_R_R_R(INS_lsl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsr, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asr, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ror, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adc, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adcs, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbc, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbcs, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_udiv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sdiv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mul, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mneg, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smull, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smnegl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smulh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umull, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umnegl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umulh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lslv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsrv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asrv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_rorv, EA_4BYTE, REG_R8, REG_R9, REG_R10);

    //
    // ARMv8.1 LSE Atomics
    //
    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_casb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casab, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casalb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_caslb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_cash, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casah, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casalh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_caslh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_cas, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casa, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casal, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_cas, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casa, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casal, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_casl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddab, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddalb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddlb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddah, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddalh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddlh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldadd, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldadda, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddal, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldadd, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldadda, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddal, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldclral, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldclral, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldsetal, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldsetal, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ldaddl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpab, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpalb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swplb, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swph, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpah, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpalh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swplh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swp, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpa, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpal, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swp, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpa, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpal, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_swpl, EA_8BYTE, REG_R8, REG_R9, REG_R10);

    theEmitter->emitIns_R_R(INS_staddb, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_staddlb, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_staddh, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_staddlh, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_stadd, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_staddl, EA_4BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_stadd, EA_8BYTE, REG_R8, REG_R10);
    theEmitter->emitIns_R_R(INS_staddl, EA_8BYTE, REG_R8, REG_R10);

    //
    // R_R_I_I
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_I_I(INS_sbfm, EA_8BYTE, REG_R2, REG_R3, 4, 39);
    theEmitter->emitIns_R_R_I_I(INS_bfm, EA_8BYTE, REG_R1, REG_R5, 20, 23);
    theEmitter->emitIns_R_R_I_I(INS_ubfm, EA_8BYTE, REG_R8, REG_R9, 36, 7);

    theEmitter->emitIns_R_R_I_I(INS_sbfiz, EA_8BYTE, REG_R2, REG_R3, 7, 37);
    theEmitter->emitIns_R_R_I_I(INS_bfi, EA_8BYTE, REG_R1, REG_R5, 23, 21);
    theEmitter->emitIns_R_R_I_I(INS_ubfiz, EA_8BYTE, REG_R8, REG_R9, 39, 5);

    theEmitter->emitIns_R_R_I_I(INS_sbfx, EA_8BYTE, REG_R2, REG_R3, 10, 24);
    theEmitter->emitIns_R_R_I_I(INS_bfxil, EA_8BYTE, REG_R1, REG_R5, 26, 16);
    theEmitter->emitIns_R_R_I_I(INS_ubfx, EA_8BYTE, REG_R8, REG_R9, 42, 8);

    theEmitter->emitIns_R_R_I_I(INS_sbfm, EA_4BYTE, REG_R2, REG_R3, 4, 19);
    theEmitter->emitIns_R_R_I_I(INS_bfm, EA_4BYTE, REG_R1, REG_R5, 10, 13);
    theEmitter->emitIns_R_R_I_I(INS_ubfm, EA_4BYTE, REG_R8, REG_R9, 16, 7);

    theEmitter->emitIns_R_R_I_I(INS_sbfiz, EA_4BYTE, REG_R2, REG_R3, 5, 17);
    theEmitter->emitIns_R_R_I_I(INS_bfi, EA_4BYTE, REG_R1, REG_R5, 13, 11);
    theEmitter->emitIns_R_R_I_I(INS_ubfiz, EA_4BYTE, REG_R8, REG_R9, 19, 5);

    theEmitter->emitIns_R_R_I_I(INS_sbfx, EA_4BYTE, REG_R2, REG_R3, 3, 14);
    theEmitter->emitIns_R_R_I_I(INS_bfxil, EA_4BYTE, REG_R1, REG_R5, 11, 9);
    theEmitter->emitIns_R_R_I_I(INS_ubfx, EA_4BYTE, REG_R8, REG_R9, 22, 8);

    //
    // R_R_R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // ADD (extended register)
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTX);

    // ADD (extended register) and left shift
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTX);

    // ADD (shifted register)
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 33, INS_OPTS_ASR);

    // EXTR (extract field from register pair)
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 1);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 31);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 32);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 63);

    theEmitter->emitIns_R_R_R_I(INS_extr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 1);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 31);

    // SUB (extended register)
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTX);

    // SUB (extended register) and left shift
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTX);

    // SUB (shifted register)
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 27, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 28, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 29, INS_OPTS_ASR);

    // bit operations
    theEmitter->emitIns_R_R_R_I(INS_and, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_8BYTE, REG_R8, REG_R9, REG_R10, 1, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_8BYTE, REG_R8, REG_R9, REG_R10, 2, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, REG_R10, 3, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_ROR);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_8BYTE, REG_R8, REG_R9, REG_R10, 5, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_8BYTE, REG_R8, REG_R9, REG_R10, 6, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_8BYTE, REG_R8, REG_R9, REG_R10, 7, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8, INS_OPTS_ROR);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_4BYTE, REG_R8, REG_R9, REG_R10, 1, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, REG_R10, 2, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_4BYTE, REG_R8, REG_R9, REG_R10, 3, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_ROR);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_4BYTE, REG_R8, REG_R9, REG_R10, 5, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_4BYTE, REG_R8, REG_R9, REG_R10, 6, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_4BYTE, REG_R8, REG_R9, REG_R10, 7, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_4BYTE, REG_R8, REG_R9, REG_R10, 8, INS_OPTS_ROR);

    //
    // R_R_R_I  -- load/store pair
    //

    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8);

    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 8);

    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    // SP and ZR tests
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_ZR, REG_R1, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R0, REG_ZR, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_R1, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R0, REG_ZR, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_SP, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_R8, 16, INS_OPTS_PRE_INDEX);

    //
    // R_R_R_Ext    -- load/store shifted/extend
    //

    genDefineTempLabel(genCreateTempLabel());

    // LDR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

    // STR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

    //
    // R_R_R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R_R(INS_madd, EA_4BYTE, REG_R0, REG_R12, REG_R27, REG_R10);
    theEmitter->emitIns_R_R_R_R(INS_msub, EA_4BYTE, REG_R1, REG_R13, REG_R28, REG_R11);
    theEmitter->emitIns_R_R_R_R(INS_smaddl, EA_4BYTE, REG_R2, REG_R14, REG_R0, REG_R12);
    theEmitter->emitIns_R_R_R_R(INS_smsubl, EA_4BYTE, REG_R3, REG_R15, REG_R1, REG_R13);
    theEmitter->emitIns_R_R_R_R(INS_umaddl, EA_4BYTE, REG_R4, REG_R19, REG_R2, REG_R14);
    theEmitter->emitIns_R_R_R_R(INS_umsubl, EA_4BYTE, REG_R5, REG_R20, REG_R3, REG_R15);

    theEmitter->emitIns_R_R_R_R(INS_madd, EA_8BYTE, REG_R6, REG_R21, REG_R4, REG_R19);
    theEmitter->emitIns_R_R_R_R(INS_msub, EA_8BYTE, REG_R7, REG_R22, REG_R5, REG_R20);
    theEmitter->emitIns_R_R_R_R(INS_smaddl, EA_8BYTE, REG_R8, REG_R23, REG_R6, REG_R21);
    theEmitter->emitIns_R_R_R_R(INS_smsubl, EA_8BYTE, REG_R9, REG_R24, REG_R7, REG_R22);
    theEmitter->emitIns_R_R_R_R(INS_umaddl, EA_8BYTE, REG_R10, REG_R25, REG_R8, REG_R23);
    theEmitter->emitIns_R_R_R_R(INS_umsubl, EA_8BYTE, REG_R11, REG_R26, REG_R9, REG_R24);

    // R_COND
    //

    // cset reg, cond
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R9, INS_COND_EQ); // eq
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R8, INS_COND_NE); // ne
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R7, INS_COND_HS); // hs
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R6, INS_COND_LO); // lo
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R5, INS_COND_MI); // mi
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R4, INS_COND_PL); // pl
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R3, INS_COND_VS); // vs
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R2, INS_COND_VC); // vc
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R1, INS_COND_HI); // hi
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R0, INS_COND_LS); // ls
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R9, INS_COND_GE); // ge
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R8, INS_COND_LT); // lt
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R7, INS_COND_GT); // gt
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R6, INS_COND_LE); // le

    // csetm reg, cond
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R9, INS_COND_EQ); // eq
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R8, INS_COND_NE); // ne
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R7, INS_COND_HS); // hs
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R6, INS_COND_LO); // lo
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R5, INS_COND_MI); // mi
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R4, INS_COND_PL); // pl
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R3, INS_COND_VS); // vs
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R2, INS_COND_VC); // vc
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R1, INS_COND_HI); // hi
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R0, INS_COND_LS); // ls
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R9, INS_COND_GE); // ge
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R8, INS_COND_LT); // lt
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R7, INS_COND_GT); // gt
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R6, INS_COND_LE); // le

    // R_R_COND
    //

    // cinc reg, reg, cond
    // cinv reg, reg, cond
    // cneg reg, reg, cond
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R0, REG_R4, INS_COND_EQ); // eq
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R1, REG_R5, INS_COND_NE); // ne
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_4BYTE, REG_R2, REG_R6, INS_COND_HS); // hs
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R3, REG_R7, INS_COND_LO); // lo
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R4, REG_R8, INS_COND_MI); // mi
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R5, REG_R9, INS_COND_PL); // pl
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R6, REG_R0, INS_COND_VS); // vs
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R7, REG_R1, INS_COND_VC); // vc
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R8, REG_R2, INS_COND_HI); // hi
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_4BYTE, REG_R9, REG_R3, INS_COND_LS); // ls
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R0, REG_R4, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R2, REG_R5, INS_COND_LT); // lt
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_4BYTE, REG_R2, REG_R6, INS_COND_GT); // gt
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_8BYTE, REG_R3, REG_R7, INS_COND_LE); // le

    // R_R_R_COND
    //

    // csel  reg, reg, reg, cond
    // csinc reg, reg, reg, cond
    // csinv reg, reg, reg, cond
    // csneg reg, reg, reg, cond
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_8BYTE, REG_R0, REG_R4, REG_R8, INS_COND_EQ);  // eq
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_4BYTE, REG_R1, REG_R5, REG_R9, INS_COND_NE); // ne
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_4BYTE, REG_R2, REG_R6, REG_R0, INS_COND_HS); // hs
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_8BYTE, REG_R3, REG_R7, REG_R1, INS_COND_LO); // lo
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_4BYTE, REG_R4, REG_R8, REG_R2, INS_COND_MI);  // mi
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_8BYTE, REG_R5, REG_R9, REG_R3, INS_COND_PL); // pl
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_8BYTE, REG_R6, REG_R0, REG_R4, INS_COND_VS); // vs
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_4BYTE, REG_R7, REG_R1, REG_R5, INS_COND_VC); // vc
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_8BYTE, REG_R8, REG_R2, REG_R6, INS_COND_HI);  // hi
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_4BYTE, REG_R9, REG_R3, REG_R7, INS_COND_LS); // ls
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_4BYTE, REG_R0, REG_R4, REG_R8, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_8BYTE, REG_R2, REG_R5, REG_R9, INS_COND_LT); // lt
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_4BYTE, REG_R2, REG_R6, REG_R0, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_8BYTE, REG_R3, REG_R7, REG_R1, INS_COND_LE); // le

    // R_R_FLAGS_COND
    //

    // ccmp reg1, reg2, nzcv, cond
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, REG_R3, INS_FLAGS_V, INS_COND_EQ);    // eq
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, REG_R2, INS_FLAGS_C, INS_COND_NE);    // ne
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, REG_R1, INS_FLAGS_Z, INS_COND_HS);    // hs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, REG_R0, INS_FLAGS_N, INS_COND_LO);    // lo
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, REG_R3, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, REG_R2, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, REG_R1, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, REG_R0, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, REG_R3, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, REG_R2, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, REG_R1, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, REG_R0, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, REG_R3, INS_FLAGS_NZC, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, REG_R2, INS_FLAGS_NZCV, INS_COND_LE); // le

    // ccmp reg1, imm, nzcv, cond
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, 3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, 2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, 1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, 0, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, 31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, 28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, 25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, 22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, 19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, 16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, 13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, 10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, 7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, 4, INS_FLAGS_NZCV, INS_COND_LE);  // le

    // ccmp reg1, imm, nzcv, cond  -- encoded as ccmn
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, -3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, -2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, -1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, -5, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, -31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, -28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, -25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, -22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, -19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, -16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, -13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, -10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, -7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, -4, INS_FLAGS_NZCV, INS_COND_LE);  // le

    // ccmn reg1, reg2, nzcv, cond
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R9, REG_R3, INS_FLAGS_V, INS_COND_EQ);    // eq
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R8, REG_R2, INS_FLAGS_C, INS_COND_NE);    // ne
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R7, REG_R1, INS_FLAGS_Z, INS_COND_HS);    // hs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R6, REG_R0, INS_FLAGS_N, INS_COND_LO);    // lo
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R5, REG_R3, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R4, REG_R2, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R3, REG_R1, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R2, REG_R0, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R1, REG_R3, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R0, REG_R2, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R9, REG_R1, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R8, REG_R0, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R7, REG_R3, INS_FLAGS_NZC, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R6, REG_R2, INS_FLAGS_NZCV, INS_COND_LE); // le

    // ccmn reg1, imm, nzcv, cond
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R9, 3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R8, 2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R7, 1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R6, 0, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R5, 31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R4, 28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R3, 25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R2, 22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R1, 19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R0, 16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R9, 13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R8, 10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R7, 7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R6, 4, INS_FLAGS_NZCV, INS_COND_LE);  // le

    //
    // Branch to register
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R(INS_br, EA_PTRSIZE, REG_R8);
    theEmitter->emitIns_R(INS_ret, EA_PTRSIZE, REG_R8);
    theEmitter->emitIns_R(INS_ret, EA_PTRSIZE, REG_LR);

    //
    // Misc
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_I(INS_BREAKPOINT, EA_PTRSIZE, 0);
    theEmitter->emitIns_I(INS_BREAKPOINT, EA_PTRSIZE, 65535);

    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_OSHLD);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_OSHST);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_OSH);

    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_NSHLD);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_NSHST);
    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_NSH);

    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_ISHLD);
    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_ISHST);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_ISH);

    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_LD);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_ST);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_SY);
}

/*****************************************************************************
 * Unit tests for AdvSimd instructions.
 */
void CodeGen::genArm64EmitterUnitTestsAdvSimd()
{
    emitter* theEmitter = GetEmitter();

    ////////////////////////////////////////////////////////////////////////////////
    //
    // SIMD and Floating point
    //
    ////////////////////////////////////////////////////////////////////////////////

    //
    // Load/Stores vector register
    //

    genDefineTempLabel(genCreateTempLabel());

    // ldr/str Vt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_8BYTE, REG_V1, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_8BYTE, REG_V2, REG_R8);
    theEmitter->emitIns_R_R(INS_ldr, EA_4BYTE, REG_V3, REG_R7);
    theEmitter->emitIns_R_R(INS_str, EA_4BYTE, REG_V4, REG_R6);
    theEmitter->emitIns_R_R(INS_ldr, EA_2BYTE, REG_V5, REG_R5);
    theEmitter->emitIns_R_R(INS_str, EA_2BYTE, REG_V6, REG_R4);
    theEmitter->emitIns_R_R(INS_ldr, EA_1BYTE, REG_V7, REG_R3);
    theEmitter->emitIns_R_R(INS_str, EA_1BYTE, REG_V8, REG_R2);
    theEmitter->emitIns_R_R(INS_ldr, EA_16BYTE, REG_V9, REG_R1);
    theEmitter->emitIns_R_R(INS_str, EA_16BYTE, REG_V10, REG_R0);

    // ldr/str Vt, [reg+cns]        -- scaled
    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 8);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 16);

    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V7, REG_R10, 1);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V7, REG_R10, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V7, REG_R10, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V7, REG_R10, 8);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V7, REG_R10, 16);

    // ldr/str Vt, [reg],cns        -- post-indexed (unscaled)
    // ldr/str Vt, [reg+cns]!       -- post-indexed (unscaled)
    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);

    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_I(INS_str, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);

    theEmitter->emitIns_R_R_I(INS_str, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_I(INS_ldur, EA_1BYTE, REG_V8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_2BYTE, REG_V8, REG_R9, 3);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_4BYTE, REG_V8, REG_R9, 5);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_V8, REG_R9, 9);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_16BYTE, REG_V8, REG_R9, 17);

    theEmitter->emitIns_R_R_I(INS_stur, EA_1BYTE, REG_V7, REG_R10, 2);
    theEmitter->emitIns_R_R_I(INS_stur, EA_2BYTE, REG_V7, REG_R10, 3);
    theEmitter->emitIns_R_R_I(INS_stur, EA_4BYTE, REG_V7, REG_R10, 5);
    theEmitter->emitIns_R_R_I(INS_stur, EA_8BYTE, REG_V7, REG_R10, 9);
    theEmitter->emitIns_R_R_I(INS_stur, EA_16BYTE, REG_V7, REG_R10, 17);

    // load/store pair
    theEmitter->emitIns_R_R_R(INS_ldnp, EA_8BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_V1, REG_V2, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_V2, REG_V3, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_V3, REG_V4, REG_R10, 24);

    theEmitter->emitIns_R_R_R(INS_ldnp, EA_4BYTE, REG_V4, REG_V5, REG_SP);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_V5, REG_V6, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_V6, REG_V7, REG_SP, 4);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_V7, REG_V8, REG_SP, 12);

    theEmitter->emitIns_R_R_R(INS_ldnp, EA_16BYTE, REG_V8, REG_V9, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_16BYTE, REG_V9, REG_V10, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_16BYTE, REG_V10, REG_V11, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_16BYTE, REG_V11, REG_V12, REG_R10, 48);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_8BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V2, REG_V3, REG_SP, 8);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V3, REG_V4, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V4, REG_V5, REG_R10, 24, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V5, REG_V6, REG_SP, 32, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V6, REG_V7, REG_SP, 40, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V7, REG_V8, REG_R10, 48, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_4BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V2, REG_V3, REG_SP, 4);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V3, REG_V4, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V4, REG_V5, REG_R10, 12, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V5, REG_V6, REG_SP, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V6, REG_V7, REG_SP, 20, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V7, REG_V8, REG_R10, 24, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_16BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V2, REG_V3, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V3, REG_V4, REG_R10, 32);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V4, REG_V5, REG_R10, 48, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V5, REG_V6, REG_SP, 64, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V6, REG_V7, REG_SP, 80, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V7, REG_V8, REG_R10, 96, INS_OPTS_PRE_INDEX);

    // LDR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 4);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V1, REG_R7, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V2, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V4, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_UXTX);

    //
    // R_R   mov and aliases for mov
    //

    // mov vector to vector
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_V0, REG_V1, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_16BYTE, REG_V2, REG_V3, /* canSkip */ false);

    theEmitter->emitIns_Mov(INS_mov, EA_4BYTE, REG_V12, REG_V13, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_2BYTE, REG_V14, REG_V15, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_1BYTE, REG_V16, REG_V17, /* canSkip */ false);

    // mov vector to general
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_R0, REG_V4, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_4BYTE, REG_R1, REG_V5, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_2BYTE, REG_R2, REG_V6, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_1BYTE, REG_R3, REG_V7, /* canSkip */ false);

    // mov general to vector
    theEmitter->emitIns_Mov(INS_mov, EA_8BYTE, REG_V8, REG_R4, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_4BYTE, REG_V9, REG_R5, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_2BYTE, REG_V10, REG_R6, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_mov, EA_1BYTE, REG_V11, REG_R7, /* canSkip */ false);

    // mov vector[index] to vector
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_V2, REG_V3, 3);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_V4, REG_V5, 7);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_V6, REG_V7, 15);

    // mov to general from vector[index]
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_R8, REG_V16, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_R9, REG_V17, 2);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_R10, REG_V18, 3);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_R11, REG_V19, 4);

    // mov to vector[index] from general
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_V20, REG_R12, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_V21, REG_R13, 2);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_V22, REG_R14, 6);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_V23, REG_R15, 8);

    // mov vector[index] to vector[index2]
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_8BYTE, REG_V8, REG_V9, 1, 0);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_4BYTE, REG_V10, REG_V11, 2, 1);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_2BYTE, REG_V12, REG_V13, 5, 2);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_1BYTE, REG_V14, REG_V15, 12, 3);

    //////////////////////////////////////////////////////////////////////////////////

    // mov/dup scalar
    theEmitter->emitIns_R_R_I(INS_dup, EA_8BYTE, REG_V24, REG_V25, 1);
    theEmitter->emitIns_R_R_I(INS_dup, EA_4BYTE, REG_V26, REG_V27, 3);
    theEmitter->emitIns_R_R_I(INS_dup, EA_2BYTE, REG_V28, REG_V29, 7);
    theEmitter->emitIns_R_R_I(INS_dup, EA_1BYTE, REG_V30, REG_V31, 15);

    // mov/ins vector element
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_8BYTE, REG_V0, REG_V1, 0, 1);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_4BYTE, REG_V2, REG_V3, 2, 2);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_2BYTE, REG_V4, REG_V5, 4, 3);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_1BYTE, REG_V6, REG_V7, 8, 4);

    // umov to general from vector element
    theEmitter->emitIns_R_R_I(INS_umov, EA_8BYTE, REG_R0, REG_V8, 1);
    theEmitter->emitIns_R_R_I(INS_umov, EA_4BYTE, REG_R1, REG_V9, 2);
    theEmitter->emitIns_R_R_I(INS_umov, EA_2BYTE, REG_R2, REG_V10, 4);
    theEmitter->emitIns_R_R_I(INS_umov, EA_1BYTE, REG_R3, REG_V11, 8);

    // ins to vector element from general
    theEmitter->emitIns_R_R_I(INS_ins, EA_8BYTE, REG_V12, REG_R4, 1);
    theEmitter->emitIns_R_R_I(INS_ins, EA_4BYTE, REG_V13, REG_R5, 3);
    theEmitter->emitIns_R_R_I(INS_ins, EA_2BYTE, REG_V14, REG_R6, 7);
    theEmitter->emitIns_R_R_I(INS_ins, EA_1BYTE, REG_V15, REG_R7, 15);

    // smov to general from vector element
    theEmitter->emitIns_R_R_I(INS_smov, EA_4BYTE, REG_R5, REG_V17, 2);
    theEmitter->emitIns_R_R_I(INS_smov, EA_2BYTE, REG_R6, REG_V18, 4);
    theEmitter->emitIns_R_R_I(INS_smov, EA_1BYTE, REG_R7, REG_V19, 8);

    // ext extract vector from pair of vectors
    theEmitter->emitIns_R_R_R_I(INS_ext, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R_I(INS_ext, EA_8BYTE, REG_V4, REG_V5, REG_V6, 7, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R_I(INS_ext, EA_16BYTE, REG_V8, REG_V9, REG_V10, 11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R_I(INS_ext, EA_16BYTE, REG_V12, REG_V13, REG_V14, 15, INS_OPTS_16B);

    //
    // R_I   movi and mvni
    //

    // movi  imm8  (vector)
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V0, 0x00, INS_OPTS_8B);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V1, 0xFF, INS_OPTS_8B);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V2, 0x00, INS_OPTS_16B);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V3, 0xFF, INS_OPTS_16B);

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V4, 0x007F, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V5, 0x7F00, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V6, 0x003F, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V7, 0x3F00, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V8, 0x1F, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V9, 0x1F00, INS_OPTS_2S);      // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V10, 0x1F0000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V11, 0x1F000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V12, 0x1FFF, INS_OPTS_2S);   // MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V13, 0x1FFFFF, INS_OPTS_2S); // MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V14, 0x37, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V15, 0x3700, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V16, 0x370000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V17, 0x37000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V18, 0x37FF, INS_OPTS_4S);   // MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V19, 0x37FFFF, INS_OPTS_4S); // MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V20, 0xFF80, INS_OPTS_4H);  // mvni
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V21, 0xFFC0, INS_OPTS_8H); // mvni

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V22, 0xFFFFFFE0, INS_OPTS_2S);  // mvni
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V23, 0xFFFFF0FF, INS_OPTS_4S); // mvni LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V24, 0xFFF8FFFF, INS_OPTS_2S);  // mvni LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V25, 0xFCFFFFFF, INS_OPTS_4S); // mvni LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V26, 0xFFFFFE00, INS_OPTS_2S);  // mvni MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V27, 0xFFFC0000, INS_OPTS_4S); // mvni MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V28, 0x00FF00FF00FF00FF, INS_OPTS_1D);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V29, 0x00FFFF0000FFFF00, INS_OPTS_2D);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V30, 0xFF000000FF000000);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V31, 0x0, INS_OPTS_2D);

    // We were not encoding immediate of movi that was int.MaxValue or int.MaxValue / 2.
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V16, 0x7fffffff, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V16, 0x3fffffff, INS_OPTS_2S);

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V8, 0x42FF, INS_OPTS_2S);   // MSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V9, 0x42FFFF, INS_OPTS_2S); // MSL 16

    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V14, 0x5DFF, INS_OPTS_4S);   // MSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V15, 0x5DFFFF, INS_OPTS_4S); // MSL 16

    //
    // R_I   orr/bic vector immediate
    //

    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

    //
    // R_F   cmp/fmov immediate
    //

    // fmov  imm8  (scalar)
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V14, 1.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V15, -1.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V0, 2.0); // encodes imm8 == 0
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V16, 10.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V17, -10.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V18, 31); // Largest encodable value
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V19, -31);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V20, 1.25);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V21, -1.25);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V22, 0.125); // Smallest encodable value
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V23, -0.125);

    // fmov  imm8  (vector)
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V0, 2.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V24, 1.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V25, 1.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V26, 1.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V27, -10.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V28, -10.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V29, -10.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V30, 31.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V31, 31.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V0, 31.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V1, -0.125, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V2, -0.125, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V3, -0.125, INS_OPTS_2D);

    // fcmp with 0.0
    theEmitter->emitIns_R_F(INS_fcmp, EA_8BYTE, REG_V12, 0.0);
    theEmitter->emitIns_R_F(INS_fcmp, EA_4BYTE, REG_V13, 0.0);
    theEmitter->emitIns_R_F(INS_fcmpe, EA_8BYTE, REG_V14, 0.0);
    theEmitter->emitIns_R_F(INS_fcmpe, EA_4BYTE, REG_V15, 0.0);

    //
    // R_R   cmeq/fmov/fcmp/fcvt
    //

    // cmeq scalar
    theEmitter->emitIns_R_R(INS_cmeq, EA_8BYTE, REG_V0, REG_V1);

    // fmov to vector to vector
    theEmitter->emitIns_Mov(INS_fmov, EA_8BYTE, REG_V0, REG_V2, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_fmov, EA_4BYTE, REG_V1, REG_V3, /* canSkip */ false);

    // fmov to vector to general
    theEmitter->emitIns_Mov(INS_fmov, EA_8BYTE, REG_R0, REG_V4, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_fmov, EA_4BYTE, REG_R1, REG_V5, /* canSkip */ false);
    //    using the optional conversion specifier
    theEmitter->emitIns_Mov(INS_fmov, EA_8BYTE, REG_R2, REG_V6, /* canSkip */ false, INS_OPTS_D_TO_8BYTE);
    theEmitter->emitIns_Mov(INS_fmov, EA_4BYTE, REG_R3, REG_V7, /* canSkip */ false, INS_OPTS_S_TO_4BYTE);

    // fmov to general to vector
    theEmitter->emitIns_Mov(INS_fmov, EA_8BYTE, REG_V8, REG_R4, /* canSkip */ false);
    theEmitter->emitIns_Mov(INS_fmov, EA_4BYTE, REG_V9, REG_R5, /* canSkip */ false);
    //   using the optional conversion specifier
    theEmitter->emitIns_Mov(INS_fmov, EA_4BYTE, REG_V11, REG_R7, /* canSkip */ false, INS_OPTS_4BYTE_TO_S);
    theEmitter->emitIns_Mov(INS_fmov, EA_8BYTE, REG_V10, REG_R6, /* canSkip */ false, INS_OPTS_8BYTE_TO_D);

    // fcmp/fcmpe
    theEmitter->emitIns_R_R(INS_fcmp, EA_8BYTE, REG_V8, REG_V16);
    theEmitter->emitIns_R_R(INS_fcmp, EA_4BYTE, REG_V9, REG_V17);
    theEmitter->emitIns_R_R(INS_fcmpe, EA_8BYTE, REG_V10, REG_V18);
    theEmitter->emitIns_R_R(INS_fcmpe, EA_4BYTE, REG_V11, REG_V19);

    // fcvt
    theEmitter->emitIns_R_R(INS_fcvt, EA_8BYTE, REG_V24, REG_V25, INS_OPTS_S_TO_D); // Single to Double
    theEmitter->emitIns_R_R(INS_fcvt, EA_4BYTE, REG_V26, REG_V27, INS_OPTS_D_TO_S); // Double to Single

    theEmitter->emitIns_R_R(INS_fcvt, EA_4BYTE, REG_V1, REG_V2, INS_OPTS_H_TO_S);
    theEmitter->emitIns_R_R(INS_fcvt, EA_8BYTE, REG_V3, REG_V4, INS_OPTS_H_TO_D);

    theEmitter->emitIns_R_R(INS_fcvt, EA_2BYTE, REG_V5, REG_V6, INS_OPTS_S_TO_H);
    theEmitter->emitIns_R_R(INS_fcvt, EA_2BYTE, REG_V7, REG_V8, INS_OPTS_D_TO_H);

    //
    // R_R   floating point conversions
    //

    // fcvtas scalar
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_V2, REG_V3);

    // fcvtas scalar to general
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtas vector
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtau scalar
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_V2, REG_V3);

    // fcvtau scalar to general
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtau vector
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtms scalar
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_V2, REG_V3);

    // fcvtms scalar to general
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtms vector
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtmu scalar
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtmu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtmu vector
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtns scalar
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_V2, REG_V3);

    // fcvtns scalar to general
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtns vector
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtnu scalar
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtnu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtnu vector
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtps scalar
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_V2, REG_V3);

    // fcvtps scalar to general
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtps vector
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtpu scalar
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtpu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtpu vector
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtzs scalar
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_V2, REG_V3);

    // fcvtzs scalar to general
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtzs vector
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtzu scalar
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtzu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtzu vector
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // scvtf scalar
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V2, REG_V3);

    // scvtf scalar from general
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V4, REG_R0, INS_OPTS_4BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V5, REG_R1, INS_OPTS_8BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V6, REG_R2, INS_OPTS_4BYTE_TO_D);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V7, REG_R3, INS_OPTS_8BYTE_TO_D);

    // scvtf vector
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // ucvtf scalar
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V2, REG_V3);

    // ucvtf scalar from general
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V4, REG_R0, INS_OPTS_4BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V5, REG_R1, INS_OPTS_8BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V6, REG_R2, INS_OPTS_4BYTE_TO_D);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V7, REG_R3, INS_OPTS_8BYTE_TO_D);

    // ucvtf vector
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    //
    // R_R   floating point operations, one dest, one source
    //

    // fabs scalar
    theEmitter->emitIns_R_R(INS_fabs, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fabs, EA_8BYTE, REG_V2, REG_V3);

    // fabs vector
    theEmitter->emitIns_R_R(INS_fabs, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fabs, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fabs, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // fmaxp scalar
    theEmitter->emitIns_R_R(INS_fmaxp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fmaxp, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_2D);

    // fmaxnmp scalar
    theEmitter->emitIns_R_R(INS_fmaxnmp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fmaxnmp, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_2D);

    // fmaxnmv vector
    theEmitter->emitIns_R_R(INS_fmaxnmv, EA_16BYTE, REG_V0, REG_V1, INS_OPTS_4S);

    // fmaxv vector
    theEmitter->emitIns_R_R(INS_fmaxv, EA_16BYTE, REG_V0, REG_V1, INS_OPTS_4S);

    // fminp scalar
    theEmitter->emitIns_R_R(INS_fminp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fminp, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_2D);

    // fminnmp scalar
    theEmitter->emitIns_R_R(INS_fminnmp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fminnmp, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_2D);

    // fminnmv vector
    theEmitter->emitIns_R_R(INS_fminnmv, EA_16BYTE, REG_V0, REG_V1, INS_OPTS_4S);

    // fminv vector
    theEmitter->emitIns_R_R(INS_fminv, EA_16BYTE, REG_V0, REG_V1, INS_OPTS_4S);

    // fneg scalar
    theEmitter->emitIns_R_R(INS_fneg, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fneg, EA_8BYTE, REG_V2, REG_V3);

    // fneg vector
    theEmitter->emitIns_R_R(INS_fneg, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fneg, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fneg, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // fsqrt scalar
    theEmitter->emitIns_R_R(INS_fsqrt, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_8BYTE, REG_V2, REG_V3);

    // fsqrt vector
    theEmitter->emitIns_R_R(INS_fsqrt, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // faddp scalar
    theEmitter->emitIns_R_R(INS_faddp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_faddp, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_2D);

    // fcmeq Vd, Vn, #0.0
    theEmitter->emitIns_R_R(INS_fcmeq, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_fcmeq, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE

    // fcmge Vd, Vn, #0.0
    theEmitter->emitIns_R_R(INS_fcmge, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_fcmge, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE

    // fcmgt Vd, Vn, #0.0
    theEmitter->emitIns_R_R(INS_fcmgt, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_fcmgt, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE

    // fcmle Vd, Vn, #0.0
    theEmitter->emitIns_R_R(INS_fcmle, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_fcmle, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE

    // fcmlt Vd, Vn, #0.0
    theEmitter->emitIns_R_R(INS_fcmlt, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_fcmlt, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE

    // frecpe scalar
    theEmitter->emitIns_R_R(INS_frecpe, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_frecpe, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE
    theEmitter->emitIns_R_R(INS_frecpe, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frecpe, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frecpe, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frecpx scalar
    theEmitter->emitIns_R_R(INS_frecpx, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frecpx, EA_8BYTE, REG_V2, REG_V3);

    // frsqrte
    theEmitter->emitIns_R_R(INS_frsqrte, EA_4BYTE, REG_V0, REG_V1); // scalar 4BYTE
    theEmitter->emitIns_R_R(INS_frsqrte, EA_8BYTE, REG_V2, REG_V3); // scalar 8BYTE
    theEmitter->emitIns_R_R(INS_frsqrte, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frsqrte, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frsqrte, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // fcvtl{2} vector
    theEmitter->emitIns_R_R(INS_fcvtl, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_fcvtl2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_fcvtl, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtl2, EA_16BYTE, REG_V5, REG_V6, INS_OPTS_4S);

    // fcvtn{2} vector
    theEmitter->emitIns_R_R(INS_fcvtn, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_fcvtn2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_fcvtn, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtn2, EA_16BYTE, REG_V5, REG_V6, INS_OPTS_4S);

    // fcvtxn scalar
    theEmitter->emitIns_R_R(INS_fcvtxn, EA_4BYTE, REG_V0, REG_V1);

    // fcvtxn{2} vector
    theEmitter->emitIns_R_R(INS_fcvtxn, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtxn2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_4S);

    genDefineTempLabel(genCreateTempLabel());

    // abs scalar
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V2, REG_V3);

    // abs vector
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V16, REG_V17, INS_OPTS_2D);

    // addv vector
    theEmitter->emitIns_R_R(INS_addv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_addv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_addv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_addv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_addv, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // cnt vector
    theEmitter->emitIns_R_R(INS_cnt, EA_8BYTE, REG_V22, REG_V23, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_cnt, EA_16BYTE, REG_V24, REG_V25, INS_OPTS_16B);

    // cls vector
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // clz vector
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // mvn vector
    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_V4, REG_V5);
    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_mvn, EA_16BYTE, REG_V8, REG_V9);
    theEmitter->emitIns_R_R(INS_mvn, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_16B);

    // neg scalar
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V2, REG_V3);

    // neg vector
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V16, REG_V17, INS_OPTS_2D);

    // not vector (the same encoding as mvn)
    theEmitter->emitIns_R_R(INS_not, EA_8BYTE, REG_V12, REG_V13);
    theEmitter->emitIns_R_R(INS_not, EA_8BYTE, REG_V14, REG_V15, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_not, EA_16BYTE, REG_V16, REG_V17);
    theEmitter->emitIns_R_R(INS_not, EA_16BYTE, REG_V18, REG_V19, INS_OPTS_16B);

    // rbit vector
    theEmitter->emitIns_R_R(INS_rbit, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rbit, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);

    // rev16 vector
    theEmitter->emitIns_R_R(INS_rev16, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev16, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);

    // rev32 vector
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev32, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_rev32, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);

    // rev64 vector
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // sadalp vector
    theEmitter->emitIns_R_R(INS_sadalp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sadalp, EA_8BYTE, REG_V2, REG_V3, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sadalp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_sadalp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sadalp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sadalp, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // saddlp vector
    theEmitter->emitIns_R_R(INS_saddlp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_saddlp, EA_8BYTE, REG_V2, REG_V3, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_saddlp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_saddlp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_saddlp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_saddlp, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // saddlv vector
    theEmitter->emitIns_R_R(INS_saddlv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_saddlv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_saddlv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_saddlv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_saddlv, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // smaxv vector
    theEmitter->emitIns_R_R(INS_smaxv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_smaxv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_smaxv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_smaxv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_smaxv, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_4S);

    // sminv vector
    theEmitter->emitIns_R_R(INS_sminv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sminv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sminv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sminv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sminv, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_4S);

    // sqabs scalar
    theEmitter->emitIns_R_R(INS_sqabs, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqabs, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqabs, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqabs, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_NONE);

    // sqabs vector
    theEmitter->emitIns_R_R(INS_sqabs, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sqabs, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sqabs, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sqabs, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sqabs, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_sqabs, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_sqabs, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // sqneg scalar
    theEmitter->emitIns_R_R(INS_sqneg, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqneg, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqneg, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqneg, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_NONE);

    // sqneg vector
    theEmitter->emitIns_R_R(INS_sqneg, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sqneg, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sqneg, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sqneg, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sqneg, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_sqneg, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_sqneg, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // sqxtn scalar
    theEmitter->emitIns_R_R(INS_sqxtn, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqxtn, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqxtn, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqxtn vector
    theEmitter->emitIns_R_R(INS_sqxtn, EA_8BYTE, REG_V0, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sqxtn, EA_8BYTE, REG_V1, REG_V7, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sqxtn, EA_8BYTE, REG_V2, REG_V8, INS_OPTS_2S);

    // sqxtn2 vector
    theEmitter->emitIns_R_R(INS_sqxtn2, EA_16BYTE, REG_V3, REG_V9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sqxtn2, EA_16BYTE, REG_V4, REG_V10, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sqxtn2, EA_16BYTE, REG_V5, REG_V11, INS_OPTS_4S);

    // sqxtun scalar
    theEmitter->emitIns_R_R(INS_sqxtun, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqxtun, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_sqxtun, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqxtun vector
    theEmitter->emitIns_R_R(INS_sqxtun, EA_8BYTE, REG_V0, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sqxtun, EA_8BYTE, REG_V1, REG_V7, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sqxtun, EA_8BYTE, REG_V2, REG_V8, INS_OPTS_2S);

    // sqxtun2 vector
    theEmitter->emitIns_R_R(INS_sqxtun2, EA_16BYTE, REG_V3, REG_V9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sqxtun2, EA_16BYTE, REG_V4, REG_V10, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sqxtun2, EA_16BYTE, REG_V5, REG_V11, INS_OPTS_4S);

    // suqadd scalar
    theEmitter->emitIns_R_R(INS_suqadd, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_suqadd, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_suqadd, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_suqadd, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_NONE);

    // suqadd vector
    theEmitter->emitIns_R_R(INS_suqadd, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_suqadd, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_suqadd, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_suqadd, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_suqadd, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_suqadd, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_suqadd, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // uadalp vector
    theEmitter->emitIns_R_R(INS_uadalp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uadalp, EA_8BYTE, REG_V2, REG_V3, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uadalp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_uadalp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uadalp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uadalp, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // uaddlp vector
    theEmitter->emitIns_R_R(INS_uaddlp, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uaddlp, EA_8BYTE, REG_V2, REG_V3, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uaddlp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_uaddlp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uaddlp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uaddlp, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // uaddlv vector
    theEmitter->emitIns_R_R(INS_uaddlv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uaddlv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uaddlv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uaddlv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uaddlv, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // umaxv vector
    theEmitter->emitIns_R_R(INS_umaxv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_umaxv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_umaxv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_umaxv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_umaxv, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_4S);

    // uminv vector
    theEmitter->emitIns_R_R(INS_uminv, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uminv, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uminv, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uminv, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uminv, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_4S);

    // uqxtn scalar
    theEmitter->emitIns_R_R(INS_uqxtn, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_uqxtn, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_uqxtn, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);

    // uqxtn vector
    theEmitter->emitIns_R_R(INS_uqxtn, EA_8BYTE, REG_V0, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uqxtn, EA_8BYTE, REG_V1, REG_V7, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uqxtn, EA_8BYTE, REG_V2, REG_V8, INS_OPTS_2S);

    // uqxtn2 vector
    theEmitter->emitIns_R_R(INS_uqxtn2, EA_16BYTE, REG_V3, REG_V9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uqxtn2, EA_16BYTE, REG_V4, REG_V10, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uqxtn2, EA_16BYTE, REG_V5, REG_V11, INS_OPTS_4S);

    // urecpe vector
    theEmitter->emitIns_R_R(INS_urecpe, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_urecpe, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_4S);

    // ursqrte vector
    theEmitter->emitIns_R_R(INS_ursqrte, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ursqrte, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_4S);

    // usqadd scalar
    theEmitter->emitIns_R_R(INS_usqadd, EA_1BYTE, REG_V0, REG_V1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_usqadd, EA_2BYTE, REG_V2, REG_V3, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_usqadd, EA_4BYTE, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R(INS_usqadd, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_NONE);

    // usqadd vector
    theEmitter->emitIns_R_R(INS_usqadd, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_usqadd, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_usqadd, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_usqadd, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_usqadd, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_usqadd, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_usqadd, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // xtn vector
    theEmitter->emitIns_R_R(INS_xtn, EA_8BYTE, REG_V0, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_xtn, EA_8BYTE, REG_V1, REG_V7, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_xtn, EA_8BYTE, REG_V2, REG_V8, INS_OPTS_2S);

    // xtn2 vector
    theEmitter->emitIns_R_R(INS_xtn2, EA_16BYTE, REG_V3, REG_V9, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_xtn2, EA_16BYTE, REG_V4, REG_V10, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_xtn2, EA_16BYTE, REG_V5, REG_V11, INS_OPTS_4S);

    //
    // R_R   floating point round to int, one dest, one source
    //

    // frinta scalar
    theEmitter->emitIns_R_R(INS_frinta, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frinta, EA_8BYTE, REG_V2, REG_V3);

    // frinta vector
    theEmitter->emitIns_R_R(INS_frinta, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frinta, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frinta, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frinti scalar
    theEmitter->emitIns_R_R(INS_frinti, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frinti, EA_8BYTE, REG_V2, REG_V3);

    // frinti vector
    theEmitter->emitIns_R_R(INS_frinti, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frinti, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frinti, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintm scalar
    theEmitter->emitIns_R_R(INS_frintm, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintm, EA_8BYTE, REG_V2, REG_V3);

    // frintm vector
    theEmitter->emitIns_R_R(INS_frintm, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintm, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintm, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintn scalar
    theEmitter->emitIns_R_R(INS_frintn, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintn, EA_8BYTE, REG_V2, REG_V3);

    // frintn vector
    theEmitter->emitIns_R_R(INS_frintn, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintn, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintn, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintp scalar
    theEmitter->emitIns_R_R(INS_frintp, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintp, EA_8BYTE, REG_V2, REG_V3);

    // frintp vector
    theEmitter->emitIns_R_R(INS_frintp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintx scalar
    theEmitter->emitIns_R_R(INS_frintx, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintx, EA_8BYTE, REG_V2, REG_V3);

    // frintx vector
    theEmitter->emitIns_R_R(INS_frintx, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintx, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintx, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintz scalar
    theEmitter->emitIns_R_R(INS_frintz, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintz, EA_8BYTE, REG_V2, REG_V3);

    // frintz vector
    theEmitter->emitIns_R_R(INS_frintz, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintz, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintz, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    //
    // R_R_R   floating point operations, one dest, two source
    //

    genDefineTempLabel(genCreateTempLabel());

    // fadd
    theEmitter->emitIns_R_R_R(INS_fadd, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fadd, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fsub
    theEmitter->emitIns_R_R_R(INS_fsub, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fsub, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fdiv
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fmax
    theEmitter->emitIns_R_R_R(INS_fmax, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmax, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmax, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmax, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmax, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fmaxp
    theEmitter->emitIns_R_R_R(INS_fmaxp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmaxp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmaxp, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2D);

    // fmaxnm
    theEmitter->emitIns_R_R_R(INS_fmaxnm, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmaxnm, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmaxnm, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmaxnm, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmaxnm, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fmaxnmp vector
    theEmitter->emitIns_R_R_R(INS_fmaxnmp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmaxnmp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmaxnmp, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2D);

    // fmin
    theEmitter->emitIns_R_R_R(INS_fmin, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmin, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmin, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmin, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmin, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fminp
    theEmitter->emitIns_R_R_R(INS_fminp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fminp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fminp, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2D);

    // fminnm
    theEmitter->emitIns_R_R_R(INS_fminnm, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fminnm, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fminnm, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fminnm, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fminnm, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fminnmp vector
    theEmitter->emitIns_R_R_R(INS_fminnmp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fminnmp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fminnmp, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2D);

    // fabd
    theEmitter->emitIns_R_R_R(INS_fabd, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fabd, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fabd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // frecps
    theEmitter->emitIns_R_R_R(INS_frecps, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_frecps, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_frecps, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_frecps, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_frecps, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // frsqrts
    theEmitter->emitIns_R_R_R(INS_frsqrts, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_frsqrts, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_frsqrts, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_frsqrts, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_frsqrts, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_fmul, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmul, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmul, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmul, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmul, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by element 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by element 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmulx, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by element 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by element 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fnmul, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fnmul, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE

    //
    // R_R_I  vector operations, one dest, one source reg, one immed
    //

    // Some of the tests cases below might appear redundant since they emit same combinations of instruction x size x
    // vector arrangements. However, these are added to verify that the split constant encoding works with both - small
    // and large constants.

    genDefineTempLabel(genCreateTempLabel());

    // sshr scalar
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V8, REG_V9, 64);

    // sshr vector
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // ssra scalar
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V8, REG_V9, 64);

    // ssra vector
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // srshr scalar
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V8, REG_V9, 64);

    // srshr vector
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // srsra scalar
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V8, REG_V9, 64);

    // srsra vector
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // shl scalar
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V0, REG_V1, 0);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V8, REG_V9, 63);

    // shl vector
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V0, REG_V1, 0, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V4, REG_V5, 8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V8, REG_V9, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V12, REG_V13, 32, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // ushr scalar
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V8, REG_V9, 64);

    // ushr vector
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // usra scalar
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V8, REG_V9, 64);

    // usra vector
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // urshr scalar
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V8, REG_V9, 64);

    // urshr vector
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // ursra scalar
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V8, REG_V9, 64);

    // ursra vector
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // sri scalar
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V8, REG_V9, 64);

    // sri vector
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V14, REG_V15, 64, INS_OPTS_2D);

    // sli scalar
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V0, REG_V1, 0);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V8, REG_V9, 63);

    // sli vector
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V0, REG_V1, 0, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V4, REG_V5, 8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V8, REG_V9, 16, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V12, REG_V13, 32, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // sshll{2} vector
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // ushll{2} vector
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // shrn{2} vector
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);

    // rshrn{2} vector
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V2, REG_V3, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V6, REG_V7, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V10, REG_V11, 32, INS_OPTS_4S);

    // sxtl{2} vector
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // uxtl{2} vector
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // sqrshrn scalar
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_1BYTE, REG_V2, REG_V3, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_2BYTE, REG_V4, REG_V5, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_2BYTE, REG_V6, REG_V7, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_4BYTE, REG_V8, REG_V9, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_4BYTE, REG_V10, REG_V11, 32, INS_OPTS_NONE);

    // sqrshrn{2} vector
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqrshrn, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqrshrn2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    // sqrshrun scalar
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_1BYTE, REG_V0, REG_V1, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_2BYTE, REG_V2, REG_V3, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_2BYTE, REG_V2, REG_V3, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_4BYTE, REG_V4, REG_V5, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_4BYTE, REG_V4, REG_V5, 32, INS_OPTS_NONE);

    // sqrshrun{2} vector
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqrshrun, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqrshrun2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    // sqshl scalar
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_1BYTE, REG_V0, REG_V1, 0, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_1BYTE, REG_V2, REG_V3, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_2BYTE, REG_V4, REG_V5, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_2BYTE, REG_V6, REG_V7, 15, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_4BYTE, REG_V8, REG_V9, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_4BYTE, REG_V10, REG_V11, 31, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_8BYTE, REG_V12, REG_V13, 32, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_8BYTE, REG_V14, REG_V15, 63, INS_OPTS_NONE);

    // sqshl vector
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqshl, EA_16BYTE, REG_V12, REG_V13, 63, INS_OPTS_2D);

    // sqshlu scalar
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_1BYTE, REG_V0, REG_V1, 0, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_1BYTE, REG_V2, REG_V3, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_2BYTE, REG_V4, REG_V5, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_2BYTE, REG_V6, REG_V7, 15, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_4BYTE, REG_V8, REG_V9, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_4BYTE, REG_V10, REG_V11, 31, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_8BYTE, REG_V12, REG_V13, 32, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_8BYTE, REG_V14, REG_V15, 63, INS_OPTS_NONE);

    // sqshlu vector
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqshlu, EA_16BYTE, REG_V12, REG_V13, 63, INS_OPTS_2D);

    // sqshrn scalar
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_1BYTE, REG_V2, REG_V3, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_2BYTE, REG_V4, REG_V5, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_2BYTE, REG_V6, REG_V7, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_4BYTE, REG_V8, REG_V9, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_4BYTE, REG_V10, REG_V11, 32, INS_OPTS_NONE);

    // sqshrn{2} vector
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshrn, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqshrn2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    // sqshrun scalar
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_1BYTE, REG_V2, REG_V3, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_2BYTE, REG_V4, REG_V5, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_2BYTE, REG_V6, REG_V7, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_4BYTE, REG_V8, REG_V9, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_4BYTE, REG_V10, REG_V11, 32, INS_OPTS_NONE);

    // sqshrun{2} vector
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshrun, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sqshrun2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    // uqrshrn scalar
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_1BYTE, REG_V2, REG_V3, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_2BYTE, REG_V4, REG_V5, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_2BYTE, REG_V6, REG_V7, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_4BYTE, REG_V8, REG_V9, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_4BYTE, REG_V10, REG_V11, 32, INS_OPTS_NONE);

    // uqrshrn{2} vector
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_uqrshrn, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_uqrshrn2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    // uqshl scalar
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_1BYTE, REG_V0, REG_V1, 0, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_1BYTE, REG_V2, REG_V3, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_2BYTE, REG_V4, REG_V5, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_2BYTE, REG_V6, REG_V7, 15, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_4BYTE, REG_V8, REG_V9, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_4BYTE, REG_V10, REG_V11, 31, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_8BYTE, REG_V12, REG_V13, 32, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_8BYTE, REG_V14, REG_V15, 63, INS_OPTS_NONE);

    // uqshl vector
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_uqshl, EA_16BYTE, REG_V12, REG_V13, 63, INS_OPTS_2D);

    // uqshrn scalar
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_1BYTE, REG_V0, REG_V1, 1, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_1BYTE, REG_V2, REG_V3, 8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_2BYTE, REG_V4, REG_V5, 9, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_2BYTE, REG_V6, REG_V7, 16, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_4BYTE, REG_V8, REG_V9, 17, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_4BYTE, REG_V10, REG_V11, 32, INS_OPTS_NONE);

    // uqshrn{2} vector
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V2, REG_V3, 8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V4, REG_V5, 1, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V6, REG_V7, 8, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V8, REG_V9, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V10, REG_V11, 16, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V12, REG_V13, 9, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V14, REG_V15, 16, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V16, REG_V17, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_uqshrn, EA_8BYTE, REG_V18, REG_V18, 32, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V20, REG_V21, 17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_uqshrn2, EA_16BYTE, REG_V22, REG_V23, 32, INS_OPTS_4S);

    //
    // R_R_R   vector operations, one dest, two source
    //

    genDefineTempLabel(genCreateTempLabel());

    // Specifying an Arrangement is optional
    //
    theEmitter->emitIns_R_R_R(INS_and, EA_8BYTE, REG_V6, REG_V7, REG_V8);
    theEmitter->emitIns_R_R_R(INS_bic, EA_8BYTE, REG_V9, REG_V10, REG_V11);
    theEmitter->emitIns_R_R_R(INS_eor, EA_8BYTE, REG_V12, REG_V13, REG_V14);
    theEmitter->emitIns_R_R_R(INS_orr, EA_8BYTE, REG_V15, REG_V16, REG_V17);
    theEmitter->emitIns_R_R_R(INS_orn, EA_8BYTE, REG_V18, REG_V19, REG_V20);
    theEmitter->emitIns_R_R_R(INS_and, EA_16BYTE, REG_V21, REG_V22, REG_V23);
    theEmitter->emitIns_R_R_R(INS_bic, EA_16BYTE, REG_V24, REG_V25, REG_V26);
    theEmitter->emitIns_R_R_R(INS_eor, EA_16BYTE, REG_V27, REG_V28, REG_V29);
    theEmitter->emitIns_R_R_R(INS_orr, EA_16BYTE, REG_V30, REG_V31, REG_V0);
    theEmitter->emitIns_R_R_R(INS_orn, EA_16BYTE, REG_V1, REG_V2, REG_V3);

    theEmitter->emitIns_R_R_R(INS_bsl, EA_8BYTE, REG_V4, REG_V5, REG_V6);
    theEmitter->emitIns_R_R_R(INS_bit, EA_8BYTE, REG_V7, REG_V8, REG_V9);
    theEmitter->emitIns_R_R_R(INS_bif, EA_8BYTE, REG_V10, REG_V11, REG_V12);
    theEmitter->emitIns_R_R_R(INS_bsl, EA_16BYTE, REG_V13, REG_V14, REG_V15);
    theEmitter->emitIns_R_R_R(INS_bit, EA_16BYTE, REG_V16, REG_V17, REG_V18);
    theEmitter->emitIns_R_R_R(INS_bif, EA_16BYTE, REG_V19, REG_V20, REG_V21);

    // Default Arrangement as per the ARM64 manual
    //
    theEmitter->emitIns_R_R_R(INS_and, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bic, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_eor, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_orr, EA_8BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_orn, EA_8BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_and, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bic, EA_16BYTE, REG_V24, REG_V25, REG_V26, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_eor, EA_16BYTE, REG_V27, REG_V28, REG_V29, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_orr, EA_16BYTE, REG_V30, REG_V31, REG_V0, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_orn, EA_16BYTE, REG_V1, REG_V2, REG_V3, INS_OPTS_16B);

    theEmitter->emitIns_R_R_R(INS_bsl, EA_8BYTE, REG_V4, REG_V5, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bit, EA_8BYTE, REG_V7, REG_V8, REG_V9, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bif, EA_8BYTE, REG_V10, REG_V11, REG_V12, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bsl, EA_16BYTE, REG_V13, REG_V14, REG_V15, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bit, EA_16BYTE, REG_V16, REG_V17, REG_V18, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bif, EA_16BYTE, REG_V19, REG_V20, REG_V21, INS_OPTS_16B);

    genDefineTempLabel(genCreateTempLabel());

    // add
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V0, REG_V1, REG_V2); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_2D);

    // addp
    theEmitter->emitIns_R_R(INS_addp, EA_16BYTE, REG_V0, REG_V1, INS_OPTS_2D); // scalar 16BYTE
    theEmitter->emitIns_R_R_R(INS_addp, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_addp, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_addp, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_addp, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_addp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_addp, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_addp, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_2D);

    // sub
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V1, REG_V2, REG_V3); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V4, REG_V5, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V7, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V10, REG_V11, REG_V12, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V13, REG_V14, REG_V15, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V16, REG_V17, REG_V18, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V19, REG_V20, REG_V21, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V22, REG_V23, REG_V24, INS_OPTS_2D);

    genDefineTempLabel(genCreateTempLabel());

    // saba vector
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sabd vector
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uaba vector
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uabd vector
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sdot vector
    theEmitter->emitIns_R_R_R(INS_sdot, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sdot, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);

    // smax vector
    theEmitter->emitIns_R_R_R(INS_smax, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smax, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smax, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smax, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smax, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_smax, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // smaxp vector
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_smaxp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // smin vector
    theEmitter->emitIns_R_R_R(INS_smin, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smin, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smin, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smin, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smin, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_smin, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sminp vector
    theEmitter->emitIns_R_R_R(INS_sminp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sminp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sminp, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sminp, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sminp, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sminp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // udot vector
    theEmitter->emitIns_R_R_R(INS_udot, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_udot, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4S);

    // umax vector
    theEmitter->emitIns_R_R_R(INS_umax, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umax, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umax, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umax, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umax, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_umax, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // umaxp vector
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_umaxp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // umin vector
    theEmitter->emitIns_R_R_R(INS_umin, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umin, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umin, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umin, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umin, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_umin, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uminp vector
    theEmitter->emitIns_R_R_R(INS_uminp, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uminp, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uminp, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uminp, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uminp, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uminp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // cmeq vector
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmeq, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // cmge vector
    theEmitter->emitIns_R_R_R(INS_cmge, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmge, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // cmgt vector
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmgt, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // cmhi vector
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmhi, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // cmhs vector
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmhs, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // cmtst vector
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_cmtst, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // faddp vector
    theEmitter->emitIns_R_R_R(INS_faddp, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_faddp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_faddp, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_2D);

    // fcmeq vector
    theEmitter->emitIns_R_R_R(INS_fcmeq, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fcmeq, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fcmeq, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_2D);

    // fcmge vector
    theEmitter->emitIns_R_R_R(INS_fcmge, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fcmge, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fcmge, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_2D);

    // fcmgt vector
    theEmitter->emitIns_R_R_R(INS_fcmgt, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fcmgt, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fcmgt, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_2D);

    // trn1 vector
    theEmitter->emitIns_R_R_R(INS_trn1, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_trn1, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // trn2 vector
    theEmitter->emitIns_R_R_R(INS_trn2, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_trn2, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // uzp1 vector
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_uzp1, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // uzp2 vector
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_uzp2, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // zip1 vector
    theEmitter->emitIns_R_R_R(INS_zip1, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_zip1, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // zip2 vector
    theEmitter->emitIns_R_R_R(INS_zip2, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_zip2, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // srshl scalar
    theEmitter->emitIns_R_R_R(INS_srshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);

    // srshl vector
    theEmitter->emitIns_R_R_R(INS_srshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_srshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // sshl scalar
    theEmitter->emitIns_R_R_R(INS_sshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);

    // sshl vector
    theEmitter->emitIns_R_R_R(INS_sshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_sshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // urshl scalar
    theEmitter->emitIns_R_R_R(INS_urshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);

    // urshl vector
    theEmitter->emitIns_R_R_R(INS_urshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_urshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // ushl scalar
    theEmitter->emitIns_R_R_R(INS_ushl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);

    // ushl vector
    theEmitter->emitIns_R_R_R(INS_ushl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_ushl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // addhn vector
    theEmitter->emitIns_R_R_R(INS_addhn, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_addhn, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_addhn, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // addhn2 vector
    theEmitter->emitIns_R_R_R(INS_addhn2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_addhn2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_addhn2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // raddhn vector
    theEmitter->emitIns_R_R_R(INS_raddhn, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_raddhn, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_raddhn, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // raddhn2 vector
    theEmitter->emitIns_R_R_R(INS_raddhn2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_raddhn2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_raddhn2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // rsubhn vector
    theEmitter->emitIns_R_R_R(INS_rsubhn, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_rsubhn, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_rsubhn, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // rsubhn2 vector
    theEmitter->emitIns_R_R_R(INS_rsubhn2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_rsubhn2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_rsubhn2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sabal vector
    theEmitter->emitIns_R_R_R(INS_sabal, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sabal, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sabal, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // sabal2 vector
    theEmitter->emitIns_R_R_R(INS_sabal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sabal2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sabal2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sabdl vector
    theEmitter->emitIns_R_R_R(INS_sabdl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sabdl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sabdl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // sabdl2 vector
    theEmitter->emitIns_R_R_R(INS_sabdl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sabdl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sabdl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // saddl vector
    theEmitter->emitIns_R_R_R(INS_saddl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_saddl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_saddl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // saddl2 vector
    theEmitter->emitIns_R_R_R(INS_saddl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_saddl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_saddl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // saddw vector
    theEmitter->emitIns_R_R_R(INS_saddw, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_saddw, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_saddw, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // saddw2 vector
    theEmitter->emitIns_R_R_R(INS_saddw2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_saddw2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_saddw2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // shadd vector
    theEmitter->emitIns_R_R_R(INS_shadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_shadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_shadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_shadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_shadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_shadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // shsub vector
    theEmitter->emitIns_R_R_R(INS_shsub, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_shsub, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_shsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_shsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_shsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_shsub, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sqadd scalar
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // sqadd vector
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sqrshl scalar
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // sqrshl vector
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_sqrshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // sqshl scalar
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // sqshl vector
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_sqshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // sqsub scalar
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // sqsub vector
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqsub, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // srhadd vector
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_srhadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // ssubl vector
    theEmitter->emitIns_R_R_R(INS_ssubl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ssubl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ssubl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // ssubl2 vector
    theEmitter->emitIns_R_R_R(INS_ssubl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ssubl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ssubl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // ssubw vector
    theEmitter->emitIns_R_R_R(INS_ssubw, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_ssubw, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_ssubw, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // ssubw2 vector
    theEmitter->emitIns_R_R_R(INS_ssubw2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_ssubw2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_ssubw2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // subhn vector
    theEmitter->emitIns_R_R_R(INS_subhn, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_subhn, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_subhn, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // sqdmlal scalar
    theEmitter->emitIns_R_R_R(INS_sqdmlal, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqdmlal, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdmlal vector
    theEmitter->emitIns_R_R_R(INS_sqdmlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqdmlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);

    // sqdmlal2 vector
    theEmitter->emitIns_R_R_R(INS_sqdmlal2, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqdmlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqdmlsl scalar
    theEmitter->emitIns_R_R_R(INS_sqdmlsl, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqdmlsl, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdmlsl vector
    theEmitter->emitIns_R_R_R(INS_sqdmlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqdmlsl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);

    // sqdmlsl2 vector
    theEmitter->emitIns_R_R_R(INS_sqdmlsl2, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqdmlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqdmulh scalar
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdmulh vector
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqdmulh, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqdmull scalar
    theEmitter->emitIns_R_R_R(INS_sqdmull, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqdmull, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdmull vector
    theEmitter->emitIns_R_R_R(INS_sqdmull, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqdmull, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);

    // sqdmull2 vector
    theEmitter->emitIns_R_R_R(INS_sqdmull2, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqdmull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqrdmlah scalar
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdrmlah vector
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqrdmlah, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqrdmlsh scalar
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdrmlsh vector
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqrdmlsh, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // sqrdmulh scalar
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_2BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_4BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);

    // sqdrmulh vector
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_16BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sqrdmulh, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);

    // subhn2 vector
    theEmitter->emitIns_R_R_R(INS_subhn2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_subhn2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_subhn2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uabal vector
    theEmitter->emitIns_R_R_R(INS_uabal, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uabal, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uabal, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // uabal2 vector
    theEmitter->emitIns_R_R_R(INS_uabal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uabal2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uabal2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uabdl vector
    theEmitter->emitIns_R_R_R(INS_uabdl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uabdl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uabdl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // uabdl2 vector
    theEmitter->emitIns_R_R_R(INS_uabdl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uabdl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uabdl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uaddl vector
    theEmitter->emitIns_R_R_R(INS_uaddl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uaddl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uaddl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // uaddl2 vector
    theEmitter->emitIns_R_R_R(INS_uaddl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uaddl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uaddl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uaddw vector
    theEmitter->emitIns_R_R_R(INS_uaddw, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uaddw, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uaddw, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // uaddw2 vector
    theEmitter->emitIns_R_R_R(INS_uaddw2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uaddw2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uaddw2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uhadd vector
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uhadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uhsub vector
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uhsub, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uqadd scalar
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // uqadd vector
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uqadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uqrshl scalar
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // uqrshl vector
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_uqrshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // uqshl scalar
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // uqshl vector
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_uqshl, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_2D);

    // uqsub scalar
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_1BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_2BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_4BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_NONE);

    // uqsub vector
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uqsub, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // urhadd vector
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_urhadd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // usubl vector
    theEmitter->emitIns_R_R_R(INS_usubl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_usubl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_usubl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // usubl2 vector
    theEmitter->emitIns_R_R_R(INS_usubl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_usubl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_usubl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // usubw vector
    theEmitter->emitIns_R_R_R(INS_usubw, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_usubw, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_usubw, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // usubw2 vector
    theEmitter->emitIns_R_R_R(INS_usubw2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_usubw2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_usubw2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    //
    // R_R_R  vector multiply
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    theEmitter->emitIns_R_R_R(INS_pmul, EA_8BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_pmul, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_16B);

    // 'mul' vector by element
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

    // 'mla' vector by element
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

    // 'mls' vector by element
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

    // pmull vector
    theEmitter->emitIns_R_R_R(INS_pmull, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_pmull, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_1D);

    // pmull2 vector
    theEmitter->emitIns_R_R_R(INS_pmull2, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_pmull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_2D);

    // sdot vector
    theEmitter->emitIns_R_R_R_I(INS_sdot, EA_8BYTE, REG_V0, REG_V1, REG_V16, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_sdot, EA_16BYTE, REG_V3, REG_V4, REG_V31, 1, INS_OPTS_4S);

    // smlal vector
    theEmitter->emitIns_R_R_R(INS_smlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smlal, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // smlal2 vector
    theEmitter->emitIns_R_R_R(INS_smlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smlal2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smlal2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // smlsl vector
    theEmitter->emitIns_R_R_R(INS_smlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smlsl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smlsl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // smlsl2 vector
    theEmitter->emitIns_R_R_R(INS_smlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smlsl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smlsl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // smull vector
    theEmitter->emitIns_R_R_R(INS_smull, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_smull, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_smull, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // smull2 vector
    theEmitter->emitIns_R_R_R(INS_smull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_smull2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_smull2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // udot vector
    theEmitter->emitIns_R_R_R_I(INS_udot, EA_8BYTE, REG_V0, REG_V1, REG_V16, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_udot, EA_16BYTE, REG_V3, REG_V4, REG_V31, 1, INS_OPTS_4S);

    // umlal vector
    theEmitter->emitIns_R_R_R(INS_umlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umlal, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // umlal2 vector
    theEmitter->emitIns_R_R_R(INS_umlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umlal2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umlal2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // umlsl vector
    theEmitter->emitIns_R_R_R(INS_umlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umlsl, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umlsl, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // umlsl2 vector
    theEmitter->emitIns_R_R_R(INS_umlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umlsl2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umlsl2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // umull vector
    theEmitter->emitIns_R_R_R(INS_umull, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_umull, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_umull, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);

    // umull2 vector
    theEmitter->emitIns_R_R_R(INS_umull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_umull2, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_umull2, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // smlal vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_smlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, 1, INS_OPTS_2S);

    // smlal2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smlal2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_smlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // smlsl vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_smlsl, EA_8BYTE, REG_V3, REG_V4, REG_V5, 1, INS_OPTS_2S);

    // smlsl2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smlsl2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_smlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // smull vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smull, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_smull, EA_8BYTE, REG_V3, REG_V4, REG_V5, 1, INS_OPTS_2S);

    // smull2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_smull2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_smull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqdmlal scalar, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdmlal vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);

    // sqdmlal2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqdmlsl scalar, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdmlsl vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);

    // sqdmlsl2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqdmulh scalar
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdmulh vector
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmulh, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqdmull scalar, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmull, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqdmull, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdmull vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmull, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmull, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);

    // sqdmull2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_sqdmull2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqdmull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqrdmlah scalar
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdrmlah vector
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlah, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqrdmlsh scalar
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdrmlsh vector
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmlsh, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // sqrdmulh scalar
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_2BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_NONE);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_4BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_NONE);

    // sqdrmulh vector
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_8BYTE, REG_V0, REG_V1, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_8BYTE, REG_V3, REG_V4, REG_V5, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_sqrdmulh, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // umlal vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umlal, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_umlal, EA_8BYTE, REG_V3, REG_V4, REG_V5, 1, INS_OPTS_2S);

    // umlal2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umlal2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_umlal2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // umlsl vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umlsl, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);

    // umlsl2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umlsl2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_umlsl2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    // umull vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umull, EA_8BYTE, REG_V0, REG_V1, REG_V2, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_umull, EA_8BYTE, REG_V3, REG_V4, REG_V5, 1, INS_OPTS_2S);

    // umull2 vector, by element
    theEmitter->emitIns_R_R_R_I(INS_umull2, EA_16BYTE, REG_V6, REG_V7, REG_V8, 7, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_umull2, EA_16BYTE, REG_V9, REG_V10, REG_V11, 3, INS_OPTS_4S);

    //
    // R_R_R   floating point operations, one source/dest, and two source
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_fmla, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmla, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmla, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by element 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by element 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmls, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmls, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmls, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by element 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by element 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    //
    // R_R_R_R   floating point operations, one dest, and three source
    //

    theEmitter->emitIns_R_R_R_R(INS_fmadd, EA_4BYTE, REG_V0, REG_V8, REG_V16, REG_V24);
    theEmitter->emitIns_R_R_R_R(INS_fmsub, EA_4BYTE, REG_V1, REG_V9, REG_V17, REG_V25);
    theEmitter->emitIns_R_R_R_R(INS_fnmadd, EA_4BYTE, REG_V2, REG_V10, REG_V18, REG_V26);
    theEmitter->emitIns_R_R_R_R(INS_fnmsub, EA_4BYTE, REG_V3, REG_V11, REG_V19, REG_V27);

    theEmitter->emitIns_R_R_R_R(INS_fmadd, EA_8BYTE, REG_V4, REG_V12, REG_V20, REG_V28);
    theEmitter->emitIns_R_R_R_R(INS_fmsub, EA_8BYTE, REG_V5, REG_V13, REG_V21, REG_V29);
    theEmitter->emitIns_R_R_R_R(INS_fnmadd, EA_8BYTE, REG_V6, REG_V14, REG_V22, REG_V30);
    theEmitter->emitIns_R_R_R_R(INS_fnmsub, EA_8BYTE, REG_V7, REG_V15, REG_V23, REG_V31);
}

/*****************************************************************************
 * Unit tests for SVE instructions.
 */
void CodeGen::genArm64EmitterUnitTestsSve()
{
    emitter* theEmitter = GetEmitter();

    //
    // R_R_R   SVE operations, one dest, two source
    //

    genDefineTempLabel(genCreateTempLabel());

    // IF_SVE_AA_3A
    theEmitter->emitIns_R_R_R(INS_sve_and, EA_SCALABLE, REG_V0, REG_P1, REG_V2,
                              INS_OPTS_SCALABLE_B); // AND     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_bic, EA_SCALABLE, REG_V3, REG_P4, REG_V5,
                              INS_OPTS_SCALABLE_H); // BIC     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_eor, EA_SCALABLE, REG_V14, REG_P5, REG_V16,
                              INS_OPTS_SCALABLE_S); // EOR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_orr, EA_SCALABLE, REG_V29, REG_P7, REG_V31,
                              INS_OPTS_SCALABLE_D); // ORR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AB_3A
    theEmitter->emitIns_R_R_R(INS_sve_add, EA_SCALABLE, REG_V5, REG_P6, REG_V7,
                              INS_OPTS_SCALABLE_B); // ADD     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sub, EA_SCALABLE, REG_V15, REG_P7, REG_V29,
                              INS_OPTS_SCALABLE_H); // SUB     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_subr, EA_SCALABLE, REG_V2, REG_P0, REG_V13,
                              INS_OPTS_SCALABLE_S); // SUBR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AC_3A
    theEmitter->emitIns_R_R_R(INS_sve_sdiv, EA_SCALABLE, REG_V3, REG_P2, REG_V9,
                              INS_OPTS_SCALABLE_S); // SDIV     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sdivr, EA_SCALABLE, REG_V31, REG_P3, REG_V29,
                              INS_OPTS_SCALABLE_D); // SDIVR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_udiv, EA_SCALABLE, REG_V1, REG_P0, REG_V0,
                              INS_OPTS_SCALABLE_S); // UDIV    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_udivr, EA_SCALABLE, REG_V13, REG_P7, REG_V15,
                              INS_OPTS_SCALABLE_D); // UDIVR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AD_3A
    theEmitter->emitIns_R_R_R(INS_sve_smax, EA_SCALABLE, REG_V24, REG_P0, REG_V2,
                              INS_OPTS_SCALABLE_B); // SMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_smin, EA_SCALABLE, REG_V9, REG_P1, REG_V27,
                              INS_OPTS_SCALABLE_H); // SMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sabd, EA_SCALABLE, REG_V5, REG_P2, REG_V6,
                              INS_OPTS_SCALABLE_B); // SABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uabd, EA_SCALABLE, REG_V23, REG_P3, REG_V9,
                              INS_OPTS_SCALABLE_S); // UABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umax, EA_SCALABLE, REG_V15, REG_P4, REG_V2,
                              INS_OPTS_SCALABLE_S); // UMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umin, EA_SCALABLE, REG_V12, REG_P7, REG_V0,
                              INS_OPTS_SCALABLE_D); // UMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AE_3A
    theEmitter->emitIns_R_R_R(INS_sve_mul, EA_SCALABLE, REG_V5, REG_P1, REG_V3,
                              INS_OPTS_SCALABLE_D); // MUL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_smulh, EA_SCALABLE, REG_V17, REG_P5, REG_V5,
                              INS_OPTS_SCALABLE_S); // SMULH   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umulh, EA_SCALABLE, REG_V12, REG_P2, REG_V24,
                              INS_OPTS_SCALABLE_B); // UMULH   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AH_3A
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V13, REG_P0, REG_V31,
                              INS_OPTS_SCALABLE_B); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V14, REG_P1, REG_V0,
                              INS_OPTS_SCALABLE_H); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V13, REG_P2, REG_V1,
                              INS_OPTS_SCALABLE_S); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V15, REG_P3, REG_V2,
                              INS_OPTS_SCALABLE_D); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V16, REG_P4, REG_V3, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_PREDICATE_MERGE); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V17, REG_P5, REG_V12, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_PREDICATE_MERGE); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V0, REG_P6, REG_V13, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_PREDICATE_MERGE); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_movprfx, EA_SCALABLE, REG_V31, REG_P7, REG_V22, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_PREDICATE_MERGE); // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>

    // IF_SVE_AM_2A : right shifts
    theEmitter->emitIns_R_R_I(INS_sve_asr, EA_SCALABLE, REG_V0, REG_P0, 1,
                              INS_OPTS_SCALABLE_B); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asr, EA_SCALABLE, REG_V1, REG_P1, 2,
                              INS_OPTS_SCALABLE_B); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asr, EA_SCALABLE, REG_V2, REG_P2, 3,
                              INS_OPTS_SCALABLE_B); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asr, EA_SCALABLE, REG_V3, REG_P3, 7,
                              INS_OPTS_SCALABLE_B); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asr, EA_SCALABLE, REG_V4, REG_P7, 8,
                              INS_OPTS_SCALABLE_B); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asrd, EA_SCALABLE, REG_V5, REG_P0, 1,
                              INS_OPTS_SCALABLE_H); // ASRD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asrd, EA_SCALABLE, REG_V6, REG_P4, 8,
                              INS_OPTS_SCALABLE_H); // ASRD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asrd, EA_SCALABLE, REG_V7, REG_P7, 15,
                              INS_OPTS_SCALABLE_H); // ASRD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_asrd, EA_SCALABLE, REG_V31, REG_P6, 16,
                              INS_OPTS_SCALABLE_H); // ASRD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsr, EA_SCALABLE, REG_V8, REG_P5, 1,
                              INS_OPTS_SCALABLE_S); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsr, EA_SCALABLE, REG_V9, REG_P6, 5,
                              INS_OPTS_SCALABLE_S); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsr, EA_SCALABLE, REG_V31, REG_P7, 17,
                              INS_OPTS_SCALABLE_S); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsr, EA_SCALABLE, REG_V0, REG_P0, 31,
                              INS_OPTS_SCALABLE_S); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsr, EA_SCALABLE, REG_V5, REG_P1, 32,
                              INS_OPTS_SCALABLE_S); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_srshr, EA_SCALABLE, REG_V6, REG_P3, 1,
                              INS_OPTS_SCALABLE_D); // SRSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_srshr, EA_SCALABLE, REG_V7, REG_P4, 3,
                              INS_OPTS_SCALABLE_D); // SRSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_srshr, EA_SCALABLE, REG_V21, REG_P5, 21,
                              INS_OPTS_SCALABLE_D); // SRSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_srshr, EA_SCALABLE, REG_V22, REG_P6, 63,
                              INS_OPTS_SCALABLE_D); // SRSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_urshr, EA_SCALABLE, REG_V31, REG_P7, 64,
                              INS_OPTS_SCALABLE_D); // URSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>

    // IF_SVE_AM_2A : left shifts
    theEmitter->emitIns_R_R_I(INS_sve_lsl, EA_SCALABLE, REG_V1, REG_P0, 0,
                              INS_OPTS_SCALABLE_B); // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsl, EA_SCALABLE, REG_V3, REG_P1, 1,
                              INS_OPTS_SCALABLE_B); // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_lsl, EA_SCALABLE, REG_V5, REG_P2, 7,
                              INS_OPTS_SCALABLE_B); // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshl, EA_SCALABLE, REG_V7, REG_P3, 0,
                              INS_OPTS_SCALABLE_H); // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshl, EA_SCALABLE, REG_V9, REG_P4, 7,
                              INS_OPTS_SCALABLE_H); // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshl, EA_SCALABLE, REG_V11, REG_P0, 9,
                              INS_OPTS_SCALABLE_H); // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshl, EA_SCALABLE, REG_V13, REG_P7, 15,
                              INS_OPTS_SCALABLE_H); // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshlu, EA_SCALABLE, REG_V14, REG_P6, 0,
                              INS_OPTS_SCALABLE_S); // SQSHLU  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshlu, EA_SCALABLE, REG_V15, REG_P0, 12,
                              INS_OPTS_SCALABLE_S); // SQSHLU  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshlu, EA_SCALABLE, REG_V16, REG_P1, 15,
                              INS_OPTS_SCALABLE_S); // SQSHLU  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqshlu, EA_SCALABLE, REG_V0, REG_P2, 31,
                              INS_OPTS_SCALABLE_S); // SQSHLU  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqshl, EA_SCALABLE, REG_V17, REG_P7, 0,
                              INS_OPTS_SCALABLE_D); // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqshl, EA_SCALABLE, REG_V18, REG_P0, 18,
                              INS_OPTS_SCALABLE_D); // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqshl, EA_SCALABLE, REG_V19, REG_P3, 32,
                              INS_OPTS_SCALABLE_D); // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqshl, EA_SCALABLE, REG_V0, REG_P2, 63,
                              INS_OPTS_SCALABLE_D); // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>

    // IF_SVE_AN_3A
    theEmitter->emitIns_R_R_R(INS_sve_asr, EA_SCALABLE, REG_V5, REG_P0, REG_V21,
                              INS_OPTS_SCALABLE_S); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_asrr, EA_SCALABLE, REG_V1, REG_P7, REG_V20,
                              INS_OPTS_SCALABLE_B); // ASRR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lsl, EA_SCALABLE, REG_V0, REG_P2, REG_V0,
                              INS_OPTS_SCALABLE_H); // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lslr, EA_SCALABLE, REG_V27, REG_P6, REG_V31,
                              INS_OPTS_SCALABLE_D); // LSLR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lsr, EA_SCALABLE, REG_V5, REG_P5, REG_V6,
                              INS_OPTS_SCALABLE_B); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lsrr, EA_SCALABLE, REG_V15, REG_P4, REG_V17,
                              INS_OPTS_SCALABLE_S); // LSRR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_AO_3A
    theEmitter->emitIns_R_R_R(INS_sve_asr, EA_SCALABLE, REG_V4, REG_P3, REG_V24, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_WIDE); // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D
    theEmitter->emitIns_R_R_R(INS_sve_lsl, EA_SCALABLE, REG_V19, REG_P7, REG_V3, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WIDE); // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D
    theEmitter->emitIns_R_R_R(INS_sve_lsr, EA_SCALABLE, REG_V0, REG_P0, REG_V0, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WIDE); // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D

    // IF_SVE_CM_3A
    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_SCALABLE, REG_V31, REG_P7, REG_V31,
                              INS_OPTS_SCALABLE_B); // CLASTA  <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_SCALABLE, REG_V30, REG_P6, REG_V30,
                              INS_OPTS_SCALABLE_D); // CLASTB  <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_CN_3A
    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_2BYTE, REG_V12, REG_P1, REG_V15, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // CLASTA  <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_V13, REG_P2, REG_V16, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // CLASTB  <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_V14, REG_P0, REG_V17, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // CLASTB  <V><dn>, <Pg>, <V><dn>, <Zm>.<T>

    // IF_SVE_CO_3A
    // Note: EA_4BYTE used for B and H (destination register is W)
    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R0, REG_P0, REG_V0,
                              INS_OPTS_SCALABLE_B); // CLASTA  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R1, REG_P2, REG_V3,
                              INS_OPTS_SCALABLE_H); // CLASTA  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_R23, REG_P5, REG_V12,
                              INS_OPTS_SCALABLE_S); // CLASTB  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_R3, REG_P6, REG_V9,
                              INS_OPTS_SCALABLE_D); // CLASTB  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>

    // IF_SVE_CX_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpeq, EA_SCALABLE, REG_P15, REG_P0, REG_V0, REG_V10,
                                INS_OPTS_SCALABLE_B); // CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpge, EA_SCALABLE, REG_P14, REG_P1, REG_V10, REG_V23,
                                INS_OPTS_SCALABLE_H); // CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpgt, EA_SCALABLE, REG_P13, REG_P2, REG_V20, REG_V24,
                                INS_OPTS_SCALABLE_S); // CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmphi, EA_SCALABLE, REG_P12, REG_P3, REG_V30, REG_V25,
                                INS_OPTS_SCALABLE_D); // CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmphs, EA_SCALABLE, REG_P11, REG_P4, REG_V1, REG_V26,
                                INS_OPTS_SCALABLE_B); // CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmple, EA_SCALABLE, REG_P10, REG_P5, REG_V11, REG_V27,
                                INS_OPTS_SCALABLE_H); // CMPLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmplo, EA_SCALABLE, REG_P11, REG_P6, REG_V12, REG_V28,
                                INS_OPTS_SCALABLE_S); // CMPLO   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpls, EA_SCALABLE, REG_P0, REG_P7, REG_V13, REG_V29,
                                INS_OPTS_SCALABLE_D); // CMPLS   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmplt, EA_SCALABLE, REG_P2, REG_P0, REG_V14, REG_V30,
                                INS_OPTS_SCALABLE_B); // CMPLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpne, EA_SCALABLE, REG_P3, REG_P1, REG_V15, REG_V20,
                                INS_OPTS_SCALABLE_H); // CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_CX_4A_A
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpeq, EA_SCALABLE, REG_P15, REG_P7, REG_V31, REG_V3, INS_OPTS_SCALABLE_B,
                                INS_SCALABLE_OPTS_WIDE); /* CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpge, EA_SCALABLE, REG_P14, REG_P6, REG_V21, REG_V13, INS_OPTS_SCALABLE_H,
                                INS_SCALABLE_OPTS_WIDE); /* CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpgt, EA_SCALABLE, REG_P13, REG_P5, REG_V11, REG_V23, INS_OPTS_SCALABLE_S,
                                INS_SCALABLE_OPTS_WIDE); /* CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmphi, EA_SCALABLE, REG_P12, REG_P4, REG_V1, REG_V31, INS_OPTS_SCALABLE_B,
                                INS_SCALABLE_OPTS_WIDE); /* CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmphs, EA_SCALABLE, REG_P11, REG_P3, REG_V0, REG_V30, INS_OPTS_SCALABLE_H,
                                INS_SCALABLE_OPTS_WIDE); /* CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmple, EA_SCALABLE, REG_P4, REG_P2, REG_V10, REG_V0, INS_OPTS_SCALABLE_S,
                                INS_SCALABLE_OPTS_WIDE); /* CMPLE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmplo, EA_SCALABLE, REG_P3, REG_P1, REG_V20, REG_V1, INS_OPTS_SCALABLE_B,
                                INS_SCALABLE_OPTS_WIDE); /* CMPLO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpls, EA_SCALABLE, REG_P2, REG_P0, REG_V30, REG_V2, INS_OPTS_SCALABLE_H,
                                INS_SCALABLE_OPTS_WIDE); /* CMPLS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmplt, EA_SCALABLE, REG_P1, REG_P7, REG_V24, REG_V8, INS_OPTS_SCALABLE_S,
                                INS_SCALABLE_OPTS_WIDE); /* CMPLT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */
    theEmitter->emitIns_R_R_R_R(INS_sve_cmpne, EA_SCALABLE, REG_P0, REG_P0, REG_V14, REG_V28, INS_OPTS_SCALABLE_B,
                                INS_SCALABLE_OPTS_WIDE); /* CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D */

    // IF_SVE_CY_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_cmpeq, EA_SCALABLE, REG_P15, REG_P0, REG_V31, 8,
                                INS_OPTS_SCALABLE_B); /* CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmpge, EA_SCALABLE, REG_P11, REG_P7, REG_V21, 1,
                                INS_OPTS_SCALABLE_H); /* CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmpgt, EA_SCALABLE, REG_P10, REG_P1, REG_V18, 4,
                                INS_OPTS_SCALABLE_S); /* CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmple, EA_SCALABLE, REG_P8, REG_P6, REG_V11, 15,
                                INS_OPTS_SCALABLE_D); /* CMPLE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmplt, EA_SCALABLE, REG_P7, REG_P2, REG_V8, -16,
                                INS_OPTS_SCALABLE_B); /* CMPLT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmpne, EA_SCALABLE, REG_P0, REG_P5, REG_V0, -14,
                                INS_OPTS_SCALABLE_H); /* CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */

    // IF_SVE_CY_3B
    theEmitter->emitIns_R_R_R_I(INS_sve_cmphi, EA_SCALABLE, REG_P15, REG_P7, REG_V19, 0,
                                INS_OPTS_SCALABLE_B); /* CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmphs, EA_SCALABLE, REG_P11, REG_P1, REG_V0, 36,
                                INS_OPTS_SCALABLE_H); /* CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmplo, EA_SCALABLE, REG_P8, REG_P5, REG_V21, 64,
                                INS_OPTS_SCALABLE_S); /* CMPLO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */
    theEmitter->emitIns_R_R_R_I(INS_sve_cmpls, EA_SCALABLE, REG_P0, REG_P3, REG_V9, 127,
                                INS_OPTS_SCALABLE_D); /* CMPLS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm> */

    // IF_SVE_DD_2A
    theEmitter->emitIns_R_R(INS_sve_pfirst, EA_SCALABLE, REG_P0, REG_P15,
                            INS_OPTS_SCALABLE_B); // PFIRST  <Pdn>.B, <Pg>, <Pdn>.B

    // IF_SVE_DG_2A
    theEmitter->emitIns_R_R(INS_sve_rdffr, EA_SCALABLE, REG_P10, REG_P15,
                            INS_OPTS_SCALABLE_B); // RDFFR   <Pd>.B, <Pg>/Z
    theEmitter->emitIns_R_R(INS_sve_rdffrs, EA_SCALABLE, REG_P7, REG_P14,
                            INS_OPTS_SCALABLE_B); // RDFFRS  <Pd>.B, <Pg>/Z

    // IF_SVE_EP_3A
    theEmitter->emitIns_R_R_R(INS_sve_shadd, EA_SCALABLE, REG_V15, REG_P0, REG_V10,
                              INS_OPTS_SCALABLE_B); // SHADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_shsub, EA_SCALABLE, REG_V16, REG_P1, REG_V11,
                              INS_OPTS_SCALABLE_H); // SHSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_shsubr, EA_SCALABLE, REG_V17, REG_P2, REG_V12,
                              INS_OPTS_SCALABLE_S); // SHSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_srhadd, EA_SCALABLE, REG_V18, REG_P3, REG_V13,
                              INS_OPTS_SCALABLE_D); // SRHADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uhadd, EA_SCALABLE, REG_V19, REG_P4, REG_V14,
                              INS_OPTS_SCALABLE_B); // UHADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uhsub, EA_SCALABLE, REG_V20, REG_P5, REG_V15,
                              INS_OPTS_SCALABLE_H); // UHSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uhsubr, EA_SCALABLE, REG_V21, REG_P6, REG_V16,
                              INS_OPTS_SCALABLE_S); // UHSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_urhadd, EA_SCALABLE, REG_V22, REG_P7, REG_V17,
                              INS_OPTS_SCALABLE_D); // URHADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_ER_3A
    theEmitter->emitIns_R_R_R(INS_sve_addp, EA_SCALABLE, REG_V23, REG_P6, REG_V18,
                              INS_OPTS_SCALABLE_B); // ADDP    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_smaxp, EA_SCALABLE, REG_V24, REG_P5, REG_V19,
                              INS_OPTS_SCALABLE_H); // SMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sminp, EA_SCALABLE, REG_V25, REG_P4, REG_V20,
                              INS_OPTS_SCALABLE_S); // SMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umaxp, EA_SCALABLE, REG_V26, REG_P3, REG_V21,
                              INS_OPTS_SCALABLE_D); // UMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uminp, EA_SCALABLE, REG_V27, REG_P2, REG_V22,
                              INS_OPTS_SCALABLE_B); // UMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_ET_3A
    theEmitter->emitIns_R_R_R(INS_sve_sqadd, EA_SCALABLE, REG_V28, REG_P1, REG_V23,
                              INS_OPTS_SCALABLE_B); // SQADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqsub, EA_SCALABLE, REG_V29, REG_P0, REG_V24,
                              INS_OPTS_SCALABLE_H); // SQSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqsubr, EA_SCALABLE, REG_V30, REG_P1, REG_V25,
                              INS_OPTS_SCALABLE_H); // SQSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_suqadd, EA_SCALABLE, REG_V31, REG_P2, REG_V26,
                              INS_OPTS_SCALABLE_B); // SUQADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqadd, EA_SCALABLE, REG_V0, REG_P3, REG_V27,
                              INS_OPTS_SCALABLE_S); // UQADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqsub, EA_SCALABLE, REG_V1, REG_P4, REG_V28,
                              INS_OPTS_SCALABLE_D); // UQSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqsubr, EA_SCALABLE, REG_V2, REG_P5, REG_V29,
                              INS_OPTS_SCALABLE_B); // UQSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_usqadd, EA_SCALABLE, REG_V3, REG_P6, REG_V30,
                              INS_OPTS_SCALABLE_B); // USQADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_EU_3A
    theEmitter->emitIns_R_R_R(INS_sve_sqrshl, EA_SCALABLE, REG_V4, REG_P7, REG_V31,
                              INS_OPTS_SCALABLE_B); // SQRSHL  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqrshlr, EA_SCALABLE, REG_V5, REG_P0, REG_V30,
                              INS_OPTS_SCALABLE_H); // SQRSHLR <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqshl, EA_SCALABLE, REG_V6, REG_P1, REG_V29,
                              INS_OPTS_SCALABLE_S); // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqshlr, EA_SCALABLE, REG_V7, REG_P2, REG_V28,
                              INS_OPTS_SCALABLE_D); // SQSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_srshl, EA_SCALABLE, REG_V8, REG_P3, REG_V27,
                              INS_OPTS_SCALABLE_B); // SRSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_srshlr, EA_SCALABLE, REG_V9, REG_P4, REG_V26,
                              INS_OPTS_SCALABLE_H); // SRSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqrshl, EA_SCALABLE, REG_V10, REG_P5, REG_V25,
                              INS_OPTS_SCALABLE_S); // UQRSHL  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqrshlr, EA_SCALABLE, REG_V11, REG_P6, REG_V24,
                              INS_OPTS_SCALABLE_D); // UQRSHLR <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqshl, EA_SCALABLE, REG_V12, REG_P7, REG_V23,
                              INS_OPTS_SCALABLE_B); // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqshlr, EA_SCALABLE, REG_V13, REG_P0, REG_V22,
                              INS_OPTS_SCALABLE_H); // UQSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_urshl, EA_SCALABLE, REG_V14, REG_P1, REG_V21,
                              INS_OPTS_SCALABLE_S); // URSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_urshlr, EA_SCALABLE, REG_V15, REG_P2, REG_V20,
                              INS_OPTS_SCALABLE_D); // URSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_GE_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_match, EA_SCALABLE, REG_P15, REG_P0, REG_V21, REG_V0,
                                INS_OPTS_SCALABLE_B); // MATCH   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_nmatch, EA_SCALABLE, REG_P0, REG_P7, REG_V11, REG_V31,
                                INS_OPTS_SCALABLE_H); // NMATCH  <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_GR_3A
    theEmitter->emitIns_R_R_R(INS_sve_faddp, EA_SCALABLE, REG_V16, REG_P3, REG_V19,
                              INS_OPTS_SCALABLE_H); // FADDP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxnmp, EA_SCALABLE, REG_V17, REG_P4, REG_V18,
                              INS_OPTS_SCALABLE_S); // FMAXNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxp, EA_SCALABLE, REG_V18, REG_P5, REG_V17,
                              INS_OPTS_SCALABLE_D); // FMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fminnmp, EA_SCALABLE, REG_V19, REG_P6, REG_V16,
                              INS_OPTS_SCALABLE_S); // FMINNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fminp, EA_SCALABLE, REG_V20, REG_P7, REG_V15,
                              INS_OPTS_SCALABLE_H); // FMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_HJ_3A
    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_2BYTE, REG_V21, REG_P6, REG_V14,
                              INS_OPTS_SCALABLE_H); // FADDA   <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_4BYTE, REG_V22, REG_P5, REG_V13,
                              INS_OPTS_SCALABLE_S); // FADDA   <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_8BYTE, REG_V23, REG_P4, REG_V12,
                              INS_OPTS_SCALABLE_D); // FADDA   <V><dn>, <Pg>, <V><dn>, <Zm>.<T>

    // IF_SVE_HL_3A
    theEmitter->emitIns_R_R_R(INS_sve_fabd, EA_SCALABLE, REG_V24, REG_P3, REG_V11,
                              INS_OPTS_SCALABLE_H); // FABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fadd, EA_SCALABLE, REG_V25, REG_P2, REG_V10,
                              INS_OPTS_SCALABLE_S); // FADD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
#ifdef ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_famax, EA_SCALABLE, REG_V26, REG_P1, REG_V9, INS_OPTS_SCALABLE_D);
    // FAMAX   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_famin, EA_SCALABLE, REG_V27, REG_P0, REG_V8, INS_OPTS_SCALABLE_H);
// FAMIN   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
#endif // ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_fdiv, EA_SCALABLE, REG_V28, REG_P0, REG_V7,
                              INS_OPTS_SCALABLE_S); // FDIV    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fdivr, EA_SCALABLE, REG_V29, REG_P1, REG_V6,
                              INS_OPTS_SCALABLE_D); // FDIVR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmax, EA_SCALABLE, REG_V30, REG_P2, REG_V5,
                              INS_OPTS_SCALABLE_H); // FMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxnm, EA_SCALABLE, REG_V31, REG_P3, REG_V4,
                              INS_OPTS_SCALABLE_S); // FMAXNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmin, EA_SCALABLE, REG_V0, REG_P4, REG_V3,
                              INS_OPTS_SCALABLE_D); // FMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fminnm, EA_SCALABLE, REG_V1, REG_P5, REG_V2,
                              INS_OPTS_SCALABLE_H); // FMINNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmul, EA_SCALABLE, REG_V2, REG_P6, REG_V1,
                              INS_OPTS_SCALABLE_S); // FMUL    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmulx, EA_SCALABLE, REG_V3, REG_P7, REG_V0,
                              INS_OPTS_SCALABLE_D); // FMULX   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fscale, EA_SCALABLE, REG_V4, REG_P6, REG_V31,
                              INS_OPTS_SCALABLE_H); // FSCALE  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fsub, EA_SCALABLE, REG_V5, REG_P5, REG_V30,
                              INS_OPTS_SCALABLE_S); // FSUB    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fsubr, EA_SCALABLE, REG_V6, REG_P4, REG_V29,
                              INS_OPTS_SCALABLE_D); // FSUBR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>

    // IF_SVE_HT_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_facge, EA_SCALABLE, REG_P0, REG_P0, REG_V10, REG_V31,
                                INS_OPTS_SCALABLE_H); // FACGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_facgt, EA_SCALABLE, REG_P15, REG_P1, REG_V20, REG_V21,
                                INS_OPTS_SCALABLE_S); // FACGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_facle, EA_SCALABLE, REG_P1, REG_P2, REG_V0, REG_V11,
                                INS_OPTS_SCALABLE_D); // FACLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_faclt, EA_SCALABLE, REG_P14, REG_P3, REG_V30, REG_V1,
                                INS_OPTS_SCALABLE_H); // FACLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmeq, EA_SCALABLE, REG_P2, REG_P4, REG_V28, REG_V8,
                                INS_OPTS_SCALABLE_S); // FCMEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmge, EA_SCALABLE, REG_P13, REG_P5, REG_V8, REG_V18,
                                INS_OPTS_SCALABLE_D); // FCMGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmgt, EA_SCALABLE, REG_P3, REG_P6, REG_V18, REG_V28,
                                INS_OPTS_SCALABLE_H); // FCMGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmle, EA_SCALABLE, REG_P12, REG_P7, REG_V1, REG_V30,
                                INS_OPTS_SCALABLE_S); // FCMLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmlt, EA_SCALABLE, REG_P4, REG_P0, REG_V11, REG_V0,
                                INS_OPTS_SCALABLE_D); // FCMLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmne, EA_SCALABLE, REG_P11, REG_P1, REG_V21, REG_V10,
                                INS_OPTS_SCALABLE_H); // FCMNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_fcmuo, EA_SCALABLE, REG_P5, REG_P2, REG_V31, REG_V20,
                                INS_OPTS_SCALABLE_S); // FCMUO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_AF_3A
    theEmitter->emitIns_R_R_R(INS_sve_andv, EA_1BYTE, REG_V0, REG_P0, REG_V0,
                              INS_OPTS_SCALABLE_B); // ANDV    <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_eorv, EA_2BYTE, REG_V1, REG_P1, REG_V1,
                              INS_OPTS_SCALABLE_H); // EORV    <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_orv, EA_4BYTE, REG_V2, REG_P2, REG_V2,
                              INS_OPTS_SCALABLE_S); // ORV     <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_orv, EA_8BYTE, REG_V3, REG_P3, REG_V3,
                              INS_OPTS_SCALABLE_D); // ORV     <V><d>, <Pg>, <Zn>.<T>

// IF_SVE_AG_3A
#ifdef ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_andqv, EA_8BYTE, REG_V4, REG_P4, REG_V4, INS_OPTS_SCALABLE_B);
    // ANDQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_eorqv, EA_8BYTE, REG_V5, REG_P5, REG_V5, INS_OPTS_SCALABLE_H);
    // EORQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_orqv, EA_8BYTE, REG_V6, REG_P6, REG_V6, INS_OPTS_SCALABLE_S);
    // ORQV    <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_orqv, EA_8BYTE, REG_V7, REG_P7, REG_V7, INS_OPTS_SCALABLE_D);
// ORQV    <Vd>.<T>, <Pg>, <Zn>.<Tb>
#endif // ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED

    // IF_SVE_AI_3A
    theEmitter->emitIns_R_R_R(INS_sve_saddv, EA_1BYTE, REG_V1, REG_P4, REG_V2,
                              INS_OPTS_SCALABLE_B); // SADDV   <Dd>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_saddv, EA_2BYTE, REG_V2, REG_P5, REG_V3,
                              INS_OPTS_SCALABLE_H); // SADDV   <Dd>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uaddv, EA_4BYTE, REG_V3, REG_P6, REG_V4,
                              INS_OPTS_SCALABLE_S); // UADDV   <Dd>, <Pg>, <Zn>.<T>

// IF_SVE_AJ_3A
#ifdef ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_addqv, EA_8BYTE, REG_V21, REG_P7, REG_V22, INS_OPTS_SCALABLE_B);
// ADDQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>
#endif // ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED

    // IF_SVE_AK_3A
    theEmitter->emitIns_R_R_R(INS_sve_smaxv, EA_8BYTE, REG_V15, REG_P7, REG_V4,
                              INS_OPTS_SCALABLE_D); // SMAXV   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sminv, EA_4BYTE, REG_V16, REG_P6, REG_V14,
                              INS_OPTS_SCALABLE_S); // SMINV   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umaxv, EA_2BYTE, REG_V17, REG_P5, REG_V24,
                              INS_OPTS_SCALABLE_H); // UMAXV   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uminv, EA_1BYTE, REG_V18, REG_P4, REG_V31,
                              INS_OPTS_SCALABLE_B); // UMINV   <V><d>, <Pg>, <Zn>.<T>

// IF_SVE_AL_3A
#ifdef ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_smaxqv, EA_8BYTE, REG_V0, REG_P5, REG_V25, INS_OPTS_SCALABLE_B);
    // SMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_sminqv, EA_8BYTE, REG_V1, REG_P4, REG_V24, INS_OPTS_SCALABLE_H);
    // SMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_umaxqv, EA_8BYTE, REG_V2, REG_P3, REG_V23, INS_OPTS_SCALABLE_S);
    // UMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_uminqv, EA_8BYTE, REG_V3, REG_P2, REG_V22, INS_OPTS_SCALABLE_D);
// UMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
#endif // ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED

    // IF_SVE_AP_3A
    theEmitter->emitIns_R_R_R(INS_sve_cls, EA_SCALABLE, REG_V31, REG_P0, REG_V0,
                              INS_OPTS_SCALABLE_B); // CLS     <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_clz, EA_SCALABLE, REG_V30, REG_P1, REG_V1,
                              INS_OPTS_SCALABLE_H); // CLZ     <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_cnot, EA_SCALABLE, REG_V29, REG_P2, REG_V2,
                              INS_OPTS_SCALABLE_S); // CNOT    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_cnt, EA_SCALABLE, REG_V28, REG_P3, REG_V3,
                              INS_OPTS_SCALABLE_D); // CNT     <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fabs, EA_SCALABLE, REG_V27, REG_P4, REG_V4,
                              INS_OPTS_SCALABLE_H); // FABS    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fneg, EA_SCALABLE, REG_V26, REG_P5, REG_V5,
                              INS_OPTS_SCALABLE_S); // FNEG    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_not, EA_SCALABLE, REG_V25, REG_P6, REG_V6,
                              INS_OPTS_SCALABLE_B); // NOT     <Zd>.<T>, <Pg>/M, <Zn>.<T>

    // IF_SVE_AQ_3A
    theEmitter->emitIns_R_R_R(INS_sve_abs, EA_SCALABLE, REG_V24, REG_P7, REG_V7,
                              INS_OPTS_SCALABLE_B); // ABS     <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_neg, EA_SCALABLE, REG_V23, REG_P0, REG_V8,
                              INS_OPTS_SCALABLE_S); // NEG     <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxtb, EA_SCALABLE, REG_V22, REG_P1, REG_V9,
                              INS_OPTS_SCALABLE_H); // SXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxtb, EA_SCALABLE, REG_V22, REG_P1, REG_V9,
                              INS_OPTS_SCALABLE_S); // SXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxtb, EA_SCALABLE, REG_V22, REG_P1, REG_V9,
                              INS_OPTS_SCALABLE_D); // SXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxth, EA_SCALABLE, REG_V21, REG_P2, REG_V10,
                              INS_OPTS_SCALABLE_S); // SXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxth, EA_SCALABLE, REG_V21, REG_P2, REG_V10,
                              INS_OPTS_SCALABLE_D); // SXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sxtw, EA_SCALABLE, REG_V20, REG_P3, REG_V11,
                              INS_OPTS_SCALABLE_D); // SXTW    <Zd>.D, <Pg>/M, <Zn>.D
    theEmitter->emitIns_R_R_R(INS_sve_uxtb, EA_SCALABLE, REG_V19, REG_P4, REG_V12,
                              INS_OPTS_SCALABLE_H); // UXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uxtb, EA_SCALABLE, REG_V19, REG_P4, REG_V12,
                              INS_OPTS_SCALABLE_S); // UXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uxtb, EA_SCALABLE, REG_V19, REG_P4, REG_V12,
                              INS_OPTS_SCALABLE_D); // UXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uxth, EA_SCALABLE, REG_V18, REG_P5, REG_V13,
                              INS_OPTS_SCALABLE_S); // UXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uxth, EA_SCALABLE, REG_V18, REG_P5, REG_V13,
                              INS_OPTS_SCALABLE_D); // UXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uxtw, EA_SCALABLE, REG_V17, REG_P6, REG_V14,
                              INS_OPTS_SCALABLE_D); // UXTW    <Zd>.D, <Pg>/M, <Zn>.D

    // IF_SVE_AR_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_mla, EA_SCALABLE, REG_V0, REG_P0, REG_P0, REG_V19,
                                INS_OPTS_SCALABLE_B); // MLA     <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_mls, EA_SCALABLE, REG_V2, REG_P1, REG_V31, REG_V31,
                                INS_OPTS_SCALABLE_H); // MLS     <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_AS_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_mad, EA_SCALABLE, REG_V4, REG_P2, REG_V23, REG_V12,
                                INS_OPTS_SCALABLE_S); // MAD     <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>
    theEmitter->emitIns_R_R_R_R(INS_sve_msb, EA_SCALABLE, REG_V8, REG_P7, REG_V12, REG_V22,
                                INS_OPTS_SCALABLE_D); // MSB     <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>

    // IF_SVE_AT_3A
    theEmitter->emitIns_R_R_R(INS_sve_add, EA_SCALABLE, REG_V0, REG_V0, REG_V0, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_UNPREDICATED); // ADD     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqadd, EA_SCALABLE, REG_V3, REG_V31, REG_V12, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_UNPREDICATED); // SQADD   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqsub, EA_SCALABLE, REG_V7, REG_V0, REG_V31, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_UNPREDICATED); // SQSUB   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sub, EA_SCALABLE, REG_V19, REG_V7, REG_V13, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_UNPREDICATED); // SUB     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqadd, EA_SCALABLE, REG_V23, REG_V28, REG_V29, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_UNPREDICATED); // UQADD   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_uqsub, EA_SCALABLE, REG_V31, REG_V31, REG_V31, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_UNPREDICATED); // UQSUB   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_BA_3A
    theEmitter->emitIns_R_R_R(INS_sve_index, EA_4BYTE, REG_V24, REG_ZR, REG_R9,
                              INS_OPTS_SCALABLE_B); // INDEX   <Zd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_index, EA_8BYTE, REG_V12, REG_R15, REG_R0,
                              INS_OPTS_SCALABLE_D); // INDEX   <Zd>.<T>, <R><n>, <R><m>

    // IF_SVE_BD_3A
    theEmitter->emitIns_R_R_R(INS_sve_mul, EA_SCALABLE, REG_V5, REG_V0, REG_V31, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_UNPREDICATED); // MUL     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_smulh, EA_SCALABLE, REG_V0, REG_V31, REG_V5, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_UNPREDICATED); // SMULH   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_umulh, EA_SCALABLE, REG_V31, REG_V5, REG_V0, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_UNPREDICATED); // UMULH   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_BE_3A
    theEmitter->emitIns_R_R_R(INS_sve_sqdmulh, EA_SCALABLE, REG_V7, REG_V28, REG_V0,
                              INS_OPTS_SCALABLE_B); // SQDMULH <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqrdmulh, EA_SCALABLE, REG_V23, REG_V3, REG_V31,
                              INS_OPTS_SCALABLE_H); // SQRDMULH <Zd>.<T>, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_BG_3A
    theEmitter->emitIns_R_R_R(INS_sve_asr, EA_SCALABLE, REG_V9, REG_V31, REG_V2, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_UNPREDICATED_WIDE); // ASR     <Zd>.<T>, <Zn>.<T>, <Zm>.D
    theEmitter->emitIns_R_R_R(INS_sve_lsl, EA_SCALABLE, REG_V19, REG_V0, REG_V12, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_UNPREDICATED_WIDE); // LSL     <Zd>.<T>, <Zn>.<T>, <Zm>.D
    theEmitter->emitIns_R_R_R(INS_sve_lsr, EA_SCALABLE, REG_V29, REG_V10, REG_V22, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_UNPREDICATED_WIDE); // LSR     <Zd>.<T>, <Zn>.<T>, <Zm>.D

    // IF_SVE_BK_3A
    theEmitter->emitIns_R_R_R(INS_sve_ftssel, EA_SCALABLE, REG_V17, REG_V16, REG_V15,
                              INS_OPTS_SCALABLE_D); // FTSSEL  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>

    // IF_SVE_BL_1A
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cntb, EA_8BYTE, REG_R0, SVE_PATTERN_POW2,
                                    1); // CNTB    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cntd, EA_8BYTE, REG_R30, SVE_PATTERN_VL1,
                                    16); // CNTD    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cnth, EA_8BYTE, REG_R12, SVE_PATTERN_VL7,
                                    5); // CNTH    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cntw, EA_8BYTE, REG_R23, SVE_PATTERN_VL256,
                                    7); // CNTW    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cntb, EA_8BYTE, REG_R21, SVE_PATTERN_MUL4,
                                    8); // CNTB    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cntd, EA_8BYTE, REG_R15, SVE_PATTERN_MUL3,
                                    10); // CNTD    <Xd>{, <pattern>{, MUL #<imm>}}
    theEmitter->emitIns_R_PATTERN_I(INS_sve_cnth, EA_8BYTE, REG_R5, SVE_PATTERN_ALL,
                                    13); // CNTH    <Xd>{, <pattern>{, MUL #<imm>}}

    // IF_SVE_CL_3A
    theEmitter->emitIns_R_R_R(INS_sve_compact, EA_SCALABLE, REG_V16, REG_P7, REG_V13,
                              INS_OPTS_SCALABLE_S); // COMPACT <Zd>.<T>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_compact, EA_SCALABLE, REG_V15, REG_P0, REG_V12,
                              INS_OPTS_SCALABLE_D); // COMPACT <Zd>.<T>, <Pg>, <Zn>.<T>

    // IF_SVE_CP_3A
    theEmitter->emitIns_R_R_R(INS_sve_cpy, EA_1BYTE, REG_V14, REG_P1, REG_V11, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // CPY     <Zd>.<T>, <Pg>/M, <V><n>
    theEmitter->emitIns_R_R_R(INS_sve_cpy, EA_4BYTE, REG_V13, REG_P2, REG_V10, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // CPY     <Zd>.<T>, <Pg>/M, <V><n>
    theEmitter->emitIns_R_R_R(INS_sve_mov, EA_2BYTE, REG_V12, REG_P3, REG_V9, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // MOV     <Zd>.<T>, <Pg>/M, <V><n>
    theEmitter->emitIns_R_R_R(INS_sve_mov, EA_8BYTE, REG_V11, REG_P4, REG_V8, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // MOV     <Zd>.<T>, <Pg>/M, <V><n>

    // IF_SVE_CQ_3A
    // Note: EA_4BYTE used for B and H (source register is W)
    theEmitter->emitIns_R_R_R(INS_sve_cpy, EA_8BYTE, REG_V10, REG_P5, REG_SP,
                              INS_OPTS_SCALABLE_D); // CPY     <Zd>.<T>, <Pg>/M, <R><n|SP>
    theEmitter->emitIns_R_R_R(INS_sve_cpy, EA_4BYTE, REG_V9, REG_P6, REG_R30,
                              INS_OPTS_SCALABLE_H); // CPY     <Zd>.<T>, <Pg>/M, <R><n|SP>
    theEmitter->emitIns_R_R_R(INS_sve_mov, EA_4BYTE, REG_V8, REG_P7, REG_R29,
                              INS_OPTS_SCALABLE_S); // MOV     <Zd>.<T>, <Pg>/M, <R><n|SP>
    theEmitter->emitIns_R_R_R(INS_sve_mov, EA_4BYTE, REG_V7, REG_P0, REG_R28,
                              INS_OPTS_SCALABLE_B); // MOV     <Zd>.<T>, <Pg>/M, <R><n|SP>

    // IF_SVE_CR_3A
    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_1BYTE, REG_V6, REG_P1, REG_V27, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // LASTA   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_2BYTE, REG_V5, REG_P2, REG_V26, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // LASTA   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_V4, REG_P3, REG_V25, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // LASTB   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_V3, REG_P4, REG_V24, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_WITH_SIMD_SCALAR); // LASTB   <V><d>, <Pg>, <Zn>.<T>

    // IF_SVE_CS_3A
    // Note: EA_4BYTE used for B and H (source register is W)
    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R1, REG_P5, REG_V23,
                              INS_OPTS_SCALABLE_B); // LASTA   <R><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R0, REG_P6, REG_V22,
                              INS_OPTS_SCALABLE_S); // LASTA   <R><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_R30, REG_P7, REG_V21,
                              INS_OPTS_SCALABLE_H); // LASTB   <R><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_R29, REG_P0, REG_V20,
                              INS_OPTS_SCALABLE_D); // LASTB   <R><d>, <Pg>, <Zn>.<T>

    // IF_SVE_CU_3A
    theEmitter->emitIns_R_R_R(INS_sve_rbit, EA_SCALABLE, REG_V28, REG_P1, REG_V19,
                              INS_OPTS_SCALABLE_H); // RBIT    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_rbit, EA_SCALABLE, REG_V28, REG_P1, REG_V19,
                              INS_OPTS_SCALABLE_B); // RBIT    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_rbit, EA_SCALABLE, REG_V28, REG_P1, REG_V19,
                              INS_OPTS_SCALABLE_S); // RBIT    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_rbit, EA_SCALABLE, REG_V28, REG_P1, REG_V19,
                              INS_OPTS_SCALABLE_D); // RBIT    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revb, EA_SCALABLE, REG_V27, REG_P2, REG_V18,
                              INS_OPTS_SCALABLE_H); // REVB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revb, EA_SCALABLE, REG_V27, REG_P2, REG_V18,
                              INS_OPTS_SCALABLE_S); // REVB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revb, EA_SCALABLE, REG_V27, REG_P2, REG_V18,
                              INS_OPTS_SCALABLE_D); // REVB    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revh, EA_SCALABLE, REG_V26, REG_P3, REG_V17,
                              INS_OPTS_SCALABLE_S); // REVH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revh, EA_SCALABLE, REG_V26, REG_P3, REG_V17,
                              INS_OPTS_SCALABLE_D); // REVH    <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_revw, EA_SCALABLE, REG_V25, REG_P4, REG_V16,
                              INS_OPTS_SCALABLE_D); // REVW    <Zd>.D, <Pg>/M, <Zn>.D

    // IF_SVE_EQ_3A
    // Note: Scalable size is the size of the destination <T>, not the source <Tb>.
    theEmitter->emitIns_R_R_R(INS_sve_sadalp, EA_SCALABLE, REG_V26, REG_P3, REG_V8,
                              INS_OPTS_SCALABLE_H); // SADALP  <Zda>.<T>, <Pg>/M, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_uadalp, EA_SCALABLE, REG_V27, REG_P2, REG_V9,
                              INS_OPTS_SCALABLE_S); // UADALP  <Zda>.<T>, <Pg>/M, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_uadalp, EA_SCALABLE, REG_V28, REG_P0, REG_V31,
                              INS_OPTS_SCALABLE_D); // UADALP  <Zda>.<T>, <Pg>/M, <Zn>.<Tb>

    // IF_SVE_ES_3A
    theEmitter->emitIns_R_R_R(INS_sve_sqabs, EA_SCALABLE, REG_V29, REG_P7, REG_V0,
                              INS_OPTS_SCALABLE_B); // SQABS   <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqneg, EA_SCALABLE, REG_V31, REG_P6, REG_V1,
                              INS_OPTS_SCALABLE_H); // SQNEG   <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqneg, EA_SCALABLE, REG_V0, REG_P5, REG_V2,
                              INS_OPTS_SCALABLE_S); // SQNEG   <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_sqneg, EA_SCALABLE, REG_V1, REG_P4, REG_V3,
                              INS_OPTS_SCALABLE_D); // SQNEG   <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_urecpe, EA_SCALABLE, REG_V2, REG_P3, REG_V4,
                              INS_OPTS_SCALABLE_S); // URECPE  <Zd>.S, <Pg>/M, <Zn>.S
    theEmitter->emitIns_R_R_R(INS_sve_ursqrte, EA_SCALABLE, REG_V3, REG_P0, REG_V5,
                              INS_OPTS_SCALABLE_S); // URSQRTE <Zd>.S, <Pg>/M, <Zn>.S

    // IF_SVE_GA_2A
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrn, EA_SCALABLE, REG_V0, REG_V0, 5,
                              INS_OPTS_SCALABLE_H); // SQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrun, EA_SCALABLE, REG_V0, REG_V0, 5,
                              INS_OPTS_SCALABLE_H); // SQRSHRUN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqrshrn, EA_SCALABLE, REG_V0, REG_V0, 5,
                              INS_OPTS_SCALABLE_H); // UQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrn, EA_SCALABLE, REG_V0, REG_V2, 16,
                              INS_OPTS_SCALABLE_H); // SQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrun, EA_SCALABLE, REG_V0, REG_V4, 7,
                              INS_OPTS_SCALABLE_H); // SQRSHRUN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqrshrn, EA_SCALABLE, REG_V0, REG_V6, 1,
                              INS_OPTS_SCALABLE_H); // UQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrn, EA_SCALABLE, REG_V30, REG_V16, 16,
                              INS_OPTS_SCALABLE_H); // SQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_sqrshrun, EA_SCALABLE, REG_V16, REG_V8, 7,
                              INS_OPTS_SCALABLE_H); // SQRSHRUN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>
    theEmitter->emitIns_R_R_I(INS_sve_uqrshrn, EA_SCALABLE, REG_V15, REG_V12, 1,
                              INS_OPTS_SCALABLE_H); // UQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>

    // IF_SVE_DL_2A
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R0, REG_P0, INS_OPTS_SCALABLE_B,
                            INS_SCALABLE_OPTS_VL_2X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R1, REG_P1, INS_OPTS_SCALABLE_B,
                            INS_SCALABLE_OPTS_VL_4X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R2, REG_P2, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_VL_2X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R3, REG_P3, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_VL_4X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_S,
                            INS_SCALABLE_OPTS_VL_2X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_S,
                            INS_SCALABLE_OPTS_VL_4X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_D,
                            INS_SCALABLE_OPTS_VL_2X); // CNTP <Xd>, <PNn>.<T>, <vl>
    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D,
                            INS_SCALABLE_OPTS_VL_4X); // CNTP <Xd>, <PNn>.<T>, <vl>

    // IF_SVE_DM_2A
    theEmitter->emitIns_R_R(INS_sve_decp, EA_8BYTE, REG_R0, REG_P0, INS_OPTS_SCALABLE_B); // DECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_decp, EA_8BYTE, REG_R1, REG_P1, INS_OPTS_SCALABLE_H); // DECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_decp, EA_8BYTE, REG_R2, REG_P2, INS_OPTS_SCALABLE_S); // DECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_decp, EA_8BYTE, REG_R3, REG_P3, INS_OPTS_SCALABLE_D); // DECP <Xdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_incp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_B); // INCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_incp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_H); // INCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_incp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_S); // INCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_incp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D); // INCP <Xdn>, <Pm>.<T>

    // IF_SVE_DN_2A
    // Note: B is reserved
    theEmitter->emitIns_R_R(INS_sve_decp, EA_SCALABLE, REG_V0, REG_P0, INS_OPTS_SCALABLE_H); // DECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_decp, EA_SCALABLE, REG_V1, REG_P1, INS_OPTS_SCALABLE_S); // DECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_decp, EA_SCALABLE, REG_V2, REG_P2, INS_OPTS_SCALABLE_D); // DECP <Zdn>.<T>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_incp, EA_SCALABLE, REG_V3, REG_P3, INS_OPTS_SCALABLE_H); // INCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_incp, EA_SCALABLE, REG_V4, REG_P4, INS_OPTS_SCALABLE_S); // INCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_incp, EA_SCALABLE, REG_V5, REG_P5, INS_OPTS_SCALABLE_D); // INCP <Zdn>.<T>, <Pm>.<T>

    // IF_SVE_DO_2A
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_4BYTE, REG_R0, REG_P0,
                            INS_OPTS_SCALABLE_B); // SQDECP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_4BYTE, REG_R1, REG_P1,
                            INS_OPTS_SCALABLE_H); // SQDECP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_4BYTE, REG_R2, REG_P2,
                            INS_OPTS_SCALABLE_S); // SQDECP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_4BYTE, REG_R3, REG_P3,
                            INS_OPTS_SCALABLE_D); // SQDECP <Xdn>, <Pm>.<T>, <Wdn>

    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_B); // SQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_H); // SQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_S); // SQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D); // SQDECP <Xdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_4BYTE, REG_R0, REG_P0,
                            INS_OPTS_SCALABLE_H); // SQINCP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_4BYTE, REG_R1, REG_P1,
                            INS_OPTS_SCALABLE_S); // SQINCP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_4BYTE, REG_R2, REG_P2,
                            INS_OPTS_SCALABLE_B); // SQINCP <Xdn>, <Pm>.<T>, <Wdn>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_4BYTE, REG_R3, REG_P3,
                            INS_OPTS_SCALABLE_D); // SQINCP <Xdn>, <Pm>.<T>, <Wdn>

    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_B); // SQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_H); // SQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_S); // SQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D); // SQINCP <Xdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_4BYTE, REG_R0, REG_P0, INS_OPTS_SCALABLE_B); // UQDECP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_4BYTE, REG_R1, REG_P1, INS_OPTS_SCALABLE_H); // UQDECP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_4BYTE, REG_R2, REG_P2, INS_OPTS_SCALABLE_S); // UQDECP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_4BYTE, REG_R3, REG_P3, INS_OPTS_SCALABLE_D); // UQDECP <Wdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_B); // UQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_H); // UQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_S); // UQDECP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D); // UQDECP <Xdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_4BYTE, REG_R0, REG_P0, INS_OPTS_SCALABLE_B); // UQINCP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_4BYTE, REG_R1, REG_P1, INS_OPTS_SCALABLE_H); // UQINCP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_4BYTE, REG_R2, REG_P2, INS_OPTS_SCALABLE_S); // UQINCP <Wdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_4BYTE, REG_R3, REG_P3, INS_OPTS_SCALABLE_D); // UQINCP <Wdn>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_B); // UQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_H); // UQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_S); // UQINCP <Xdn>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D); // UQINCP <Xdn>, <Pm>.<T>

    // IF_SVE_DP_2A
    // NOTE: B is reserved
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_SCALABLE, REG_V0, REG_P0,
                            INS_OPTS_SCALABLE_H); // SQDECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_SCALABLE, REG_V1, REG_P1,
                            INS_OPTS_SCALABLE_S); // SQDECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqdecp, EA_SCALABLE, REG_V2, REG_P2,
                            INS_OPTS_SCALABLE_D); // SQDECP <Zdn>.<T>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_SCALABLE, REG_V3, REG_P3,
                            INS_OPTS_SCALABLE_H); // SQINCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_SCALABLE, REG_V4, REG_P4,
                            INS_OPTS_SCALABLE_S); // SQINCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_sqincp, EA_SCALABLE, REG_V5, REG_P5,
                            INS_OPTS_SCALABLE_D); // SQINCP <Zdn>.<T>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_SCALABLE, REG_V6, REG_P6,
                            INS_OPTS_SCALABLE_H); // UQDECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_SCALABLE, REG_V7, REG_P7,
                            INS_OPTS_SCALABLE_S); // UQDECP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqdecp, EA_SCALABLE, REG_V8, REG_P0,
                            INS_OPTS_SCALABLE_D); // UQDECP <Zdn>.<T>, <Pm>.<T>

    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_SCALABLE, REG_V9, REG_P1,
                            INS_OPTS_SCALABLE_H); // UQINCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_SCALABLE, REG_V10, REG_P2,
                            INS_OPTS_SCALABLE_S); // UQINCP <Zdn>.<T>, <Pm>.<T>
    theEmitter->emitIns_R_R(INS_sve_uqincp, EA_SCALABLE, REG_V11, REG_P3,
                            INS_OPTS_SCALABLE_D); // UQINCP <Zdn>.<T>, <Pm>.<T>

    // IF_SVE_DQ_0A
    theEmitter->emitIns_I(INS_sve_setffr, EA_PTRSIZE, 0); // SETFFR

    // IF_SVE_DR_1A
    theEmitter->emitIns_R(INS_sve_wrffr, EA_SCALABLE, REG_P0); // WRFFR <Pn>.B

    // IF_SVE_DS_2A
    theEmitter->emitIns_R_R(INS_sve_ctermeq, EA_8BYTE, REG_R0, REG_R1, INS_OPTS_NONE); // CTERMEQ <R><n>, <R><m>
    theEmitter->emitIns_R_R(INS_sve_ctermeq, EA_4BYTE, REG_R2, REG_R3, INS_OPTS_NONE); // CTERMEQ <R><n>, <R><m>
    theEmitter->emitIns_R_R(INS_sve_ctermne, EA_8BYTE, REG_R4, REG_R5, INS_OPTS_NONE); // CTERMNE <R><n>, <R><m>
    theEmitter->emitIns_R_R(INS_sve_ctermne, EA_4BYTE, REG_R6, REG_R7, INS_OPTS_NONE); // CTERMNE <R><n>, <R><m>

    // IF_SVE_GD_2A
    theEmitter->emitIns_R_R(INS_sve_sqxtnb, EA_SCALABLE, REG_V0, REG_V5, INS_OPTS_SCALABLE_B);  // SQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtnt, EA_SCALABLE, REG_V3, REG_V7, INS_OPTS_SCALABLE_B);  // SQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunb, EA_SCALABLE, REG_V0, REG_V8, INS_OPTS_SCALABLE_B); // SQXTUNB <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunt, EA_SCALABLE, REG_V5, REG_V3, INS_OPTS_SCALABLE_B); // SQXTUNT <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnb, EA_SCALABLE, REG_V0, REG_V7, INS_OPTS_SCALABLE_B);  // UQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnt, EA_SCALABLE, REG_V0, REG_V1, INS_OPTS_SCALABLE_B);  // UQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtnb, EA_SCALABLE, REG_V0, REG_V0, INS_OPTS_SCALABLE_H);  // SQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtnt, EA_SCALABLE, REG_V9, REG_V9, INS_OPTS_SCALABLE_H);  // SQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunb, EA_SCALABLE, REG_V0, REG_V6, INS_OPTS_SCALABLE_H); // SQXTUNB <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunt, EA_SCALABLE, REG_V0, REG_V4, INS_OPTS_SCALABLE_H); // SQXTUNT <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnb, EA_SCALABLE, REG_V6, REG_V2, INS_OPTS_SCALABLE_H);  // UQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnt, EA_SCALABLE, REG_V0, REG_V1, INS_OPTS_SCALABLE_H);  // UQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtnb, EA_SCALABLE, REG_V7, REG_V7, INS_OPTS_SCALABLE_S);  // SQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtnt, EA_SCALABLE, REG_V0, REG_V8, INS_OPTS_SCALABLE_S);  // SQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunb, EA_SCALABLE, REG_V0, REG_V9, INS_OPTS_SCALABLE_S); // SQXTUNB <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_sqxtunt, EA_SCALABLE, REG_V0, REG_V4, INS_OPTS_SCALABLE_S); // SQXTUNT <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnb, EA_SCALABLE, REG_V3, REG_V9, INS_OPTS_SCALABLE_S);  // UQXTNB  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    theEmitter->emitIns_R_R(INS_sve_uqxtnt, EA_SCALABLE, REG_V0, REG_V3, INS_OPTS_SCALABLE_S);  // UQXTNT  <Zd>.<T>,
                                                                                                // <Zn>.<Tb>
    // IF_SVE_GK_2A
    theEmitter->emitIns_R_R(INS_sve_aesd, EA_SCALABLE, REG_V0, REG_V0, INS_OPTS_SCALABLE_B); // AESD    <Zdn>.B,
                                                                                             // <Zdn>.B, <Zm>.B
    theEmitter->emitIns_R_R(INS_sve_aese, EA_SCALABLE, REG_V1, REG_V2, INS_OPTS_SCALABLE_B); // AESE    <Zdn>.B,
                                                                                             // <Zdn>.B, <Zm>.B
    theEmitter->emitIns_R_R(INS_sve_sm4e, EA_SCALABLE, REG_V3, REG_V5, INS_OPTS_SCALABLE_S); // SM4E    <Zdn>.S,
                                                                                             // <Zdn>.S, <Zm>.S

    // IF_SVE_GL_1A
    theEmitter->emitIns_R(INS_sve_aesimc, EA_SCALABLE, REG_V0); // AESIMC  <Zdn>.B, <Zdn>.B
    theEmitter->emitIns_R(INS_sve_aesmc, EA_SCALABLE, REG_V5);  // AESMC   <Zdn>.B, <Zdn>.B

// IF_SVE_GS_3A
#ifdef ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED
    theEmitter->emitIns_R_R_R(INS_sve_faddqv, EA_8BYTE, REG_V16, REG_P0, REG_V12,
                              INS_OPTS_SCALABLE_H); // FADDQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxnmqv, EA_8BYTE, REG_V17, REG_P1, REG_V11,
                              INS_OPTS_SCALABLE_S); // FMAXNMQV <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxqv, EA_8BYTE, REG_V18, REG_P3, REG_V10,
                              INS_OPTS_SCALABLE_D); // FMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_fminnmqv, EA_8BYTE, REG_V19, REG_P4, REG_V9,
                              INS_OPTS_SCALABLE_H); // FMINNMQV <Vd>.<T>, <Pg>, <Zn>.<Tb>
    theEmitter->emitIns_R_R_R(INS_sve_fminqv, EA_8BYTE, REG_V20, REG_P5, REG_V8,
                              INS_OPTS_SCALABLE_D); // FMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>
#endif                                              // ALL_ARM64_EMITTER_UNIT_TESTS_SVE_UNSUPPORTED

    // IF_SVE_HE_3A
    theEmitter->emitIns_R_R_R(INS_sve_faddv, EA_2BYTE, REG_V21, REG_P7, REG_V7,
                              INS_OPTS_SCALABLE_H); // FADDV   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxnmv, EA_2BYTE, REG_V22, REG_P6, REG_V6,
                              INS_OPTS_SCALABLE_H); // FMAXNMV <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fmaxv, EA_4BYTE, REG_V23, REG_P5, REG_V5,
                              INS_OPTS_SCALABLE_S); // FMAXV   <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fminnmv, EA_8BYTE, REG_V24, REG_P4, REG_V4,
                              INS_OPTS_SCALABLE_D); // FMINNMV <V><d>, <Pg>, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fminv, EA_4BYTE, REG_V25, REG_P3, REG_V3,
                              INS_OPTS_SCALABLE_S); // FMINV   <V><d>, <Pg>, <Zn>.<T>

    // IF_SVE_HQ_3A
    theEmitter->emitIns_R_R_R(INS_sve_frinta, EA_SCALABLE, REG_V26, REG_P7, REG_V2,
                              INS_OPTS_SCALABLE_H); // FRINTA  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frinti, EA_SCALABLE, REG_V27, REG_P6, REG_V1,
                              INS_OPTS_SCALABLE_S); // FRINTI  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frintm, EA_SCALABLE, REG_V28, REG_P5, REG_V0,
                              INS_OPTS_SCALABLE_D); // FRINTM  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frintn, EA_SCALABLE, REG_V29, REG_P4, REG_V10,
                              INS_OPTS_SCALABLE_H); // FRINTN  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frintp, EA_SCALABLE, REG_V30, REG_P3, REG_V11,
                              INS_OPTS_SCALABLE_S); // FRINTP  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frintx, EA_SCALABLE, REG_V31, REG_P2, REG_V12,
                              INS_OPTS_SCALABLE_D); // FRINTX  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_frintz, EA_SCALABLE, REG_V0, REG_P0, REG_V13,
                              INS_OPTS_SCALABLE_H); // FRINTZ  <Zd>.<T>, <Pg>/M, <Zn>.<T>

    // IF_SVE_HR_3A
    theEmitter->emitIns_R_R_R(INS_sve_frecpx, EA_SCALABLE, REG_V5, REG_P5, REG_V5,
                              INS_OPTS_SCALABLE_H); // FRECPX  <Zd>.<T>, <Pg>/M, <Zn>.<T>
    theEmitter->emitIns_R_R_R(INS_sve_fsqrt, EA_SCALABLE, REG_V6, REG_P6, REG_V6,
                              INS_OPTS_SCALABLE_S); // FSQRT   <Zd>.<T>, <Pg>/M, <Zn>.<T>

    // IF_SVE_DT_3A
    theEmitter->emitIns_R_R_R(INS_sve_whilege, EA_4BYTE, REG_P0, REG_R0, REG_R1,
                              INS_OPTS_SCALABLE_B); // WHILEGE <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilege, EA_8BYTE, REG_P1, REG_R2, REG_R3,
                              INS_OPTS_SCALABLE_B); // WHILEGE <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilegt, EA_4BYTE, REG_P2, REG_R4, REG_R5,
                              INS_OPTS_SCALABLE_B); // WHILEGT <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilegt, EA_8BYTE, REG_P3, REG_R6, REG_R7,
                              INS_OPTS_SCALABLE_B); // WHILEGT <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilehi, EA_4BYTE, REG_P4, REG_R8, REG_R9,
                              INS_OPTS_SCALABLE_H); // WHILEHI <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilehi, EA_8BYTE, REG_P5, REG_R10, REG_R11,
                              INS_OPTS_SCALABLE_H); // WHILEHI <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilehs, EA_4BYTE, REG_P6, REG_R12, REG_R13,
                              INS_OPTS_SCALABLE_H); // WHILEHS <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilehs, EA_8BYTE, REG_P7, REG_R14, REG_R15,
                              INS_OPTS_SCALABLE_H); // WHILEHS <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilele, EA_4BYTE, REG_P8, REG_R0, REG_R1,
                              INS_OPTS_SCALABLE_S); // WHILELE <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilele, EA_8BYTE, REG_P9, REG_R2, REG_R3,
                              INS_OPTS_SCALABLE_S); // WHILELE <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilelo, EA_4BYTE, REG_P10, REG_R4, REG_R5,
                              INS_OPTS_SCALABLE_S); // WHILELO <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilelo, EA_8BYTE, REG_P11, REG_R6, REG_R7,
                              INS_OPTS_SCALABLE_S); // WHILELO <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilels, EA_4BYTE, REG_P12, REG_R8, REG_R9,
                              INS_OPTS_SCALABLE_D); // WHILELS <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilels, EA_8BYTE, REG_P13, REG_R10, REG_R11,
                              INS_OPTS_SCALABLE_D); // WHILELS <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilelt, EA_4BYTE, REG_P14, REG_R12, REG_R13,
                              INS_OPTS_SCALABLE_D); // WHILELT <Pd>.<T>, <R><n>, <R><m>
    theEmitter->emitIns_R_R_R(INS_sve_whilelt, EA_8BYTE, REG_P15, REG_R14, REG_R15,
                              INS_OPTS_SCALABLE_D); // WHILELT <Pd>.<T>, <R><n>, <R><m>

    // IF_SVE_DU_3A
    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P0, REG_R0, REG_R1,
                              INS_OPTS_SCALABLE_B); // WHILERW <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P1, REG_R2, REG_R3,
                              INS_OPTS_SCALABLE_H); // WHILERW <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P2, REG_R4, REG_R5,
                              INS_OPTS_SCALABLE_S); // WHILERW <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P3, REG_R6, REG_R7,
                              INS_OPTS_SCALABLE_D); // WHILERW <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P4, REG_R8, REG_R9,
                              INS_OPTS_SCALABLE_B); // WHILEWR <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P5, REG_R10, REG_R11,
                              INS_OPTS_SCALABLE_H); // WHILEWR <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P6, REG_R12, REG_R13,
                              INS_OPTS_SCALABLE_S); // WHILEWR <Pd>.<T>, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P7, REG_R14, REG_R15,
                              INS_OPTS_SCALABLE_D); // WHILEWR <Pd>.<T>, <Xn>, <Xm>

    // IF_SVE_DX_3A
    theEmitter->emitIns_R_R_R(INS_sve_whilege, EA_8BYTE, REG_P0, REG_R0, REG_R1, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILEGE {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilegt, EA_8BYTE, REG_P1, REG_R2, REG_R3, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILEGT {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilehi, EA_8BYTE, REG_P2, REG_R4, REG_R5, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILEHI {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilehs, EA_8BYTE, REG_P3, REG_R6, REG_R7, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILEHS {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilele, EA_8BYTE, REG_P4, REG_R8, REG_R9, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILELE {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilelo, EA_8BYTE, REG_P5, REG_R10, REG_R11, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILELO {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilels, EA_8BYTE, REG_P6, REG_R12, REG_R13, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILELS {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>
    theEmitter->emitIns_R_R_R(INS_sve_whilelt, EA_8BYTE, REG_P7, REG_R14, REG_R15, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR); // WHILELT {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>

    // IF_SVE_DY_3A
    theEmitter->emitIns_R_R_R(INS_sve_whilege, EA_8BYTE, REG_P8, REG_R0, REG_R1, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_2X); // WHILEGE <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilege, EA_8BYTE, REG_P9, REG_R2, REG_R3, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_4X); // WHILEGE <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilegt, EA_8BYTE, REG_P10, REG_R4, REG_R5, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_VL_2X); // WHILEGT <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilegt, EA_8BYTE, REG_P11, REG_R6, REG_R7, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_VL_4X); // WHILEGT <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilehi, EA_8BYTE, REG_P12, REG_R8, REG_R9, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_VL_2X); // WHILEHI <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilehi, EA_8BYTE, REG_P13, REG_R10, REG_R11, INS_OPTS_SCALABLE_S,
                              INS_SCALABLE_OPTS_VL_4X); // WHILEHI <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilehs, EA_8BYTE, REG_P14, REG_R12, REG_R13, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_VL_2X); // WHILEHS <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilehs, EA_8BYTE, REG_P15, REG_R14, REG_R15, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_VL_4X); // WHILEHS <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilele, EA_8BYTE, REG_P8, REG_R0, REG_R1, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_2X); // WHILELE <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilele, EA_8BYTE, REG_P9, REG_R2, REG_R3, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_4X); // WHILELE <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilelo, EA_8BYTE, REG_P10, REG_R4, REG_R5, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_VL_2X); // WHILELO <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilelo, EA_8BYTE, REG_P11, REG_R6, REG_R7, INS_OPTS_SCALABLE_H,
                              INS_SCALABLE_OPTS_VL_4X); // WHILELO <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilels, EA_8BYTE, REG_P12, REG_R8, REG_R9, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_VL_2X); // WHILELS <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilels, EA_8BYTE, REG_P13, REG_R10, REG_R11, INS_OPTS_SCALABLE_D,
                              INS_SCALABLE_OPTS_VL_4X); // WHILELS <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilelt, EA_8BYTE, REG_P14, REG_R12, REG_R13, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_2X); // WHILELT <PNd>.<T>, <Xn>, <Xm>, <vl>
    theEmitter->emitIns_R_R_R(INS_sve_whilelt, EA_8BYTE, REG_P15, REG_R14, REG_R15, INS_OPTS_SCALABLE_B,
                              INS_SCALABLE_OPTS_VL_4X); // WHILELT <PNd>.<T>, <Xn>, <Xm>, <vl>

    // IF_SVE_DZ_1A
    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P8, INS_OPTS_SCALABLE_B);  // PTRUE <PNd>.<T>
    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P9, INS_OPTS_SCALABLE_H);  // PTRUE <PNd>.<T>
    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P10, INS_OPTS_SCALABLE_S); // PTRUE <PNd>.<T>
    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P11, INS_OPTS_SCALABLE_D); // PTRUE <PNd>.<T>

    // IF_SVE_EA_1A
    // Note: B is reserved
    theEmitter->emitIns_R_F(INS_sve_fdup, EA_SCALABLE, REG_V0, 2.0, INS_OPTS_SCALABLE_H);    // FDUP <Zd>.<T>, #<const>
    theEmitter->emitIns_R_F(INS_sve_fdup, EA_SCALABLE, REG_V1, 1.0, INS_OPTS_SCALABLE_S);    // FDUP <Zd>.<T>, #<const>
    theEmitter->emitIns_R_F(INS_sve_fdup, EA_SCALABLE, REG_V2, 2.0, INS_OPTS_SCALABLE_D);    // FDUP <Zd>.<T>, #<const>
    theEmitter->emitIns_R_F(INS_sve_fmov, EA_SCALABLE, REG_V3, -10.0, INS_OPTS_SCALABLE_H);  // FMOV <Zd>.<T>, #<const>
    theEmitter->emitIns_R_F(INS_sve_fmov, EA_SCALABLE, REG_V4, -0.125, INS_OPTS_SCALABLE_S); // FMOV <Zd>.<T>, #<const>
    theEmitter->emitIns_R_F(INS_sve_fmov, EA_SCALABLE, REG_V5, 31.0, INS_OPTS_SCALABLE_D);   // FMOV <Zd>.<T>, #<const>

    // IF_SVE_EB_1A
    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V0, -128,
                            INS_OPTS_SCALABLE_B); // DUP <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V1, 0, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_SHIFT); // DUP <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V2, 5,
                            INS_OPTS_SCALABLE_S); // DUP <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V3, 127,
                            INS_OPTS_SCALABLE_D); // DUP <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_mov, EA_SCALABLE, REG_V4, 0,
                            INS_OPTS_SCALABLE_B); // MOV <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_mov, EA_SCALABLE, REG_V5, -128, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_SHIFT); // MOV <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_mov, EA_SCALABLE, REG_V6, 5, INS_OPTS_SCALABLE_S,
                            INS_SCALABLE_OPTS_SHIFT); // MOV <Zd>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_mov, EA_SCALABLE, REG_V7, 127, INS_OPTS_SCALABLE_D,
                            INS_SCALABLE_OPTS_SHIFT); // MOV <Zd>.<T>, #<imm>{, <shift>}

    // IF_SVE_EC_1A
    theEmitter->emitIns_R_I(INS_sve_add, EA_SCALABLE, REG_V0, 0,
                            INS_OPTS_SCALABLE_B); // ADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_sqadd, EA_SCALABLE, REG_V1, 0, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_SHIFT); // SQADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_sqsub, EA_SCALABLE, REG_V2, 1,
                            INS_OPTS_SCALABLE_S); // SQSUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_sub, EA_SCALABLE, REG_V3, 128,
                            INS_OPTS_SCALABLE_D); // SUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_subr, EA_SCALABLE, REG_V4, 255,
                            INS_OPTS_SCALABLE_B); // SUBR <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_uqadd, EA_SCALABLE, REG_V5, 5, INS_OPTS_SCALABLE_H,
                            INS_SCALABLE_OPTS_SHIFT); // UQADD <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}
    theEmitter->emitIns_R_I(INS_sve_uqsub, EA_SCALABLE, REG_V6, 255, INS_OPTS_SCALABLE_S,
                            INS_SCALABLE_OPTS_SHIFT); // UQSUB <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}

    // IF_SVE_ED_1A
    theEmitter->emitIns_R_I(INS_sve_smax, EA_SCALABLE, REG_V0, -128,
                            INS_OPTS_SCALABLE_B); // SMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_smax, EA_SCALABLE, REG_V1, 127,
                            INS_OPTS_SCALABLE_H); // SMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_smin, EA_SCALABLE, REG_V2, -128,
                            INS_OPTS_SCALABLE_S); // SMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_smin, EA_SCALABLE, REG_V3, 127,
                            INS_OPTS_SCALABLE_D); // SMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_umax, EA_SCALABLE, REG_V4, 0,
                            INS_OPTS_SCALABLE_B); // UMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_umax, EA_SCALABLE, REG_V5, 255,
                            INS_OPTS_SCALABLE_H); // UMAX <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_umin, EA_SCALABLE, REG_V6, 0,
                            INS_OPTS_SCALABLE_S); // UMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_umin, EA_SCALABLE, REG_V7, 255,
                            INS_OPTS_SCALABLE_D); // UMIN <Zdn>.<T>, <Zdn>.<T>, #<imm>

    // IF_SVE_EE_1A
    theEmitter->emitIns_R_I(INS_sve_mul, EA_SCALABLE, REG_V0, -128,
                            INS_OPTS_SCALABLE_B); // MUL <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_mul, EA_SCALABLE, REG_V1, 0,
                            INS_OPTS_SCALABLE_H); // MUL <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_mul, EA_SCALABLE, REG_V2, 5,
                            INS_OPTS_SCALABLE_S); // MUL <Zdn>.<T>, <Zdn>.<T>, #<imm>
    theEmitter->emitIns_R_I(INS_sve_mul, EA_SCALABLE, REG_V3, 127,
                            INS_OPTS_SCALABLE_D); // MUL <Zdn>.<T>, <Zdn>.<T>, #<imm>

    // IF_SVE_IH_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1d, EA_SCALABLE, REG_V5, REG_P3, REG_R4, 0,
                                INS_OPTS_SCALABLE_D); // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IH_3A_A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1d, EA_SCALABLE, REG_V0, REG_P2, REG_R3, 5,
                                INS_OPTS_SCALABLE_Q); // LD1D    {<Zt>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IH_3A_F
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1w, EA_SCALABLE, REG_V0, REG_P2, REG_R3, 3,
                                INS_OPTS_SCALABLE_S); // LD1W    {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1w, EA_SCALABLE, REG_V0, REG_P2, REG_R3, 3,
                                INS_OPTS_SCALABLE_D); // LD1W    {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1w, EA_SCALABLE, REG_V0, REG_P2, REG_R3, 3,
                                INS_OPTS_SCALABLE_Q); // LD1W    {<Zt>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IJ_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sw, EA_SCALABLE, REG_V0, REG_P5, REG_R3, 4,
                                INS_OPTS_SCALABLE_D); // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IJ_3A_D
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sb, EA_SCALABLE, REG_V3, REG_P0, REG_R2, 6,
                                INS_OPTS_SCALABLE_H); // LD1SB   {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sb, EA_SCALABLE, REG_V3, REG_P0, REG_R2, 6,
                                INS_OPTS_SCALABLE_S); // LD1SB   {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sb, EA_SCALABLE, REG_V3, REG_P0, REG_R2, 6,
                                INS_OPTS_SCALABLE_D); // LD1SB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IJ_3A_E
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1b, EA_SCALABLE, REG_V5, REG_P1, REG_R3, 7,
                                INS_OPTS_SCALABLE_B); // LD1B    {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1b, EA_SCALABLE, REG_V5, REG_P1, REG_R3, 7,
                                INS_OPTS_SCALABLE_H); // LD1B    {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1b, EA_SCALABLE, REG_V5, REG_P1, REG_R3, 7,
                                INS_OPTS_SCALABLE_S); // LD1B    {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1b, EA_SCALABLE, REG_V5, REG_P1, REG_R3, 7,
                                INS_OPTS_SCALABLE_D); // LD1B    {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IJ_3A_F
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sh, EA_SCALABLE, REG_V7, REG_P3, REG_R5, 2,
                                INS_OPTS_SCALABLE_S); // LD1SH   {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1sh, EA_SCALABLE, REG_V7, REG_P3, REG_R5, 2,
                                INS_OPTS_SCALABLE_D); // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IJ_3A_G
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1,
                                INS_OPTS_SCALABLE_H); // LD1H    {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1,
                                INS_OPTS_SCALABLE_S); // LD1H    {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1,
                                INS_OPTS_SCALABLE_D); // LD1H    {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IL_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1d, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 0,
                                INS_OPTS_SCALABLE_D); // LDNF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sw, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 0,
                                INS_OPTS_SCALABLE_D); // LDNF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1d, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sw, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IL_3A_A
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sh, EA_SCALABLE, REG_V0, REG_P1, REG_R5, 5,
                                INS_OPTS_SCALABLE_S); // LDNF1SH {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1w, EA_SCALABLE, REG_V0, REG_P2, REG_R4, 5,
                                INS_OPTS_SCALABLE_S); // LDNF1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sh, EA_SCALABLE, REG_V0, REG_P1, REG_R5, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1w, EA_SCALABLE, REG_V0, REG_P2, REG_R4, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1W  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IL_3A_B
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5,
                                INS_OPTS_SCALABLE_H); // LDNF1H  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sb, EA_SCALABLE, REG_V0, REG_P4, REG_R1, 5,
                                INS_OPTS_SCALABLE_H); // LDNF1SB {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5,
                                INS_OPTS_SCALABLE_S); // LDNF1H  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sb, EA_SCALABLE, REG_V0, REG_P4, REG_R1, 5,
                                INS_OPTS_SCALABLE_S); // LDNF1SB {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1H  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1sb, EA_SCALABLE, REG_V0, REG_P4, REG_R1, 5,
                                INS_OPTS_SCALABLE_D); // LDNF1SB {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IL_3A_C
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1b, EA_SCALABLE, REG_V2, REG_P5, REG_R3, -4,
                                INS_OPTS_SCALABLE_B); // LDNF1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1b, EA_SCALABLE, REG_V2, REG_P5, REG_R3, -2,
                                INS_OPTS_SCALABLE_H); // LDNF1B  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1b, EA_SCALABLE, REG_V2, REG_P5, REG_R3, 2,
                                INS_OPTS_SCALABLE_S); // LDNF1B  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1b, EA_SCALABLE, REG_V2, REG_P5, REG_R3, 1,
                                INS_OPTS_SCALABLE_D); // LDNF1B  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IM_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnt1b, EA_SCALABLE, REG_V0, REG_P1, REG_R2, -5,
                                INS_OPTS_SCALABLE_B); // LDNT1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnt1d, EA_SCALABLE, REG_V3, REG_P4, REG_R5, -1,
                                INS_OPTS_SCALABLE_D); // LDNT1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnt1h, EA_SCALABLE, REG_V6, REG_P7, REG_R8, 0,
                                INS_OPTS_SCALABLE_H); // LDNT1H  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ldnt1w, EA_SCALABLE, REG_V1, REG_P2, REG_R3, -8,
                                INS_OPTS_SCALABLE_S); // LDNT1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_IO_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rob, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 0,
                                INS_OPTS_SCALABLE_B); // LD1ROB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rod, EA_SCALABLE, REG_V4, REG_P5, REG_R6, -32,
                                INS_OPTS_SCALABLE_D); // LD1ROD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1roh, EA_SCALABLE, REG_V8, REG_P3, REG_R1, -256,
                                INS_OPTS_SCALABLE_H); // LD1ROH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1row, EA_SCALABLE, REG_V3, REG_P4, REG_R0, 224,
                                INS_OPTS_SCALABLE_S); // LD1ROW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rqb, EA_SCALABLE, REG_V6, REG_P7, REG_R8, 64,
                                INS_OPTS_SCALABLE_B); // LD1RQB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rqd, EA_SCALABLE, REG_V9, REG_P0, REG_R1, -128,
                                INS_OPTS_SCALABLE_D); // LD1RQD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rqh, EA_SCALABLE, REG_V4, REG_P5, REG_R6, 112,
                                INS_OPTS_SCALABLE_H); // LD1RQH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rqw, EA_SCALABLE, REG_V31, REG_P2, REG_R1, -16,
                                INS_OPTS_SCALABLE_S); // LD1RQW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]

    // IF_SVE_IQ_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V0, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V31, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 14,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3q, EA_SCALABLE, REG_V0, REG_P4, REG_R5, -24,
                                INS_OPTS_SCALABLE_Q); // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3q, EA_SCALABLE, REG_V0, REG_P4, REG_R5, 21,
                                INS_OPTS_SCALABLE_Q); // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V0, REG_P5, REG_R3, -32,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V0, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V12, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V13, REG_P1, REG_R2, 14,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3q, EA_SCALABLE, REG_V14, REG_P4, REG_R5, -24,
                                INS_OPTS_SCALABLE_Q); // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3q, EA_SCALABLE, REG_V15, REG_P4, REG_R5, 21,
                                INS_OPTS_SCALABLE_Q); // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V16, REG_P5, REG_R3, -32,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V27, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V28, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V29, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V30, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4q, EA_SCALABLE, REG_V31, REG_P5, REG_R3, 28,
                                INS_OPTS_SCALABLE_Q); // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2q, EA_SCALABLE, REG_V31, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_Q); // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]

    // IF_SVE_IS_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2b, EA_SCALABLE, REG_V0, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_B); // LD2B    {<Zt1>.B, <Zt2>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2d, EA_SCALABLE, REG_V4, REG_P5, REG_R7, 14,
                                INS_OPTS_SCALABLE_D); // LD2D    {<Zt1>.D, <Zt2>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2h, EA_SCALABLE, REG_V6, REG_P5, REG_R4, 8,
                                INS_OPTS_SCALABLE_H); // LD2H    {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld2w, EA_SCALABLE, REG_V0, REG_P0, REG_R1, 2,
                                INS_OPTS_SCALABLE_S); // LD2W    {<Zt1>.S, <Zt2>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
                                                      // VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3b, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 21,
                                INS_OPTS_SCALABLE_B); // LD3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3d, EA_SCALABLE, REG_V0, REG_P0, REG_R0, -24,
                                INS_OPTS_SCALABLE_D); // LD3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3h, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 21,
                                INS_OPTS_SCALABLE_H); // LD3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld3w, EA_SCALABLE, REG_V0, REG_P0, REG_R0, -24,
                                INS_OPTS_SCALABLE_S); // LD3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>/Z, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4b, EA_SCALABLE, REG_V31, REG_P2, REG_R1, -32,
                                INS_OPTS_SCALABLE_B); // LD4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4d, EA_SCALABLE, REG_V8, REG_P0, REG_R0, 28,
                                INS_OPTS_SCALABLE_D); // LD4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4h, EA_SCALABLE, REG_V5, REG_P4, REG_R3, -32,
                                INS_OPTS_SCALABLE_H); // LD4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_ld4w, EA_SCALABLE, REG_V0, REG_P1, REG_R2, 28,
                                INS_OPTS_SCALABLE_S); // LD4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>/Z,
                                                      // [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_JE_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_st2q, EA_SCALABLE, REG_V0, REG_P3, REG_R0, -16,
                                INS_OPTS_SCALABLE_Q); // ST2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st3q, EA_SCALABLE, REG_V2, REG_P3, REG_R4, 21,
                                INS_OPTS_SCALABLE_Q); // ST3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>, [<Xn|SP>{, #<imm>,
                                                      // MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4q, EA_SCALABLE, REG_V7, REG_P6, REG_R5, 28,
                                INS_OPTS_SCALABLE_Q); // ST4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]

    // IF_SVE_JM_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_stnt1b, EA_SCALABLE, REG_V1, REG_P2, REG_R3, 4,
                                INS_OPTS_SCALABLE_B); // STNT1B  {<Zt>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_stnt1d, EA_SCALABLE, REG_V8, REG_P7, REG_R6, 5,
                                INS_OPTS_SCALABLE_D); // STNT1D  {<Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_stnt1h, EA_SCALABLE, REG_V9, REG_P1, REG_R0, -5,
                                INS_OPTS_SCALABLE_H); // STNT1H  {<Zt>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_stnt1w, EA_SCALABLE, REG_V0, REG_P0, REG_R2, -7,
                                INS_OPTS_SCALABLE_S); // STNT1W  {<Zt>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_JN_3C
    theEmitter->emitIns_R_R_R_I(INS_sve_st1d, EA_SCALABLE, REG_V1, REG_P2, REG_R3, 4,
                                INS_OPTS_SCALABLE_D); // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1w, EA_SCALABLE, REG_V3, REG_P4, REG_R5, 6,
                                INS_OPTS_SCALABLE_Q); // ST1W    {<Zt>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_JN_3C_D
    theEmitter->emitIns_R_R_R_I(INS_sve_st1d, EA_SCALABLE, REG_V2, REG_P1, REG_R0, 0,
                                INS_OPTS_SCALABLE_Q); // ST1D    {<Zt>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_JO_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_st2b, EA_SCALABLE, REG_V0, REG_P1, REG_R2, -16,
                                INS_OPTS_SCALABLE_B); // ST2B    {<Zt1>.B, <Zt2>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st2d, EA_SCALABLE, REG_V5, REG_P4, REG_R3, -16,
                                INS_OPTS_SCALABLE_D); // ST2D    {<Zt1>.D, <Zt2>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st2h, EA_SCALABLE, REG_V6, REG_P7, REG_R8, -16,
                                INS_OPTS_SCALABLE_H); // ST2H    {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st2w, EA_SCALABLE, REG_V8, REG_P1, REG_R9, -16,
                                INS_OPTS_SCALABLE_S); // ST2W    {<Zt1>.S, <Zt2>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st3b, EA_SCALABLE, REG_V7, REG_P6, REG_R5, -24,
                                INS_OPTS_SCALABLE_B); // ST3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>, [<Xn|SP>{, #<imm>,
                                                      // MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st3d, EA_SCALABLE, REG_V2, REG_P3, REG_R4, -24,
                                INS_OPTS_SCALABLE_D); // ST3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>, [<Xn|SP>{, #<imm>,
                                                      // MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st3h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, -24,
                                INS_OPTS_SCALABLE_H); // ST3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>{, #<imm>,
                                                      // MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st3w, EA_SCALABLE, REG_V1, REG_P3, REG_R8, -24,
                                INS_OPTS_SCALABLE_S); // ST3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>, [<Xn|SP>{, #<imm>,
                                                      // MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4b, EA_SCALABLE, REG_V0, REG_P0, REG_R0, -32,
                                INS_OPTS_SCALABLE_B); // ST4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4d, EA_SCALABLE, REG_V2, REG_P0, REG_R1, -32,
                                INS_OPTS_SCALABLE_D); // ST4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4h, EA_SCALABLE, REG_V3, REG_P5, REG_R2, -32,
                                INS_OPTS_SCALABLE_H); // ST4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4w, EA_SCALABLE, REG_V0, REG_P1, REG_R5, 28,
                                INS_OPTS_SCALABLE_S); // ST4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st4w, EA_SCALABLE, REG_V31, REG_P1, REG_R5, 28,
                                INS_OPTS_SCALABLE_S); // ST4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>{,
                                                      // #<imm>, MUL VL}]

    // IF_SVE_JD_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V4, REG_P1, REG_R2, REG_R0,
                                INS_OPTS_SCALABLE_B); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V4, REG_P5, REG_R6, REG_R2,
                                INS_OPTS_SCALABLE_H); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V6, REG_P5, REG_R7, REG_R4,
                                INS_OPTS_SCALABLE_S); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V4, REG_P0, REG_R1, REG_R2,
                                INS_OPTS_SCALABLE_D); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P6, REG_R1, REG_R2, INS_OPTS_SCALABLE_H,
                                INS_SCALABLE_OPTS_LSL_N); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_R4, INS_OPTS_SCALABLE_S,
                                INS_SCALABLE_OPTS_LSL_N); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P2, REG_R4, REG_R0, INS_OPTS_SCALABLE_D,
                                INS_SCALABLE_OPTS_LSL_N); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]

    // IF_SVE_JD_4B
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_R3, INS_OPTS_SCALABLE_S,
                                INS_SCALABLE_OPTS_LSL_N); // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #2]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V2, REG_P3, REG_R4, REG_R5, INS_OPTS_SCALABLE_D,
                                INS_SCALABLE_OPTS_LSL_N); // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #2]

    // IF_SVE_JJ_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_st1d, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_V3, INS_OPTS_SCALABLE_D_UXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1d, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_V3, INS_OPTS_SCALABLE_D_SXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P1, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_UXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P1, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_SXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V0, REG_P3, REG_R1, REG_V2, INS_OPTS_SCALABLE_S_UXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V0, REG_P3, REG_R1, REG_V2, INS_OPTS_SCALABLE_S_SXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]

    // IF_SVE_JJ_4A_B
    theEmitter->emitIns_R_R_R_R(INS_sve_st1d, EA_SCALABLE, REG_V3, REG_P1, REG_R2, REG_V5,
                                INS_OPTS_SCALABLE_D_UXTW); // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1d, EA_SCALABLE, REG_V3, REG_P1, REG_R2, REG_V5,
                                INS_OPTS_SCALABLE_D_SXTW); // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V2, REG_P3, REG_R1, REG_V4, INS_OPTS_SCALABLE_D_UXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V2, REG_P3, REG_R1, REG_V4, INS_OPTS_SCALABLE_D_SXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V1, REG_P4, REG_R2, REG_V3, INS_OPTS_SCALABLE_D_UXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V1, REG_P4, REG_R2, REG_V3, INS_OPTS_SCALABLE_D_SXTW,
                                INS_SCALABLE_OPTS_MOD_N); // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]

    // IF_SVE_JJ_4A_C
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P5, REG_R1, REG_V3,
                                INS_OPTS_SCALABLE_D_UXTW); // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P5, REG_R1, REG_V3,
                                INS_OPTS_SCALABLE_D_SXTW); // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V0, REG_P2, REG_R3, REG_V4,
                                INS_OPTS_SCALABLE_D_UXTW); // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V0, REG_P2, REG_R3, REG_V4,
                                INS_OPTS_SCALABLE_D_SXTW); // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]

    // IF_SVE_JJ_4A_D
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V7, REG_P5, REG_R4, REG_V1,
                                INS_OPTS_SCALABLE_S_UXTW); // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V7, REG_P5, REG_R4, REG_V1,
                                INS_OPTS_SCALABLE_S_SXTW); // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_V2,
                                INS_OPTS_SCALABLE_S_UXTW); // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1w, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_V2,
                                INS_OPTS_SCALABLE_S_SXTW); // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]

    // IF_SVE_JK_4A
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V4, REG_P2, REG_R0, REG_V1,
                                INS_OPTS_SCALABLE_D_UXTW); // ST1B    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V4, REG_P2, REG_R0, REG_V1,
                                INS_OPTS_SCALABLE_D_SXTW); // ST1B    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]

    // IF_SVE_JK_4A_B
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V1, REG_P4, REG_R3, REG_V0,
                                INS_OPTS_SCALABLE_S_UXTW); // ST1B    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
    theEmitter->emitIns_R_R_R_R(INS_sve_st1b, EA_SCALABLE, REG_V1, REG_P4, REG_R3, REG_V0,
                                INS_OPTS_SCALABLE_S_SXTW); // ST1B    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]

    // IF_SVE_JN_3A
    theEmitter->emitIns_R_R_R_I(INS_sve_st1b, EA_SCALABLE, REG_V3, REG_P2, REG_R1, 5,
                                INS_OPTS_SCALABLE_B); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1b, EA_SCALABLE, REG_V3, REG_P2, REG_R1, 4,
                                INS_OPTS_SCALABLE_H); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, 3,
                                INS_OPTS_SCALABLE_H); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1b, EA_SCALABLE, REG_V3, REG_P2, REG_R1, 2,
                                INS_OPTS_SCALABLE_S); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, 1,
                                INS_OPTS_SCALABLE_S); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1b, EA_SCALABLE, REG_V3, REG_P2, REG_R1, 0,
                                INS_OPTS_SCALABLE_D); // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, -2,
                                INS_OPTS_SCALABLE_D); // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]

    // IF_SVE_JN_3B
    theEmitter->emitIns_R_R_R_I(INS_sve_st1w, EA_SCALABLE, REG_V2, REG_P1, REG_R3, 5,
                                INS_OPTS_SCALABLE_S); // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
    theEmitter->emitIns_R_R_R_I(INS_sve_st1w, EA_SCALABLE, REG_V2, REG_P1, REG_R3, 1,
                                INS_OPTS_SCALABLE_D); // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
}

#endif // defined(TARGET_ARM64) && defined(DEBUG)
