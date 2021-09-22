// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef TARGET_H_
#define TARGET_H_

// Native Varargs are not supported on Unix (all architectures) and Windows ARM
#if defined(TARGET_WINDOWS) && !defined(TARGET_ARM)
#define FEATURE_VARARG 1
#else
#define FEATURE_VARARG 0
#endif

/*****************************************************************************/
// The following are human readable names for the target architectures
#if defined(TARGET_X86)
#define TARGET_READABLE_NAME "X86"
#elif defined(TARGET_AMD64)
#define TARGET_READABLE_NAME "AMD64"
#elif defined(TARGET_ARM)
#define TARGET_READABLE_NAME "ARM"
#elif defined(TARGET_ARM64)
#define TARGET_READABLE_NAME "ARM64"
#else
#error Unsupported or unset target architecture
#endif

/*****************************************************************************/
// The following are intended to capture only those #defines that cannot be replaced
// with static const members of Target
#if defined(TARGET_XARCH)
#define REGMASK_BITS 32
#define CSE_CONST_SHARED_LOW_BITS 16

#elif defined(TARGET_ARM)
#define REGMASK_BITS 64
#define CSE_CONST_SHARED_LOW_BITS 12

#elif defined(TARGET_ARM64)
#define REGMASK_BITS 64
#define CSE_CONST_SHARED_LOW_BITS 12

#else
#error Unsupported or unset target architecture
#endif

//------------------------------------------------------------------------
//
// Each register list in register.h must declare REG_STK as the last value.
// In the following enum declarations, the following REG_XXX are created beyond
// the "real" registers:
//    REG_STK          - Used to indicate something evaluated onto the stack.
//    ACTUAL_REG_COUNT - The number of physical registers. (same as REG_STK).
//    REG_COUNT        - The number of physical register + REG_STK. This is the count of values that may
//                       be assigned during register allocation.
//    REG_NA           - Used to indicate that a register is either not yet assigned or not required.
//
#if defined(TARGET_ARM)
enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned __int64
{
    RBM_NONE = 0,
#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_ARM64)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, xname, wname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned __int64
{
    RBM_NONE = 0,
#define REGDEF(name, rnum, mask, xname, wname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_AMD64)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned
{
    RBM_NONE = 0,

#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#elif defined(TARGET_X86)

enum _regNumber_enum : unsigned
{
#define REGDEF(name, rnum, mask, sname) REG_##name = rnum,
#define REGALIAS(alias, realname) REG_##alias = REG_##realname,
#include "register.h"

    REG_COUNT,
    REG_NA           = REG_COUNT,
    ACTUAL_REG_COUNT = REG_COUNT - 1 // everything but REG_STK (only real regs)
};

enum _regMask_enum : unsigned
{
    RBM_NONE = 0,

#define REGDEF(name, rnum, mask, sname) RBM_##name = mask,
#define REGALIAS(alias, realname) RBM_##alias = RBM_##realname,
#include "register.h"
};

#else
#error Unsupported target architecture
#endif

/*****************************************************************************/

// TODO-Cleanup: The types defined below are mildly confusing: why are there both?
// regMaskSmall is large enough to represent the entire set of registers.
// If regMaskSmall is smaller than a "natural" integer type, regMaskTP is wider, based
// on a belief by the original authors of the JIT that in some situations it is more
// efficient to have the wider representation.  This belief should be tested, and if it
// is false, then we should coalesce these two types into one (the Small width, probably).
// In any case, we believe that is OK to freely cast between these types; no information will
// be lost.

#ifdef TARGET_ARMARCH
typedef unsigned __int64 regMaskTP;
#else
typedef unsigned       regMaskTP;
#endif

#if REGMASK_BITS == 8
typedef unsigned char regMaskSmall;
#define REG_MASK_INT_FMT "%02X"
#define REG_MASK_ALL_FMT "%02X"
#elif REGMASK_BITS == 16
typedef unsigned short regMaskSmall;
#define REG_MASK_INT_FMT "%04X"
#define REG_MASK_ALL_FMT "%04X"
#elif REGMASK_BITS == 32
typedef unsigned regMaskSmall;
#define REG_MASK_INT_FMT "%08X"
#define REG_MASK_ALL_FMT "%08X"
#else
typedef unsigned __int64 regMaskSmall;
#define REG_MASK_INT_FMT "%04llX"
#define REG_MASK_ALL_FMT "%016llX"
#endif

typedef _regNumber_enum regNumber;
typedef unsigned char   regNumberSmall;

/*****************************************************************************/

#define LEA_AVAILABLE 1
#define SCALED_ADDR_MODES 1

/*****************************************************************************/

#ifdef DEBUG
#define DSP_SRC_OPER_LEFT 0
#define DSP_SRC_OPER_RIGHT 1
#define DSP_DST_OPER_LEFT 1
#define DSP_DST_OPER_RIGHT 0
#endif

/*****************************************************************************/

// The pseudorandom nop insertion is not necessary for current scenarios
// #define PSEUDORANDOM_NOP_INSERTION

/*****************************************************************************/

// clang-format off
#if defined(TARGET_X86)
#include "targetx86.h"
#elif defined(TARGET_AMD64)
#include "targetamd64.h"
#elif defined(TARGET_ARM)
#include "targetarm.h"
#elif defined(TARGET_ARM64)
#include "targetarm64.h"
#else
  #error Unsupported or unset target architecture
#endif

#ifdef TARGET_XARCH

  #define JMP_DIST_SMALL_MAX_NEG  (-128)
  #define JMP_DIST_SMALL_MAX_POS  (+127)

  #define JCC_DIST_SMALL_MAX_NEG  (-128)
  #define JCC_DIST_SMALL_MAX_POS  (+127)

  #define JMP_SIZE_SMALL          (2)
  #define JMP_SIZE_LARGE          (5)

  #define JCC_SIZE_SMALL          (2)
  #define JCC_SIZE_LARGE          (6)

  #define PUSH_INST_SIZE          (5)
  #define CALL_INST_SIZE          (5)

#endif // TARGET_XARCH

C_ASSERT(REG_FIRST == 0);
C_ASSERT(REG_INT_FIRST < REG_INT_LAST);
C_ASSERT(REG_FP_FIRST  < REG_FP_LAST);

// Opportunistic tail call feature converts non-tail prefixed calls into
// tail calls where possible. It requires fast tail calling mechanism for
// performance. Otherwise, we are better off not converting non-tail prefixed
// calls into tail calls.
C_ASSERT((FEATURE_TAILCALL_OPT == 0) || (FEATURE_FASTTAILCALL == 1));

/*****************************************************************************/

#define BITS_PER_BYTE              8
#define RBM_ALL(type) (varTypeUsesFloatReg(type) ? RBM_ALLFLOAT : RBM_ALLINT)

/*****************************************************************************/

#if CPU_HAS_BYTE_REGS
  #define RBM_BYTE_REGS           (RBM_EAX|RBM_ECX|RBM_EDX|RBM_EBX)
  #define BYTE_REG_COUNT          4
  #define RBM_NON_BYTE_REGS       (RBM_ESI|RBM_EDI)
#else
  #define RBM_BYTE_REGS            RBM_ALLINT
  #define RBM_NON_BYTE_REGS        RBM_NONE
#endif
// clang-format on

/*****************************************************************************/
class Target
{
public:
    static const char* g_tgtCPUName;
    static const char* g_tgtPlatformName;

    enum ArgOrder
    {
        ARG_ORDER_R2L,
        ARG_ORDER_L2R
    };
    static const enum ArgOrder g_tgtArgOrder;
    static const enum ArgOrder g_tgtUnmanagedArgOrder;
};

#if defined(DEBUG) || defined(LATE_DISASM) || DUMP_GC_TABLES
const char* getRegName(unsigned reg); // this is for gcencode.cpp and disasm.cpp that don't use the regNumber type
const char* getRegName(regNumber reg);
#endif // defined(DEBUG) || defined(LATE_DISASM) || DUMP_GC_TABLES

#ifdef DEBUG
const char* getRegNameFloat(regNumber reg, var_types type);
extern void dspRegMask(regMaskTP regMask, size_t minSiz = 0);
#endif

#if CPU_HAS_BYTE_REGS
inline bool isByteReg(regNumber reg)
{
    return (reg <= REG_EBX);
}
#else
inline bool isByteReg(regNumber reg)
{
    return true;
}
#endif

inline regMaskTP genRegMask(regNumber reg);
inline regMaskTP genRegMaskFloat(regNumber reg, var_types type = TYP_DOUBLE);

/*****************************************************************************
 * Return true if the register number is valid
 */
inline bool genIsValidReg(regNumber reg)
{
    /* It's safest to perform an unsigned comparison in case reg is negative */
    return ((unsigned)reg < (unsigned)REG_COUNT);
}

/*****************************************************************************
 * Return true if the register is a valid integer register
 */
inline bool genIsValidIntReg(regNumber reg)
{
    return reg >= REG_INT_FIRST && reg <= REG_INT_LAST;
}

/*****************************************************************************
 * Return true if the register is a valid floating point register
 */
inline bool genIsValidFloatReg(regNumber reg)
{
    return reg >= REG_FP_FIRST && reg <= REG_FP_LAST;
}

#ifdef TARGET_ARM

/*****************************************************************************
 * Return true if the register is a valid floating point double register
 */
inline bool genIsValidDoubleReg(regNumber reg)
{
    return genIsValidFloatReg(reg) && (((reg - REG_FP_FIRST) & 0x1) == 0);
}

#endif // TARGET_ARM

//-------------------------------------------------------------------------------------------
// hasFixedRetBuffReg:
//     Returns true if our target architecture uses a fixed return buffer register
//
inline bool hasFixedRetBuffReg()
{
#ifdef TARGET_ARM64
    return true;
#else
    return false;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffReg:
//     Returns the regNumber to use for the fixed return buffer
//
inline regNumber theFixedRetBuffReg()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return REG_ARG_RET_BUFF;
#else
    return REG_NA;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffMask:
//     Returns the regNumber to use for the fixed return buffer
//
inline regMaskTP theFixedRetBuffMask()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return RBM_ARG_RET_BUFF;
#else
    return 0;
#endif
}

//-------------------------------------------------------------------------------------------
// theFixedRetBuffArgNum:
//     Returns the argNum to use for the fixed return buffer
//
inline unsigned theFixedRetBuffArgNum()
{
    assert(hasFixedRetBuffReg()); // This predicate should be checked before calling this method
#ifdef TARGET_ARM64
    return RET_BUFF_ARGNUM;
#else
    return BAD_VAR_NUM;
#endif
}

//-------------------------------------------------------------------------------------------
// fullIntArgRegMask:
//     Returns the full mask of all possible integer registers
//     Note this includes the fixed return buffer register on Arm64
//
inline regMaskTP fullIntArgRegMask()
{
    if (hasFixedRetBuffReg())
    {
        return RBM_ARG_REGS | theFixedRetBuffMask();
    }
    else
    {
        return RBM_ARG_REGS;
    }
}

//-------------------------------------------------------------------------------------------
// isValidIntArgReg:
//     Returns true if the register is a valid integer argument register
//     Note this method also returns true on Arm64 when 'reg' is the RetBuff register
//
inline bool isValidIntArgReg(regNumber reg)
{
    return (genRegMask(reg) & fullIntArgRegMask()) != 0;
}

//-------------------------------------------------------------------------------------------
// genRegArgNext:
//     Given a register that is an integer or floating point argument register
//     returns the next argument register
//
regNumber genRegArgNext(regNumber argReg);

//-------------------------------------------------------------------------------------------
// isValidFloatArgReg:
//     Returns true if the register is a valid floating-point argument register
//
inline bool isValidFloatArgReg(regNumber reg)
{
    if (reg == REG_NA)
    {
        return false;
    }
    else
    {
        return (reg >= FIRST_FP_ARGREG) && (reg <= LAST_FP_ARGREG);
    }
}

/*****************************************************************************
 *
 *  Can the register hold the argument type?
 */

#ifdef TARGET_ARM
inline bool floatRegCanHoldType(regNumber reg, var_types type)
{
    assert(genIsValidFloatReg(reg));
    if (type == TYP_DOUBLE)
    {
        return ((reg - REG_F0) % 2) == 0;
    }
    else
    {
        // Can be TYP_STRUCT for HFA. It's not clear that's correct; what about
        // HFA of double? We wouldn't be asserting the right alignment, and
        // callers like genRegMaskFloat() wouldn't be generating the right mask.

        assert((type == TYP_FLOAT) || (type == TYP_STRUCT));
        return true;
    }
}
#else
// AMD64: xmm registers can hold any float type
// x86: FP stack can hold any float type
// ARM64: Floating-point/SIMD registers can hold any type.
inline bool floatRegCanHoldType(regNumber reg, var_types type)
{
    return true;
}
#endif

/*****************************************************************************
 *
 *  Map a register number to a register mask.
 */

extern const regMaskSmall regMasks[REG_COUNT];

inline regMaskTP genRegMask(regNumber reg)
{
    assert((unsigned)reg < ArrLen(regMasks));
#ifdef TARGET_AMD64
    // shift is faster than a L1 hit on modern x86
    // (L1 latency on sandy bridge is 4 cycles for [base] and 5 for [base + index*c] )
    // the reason this is AMD-only is because the x86 BE will try to get reg masks for REG_STK
    // and the result needs to be zero.
    regMaskTP result = 1 << reg;
    assert(result == regMasks[reg]);
    return result;
#else
    return regMasks[reg];
#endif
}

/*****************************************************************************
 *
 *  Map a register number to a floating-point register mask.
 */

inline regMaskTP genRegMaskFloat(regNumber reg, var_types type /* = TYP_DOUBLE */)
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_X86)
    assert(genIsValidFloatReg(reg));
    assert((unsigned)reg < ArrLen(regMasks));
    return regMasks[reg];
#elif defined(TARGET_ARM)
    assert(floatRegCanHoldType(reg, type));
    assert(reg >= REG_F0 && reg <= REG_F31);

    if (type == TYP_DOUBLE)
    {
        return regMasks[reg] | regMasks[reg + 1];
    }
    else
    {
        return regMasks[reg];
    }
#else
#error Unsupported or unset target architecture
#endif
}

//------------------------------------------------------------------------
// genRegMask: Given a register, and its type, generate the appropriate regMask
//
// Arguments:
//    regNum   - the register of interest
//    type     - the type of regNum (i.e. the type it is being used as)
//
// Return Value:
//    This will usually return the same value as genRegMask(regNum), but
//    on architectures where multiple registers are used for certain types
//    (e.g. TYP_DOUBLE on ARM), it will return a regMask that includes
//    all the registers.
//    Registers that are used in pairs, but separately named (e.g. TYP_LONG
//    on ARM) will return just the regMask for the given register.
//
// Assumptions:
//    For registers that are used in pairs, the caller will be handling
//    each member of the pair separately.
//
inline regMaskTP genRegMask(regNumber regNum, var_types type)
{
#ifndef TARGET_ARM
    return genRegMask(regNum);
#else
    regMaskTP regMask = RBM_NONE;

    if (varTypeUsesFloatReg(type))
    {
        regMask = genRegMaskFloat(regNum, type);
    }
    else
    {
        regMask = genRegMask(regNum);
    }
    return regMask;
#endif
}

/*****************************************************************************
 *
 *  These arrays list the callee-saved register numbers (and bitmaps, respectively) for
 *  the current architecture.
 */
extern const regNumber raRegCalleeSaveOrder[CNT_CALLEE_SAVED];
extern const regMaskTP raRbmCalleeSaveOrder[CNT_CALLEE_SAVED];

// This method takes a "compact" bitset of the callee-saved registers, and "expands" it to a full register mask.
regMaskSmall genRegMaskFromCalleeSavedMask(unsigned short);

/*****************************************************************************
 *
 *  Assumes that "reg" is of the given "type". Return the next unused reg number after "reg"
 *  of this type, else REG_NA if there are no more.
 */

inline regNumber regNextOfType(regNumber reg, var_types type)
{
    regNumber regReturn;

#ifdef TARGET_ARM
    if (type == TYP_DOUBLE)
    {
        // Skip odd FP registers for double-precision types
        assert(floatRegCanHoldType(reg, type));
        regReturn = regNumber(reg + 2);
    }
    else
    {
        regReturn = REG_NEXT(reg);
    }
#else // TARGET_ARM
    regReturn = REG_NEXT(reg);
#endif

    if (varTypeUsesFloatReg(type))
    {
        if (regReturn > REG_FP_LAST)
        {
            regReturn = REG_NA;
        }
    }
    else
    {
        if (regReturn > REG_INT_LAST)
        {
            regReturn = REG_NA;
        }
    }

    return regReturn;
}

/*****************************************************************************
 *
 *  Type checks
 */

inline bool isFloatRegType(var_types type)
{
    return varTypeUsesFloatReg(type);
}

// If the WINDOWS_AMD64_ABI is defined make sure that TARGET_AMD64 is also defined.
#if defined(WINDOWS_AMD64_ABI)
#if !defined(TARGET_AMD64)
#error When WINDOWS_AMD64_ABI is defined you must define TARGET_AMD64 defined as well.
#endif
#endif

/*****************************************************************************/
// Some sanity checks on some of the register masks
// Stack pointer is never part of RBM_ALLINT
C_ASSERT((RBM_ALLINT & RBM_SPBASE) == RBM_NONE);
C_ASSERT((RBM_INT_CALLEE_SAVED & RBM_SPBASE) == RBM_NONE);

#if ETW_EBP_FRAMED
// Frame pointer isn't either if we're supporting ETW frame chaining
C_ASSERT((RBM_ALLINT & RBM_FPBASE) == RBM_NONE);
C_ASSERT((RBM_INT_CALLEE_SAVED & RBM_FPBASE) == RBM_NONE);
#endif
/*****************************************************************************/

#ifdef TARGET_64BIT
typedef unsigned __int64 target_size_t;
typedef __int64          target_ssize_t;
#define TARGET_SIGN_BIT (1ULL << 63)

#else // !TARGET_64BIT
typedef unsigned int   target_size_t;
typedef int            target_ssize_t;
#define TARGET_SIGN_BIT (1ULL << 31)

#endif // !TARGET_64BIT

C_ASSERT(sizeof(target_size_t) == TARGET_POINTER_SIZE);
C_ASSERT(sizeof(target_ssize_t) == TARGET_POINTER_SIZE);

#if defined(TARGET_X86)
// instrDescCns holds constant values for the emitter. The X86 compiler is unique in that it
// may represent relocated pointer values with these constants. On the 64bit to 32 bit
// cross-targetting jit, the the constant value must be represented as a 64bit value in order
// to represent these pointers.
typedef ssize_t cnsval_ssize_t;
typedef size_t  cnsval_size_t;
#else
typedef target_ssize_t cnsval_ssize_t;
typedef target_size_t  cnsval_size_t;
#endif

/*****************************************************************************/
#endif // TARGET_H_
/*****************************************************************************/
