// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

CompPhaseNameMacro(PHASE_PRE_IMPORT,             "Pre-import",                     "PRE-IMP",  false, -1)
CompPhaseNameMacro(PHASE_IMPORTATION,            "Importation",                    "IMPORT",   false, -1)
CompPhaseNameMacro(PHASE_POST_IMPORT,            "Post-import",                    "POST-IMP", false, -1)
CompPhaseNameMacro(PHASE_MORPH,                  "Morph",                          "MORPH",    false, -1)
CompPhaseNameMacro(PHASE_GS_COOKIE,              "GS Cookie",                      "GS-COOK",  false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_PREDS,          "Compute preds",                  "PREDS",    false, -1)
CompPhaseNameMacro(PHASE_MARK_GC_POLL_BLOCKS,    "Mark GC poll blocks",            "GC-POLL",  false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS,   "Compute edge weights (1)",       "EDG-WGT",  false, -1)
#if FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_CREATE_FUNCLETS,        "Create EH funclets",             "EH-FUNC",  false, -1)
#endif // FEATURE_EH_FUNCLETS
CompPhaseNameMacro(PHASE_OPTIMIZE_LAYOUT,        "Optimize layout",                "LAYOUT",   false, -1)
CompPhaseNameMacro(PHASE_OPTIMIZE_LOOPS,         "Optimize loops",                 "LOOP-OPT", false, -1)
CompPhaseNameMacro(PHASE_CLONE_LOOPS,            "Clone loops",                    "LP-CLONE", false, -1)
CompPhaseNameMacro(PHASE_UNROLL_LOOPS,           "Unroll loops",                   "UNROLL",   false, -1)
CompPhaseNameMacro(PHASE_HOIST_LOOP_CODE,        "Hoist loop code",                "LP-HOIST", false, -1)
CompPhaseNameMacro(PHASE_MARK_LOCAL_VARS,        "Mark local vars",                "MARK-LCL", false, -1)
CompPhaseNameMacro(PHASE_OPTIMIZE_BOOLS,         "Optimize bools",                 "OPT-BOOL", false, -1)
CompPhaseNameMacro(PHASE_FIND_OPER_ORDER,        "Find oper order",                "OPER-ORD", false, -1)
CompPhaseNameMacro(PHASE_SET_BLOCK_ORDER,        "Set block order",                "BLK-ORD",  false, -1)
CompPhaseNameMacro(PHASE_BUILD_SSA,              "Build SSA representation",       "SSA",      true,  -1)
CompPhaseNameMacro(PHASE_BUILD_SSA_TOPOSORT,     "SSA: topological sort",          "SSA-SORT", false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_DOMS,         "SSA: Doms1",                     "SSA-DOMS", false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_LIVENESS,     "SSA: liveness",                  "SSA-LIVE", false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_IDF,          "SSA: IDF",                       "SSA-IDF",  false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_INSERT_PHIS,  "SSA: insert phis",               "SSA-PHI",  false, PHASE_BUILD_SSA)
CompPhaseNameMacro(PHASE_BUILD_SSA_RENAME,       "SSA: rename",                    "SSA-REN",  false, PHASE_BUILD_SSA)

CompPhaseNameMacro(PHASE_EARLY_PROP,             "Early Value Propagation",        "ERL-PROP", false, -1)
CompPhaseNameMacro(PHASE_VALUE_NUMBER,           "Do value numbering",             "VAL-NUM",  false, -1)

CompPhaseNameMacro(PHASE_OPTIMIZE_INDEX_CHECKS,  "Optimize index checks",          "OPT-CHK",  false, -1)

#if FEATURE_VALNUM_CSE
CompPhaseNameMacro(PHASE_OPTIMIZE_VALNUM_CSES,   "Optimize Valnum CSEs",           "OPT-CSE",  false, -1)
#endif  

CompPhaseNameMacro(PHASE_VN_COPY_PROP,           "VN based copy prop",             "CP-PROP",  false, -1)
#if ASSERTION_PROP
CompPhaseNameMacro(PHASE_ASSERTION_PROP_MAIN,    "Assertion prop",                 "AST-PROP", false, -1)
#endif
CompPhaseNameMacro(PHASE_UPDATE_FLOW_GRAPH,      "Update flow graph",              "UPD-FG",   false, -1)
CompPhaseNameMacro(PHASE_COMPUTE_EDGE_WEIGHTS2,  "Compute edge weights (2)",       "EDG-WGT2", false, -1)
CompPhaseNameMacro(PHASE_DETERMINE_FIRST_COLD_BLOCK, "Determine first cold block", "COLD-BLK", false, -1)
CompPhaseNameMacro(PHASE_RATIONALIZE,            "Rationalize IR",                 "RAT",      false, -1)
CompPhaseNameMacro(PHASE_SIMPLE_LOWERING,        "Do 'simple' lowering",           "SMP-LWR",  false, -1)

CompPhaseNameMacro(PHASE_LCLVARLIVENESS,         "Local var liveness",             "LIVENESS", true, -1)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INIT,    "Local var liveness init",        "LIV-INIT", false, PHASE_LCLVARLIVENESS)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_PERBLOCK,"Per block local var liveness",   "LIV-BLK",  false, PHASE_LCLVARLIVENESS)
CompPhaseNameMacro(PHASE_LCLVARLIVENESS_INTERBLOCK,  "Global local var liveness",  "LIV-GLBL", false, PHASE_LCLVARLIVENESS)

CompPhaseNameMacro(PHASE_LVA_ADJUST_REF_COUNTS,  "LVA adjust ref counts",          "REF-CNT",  false, -1)

#ifdef LEGACY_BACKEND
CompPhaseNameMacro(PHASE_RA_ASSIGN_VARS,         "RA assign vars",                 "REGALLOC", false, -1)
#endif // LEGACY_BACKEND
CompPhaseNameMacro(PHASE_LOWERING_DECOMP,        "Lowering decomposition",         "LWR-DEC",  false, -1)
CompPhaseNameMacro(PHASE_LOWERING,               "Lowering nodeinfo",              "LWR-INFO", false, -1)
#ifndef LEGACY_BACKEND
CompPhaseNameMacro(PHASE_LINEAR_SCAN,            "Linear scan register alloc",     "LSRA",     true, -1)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_BUILD,      "LSRA build intervals",           "LSRA-BLD", false, PHASE_LINEAR_SCAN)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_ALLOC,      "LSRA allocate",                  "LSRA-ALL", false, PHASE_LINEAR_SCAN)
CompPhaseNameMacro(PHASE_LINEAR_SCAN_RESOLVE,    "LSRA resolve",                   "LSRA-RES", false, PHASE_LINEAR_SCAN)
#endif // !LEGACY_BACKEND
CompPhaseNameMacro(PHASE_GENERATE_CODE,          "Generate code",                  "CODEGEN",  false, -1)
CompPhaseNameMacro(PHASE_EMIT_CODE,              "Emit code",                      "EMIT",     false, -1)
CompPhaseNameMacro(PHASE_EMIT_GCEH,              "Emit GC+EH tables",              "EMT-GCEH", false, -1)

#undef CompPhaseNameMacro
