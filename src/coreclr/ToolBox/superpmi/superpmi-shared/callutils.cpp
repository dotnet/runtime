// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// CallUtils.cpp - Utility code for analyzing and working with managed calls
//----------------------------------------------------------

#include "standardpch.h"
#include "callutils.h"
#include "typeutils.h"
#include "errorhandling.h"
#include "logging.h"
#include "spmiutil.h"

// String representations of the JIT helper functions
const char* kHelperName[CORINFO_HELP_COUNT] = {
#define JITHELPER(code, pfnHelper, sig) #code,
#define DYNAMICJITHELPER(code, pfnHelper, sig) #code,
#include "jithelpers.h"
};

//-------------------------------------------------------------------------------------------------

//
// Provides information about the target of an outgoing call, based on where it was emitted in the
// generated code stream.
//
// Primarily, this returns what the destination of the call is (e.g. a method, a helper function), but this
// can also provide:
//
//    - A symbolic name for the call target (i.e. the function name).
//    - For non-helper methods, the method signature of the call target.
//
// Arguments:
//    mc                  - The method context of the method containing this call site.
//    cr                  - The compile result for the method containing this call site.
//    callInstrOffset     - The native offset of the call site in the generated code stream.
//    outSigInfo          - [out] The signature of the outgoing call. Optional (pass nullptr if unwanted).
//    outCallTargetSymbol - [out] A string representation of the outgoing call. Optional (pass nullptr if
//                          unwanted).
//
// Return Value:
//    What type of call the outgoing call is.
//
// Notes:
//    - This depends on the JIT having registered the call site with the EE through recordCallSite. If the
//      JIT didn't do this, GetDirectCallSite can obtain most of the same information for direct calls.
//    - If the call site is for a helper method, then outSigInfo will not be changed, since helper calls
//      have no signature information.
//    - If you pass in a valid pointer for outCallTargetSymbol, this function will allocate memory for it
//      if it is able to understand that call (i.e. if it does not return CallType_Unknown). You, the caller,
//      are responsible for freeing the memory (with delete[]).
//
CallType CallUtils::GetRecordedCallSiteInfo(MethodContext*            mc,
                                            CompileResult*            cr,
                                            unsigned int              callInstrOffset,
                                            /*out*/ CORINFO_SIG_INFO* outSigInfo,
                                            /*out*/ char**            outCallTargetSymbol)
{
    AssertCodeMsg(mc != nullptr, EXCEPTIONCODE_CALLUTILS,
                  "Null method context passed into GetCallTargetInfo for call at offset %x.", callInstrOffset);
    AssertCodeMsg(cr != nullptr, EXCEPTIONCODE_CALLUTILS,
                  "Null compile result passed into GetCallTargetInfo for call at offset %x.", callInstrOffset);

    CallType targetType = CallType_Unknown;

    CORINFO_SIG_INFO callSig;
    bool             recordedCallSig = cr->fndRecordCallSiteSigInfo(callInstrOffset, &callSig);

    CORINFO_METHOD_HANDLE methodHandle         = nullptr;
    bool                  recordedMethodHandle = cr->fndRecordCallSiteMethodHandle(callInstrOffset, &methodHandle);

    if (recordedCallSig)
    {
        if (outSigInfo != nullptr)
            *outSigInfo = callSig;

        if (outCallTargetSymbol != nullptr)
            *outCallTargetSymbol = (char*)GetMethodFullName(mc, methodHandle, callSig);

        targetType = CallType_UserFunction;
    }
    else if (recordedMethodHandle)
    {
        CorInfoHelpFunc helperNum = CallUtils::GetHelperNum(methodHandle);
        AssertCodeMsg(helperNum != CORINFO_HELP_UNDEF, EXCEPTIONCODE_CALLUTILS,
                      "Unknown call at offset %x with method handle %016llX.", callInstrOffset, methodHandle);

        size_t length        = strlen(kHelperName[helperNum]) + 1;
        *outCallTargetSymbol = new char[length];
        strcpy_s(*outCallTargetSymbol, length, kHelperName[helperNum]);

        targetType = CallType_Helper;
    }
    else
    {
        LogWarning("Call site at offset %x was not recorded via recordCallSite.", callInstrOffset);
    }

    return targetType;
}

//
// Provides information about the target of an outgoing call, based on the outgoing call's target address.
//
// Primarily, this returns what the destination of the call is (e.g. a method, a helper function), but this
// can also provide:
//
//    - A symbolic name for the call target (i.e. the function name).
//    - For certain types of managed methods, the method signature of the call target.
//
// Arguments:
//    mc                  - The method context of the method containing this outgoing call.
//    callTarget          - The target address of the outgoing call.
//    outSigInfo          - [out] The signature of the outgoing call. Optional (pass nullptr if unwanted).
//    outCallTargetSymbol - [out] A string representation of the outgoing call. Optional (pass nullptr if
//                          unwanted).
//
// Return Value:
//    What type of call the outgoing call is.
//
// Assumptions:
//    The given method address does not point to a jump stub.
//
// Notes:
//    - This only works for direct calls that have a static target address.
//    - If you pass in a valid pointer for outCallTargetSymbol, this function will allocate memory for it
//      if it is able to understand that call (i.e. if it does not return CallType_Unknown). You, the caller,
//      are responsible for freeing the memory (with delete[]).
//

CallType CallUtils::GetDirectCallSiteInfo(MethodContext*            mc,
                                          void*                     callTarget,
                                          /*out*/ CORINFO_SIG_INFO* outSigInfo,
                                          /*out*/ char**            outCallTargetSymbol)
{
    AssertCodeMsg(mc != nullptr, EXCEPTIONCODE_CALLUTILS,
                  "Null method context passed into GetCallTargetInfo for call to target %016llX.", callTarget);

    CallType              targetType = CallType_Unknown;
    DLD                   functionEntryPoint;
    CORINFO_METHOD_HANDLE methodHandle;

    // Try to first obtain a method handle associated with this call target
    functionEntryPoint.A = CastPointer(callTarget);
    functionEntryPoint.B = 0; // TODO-Cleanup: we should be more conscious of this...

    if (mc->fndGetFunctionEntryPoint(functionEntryPoint, &methodHandle))
    {
        // Now try to obtain the call info associated with this method handle

        struct Param
        {
            MethodContext*         mc;
            CORINFO_SIG_INFO*      outSigInfo;
            char**                 outCallTargetSymbol;
            CallType*              pTargetType;
            CORINFO_METHOD_HANDLE* pMethodHandle;
        } param;
        param.mc                  = mc;
        param.outSigInfo          = outSigInfo;
        param.outCallTargetSymbol = outCallTargetSymbol;
        param.pTargetType         = &targetType;
        param.pMethodHandle       = &methodHandle;

        PAL_TRY(Param*, pParam, &param)
        {
            CORINFO_CALL_INFO callInfo;

            pParam->mc->repGetCallInfoFromMethodHandle(*pParam->pMethodHandle, &callInfo);

            if (pParam->outSigInfo != nullptr)
                *pParam->outSigInfo = callInfo.sig;

            if (pParam->outCallTargetSymbol != nullptr)
                *pParam->outCallTargetSymbol =
                    (char*)GetMethodFullName(pParam->mc, *pParam->pMethodHandle, callInfo.sig);

            *pParam->pTargetType = CallType_UserFunction;
        }
        PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CatchMC)
        {
            LogWarning("Didn't find call info for method handle %016llX (call target: %016llX)", methodHandle,
                       callTarget);
        }
        PAL_ENDTRY
    }
    else
    {
        // No method handle associated with this target, so check if it's a helper
        CorInfoHelpFunc helperNum;

        if (mc->fndGetHelperFtn(callTarget, &helperNum))
        {
            if (outCallTargetSymbol != nullptr)
            {
                size_t length        = strlen(kHelperName[helperNum]) + 1;
                *outCallTargetSymbol = new char[length];
                strcpy_s(*outCallTargetSymbol, length, kHelperName[helperNum]);
            }

            targetType = CallType_Helper;
        }
        else
        {
            LogWarning("Call to target %016llX has no method handle and is not a helper call.", callTarget);
        }
    }

    return targetType;
}

//-------------------------------------------------------------------------------------------------
// Utilty code that was stolen from various sections of the JIT codebase and tweaked to go through
// SuperPMI's method context replaying instead of directly making calls into the JIT/EE interface.
//-------------------------------------------------------------------------------------------------

// Originally from src/jit/ee_il_dll.cpp
const char* CallUtils::GetMethodName(MethodContext* mc, CORINFO_METHOD_HANDLE method, const char** classNamePtr)
{
    if (GetHelperNum(method))
    {
        if (classNamePtr != nullptr)
            *classNamePtr = "HELPER";

        // The JIT version uses the getHelperName JIT/EE interface call, but this is easier for us
        return kHelperName[GetHelperNum(method)];
    }

    if (IsNativeMethod(method))
    {
        if (classNamePtr != nullptr)
            *classNamePtr = "NATIVE";
        method            = GetMethodHandleForNative(method);
    }

    return (mc->repGetMethodName(method, classNamePtr));
}

// Originally from src/jit/eeinterface.cpp
// If `ignoreMethodName` is `true`, we construct the function signature with a dummy method name that will be the
// same for all methods.
const char* CallUtils::GetMethodFullName(MethodContext* mc, CORINFO_METHOD_HANDLE hnd, CORINFO_SIG_INFO sig, bool ignoreMethodName /* = false */)
{
    const char* returnType = NULL;

    const char* className = ignoreMethodName ? "CLASS" : nullptr;
    const char* methodName = ignoreMethodName ? "METHOD" : GetMethodName(mc, hnd, &className);
    if ((GetHelperNum(hnd) != CORINFO_HELP_UNDEF) || IsNativeMethod(hnd))
    {
        return methodName;
    }

    size_t   length = 0;
    unsigned i;

    /* Generating the full signature is a two-pass process. First we have to walk
       the components in order to assess the total size, then we allocate the buffer
       and copy the elements into it.
     */

    /* Right now there is a race-condition in the EE, className can be NULL */

    /* initialize length with length of className and '.' */

    if (className != nullptr)
        length = strlen(className) + 1;
    else
    {
        // Tweaked to avoid using CRT assertions
        Assert(strlen("<NULL>.") == 7);
        length = 7;
    }

    /* add length of methodName and opening bracket */
    length += strlen(methodName) + 1;

    CORINFO_ARG_LIST_HANDLE argList = sig.args;

    for (i = 0; i < sig.numArgs; i++)
    {
        // Tweaked to use EE types instead of JIT-specific types
        CORINFO_CLASS_HANDLE typeHandle;
        DWORD                exception;
        CorInfoType          type = strip(mc->repGetArgType(&sig, argList, &typeHandle, &exception));

        length += strlen(TypeUtils::GetCorInfoTypeName(type));
        argList = mc->repGetArgNext(argList);
    }

    /* add ',' if there is more than one argument */

    if (sig.numArgs > 1)
        length += (sig.numArgs - 1);

    // Tweaked to use EE types instead of JIT-specific types
    if (sig.retType != CORINFO_TYPE_VOID)
    {
        returnType = TypeUtils::GetCorInfoTypeName(sig.retType);
        length += strlen(returnType) + 1; // don't forget the delimiter ':'
    }

    // Does it have a 'this' pointer? Don't count explicit this, which has the this pointer type as the first element of
    // the arg type list
    if (sig.hasThis() && !sig.hasExplicitThis())
    {
        // Tweaked to avoid using CRT assertions
        Assert(strlen(":this") == 5);
        length += 5;
    }

    /* add closing bracket and null terminator */

    length += 2;

    char* retName = new char[length]; // Tweaked to use "new" instead of compGetMem

    /* Now generate the full signature string in the allocated buffer */

    if (className)
    {
        strcpy_s(retName, length, className);
        strcat_s(retName, length, ":");
    }
    else
    {
        strcpy_s(retName, length, "<NULL>.");
    }

    strcat_s(retName, length, methodName);

    // append the signature
    strcat_s(retName, length, "(");

    argList = sig.args;

    for (i = 0; i < sig.numArgs; i++)
    {
        // Tweaked to use EE types instead of JIT-specific types
        CORINFO_CLASS_HANDLE typeHandle;
        DWORD                exception;
        CorInfoType          type = strip(mc->repGetArgType(&sig, argList, &typeHandle, &exception));
        strcat_s(retName, length, TypeUtils::GetCorInfoTypeName(type));

        argList = mc->repGetArgNext(argList);
        if (i + 1 < sig.numArgs)
            strcat_s(retName, length, ",");
    }

    strcat_s(retName, length, ")");

    if (returnType)
    {
        strcat_s(retName, length, ":");
        strcat_s(retName, length, returnType);
    }

    // Does it have a 'this' pointer? Don't count explicit this, which has the this pointer type as the first element of
    // the arg type list
    if (sig.hasThis() && !sig.hasExplicitThis())
    {
        strcat_s(retName, length, ":this");
    }

    // Tweaked to avoid using CRT assertions
    Assert(strlen(retName) == (length - 1));

    return (retName);
}

// Originally from jit/compiler.hpp
inline CorInfoHelpFunc CallUtils::GetHelperNum(CORINFO_METHOD_HANDLE method)
{
    // Helpers are marked by the fact that they are odd numbers
    if (!(((size_t)method) & 1))
        return (CORINFO_HELP_UNDEF);
    return ((CorInfoHelpFunc)(((size_t)method) >> 2));
}

// Originally from jit/compiler.hpp
inline bool CallUtils::IsNativeMethod(CORINFO_METHOD_HANDLE method)
{
    return ((((size_t)method) & 0x2) == 0x2);
}

// Originally from jit/compiler.hpp
inline CORINFO_METHOD_HANDLE CallUtils::GetMethodHandleForNative(CORINFO_METHOD_HANDLE method)
{
    // Tweaked to avoid using CRT assertions
    Assert((((size_t)method) & 0x3) == 0x2);
    return (CORINFO_METHOD_HANDLE)(((size_t)method) & ~0x3);
}
