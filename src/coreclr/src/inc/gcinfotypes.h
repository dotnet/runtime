// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __GCINFOTYPES_H__
#define __GCINFOTYPES_H__

// This file is included when building an "alt jit".  In that case, we are doing a cross-compile:
// we may be building the ARM jit on x86, for example.  We generally make that work by conditionalizing on
// a _TARGET_XXX_ variable that we explicitly set in the build, rather than the _XXX_ variable implicitly
// set by the compiler.  But this file is *also* included by the runtime, and needs in that case to be
// conditionalized by the actual platform we're compiling for.  We solve this by:
//    1) conditionalizing on _TARGET_XXX_ in this file,
//    2) having a _TARGET_SET_ variable so we know whether we're in a compilation for JIT in which some
//       _TARGET_XXX_ has already been set, and
//    3) if _TARGET_SET_ is not set, set the _TARGET_XXX_ variable appropriate for the current _XXX_.
// 
#ifndef _TARGET_SET_

//#ifdef _X86_
//#define _TARGET_X86_
//#endif

//#ifdef _AMD64_
//#define _TARGET_AMD64_
//#endif

//#ifdef _ARM_
//#define _TARGET_ARM_
//#endif

#endif // _TARGET_SET_

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#define PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
//
// The EH vector mechanism is not completely worked out, 
//   so it's temporarily disabled. We rely on fully-interruptible instead.
//
#define DISABLE_EH_VECTORS
#endif


#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#define FIXED_STACK_PARAMETER_SCRATCH_AREA
#endif

#define BITS_PER_SIZE_T ((int)sizeof(size_t)*8)


//--------------------------------------------------------------------------------
// It turns out, that ((size_t)x) << y == x, when y is not a literal 
//      and its value is BITS_PER_SIZE_T
// I guess the processor only shifts of the right operand modulo BITS_PER_SIZE_T
// In many cases, we want the above operation to yield 0, 
//      hence the following macros
//--------------------------------------------------------------------------------
__forceinline size_t SAFE_SHIFT_LEFT(size_t x, size_t count)
{
    _ASSERTE(count <= BITS_PER_SIZE_T);
    return (x << 1) << (count-1);
}
__forceinline size_t SAFE_SHIFT_RIGHT(size_t x, size_t count)
{
    _ASSERTE(count <= BITS_PER_SIZE_T);
    return (x >> 1) >> (count-1);
}

inline UINT32 CeilOfLog2(size_t x)
{
    _ASSERTE(x > 0);
    UINT32 result = (x & (x-1)) ? 1 : 0;
    while(x != 1)
    {
        result++;
        x >>= 1;
    }
    return result;
}

enum GcSlotFlags
{
    GC_SLOT_BASE                = 0x0,
    GC_SLOT_INTERIOR            = 0x1,
    GC_SLOT_PINNED              = 0x2,
    GC_SLOT_UNTRACKED           = 0x4,

    // For internal use by the encoder/decoder
    GC_SLOT_IS_REGISTER         = 0x8,
    GC_SLOT_IS_DELETED          = 0x10,
};

enum GcStackSlotBase
{
    GC_CALLER_SP_REL            = 0x0,
    GC_SP_REL                   = 0x1,
    GC_FRAMEREG_REL             = 0x2,

    GC_SPBASE_FIRST = GC_CALLER_SP_REL,
    GC_SPBASE_LAST = GC_FRAMEREG_REL,
};

#ifdef _DEBUG
const char* const GcStackSlotBaseNames[] =
{
    "caller.sp",
    "sp",
    "frame",
};
#endif


enum GcSlotState
{
    GC_SLOT_DEAD                = 0x0,
    GC_SLOT_LIVE                = 0x1,
};

struct GcStackSlot
{
    INT32 SpOffset;
    GcStackSlotBase Base;

    bool operator==(const GcStackSlot& other)
    {
        return ((SpOffset == other.SpOffset) && (Base == other.Base));
    }
    bool operator!=(const GcStackSlot& other)
    {
        return ((SpOffset != other.SpOffset) || (Base != other.Base));
    }
};

// Stack offsets must be 8-byte aligned, so we use this unaligned
//  offset to represent that the method doesn't have a security object
#define NO_SECURITY_OBJECT        (-1)
#define NO_GS_COOKIE              (-1)
#define NO_STACK_BASE_REGISTER    (0xffffffff)
#define NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA (0xffffffff)
#define NO_GENERICS_INST_CONTEXT  (-1)
#define NO_PSP_SYM                (-1)


#if defined(_TARGET_AMD64_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 8    // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>3)
#define DENORMALIZE_STACK_SLOT(x) ((x)<<3)
#define NORMALIZE_CODE_LENGTH(x) (x)
#define DENORMALIZE_CODE_LENGTH(x) (x)
// Encode RBP as 0
#define NORMALIZE_STACK_BASE_REGISTER(x) ((x) ^ 5)
#define DENORMALIZE_STACK_BASE_REGISTER(x) ((x) ^ 5)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>3)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<3)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)
#define DENORMALIZE_CODE_OFFSET(x) (x)
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 8
#define STACK_BASE_REGISTER_ENCBASE 3
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 4
#define NUM_REGISTERS_ENCBASE 2
#define NUM_STACK_SLOTS_ENCBASE 2
#define NUM_UNTRACKED_SLOTS_ENCBASE 1
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 6
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE 2
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 2
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#elif defined(_TARGET_ARM_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 4   // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>2)
#define DENORMALIZE_STACK_SLOT(x) ((x)<<2)
#define NORMALIZE_CODE_LENGTH(x) ((x)>>1)
#define DENORMALIZE_CODE_LENGTH(x) ((x)<<1)
// Encode R11 as zero
#define NORMALIZE_STACK_BASE_REGISTER(x) ((((x) - 4) & 7) ^ 7)
#define DENORMALIZE_STACK_BASE_REGISTER(x) (((x) ^ 7) + 4)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>2)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<2)
#define CODE_OFFSETS_NEED_NORMALIZATION 1
#define NORMALIZE_CODE_OFFSET(x) (x)   // Instructions are 2/4 bytes long in Thumb/ARM states, 
#define DENORMALIZE_CODE_OFFSET(x) (x) // but the safe-point offsets are encoded with a -1 adjustment.
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

// The choices of these encoding bases only affects space overhead
// and performance, not semantics/correctness.
#define PSP_SYM_STACK_SLOT_ENCBASE 5 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 5
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 5
#define GS_COOKIE_STACK_SLOT_ENCBASE 5
#define CODE_LENGTH_ENCBASE 7
#define STACK_BASE_REGISTER_ENCBASE 1
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 3
#define NUM_REGISTERS_ENCBASE 2
#define NUM_STACK_SLOTS_ENCBASE 3
#define NUM_UNTRACKED_SLOTS_ENCBASE 3
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 4
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 2
#define REGISTER_DELTA_ENCBASE 1
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 3
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 2
#define NUM_EH_CLAUSES_ENCBASE 3
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#elif defined(_TARGET_ARM64_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 8    // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>3)   // GC Pointers are 8-bytes aligned
#define DENORMALIZE_STACK_SLOT(x) ((x)<<3)
#define NORMALIZE_CODE_LENGTH(x) ((x)>>2)   // All Instructions are 4 bytes long
#define DENORMALIZE_CODE_LENGTH(x) ((x)<<2) 
#define NORMALIZE_STACK_BASE_REGISTER(x) ((x)^29) // Encode Frame pointer X29 as zero
#define DENORMALIZE_STACK_BASE_REGISTER(x) ((x)^29)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>3)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<3)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)   // Instructions are 4 bytes long, but the safe-point
#define DENORMALIZE_CODE_OFFSET(x) (x) // offsets are encoded with a -1 adjustment.
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 8
#define STACK_BASE_REGISTER_ENCBASE 2 // FP encoded as 0, SP as 2.
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 4
#define NUM_REGISTERS_ENCBASE 3
#define NUM_STACK_SLOTS_ENCBASE 2
#define NUM_UNTRACKED_SLOTS_ENCBASE 1
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 6
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE 2
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 3
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#else

#ifndef _TARGET_X86_
#ifdef PORTABILITY_WARNING
PORTABILITY_WARNING("Please specialize these definitions for your platform!")
#endif
#endif

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 4   // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) (x)
#define DENORMALIZE_STACK_SLOT(x) (x)
#define NORMALIZE_CODE_LENGTH(x) (x)
#define DENORMALIZE_CODE_LENGTH(x) (x)
#define NORMALIZE_STACK_BASE_REGISTER(x) (x)
#define DENORMALIZE_STACK_BASE_REGISTER(x) (x)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) (x)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) (x)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)
#define DENORMALIZE_CODE_OFFSET(x) (x)
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 6
#define STACK_BASE_REGISTER_ENCBASE 3
#define SIZE_OF_STACK_AREA_ENCBASE 6
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 3
#define NUM_REGISTERS_ENCBASE 3
#define NUM_STACK_SLOTS_ENCBASE 5
#define NUM_UNTRACKED_SLOTS_ENCBASE 5
#define NORM_PROLOG_SIZE_ENCBASE 4
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 5
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 5
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE REGISTER_ENCBASE
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 4
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#endif

#endif // !__GCINFOTYPES_H__

