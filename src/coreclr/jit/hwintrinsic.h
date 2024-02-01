// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _HW_INTRINSIC_H_
#define _HW_INTRINSIC_H_

#ifdef FEATURE_HW_INTRINSICS

#ifdef TARGET_XARCH
enum HWIntrinsicCategory : uint8_t
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
#elif defined(TARGET_ARM64)
enum HWIntrinsicCategory : uint8_t
{
    // Most of the Arm64 intrinsic fall into SIMD category:
    // - vector or scalar intrinsics that operate on one-or-many SIMD registers
    HW_Category_SIMD,

    // Scalar intrinsics operate on general purpose registers (e.g. cls, clz, rbit)
    HW_Category_Scalar,

    // Memory access intrinsics
    HW_Category_MemoryLoad,
    HW_Category_MemoryStore,

    // These are Arm64 that share some features in a given category (e.g. immediate operand value range)
    HW_Category_ShiftLeftByImmediate,
    HW_Category_ShiftRightByImmediate,
    HW_Category_SIMDByIndexedElement,

    // Helper intrinsics
    // - do not directly correspond to a instruction, such as Vector64.AllBitsSet
    HW_Category_Helper,

    // Special intrinsics
    // - have to be addressed specially
    HW_Category_Special
};
#else
#error Unsupported platform
#endif
enum HWIntrinsicFlag : unsigned int
{
    HW_Flag_NoFlag = 0,

    // Commutative
    // - if a binary-op intrinsic is commutative (e.g., Add, Multiply), its op1 can be contained
    HW_Flag_Commutative = 0x1,

    // NoCodeGen
    // - should be transformed in the compiler front-end, cannot reach CodeGen
    HW_Flag_NoCodeGen = 0x2,

    // Multi-instruction
    // - that one intrinsic can generate multiple instructions
    HW_Flag_MultiIns = 0x4,

    // Select base type using the first argument type
    HW_Flag_BaseTypeFromFirstArg = 0x8,

    // Select base type using the second argument type
    HW_Flag_BaseTypeFromSecondArg = 0x10,

    // Indicates compFloatingPointUsed does not need to be set.
    HW_Flag_NoFloatingPointUsed = 0x20,

    // NoJmpTable IMM
    // the imm intrinsic does not need jumptable fallback when it gets non-const argument
    HW_Flag_NoJmpTableIMM = 0x40,

    // Special codegen
    // the intrinsics need special rules in CodeGen,
    // but may be table-driven in the front-end
    HW_Flag_SpecialCodeGen = 0x80,

    // Special import
    // the intrinsics need special rules in importer,
    // but may be table-driven in the back-end
    HW_Flag_SpecialImport = 0x100,

    // The intrinsic returns result in multiple registers.
    HW_Flag_MultiReg = 0x200,

// The below is for defining platform-specific flags
#if defined(TARGET_XARCH)
    // Full range IMM intrinsic
    // - the immediate value is valid on the full range of imm8 (0-255)
    HW_Flag_FullRangeIMM = 0x400,

    // Maybe IMM
    // the intrinsic has either imm or Vector overloads
    HW_Flag_MaybeIMM = 0x800,

    // Copy Upper bits
    // some SIMD scalar intrinsics need the semantics of copying upper bits from the source operand
    HW_Flag_CopyUpperBits = 0x1000,

    // Maybe Memory Load/Store
    // - some intrinsics may have pointer overloads but without HW_Category_MemoryLoad/HW_Category_MemoryStore
    HW_Flag_MaybeMemoryLoad  = 0x2000,
    HW_Flag_MaybeMemoryStore = 0x4000,

    // No Read/Modify/Write Semantics
    // the intrinsic doesn't have read/modify/write semantics in two/three-operand form.
    HW_Flag_NoRMWSemantics = 0x8000,

    // NoContainment
    // the intrinsic cannot be handled by containment,
    // all the intrinsic that have explicit memory load/store semantics should have this flag
    HW_Flag_NoContainment = 0x10000,

    // Returns Per-Element Mask
    // the intrinsic returns a vector containing elements that are either "all bits set" or "all bits clear"
    // this output can be used as a per-element mask
    HW_Flag_ReturnsPerElementMask = 0x20000,

    // AvxOnlyCompatible
    // the intrinsic can be used on hardware with AVX but not AVX2 support
    HW_Flag_AvxOnlyCompatible = 0x40000,

    // MaybeCommutative
    // - if a binary-op intrinsic is maybe commutative (e.g., Max or Min for float/double), its op1 can possibly be
    // contained
    HW_Flag_MaybeCommutative = 0x80000,

    // The intrinsic has no EVEX compatible form
    HW_Flag_NoEvexSemantics = 0x100000,

#elif defined(TARGET_ARM64)
    // The intrinsic has an immediate operand
    // - the value can be (and should be) encoded in a corresponding instruction when the operand value is constant
    HW_Flag_HasImmediateOperand = 0x400,

    // The intrinsic has read/modify/write semantics in multiple-operands form.
    HW_Flag_HasRMWSemantics = 0x800,

    // The intrinsic operates on the lower part of a SIMD register
    // - the upper part of the source registers are ignored
    // - the upper part of the destination register is zeroed
    HW_Flag_SIMDScalar = 0x1000,

    // The intrinsic supports some sort of containment analysis
    HW_Flag_SupportsContainment = 0x2000,

    // The intrinsic needs consecutive registers
    HW_Flag_NeedsConsecutiveRegisters = 0x4000,
#else
#error Unsupported platform
#endif

    // The intrinsic has some barrier special side effect that should be tracked
    HW_Flag_SpecialSideEffect_Barrier = 0x200000,

    // The intrinsic has some other special side effect that should be tracked
    HW_Flag_SpecialSideEffect_Other = 0x400000,

    HW_Flag_SpecialSideEffectMask = (HW_Flag_SpecialSideEffect_Barrier | HW_Flag_SpecialSideEffect_Other),

    // MaybeNoJmpTable IMM
    // the imm intrinsic may not need jumptable fallback when it gets non-const argument
    HW_Flag_MaybeNoJmpTableIMM = 0x800000,

#if defined(TARGET_XARCH)
    // The intrinsic is an RMW intrinsic
    HW_Flag_RmwIntrinsic = 0x1000000,

    // The intrinsic is a FusedMultiplyAdd intrinsic
    HW_Flag_FmaIntrinsic = 0x2000000,

    // The intrinsic is a PermuteVar2x intrinsic
    HW_Flag_PermuteVar2x = 0x4000000,

    // The intrinsic is an embedded broadcast compatible intrinsic
    HW_Flag_EmbBroadcastCompatible = 0x8000000,

    // The intrinsic is an embedded rounding compatible intrinsic
    HW_Flag_EmbRoundingCompatible = 0x10000000,

    // The intrinsic is an embedded masking incompatible intrinsic
    HW_Flag_EmbMaskingIncompatible = 0x20000000,
#endif // TARGET_XARCH

    HW_Flag_CanBenefitFromConstantProp = 0x80000000,
};

#if defined(TARGET_XARCH)
// This mirrors the System.Runtime.Intrinsics.X86.FloatComparisonMode enumeration
enum class FloatComparisonMode : uint8_t
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

enum class FloatRoundingMode : uint8_t
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

enum class IntComparisonMode : uint8_t
{
    Equal           = 0,
    LessThan        = 1,
    LessThanOrEqual = 2,
    False           = 3,

    NotEqual           = 4,
    GreaterThanOrEqual = 5,
    GreaterThan        = 6,
    True               = 7,

    NotGreaterThanOrEqual = LessThan,
    NotGreaterThan        = LessThanOrEqual,

    NotLessThan        = GreaterThanOrEqual,
    NotLessThanOrEqual = GreaterThan
};

enum class TernaryLogicUseFlags : uint8_t
{
    // Indicates no flags are present
    None = 0,

    // Indicates the ternary logic uses A
    A = 1 << 0,

    // Indicates the ternary logic uses B
    B = 1 << 1,

    // Indicates the ternary logic uses C
    C = 1 << 2,

    // Indicates the ternary logic uses A and B
    AB = (A | B),

    // Indicates the ternary logic uses A and C
    AC = (A | C),

    // Indicates the ternary logic uses B and C
    BC = (B | C),

    // Indicates the ternary logic uses A, B, and C
    ABC = (A | B | C),
};

enum class TernaryLogicOperKind : uint8_t
{
    // Indicates no operation is done
    None = 0,

    // value
    Select = 1,

    // constant true (1)
    True = 2,

    // constant false (0)
    False = 3,

    // ~value
    Not = 4,

    // left & right
    And = 5,

    // ~(left & right)
    Nand = 6,

    // left | right
    Or = 7,

    // ~(left | right)
    Nor = 8,

    // left ^ right
    Xor = 9,

    // ~(left ^ right)
    Xnor = 10,

    // cond ? left : right
    Cond = 11,

    // returns 0 if two+ of the three input bits are 0; else 1
    Major = 12,

    // returns 0 if two+ of the three input bits are 1; else 0
    Minor = 13,
};

struct TernaryLogicInfo
{
    // We have 256 entries, so we compress as much as possible
    // This gives us 3-bytes per entry (21-bits)

    TernaryLogicOperKind oper1 : 4;
    TernaryLogicUseFlags oper1Use : 3;

    TernaryLogicOperKind oper2 : 4;
    TernaryLogicUseFlags oper2Use : 3;

    TernaryLogicOperKind oper3 : 4;
    TernaryLogicUseFlags oper3Use : 3;

    static const TernaryLogicInfo& lookup(uint8_t control);

    TernaryLogicUseFlags GetAllUseFlags() const
    {
        uint8_t useFlagsBits = 0;

        useFlagsBits |= static_cast<uint8_t>(oper1Use);
        useFlagsBits |= static_cast<uint8_t>(oper2Use);
        useFlagsBits |= static_cast<uint8_t>(oper3Use);

        return static_cast<TernaryLogicUseFlags>(useFlagsBits);
    }
};
#endif // TARGET_XARCH

struct HWIntrinsicInfo
{
    // 32-bit: 36-bytes (34+2 trailing padding)
    // 64-bit: 40-bytes (38+2 trailing padding)

    const char*         name;     // 4 or 8-bytes
    HWIntrinsicFlag     flags;    // 4-bytes
    NamedIntrinsic      id;       // 2-bytes
    uint16_t            ins[10];  // 10 * 2-bytes
    uint8_t             isa;      // 1-byte
    int8_t              simdSize; // 1-byte
    int8_t              numArgs;  // 1-byte
    HWIntrinsicCategory category; // 1-byte

    static const HWIntrinsicInfo& lookup(NamedIntrinsic id);

    static NamedIntrinsic lookupId(Compiler*         comp,
                                   CORINFO_SIG_INFO* sig,
                                   const char*       className,
                                   const char*       methodName,
                                   const char*       enclosingClassName);
    static CORINFO_InstructionSet lookupIsa(const char* className, const char* enclosingClassName);

    static unsigned lookupSimdSize(Compiler* comp, NamedIntrinsic id, CORINFO_SIG_INFO* sig);

#if defined(TARGET_XARCH)
    static int lookupImmUpperBound(NamedIntrinsic intrinsic);
#elif defined(TARGET_ARM64)
    static void lookupImmBounds(
        NamedIntrinsic intrinsic, int simdSize, var_types baseType, int* lowerBound, int* upperBound);
#else
#error Unsupported platform
#endif

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
        uint8_t result = lookup(id).isa;
        return static_cast<CORINFO_InstructionSet>(result);
    }

#ifdef TARGET_XARCH
    static int lookupIval(Compiler* comp, NamedIntrinsic id, var_types simdBaseType);
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

        uint16_t result = lookup(id).ins[type - TYP_BYTE];
        return static_cast<instruction>(result);
    }

    static instruction lookupIns(GenTreeHWIntrinsic* intrinsicNode)
    {
        assert(intrinsicNode != nullptr);

        NamedIntrinsic intrinsic = intrinsicNode->GetHWIntrinsicId();
        var_types      type      = TYP_UNKNOWN;

        if (lookupCategory(intrinsic) == HW_Category_Scalar)
        {
            type = intrinsicNode->TypeGet();
        }
        else
        {
            type = intrinsicNode->GetSimdBaseType();
        }

        return lookupIns(intrinsic, type);
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

#if defined(TARGET_XARCH)
    static bool IsEmbBroadcastCompatible(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_EmbBroadcastCompatible) != 0;
    }

    static bool IsEmbRoundingCompatible(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_EmbRoundingCompatible) != 0;
    }

    static bool IsEmbMaskingCompatible(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_EmbMaskingIncompatible) == 0;
    }

    static size_t EmbRoundingArgPos(NamedIntrinsic id)
    {
        // This helper function returns the expected position,
        // where the embedded rounding control argument should be.
        assert(IsEmbRoundingCompatible(id));
        switch (id)
        {
            case NI_AVX512F_Add:
                return 3;

            default:
                unreached();
        }
    }
#endif // TARGET_XARCH

    static bool CanBenefitFromConstantProp(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_CanBenefitFromConstantProp) != 0;
    }

    static bool IsMaybeCommutative(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
#if defined(TARGET_XARCH)
        return (flags & HW_Flag_MaybeCommutative) != 0;
#elif defined(TARGET_ARM64)
        return false;
#else
#error Unsupported platform
#endif
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

    static bool ReturnsPerElementMask(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
#if defined(TARGET_XARCH)
        return (flags & HW_Flag_ReturnsPerElementMask) != 0;
#elif defined(TARGET_ARM64)
        unreached();
#else
#error Unsupported platform
#endif
    }

#if defined(TARGET_XARCH)
    static bool AvxOnlyCompatible(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_AvxOnlyCompatible) != 0;
    }
#endif

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

#ifdef TARGET_XARCH
    static bool HasFullRangeImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_FullRangeIMM) != 0;
    }

    static bool MaybeImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MaybeIMM) != 0;
    }

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

#ifdef TARGET_ARM64
    static bool NeedsConsecutiveRegisters(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NeedsConsecutiveRegisters) != 0;
    }
#endif

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
    //------------------------------------------------------------------------
    // HasEvexSemantics: Checks if the NamedIntrinsic has a lowering to
    // to an instruction with an EVEX form.
    //
    // Return Value:
    // true if the NamedIntrinsic lowering has an EVEX form.
    //
    static bool HasEvexSemantics(NamedIntrinsic id)
    {
#if defined(TARGET_XARCH)
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_NoEvexSemantics) == 0;
#else
        return false;
#endif
    }

    static bool HasSpecialImport(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialImport) != 0;
    }

    static bool IsMultiReg(NamedIntrinsic id)
    {
        const HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MultiReg) != 0;
    }

    static int GetMultiRegCount(NamedIntrinsic id)
    {
        assert(IsMultiReg(id));

        switch (id)
        {
#ifdef TARGET_ARM64
            case NI_AdvSimd_Arm64_LoadPairScalarVector64:
            case NI_AdvSimd_Arm64_LoadPairScalarVector64NonTemporal:
            case NI_AdvSimd_Arm64_LoadPairVector64:
            case NI_AdvSimd_Arm64_LoadPairVector64NonTemporal:
            case NI_AdvSimd_Arm64_LoadPairVector128:
            case NI_AdvSimd_Arm64_LoadPairVector128NonTemporal:
            case NI_AdvSimd_LoadVector64x2AndUnzip:
            case NI_AdvSimd_Arm64_LoadVector128x2AndUnzip:
            case NI_AdvSimd_LoadVector64x2:
            case NI_AdvSimd_Arm64_LoadVector128x2:
            case NI_AdvSimd_LoadAndInsertScalarVector64x2:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
            case NI_AdvSimd_LoadAndReplicateToVector64x2:
            case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x2:
                return 2;

            case NI_AdvSimd_LoadVector64x3AndUnzip:
            case NI_AdvSimd_Arm64_LoadVector128x3AndUnzip:
            case NI_AdvSimd_LoadVector64x3:
            case NI_AdvSimd_Arm64_LoadVector128x3:
            case NI_AdvSimd_LoadAndInsertScalarVector64x3:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
            case NI_AdvSimd_LoadAndReplicateToVector64x3:
            case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x3:
                return 3;

            case NI_AdvSimd_LoadVector64x4AndUnzip:
            case NI_AdvSimd_Arm64_LoadVector128x4AndUnzip:
            case NI_AdvSimd_LoadVector64x4:
            case NI_AdvSimd_Arm64_LoadVector128x4:
            case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
            case NI_AdvSimd_LoadAndReplicateToVector64x4:
            case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x4:
                return 4;
#endif

#ifdef TARGET_XARCH
            case NI_X86Base_DivRem:
            case NI_X86Base_X64_DivRem:
                return 2;
#endif // TARGET_XARCH

            default:
                unreached();
        }
    }

#ifdef TARGET_ARM64
    static bool SIMDScalar(NamedIntrinsic id)
    {
        const HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SIMDScalar) != 0;
    }

    static bool HasImmediateOperand(NamedIntrinsic id)
    {
        const HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_HasImmediateOperand) != 0;
    }
#endif // TARGET_ARM64

    static bool HasSpecialSideEffect(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialSideEffectMask) != 0;
    }

    static bool HasSpecialSideEffect_Barrier(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_SpecialSideEffect_Barrier) != 0;
    }

    static bool MaybeNoJmpTableImm(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_MaybeNoJmpTableIMM) != 0;
    }

#if defined(TARGET_XARCH)
    static bool IsRmwIntrinsic(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_RmwIntrinsic) != 0;
    }

    static bool IsFmaIntrinsic(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_FmaIntrinsic) != 0;
    }

    static bool IsPermuteVar2x(NamedIntrinsic id)
    {
        HWIntrinsicFlag flags = lookupFlags(id);
        return (flags & HW_Flag_PermuteVar2x) != 0;
    }
#endif // TARGET_XARCH
};

#ifdef TARGET_ARM64

struct HWIntrinsic final
{
    HWIntrinsic(const GenTreeHWIntrinsic* node)
        : op1(nullptr), op2(nullptr), op3(nullptr), op4(nullptr), numOperands(0), baseType(TYP_UNDEF)
    {
        assert(node != nullptr);

        id       = node->GetHWIntrinsicId();
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
    GenTree*            op4;
    size_t              numOperands;
    var_types           baseType;

private:
    void InitializeOperands(const GenTreeHWIntrinsic* node)
    {
        numOperands = node->GetOperandCount();

        switch (numOperands)
        {
            case 4:
                op4 = node->Op(4);
                FALLTHROUGH;
            case 3:
                op3 = node->Op(3);
                FALLTHROUGH;
            case 2:
                op2 = node->Op(2);
                FALLTHROUGH;
            case 1:
                op1 = node->Op(1);
                FALLTHROUGH;
            case 0:
                break;

            default:
                unreached();
        }
    }

    void InitializeBaseType(const GenTreeHWIntrinsic* node)
    {
        baseType = node->GetSimdBaseType();

        if (baseType == TYP_UNKNOWN)
        {
            assert((category == HW_Category_Scalar) || (category == HW_Category_Special));

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

            if (category == HW_Category_Scalar)
            {
                baseType = genActualType(baseType);
            }
        }
    }
};

#endif // TARGET_ARM64

#endif // FEATURE_HW_INTRINSICS

#endif // _HW_INTRINSIC_H_
