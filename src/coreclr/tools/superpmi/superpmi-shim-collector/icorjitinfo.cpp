// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "icorjitinfo.h"
#include "superpmi-shim-collector.h"
#include "icorjitcompiler.h"
#include "methodcontext.h"
#include "errorhandling.h"
#include "logging.h"

#define fatMC // this is nice to have on so ildump works...

// Stuff on ICorStaticInfo
/**********************************************************************************/
//
// ICorMethodInfo
//
/**********************************************************************************/

// Quick check whether the method is a jit intrinsic. Returns the same value as getMethodAttribs(ftn) &
// CORINFO_FLG_INTRINSIC, except faster.
bool interceptor_ICJI::isIntrinsic(CORINFO_METHOD_HANDLE ftn)
{
    mc->cr->AddCall("isIntrinsic");
    bool temp = original_ICorJitInfo->isIntrinsic(ftn);
    mc->recIsIntrinsic(ftn, temp);
    return temp;
}

bool interceptor_ICJI::notifyMethodInfoUsage(CORINFO_METHOD_HANDLE ftn)
{
    mc->cr->AddCall("notifyMethodInfoUsage");
    bool temp = original_ICorJitInfo->notifyMethodInfoUsage(ftn);
    mc->recNotifyMethodInfoUsage(ftn, temp);
    return temp;
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
uint32_t interceptor_ICJI::getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */)
{
    mc->cr->AddCall("getMethodAttribs");
    DWORD temp = original_ICorJitInfo->getMethodAttribs(ftn);
    mc->recGetMethodAttribs(ftn, temp);
    return temp;
}

// sets private JIT flags, which can be, retrieved using getAttrib.
void interceptor_ICJI::setMethodAttribs(CORINFO_METHOD_HANDLE     ftn, /* IN */
                                        CorInfoMethodRuntimeFlags attribs /* IN */)
{
    mc->cr->AddCall("setMethodAttribs");
    original_ICorJitInfo->setMethodAttribs(ftn, attribs);
    mc->cr->recSetMethodAttribs(ftn, attribs);
}

// Given a method descriptor ftnHnd, extract signature information into sigInfo
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
void interceptor_ICJI::getMethodSig(CORINFO_METHOD_HANDLE ftn,         /* IN  */
                                    CORINFO_SIG_INFO*     sig,         /* OUT */
                                    CORINFO_CLASS_HANDLE  memberParent /* IN */
                                    )
{
    mc->cr->AddCall("getMethodSig");
    original_ICorJitInfo->getMethodSig(ftn, sig, memberParent);
    mc->recGetMethodSig(ftn, sig, memberParent);
}

/*********************************************************************
* Note the following methods can only be used on functions known
* to be IL.  This includes the method being compiled and any method
* that 'getMethodInfo' returns true for
*********************************************************************/
// return information about a method private to the implementation
//      returns false if method is not IL, or is otherwise unavailable.
//      This method is used to fetch data needed to inline functions
bool interceptor_ICJI::getMethodInfo(CORINFO_METHOD_HANDLE  ftn,    /* IN  */
                                     CORINFO_METHOD_INFO*   info,   /* OUT */
                                     CORINFO_CONTEXT_HANDLE context /* IN  */
                                     )
{
    bool temp  = false;

    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("getMethodInfo");
        temp = original_ICorJitInfo->getMethodInfo(ftn, info, context);
    },
    [&](DWORD exceptionCode)
    {
        this->mc->recGetMethodInfo(ftn, info, context, temp, exceptionCode);
    });

    return temp;
}

bool interceptor_ICJI::haveSameMethodDefinition(
    CORINFO_METHOD_HANDLE methHnd1,
    CORINFO_METHOD_HANDLE methHnd2)
{
    bool result = original_ICorJitInfo->haveSameMethodDefinition(methHnd1, methHnd2);
    mc->recHaveSameMethodDefinition(methHnd1, methHnd2, result);

    return result;
}

// Decides if you have any limitations for inlining. If everything's OK, it will return
// INLINE_PASS.
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
//
// The inlined method need not be verified

CorInfoInline interceptor_ICJI::canInline(CORINFO_METHOD_HANDLE callerHnd,    /* IN  */
                                          CORINFO_METHOD_HANDLE calleeHnd     /* IN  */
                                          )
{
    CorInfoInline temp          = INLINE_NEVER;

    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("canInline");
        temp = original_ICorJitInfo->canInline(callerHnd, calleeHnd);
    },
    [&](DWORD exceptionCode)
    {
        this->mc->recCanInline(callerHnd, calleeHnd, temp, exceptionCode);
    });

    return temp;
}

void interceptor_ICJI::beginInlining(CORINFO_METHOD_HANDLE inlinerHnd,
                                     CORINFO_METHOD_HANDLE inlineeHnd)
{
    mc->cr->AddCall("beginInlining");
    original_ICorJitInfo->beginInlining(inlinerHnd, inlineeHnd);
}

// Reports whether or not a method can be inlined, and why.  canInline is responsible for reporting all
// inlining results when it returns INLINE_FAIL and INLINE_NEVER.  All other results are reported by the
// JIT.
void interceptor_ICJI::reportInliningDecision(CORINFO_METHOD_HANDLE inlinerHnd,
                                              CORINFO_METHOD_HANDLE inlineeHnd,
                                              CorInfoInline         inlineResult,
                                              const char*           reason)
{
    mc->cr->AddCall("reportInliningDecision");
    original_ICorJitInfo->reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
    mc->cr->recReportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
}

// Returns false if the call is across security boundaries thus we cannot tailcall
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
bool interceptor_ICJI::canTailCall(CORINFO_METHOD_HANDLE callerHnd,         /* IN */
                                   CORINFO_METHOD_HANDLE declaredCalleeHnd, /* IN */
                                   CORINFO_METHOD_HANDLE exactCalleeHnd,    /* IN */
                                   bool                  fIsTailPrefix      /* IN */
                                   )
{
    mc->cr->AddCall("canTailCall");
    bool temp = original_ICorJitInfo->canTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
    mc->recCanTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix, temp);
    return temp;
}

// Reports whether or not a method can be tail called, and why.
// canTailCall is responsible for reporting all results when it returns
// false.  All other results are reported by the JIT.
void interceptor_ICJI::reportTailCallDecision(CORINFO_METHOD_HANDLE callerHnd,
                                              CORINFO_METHOD_HANDLE calleeHnd,
                                              bool                  fIsTailPrefix,
                                              CorInfoTailCall       tailCallResult,
                                              const char*           reason)
{
    mc->cr->AddCall("reportTailCallDecision");
    original_ICorJitInfo->reportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
    mc->cr->recReportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
}

// get individual exception handler
void interceptor_ICJI::getEHinfo(CORINFO_METHOD_HANDLE ftn,      /* IN  */
                                 unsigned              EHnumber, /* IN */
                                 CORINFO_EH_CLAUSE*    clause    /* OUT */
                                 )
{
    mc->cr->AddCall("getEHinfo");
    original_ICorJitInfo->getEHinfo(ftn, EHnumber, clause);
    mc->recGetEHinfo(ftn, EHnumber, clause);
}

// return class it belongs to
CORINFO_CLASS_HANDLE interceptor_ICJI::getMethodClass(CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("getMethodClass");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getMethodClass(method);
    mc->recGetMethodClass(method, temp);
    return temp;
}

// This function returns the offset of the specified method in the
// vtable of it's owning class or interface.
void interceptor_ICJI::getMethodVTableOffset(CORINFO_METHOD_HANDLE method,                 /* IN */
                                             unsigned*             offsetOfIndirection,    /* OUT */
                                             unsigned*             offsetAfterIndirection, /* OUT */
                                             bool*                 isRelative              /* OUT */
                                             )
{
    mc->cr->AddCall("getMethodVTableOffset");
    original_ICorJitInfo->getMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
    mc->recGetMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
}

bool interceptor_ICJI::resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO * info)
{
    mc->cr->AddCall("resolveVirtualMethod");
    bool result = original_ICorJitInfo->resolveVirtualMethod(info);
    mc->recResolveVirtualMethod(info, result);
    return result;
}

// Get the unboxed entry point for a method, if possible.
CORINFO_METHOD_HANDLE interceptor_ICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg)
{
    mc->cr->AddCall("getUnboxedEntry");
    bool                  localRequiresInstMethodTableArg = false;
    CORINFO_METHOD_HANDLE result = original_ICorJitInfo->getUnboxedEntry(ftn, &localRequiresInstMethodTableArg);
    mc->recGetUnboxedEntry(ftn, &localRequiresInstMethodTableArg, result);
    if (requiresInstMethodTableArg != nullptr)
    {
        *requiresInstMethodTableArg = localRequiresInstMethodTableArg;
    }
    return result;
}

// Given T, return the type of the default Comparer<T>.
// Returns null if the type can't be determined exactly.
CORINFO_CLASS_HANDLE interceptor_ICJI::getDefaultComparerClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getDefaultComparerClass");
    CORINFO_CLASS_HANDLE result = original_ICorJitInfo->getDefaultComparerClass(cls);
    mc->recGetDefaultComparerClass(cls, result);
    return result;
}

// Given T, return the type of the default EqualityComparer<T>.
// Returns null if the type can't be determined exactly.
CORINFO_CLASS_HANDLE interceptor_ICJI::getDefaultEqualityComparerClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getDefaultEqualityComparerClass");
    CORINFO_CLASS_HANDLE result = original_ICorJitInfo->getDefaultEqualityComparerClass(cls);
    mc->recGetDefaultEqualityComparerClass(cls, result);
    return result;
}

void interceptor_ICJI::expandRawHandleIntrinsic(CORINFO_RESOLVED_TOKEN*       pResolvedToken,
                                                CORINFO_GENERICHANDLE_RESULT* pResult)
{
    mc->cr->AddCall("expandRawHandleIntrinsic");
    original_ICorJitInfo->expandRawHandleIntrinsic(pResolvedToken, pResult);
    mc->recExpandRawHandleIntrinsic(pResolvedToken, pResult);
}

// Is the given type in System.Private.Corelib and marked with IntrinsicAttribute?
bool interceptor_ICJI::isIntrinsicType(CORINFO_CLASS_HANDLE classHnd)
{
    mc->cr->AddCall("isIntrinsicType");
    bool temp = original_ICorJitInfo->isIntrinsicType(classHnd);
    mc->recIsIntrinsicType(classHnd, temp);
    return temp;
}

// return the entry point calling convention for any of the following
// - a P/Invoke
// - a method marked with UnmanagedCallersOnly
// - a function pointer with the CORINFO_CALLCONV_UNMANAGED calling convention.
CorInfoCallConvExtension interceptor_ICJI::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig, bool* pSuppressGCTransition)
{
    mc->cr->AddCall("getUnmanagedCallConv");
    CorInfoCallConvExtension temp = original_ICorJitInfo->getUnmanagedCallConv(method, callSiteSig, pSuppressGCTransition);
    mc->recGetUnmanagedCallConv(method, callSiteSig, temp, *pSuppressGCTransition);
    return temp;
}

// return if any marshaling is required for PInvoke methods.  Note that
// method == 0 => calli.  The call site sig is only needed for the varargs or calli case
bool interceptor_ICJI::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    mc->cr->AddCall("pInvokeMarshalingRequired");
    bool temp = original_ICorJitInfo->pInvokeMarshalingRequired(method, callSiteSig);
    mc->recPInvokeMarshalingRequired(method, callSiteSig, temp);
    return temp;
}

// Check constraints on method type arguments (only).
bool interceptor_ICJI::satisfiesMethodConstraints(CORINFO_CLASS_HANDLE  parent, // the exact parent of the method
                                                  CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("satisfiesMethodConstraints");
    bool temp = original_ICorJitInfo->satisfiesMethodConstraints(parent, method);
    mc->recSatisfiesMethodConstraints(parent, method, temp);
    return temp;
}

// load and restore the method
void interceptor_ICJI::methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("methodMustBeLoadedBeforeCodeIsRun");
    original_ICorJitInfo->methodMustBeLoadedBeforeCodeIsRun(method);
    mc->cr->recMethodMustBeLoadedBeforeCodeIsRun(method);
}

CORINFO_METHOD_HANDLE interceptor_ICJI::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("mapMethodDeclToMethodImpl");
    return original_ICorJitInfo->mapMethodDeclToMethodImpl(method);
}

// Returns the global cookie for the /GS unsafe buffer checks
// The cookie might be a constant value (JIT), or a handle to memory location (Ngen)
void interceptor_ICJI::getGSCookie(GSCookie*  pCookieVal, // OUT
                                   GSCookie** ppCookieVal // OUT
                                   )
{
    mc->cr->AddCall("getGSCookie");
    original_ICorJitInfo->getGSCookie(pCookieVal, ppCookieVal);
    mc->recGetGSCookie(pCookieVal, ppCookieVal);
}

// Provide patchpoint info for the method currently being jitted.
void interceptor_ICJI::setPatchpointInfo(PatchpointInfo* patchpointInfo)
{
    mc->cr->AddCall("setPatchpointInfo");
    mc->cr->recSetPatchpointInfo(patchpointInfo); // Since the EE frees, we've gotta record before its sent to the EE.
    original_ICorJitInfo->setPatchpointInfo(patchpointInfo);
}

// Get OSR info for the method currently being jitted
PatchpointInfo* interceptor_ICJI::getOSRInfo(unsigned* ilOffset)
{
    mc->cr->AddCall("getOSRInfo");
    PatchpointInfo* patchpointInfo = original_ICorJitInfo->getOSRInfo(ilOffset);
    mc->recGetOSRInfo(patchpointInfo, ilOffset);
    return patchpointInfo;
}

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/
// Resolve metadata token into runtime method handles.
void interceptor_ICJI::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("resolveToken");
        original_ICorJitInfo->resolveToken(pResolvedToken);
    },
    [&](DWORD exceptionCode)
    {
        this->mc->recResolveToken(pResolvedToken, exceptionCode);
    });
}

// Signature information about the call sig
void interceptor_ICJI::findSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                               unsigned               sigTOK,  /* IN */
                               CORINFO_CONTEXT_HANDLE context, /* IN */
                               CORINFO_SIG_INFO*      sig      /* OUT */
                               )
{
    mc->cr->AddCall("findSig");
    original_ICorJitInfo->findSig(module, sigTOK, context, sig);
    mc->recFindSig(module, sigTOK, context, sig);
}

// for Varargs, the signature at the call site may differ from
// the signature at the definition.  Thus we need a way of
// fetching the call site information
void interceptor_ICJI::findCallSiteSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                                       unsigned               methTOK, /* IN */
                                       CORINFO_CONTEXT_HANDLE context, /* IN */
                                       CORINFO_SIG_INFO*      sig      /* OUT */
                                       )
{
    mc->cr->AddCall("findCallSiteSig");
    original_ICorJitInfo->findCallSiteSig(module, methTOK, context, sig);
    mc->recFindCallSiteSig(module, methTOK, context, sig);
}

CORINFO_CLASS_HANDLE interceptor_ICJI::getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken /* IN  */)
{
    mc->cr->AddCall("getTokenTypeAsHandle");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getTokenTypeAsHandle(pResolvedToken);
    mc->recGetTokenTypeAsHandle(pResolvedToken, temp);
    return temp;
}

int interceptor_ICJI::getStringLiteral(CORINFO_MODULE_HANDLE module,    /* IN  */
                                       unsigned              metaTOK,   /* IN  */
                                       char16_t*             buffer,    /* OUT */
                                       int                   bufferSize,/* IN  */
                                       int                   startIndex /* IN  */
                                       )
{
    mc->cr->AddCall("getStringLiteral");
    int temp = original_ICorJitInfo->getStringLiteral(module, metaTOK, buffer, bufferSize, startIndex);
    mc->recGetStringLiteral(module, metaTOK, buffer, bufferSize, startIndex, temp);
    return temp;
}

size_t interceptor_ICJI::printObjectDescription(CORINFO_OBJECT_HANDLE handle,             /* IN  */
                                                char*                 buffer,             /* OUT */
                                                size_t                bufferSize,         /* IN  */
                                                size_t*               pRequiredBufferSize /* OUT */
                                                )
{
    mc->cr->AddCall("printObjectDescription");
    size_t temp = original_ICorJitInfo->printObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize);
    mc->recPrintObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize, temp);
    return temp;
}

/**********************************************************************************/
//
// ICorClassInfo
//
/**********************************************************************************/

// If the value class 'cls' is isomorphic to a primitive type it will
// return that type, otherwise it will return CORINFO_TYPE_VALUECLASS
CorInfoType interceptor_ICJI::asCorInfoType(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("asCorInfoType");
    CorInfoType temp = original_ICorJitInfo->asCorInfoType(cls);
    mc->recAsCorInfoType(cls, temp);
    return temp;
}

const char* interceptor_ICJI::getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
{
    mc->cr->AddCall("getClassNameFromMetadata");
    const char* temp = original_ICorJitInfo->getClassNameFromMetadata(cls, namespaceName);
    mc->recGetClassNameFromMetadata(cls, (char*)temp, namespaceName);
    return temp;
}

CORINFO_CLASS_HANDLE interceptor_ICJI::getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
{
    mc->cr->AddCall("getTypeInstantiationArgument");
    CORINFO_CLASS_HANDLE result = original_ICorJitInfo->getTypeInstantiationArgument(cls, index);
    mc->recGetTypeInstantiationArgument(cls, index, result);
    return result;
}

size_t interceptor_ICJI::printClassName(CORINFO_CLASS_HANDLE cls, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    mc->cr->AddCall("printClassName");
    size_t bytesWritten = original_ICorJitInfo->printClassName(cls, buffer, bufferSize, pRequiredBufferSize);
    mc->recPrintClassName(cls, buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    return bytesWritten;
}

// Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) &
// CORINFO_FLG_VALUECLASS, except faster.
bool interceptor_ICJI::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isValueClass");
    bool temp = original_ICorJitInfo->isValueClass(cls);
    mc->recIsValueClass(cls, temp);
    return temp;
}

// Decides how the JIT should do the optimization to inline the check for
//     GetTypeFromHandle(handle) == obj.GetType() (for CORINFO_INLINE_TYPECHECK_SOURCE_VTABLE)
//     GetTypeFromHandle(X) == GetTypeFromHandle(Y) (for CORINFO_INLINE_TYPECHECK_SOURCE_TOKEN)
CorInfoInlineTypeCheck interceptor_ICJI::canInlineTypeCheck(CORINFO_CLASS_HANDLE         cls,
                                                            CorInfoInlineTypeCheckSource source)
{
    mc->cr->AddCall("canInlineTypeCheck");
    CorInfoInlineTypeCheck temp = original_ICorJitInfo->canInlineTypeCheck(cls, source);
    mc->recCanInlineTypeCheck(cls, source, temp);
    return temp;
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
uint32_t interceptor_ICJI::getClassAttribs(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassAttribs");
    DWORD temp = original_ICorJitInfo->getClassAttribs(cls);
    mc->recGetClassAttribs(cls, temp);
    return temp;
}

CORINFO_MODULE_HANDLE interceptor_ICJI::getClassModule(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassModule");
    CORINFO_MODULE_HANDLE temp = original_ICorJitInfo->getClassModule(cls);
    mc->recGetClassModule(cls, temp);
    return temp;
}

// Returns the assembly that contains the module "mod".
CORINFO_ASSEMBLY_HANDLE interceptor_ICJI::getModuleAssembly(CORINFO_MODULE_HANDLE mod)
{
    mc->cr->AddCall("getModuleAssembly");
    CORINFO_ASSEMBLY_HANDLE temp = original_ICorJitInfo->getModuleAssembly(mod);
    mc->recGetModuleAssembly(mod, temp);
    return temp;
}

// Returns the name of the assembly "assem".
const char* interceptor_ICJI::getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem)
{
    mc->cr->AddCall("getAssemblyName");
    const char* temp = original_ICorJitInfo->getAssemblyName(assem);
    mc->recGetAssemblyName(assem, temp);
    return temp;
}

// Allocate and delete process-lifetime objects.  Should only be
// referred to from static fields, lest a leak occur.
// Note that "LongLifetimeFree" does not execute destructors, if "obj"
// is an array of a struct type with a destructor.
void* interceptor_ICJI::LongLifetimeMalloc(size_t sz)
{
    mc->cr->AddCall("LongLifetimeMalloc");
    return original_ICorJitInfo->LongLifetimeMalloc(sz);
}

void interceptor_ICJI::LongLifetimeFree(void* obj)
{
    mc->cr->AddCall("LongLifetimeFree");
    original_ICorJitInfo->LongLifetimeFree(obj);
}

bool interceptor_ICJI::getIsClassInitedFlagAddress(CORINFO_CLASS_HANDLE  cls,
                                                   CORINFO_CONST_LOOKUP* addr,
                                                   int*                  offset)
{
    mc->cr->AddCall("getIsClassInitedFlagAddress");
    bool temp = original_ICorJitInfo->getIsClassInitedFlagAddress(cls, addr, offset);
    mc->recGetIsClassInitedFlagAddress(cls, addr, offset, temp);
    return temp;
}

bool interceptor_ICJI::getStaticBaseAddress(CORINFO_CLASS_HANDLE  cls,
                                            bool                  isGc,
                                            CORINFO_CONST_LOOKUP* addr)
{
    mc->cr->AddCall("getStaticBaseAddress");
    bool temp = original_ICorJitInfo->getStaticBaseAddress(cls, isGc, addr);
    mc->recGetStaticBaseAddress(cls, isGc, addr, temp);
    return temp;
}

// return the number of bytes needed by an instance of the class
unsigned interceptor_ICJI::getClassSize(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassSize");
    unsigned temp = original_ICorJitInfo->getClassSize(cls);
    mc->recGetClassSize(cls, temp);
    return temp;
}

// return the number of bytes needed by an instance of the class allocated on the heap
unsigned interceptor_ICJI::getHeapClassSize(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getHeapClassSize");
    unsigned temp = original_ICorJitInfo->getHeapClassSize(cls);
    mc->recGetHeapClassSize(cls, temp);
    return temp;
}

bool interceptor_ICJI::canAllocateOnStack(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("canAllocateOnStack");
    bool temp = original_ICorJitInfo->canAllocateOnStack(cls);
    mc->recCanAllocateOnStack(cls, temp);
    return temp;
}

unsigned interceptor_ICJI::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, bool fDoubleAlignHint)
{
    mc->cr->AddCall("getClassAlignmentRequirement");
    unsigned temp = original_ICorJitInfo->getClassAlignmentRequirement(cls, fDoubleAlignHint);
    mc->recGetClassAlignmentRequirement(cls, fDoubleAlignHint, temp);
    return temp;
}

// This is only called for Value classes.  It returns a boolean array
// in representing of 'cls' from a GC perspective.  The class is
// assumed to be an array of machine words
// (of length // getClassSize(cls) / sizeof(void*)),
// 'gcPtrs' is a pointer to an array of BYTEs of this length.
// getClassGClayout fills in this array so that gcPtrs[i] is set
// to one of the CorInfoGCType values which is the GC type of
// the i-th machine word of an object of type 'cls'
// returns the number of GC pointers in the array
unsigned interceptor_ICJI::getClassGClayout(CORINFO_CLASS_HANDLE cls,   /* IN */
                                            BYTE*                gcPtrs /* OUT */
                                            )
{
    mc->cr->AddCall("getClassGClayout");
    unsigned temp = original_ICorJitInfo->getClassGClayout(cls, gcPtrs);
    unsigned len  = (getClassSize(cls) + sizeof(void*) - 1) / sizeof(void*);
    mc->recGetClassGClayout(cls, gcPtrs, len, temp);
    return temp;
}

// returns the number of instance fields in a class
unsigned interceptor_ICJI::getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls /* IN */
                                                     )
{
    mc->cr->AddCall("getClassNumInstanceFields");
    unsigned temp = original_ICorJitInfo->getClassNumInstanceFields(cls);
    mc->recGetClassNumInstanceFields(cls, temp);
    return temp;
}

CORINFO_FIELD_HANDLE interceptor_ICJI::getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num)
{
    mc->cr->AddCall("getFieldInClass");
    CORINFO_FIELD_HANDLE temp = original_ICorJitInfo->getFieldInClass(clsHnd, num);
    mc->recGetFieldInClass(clsHnd, num, temp);
    return temp;
}

GetTypeLayoutResult interceptor_ICJI::getTypeLayout(
    CORINFO_CLASS_HANDLE typeHnd,
    CORINFO_TYPE_LAYOUT_NODE* nodes,
    size_t* numNodes)
{
    mc->cr->AddCall("getTypeLayout");
    GetTypeLayoutResult result = original_ICorJitInfo->getTypeLayout(typeHnd, nodes, numNodes);
    mc->recGetTypeLayout(result, typeHnd, nodes, *numNodes);
    return result;
}

bool interceptor_ICJI::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, bool fOptional)
{
    mc->cr->AddCall("checkMethodModifier");
    bool result = original_ICorJitInfo->checkMethodModifier(hMethod, modifier, fOptional);
    mc->recCheckMethodModifier(hMethod, modifier, fOptional, result);
    return result;
}

// returns the "NEW" helper optimized for "newCls."
CorInfoHelpFunc interceptor_ICJI::getNewHelper(CORINFO_CLASS_HANDLE  classHandle,
                                               bool* pHasSideEffects)
{
    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;
    bool hasSideEffects = false;

    RunWithErrorExceptionCodeCaptureAndContinue(
        [&]()
        {
            mc->cr->AddCall("getNewHelper");
            result = original_ICorJitInfo->getNewHelper(classHandle, &hasSideEffects);
        },
        [&](DWORD exceptionCode)
        {
            mc->recGetNewHelper(classHandle, hasSideEffects, result, exceptionCode);
        });

    if (pHasSideEffects != nullptr)
    {
        *pHasSideEffects = hasSideEffects;
    }

    return result;
}

// returns the newArr (1-Dim array) helper optimized for "arrayCls."
CorInfoHelpFunc interceptor_ICJI::getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls)
{
    mc->cr->AddCall("getNewArrHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getNewArrHelper(arrayCls);
    mc->recGetNewArrHelper(arrayCls, temp);
    return temp;
}

// returns the optimized "IsInstanceOf" or "ChkCast" helper
CorInfoHelpFunc interceptor_ICJI::getCastingHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fThrowing)
{
    mc->cr->AddCall("getCastingHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getCastingHelper(pResolvedToken, fThrowing);
    mc->recGetCastingHelper(pResolvedToken, fThrowing, temp);
    return temp;
}

// returns helper to trigger static constructor
CorInfoHelpFunc interceptor_ICJI::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    mc->cr->AddCall("getSharedCCtorHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getSharedCCtorHelper(clsHnd);
    mc->recGetSharedCCtorHelper(clsHnd, temp);
    return temp;
}

// This is not pretty.  Boxing nullable<T> actually returns
// a boxed<T> not a boxed Nullable<T>.  This call allows the verifier
// to call back to the EE on the 'box' instruction and get the transformed
// type to use for verification.
CORINFO_CLASS_HANDLE interceptor_ICJI::getTypeForBox(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getTypeForBox");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getTypeForBox(cls);
    mc->recGetTypeForBox(cls, temp);
    return temp;
}

// returns the correct box helper for a particular class.  Note
// that if this returns CORINFO_HELP_BOX, the JIT can assume
// 'standard' boxing (allocate object and copy), and optimize
CorInfoHelpFunc interceptor_ICJI::getBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getBoxHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getBoxHelper(cls);
    mc->recGetBoxHelper(cls, temp);
    return temp;
}

// returns the unbox helper.  If 'helperCopies' points to a true
// value it means the JIT is requesting a helper that unboxes the
// value into a particular location and thus has the signature
//     void unboxHelper(void* dest, CORINFO_CLASS_HANDLE cls, Object* obj)
// Otherwise (it is null or points at a FALSE value) it is requesting
// a helper that returns a pointer to the unboxed data
//     void* unboxHelper(CORINFO_CLASS_HANDLE cls, Object* obj)
// The EE has the option of NOT returning the copy style helper
// (But must be able to always honor the non-copy style helper)
// The EE set 'helperCopies' on return to indicate what kind of
// helper has been created.
CorInfoHelpFunc interceptor_ICJI::getUnBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getUnBoxHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getUnBoxHelper(cls);
    mc->recGetUnBoxHelper(cls, temp);
    return temp;
}

CORINFO_OBJECT_HANDLE interceptor_ICJI::getRuntimeTypePointer(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getRuntimeTypePointer");
    CORINFO_OBJECT_HANDLE temp = original_ICorJitInfo->getRuntimeTypePointer(cls);
    mc->recGetRuntimeTypePointer(cls, temp);
    return temp;
}

bool interceptor_ICJI::isObjectImmutable(CORINFO_OBJECT_HANDLE typeObj)
{
    mc->cr->AddCall("isObjectImmutable");
    bool temp = original_ICorJitInfo->isObjectImmutable(typeObj);
    mc->recIsObjectImmutable(typeObj, temp);
    return temp;
}

bool interceptor_ICJI::getStringChar(CORINFO_OBJECT_HANDLE strObj, int index, uint16_t* value)
{
    mc->cr->AddCall("getStringChar");
    bool temp = original_ICorJitInfo->getStringChar(strObj, index, value);
    mc->recGetStringChar(strObj, index, value, temp);
    return temp;
}

CORINFO_CLASS_HANDLE interceptor_ICJI::getObjectType(CORINFO_OBJECT_HANDLE typeObj)
{
    mc->cr->AddCall("getObjectType");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getObjectType(typeObj);
    mc->recGetObjectType(typeObj, temp);
    return temp;
}

bool interceptor_ICJI::getReadyToRunHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                           CORINFO_LOOKUP_KIND*    pGenericLookupKind,
                                           CorInfoHelpFunc         id,
                                           CORINFO_CONST_LOOKUP*   pLookup)
{
    mc->cr->AddCall("getReadyToRunHelper");
    bool result = original_ICorJitInfo->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, id, pLookup);
    mc->recGetReadyToRunHelper(pResolvedToken, pGenericLookupKind, id, pLookup, result);
    return result;
}

void interceptor_ICJI::getReadyToRunDelegateCtorHelper(CORINFO_RESOLVED_TOKEN* pTargetMethod,
                                                       mdToken                 targetConstraint,
                                                       CORINFO_CLASS_HANDLE    delegateType,
                                                       CORINFO_LOOKUP*         pLookup)
{
    mc->cr->AddCall("getReadyToRunDelegateCtorHelper");
    original_ICorJitInfo->getReadyToRunDelegateCtorHelper(pTargetMethod, targetConstraint, delegateType, pLookup);
    mc->recGetReadyToRunDelegateCtorHelper(pTargetMethod, targetConstraint, delegateType, pLookup);
}

// This function tries to initialize the class (run the class constructor).
// this function returns whether the JIT must insert helper calls before
// accessing static field or method.
//
// See code:ICorClassInfo#ClassConstruction.
CorInfoInitClassResult interceptor_ICJI::initClass(
    CORINFO_FIELD_HANDLE field,        // Non-nullptr - inquire about cctor trigger before static field access
                                       // nullptr - inquire about cctor trigger in method prolog
    CORINFO_METHOD_HANDLE  method,     // Method referencing the field or prolog
    CORINFO_CONTEXT_HANDLE context     // Exact context of method
    )
{
    mc->cr->AddCall("initClass");
    CorInfoInitClassResult temp = original_ICorJitInfo->initClass(field, method, context);
    mc->recInitClass(field, method, context, temp);
    return temp;
}

// This used to be called "loadClass".  This records the fact
// that the class must be loaded (including restored if necessary) before we execute the
// code that we are currently generating.  When jitting code
// the function loads the class immediately.  When zapping code
// the zapper will if necessary use the call to record the fact that we have
// to do a fixup/restore before running the method currently being generated.
//
// This is typically used to ensure value types are loaded before zapped
// code that manipulates them is executed, so that the GC can access information
// about those value types.
void interceptor_ICJI::classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("classMustBeLoadedBeforeCodeIsRun");
    original_ICorJitInfo->classMustBeLoadedBeforeCodeIsRun(cls);
    mc->cr->recClassMustBeLoadedBeforeCodeIsRun(cls);
}

// returns the class handle for the special builtin classes
CORINFO_CLASS_HANDLE interceptor_ICJI::getBuiltinClass(CorInfoClassId classId)
{
    mc->cr->AddCall("getBuiltinClass");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getBuiltinClass(classId);
    mc->recGetBuiltinClass(classId, temp);
    return temp;
}

// "System.Int32" ==> CORINFO_TYPE_INT..
CorInfoType interceptor_ICJI::getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getTypeForPrimitiveValueClass");
    CorInfoType temp = original_ICorJitInfo->getTypeForPrimitiveValueClass(cls);
    mc->recGetTypeForPrimitiveValueClass(cls, temp);
    return temp;
}

// "System.Int32" ==> CORINFO_TYPE_INT..
// "System.UInt32" ==> CORINFO_TYPE_UINT..
CorInfoType interceptor_ICJI::getTypeForPrimitiveNumericClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getTypeForPrimitiveNumericClass");
    CorInfoType temp = original_ICorJitInfo->getTypeForPrimitiveNumericClass(cls);
    mc->recGetTypeForPrimitiveNumericClass(cls, temp);
    return temp;
}

// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
bool interceptor_ICJI::canCast(CORINFO_CLASS_HANDLE child, // subtype (extends parent)
                               CORINFO_CLASS_HANDLE parent // base type
                               )
{
    mc->cr->AddCall("canCast");
    bool temp = original_ICorJitInfo->canCast(child, parent);
    mc->recCanCast(child, parent, temp);
    return temp;
}

// See if a cast from fromClass to toClass will succeed, fail, or needs
// to be resolved at runtime.
TypeCompareState interceptor_ICJI::compareTypesForCast(CORINFO_CLASS_HANDLE fromClass, CORINFO_CLASS_HANDLE toClass)
{
    mc->cr->AddCall("compareTypesForCast");
    TypeCompareState temp = original_ICorJitInfo->compareTypesForCast(fromClass, toClass);
    mc->recCompareTypesForCast(fromClass, toClass, temp);
    return temp;
}

// See if types represented by cls1 and cls2 compare equal, not
// equal, or the comparison needs to be resolved at runtime.
TypeCompareState interceptor_ICJI::compareTypesForEquality(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mc->cr->AddCall("compareTypesForEquality");
    TypeCompareState temp = original_ICorJitInfo->compareTypesForEquality(cls1, cls2);
    mc->recCompareTypesForEquality(cls1, cls2, temp);
    return temp;
}

// Returns true if cls2 is known to be a more specific type than cls1.
bool interceptor_ICJI::isMoreSpecificType(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mc->cr->AddCall("isMoreSpecificType");
    bool temp = original_ICorJitInfo->isMoreSpecificType(cls1, cls2);
    mc->recIsMoreSpecificType(cls1, cls2, temp);
    return temp;
}

// Returns TypeCompareState::Must if cls is known to be an enum.
// For enums with known exact type returns the underlying
// type in underlyingType when the provided pointer is
// non-NULL.
// Returns TypeCompareState::May when a runtime check is required.
TypeCompareState interceptor_ICJI::isEnum(CORINFO_CLASS_HANDLE cls, CORINFO_CLASS_HANDLE* underlyingType)
{
    mc->cr->AddCall("isEnum");
    CORINFO_CLASS_HANDLE tempUnderlyingType = nullptr;
    TypeCompareState temp = original_ICorJitInfo->isEnum(cls, &tempUnderlyingType);
    mc->recIsEnum(cls, tempUnderlyingType, temp);
    if (underlyingType != nullptr)
    {
        *underlyingType = tempUnderlyingType;
    }
    return temp;
}

// Given a class handle, returns the Parent type.
// For COMObjectType, it returns Class Handle of System.Object.
// Returns 0 if System.Object is passed in.
CORINFO_CLASS_HANDLE interceptor_ICJI::getParentType(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getParentType");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getParentType(cls);
    mc->recGetParentType(cls, temp);
    return temp;
}

// Returns the CorInfoType of the "child type". If the child type is
// not a primitive type, *clsRet will be set.
// Given an Array of Type Foo, returns Foo.
// Given BYREF Foo, returns Foo
CorInfoType interceptor_ICJI::getChildType(CORINFO_CLASS_HANDLE clsHnd, CORINFO_CLASS_HANDLE* clsRet)
{
    mc->cr->AddCall("getChildType");
    CorInfoType temp = original_ICorJitInfo->getChildType(clsHnd, clsRet);
    mc->recGetChildType(clsHnd, clsRet, temp);
    return temp;
}

// Check if this is a single dimensional array type
bool interceptor_ICJI::isSDArray(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isSDArray");
    bool temp = original_ICorJitInfo->isSDArray(cls);
    mc->recIsSDArray(cls, temp);
    return temp;
}

// Get the numbmer of dimensions in an array
unsigned interceptor_ICJI::getArrayRank(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getArrayRank");
    unsigned result = original_ICorJitInfo->getArrayRank(cls);
    mc->recGetArrayRank(cls, result);
    return result;
}

// Get the index of runtime provided array method
CorInfoArrayIntrinsic interceptor_ICJI::getArrayIntrinsicID(CORINFO_METHOD_HANDLE ftn)
{
    mc->cr->AddCall("getArrayIntrinsicID");
    CorInfoArrayIntrinsic result = original_ICorJitInfo->getArrayIntrinsicID(ftn);
    mc->recGetArrayIntrinsicID(ftn, result);
    return result;
}

// Get static field data for an array
void* interceptor_ICJI::getArrayInitializationData(CORINFO_FIELD_HANDLE field, uint32_t size)
{
    mc->cr->AddCall("getArrayInitializationData");
    void* temp = original_ICorJitInfo->getArrayInitializationData(field, size);
    mc->recGetArrayInitializationData(field, size, temp);
    return temp;
}

// Check Visibility rules.
CorInfoIsAccessAllowedResult interceptor_ICJI::canAccessClass(
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    CORINFO_METHOD_HANDLE   callerHandle,
    CORINFO_HELPER_DESC*    pAccessHelper /* If canAccessMethod returns something other
                                                than ALLOWED, then this is filled in. */
    )
{
    mc->cr->AddCall("canAccessClass");
    CorInfoIsAccessAllowedResult temp =
        original_ICorJitInfo->canAccessClass(pResolvedToken, callerHandle, pAccessHelper);
    mc->recCanAccessClass(pResolvedToken, callerHandle, pAccessHelper, temp);
    return temp;
}

/**********************************************************************************/
//
// ICorFieldInfo
//
/**********************************************************************************/

size_t interceptor_ICJI::printFieldName(CORINFO_FIELD_HANDLE ftn, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    mc->cr->AddCall("printFieldName");
    size_t temp = original_ICorJitInfo->printFieldName(ftn, buffer, bufferSize, pRequiredBufferSize);
    mc->recPrintFieldName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
    return temp;
}

// return class it belongs to
CORINFO_CLASS_HANDLE interceptor_ICJI::getFieldClass(CORINFO_FIELD_HANDLE field)
{
    mc->cr->AddCall("getFieldClass");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->getFieldClass(field);
    mc->recGetFieldClass(field, temp);
    return temp;
}

// Return the field's type, if it is CORINFO_TYPE_VALUECLASS 'structType' is set
// the field's value class (if 'structType' == 0, then don't bother
// the structure info).
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
CorInfoType interceptor_ICJI::getFieldType(CORINFO_FIELD_HANDLE  field,
                                           CORINFO_CLASS_HANDLE* structType,
                                           CORINFO_CLASS_HANDLE  memberParent /* IN */
                                           )
{
    mc->cr->AddCall("getFieldType");
    CorInfoType temp = original_ICorJitInfo->getFieldType(field, structType, memberParent);
    mc->recGetFieldType(field, structType, memberParent, temp);
    return temp;
}

// return the data member's instance offset
unsigned interceptor_ICJI::getFieldOffset(CORINFO_FIELD_HANDLE field)
{
    mc->cr->AddCall("getFieldOffset");
    unsigned temp = original_ICorJitInfo->getFieldOffset(field);
    mc->recGetFieldOffset(field, temp);
    return temp;
}

void interceptor_ICJI::getFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_METHOD_HANDLE   callerHandle,
                                    CORINFO_ACCESS_FLAGS    flags,
                                    CORINFO_FIELD_INFO*     pResult)
{
    mc->cr->AddCall("getFieldInfo");
    original_ICorJitInfo->getFieldInfo(pResolvedToken, callerHandle, flags, pResult);
    mc->recGetFieldInfo(pResolvedToken, callerHandle, flags, pResult);
}

uint32_t interceptor_ICJI::getThreadLocalFieldInfo(CORINFO_FIELD_HANDLE field, bool isGCType)
{
    mc->cr->AddCall("getThreadLocalFieldInfo");
    uint32_t result = original_ICorJitInfo->getThreadLocalFieldInfo(field, isGCType);
    mc->recGetThreadLocalFieldInfo(field, isGCType, result);
    return result;
}

void interceptor_ICJI::getThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
{
    mc->cr->AddCall("getThreadLocalStaticBlocksInfo");
    original_ICorJitInfo->getThreadLocalStaticBlocksInfo(pInfo);
    mc->recGetThreadLocalStaticBlocksInfo(pInfo);
}

// Returns true iff "fldHnd" represents a static field.
bool interceptor_ICJI::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    mc->cr->AddCall("isFieldStatic");
    bool result = original_ICorJitInfo->isFieldStatic(fldHnd);
    mc->recIsFieldStatic(fldHnd, result);
    return result;
}

int interceptor_ICJI::getArrayOrStringLength(CORINFO_OBJECT_HANDLE objHnd)
{
    mc->cr->AddCall("getArrayOrStringLength");
    int result = original_ICorJitInfo->getArrayOrStringLength(objHnd);
    mc->recGetArrayOrStringLength(objHnd, result);
    return result;
}

/*********************************************************************************/
//
// ICorDebugInfo
//
/*********************************************************************************/
// Query the EE to find out where interesting break points
// in the code are.  The native compiler will ensure that these places
// have a corresponding break point in native code.
//
// Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
// be used only as a hint and the native compiler should not change its
// code generation.
void interceptor_ICJI::getBoundaries(CORINFO_METHOD_HANDLE ftn,        // [IN] method of interest
                                     unsigned int*         cILOffsets, // [OUT] size of pILOffsets
                                     uint32_t**            pILOffsets, // [OUT] IL offsets of interest
                                                                       //       jit MUST free with freeArray!
                                     ICorDebugInfo::BoundaryTypes* implicitBoundaries // [OUT] tell jit, all boundaries of
                                                                                     // this type
                                     )
{
    mc->cr->AddCall("getBoundaries");
    original_ICorJitInfo->getBoundaries(ftn, cILOffsets, pILOffsets, implicitBoundaries);
    mc->recGetBoundaries(ftn, cILOffsets, pILOffsets, implicitBoundaries);
}

// Report back the mapping from IL to native code,
// this map should include all boundaries that 'getBoundaries'
// reported as interesting to the debugger.

// Note that debugger (and profiler) is assuming that all of the
// offsets form a contiguous block of memory, and that the
// OffsetMapping is sorted in order of increasing native offset.
// Note - Ownership of pMap is transferred with this call.  We need to record it before its passed on to the EE.
void interceptor_ICJI::setBoundaries(CORINFO_METHOD_HANDLE         ftn,  // [IN] method of interest
                                     ULONG32                       cMap, // [IN] size of pMap
                                     ICorDebugInfo::OffsetMapping* pMap  // [IN] map including all points of interest.
                                                                         //      jit allocated with allocateArray, EE
                                                                         //      frees
                                     )
{
    mc->cr->AddCall("setBoundaries");
    mc->cr->recSetBoundaries(ftn, cMap, pMap); // Since the EE frees, we've gotta record before its sent to the EE.
    original_ICorJitInfo->setBoundaries(ftn, cMap, pMap);
}

// Query the EE to find out the scope of local variables.
// normally the JIT would trash variables after last use, but
// under debugging, the JIT needs to keep them live over their
// entire scope so that they can be inspected.
//
// Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
// be used only as a hint and the native compiler should not change its
// code generation.
void interceptor_ICJI::getVars(CORINFO_METHOD_HANDLE      ftn,   // [IN]  method of interest
                               ULONG32*                   cVars, // [OUT] size of 'vars'
                               ICorDebugInfo::ILVarInfo** vars,  // [OUT] scopes of variables of interest
                                                                 //       jit MUST free with freeArray!
                               bool* extendOthers                // [OUT] it TRUE, then assume the scope
                                                                 //       of unmentioned vars is entire method
                               )
{
    mc->cr->AddCall("getVars");
    original_ICorJitInfo->getVars(ftn, cVars, vars, extendOthers);
    mc->recGetVars(ftn, cVars, vars, extendOthers);
}

// Report back to the EE the location of every variable.
// note that the JIT might split lifetimes into different
// locations etc.
// Note - Ownership of vars is transferred with this call.  We need to record it before its passed on to the EE.
void interceptor_ICJI::setVars(CORINFO_METHOD_HANDLE         ftn,   // [IN] method of interest
                               ULONG32                       cVars, // [IN] size of 'vars'
                               ICorDebugInfo::NativeVarInfo* vars   // [IN] map telling where local vars are stored at
                                                                    // what points
                                                                    //      jit allocated with allocateArray, EE frees
                               )
{
    mc->cr->AddCall("setVars");
    mc->cr->recSetVars(ftn, cVars, vars); // Since the EE frees, we've gotta record before its sent to the EE.
    original_ICorJitInfo->setVars(ftn, cVars, vars);
}

void interceptor_ICJI::reportRichMappings(ICorDebugInfo::InlineTreeNode*    inlineTreeNodes,
                                          uint32_t                          numInlineTreeNodes,
                                          ICorDebugInfo::RichOffsetMapping* mappings,
                                          uint32_t                          numMappings)
{
    mc->cr->AddCall("reportRichMappings");
    // TODO: record these mappings
    original_ICorJitInfo->reportRichMappings(inlineTreeNodes, numInlineTreeNodes, mappings, numMappings);
}

/*-------------------------- Misc ---------------------------------------*/
// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* interceptor_ICJI::allocateArray(size_t cBytes)
{
    mc->cr->AddCall("allocateArray");
    return original_ICorJitInfo->allocateArray(cBytes);
}

// JitCompiler will free arrays passed by the EE using this
// For eg, The EE returns memory in getVars() and getBoundaries()
// to the JitCompiler, which the JitCompiler should release using
// freeArray()
void interceptor_ICJI::freeArray(void* array)
{
    mc->cr->AddCall("freeArray");
    original_ICorJitInfo->freeArray(array);
}

/*********************************************************************************/
//
// ICorArgInfo
//
/*********************************************************************************/
// advance the pointer to the argument list.
// a ptr of 0, is special and always means the first argument
CORINFO_ARG_LIST_HANDLE interceptor_ICJI::getArgNext(CORINFO_ARG_LIST_HANDLE args /* IN */
                                                     )
{
    mc->cr->AddCall("getArgNext");
    CORINFO_ARG_LIST_HANDLE temp = original_ICorJitInfo->getArgNext(args);
    mc->recGetArgNext(args, temp);
    return temp;
}

// Get the type of a particular argument
// CORINFO_TYPE_UNDEF is returned when there are no more arguments
// If the type returned is a primitive type (or an enum) *vcTypeRet set to nullptr
// otherwise it is set to the TypeHandle associted with the type
// Enumerations will always look their underlying type (probably should fix this)
// Otherwise vcTypeRet is the type as would be seen by the IL,
// The return value is the type that is used for calling convention purposes
// (Thus if the EE wants a value class to be passed like an int, then it will
// return CORINFO_TYPE_INT
CorInfoTypeWithMod interceptor_ICJI::getArgType(CORINFO_SIG_INFO*       sig,      /* IN */
                                                CORINFO_ARG_LIST_HANDLE args,     /* IN */
                                                CORINFO_CLASS_HANDLE*   vcTypeRet /* OUT */
                                                )
{
    CorInfoTypeWithMod      temp      = (CorInfoTypeWithMod)CORINFO_TYPE_UNDEF;

    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("getArgType");
        temp =
            original_ICorJitInfo->getArgType(sig, args, vcTypeRet);

#ifdef fatMC
        CORINFO_CLASS_HANDLE temp3 = getArgClass(sig, args);
#endif
    },
    [&](DWORD exceptionCode)
    {
        this->mc->recGetArgType(sig, args, vcTypeRet, temp, exceptionCode);
    });

    return temp;
}

int interceptor_ICJI::getExactClasses(CORINFO_CLASS_HANDLE  baseType,        /* IN */
                                      int                   maxExactClasses, /* IN */
                                      CORINFO_CLASS_HANDLE* exactClsRet)     /* OUT */
{
    mc->cr->AddCall("getExactClasses");
    int result = original_ICorJitInfo->getExactClasses(baseType, maxExactClasses, exactClsRet);
    this->mc->recGetExactClasses(baseType, maxExactClasses, exactClsRet, result);
    return result;
}

// If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
CORINFO_CLASS_HANDLE interceptor_ICJI::getArgClass(CORINFO_SIG_INFO*       sig, /* IN */
                                                   CORINFO_ARG_LIST_HANDLE args /* IN */
                                                   )
{
    CORINFO_CLASS_HANDLE    temp = 0;

    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("getArgClass");
        temp = original_ICorJitInfo->getArgClass(sig, args);
    },
    [&](DWORD exceptionCode)
    {
        this->mc->recGetArgClass(sig, args, temp, exceptionCode);
    });

    return temp;
}

// Returns type of HFA for valuetype
CorInfoHFAElemType interceptor_ICJI::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    mc->cr->AddCall("getHFAType");
    CorInfoHFAElemType temp = original_ICorJitInfo->getHFAType(hClass);
    this->mc->recGetHFAType(hClass, temp);
    return temp;
}

/*****************************************************************************
 * ICorStaticInfo contains EE interface methods which return values that are
 * constant from invocation to invocation.  Thus they may be embedded in
 * persisted information like statically generated code. (This is of course
 * assuming that all code versions are identical each time.)
 *****************************************************************************/
// Return details about EE internal data structures
void interceptor_ICJI::getEEInfo(CORINFO_EE_INFO* pEEInfoOut)
{
    mc->cr->AddCall("getEEInfo");
    original_ICorJitInfo->getEEInfo(pEEInfoOut);
    mc->recGetEEInfo(pEEInfoOut);
}

// Returns name of the JIT timer log
const char16_t* interceptor_ICJI::getJitTimeLogFilename()
{
    mc->cr->AddCall("getJitTimeLogFilename");
    const char16_t* temp = original_ICorJitInfo->getJitTimeLogFilename();
    mc->recGetJitTimeLogFilename((LPCWSTR)temp);
    return temp;
}

/*********************************************************************************/
//
// Diagnostic methods
//
/*********************************************************************************/
// this function is for debugging only. Returns method token.
// Returns mdMethodDefNil for dynamic methods.
mdMethodDef interceptor_ICJI::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
{
    mc->cr->AddCall("getMethodDefFromMethod");
    mdMethodDef result = original_ICorJitInfo->getMethodDefFromMethod(hMethod);
    mc->recGetMethodDefFromMethod(hMethod, result);
    return result;
}

size_t interceptor_ICJI::printMethodName(CORINFO_METHOD_HANDLE ftn, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    mc->cr->AddCall("printMethodName");
    size_t temp = original_ICorJitInfo->printMethodName(ftn, buffer, bufferSize, pRequiredBufferSize);
    mc->recPrintMethodName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
    return temp;
}

const char* interceptor_ICJI::getMethodNameFromMetadata(CORINFO_METHOD_HANDLE ftn,                  /* IN */
                                                        const char**          className,            /* OUT */
                                                        const char**          namespaceName,        /* OUT */
                                                        const char**          enclosingClassName   /* OUT */
                                                        )
{
    mc->cr->AddCall("getMethodNameFromMetadata");
    const char* temp = original_ICorJitInfo->getMethodNameFromMetadata(ftn, className, namespaceName, enclosingClassName);
    mc->recGetMethodNameFromMetadata(ftn, (char*)temp, className, namespaceName, enclosingClassName);
    return temp;
}

// this function is for debugging only.  It returns a value that
// is will always be the same for a given method.  It is used
// to implement the 'jitRange' functionality
unsigned interceptor_ICJI::getMethodHash(CORINFO_METHOD_HANDLE ftn /* IN */
                                         )
{
    mc->cr->AddCall("getMethodHash");
    unsigned temp = original_ICorJitInfo->getMethodHash(ftn);
    mc->recGetMethodHash(ftn, temp);
    return temp;
}

bool interceptor_ICJI::getSystemVAmd64PassStructInRegisterDescriptor(
    /* IN */ CORINFO_CLASS_HANDLE                                  structHnd,
    /* OUT */ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    mc->cr->AddCall("getSystemVAmd64PassStructInRegisterDescriptor");
    bool result =
        original_ICorJitInfo->getSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
    mc->recGetSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr, result);
    return result;
}

uint32_t interceptor_ICJI::getLoongArch64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE structHnd)
{
    mc->cr->AddCall("getLoongArch64PassStructInRegisterFlags");
    uint32_t temp = original_ICorJitInfo->getLoongArch64PassStructInRegisterFlags(structHnd);
    mc->recGetLoongArch64PassStructInRegisterFlags(structHnd, temp);
    return temp;
}

uint32_t interceptor_ICJI::getRISCV64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE structHnd)
{
    mc->cr->AddCall("getRISCV64PassStructInRegisterFlags");
    uint32_t temp = original_ICorJitInfo->getRISCV64PassStructInRegisterFlags(structHnd);
    mc->recGetRISCV64PassStructInRegisterFlags(structHnd, temp);
    return temp;
}

// Stuff on ICorDynamicInfo
uint32_t interceptor_ICJI::getThreadTLSIndex(void** ppIndirection)
{
    mc->cr->AddCall("getThreadTLSIndex");
    uint32_t temp = original_ICorJitInfo->getThreadTLSIndex(ppIndirection);
    mc->recGetThreadTLSIndex(ppIndirection, temp);
    return temp;
}

int32_t* interceptor_ICJI::getAddrOfCaptureThreadGlobal(void** ppIndirection)
{
    mc->cr->AddCall("getAddrOfCaptureThreadGlobal");
    int32_t* temp = original_ICorJitInfo->getAddrOfCaptureThreadGlobal(ppIndirection);
    mc->recGetAddrOfCaptureThreadGlobal(ppIndirection, temp);
    return temp;
}

// return the native entry point to an EE helper (see CorInfoHelpFunc)
void* interceptor_ICJI::getHelperFtn(CorInfoHelpFunc ftnNum, void** ppIndirection)
{
    mc->cr->AddCall("getHelperFtn");
    void* temp = original_ICorJitInfo->getHelperFtn(ftnNum, ppIndirection);
    mc->recGetHelperFtn(ftnNum, ppIndirection, temp);
    return temp;
}

// return a callable address of the function (native code). This function
// may return a different value (depending on whether the method has
// been JITed or not.
void interceptor_ICJI::getFunctionEntryPoint(CORINFO_METHOD_HANDLE ftn,     /* IN  */
                                             CORINFO_CONST_LOOKUP* pResult, /* OUT */
                                             CORINFO_ACCESS_FLAGS  accessFlags)
{
    mc->cr->AddCall("getFunctionEntryPoint");
    original_ICorJitInfo->getFunctionEntryPoint(ftn, pResult, accessFlags);
    mc->recGetFunctionEntryPoint(ftn, pResult, accessFlags);
}

// return a directly callable address. This can be used similarly to the
// value returned by getFunctionEntryPoint() except that it is
// guaranteed to be multi callable entrypoint.
void interceptor_ICJI::getFunctionFixedEntryPoint(
            CORINFO_METHOD_HANDLE ftn,
            bool isUnsafeFunctionPointer,
            CORINFO_CONST_LOOKUP* pResult)
{
    mc->cr->AddCall("getFunctionFixedEntryPoint");
    original_ICorJitInfo->getFunctionFixedEntryPoint(ftn, isUnsafeFunctionPointer, pResult);
    mc->recGetFunctionFixedEntryPoint(ftn, pResult);
}

// get the synchronization handle that is passed to monXstatic function
void* interceptor_ICJI::getMethodSync(CORINFO_METHOD_HANDLE ftn, void** ppIndirection)
{
    mc->cr->AddCall("getMethodSync");
    void* temp = original_ICorJitInfo->getMethodSync(ftn, ppIndirection);
    mc->recGetMethodSync(ftn, ppIndirection, temp);
    return temp;
}

// These entry points must be called if a handle is being embedded in
// the code to be passed to a JIT helper function. (as opposed to just
// being passed back into the ICorInfo interface.)

// get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
// Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
CorInfoHelpFunc interceptor_ICJI::getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle)
{
    mc->cr->AddCall("getLazyStringLiteralHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getLazyStringLiteralHelper(handle);
    mc->recGetLazyStringLiteralHelper(handle, temp);
    return temp;
}

CORINFO_MODULE_HANDLE interceptor_ICJI::embedModuleHandle(CORINFO_MODULE_HANDLE handle, void** ppIndirection)
{
    mc->cr->AddCall("embedModuleHandle");
    CORINFO_MODULE_HANDLE temp = original_ICorJitInfo->embedModuleHandle(handle, ppIndirection);
    mc->recEmbedModuleHandle(handle, ppIndirection, temp);
    return temp;
}

CORINFO_CLASS_HANDLE interceptor_ICJI::embedClassHandle(CORINFO_CLASS_HANDLE handle, void** ppIndirection)
{
    mc->cr->AddCall("embedClassHandle");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->embedClassHandle(handle, ppIndirection);
    mc->recEmbedClassHandle(handle, ppIndirection, temp);
    return temp;
}

CORINFO_METHOD_HANDLE interceptor_ICJI::embedMethodHandle(CORINFO_METHOD_HANDLE handle, void** ppIndirection)
{
    mc->cr->AddCall("embedMethodHandle");
    CORINFO_METHOD_HANDLE temp = original_ICorJitInfo->embedMethodHandle(handle, ppIndirection);
    mc->recEmbedMethodHandle(handle, ppIndirection, temp);
    return temp;
}

CORINFO_FIELD_HANDLE interceptor_ICJI::embedFieldHandle(CORINFO_FIELD_HANDLE handle, void** ppIndirection)
{
    mc->cr->AddCall("embedFieldHandle");
    CORINFO_FIELD_HANDLE temp = original_ICorJitInfo->embedFieldHandle(handle, ppIndirection);
    mc->recEmbedFieldHandle(handle, ppIndirection, temp);
    return temp;
}

// Given a module scope (module), a method handle (context) and
// a metadata token (metaTOK), fetch the handle
// (type, field or method) associated with the token.
// If this is not possible at compile-time (because the current method's
// code is shared and the token contains generic parameters)
// then indicate how the handle should be looked up at run-time.
//
void interceptor_ICJI::embedGenericHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                          bool fEmbedParent, // TRUE - embeds parent type handle of the field/method
                                                             // handle
                                          CORINFO_GENERICHANDLE_RESULT* pResult)
{
    mc->cr->AddCall("embedGenericHandle");
    original_ICorJitInfo->embedGenericHandle(pResolvedToken, fEmbedParent, pResult);
    mc->recEmbedGenericHandle(pResolvedToken, fEmbedParent, pResult);
}

// Return information used to locate the exact enclosing type of the current method.
// Used only to invoke .cctor method from code shared across generic instantiations
//   !needsRuntimeLookup       statically known (enclosing type of method itself)
//   needsRuntimeLookup:
//      CORINFO_LOOKUP_THISOBJ     use vtable pointer of 'this' param
//      CORINFO_LOOKUP_CLASSPARAM  use vtable hidden param
//      CORINFO_LOOKUP_METHODPARAM use enclosing type of method-desc hidden param
void interceptor_ICJI::getLocationOfThisType(CORINFO_METHOD_HANDLE context, CORINFO_LOOKUP_KIND* pLookupKind)
{
    mc->cr->AddCall("getLocationOfThisType");
    original_ICorJitInfo->getLocationOfThisType(context, pLookupKind);
    mc->recGetLocationOfThisType(context, pLookupKind);
}

// return address of fixup area for late-bound PInvoke calls.
void interceptor_ICJI::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP* pLookup)
{
    mc->cr->AddCall("getAddressOfPInvokeTarget");
    original_ICorJitInfo->getAddressOfPInvokeTarget(method, pLookup);
    mc->recGetAddressOfPInvokeTarget(method, pLookup);
}

// Generate a cookie based on the signature that would needs to be passed
// to CORINFO_HELP_PINVOKE_CALLI
LPVOID interceptor_ICJI::GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig, void** ppIndirection)
{
    mc->cr->AddCall("GetCookieForPInvokeCalliSig");
    LPVOID temp = original_ICorJitInfo->GetCookieForPInvokeCalliSig(szMetaSig, ppIndirection);
    mc->recGetCookieForPInvokeCalliSig(szMetaSig, ppIndirection, temp);
    return temp;
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool interceptor_ICJI::canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
{
    mc->cr->AddCall("canGetCookieForPInvokeCalliSig");
    bool temp = original_ICorJitInfo->canGetCookieForPInvokeCalliSig(szMetaSig);
    mc->recCanGetCookieForPInvokeCalliSig(szMetaSig, temp);
    return temp;
}

// Gets a handle that is checked to see if the current method is
// included in "JustMyCode"
CORINFO_JUST_MY_CODE_HANDLE interceptor_ICJI::getJustMyCodeHandle(CORINFO_METHOD_HANDLE         method,
                                                                  CORINFO_JUST_MY_CODE_HANDLE** ppIndirection)
{
    mc->cr->AddCall("getJustMyCodeHandle");
    CORINFO_JUST_MY_CODE_HANDLE temp = original_ICorJitInfo->getJustMyCodeHandle(method, ppIndirection);
    mc->recGetJustMyCodeHandle(method, ppIndirection, temp);
    return temp;
}

// Gets a method handle that can be used to correlate profiling data.
// This is the IP of a native method, or the address of the descriptor struct
// for IL.  Always guaranteed to be unique per process, and not to move. */
void interceptor_ICJI::GetProfilingHandle(bool* pbHookFunction, void** pProfilerHandle, bool* pbIndirectedHandles)
{
    mc->cr->AddCall("GetProfilingHandle");
    original_ICorJitInfo->GetProfilingHandle(pbHookFunction, pProfilerHandle, pbIndirectedHandles);
    mc->recGetProfilingHandle(pbHookFunction, pProfilerHandle, pbIndirectedHandles);
}

// Returns instructions on how to make the call. See code:CORINFO_CALL_INFO for possible return values.
void interceptor_ICJI::getCallInfo(
    // Token info
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    // Generics info
    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
    // Security info
    CORINFO_METHOD_HANDLE callerHandle,
    // Jit info
    CORINFO_CALLINFO_FLAGS flags,
    // out params
    CORINFO_CALL_INFO* pResult)
{
    RunWithErrorExceptionCodeCaptureAndContinue(
    [&]()
    {
        mc->cr->AddCall("getCallInfo");
        original_ICorJitInfo->getCallInfo(pResolvedToken, pConstrainedResolvedToken,
                                                         callerHandle, flags, pResult);
    },
    [&](DWORD exceptionCode)
    {
        mc->recGetCallInfo(pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult,
                           exceptionCode);
    });
}

bool interceptor_ICJI::getStaticFieldContent(CORINFO_FIELD_HANDLE field, uint8_t* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
{
    mc->cr->AddCall("getStaticFieldContent");
    bool result = original_ICorJitInfo->getStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects);
    mc->recGetStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects, result);
    return result;
}

bool interceptor_ICJI::getObjectContent(CORINFO_OBJECT_HANDLE obj, uint8_t* buffer, int bufferSize, int valueOffset)
{
    mc->cr->AddCall("getObjectContent");
    bool result = original_ICorJitInfo->getObjectContent(obj, buffer, bufferSize, valueOffset);
    mc->recGetObjectContent(obj, buffer, bufferSize, valueOffset, result);
    return result;
}

// return the class handle for the current value of a static field
CORINFO_CLASS_HANDLE interceptor_ICJI::getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative)
{
    mc->cr->AddCall("getStaticFieldCurrentClass");
    CORINFO_CLASS_HANDLE result = original_ICorJitInfo->getStaticFieldCurrentClass(field, pIsSpeculative);
    mc->recGetStaticFieldCurrentClass(field, pIsSpeculative, result);
    return result;
}

// registers a vararg sig & returns a VM cookie for it (which can contain other stuff)
CORINFO_VARARGS_HANDLE interceptor_ICJI::getVarArgsHandle(CORINFO_SIG_INFO* pSig, void** ppIndirection)
{
    mc->cr->AddCall("getVarArgsHandle");
    CORINFO_VARARGS_HANDLE temp = original_ICorJitInfo->getVarArgsHandle(pSig, ppIndirection);
    mc->recGetVarArgsHandle(pSig, ppIndirection, temp);
    return temp;
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool interceptor_ICJI::canGetVarArgsHandle(CORINFO_SIG_INFO* pSig)
{
    mc->cr->AddCall("canGetVarArgsHandle");
    bool temp = original_ICorJitInfo->canGetVarArgsHandle(pSig);
    mc->recCanGetVarArgsHandle(pSig, temp);
    return temp;
}

// Allocate a string literal on the heap and return a handle to it
InfoAccessType interceptor_ICJI::constructStringLiteral(CORINFO_MODULE_HANDLE module, mdToken metaTok, void** ppValue)
{
    mc->cr->AddCall("constructStringLiteral");
    InfoAccessType temp = original_ICorJitInfo->constructStringLiteral(module, metaTok, ppValue);
    mc->recConstructStringLiteral(module, metaTok, *ppValue, temp);
    return temp;
}

bool interceptor_ICJI::convertPInvokeCalliToCall(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fMustConvert)
{
    mc->cr->AddCall("convertPInvokeCalliToCall");
    bool result = original_ICorJitInfo->convertPInvokeCalliToCall(pResolvedToken, fMustConvert);
    mc->recConvertPInvokeCalliToCall(pResolvedToken, fMustConvert, result);
    return result;
}

InfoAccessType interceptor_ICJI::emptyStringLiteral(void** ppValue)
{
    mc->cr->AddCall("emptyStringLiteral");
    InfoAccessType temp = original_ICorJitInfo->emptyStringLiteral(ppValue);
    mc->recEmptyStringLiteral(ppValue, temp);
    return temp;
}

// (static fields only) given that 'field' refers to thread local store,
// return the ID (TLS index), which is used to find the beginning of the
// TLS data area for the particular DLL 'field' is associated with.
uint32_t interceptor_ICJI::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    mc->cr->AddCall("getFieldThreadLocalStoreID");
    uint32_t temp = original_ICorJitInfo->getFieldThreadLocalStoreID(field, ppIndirection);
    mc->recGetFieldThreadLocalStoreID(field, ppIndirection, temp);
    return temp;
}

CORINFO_METHOD_HANDLE interceptor_ICJI::GetDelegateCtor(CORINFO_METHOD_HANDLE methHnd,
                                                        CORINFO_CLASS_HANDLE  clsHnd,
                                                        CORINFO_METHOD_HANDLE targetMethodHnd,
                                                        DelegateCtorArgs*     pCtorData)
{
    mc->cr->AddCall("GetDelegateCtor");
    CORINFO_METHOD_HANDLE temp = original_ICorJitInfo->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData);
    mc->recGetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData, temp);
    return temp;
}

void interceptor_ICJI::MethodCompileComplete(CORINFO_METHOD_HANDLE methHnd)
{
    mc->cr->AddCall("MethodCompileComplete");
    original_ICorJitInfo->MethodCompileComplete(methHnd);
}

bool interceptor_ICJI::getTailCallHelpers(
        CORINFO_RESOLVED_TOKEN* callToken,
        CORINFO_SIG_INFO* sig,
        CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
        CORINFO_TAILCALL_HELPERS* pResult)
{
    mc->cr->AddCall("getTailCallHelpers");
    bool result = original_ICorJitInfo->getTailCallHelpers(callToken, sig, flags, pResult);
    if (result)
        mc->recGetTailCallHelpers(callToken, sig, flags, pResult);
    else
        mc->recGetTailCallHelpers(callToken, sig, flags, nullptr);
    return result;
}

void interceptor_ICJI::updateEntryPointForTailCall(CORINFO_CONST_LOOKUP* entryPoint)
{
    mc->cr->AddCall("updateEntryPointForTailCall");
    CORINFO_CONST_LOOKUP origEntryPoint = *entryPoint;
    original_ICorJitInfo->updateEntryPointForTailCall(entryPoint);
    mc->recUpdateEntryPointForTailCall(origEntryPoint, *entryPoint);
}

// Stuff directly on ICorJitInfo

// Returns extended flags for a particular compilation instance.
uint32_t interceptor_ICJI::getJitFlags(CORJIT_FLAGS* jitFlags, uint32_t sizeInBytes)
{
    mc->cr->AddCall("getJitFlags");
    uint32_t result = original_ICorJitInfo->getJitFlags(jitFlags, sizeInBytes);
    mc->recGetJitFlags(jitFlags, sizeInBytes, result);
    return result;
}

// Runs the given function with the given parameter under an error trap
// and returns true if the function completes successfully. We don't
// record the results of the call: when this call gets played back,
// its result will depend on whether or not `function` calls something
// that throws at playback time rather than at capture time.
bool interceptor_ICJI::runWithErrorTrap(void (*function)(void*), void* param)
{
    mc->cr->AddCall("runWithErrorTrap");
    return original_ICorJitInfo->runWithErrorTrap(function, param);
}

// Runs the given function with the given parameter under an error trap
// and returns true if the function completes successfully. We don't
// record the results of the call: when this call gets played back,
// its result will depend on whether or not `function` calls something
// that throws at playback time rather than at capture time.
bool interceptor_ICJI::runWithSPMIErrorTrap(void (*function)(void*), void* param)
{
    return RunWithSPMIErrorTrap(function, param);
}

// get a block of memory for the code, readonly data, and read-write data
void interceptor_ICJI::allocMem(AllocMemArgs *pArgs)
{
    mc->cr->AddCall("allocMem");
    original_ICorJitInfo->allocMem(pArgs);
    mc->cr->recAllocMem(pArgs->hotCodeSize, pArgs->coldCodeSize, pArgs->roDataSize, pArgs->xcptnsCount, pArgs->flag, &pArgs->hotCodeBlock, &pArgs->coldCodeBlock,
                        &pArgs->roDataBlock);
}

// Reserve memory for the method/funclet's unwind information.
// Note that this must be called before allocMem. It should be
// called once for the main method, once for every funclet, and
// once for every block of cold code for which allocUnwindInfo
// will be called.
//
// This is necessary because jitted code must allocate all the
// memory needed for the unwindInfo at the allocMem call.
// For prejitted code we split up the unwinding information into
// separate sections .rdata and .pdata.
//
void interceptor_ICJI::reserveUnwindInfo(bool     isFunclet,  /* IN */
                                         bool     isColdCode, /* IN */
                                         uint32_t unwindSize  /* IN */
                                         )
{
    mc->cr->AddCall("reserveUnwindInfo");
    original_ICorJitInfo->reserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    mc->cr->recReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
}

// Allocate and initialize the .rdata and .pdata for this method or
// funclet, and get the block of memory needed for the machine-specific
// unwind information (the info for crawling the stack frame).
// Note that allocMem must be called first.
//
// Parameters:
//
//    pHotCode        main method code buffer, always filled in
//    pColdCode       cold code buffer, only filled in if this is cold code,
//                      null otherwise
//    startOffset     start of code block, relative to appropriate code buffer
//                      (e.g. pColdCode if cold, pHotCode if hot).
//    endOffset       end of code block, relative to appropriate code buffer
//    unwindSize      size of unwind info pointed to by pUnwindBlock
//    pUnwindBlock    pointer to unwind info
//    funcKind        type of funclet (main method code, handler, filter)
//
void interceptor_ICJI::allocUnwindInfo(uint8_t*       pHotCode,     /* IN */
                                       uint8_t*       pColdCode,    /* IN */
                                       uint32_t       startOffset,  /* IN */
                                       uint32_t       endOffset,    /* IN */
                                       uint32_t       unwindSize,   /* IN */
                                       uint8_t*       pUnwindBlock, /* IN */
                                       CorJitFuncKind funcKind      /* IN */
                                       )
{
    mc->cr->AddCall("allocUnwindInfo");
    original_ICorJitInfo->allocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock,
                                          funcKind);
    mc->cr->recAllocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
}

// Get a block of memory needed for the code manager information,
// (the info for enumerating the GC pointers while crawling the
// stack frame).
// Note that allocMem must be called first
void* interceptor_ICJI::allocGCInfo(size_t size /* IN */)
{
    mc->cr->AddCall("allocGCInfo");
    void* temp = original_ICorJitInfo->allocGCInfo(size);
    mc->cr->recAllocGCInfo(size, temp);
    return temp;
}

// Indicate how many exception handler blocks are to be returned.
// This is guaranteed to be called before any 'setEHinfo' call.
// Note that allocMem must be called before this method can be called.
void interceptor_ICJI::setEHcount(unsigned cEH /* IN */)
{
    mc->cr->AddCall("setEHcount");
    original_ICorJitInfo->setEHcount(cEH);
    mc->cr->recSetEHcount(cEH);
}

// Set the values for one particular exception handler block.
// Handler regions should be lexically contiguous.
// This is because FinallyIsUnwinding() uses lexicality to
// determine if a "finally" clause is executing.
void interceptor_ICJI::setEHinfo(unsigned                 EHnumber, /* IN  */
                                 const CORINFO_EH_CLAUSE* clause    /* IN */
                                 )
{
    mc->cr->AddCall("setEHinfo");
    original_ICorJitInfo->setEHinfo(EHnumber, clause);
    mc->cr->recSetEHinfo(EHnumber, clause);
}

// Level 1 -> fatalError, Level 2 -> Error, Level 3 -> Warning
// Level 4 means happens 10 times in a run, level 5 means 100, level 6 means 1000 ...
// returns non-zero if the logging succeeded
bool interceptor_ICJI::logMsg(unsigned level, const char* fmt, va_list args)
{
    mc->cr->AddCall("logMsg");
    return original_ICorJitInfo->logMsg(level, fmt, args);
}

// do an assert.  will return true if the code should retry (DebugBreak)
// returns false, if the assert should be ignored.
int interceptor_ICJI::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    mc->cr->AddCall("doAssert");

    m_compiler->finalizeAndCommitCollection(mc, CORJIT_INTERNALERROR, nullptr, 0);
    // The following assert may not always fail fast, so make sure we do not
    // save the collection twice if it throws an unwindable exception.
    m_savedCollectionEarly = true;

    return original_ICorJitInfo->doAssert(szFile, iLine, szExpr);
}

void interceptor_ICJI::reportFatalError(CorJitResult result)
{
    mc->cr->AddCall("reportFatalError");
    original_ICorJitInfo->reportFatalError(result);
    mc->cr->recReportFatalError(result);
}

// allocate a basic block profile buffer where execution counts will be stored
// for jitted basic blocks.
HRESULT interceptor_ICJI::allocPgoInstrumentationBySchema(CORINFO_METHOD_HANDLE ftnHnd,
                                                          PgoInstrumentationSchema* pSchema,
                                                          uint32_t countSchemaItems,
                                                          uint8_t** pInstrumentationData)
{
    mc->cr->AddCall("allocPgoInstrumentationBySchema");
    HRESULT result = original_ICorJitInfo->allocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData);
    mc->recAllocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData, result);
    return result;
}

// get profile information to be used for optimizing the current method.  The format
// of the buffer is the same as the format the JIT passes to allocMethodBlockCounts.
HRESULT interceptor_ICJI::getPgoInstrumentationResults(CORINFO_METHOD_HANDLE      ftnHnd,
                                                       PgoInstrumentationSchema **pSchema,                    // pointer to the schema table which describes the instrumentation results (pointer will not remain valid after jit completes)
                                                       uint32_t *                 pCountSchemaItems,          // pointer to the count schema items
                                                       uint8_t **                 pInstrumentationData,       // pointer to the actual instrumentation data (pointer will not remain valid after jit completes)
                                                       PgoSource*                 pPgoSource)
{
    mc->cr->AddCall("getPgoInstrumentationResults");
    HRESULT temp = original_ICorJitInfo->getPgoInstrumentationResults(ftnHnd, pSchema, pCountSchemaItems, pInstrumentationData, pPgoSource);
    mc->recGetPgoInstrumentationResults(ftnHnd, pSchema, pCountSchemaItems, pInstrumentationData, pPgoSource, temp);
    return temp;
}

// Associates a native call site, identified by its offset in the native code stream, with
// the signature information and method handle the JIT used to lay out the call site. If
// the call site has no signature information (e.g. a helper call) or has no method handle
// (e.g. a CALLI P/Invoke), then null should be passed instead.
void interceptor_ICJI::recordCallSite(uint32_t              instrOffset, /* IN */
                                      CORINFO_SIG_INFO*     callSig,     /* IN */
                                      CORINFO_METHOD_HANDLE methodHandle /* IN */
                                      )
{
    mc->cr->AddCall("recordCallSite");
    original_ICorJitInfo->recordCallSite(instrOffset, callSig, methodHandle);
    mc->cr->recRecordCallSite(instrOffset, callSig, methodHandle);
}

// A relocation is recorded if we are pre-jitting.
// A jump thunk may be inserted if we are jitting
void interceptor_ICJI::recordRelocation(void*    location,   /* IN  */
                                        void*    locationRW, /* IN  */
                                        void*    target,     /* IN  */
                                        uint16_t fRelocType, /* IN  */
                                        int32_t  addlDelta   /* IN  */
                                        )
{
    mc->cr->AddCall("recordRelocation");
    original_ICorJitInfo->recordRelocation(location, locationRW, target, fRelocType, addlDelta);
    mc->cr->recRecordRelocation(location, target, fRelocType, addlDelta);
}

uint16_t interceptor_ICJI::getRelocTypeHint(void* target)
{
    mc->cr->AddCall("getRelocTypeHint");
    WORD result = original_ICorJitInfo->getRelocTypeHint(target);
    mc->recGetRelocTypeHint(target, result);
    return result;
}

// For what machine does the VM expect the JIT to generate code? The VM
// returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
// is cross-compiling (such as the case for crossgen2), it will return a
// different value than if it was compiling for the host architecture.
//
uint32_t interceptor_ICJI::getExpectedTargetArchitecture()
{
    mc->cr->AddCall("getExpectedTargetArchitecture");
    DWORD result = original_ICorJitInfo->getExpectedTargetArchitecture();
    mc->recGetExpectedTargetArchitecture(result);
    return result;
}

bool interceptor_ICJI::notifyInstructionSetUsage(CORINFO_InstructionSet instructionSet, bool supported)
{
    return original_ICorJitInfo->notifyInstructionSetUsage(instructionSet, supported);
}
