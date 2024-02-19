// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

#ifdef JITMETADATA
#define JITMETADATAINFO(name, type, flags) JITMETADATA(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) JITMETADATA(name, type, flags)
#endif

#if !defined(JITMETADATAINFO) || !defined(JITMETADATAMETRIC)
#error  Define JITMETADATAINFO and JITMETADATAMETRIC before including this file.
#endif

//              Name,                          type              flags
JITMETADATAINFO(MethodFullName,                const char*,      0)
JITMETADATAINFO(TieringName,                   const char*,      0)
JITMETADATAINFO(MethodOptimized,               bool,             0)
JITMETADATAMETRIC(PerfScore,                   double,           JIT_METADATA_LOWER_IS_BETTER)
JITMETADATAMETRIC(LoopsFoundDuringOpts,        int,              0)
JITMETADATAMETRIC(LoopsCloned,                 int,              0)
JITMETADATAMETRIC(LoopAlignmentCandidates,     int,              0)
JITMETADATAMETRIC(LoopsAligned,                int,              0)
JITMETADATAMETRIC(BytesAllocated,              int64_t,          0)
JITMETADATAMETRIC(BasicBlocksAtCodegen,        int,              0)

#undef  JITMETADATA
#undef  JITMETADATAINFO
#undef  JITMETADATAMETRIC

// clang-format on
