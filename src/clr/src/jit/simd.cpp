//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
//   SIMD Support
//
// IMPORTANT NOTES AND CAVEATS:
//
// This implementation is preliminary, and may change dramatically.
//
// New JIT types, TYP_SIMDxx, are introduced, and the SIMD intrinsics are created as GT_SIMD nodes.
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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator.

#ifdef FEATURE_SIMD


// Intrinsic Id to intrinsic info map
const SIMDIntrinsicInfo simdIntrinsicInfoArray[] = 
{
    #define SIMD_INTRINSIC(mname, inst, id, name, retType, argCount, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10)   \
                           {SIMDIntrinsic##id, mname, inst, retType, argCount, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10},
    #include "simdintrinsiclist.h"
};

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
    unsigned sizeBytes = 0;        
    var_types baseType = getBaseTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
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
#ifdef _TARGET_AMD64_
    // Fixed length vectors have the following alignment preference
    // Vector2/3 = 8 byte alignment
    // Vector4 = 16-byte alignment
    unsigned size = genTypeSize(simdType);

    // preferred alignment for SSE2 128-bit vectors is 16-bytes
    if (size == 8) 
        return 8;

    // As per Intel manual, AVX vectors preferred alignment is 32-bytes but on Amd64 
    // RSP/EBP is aligned at 16-bytes, therefore to align SIMD types at 32-bytes we need even
    // RSP/EBP to be 32-byte aligned. It is not clear whether additional stack space used in
    // aligning stack is worth the benefit and for now will use 16-byte alignment for AVX
    // 256-bit vectors with unaligned load/stores to/from memory.
    return 16;
#else
    assert(!"getSIMDTypeAlignment() unimplemented on target arch");
    unreached();
#endif
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
// TODO-Throughput: current implementation parses class name to find base type. Change 
//         this when we implement  SIMD intrinsic identification for the final
//         product.
//
var_types Compiler::getBaseTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd,
                                                 unsigned *sizeBytes /*= nullptr */)
{
    assert(featureSIMD);   
    if (typeHnd == nullptr)
    {
        return TYP_UNKNOWN;
    }

    // fast path search using cached type handles of important types
    var_types simdBaseType = TYP_UNKNOWN;
    unsigned size = 0;

    // Early return if it is not a SIMD module.
    if (!isSIMDClass(typeHnd))
    {
        return TYP_UNKNOWN;
    }

    // The most likely to be used type handles are looked up first followed by
    // less likely to be used type handles
    if (typeHnd == SIMDFloatHandle)
    {
        simdBaseType = TYP_FLOAT;
        JITDUMP("  Known type SIMD Vector<Float>\n");
    }
    else if (typeHnd == SIMDIntHandle)
    {
        simdBaseType = TYP_INT;
        JITDUMP("  Known type SIMD Vector<Int>\n");
    }
    else if (typeHnd == SIMDVector2Handle)
    {
        simdBaseType = TYP_FLOAT;
        size = 2*genTypeSize(TYP_FLOAT);
        assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
        JITDUMP("  Known type Vector2\n");
    }
    else if (typeHnd == SIMDVector3Handle)
    {
        simdBaseType = TYP_FLOAT;
        size = 3*genTypeSize(TYP_FLOAT);
        assert(size == info.compCompHnd->getClassSize(typeHnd));
        JITDUMP("  Known type Vector3\n");
    }
    else if (typeHnd == SIMDVector4Handle)
    {
        simdBaseType = TYP_FLOAT;
        size = 4*genTypeSize(TYP_FLOAT);
        assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
        JITDUMP("  Known type Vector4\n");
    }
    else if (typeHnd == SIMDVectorHandle)
    {
        JITDUMP("  Known type Vector\n");
    }
    else if (typeHnd == SIMDUShortHandle)
    {
        simdBaseType = TYP_CHAR;
        JITDUMP("  Known type SIMD Vector<ushort>\n");
    }
    else if (typeHnd == SIMDUByteHandle)
    {
        simdBaseType = TYP_UBYTE;
        JITDUMP("  Known type SIMD Vector<ubyte>\n");
    }
    else if (typeHnd == SIMDDoubleHandle)
    {
        simdBaseType = TYP_DOUBLE;
        JITDUMP("  Known type SIMD Vector<Double>\n");
    }
    else if (typeHnd == SIMDLongHandle)
    {
        simdBaseType = TYP_LONG;
        JITDUMP("  Known type SIMD Vector<Long>\n");
    }
    else if (typeHnd == SIMDShortHandle)
    {
        simdBaseType = TYP_SHORT;
        JITDUMP("  Known type SIMD Vector<short>\n");
    }
    else if (typeHnd == SIMDByteHandle)
    {
        simdBaseType = TYP_BYTE;
        JITDUMP("  Known type SIMD Vector<byte>\n");
    }
    else if (typeHnd == SIMDUIntHandle)
    {
        simdBaseType = TYP_UINT;
        JITDUMP("  Known type SIMD Vector<uint>\n");
    }
    else if (typeHnd == SIMDULongHandle)
    {
        simdBaseType = TYP_ULONG;
        JITDUMP("  Known type SIMD Vector<ulong>\n");
    }

    // slow path search
    if (simdBaseType == TYP_UNKNOWN)
    {
        // Doesn't match with any of the cached type handles.
        // Obtain base type by parsing fully qualified class name.
        //
        // TODO-Throughput: implement product shipping solution to query base type.
        WCHAR className[256] = {0};
        WCHAR *pbuf = &className[0];
        int len = sizeof(className)/sizeof(className[0]);
        info.compCompHnd->appendClassName(&pbuf, &len, typeHnd, TRUE, FALSE, FALSE);   
        noway_assert(pbuf < &className[256]);
        JITDUMP("SIMD Candidate Type %S\n", className);

        if (wcsncmp(className, W("System.Numerics."), 16) == 0)
        {
            if (wcsncmp(&(className[16]), W("Vector`1["), 9) == 0)
            {
                if (wcsncmp(&(className[25]), W("System.Single"), 13) == 0)
                {
                    SIMDFloatHandle = typeHnd;
                    simdBaseType = TYP_FLOAT;
                    JITDUMP("  Found type SIMD Vector<Float>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.Int32"), 12) == 0)
                {
                    SIMDIntHandle = typeHnd;
                    simdBaseType = TYP_INT;
                    JITDUMP("  Found type SIMD Vector<Int>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.UInt16"), 13) == 0)
                {
                    SIMDUShortHandle = typeHnd;
                    simdBaseType = TYP_CHAR;
                    JITDUMP("  Found type SIMD Vector<ushort>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.Byte"), 11) == 0)
                {
                    SIMDUByteHandle = typeHnd;
                    simdBaseType = TYP_UBYTE;
                    JITDUMP("  Found type SIMD Vector<ubyte>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.Double"), 13) == 0)
                {
                    SIMDDoubleHandle = typeHnd;
                    simdBaseType = TYP_DOUBLE;
                    JITDUMP("  Found type SIMD Vector<Double>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.Int64"), 12) == 0)
                {
                    SIMDLongHandle = typeHnd;
                    simdBaseType = TYP_LONG;
                    JITDUMP("  Found type SIMD Vector<Long>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.Int16"), 12) == 0)
                {
                    SIMDShortHandle = typeHnd;
                    simdBaseType = TYP_SHORT;
                    JITDUMP("  Found type SIMD Vector<short>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.SByte"), 12) == 0)
                {
                    SIMDByteHandle = typeHnd;
                    simdBaseType = TYP_BYTE;
                    JITDUMP("  Found type SIMD Vector<byte>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.UInt32"), 13) == 0)
                {
                    SIMDUIntHandle = typeHnd;
                    simdBaseType = TYP_UINT;
                    JITDUMP("  Found type SIMD Vector<uint>\n");
                }
                else if (wcsncmp(&(className[25]), W("System.UInt64"), 13) == 0)
                {
                    SIMDULongHandle = typeHnd;
                    simdBaseType = TYP_ULONG;
                    JITDUMP("  Found type SIMD Vector<ulong>\n");
                }
                else
                {
                    JITDUMP("  Unknown SIMD Vector<T>\n");
                }
            }
            else if (wcsncmp(&(className[16]), W("Vector2"), 8) == 0) 
            {
                SIMDVector2Handle = typeHnd;

                simdBaseType = TYP_FLOAT;
                size = 2*genTypeSize(TYP_FLOAT);
                assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                JITDUMP(" Found Vector2\n");
            }
            else if (wcsncmp(&(className[16]), W("Vector3"), 8) == 0)
            {
                SIMDVector3Handle = typeHnd;

                simdBaseType = TYP_FLOAT;
                size = 3*genTypeSize(TYP_FLOAT);
                assert(size == info.compCompHnd->getClassSize(typeHnd));
                JITDUMP(" Found Vector3\n");
            }
            else if (wcsncmp(&(className[16]), W("Vector4"), 8) == 0)
            {
                SIMDVector4Handle = typeHnd;

                simdBaseType = TYP_FLOAT;
                size = 4*genTypeSize(TYP_FLOAT);
                assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                JITDUMP(" Found Vector4\n");
            }
            else if (wcsncmp(&(className[16]), W("Vector"), 6) == 0)
            {
                SIMDVectorHandle = typeHnd;
                JITDUMP(" Found type Vector\n");
            }
            else
            {
                JITDUMP("  Unknown SIMD Type\n");
            }
        }
    }

    if (simdBaseType != TYP_UNKNOWN &&
        sizeBytes != nullptr)
    {
        // If not a fixed size vector then its size is same as SIMD vector
        // register length in bytes
        if (size == 0)
        {
            size = getSIMDVectorRegisterByteLength();
        }

        *sizeBytes = size;
    }

    return simdBaseType;
}

//--------------------------------------------------------------------------------------
// getSIMDIntrinsicInfo: get SIMD intrinsic info given the method handle.
//
// Arguments:
//    inOutTypeHnd    - The handle of the type on which the method is invoked.  This is an in-out param.
//    methodHnd       - The handle of the method we're interested in.
//    sig             - method signature info
//    isNewObj        - whether this call represents a newboj constructor call
//    argCount        - argument count - out pram
//    baseType        - base type of the intrinsic - out param
//    sizeBytes       - size of SIMD vector type on which the method is invoked - out param
//
// Return Value:
//    SIMDIntrinsicInfo struct initialized corresponding to methodHnd.
//    Sets SIMDIntrinsicInfo.id to SIMDIntrinsicInvalid if methodHnd doesn't correspond 
//    to any SIMD intrinsic.  Also, sets the out params inOutTypeHnd, argCount, baseType and
//    sizeBytes.
//
//    Note that VectorMath class doesn't have a base type and first argument of the method
//    determines the SIMD vector type on which intrinsic is invoked. In such a case inOutTypeHnd
//    is modified by this routine.
//
// TODO-Throughput: The current implementation is based on method name string parsing.
//         Although we now have type identification from the VM, the parsing of intrinsic names
//         could be made more efficient.
//
const SIMDIntrinsicInfo* Compiler::getSIMDIntrinsicInfo(CORINFO_CLASS_HANDLE*  inOutTypeHnd,
                                                        CORINFO_METHOD_HANDLE methodHnd,
                                                        CORINFO_SIG_INFO *    sig,
                                                        bool                  isNewObj,
                                                        unsigned*             argCount,
                                                        var_types*            baseType,
                                                        unsigned*             sizeBytes)
{
    assert(featureSIMD);
    assert(baseType != nullptr);
    assert(sizeBytes != nullptr);
    
    // get baseType and size of the type
    CORINFO_CLASS_HANDLE typeHnd = *inOutTypeHnd;
    *baseType = getBaseTypeAndSizeOfSIMDType(typeHnd, sizeBytes);

    bool isHWAcceleratedIntrinsic = false;
    if (typeHnd == SIMDVectorHandle)
    {
        // All of the supported intrinsics on this static class take a first argument that's a vector,
        // which determines the baseType.
        // The exception is the IsHardwareAccelerated property, which is handled as a special case.
        assert(*baseType == TYP_UNKNOWN);
        if(sig->numArgs == 0)
        {
            const SIMDIntrinsicInfo* hwAccelIntrinsicInfo = &(simdIntrinsicInfoArray[SIMDIntrinsicHWAccel]);
            if ((strcmp(eeGetMethodName(methodHnd, nullptr), hwAccelIntrinsicInfo->methodName) == 0) &&
                JITtype2varType(sig->retType) == hwAccelIntrinsicInfo->retType)
            {
                // Sanity check
                assert(hwAccelIntrinsicInfo->argCount == 0 && hwAccelIntrinsicInfo->isInstMethod == false);
                return hwAccelIntrinsicInfo;
            }
            return nullptr;
        }
        else
        {
            typeHnd = info.compCompHnd->getArgClass(sig, sig->args);
            *inOutTypeHnd = typeHnd;
            *baseType = getBaseTypeAndSizeOfSIMDType(typeHnd, sizeBytes);
        }
    }

    if (*baseType == TYP_UNKNOWN)
    {
        JITDUMP("NOT a SIMD Intrinsic: unsupported baseType\n");
        return nullptr;
    }

    // account for implicit "this" arg
    *argCount = sig->numArgs;
    if (sig->hasThis())
    {
        *argCount += 1;
    }

    // Get the Intrinsic Id by parsing method name.
    //
    // TODO-Throughput: replace sequential search by binary search by arranging entries
    // sorted by method name.
    SIMDIntrinsicID intrinsicId = SIMDIntrinsicInvalid;
    const char*  methodName = eeGetMethodName(methodHnd, nullptr);
    for (int i = SIMDIntrinsicNone+1; i < SIMDIntrinsicInvalid; ++i)
    {
        if (strcmp(methodName, simdIntrinsicInfoArray[i].methodName) == 0)
        {
            // Found an entry for the method; further check whether it is one of
            // the supported base types.
            bool found = false;
            for (int j=0; j < SIMD_INTRINSIC_MAX_BASETYPE_COUNT; ++j)
            {
                // Convention: if there are fewer base types supported than MAX_BASETYPE_COUNT,
                // the end of the list is marked by TYP_UNDEF.                
                if (simdIntrinsicInfoArray[i].supportedBaseTypes[j] == TYP_UNDEF)
                    break;

                if (simdIntrinsicInfoArray[i].supportedBaseTypes[j] == *baseType)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                continue;
            }

            // Now, check the arguments.
            unsigned int fixedArgCnt = simdIntrinsicInfoArray[i].argCount;
            unsigned int expectedArgCnt = fixedArgCnt;

            // First handle SIMDIntrinsicInitN, where the arg count depends on the type.
            // The listed arg types include the vector and the first two init values, which is the expected number
            // for Vector2.  For other cases, we'll check their types here.
            if (*argCount > expectedArgCnt)
            {
                if (i == SIMDIntrinsicInitN)
                {
                    if (*argCount == 3 && typeHnd == SIMDVector2Handle)
                    {
                        expectedArgCnt = 3;
                    }
                    else if (*argCount == 4 && typeHnd == SIMDVector3Handle)
                    {
                        expectedArgCnt = 4;
                    }
                    else if (*argCount == 5 && typeHnd == SIMDVector4Handle)
                    {
                        expectedArgCnt = 5;
                    }
                }
                else if (i == SIMDIntrinsicInitFixed)
                {
                    if (*argCount == 4 && typeHnd == SIMDVector4Handle)
                    {
                        expectedArgCnt = 4;
                    }
                }
            }
            if (*argCount != expectedArgCnt)
            {
                continue;
            }

            // Validate the types of individual args passed are what is expected of.
            // If any of the types don't match with what is expected, don't consider
            // as an intrinsic.  This will make an older JIT with SIMD capabilities
            // resilient to breaking changes to SIMD managed API.
            //
            // Note that from IL type stack, args get popped in right to left order
            // whereas args get listed in method signatures in left to right order.

            int stackIndex = (expectedArgCnt - 1);

            // Track the arguments from the signature - we currently only use this to distinguish
            // integral and pointer types, both of which will by TYP_I_IMPL on the importer stack.
            CORINFO_ARG_LIST_HANDLE     argLst  = sig->args;

            CORINFO_CLASS_HANDLE        argClass;
            for (unsigned int argIndex = 0; found == true && argIndex < expectedArgCnt; argIndex++)
            {
                bool isThisPtr = ((argIndex == 0) && sig->hasThis());

                // In case of "newobj SIMDVector<T>(T val)", thisPtr won't be present on type stack.
                // We don't check anything in that case.
                if (!isThisPtr || !isNewObj)
                {
                    GenTreePtr arg = impStackTop(stackIndex).val;

                    var_types expectedArgType;
                    if (argIndex < fixedArgCnt)
                    {
                        // Convention:
                        //   - intrinsicInfo.argType[i] == TYP_UNDEF - intrinsic doesn't have a valid arg at position i
                        //   - intrinsicInfo.argType[i] == TYP_UNKNOWN - arg type should be same as basetype
                        // Note that we pop the args off in reverse order.
                        expectedArgType = simdIntrinsicInfoArray[i].argType[argIndex];
                        assert(expectedArgType != TYP_UNDEF);
                        if (expectedArgType == TYP_UNKNOWN)
                        {
                            // JIT maps uint/ulong type vars to TYP_INT/TYP_LONG.                            
                            expectedArgType = (*baseType == TYP_UINT || *baseType == TYP_ULONG) ? genActualType(*baseType) : *baseType;
                        }
                    }
                    else
                    {
                        expectedArgType = *baseType;
                    }

                    var_types argType = arg->TypeGet();
                    if (!isThisPtr && argType == TYP_I_IMPL)
                    {
                        // The reference implementation has a constructor that takes a pointer.
                        // We don't want to recognize that one.  This requires us to look at the CorInfoType
                        // in order to distinguish a signature with a pointer argument from one with an
                        // integer argument of pointer size, both of which will be TYP_I_IMPL on the stack.
                        // TODO-Review: This seems quite fragile.  We should consider beefing up the checking
                        // here.
                        CorInfoType corType = strip(info.compCompHnd->getArgType(sig, argLst, &argClass));
                        if (corType == CORINFO_TYPE_PTR)
                        {
                            found = false;
                        }
                    }

                    if (varTypeIsSIMD(argType))
                    {
                        argType = TYP_STRUCT;
                    }
                    if (argType != expectedArgType)
                    {
                        found = false;
                    }
                }
                if (argIndex != 0 || !sig->hasThis())
                {
                    argLst = info.compCompHnd->getArgNext(argLst);
                }
                stackIndex--;
            }

            // Cross check return type and static vs. instance is what we are expecting.
            // If not, don't consider it as an intrinsic.    
            // Note that ret type of TYP_UNKNOWN means that it is not known apriori and must be same as baseType
            if (found)
            {
                var_types expectedRetType = simdIntrinsicInfoArray[i].retType;
                if (expectedRetType == TYP_UNKNOWN)
                {
                    // JIT maps uint/ulong type vars to TYP_INT/TYP_LONG.                            
                    expectedRetType = (*baseType == TYP_UINT || *baseType == TYP_ULONG) ? genActualType(*baseType) : *baseType;
                }

                if (JITtype2varType(sig->retType) != expectedRetType ||
                    sig->hasThis() != simdIntrinsicInfoArray[i].isInstMethod)
                {
                    found = false;
                }
            }

            if (found)
            {
                intrinsicId = (SIMDIntrinsicID) i;
                break;
            }
        }
    }

    if (intrinsicId != SIMDIntrinsicInvalid)
    {
        JITDUMP("Method %s maps to SIMD intrinsic %s\n", methodName, simdIntrinsicNames[intrinsicId]);
        return &simdIntrinsicInfoArray[intrinsicId];
    }
    else
    {
        JITDUMP("Method %s is NOT a SIMD intrinsic\n", methodName);
    }

    return nullptr;
}

// Pops and returns GenTree node from importer's type stack.
// Normalizes TYP_STRUCT value in case of GT_CALL, GT_RET_EXPR and arg nodes.
//
// Arguments:
//    type        -  the type of value that the caller expects to be popped off the stack.
//    expectAddr  -  if true indicates we are expecting type stack entry to be a TYP_BYREF.
//
// Notes:
//    If the popped value is a struct, and the expected type is a simd type, it will be set
//    to that type, otherwise it will assert if the type being popped is not the expected type.

GenTreePtr Compiler::impSIMDPopStack(var_types type, bool expectAddr)
{
    StackEntry      se   = impPopStack();
    typeInfo        ti   = se.seTypeInfo;
    GenTreePtr      tree = se.val;

    // If expectAddr is true implies what we have on stack is address and we need
    // SIMD type struct that it points to.
    if (expectAddr)
    {
        assert(tree->TypeGet() == TYP_BYREF);
        if (tree->OperGet() == GT_ADDR)
        {
            tree = tree->gtGetOp1();
        }
        else
        {
            tree = gtNewOperNode(GT_IND, type, tree);
        }
    }
    
    bool isParam = false;

    // If we have a ldobj of a SIMD local we need to transform it.
    if (tree->OperGet() == GT_LDOBJ)
    {
        GenTree* addr = tree->gtOp.gtOp1;
        if ((addr->OperGet() == GT_ADDR) && isSIMDTypeLocal(addr->gtOp.gtOp1))
        {
            tree = addr->gtOp.gtOp1;
        }
    }

    if (tree->OperGet() == GT_LCL_VAR)
    {
        unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
        LclVarDsc* lclVarDsc  = &lvaTable[lclNum];
        isParam = lclVarDsc->lvIsParam;
    }

    // normalize TYP_STRUCT value
    if (varTypeIsStruct(tree) && ((tree->OperGet() == GT_RET_EXPR) || (tree->OperGet() == GT_CALL) || isParam))
    {
        assert(ti.IsType(TI_STRUCT));
        CORINFO_CLASS_HANDLE structType = ti.GetClassHandleForValueClass();
        tree = impNormStructVal(tree, structType, (unsigned)CHECK_SPILL_ALL);
    }

    // Now set the type of the tree to the specialized SIMD struct type, if applicable.
    if (genActualType(tree->gtType) != genActualType(type))
    {
        assert(tree->gtType == TYP_STRUCT);
        tree->gtType = type;
    }
    else if (tree->gtType == TYP_BYREF)
    {
        assert(tree->IsLocal() ||
               (tree->gtOper == GT_ADDR) && varTypeIsSIMD(tree->gtGetOp1()));
    }

    return tree;
}

// impSIMDGetFixed: Create a GT_SIMD tree for a Get property of SIMD vector with a fixed index.
//
// Arguments:
//    baseType - The base (element) type of the SIMD vector.
//    simdSize - The total size in bytes of the SIMD vector.
//    index    - The index of the field to get.
//
// Return Value:
//    Returns a GT_SIMD node with the SIMDIntrinsicGetItem intrinsic id.
//
GenTreeSIMD* Compiler::impSIMDGetFixed(var_types       simdType,
                                       var_types       baseType,
                                       unsigned        simdSize,
                                       int             index)
{
    assert(simdSize >= ((index + 1) * genTypeSize(baseType)));

    // op1 is a SIMD source.
    GenTree* op1 = impSIMDPopStack(simdType, true);

    GenTree* op2 = gtNewIconNode(index);    
    GenTreeSIMD* simdTree = gtNewSIMDNode(baseType, op1, op2, SIMDIntrinsicGetItem, baseType, simdSize);
    return simdTree;
}

#ifdef _TARGET_AMD64_
// impSIMDLongRelOpEqual: transforms operands and returns the SIMD intrinsic to be applied on 
// transformed operands to obtain == comparison result.
//
// Argumens:
//    typeHnd  -  type handle of SIMD vector
//    size     -  SIMD vector size
//    op1      -  in-out parameter; first operand
//    op2      -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params op1, op2 and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDLongRelOpEqual(CORINFO_CLASS_HANDLE typeHnd,
                                                unsigned size,
                                                GenTree** pOp1,
                                                GenTree** pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    // There is no direct SSE2 support for comparing TYP_LONG vectors.
    // These have to be implemented in terms of TYP_INT vector comparison operations.
    // 
    // Equality(v1, v2):
    // tmp = (v1 == v2) i.e. compare for equality as if v1 and v2 are vector<int>
    // result = BitwiseAnd(t, shuffle(t, (2, 3, 1 0)))
    // Shuffle is meant to swap the comparison results of low-32-bits and high 32-bits of respective long elements.

    // Compare vector<long> as if they were vector<int> and assign the result to a temp
    GenTree* compResult = gtNewSIMDNode(simdType, *pOp1, *pOp2, SIMDIntrinsicEqual, TYP_INT, size);
    unsigned lclNum = lvaGrabTemp(true DEBUGARG("SIMD Long =="));
    lvaSetStruct(lclNum, typeHnd, false);
    GenTree* tmp = gtNewLclvNode(lclNum, simdType);
    GenTree* asg = gtNewTempAssign(lclNum, compResult);

    // op1 = GT_COMMA(tmp=compResult, tmp)
    // op2 = Shuffle(tmp, 0xB1)
    // IntrinsicId = BitwiseAnd
    *pOp1 = gtNewOperNode(GT_COMMA, simdType, asg, tmp);
    *pOp2 = gtNewSIMDNode(simdType, gtNewLclvNode(lclNum, simdType), gtNewIconNode(SHUFFLE_ZWYX, TYP_INT), SIMDIntrinsicShuffleSSE2, TYP_INT, size);
    return SIMDIntrinsicBitwiseAnd;
}

// impSIMDLongRelOpGreaterThan: transforms operands and returns the SIMD intrinsic to be applied on 
// transformed operands to obtain > comparison result.
//
// Argumens:
//    typeHnd  -  type handle of SIMD vector
//    size     -  SIMD vector size
//    pOp1     -  in-out parameter; first operand
//    pOp2     -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params pOp1, pOp2 and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDLongRelOpGreaterThan(CORINFO_CLASS_HANDLE typeHnd,
                                                      unsigned size,
                                                      GenTree** pOp1,
                                                      GenTree** pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    // GreaterThan(v1, v2) where v1 and v2 are vector long.
    // Let us consider the case of single long element comparison.
    // say L1 = (x1, y1) and L2 = (x2, y2) where x1, y1, x2, and y2 are 32-bit integers that comprise the longs L1 and L2.    
    //
    // GreaterThan(L1, L2) can be expressed in terms of > relationship between 32-bit integers that comprise L1 and L2 as 
    //                    =  (x1, y1) > (x2, y2)
    //                    =  (x1 > x2) || [(x1 == x2) && (y1 > y2)]   - eq (1)
    //
    // t = (v1 > v2)  32-bit signed comparison
    // u = (v1 == v2) 32-bit sized element equality
    // v = (v1 > v2)  32-bit unsigned comparison
    //
    // z = shuffle(t, (3, 3, 1, 1))  - This corresponds to (x1 > x2) in eq(1) above
    // t1 = Shuffle(v, (2, 2, 0, 0)) - This corresponds to (y1 > y2) in eq(1) above
    // u1 = Shuffle(u, (3, 3, 1, 1)) - This corresponds to (x1 == x2) in eq(1) above
    // w = And(t1, u1)               - This corresponds to [(x1 == x2) && (y1 > y2)] in eq(1) above
    // Result = BitwiseOr(z, w)

    // Since op1 and op2 gets used multiple times, make sure side effects are computed.
    GenTree* dupOp1 = nullptr;
    GenTree* dupOp2 = nullptr;
    GenTree* dupDupOp1 = nullptr;
    GenTree* dupDupOp2 = nullptr;

    if (((*pOp1)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp1 = fgInsertCommaFormTemp(pOp1, typeHnd);        
        dupDupOp1 = gtNewLclvNode(dupOp1->AsLclVarCommon()->GetLclNum(), simdType);
    }
    else
    {
        dupOp1 = gtCloneExpr(*pOp1);
        dupDupOp1 = gtCloneExpr(*pOp1);
    }

    if (((*pOp2)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp2 = fgInsertCommaFormTemp(pOp2, typeHnd);
        dupDupOp2 = gtNewLclvNode(dupOp2->AsLclVarCommon()->GetLclNum(), simdType);
    }
    else
    {
        dupOp2 = gtCloneExpr(*pOp2);
        dupDupOp2 = gtCloneExpr(*pOp2);
    }

    assert(dupDupOp1 != nullptr && dupDupOp2 != nullptr);
    assert(dupOp1 != nullptr && dupOp2 != nullptr);
    assert(*pOp1 != nullptr && *pOp2 != nullptr);

    // v1GreaterThanv2Signed - signed 32-bit comparison
    GenTree* v1GreaterThanv2Signed = gtNewSIMDNode(simdType, *pOp1, *pOp2, SIMDIntrinsicGreaterThan, TYP_INT, size);

    // v1Equalsv2 - 32-bit equality
    GenTree* v1Equalsv2 = gtNewSIMDNode(simdType, dupOp1, dupOp2, SIMDIntrinsicEqual, TYP_INT, size);

    // v1GreaterThanv2Unsigned - unsigned 32-bit comparison
    var_types tempBaseType = TYP_UINT;
    SIMDIntrinsicID sid = impSIMDRelOp(SIMDIntrinsicGreaterThan, typeHnd, size, &tempBaseType, &dupDupOp1, &dupDupOp2);
    GenTree* v1GreaterThanv2Unsigned = gtNewSIMDNode(simdType, dupDupOp1, dupDupOp2, sid, tempBaseType, size);

    GenTree* z = gtNewSIMDNode(simdType, v1GreaterThanv2Signed, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), SIMDIntrinsicShuffleSSE2, TYP_FLOAT, size);
    GenTree* t1 = gtNewSIMDNode(simdType, v1GreaterThanv2Unsigned, gtNewIconNode(SHUFFLE_ZZXX, TYP_INT), SIMDIntrinsicShuffleSSE2, TYP_FLOAT, size);
    GenTree* u1 = gtNewSIMDNode(simdType, v1Equalsv2, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), SIMDIntrinsicShuffleSSE2, TYP_FLOAT, size);
    GenTree* w = gtNewSIMDNode(simdType, u1, t1, SIMDIntrinsicBitwiseAnd, TYP_INT, size);

    *pOp1 = z;
    *pOp2 = w;
    return SIMDIntrinsicBitwiseOr;
}

// impSIMDLongRelOpGreaterThanOrEqual: transforms operands and returns the SIMD intrinsic to be applied on 
// transformed operands to obtain >= comparison result.
//
// Argumens:
//    typeHnd  -  type handle of SIMD vector
//    size     -  SIMD vector size
//    pOp1      -  in-out parameter; first operand
//    pOp2      -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params pOp1, pOp2 and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDLongRelOpGreaterThanOrEqual(CORINFO_CLASS_HANDLE typeHnd,
                                                             unsigned size,
                                                             GenTree** pOp1,
                                                             GenTree** pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    // expand this to (a == b) | (a > b)
    GenTree* dupOp1 = nullptr;
    GenTree* dupOp2 = nullptr;

    if (((*pOp1)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp1 = fgInsertCommaFormTemp(pOp1, typeHnd);
    }
    else
    {
        dupOp1 = gtCloneExpr(*pOp1);
    }

    if (((*pOp2)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp2 = fgInsertCommaFormTemp(pOp2, typeHnd);
    }
    else
    {
        dupOp2 = gtCloneExpr(*pOp2);
    }

    assert(dupOp1 != nullptr && dupOp2 != nullptr);
    assert(*pOp1 != nullptr && *pOp2 != nullptr);

    // (a==b)
    SIMDIntrinsicID id = impSIMDLongRelOpEqual(typeHnd, size, pOp1, pOp2);
    *pOp1 = gtNewSIMDNode(simdType, *pOp1, *pOp2, id, TYP_LONG, size);

    // (a > b)
    id = impSIMDLongRelOpGreaterThan(typeHnd, size, &dupOp1, &dupOp2);
    *pOp2 = gtNewSIMDNode(simdType, dupOp1, dupOp2, id, TYP_LONG, size);

    return SIMDIntrinsicBitwiseOr;
}

// impSIMDInt32OrSmallIntRelOpGreaterThanOrEqual: transforms operands and returns the SIMD intrinsic to be applied on 
// transformed operands to obtain >= comparison result in case of integer base type vectors
//
// Argumens:
//    typeHnd  -  type handle of SIMD vector
//    size     -  SIMD vector size
//    baseType -  base type of SIMD vector
//    pOp1      -  in-out parameter; first operand
//    pOp2      -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params pOp1, pOp2 and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDIntegralRelOpGreaterThanOrEqual(CORINFO_CLASS_HANDLE typeHnd,
                                                                        unsigned size,
                                                                        var_types baseType,
                                                                        GenTree** pOp1,
                                                                        GenTree** pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    // This routine should be used only for integer base type vectors
    assert(varTypeIsIntegral(baseType));
    if ((getSIMDInstructionSet() == InstructionSet_SSE2) &&
        ((baseType == TYP_LONG) || baseType == TYP_UBYTE))
    {
        return impSIMDLongRelOpGreaterThanOrEqual(typeHnd, size, pOp1, pOp2);
    }

    // expand this to (a == b) | (a > b)
    GenTree* dupOp1 = nullptr;
    GenTree* dupOp2 = nullptr;

    if (((*pOp1)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp1 = fgInsertCommaFormTemp(pOp1, typeHnd);
    }
    else
    {
        dupOp1 = gtCloneExpr(*pOp1);
    }

    if (((*pOp2)->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        dupOp2 = fgInsertCommaFormTemp(pOp2, typeHnd);
    }
    else
    {
        dupOp2 = gtCloneExpr(*pOp2);
    }

    assert(dupOp1 != nullptr && dupOp2 != nullptr);
    assert(*pOp1 != nullptr && *pOp2 != nullptr);

    // (a==b)
    *pOp1 = gtNewSIMDNode(simdType, *pOp1, *pOp2, SIMDIntrinsicEqual, baseType, size);

    // (a > b)
    *pOp2 = gtNewSIMDNode(simdType, dupOp1, dupOp2, SIMDIntrinsicGreaterThan, baseType, size);

    return SIMDIntrinsicBitwiseOr;
}
#endif //_TARGET_AMD64_

// Transforms operands and returns the SIMD intrinsic to be applied on 
// transformed operands to obtain given relop result.
//
// Argumens:
//    relOpIntrinsicId - Relational operator SIMD intrinsic
//    typeHnd          - type handle of SIMD vector
//    size             -  SIMD vector size
//    inOutBaseType    - base type of SIMD vector
//    pOp1             -  in-out parameter; first operand
//    pOp2             -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params pOp1, pOp2, inOutBaseType and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDRelOp(SIMDIntrinsicID relOpIntrinsicId,
                                       CORINFO_CLASS_HANDLE typeHnd,
                                       unsigned size,
                                       var_types* inOutBaseType,
                                       GenTree** pOp1,
                                       GenTree** pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    assert(isRelOpSIMDIntrinsic(relOpIntrinsicId));

#ifdef _TARGET_AMD64_
    SIMDIntrinsicID intrinsicID = relOpIntrinsicId;
    var_types baseType = *inOutBaseType;

    if (varTypeIsFloating(baseType))
    {
        // SSE2/AVX doesn't support > and >= on vector float/double.  
        // Therefore, we need to use < and <= with swapped operands
        if (relOpIntrinsicId == SIMDIntrinsicGreaterThan ||
            relOpIntrinsicId == SIMDIntrinsicGreaterThanOrEqual)
        {
            GenTree* tmp = *pOp1;
            *pOp1 = *pOp2;
            *pOp2 =  tmp;

            intrinsicID = (relOpIntrinsicId == SIMDIntrinsicGreaterThan) ? SIMDIntrinsicLessThan : 
                                                                           SIMDIntrinsicLessThanOrEqual;
        }
    }
    else if (varTypeIsIntegral(baseType))
    {
        // SSE/AVX doesn't support < and <= on integer base type vectors.
        // Therefore, we need to use > and >= with swapped operands. 
        if (intrinsicID == SIMDIntrinsicLessThan ||
            intrinsicID == SIMDIntrinsicLessThanOrEqual)
        {
            GenTree* tmp = *pOp1;
            *pOp1 = *pOp2;
            *pOp2 =  tmp;

            intrinsicID = (relOpIntrinsicId == SIMDIntrinsicLessThan) ? SIMDIntrinsicGreaterThan : 
                                                                        SIMDIntrinsicGreaterThanOrEqual;
        }

        if ((getSIMDInstructionSet() == InstructionSet_SSE2) && baseType == TYP_LONG)
        {
            // There is no direct SSE2 support for comparing TYP_LONG vectors.
            // These have to be implemented interms of TYP_INT vector comparison operations.
            if (intrinsicID == SIMDIntrinsicEqual)
            { 
                intrinsicID = impSIMDLongRelOpEqual(typeHnd, size, pOp1, pOp2);
            }
            else if (intrinsicID == SIMDIntrinsicGreaterThan)
            {
                intrinsicID = impSIMDLongRelOpGreaterThan(typeHnd, size, pOp1, pOp2);
            }
            else if (intrinsicID == SIMDIntrinsicGreaterThanOrEqual)
            {
                intrinsicID = impSIMDLongRelOpGreaterThanOrEqual(typeHnd, size, pOp1, pOp2);
            }
            else
            {
                unreached();
            }
        }
        // SSE2 and AVX direct support for signed comparison of int32, int16 and int8 types
        else if (!varTypeIsUnsigned(baseType))
        {
            if (intrinsicID == SIMDIntrinsicGreaterThanOrEqual)
            {
                intrinsicID = impSIMDIntegralRelOpGreaterThanOrEqual(typeHnd, size, baseType, pOp1, pOp2);
            }
        }
        else // unsigned
        {
            // Vector<byte>, Vector<ushort>, Vector<uint> and Vector<ulong>:
            // SSE2 supports > for signed comparison. Therefore, to use it for
            // comparing unsigned numbers, we subtract a constant from both the 
            // operands such that the result fits within the corresponding signed
            // type.  The resulting signed numbers are compared using SSE2 signed
            // comparison.
            //
            // Vector<byte>: constant to be subtracted is 2^7
            // Vector<ushort> constant to be subtracted is 2^15
            // Vector<uint> constant to be subtracted is 2^31
            // Vector<ulong> constant to be subtracted is 2^63
            //
            // We need to treat op1 and op2 as signed for comparison purpose after
            // the transformation.
            ssize_t constVal = 0;
            switch(baseType)
            {
            case TYP_UBYTE:
                constVal = 0x80808080;
                *inOutBaseType = TYP_BYTE;
                break;
            case TYP_CHAR:
                constVal = 0x80008000;
                *inOutBaseType = TYP_SHORT;
                break;
            case TYP_UINT:
                constVal = 0x80000000;
                *inOutBaseType = TYP_INT;
                break;
            case TYP_ULONG:
                constVal = 0x8000000000000000LL;
                *inOutBaseType = TYP_LONG;
                break;
            default:
                unreached();
                break;
            }
            assert(constVal != 0);

            // This transformation is not required for equality.
            if (intrinsicID != SIMDIntrinsicEqual)
            {
                // For constructing const vector use either long or int base type.                
                var_types tempBaseType = (baseType == TYP_ULONG) ? TYP_LONG : TYP_INT;
                GenTree* initVal = gtNewIconNode(constVal);
                initVal->gtType = tempBaseType;
                GenTree* constVector = gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, tempBaseType, size);

                // Assign constVector to a temp, since we intend to use it more than once
                // TODO-CQ: We have quite a few such constant vectors constructed during
                // the importation of SIMD intrinsics.  Make sure that we have a single
                // temp per distinct constant per method.
                GenTree* tmp = fgInsertCommaFormTemp(&constVector, typeHnd);

                // op1 = op1 - constVector
                // op2 = op2 - constVector
                *pOp1 = gtNewSIMDNode(simdType, *pOp1, constVector, SIMDIntrinsicSub, baseType, size);
                *pOp2 = gtNewSIMDNode(simdType, *pOp2, tmp, SIMDIntrinsicSub, baseType, size);
            }

            return impSIMDRelOp(intrinsicID, typeHnd, size, inOutBaseType, pOp1, pOp2);
        }
    }

    return intrinsicID;
#else
    assert(!"impSIMDRelOp() unimplemented on target arch");
    unreached();
#endif //_TARGET_AMD64_
}

// Creates a GT_SIMD tree for Select operation
//
// Argumens:
//    typeHnd          -  type handle of SIMD vector
//    baseType         -  base type of SIMD vector
//    size             -  SIMD vector size
//    op1              -  first operand = Condition vector vc
//    op2              -  second operand = va
//    op3              -  third operand = vb
//
// Return Value:
//    Returns GT_SIMD tree that computes Select(vc, va, vb)
//
GenTreePtr  Compiler::impSIMDSelect(CORINFO_CLASS_HANDLE typeHnd,
                                    var_types baseType,
                                    unsigned size,
                                    GenTree* op1,
                                    GenTree* op2,
                                    GenTree* op3)
{
    assert(varTypeIsSIMD(op1));
    var_types simdType = op1->TypeGet();
    assert(op2->TypeGet() == simdType);
    assert(op3->TypeGet() == simdType);


    // Select(BitVector vc, va, vb) = (va & vc) | (vb & !vc)
    // Select(op1, op2, op3)        = (op2 & op1) | (op3 & !op1)
    //                              = SIMDIntrinsicBitwiseOr(SIMDIntrinsicBitwiseAnd(op2, op1), SIMDIntrinsicBitwiseAndNot(op3, op1))
    //
    // If Op1 has side effect, create an assignment to a temp
    GenTree* tmp = op1;
    GenTree* asg = nullptr;
    if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        unsigned lclNum = lvaGrabTemp(true DEBUGARG("SIMD Select"));
        lvaSetStruct(lclNum, typeHnd, false);
        tmp = gtNewLclvNode(lclNum, op1->TypeGet());
        asg = gtNewTempAssign(lclNum, op1);
    }

    GenTree* andExpr = gtNewSIMDNode(simdType, op2, tmp, SIMDIntrinsicBitwiseAnd, baseType, size);
    GenTree* dupOp1 = gtCloneExpr(tmp);
    assert(dupOp1 != nullptr);
    GenTree* andNotExpr = gtNewSIMDNode(simdType, dupOp1, op3, SIMDIntrinsicBitwiseAndNot, baseType, size);
    GenTree* simdTree = gtNewSIMDNode(simdType, andExpr, andNotExpr, SIMDIntrinsicBitwiseOr, baseType, size);

    // If asg not null, create a GT_COMMA tree.
    if (asg != nullptr)
    {
        simdTree  = gtNewOperNode(GT_COMMA, simdTree->TypeGet(), asg, simdTree);
    }

    return simdTree;
}

// Creates a GT_SIMD tree for Min/Max operation
//
// Argumens:
//    IntrinsicId      -  SIMD intrinsic Id, either Min or Max
//    typeHnd          -  type handle of SIMD vector
//    baseType         -  base type of SIMD vector
//    size             -  SIMD vector size
//    op1              -  first operand = va
//    op2              -  second operand = vb
//
// Return Value:
//    Returns GT_SIMD tree that computes Max(va, vb)
//
GenTreePtr  Compiler::impSIMDMinMax(SIMDIntrinsicID intrinsicId,
                                    CORINFO_CLASS_HANDLE typeHnd,
                                    var_types baseType,
                                    unsigned size,
                                    GenTree* op1,
                                    GenTree* op2)
{
    assert(intrinsicId == SIMDIntrinsicMin || intrinsicId == SIMDIntrinsicMax);
    assert(varTypeIsSIMD(op1));
    var_types simdType = op1->TypeGet();
    assert(op2->TypeGet() == simdType);
    
#ifdef _TARGET_AMD64_
    // SSE2 has direct support for float/double/signed word/unsigned byte.
    // For other integer types we compute min/max as follows
    //
    // int32/uint32/int64/uint64:
    //       compResult        = (op1 < op2) in case of Min 
    //                           (op1 > op2) in case of Max
    //       Min/Max(op1, op2) = Select(compResult, op1, op2)
    //
    // unsigned word:
    //        op1 = op1 - 2^15  ; to make it fit within a signed word
    //        op2 = op2 - 2^15  ; to make it fit within a signed word
    //        result = SSE2 signed word Min/Max(op1, op2)
    //        result = result + 2^15  ; readjust it back
    //
    // signed byte:
    //        op1 = op1 + 2^7  ; to make it unsigned 
    //        op1 = op1 + 2^7  ; to make it unsigned
    //        result = SSE2 unsigned byte Min/Max(op1, op2)
    //        result = result - 2^15 ; readjust it back
             
    GenTree* simdTree = nullptr;

    if (varTypeIsFloating(baseType) || baseType == TYP_SHORT || baseType == TYP_UBYTE)
    {
        // SSE2 has direct support
        simdTree = gtNewSIMDNode(simdType, op1, op2, intrinsicId, baseType, size);
    }
    else if (baseType == TYP_CHAR || baseType == TYP_BYTE)
    {
        int constVal;
        SIMDIntrinsicID operIntrinsic;
        SIMDIntrinsicID adjustIntrinsic;
        var_types minMaxOperBaseType;
        if (baseType == TYP_CHAR)
        {
            constVal = 0x80008000;
            operIntrinsic = SIMDIntrinsicSub;
            adjustIntrinsic = SIMDIntrinsicAdd;
            minMaxOperBaseType = TYP_SHORT;
        }
        else
        {
            assert(baseType == TYP_BYTE);
            constVal = 0x80808080;
            operIntrinsic = SIMDIntrinsicAdd;
            adjustIntrinsic = SIMDIntrinsicSub;
            minMaxOperBaseType = TYP_UBYTE;
        }

        GenTree* initVal = gtNewIconNode(constVal);
        GenTree* constVector = gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, TYP_INT, size);

        // Assign constVector to a temp, since we intend to use it more than once
        // TODO-CQ: We have quite a few such constant vectors constructed during
        // the importation of SIMD intrinsics.  Make sure that we have a single
        // temp per distinct constant per method.
        GenTree* tmp = fgInsertCommaFormTemp(&constVector, typeHnd);

        // op1 = op1 - constVector
        // op2 = op2 - constVector
        op1 = gtNewSIMDNode(simdType, op1, constVector, operIntrinsic, baseType, size);
        op2 = gtNewSIMDNode(simdType, op2, tmp, operIntrinsic, baseType, size);

        // compute min/max of op1 and op2 considering them as if minMaxOperBaseType
        simdTree = gtNewSIMDNode(simdType, op1, op2, intrinsicId, minMaxOperBaseType, size);

        // re-adjust the value by adding or subtracting constVector
        tmp = gtNewLclvNode(tmp->AsLclVarCommon()->GetLclNum(), tmp->TypeGet());
        simdTree = gtNewSIMDNode(simdType, simdTree, tmp, adjustIntrinsic, baseType, size);
    }
    else
    {
        GenTree* dupOp1 = nullptr;
        GenTree* dupOp2 = nullptr;
        GenTree* op1Assign = nullptr;
        GenTree* op2Assign = nullptr;
        unsigned op1LclNum;
        unsigned op2LclNum;

        if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            op1LclNum = lvaGrabTemp(true DEBUGARG("SIMD Min/Max"));
            dupOp1 = gtNewLclvNode(op1LclNum, op1->TypeGet());
            lvaSetStruct(op1LclNum, typeHnd, false);
            op1Assign = gtNewTempAssign(op1LclNum, op1);
            op1 = gtNewLclvNode(op1LclNum, op1->TypeGet());
        }
        else
        {
            dupOp1 = gtCloneExpr(op1);
        }

        if ((op2->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            op2LclNum = lvaGrabTemp(true DEBUGARG("SIMD Min/Max"));
            dupOp2 = gtNewLclvNode(op2LclNum, op2->TypeGet());
            lvaSetStruct(op2LclNum, typeHnd, false);
            op2Assign = gtNewTempAssign(op2LclNum, op2);
            op2 = gtNewLclvNode(op2LclNum, op2->TypeGet());
        }
        else
        {
            dupOp2 = gtCloneExpr(op2);
        }

        SIMDIntrinsicID relOpIntrinsic = (intrinsicId == SIMDIntrinsicMin) ? SIMDIntrinsicLessThan : SIMDIntrinsicGreaterThan;
        var_types relOpBaseType = baseType;

        // compResult = op1 relOp op2
        // simdTree = Select(compResult, op1, op2);
        assert(dupOp1 != nullptr);
        assert(dupOp2 != nullptr);
        relOpIntrinsic = impSIMDRelOp(relOpIntrinsic, typeHnd, size, &relOpBaseType,  &dupOp1, &dupOp2);
        GenTree* compResult = gtNewSIMDNode(simdType, dupOp1, dupOp2, relOpIntrinsic, relOpBaseType, size);
        unsigned compResultLclNum = lvaGrabTemp(true DEBUGARG("SIMD Min/Max"));
        lvaSetStruct(compResultLclNum, typeHnd, false);
        GenTree* compResultAssign = gtNewTempAssign(compResultLclNum, compResult);
        compResult = gtNewLclvNode(compResultLclNum, compResult->TypeGet());
        simdTree = impSIMDSelect(typeHnd, baseType, size, compResult, op1, op2);
        simdTree = gtNewOperNode(GT_COMMA, simdTree->TypeGet(), compResultAssign, simdTree);
        
        // Now create comma trees if we have created assignments of op1/op2 to temps
        if (op2Assign != nullptr)
        {
            simdTree  = gtNewOperNode(GT_COMMA, simdTree->TypeGet(), op2Assign, simdTree);
        }

        if (op1Assign != nullptr)
        {
            simdTree  = gtNewOperNode(GT_COMMA, simdTree->TypeGet(), op1Assign, simdTree);
        }
    }

    assert(simdTree != nullptr);
    return simdTree;
#else
    assert(!"impSIMDMinMax() unimplemented on target arch");
    unreached();
#endif //_TARGET_AMD64_
}

//------------------------------------------------------------------------
// getOp1ForConstructor: Get the op1 for a constructor call.
//
// Arguments:
//    opcode     - the opcode being handled (needed to identify the CEE_NEWOBJ case)
//    newobjThis - For CEE_NEWOBJ, this is the temp grabbed for the allocated uninitalized object.
//    clsHnd    - The handle of the class of the method.
//
// Return Value:
//    The tree node representing the object to be initialized with the constructor.
//
// Notes:
//    This method handles the differences between the CEE_NEWOBJ and constructor cases.
//
GenTreePtr Compiler::getOp1ForConstructor(OPCODE                   opcode,
                                          GenTreePtr               newobjThis,
                                          CORINFO_CLASS_HANDLE     clsHnd)
{
    GenTree* op1;
    if (opcode == CEE_NEWOBJ)
    {
        op1 = newobjThis;
        assert(newobjThis->gtOper == GT_ADDR &&
            newobjThis->gtOp.gtOp1->gtOper == GT_LCL_VAR);

        // push newobj result on type stack
        unsigned tmp = op1->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
        impPushOnStack(gtNewLclvNode(tmp, lvaGetRealType(tmp)), verMakeTypeInfo(clsHnd).NormaliseForStack());
    }
    else
    {
        op1 = impSIMDPopStack(TYP_BYREF);
    }
    assert(op1->TypeGet() == TYP_BYREF);
    return op1;
}

//-------------------------------------------------------------------
// Set the flag that indicates that the lclVar referenced by this tree 
// is used in a SIMD intrinsic.
// Arguments:
//      tree - GenTreePtr

void Compiler::setLclRelatedToSIMDIntrinsic(GenTreePtr tree)
{
        assert(tree->OperIsLocal());
        unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
        LclVarDsc* lclVarDsc = &lvaTable[lclNum];
        lclVarDsc->lvUsedInSIMDIntrinsic = true;
}

//-------------------------------------------------------------
// Check if two field nodes reference at the same memory location.
// Notice that this check is just based on pattern matching. 
// Arguments:
//      op1 - GenTreePtr. 
//      op2 - GenTreePtr. 
// Return Value:
//    If op1's parents node and op2's parents node are at the same location, return true. Otherwise, return false

bool areFieldsParentsLocatedSame(GenTreePtr op1, GenTreePtr op2)
{
    assert(op1->OperGet() == GT_FIELD);
    assert(op2->OperGet() == GT_FIELD);
    
    GenTreePtr op1ObjRef = op1->gtField.gtFldObj;
    GenTreePtr op2ObjRef = op2->gtField.gtFldObj;
    while (op1ObjRef != nullptr && op2ObjRef != nullptr)
    {

        if(op1ObjRef->OperGet() != op2ObjRef->OperGet())
        {
            break;
        }
        else if (op1ObjRef->OperGet() == GT_ADDR)
        {
            op1ObjRef = op1ObjRef->gtOp.gtOp1;
            op2ObjRef = op2ObjRef->gtOp.gtOp1;
        }

        if (op1ObjRef->OperIsLocal() && 
            op2ObjRef->OperIsLocal() &&
            op1ObjRef->AsLclVarCommon()->GetLclNum() == op2ObjRef->AsLclVarCommon()->GetLclNum())
        {
            return true;
        }
        else if (op1ObjRef->OperGet() == GT_FIELD && 
                 op2ObjRef->OperGet() == GT_FIELD && 
                 op1ObjRef->gtField.gtFldHnd == op2ObjRef->gtField.gtFldHnd)
        {
            op1ObjRef = op1ObjRef->gtField.gtFldObj;
            op2ObjRef = op2ObjRef->gtField.gtFldObj;
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
//      first - GenTreePtr. The Type of the node should be TYP_FLOAT
//      second - GenTreePtr. The Type of the node should be TYP_FLOAT
// Return Value:
//      if the first field is located before second field, and they are located contiguously,
//      then return true. Otherwise, return false.

bool Compiler::areFieldsContiguous(GenTreePtr first, GenTreePtr second)
{
    assert(first->OperGet() == GT_FIELD);
    assert(second->OperGet() == GT_FIELD); 
    assert(first->gtType == TYP_FLOAT);
    assert(second->gtType == TYP_FLOAT);
    
    var_types firstFieldType = first->gtType;
    var_types secondFieldType = second->gtType;
    
    unsigned firstFieldEndOffset = first->gtField.gtFldOffset + genTypeSize(firstFieldType);
    unsigned secondFieldOffset = second->gtField.gtFldOffset;
    if (firstFieldEndOffset == secondFieldOffset &&
        firstFieldType == secondFieldType        &&
        areFieldsParentsLocatedSame(first, second))
    {
        return true;
    }

    return false;
}

//-------------------------------------------------------------------------------
// Check whether two array element nodes are located contiguously or not.
// Arguments:
//      op1 - GenTreePtr.
//      op2 - GenTreePtr.
// Return Value:
//      if the array element op1 is located before array element op2, and they are contiguous,
//      then return true. Otherwise, return false.
// TODO-CQ: 
//      Right this can only check array element with const number as index. In future, 
//      we should consider to allow this function to check the index using expression.

bool Compiler::areArrayElementsContiguous(GenTreePtr op1, GenTreePtr op2)
{
    noway_assert(op1->gtOper == GT_INDEX);
    noway_assert(op2->gtOper == GT_INDEX);
    GenTreeIndex* op1Index = op1->AsIndex();
    GenTreeIndex* op2Index = op2->AsIndex();

    GenTreePtr op1ArrayRef = op1Index->Arr();
    GenTreePtr op2ArrayRef = op2Index->Arr();
    assert(op1ArrayRef->TypeGet() == TYP_REF);
    assert(op2ArrayRef->TypeGet() == TYP_REF);

    GenTreePtr op1IndexNode = op1Index->Index();
    GenTreePtr op2IndexNode = op2Index->Index();
    if ((op1IndexNode->OperGet() == GT_CNS_INT && op2IndexNode->OperGet() == GT_CNS_INT) &&
        op1IndexNode->gtIntCon.gtIconVal + 1 == op2IndexNode->gtIntCon.gtIconVal)
    {
        if (op1ArrayRef->OperGet() == GT_FIELD &&
            op2ArrayRef->OperGet() == GT_FIELD &&
            areFieldsParentsLocatedSame(op1ArrayRef, op2ArrayRef))
        {
            return true;
        }
        else if (op1ArrayRef->OperIsLocal() && op2ArrayRef->OperIsLocal() &&
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
//      op1 - GenTreePtr. 
//      op2 - GenTreePtr. 
// Return Value:
//      if the argument node op1 is located before argument node op2, and they are located contiguously,
//      then return true. Otherwise, return false.
// TODO-CQ: 
//      Right now this can only check field and array. In future we should add more cases.
//      

bool Compiler::areArgumentsContiguous(GenTreePtr op1, GenTreePtr op2)
{
    if(op1->OperGet() == GT_INDEX && op2->OperGet() == GT_INDEX)
    {
        return areArrayElementsContiguous(op1, op2);
    }
    else if(op1->OperGet() == GT_FIELD && op2->OperGet() == GT_FIELD)
    {
        return areFieldsContiguous(op1, op2);
    }
    return false;
} 

//--------------------------------------------------------------------------------------------------------
// createAddressNodeForSIMDInit: Generate the address node(GT_LEA) if we want to intialize vector2, vector3 or vector4 from first argument's address. 
//
// Arguments:
//      tree - GenTreePtr. This the tree node which is used to get the address for indir.
//      simdsize - unsigned. This the simd vector size.
//      arrayElementsCount - unsigned. This is used for generating the boundary check for array.
//
// Return value:
//      return the address node.
//
// TODO-CQ: 
//      1. Currently just support for GT_FIELD and GT_INDEX, because we can only verify the GT_INDEX node or GT_Field are located contiguously or not.
//      In future we should support more cases.
//      2.Though it happens to just work fine front-end phases are not aware of GT_LEA node.  Therefore, convert these to use GT_ADDR .   
GenTreePtr Compiler::createAddressNodeForSIMDInit(GenTreePtr tree, unsigned simdSize)
{
    assert(tree->OperGet() == GT_FIELD || tree->OperGet() == GT_INDEX);
    GenTreePtr byrefNode = nullptr;
    GenTreePtr startIndex = nullptr;
    unsigned offset = 0;
    var_types baseType = tree->gtType;
    
    if (tree->OperGet() == GT_FIELD)
    {
        GenTreePtr objRef = tree->gtField.gtFldObj;
        if(objRef != nullptr && objRef->gtOper == GT_ADDR)
        {
            GenTreePtr obj = objRef->gtOp.gtOp1;

            // If the field is directly from a struct, then in this case,
            // we should set this struct's lvUsedInSIMDIntrinsic as true, 
            // so that this sturct won't be promoted.
            // e.g. s.x x is a field, and s is a struct, then we should set the s's lvUsedInSIMDIntrinsic as true.
            // so that s won't be promoted.
            // Notice that if we have a case like s1.s2.x. s1 s2 are struct, and x is a field, then it is possible that s1 can be promoted, so that s2 can be promoted.
            // The reason for that is if we don't allow s1 to be promoted, then this will affect the other optimizations which are depend on s1's struct promotion.
            // TODO-CQ:
            //  In future, we should optimize this case so that if there is a nested field like s1.s2.x and s1.s2.x's address is used for 
            //  initializing the vector, then s1 can be promoted but s2 can't. 
            if(varTypeIsSIMD(obj) && obj->OperIsLocal())
            {
                setLclRelatedToSIMDIntrinsic(obj);
            }
        }

        byrefNode = gtCloneExpr(tree->gtField.gtFldObj);
        assert(byrefNode != nullptr);
        offset = tree->gtField.gtFldOffset;

    }
    else if(tree->OperGet() == GT_INDEX)
    {

        GenTreePtr index = tree->AsIndex()->Index();
        assert(index->OperGet() == GT_CNS_INT);
       
        GenTreePtr checkIndexExpr = nullptr;
        unsigned indexVal = (unsigned)(index->gtIntCon.gtIconVal);
        offset = indexVal * genTypeSize(tree->TypeGet()); 
        GenTreePtr arrayRef = tree->AsIndex()->Arr();
        
        // Generate the boundary check exception.
        // The length for boundary check should be the maximum index number which should be
        // (first argument's index number) + (how many array arguments we have) - 1 
        // = indexVal + arrayElementsCount - 1
        unsigned arrayElementsCount = simdSize / genTypeSize(baseType);
        checkIndexExpr = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT,  indexVal + arrayElementsCount - 1);
        GenTreeArrLen*       arrLen     = new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, arrayRef, (int)offsetof(CORINFO_Array, length));
        GenTreeBoundsChk*    arrBndsChk = new (this, GT_ARR_BOUNDS_CHECK) GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, arrLen, checkIndexExpr, SCK_RNGCHK_FAIL);

        offset += offsetof(CORINFO_Array, u1Elems);
        byrefNode = gtNewOperNode(GT_COMMA, arrayRef->TypeGet(), arrBndsChk, gtCloneExpr(arrayRef));

    }
    else
    {
        unreached();
    }
    GenTreePtr address = new (this, GT_LEA) GenTreeAddrMode(TYP_BYREF, byrefNode, startIndex, genTypeSize(tree->TypeGet()), offset);
    return address;
}

//-------------------------------------------------------------------------------
// impMarkContiguousSIMDFieldAssignments: Try to identify if there are contiguous 
// assignments from SIMD field to memory. If there are, then mark the related 
// lclvar so that it won't be promoted.
//
// Arguments:
//      stmt - GenTreePtr. Input statement node.

void Compiler::impMarkContiguousSIMDFieldAssignments(GenTreePtr stmt)
{
    if (!featureSIMD || opts.MinOpts())
    {
        return;
    }
    GenTreePtr expr = stmt->gtStmt.gtStmtExpr;
    if (expr->OperGet() == GT_ASG &&
        expr->TypeGet() == TYP_FLOAT)
    {
        GenTreePtr curDst = expr->gtOp.gtOp1;
        GenTreePtr curSrc = expr->gtOp.gtOp2; 
        unsigned index = 0;
        var_types baseType = TYP_UNKNOWN;
        unsigned simdSize = 0;
        GenTreePtr srcSimdStructNode = getSIMDStructFromField(curSrc, &baseType, &index, &simdSize, true);
        if (srcSimdStructNode == nullptr ||
            baseType != TYP_FLOAT)
        {
            fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
        }
        else if (index == 0 && isSIMDTypeLocal(srcSimdStructNode))
        {
            fgPreviousCandidateSIMDFieldAsgStmt = stmt;
        }
        else if (fgPreviousCandidateSIMDFieldAsgStmt != nullptr)
        {
            assert(index > 0);
            GenTreePtr prevAsgExpr = fgPreviousCandidateSIMDFieldAsgStmt->gtStmt.gtStmtExpr;
            GenTreePtr prevDst = prevAsgExpr->gtOp.gtOp1;
            GenTreePtr prevSrc = prevAsgExpr->gtOp.gtOp2;
            if (!areArgumentsContiguous(prevDst, curDst) ||
                !areArgumentsContiguous(prevSrc, curSrc))
            {
                fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
            }
            else
            {
                if (index == (simdSize / genTypeSize(baseType) - 1))
                {
                    // Successfully found the pattern, mark the lclvar as UsedInSIMDIntrinsic
                    if (srcSimdStructNode->OperIsLocal())
                    {
                        setLclRelatedToSIMDIntrinsic(srcSimdStructNode);
                    }

                    if (curDst->OperGet() == GT_FIELD)
                    {
                        GenTreePtr objRef = curDst->gtField.gtFldObj;
                        if (objRef != nullptr && objRef->gtOper == GT_ADDR)
                        {
                            GenTreePtr obj = objRef->gtOp.gtOp1;
                            if (varTypeIsStruct(obj) && obj->OperIsLocal())
                            {
                                setLclRelatedToSIMDIntrinsic(obj);
                            }
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

//------------------------------------------------------------------------
// impSIMDIntrinsic: Check method to see if it is a SIMD method
//
// Arguments:
//    opcode     - the opcode being handled (needed to identify the CEE_NEWOBJ case)
//    newobjThis - For CEE_NEWOBJ, this is the temp grabbed for the allocated uninitalized object.
//    clsHnd     - The handle of the class of the method.
//    method     - The handle of the method.
//    sig        - The call signature for the method.
//    memberRef  - The memberRef token for the method reference.
//
// Return Value:
//    If clsHnd is a known SIMD type, and 'method' is one of the methods that are
//    implemented as an intrinsic in the JIT, then return the tree that implements
//    it.
//
GenTreePtr Compiler::impSIMDIntrinsic(OPCODE                   opcode,
                                      GenTreePtr               newobjThis,
                                      CORINFO_CLASS_HANDLE     clsHnd,
                                      CORINFO_METHOD_HANDLE    methodHnd,
                                      CORINFO_SIG_INFO *       sig,
                                      int                      memberRef)
{
    assert(featureSIMD);

    if (!isSIMDClass(clsHnd))
    {
        return nullptr;
    }

    // Get base type and intrinsic Id
    var_types baseType = TYP_UNKNOWN;
    unsigned  size = 0;
    unsigned  argCount = 0;
    const SIMDIntrinsicInfo* intrinsicInfo = getSIMDIntrinsicInfo(&clsHnd, methodHnd, sig, (opcode == CEE_NEWOBJ), &argCount, &baseType, &size);
    if (intrinsicInfo == nullptr || intrinsicInfo->id == SIMDIntrinsicInvalid)
    {
        return nullptr;
    }

    SIMDIntrinsicID simdIntrinsicID = intrinsicInfo->id;
    var_types simdType;
    if (baseType != TYP_UNKNOWN)
    {
        simdType = getSIMDTypeForSize(size);
    }
    else
    {
        assert(simdIntrinsicID == SIMDIntrinsicHWAccel);
        simdType = TYP_UNKNOWN;
    }
    bool instMethod = intrinsicInfo->isInstMethod;
    var_types  callType = JITtype2varType(sig->retType);       
    if (callType == TYP_STRUCT)
    {
        // Note that here we are assuming that, if the call returns a struct, that it is the same size as the
        // struct on which the method is declared. This is currently true for all methods on Vector types,
        // but if this ever changes, we will need to determine the callType from the signature.
        assert(info.compCompHnd->getClassSize(sig->retTypeClass) == genTypeSize(simdType));
        callType = simdType;
    }

    GenTree*     simdTree   = nullptr;
    GenTree*     op1        = nullptr;
    GenTree*     op2        = nullptr;
    GenTree*     op3        = nullptr;
    GenTree*     retVal     = nullptr;
    GenTree*     copyBlkDst = nullptr;
    bool         doCopyBlk  = false;

    switch(simdIntrinsicID)
    {
    case SIMDIntrinsicGetCount:
        {
            int length = getSIMDVectorLength(clsHnd);
            GenTreeIntCon* intConstTree = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, length);
            retVal = intConstTree;
        }
        break;

    case SIMDIntrinsicGetZero:
        {
            baseType = genActualType(baseType);
            GenTree *initVal = gtNewZeroConNode(baseType);
            initVal->gtType = baseType;
            simdTree = gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicGetOne:
        {
            GenTree *initVal;
            if (varTypeIsSmallInt(baseType))
            {
                unsigned baseSize = genTypeSize(baseType);
                int val;
                if (baseSize == 1)
                {
                    val = 0x01010101;
                }
                else
                {
                    val = 0x00010001;
                }
                initVal = gtNewIconNode(val);
            }
            else
            {
                initVal = gtNewOneConNode(baseType);
            }

            baseType = genActualType(baseType);
            initVal->gtType = baseType;
            simdTree = gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicGetAllOnes:
        {
            // Equivalent to (Vector<T>) new Vector<int>(0xffffffff);
            GenTree *initVal = gtNewIconNode(0xffffffff, TYP_INT);
            simdTree = gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, TYP_INT, size);
            if (baseType != TYP_INT)
            {
                // cast it to required baseType if different from TYP_INT
                simdTree = gtNewSIMDNode(simdType, simdTree, nullptr, SIMDIntrinsicCast, baseType, size);
            }
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicInit:
    case SIMDIntrinsicInitN:
        {
            // SIMDIntrinsicInit:
            //    op2 - the initializer value
            //    op1 - byref of vector
            //
            // SIMDIntrinsicInitN
            //    op2 - list of initializer values stitched into a list
            //    op1 - byref of vector
            bool initFromFirstArgIndir = false;
            if (simdIntrinsicID == SIMDIntrinsicInit)
            {
                op2 = impSIMDPopStack(baseType);            
            }
            else 
            {
                assert(simdIntrinsicID == SIMDIntrinsicInitN);
                assert(baseType == TYP_FLOAT);

                unsigned initCount = argCount - 1;
                unsigned elementCount = getSIMDVectorLength(size, baseType);
                noway_assert(initCount == elementCount);
                GenTree* nextArg = op2;
            
                // Build a GT_LIST with the N values.
                // We must maintain left-to-right order of the args, but we will pop
                // them off in reverse order (the Nth arg was pushed onto the stack last).
                
                GenTree* list = nullptr;
                GenTreePtr firstArg = nullptr;
                GenTreePtr prevArg = nullptr;
                int offset = 0;
                bool areArgsContiguous = true;
                for (unsigned i = 0; i < initCount; i++)
                {   
                    GenTree* nextArg = impSIMDPopStack(baseType);
                    if (areArgsContiguous)
                    {                      
                        GenTreePtr curArg = nextArg;
                        firstArg = curArg;    
                        
                        if(prevArg != nullptr)
                        {
                            // Recall that we are popping the args off the stack in reverse order.
                            areArgsContiguous = areArgumentsContiguous(curArg, prevArg);
                        }
                        prevArg = curArg;   
                    }
               
                    list  = new (this, GT_LIST) GenTreeOp(GT_LIST, baseType, nextArg, list);
                }

                if (areArgsContiguous && baseType == TYP_FLOAT)
                {
                    // Since Vector2, Vector3 and Vector4's arguments type are only float, 
                    // we intialize the vector from first argument address, only when 
                    // the baseType is TYP_FLOAT and the arguments are located contiguously in memory
                    initFromFirstArgIndir = true;
                    GenTreePtr op2Address = createAddressNodeForSIMDInit(firstArg, size);
                    var_types simdType = getSIMDTypeForSize(size);
                    op2 = gtNewOperNode(GT_IND, simdType, op2Address);
                }
                else
                {
                    op2 = list;
                }
            }

            op1 = getOp1ForConstructor(opcode, newobjThis, clsHnd);

            assert(op1->TypeGet() == TYP_BYREF);
            assert(genActualType(op2->TypeGet()) == genActualType(baseType)||initFromFirstArgIndir);

#if AVX_WITHOUT_AVX2
            // NOTE: This #define, AVX_WITHOUT_AVX2, is never defined.  This code is kept here
            // in case we decide to implement AVX support (32 byte vectors) with AVX only.
            // On AVX (as opposed to AVX2), broadcast is supported only for float and double,
            // and requires taking a mem address of the value.
            // If not a constant, take the addr of op2.
            if (simdIntrinsicID == SIMDIntrinsicInit && canUseAVX())
            {
                if (!op2->OperIsConst())
                {
                    // It is better to assign op2 to a temp and take the addr of temp
                    // rather than taking address of op2 since the latter would make op2
                    // address-taken and ineligible for register allocation.
                    //
                    // op2 = GT_COMMA(tmp=op2, GT_ADDR(tmp))
                    unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Val addr for vector Init"));
                    GenTreePtr asg  = gtNewTempAssign(tmpNum, op2);
                    GenTreePtr tmp = gtNewLclvNode(tmpNum, op2->TypeGet());
                    tmp = gtNewOperNode(GT_ADDR, TYP_BYREF,tmp);
                    op2 = gtNewOperNode(GT_COMMA, TYP_BYREF, asg, tmp);
                }
            }
#endif
            // For integral base types of size less than TYP_INT, expand the initializer
            // to fill size of TYP_INT bytes.
            if (varTypeIsSmallInt(baseType))
            {
                // This case should occur only for Init intrinsic.
                assert(simdIntrinsicID == SIMDIntrinsicInit);

                unsigned baseSize = genTypeSize(baseType);
                int multiplier;
                if (baseSize == 1)
                {
                    multiplier = 0x01010101;
                }
                else
                {
                    assert(baseSize == 2);
                    multiplier = 0x00010001;
                }

                GenTree* t1 = nullptr;
                if (baseType == TYP_BYTE)
                {
                    // What we have is a signed byte initializer,
                    // which when loaded to a reg will get sign extended to TYP_INT.
                    // But what we need is the initializer without sign extended or
                    // rather zero extended to 32-bits.
                    t1 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(0xff, TYP_INT));
                }
                else if (baseType == TYP_SHORT)
                {
                    // What we have is a signed short initializer,
                    // which when loaded to a reg will get sign extended to TYP_INT.
                    // But what we need is the initializer without sign extended or
                    // rather zero extended to 32-bits.
                    t1 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(0xffff, TYP_INT));
                }
                else
                {
                    assert(baseType == TYP_UBYTE || baseType == TYP_CHAR);
                    t1 = gtNewCastNode(TYP_INT, op2, TYP_INT);
                }
                
                assert(t1 != nullptr);
                GenTree* t2 = gtNewIconNode(multiplier, TYP_INT);
                op2 = gtNewOperNode(GT_MUL, TYP_INT, t1, t2);

                // Construct a vector of TYP_INT with the new initializer and cast it back to vector of baseType
                simdTree = gtNewSIMDNode(simdType, op2, nullptr, simdIntrinsicID, TYP_INT, size);
                simdTree = gtNewSIMDNode(simdType, simdTree, nullptr, SIMDIntrinsicCast, baseType, size);
            }
            else
            {

                if (initFromFirstArgIndir)
                {
                    simdTree = op2;
                    if (op1->gtOp.gtOp1->OperIsLocal())
                    {
                        // label the dst struct's lclvar is used for SIMD intrinsic,
                        // so that this dst struct won't be promoted.
                        setLclRelatedToSIMDIntrinsic(op1->gtOp.gtOp1);
                    }
                }
                else
                {
                    simdTree = gtNewSIMDNode(simdType, op2, nullptr, simdIntrinsicID, baseType, size);
                }
            }

            copyBlkDst = op1;
            doCopyBlk = true;
        }
        break;

    case SIMDIntrinsicInitArray:
    case SIMDIntrinsicInitArrayX:
    case SIMDIntrinsicCopyToArray:
    case SIMDIntrinsicCopyToArrayX:
        {
            // op3 - index into array in case of SIMDIntrinsicCopyToArrayX and SIMDIntrinsicInitArrayX
            // op2 - array itself
            // op1 - byref to vector struct

            unsigned int vectorLength = getSIMDVectorLength(size, baseType);
            // (This constructor takes only the zero-based arrays.)
            // We will add one or two bounds checks:
            // 1. If we have an index, we must do a check on that first.
            //    We can't combine it with the index + vectorLength check because
            //    a. It might be negative, and b. It may need to raise a different exception
            //    (captured as SCK_ARG_RNG_EXCPN for CopyTo and SCK_RNGCHK_FAIL for Init). 
            // 2. We need to generate a check (SCK_ARG_EXCPN for CopyTo and SCK_RNGCHK_FAIL for Init)
            //    for the last array element we will access.
            //    We'll either check against (vectorLength - 1) or (index + vectorLength - 1).

            GenTree* checkIndexExpr = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, vectorLength - 1);

            // Get the index into the array.  If it has been provided, it will be on the
            // top of the stack.  Otherwise, it is null.
            if (argCount == 3)
            {
                op3 = impSIMDPopStack(TYP_INT);
                if (op3->IsZero())
                {
                    op3 = nullptr;
                }
            }
            else
            {
                // TODO-CQ: Here, or elsewhere, check for the pattern where op2 is a newly constructed array, and
                // change this to the InitN form.
                // op3 = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, 0);
                op3 = nullptr;
            }

            // Clone the array for use in the bounds check.
            op2 = impSIMDPopStack(TYP_REF);
            assert(op2->TypeGet() == TYP_REF);
            GenTree* arrayRefForArgChk = op2;
            GenTree* argRngChk = nullptr;
            GenTree* asg = nullptr;
            if ((arrayRefForArgChk->gtFlags & GTF_SIDE_EFFECT) != 0)
            {
                op2 = fgInsertCommaFormTemp(&arrayRefForArgChk);
            }
            else
            {
                op2 = gtCloneExpr(arrayRefForArgChk);
            }
            assert(op2 != nullptr);

            if (op3 != nullptr)
            {
                SpecialCodeKind op3CheckKind;
                if (simdIntrinsicID == SIMDIntrinsicInitArrayX)
                {
                    op3CheckKind = SCK_RNGCHK_FAIL;
                }
                else
                {
                    assert(simdIntrinsicID == SIMDIntrinsicCopyToArrayX);
                    op3CheckKind = SCK_ARG_RNG_EXCPN;
                }
                // We need to use the original expression on this, which is the first check.
                GenTree* arrayRefForArgRngChk = arrayRefForArgChk;
                // Then we clone the clone we just made for the next check.
                arrayRefForArgChk = gtCloneExpr(op2);
                // We know we MUST have had a cloneable expression.
                assert(arrayRefForArgChk != nullptr);
                GenTree* index = op3;
                if ((index->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    op3 = fgInsertCommaFormTemp(&index);
                }
                else
                {
                    op3 = gtCloneExpr(index);
                }

                GenTreeArrLen* arrLen = new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, arrayRefForArgRngChk, (int)offsetof(CORINFO_Array, length));
                argRngChk = new (this, GT_ARR_BOUNDS_CHECK) GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, arrLen, index, op3CheckKind);
                // Now, clone op3 to create another node for the argChk
                GenTree* index2 = gtCloneExpr(op3);
                assert(index != nullptr);
                checkIndexExpr = gtNewOperNode(GT_ADD, TYP_INT, index2, checkIndexExpr);
            }

            // Insert a bounds check for index + offset - 1.
            // This must be a "normal" array.
            SpecialCodeKind op2CheckKind;
            if (simdIntrinsicID == SIMDIntrinsicInitArray || simdIntrinsicID == SIMDIntrinsicInitArrayX)
            {
                op2CheckKind = SCK_RNGCHK_FAIL;
            }
            else
            {
                op2CheckKind = SCK_ARG_EXCPN;
            }
            GenTreeArrLen*       arrLen     = new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, arrayRefForArgChk, (int)offsetof(CORINFO_Array, length));
            GenTreeBoundsChk*    argChk = new (this, GT_ARR_BOUNDS_CHECK) GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, arrLen, checkIndexExpr, op2CheckKind);

            // Create a GT_COMMA tree for the bounds check(s).
            op2 = gtNewOperNode(GT_COMMA, op2->TypeGet(), argChk, op2);
            if (argRngChk != nullptr)
            {
                op2 = gtNewOperNode(GT_COMMA, op2->TypeGet(), argRngChk, op2);
            }

            if (simdIntrinsicID == SIMDIntrinsicInitArray || simdIntrinsicID == SIMDIntrinsicInitArrayX)
            {
                op1 = getOp1ForConstructor(opcode, newobjThis, clsHnd);
                simdTree = gtNewSIMDNode(simdType, op2, op3, SIMDIntrinsicInitArray, baseType, size);
                copyBlkDst = op1;
                doCopyBlk = true;
            }
            else
            {
                assert(simdIntrinsicID == SIMDIntrinsicCopyToArray || simdIntrinsicID == SIMDIntrinsicCopyToArrayX);
                op1 = impSIMDPopStack(simdType, instMethod);                
                assert(op1->TypeGet() == simdType);

                // copy vector (op1) to array (op2) starting at index (op3)
                simdTree = op1;
                
                // TODO-Cleanup: Though it happens to just work fine front-end phases are not aware of GT_LEA node.  Therefore, convert these to use GT_ADDR .   
                copyBlkDst = new (this, GT_LEA) GenTreeAddrMode(TYP_BYREF, op2, op3, genTypeSize(baseType),  offsetof(CORINFO_Array, u1Elems));
                doCopyBlk = true;                
            }
        }
        break;

    case SIMDIntrinsicInitFixed:
        {
            // We are initializing a fixed-length vector VLarge with a smaller fixed-length vector VSmall, plus 1 or 2 additional floats.
            //    op4 (optional) - float value for VLarge.W, if VLarge is Vector4, and VSmall is Vector2
            //    op3 - float value for VLarge.Z or VLarge.W
            //    op2 - VSmall
            //    op1 - byref of VLarge
            assert(baseType == TYP_FLOAT);
            unsigned elementByteCount = 4;

            GenTree* op4 = nullptr;
            if (argCount == 4)
            {
                op4 = impSIMDPopStack(TYP_FLOAT);
                assert(op4->TypeGet() == TYP_FLOAT);
            }
            op3 = impSIMDPopStack(TYP_FLOAT);
            assert(op3->TypeGet() == TYP_FLOAT);
            // The input vector will either be TYP_SIMD8 or TYP_SIMD12.
            var_types smallSIMDType = TYP_SIMD8;
            if ((op4 == nullptr) && (simdType == TYP_SIMD16))
            {
                smallSIMDType = TYP_SIMD12;
            }
            op2 = impSIMDPopStack(smallSIMDType);
            op1 = getOp1ForConstructor(opcode, newobjThis, clsHnd);

            // We are going to redefine the operands so that:
            // - op3 is the value that's going into the Z position, or null if it's a Vector4 constructor with a single operand, and
            // - op4 is the W position value, or null if this is a Vector3 constructor.
            if (size == 16 && argCount == 3)
            {
                op4 = op3;
                op3 = nullptr;
            }

            simdTree = op2;
            if (op3 != nullptr)
            {
                simdTree = gtNewSIMDNode(simdType, simdTree, op3, SIMDIntrinsicSetZ, baseType, size);
            }
            if (op4 != nullptr)
            {
                simdTree = gtNewSIMDNode(simdType, simdTree, op4, SIMDIntrinsicSetW, baseType, size);
            }

            copyBlkDst = op1;
            doCopyBlk = true;
        }
        break;

    case SIMDIntrinsicOpEquality:
    case SIMDIntrinsicInstEquals:
        {
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            assert(op1->TypeGet() == simdType);
            assert(op2->TypeGet() == simdType);

            simdTree = gtNewSIMDNode(genActualType(callType), op1, op2, SIMDIntrinsicOpEquality, baseType, size);
            retVal = simdTree;
        }
        break;
    
    case SIMDIntrinsicOpInEquality:
        {
            // op1 is the first operand
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            assert(op1->TypeGet() == simdType);
            assert(op2->TypeGet() == simdType);

            simdTree = gtNewSIMDNode(genActualType(callType), op1, op2, SIMDIntrinsicOpInEquality, baseType, size);
            retVal = simdTree;
        }
        break;    
    
    case SIMDIntrinsicEqual:
    case SIMDIntrinsicLessThan:    
    case SIMDIntrinsicLessThanOrEqual:   
    case SIMDIntrinsicGreaterThan:    
    case SIMDIntrinsicGreaterThanOrEqual:
        {          
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            assert(op1->TypeGet() == simdType);
            assert(op2->TypeGet() == simdType);

            SIMDIntrinsicID intrinsicID = impSIMDRelOp(simdIntrinsicID, clsHnd, size, &baseType, &op1, &op2);
            simdTree = gtNewSIMDNode(genActualType(callType), op1, op2, intrinsicID, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicAdd:        
    case SIMDIntrinsicSub:     
    case SIMDIntrinsicMul:
    case SIMDIntrinsicDiv:
    case SIMDIntrinsicBitwiseAnd:
    case SIMDIntrinsicBitwiseAndNot:    
    case SIMDIntrinsicBitwiseOr:    
    case SIMDIntrinsicBitwiseXor:
        {
#if defined(_TARGET_AMD64_) && defined(DEBUG)
            // check for the cases where we don't support intrinsics.
            // This check should be done before we make modifications to type stack.
            // Note that this is more of a double safety check for robustness since
            // we expect getSIMDIntrinsicInfo() to have filtered out intrinsics on
            // unsupported base types. If getSIMdIntrinsicInfo() doesn't filter due
            // to some bug, assert in chk/dbg will fire.
            if (!varTypeIsFloating(baseType))
            {
                if (simdIntrinsicID == SIMDIntrinsicMul)
                {
                    if ((baseType != TYP_INT) && (baseType != TYP_SHORT))
                    {
                        // TODO-CQ: implement mul on these integer vectors.
                        // Note that SSE2 has no direct support for these vectors.
                        assert(!"Mul not supported on long/ulong/uint/small int vectors\n");
                        return nullptr;
                    }
                }
                
                // common to all integer type vectors
                if (simdIntrinsicID == SIMDIntrinsicDiv)
                {
                    // SSE2 doesn't support div on non-floating point vectors.
                    assert(!"Div not supported on integer type vectors\n");
                    return nullptr;
                }
            }
#endif //_TARGET_AMD64_ && DEBUG

            // op1 is the first operand; if instance method, op1 is "this" arg
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            simdTree = gtNewSIMDNode(simdType, op1, op2, simdIntrinsicID, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicSelect:
        {
            // op3 is a SIMD variable that is the second source
            // op2 is a SIMD variable that is the first source
            // op1 is a SIMD variable which is the bit mask.
            op3 = impSIMDPopStack(simdType);
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            retVal = impSIMDSelect(clsHnd, baseType, size, op1, op2, op3);
        }
        break;

    case SIMDIntrinsicMin:
    case SIMDIntrinsicMax:
        {
            // op1 is the first operand; if instance method, op1 is "this" arg
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            retVal = impSIMDMinMax(simdIntrinsicID, clsHnd, baseType, size, op1, op2);
        }
        break;

    case SIMDIntrinsicGetItem:
        {
            // op1 is a SIMD variable that is "this" arg
            // op2 is an index of TYP_INT
            op2 = impSIMDPopStack(TYP_INT);
            op1 = impSIMDPopStack(simdType, instMethod);
            unsigned int vectorLength = getSIMDVectorLength(size, baseType);
            if (!op2->IsCnsIntOrI() || op2->AsIntCon()->gtIconVal >= vectorLength)
            {
                // We need to bounds-check the length of the vector.
                // For that purpose, we need to clone the index expression.
                GenTree* index = op2;
                if ((index->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    op2 = fgInsertCommaFormTemp(&index);
                }
                else
                {
                    op2 = gtCloneExpr(index);
                }

                GenTree* lengthNode = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, vectorLength);
                GenTreeBoundsChk* simdChk = new (this, GT_SIMD_CHK) GenTreeBoundsChk(GT_SIMD_CHK, TYP_VOID, lengthNode, index, SCK_RNGCHK_FAIL);

                // Create a GT_COMMA tree for the bounds check.
                op2 = gtNewOperNode(GT_COMMA, op2->TypeGet(), simdChk, op2);
            }

            assert(op1->TypeGet() == simdType);
            assert(op2->TypeGet() == TYP_INT);

            simdTree = gtNewSIMDNode(genActualType(callType), op1, op2, simdIntrinsicID, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicDotProduct:
        {
#if defined(_TARGET_AMD64_) && defined(DEBUG)
            // Right now dot product is supported only for float vectors.
            // See SIMDIntrinsicList.h for supported base types for this intrinsic.
            if (!varTypeIsFloating(baseType)) 
            {
                assert(!"Dot product on integer type vectors not supported");
                return nullptr;
            }
#endif //_TARGET_AMD64_ && DEBUG

            // op1 is a SIMD variable that is the first source and also "this" arg.
            // op2 is a SIMD variable which is the second source.
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            simdTree = gtNewSIMDNode(baseType, op1, op2, simdIntrinsicID, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicSqrt:
        {
#if defined(_TARGET_AMD64_) && defined(DEBUG)
                // SSE/AVX doesn't support sqrt on integer type vectors and hence
                // should never be seen as an intrinsic here. See SIMDIntrinsicList.h 
                // for supported base types for this intrinsic.
                if (!varTypeIsFloating(baseType))
                {
                    assert(!"Sqrt not supported on integer vectors\n");
                    return nullptr;
                }
#endif // _TARGET_AMD64_ && DEBUG

            op1 = impSIMDPopStack(simdType);

            retVal = gtNewSIMDNode(genActualType(callType), op1, nullptr, simdIntrinsicID, baseType, size);
        }
        break;

    case SIMDIntrinsicAbs:
        { 
            op1 = impSIMDPopStack(simdType);

#ifdef _TARGET_AMD64_
            if (varTypeIsFloating(baseType))
            {
                // Abs(vf) = vf & new SIMDVector<float>(0x7fffffff);
                // Abs(vd) = vf & new SIMDVector<double>(0x7fffffffffffffff);
                GenTree* bitMask = nullptr;
                if (baseType == TYP_FLOAT)
                {
                    float f;
                    static_assert_no_msg(sizeof(float) == sizeof(int));
                    *((int *)&f) = 0x7fffffff;
                    bitMask = gtNewDconNode(f);
                }
                else if (baseType == TYP_DOUBLE)
                {
                    double d;
                    static_assert_no_msg(sizeof(double) == sizeof(__int64));
                    *((__int64*)&d) = 0x7fffffffffffffffLL;
                    bitMask = gtNewDconNode(d);
                }

                assert(bitMask != nullptr);
                bitMask->gtType = baseType;
                GenTree* bitMaskVector = gtNewSIMDNode(simdType, bitMask, SIMDIntrinsicInit, baseType, size);
                retVal = gtNewSIMDNode(simdType, op1, bitMaskVector, SIMDIntrinsicBitwiseAnd, baseType, size);            
            }
            else if (baseType == TYP_CHAR || baseType == TYP_UBYTE || baseType == TYP_UINT || baseType == TYP_ULONG)
            {
                // Abs is a no-op on unsigned integer type vectors
                retVal = op1;
            }
            else
            {
                // SSE/AVX doesn't support abs on signed integer vectors and hence
                // should never be seen as an intrinsic here. See SIMDIntrinsicList.h
                // for supported base types for this intrinsic.
                unreached();
            }

#else  //!_TARGET_AMD64_
            assert(!"Abs intrinsic on non-Amd64 target not implemented");
            unreached();
#endif  //!_TARGET_AMD64_
        }
        break;

    case SIMDIntrinsicGetW:
        retVal = impSIMDGetFixed(simdType, baseType, size, 3);
        break;

    case SIMDIntrinsicGetZ:
        retVal = impSIMDGetFixed(simdType, baseType, size, 2);
        break;

    case SIMDIntrinsicGetY:
        retVal = impSIMDGetFixed(simdType, baseType, size, 1);
        break;

    case SIMDIntrinsicGetX:
        retVal = impSIMDGetFixed(simdType, baseType, size, 0);
        break;

    case SIMDIntrinsicSetW:
    case SIMDIntrinsicSetZ:
    case SIMDIntrinsicSetY:
    case SIMDIntrinsicSetX:
        {            
            // op2 is the value to be set at indexTemp position
            // op1 is SIMD vector that is going to be modified, which is a byref

            // If op1 has a side-effect, then don't make it an intrinsic.
            // It would be in-efficient to read the entire vector into xmm reg,
            // modify it and write back entire xmm reg.           
            //
            // TODO-CQ: revisit this later.
            op1 = impStackTop(1).val;
            if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
            {
                return nullptr;
            }

            op2 = impSIMDPopStack(baseType);
            op1 = impSIMDPopStack(simdType, instMethod);

            GenTree* src = gtCloneExpr(op1);
            assert(src != nullptr);
            simdTree = gtNewSIMDNode(simdType, src, op2, simdIntrinsicID, baseType, size);
            
            copyBlkDst = gtNewOperNode(GT_ADDR, TYP_BYREF, op1);
            doCopyBlk = true;
        }
        break;

    // Unary operators that take and return a Vector.
    case SIMDIntrinsicCast:
        {
            op1 = impSIMDPopStack(simdType, instMethod);

            simdTree = gtNewSIMDNode(simdType, op1, nullptr, simdIntrinsicID, baseType, size);
            retVal = simdTree;
        }
        break;

    case SIMDIntrinsicHWAccel:
        {
            GenTreeIntCon* intConstTree = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, 1);
            retVal = intConstTree;
        }
        break;

    default:
        assert(!"Unimplemented SIMD Intrinsic");
        return nullptr;
    }

#ifdef _TARGET_AMD64_
    // Amd64: also indicate that we use floating point registers.
    // The need for setting this here is that a method may not have SIMD
    // type lclvars, but might be exercising SIMD intrinsics on fields of
    // SIMD type.
    //
    // e.g.  public Vector<float> ComplexVecFloat::sqabs() { return this.r * this.r + this.i * this.i; }
    compFloatingPointUsed = true;
#endif

    // At this point, we have a tree that we are going to store into a destination.
    // TODO-Cleanup: Once we've "plumbed" SIMD types all the way through the front-end, this should
    // be a simple store or assignment.
    if (doCopyBlk)
    {
        retVal = gtNewBlkOpNode(GT_COPYBLK,
                                copyBlkDst,
                                gtNewOperNode(GT_ADDR, TYP_BYREF,  simdTree),
                                gtNewIconNode(getSIMDTypeSizeInBytes(clsHnd)),
                                false);
        retVal->gtFlags |= ((simdTree->gtFlags | copyBlkDst->gtFlags) & GTF_ALL_EFFECT);
    }

    return retVal;
}

#endif // FEATURE_SIMD

#endif // !LEGACY_BACKEND
