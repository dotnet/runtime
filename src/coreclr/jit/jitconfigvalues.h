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
CONFIG_INTEGER(AltJitLimit, L"AltJitLimit", 0)               // Max number of functions to use altjit for (decimal)
CONFIG_INTEGER(AltJitSkipOnAssert, L"AltJitSkipOnAssert", 0) // If AltJit hits an assert, fall back to the fallback
                                                               // JIT. Useful in conjunction with
                                                               // COMPlus_ContinueOnAssert=1
CONFIG_INTEGER(BreakOnDumpToken, L"BreakOnDumpToken", 0xffffffff) // Breaks when using internal logging on a
                                                                    // particular token value.
CONFIG_INTEGER(DebugBreakOnVerificationFailure, L"DebugBreakOnVerificationFailure", 0) // Halts the jit on
                                                                                         // verification failure
CONFIG_INTEGER(DiffableDasm, L"JitDiffableDasm", 0)          // Make the disassembly diff-able
CONFIG_INTEGER(JitDasmWithAddress, L"JitDasmWithAddress", 0) // Print the process address next to each instruction of
                                                               // the disassembly
CONFIG_INTEGER(DisplayLoopHoistStats, L"JitLoopHoistStats", 0) // Display JIT loop hoisting statistics
CONFIG_INTEGER(DisplayLsraStats, L"JitLsraStats", 0) // Display JIT Linear Scan Register Allocator statistics
                                                       // if set to 1. If set to "2", display the stats in csv format.
                                                       // Recommended to use with JitStdOutFile flag.
CONFIG_STRING(JitLsraOrdering, L"JitLsraOrdering")   // LSRA heuristics ordering
CONFIG_INTEGER(DumpJittedMethods, L"DumpJittedMethods", 0) // Prints all jitted methods to the console
CONFIG_INTEGER(EnablePCRelAddr, L"JitEnablePCRelAddr", 1)  // Whether absolute addr be encoded as PC-rel offset by
                                                             // RyuJIT where possible
CONFIG_INTEGER(JitAssertOnMaxRAPasses, L"JitAssertOnMaxRAPasses", 0)
CONFIG_INTEGER(JitBreakEmitOutputInstr, L"JitBreakEmitOutputInstr", -1)
CONFIG_INTEGER(JitBreakMorphTree, L"JitBreakMorphTree", 0xffffffff)
CONFIG_INTEGER(JitBreakOnBadCode, L"JitBreakOnBadCode", 0)
CONFIG_INTEGER(JitBreakOnMinOpts, L"JITBreakOnMinOpts", 0) // Halt if jit switches to MinOpts
CONFIG_INTEGER(JitBreakOnUnsafeCode, L"JitBreakOnUnsafeCode", 0)
CONFIG_INTEGER(JitCanUseSSE2, L"JitCanUseSSE2", -1)
CONFIG_INTEGER(JitCloneLoops, L"JitCloneLoops", 1) // If 0, don't clone. Otherwise clone loops for optimizations.
CONFIG_INTEGER(JitDebugLogLoopCloning, L"JitDebugLogLoopCloning", 0) // In debug builds log places where loop cloning
                                                                       // optimizations are performed on the fast path.
CONFIG_INTEGER(JitDefaultFill, L"JitDefaultFill", 0xdd) // In debug builds, initialize the memory allocated by the nra
                                                          // with this byte.
CONFIG_INTEGER(JitAlignLoopMinBlockWeight,
               L"JitAlignLoopMinBlockWeight",
               DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT) // Minimum weight needed for the first block of a loop to make it a
                                                    // candidate for alignment.
CONFIG_INTEGER(JitAlignLoopMaxCodeSize,
               L"JitAlignLoopMaxCodeSize",
               DEFAULT_MAX_LOOPSIZE_FOR_ALIGN) // For non-adaptive alignment, minimum loop size (in bytes) for which
                                               // alignment will be done.
                                               // Defaults to 3 blocks of 32 bytes chunks = 96 bytes.
CONFIG_INTEGER(JitAlignLoopBoundary,
               L"JitAlignLoopBoundary",
               DEFAULT_ALIGN_LOOP_BOUNDARY) // For non-adaptive alignment, address boundary (power of 2) at which loop
                                            // alignment should be done. By default, 32B.
CONFIG_INTEGER(JitAlignLoopForJcc,
               L"JitAlignLoopForJcc",
               0) // If set, for non-adaptive alignment, ensure loop jmps are not on or cross alignment boundary.

CONFIG_INTEGER(JitAlignLoopAdaptive,
               L"JitAlignLoopAdaptive",
               1) // If set, perform adaptive loop alignment that limits number of padding based on loop size.

CONFIG_INTEGER(JitDirectAlloc, L"JitDirectAlloc", 0)
CONFIG_INTEGER(JitDoubleAlign, L"JitDoubleAlign", 1)
CONFIG_INTEGER(JitDumpASCII, L"JitDumpASCII", 1)               // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpTerseLsra, L"JitDumpTerseLsra", 1)       // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, L"JitDumpToDebugger", 0)     // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, L"JitDumpVerboseSsa", 0)     // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, L"JitDumpVerboseTrees", 0) // Enable more verbose tree dumps
CONFIG_INTEGER(JitEmitPrintRefRegs, L"JitEmitPrintRefRegs", 0)
CONFIG_INTEGER(JitEnableDevirtualization, L"JitEnableDevirtualization", 1) // Enable devirtualization in importer
CONFIG_INTEGER(JitEnableLateDevirtualization, L"JitEnableLateDevirtualization", 1) // Enable devirtualization after
                                                                                     // inlining
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, L"JitExpensiveDebugCheckLevel", 0) // Level indicates how much checking
                                                                                 // beyond the default to do in debug
                                                                                 // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, L"JitForceFallback", 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitForceVer, L"JitForceVer", 0)
CONFIG_INTEGER(JitFullyInt, L"JitFullyInt", 0)           // Forces Fully interruptible code
CONFIG_INTEGER(JitFunctionTrace, L"JitFunctionTrace", 0) // If non-zero, print JIT start/end logging
CONFIG_INTEGER(JitGCChecks, L"JitGCChecks", 0)
CONFIG_INTEGER(JitGCInfoLogging, L"JitGCInfoLogging", 0) // If true, prints GCInfo-related output to standard output.
CONFIG_INTEGER(JitHashBreak, L"JitHashBreak", -1)        // Same as JitBreak, but for a method hash
CONFIG_INTEGER(JitHashDump, L"JitHashDump", -1)          // Same as JitDump, but for a method hash
CONFIG_INTEGER(JitHashHalt, L"JitHashHalt", -1)          // Same as JitHalt, but for a method hash
CONFIG_INTEGER(JitInlineAdditionalMultiplier, L"JitInlineAdditionalMultiplier", 0)
CONFIG_INTEGER(JitInlinePrintStats, L"JitInlinePrintStats", 0)
CONFIG_INTEGER(JitInlineSize, L"JITInlineSize", DEFAULT_MAX_INLINE_SIZE)
CONFIG_INTEGER(JitInlineDepth, L"JITInlineDepth", DEFAULT_MAX_INLINE_DEPTH)
CONFIG_INTEGER(JitLongAddress, L"JitLongAddress", 0) // Force using the large pseudo instruction form for long address
CONFIG_INTEGER(JitMaxTempAssert, L"JITMaxTempAssert", 1)
CONFIG_INTEGER(JitMaxUncheckedOffset, L"JitMaxUncheckedOffset", 8)
CONFIG_INTEGER(JitMinOpts, L"JITMinOpts", 0)                                       // Forces MinOpts
CONFIG_INTEGER(JitMinOptsBbCount, L"JITMinOptsBbCount", DEFAULT_MIN_OPTS_BB_COUNT) // Internal jit control of MinOpts
CONFIG_INTEGER(JitMinOptsCodeSize, L"JITMinOptsCodeSize", DEFAULT_MIN_OPTS_CODE_SIZE)       // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsInstrCount, L"JITMinOptsInstrCount", DEFAULT_MIN_OPTS_INSTR_COUNT) // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsLvNumCount, L"JITMinOptsLvNumcount", DEFAULT_MIN_OPTS_LV_NUM_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitMinOptsLvRefCount, L"JITMinOptsLvRefcount", DEFAULT_MIN_OPTS_LV_REF_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitNoCMOV, L"JitNoCMOV", 0)
CONFIG_INTEGER(JitNoCSE, L"JitNoCSE", 0)
CONFIG_INTEGER(JitNoCSE2, L"JitNoCSE2", 0)
CONFIG_INTEGER(JitNoForceFallback, L"JitNoForceFallback", 0) // Set to non-zero to prevent NOWAY assert testing.
                                                               // Overrides COMPlus_JitForceFallback and JIT stress
                                                               // flags.
CONFIG_INTEGER(JitNoHoist, L"JitNoHoist", 0)
CONFIG_INTEGER(JitNoInline, L"JitNoInline", 0)                 // Disables inlining of all methods
CONFIG_INTEGER(JitNoMemoryBarriers, L"JitNoMemoryBarriers", 0) // If 1, don't generate memory barriers
CONFIG_INTEGER(JitNoRegLoc, L"JitNoRegLoc", 0)
CONFIG_INTEGER(JitNoStructPromotion, L"JitNoStructPromotion", 0) // Disables struct promotion in Jit32
CONFIG_INTEGER(JitNoUnroll, L"JitNoUnroll", 0)
CONFIG_INTEGER(JitOrder, L"JitOrder", 0)
CONFIG_INTEGER(JitQueryCurrentStaticFieldClass, L"JitQueryCurrentStaticFieldClass", 1)
CONFIG_INTEGER(JitReportFastTailCallDecisions, L"JitReportFastTailCallDecisions", 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, L"JITPInvokeCheckEnabled", 0)
CONFIG_INTEGER(JitPInvokeEnabled, L"JITPInvokeEnabled", 1)
CONFIG_METHODSET(JitPrintInlinedMethods, L"JitPrintInlinedMethods")
CONFIG_METHODSET(JitPrintDevirtualizedMethods, L"JitPrintDevirtualizedMethods")
CONFIG_INTEGER(JitProfileChecks, L"JitProfileChecks", 0) // 1 enable in dumps, 2 assert if issues found
CONFIG_INTEGER(JitRequired, L"JITRequired", -1)
CONFIG_INTEGER(JitRoundFloat, L"JITRoundFloat", DEFAULT_ROUND_LEVEL)
CONFIG_INTEGER(JitStackAllocToLocalSize, L"JitStackAllocToLocalSize", DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE)
CONFIG_INTEGER(JitSkipArrayBoundCheck, L"JitSkipArrayBoundCheck", 0)
CONFIG_INTEGER(JitSlowDebugChecksEnabled, L"JitSlowDebugChecksEnabled", 1) // Turn on slow debug checks
CONFIG_INTEGER(JitSplitFunctionSize, L"JitSplitFunctionSize", 0) // On ARM, use this as the maximum function/funclet
                                                                   // size for creating function fragments (and creating
                                                                   // multiple RUNTIME_FUNCTION entries)
CONFIG_INTEGER(JitSsaStress, L"JitSsaStress", 0) // Perturb order of processing of blocks in SSA; 0 = no stress; 1 =
                                                   // use method hash; * = supplied value as random hash
CONFIG_INTEGER(JitStackChecks, L"JitStackChecks", 0)
CONFIG_STRING(JitStdOutFile, L"JitStdOutFile") // If set, sends JIT's stdout output to this file.
CONFIG_INTEGER(JitStress, L"JitStress", 0) // Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary
                                             // stress based on a hash of the method and this value
CONFIG_INTEGER(JitStressBBProf, L"JitStressBBProf", 0)               // Internal Jit stress mode
CONFIG_INTEGER(JitStressBiasedCSE, L"JitStressBiasedCSE", 0x101)     // Internal Jit stress mode: decimal bias value
                                                                       // between (0,100) to perform CSE on a candidate.
                                                                       // 100% = All CSEs. 0% = 0 CSE. (> 100) means no
                                                                       // stress.
CONFIG_INTEGER(JitStressFP, L"JitStressFP", 0)                       // Internal Jit stress mode
CONFIG_INTEGER(JitStressModeNamesOnly, L"JitStressModeNamesOnly", 0) // Internal Jit stress: if nonzero, only enable
                                                                       // stress modes listed in JitStressModeNames
CONFIG_INTEGER(JitStressRegs, L"JitStressRegs", 0)
CONFIG_INTEGER(JitVNMapSelLimit, L"JitVNMapSelLimit", 0) // If non-zero, assert if # of VNF_MapSelect applications
                                                           // considered reaches this
CONFIG_INTEGER(NgenHashDump, L"NgenHashDump", -1)        // same as JitHashDump, but for ngen
CONFIG_INTEGER(NgenOrder, L"NgenOrder", 0)
CONFIG_INTEGER(RunAltJitCode, L"RunAltJitCode", 1) // If non-zero, and the compilation succeeds for an AltJit, then
                                                     // use the code. If zero, then we always throw away the generated
                                                     // code and fall back to the default compiler.
CONFIG_INTEGER(RunComponentUnitTests, L"JitComponentUnitTests", 0) // Run JIT component unit tests
CONFIG_INTEGER(ShouldInjectFault, L"InjectFault", 0)
CONFIG_INTEGER(StressCOMCall, L"StressCOMCall", 0)
CONFIG_INTEGER(TailcallStress, L"TailcallStress", 0)
CONFIG_INTEGER(TreesBeforeAfterMorph, L"JitDumpBeforeAfterMorph", 0) // If 1, display each tree before/after morphing

CONFIG_METHODSET(JitBreak, L"JitBreak") // Stops in the importer when compiling a specified method
CONFIG_METHODSET(JitDebugBreak, L"JitDebugBreak")
CONFIG_METHODSET(JitDisasm, L"JitDisasm")                  // Dumps disassembly for specified method
CONFIG_STRING(JitDisasmAssemblies, L"JitDisasmAssemblies") // Only show JitDisasm and related info for methods
                                                             // from this semicolon-delimited list of assemblies.
CONFIG_INTEGER(JitDisasmWithGC, L"JitDisasmWithGC", 0)     // Dump interleaved GC Info for any method disassembled.
CONFIG_METHODSET(JitDump, L"JitDump")                      // Dumps trees for specified method
CONFIG_METHODSET(JitEHDump, L"JitEHDump")                  // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(JitExclude, L"JitExclude")
CONFIG_METHODSET(JitForceProcedureSplitting, L"JitForceProcedureSplitting")
CONFIG_METHODSET(JitGCDump, L"JitGCDump")
CONFIG_METHODSET(JitDebugDump, L"JitDebugDump")
CONFIG_METHODSET(JitHalt, L"JitHalt") // Emits break instruction into jitted code
CONFIG_METHODSET(JitImportBreak, L"JitImportBreak")
CONFIG_METHODSET(JitInclude, L"JitInclude")
CONFIG_METHODSET(JitLateDisasm, L"JitLateDisasm")
CONFIG_METHODSET(JitMinOptsName, L"JITMinOptsName")                   // Forces MinOpts for a named function
CONFIG_METHODSET(JitNoProcedureSplitting, L"JitNoProcedureSplitting") // Disallow procedure splitting for specified
                                                                        // methods
CONFIG_METHODSET(JitNoProcedureSplittingEH, L"JitNoProcedureSplittingEH") // Disallow procedure splitting for
                                                                            // specified methods if they contain
                                                                            // exception handling
CONFIG_METHODSET(JitStressOnly, L"JitStressOnly") // Internal Jit stress mode: stress only the specified method(s)
CONFIG_METHODSET(JitUnwindDump, L"JitUnwindDump") // Dump the unwind codes for the method
///
/// NGEN
///
CONFIG_METHODSET(NgenDisasm, L"NgenDisasm") // Same as JitDisasm, but for ngen
CONFIG_METHODSET(NgenDump, L"NgenDump")     // Same as JitDump, but for ngen
CONFIG_METHODSET(NgenEHDump, L"NgenEHDump") // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(NgenGCDump, L"NgenGCDump")
CONFIG_METHODSET(NgenDebugDump, L"NgenDebugDump")
CONFIG_METHODSET(NgenUnwindDump, L"NgenUnwindDump") // Dump the unwind codes for the method
///
/// JIT
///
CONFIG_METHODSET(JitDumpFg, L"JitDumpFg")        // Dumps Xml/Dot Flowgraph for specified method
CONFIG_STRING(JitDumpFgDir, L"JitDumpFgDir")     // Directory for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgFile, L"JitDumpFgFile")   // Filename for Xml/Dot flowgraph dump(s) (default: "default")
CONFIG_STRING(JitDumpFgPhase, L"JitDumpFgPhase") // Phase-based Xml/Dot flowgraph support. Set to the short name of a
                                                   // phase to see the flowgraph after that phase. Leave unset to dump
                                                   // after COLD-BLK (determine first cold block) or set to * for all
                                                   // phases

CONFIG_STRING(JitDumpFgPrePhase,
              L"JitDumpFgPrePhase") // Same as JitDumpFgPhase, but specifies to dump pre-phase, not post-phase.
CONFIG_INTEGER(JitDumpFgDot, L"JitDumpFgDot", 1)     // 0 == dump XML format; non-zero == dump DOT format
CONFIG_INTEGER(JitDumpFgEH, L"JitDumpFgEH", 0)       // 0 == no EH regions; non-zero == include EH regions
CONFIG_INTEGER(JitDumpFgLoops, L"JitDumpFgLoops", 0) // 0 == no loop regions; non-zero == include loop regions

CONFIG_INTEGER(JitDumpFgConstrained, L"JitDumpFgConstrained", 1) // 0 == don't constrain to mostly linear layout;
                                                                   // non-zero == force mostly lexical block
                                                                   // linear layout
CONFIG_INTEGER(JitDumpFgBlockID, L"JitDumpFgBlockID", 0) // 0 == display block with bbNum; 1 == display with both
                                                           // bbNum and bbID

CONFIG_STRING(JitLateDisasmTo, L"JITLateDisasmTo")
CONFIG_STRING(JitRange, L"JitRange")
CONFIG_STRING(JitStressModeNames, L"JitStressModeNames") // Internal Jit stress mode: stress using the given set of
                                                           // stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, L"JitStressModeNamesNot") // Internal Jit stress mode: do NOT stress using the
                                                                 // given set of stress mode names, e.g. STRESS_REGS,
                                                                 // STRESS_TAILCALL
CONFIG_STRING(JitStressRange, L"JitStressRange")               // Internal Jit stress mode
///
/// NGEN
///
CONFIG_METHODSET(NgenDumpFg, L"NgenDumpFg")      // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgDir, L"NgenDumpFgDir")   // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgFile, L"NgenDumpFgFile") // Ngen Xml/Dot flowgraph dump support
///
/// JIT Hardware Intrinsics
///
CONFIG_INTEGER(EnableIncompleteISAClass, L"EnableIncompleteISAClass", 0) // Enable testing not-yet-implemented
                                                                           // intrinsic classes

#endif // defined(DEBUG)

#if FEATURE_LOOP_ALIGN
CONFIG_INTEGER(JitAlignLoops, L"JitAlignLoops", 1) // If set, align inner loops
#else
CONFIG_INTEGER(JitAlignLoops, L"JitAlignLoops", 0)
#endif

///
/// JIT
///
#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
CONFIG_INTEGER(JitNoRangeChks, L"JitNoRngChks", 0) // If 1, don't generate range checks
#endif

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate
// fallback to main JIT on hitting a NYI.
#if defined(TARGET_ARM64) || defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, L"AltJitAssertOnNYI", 0) // Controls the AltJit behavior of NYI stuff
#else                                                        // !defined(TARGET_ARM64) && !defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, L"AltJitAssertOnNYI", 1) // Controls the AltJit behavior of NYI stuff
#endif                                                       // defined(TARGET_ARM64) || defined(TARGET_X86)
///
/// JIT Hardware Intrinsics
///
#if defined(TARGET_X86) || defined(TARGET_AMD64)
CONFIG_INTEGER(EnableSSE3_4, L"EnableSSE3_4", 1) // Enable SSE3, SSSE3, SSE 4.1 and 4.2 instruction set as default
#endif

#if defined(TARGET_AMD64) || defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default. When both AVX and SSE3_4 are set, we will use the most
// capable instruction set available which will prefer AVX over SSE3/4.
CONFIG_INTEGER(EnableHWIntrinsic, L"EnableHWIntrinsic", 1) // Enable Base
CONFIG_INTEGER(EnableSSE, L"EnableSSE", 1)                 // Enable SSE
CONFIG_INTEGER(EnableSSE2, L"EnableSSE2", 1)               // Enable SSE2
CONFIG_INTEGER(EnableSSE3, L"EnableSSE3", 1)               // Enable SSE3
CONFIG_INTEGER(EnableSSSE3, L"EnableSSSE3", 1)             // Enable SSSE3
CONFIG_INTEGER(EnableSSE41, L"EnableSSE41", 1)             // Enable SSE41
CONFIG_INTEGER(EnableSSE42, L"EnableSSE42", 1)             // Enable SSE42
CONFIG_INTEGER(EnableAVX, L"EnableAVX", 1)                 // Enable AVX
CONFIG_INTEGER(EnableAVX2, L"EnableAVX2", 1)               // Enable AVX2
CONFIG_INTEGER(EnableFMA, L"EnableFMA", 1)                 // Enable FMA
CONFIG_INTEGER(EnableAES, L"EnableAES", 1)                 // Enable AES
CONFIG_INTEGER(EnableBMI1, L"EnableBMI1", 1)               // Enable BMI1
CONFIG_INTEGER(EnableBMI2, L"EnableBMI2", 1)               // Enable BMI2
CONFIG_INTEGER(EnableLZCNT, L"EnableLZCNT", 1)             // Enable AES
CONFIG_INTEGER(EnablePCLMULQDQ, L"EnablePCLMULQDQ", 1)     // Enable PCLMULQDQ
CONFIG_INTEGER(EnablePOPCNT, L"EnablePOPCNT", 1)           // Enable POPCNT
#else                                                        // !defined(TARGET_AMD64) && !defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default
CONFIG_INTEGER(EnableAVX, L"EnableAVX", 0)
#endif                                                       // !defined(TARGET_AMD64) && !defined(TARGET_X86)

CONFIG_INTEGER(EnableEHWriteThru, L"EnableEHWriteThrL", 1) // Enable the register allocator to support EH-write thru:
                                                             // partial enregistration of vars exposed on EH boundaries
CONFIG_INTEGER(EnableMultiRegLocals, L"EnableMultiRegLocals", 1) // Enable the enregistration of locals that are
                                                                   // defined or used in a multireg context.

// clang-format off

#if defined(TARGET_ARM64)
CONFIG_INTEGER(EnableHWIntrinsic,       L"EnableHWIntrinsic", 1)
CONFIG_INTEGER(EnableArm64Aes,          L"EnableArm64Aes", 1)
CONFIG_INTEGER(EnableArm64Atomics,      L"EnableArm64Atomics", 1)
CONFIG_INTEGER(EnableArm64Crc32,        L"EnableArm64Crc32", 1)
CONFIG_INTEGER(EnableArm64Dcpop,        L"EnableArm64Dcpop", 1)
CONFIG_INTEGER(EnableArm64Dp,           L"EnableArm64Dp", 1)
CONFIG_INTEGER(EnableArm64Fcma,         L"EnableArm64Fcma", 1)
CONFIG_INTEGER(EnableArm64Fp,           L"EnableArm64Fp", 1)
CONFIG_INTEGER(EnableArm64Fp16,         L"EnableArm64Fp16", 1)
CONFIG_INTEGER(EnableArm64Jscvt,        L"EnableArm64Jscvt", 1)
CONFIG_INTEGER(EnableArm64Lrcpc,        L"EnableArm64Lrcpc", 1)
CONFIG_INTEGER(EnableArm64Pmull,        L"EnableArm64Pmull", 1)
CONFIG_INTEGER(EnableArm64Sha1,         L"EnableArm64Sha1", 1)
CONFIG_INTEGER(EnableArm64Sha256,       L"EnableArm64Sha256", 1)
CONFIG_INTEGER(EnableArm64Sha512,       L"EnableArm64Sha512", 1)
CONFIG_INTEGER(EnableArm64Sha3,         L"EnableArm64Sha3", 1)
CONFIG_INTEGER(EnableArm64AdvSimd,      L"EnableArm64AdvSimd", 1)
CONFIG_INTEGER(EnableArm64AdvSimd_v81,  L"EnableArm64AdvSimd_v81", 1)
CONFIG_INTEGER(EnableArm64AdvSimd_Fp16, L"EnableArm64AdvSimd_Fp16", 1)
CONFIG_INTEGER(EnableArm64Sm3,          L"EnableArm64Sm3", 1)
CONFIG_INTEGER(EnableArm64Sm4,          L"EnableArm64Sm4", 1)
CONFIG_INTEGER(EnableArm64Sve,          L"EnableArm64Sve", 1)
#endif // defined(TARGET_ARM64)

#if defined(CONFIGURABLE_ARM_ABI)
CONFIG_INTEGER(JitSoftFP, L"JitSoftFP", 0)
#endif // defined(CONFIGURABLE_ARM_ABI)

// clang-format on

#ifdef FEATURE_SIMD
CONFIG_INTEGER(JitDisableSimdVN, L"JitDisableSimdVN", 0) // Default 0, ValueNumbering of SIMD nodes and HW Intrinsic
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
CONFIG_INTEGER(JitConstCSE, L"JitConstCSE", 0)

#define CONST_CSE_ENABLE_ARM64 0
#define CONST_CSE_DISABLE_ALL 1
#define CONST_CSE_ENABLE_ARM64_NO_SHARING 2
#define CONST_CSE_ENABLE_ALL 3
#define CONST_CSE_ENABLE_ALL_NO_SHARING 4

///
/// JIT
///
#if !defined(DEBUG) && !defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, L"JitEnableNoWayAssert", 0)
#else  // defined(DEBUG) || defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, L"JitEnableNoWayAssert", 1)
#endif // !defined(DEBUG) && !defined(_DEBUG)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
#define JitMinOptsTrackGCrefs_Default 0 // Not tracking GC refs in MinOpts is new behavior
#else
#define JitMinOptsTrackGCrefs_Default 1
#endif
CONFIG_INTEGER(JitMinOptsTrackGCrefs, L"JitMinOptsTrackGCrefs", JitMinOptsTrackGCrefs_Default) // Track GC roots

// The following should be wrapped inside "#if MEASURE_MEM_ALLOC / #endif", but
// some files include this one without bringing in the definitions from "jit.h"
// so we don't always know what the "true" value of that flag should be. For now
// we take the easy way out and always include the flag, even in release builds
// (normally MEASURE_MEM_ALLOC is off for release builds but if it's toggled on
// for release in "jit.h" the flag would be missing for some includers).
// TODO-Cleanup: need to make 'MEASURE_MEM_ALLOC' well-defined here at all times.
CONFIG_INTEGER(DisplayMemStats, L"JitMemStats", 0) // Display JIT memory usage statistics

CONFIG_INTEGER(JitAggressiveInlining, L"JitAggressiveInlining", 0) // Aggressive inlining of all methods
CONFIG_INTEGER(JitELTHookEnabled, L"JitELTHookEnabled", 0)         // If 1, emit Enter/Leave/TailCall callbacks
CONFIG_INTEGER(JitInlineSIMDMultiplier, L"JitInlineSIMDMultiplier", 3)

#if defined(FEATURE_ENABLE_NO_RANGE_CHECKS)
CONFIG_INTEGER(JitNoRngChks, L"JitNoRngChks", 0) // If 1, don't generate range checks
#endif                                             // defined(FEATURE_ENABLE_NO_RANGE_CHECKS)

#if defined(OPT_CONFIG)
CONFIG_INTEGER(JitDoAssertionProp, L"JitDoAssertionProp", 1) // Perform assertion propagation optimization
CONFIG_INTEGER(JitDoCopyProp, L"JitDoCopyProp", 1)   // Perform copy propagation on variables that appear redundant
CONFIG_INTEGER(JitDoEarlyProp, L"JitDoEarlyProp", 1) // Perform Early Value Propagation
CONFIG_INTEGER(JitDoLoopHoisting, L"JitDoLoopHoisting", 1)   // Perform loop hoisting on loop invariant values
CONFIG_INTEGER(JitDoLoopInversion, L"JitDoLoopInversion", 1) // Perform loop inversion on "for/while" loops
CONFIG_INTEGER(JitDoRangeAnalysis, L"JitDoRangeAnalysis", 1) // Perform range check analysis
CONFIG_INTEGER(JitDoRedundantBranchOpts, L"JitDoRedundantBranchOpts", 1) // Perform redundant branch optimizations
CONFIG_INTEGER(JitDoSsa, L"JitDoSsa", 1) // Perform Static Single Assignment (SSA) numbering on the variables
CONFIG_INTEGER(JitDoValueNumber, L"JitDoValueNumber", 1) // Perform value numbering on method expressions

CONFIG_METHODSET(JitOptRepeat, L"JitOptRepeat")            // Runs optimizer multiple times on the method
CONFIG_INTEGER(JitOptRepeatCount, L"JitOptRepeatCount", 2) // Number of times to repeat opts when repeating
#endif                                                       // defined(OPT_CONFIG)

CONFIG_INTEGER(JitTelemetry, L"JitTelemetry", 1) // If non-zero, gather JIT telemetry data

// Max # of MapSelect's considered for a particular top-level invocation.
CONFIG_INTEGER(JitVNMapSelBudget, L"JitVNMapSelBudget", DEFAULT_MAP_SELECT_BUDGET)

CONFIG_INTEGER(TailCallLoopOpt, L"TailCallLoopOpt", 1) // Convert recursive tail calls to loops
CONFIG_METHODSET(AltJit, L"AltJit")         // Enables AltJit and selectively limits it to the specified methods.
CONFIG_METHODSET(AltJitNgen, L"AltJitNgen") // Enables AltJit for NGEN and selectively limits it
                                              // to the specified methods.

CONFIG_STRING(AltJitExcludeAssemblies, L"AltJitExcludeAssemblies") // Do not use AltJit on this
                                                                     // semicolon-delimited list of assemblies.

CONFIG_INTEGER(JitMeasureIR, L"JitMeasureIR", 0) // If set, measure the IR size after some phases and report it in
                                                   // the time log.

CONFIG_STRING(JitFuncInfoFile, L"JitFuncInfoLogFile") // If set, gather JIT function info and write to this file.
CONFIG_STRING(JitTimeLogCsv, L"JitTimeLogCsv") // If set, gather JIT throughput data and write to a CSV file. This
                                                 // mode must be used in internal retail builds.
CONFIG_STRING(TailCallOpt, L"TailCallOpt")
CONFIG_INTEGER(FastTailCalls, L"FastTailCalls", 1) // If set, allow fast tail calls; otherwise allow only helper-based
                                                     // calls
                                                     // for explicit tail calls.

CONFIG_INTEGER(JitMeasureNowayAssert, L"JitMeasureNowayAssert", 0) // Set to 1 to measure noway_assert usage. Only
                                                                     // valid if MEASURE_NOWAY is defined.
CONFIG_STRING(JitMeasureNowayAssertFile,
              L"JitMeasureNowayAssertFile") // Set to file to write noway_assert usage to a file (if not
                                              // set: stdout). Only valid if MEASURE_NOWAY is defined.
#if defined(DEBUG)
CONFIG_INTEGER(EnableExtraSuperPmiQueries, L"EnableExtraSuperPmiQueries", 0) // Make extra queries to somewhat
                                                                               // future-proof SuperPmi method contexts.
#endif                                                                         // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)
CONFIG_INTEGER(JitInlineDumpData, L"JitInlineDumpData", 0)
CONFIG_INTEGER(JitInlineDumpXml, L"JitInlineDumpXml", 0) // 1 = full xml (+ failures in DEBUG)
                                                           // 2 = only methods with inlines (+ failures in DEBUG)
                                                           // 3 = only methods with inlines, no failures
CONFIG_INTEGER(JitInlineLimit, L"JitInlineLimit", -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, L"JitInlinePolicyDiscretionary", 0)
CONFIG_INTEGER(JitInlinePolicyFull, L"JitInlinePolicyFull", 0)
CONFIG_INTEGER(JitInlinePolicySize, L"JitInlinePolicySize", 0)
CONFIG_INTEGER(JitInlinePolicyRandom, L"JitInlinePolicyRandom", 0) // nonzero enables; value is the external random
                                                                     // seed
CONFIG_INTEGER(JitInlinePolicyReplay, L"JitInlinePolicyReplay", 0)
CONFIG_STRING(JitNoInlineRange, L"JitNoInlineRange")
CONFIG_STRING(JitInlineReplayFile, L"JitInlineReplayFile")
#endif // defined(DEBUG) || defined(INLINE_DATA)

CONFIG_INTEGER(JitInlinePolicyModel, L"JitInlinePolicyModel", 0)
CONFIG_INTEGER(JitInlinePolicyProfile, L"JitInlinePolicyProfile", 0)
CONFIG_INTEGER(JitInlinePolicyProfileThreshold, L"JitInlinePolicyProfileThreshold", 40)
CONFIG_INTEGER(JitObjectStackAllocation, L"JitObjectStackAllocation", 0)

CONFIG_INTEGER(JitEECallTimingInfo, L"JitEECallTimingInfo", 0)

#if defined(DEBUG)
CONFIG_INTEGER(JitEnableFinallyCloning, L"JitEnableFinallyCloning", 1)
CONFIG_INTEGER(JitEnableRemoveEmptyTry, L"JitEnableRemoveEmptyTry", 1)
#endif // DEBUG

// Overall master enable for Guarded Devirtualization.
CONFIG_INTEGER(JitEnableGuardedDevirtualization, L"JitEnableGuardedDevirtualization", 1)

// Various policies for GuardedDevirtualization
CONFIG_INTEGER(JitGuardedDevirtualizationChainLikelihood, L"JitGuardedDevirtualizationChainLikelihood", 0x4B) // 75
CONFIG_INTEGER(JitGuardedDevirtualizationChainStatements, L"JitGuardedDevirtualizationChainStatements", 4)
#if defined(DEBUG)
CONFIG_STRING(JitGuardedDevirtualizationRange, L"JitGuardedDevirtualizationRange")
#endif // DEBUG

// Enable insertion of patchpoints into Tier0 methods with loops.
CONFIG_INTEGER(TC_OnStackReplacement, L"TC_OnStackReplacement", 0)
// Initial patchpoint counter value used by jitted code
CONFIG_INTEGER(TC_OnStackReplacement_InitialCounter, L"TC_OnStackReplacement_InitialCounter", 1000)

// Profile instrumentation options
CONFIG_INTEGER(JitMinimalJitProfiling, L"JitMinimalJitProfiling", 1)
CONFIG_INTEGER(JitMinimalPrejitProfiling, L"JitMinimalPrejitProfiling", 0)
CONFIG_INTEGER(JitClassProfiling, L"JitClassProfiling", 1)
CONFIG_INTEGER(JitEdgeProfiling, L"JitEdgeProfiling", 1)
CONFIG_INTEGER(JitCollect64BitCounts, L"JitCollect64BitCounts", 0) // Collect counts as 64-bit values.

// Profile consumption options
CONFIG_INTEGER(JitDisablePgo, L"JitDisablePgo", 0) // Ignore pgo data for all methods
#if defined(DEBUG)
CONFIG_STRING(JitEnablePgoRange, L"JitEnablePgoRange") // Enable pgo data for only some methods
#endif                                                   // debug

// Control when Virtual Calls are expanded
CONFIG_INTEGER(JitExpandCallsEarly, L"JitExpandCallsEarly", 1) // Expand Call targets early (in the global morph
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
CONFIG_STRING(JitFunctionFile, L"JitFunctionFile")
#endif // DEBUG

#if defined(DEBUG)
#if defined(TARGET_ARM64)
// JitSaveFpLrWithCalleeSavedRegisters:
//    0: use default frame type decision
//    1: disable frames that save FP/LR registers with the callee-saved registers (at the top of the frame)
//    2: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame)
CONFIG_INTEGER(JitSaveFpLrWithCalleeSavedRegisters, L"JitSaveFpLrWithCalleeSavedRegisters", 0)
#endif // defined(TARGET_ARM64)
#endif // DEBUG

// Allow to enregister locals with struct type.
CONFIG_INTEGER(JitEnregStructLocals, L"JitEnregStructLocals", 0)

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
