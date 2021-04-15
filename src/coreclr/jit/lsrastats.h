// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

/*****************************************************************************/
/*****************************************************************************/
#ifndef LSRA_STAT_DEF
#error  Must define LSRA_STAT_DEF macro before including this file
#endif

#if TRACK_LSRA_STATS

// Number of spills of local vars or tree temps in this basic block.
LSRA_STAT_DEF(STAT_SPILL,                   "SpillCount")

// Number of GT_COPY nodes inserted in this basic block while allocating regs.
// Note that GT_COPY nodes are also inserted as part of basic block boundary
// resolution, which are accounted against resolutionMovCount but not
// against copyRegCount.
LSRA_STAT_DEF(STAT_COPY_REG,                "CopyReg")

// Number of resolution moves inserted in this basic block.
LSRA_STAT_DEF(STAT_RESOLUTION_MOV,          "ResolutionMovs")

// Number of critical edges from this block that are split.
LSRA_STAT_DEF(STAT_SPLIT_EDGE,              "SplitEdges")

// Register selection stats

LSRA_STAT_DEF(REGSEL_FREE,                  "FREE")
LSRA_STAT_DEF(REGSEL_CONST_AVAILABLE,       "CONST_AVAILABLE")
LSRA_STAT_DEF(REGSEL_THIS_ASSIGNED,         "THIS_ASSIGNED")
LSRA_STAT_DEF(REGSEL_COVERS,                "COVERS")
LSRA_STAT_DEF(REGSEL_OWN_PREFERENCE,        "OWN_PREFERENCE")
LSRA_STAT_DEF(REGSEL_COVERS_RELATED,        "COVERS_RELATED")
LSRA_STAT_DEF(REGSEL_RELATED_PREFERENCE,    "RELATED_PREFERENCE")
LSRA_STAT_DEF(REGSEL_CALLER_CALLEE,         "CALLER_CALLEE")
LSRA_STAT_DEF(REGSEL_UNASSIGNED,            "UNASSIGNED")
LSRA_STAT_DEF(REGSEL_COVERS_FULL,           "COVERS_FULL")
LSRA_STAT_DEF(REGSEL_BEST_FIT,              "BEST_FIT")
LSRA_STAT_DEF(REGSEL_IS_PREV_REG,           "IS_PREV_REG")
LSRA_STAT_DEF(REGSEL_REG_ORDER,             "REG_ORDER")
LSRA_STAT_DEF(REGSEL_SPILL_COST,            "SPILL_COST")
LSRA_STAT_DEF(REGSEL_FAR_NEXT_REF,          "FAR_NEXT_REF")
LSRA_STAT_DEF(REGSEL_PREV_REG_OPT,          "PREV_REG_OPT")
LSRA_STAT_DEF(REGSEL_REG_NUM,               "REG_NUM")

LSRA_STAT_DEF(COUNT,                        "COUNT")

#endif // TRACK_LSRA_STATS

// clang-format on
