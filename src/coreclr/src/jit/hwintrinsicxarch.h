// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _HW_INTRINSIC_XARCH_H_
#define _HW_INTRINSIC_XARCH_H_

#ifdef FEATURE_HW_INTRINSICS

enum HWIntrinsicCategory : unsigned int
{
    // Simple SIMD intrinsics
    // - take Vector128/256<T> parameters
    // - return a Vector128/256<T>
    // - the codegen of overloads can be determined by intrinsicID and base type of returned vector
    HW_Category_SimpleSIMD,

    // IMM intrinsics
    // - some SIMD intrinsics requires immediate value (i.e. imm8) to generate instruction
    HW_Category_IMM,

    // Scalar intrinsics
    // - operate over general purpose registers, like crc32, lzcnt, popcnt, etc.
    HW_Category_Scalar,

    // SIMD scalar
    // - operate over vector registers(XMM), but just compute on the first element
    HW_Category_SIMDScalar,

    // Memory access intrinsics
    // - e.g., Avx.Load, Avx.Store, Sse.LoadAligned
    HW_Category_MemoryLoad,
    HW_Category_MemoryStore,

    // Helper intrinsics
    // - do not directly correspond to a instruction, such as Avx.SetAllVector256
    HW_Category_Helper,

    // Special intrinsics
    // - have to be addressed specially
    HW_Category_Special
};

enum HWIntrinsicFlag : unsigned int
{
    HW_Flag_NoFlag = 0,

    // Commutative
    // - if a binary-op intrinsic is commutative (e.g., Add, Multiply), its op1 can be contained
    HW_Flag_Commutative = 0x1,

    // Full range IMM intrinsic
    // - the immediate value is valid on the full range of imm8 (0-255)
    HW_Flag_FullRangeIMM = 0x2,

    // NoCodeGen
    // - should be transformed in the compiler front-end, cannot reach CodeGen
    HW_Flag_NoCodeGen = 0x8,

    // Unfixed SIMD-size
    // - overloaded on multiple vector sizes (SIMD size in the table is unreliable)
    HW_Flag_UnfixedSIMDSize = 0x10,

    // Multi-instruction
    // - that one intrinsic can generate multiple instructions
    HW_Flag_MultiIns = 0x20,

    // NoContainment
    // the intrinsic cannot be handled by comtainment,
    // all the intrinsic that have explicit memory load/store semantics should have this flag
    HW_Flag_NoContainment = 0x40,

    // Copy Upper bits
    // some SIMD scalar intrinsics need the semantics of copying upper bits from the source operand
    HW_Flag_CopyUpperBits = 0x80,

    // Select base type using the first argument type
    HW_Flag_BaseTypeFromFirstArg = 0x100,

    // Indicates compFloatingPointUsed does not need to be set.
    HW_Flag_NoFloatingPointUsed = 0x200,

    // Maybe IMM
    // the intrinsic has either imm or Vector overloads
    HW_Flag_MaybeIMM = 0x400,

    // NoJmpTable IMM
    // the imm intrinsic does not need jumptable fallback when it gets non-const argument
    HW_Flag_NoJmpTableIMM = 0x800,

    // Select base type using the second argument type
    HW_Flag_BaseTypeFromSecondArg = 0x1000,

    // Special codegen
    // the intrinsics need special rules in CodeGen,
    // but may be table-driven in the front-end
    HW_Flag_SpecialCodeGen = 0x2000,

    // No Read/Modify/Write Semantics
    // the intrinsic doesn't have read/modify/write semantics in two/three-operand form.
    HW_Flag_NoRMWSemantics = 0x4000,

    // Special import
    // the intrinsics need special rules in importer,
    // but may be table-driven in the back-end
    HW_Flag_SpecialImport = 0x8000,

    // Maybe Memory Load/Store
    // - some intrinsics may have pointer overloads but without HW_Category_MemoryLoad/HW_Category_MemoryStore
    HW_Flag_MaybeMemoryLoad  = 0x10000,
    HW_Flag_MaybeMemoryStore = 0x20000,
};

struct HWIntrinsicInfo
{
    NamedIntrinsic      id;
    const char*         name;
    InstructionSet      isa;
    int                 ival;
    unsigned            simdSize;
    int                 numArgs;
    instruction         ins[10];
    HWIntrinsicCategory category;
    HWIntrinsicFlag     flags;

    static const HWIntrinsicInfo& lookup(NamedIntrinsic id);

    static NamedIntrinsic lookupId(Compiler*   comp,
                                   const char* className,
                                   const char* methodName,
                                   const char* enclosingClassName);
    static InstructionSet lookupIsa(const char* className, const char* enclosingClassName);

    static unsigned lookupSimdSize(Compiler* comp, NamedIntrinsic id, CORINFO_SIG_INFO* sig);

    static int lookupNumArgs(const GenTreeHWIntrinsic* node);
    static GenTree* lookupLastOp(const GenTreeHWIntrinsic* node);
    static bool isImmOp(NamedIntrinsic id, const GenTree* op);

    static int lookupImmUpperBound(NamedIntrinsic id);
    static bool isInImmRange(NamedIntrinsic id, int ival);
    static bool isAVX2GatherIntrinsic(NamedIntrinsic id);

    static bool isFullyImplementedIsa(InstructionSet isa);
    static bool isScalarIsa(InstructionSet isa);

    // Member lookup

    static NamedIntrinsic lookupId(NamedIntrinsic id)
    {
        return lookup(id).id;
    }

    static const char* lookupName(NamedIntrinsic id)
    {
        return lookup(id).name;
    }

    static InstructionSet lookupIsa(NamedIntrinsic id)
    {
        return lookup(id).isa;
    }

    static int lookupIval(NamedIntrinsic id)
    {
        return lookup(id).ival;
    }

    static unsigned lookupSimdSize(NamedIntrinsic id)
    {
        return lookup(id).simdSize;
    }

    static int lookupNumArgs(NamedIntrinsic id)
    {
        return lookup(id).numArgs;
    }

    static instruction lookupIns(NamedIntrinsic id, var_types type)
    {
        if ((type < TYP_BYTE) || (type > TYP_DOUBLE))
        {
            assert(!"Unexpected type");
            return INS_invalid;
        }
        return lookup(id).ins[type - TYP_BYTE];
    }

    static HWIntrinsicCategory lookupCategory(NamedIntrinsic id)
    {
        return lookup(id).category;
    }

    static HWIntrinsicFlag lookupFlags(NamedIntrinsic id)
    {
        return lookup(id).flags;
    }

    // Flags lookup

    static bool IsCommutative(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_Commutative) != 0;
    }

    static bool HasFullRangeImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_FullRangeIMM) != 0;
    }

    static bool RequiresCodegen(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoCodeGen) == 0;
    }

    static bool HasFixedSimdSize(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_UnfixedSIMDSize) == 0;
    }

    static bool GeneratesMultipleIns(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MultiIns) != 0;
    }

    static bool SupportsContainment(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoContainment) == 0;
    }

    static bool CopiesUpperBits(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_CopyUpperBits) != 0;
    }

    static bool BaseTypeFromFirstArg(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_BaseTypeFromFirstArg) != 0;
    }

    static bool IsFloatingPointUsed(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoFloatingPointUsed) == 0;
    }

    static bool MaybeImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MaybeIMM) != 0;
    }

    static bool MaybeMemoryLoad(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MaybeMemoryLoad) != 0;
    }

    static bool MaybeMemoryStore(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MaybeMemoryStore) != 0;
    }

    static bool NoJmpTableImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoJmpTableIMM) != 0;
    }

    static bool BaseTypeFromSecondArg(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_BaseTypeFromSecondArg) != 0;
    }

    static bool HasSpecialCodegen(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialCodeGen) != 0;
    }

    static bool HasRMWSemantics(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoRMWSemantics) == 0;
    }

    static bool HasSpecialImport(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialImport) != 0;
    }
};

#endif // FEATURE_HW_INTRINSICS

#endif // _HW_INTRINSIC_XARCH_H_
