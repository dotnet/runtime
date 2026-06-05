// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// lookupInstructionSet: Gets the InstructionSet for a given class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//
// Return Value:
//    The InstructionSet associated with className
CORINFO_InstructionSet Compiler::lookupInstructionSet(const char* className)
{
    assert(className != nullptr);
    if (strcmp(className, "WasmBase") == 0)
    {
        return InstructionSet_WasmBase;
    }
    else if (strcmp(className, "PackedSimd") == 0)
    {
        return InstructionSet_PackedSimd;
    }
    else if (strcmp(className, "Vector128") == 0)
    {
        return InstructionSet_Vector128;
    }

    return InstructionSet_ILLEGAL;
}

int HWIntrinsicInfo::lookupImmUpperBound(NamedIntrinsic id, var_types baseType)
{
    NYI_WASM_SIMD("lookupImmUpperBound");
    return 0;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclosing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    innerEnclosingClassName -- The name of the inner enclosing class or nullptr if one doesn't exist
//    outerEnclosingClassName -- The name of the outer enclosing class or nullptr if one doesn't exist
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
//
CORINFO_InstructionSet Compiler::lookupIsa(const char* className,
                                           const char* innerEnclosingClassName,
                                           const char* outerEnclosingClassName)
{
    assert(className != nullptr);

    if (innerEnclosingClassName == nullptr)
    {
        return lookupInstructionSet(className);
    }

    return InstructionSet_ILLEGAL;
}

GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types simdBaseType)
{
    NYI_WASM_SIMD("impNonConstFallback");
    return nullptr;
}

GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO* sig R2RARG(CORINFO_CONST_LOOKUP* entryPoint),
                                       var_types             simdBaseType,
                                       var_types             retType,
                                       unsigned              simdSize,
                                       bool                  mustExpand)
{
    NYI_WASM_SIMD("impSpecialIntrinsic");
    return nullptr;
}
#endif