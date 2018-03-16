// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#ifdef FEATURE_HW_INTRINSICS

struct HWIntrinsicInfo
{
    NamedIntrinsic      intrinsicID;
    const char*         intrinsicName;
    InstructionSet      isa;
    int                 ival;
    unsigned            simdSize;
    int                 numArgs;
    instruction         ins[10];
    HWIntrinsicCategory category;
    HWIntrinsicFlag     flags;
};

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
#define HARDWARE_INTRINSIC(id, name, isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    {NI_##id, name, InstructionSet_##isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag},
#include "hwintrinsiclistxarch.h"
};

extern const char* getHWIntrinsicName(NamedIntrinsic intrinsic)
{
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].intrinsicName;
}

//------------------------------------------------------------------------
// lookupHWIntrinsicISA: map class name to InstructionSet value
//
// Arguments:
//    className -- class name in System.Runtime.Intrinsics.X86
//
// Return Value:
//    Id for the ISA class.
//
InstructionSet Compiler::lookupHWIntrinsicISA(const char* className)
{
    if (className != nullptr)
    {
        if (className[0] == 'A')
        {
            if (strcmp(className, "Aes") == 0)
            {
                return InstructionSet_AES;
            }
            else if (strcmp(className, "Avx") == 0)
            {
                return InstructionSet_AVX;
            }
            else if (strcmp(className, "Avx2") == 0)
            {
                return InstructionSet_AVX2;
            }
        }
        if (className[0] == 'S')
        {
            if (strcmp(className, "Sse") == 0)
            {
                return InstructionSet_SSE;
            }
            else if (strcmp(className, "Sse2") == 0)
            {
                return InstructionSet_SSE2;
            }
            else if (strcmp(className, "Sse3") == 0)
            {
                return InstructionSet_SSE3;
            }
            else if (strcmp(className, "Ssse3") == 0)
            {
                return InstructionSet_SSSE3;
            }
            else if (strcmp(className, "Sse41") == 0)
            {
                return InstructionSet_SSE41;
            }
            else if (strcmp(className, "Sse42") == 0)
            {
                return InstructionSet_SSE42;
            }
        }

        if (strcmp(className, "Bmi1") == 0)
        {
            return InstructionSet_BMI1;
        }
        else if (strcmp(className, "Bmi2") == 0)
        {
            return InstructionSet_BMI2;
        }
        else if (strcmp(className, "Fma") == 0)
        {
            return InstructionSet_FMA;
        }
        else if (strcmp(className, "Lzcnt") == 0)
        {
            return InstructionSet_LZCNT;
        }
        else if (strcmp(className, "Pclmulqdq") == 0)
        {
            return InstructionSet_PCLMULQDQ;
        }
        else if (strcmp(className, "Popcnt") == 0)
        {
            return InstructionSet_POPCNT;
        }
    }

    JITDUMP("Unsupported ISA.\n");
    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupHWIntrinsic: map intrinsic name to named intrinsic value
//
// Arguments:
//    methodName -- name of the intrinsic function.
//    isa        -- instruction set of the intrinsic.
//
// Return Value:
//    Id for the hardware intrinsic
//
// TODO-Throughput: replace sequential search by binary search
NamedIntrinsic Compiler::lookupHWIntrinsic(const char* methodName, InstructionSet isa)
{
    NamedIntrinsic result = NI_Illegal;
    if (isa != InstructionSet_ILLEGAL)
    {
        for (int i = 0; i < NI_HW_INTRINSIC_END - NI_HW_INTRINSIC_START - 1; i++)
        {
            if (isa == hwIntrinsicInfoArray[i].isa && strcmp(methodName, hwIntrinsicInfoArray[i].intrinsicName) == 0)
            {
                result = hwIntrinsicInfoArray[i].intrinsicID;
                break;
            }
        }
    }
    return result;
}

//------------------------------------------------------------------------
// isaOfHWIntrinsic: map named intrinsic value to its instruction set
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//    instruction set of the intrinsic.
//
InstructionSet Compiler::isaOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].isa;
}

//------------------------------------------------------------------------
// ivalOfHWIntrinsic: get the imm8 value of this intrinsic from the hwIntrinsicInfoArray table
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     The imm8 value that is implicit for this intrinsic, or -1 for intrinsics that do not take an immediate, or for
//     which the immediate is an explicit argument.
//
int Compiler::ivalOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].ival;
}

//------------------------------------------------------------------------
// simdSizeOfHWIntrinsic: get the SIMD size of this intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the SIMD size of this intrinsic
//         - from the hwIntrinsicInfoArray table if intrinsic has NO HW_Flag_UnfixedSIMDSize
//         - from the signature if intrinsic has HW_Flag_UnfixedSIMDSize
//
// Note - this function is only used by the importer
//        after importation (i.e., codegen), we can get the SIMD size from GenTreeHWIntrinsic IR
unsigned Compiler::simdSizeOfHWIntrinsic(NamedIntrinsic intrinsic, CORINFO_SIG_INFO* sig)
{
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);

    HWIntrinsicFlag flags = flagsOfHWIntrinsic(intrinsic);

    if ((flags & HW_Flag_UnfixedSIMDSize) == 0)
    {
        return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].simdSize;
    }

    CORINFO_CLASS_HANDLE typeHnd = nullptr;

    if (JITtype2varType(sig->retType) == TYP_STRUCT)
    {
        typeHnd = sig->retTypeSigClass;
    }
    else
    {
        assert((flags & HW_Flag_BaseTypeFromFirstArg) != 0);
        typeHnd = info.compCompHnd->getArgClass(sig, sig->args);
    }

    unsigned  simdSize = 0;
    var_types baseType = getBaseTypeAndSizeOfSIMDType(typeHnd, &simdSize);
    assert(simdSize > 0 && baseType != TYP_UNKNOWN);
    return simdSize;
}

// TODO_XARCH-CQ - refactoring of numArgsOfHWIntrinsic fast path into inlinable
// function and slow local static function may increase performance significantly

//------------------------------------------------------------------------
// numArgsOfHWIntrinsic: gets the number of arguments for the hardware intrinsic.
// This attempts to do a table based lookup but will fallback to the number
// of operands in 'node' if the table entry is -1.
//
// Arguments:
//    node      -- GenTreeHWIntrinsic* node with nullptr default value
//
// Return Value:
//     number of arguments
//
int Compiler::numArgsOfHWIntrinsic(GenTreeHWIntrinsic* node)
{
    assert(node != nullptr);

    NamedIntrinsic intrinsic = node->gtHWIntrinsicId;

    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);

    int numArgs = hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].numArgs;
    if (numArgs >= 0)
    {
        return numArgs;
    }

    assert(numArgs == -1);

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    if (op2 != nullptr)
    {
        return 2;
    }

    if (op1 != nullptr)
    {
        if (op1->OperIsList())
        {
            numArgs              = 0;
            GenTreeArgList* list = op1->AsArgList();

            while (list != nullptr)
            {
                numArgs++;
                list = list->Rest();
            }

            assert(numArgs > 0);
            return numArgs;
        }
        else
        {
            return 1;
        }
    }
    else
    {
        return 0;
    }
}

//------------------------------------------------------------------------
// lastOpOfHWIntrinsic: get the last operand of a HW intrinsic
//
// Arguments:
//    node   -- the intrinsic node.
//    numArgs-- number of argument
//
// Return Value:
//     number of arguments
//
GenTree* Compiler::lastOpOfHWIntrinsic(GenTreeHWIntrinsic* node, int numArgs)
{
    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();
    switch (numArgs)
    {
        case 0:
            return nullptr;
        case 1:
            assert(op1 != nullptr);
            return op1;
        case 2:
            assert(op2 != nullptr);
            return op2;
        case 3:
            assert(op1->OperIsList());
            assert(op1->AsArgList()->Rest()->Rest()->Current() != nullptr);
            assert(op1->AsArgList()->Rest()->Rest()->Rest() == nullptr);
            return op1->AsArgList()->Rest()->Rest()->Current();
        default:
            unreached();
            return nullptr;
    }
}

//------------------------------------------------------------------------
// insOfHWIntrinsic: get the instruction of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    type      -- vector base type of this intrinsic
//
// Return Value:
//     the instruction of the given intrinsic on the base type
//     return INS_invalid for unsupported base types
//
instruction Compiler::insOfHWIntrinsic(NamedIntrinsic intrinsic, var_types type)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    assert(type >= TYP_BYTE && type <= TYP_DOUBLE);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].ins[type - TYP_BYTE];
}

//------------------------------------------------------------------------
// categoryOfHWIntrinsic: get the category of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the category of the given intrinsic
//
HWIntrinsicCategory Compiler::categoryOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].category;
}

//------------------------------------------------------------------------
// HWIntrinsicFlag: get the flags of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the flags of the given intrinsic
//
HWIntrinsicFlag Compiler::flagsOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].flags;
}

//------------------------------------------------------------------------
// getArgForHWIntrinsic: get the argument from the stack and match  the signature
//
// Arguments:
//    argType   -- the required type of argument
//    argClass  -- the class handle of argType
//
// Return Value:
//     get the argument at the given index from the stack and match  the signature
//
GenTree* Compiler::getArgForHWIntrinsic(var_types argType, CORINFO_CLASS_HANDLE argClass)
{
    GenTree* arg = nullptr;
    if (argType == TYP_STRUCT)
    {
        unsigned int argSizeBytes;
        var_types    base = getBaseTypeAndSizeOfSIMDType(argClass, &argSizeBytes);
        argType           = getSIMDTypeForSize(argSizeBytes);
        assert((argType == TYP_SIMD32) || (argType == TYP_SIMD16));
        arg = impSIMDPopStack(argType);
        assert((arg->TypeGet() == TYP_SIMD16) || (arg->TypeGet() == TYP_SIMD32));
    }
    else
    {
        assert(varTypeIsArithmetic(argType));
        arg = impPopStack().val;
        assert(varTypeIsArithmetic(arg->TypeGet()));
        assert(genActualType(arg->gtType) == genActualType(argType));
    }
    return arg;
}

//------------------------------------------------------------------------
// immUpperBoundOfHWIntrinsic: get the max imm-value of non-full-range IMM intrinsic
//
// Arguments:
//    intrinsic  -- intrinsic ID
//
// Return Value:
//     the max imm-value of non-full-range IMM intrinsic
//
int Compiler::immUpperBoundOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(categoryOfHWIntrinsic(intrinsic) == HW_Category_IMM);
    switch (intrinsic)
    {
        case NI_AVX_Compare:
        case NI_AVX_CompareScalar:
            return 31; // enum FloatComparisonMode has 32 values

        default:
            assert((flagsOfHWIntrinsic(intrinsic) & HW_Flag_FullRangeIMM) != 0);
            return 255;
    }
}

//------------------------------------------------------------------------
// impNonConstFallback: convert certain SSE2/AVX2 shift intrinsic to its semantic alternative when the imm-arg is
// not a compile-time constant
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    simdType   -- Vector type
//    baseType   -- base type of the Vector128/256<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types baseType)
{
    assert((flagsOfHWIntrinsic(intrinsic) & HW_Flag_NoJmpTableIMM) != 0);
    switch (intrinsic)
    {
        case NI_SSE2_ShiftLeftLogical:
        case NI_SSE2_ShiftRightArithmetic:
        case NI_SSE2_ShiftRightLogical:
        case NI_AVX2_ShiftLeftLogical:
        case NI_AVX2_ShiftRightArithmetic:
        case NI_AVX2_ShiftRightLogical:
        {
            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack(simdType);
            GenTree* tmpOp =
                gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_SSE2_ConvertScalarToVector128Int32, TYP_INT, 16);
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, baseType, genTypeSize(simdType));
        }

        default:
            unreached();
            return nullptr;
    }
}

//------------------------------------------------------------------------
// isImmHWIntrinsic: check the intrinsic is a imm-intrinsic overload or not
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    lastOp     -- the last operand of the intrinsic that may point to the imm-arg
//
// Return Value:
//        Return true iff the intrinsics is an imm-intrinsic overload.
//        Note: that some intrinsics, with HW_Flag_MaybeIMM set, have both imm (integer immediate) and vector (i.e.
//        non-TYP_INT) overloads.
//
bool Compiler::isImmHWIntrinsic(NamedIntrinsic intrinsic, GenTree* lastOp)
{
    if (categoryOfHWIntrinsic(intrinsic) != HW_Category_IMM)
    {
        return false;
    }

    if ((flagsOfHWIntrinsic(intrinsic) & HW_Flag_MaybeIMM) != 0 && genActualType(lastOp->TypeGet()) != TYP_INT)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// addRangeCheckIfNeeded: add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    lastOp     -- the last operand of the intrinsic that points to the imm-arg
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//     add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckIfNeeded(NamedIntrinsic intrinsic, GenTree* lastOp, bool mustExpand)
{
    assert(lastOp != nullptr);
    // Full-range imm-intrinsics do not need the range-check
    // because the imm-parameter of the intrinsic method is a byte.
    if (mustExpand && ((flagsOfHWIntrinsic(intrinsic) & HW_Flag_FullRangeIMM) == 0) &&
        isImmHWIntrinsic(intrinsic, lastOp))
    {
        assert(!lastOp->IsCnsIntOrI());
        GenTree* upperBoundNode = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, immUpperBoundOfHWIntrinsic(intrinsic));
        GenTree* index          = nullptr;
        if ((lastOp->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            index = fgInsertCommaFormTemp(&lastOp);
        }
        else
        {
            index = gtCloneExpr(lastOp);
        }
        GenTreeBoundsChk* hwIntrinsicChk = new (this, GT_HW_INTRINSIC_CHK)
            GenTreeBoundsChk(GT_HW_INTRINSIC_CHK, TYP_VOID, index, upperBoundNode, SCK_RNGCHK_FAIL);
        hwIntrinsicChk->gtThrowKind = SCK_ARG_RNG_EXCPN;
        return gtNewOperNode(GT_COMMA, lastOp->TypeGet(), hwIntrinsicChk, lastOp);
    }
    else
    {
        return lastOp;
    }
}

//------------------------------------------------------------------------
// isFullyImplmentedISAClass: return true if all the hardware intrinsics
//    of this ISA are implemented in RyuJIT.
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true - all the hardware intrinsics of "isa" are implemented in RyuJIT.
//
bool Compiler::isFullyImplmentedISAClass(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_SSE42:
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
        case InstructionSet_AES:
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_FMA:
        case InstructionSet_PCLMULQDQ:
            return false;

        case InstructionSet_SSE:
        case InstructionSet_SSE2:
        case InstructionSet_SSE3:
        case InstructionSet_SSSE3:
        case InstructionSet_SSE41:
        case InstructionSet_LZCNT:
        case InstructionSet_POPCNT:
            return true;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// isScalarISA:
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true - if "isa" only contains scalar instructions
//
bool Compiler::isScalarISA(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_LZCNT:
        case InstructionSet_POPCNT:
            return true;

        default:
            return false;
    }
}

//------------------------------------------------------------------------
// compSupportsHWIntrinsic: compiler support of hardware intrinsics
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true if
//    - isa is a scalar ISA
//    - isa is a SIMD ISA and featureSIMD=true
//    - isa is fully implemented or EnableIncompleteISAClass=true
bool Compiler::compSupportsHWIntrinsic(InstructionSet isa)
{
    return (featureSIMD || isScalarISA(isa)) && (
#ifdef DEBUG
                                                    JitConfig.EnableIncompleteISAClass() ||
#endif
                                                    isFullyImplmentedISAClass(isa));
}

//------------------------------------------------------------------------
// hwIntrinsicSignatureTypeSupported: platform support of hardware intrinsics
//
// Arguments:
//    retType - return type
//    sig     - intrinsic signature
//    flags   - flags of the intrinsics
//
// Return Value:
//    Returns true iff the given type signature is supported
// Notes:
//    - This is only used on 32-bit systems to determine whether the signature uses no 64-bit registers.
//    - The `retType` is passed to avoid another call to the type system, as it has already been retrieved.
bool Compiler::hwIntrinsicSignatureTypeSupported(var_types retType, CORINFO_SIG_INFO* sig, HWIntrinsicFlag flags)
{
#ifdef _TARGET_X86_
    CORINFO_CLASS_HANDLE argClass;

    if ((flags & HW_Flag_64BitOnly) != 0)
    {
        return false;
    }
    else if ((flags & HW_Flag_SecondArgMaybe64Bit) != 0)
    {
        assert(sig->numArgs >= 2);
        CorInfoType corType =
            strip(info.compCompHnd->getArgType(sig, info.compCompHnd->getArgNext(sig->args), &argClass));
        return !varTypeIsLong(JITtype2varType(corType));
    }

    return !varTypeIsLong(retType);
#else
    return true;
#endif
}

//------------------------------------------------------------------------
// impIsTableDrivenHWIntrinsic:
//
// Arguments:
//    category - category of a HW intrinsic
//
// Return Value:
//    returns true if this category can be table-driven in the importer
//
static bool impIsTableDrivenHWIntrinsic(HWIntrinsicCategory category, HWIntrinsicFlag flags)
{
    // HW_Flag_NoCodeGen implies this intrinsic should be manually morphed in the importer.
    return category != HW_Category_Special && category != HW_Category_Scalar &&
           ((flags & (HW_Flag_NoCodeGen | HW_Flag_SpecialImport)) == 0);
}

//------------------------------------------------------------------------
// impHWIntrinsic: dispatch hardware intrinsics to their own implementation
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    method    -- method handle of the intrinsic function.
//    sig       -- signature of the intrinsic call
//
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impHWIntrinsic(NamedIntrinsic        intrinsic,
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO*     sig,
                                  bool                  mustExpand)
{
    InstructionSet      isa      = isaOfHWIntrinsic(intrinsic);
    HWIntrinsicCategory category = categoryOfHWIntrinsic(intrinsic);
    HWIntrinsicFlag     flags    = flagsOfHWIntrinsic(intrinsic);
    int                 numArgs  = sig->numArgs;
    var_types           retType  = JITtype2varType(sig->retType);
    var_types           baseType = TYP_UNKNOWN;

    if ((retType == TYP_STRUCT) && featureSIMD)
    {
        unsigned int sizeBytes;
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
        retType  = getSIMDTypeForSize(sizeBytes);
        assert(sizeBytes != 0);
    }

    // This intrinsic is supported if
    // - the ISA is available on the underlying hardware (compSupports returns true)
    // - the compiler supports this hardware intrinsics (compSupportsHWIntrinsic returns true)
    // - intrinsics do not require 64-bit registers (r64) on 32-bit platforms (signatureTypeSupproted returns
    // true)
    bool issupported =
        compSupports(isa) && compSupportsHWIntrinsic(isa) && hwIntrinsicSignatureTypeSupported(retType, sig, flags);

    if (category == HW_Category_IsSupportedProperty)
    {
        return gtNewIconNode(issupported);
    }
    // - calling to unsupported intrinsics must throw PlatforNotSupportedException
    else if (!issupported)
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }
    // Avoid checking stacktop for 0-op intrinsics
    if (sig->numArgs > 0 && isImmHWIntrinsic(intrinsic, impStackTop().val))
    {
        GenTree* lastOp = impStackTop().val;
        // The imm-HWintrinsics that do not accept all imm8 values may throw
        // ArgumentOutOfRangeException when the imm argument is not in the valid range
        if ((flags & HW_Flag_FullRangeIMM) == 0)
        {
            if (!mustExpand && lastOp->IsCnsIntOrI() &&
                lastOp->AsIntCon()->IconValue() > immUpperBoundOfHWIntrinsic(intrinsic))
            {
                return nullptr;
            }
        }

        if (!lastOp->IsCnsIntOrI())
        {
            if ((flags & HW_Flag_NoJmpTableIMM) == 0 && !mustExpand)
            {
                // When the imm-argument is not a constant and we are not being forced to expand, we need to
                // return nullptr so a GT_CALL to the intrinsic method is emitted instead. The
                // intrinsic method is recursive and will be forced to expand, at which point
                // we emit some less efficient fallback code.
                return nullptr;
            }
            else if ((flags & HW_Flag_NoJmpTableIMM) != 0)
            {
                return impNonConstFallback(intrinsic, retType, baseType);
            }
        }
    }

    bool isTableDriven = impIsTableDrivenHWIntrinsic(category, flags);

    if (isTableDriven && ((category == HW_Category_MemoryStore) ||
                          ((flags & (HW_Flag_BaseTypeFromFirstArg | HW_Flag_BaseTypeFromSecondArg)) != 0)))
    {
        if ((flags & HW_Flag_BaseTypeFromFirstArg) != 0)
        {
            baseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
        }
        else
        {
            assert((category == HW_Category_MemoryStore) || ((flags & HW_Flag_BaseTypeFromSecondArg) != 0));
            CORINFO_ARG_LIST_HANDLE secondArg      = info.compCompHnd->getArgNext(sig->args);
            CORINFO_CLASS_HANDLE    secondArgClass = info.compCompHnd->getArgClass(sig, secondArg);
            baseType                               = getBaseTypeOfSIMDType(secondArgClass);

            if (baseType == TYP_UNKNOWN) // the second argument is not a vector
            {
                baseType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, secondArg, &secondArgClass)));
                assert(baseType != TYP_STRUCT);
            }
        }

        assert(baseType != TYP_UNKNOWN);
    }

    if (((flags & (HW_Flag_OneTypeGeneric | HW_Flag_TwoTypeGeneric)) != 0) && ((flags & HW_Flag_SpecialImport) == 0))
    {
        if (!varTypeIsArithmetic(baseType))
        {
            return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
        }

        if ((flags & HW_Flag_TwoTypeGeneric) != 0)
        {
            // StaticCast<T, U> has two type parameters.
            assert(numArgs == 1);
            var_types srcType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
            if (!varTypeIsArithmetic(srcType))
            {
                return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
            }
        }
    }

    if ((flags & HW_Flag_NoFloatingPointUsed) == 0)
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic is being on SIMD fields, but
        // where no SIMD local vars are in use. This is the same logic as is used for FEATURE_SIMD.
        compFloatingPointUsed = true;
    }

    // table-driven importer of simple intrinsics
    if (isTableDriven)
    {
        unsigned                simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);
        CORINFO_ARG_LIST_HANDLE argList  = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        var_types               argType = TYP_UNKNOWN;

        assert(numArgs >= 0);
        assert(insOfHWIntrinsic(intrinsic, baseType) != INS_invalid);
        assert(simdSize == 32 || simdSize == 16);

        GenTree* retNode = nullptr;
        GenTree* op1     = nullptr;
        GenTree* op2     = nullptr;

        switch (numArgs)
        {
            case 0:
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
                break;
            case 1:
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
                break;
            case 2:
                argType = JITtype2varType(
                    strip(info.compCompHnd->getArgType(sig, info.compCompHnd->getArgNext(argList), &argClass)));
                op2 = getArgForHWIntrinsic(argType, argClass);

                op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, baseType, simdSize);
                break;

            case 3:
            {
                CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
                CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

                argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
                GenTree* op3 = getArgForHWIntrinsic(argType, argClass);

                op3 = addRangeCheckIfNeeded(intrinsic, op3, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2     = getArgForHWIntrinsic(argType, argClass);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, baseType, simdSize);
                break;
            }
            default:
                unreached();
        }
        return retNode;
    }

    // other intrinsics need special importation
    switch (isa)
    {
        case InstructionSet_SSE:
            return impSSEIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE2:
            return impSSE2Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE42:
            return impSSE42Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
            return impAvxOrAvx2Intrinsic(intrinsic, method, sig, mustExpand);

        case InstructionSet_AES:
            return impAESIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_BMI1:
            return impBMI1Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_BMI2:
            return impBMI2Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_FMA:
            return impFMAIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_LZCNT:
            return impLZCNTIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_PCLMULQDQ:
            return impPCLMULQDQIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_POPCNT:
            return impPOPCNTIntrinsic(intrinsic, method, sig, mustExpand);
        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSEIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    GenTree* retNode  = nullptr;
    GenTree* op1      = nullptr;
    GenTree* op2      = nullptr;
    GenTree* op3      = nullptr;
    GenTree* op4      = nullptr;
    int      simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);

    // The Prefetch and StoreFence intrinsics don't take any SIMD operands
    // and have a simdSize of 0
    assert((simdSize == 16) || (simdSize == 0));

    switch (intrinsic)
    {
        case NI_SSE_MoveMask:
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_INT);
            assert(getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args)) == TYP_FLOAT);
            op1     = impSIMDPopStack(TYP_SIMD16);
            retNode = gtNewSimdHWIntrinsicNode(TYP_INT, op1, intrinsic, TYP_FLOAT, simdSize);
            break;

        case NI_SSE_Prefetch0:
        case NI_SSE_Prefetch1:
        case NI_SSE_Prefetch2:
        case NI_SSE_PrefetchNonTemporal:
        {
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, intrinsic, TYP_UBYTE, 0);
            break;
        }

        case NI_SSE_StoreFence:
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, TYP_VOID, 0);
            break;

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE2Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    int       ival     = -1;
    int       simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);
    var_types baseType = TYP_UNKNOWN;
    var_types retType  = TYP_UNKNOWN;

    // The  fencing intrinsics don't take any operands and simdSize is 0
    assert((simdSize == 16) || (simdSize == 0));

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    CORINFO_CLASS_HANDLE    argClass;
    var_types               argType = TYP_UNKNOWN;

    switch (intrinsic)
    {
        case NI_SSE2_CompareLessThan:
        {
            assert(sig->numArgs == 2);
            op2      = impSIMDPopStack(TYP_SIMD16);
            op1      = impSIMDPopStack(TYP_SIMD16);
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            if (baseType == TYP_DOUBLE)
            {
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, intrinsic, baseType, simdSize);
            }
            else
            {
                retNode =
                    gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, NI_SSE2_CompareGreaterThan, baseType, simdSize);
            }
            break;
        }

        case NI_SSE2_LoadFence:
        case NI_SSE2_MemoryFence:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, TYP_VOID, simdSize);
            break;
        }

        case NI_SSE2_MoveMask:
        {
            assert(sig->numArgs == 1);
            retType = JITtype2varType(sig->retType);
            assert(retType == TYP_INT);
            op1      = impSIMDPopStack(TYP_SIMD16);
            baseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
            retNode  = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, NI_SSE2_StoreNonTemporal, op2->TypeGet(), 0);
            break;
        }

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE42Intrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types callType = JITtype2varType(sig->retType);

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    CORINFO_CLASS_HANDLE    argClass;
    CorInfoType             corType;
    switch (intrinsic)
    {
        case NI_SSE42_Crc32:
            assert(sig->numArgs == 2);
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            argList = info.compCompHnd->getArgNext(argList);                        // the second argument
            corType = strip(info.compCompHnd->getArgType(sig, argList, &argClass)); // type of the second argument

            retNode = gtNewScalarHWIntrinsicNode(callType, op1, op2, NI_SSE42_Crc32);

            // TODO - currently we use the BaseType to bring the type of the second argument
            // to the code generator. May encode the overload info in other way.
            retNode->gtHWIntrinsic.gtSIMDBaseType = JITtype2varType(corType);
            break;

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAvxOrAvx2Intrinsic(NamedIntrinsic        intrinsic,
                                         CORINFO_METHOD_HANDLE method,
                                         CORINFO_SIG_INFO*     sig,
                                         bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types baseType = TYP_UNKNOWN;
    int       simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);

    switch (intrinsic)
    {
        case NI_AVX_ExtractVector128:
        case NI_AVX2_ExtractVector128:
        {
            GenTree* lastOp = impPopStack().val;
            assert(lastOp->IsCnsIntOrI() || mustExpand);
            GenTree* vectorOp = impSIMDPopStack(TYP_SIMD32);
            if (sig->numArgs == 2)
            {
                baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
                if (!varTypeIsArithmetic(baseType))
                {
                    retNode = impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
                }
                else
                {
                    retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, lastOp, intrinsic, baseType, 32);
                }
            }
            else
            {
                assert(sig->numArgs == 3);
                op1                                    = impPopStack().val;
                CORINFO_ARG_LIST_HANDLE secondArg      = info.compCompHnd->getArgNext(sig->args);
                CORINFO_CLASS_HANDLE    secondArgClass = info.compCompHnd->getArgClass(sig, secondArg);
                baseType                               = getBaseTypeOfSIMDType(secondArgClass);
                retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, vectorOp, lastOp, intrinsic, baseType, 32);
            }
            break;
        }
        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAESIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impBMI1Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impBMI2Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impFMAIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impLZCNTIntrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    assert(sig->numArgs == 1);
    var_types callType = JITtype2varType(sig->retType);
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, NI_LZCNT_LeadingZeroCount);
}

GenTree* Compiler::impPCLMULQDQIntrinsic(NamedIntrinsic        intrinsic,
                                         CORINFO_METHOD_HANDLE method,
                                         CORINFO_SIG_INFO*     sig,
                                         bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impPOPCNTIntrinsic(NamedIntrinsic        intrinsic,
                                      CORINFO_METHOD_HANDLE method,
                                      CORINFO_SIG_INFO*     sig,
                                      bool                  mustExpand)
{
    assert(sig->numArgs == 1);
    var_types callType = JITtype2varType(sig->retType);
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, NI_POPCNT_PopCount);
}

#endif // FEATURE_HW_INTRINSICS
