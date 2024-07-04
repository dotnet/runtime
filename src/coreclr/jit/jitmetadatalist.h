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

//              Name,                                    type              flags
JITMETADATAINFO(MethodFullName,                          const char*,      0)
JITMETADATAINFO(TieringName,                             const char*,      0)
JITMETADATAMETRIC(PhysicallyPromotedFields,              int,              0)
JITMETADATAMETRIC(LoopsFoundDuringOpts,                  int,              0)
JITMETADATAMETRIC(LoopsCloned,                           int,              0)
JITMETADATAMETRIC(LoopsUnrolled,                         int,              0)
JITMETADATAMETRIC(LoopAlignmentCandidates,               int,              0)
JITMETADATAMETRIC(LoopsAligned,                          int,              0)
JITMETADATAMETRIC(LoopsIVWidened,                        int,              0)
JITMETADATAMETRIC(WidenedIVs,                            int,              0)
JITMETADATAMETRIC(LoopsMadeDownwardsCounted,             int,              0)
JITMETADATAMETRIC(LoopsStrengthReduced,                  int,              0)
JITMETADATAMETRIC(VarsInSsa,                             int,              0)
JITMETADATAMETRIC(HoistedExpressions,                    int,              0)
JITMETADATAMETRIC(RedundantBranchesEliminated,           int,              JIT_METADATA_HIGHER_IS_BETTER)
JITMETADATAMETRIC(JumpThreadingsPerformed,               int,              JIT_METADATA_HIGHER_IS_BETTER)
JITMETADATAMETRIC(CseCount,                              int,              0)
JITMETADATAMETRIC(BasicBlocksAtCodegen,                  int,              0)
JITMETADATAMETRIC(PerfScore,                             double,           JIT_METADATA_LOWER_IS_BETTER)
JITMETADATAMETRIC(BytesAllocated,                        int64_t,          JIT_METADATA_LOWER_IS_BETTER)
JITMETADATAMETRIC(ImporterBranchFold,                    int,              0)
JITMETADATAMETRIC(ImporterSwitchFold,                    int,              0)
JITMETADATAMETRIC(DevirtualizedCall,                     int,              0)
JITMETADATAMETRIC(DevirtualizedCallUnboxedEntry,         int,              0)
JITMETADATAMETRIC(DevirtualizedCallRemovedBox,           int,              0)
JITMETADATAMETRIC(GDV,                                   int,              0)
JITMETADATAMETRIC(ClassGDV,                              int,              0)
JITMETADATAMETRIC(MethodGDV,                             int,              0)
JITMETADATAMETRIC(MultiGuessGDV,                         int,              0)
JITMETADATAMETRIC(ChainedGDV,                            int,              0)
JITMETADATAMETRIC(InlinerBranchFold,                     int,              0)
JITMETADATAMETRIC(InlineAttempt,                         int,              0)
JITMETADATAMETRIC(InlineCount,                           int,              0)
JITMETADATAMETRIC(ProfileConsistentBeforeInline,         int,              0)
JITMETADATAMETRIC(ProfileConsistentAfterInline,          int,              0)
JITMETADATAMETRIC(ProfileSynthesizedBlendedOrRepaired,   int,              0)
JITMETADATAMETRIC(ProfileInconsistentInitially,          int,              0)
JITMETADATAMETRIC(ProfileInconsistentResetLeave,         int,              0)
JITMETADATAMETRIC(ProfileInconsistentImporterBranchFold, int,              0)
JITMETADATAMETRIC(ProfileInconsistentImporterSwitchFold, int,              0)
JITMETADATAMETRIC(ProfileInconsistentChainedGDV,         int,              0)
JITMETADATAMETRIC(ProfileInconsistentScratchBB,          int,              0)
JITMETADATAMETRIC(ProfileInconsistentInlinerBranchFold,  int,              0)
JITMETADATAMETRIC(ProfileInconsistentInlineeScale,       int,              0)
JITMETADATAMETRIC(ProfileInconsistentInlinee,            int,              0)
JITMETADATAMETRIC(ProfileInconsistentNoReturnInlinee,    int,              0)
JITMETADATAMETRIC(ProfileInconsistentMayThrowInlinee,    int,              0)
JITMETADATAMETRIC(NewRefClassHelperCalls,                int,              0)
JITMETADATAMETRIC(StackAllocatedRefClasses,              int,              0)
JITMETADATAMETRIC(NewBoxedValueClassHelperCalls,         int,              0)
JITMETADATAMETRIC(StackAllocatedBoxedValueClasses,       int,              0)

#undef JITMETADATA
#undef JITMETADATAINFO
#undef JITMETADATAMETRIC

// clang-format on
