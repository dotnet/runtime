// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

/*****************************************************************************/
/*****************************************************************************/

#ifndef REG_SEL_DEF
#error  Must define REG_SEL_DEF macro before including this file
#endif

// Register selection stats
// Each register will receive a score which takes into account the scoring criteria below.
// These were selected on the assumption that they will have an impact on the "goodness"
// of a register selection, and have been tuned to a certain extent by observing the impact
// of the ordering on asmDiffs.  However, there is much more room for tuning,
// and perhaps additional criteria.
//
//          name                score       short_name      orderSeqId
REG_SEL_DEF(FREE,               0x10000,    "FREE ",        'A')    // It is not currently assigned to an *active* interval

// These are the original criteria for comparing registers that are free.

REG_SEL_DEF(CONST_AVAILABLE,    0x08000,    "CONST",        'B')   // It is a constant value that is already in an acceptable register.
REG_SEL_DEF(THIS_ASSIGNED,      0x04000,    "THISA",        'C')   // It is in the interval's preference set and it is already assigned to this interval.
REG_SEL_DEF(COVERS,             0x02000,    "COVRS",        'D')   // It is in the interval's preference set and it covers the current range.
REG_SEL_DEF(OWN_PREFERENCE,     0x01000,    "OWNPR",        'E')   // It is in the preference set of this interval.
REG_SEL_DEF(COVERS_RELATED,     0x00800,    "COREL",        'F')   // It is in the preference set of the related interval and covers its entire lifetime.
REG_SEL_DEF(RELATED_PREFERENCE, 0x00400,    "RELPR",        'G')   // It is in the preference set of the related interval.
REG_SEL_DEF(CALLER_CALLEE,      0x00200,    "CRCE ",        'H')   // It is in the right "set" for the interval (caller or callee-save).
REG_SEL_DEF(UNASSIGNED,         0x00100,    "UNASG",        'I')   // It is not currently assigned to any (active or inactive) interval
REG_SEL_DEF(COVERS_FULL,        0x00080,    "COFUL",        'J')   // It covers the full range of the interval from current position to the end.
REG_SEL_DEF(BEST_FIT,           0x00040,    "BSFIT",        'K')   // The available range is the closest match to the full range of the interval.
REG_SEL_DEF(IS_PREV_REG,        0x00020,    "PRVRG",        'L')   // This register was previously assigned to the interval.
REG_SEL_DEF(REG_ORDER,          0x00010,    "ORDER",        'M')   // Tie-breaker

// These are the original criteria for comparing registers that are in use.
REG_SEL_DEF(SPILL_COST,         0x00008,    "SPILL",        'N')   // It has the lowest cost of all the candidates.
REG_SEL_DEF(FAR_NEXT_REF,       0x00004,    "FNREF",        'O')   // It has a farther next reference than the best candidate thus far.
REG_SEL_DEF(PREV_REG_OPT,       0x00002,    "PRGOP",        'P')   // The previous RefPosition of its current assigned interval is RegOptional.

// TODO-CQ: Consider using REG_ORDER as a tie-breaker even for busy registers.
REG_SEL_DEF(REG_NUM,            0x00001,    "RGNUM",        'Q')   // It has a lower register number.

// clang-format on
