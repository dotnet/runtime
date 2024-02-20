// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

#ifdef JITMETADATA
#define JITMETADATAINFO(name, type, flags) JITMETADATA(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) JITMETADATA(name, type, flags)
#endif

#if !defined(JITMETADATAINFO) || !defined(JITMETADATAMETRIC)
#error Define JITMETADATAINFO and JITMETADATAMETRIC before including this file.
#endif

//              Name,                          type              flags
JITMETADATAINFO(MethodFullName,                const char*,      0)
JITMETADATAINFO(TieringName,                   const char*,      0)
JITMETADATAMETRIC(PhysicallyPromotedFields,    int,              0)
JITMETADATAMETRIC(LoopsFoundDuringOpts,        int,              0)
JITMETADATAMETRIC(LoopsCloned,                 int,              0)
JITMETADATAMETRIC(LoopsUnrolled,               int,              0)
JITMETADATAMETRIC(LoopAlignmentCandidates,     int,              0)
JITMETADATAMETRIC(LoopsAligned,                int,              0)
JITMETADATAMETRIC(VarsInSsa,                   int,              0)
JITMETADATAMETRIC(HoistedExpressions,          int,              0)
JITMETADATAMETRIC(RedundantBranchesEliminated, int,              JIT_METADATA_HIGHER_IS_BETTER)
JITMETADATAMETRIC(JumpThreadingsPerformed,     int,              JIT_METADATA_HIGHER_IS_BETTER)
JITMETADATAMETRIC(Cses,                        int,              0)
JITMETADATAMETRIC(BasicBlocksAtCodegen,        int,              0)
JITMETADATAMETRIC(PerfScore,                   double,           JIT_METADATA_LOWER_IS_BETTER)
JITMETADATAMETRIC(BytesAllocated,              int64_t,          JIT_METADATA_LOWER_IS_BETTER)

#undef JITMETADATA
#undef JITMETADATAINFO
#undef JITMETADATAMETRIC

// clang-format on
