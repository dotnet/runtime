// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)
#error CONFIG_INTEGER, CONFIG_STRING, and CONFIG_METHODSET must be defined before including this file.
#endif // !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)

#ifdef DEBUG
#define OPT_CONFIG // Enable optimization level configuration.
#endif

#if defined(DEBUG)

///
/// JIT
///
CONFIG_INTEGER(AltJitLimit, u"AltJitLimit", 0)               // Max number of functions to use altjit for (decimal)
CONFIG_INTEGER(AltJitSkipOnAssert, u"AltJitSkipOnAssert", 0) // If AltJit hits an assert, fall back to the fallback
                                                               // JIT. Useful in conjunction with
                                                               // COMPlus_ContinueOnAssert=1
CONFIG_INTEGER(BreakOnDumpToken, u"BreakOnDumpToken", 0xffffffff) // Breaks when using internal logging on a
                                                                    // particular token value.
CONFIG_INTEGER(DebugBreakOnVerificationFailure, u"DebugBreakOnVerificationFailure", 0) // Halts the jit on
                                                                                         // verification failure
CONFIG_INTEGER(DiffableDasm, u"JitDiffableDasm", 0)          // Make the disassembly diff-able
CONFIG_INTEGER(JitDasmWithAddress, u"JitDasmWithAddress", 0) // Print the process address next to each instruction of
                                                               // the disassembly
CONFIG_INTEGER(DisplayLoopHoistStats, u"JitLoopHoistStats", 0) // Display JIT loop hoisting statistics
CONFIG_INTEGER(DisplayLsraStats, u"JitLsraStats", 0) // Display JIT Linear Scan Register Allocator statistics
                                                       // if set to 1. If set to "2", display the stats in csv format.
                                                       // Recommended to use with JitStdOutFile flag.
CONFIG_STRING(JitLsraOrdering, u"JitLsraOrdering")   // LSRA heuristics ordering
CONFIG_INTEGER(DumpJittedMethods, u"DumpJittedMethods", 0) // Prints all jitted methods to the console
CONFIG_INTEGER(EnablePCRelAddr, u"JitEnablePCRelAddr", 1)  // Whether absolute addr be encoded as PC-rel offset by
                                                             // RyuJIT where possible
CONFIG_INTEGER(JitAssertOnMaxRAPasses, u"JitAssertOnMaxRAPasses", 0)
CONFIG_INTEGER(JitBreakEmitOutputInstr, u"JitBreakEmitOutputInstr", -1)
CONFIG_INTEGER(JitBreakMorphTree, u"JitBreakMorphTree", 0xffffffff)
CONFIG_INTEGER(JitBreakOnBadCode, u"JitBreakOnBadCode", 0)
CONFIG_INTEGER(JitBreakOnMinOpts, u"JITBreakOnMinOpts", 0) // Halt if jit switches to MinOpts
CONFIG_INTEGER(JitBreakOnUnsafeCode, u"JitBreakOnUnsafeCode", 0)
CONFIG_INTEGER(JitCanUseSSE2, u"JitCanUseSSE2", -1)
CONFIG_INTEGER(JitCloneLoops, u"JitCloneLoops", 1) // If 0, don't clone. Otherwise clone loops for optimizations.
CONFIG_INTEGER(JitDebugLogLoopCloning, u"JitDebugLogLoopCloning", 0) // In debug builds log places where loop cloning
                                                                       // optimizations are performed on the fast path.
CONFIG_INTEGER(JitDefaultFill, u"JitDefaultFill", 0xdd) // In debug builds, initialize the memory allocated by the nra
                                                          // with this byte.
CONFIG_INTEGER(JitAlignLoopMinBlockWeight,
               u"JitAlignLoopMinBlockWeight",
               DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT) // Minimum weight needed for the first block of a loop to make it a
                                                    // candidate for alignment.
CONFIG_INTEGER(JitAlignLoopMaxCodeSize,
               u"JitAlignLoopMaxCodeSize",
               DEFAULT_MAX_LOOPSIZE_FOR_ALIGN) // For non-adaptive alignment, minimum loop size (in bytes) for which
                                               // alignment will be done.
                                               // Defaults to 3 blocks of 32 bytes chunks = 96 bytes.
CONFIG_INTEGER(JitAlignLoopBoundary,
               u"JitAlignLoopBoundary",
               DEFAULT_ALIGN_LOOP_BOUNDARY) // For non-adaptive alignment, address boundary (power of 2) at which loop
                                            // alignment should be done. By default, 32B.
CONFIG_INTEGER(JitAlignLoopForJcc,
               u"JitAlignLoopForJcc",
               0) // If set, for non-adaptive alignment, ensure loop jmps are not on or cross alignment boundary.

CONFIG_INTEGER(JitAlignLoopAdaptive,
               u"JitAlignLoopAdaptive",
               1) // If set, perform adaptive loop alignment that limits number of padding based on loop size.

CONFIG_INTEGER(JitDirectAlloc, u"JitDirectAlloc", 0)
CONFIG_INTEGER(JitDoubleAlign, u"JitDoubleAlign", 1)
CONFIG_INTEGER(JitDumpASCII, u"JitDumpASCII", 1)               // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpTerseLsra, u"JitDumpTerseLsra", 1)       // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, u"JitDumpToDebugger", 0)     // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, u"JitDumpVerboseSsa", 0)     // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, u"JitDumpVerboseTrees", 0) // Enable more verbose tree dumps
CONFIG_INTEGER(JitEmitPrintRefRegs, u"JitEmitPrintRefRegs", 0)
CONFIG_INTEGER(JitEnableDevirtualization, u"JitEnableDevirtualization", 1) // Enable devirtualization in importer
CONFIG_INTEGER(JitEnableLateDevirtualization, u"JitEnableLateDevirtualization", 1) // Enable devirtualization after
                                                                                     // inlining
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, u"JitExpensiveDebugCheckLevel", 0) // Level indicates how much checking
                                                                                 // beyond the default to do in debug
                                                                                 // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, u"JitForceFallback", 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitForceVer, u"JitForceVer", 0)
CONFIG_INTEGER(JitFullyInt, u"JitFullyInt", 0)           // Forces Fully interruptible code
CONFIG_INTEGER(JitFunctionTrace, u"JitFunctionTrace", 0) // If non-zero, print JIT start/end logging
CONFIG_INTEGER(JitGCChecks, u"JitGCChecks", 0)
CONFIG_INTEGER(JitGCInfoLogging, u"JitGCInfoLogging", 0) // If true, prints GCInfo-related output to standard output.
CONFIG_INTEGER(JitHashBreak, u"JitHashBreak", -1)        // Same as JitBreak, but for a method hash
CONFIG_INTEGER(JitHashDump, u"JitHashDump", -1)          // Same as JitDump, but for a method hash
CONFIG_INTEGER(JitHashHalt, u"JitHashHalt", -1)          // Same as JitHalt, but for a method hash
CONFIG_INTEGER(JitInlineAdditionalMultiplier, u"JitInlineAdditionalMultiplier", 0)
CONFIG_INTEGER(JitInlinePrintStats, u"JitInlinePrintStats", 0)
CONFIG_INTEGER(JitInlineSize, u"JITInlineSize", DEFAULT_MAX_INLINE_SIZE)
CONFIG_INTEGER(JitInlineDepth, u"JITInlineDepth", DEFAULT_MAX_INLINE_DEPTH)
CONFIG_INTEGER(JitLongAddress, u"JitLongAddress", 0) // Force using the large pseudo instruction form for long address
CONFIG_INTEGER(JitMaxTempAssert, u"JITMaxTempAssert", 1)
CONFIG_INTEGER(JitMaxUncheckedOffset, u"JitMaxUncheckedOffset", 8)
CONFIG_INTEGER(JitMinOpts, u"JITMinOpts", 0)                                       // Forces MinOpts
CONFIG_INTEGER(JitMinOptsBbCount, u"JITMinOptsBbCount", DEFAULT_MIN_OPTS_BB_COUNT) // Internal jit control of MinOpts
CONFIG_INTEGER(JitMinOptsCodeSize, u"JITMinOptsCodeSize", DEFAULT_MIN_OPTS_CODE_SIZE)       // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsInstrCount, u"JITMinOptsInstrCount", DEFAULT_MIN_OPTS_INSTR_COUNT) // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsLvNumCount, u"JITMinOptsLvNumcount", DEFAULT_MIN_OPTS_LV_NUM_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitMinOptsLvRefCount, u"JITMinOptsLvRefcount", DEFAULT_MIN_OPTS_LV_REF_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitNoCMOV, u"JitNoCMOV", 0)
CONFIG_INTEGER(JitNoCSE, u"JitNoCSE", 0)
CONFIG_INTEGER(JitNoCSE2, u"JitNoCSE2", 0)
CONFIG_INTEGER(JitNoForceFallback, u"JitNoForceFallback", 0) // Set to non-zero to prevent NOWAY assert testing.
                                                               // Overrides COMPlus_JitForceFallback and JIT stress
                                                               // flags.
CONFIG_INTEGER(JitNoHoist, u"JitNoHoist", 0)
CONFIG_INTEGER(JitNoInline, u"JitNoInline", 0)                 // Disables inlining of all methods
CONFIG_INTEGER(JitNoMemoryBarriers, u"JitNoMemoryBarriers", 0) // If 1, don't generate memory barriers
CONFIG_INTEGER(JitNoRegLoc, u"JitNoRegLoc", 0)
CONFIG_INTEGER(JitNoStructPromotion, u"JitNoStructPromotion", 0) // Disables struct promotion in Jit32
CONFIG_INTEGER(JitNoUnroll, u"JitNoUnroll", 0)
CONFIG_INTEGER(JitOrder, u"JitOrder", 0)
CONFIG_INTEGER(JitQueryCurrentStaticFieldClass, u"JitQueryCurrentStaticFieldClass", 1)
CONFIG_INTEGER(JitReportFastTailCallDecisions, u"JitReportFastTailCallDecisions", 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, u"JITPInvokeCheckEnabled", 0)
CONFIG_INTEGER(JitPInvokeEnabled, u"JITPInvokeEnabled", 1)
CONFIG_METHODSET(JitPrintInlinedMethods, u"JitPrintInlinedMethods")
CONFIG_METHODSET(JitPrintDevirtualizedMethods, u"JitPrintDevirtualizedMethods")
CONFIG_INTEGER(JitProfileChecks, u"JitProfileChecks", 0) // 1 enable in dumps, 2 assert if issues found
CONFIG_INTEGER(JitRequired, u"JITRequired", -1)
CONFIG_INTEGER(JitRoundFloat, u"JITRoundFloat", DEFAULT_ROUND_LEVEL)
CONFIG_INTEGER(JitStackAllocToLocalSize, u"JitStackAllocToLocalSize", DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE)
CONFIG_INTEGER(JitSkipArrayBoundCheck, u"JitSkipArrayBoundCheck", 0)
CONFIG_INTEGER(JitSlowDebugChecksEnabled, u"JitSlowDebugChecksEnabled", 1) // Turn on slow debug checks
CONFIG_INTEGER(JitSplitFunctionSize, u"JitSplitFunctionSize", 0) // On ARM, use this as the maximum function/funclet
                                                                   // size for creating function fragments (and creating
                                                                   // multiple RUNTIME_FUNCTION entries)
CONFIG_INTEGER(JitSsaStress, u"JitSsaStress", 0) // Perturb order of processing of blocks in SSA; 0 = no stress; 1 =
                                                   // use method hash; * = supplied value as random hash
CONFIG_INTEGER(JitStackChecks, u"JitStackChecks", 0)
CONFIG_STRING(JitStdOutFile, u"JitStdOutFile") // If set, sends JIT's stdout output to this file.
CONFIG_INTEGER(JitStress, u"JitStress", 0) // Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary
                                             // stress based on a hash of the method and this value
CONFIG_INTEGER(JitStressBBProf, u"JitStressBBProf", 0)               // Internal Jit stress mode
CONFIG_INTEGER(JitStressBiasedCSE, u"JitStressBiasedCSE", 0x101)     // Internal Jit stress mode: decimal bias value
                                                                       // between (0,100) to perform CSE on a candidate.
                                                                       // 100% = All CSEs. 0% = 0 CSE. (> 100) means no
                                                                       // stress.
CONFIG_INTEGER(JitStressFP, u"JitStressFP", 0)                       // Internal Jit stress mode
CONFIG_INTEGER(JitStressModeNamesOnly, u"JitStressModeNamesOnly", 0) // Internal Jit stress: if nonzero, only enable
                                                                       // stress modes listed in JitStressModeNames
CONFIG_INTEGER(JitStressRegs, u"JitStressRegs", 0)
CONFIG_INTEGER(JitVNMapSelLimit, u"JitVNMapSelLimit", 0) // If non-zero, assert if # of VNF_MapSelect applications
                                                           // considered reaches this
CONFIG_INTEGER(NgenHashDump, u"NgenHashDump", -1)        // same as JitHashDump, but for ngen
CONFIG_INTEGER(NgenOrder, u"NgenOrder", 0)
CONFIG_INTEGER(RunAltJitCode, u"RunAltJitCode", 1) // If non-zero, and the compilation succeeds for an AltJit, then
                                                     // use the code. If zero, then we always throw away the generated
                                                     // code and fall back to the default compiler.
CONFIG_INTEGER(RunComponentUnitTests, u"JitComponentUnitTests", 0) // Run JIT component unit tests
CONFIG_INTEGER(ShouldInjectFault, u"InjectFault", 0)
CONFIG_INTEGER(StressCOMCall, u"StressCOMCall", 0)
CONFIG_INTEGER(TailcallStress, u"TailcallStress", 0)
CONFIG_INTEGER(TreesBeforeAfterMorph, u"JitDumpBeforeAfterMorph", 0) // If 1, display each tree before/after morphing

CONFIG_METHODSET(JitBreak, u"JitBreak") // Stops in the importer when compiling a specified method
CONFIG_METHODSET(JitDebugBreak, u"JitDebugBreak")
CONFIG_METHODSET(JitDisasm, u"JitDisasm")                  // Dumps disassembly for specified method
CONFIG_STRING(JitDisasmAssemblies, u"JitDisasmAssemblies") // Only show JitDisasm and related info for methods
                                                             // from this semicolon-delimited list of assemblies.
CONFIG_INTEGER(JitDisasmWithGC, u"JitDisasmWithGC", 0)     // Dump interleaved GC Info for any method disassembled.
CONFIG_METHODSET(JitDump, u"JitDump")                      // Dumps trees for specified method
CONFIG_METHODSET(JitEHDump, u"JitEHDump")                  // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(JitExclude, u"JitExclude")
CONFIG_METHODSET(JitForceProcedureSplitting, u"JitForceProcedureSplitting")
CONFIG_METHODSET(JitGCDump, u"JitGCDump")
CONFIG_METHODSET(JitDebugDump, u"JitDebugDump")
CONFIG_METHODSET(JitHalt, u"JitHalt") // Emits break instruction into jitted code
CONFIG_METHODSET(JitImportBreak, u"JitImportBreak")
CONFIG_METHODSET(JitInclude, u"JitInclude")
CONFIG_METHODSET(JitLateDisasm, u"JitLateDisasm")
CONFIG_METHODSET(JitMinOptsName, u"JITMinOptsName")                   // Forces MinOpts for a named function
CONFIG_METHODSET(JitNoProcedureSplitting, u"JitNoProcedureSplitting") // Disallow procedure splitting for specified
                                                                        // methods
CONFIG_METHODSET(JitNoProcedureSplittingEH, u"JitNoProcedureSplittingEH") // Disallow procedure splitting for
                                                                            // specified methods if they contain
                                                                            // exception handling
CONFIG_METHODSET(JitStressOnly, u"JitStressOnly") // Internal Jit stress mode: stress only the specified method(s)
CONFIG_METHODSET(JitUnwindDump, u"JitUnwindDump") // Dump the unwind codes for the method
///
/// NGEN
///
CONFIG_METHODSET(NgenDisasm, u"NgenDisasm") // Same as JitDisasm, but for ngen
CONFIG_METHODSET(NgenDump, u"NgenDump")     // Same as JitDump, but for ngen
CONFIG_METHODSET(NgenEHDump, u"NgenEHDump") // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(NgenGCDump, u"NgenGCDump")
CONFIG_METHODSET(NgenDebugDump, u"NgenDebugDump")
CONFIG_METHODSET(NgenUnwindDump, u"NgenUnwindDump") // Dump the unwind codes for the method
///
/// JIT
///
CONFIG_METHODSET(JitDumpFg, u"JitDumpFg")        // Dumps Xml/Dot Flowgraph for specified method
CONFIG_STRING(JitDumpFgDir, u"JitDumpFgDir")     // Directory for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgFile, u"JitDumpFgFile")   // Filename for Xml/Dot flowgraph dump(s) (default: "default")
CONFIG_STRING(JitDumpFgPhase, u"JitDumpFgPhase") // Phase-based Xml/Dot flowgraph support. Set to the short name of a
                                                   // phase to see the flowgraph after that phase. Leave unset to dump
                                                   // after COLD-BLK (determine first cold block) or set to * for all
                                                   // phases

CONFIG_STRING(JitDumpFgPrePhase,
              u"JitDumpFgPrePhase") // Same as JitDumpFgPhase, but specifies to dump pre-phase, not post-phase.
CONFIG_INTEGER(JitDumpFgDot, u"JitDumpFgDot", 1)     // 0 == dump XML format; non-zero == dump DOT format
CONFIG_INTEGER(JitDumpFgEH, u"JitDumpFgEH", 0)       // 0 == no EH regions; non-zero == include EH regions
CONFIG_INTEGER(JitDumpFgLoops, u"JitDumpFgLoops", 0) // 0 == no loop regions; non-zero == include loop regions

CONFIG_INTEGER(JitDumpFgConstrained, u"JitDumpFgConstrained", 1) // 0 == don't constrain to mostly linear layout;
                                                                   // non-zero == force mostly lexical block
                                                                   // linear layout
CONFIG_INTEGER(JitDumpFgBlockID, u"JitDumpFgBlockID", 0) // 0 == display block with bbNum; 1 == display with both
                                                           // bbNum and bbID

CONFIG_STRING(JitLateDisasmTo, u"JITLateDisasmTo")
CONFIG_STRING(JitRange, u"JitRange")
CONFIG_STRING(JitStressModeNames, u"JitStressModeNames") // Internal Jit stress mode: stress using the given set of
                                                           // stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, u"JitStressModeNamesNot") // Internal Jit stress mode: do NOT stress using the
                                                                 // given set of stress mode names, e.g. STRESS_REGS,
                                                                 // STRESS_TAILCALL
CONFIG_STRING(JitStressRange, u"JitStressRange")               // Internal Jit stress mode
///
/// NGEN
///
CONFIG_METHODSET(NgenDumpFg, u"NgenDumpFg")      // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgDir, u"NgenDumpFgDir")   // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgFile, u"NgenDumpFgFile") // Ngen Xml/Dot flowgraph dump support
///
/// JIT Hardware Intrinsics
///
CONFIG_INTEGER(EnableIncompleteISAClass, u"EnableIncompleteISAClass", 0) // Enable testing not-yet-implemented
                                                                           // intrinsic classes

#endif // defined(DEBUG)

#if FEATURE_LOOP_ALIGN
CONFIG_INTEGER(JitAlignLoops, u"JitAlignLoops", 1) // If set, align inner loops
#else
CONFIG_INTEGER(JitAlignLoops, u"JitAlignLoops", 0)
#endif

///
/// JIT
///
#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
CONFIG_INTEGER(JitNoRangeChks, u"JitNoRngChks", 0) // If 1, don't generate range checks
#endif

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate
// fallback to main JIT on hitting a NYI.
#if defined(TARGET_ARM64) || defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, u"AltJitAssertOnNYI", 0) // Controls the AltJit behavior of NYI stuff
#else                                                        // !defined(TARGET_ARM64) && !defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, u"AltJitAssertOnNYI", 1) // Controls the AltJit behavior of NYI stuff
#endif                                                       // defined(TARGET_ARM64) || defined(TARGET_X86)
///
/// JIT Hardware Intrinsics
///
#if defined(TARGET_X86) || defined(TARGET_AMD64)
CONFIG_INTEGER(EnableSSE3_4, u"EnableSSE3_4", 1) // Enable SSE3, SSSE3, SSE 4.1 and 4.2 instruction set as default
#endif

#if defined(TARGET_AMD64) || defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default. When both AVX and SSE3_4 are set, we will use the most
// capable instruction set available which will prefer AVX over SSE3/4.
CONFIG_INTEGER(EnableHWIntrinsic, u"EnableHWIntrinsic", 1) // Enable Base
CONFIG_INTEGER(EnableSSE, u"EnableSSE", 1)                 // Enable SSE
CONFIG_INTEGER(EnableSSE2, u"EnableSSE2", 1)               // Enable SSE2
CONFIG_INTEGER(EnableSSE3, u"EnableSSE3", 1)               // Enable SSE3
CONFIG_INTEGER(EnableSSSE3, u"EnableSSSE3", 1)             // Enable SSSE3
CONFIG_INTEGER(EnableSSE41, u"EnableSSE41", 1)             // Enable SSE41
CONFIG_INTEGER(EnableSSE42, u"EnableSSE42", 1)             // Enable SSE42
CONFIG_INTEGER(EnableAVX, u"EnableAVX", 1)                 // Enable AVX
CONFIG_INTEGER(EnableAVX2, u"EnableAVX2", 1)               // Enable AVX2
CONFIG_INTEGER(EnableFMA, u"EnableFMA", 1)                 // Enable FMA
CONFIG_INTEGER(EnableAES, u"EnableAES", 1)                 // Enable AES
CONFIG_INTEGER(EnableBMI1, u"EnableBMI1", 1)               // Enable BMI1
CONFIG_INTEGER(EnableBMI2, u"EnableBMI2", 1)               // Enable BMI2
CONFIG_INTEGER(EnableLZCNT, u"EnableLZCNT", 1)             // Enable AES
CONFIG_INTEGER(EnablePCLMULQDQ, u"EnablePCLMULQDQ", 1)     // Enable PCLMULQDQ
CONFIG_INTEGER(EnablePOPCNT, u"EnablePOPCNT", 1)           // Enable POPCNT
#else                                                        // !defined(TARGET_AMD64) && !defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default
CONFIG_INTEGER(EnableAVX, u"EnableAVX", 0)
#endif                                                       // !defined(TARGET_AMD64) && !defined(TARGET_X86)

CONFIG_INTEGER(EnableEHWriteThru, u"EnableEHWriteThru", 1) // Enable the register allocator to support EH-write thru:
                                                             // partial enregistration of vars exposed on EH boundaries
CONFIG_INTEGER(EnableMultiRegLocals, u"EnableMultiRegLocals", 1) // Enable the enregistration of locals that are
                                                                   // defined or used in a multireg context.

// clang-format off

#if defined(TARGET_ARM64)
CONFIG_INTEGER(EnableHWIntrinsic,       u"EnableHWIntrinsic", 1)
CONFIG_INTEGER(EnableArm64Aes,          u"EnableArm64Aes", 1)
CONFIG_INTEGER(EnableArm64Atomics,      u"EnableArm64Atomics", 1)
CONFIG_INTEGER(EnableArm64Crc32,        u"EnableArm64Crc32", 1)
CONFIG_INTEGER(EnableArm64Dcpop,        u"EnableArm64Dcpop", 1)
CONFIG_INTEGER(EnableArm64Dp,           u"EnableArm64Dp", 1)
CONFIG_INTEGER(EnableArm64Fcma,         u"EnableArm64Fcma", 1)
CONFIG_INTEGER(EnableArm64Fp,           u"EnableArm64Fp", 1)
CONFIG_INTEGER(EnableArm64Fp16,         u"EnableArm64Fp16", 1)
CONFIG_INTEGER(EnableArm64Jscvt,        u"EnableArm64Jscvt", 1)
CONFIG_INTEGER(EnableArm64Lrcpc,        u"EnableArm64Lrcpc", 1)
CONFIG_INTEGER(EnableArm64Pmull,        u"EnableArm64Pmull", 1)
CONFIG_INTEGER(EnableArm64Sha1,         u"EnableArm64Sha1", 1)
CONFIG_INTEGER(EnableArm64Sha256,       u"EnableArm64Sha256", 1)
CONFIG_INTEGER(EnableArm64Sha512,       u"EnableArm64Sha512", 1)
CONFIG_INTEGER(EnableArm64Sha3,         u"EnableArm64Sha3", 1)
CONFIG_INTEGER(EnableArm64AdvSimd,      u"EnableArm64AdvSimd", 1)
CONFIG_INTEGER(EnableArm64AdvSimd_v81,  u"EnableArm64AdvSimd_v81", 1)
CONFIG_INTEGER(EnableArm64AdvSimd_Fp16, u"EnableArm64AdvSimd_Fp16", 1)
CONFIG_INTEGER(EnableArm64Sm3,          u"EnableArm64Sm3", 1)
CONFIG_INTEGER(EnableArm64Sm4,          u"EnableArm64Sm4", 1)
CONFIG_INTEGER(EnableArm64Sve,          u"EnableArm64Sve", 1)
#endif // defined(TARGET_ARM64)

#if defined(CONFIGURABLE_ARM_ABI)
CONFIG_INTEGER(JitSoftFP, u"JitSoftFP", 0)
#endif // defined(CONFIGURABLE_ARM_ABI)

// clang-format on

#ifdef FEATURE_SIMD
CONFIG_INTEGER(JitDisableSimdVN, u"JitDisableSimdVN", 0) // Default 0, ValueNumbering of SIMD nodes and HW Intrinsic
                                                           // nodes enabled
                                                           // If 1, then disable ValueNumbering of SIMD nodes
                                                           // If 2, then disable ValueNumbering of HW Intrinsic nodes
                                                           // If 3, disable both SIMD and HW Intrinsic nodes
#endif                                                     // FEATURE_SIMD

// Default 0, enable the CSE of Constants, including nearby offsets. (only for ARM64)
// If 1, disable all the CSE of Constants
// If 2, enable the CSE of Constants but don't combine with nearby offsets. (only for ARM64)
// If 3, enable the CSE of Constants including nearby offsets. (all platforms)
// If 4, enable the CSE of Constants but don't combine with nearby offsets. (all platforms)
//
CONFIG_INTEGER(JitConstCSE, u"JitConstCSE", 0)

#define CONST_CSE_ENABLE_ARM64 0
#define CONST_CSE_DISABLE_ALL 1
#define CONST_CSE_ENABLE_ARM64_NO_SHARING 2
#define CONST_CSE_ENABLE_ALL 3
#define CONST_CSE_ENABLE_ALL_NO_SHARING 4

///
/// JIT
///
#if !defined(DEBUG) && !defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, u"JitEnableNoWayAssert", 0)
#else  // defined(DEBUG) || defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, u"JitEnableNoWayAssert", 1)
#endif // !defined(DEBUG) && !defined(_DEBUG)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
#define JitMinOptsTrackGCrefs_Default 0 // Not tracking GC refs in MinOpts is new behavior
#else
#define JitMinOptsTrackGCrefs_Default 1
#endif
CONFIG_INTEGER(JitMinOptsTrackGCrefs, u"JitMinOptsTrackGCrefs", JitMinOptsTrackGCrefs_Default) // Track GC roots

// The following should be wrapped inside "#if MEASURE_MEM_ALLOC / #endif", but
// some files include this one without bringing in the definitions from "jit.h"
// so we don't always know what the "true" value of that flag should be. For now
// we take the easy way out and always include the flag, even in release builds
// (normally MEASURE_MEM_ALLOC is off for release builds but if it's toggled on
// for release in "jit.h" the flag would be missing for some includers).
// TODO-Cleanup: need to make 'MEASURE_MEM_ALLOC' well-defined here at all times.
CONFIG_INTEGER(DisplayMemStats, u"JitMemStats", 0) // Display JIT memory usage statistics

CONFIG_INTEGER(JitAggressiveInlining, u"JitAggressiveInlining", 0) // Aggressive inlining of all methods
CONFIG_INTEGER(JitELTHookEnabled, u"JitELTHookEnabled", 0)         // If 1, emit Enter/Leave/TailCall callbacks
CONFIG_INTEGER(JitInlineSIMDMultiplier, u"JitInlineSIMDMultiplier", 3)

#if defined(FEATURE_ENABLE_NO_RANGE_CHECKS)
CONFIG_INTEGER(JitNoRngChks, u"JitNoRngChks", 0) // If 1, don't generate range checks
#endif                                             // defined(FEATURE_ENABLE_NO_RANGE_CHECKS)

#if defined(OPT_CONFIG)
CONFIG_INTEGER(JitDoAssertionProp, u"JitDoAssertionProp", 1) // Perform assertion propagation optimization
CONFIG_INTEGER(JitDoCopyProp, u"JitDoCopyProp", 1)   // Perform copy propagation on variables that appear redundant
CONFIG_INTEGER(JitDoEarlyProp, u"JitDoEarlyProp", 1) // Perform Early Value Propagation
CONFIG_INTEGER(JitDoLoopHoisting, u"JitDoLoopHoisting", 1)   // Perform loop hoisting on loop invariant values
CONFIG_INTEGER(JitDoLoopInversion, u"JitDoLoopInversion", 1) // Perform loop inversion on "for/while" loops
CONFIG_INTEGER(JitDoRangeAnalysis, u"JitDoRangeAnalysis", 1) // Perform range check analysis
CONFIG_INTEGER(JitDoRedundantBranchOpts, u"JitDoRedundantBranchOpts", 1) // Perform redundant branch optimizations
CONFIG_INTEGER(JitDoSsa, u"JitDoSsa", 1) // Perform Static Single Assignment (SSA) numbering on the variables
CONFIG_INTEGER(JitDoValueNumber, u"JitDoValueNumber", 1) // Perform value numbering on method expressions

CONFIG_METHODSET(JitOptRepeat, u"JitOptRepeat")            // Runs optimizer multiple times on the method
CONFIG_INTEGER(JitOptRepeatCount, u"JitOptRepeatCount", 2) // Number of times to repeat opts when repeating
#endif                                                       // defined(OPT_CONFIG)

CONFIG_INTEGER(JitTelemetry, u"JitTelemetry", 1) // If non-zero, gather JIT telemetry data

// Max # of MapSelect's considered for a particular top-level invocation.
CONFIG_INTEGER(JitVNMapSelBudget, u"JitVNMapSelBudget", DEFAULT_MAP_SELECT_BUDGET)

CONFIG_INTEGER(TailCallLoopOpt, u"TailCallLoopOpt", 1) // Convert recursive tail calls to loops
CONFIG_METHODSET(AltJit, u"AltJit")         // Enables AltJit and selectively limits it to the specified methods.
CONFIG_METHODSET(AltJitNgen, u"AltJitNgen") // Enables AltJit for NGEN and selectively limits it
                                              // to the specified methods.

CONFIG_STRING(AltJitExcludeAssemblies, u"AltJitExcludeAssemblies") // Do not use AltJit on this
                                                                     // semicolon-delimited list of assemblies.

CONFIG_INTEGER(JitMeasureIR, u"JitMeasureIR", 0) // If set, measure the IR size after some phases and report it in
                                                   // the time log.

CONFIG_STRING(JitFuncInfoFile, u"JitFuncInfoLogFile") // If set, gather JIT function info and write to this file.
CONFIG_STRING(JitTimeLogCsv, u"JitTimeLogCsv") // If set, gather JIT throughput data and write to a CSV file. This
                                                 // mode must be used in internal retail builds.
CONFIG_STRING(TailCallOpt, u"TailCallOpt")
CONFIG_INTEGER(FastTailCalls, u"FastTailCalls", 1) // If set, allow fast tail calls; otherwise allow only helper-based
                                                     // calls
                                                     // for explicit tail calls.

CONFIG_INTEGER(JitMeasureNowayAssert, u"JitMeasureNowayAssert", 0) // Set to 1 to measure noway_assert usage. Only
                                                                     // valid if MEASURE_NOWAY is defined.
CONFIG_STRING(JitMeasureNowayAssertFile,
              u"JitMeasureNowayAssertFile") // Set to file to write noway_assert usage to a file (if not
                                              // set: stdout). Only valid if MEASURE_NOWAY is defined.
#if defined(DEBUG)
CONFIG_INTEGER(EnableExtraSuperPmiQueries, u"EnableExtraSuperPmiQueries", 0) // Make extra queries to somewhat
                                                                               // future-proof SuperPmi method contexts.
#endif                                                                         // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)
CONFIG_INTEGER(JitInlineDumpData, u"JitInlineDumpData", 0)
CONFIG_INTEGER(JitInlineDumpXml, u"JitInlineDumpXml", 0) // 1 = full xml (+ failures in DEBUG)
                                                           // 2 = only methods with inlines (+ failures in DEBUG)
                                                           // 3 = only methods with inlines, no failures
CONFIG_INTEGER(JitInlineLimit, u"JitInlineLimit", -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, u"JitInlinePolicyDiscretionary", 0)
CONFIG_INTEGER(JitInlinePolicyFull, u"JitInlinePolicyFull", 0)
CONFIG_INTEGER(JitInlinePolicySize, u"JitInlinePolicySize", 0)
CONFIG_INTEGER(JitInlinePolicyRandom, u"JitInlinePolicyRandom", 0) // nonzero enables; value is the external random
                                                                     // seed
CONFIG_INTEGER(JitInlinePolicyReplay, u"JitInlinePolicyReplay", 0)
CONFIG_STRING(JitNoInlineRange, u"JitNoInlineRange")
CONFIG_STRING(JitInlineReplayFile, u"JitInlineReplayFile")
#endif // defined(DEBUG) || defined(INLINE_DATA)

CONFIG_INTEGER(JitInlinePolicyModel, u"JitInlinePolicyModel", 0)
CONFIG_INTEGER(JitInlinePolicyProfile, u"JitInlinePolicyProfile", 0)
CONFIG_INTEGER(JitInlinePolicyProfileThreshold, u"JitInlinePolicyProfileThreshold", 40)
CONFIG_INTEGER(JitObjectStackAllocation, u"JitObjectStackAllocation", 0)

CONFIG_INTEGER(JitEECallTimingInfo, u"JitEECallTimingInfo", 0)

#if defined(DEBUG)
CONFIG_INTEGER(JitEnableFinallyCloning, u"JitEnableFinallyCloning", 1)
CONFIG_INTEGER(JitEnableRemoveEmptyTry, u"JitEnableRemoveEmptyTry", 1)
#endif // DEBUG

// Overall master enable for Guarded Devirtualization.
CONFIG_INTEGER(JitEnableGuardedDevirtualization, u"JitEnableGuardedDevirtualization", 1)

// Various policies for GuardedDevirtualization
CONFIG_INTEGER(JitGuardedDevirtualizationChainLikelihood, u"JitGuardedDevirtualizationChainLikelihood", 0x4B) // 75
CONFIG_INTEGER(JitGuardedDevirtualizationChainStatements, u"JitGuardedDevirtualizationChainStatements", 4)
#if defined(DEBUG)
CONFIG_STRING(JitGuardedDevirtualizationRange, u"JitGuardedDevirtualizationRange")
#endif // DEBUG

// Enable insertion of patchpoints into Tier0 methods with loops.
CONFIG_INTEGER(TC_OnStackReplacement, u"TC_OnStackReplacement", 0)
// Initial patchpoint counter value used by jitted code
CONFIG_INTEGER(TC_OnStackReplacement_InitialCounter, u"TC_OnStackReplacement_InitialCounter", 1000)

// Profile instrumentation options
CONFIG_INTEGER(JitMinimalJitProfiling, u"JitMinimalJitProfiling", 1)
CONFIG_INTEGER(JitMinimalPrejitProfiling, u"JitMinimalPrejitProfiling", 0)
CONFIG_INTEGER(JitClassProfiling, u"JitClassProfiling", 1)
CONFIG_INTEGER(JitEdgeProfiling, u"JitEdgeProfiling", 1)
CONFIG_INTEGER(JitCollect64BitCounts, u"JitCollect64BitCounts", 0) // Collect counts as 64-bit values.

// Profile consumption options
CONFIG_INTEGER(JitDisablePgo, u"JitDisablePgo", 0) // Ignore pgo data for all methods
#if defined(DEBUG)
CONFIG_STRING(JitEnablePgoRange, u"JitEnablePgoRange") // Enable pgo data for only some methods
#endif                                                   // debug

// Control when Virtual Calls are expanded
CONFIG_INTEGER(JitExpandCallsEarly, u"JitExpandCallsEarly", 1) // Expand Call targets early (in the global morph
                                                                 // phase)

#if defined(DEBUG)
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
CONFIG_STRING(JitFunctionFile, u"JitFunctionFile")
#endif // DEBUG

#if defined(DEBUG)
#if defined(TARGET_ARM64)
// JitSaveFpLrWithCalleeSavedRegisters:
//    0: use default frame type decision
//    1: disable frames that save FP/LR registers with the callee-saved registers (at the top of the frame)
//    2: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame)
CONFIG_INTEGER(JitSaveFpLrWithCalleeSavedRegisters, u"JitSaveFpLrWithCalleeSavedRegisters", 0)
#endif // defined(TARGET_ARM64)
#endif // DEBUG

// Allow to enregister locals with struct type.
CONFIG_INTEGER(JitEnregStructLocals, u"JitEnregStructLocals", 0)

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
