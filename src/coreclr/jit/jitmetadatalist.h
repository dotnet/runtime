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

// List of metadata that the JIT can report. There are two categories:
//
// - JITMETADATAINFO: General info that can be of any type and that cannot be
//   aggregated in straightforward ways. These properties are not handled
//   automatically; the JIT must explicitly report them using
//   JitMetadata::report, and the SPMI side needs to manually handle (or ignore)
//   them in ICorJitInfo::reportMetadata.
//
// - JITMETADATAMETRIC: Metrics which are numeric types (currently int, double
//   and int64_t types supported). Their reporting is handled automatically and
//   they will be propagated all the way into SPMI replay/diff results.

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
JITMETADATAMETRIC(CseCount,                    int,              0)
JITMETADATAMETRIC(BasicBlocksAtCodegen,        int,              0)
JITMETADATAMETRIC(PerfScore,                   double,           JIT_METADATA_LOWER_IS_BETTER)
JITMETADATAMETRIC(BytesAllocated,              int64_t,          JIT_METADATA_LOWER_IS_BETTER)

#undef JITMETADATA
#undef JITMETADATAINFO
#undef JITMETADATAMETRIC

// clang-format on
