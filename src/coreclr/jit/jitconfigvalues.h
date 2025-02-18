// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(RELEASE_CONFIG_INTEGER) || !defined(RELEASE_CONFIG_STRING) || !defined(RELEASE_CONFIG_METHODSET)
#error RELEASE_CONFIG_INTEGER, RELEASE_CONFIG_STRING, and RELEASE_CONFIG_METHODSET must be defined before including this file.
#endif

#ifdef DEBUG
#define CONFIG_INTEGER(name, key, defaultValue) RELEASE_CONFIG_INTEGER(name, key, defaultValue)
#define CONFIG_STRING(name, key)                RELEASE_CONFIG_STRING(name, key)
#define CONFIG_METHODSET(name, key)             RELEASE_CONFIG_METHODSET(name, key)
#else
#define CONFIG_INTEGER(name, key, defaultValue)
#define CONFIG_STRING(name, key)
#define CONFIG_METHODSET(name, key)
#endif

#ifdef DEBUG
#define OPT_CONFIG
#endif

#ifdef OPT_CONFIG
#define OPT_CONFIG_INTEGER(name, key, defaultValue) RELEASE_CONFIG_INTEGER(name, key, defaultValue)
#define OPT_CONFIG_STRING(name, key)                RELEASE_CONFIG_STRING(name, key)
#define OPT_CONFIG_METHODSET(name, key)             RELEASE_CONFIG_METHODSET(name, key)
#else
#define OPT_CONFIG_INTEGER(name, key, defaultValue)
#define OPT_CONFIG_STRING(name, key)
#define OPT_CONFIG_METHODSET(name, key)
#endif

// Max number of functions to use altjit for (decimal)
CONFIG_INTEGER(AltJitLimit, "AltJitLimit", 0)

// If AltJit hits an assert, fall back to the fallback JIT. Useful in conjunction with DOTNET_ContinueOnAssert=1
CONFIG_INTEGER(AltJitSkipOnAssert, "AltJitSkipOnAssert", 0)

// Breaks when using internal logging on a particular token value.
CONFIG_INTEGER(BreakOnDumpToken, "BreakOnDumpToken", 0xffffffff)

// Halts the jit on verification failure
CONFIG_INTEGER(DebugBreakOnVerificationFailure, "DebugBreakOnVerificationFailure", 0)

CONFIG_INTEGER(DisplayLoopHoistStats, "JitLoopHoistStats", 0) // Display JIT loop hoisting statistics

// Display JIT Linear Scan Register Allocator statistics
// If set to "1", display the stats in textual format.
// If set to "2", display the stats in csv format.
// If set to "3", display the stats in summarize format.
// Recommended to use with JitStdOutFile flag.
CONFIG_INTEGER(DisplayLsraStats, "JitLsraStats", 0)

CONFIG_STRING(JitLsraOrdering, "JitLsraOrdering")        // LSRA heuristics ordering
CONFIG_INTEGER(EnablePCRelAddr, "JitEnablePCRelAddr", 1) // Whether absolute addr be encoded as PC-rel offset by
                                                         // RyuJIT where possible
CONFIG_INTEGER(JitAssertOnMaxRAPasses, "JitAssertOnMaxRAPasses", 0)
CONFIG_INTEGER(JitBreakEmitOutputInstr, "JitBreakEmitOutputInstr", -1)
CONFIG_INTEGER(JitBreakMorphTree, "JitBreakMorphTree", 0xffffffff)
CONFIG_INTEGER(JitBreakOnBadCode, "JitBreakOnBadCode", 0)
CONFIG_INTEGER(JitBreakOnMinOpts, "JITBreakOnMinOpts", 0) // Halt if jit switches to MinOpts
CONFIG_INTEGER(JitCloneLoops, "JitCloneLoops", 1)         // If 0, don't clone. Otherwise clone loops for optimizations.
CONFIG_INTEGER(JitCloneLoopsWithEH, "JitCloneLoopsWithEH", 1) // If 0, don't clone loops containing EH regions
CONFIG_INTEGER(JitCloneLoopsWithGdvTests, "JitCloneLoopsWithGdvTests", 1)     // If 0, don't clone loops based on
                                                                              // invariant type/method address tests
RELEASE_CONFIG_INTEGER(JitCloneLoopsSizeLimit, "JitCloneLoopsSizeLimit", 400) // limit cloning to loops with no more
                                                                              // than this many tree nodes
CONFIG_INTEGER(JitDebugLogLoopCloning, "JitDebugLogLoopCloning", 0) // In debug builds log places where loop cloning
                                                                    // optimizations are performed on the fast path.
CONFIG_INTEGER(JitDefaultFill, "JitDefaultFill", 0xdd) // In debug builds, initialize the memory allocated by the nra
                                                       // with this byte.

// Minimum weight needed for the first block of a loop to make it a candidate for alignment.
CONFIG_INTEGER(JitAlignLoopMinBlockWeight, "JitAlignLoopMinBlockWeight", DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT)

// For non-adaptive alignment, minimum loop size (in bytes) for which alignment will be done.
// Defaults to 3 blocks of 32 bytes chunks = 96 bytes.
CONFIG_INTEGER(JitAlignLoopMaxCodeSize, "JitAlignLoopMaxCodeSize", DEFAULT_MAX_LOOPSIZE_FOR_ALIGN)

// For non-adaptive alignment, address boundary (power of 2) at which loop alignment should be done. By default, 32B.
CONFIG_INTEGER(JitAlignLoopBoundary, "JitAlignLoopBoundary", DEFAULT_ALIGN_LOOP_BOUNDARY)

// If set, for non-adaptive alignment, ensure loop jmps are not on or cross alignment boundary.
CONFIG_INTEGER(JitAlignLoopForJcc, "JitAlignLoopForJcc", 0)

// If set, perform adaptive loop alignment that limits number of padding based on loop size.
CONFIG_INTEGER(JitAlignLoopAdaptive, "JitAlignLoopAdaptive", 1)

// If set, try to hide align instruction (if any) behind an unconditional jump instruction (if any)
// that is present before the loop start.
CONFIG_INTEGER(JitHideAlignBehindJmp, "JitHideAlignBehindJmp", 1)

// Track stores to locals done through return buffers.
CONFIG_INTEGER(JitOptimizeStructHiddenBuffer, "JitOptimizeStructHiddenBuffer", 1)

CONFIG_INTEGER(JitUnrollLoopMaxIterationCount,
               "JitUnrollLoopMaxIterationCount",
               DEFAULT_UNROLL_LOOP_MAX_ITERATION_COUNT)

CONFIG_INTEGER(JitUnrollLoopsWithEH, "JitUnrollLoopsWithEH", 0) // If 0, don't unroll loops containing EH regions

CONFIG_INTEGER(JitDirectAlloc, "JitDirectAlloc", 0)
CONFIG_INTEGER(JitDoubleAlign, "JitDoubleAlign", 1)
CONFIG_INTEGER(JitEmitPrintRefRegs, "JitEmitPrintRefRegs", 0)
CONFIG_INTEGER(JitEnableDevirtualization, "JitEnableDevirtualization", 1)         // Enable devirtualization in importer
CONFIG_INTEGER(JitEnableLateDevirtualization, "JitEnableLateDevirtualization", 1) // Enable devirtualization after
                                                                                  // inlining
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, "JitExpensiveDebugCheckLevel", 0)     // Level indicates how much checking
                                                                                  // beyond the default to do in debug
                                                                                  // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, "JitForceFallback", 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitFullyInt, "JitFullyInt", 0)           // Forces Fully interruptible code
CONFIG_INTEGER(JitFunctionTrace, "JitFunctionTrace", 0) // If non-zero, print JIT start/end logging
CONFIG_INTEGER(JitGCChecks, "JitGCChecks", 0)
CONFIG_INTEGER(JitGCInfoLogging, "JitGCInfoLogging", 0) // If true, prints GCInfo-related output to standard output.
CONFIG_INTEGER(JitHashBreak, "JitHashBreak", -1)        // Same as JitBreak, but for a method hash
CONFIG_INTEGER(JitHashHalt, "JitHashHalt", -1)          // Same as JitHalt, but for a method hash
CONFIG_INTEGER(JitInlineAdditionalMultiplier, "JitInlineAdditionalMultiplier", 0)
CONFIG_INTEGER(JitInlinePrintStats, "JitInlinePrintStats", 0)
CONFIG_INTEGER(JitInlineSize, "JITInlineSize", DEFAULT_MAX_INLINE_SIZE)
CONFIG_INTEGER(JitInlineDepth, "JITInlineDepth", DEFAULT_MAX_INLINE_DEPTH)
CONFIG_INTEGER(JitForceInlineDepth, "JITForceInlineDepth", DEFAULT_MAX_FORCE_INLINE_DEPTH)
CONFIG_INTEGER(JitLongAddress, "JitLongAddress", 0) // Force using the large pseudo instruction form for long address
CONFIG_INTEGER(JitMaxUncheckedOffset, "JitMaxUncheckedOffset", 8)

//
// MinOpts
//

CONFIG_INTEGER(JitMinOpts, "JITMinOpts", 0)        // Forces MinOpts
CONFIG_METHODSET(JitMinOptsName, "JITMinOptsName") // Forces MinOpts for a named function

// Internal jit control of MinOpts
CONFIG_INTEGER(JitMinOptsBbCount, "JITMinOptsBbCount", DEFAULT_MIN_OPTS_BB_COUNT)
CONFIG_INTEGER(JitMinOptsCodeSize, "JITMinOptsCodeSize", DEFAULT_MIN_OPTS_CODE_SIZE)
CONFIG_INTEGER(JitMinOptsInstrCount, "JITMinOptsInstrCount", DEFAULT_MIN_OPTS_INSTR_COUNT)
CONFIG_INTEGER(JitMinOptsLvNumCount, "JITMinOptsLvNumcount", DEFAULT_MIN_OPTS_LV_NUM_COUNT)
CONFIG_INTEGER(JitMinOptsLvRefCount, "JITMinOptsLvRefcount", DEFAULT_MIN_OPTS_LV_REF_COUNT)

CONFIG_INTEGER(JitNoCSE, "JitNoCSE", 0)
CONFIG_INTEGER(JitNoCSE2, "JitNoCSE2", 0)
CONFIG_INTEGER(JitNoForceFallback, "JitNoForceFallback", 0) // Set to non-zero to prevent NOWAY assert testing.
                                                            // Overrides DOTNET_JitForceFallback and JIT stress
                                                            // flags.
CONFIG_INTEGER(JitNoForwardSub, "JitNoForwardSub", 0)       // Disables forward sub
CONFIG_INTEGER(JitNoHoist, "JitNoHoist", 0)
CONFIG_INTEGER(JitNoMemoryBarriers, "JitNoMemoryBarriers", 0)   // If 1, don't generate memory barriers
CONFIG_INTEGER(JitNoStructPromotion, "JitNoStructPromotion", 0) // Disables struct promotion 1 - for all, 2 - for
                                                                // params.
CONFIG_INTEGER(JitNoUnroll, "JitNoUnroll", 0)
CONFIG_INTEGER(JitOrder, "JitOrder", 0)
CONFIG_INTEGER(JitQueryCurrentStaticFieldClass, "JitQueryCurrentStaticFieldClass", 1)
CONFIG_INTEGER(JitReportFastTailCallDecisions, "JitReportFastTailCallDecisions", 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, "JITPInvokeCheckEnabled", 0)
CONFIG_INTEGER(JitPInvokeEnabled, "JITPInvokeEnabled", 1)

CONFIG_INTEGER(JitHoistLimit, "JitHoistLimit", -1) // Specifies the maximum number of hoist candidates to hoist

// Controls verbosity for JitPrintInlinedMethods. Ignored for JitDump where it's always set.
CONFIG_INTEGER(JitPrintInlinedMethodsVerbose, "JitPrintInlinedMethodsVerboseLevel", 0)

// Prints a tree of inlinees for a specific method (use '*' for all methods)
CONFIG_METHODSET(JitPrintInlinedMethods, "JitPrintInlinedMethods")

CONFIG_METHODSET(JitPrintDevirtualizedMethods, "JitPrintDevirtualizedMethods")
// -1: just do internal checks (CHECK_HASLIKELIHOOD | CHECK_LIKELIHOODSUM | RAISE_ASSERT)
// Else bitflag:
//  - 0x1: check edges have likelihoods
//  - 0x2: check edge likelihoods sum to 1.0
//  - 0x4: fully check likelihoods
//  - 0x8: assert on check failure
//  - 0x10: check block profile weights
CONFIG_INTEGER(JitProfileChecks, "JitProfileChecks", -1)

CONFIG_INTEGER(JitRequired, "JITRequired", -1)
CONFIG_INTEGER(JitStackAllocToLocalSize, "JitStackAllocToLocalSize", DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE)
CONFIG_INTEGER(JitSkipArrayBoundCheck, "JitSkipArrayBoundCheck", 0)
CONFIG_INTEGER(JitSlowDebugChecksEnabled, "JitSlowDebugChecksEnabled", 1) // Turn on slow debug checks

// On ARM, use this as the maximum function/funclet size for creating function fragments (and creating
// multiple RUNTIME_FUNCTION entries)
CONFIG_INTEGER(JitSplitFunctionSize, "JitSplitFunctionSize", 0)

// Perturb order of processing of blocks in SSA; 0 = no stress; 1 = use method hash; * = supplied value as random hash
CONFIG_INTEGER(JitSsaStress, "JitSsaStress", 0)

CONFIG_INTEGER(JitStackChecks, "JitStackChecks", 0)

// Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary stress based on a hash of the method and
// this value.
CONFIG_INTEGER(JitStress, "JitStress", 0)

CONFIG_INTEGER(JitStressBBProf, "JitStressBBProf", 0)                         // Internal Jit stress mode
CONFIG_INTEGER(JitStressProcedureSplitting, "JitStressProcedureSplitting", 0) // Always split after the first basic
                                                                              // block.
CONFIG_INTEGER(JitStressRegs, "JitStressRegs", 0)
CONFIG_STRING(JitStressRegsRange, "JitStressRegsRange") // Only apply JitStressRegs to methods in this hash range

// If non-negative value N, only stress split the first N trees.
CONFIG_INTEGER(JitStressSplitTreeLimit, "JitStressSplitTreeLimit", -1)

// If non-zero, assert if # of VNF_MapSelect applications considered reaches this.
CONFIG_INTEGER(JitVNMapSelLimit, "JitVNMapSelLimit", 0)

// If non-zero, and the compilation succeeds for an AltJit, then use the code. If zero, then we always throw away the
// generated code and fall back to the default compiler.
CONFIG_INTEGER(RunAltJitCode, "RunAltJitCode", 1)

CONFIG_INTEGER(RunComponentUnitTests, "JitComponentUnitTests", 0) // Run JIT component unit tests
CONFIG_INTEGER(ShouldInjectFault, "InjectFault", 0)
CONFIG_INTEGER(TailcallStress, "TailcallStress", 0)

CONFIG_METHODSET(JitBreak, "JitBreak") // Stops in the importer when compiling a specified method
CONFIG_METHODSET(JitDebugBreak, "JitDebugBreak")

//
// JitDump
//

CONFIG_METHODSET(JitDump, "JitDump")                                  // Dumps trees for specified method
CONFIG_INTEGER(JitHashDump, "JitHashDump", -1)                        // Same as JitDump, but for a method hash
CONFIG_INTEGER(JitDumpTier0, "JitDumpTier0", 1)                       // Dump tier0 jit compilations
CONFIG_INTEGER(JitDumpOSR, "JitDumpOSR", 1)                           // Dump OSR jit compilations
CONFIG_INTEGER(JitDumpAtOSROffset, "JitDumpAtOSROffset", -1)          // Dump only OSR jit compilations with this offset
CONFIG_INTEGER(JitDumpInlinePhases, "JitDumpInlinePhases", 1)         // Dump inline compiler phases
CONFIG_INTEGER(JitDumpASCII, "JitDumpASCII", 1)                       // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpTerseLsra, "JitDumpTerseLsra", 1)               // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, "JitDumpToDebugger", 0)             // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, "JitDumpVerboseSsa", 0)             // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, "JitDumpVerboseTrees", 0)         // Enable more verbose tree dumps
CONFIG_INTEGER(JitDumpTreeIDs, "JitDumpTreeIDs", 1)                   // Print tree IDs in dumps
CONFIG_INTEGER(JitDumpBeforeAfterMorph, "JitDumpBeforeAfterMorph", 0) // If 1, display each tree before/after
                                                                      // morphing

// When dumping blocks, display "*" instead of block number for lexical "next" blocks, to reduce clutter.
CONFIG_INTEGER(JitDumpTerseNextBlock, "JitDumpTerseNextBlock", 0)

CONFIG_METHODSET(JitEHDump, "JitEHDump") // Dump the EH table for the method, as reported to the VM

CONFIG_METHODSET(JitExclude, "JitExclude")
CONFIG_INTEGER(JitFakeProcedureSplitting, "JitFakeProcedureSplitting", 0) // Do code splitting independent of VM.
CONFIG_METHODSET(JitForceProcedureSplitting, "JitForceProcedureSplitting")
CONFIG_METHODSET(JitGCDump, "JitGCDump")
CONFIG_METHODSET(JitDebugDump, "JitDebugDump")
CONFIG_METHODSET(JitHalt, "JitHalt") // Emits break instruction into jitted code
CONFIG_METHODSET(JitInclude, "JitInclude")
CONFIG_METHODSET(JitLateDisasm, "JitLateDisasm")  // Generate late disassembly for the specified methods.
CONFIG_STRING(JitLateDisasmTo, "JitLateDisasmTo") // If set, sends late disassembly output to this file instead of
                                                  // stdout/JitStdOutFile.
CONFIG_METHODSET(JitNoProcedureSplitting, "JitNoProcedureSplitting")     // Disallow procedure splitting for specified
                                                                         // methods
CONFIG_METHODSET(JitNoProcedureSplittingEH, "JitNoProcedureSplittingEH") // Disallow procedure splitting for
                                                                         // specified methods if they contain
                                                                         // exception handling
CONFIG_METHODSET(JitStressOnly, "JitStressOnly") // Internal Jit stress mode: stress only the specified method(s)
CONFIG_METHODSET(JitUnwindDump, "JitUnwindDump") // Dump the unwind codes for the method

//
// JitDumpFg - dump flowgraph
//

CONFIG_METHODSET(JitDumpFg, "JitDumpFg")        // Dumps Xml/Dot Flowgraph for specified method
CONFIG_STRING(JitDumpFgDir, "JitDumpFgDir")     // Directory for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgFile, "JitDumpFgFile")   // Filename for Xml/Dot flowgraph dump(s) (default: "default")
CONFIG_STRING(JitDumpFgPhase, "JitDumpFgPhase") // Phase-based Xml/Dot flowgraph support. Set to the short name of a
                                                // phase to see the flowgraph after that phase. Leave unset to dump
                                                // after COLD-BLK (determine first cold block) or set to * for all
                                                // phases
CONFIG_STRING(JitDumpFgPrePhase, "JitDumpFgPrePhase") // Same as JitDumpFgPhase, but specifies to dump pre-phase, not
                                                      // post-phase.
CONFIG_INTEGER(JitDumpFgDot, "JitDumpFgDot", 1)       // 0 == dump XML format; non-zero == dump DOT format
CONFIG_INTEGER(JitDumpFgEH, "JitDumpFgEH", 0)         // 0 == no EH regions; non-zero == include EH regions
CONFIG_INTEGER(JitDumpFgLoops, "JitDumpFgLoops", 0)   // 0 == no loop regions; non-zero == include loop regions

CONFIG_INTEGER(JitDumpFgConstrained, "JitDumpFgConstrained", 1) // 0 == don't constrain to mostly linear layout;
                                                                // non-zero == force mostly lexical block
                                                                // linear layout
CONFIG_INTEGER(JitDumpFgBlockID, "JitDumpFgBlockID", 0)         // 0 == display block with bbNum; 1 == display with both
                                                                // bbNum and bbID
CONFIG_INTEGER(JitDumpFgBlockFlags, "JitDumpFgBlockFlags", 0)   // 0 == don't display block flags; 1 == display flags
CONFIG_INTEGER(JitDumpFgLoopFlags, "JitDumpFgLoopFlags", 0)     // 0 == don't display loop flags; 1 == display flags
CONFIG_INTEGER(JitDumpFgBlockOrder, "JitDumpFgBlockOrder", 0)   // 0 == bbNext order;  1 == bbNum order; 2 == bbID
                                                                // order
CONFIG_INTEGER(JitDumpFgMemorySsa, "JitDumpFgMemorySsa", 0)     // non-zero: show memory phis + SSA/VNs

CONFIG_STRING(JitRange, "JitRange")

// Internal Jit stress mode: stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL.
// Unless JitStressModeNamesOnly is non-zero, other stress modes from a JitStress setting may also be invoked.
CONFIG_STRING(JitStressModeNames, "JitStressModeNames")

// Internal Jit stress: if nonzero, only enable stress modes listed in JitStressModeNames.
CONFIG_INTEGER(JitStressModeNamesOnly, "JitStressModeNamesOnly", 0)

// Internal Jit stress mode: only allow stress using the given set of stress mode names, e.g. STRESS_REGS,
// STRESS_TAILCALL. Note that JitStress must be enabled first, and then only the mentioned stress modes are allowed
// to be used, at the same percentage weighting as with JitStress -- the stress modes mentioned are NOT
// unconditionally true for a call to `compStressCompile`. This is basically the opposite of JitStressModeNamesNot.
CONFIG_STRING(JitStressModeNamesAllow, "JitStressModeNamesAllow")

// Internal Jit stress mode: do NOT stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, "JitStressModeNamesNot")

CONFIG_STRING(JitStressRange, "JitStressRange")        // Internal Jit stress mode
CONFIG_METHODSET(JitEmitUnitTests, "JitEmitUnitTests") // Generate emitter unit tests in the specified functions
CONFIG_STRING(JitEmitUnitTestsSections, "JitEmitUnitTestsSections") // Generate this set of unit tests

///
/// JIT Hardware Intrinsics
///
CONFIG_INTEGER(EnableIncompleteISAClass, "EnableIncompleteISAClass", 0) // Enable testing not-yet-implemented

//
// JitDisasm
//

RELEASE_CONFIG_METHODSET(JitDisasm, "JitDisasm")                  // Print codegen for given methods
RELEASE_CONFIG_INTEGER(JitDisasmTesting, "JitDisasmTesting", 0)   // Display BEGIN METHOD/END METHOD anchors for disasm
                                                                  // testing
RELEASE_CONFIG_INTEGER(JitDisasmDiffable, "JitDisasmDiffable", 0) // Make the disassembly diff-able
RELEASE_CONFIG_INTEGER(JitDisasmSummary, "JitDisasmSummary", 0)   // Prints all jitted methods to the console

// Hides disassembly for unoptimized codegen
RELEASE_CONFIG_INTEGER(JitDisasmOnlyOptimized, "JitDisasmOnlyOptimized", 0)

// Print the alignment boundaries.
RELEASE_CONFIG_INTEGER(JitDisasmWithAlignmentBoundaries, "JitDisasmWithAlignmentBoundaries", 0)

// Print the instruction code bytes
RELEASE_CONFIG_INTEGER(JitDisasmWithCodeBytes, "JitDisasmWithCodeBytes", 0)

// Only show JitDisasm and related info for methods from this semicolon-delimited list of assemblies.
CONFIG_STRING(JitDisasmAssemblies, "JitDisasmAssemblies")

// Dump interleaved GC Info for any method disassembled.
CONFIG_INTEGER(JitDisasmWithGC, "JitDisasmWithGC", 0)

// Dump interleaved debug info for any method disassembled.
CONFIG_INTEGER(JitDisasmWithDebugInfo, "JitDisasmWithDebugInfo", 0)

// Display native code when any register spilling occurs
CONFIG_INTEGER(JitDisasmSpilled, "JitDisasmSpilled", 0)

// Print the process address next to each instruction of the disassembly
CONFIG_INTEGER(JitDasmWithAddress, "JitDasmWithAddress", 0)

RELEASE_CONFIG_STRING(JitStdOutFile, "JitStdOutFile") // If set, sends JIT's stdout output to this file.

RELEASE_CONFIG_INTEGER(RichDebugInfo, "RichDebugInfo", 0) // If 1, keep rich debug info and report it back to the EE

CONFIG_STRING(WriteRichDebugInfoFile, "WriteRichDebugInfoFile") // Write rich debug info in JSON format to this file

#if FEATURE_LOOP_ALIGN
RELEASE_CONFIG_INTEGER(JitAlignLoops, "JitAlignLoops", 1) // If set, align inner loops
#else
RELEASE_CONFIG_INTEGER(JitAlignLoops, "JitAlignLoops", 0)
#endif

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate
// fallback to main JIT on hitting a NYI.
RELEASE_CONFIG_INTEGER(AltJitAssertOnNYI, "AltJitAssertOnNYI", 1) // Controls the AltJit behavior of NYI stuff

// Enable the register allocator to support EH-write thru: partial enregistration of vars exposed on EH boundaries
RELEASE_CONFIG_INTEGER(EnableEHWriteThru, "EnableEHWriteThru", 1)

// Enable the enregistration of locals that are defined or used in a multireg context.
RELEASE_CONFIG_INTEGER(EnableMultiRegLocals, "EnableMultiRegLocals", 1)

// Disables inlining of all methods
RELEASE_CONFIG_INTEGER(JitNoInline, "JitNoInline", 0)

#if defined(DEBUG)
CONFIG_INTEGER(JitStressRex2Encoding, "JitStressRex2Encoding", 0) // Enable rex2 encoding for compatible instructions.
CONFIG_INTEGER(JitStressPromotedEvexEncoding, "JitStressPromotedEvexEncoding", 0) // Enable promoted EVEX encoding for
                                                                                  // compatible instructions.
#endif

// clang-format off

#if defined(TARGET_AMD64) || defined(TARGET_X86)
// Enable EVEX encoding for SIMD instructions when AVX-512VL is available.
CONFIG_INTEGER(JitStressEvexEncoding, "JitStressEvexEncoding", 0)
#endif

RELEASE_CONFIG_INTEGER(PreferredVectorBitWidth,     "PreferredVectorBitWidth",   0) // The preferred decimal width, in bits, to use for any implicit vectorization emitted. A value less than 128 is treated as the system default.

//
// Hardware Intrinsic ISAs; keep in sync with clrconfigvalues.h
//
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
//TODO: should implement LoongArch64's features.
//TODO-RISCV64-CQ: should implement RISCV64's features.
RELEASE_CONFIG_INTEGER(EnableHWIntrinsic,           "EnableHWIntrinsic",         0) // Allows Base+ hardware intrinsics to be disabled
#else
RELEASE_CONFIG_INTEGER(EnableHWIntrinsic,           "EnableHWIntrinsic",         1) // Allows Base+ hardware intrinsics to be disabled
#endif // defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
RELEASE_CONFIG_INTEGER(EnableAES,                   "EnableAES",                 1) // Allows AES+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX,                   "EnableAVX",                 1) // Allows AVX+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX2,                  "EnableAVX2",                1) // Allows AVX2+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512BW,              "EnableAVX512BW",            1) // Allows AVX512BW+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512BW_VL,           "EnableAVX512BW_VL",         1) // Allows AVX512BW+ AVX512VL+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512CD,              "EnableAVX512CD",            1) // Allows AVX512CD+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512CD_VL,           "EnableAVX512CD_VL",         1) // Allows AVX512CD+ AVX512VL+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512DQ,              "EnableAVX512DQ",            1) // Allows AVX512DQ+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512DQ_VL,           "EnableAVX512DQ_VL",         1) // Allows AVX512DQ+ AVX512VL+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512F,               "EnableAVX512F",             1) // Allows AVX512F+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512F_VL,            "EnableAVX512F_VL",          1) // Allows AVX512F+ AVX512VL+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512VBMI,            "EnableAVX512VBMI",          1) // Allows AVX512VBMI+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX512VBMI_VL,         "EnableAVX512VBMI_VL",       1) // Allows AVX512VBMI_VL+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX10v1,               "EnableAVX10v1",             1) // Allows AVX10v1+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVX10v2,               "EnableAVX10v2",             1) // Allows AVX10v2+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAVXVNNI,               "EnableAVXVNNI",             1) // Allows AVXVNNI+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableBMI1,                  "EnableBMI1",                1) // Allows BMI1+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableBMI2,                  "EnableBMI2",                1) // Allows BMI2+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableFMA,                   "EnableFMA",                 1) // Allows FMA+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableGFNI,                  "EnableGFNI",                1) // Allows GFNI+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableLZCNT,                 "EnableLZCNT",               1) // Allows LZCNT+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnablePCLMULQDQ,             "EnablePCLMULQDQ",           1) // Allows PCLMULQDQ+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableVPCLMULQDQ,            "EnableVPCLMULQDQ",          1) // Allows VPCLMULQDQ+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnablePOPCNT,                "EnablePOPCNT",              1) // Allows POPCNT+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE,                   "EnableSSE",                 1) // Allows SSE+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE2,                  "EnableSSE2",                1) // Allows SSE2+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE3,                  "EnableSSE3",                1) // Allows SSE3+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE3_4,                "EnableSSE3_4",              1) // Allows SSE3+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE41,                 "EnableSSE41",               1) // Allows SSE4.1+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSE42,                 "EnableSSE42",               1) // Allows SSE4.2+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableSSSE3,                 "EnableSSSE3",               1) // Allows SSSE3+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableAPX,                   "EnableAPX",                 0) // Allows APX+ features to be disabled
#elif defined(TARGET_ARM64)
RELEASE_CONFIG_INTEGER(EnableArm64AdvSimd,          "EnableArm64AdvSimd",        1) // Allows Arm64 AdvSimd+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Aes,              "EnableArm64Aes",            1) // Allows Arm64 Aes+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Atomics,          "EnableArm64Atomics",        1) // Allows Arm64 Atomics+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Crc32,            "EnableArm64Crc32",          1) // Allows Arm64 Crc32+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Dczva,            "EnableArm64Dczva",          1) // Allows Arm64 Dczva+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Dp,               "EnableArm64Dp",             1) // Allows Arm64 Dp+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Rdm,              "EnableArm64Rdm",            1) // Allows Arm64 Rdm+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Sha1,             "EnableArm64Sha1",           1) // Allows Arm64 Sha1+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Sha256,           "EnableArm64Sha256",         1) // Allows Arm64 Sha256+ hardware intrinsics to be disabled
RELEASE_CONFIG_INTEGER(EnableArm64Sve,              "EnableArm64Sve",            1) // Allows Arm64 Sve+ hardware intrinsics to be disabled
#endif

RELEASE_CONFIG_INTEGER(EnableEmbeddedBroadcast,     "EnableEmbeddedBroadcast",   1) // Allows embedded broadcasts to be disabled
RELEASE_CONFIG_INTEGER(EnableEmbeddedMasking,       "EnableEmbeddedMasking",     1) // Allows embedded masking to be disabled
RELEASE_CONFIG_INTEGER(EnableApxNDD,                "EnableApxNDD",              0) // Allows APX NDD feature to be disabled

// clang-format on

#ifdef FEATURE_SIMD
// Default 0, ValueNumbering of SIMD nodes and HW Intrinsic nodes enabled
// If 1, then disable ValueNumbering of SIMD nodes
// If 2, then disable ValueNumbering of HW Intrinsic nodes
// If 3, disable both SIMD and HW Intrinsic nodes
RELEASE_CONFIG_INTEGER(JitDisableSimdVN, "JitDisableSimdVN", 0)
#endif

// Default 0, enable the CSE of Constants, including nearby offsets. (only for ARM/ARM64)
// If 1, disable all the CSE of Constants
// If 2, enable the CSE of Constants but don't combine with nearby offsets. (only for ARM/ARM64)
// If 3, enable the CSE of Constants including nearby offsets. (all platforms)
// If 4, enable the CSE of Constants but don't combine with nearby offsets. (all platforms)
//
#define CONST_CSE_ENABLE_ARM            0
#define CONST_CSE_DISABLE_ALL           1
#define CONST_CSE_ENABLE_ARM_NO_SHARING 2
#define CONST_CSE_ENABLE_ALL            3
#define CONST_CSE_ENABLE_ALL_NO_SHARING 4
RELEASE_CONFIG_INTEGER(JitConstCSE, "JitConstCSE", CONST_CSE_ENABLE_ARM)

// If nonzero, use the greedy RL policy.
//
RELEASE_CONFIG_INTEGER(JitRLCSEGreedy, "JitRLCSEGreedy", 0)

// If nonzero, dump out details of parameterized policy evaluation and gradient updates.
RELEASE_CONFIG_INTEGER(JitRLCSEVerbose, "JitRLCSEVerbose", 0)

// Allow fine-grained controls of CSEs done in a particular method
//
// Specify method that will respond to the CSEMask.
// -1 means feature disabled and all methods run CSE normally.
CONFIG_INTEGER(JitCSEHash, "JitCSEHash", -1)

// Bitmask of allowed CSEs in methods specified by JitCSEHash.
// These bits control the "cse attempts" made by normal jitting,
// for the first 32 CSEs attempted (Note this is not the same as
// the CSE candidate number, which reflects the order
// in which CSEs were discovered).
//
// 0: do no CSEs
// 1: do only the first CSE
// 2: do only the second CSE
// C: do only the third and fourth CSEs
// F: do only the first four CSEs
// ...etc...
// FFFFFFFF : do all the CSEs normally done
CONFIG_INTEGER(JitCSEMask, "JitCSEMask", 0)

// Enable metric output in jit disasm and elsewhere
CONFIG_INTEGER(JitMetrics, "JitMetrics", 0)

// When nonzero, choose CSE candidates randomly, with hash salt specified by the (decimal) value of the config.
CONFIG_INTEGER(JitRandomCSE, "JitRandomCSE", 0)

// When set, specifies the exact CSEs to perform as a sequence of CSE candidate numbers.
CONFIG_STRING(JitReplayCSE, "JitReplayCSE")

// When set, specify the sequence of rewards from the CSE replay.
// There should be one reward per step in the sequence.
CONFIG_STRING(JitReplayCSEReward, "JitReplayCSEReward")

// When set, specifies the initial parameter string for
// the reinforcement-learning based CSE heuristic.
//
// Note you can also set JitReplayCSE and JitReplayCSEPerfScore
// along with this, in which case we are asking for a policy
// evaluation/update based on the provided sequence.
CONFIG_STRING(JitRLCSE, "JitRLCSE")

// When set, specify the alpha value (step size) to use in learning.
CONFIG_STRING(JitRLCSEAlpha, "JitRLCSEAlpha")

// If nonzero, dump candidate feature values
CONFIG_INTEGER(JitRLCSECandidateFeatures, "JitRLCSECandidateFeatures", 0)

// Enable CSE_HeuristicRLHook
CONFIG_INTEGER(JitRLHook, "JitRLHook", 0) // If 1, emit RL callbacks

// If 1, emit feature column names
CONFIG_INTEGER(JitRLHookEmitFeatureNames, "JitRLHookEmitFeatureNames", 0)

// A list of CSEs to choose, in the order they should be applied.
CONFIG_STRING(JitRLHookCSEDecisions, "JitRLHookCSEDecisions")

#if !defined(DEBUG) && !defined(_DEBUG)
RELEASE_CONFIG_INTEGER(JitEnableNoWayAssert, "JitEnableNoWayAssert", 0)
#else  // defined(DEBUG) || defined(_DEBUG)
RELEASE_CONFIG_INTEGER(JitEnableNoWayAssert, "JitEnableNoWayAssert", 1)
#endif // !defined(DEBUG) && !defined(_DEBUG)

// The following should be wrapped inside "#if MEASURE_MEM_ALLOC / #endif", but
// some files include this one without bringing in the definitions from "jit.h"
// so we don't always know what the "true" value of that flag should be. For now
// we take the easy way out and always include the flag, even in release builds
// (normally MEASURE_MEM_ALLOC is off for release builds but if it's toggled on
// for release in "jit.h" the flag would be missing for some includers).
// TODO-Cleanup: need to make 'MEASURE_MEM_ALLOC' well-defined here at all times.
RELEASE_CONFIG_INTEGER(DisplayMemStats, "JitMemStats", 0) // Display JIT memory usage statistics

CONFIG_INTEGER(JitEnregStats, "JitEnregStats", 0) // Display JIT enregistration statistics

RELEASE_CONFIG_INTEGER(JitAggressiveInlining, "JitAggressiveInlining", 0) // Aggressive inlining of all methods
RELEASE_CONFIG_INTEGER(JitELTHookEnabled, "JitELTHookEnabled", 0)         // If 1, emit Enter/Leave/TailCall callbacks
RELEASE_CONFIG_INTEGER(JitInlineSIMDMultiplier, "JitInlineSIMDMultiplier", 3)

// Ex lclMAX_TRACKED constant.
RELEASE_CONFIG_INTEGER(JitMaxLocalsToTrack, "JitMaxLocalsToTrack", 0x400)

#if defined(FEATURE_ENABLE_NO_RANGE_CHECKS)
RELEASE_CONFIG_INTEGER(JitNoRngChks, "JitNoRngChks", 0) // If 1, don't generate range checks
#endif

OPT_CONFIG_INTEGER(JitDoAssertionProp, "JitDoAssertionProp", 1) // Perform assertion propagation optimization
OPT_CONFIG_INTEGER(JitDoCopyProp, "JitDoCopyProp", 1) // Perform copy propagation on variables that appear redundant
OPT_CONFIG_INTEGER(JitDoOptimizeIVs, "JitDoOptimizeIVs", 1)     // Perform optimization of induction variables
OPT_CONFIG_INTEGER(JitDoEarlyProp, "JitDoEarlyProp", 1)         // Perform Early Value Propagation
OPT_CONFIG_INTEGER(JitDoLoopHoisting, "JitDoLoopHoisting", 1)   // Perform loop hoisting on loop invariant values
OPT_CONFIG_INTEGER(JitDoLoopInversion, "JitDoLoopInversion", 1) // Perform loop inversion on "for/while" loops
OPT_CONFIG_INTEGER(JitDoRangeAnalysis, "JitDoRangeAnalysis", 1) // Perform range check analysis
OPT_CONFIG_INTEGER(JitDoVNBasedDeadStoreRemoval, "JitDoVNBasedDeadStoreRemoval", 1) // Perform VN-based dead store
                                                                                    // removal
OPT_CONFIG_INTEGER(JitDoRedundantBranchOpts, "JitDoRedundantBranchOpts", 1) // Perform redundant branch optimizations
OPT_CONFIG_STRING(JitEnableRboRange, "JitEnableRboRange")
OPT_CONFIG_STRING(JitEnableHeadTailMergeRange, "JitEnableHeadTailMergeRange")
OPT_CONFIG_STRING(JitEnableVNBasedDeadStoreRemovalRange, "JitEnableVNBasedDeadStoreRemovalRange")
OPT_CONFIG_STRING(JitEnableEarlyLivenessRange, "JitEnableEarlyLivenessRange")
OPT_CONFIG_STRING(JitOnlyOptimizeRange,
                  "JitOnlyOptimizeRange") // If set, all methods that do _not_ match are forced into MinOpts
OPT_CONFIG_STRING(JitEnablePhysicalPromotionRange, "JitEnablePhysicalPromotionRange")
OPT_CONFIG_STRING(JitEnableCrossBlockLocalAssertionPropRange, "JitEnableCrossBlockLocalAssertionPropRange")
OPT_CONFIG_STRING(JitEnableInductionVariableOptsRange, "JitEnableInductionVariableOptsRange")
OPT_CONFIG_STRING(JitEnableLocalAddrPropagationRange, "JitEnableLocalAddrPropagationRange")

OPT_CONFIG_INTEGER(JitDoSsa, "JitDoSsa", 1) // Perform Static Single Assignment (SSA) numbering on the variables
OPT_CONFIG_INTEGER(JitDoValueNumber, "JitDoValueNumber", 1) // Perform value numbering on method expressions

OPT_CONFIG_STRING(JitOptRepeatRange, "JitOptRepeatRange") // Enable JitOptRepeat based on method hash range

OPT_CONFIG_INTEGER(JitDoIfConversion, "JitDoIfConversion", 1)                       // Perform If conversion
OPT_CONFIG_INTEGER(JitDoOptimizeMaskConversions, "JitDoOptimizeMaskConversions", 1) // Perform optimization of mask
                                                                                    // conversions

RELEASE_CONFIG_INTEGER(JitEnableOptRepeat, "JitEnableOptRepeat", 1) // If zero, do not allow JitOptRepeat
RELEASE_CONFIG_METHODSET(JitOptRepeat, "JitOptRepeat")            // Runs optimizer multiple times on specified methods
RELEASE_CONFIG_INTEGER(JitOptRepeatCount, "JitOptRepeatCount", 2) // Number of times to repeat opts when repeating

// Max # of MapSelect's considered for a particular top-level invocation.
RELEASE_CONFIG_INTEGER(JitVNMapSelBudget, "JitVNMapSelBudget", DEFAULT_MAP_SELECT_BUDGET)

RELEASE_CONFIG_INTEGER(TailCallLoopOpt, "TailCallLoopOpt", 1) // Convert recursive tail calls to loops
RELEASE_CONFIG_METHODSET(AltJit, "AltJit")         // Enables AltJit and selectively limits it to the specified methods.
RELEASE_CONFIG_METHODSET(AltJitNgen, "AltJitNgen") // Enables AltJit for NGEN and selectively limits it
                                                   // to the specified methods.

// Do not use AltJit on this semicolon-delimited list of assemblies.
RELEASE_CONFIG_STRING(AltJitExcludeAssemblies, "AltJitExcludeAssemblies")

// If set, measure the IR size after some phases and report it in the time log.
RELEASE_CONFIG_INTEGER(JitMeasureIR, "JitMeasureIR", 0)

// If set, gather JIT function info and write to this file.
RELEASE_CONFIG_STRING(JitFuncInfoFile, "JitFuncInfoLogFile")

// If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds.
RELEASE_CONFIG_STRING(JitTimeLogCsv, "JitTimeLogCsv")

// If set, gather JIT throughput data and write to this file.
RELEASE_CONFIG_STRING(JitTimeLogFile, "JitTimeLogFile")

RELEASE_CONFIG_STRING(TailCallOpt, "TailCallOpt")

// If set, allow fast tail calls; otherwise allow only helper-based calls for explicit tail calls.
RELEASE_CONFIG_INTEGER(FastTailCalls, "FastTailCalls", 1)

// Set to 1 to measure noway_assert usage. Only valid if MEASURE_NOWAY is defined.
RELEASE_CONFIG_INTEGER(JitMeasureNowayAssert, "JitMeasureNowayAssert", 0)

// Set to file to write noway_assert usage to a file (if not set: stdout). Only valid if MEASURE_NOWAY is defined.
RELEASE_CONFIG_STRING(JitMeasureNowayAssertFile, "JitMeasureNowayAssertFile")

CONFIG_INTEGER(EnableExtraSuperPmiQueries, "EnableExtraSuperPmiQueries", 0) // Make extra queries to somewhat
                                                                            // future-proof SuperPmi method contexts.

CONFIG_INTEGER(JitInlineDumpData, "JitInlineDumpData", 0)
CONFIG_INTEGER(JitInlineDumpXml, "JitInlineDumpXml", 0) // 1 = full xml (+ failures in DEBUG)
                                                        // 2 = only methods with inlines (+ failures in DEBUG)
                                                        // 3 = only methods with inlines, no failures
CONFIG_STRING(JitInlineDumpXmlFile, "JitInlineDumpXmlFile")
CONFIG_INTEGER(JitInlinePolicyDumpXml, "JitInlinePolicyDumpXml", 0)
CONFIG_INTEGER(JitInlineLimit, "JitInlineLimit", -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, "JitInlinePolicyDiscretionary", 0)
CONFIG_INTEGER(JitInlinePolicyFull, "JitInlinePolicyFull", 0)
CONFIG_INTEGER(JitInlinePolicySize, "JitInlinePolicySize", 0)
CONFIG_INTEGER(JitInlinePolicyRandom, "JitInlinePolicyRandom", 0) // nonzero enables; value is the external random
                                                                  // seed
CONFIG_INTEGER(JitInlinePolicyReplay, "JitInlinePolicyReplay", 0)
CONFIG_STRING(JitNoInlineRange, "JitNoInlineRange")
CONFIG_STRING(JitInlineReplayFile, "JitInlineReplayFile")

// Extended version of DefaultPolicy that includes a more precise IL scan,
// relies on PGO if it exists and generally is more aggressive.
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicy, "JitExtDefaultPolicy", 1)
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicyMaxIL, "JitExtDefaultPolicyMaxIL", 0x80)
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicyMaxILProf, "JitExtDefaultPolicyMaxILProf", 0x400)
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicyMaxBB, "JitExtDefaultPolicyMaxBB", 7)

// Inliner uses the following formula for PGO-driven decisions:
//
//    BM = BM * ((1.0 - ProfTrust) + ProfWeight * ProfScale)
//
// Where BM is a benefit multiplier composed from various observations (e.g. "const arg makes a branch foldable").
// If a profile data can be trusted for 100% we can safely just give up on inlining anything inside cold blocks
// (except the cases where inlining in cold blocks improves type info/escape analysis for the whole caller).
// For now, it's only applied for dynamic PGO.
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicyProfTrust, "JitExtDefaultPolicyProfTrust", 0x7)
RELEASE_CONFIG_INTEGER(JitExtDefaultPolicyProfScale, "JitExtDefaultPolicyProfScale", 0x2A)

RELEASE_CONFIG_INTEGER(JitInlinePolicyModel, "JitInlinePolicyModel", 0)
RELEASE_CONFIG_INTEGER(JitInlinePolicyProfile, "JitInlinePolicyProfile", 0)
RELEASE_CONFIG_INTEGER(JitInlinePolicyProfileThreshold, "JitInlinePolicyProfileThreshold", 40)
CONFIG_STRING(JitObjectStackAllocationRange, "JitObjectStackAllocationRange")
RELEASE_CONFIG_INTEGER(JitObjectStackAllocation, "JitObjectStackAllocation", 1)
RELEASE_CONFIG_INTEGER(JitObjectStackAllocationRefClass, "JitObjectStackAllocationRefClass", 1)
RELEASE_CONFIG_INTEGER(JitObjectStackAllocationBoxedValueClass, "JitObjectStackAllocationBoxedValueClass", 1)
RELEASE_CONFIG_INTEGER(JitObjectStackAllocationConditionalEscape, "JitObjectStackAllocationConditionalEscape", 1)
CONFIG_STRING(JitObjectStackAllocationConditionalEscapeRange, "JitObjectStackAllocationConditionalEscapeRange")
RELEASE_CONFIG_INTEGER(JitObjectStackAllocationArray, "JitObjectStackAllocationArray", 1)
RELEASE_CONFIG_INTEGER(JitObjectStackAllocationSize, "JitObjectStackAllocationSize", 528)

RELEASE_CONFIG_INTEGER(JitEECallTimingInfo, "JitEECallTimingInfo", 0)

CONFIG_INTEGER(JitEnableFinallyCloning, "JitEnableFinallyCloning", 1)
CONFIG_INTEGER(JitEnableRemoveEmptyTry, "JitEnableRemoveEmptyTry", 1)
CONFIG_INTEGER(JitEnableRemoveEmptyTryCatchOrTryFault, "JitEnableRemoveEmptyTryCatchOrTryFault", 1)

// Overall master enable for Guarded Devirtualization.
RELEASE_CONFIG_INTEGER(JitEnableGuardedDevirtualization, "JitEnableGuardedDevirtualization", 1)

#define MAX_GDV_TYPE_CHECKS 5
// Number of types to probe for polymorphic virtual call-sites to devirtualize them,
// Max number is MAX_GDV_TYPE_CHECKS defined above ^. -1 means it's up to JIT to decide
RELEASE_CONFIG_INTEGER(JitGuardedDevirtualizationMaxTypeChecks, "JitGuardedDevirtualizationMaxTypeChecks", -1)

// Various policies for GuardedDevirtualization (0x4B == 75)
RELEASE_CONFIG_INTEGER(JitGuardedDevirtualizationChainLikelihood, "JitGuardedDevirtualizationChainLikelihood", 0x4B)
RELEASE_CONFIG_INTEGER(JitGuardedDevirtualizationChainStatements, "JitGuardedDevirtualizationChainStatements", 1)
CONFIG_STRING(JitGuardedDevirtualizationRange, "JitGuardedDevirtualizationRange")
CONFIG_INTEGER(JitRandomGuardedDevirtualization, "JitRandomGuardedDevirtualization", 0)

// Enable insertion of patchpoints into Tier0 methods, switching to optimized where needed.
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
RELEASE_CONFIG_INTEGER(TC_OnStackReplacement, "TC_OnStackReplacement", 1)
#else
RELEASE_CONFIG_INTEGER(TC_OnStackReplacement, "TC_OnStackReplacement", 0)
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

// Initial patchpoint counter value used by jitted code
RELEASE_CONFIG_INTEGER(TC_OnStackReplacement_InitialCounter, "TC_OnStackReplacement_InitialCounter", 1000)

// Enable partial compilation for Tier0 methods
RELEASE_CONFIG_INTEGER(TC_PartialCompilation, "TC_PartialCompilation", 0)

// If partial compilation is enabled, use random heuristic for patchpoint placement
CONFIG_INTEGER(JitRandomPartialCompilation, "JitRandomPartialCompilation", 0)

// Patchpoint strategy:
// 0 - backedge sources
// 1 - backedge targets
// 2 - adaptive (default)
RELEASE_CONFIG_INTEGER(TC_PatchpointStrategy, "TC_PatchpointStrategy", 2)

// Randomly sprinkle patchpoints. Value is the likelihood any given stack-empty point becomes a patchpoint.
CONFIG_INTEGER(JitRandomOnStackReplacement, "JitRandomOnStackReplacement", 0)

// Place patchpoint at the specified IL offset, if possible. Overrides random placement.
CONFIG_INTEGER(JitOffsetOnStackReplacement, "JitOffsetOnStackReplacement", -1)

// EnableOsrRange allows you to limit the set of methods that will rely on OSR to escape
// from Tier0 code. Methods outside the range that would normally be jitted at Tier0
// and have patchpoints will instead be switched to optimized.
CONFIG_STRING(JitEnableOsrRange, "JitEnableOsrRange")

// EnablePatchpointRange allows you to limit the set of Tier0 methods that
// will have patchpoints, and hence control which methods will create OSR methods.
// Unlike EnableOsrRange, it will not alter the optimization setting for methods
// outside the enabled range.
CONFIG_STRING(JitEnablePatchpointRange, "JitEnablePatchpointRange")

// Profile instrumentation options
RELEASE_CONFIG_INTEGER(JitInterlockedProfiling, "JitInterlockedProfiling", 0)
RELEASE_CONFIG_INTEGER(JitScalableProfiling, "JitScalableProfiling", 1)
RELEASE_CONFIG_INTEGER(JitCounterPadding, "JitCounterPadding", 0) // number of unused extra slots per counter
RELEASE_CONFIG_INTEGER(JitMinimalJitProfiling, "JitMinimalJitProfiling", 1)
RELEASE_CONFIG_INTEGER(JitMinimalPrejitProfiling, "JitMinimalPrejitProfiling", 0)

RELEASE_CONFIG_INTEGER(JitProfileValues, "JitProfileValues", 1) // Value profiling, e.g. Buffer.Memmove's size
RELEASE_CONFIG_INTEGER(JitProfileCasts, "JitProfileCasts", 1)   // Profile castclass/isinst
RELEASE_CONFIG_INTEGER(JitConsumeProfileForCasts, "JitConsumeProfileForCasts", 1) // Consume profile data (if any)
                                                                                  // for castclass/isinst

RELEASE_CONFIG_INTEGER(JitClassProfiling, "JitClassProfiling", 1)         // Profile virtual and interface calls
RELEASE_CONFIG_INTEGER(JitDelegateProfiling, "JitDelegateProfiling", 1)   // Profile resolved delegate call targets
RELEASE_CONFIG_INTEGER(JitVTableProfiling, "JitVTableProfiling", 0)       // Profile resolved vtable call targets
RELEASE_CONFIG_INTEGER(JitEdgeProfiling, "JitEdgeProfiling", 1)           // Profile edges instead of blocks
RELEASE_CONFIG_INTEGER(JitCollect64BitCounts, "JitCollect64BitCounts", 0) // Collect counts as 64-bit values.

// Profile consumption options
RELEASE_CONFIG_INTEGER(JitDisablePGO, "JitDisablePGO", 0)     // Ignore PGO data for all methods
CONFIG_STRING(JitEnablePGORange, "JitEnablePGORange")         // Enable PGO data for only some methods
CONFIG_INTEGER(JitRandomEdgeCounts, "JitRandomEdgeCounts", 0) // Substitute random values for edge counts
CONFIG_INTEGER(JitCrossCheckDevirtualizationAndPGO, "JitCrossCheckDevirtualizationAndPGO", 0)
CONFIG_INTEGER(JitNoteFailedExactDevirtualization, "JitNoteFailedExactDevirtualization", 0)
CONFIG_INTEGER(JitRandomlyCollect64BitCounts, "JitRandomlyCollect64BitCounts", 0) // Collect 64-bit counts randomly
                                                                                  // for some methods.

// 1: profile synthesis for root methods
// 2: profile synthesis for root methods w/o PGO data
// 3: profile synthesis for root methods, blend with existing PGO data
CONFIG_INTEGER(JitSynthesizeCounts, "JitSynthesizeCounts", 0)

// If instrumenting the method, run synthesis and save the synthesis results
// as edge or block profile data. Do not actually instrument.
CONFIG_INTEGER(JitPropagateSynthesizedCountsToProfileData, "JitPropagateSynthesizedCountsToProfileData", 0)

// Use general (gauss-seidel) solver
CONFIG_INTEGER(JitSynthesisUseSolver, "JitSynthesisUseSolver", 1)

// Weight for exception regions for synthesis
CONFIG_STRING(JitSynthesisExceptionWeight, "JitSynthesisExceptionWeight")

// Devirtualize virtual calls with getExactClasses (NativeAOT only for now)
RELEASE_CONFIG_INTEGER(JitEnableExactDevirtualization, "JitEnableExactDevirtualization", 1)

// Force the generation of CFG checks
RELEASE_CONFIG_INTEGER(JitForceControlFlowGuard, "JitForceControlFlowGuard", 0);

// JitCFGUseDispatcher values:
// 0: Never use dispatcher
// 1: Use dispatcher on all platforms that support it
// 2: Default behavior, depends on platform (yes on x64, no on arm64)
RELEASE_CONFIG_INTEGER(JitCFGUseDispatcher, "JitCFGUseDispatcher", 2)

// Enable head and tail merging
RELEASE_CONFIG_INTEGER(JitEnableHeadTailMerge, "JitEnableHeadTailMerge", 1)

// Enable physical promotion
RELEASE_CONFIG_INTEGER(JitEnablePhysicalPromotion, "JitEnablePhysicalPromotion", 1)

// Enable cross-block local assertion prop
RELEASE_CONFIG_INTEGER(JitEnableCrossBlockLocalAssertionProp, "JitEnableCrossBlockLocalAssertionProp", 1)

// Do greedy RPO-based layout in Compiler::fgReorderBlocks.
RELEASE_CONFIG_INTEGER(JitDoReversePostOrderLayout, "JitDoReversePostOrderLayout", 1);

// Enable strength reduction
RELEASE_CONFIG_INTEGER(JitEnableStrengthReduction, "JitEnableStrengthReduction", 1)

// Enable IV optimizations
RELEASE_CONFIG_INTEGER(JitEnableInductionVariableOpts, "JitEnableInductionVariableOpts", 1)

// JitFunctionFile: Name of a file that contains a list of functions. If the currently compiled function is in the
// file, certain other JIT config variables will be active. If the currently compiled function is not in the file,
// the specific JIT config variables will not be active.
//
// Functions are approximately in the format output by JitFunctionTrace, e.g.:
//
// System.CLRConfig:GetBoolValue(ref,byref):bool (MethodHash=3c54d35e)
//   -- use the MethodHash, not the function name
//
// System.CLRConfig:GetBoolValue(ref,byref):bool
//   -- use just the name
//
// Lines with leading ";" "#" or "//" are ignored.
//
// If this is unset, then the JIT config values have their normal behavior.
//
CONFIG_STRING(JitFunctionFile, "JitFunctionFile")

CONFIG_METHODSET(JitRawHexCode, "JitRawHexCode")
CONFIG_STRING(JitRawHexCodeFile, "JitRawHexCodeFile")

#if defined(TARGET_ARM64)
// JitSaveFpLrWithCalleeSavedRegisters:
//    0: use default frame type decision
//    1: disable frames that save FP/LR registers with the callee-saved registers (at the top of the frame)
//    2: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame)
//    3: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame) and also force using the large funclet frame variation (frame 5) if possible.
CONFIG_INTEGER(JitSaveFpLrWithCalleeSavedRegisters, "JitSaveFpLrWithCalleeSavedRegisters", 0)
#endif // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
// Disable emitDispIns by default
CONFIG_INTEGER(JitDispIns, "JitDispIns", 0)
#endif // defined(TARGET_LOONGARCH64)

// Allow to enregister locals with struct type.
RELEASE_CONFIG_INTEGER(JitEnregStructLocals, "JitEnregStructLocals", 1)

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
#undef RELEASE_CONFIG_INTEGER
#undef RELEASE_CONFIG_STRING
#undef RELEASE_CONFIG_METHODSET
#undef OPT_CONFIG_INTEGER
#undef OPT_CONFIG_STRING
#undef OPT_CONFIG_METHODSET
