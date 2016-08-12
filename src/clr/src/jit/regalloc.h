// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef REGALLOC_H_
#define REGALLOC_H_

// Some things that are used by both LSRA and regpredict allocators.

enum FrameType
{
    FT_NOT_SET,
    FT_ESP_FRAME,
    FT_EBP_FRAME,
#if DOUBLE_ALIGN
    FT_DOUBLE_ALIGN_FRAME,
#endif
};

#ifdef LEGACY_BACKEND

#include "varset.h"

/*****************************************************************************/
/*****************************************************************************/

// This enumeration specifies register restrictions for the predictor
enum rpPredictReg
{
    PREDICT_NONE,        // any subtree
    PREDICT_ADDR,        // subtree is left side of an assignment
    PREDICT_REG,         // subtree must be any register
    PREDICT_SCRATCH_REG, // subtree must be any writable register

#if defined(_TARGET_ARM_)
    PREDICT_PAIR_R0R1, // subtree will write R0 and R1
    PREDICT_PAIR_R2R3, // subtree will write R2 and R3

#elif defined(_TARGET_AMD64_)

    PREDICT_NOT_REG_EAX, // subtree must be any writable register, except EAX
    PREDICT_NOT_REG_ECX, // subtree must be any writable register, except ECX

#elif defined(_TARGET_X86_)

    PREDICT_NOT_REG_EAX, // subtree must be any writable register, except EAX
    PREDICT_NOT_REG_ECX, // subtree must be any writable register, except ECX

    PREDICT_PAIR_EAXEDX, // subtree will write EAX and EDX
    PREDICT_PAIR_ECXEBX, // subtree will write ECX and EBX

#else
#error "Unknown Target!"
#endif // _TARGET_

#define REGDEF(name, rnum, mask, sname) PREDICT_REG_##name,
#include "register.h"

    // The following are use whenever we have a ASG node into a LCL_VAR that
    // we predict to be enregistered.  This flags indicates that we can expect
    // to use the register that is being assigned into as the temporary to
    // compute the right side of the ASGN node.

    PREDICT_REG_VAR_T00, // write the register used by tracked varable 00
    PREDICT_REG_VAR_MAX = PREDICT_REG_VAR_T00 + lclMAX_TRACKED - 1,

    PREDICT_COUNT = PREDICT_REG_VAR_T00,

#define REGDEF(name, rnum, mask, sname)
#define REGALIAS(alias, realname) PREDICT_REG_##alias = PREDICT_REG_##realname,
#include "register.h"

#if defined(_TARGET_ARM_)

    PREDICT_REG_FIRST = PREDICT_REG_R0,
    PREDICT_INTRET    = PREDICT_REG_R0,
    PREDICT_LNGRET    = PREDICT_PAIR_R0R1,
    PREDICT_FLTRET    = PREDICT_REG_F0,

#elif defined(_TARGET_AMD64_)

    PREDICT_REG_FIRST = PREDICT_REG_RAX,
    PREDICT_INTRET    = PREDICT_REG_EAX,
    PREDICT_LNGRET    = PREDICT_REG_RAX,

#elif defined(_TARGET_X86_)

    PREDICT_REG_FIRST = PREDICT_REG_EAX,
    PREDICT_INTRET    = PREDICT_REG_EAX,
    PREDICT_LNGRET    = PREDICT_PAIR_EAXEDX,

#else
#error "Unknown _TARGET_"
#endif // _TARGET_

};
#if DOUBLE_ALIGN
enum CanDoubleAlign
{
    CANT_DOUBLE_ALIGN,
    CAN_DOUBLE_ALIGN,
    MUST_DOUBLE_ALIGN,
    COUNT_DOUBLE_ALIGN,

    DEFAULT_DOUBLE_ALIGN = CAN_DOUBLE_ALIGN
};
#endif

#endif // LEGACY_BACKEND

#endif // REGALLOC_H_
