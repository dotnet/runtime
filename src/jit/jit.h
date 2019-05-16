// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef _JIT_H_
#define _JIT_H_
/*****************************************************************************/

//
// clr.sln only defines _DEBUG
// The jit uses DEBUG rather than _DEBUG
// So we make sure that _DEBUG implies DEBUG
//
#ifdef _DEBUG
#ifndef DEBUG
#define DEBUG 1
#endif
#endif

// Clang-format messes with the indentation of comments if they directly precede an
// ifdef. This macro allows us to anchor the comments to the regular flow of code.
#define CLANG_FORMAT_COMMENT_ANCHOR ;

// Clang-tidy replaces 0 with nullptr in some templated functions, causing a build
// break. Replacing those instances with ZERO avoids this change
#define ZERO 0

#ifdef _MSC_VER
// These don't seem useful, so turning them off is no big deal
#pragma warning(disable : 4065) // "switch statement contains 'default' but no 'case' labels" (happens due to #ifdefs)
#pragma warning(disable : 4510) // can't generate default constructor
#pragma warning(disable : 4511) // can't generate copy constructor
#pragma warning(disable : 4512) // can't generate assignment constructor
#pragma warning(disable : 4610) // user defined constructor required
#pragma warning(disable : 4211) // nonstandard extension used (char name[0] in structs)
#pragma warning(disable : 4127) // conditional expression constant
#pragma warning(disable : 4201) // "nonstandard extension used : nameless struct/union"

// Depending on the code base, you may want to not disable these
#pragma warning(disable : 4245) // assigning signed / unsigned
#pragma warning(disable : 4146) // unary minus applied to unsigned

#pragma warning(disable : 4100) // unreferenced formal parameter
#pragma warning(disable : 4291) // new operator without delete (only in emitX86.cpp)
#endif

#ifdef _MSC_VER
#define CHECK_STRUCT_PADDING 0 // Set this to '1' to enable warning C4820 "'bytes' bytes padding added after
                               // construct 'member_name'" on interesting structs/classes
#else
#define CHECK_STRUCT_PADDING 0 // Never enable it for non-MSFT compilers
#endif

#if defined(_X86_)
#if defined(_ARM_)
#error Cannot define both _X86_ and _ARM_
#endif
#if defined(_AMD64_)
#error Cannot define both _X86_ and _AMD64_
#endif
#if defined(_ARM64_)
#error Cannot define both _X86_ and _ARM64_
#endif
#define _HOST_X86_
#elif defined(_AMD64_)
#if defined(_X86_)
#error Cannot define both _AMD64_ and _X86_
#endif
#if defined(_ARM_)
#error Cannot define both _AMD64_ and _ARM_
#endif
#if defined(_ARM64_)
#error Cannot define both _AMD64_ and _ARM64_
#endif
#define _HOST_AMD64_
#elif defined(_ARM_)
#if defined(_X86_)
#error Cannot define both _ARM_ and _X86_
#endif
#if defined(_AMD64_)
#error Cannot define both _ARM_ and _AMD64_
#endif
#if defined(_ARM64_)
#error Cannot define both _ARM_ and _ARM64_
#endif
#define _HOST_ARM_
#elif defined(_ARM64_)
#if defined(_X86_)
#error Cannot define both _ARM64_ and _X86_
#endif
#if defined(_AMD64_)
#error Cannot define both _ARM64_ and _AMD64_
#endif
#if defined(_ARM_)
#error Cannot define both _ARM64_ and _ARM_
#endif
#define _HOST_ARM64_
#else
#error Unsupported or unset host architecture
#endif

#if defined(_HOST_AMD64_) || defined(_HOST_ARM64_)
#define _HOST_64BIT_
#endif

#if defined(_TARGET_X86_)
#if defined(_TARGET_ARM_)
#error Cannot define both _TARGET_X86_ and _TARGET_ARM_
#endif
#if defined(_TARGET_AMD64_)
#error Cannot define both _TARGET_X86_ and _TARGET_AMD64_
#endif
#if defined(_TARGET_ARM64_)
#error Cannot define both _TARGET_X86_ and _TARGET_ARM64_
#endif
#if !defined(_HOST_X86_)
#define _CROSS_COMPILER_
#endif
#elif defined(_TARGET_AMD64_)
#if defined(_TARGET_X86_)
#error Cannot define both _TARGET_AMD64_ and _TARGET_X86_
#endif
#if defined(_TARGET_ARM_)
#error Cannot define both _TARGET_AMD64_ and _TARGET_ARM_
#endif
#if defined(_TARGET_ARM64_)
#error Cannot define both _TARGET_AMD64_ and _TARGET_ARM64_
#endif
#if !defined(_HOST_AMD64_)
#define _CROSS_COMPILER_
#endif
#elif defined(_TARGET_ARM_)
#if defined(_TARGET_X86_)
#error Cannot define both _TARGET_ARM_ and _TARGET_X86_
#endif
#if defined(_TARGET_AMD64_)
#error Cannot define both _TARGET_ARM_ and _TARGET_AMD64_
#endif
#if defined(_TARGET_ARM64_)
#error Cannot define both _TARGET_ARM_ and _TARGET_ARM64_
#endif
#if !defined(_HOST_ARM_)
#define _CROSS_COMPILER_
#endif
#elif defined(_TARGET_ARM64_)
#if defined(_TARGET_X86_)
#error Cannot define both _TARGET_ARM64_ and _TARGET_X86_
#endif
#if defined(_TARGET_AMD64_)
#error Cannot define both _TARGET_ARM64_ and _TARGET_AMD64_
#endif
#if defined(_TARGET_ARM_)
#error Cannot define both _TARGET_ARM64_ and _TARGET_ARM_
#endif
#if !defined(_HOST_ARM64_)
#define _CROSS_COMPILER_
#endif
#else
#error Unsupported or unset target architecture
#endif

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
#ifndef _TARGET_64BIT_
#define _TARGET_64BIT_
#endif // _TARGET_64BIT_
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

#ifdef _TARGET_64BIT_
#ifdef _TARGET_X86_
#error Cannot define both _TARGET_X86_ and _TARGET_64BIT_
#endif // _TARGET_X86_
#ifdef _TARGET_ARM_
#error Cannot define both _TARGET_ARM_ and _TARGET_64BIT_
#endif // _TARGET_ARM_
#endif // _TARGET_64BIT_

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
#define _TARGET_XARCH_
#endif

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#define _TARGET_ARMARCH_
#endif

// If the UNIX_AMD64_ABI is defined make sure that _TARGET_AMD64_ is also defined.
#if defined(UNIX_AMD64_ABI)
#if !defined(_TARGET_AMD64_)
#error When UNIX_AMD64_ABI is defined you must define _TARGET_AMD64_ defined as well.
#endif
#endif

// If the UNIX_X86_ABI is defined make sure that _TARGET_X86_ is also defined.
#if defined(UNIX_X86_ABI)
#if !defined(_TARGET_X86_)
#error When UNIX_X86_ABI is defined you must define _TARGET_X86_ defined as well.
#endif
#endif

#if defined(PLATFORM_UNIX)
#define _HOST_UNIX_
#endif

// Are we generating code to target Unix? This is true if we will run on Unix (_HOST_UNIX_ is defined).
// It's also true if we are building an altjit targetting Unix, which we determine by checking if either
// UNIX_AMD64_ABI or UNIX_X86_ABI is defined.
#if defined(_HOST_UNIX_) || ((defined(UNIX_AMD64_ABI) || defined(UNIX_X86_ABI)) && defined(ALT_JIT))
#define _TARGET_UNIX_
#endif

#ifndef _TARGET_UNIX_
#define _TARGET_WINDOWS_
#endif // !_TARGET_UNIX_

// --------------------------------------------------------------------------------
// IMAGE_FILE_MACHINE_TARGET
// --------------------------------------------------------------------------------

#if defined(_TARGET_X86_)
#define IMAGE_FILE_MACHINE_TARGET IMAGE_FILE_MACHINE_I386
#elif defined(_TARGET_AMD64_)
#define IMAGE_FILE_MACHINE_TARGET IMAGE_FILE_MACHINE_AMD64
#elif defined(_TARGET_ARM_)
#define IMAGE_FILE_MACHINE_TARGET IMAGE_FILE_MACHINE_ARMNT
#elif defined(_TARGET_ARM64_)
#define IMAGE_FILE_MACHINE_TARGET IMAGE_FILE_MACHINE_ARM64 // 0xAA64
#else
#error Unsupported or unset target architecture
#endif

// Include the AMD64 unwind codes when appropriate.
#if defined(_TARGET_AMD64_)
// We need to temporarily set PLATFORM_UNIX, if necessary, to get the Unix-specific unwind codes.
#if defined(_TARGET_UNIX_) && !defined(_HOST_UNIX_)
#define PLATFORM_UNIX
#endif
#include "win64unwind.h"
#if defined(_TARGET_UNIX_) && !defined(_HOST_UNIX_)
#undef PLATFORM_UNIX
#endif
#endif

#include "corhdr.h"
#include "corjit.h"
#include "jitee.h"

#define __OPERATOR_NEW_INLINE 1 // indicate that I will define these
#define __PLACEMENT_NEW_INLINE  // don't bring in the global placement new, it is easy to make a mistake
                                // with our new(compiler*) pattern.

#include "utilcode.h" // this defines assert as _ASSERTE
#include "host.h"     // this redefines assert for the JIT to use assertAbort
#include "utils.h"

#ifdef DEBUG
#define INDEBUG(x) x
#define INDEBUG_COMMA(x) x,
#define DEBUGARG(x) , x
#else
#define INDEBUG(x)
#define INDEBUG_COMMA(x)
#define DEBUGARG(x)
#endif

#if defined(DEBUG) || defined(LATE_DISASM)
#define INDEBUG_LDISASM_COMMA(x) x,
#else
#define INDEBUG_LDISASM_COMMA(x)
#endif

#if defined(UNIX_AMD64_ABI)
#define UNIX_AMD64_ABI_ONLY_ARG(x) , x
#define UNIX_AMD64_ABI_ONLY(x) x
#else // !defined(UNIX_AMD64_ABI)
#define UNIX_AMD64_ABI_ONLY_ARG(x)
#define UNIX_AMD64_ABI_ONLY(x)
#endif // defined(UNIX_AMD64_ABI)

#if defined(UNIX_AMD64_ABI) || !defined(_TARGET_64BIT_) || (defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_))
#define FEATURE_PUT_STRUCT_ARG_STK 1
#define PUT_STRUCT_ARG_STK_ONLY_ARG(x) , x
#define PUT_STRUCT_ARG_STK_ONLY(x) x
#else // !(defined(UNIX_AMD64_ABI) && defined(_TARGET_64BIT_) && !(defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_))
#define PUT_STRUCT_ARG_STK_ONLY_ARG(x)
#define PUT_STRUCT_ARG_STK_ONLY(x)
#endif // !(defined(UNIX_AMD64_ABI) && defined(_TARGET_64BIT_) && !(defined(_TARGET_WINDOWS_) &&
       // defined(_TARGET_ARM64_))

#if defined(UNIX_AMD64_ABI)
#define UNIX_AMD64_ABI_ONLY_ARG(x) , x
#define UNIX_AMD64_ABI_ONLY(x) x
#else // !defined(UNIX_AMD64_ABI)
#define UNIX_AMD64_ABI_ONLY_ARG(x)
#define UNIX_AMD64_ABI_ONLY(x)
#endif // defined(UNIX_AMD64_ABI)

#if defined(UNIX_AMD64_ABI) || defined(_TARGET_ARM64_)
#define MULTIREG_HAS_SECOND_GC_RET 1
#define MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(x) , x
#define MULTIREG_HAS_SECOND_GC_RET_ONLY(x) x
#else // !defined(UNIX_AMD64_ABI)
#define MULTIREG_HAS_SECOND_GC_RET 0
#define MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(x)
#define MULTIREG_HAS_SECOND_GC_RET_ONLY(x)
#endif // defined(UNIX_AMD64_ABI)

// Arm64 Windows supports FEATURE_ARG_SPLIT, note this is different from
// the official Arm64 ABI.
// Case: splitting 16 byte struct between x7 and stack
#if (defined(_TARGET_ARM_) || (defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)))
#define FEATURE_ARG_SPLIT 1
#else
#define FEATURE_ARG_SPLIT 0
#endif // (defined(_TARGET_ARM_) || (defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)))

// To get rid of warning 4701 : local variable may be used without being initialized
#define DUMMY_INIT(x) (x)

#define REGEN_SHORTCUTS 0
#define REGEN_CALLPAT 0

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          jit.h                                            XX
XX                                                                           XX
XX   Interface of the JIT with jit.cpp                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#if defined(DEBUG)
#include "log.h"

#define INFO6 LL_INFO10000   // Did Jit or Inline succeeded?
#define INFO7 LL_INFO100000  // NYI stuff
#define INFO8 LL_INFO1000000 // Weird failures
#define INFO9 LL_EVERYTHING  // Info about incoming settings
#define INFO10 LL_EVERYTHING // Totally verbose

#endif // DEBUG

typedef class ICorJitInfo* COMP_HANDLE;

const CORINFO_CLASS_HANDLE NO_CLASS_HANDLE = (CORINFO_CLASS_HANDLE) nullptr;

/*****************************************************************************/

inline bool False()
{
    return false;
} // Use to disable code while keeping prefast happy

// We define two IL offset types, as follows:
//
// IL_OFFSET:  either a distinguished value, or an IL offset.
// IL_OFFSETX: either a distinguished value, or the top two bits are a flags, and the remaining bottom
//             bits are a IL offset.
//
// In both cases, the set of legal distinguished values is:
//     BAD_IL_OFFSET             -- A unique illegal IL offset number. Note that it must be different from
//                                  the ICorDebugInfo values, below, and must also not be a legal IL offset.
//     ICorDebugInfo::NO_MAPPING -- The IL offset corresponds to no source code (such as EH step blocks).
//     ICorDebugInfo::PROLOG     -- The IL offset indicates a prolog
//     ICorDebugInfo::EPILOG     -- The IL offset indicates an epilog
//
// The IL offset must be in the range [0 .. 0x3fffffff]. This is because we steal
// the top two bits in IL_OFFSETX for flags, but we want the maximum range to be the same
// for both types. The IL value can't be larger than the maximum IL offset of the function
// being compiled.
//
// Blocks and statements never store one of the ICorDebugInfo values, even for IL_OFFSETX types. These are
// only stored in the IPmappingDsc struct, ipmdILoffsx field.

typedef unsigned IL_OFFSET;

const IL_OFFSET BAD_IL_OFFSET = 0x80000000;
const IL_OFFSET MAX_IL_OFFSET = 0x3fffffff;

typedef unsigned IL_OFFSETX;                                 // IL_OFFSET with stack-empty or call-instruction bit
const IL_OFFSETX IL_OFFSETX_STKBIT             = 0x80000000; // Note: this bit is set when the stack is NOT empty!
const IL_OFFSETX IL_OFFSETX_CALLINSTRUCTIONBIT = 0x40000000; // Set when the IL offset is for a call instruction.
const IL_OFFSETX IL_OFFSETX_BITS               = IL_OFFSETX_STKBIT | IL_OFFSETX_CALLINSTRUCTIONBIT;

IL_OFFSET jitGetILoffs(IL_OFFSETX offsx);
IL_OFFSET jitGetILoffsAny(IL_OFFSETX offsx);
bool jitIsStackEmpty(IL_OFFSETX offsx);
bool jitIsCallInstruction(IL_OFFSETX offsx);

const unsigned BAD_VAR_NUM = UINT_MAX;

// Code can't be more than 2^31 in any direction.  This is signed, so it should be used for anything that is
// relative to something else.
typedef int NATIVE_OFFSET;

// This is the same as the above, but it's used in absolute contexts (i.e. offset from the start).  Also,
// this is used for native code sizes.
typedef unsigned UNATIVE_OFFSET;

typedef ptrdiff_t ssize_t;

// For the following specially handled FIELD_HANDLES we need
//   values that are negative and have the low two bits zero
// See eeFindJitDataOffs and eeGetJitDataOffs in Compiler.hpp
#define FLD_GLOBAL_DS ((CORINFO_FIELD_HANDLE)-4)
#define FLD_GLOBAL_FS ((CORINFO_FIELD_HANDLE)-8)

/*****************************************************************************/

#include "vartype.h"

/*****************************************************************************/

// Late disassembly is OFF by default. Can be turned ON by
// adding /DLATE_DISASM=1 on the command line.
// Always OFF in the non-debug version

#if defined(LATE_DISASM) && (LATE_DISASM == 0)
#undef LATE_DISASM
#endif

/*****************************************************************************/

/*****************************************************************************/

#define FEATURE_VALNUM_CSE 1 // enable the Value Number CSE optimization logic

// true if Value Number CSE is enabled
#define FEATURE_ANYCSE FEATURE_VALNUM_CSE

#define CSE_INTO_HANDLERS 0

#define LARGE_EXPSET 1   // Track 64 or 32 assertions/copies/consts/rangechecks
#define ASSERTION_PROP 1 // Enable value/assertion propagation

#define LOCAL_ASSERTION_PROP ASSERTION_PROP // Enable local assertion propagation

//=============================================================================

#define OPT_BOOL_OPS 1 // optimize boolean operations

//=============================================================================

#define REDUNDANT_LOAD 1      // track locals in regs, suppress loads
#define DUMP_FLOWGRAPHS DEBUG // Support for creating Xml Flowgraph reports in *.fgx files

#define HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION 1 // if 1 we must have all handler entry points in the Hot code section

/*****************************************************************************/

#define VPTR_OFFS 0 // offset of vtable pointer from obj ptr

/*****************************************************************************/

#define DUMP_GC_TABLES DEBUG
#define VERIFY_GC_TABLES 0
#define REARRANGE_ADDS 1

#define FUNC_INFO_LOGGING 1 // Support dumping function info to a file. In retail, only NYIs, with no function name,
                            // are dumped.

/*****************************************************************************/
/*****************************************************************************/
/* Set these to 1 to collect and output various statistics about the JIT */

#define CALL_ARG_STATS 0      // Collect stats about calls and call arguments.
#define COUNT_BASIC_BLOCKS 0  // Create a histogram of basic block sizes, and a histogram of IL sizes in the simple
                              // case of single block methods.
#define COUNT_LOOPS 0         // Collect stats about loops, such as the total number of natural loops, a histogram of
                              // the number of loop exits, etc.
#define DATAFLOW_ITER 0       // Count iterations in lexical CSE and constant folding dataflow.
#define DISPLAY_SIZES 0       // Display generated code, data, and GC information sizes.
#define MEASURE_BLOCK_SIZE 0  // Collect stats about basic block and flowList node sizes and memory allocations.
#define MEASURE_FATAL 0       // Count the number of calls to fatal(), including NYIs and noway_asserts.
#define MEASURE_NODE_SIZE 0   // Collect stats about GenTree node allocations.
#define MEASURE_PTRTAB_SIZE 0 // Collect stats about GC pointer table allocations.
#define EMITTER_STATS 0       // Collect stats on the emitter.
#define NODEBASH_STATS 0      // Collect stats on changed gtOper values in GenTree's.
#define COUNT_AST_OPERS 0     // Display use counts for GenTree operators.

#define VERBOSE_SIZES 0  // Always display GC info sizes. If set, DISPLAY_SIZES must also be set.
#define VERBOSE_VERIFY 0 // Dump additional information when verifying code. Useful to debug verification bugs.

#ifdef DEBUG
#define MEASURE_MEM_ALLOC 1 // Collect memory allocation stats.
#define LOOP_HOIST_STATS 1  // Collect loop hoisting stats.
#define TRACK_LSRA_STATS 1  // Collect LSRA stats
#else
#define MEASURE_MEM_ALLOC 0 // You can set this to 1 to get memory stats in retail, as well
#define LOOP_HOIST_STATS 0  // You can set this to 1 to get loop hoist stats in retail, as well
#define TRACK_LSRA_STATS 0  // You can set this to 1 to get LSRA stats in retail, as well
#endif

// Timing calls to clr.dll is only available under certain conditions.
#ifndef FEATURE_JIT_METHOD_PERF
#define MEASURE_CLRAPI_CALLS 0 // Can't time these calls without METHOD_PERF.
#endif
#ifdef DEBUG
#define MEASURE_CLRAPI_CALLS 0 // No point in measuring DEBUG code.
#endif
#if !defined(_HOST_X86_) && !defined(_HOST_AMD64_)
#define MEASURE_CLRAPI_CALLS 0 // Cycle counters only hooked up on x86/x64.
#endif
#if !defined(_MSC_VER) && !defined(__GNUC__)
#define MEASURE_CLRAPI_CALLS 0 // Only know how to do this with VC and Clang.
#endif

// If none of the above set the flag to 0, it's available.
#ifndef MEASURE_CLRAPI_CALLS
#define MEASURE_CLRAPI_CALLS 0 // Set to 1 to measure time in ICorJitInfo calls.
#endif

/*****************************************************************************/
/* Portability Defines */
/*****************************************************************************/
#ifdef _TARGET_X86_
#define JIT32_GCENCODER
#endif

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/

#define DUMPER

#else // !DEBUG

#if DUMP_GC_TABLES
#pragma message("NOTE: this non-debug build has GC ptr table dumping always enabled!")
const bool dspGCtbls = true;
#endif

/*****************************************************************************/
#endif // !DEBUG

#ifdef DEBUG
#define JITDUMP(...)                                                                                                   \
    {                                                                                                                  \
        if (JitTls::GetCompiler()->verbose)                                                                            \
            logf(__VA_ARGS__);                                                                                         \
    }
#define JITLOG(x)                                                                                                      \
    {                                                                                                                  \
        JitLogEE x;                                                                                                    \
    }
#define JITLOG_THIS(t, x)                                                                                              \
    {                                                                                                                  \
        (t)->JitLogEE x;                                                                                               \
    }
#define DBEXEC(flg, expr)                                                                                              \
    if (flg)                                                                                                           \
    {                                                                                                                  \
        expr;                                                                                                          \
    }
#define DISPNODE(t)                                                                                                    \
    if (JitTls::GetCompiler()->verbose)                                                                                \
        JitTls::GetCompiler()->gtDispTree(t, nullptr, nullptr, true);
#define DISPTREE(t)                                                                                                    \
    if (JitTls::GetCompiler()->verbose)                                                                                \
        JitTls::GetCompiler()->gtDispTree(t);
#define DISPRANGE(range)                                                                                               \
    if (JitTls::GetCompiler()->verbose)                                                                                \
        JitTls::GetCompiler()->gtDispRange(range);
#define DISPTREERANGE(range, t)                                                                                        \
    if (JitTls::GetCompiler()->verbose)                                                                                \
        JitTls::GetCompiler()->gtDispTreeRange(range, t);
#define VERBOSE JitTls::GetCompiler()->verbose
#else // !DEBUG
#define JITDUMP(...)
#define JITLOG(x)
#define JITLOG_THIS(t, x)
#define DBEXEC(flg, expr)
#define DISPNODE(t)
#define DISPTREE(t)
#define DISPRANGE(range)
#define DISPTREERANGE(range, t)
#define VERBOSE 0
#endif // !DEBUG

/*****************************************************************************
 *
 * Double alignment. This aligns ESP to 0 mod 8 in function prolog, then uses ESP
 * to reference locals, EBP to reference parameters.
 * It only makes sense if frameless method support is on.
 * (frameless method support is now always on)
 */

#ifdef _TARGET_X86_
#define DOUBLE_ALIGN 1 // permit the double alignment of ESP in prolog,
                       //  and permit the double alignment of local offsets
#else
#define DOUBLE_ALIGN 0 // no special handling for double alignment
#endif

#ifdef DEBUG

// Forward declarations for UninitializedWord and IsUninitialized are needed by alloc.h
template <typename T>
inline T UninitializedWord(Compiler* comp);

template <typename T>
inline bool IsUninitialized(T data);

#endif // DEBUG

/*****************************************************************************/

enum accessLevel
{
    ACL_NONE,
    ACL_PRIVATE,
    ACL_DEFAULT,
    ACL_PROTECTED,
    ACL_PUBLIC,
};

/*****************************************************************************/

#define castto(var, typ) (*(typ*)&var)

#define sizeto(typ, mem) (offsetof(typ, mem) + sizeof(((typ*)0)->mem))

/*****************************************************************************/

#ifdef NO_MISALIGNED_ACCESS

#define MISALIGNED_RD_I2(src) (*castto(src, char*) | *castto(src + 1, char*) << 8)

#define MISALIGNED_RD_U2(src) (*castto(src, char*) | *castto(src + 1, char*) << 8)

#define MISALIGNED_WR_I2(dst, val)                                                                                     \
    *castto(dst, char*)     = val;                                                                                     \
    *castto(dst + 1, char*) = val >> 8;

#define MISALIGNED_WR_I4(dst, val)                                                                                     \
    *castto(dst, char*)     = val;                                                                                     \
    *castto(dst + 1, char*) = val >> 8;                                                                                \
    *castto(dst + 2, char*) = val >> 16;                                                                               \
    *castto(dst + 3, char*) = val >> 24;

#else

#define MISALIGNED_RD_I2(src) (*castto(src, short*))
#define MISALIGNED_RD_U2(src) (*castto(src, unsigned short*))

#define MISALIGNED_WR_I2(dst, val) *castto(dst, short*) = val;
#define MISALIGNED_WR_I4(dst, val) *castto(dst, int*)   = val;

#define MISALIGNED_WR_ST(dst, val) *castto(dst, ssize_t*) = val;

#endif

/*****************************************************************************/

inline size_t roundUp(size_t size, size_t mult = sizeof(size_t))
{
    assert(mult && ((mult & (mult - 1)) == 0)); // power of two test

    return (size + (mult - 1)) & ~(mult - 1);
}

inline size_t roundDn(size_t size, size_t mult = sizeof(size_t))
{
    assert(mult && ((mult & (mult - 1)) == 0)); // power of two test

    return (size) & ~(mult - 1);
}

#ifdef _HOST_64BIT_
inline unsigned int roundUp(unsigned size, unsigned mult)
{
    return (unsigned int)roundUp((size_t)size, (size_t)mult);
}

inline unsigned int roundDn(unsigned size, unsigned mult)
{
    return (unsigned int)roundDn((size_t)size, (size_t)mult);
}
#endif // _HOST_64BIT_

inline unsigned int unsigned_abs(int x)
{
    return ((unsigned int)abs(x));
}

#ifdef _TARGET_64BIT_
inline size_t unsigned_abs(ssize_t x)
{
    return ((size_t)abs(x));
}
#endif // _TARGET_64BIT_

/*****************************************************************************/

#if CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE || MEASURE_MEM_ALLOC

#define HISTOGRAM_MAX_SIZE_COUNT 64

class Histogram
{
public:
    Histogram(const unsigned* const sizeTable);

    void dump(FILE* output);
    void record(unsigned size);

private:
    unsigned              m_sizeCount;
    const unsigned* const m_sizeTable;
    unsigned              m_counts[HISTOGRAM_MAX_SIZE_COUNT];
};

#endif // CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE

/*****************************************************************************/
#ifdef ICECAP
#include "icapexp.h"
#include "icapctrl.h"
#endif

/*****************************************************************************/

#include "error.h"

/*****************************************************************************/

#if CHECK_STRUCT_PADDING
#pragma warning(push)
#pragma warning(default : 4820) // 'bytes' bytes padding added after construct 'member_name'
#endif                          // CHECK_STRUCT_PADDING
#include "alloc.h"
#include "target.h"

#if FEATURE_TAILCALL_OPT

#ifdef FEATURE_CORECLR
// CoreCLR - enable tail call opt for the following IL pattern
//
//     call someFunc
//     jmp/jcc RetBlock
//     ...
//  RetBlock:
//     ret
#define FEATURE_TAILCALL_OPT_SHARED_RETURN 1
#else
// Desktop: Keep this to zero as one of app-compat apps that is using GetCallingAssembly()
// has an issue turning this ON.
//
// Refer to TF: Bug: 824625 and its associated regression TF Bug: 1113265
#define FEATURE_TAILCALL_OPT_SHARED_RETURN 0
#endif // FEATURE_CORECLR

#else // !FEATURE_TAILCALL_OPT
#define FEATURE_TAILCALL_OPT_SHARED_RETURN 0
#endif // !FEATURE_TAILCALL_OPT

#define CLFLG_CODESIZE 0x00001
#define CLFLG_CODESPEED 0x00002
#define CLFLG_CSE 0x00004
#define CLFLG_REGVAR 0x00008
#define CLFLG_RNGCHKOPT 0x00010
#define CLFLG_DEADASGN 0x00020
#define CLFLG_CODEMOTION 0x00040
#define CLFLG_QMARK 0x00080
#define CLFLG_TREETRANS 0x00100
#define CLFLG_INLINING 0x00200
#define CLFLG_CONSTANTFOLD 0x00800

#if FEATURE_STRUCTPROMOTE
#define CLFLG_STRUCTPROMOTE 0x00400
#else
#define CLFLG_STRUCTPROMOTE 0x00000
#endif

#define CLFLG_MAXOPT                                                                                                   \
    (CLFLG_CSE | CLFLG_REGVAR | CLFLG_RNGCHKOPT | CLFLG_DEADASGN | CLFLG_CODEMOTION | CLFLG_QMARK | CLFLG_TREETRANS |  \
     CLFLG_INLINING | CLFLG_STRUCTPROMOTE | CLFLG_CONSTANTFOLD)

#define CLFLG_MINOPT (CLFLG_TREETRANS)

/*****************************************************************************/

extern void dumpILBytes(const BYTE* const codeAddr, unsigned codeSize, unsigned alignSize);

extern unsigned dumpSingleInstr(const BYTE* const codeAddr, IL_OFFSET offs, const char* prefix = nullptr);

extern void dumpILRange(const BYTE* const codeAddr, unsigned codeSize); // in bytes

/*****************************************************************************/

extern int jitNativeCode(CORINFO_METHOD_HANDLE methodHnd,
                         CORINFO_MODULE_HANDLE classHnd,
                         COMP_HANDLE           compHnd,
                         CORINFO_METHOD_INFO*  methodInfo,
                         void**                methodCodePtr,
                         ULONG*                methodCodeSize,
                         JitFlags*             compileFlags,
                         void*                 inlineInfoPtr);

#ifdef _HOST_64BIT_
const size_t INVALID_POINTER_VALUE = 0xFEEDFACEABADF00D;
#else
const size_t INVALID_POINTER_VALUE = 0xFEEDFACE;
#endif

// Constants for making sure size_t fit into smaller types.
const size_t MAX_USHORT_SIZE_T   = static_cast<size_t>(static_cast<unsigned short>(-1));
const size_t MAX_UNSIGNED_SIZE_T = static_cast<size_t>(static_cast<unsigned>(-1));

// These assume 2's complement...
const int MAX_SHORT_AS_INT = 32767;
const int MIN_SHORT_AS_INT = -32768;

/*****************************************************************************/

class Compiler;

class JitTls
{
#ifdef DEBUG
    Compiler* m_compiler;
    LogEnv    m_logEnv;
    JitTls*   m_next;
#endif

public:
    JitTls(ICorJitInfo* jitInfo);
    ~JitTls();

#ifdef DEBUG
    static LogEnv* GetLogEnv();
#endif

    static Compiler* GetCompiler();
    static void SetCompiler(Compiler* compiler);
};

#if defined(DEBUG)
//  Include the definition of Compiler for use by these template functions
//
#include "compiler.h"

//****************************************************************************
//
//  Returns a word filled with the JITs allocator default fill value.
//
template <typename T>
inline T UninitializedWord(Compiler* comp)
{
    unsigned char defaultFill = 0xdd;
    if (comp == nullptr)
    {
        comp = JitTls::GetCompiler();
    }
    defaultFill = Compiler::compGetJitDefaultFill(comp);
    assert(defaultFill <= 0xff);
    __int64 word = 0x0101010101010101LL * defaultFill;
    return (T)word;
}

//****************************************************************************
//
//  Tries to determine if this value is coming from uninitialized JIT memory
//    - Returns true if the value matches what we initialized the memory to.
//
//  Notes:
//    - Asserts that use this are assuming that the UninitializedWord value
//      isn't a legal value for 'data'.  Thus using a default fill value of
//      0x00 will often trigger such asserts.
//
template <typename T>
inline bool IsUninitialized(T data)
{
    return data == UninitializedWord<T>(JitTls::GetCompiler());
}

#pragma warning(push)
#pragma warning(disable : 4312)
//****************************************************************************
//
//  Debug template definitions for dspPtr, dspOffset
//    - Used to format pointer/offset values for diffable Disasm
//
template <typename T>
T dspPtr(T p)
{
    return (p == ZERO) ? ZERO : (JitTls::GetCompiler()->opts.dspDiffable ? T(0xD1FFAB1E) : p);
}

template <typename T>
T dspOffset(T o)
{
    return (o == ZERO) ? ZERO : (JitTls::GetCompiler()->opts.dspDiffable ? T(0xD1FFAB1E) : o);
}
#pragma warning(pop)

#else // !defined(DEBUG)

//****************************************************************************
//
//  Non-Debug template definitions for dspPtr, dspOffset
//    - This is a nop in non-Debug builds
//
template <typename T>
T dspPtr(T p)
{
    return p;
}

template <typename T>
T dspOffset(T o)
{
    return o;
}

#endif // !defined(DEBUG)

/*****************************************************************************/
#endif //_JIT_H_
/*****************************************************************************/
