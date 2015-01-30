//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: CompPhases.h
//

//
// Names of x86 JIT phases, in order.  Assumes that the caller defines CompPhaseNameMacro
// in a useful way before including this file, e.g., to define the phase enumeration and the
// corresponding array of string names of those phases.  This include file undefines CompPhaseNameMacro
// after the last use.
// The arguments are:
//   CompPhaseNameMacro(enumName, stringName, hasChildren, parent)
//     "enumName" is an Enumeration-style all-caps name.
//     "stringName" is a self-explanatory.
//     "hasChildren" is true if this phase is broken out into subphases.
//         (We should never do EndPhase on a phase that has children, only on 'leaf phases.')
//     "parent" is -1 for leaf phases, otherwise it is the "enumName" of the parent phase.

CompPhaseNameMacro(PHASE_PRE_IMPORT,             "Pre-import",                     false, -1)
CompPhaseNameMacro(PHASE_IMPORTATION,            "Importation",                    false, -1)
CompPhaseNameMacro(PHASE_POST_IMPORT,            "Post-import",                    false, -1)
CompPhaseNameMacro(PHASE_MORPH,                  "Morph",                          false, -1)
CompPhaseNameMacro(PHASE_GS_COOKIE,              "GS Cookie",                      false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_PREDS,          "Compute preds",                  false, -1)
CompPhaseNameMacro(PHASE_MARK_GC_POLL_BLOCKS,    "Mark GC poll blocks",            false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS,   "Compute edge weights (1)",       false, -1)
#if FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_CREATE_FUNCLETS, "Create EH funclets", false, -1)
#endif // FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_OPTIMIZE_LAYOUT,        "Optimize layout",                false, -1)
CompPhaseNameMacro(PHASE_OPTIMIZE_LOOPS,         "Optimize loops",                 false, -1)
CompPhaseNameMacro(PHASE_CLONE_LOOPS,            "Clone loops",                    false, -1)
CompPhaseNameMacro(PHASE_UNROLL_LOOPS,           "Unroll loops",                   false, -1)
CompPhaseNameMacro(PHASE_HOIST_LOOP_CODE,        "Hoist loop code",                false, -1)
CompPhaseNameMacro(PHASE_MARK_LOCAL_VARS,        "Mark local vars",                false, -1)
CompPhaseNameMacro(PHASE_OPTIMIZE_BOOLS,         "Optimize bools",                 false, -1)
CompPhaseNameMacro(PHASE_FIND_OPER_ORDER,        "Find oper order",                false, -1)
CompPhaseNameMacro(PHASE_SET_BLOCK_ORDER,        "Set block order",                false, -1)
CompPhaseNameMacro(PHASE_BUILD_SSA,              "Build SSA representation",       true,  -1)
CompPhaseNameMacro(PHASE_BUILD_SSA_TOPOSORT,     "SSA: topological sort",          false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_DOMS,         "SSA: Doms1",                     false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_LIVENESS,     "SSA: liveness",                  false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_IDF,          "SSA: IDF",                       false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_INSERT_PHIS,  "SSA: insert phis",               false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_RENAME,       "SSA: rename",                    false, PHASE_BUILD_SSA)

CompPhaseNameMacro(PHASE_VALUE_NUMBER,           "Do value numbering",             false, -1)

CompPhaseNameMacro(PHASE_OPTIMIZE_INDEX_CHECKS,  "Optimize index checks",          false, -1)

#if FEATURE_VALNUM_CSE
CompPhaseNameMacro(PHASE_OPTIMIZE_VALNUM_CSES,   "Optimize Valnum CSEs",           false, -1)
#endif  

CompPhaseNameMacro(PHASE_VN_COPY_PROP,           "VN based copy prop",             false, -1)
#if ASSERTION_PROP
CompPhaseNameMacro(PHASE_ASSERTION_PROP_MAIN,    "Assertion prop",                 false, -1)
#endif
CompPhaseNameMacro(PHASE_UPDATE_FLOW_GRAPH,      "Update flow graph",              false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS2,  "Compute edge weights (2)",       false, -1)
CompPhaseNameMacro(PHASE_DETERMINE_FIRST_COLD_BLOCK, "Determine first cold block", false, -1)
CompPhaseNameMacro(PHASE_RATIONALIZE,            "Rationalize IR",                 false, -1)
CompPhaseNameMacro(PHASE_SIMPLE_LOWERING,        "Do 'simple' lowering",           false, -1)

CompPhaseNameMacro(PHASE_LCLVARLIVENESS,         "Local var liveness",             true, -1)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INIT,    "Local var liveness init",        false, PHASE_LCLVARLIVENESS)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_PERBLOCK,"Per block local var liveness",   false, PHASE_LCLVARLIVENESS)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INTERBLOCK,  "Global local var liveness",  false, PHASE_LCLVARLIVENESS)

CompPhaseNameMacro(PHASE_LVA_ADJUST_REF_COUNTS,  "LVA adjust ref counts",          false, -1)

#ifdef LEGACY_BACKEND
CompPhaseNameMacro(PHASE_RA_ASSIGN_VARS,         "RA assign vars",                 false, -1)
#endif // LEGACY_BACKEND
CompPhaseNameMacro(PHASE_LOWERING_DECOMP,        "Lowering decomposition",         false, -1)
CompPhaseNameMacro(PHASE_LOWERING,               "Lowering nodeinfo",              false, -1)
#ifndef LEGACY_BACKEND
CompPhaseNameMacro(PHASE_LINEAR_SCAN,            "Linear scan register alloc",     true, -1)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_BUILD,      "LSRA build intervals",           false, PHASE_LINEAR_SCAN)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_ALLOC,      "LSRA allocate",                  false, PHASE_LINEAR_SCAN)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_RESOLVE,    "LSRA resolve",                   false, PHASE_LINEAR_SCAN)
#endif // !LEGACY_BACKEND
CompPhaseNameMacro(PHASE_GENERATE_CODE,          "Generate code",                  false, -1)
CompPhaseNameMacro(PHASE_EMIT_CODE,              "Emit code",                      false, -1)
CompPhaseNameMacro(PHASE_EMIT_GCEH,              "Emit GC+EH tables",              false, -1)

#undef CompPhaseNameMacro
