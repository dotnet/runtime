// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CompPhases.h
//

//
// Names of JIT phases, in order.  Assumes that the caller defines CompPhaseNameMacro
// in a useful way before including this file, e.g., to define the phase enumeration and the
// corresponding array of string names of those phases.  This include file undefines CompPhaseNameMacro
// after the last use.
// The arguments are:
//   CompPhaseNameMacro(enumName, stringName, hasChildren, parent, measureIR)
//     "enumName" is an Enumeration-style all-caps name.
//     "stringName" is a self-explanatory.
//     "hasChildren" is true if this phase is broken out into subphases.
//         (We should never do EndPhase on a phase that has children, only on 'leaf phases.')
//     "parent" is -1 for leaf phases, otherwise it is the "enumName" of the parent phase.
//     "measureIR" is true for phases that generate a count of IR nodes during EndPhase when JitConfig.MeasureIR is
//         true.

// clang-format off
//                 enumName                          stringName                        hasChildren
//                                                                                           parent
//                                                                                                measureIR
CompPhaseNameMacro(PHASE_PRE_IMPORT,                 "Pre-import",                     false, -1, false)
CompPhaseNameMacro(PHASE_IMPORTATION,                "Importation",                    false, -1, true)
CompPhaseNameMacro(PHASE_INDXCALL,                   "Indirect call transform",        false, -1, true)
CompPhaseNameMacro(PHASE_PATCHPOINTS,                "Expand patchpoints",             false, -1, true)
CompPhaseNameMacro(PHASE_POST_IMPORT,                "Post-import",                    false, -1, false)
CompPhaseNameMacro(PHASE_IBCPREP,                    "Profile instrumentation prep",   false, -1, false)
CompPhaseNameMacro(PHASE_IBCINSTR,                   "Profile instrumentation",        false, -1, false)
CompPhaseNameMacro(PHASE_INCPROFILE,                 "Profile incorporation",          false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_INIT,                 "Morph - Init",                   false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_INLINE,               "Morph - Inlining",               false, -1, true)
CompPhaseNameMacro(PHASE_MORPH_ADD_INTERNAL,         "Morph - Add internal blocks",    false, -1, true)
CompPhaseNameMacro(PHASE_ALLOCATE_OBJECTS,           "Allocate Objects",               false, -1, false)
CompPhaseNameMacro(PHASE_EMPTY_TRY,                  "Remove empty try",               false, -1, false)
CompPhaseNameMacro(PHASE_EMPTY_FINALLY,              "Remove empty finally",           false, -1, false)
CompPhaseNameMacro(PHASE_MERGE_FINALLY_CHAINS,       "Merge callfinally chains",       false, -1, false)
CompPhaseNameMacro(PHASE_CLONE_FINALLY,              "Clone finally",                  false, -1, false)
CompPhaseNameMacro(PHASE_UPDATE_FINALLY_FLAGS,       "Update finally target flags",    false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_PREDS,              "Compute preds",                  false, -1, false)
CompPhaseNameMacro(PHASE_EARLY_UPDATE_FLOW_GRAPH,    "Update flow graph early pass",   false, -1, false)
CompPhaseNameMacro(PHASE_STR_ADRLCL,                 "Morph - Structs/AddrExp",        false, -1, false)
CompPhaseNameMacro(PHASE_FWD_SUB,                    "Forward Substitution",           false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_IMPBYREF,             "Morph - ByRefs",                 false, -1, false)
CompPhaseNameMacro(PHASE_PROMOTE_STRUCTS,            "Morph - Promote Structs",        false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_GLOBAL,               "Morph - Global",                 false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_END,                  "Morph - Finish",                 false, -1, true)
CompPhaseNameMacro(PHASE_GS_COOKIE,                  "GS Cookie",                      false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS,       "Compute edge weights (1, false)",false, -1, false)
#if defined(FEATURE_EH_FUNCLETS)
CompPhaseNameMacro(PHASE_CREATE_FUNCLETS,            "Create EH funclets",             false, -1, false)
#endif // FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_TAIL_MERGE,                 "Tail merge",                     false, -1, false)
CompPhaseNameMacro(PHASE_MERGE_THROWS,               "Merge throw blocks",             false, -1, false)
CompPhaseNameMacro(PHASE_INVERT_LOOPS,               "Invert loops",                   false, -1, false)
CompPhaseNameMacro(PHASE_TAIL_MERGE2,                "Post-morph tail merge",          false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_FLOW,              "Optimize control flow",          false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_LAYOUT,            "Optimize layout",                false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_REACHABILITY,       "Compute blocks reachability",    false, -1, false)
CompPhaseNameMacro(PHASE_SET_BLOCK_WEIGHTS,          "Set block weights",              false, -1, false)
CompPhaseNameMacro(PHASE_ZERO_INITS,                 "Redundant zero Inits",           false, -1, false)
CompPhaseNameMacro(PHASE_FIND_LOOPS,                 "Find loops",                     false, -1, false)
CompPhaseNameMacro(PHASE_CLONE_LOOPS,                "Clone loops",                    false, -1, false)
CompPhaseNameMacro(PHASE_UNROLL_LOOPS,               "Unroll loops",                   false, -1, false)
CompPhaseNameMacro(PHASE_CLEAR_LOOP_INFO,            "Clear loop info",                false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_MDARR,                "Morph array ops",                false, -1, false)
CompPhaseNameMacro(PHASE_HOIST_LOOP_CODE,            "Hoist loop code",                false, -1, false)
CompPhaseNameMacro(PHASE_MARK_LOCAL_VARS,            "Mark local vars",                false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_ADD_COPIES,        "Opt add copies",                 false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_BOOLS,             "Optimize bools",                 false, -1, false)
CompPhaseNameMacro(PHASE_FIND_OPER_ORDER,            "Find oper order",                false, -1, false)
CompPhaseNameMacro(PHASE_SET_BLOCK_ORDER,            "Set block order",                false, -1, true)
CompPhaseNameMacro(PHASE_BUILD_SSA,                  "Build SSA representation",       true,  -1, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_TOPOSORT,         "SSA: topological sort",          false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_DOMS,             "SSA: Doms1",                     false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_LIVENESS,         "SSA: liveness",                  false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_DF,               "SSA: DF",                        false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_INSERT_PHIS,      "SSA: insert phis",               false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_RENAME,           "SSA: rename",                    false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_EARLY_PROP,                 "Early Value Propagation",        false, -1, false)
CompPhaseNameMacro(PHASE_VALUE_NUMBER,               "Do value numbering",             false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_INDEX_CHECKS,      "Optimize index checks",          false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_VALNUM_CSES,       "Optimize Valnum CSEs",           false, -1, false)
CompPhaseNameMacro(PHASE_VN_COPY_PROP,               "VN based copy prop",             false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_BRANCHES,          "Redundant branch opts",          false, -1, false)
CompPhaseNameMacro(PHASE_ASSERTION_PROP_MAIN,        "Assertion prop",                 false, -1, false)
CompPhaseNameMacro(PHASE_IF_CONVERSION,              "If conversion",                  false, -1, false)
CompPhaseNameMacro(PHASE_OPT_UPDATE_FLOW_GRAPH,      "Update flow graph opt pass",     false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS2,      "Compute edge weights (2, false)",false, -1, false)
CompPhaseNameMacro(PHASE_INSERT_GC_POLLS,            "Insert GC Polls",                false, -1, true)
CompPhaseNameMacro(PHASE_DETERMINE_FIRST_COLD_BLOCK, "Determine first cold block",     false, -1, true)
CompPhaseNameMacro(PHASE_RATIONALIZE,                "Rationalize IR",                 false, -1, false)
CompPhaseNameMacro(PHASE_SIMPLE_LOWERING,            "Do 'simple' lowering",           false, -1, false)

CompPhaseNameMacro(PHASE_LCLVARLIVENESS,             "Local var liveness",             true,  -1, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INIT,        "Local var liveness init",        false, PHASE_LCLVARLIVENESS, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_PERBLOCK,    "Per block local var liveness",   false, PHASE_LCLVARLIVENESS, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INTERBLOCK,  "Global local var liveness",      false, PHASE_LCLVARLIVENESS, false)

CompPhaseNameMacro(PHASE_LOWERING_DECOMP,            "Lowering decomposition",         false, -1, false)
CompPhaseNameMacro(PHASE_LOWERING,                   "Lowering nodeinfo",              false, -1, true)
CompPhaseNameMacro(PHASE_STACK_LEVEL_SETTER,         "Calculate stack level slots",    false, -1, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN,                "Linear scan register alloc",     true,  -1, true)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_BUILD,          "LSRA build intervals",           false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_ALLOC,          "LSRA allocate",                  false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_RESOLVE,        "LSRA resolve",                   false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_ALIGN_LOOPS,                "Place 'align' instructions",     false, -1, false)
CompPhaseNameMacro(PHASE_GENERATE_CODE,              "Generate code",                  false, -1, false)
CompPhaseNameMacro(PHASE_EMIT_CODE,                  "Emit code",                      false, -1, false)
CompPhaseNameMacro(PHASE_EMIT_GCEH,                  "Emit GC+EH tables",              false, -1, false)
CompPhaseNameMacro(PHASE_POST_EMIT,                  "Post-Emit",                      false, -1, false)

#if MEASURE_CLRAPI_CALLS
// The following is a "pseudo-phase" - it aggregates timing info
// for calls through ICorJitInfo across all "real" phases.
CompPhaseNameMacro(PHASE_CLR_API,                    "CLR API calls",                  false, -1, false)
#endif
// clang-format on

#undef CompPhaseNameMacro
