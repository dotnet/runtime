// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

/*****************************************************************************/
/*****************************************************************************/

#ifndef BUSY_REG_SEL_DEF
#error  Must define BUSY_REG_SEL_DEF macro before including this file
#endif

// These are the original criteria for comparing registers that are in use.
//
//          name                score       short_name      orderSeqId
BUSY_REG_SEL_DEF(SPILL_COST,         0x00008,    "SPILL",        'N')   // It has the lowest cost of all the candidates.
BUSY_REG_SEL_DEF(FAR_NEXT_REF,       0x00004,    "FNREF",        'O')   // It has a farther next reference than the best candidate thus far.
BUSY_REG_SEL_DEF(PREV_REG_OPT,       0x00002,    "PRGOP",        'P')   // The previous RefPosition of its current assigned interval is RegOptional.

// TODO-CQ: Consider using REG_ORDER as a tie-breaker even for busy registers.
BUSY_REG_SEL_DEF(REG_NUM,            0x00001,    "RGNUM",        'Q')   // It has a lower register number.

// clang-format on
