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
//   CompPhaseNameMacro(enumName, stringName, shortName, hasChildren, parent, measureIR)
//     "enumName" is an Enumeration-style all-caps name.
//     "stringName" is a self-explanatory.
//     "shortName" is an abbreviated form for stringName
//     "hasChildren" is true if this phase is broken out into subphases.
//         (We should never do EndPhase on a phase that has children, only on 'leaf phases.')
//     "parent" is -1 for leaf phases, otherwise it is the "enumName" of the parent phase.
//     "measureIR" is true for phases that generate a count of IR nodes during EndPhase when JitConfig.MeasureIR is
//     true.

// clang-format off
//                 enumName                      stringName                        shortName hasChildren measureIR
//                                                                                                   parent
CompPhaseNameMacro(PHASE_PRE_IMPORT,             "Pre-import",                     "PRE-IMP",  false, -1, false)
CompPhaseNameMacro(PHASE_IMPORTATION,            "Importation",                    "IMPORT",   false, -1, true)
CompPhaseNameMacro(PHASE_INDXCALL,               "Indirect call transform",        "INDXCALL", false, -1, true)
CompPhaseNameMacro(PHASE_PATCHPOINTS,            "Expand patchpoints",             "PPOINT",   false, -1, true)
CompPhaseNameMacro(PHASE_POST_IMPORT,            "Post-import",                    "POST-IMP", false, -1, false)
CompPhaseNameMacro(PHASE_IBCPREP,                "Profile instrumentation prep",   "IBCPREP",  false, -1, false)
CompPhaseNameMacro(PHASE_IBCINSTR,               "Profile instrumentation",        "IBCINSTR", false, -1, false)
CompPhaseNameMacro(PHASE_INCPROFILE,             "Profile incorporation",          "INCPROF",  false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_INIT,             "Morph - Init",                   "MOR-INIT", false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_INLINE,           "Morph - Inlining",               "MOR-INL",  false, -1, true)
CompPhaseNameMacro(PHASE_MORPH_ADD_INTERNAL,     "Morph - Add internal blocks",    "MOR-ADD",  false, -1, true)
CompPhaseNameMacro(PHASE_ALLOCATE_OBJECTS,       "Allocate Objects",               "ALLOC-OBJ", false, -1, false)
CompPhaseNameMacro(PHASE_EMPTY_TRY,              "Remove empty try",               "EMPTYTRY", false, -1, false)
CompPhaseNameMacro(PHASE_EMPTY_FINALLY,          "Remove empty finally",           "EMPTYFIN", false, -1, false)
CompPhaseNameMacro(PHASE_MERGE_FINALLY_CHAINS,   "Merge callfinally chains",       "MRGCFCHN", false, -1, false)
CompPhaseNameMacro(PHASE_CLONE_FINALLY,          "Clone finally",                  "CLONEFIN", false, -1, false)
CompPhaseNameMacro(PHASE_UPDATE_FINALLY_FLAGS,   "Update finally target flags",    "UPD-FTF",  false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_PREDS,          "Compute preds",                  "PREDS",    false, -1, false)
CompPhaseNameMacro(PHASE_EARLY_UPDATE_FLOW_GRAPH,"Update flow graph early pass",   "UPD-FG-E", false, -1, false)
CompPhaseNameMacro(PHASE_STR_ADRLCL,             "Morph - Structs/AddrExp",        "MOR-STRAL",false, -1, false)
CompPhaseNameMacro(PHASE_FWD_SUB,                "Forward Substitution",           "FWD-SUB",  false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_IMPBYREF,         "Morph - ByRefs",                 "MOR-BYREF",false, -1, false)
CompPhaseNameMacro(PHASE_PROMOTE_STRUCTS,        "Morph - Promote Structs",        "PROMOTER" ,false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_GLOBAL,           "Morph - Global",                 "MOR-GLOB", false, -1, false)
CompPhaseNameMacro(PHASE_MORPH_END,              "Morph - Finish",                 "MOR-END",  false, -1, true)
CompPhaseNameMacro(PHASE_GS_COOKIE,              "GS Cookie",                      "GS-COOK",  false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS,   "Compute edge weights (1, false)","EDG-WGT",  false, -1, false)
#if defined(FEATURE_EH_FUNCLETS)
CompPhaseNameMacro(PHASE_CREATE_FUNCLETS,        "Create EH funclets",             "EH-FUNC",  false, -1, false)
#endif // FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_MERGE_THROWS,           "Merge throw blocks",             "MRGTHROW", false, -1, false)
CompPhaseNameMacro(PHASE_INVERT_LOOPS,           "Invert loops",                   "LOOP-INV", false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_LAYOUT,        "Optimize layout",                "LAYOUT",   false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_REACHABILITY,   "Compute blocks reachability",    "BL_REACH", false, -1, false)
CompPhaseNameMacro(PHASE_SET_BLOCK_WEIGHTS,      "Set block weights",              "BL-WEIGHTS", false, -1, false)
CompPhaseNameMacro(PHASE_ZERO_INITS,             "Redundant zero Inits",           "ZERO-INIT", false, -1, false)
CompPhaseNameMacro(PHASE_FIND_LOOPS,             "Find loops",                     "LOOP-FND", false, -1, false)
CompPhaseNameMacro(PHASE_CLONE_LOOPS,            "Clone loops",                    "LP-CLONE", false, -1, false)
CompPhaseNameMacro(PHASE_UNROLL_LOOPS,           "Unroll loops",                   "UNROLL",   false, -1, false)
CompPhaseNameMacro(PHASE_CLEAR_LOOP_INFO,        "Clear loop info",                "LP-CLEAR", false, -1, false)
CompPhaseNameMacro(PHASE_HOIST_LOOP_CODE,        "Hoist loop code",                "LP-HOIST", false, -1, false)
CompPhaseNameMacro(PHASE_MARK_LOCAL_VARS,        "Mark local vars",                "MARK-LCL", false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_BOOLS,         "Optimize bools",                 "OPT-BOOL", false, -1, false)
CompPhaseNameMacro(PHASE_FIND_OPER_ORDER,        "Find oper order",                "OPER-ORD", false, -1, false)
CompPhaseNameMacro(PHASE_SET_BLOCK_ORDER,        "Set block order",                "BLK-ORD",  false, -1, true)
CompPhaseNameMacro(PHASE_BUILD_SSA,              "Build SSA representation",       "SSA",      true,  -1, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_TOPOSORT,     "SSA: topological sort",          "SSA-SORT", false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_DOMS,         "SSA: Doms1",                     "SSA-DOMS", false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_LIVENESS,     "SSA: liveness",                  "SSA-LIVE", false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_DF,           "SSA: DF",                        "SSA-DF",   false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_INSERT_PHIS,  "SSA: insert phis",               "SSA-PHI",  false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_BUILD_SSA_RENAME,       "SSA: rename",                    "SSA-REN",  false, PHASE_BUILD_SSA, false)
CompPhaseNameMacro(PHASE_EARLY_PROP,             "Early Value Propagation",        "ERL-PROP", false, -1, false)
CompPhaseNameMacro(PHASE_VALUE_NUMBER,           "Do value numbering",             "VAL-NUM",  false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_INDEX_CHECKS,  "Optimize index checks",          "OPT-CHK",  false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_VALNUM_CSES,   "Optimize Valnum CSEs",           "OPT-CSE",  false, -1, false)
CompPhaseNameMacro(PHASE_VN_COPY_PROP,           "VN based copy prop",             "CP-PROP",  false, -1, false)
CompPhaseNameMacro(PHASE_OPTIMIZE_BRANCHES,      "Redundant branch opts",          "OPT-BR",   false, -1, false)
CompPhaseNameMacro(PHASE_ASSERTION_PROP_MAIN,    "Assertion prop",                 "AST-PROP", false, -1, false)
CompPhaseNameMacro(PHASE_OPT_UPDATE_FLOW_GRAPH,  "Update flow graph opt pass",     "UPD-FG-O", false, -1, false)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS2,  "Compute edge weights (2, false)","EDG-WGT2", false, -1, false)
CompPhaseNameMacro(PHASE_REMOVE_DEAD_BLOCKS,     "Remove dead blocks",             "DEAD-BLK", false, -1, false)
CompPhaseNameMacro(PHASE_INSERT_GC_POLLS,        "Insert GC Polls",                "GC-POLLS", false, -1, true)
CompPhaseNameMacro(PHASE_DETERMINE_FIRST_COLD_BLOCK, "Determine first cold block", "COLD-BLK", false, -1, true)
CompPhaseNameMacro(PHASE_RATIONALIZE,            "Rationalize IR",                 "RAT",      false, -1, false)
CompPhaseNameMacro(PHASE_SIMPLE_LOWERING,        "Do 'simple' lowering",           "SMP-LWR",  false, -1, false)

CompPhaseNameMacro(PHASE_LCLVARLIVENESS,         "Local var liveness",             "LIVENESS", true, -1, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INIT,    "Local var liveness init",        "LIV-INIT", false, PHASE_LCLVARLIVENESS, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_PERBLOCK,"Per block local var liveness",   "LIV-BLK",  false, PHASE_LCLVARLIVENESS, false)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INTERBLOCK,  "Global local var liveness",  "LIV-GLBL", false, PHASE_LCLVARLIVENESS, false)

CompPhaseNameMacro(PHASE_LOWERING_DECOMP,        "Lowering decomposition",         "LWR-DEC",  false, -1, false)
CompPhaseNameMacro(PHASE_LOWERING,               "Lowering nodeinfo",              "LWR-INFO", false, -1, true)
CompPhaseNameMacro(PHASE_STACK_LEVEL_SETTER,     "Calculate stack level slots",    "STK-SET",  false, -1, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN,            "Linear scan register alloc",     "LSRA",     true, -1, true)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_BUILD,      "LSRA build intervals",           "LSRA-BLD", false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_ALLOC,      "LSRA allocate",                  "LSRA-ALL", false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_RESOLVE,    "LSRA resolve",                   "LSRA-RES", false, PHASE_LINEAR_SCAN, false)
CompPhaseNameMacro(PHASE_ALIGN_LOOPS,            "Place 'align' instructions",     "LOOP-ALIGN",  false, -1, false)
CompPhaseNameMacro(PHASE_GENERATE_CODE,          "Generate code",                  "CODEGEN",  false, -1, false)
CompPhaseNameMacro(PHASE_EMIT_CODE,              "Emit code",                      "EMIT",     false, -1, false)
CompPhaseNameMacro(PHASE_EMIT_GCEH,              "Emit GC+EH tables",              "EMT-GCEH", false, -1, false)
CompPhaseNameMacro(PHASE_POST_EMIT,              "Post-Emit",                      "POST-EMIT", false, -1, false)

#if MEASURE_CLRAPI_CALLS
// The following is a "pseudo-phase" - it aggregates timing info
// for calls through ICorJitInfo across all "real" phases.
CompPhaseNameMacro(PHASE_CLR_API,                "CLR API calls",                  "CLR-API",  false, -1, false)
#endif
// clang-format on

#undef CompPhaseNameMacro
