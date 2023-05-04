// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//   SIMD Support
//
// IMPORTANT NOTES AND CAVEATS:
//
// This implementation is preliminary, and may change dramatically.
//
// New JIT types, TYP_SIMDxx, are introduced, and the hwintrinsics are created as GT_HWINTRINSC nodes.
// Nodes of SIMD types will be typed as TYP_SIMD* (e.g. TYP_SIMD8, TYP_SIMD16, etc.).
//
// Note that currently the "reference implementation" is the same as the runtime dll.  As such, it is currently
// providing implementations for those methods not currently supported by the JIT as intrinsics.
//
// These are currently recognized using string compares, in order to provide an implementation in the JIT
// without taking a dependency on the VM.
// Furthermore, in the CTP, in order to limit the impact of doing these string compares
// against assembly names, we only look for the SIMDVector assembly if we are compiling a class constructor.  This
// makes it somewhat more "pay for play" but is a significant usability compromise.
// This has been addressed for RTM by doing the assembly recognition in the VM.
// --------------------------------------------------------------------------------------

#include "jitpch.h"
#include "simd.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// getSIMDVectorLength: Get the length (number of elements of base type) of
//                      SIMD Vector given its size and base (element) type.
//
// Arguments:
//    simdSize   - size of the SIMD vector
//    baseType   - type of the elements of the SIMD vector
//
// static
int Compiler::getSIMDVectorLength(unsigned simdSize, var_types baseType)
{
    return simdSize / genTypeSize(baseType);
}

//------------------------------------------------------------------------
// Get the length (number of elements of base type) of SIMD Vector given by typeHnd.
//
// Arguments:
//    typeHnd  - type handle of the SIMD vector
//
int Compiler::getSIMDVectorLength(CORINFO_CLASS_HANDLE typeHnd)
{
    unsigned    sizeBytes   = 0;
    CorInfoType baseJitType = getBaseJitTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
    var_types   baseType    = JitType2PreciseVarType(baseJitType);
    return getSIMDVectorLength(sizeBytes, baseType);
}

//------------------------------------------------------------------------
// Get the preferred alignment of SIMD vector type for better performance.
//
// Arguments:
//    typeHnd  - type handle of the SIMD vector
//
int Compiler::getSIMDTypeAlignment(var_types simdType)
{
    unsigned size = genTypeSize(simdType);

#ifdef TARGET_XARCH
    // Fixed length vectors have the following alignment preference
    // Vector2   = 8 byte alignment
    // Vector3/4 = 16-byte alignment

    // preferred alignment for SSE2 128-bit vectors is 16-bytes
    if (size == 8)
    {
        return 8;
    }
    else if (size <= 16)
    {
        assert((size == 12) || (size == 16));
        return 16;
    }
    else if (size == 32)
    {
        return 32;
    }
    else
    {
        assert(size == 64);
        return 64;
    }
#elif defined(TARGET_ARM64)
    // preferred alignment for 64-bit vectors is 8-bytes.
    // For everything else, 16-bytes.
    return (size == 8) ? 8 : 16;
#else
    assert(!"getSIMDTypeAlignment() unimplemented on target arch");
    unreached();
#endif
}

//------------------------------------------------------------------------
// Get, and allocate if necessary, the SIMD temp used for various operations.
// The temp is allocated as the maximum sized type of all operations required.
//
// Arguments:
//    simdType - Required SIMD type
//
// Returns:
//    The temp number
//
unsigned Compiler::getSIMDInitTempVarNum(var_types simdType)
{
    if (lvaSIMDInitTempVarNum == BAD_VAR_NUM)
    {
        JITDUMP("Allocating SIMDInitTempVar as %s\n", varTypeName(simdType));
        lvaSIMDInitTempVarNum                  = lvaGrabTempWithImplicitUse(false DEBUGARG("SIMDInitTempVar"));
        lvaTable[lvaSIMDInitTempVarNum].lvType = simdType;
    }
    else if (genTypeSize(lvaTable[lvaSIMDInitTempVarNum].lvType) < genTypeSize(simdType))
    {
        // We want the largest required type size for the temp.
        JITDUMP("Increasing SIMDInitTempVar type size from %s to %s\n",
                varTypeName(lvaTable[lvaSIMDInitTempVarNum].lvType), varTypeName(simdType));
        lvaTable[lvaSIMDInitTempVarNum].lvType = simdType;
    }
    return lvaSIMDInitTempVarNum;
}

//----------------------------------------------------------------------------------
// Return the base type and size of SIMD vector type given its type handle.
//
// Arguments:
//    typeHnd   - The handle of the type we're interested in.
//    sizeBytes - out param
//
// Return Value:
//    base type of SIMD vector.
//    sizeBytes if non-null is set to size in bytes.
//
// Notes:
//    If the size of the struct is already known call structSizeMightRepresentSIMDType
//    to determine if this api needs to be called.
//
// TODO-Throughput: current implementation parses class name to find base type. Change
//         this when we implement  SIMD intrinsic identification for the final
//         product.
CorInfoType Compiler::getBaseJitTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd, unsigned* sizeBytes /*= nullptr */)
{
    if (m_simdHandleCache == nullptr)
    {
        if (impInlineInfo == nullptr)
        {
            m_simdHandleCache = new (this, CMK_Generic) SIMDHandlesCache();
        }
        else
        {
            // Steal the inliner compiler's cache (create it if not available).

            if (impInlineInfo->InlineRoot->m_simdHandleCache == nullptr)
            {
                impInlineInfo->InlineRoot->m_simdHandleCache = new (this, CMK_Generic) SIMDHandlesCache();
            }

            m_simdHandleCache = impInlineInfo->InlineRoot->m_simdHandleCache;
        }
    }

    if (sizeBytes != nullptr)
    {
        *sizeBytes = 0;
    }

    if ((typeHnd == nullptr) || !isIntrinsicType(typeHnd))
    {
        return CORINFO_TYPE_UNDEF;
    }

    const char* namespaceName;
    const char* className = getClassNameFromMetadata(typeHnd, &namespaceName);

    // fast path search using cached type handles of important types
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned    size            = 0;

    if (isNumericsNamespace(namespaceName))
    {
        switch (className[0])
        {
            case 'P':
            {
                if (strcmp(className, "Plane") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP("  Known type Plane\n");
                m_simdHandleCache->PlaneHandle = typeHnd;

                simdBaseJitType = CORINFO_TYPE_FLOAT;
                size            = 4 * genTypeSize(TYP_FLOAT);
                break;
            }

            case 'Q':
            {
                if (strcmp(className, "Quaternion") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP("  Known type Quaternion\n");
                m_simdHandleCache->QuaternionHandle = typeHnd;

                simdBaseJitType = CORINFO_TYPE_FLOAT;
                size            = 4 * genTypeSize(TYP_FLOAT);
                break;
            }

            case 'V':
            {
                if (strncmp(className, "Vector", 6) != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                switch (className[6])
                {
                    case '\0':
                    {
                        JITDUMP(" Found type Vector\n");
                        m_simdHandleCache->VectorHandle = typeHnd;

                        size = getSIMDVectorRegisterByteLength();
                        break;
                    }

                    case '2':
                    {
                        if (className[7] != '\0')
                        {
                            return CORINFO_TYPE_UNDEF;
                        }

                        JITDUMP(" Found Vector2\n");
                        m_simdHandleCache->Vector2Handle = typeHnd;

                        simdBaseJitType = CORINFO_TYPE_FLOAT;
                        size            = 2 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '3':
                    {
                        if (className[7] != '\0')
                        {
                            return CORINFO_TYPE_UNDEF;
                        }

                        JITDUMP(" Found Vector3\n");
                        m_simdHandleCache->Vector3Handle = typeHnd;

                        simdBaseJitType = CORINFO_TYPE_FLOAT;
                        size            = 3 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '4':
                    {
                        if (className[7] != '\0')
                        {
                            return CORINFO_TYPE_UNDEF;
                        }

                        JITDUMP(" Found Vector4\n");
                        m_simdHandleCache->Vector4Handle = typeHnd;

                        simdBaseJitType = CORINFO_TYPE_FLOAT;
                        size            = 4 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '`':
                    {
                        if ((className[7] != '1') || (className[8] != '\0'))
                        {
                            return CORINFO_TYPE_UNDEF;
                        }

                        CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                        simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                        if ((simdBaseJitType < CORINFO_TYPE_BYTE) || (simdBaseJitType > CORINFO_TYPE_DOUBLE))
                        {
                            return CORINFO_TYPE_UNDEF;
                        }

                        JITDUMP(" Found Vector<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));

                        size = getSIMDVectorRegisterByteLength();
                        break;
                    }

                    default:
                    {
                        return CORINFO_TYPE_UNDEF;
                    }
                }
                break;
            }

            default:
            {
                return CORINFO_TYPE_UNDEF;
            }
        }
    }
#ifdef FEATURE_HW_INTRINSICS
    else
    {
        size = info.compCompHnd->getClassSize(typeHnd);

        switch (size)
        {
#if defined(TARGET_ARM64)
            case 8:
            {
                if (strcmp(className, "Vector64`1") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseJitType < CORINFO_TYPE_BYTE) || (simdBaseJitType > CORINFO_TYPE_DOUBLE))
                {
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP(" Found Vector64<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));
                break;
            }
#endif // TARGET_ARM64

            case 16:
            {
                if (strcmp(className, "Vector128`1") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseJitType < CORINFO_TYPE_BYTE) || (simdBaseJitType > CORINFO_TYPE_DOUBLE))
                {
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP(" Found Vector128<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));
                break;
            }

#if defined(TARGET_XARCH)
            case 32:
            {
                if (strcmp(className, "Vector256`1") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseJitType < CORINFO_TYPE_BYTE) || (simdBaseJitType > CORINFO_TYPE_DOUBLE))
                {
                    return CORINFO_TYPE_UNDEF;
                }

                if (!compExactlyDependsOn(InstructionSet_AVX))
                {
                    // We must treat as a regular struct if AVX isn't supported
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP(" Found Vector256<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));
                break;
            }

            case 64:
            {
                if (strcmp(className, "Vector512`1") != 0)
                {
                    return CORINFO_TYPE_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseJitType < CORINFO_TYPE_BYTE) || (simdBaseJitType > CORINFO_TYPE_DOUBLE))
                {
                    return CORINFO_TYPE_UNDEF;
                }

                if (!compExactlyDependsOn(InstructionSet_AVX512F))
                {
                    // We must treat as a regular struct if AVX512F isn't supported
                    return CORINFO_TYPE_UNDEF;
                }

                JITDUMP(" Found Vector512<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));
                break;
            }
#endif // TARGET_XARCH

            default:
            {
                return CORINFO_TYPE_UNDEF;
            }
        }
    }
#endif // FEATURE_HW_INTRINSICS

    if (sizeBytes != nullptr)
    {
        *sizeBytes = size;
    }

    if (simdBaseJitType != CORINFO_TYPE_UNDEF)
    {
        assert(size == info.compCompHnd->getClassSize(typeHnd));
        setUsesSIMDTypes(true);
    }

    return simdBaseJitType;
}

//------------------------------------------------------------------------
// impSIMDPopStack: Pop a SIMD value from the importer's stack.
//
// Spills calls with return buffers to temps.
//
GenTree* Compiler::impSIMDPopStack()
{
    StackEntry se   = impPopStack();
    GenTree*   tree = se.val;
    assert(varTypeIsSIMD(tree));

    // Handle calls that may return the struct via a return buffer.
    if (tree->OperIs(GT_CALL, GT_RET_EXPR))
    {
        tree = impNormStructVal(tree, se.seTypeInfo.GetClassHandle(), CHECK_SPILL_ALL);
    }

    return tree;
}

//-------------------------------------------------------------------
// Set the flag that indicates that the lclVar referenced by this tree
// is used in a SIMD intrinsic.
// Arguments:
//      tree - GenTree*
//
void Compiler::setLclRelatedToSIMDIntrinsic(GenTree* tree)
{
    assert(tree->OperIs(GT_LCL_VAR) || tree->IsLclVarAddr());
    LclVarDsc* lclVarDsc             = lvaGetDesc(tree->AsLclVarCommon());
    lclVarDsc->lvUsedInSIMDIntrinsic = true;
}

//-------------------------------------------------------------
// Check if two field nodes reference at the same memory location.
// Notice that this check is just based on pattern matching.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//    If op1's parents node and op2's parents node are at the same location, return true. Otherwise, return false

bool areFieldsParentsLocatedSame(GenTree* op1, GenTree* op2)
{
    assert(op1->OperGet() == GT_FIELD);
    assert(op2->OperGet() == GT_FIELD);

    GenTree* op1ObjRef = op1->AsField()->GetFldObj();
    GenTree* op2ObjRef = op2->AsField()->GetFldObj();
    while (op1ObjRef != nullptr && op2ObjRef != nullptr)
    {
        if (op1ObjRef->OperGet() != op2ObjRef->OperGet())
        {
            break;
        }

        if ((op1ObjRef->OperIs(GT_LCL_VAR) || op1ObjRef->IsLclVarAddr()) &&
            (op1ObjRef->AsLclVarCommon()->GetLclNum() == op2ObjRef->AsLclVarCommon()->GetLclNum()))
        {
            return true;
        }

        if (op1ObjRef->OperIs(GT_FIELD) && (op1ObjRef->AsField()->gtFldHnd == op2ObjRef->AsField()->gtFldHnd))
        {
            op1ObjRef = op1ObjRef->AsField()->GetFldObj();
            op2ObjRef = op2ObjRef->AsField()->GetFldObj();
            continue;
        }
        else
        {
            break;
        }
    }

    return false;
}

//----------------------------------------------------------------------
// Check whether two field are contiguous
// Arguments:
//      first - GenTree*. The Type of the node should be TYP_FLOAT
//      second - GenTree*. The Type of the node should be TYP_FLOAT
// Return Value:
//      if the first field is located before second field, and they are located contiguously,
//      then return true. Otherwise, return false.

bool Compiler::areFieldsContiguous(GenTree* first, GenTree* second)
{
    assert(first->OperGet() == GT_FIELD);
    assert(second->OperGet() == GT_FIELD);
    assert(first->gtType == TYP_FLOAT);
    assert(second->gtType == TYP_FLOAT);

    var_types firstFieldType  = first->gtType;
    var_types secondFieldType = second->gtType;

    unsigned firstFieldEndOffset = first->AsField()->gtFldOffset + genTypeSize(firstFieldType);
    unsigned secondFieldOffset   = second->AsField()->gtFldOffset;
    if (firstFieldEndOffset == secondFieldOffset && firstFieldType == secondFieldType &&
        areFieldsParentsLocatedSame(first, second))
    {
        return true;
    }

    return false;
}

//----------------------------------------------------------------------
// areLocalFieldsContiguous: Check whether two local field are contiguous
//
// Arguments:
//    first - the first local field
//    second - the second local field
//
// Return Value:
//    If the first field is located before second field, and they are located contiguously,
//    then return true. Otherwise, return false.
//
bool Compiler::areLocalFieldsContiguous(GenTreeLclFld* first, GenTreeLclFld* second)
{
    assert(first->TypeIs(TYP_FLOAT));
    assert(second->TypeIs(TYP_FLOAT));

    return (first->TypeGet() == second->TypeGet()) &&
           (first->GetLclOffs() + genTypeSize(first->TypeGet()) == second->GetLclOffs());
}

//-------------------------------------------------------------------------------
// Check whether two array element nodes are located contiguously or not.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//      if the array element op1 is located before array element op2, and they are contiguous,
//      then return true. Otherwise, return false.
// TODO-CQ:
//      Right this can only check array element with const number as index. In future,
//      we should consider to allow this function to check the index using expression.
//
bool Compiler::areArrayElementsContiguous(GenTree* op1, GenTree* op2)
{
    assert(op1->OperIs(GT_IND) && op2->OperIs(GT_IND));
    assert(!op1->TypeIs(TYP_STRUCT) && (op1->TypeGet() == op2->TypeGet()));

    GenTreeIndexAddr* op1IndexAddr = op1->AsIndir()->Addr()->AsIndexAddr();
    GenTreeIndexAddr* op2IndexAddr = op2->AsIndir()->Addr()->AsIndexAddr();

    GenTree* op1ArrayRef = op1IndexAddr->Arr();
    GenTree* op2ArrayRef = op2IndexAddr->Arr();
    assert(op1ArrayRef->TypeGet() == TYP_REF);
    assert(op2ArrayRef->TypeGet() == TYP_REF);

    GenTree* op1IndexNode = op1IndexAddr->Index();
    GenTree* op2IndexNode = op2IndexAddr->Index();
    if ((op1IndexNode->OperGet() == GT_CNS_INT && op2IndexNode->OperGet() == GT_CNS_INT) &&
        op1IndexNode->AsIntCon()->gtIconVal + 1 == op2IndexNode->AsIntCon()->gtIconVal)
    {
        if (op1ArrayRef->OperIs(GT_FIELD) && op2ArrayRef->OperIs(GT_FIELD) &&
            areFieldsParentsLocatedSame(op1ArrayRef, op2ArrayRef))
        {
            return true;
        }
        else if (op1ArrayRef->OperIs(GT_LCL_VAR) && op2ArrayRef->OperIs(GT_LCL_VAR) &&
                 op1ArrayRef->AsLclVarCommon()->GetLclNum() == op2ArrayRef->AsLclVarCommon()->GetLclNum())
        {
            return true;
        }
    }
    return false;
}

//-------------------------------------------------------------------------------
// Check whether two argument nodes are contiguous or not.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//      if the argument node op1 is located before argument node op2, and they are located contiguously,
//      then return true. Otherwise, return false.
// TODO-CQ:
//      Right now this can only check field and array. In future we should add more cases.
//
bool Compiler::areArgumentsContiguous(GenTree* op1, GenTree* op2)
{
    if (op1->TypeGet() != op2->TypeGet())
    {
        return false;
    }

    assert(!op1->TypeIs(TYP_STRUCT));

    if (op1->OperIs(GT_IND) && op1->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR) && op2->OperIs(GT_IND) &&
        op2->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR))
    {
        return areArrayElementsContiguous(op1, op2);
    }
    else if (op1->OperIs(GT_FIELD) && op2->OperIs(GT_FIELD))
    {
        return areFieldsContiguous(op1, op2);
    }
    else if (op1->OperIs(GT_LCL_FLD) && op2->OperIs(GT_LCL_FLD))
    {
        return areLocalFieldsContiguous(op1->AsLclFld(), op2->AsLclFld());
    }
    return false;
}

//--------------------------------------------------------------------------------------------------------
// CreateAddressNodeForSimdHWIntrinsicCreate: Generate the address node if we want to initialize a simd type
// from first argument's address.
//
// Arguments:
//      tree         - The tree node which is used to get the address for indir.
//      simdBaseType - The type of the elements in the SIMD node
//      simdsize     - The simd vector size.
//
// Return value:
//      return the address node.
//
// TODO-CQ:
//      Currently just supports GT_FIELD and GT_IND(GT_INDEX_ADDR), because we can only verify those nodes
//      are located contiguously or not. In future we should support more cases.
//
GenTree* Compiler::CreateAddressNodeForSimdHWIntrinsicCreate(GenTree* tree, var_types simdBaseType, unsigned simdSize)
{
    GenTree*  byrefNode = nullptr;
    unsigned  offset    = 0;
    var_types baseType  = tree->gtType;

    if (tree->OperIs(GT_FIELD))
    {
        GenTree* objRef = tree->AsField()->GetFldObj();
        if ((objRef != nullptr) && objRef->IsLclVarAddr())
        {
            // If the field is directly from a struct, then in this case,
            // we should set this struct's lvUsedInSIMDIntrinsic as true,
            // so that this sturct won't be promoted.
            // e.g. s.x x is a field, and s is a struct, then we should set the s's lvUsedInSIMDIntrinsic as true.
            // so that s won't be promoted.
            // Notice that if we have a case like s1.s2.x. s1 s2 are struct, and x is a field, then it is possible that
            // s1 can be promoted, so that s2 can be promoted. The reason for that is if we don't allow s1 to be
            // promoted, then this will affect the other optimizations which are depend on s1's struct promotion.
            // TODO-CQ:
            //  In future, we should optimize this case so that if there is a nested field like s1.s2.x and s1.s2.x's
            //  address is used for initializing the vector, then s1 can be promoted but s2 can't.
            if (varTypeIsSIMD(lvaGetDesc(objRef->AsLclFld())))
            {
                setLclRelatedToSIMDIntrinsic(objRef);
            }
        }

        byrefNode = gtCloneExpr(tree->AsField()->GetFldObj());
        assert(byrefNode != nullptr);
        offset = tree->AsField()->gtFldOffset;
    }
    else
    {
        assert(tree->OperIs(GT_IND) && tree->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR));

        GenTreeIndexAddr* indexAddr = tree->AsIndir()->Addr()->AsIndexAddr();
        GenTree*          arrayRef  = indexAddr->Arr();
        GenTree*          index     = indexAddr->Index();
        assert(index->IsCnsIntOrI());

        GenTree* checkIndexExpr = nullptr;
        unsigned indexVal       = (unsigned)index->AsIntCon()->gtIconVal;
        offset                  = indexVal * genTypeSize(tree->TypeGet());

        // Generate the boundary check exception.
        // The length for boundary check should be the maximum index number which should be
        // (first argument's index number) + (how many array arguments we have) - 1
        // = indexVal + arrayElementsCount - 1
        unsigned arrayElementsCount = simdSize / genTypeSize(simdBaseType);
        checkIndexExpr              = gtNewIconNode(indexVal + arrayElementsCount - 1);
        GenTreeArrLen*    arrLen    = gtNewArrLen(TYP_INT, arrayRef, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
        GenTreeBoundsChk* arrBndsChk =
            new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(checkIndexExpr, arrLen, SCK_ARG_RNG_EXCPN);

        offset += OFFSETOF__CORINFO_Array__data;
        byrefNode = gtNewOperNode(GT_COMMA, arrayRef->TypeGet(), arrBndsChk, gtCloneExpr(arrayRef));
    }

    GenTree* address = byrefNode;
    if (offset != 0)
    {
        address = gtNewOperNode(GT_ADD, TYP_BYREF, address, gtNewIconNode(offset, TYP_I_IMPL));
    }

    return address;
}

//-------------------------------------------------------------------------------
// impMarkContiguousSIMDFieldAssignments: Try to identify if there are contiguous
// assignments from SIMD field to memory. If there are, then mark the related
// lclvar so that it won't be promoted.
//
// Arguments:
//      stmt - GenTree*. Input statement node.
//
void Compiler::impMarkContiguousSIMDFieldAssignments(Statement* stmt)
{
    if (opts.OptimizationDisabled())
    {
        return;
    }
    GenTree* expr = stmt->GetRootNode();
    if (expr->OperGet() == GT_ASG && expr->TypeGet() == TYP_FLOAT)
    {
        GenTree*    curDst          = expr->AsOp()->gtOp1;
        GenTree*    curSrc          = expr->AsOp()->gtOp2;
        unsigned    index           = 0;
        CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
        unsigned    simdSize        = 0;
        GenTree*    srcSimdLclAddr  = getSIMDStructFromField(curSrc, &simdBaseJitType, &index, &simdSize, true);

        if (srcSimdLclAddr == nullptr || simdBaseJitType != CORINFO_TYPE_FLOAT)
        {
            fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
        }
        else if (index == 0)
        {
            fgPreviousCandidateSIMDFieldAsgStmt = stmt;
        }
        else if (fgPreviousCandidateSIMDFieldAsgStmt != nullptr)
        {
            assert(index > 0);
            var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
            GenTree*  prevAsgExpr  = fgPreviousCandidateSIMDFieldAsgStmt->GetRootNode();
            GenTree*  prevDst      = prevAsgExpr->AsOp()->gtOp1;
            GenTree*  prevSrc      = prevAsgExpr->AsOp()->gtOp2;
            if (!areArgumentsContiguous(prevDst, curDst) || !areArgumentsContiguous(prevSrc, curSrc))
            {
                fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
            }
            else
            {
                if (index == (simdSize / genTypeSize(simdBaseType) - 1))
                {
                    // Successfully found the pattern, mark the lclvar as UsedInSIMDIntrinsic
                    setLclRelatedToSIMDIntrinsic(srcSimdLclAddr);

                    if (curDst->OperIs(GT_FIELD) && curDst->AsField()->IsInstance())
                    {
                        GenTree* objRef = curDst->AsField()->GetFldObj();
                        if (objRef->IsLclVarAddr() && varTypeIsStruct(lvaGetDesc(objRef->AsLclFld())))
                        {
                            setLclRelatedToSIMDIntrinsic(objRef);
                        }
                    }
                }
                else
                {
                    fgPreviousCandidateSIMDFieldAsgStmt = stmt;
                }
            }
        }
    }
    else
    {
        fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
    }
}
#endif // FEATURE_SIMD
