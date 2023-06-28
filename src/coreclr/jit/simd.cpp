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
//    The type handle passed here can only be used in a subset of JIT-EE calls
//    since it may be called by promotion during prejit of a method that does
//    not version with SPC. See CORINFO_TYPE_LAYOUT_NODE for the contract on
//    the supported JIT-EE calls.
//
// TODO-Throughput: current implementation parses class name to find base type. Change
//         this when we implement  SIMD intrinsic identification for the final
//         product.
//
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
                        size = getVectorTByteLength();

                        if (size == 0)
                        {
                            return CORINFO_TYPE_UNDEF;
                        }
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

                if (!compOpportunisticallyDependsOn(InstructionSet_AVX))
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

                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512F))
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
        tree = impNormStructVal(tree, CHECK_SPILL_ALL);
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
    assert(tree->OperIsScalarLocal() || tree->IsLclVarAddr());
    LclVarDsc* lclVarDsc             = lvaGetDesc(tree->AsLclVarCommon());
    lclVarDsc->lvUsedInSIMDIntrinsic = true;
}

//-------------------------------------------------------------
// Check if two field address nodes reference at the same location.
//
// Arguments:
//   op1 - first field address
//   op2 - second field address
//
// Return Value:
//    If op1's parents node and op2's parents node are at the same
//    location, return true. Otherwise, return false
//
bool areFieldAddressesTheSame(GenTreeFieldAddr* op1, GenTreeFieldAddr* op2)
{
    assert(op1->OperIs(GT_FIELD_ADDR) && op2->OperIs(GT_FIELD_ADDR));

    GenTree* op1ObjRef = op1->GetFldObj();
    GenTree* op2ObjRef = op2->GetFldObj();
    while ((op1ObjRef != nullptr) && (op2ObjRef != nullptr))
    {
        assert(varTypeIsI(genActualType(op1ObjRef)) && varTypeIsI(genActualType(op2ObjRef)));

        if (op1ObjRef->OperGet() != op2ObjRef->OperGet())
        {
            break;
        }

        if ((op1ObjRef->OperIs(GT_LCL_VAR) || op1ObjRef->IsLclVarAddr()) &&
            (op1ObjRef->AsLclVarCommon()->GetLclNum() == op2ObjRef->AsLclVarCommon()->GetLclNum()))
        {
            return true;
        }

        if (op1ObjRef->OperIs(GT_IND))
        {
            op1ObjRef = op1ObjRef->AsIndir()->Addr();
            op2ObjRef = op2ObjRef->AsIndir()->Addr();
            continue;
        }

        if (op1ObjRef->OperIs(GT_FIELD_ADDR) &&
            (op1ObjRef->AsFieldAddr()->gtFldHnd == op2ObjRef->AsFieldAddr()->gtFldHnd))
        {
            op1ObjRef = op1ObjRef->AsFieldAddr()->GetFldObj();
            op2ObjRef = op2ObjRef->AsFieldAddr()->GetFldObj();
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
// areFieldsContiguous: Check whether two fields are contiguous.
//
// Arguments:
//      op1 - The first field indirection
//      op2 - The second field indirection
//
// Return Value:
//      If the first field is located before second field, and they are
//      located contiguously, then return true. Otherwise, return false.
//
bool Compiler::areFieldsContiguous(GenTreeIndir* op1, GenTreeIndir* op2)
{
    assert(op1->isIndir() && op2->isIndir());
    // TODO-1stClassStructs: delete once IND<struct> nodes are no more.
    assert(!op1->TypeIs(TYP_STRUCT) && !op2->TypeIs(TYP_STRUCT));

    var_types         op1Type      = op1->TypeGet();
    var_types         op2Type      = op2->TypeGet();
    GenTreeFieldAddr* op1Addr      = op1->Addr()->AsFieldAddr();
    GenTreeFieldAddr* op2Addr      = op2->Addr()->AsFieldAddr();
    unsigned          op1EndOffset = op1Addr->gtFldOffset + genTypeSize(op1Type);
    unsigned          op2Offset    = op2Addr->gtFldOffset;
    if ((op1Type == op2Type) && (op1EndOffset == op2Offset) && areFieldAddressesTheSame(op1Addr, op2Addr))
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
    assert(op1->isIndir() && op2->isIndir());
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
        (op1IndexNode->AsIntCon()->gtIconVal + 1 == op2IndexNode->AsIntCon()->gtIconVal))
    {
        if (op1ArrayRef->OperIs(GT_IND) && op2ArrayRef->OperIs(GT_IND))
        {
            GenTree* op1ArrayRefAddr = op1ArrayRef->AsIndir()->Addr();
            GenTree* op2ArrayRefAddr = op2ArrayRef->AsIndir()->Addr();
            if (op1ArrayRefAddr->OperIs(GT_FIELD_ADDR) && op2ArrayRefAddr->OperIs(GT_FIELD_ADDR) &&
                areFieldAddressesTheSame(op1ArrayRefAddr->AsFieldAddr(), op2ArrayRefAddr->AsFieldAddr()))
            {
                return true;
            }
        }
        else if (op1ArrayRef->OperIs(GT_LCL_VAR) && op2ArrayRef->OperIs(GT_LCL_VAR) &&
                 (op1ArrayRef->AsLclVar()->GetLclNum() == op2ArrayRef->AsLclVar()->GetLclNum()))
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

    if (op1->isIndir() && op2->isIndir())
    {
        GenTree* op1Addr = op1->AsIndir()->Addr();
        GenTree* op2Addr = op2->AsIndir()->Addr();

        if (op1Addr->OperIs(GT_INDEX_ADDR) && op2Addr->OperIs(GT_INDEX_ADDR))
        {
            return areArrayElementsContiguous(op1, op2);
        }
        if (op1Addr->OperIs(GT_FIELD_ADDR) && op2Addr->OperIs(GT_FIELD_ADDR))
        {
            return areFieldsContiguous(op1->AsIndir(), op2->AsIndir());
        }
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
//      Currently just supports GT_IND/GT_STOREIND(GT_INDEX_ADDR / GT_FIELD_ADDR), because we can only verify
//      those nodes are located contiguously or not. In future we should support more cases.
//
GenTree* Compiler::CreateAddressNodeForSimdHWIntrinsicCreate(GenTree* tree, var_types simdBaseType, unsigned simdSize)
{
    assert(tree->isIndir());
    GenTree* addr = tree->AsIndir()->Addr();

    if (addr->OperIs(GT_FIELD_ADDR))
    {
        assert(addr->AsFieldAddr()->IsInstance());

        // If the field is directly from a struct, then in this case, we should set this
        // struct's lvUsedInSIMDIntrinsic as true, so that this sturct won't be promoted.
        GenTree* objRef = addr->AsFieldAddr()->GetFldObj();
        if (objRef->IsLclVarAddr() && varTypeIsSIMD(lvaGetDesc(objRef->AsLclFld())))
        {
            setLclRelatedToSIMDIntrinsic(objRef);
        }

        return addr;
    }

    GenTree* arrayRef = addr->AsIndexAddr()->Arr();
    GenTree* index    = addr->AsIndexAddr()->Index();
    assert(index->IsCnsIntOrI());

    unsigned indexVal = (unsigned)index->AsIntCon()->gtIconVal;
    unsigned offset   = indexVal * genTypeSize(tree->TypeGet());

    // Generate the boundary check exception.
    // The length for boundary check should be the maximum index number which should be
    // (first argument's index number) + (how many array arguments we have) - 1 = indexVal + arrayElementsCount - 1
    //
    unsigned          arrayElementsCount = simdSize / genTypeSize(simdBaseType);
    GenTree*          checkIndexExpr     = gtNewIconNode(indexVal + arrayElementsCount - 1);
    GenTreeArrLen*    arrLen = gtNewArrLen(TYP_INT, arrayRef, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
    GenTreeBoundsChk* arrBndsChk =
        new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(checkIndexExpr, arrLen, SCK_ARG_RNG_EXCPN);

    offset += OFFSETOF__CORINFO_Array__data;
    GenTree* address = gtNewOperNode(GT_COMMA, arrayRef->TypeGet(), arrBndsChk, gtCloneExpr(arrayRef));
    address          = gtNewOperNode(GT_ADD, TYP_BYREF, address, gtNewIconNode(offset, TYP_I_IMPL));

    return address;
}

//-------------------------------------------------------------------------------
// impMarkContiguousSIMDFieldStores: Try to identify if there are contiguous
// assignments from SIMD field to memory. If there are, then mark the related
// lclvar so that it won't be promoted.
//
// Arguments:
//      stmt - GenTree*. Input statement node.
//
void Compiler::impMarkContiguousSIMDFieldStores(Statement* stmt)
{
    if (opts.OptimizationDisabled())
    {
        return;
    }
    GenTree* expr = stmt->GetRootNode();
    if (expr->OperIsStore() && expr->TypeIs(TYP_FLOAT))
    {
        GenTree*  curValue       = expr->Data();
        unsigned  index          = 0;
        var_types simdBaseType   = curValue->TypeGet();
        unsigned  simdSize       = 0;
        GenTree*  srcSimdLclAddr = getSIMDStructFromField(curValue, &index, &simdSize, true);

        if (srcSimdLclAddr == nullptr || simdBaseType != TYP_FLOAT)
        {
            fgPreviousCandidateSIMDFieldStoreStmt = nullptr;
        }
        else if (index == 0)
        {
            fgPreviousCandidateSIMDFieldStoreStmt = stmt;
        }
        else if (fgPreviousCandidateSIMDFieldStoreStmt != nullptr)
        {
            assert(index > 0);
            GenTree* curStore  = expr;
            GenTree* prevStore = fgPreviousCandidateSIMDFieldStoreStmt->GetRootNode();
            GenTree* prevValue = prevStore->Data();
            if (!areArgumentsContiguous(prevStore, curStore) || !areArgumentsContiguous(prevValue, curValue))
            {
                fgPreviousCandidateSIMDFieldStoreStmt = nullptr;
            }
            else
            {
                if (index == (simdSize / genTypeSize(simdBaseType) - 1))
                {
                    // Successfully found the pattern, mark the lclvar as UsedInSIMDIntrinsic
                    setLclRelatedToSIMDIntrinsic(srcSimdLclAddr);

                    if (curStore->OperIs(GT_STOREIND) && curStore->AsIndir()->Addr()->OperIs(GT_FIELD_ADDR))
                    {
                        GenTreeFieldAddr* addr = curStore->AsIndir()->Addr()->AsFieldAddr();
                        if (addr->IsInstance())
                        {
                            GenTree* objRef = addr->GetFldObj();
                            if (objRef->IsLclVarAddr() && varTypeIsStruct(lvaGetDesc(objRef->AsLclFld())))
                            {
                                setLclRelatedToSIMDIntrinsic(objRef);
                            }
                        }
                    }
                }
                else
                {
                    fgPreviousCandidateSIMDFieldStoreStmt = stmt;
                }
            }
        }
    }
    else
    {
        fgPreviousCandidateSIMDFieldStoreStmt = nullptr;
    }
}
#endif // FEATURE_SIMD
