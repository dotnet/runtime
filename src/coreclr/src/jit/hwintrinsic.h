// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _HW_INTRINSIC_H_
#define _HW_INTRINSIC_H_

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
    HW_Flag_NoCodeGen = 0x4,

    // Multi-instruction
    // - that one intrinsic can generate multiple instructions
    HW_Flag_MultiIns = 0x8,

    // Select base type using the first argument type
    HW_Flag_BaseTypeFromFirstArg = 0x10,

    // Select base type using the second argument type
    HW_Flag_BaseTypeFromSecondArg = 0x20,

    // Indicates compFloatingPointUsed does not need to be set.
    HW_Flag_NoFloatingPointUsed = 0x40,

    // Maybe IMM
    // the intrinsic has either imm or Vector overloads
    HW_Flag_MaybeIMM = 0x80,

    // NoJmpTable IMM
    // the imm intrinsic does not need jumptable fallback when it gets non-const argument
    HW_Flag_NoJmpTableIMM = 0x100,

    // Special codegen
    // the intrinsics need special rules in CodeGen,
    // but may be table-driven in the front-end
    HW_Flag_SpecialCodeGen = 0x200,

    // Special import
    // the intrinsics need special rules in importer,
    // but may be table-driven in the back-end
    HW_Flag_SpecialImport = 0x400,

// The below is for defining platform-specific flags
#if defined(TARGET_XARCH)
    // Copy Upper bits
    // some SIMD scalar intrinsics need the semantics of copying upper bits from the source operand
    HW_Flag_CopyUpperBits = 0x800,

    // Maybe Memory Load/Store
    // - some intrinsics may have pointer overloads but without HW_Category_MemoryLoad/HW_Category_MemoryStore
    HW_Flag_MaybeMemoryLoad  = 0x1000,
    HW_Flag_MaybeMemoryStore = 0x2000,

    // No Read/Modify/Write Semantics
    // the intrinsic doesn't have read/modify/write semantics in two/three-operand form.
    HW_Flag_NoRMWSemantics = 0x4000,

    // NoContainment
    // the intrinsic cannot be handled by containment,
    // all the intrinsic that have explicit memory load/store semantics should have this flag
    HW_Flag_NoContainment = 0x8000,

#elif defined(TARGET_ARM64)
    // The intrinsic has read/modify/write semantics in multiple-operands form.
    HW_Flag_HasRMWSemantics = 0x800,

    // The intrinsic supports some sort of containment analysis.
    HW_Flag_SupportsContainment = 0x1000,
#else
#error Unsupported platform
#endif
};

#if defined(TARGET_XARCH)
// This mirrors the System.Runtime.Intrinsics.X86.FloatComparisonMode enumeration
enum class FloatComparisonMode : unsigned char
{
    // _CMP_EQ_OQ
    OrderedEqualNonSignaling = 0,

    // _CMP_LT_OS
    OrderedLessThanSignaling = 1,

    // _CMP_LE_OS
    OrderedLessThanOrEqualSignaling = 2,

    // _CMP_UNORD_Q
    UnorderedNonSignaling = 3,

    // _CMP_NEQ_UQ
    UnorderedNotEqualNonSignaling = 4,

    // _CMP_NLT_US
    UnorderedNotLessThanSignaling = 5,

    // _CMP_NLE_US
    UnorderedNotLessThanOrEqualSignaling = 6,

    // _CMP_ORD_Q
    OrderedNonSignaling = 7,

    // _CMP_EQ_UQ
    UnorderedEqualNonSignaling = 8,

    // _CMP_NGE_US
    UnorderedNotGreaterThanOrEqualSignaling = 9,

    // _CMP_NGT_US
    UnorderedNotGreaterThanSignaling = 10,

    // _CMP_FALSE_OQ
    OrderedFalseNonSignaling = 11,

    // _CMP_NEQ_OQ
    OrderedNotEqualNonSignaling = 12,

    // _CMP_GE_OS
    OrderedGreaterThanOrEqualSignaling = 13,

    // _CMP_GT_OS
    OrderedGreaterThanSignaling = 14,

    // _CMP_TRUE_UQ
    UnorderedTrueNonSignaling = 15,

    // _CMP_EQ_OS
    OrderedEqualSignaling = 16,

    // _CMP_LT_OQ
    OrderedLessThanNonSignaling = 17,

    // _CMP_LE_OQ
    OrderedLessThanOrEqualNonSignaling = 18,

    // _CMP_UNORD_S
    UnorderedSignaling = 19,

    // _CMP_NEQ_US
    UnorderedNotEqualSignaling = 20,

    // _CMP_NLT_UQ
    UnorderedNotLessThanNonSignaling = 21,

    // _CMP_NLE_UQ
    UnorderedNotLessThanOrEqualNonSignaling = 22,

    // _CMP_ORD_S
    OrderedSignaling = 23,

    // _CMP_EQ_US
    UnorderedEqualSignaling = 24,

    // _CMP_NGE_UQ
    UnorderedNotGreaterThanOrEqualNonSignaling = 25,

    // _CMP_NGT_UQ
    UnorderedNotGreaterThanNonSignaling = 26,

    // _CMP_FALSE_OS
    OrderedFalseSignaling = 27,

    // _CMP_NEQ_OS
    OrderedNotEqualSignaling = 28,

    // _CMP_GE_OQ
    OrderedGreaterThanOrEqualNonSignaling = 29,

    // _CMP_GT_OQ
    OrderedGreaterThanNonSignaling = 30,

    // _CMP_TRUE_US
    UnorderedTrueSignaling = 31,
};

enum class FloatRoundingMode : unsigned char
{
    // _MM_FROUND_TO_NEAREST_INT
    ToNearestInteger = 0x00,

    // _MM_FROUND_TO_NEG_INF
    ToNegativeInfinity = 0x01,

    // _MM_FROUND_TO_POS_INF
    ToPositiveInfinity = 0x02,

    // _MM_FROUND_TO_ZERO
    ToZero = 0x03,

    // _MM_FROUND_CUR_DIRECTION
    CurrentDirection = 0x04,

    // _MM_FROUND_RAISE_EXC
    RaiseException = 0x00,

    // _MM_FROUND_NO_EXC
    NoException = 0x08,
};
#endif // TARGET_XARCH

struct HWIntrinsicInfo
{
    NamedIntrinsic         id;
    const char*            name;
    CORINFO_InstructionSet isa;
    int                    simdSize;
    int                    numArgs;
    instruction            ins[10];
    HWIntrinsicCategory    category;
    HWIntrinsicFlag        flags;

    static const HWIntrinsicInfo& lookup(NamedIntrinsic id);

    static NamedIntrinsic lookupId(Compiler*         comp,
                                   CORINFO_SIG_INFO* sig,
                                   const char*       className,
                                   const char*       methodName,
                                   const char*       enclosingClassName);
    static CORINFO_InstructionSet lookupIsa(const char* className, const char* enclosingClassName);

    static unsigned lookupSimdSize(Compiler* comp, NamedIntrinsic id, CORINFO_SIG_INFO* sig);
    static int lookupNumArgs(const GenTreeHWIntrinsic* node);
    static GenTree* lookupLastOp(const GenTreeHWIntrinsic* node);

#if defined(TARGET_XARCH)
    static int lookupImmUpperBound(NamedIntrinsic intrinsic);
#elif defined(TARGET_ARM64)
    static void lookupImmBounds(
        NamedIntrinsic intrinsic, int simdSize, var_types baseType, int* lowerBound, int* upperBound);
#else
#error Unsupported platform
#endif

    static bool isInImmRange(NamedIntrinsic id, int ival, int simdSize, var_types baseType);
    static bool isImmOp(NamedIntrinsic id, const GenTree* op);
    static bool isFullyImplementedIsa(CORINFO_InstructionSet isa);
    static bool isScalarIsa(CORINFO_InstructionSet isa);

#ifdef TARGET_XARCH
    static bool isAVX2GatherIntrinsic(NamedIntrinsic id);
    static FloatComparisonMode lookupFloatComparisonModeForSwappedArgs(FloatComparisonMode comparison);
#endif

    // Member lookup

    static NamedIntrinsic lookupId(NamedIntrinsic id)
    {
        return lookup(id).id;
    }

    static const char* lookupName(NamedIntrinsic id)
    {
        return lookup(id).name;
    }

    static CORINFO_InstructionSet lookupIsa(NamedIntrinsic id)
    {
        return lookup(id).isa;
    }

#ifdef TARGET_XARCH
    static int lookupIval(NamedIntrinsic id, bool opportunisticallyDependsOnAVX)
    {
        switch (id)
        {
            case NI_SSE_CompareEqual:
            case NI_SSE_CompareScalarEqual:
            case NI_SSE2_CompareEqual:
            case NI_SSE2_CompareScalarEqual:
            case NI_AVX_CompareEqual:
            {
                return static_cast<int>(FloatComparisonMode::OrderedEqualNonSignaling);
            }

            case NI_SSE_CompareGreaterThan:
            case NI_SSE_CompareScalarGreaterThan:
            case NI_SSE2_CompareGreaterThan:
            case NI_SSE2_CompareScalarGreaterThan:
            case NI_AVX_CompareGreaterThan:
            {
                if (opportunisticallyDependsOnAVX)
                {
                    return static_cast<int>(FloatComparisonMode::OrderedGreaterThanSignaling);
                }

                // CompareGreaterThan is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareGreaterThan);
                return static_cast<int>(FloatComparisonMode::OrderedLessThanSignaling);
            }

            case NI_SSE_CompareLessThan:
            case NI_SSE_CompareScalarLessThan:
            case NI_SSE2_CompareLessThan:
            case NI_SSE2_CompareScalarLessThan:
            case NI_AVX_CompareLessThan:
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanSignaling);
            }

            case NI_SSE_CompareGreaterThanOrEqual:
            case NI_SSE_CompareScalarGreaterThanOrEqual:
            case NI_SSE2_CompareGreaterThanOrEqual:
            case NI_SSE2_CompareScalarGreaterThanOrEqual:
            case NI_AVX_CompareGreaterThanOrEqual:
            {
                if (opportunisticallyDependsOnAVX)
                {
                    return static_cast<int>(FloatComparisonMode::OrderedGreaterThanOrEqualSignaling);
                }

                // CompareGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareGreaterThanOrEqual);
                return static_cast<int>(FloatComparisonMode::OrderedLessThanOrEqualSignaling);
            }

            case NI_SSE_CompareLessThanOrEqual:
            case NI_SSE_CompareScalarLessThanOrEqual:
            case NI_SSE2_CompareLessThanOrEqual:
            case NI_SSE2_CompareScalarLessThanOrEqual:
            case NI_AVX_CompareLessThanOrEqual:
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanOrEqualSignaling);
            }

            case NI_SSE_CompareNotEqual:
            case NI_SSE_CompareScalarNotEqual:
            case NI_SSE2_CompareNotEqual:
            case NI_SSE2_CompareScalarNotEqual:
            case NI_AVX_CompareNotEqual:
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotEqualNonSignaling);
            }

            case NI_SSE_CompareNotGreaterThan:
            case NI_SSE_CompareScalarNotGreaterThan:
            case NI_SSE2_CompareNotGreaterThan:
            case NI_SSE2_CompareScalarNotGreaterThan:
            case NI_AVX_CompareNotGreaterThan:
            {
                if (opportunisticallyDependsOnAVX)
                {
                    return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanSignaling);
                }

                // CompareNotGreaterThan is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareNotGreaterThan);
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanSignaling);
            }

            case NI_SSE_CompareNotLessThan:
            case NI_SSE_CompareScalarNotLessThan:
            case NI_SSE2_CompareNotLessThan:
            case NI_SSE2_CompareScalarNotLessThan:
            case NI_AVX_CompareNotLessThan:
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanSignaling);
            }

            case NI_SSE_CompareNotGreaterThanOrEqual:
            case NI_SSE_CompareScalarNotGreaterThanOrEqual:
            case NI_SSE2_CompareNotGreaterThanOrEqual:
            case NI_SSE2_CompareScalarNotGreaterThanOrEqual:
            case NI_AVX_CompareNotGreaterThanOrEqual:
            {
                if (opportunisticallyDependsOnAVX)
                {
                    return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling);
                }

                // CompareNotGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareNotGreaterThanOrEqual);
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling);
            }

            case NI_SSE_CompareNotLessThanOrEqual:
            case NI_SSE_CompareScalarNotLessThanOrEqual:
            case NI_SSE2_CompareNotLessThanOrEqual:
            case NI_SSE2_CompareScalarNotLessThanOrEqual:
            case NI_AVX_CompareNotLessThanOrEqual:
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling);
            }

            case NI_SSE_CompareOrdered:
            case NI_SSE_CompareScalarOrdered:
            case NI_SSE2_CompareOrdered:
            case NI_SSE2_CompareScalarOrdered:
            case NI_AVX_CompareOrdered:
            {
                return static_cast<int>(FloatComparisonMode::OrderedNonSignaling);
            }

            case NI_SSE_CompareUnordered:
            case NI_SSE_CompareScalarUnordered:
            case NI_SSE2_CompareUnordered:
            case NI_SSE2_CompareScalarUnordered:
            case NI_AVX_CompareUnordered:
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNonSignaling);
            }

            case NI_SSE41_Ceiling:
            case NI_SSE41_CeilingScalar:
            case NI_SSE41_RoundToPositiveInfinity:
            case NI_SSE41_RoundToPositiveInfinityScalar:
            case NI_AVX_Ceiling:
            case NI_AVX_RoundToPositiveInfinity:
            {
                return static_cast<int>(FloatRoundingMode::ToPositiveInfinity);
            }

            case NI_SSE41_Floor:
            case NI_SSE41_FloorScalar:
            case NI_SSE41_RoundToNegativeInfinity:
            case NI_SSE41_RoundToNegativeInfinityScalar:
            case NI_AVX_Floor:
            case NI_AVX_RoundToNegativeInfinity:
            {
                return static_cast<int>(FloatRoundingMode::ToNegativeInfinity);
            }

            case NI_SSE41_RoundCurrentDirection:
            case NI_SSE41_RoundCurrentDirectionScalar:
            case NI_AVX_RoundCurrentDirection:
            {
                return static_cast<int>(FloatRoundingMode::CurrentDirection);
            }

            case NI_SSE41_RoundToNearestInteger:
            case NI_SSE41_RoundToNearestIntegerScalar:
            case NI_AVX_RoundToNearestInteger:
            {
                return static_cast<int>(FloatRoundingMode::ToNearestInteger);
            }

            case NI_SSE41_RoundToZero:
            case NI_SSE41_RoundToZeroScalar:
            case NI_AVX_RoundToZero:
            {
                return static_cast<int>(FloatRoundingMode::ToZero);
            }

            default:
            {
                return -1;
            }
        }
    }
#endif

    static bool tryLookupSimdSize(NamedIntrinsic id, unsigned* pSimdSize)
    {
        bool succeeded = false;
        if (lookup(id).simdSize != -1)
        {
            *pSimdSize = lookup(id).simdSize;
            succeeded  = true;
        }
        return succeeded;
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

    static bool GeneratesMultipleIns(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MultiIns) != 0;
    }

    static bool SupportsContainment(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
#if defined(TARGET_XARCH)
        return (flags & HW_Flag_NoContainment) == 0;
#elif defined(TARGET_ARM64)
        return (flags & HW_Flag_SupportsContainment) != 0;
#else
#error Unsupported platform
#endif
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

#ifdef TARGET_XARCH
    static bool CopiesUpperBits(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_CopyUpperBits) != 0;
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
#endif

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
#if defined(TARGET_XARCH)
        return (flags & HW_Flag_NoRMWSemantics) == 0;
#elif defined(TARGET_ARM64)
        return (flags & HW_Flag_HasRMWSemantics) != 0;
#else
#error Unsupported platform
#endif
    }

    static bool HasSpecialImport(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialImport) != 0;
    }
};

#ifdef TARGET_ARM64

struct HWIntrinsic final
{
    HWIntrinsic(const GenTreeHWIntrinsic* node)
        : op1(nullptr), op2(nullptr), op3(nullptr), numOperands(0), baseType(TYP_UNDEF)
    {
        assert(node != nullptr);

        id       = node->gtHWIntrinsicId;
        category = HWIntrinsicInfo::lookupCategory(id);

        assert(HWIntrinsicInfo::RequiresCodegen(id));

        InitializeOperands(node);
        InitializeBaseType(node);
    }

    bool IsTableDriven() const
    {
        // TODO-Arm64-Cleanup - make more categories to the table-driven framework
        bool isTableDrivenCategory = category != HW_Category_Helper;
        bool isTableDrivenFlag = !HWIntrinsicInfo::GeneratesMultipleIns(id) && !HWIntrinsicInfo::HasSpecialCodegen(id);

        return isTableDrivenCategory && isTableDrivenFlag;
    }

    NamedIntrinsic      id;
    HWIntrinsicCategory category;
    GenTree*            op1;
    GenTree*            op2;
    GenTree*            op3;
    int                 numOperands;
    var_types           baseType;

private:
    void InitializeOperands(const GenTreeHWIntrinsic* node)
    {
        op1 = node->gtGetOp1();
        op2 = node->gtGetOp2();

        if (op1 == nullptr)
        {
            numOperands = 0;
        }
        else if (op1->OperIsList())
        {
            assert(op2 == nullptr);

            GenTreeArgList* list = op1->AsArgList();
            op1                  = list->Current();
            list                 = list->Rest();
            op2                  = list->Current();
            list                 = list->Rest();
            op3                  = list->Current();

            assert(list->Rest() == nullptr);

            numOperands = 3;
        }
        else if (op2 != nullptr)
        {
            numOperands = 2;
        }
        else
        {
            numOperands = 1;
        }
    }

    void InitializeBaseType(const GenTreeHWIntrinsic* node)
    {
        baseType = node->gtSIMDBaseType;

        if (baseType == TYP_UNKNOWN)
        {
            assert(category == HW_Category_Scalar);

            if (HWIntrinsicInfo::BaseTypeFromFirstArg(id))
            {
                assert(op1 != nullptr);
                baseType = op1->TypeGet();
            }
            else if (HWIntrinsicInfo::BaseTypeFromSecondArg(id))
            {
                assert(op2 != nullptr);
                baseType = op2->TypeGet();
            }
            else
            {
                baseType = node->TypeGet();
            }
        }
    }
};

#endif // TARGET_ARM64

#endif // FEATURE_HW_INTRINSICS

#endif // _HW_INTRINSIC_H_
