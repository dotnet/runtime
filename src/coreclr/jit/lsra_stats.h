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
LSRA_STAT_DEF(STAT_SPILL,               "SpillCount")

// Number of GT_COPY nodes inserted in this basic block while allocating regs.
// Note that GT_COPY nodes are also inserted as part of basic block boundary
// resolution, which are accounted against resolutionMovCount but not
// against copyRegCount.
LSRA_STAT_DEF(STAT_COPY_REG,             "CopyReg")

// Number of resolution moves inserted in this basic block.
LSRA_STAT_DEF(STAT_RESOLUTION_MOV,       "ResolutionMovs")

// Number of critical edges from this block that are split.
LSRA_STAT_DEF(STAT_SPLIT_EDGE,           "SplitEdges")

#endif // TRACK_LSRA_STATS

// clang-format on
