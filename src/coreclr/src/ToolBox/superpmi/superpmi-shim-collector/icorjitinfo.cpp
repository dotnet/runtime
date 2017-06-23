//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "icorjitinfo.h"
#include "superpmi-shim-collector.h"
#include "ieememorymanager.h"
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
// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD interceptor_ICJI::getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */)
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
bool interceptor_ICJI::getMethodInfo(CORINFO_METHOD_HANDLE ftn, /* IN  */
                                     CORINFO_METHOD_INFO*  info /* OUT */
                                     )
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*     pThis;
        CORINFO_METHOD_HANDLE ftn;
        CORINFO_METHOD_INFO*  info;
        bool                  temp;
    } param;
    param.pThis = this;
    param.ftn   = ftn;
    param.info  = info;
    param.temp  = false;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("getMethodInfo");
    pParam->temp = pParam->pThis->original_ICorJitInfo->getMethodInfo(pParam->ftn, pParam->info);
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recGetMethodInfo(ftn, info, param.temp, param.exceptionCode);
}
PAL_ENDTRY

return param.temp;
}

// Decides if you have any limitations for inlining. If everything's OK, it will return
// INLINE_PASS and will fill out pRestrictions with a mask of restrictions the caller of this
// function must respect. If caller passes pRestrictions = nullptr, if there are any restrictions
// INLINE_FAIL will be returned
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
//
// The inlined method need not be verified

CorInfoInline interceptor_ICJI::canInline(CORINFO_METHOD_HANDLE callerHnd,    /* IN  */
                                          CORINFO_METHOD_HANDLE calleeHnd,    /* IN  */
                                          DWORD*                pRestrictions /* OUT */
                                          )
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*     pThis;
        CORINFO_METHOD_HANDLE callerHnd;
        CORINFO_METHOD_HANDLE calleeHnd;
        DWORD*                pRestrictions;
        CorInfoInline         temp;
    } param;
    param.pThis         = this;
    param.callerHnd     = callerHnd;
    param.calleeHnd     = calleeHnd;
    param.pRestrictions = pRestrictions;
    param.temp          = INLINE_NEVER;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("canInline");
    pParam->temp =
        pParam->pThis->original_ICorJitInfo->canInline(pParam->callerHnd, pParam->calleeHnd, pParam->pRestrictions);
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recCanInline(callerHnd, calleeHnd, pRestrictions, param.temp, param.exceptionCode);
}
PAL_ENDTRY

return param.temp;
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

// return module it belongs to
CORINFO_MODULE_HANDLE interceptor_ICJI::getMethodModule(CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("getMethodModule");
    return original_ICorJitInfo->getMethodModule(method);
}

// This function returns the offset of the specified method in the
// vtable of it's owning class or interface.
void interceptor_ICJI::getMethodVTableOffset(CORINFO_METHOD_HANDLE method,                /* IN */
                                             unsigned*             offsetOfIndirection,   /* OUT */
                                             unsigned*             offsetAfterIndirection,/* OUT */
                                             unsigned*             isRelative             /* OUT */
                                             )
{
    mc->cr->AddCall("getMethodVTableOffset");
    original_ICorJitInfo->getMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
    mc->recGetMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
}

// Find the virtual method in implementingClass that overrides virtualMethod.
// Return null if devirtualization is not possible.
CORINFO_METHOD_HANDLE interceptor_ICJI::resolveVirtualMethod(CORINFO_METHOD_HANDLE  virtualMethod,
                                                             CORINFO_CLASS_HANDLE   implementingClass,
                                                             CORINFO_CONTEXT_HANDLE ownerType)
{
    mc->cr->AddCall("resolveVirtualMethod");
    CORINFO_METHOD_HANDLE result =
        original_ICorJitInfo->resolveVirtualMethod(virtualMethod, implementingClass, ownerType);
    mc->recResolveVirtualMethod(virtualMethod, implementingClass, ownerType, result);
    return result;
}

void interceptor_ICJI::expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    mc->cr->AddCall("expandRawHandleIntrinsic");
    original_ICorJitInfo->expandRawHandleIntrinsic(pResolvedToken, pResult);
}

// If a method's attributes have (getMethodAttribs) CORINFO_FLG_INTRINSIC set,
// getIntrinsicID() returns the intrinsic ID.
CorInfoIntrinsics interceptor_ICJI::getIntrinsicID(CORINFO_METHOD_HANDLE method, bool* pMustExpand /* OUT */
                                                   )
{
    mc->cr->AddCall("getIntrinsicID");
    CorInfoIntrinsics temp = original_ICorJitInfo->getIntrinsicID(method, pMustExpand);
    mc->recGetIntrinsicID(method, pMustExpand, temp);
    return temp;
}

// Is the given module the System.Numerics.Vectors module?
bool interceptor_ICJI::isInSIMDModule(CORINFO_CLASS_HANDLE classHnd)
{
    mc->cr->AddCall("isInSIMDModule");
    bool temp = original_ICorJitInfo->isInSIMDModule(classHnd);
    mc->recIsInSIMDModule(classHnd, temp);
    return temp;
}

// return the unmanaged calling convention for a PInvoke
CorInfoUnmanagedCallConv interceptor_ICJI::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("getUnmanagedCallConv");
    CorInfoUnmanagedCallConv temp = original_ICorJitInfo->getUnmanagedCallConv(method);
    mc->recGetUnmanagedCallConv(method, temp);
    return temp;
}

// return if any marshaling is required for PInvoke methods.  Note that
// method == 0 => calli.  The call site sig is only needed for the varargs or calli case
BOOL interceptor_ICJI::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    mc->cr->AddCall("pInvokeMarshalingRequired");
    BOOL temp = original_ICorJitInfo->pInvokeMarshalingRequired(method, callSiteSig);
    mc->recPInvokeMarshalingRequired(method, callSiteSig, temp);
    return temp;
}

// Check constraints on method type arguments (only).
// The parent class should be checked separately using satisfiesClassConstraints(parent).
BOOL interceptor_ICJI::satisfiesMethodConstraints(CORINFO_CLASS_HANDLE  parent, // the exact parent of the method
                                                  CORINFO_METHOD_HANDLE method)
{
    mc->cr->AddCall("satisfiesMethodConstraints");
    BOOL temp = original_ICorJitInfo->satisfiesMethodConstraints(parent, method);
    mc->recSatisfiesMethodConstraints(parent, method, temp);
    return temp;
}

// Given a delegate target class, a target method parent class,  a  target method,
// a delegate class, check if the method signature is compatible with the Invoke method of the delegate
// (under the typical instantiation of any free type variables in the memberref signatures).
BOOL interceptor_ICJI::isCompatibleDelegate(
    CORINFO_CLASS_HANDLE  objCls,          /* type of the delegate target, if any */
    CORINFO_CLASS_HANDLE  methodParentCls, /* exact parent of the target method, if any */
    CORINFO_METHOD_HANDLE method,          /* (representative) target method, if any */
    CORINFO_CLASS_HANDLE  delegateCls,     /* exact type of the delegate */
    BOOL*                 pfIsOpenDelegate /* is the delegate open */
    )
{
    mc->cr->AddCall("isCompatibleDelegate");
    BOOL temp =
        original_ICorJitInfo->isCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate);
    mc->recIsCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate, temp);
    return temp;
}

// Indicates if the method is an instance of the generic
// method that passes (or has passed) verification
CorInfoInstantiationVerification interceptor_ICJI::isInstantiationOfVerifiedGeneric(CORINFO_METHOD_HANDLE method /* IN
                                                                                                                  */
                                                                                    )
{
    mc->cr->AddCall("isInstantiationOfVerifiedGeneric");
    CorInfoInstantiationVerification temp = original_ICorJitInfo->isInstantiationOfVerifiedGeneric(method);
    mc->recIsInstantiationOfVerifiedGeneric(method, temp);
    return temp;
}

// Loads the constraints on a typical method definition, detecting cycles;
// for use in verification.
void interceptor_ICJI::initConstraintsForVerification(CORINFO_METHOD_HANDLE method,                        /* IN */
                                                      BOOL*                 pfHasCircularClassConstraints, /* OUT */
                                                      BOOL*                 pfHasCircularMethodConstraint  /* OUT */
                                                      )
{
    mc->cr->AddCall("initConstraintsForVerification");
    original_ICorJitInfo->initConstraintsForVerification(method, pfHasCircularClassConstraints,
                                                         pfHasCircularMethodConstraint);
    mc->recInitConstraintsForVerification(method, pfHasCircularClassConstraints, pfHasCircularMethodConstraint);
}

// Returns enum whether the method does not require verification
// Also see ICorModuleInfo::canSkipVerification
CorInfoCanSkipVerificationResult interceptor_ICJI::canSkipMethodVerification(CORINFO_METHOD_HANDLE ftnHandle)
{
    mc->cr->AddCall("canSkipMethodVerification");
    CorInfoCanSkipVerificationResult temp = original_ICorJitInfo->canSkipMethodVerification(ftnHandle);
    mc->recCanSkipMethodVerification(ftnHandle, FALSE, temp);
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

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/
// Resolve metadata token into runtime method handles.
void interceptor_ICJI::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*       pThis;
        CORINFO_RESOLVED_TOKEN* pResolvedToken;
    } param;
    param.pThis          = this;
    param.pResolvedToken = pResolvedToken;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("resolveToken");
    pParam->pThis->original_ICorJitInfo->resolveToken(pParam->pResolvedToken);
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recResolveToken(param.pResolvedToken, param.exceptionCode);
}
PAL_ENDTRY
}

bool interceptor_ICJI::tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    mc->cr->AddCall("tryResolveToken");
    bool success = original_ICorJitInfo->tryResolveToken(pResolvedToken);
    mc->recResolveToken(pResolvedToken, success);
    return success;
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

// Returns true if the module does not require verification
//
// If fQuickCheckOnlyWithoutCommit=TRUE, the function only checks that the
// module does not currently require verification in the current AppDomain.
// This decision could change in the future, and so should not be cached.
// If it is cached, it should only be used as a hint.
// This is only used by ngen for calculating certain hints.
//
// Returns enum whether the module does not require verification
// Also see ICorMethodInfo::canSkipMethodVerification();
CorInfoCanSkipVerificationResult interceptor_ICJI::canSkipVerification(CORINFO_MODULE_HANDLE module /* IN  */
                                                                       )
{
    mc->cr->AddCall("canSkipVerification");
    return original_ICorJitInfo->canSkipVerification(module);
}

// Checks if the given metadata token is valid
BOOL interceptor_ICJI::isValidToken(CORINFO_MODULE_HANDLE module, /* IN  */
                                    unsigned              metaTOK /* IN  */
                                    )
{
    mc->cr->AddCall("isValidToken");
    BOOL result = original_ICorJitInfo->isValidToken(module, metaTOK);
    mc->recIsValidToken(module, metaTOK, result);
    return result;
}

// Checks if the given metadata token is valid StringRef
BOOL interceptor_ICJI::isValidStringRef(CORINFO_MODULE_HANDLE module, /* IN  */
                                        unsigned              metaTOK /* IN  */
                                        )
{
    mc->cr->AddCall("isValidStringRef");
    BOOL temp = original_ICorJitInfo->isValidStringRef(module, metaTOK);
    mc->recIsValidStringRef(module, metaTOK, temp);
    return temp;
}

BOOL interceptor_ICJI::shouldEnforceCallvirtRestriction(CORINFO_MODULE_HANDLE scope)
{
    mc->cr->AddCall("shouldEnforceCallvirtRestriction");
    BOOL temp = original_ICorJitInfo->shouldEnforceCallvirtRestriction(scope);
    mc->recShouldEnforceCallvirtRestriction(scope, temp);
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

// for completeness
const char* interceptor_ICJI::getClassName(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassName");
    const char* result = original_ICorJitInfo->getClassName(cls);
    mc->recGetClassName(cls, result);
    return result;
}

// Append a (possibly truncated) representation of the type cls to the preallocated buffer ppBuf of length pnBufLen
// If fNamespace=TRUE, include the namespace/enclosing classes
// If fFullInst=TRUE (regardless of fNamespace and fAssembly), include namespace and assembly for any type parameters
// If fAssembly=TRUE, suffix with a comma and the full assembly qualification
// return size of representation
int interceptor_ICJI::appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf,
                                      int*                                    pnBufLen,
                                      CORINFO_CLASS_HANDLE                    cls,
                                      BOOL                                    fNamespace,
                                      BOOL                                    fFullInst,
                                      BOOL                                    fAssembly)
{
    mc->cr->AddCall("appendClassName");
    WCHAR* pBuf = *ppBuf;
    int    nLen = original_ICorJitInfo->appendClassName(ppBuf, pnBufLen, cls, fNamespace, fFullInst, fAssembly);
    mc->recAppendClassName(cls, fNamespace, fFullInst, fAssembly, pBuf);
    return nLen;
}

// Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) &
// CORINFO_FLG_VALUECLASS, except faster.
BOOL interceptor_ICJI::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isValueClass");
    BOOL temp = original_ICorJitInfo->isValueClass(cls);
    mc->recIsValueClass(cls, temp);
    return temp;
}

// If this method returns true, JIT will do optimization to inline the check for
//     GetTypeFromHandle(handle) == obj.GetType()
BOOL interceptor_ICJI::canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("canInlineTypeCheckWithObjectVTable");
    BOOL temp = original_ICorJitInfo->canInlineTypeCheckWithObjectVTable(cls);
    mc->recCanInlineTypeCheckWithObjectVTable(cls, temp);
    return temp;
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD interceptor_ICJI::getClassAttribs(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassAttribs");
    DWORD temp = original_ICorJitInfo->getClassAttribs(cls);
    mc->recGetClassAttribs(cls, temp);
    return temp;
}

// Returns "TRUE" iff "cls" is a struct type such that return buffers used for returning a value
// of this type must be stack-allocated.  This will generally be true only if the struct
// contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
// an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
// returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
// buffers do not require GC write barriers.
BOOL interceptor_ICJI::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isStructRequiringStackAllocRetBuf");
    BOOL temp = original_ICorJitInfo->isStructRequiringStackAllocRetBuf(cls);
    mc->recIsStructRequiringStackAllocRetBuf(cls, temp);
    return temp;
}

CORINFO_MODULE_HANDLE interceptor_ICJI::getClassModule(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("getClassModule");
    return original_ICorJitInfo->getClassModule(cls);
}

// Returns the assembly that contains the module "mod".
CORINFO_ASSEMBLY_HANDLE interceptor_ICJI::getModuleAssembly(CORINFO_MODULE_HANDLE mod)
{
    mc->cr->AddCall("getModuleAssembly");
    return original_ICorJitInfo->getModuleAssembly(mod);
}

// Returns the name of the assembly "assem".
const char* interceptor_ICJI::getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem)
{
    mc->cr->AddCall("getAssemblyName");
    return original_ICorJitInfo->getAssemblyName(assem);
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

size_t interceptor_ICJI::getClassModuleIdForStatics(CORINFO_CLASS_HANDLE   cls,
                                                    CORINFO_MODULE_HANDLE* pModule,
                                                    void**                 ppIndirection)
{
    mc->cr->AddCall("getClassModuleIdForStatics");
    size_t temp = original_ICorJitInfo->getClassModuleIdForStatics(cls, pModule, ppIndirection);
    mc->recGetClassModuleIdForStatics(cls, pModule, ppIndirection, temp);
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

unsigned interceptor_ICJI::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint)
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

BOOL interceptor_ICJI::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional)
{
    mc->cr->AddCall("checkMethodModifier");
    BOOL result = original_ICorJitInfo->checkMethodModifier(hMethod, modifier, fOptional);
    mc->recCheckMethodModifier(hMethod, modifier, fOptional, result);
    return result;
}

// returns the "NEW" helper optimized for "newCls."
CorInfoHelpFunc interceptor_ICJI::getNewHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                               CORINFO_METHOD_HANDLE   callerHandle)
{
    mc->cr->AddCall("getNewHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getNewHelper(pResolvedToken, callerHandle);
    mc->recGetNewHelper(pResolvedToken, callerHandle, temp);
    return temp;
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

CorInfoHelpFunc interceptor_ICJI::getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn)
{
    mc->cr->AddCall("getSecurityPrologHelper");
    CorInfoHelpFunc temp = original_ICorJitInfo->getSecurityPrologHelper(ftn);
    mc->recGetSecurityPrologHelper(ftn, temp);
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
                                                       CORINFO_CLASS_HANDLE    delegateType,
                                                       CORINFO_LOOKUP*         pLookup)
{
    mc->cr->AddCall("getReadyToRunDelegateCtorHelper");
    original_ICorJitInfo->getReadyToRunDelegateCtorHelper(pTargetMethod, delegateType, pLookup);
    mc->recGetReadyToRunDelegateCtorHelper(pTargetMethod, delegateType, pLookup);
}

const char* interceptor_ICJI::getHelperName(CorInfoHelpFunc funcNum)
{
    mc->cr->AddCall("getHelperName");
    const char* temp = original_ICorJitInfo->getHelperName(funcNum);
    mc->recGetHelperName(funcNum, temp);
    return temp;
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
    CORINFO_CONTEXT_HANDLE context,    // Exact context of method
    BOOL                   speculative // TRUE means don't actually run it
    )
{
    mc->cr->AddCall("initClass");
    CorInfoInitClassResult temp = original_ICorJitInfo->initClass(field, method, context, speculative);
    mc->recInitClass(field, method, context, speculative, temp);
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

// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
BOOL interceptor_ICJI::canCast(CORINFO_CLASS_HANDLE child, // subtype (extends parent)
                               CORINFO_CLASS_HANDLE parent // base type
                               )
{
    mc->cr->AddCall("canCast");
    BOOL temp = original_ICorJitInfo->canCast(child, parent);
    mc->recCanCast(child, parent, temp);
    return temp;
}

// TRUE if cls1 and cls2 are considered equivalent types.
BOOL interceptor_ICJI::areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mc->cr->AddCall("areTypesEquivalent");
    BOOL temp = original_ICorJitInfo->areTypesEquivalent(cls1, cls2);
    mc->recAreTypesEquivalent(cls1, cls2, temp);
    return temp;
}

// returns is the intersection of cls1 and cls2.
CORINFO_CLASS_HANDLE interceptor_ICJI::mergeClasses(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mc->cr->AddCall("mergeClasses");
    CORINFO_CLASS_HANDLE temp = original_ICorJitInfo->mergeClasses(cls1, cls2);
    mc->recMergeClasses(cls1, cls2, temp);
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

// Check constraints on type arguments of this class and parent classes
BOOL interceptor_ICJI::satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("satisfiesClassConstraints");
    BOOL temp = original_ICorJitInfo->satisfiesClassConstraints(cls);
    mc->recSatisfiesClassConstraints(cls, temp);
    return temp;
}

// Check if this is a single dimensional array type
BOOL interceptor_ICJI::isSDArray(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isSDArray");
    BOOL temp = original_ICorJitInfo->isSDArray(cls);
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

// Get static field data for an array
void* interceptor_ICJI::getArrayInitializationData(CORINFO_FIELD_HANDLE field, DWORD size)
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
// this function is for debugging only.  It returns the field name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* interceptor_ICJI::getFieldName(CORINFO_FIELD_HANDLE ftn,       /* IN */
                                           const char**         moduleName /* OUT */
                                           )
{
    mc->cr->AddCall("getFieldName");
    const char* temp = original_ICorJitInfo->getFieldName(ftn, moduleName);
    mc->recGetFieldName(ftn, moduleName, temp);
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

// TODO: jit64 should be switched to the same plan as the i386 jits - use
// getClassGClayout to figure out the need for writebarrier helper, and inline the copying.
// The interpretted value class copy is slow. Once this happens, USE_WRITE_BARRIER_HELPERS
bool interceptor_ICJI::isWriteBarrierHelperRequired(CORINFO_FIELD_HANDLE field)
{
    mc->cr->AddCall("isWriteBarrierHelperRequired");
    bool result = original_ICorJitInfo->isWriteBarrierHelperRequired(field);
    mc->recIsWriteBarrierHelperRequired(field, result);
    return result;
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

// Returns true iff "fldHnd" represents a static field.
bool interceptor_ICJI::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    mc->cr->AddCall("isFieldStatic");
    bool result = original_ICorJitInfo->isFieldStatic(fldHnd);
    mc->recIsFieldStatic(fldHnd, result);
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
                                     DWORD**               pILOffsets, // [OUT] IL offsets of interest
                                                                       //       jit MUST free with freeArray!
                                     ICorDebugInfo::BoundaryTypes* implictBoundaries // [OUT] tell jit, all boundries of
                                                                                     // this type
                                     )
{
    mc->cr->AddCall("getBoundaries");
    original_ICorJitInfo->getBoundaries(ftn, cILOffsets, pILOffsets, implictBoundaries);
    mc->recGetBoundaries(ftn, cILOffsets, pILOffsets, implictBoundaries);
}

// Report back the mapping from IL to native code,
// this map should include all boundaries that 'getBoundaries'
// reported as interesting to the debugger.

// Note that debugger (and profiler) is assuming that all of the
// offsets form a contiguous block of memory, and that the
// OffsetMapping is sorted in order of increasing native offset.
 //Note - Ownership of pMap is transfered with this call.  We need to record it before its passed on to the EE.
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

// Query the EE to find out the scope of local varables.
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
 //Note - Ownership of vars is transfered with this call.  We need to record it before its passed on to the EE.
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

/*-------------------------- Misc ---------------------------------------*/
// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* interceptor_ICJI::allocateArray(ULONG cBytes)
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
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*       pThis;
        CORINFO_SIG_INFO*       sig;
        CORINFO_ARG_LIST_HANDLE args;
        CORINFO_CLASS_HANDLE*   vcTypeRet;
        CorInfoTypeWithMod      temp;
    } param;
    param.pThis     = this;
    param.sig       = sig;
    param.args      = args;
    param.vcTypeRet = vcTypeRet;
    param.temp      = (CorInfoTypeWithMod)CORINFO_TYPE_UNDEF;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("getArgType");
    pParam->temp = pParam->pThis->original_ICorJitInfo->getArgType(pParam->sig, pParam->args, pParam->vcTypeRet);

#ifdef fatMC
    CORINFO_CLASS_HANDLE temp3 = pParam->pThis->getArgClass(pParam->sig, pParam->args);
#endif
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recGetArgType(sig, args, vcTypeRet, param.temp, param.exceptionCode);
}
PAL_ENDTRY

return param.temp;
}

// If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
CORINFO_CLASS_HANDLE interceptor_ICJI::getArgClass(CORINFO_SIG_INFO*       sig, /* IN */
                                                   CORINFO_ARG_LIST_HANDLE args /* IN */
                                                   )
{
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*       pThis;
        CORINFO_SIG_INFO*       sig;
        CORINFO_ARG_LIST_HANDLE args;
        CORINFO_CLASS_HANDLE    temp;
    } param;
    param.pThis = this;
    param.sig   = sig;
    param.args  = args;
    param.temp  = 0;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("getArgClass");
    pParam->temp = pParam->pThis->original_ICorJitInfo->getArgClass(pParam->sig, pParam->args);
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recGetArgClass(sig, args, param.temp, param.exceptionCode);

    // to build up a fat mc
    getClassName(param.temp);
}
PAL_ENDTRY

return param.temp;
}

// Returns type of HFA for valuetype
CorInfoType interceptor_ICJI::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    mc->cr->AddCall("getHFAType");
    CorInfoType temp = original_ICorJitInfo->getHFAType(hClass);
    this->mc->recGetHFAType(hClass, temp);
    return temp;
}

/*****************************************************************************
* ICorErrorInfo contains methods to deal with SEH exceptions being thrown
* from the corinfo interface.  These methods may be called when an exception
* with code EXCEPTION_COMPLUS is caught.
*****************************************************************************/
// Returns the HRESULT of the current exception
HRESULT interceptor_ICJI::GetErrorHRESULT(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    mc->cr->AddCall("GetErrorHRESULT");
    return original_ICorJitInfo->GetErrorHRESULT(pExceptionPointers);
}

// Fetches the message of the current exception
// Returns the size of the message (including terminating null). This can be
// greater than bufferLength if the buffer is insufficient.
ULONG interceptor_ICJI::GetErrorMessage(__inout_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength)
{
    mc->cr->AddCall("GetErrorMessage");
    return original_ICorJitInfo->GetErrorMessage(buffer, bufferLength);
}

// returns EXCEPTION_EXECUTE_HANDLER if it is OK for the compile to handle the
//                        exception, abort some work (like the inlining) and continue compilation
// returns EXCEPTION_CONTINUE_SEARCH if exception must always be handled by the EE
//                    things like ThreadStoppedException ...
// returns EXCEPTION_CONTINUE_EXECUTION if exception is fixed up by the EE
int interceptor_ICJI::FilterException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    mc->cr->AddCall("FilterException");
    int temp = original_ICorJitInfo->FilterException(pExceptionPointers);
    mc->recFilterException(pExceptionPointers, temp);
    return temp;
}

// Cleans up internal EE tracking when an exception is caught.
void interceptor_ICJI::HandleException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    // bswHack?
    mc->cr->AddCall("HandleException");
    original_ICorJitInfo->HandleException(pExceptionPointers);
    mc->recHandleException(pExceptionPointers);
}

void interceptor_ICJI::ThrowExceptionForJitResult(HRESULT result)
{
    mc->cr->AddCall("ThrowExceptionForJitResult");
    original_ICorJitInfo->ThrowExceptionForJitResult(result);
}

// Throws an exception defined by the given throw helper.
void interceptor_ICJI::ThrowExceptionForHelper(const CORINFO_HELPER_DESC* throwHelper)
{
    mc->cr->AddCall("ThrowExceptionForHelper");
    original_ICorJitInfo->ThrowExceptionForHelper(throwHelper);
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
LPCWSTR interceptor_ICJI::getJitTimeLogFilename()
{
    mc->cr->AddCall("getJitTimeLogFilename");
    LPCWSTR temp = original_ICorJitInfo->getJitTimeLogFilename();
    mc->recGetJitTimeLogFilename(temp);
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

// this function is for debugging only.  It returns the method name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* interceptor_ICJI::getMethodName(CORINFO_METHOD_HANDLE ftn,       /* IN */
                                            const char**          moduleName /* OUT */
                                            )
{
    mc->cr->AddCall("getMethodName");
    const char* temp = original_ICorJitInfo->getMethodName(ftn, moduleName);
    mc->recGetMethodName(ftn, (char*)temp, moduleName);
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

// this function is for debugging only.
size_t interceptor_ICJI::findNameOfToken(CORINFO_MODULE_HANDLE              module,        /* IN  */
                                         mdToken                            metaTOK,       /* IN  */
                                         __out_ecount(FQNameCapacity) char* szFQName,      /* OUT */
                                         size_t                             FQNameCapacity /* IN */
                                         )
{
    mc->cr->AddCall("findNameOfToken");
    size_t result = original_ICorJitInfo->findNameOfToken(module, metaTOK, szFQName, FQNameCapacity);
    mc->recFindNameOfToken(module, metaTOK, szFQName, FQNameCapacity, result);
    return result;
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

// Stuff on ICorDynamicInfo
DWORD interceptor_ICJI::getThreadTLSIndex(void** ppIndirection)
{
    mc->cr->AddCall("getThreadTLSIndex");
    DWORD temp = original_ICorJitInfo->getThreadTLSIndex(ppIndirection);
    mc->recGetThreadTLSIndex(ppIndirection, temp);
    return temp;
}

const void* interceptor_ICJI::getInlinedCallFrameVptr(void** ppIndirection)
{
    mc->cr->AddCall("getInlinedCallFrameVptr");
    const void* temp = original_ICorJitInfo->getInlinedCallFrameVptr(ppIndirection);
    mc->recGetInlinedCallFrameVptr(ppIndirection, temp);
    return temp;
}

LONG* interceptor_ICJI::getAddrOfCaptureThreadGlobal(void** ppIndirection)
{
    mc->cr->AddCall("getAddrOfCaptureThreadGlobal");
    LONG* temp = original_ICorJitInfo->getAddrOfCaptureThreadGlobal(ppIndirection);
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
void interceptor_ICJI::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE ftn, CORINFO_CONST_LOOKUP* pResult)
{
    mc->cr->AddCall("getFunctionFixedEntryPoint");
    original_ICorJitInfo->getFunctionFixedEntryPoint(ftn, pResult);
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
                                          BOOL fEmbedParent, // TRUE - embeds parent type handle of the field/method
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
CORINFO_LOOKUP_KIND interceptor_ICJI::getLocationOfThisType(CORINFO_METHOD_HANDLE context)
{
    mc->cr->AddCall("getLocationOfThisType");
    CORINFO_LOOKUP_KIND temp = original_ICorJitInfo->getLocationOfThisType(context);
    mc->recGetLocationOfThisType(context, &temp);
    return temp;
}

// return the unmanaged target *if method has already been prelinked.*
void* interceptor_ICJI::getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method, void** ppIndirection)
{
    mc->cr->AddCall("getPInvokeUnmanagedTarget");
    void* result = original_ICorJitInfo->getPInvokeUnmanagedTarget(method, ppIndirection);
    mc->recGetPInvokeUnmanagedTarget(method, ppIndirection, result);
    return result;
}

// return address of fixup area for late-bound PInvoke calls.
void* interceptor_ICJI::getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method, void** ppIndirection)
{
    mc->cr->AddCall("getAddressOfPInvokeFixup");
    void* temp = original_ICorJitInfo->getAddressOfPInvokeFixup(method, ppIndirection);
    mc->recGetAddressOfPInvokeFixup(method, ppIndirection, temp);
    return temp;
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
void interceptor_ICJI::GetProfilingHandle(BOOL* pbHookFunction, void** pProfilerHandle, BOOL* pbIndirectedHandles)
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
    struct Param : FilterSuperPMIExceptionsParam_CaptureException
    {
        interceptor_ICJI*       pThis;
        CORINFO_RESOLVED_TOKEN* pResolvedToken;
        CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken;
        CORINFO_METHOD_HANDLE   callerHandle;
        CORINFO_CALLINFO_FLAGS  flags;
        CORINFO_CALL_INFO*      pResult;
    } param;
    param.pThis                     = this;
    param.pResolvedToken            = pResolvedToken;
    param.pConstrainedResolvedToken = pConstrainedResolvedToken;
    param.callerHandle              = callerHandle;
    param.flags                     = flags;
    param.pResult                   = pResult;

    PAL_TRY(Param*, pOuterParam,
            &param){PAL_TRY(Param*, pParam, pOuterParam){pParam->pThis->mc->cr->AddCall("getCallInfo");
    pParam->pThis->original_ICorJitInfo->getCallInfo(pParam->pResolvedToken, pParam->pConstrainedResolvedToken,
                                                     pParam->callerHandle, pParam->flags, pParam->pResult);
}
PAL_EXCEPT_FILTER(FilterSuperPMIExceptions_CaptureExceptionAndContinue)
{
}
PAL_ENDTRY
}
PAL_FINALLY
{
    this->mc->recGetCallInfo(pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult,
                             param.exceptionCode);
}
PAL_ENDTRY
}

BOOL interceptor_ICJI::canAccessFamily(CORINFO_METHOD_HANDLE hCaller, CORINFO_CLASS_HANDLE hInstanceType)
{
    mc->cr->AddCall("canAccessFamily");
    BOOL temp = original_ICorJitInfo->canAccessFamily(hCaller, hInstanceType);
    mc->recCanAccessFamily(hCaller, hInstanceType, temp);
    return temp;
}

// Returns TRUE if the Class Domain ID is the RID of the class (currently true for every class
// except reflection emitted classes and generics)
BOOL interceptor_ICJI::isRIDClassDomainID(CORINFO_CLASS_HANDLE cls)
{
    mc->cr->AddCall("isRIDClassDomainID");
    return original_ICorJitInfo->isRIDClassDomainID(cls);
}

// returns the class's domain ID for accessing shared statics
unsigned interceptor_ICJI::getClassDomainID(CORINFO_CLASS_HANDLE cls, void** ppIndirection)
{
    mc->cr->AddCall("getClassDomainID");
    unsigned temp = original_ICorJitInfo->getClassDomainID(cls, ppIndirection);
    mc->recGetClassDomainID(cls, ppIndirection, temp);
    return temp;
}

// return the data's address (for static fields only)
void* interceptor_ICJI::getFieldAddress(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    mc->cr->AddCall("getFieldAddress");
    void* temp = original_ICorJitInfo->getFieldAddress(field, ppIndirection);

    // Figure out the element type so we know how much we can load
    CORINFO_CLASS_HANDLE cch;
    CorInfoType          cit = getFieldType(field, &cch, NULL);
    mc->recGetFieldAddress(field, ppIndirection, temp, cit);
    return temp;
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

InfoAccessType interceptor_ICJI::emptyStringLiteral(void** ppValue)
{
    mc->cr->AddCall("emptyStringLiteral");
    InfoAccessType temp = original_ICorJitInfo->emptyStringLiteral(ppValue);
    mc->recEmptyStringLiteral(ppValue, temp);
    return temp;
}

// (static fields only) given that 'field' refers to thread local store,
// return the ID (TLS index), which is used to find the begining of the
// TLS data area for the particular DLL 'field' is associated with.
DWORD interceptor_ICJI::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    mc->cr->AddCall("getFieldThreadLocalStoreID");
    DWORD temp = original_ICorJitInfo->getFieldThreadLocalStoreID(field, ppIndirection);
    mc->recGetFieldThreadLocalStoreID(field, ppIndirection, temp);
    return temp;
}

// Sets another object to intercept calls to "self" and current method being compiled
void interceptor_ICJI::setOverride(ICorDynamicInfo* pOverride, CORINFO_METHOD_HANDLE currentMethod)
{
    mc->cr->AddCall("setOverride");
    original_ICorJitInfo->setOverride(pOverride, currentMethod);
}

// Adds an active dependency from the context method's module to the given module
// This is internal callback for the EE. JIT should not call it directly.
void interceptor_ICJI::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo)
{
    mc->cr->AddCall("addActiveDependency");
    original_ICorJitInfo->addActiveDependency(moduleFrom, moduleTo);
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

// return a thunk that will copy the arguments for the given signature.
void* interceptor_ICJI::getTailCallCopyArgsThunk(CORINFO_SIG_INFO* pSig, CorInfoHelperTailCallSpecialHandling flags)
{
    mc->cr->AddCall("getTailCallCopyArgsThunk");
    void* result = original_ICorJitInfo->getTailCallCopyArgsThunk(pSig, flags);
    mc->recGetTailCallCopyArgsThunk(pSig, flags, result);
    return result;
}

// Stuff directly on ICorJitInfo

// Returns extended flags for a particular compilation instance.
DWORD interceptor_ICJI::getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes)
{
    mc->cr->AddCall("getJitFlags");
    DWORD result = original_ICorJitInfo->getJitFlags(jitFlags, sizeInBytes);
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

// return memory manager that the JIT can use to allocate a regular memory
IEEMemoryManager* interceptor_ICJI::getMemoryManager()
{
    mc->cr->AddCall("getMemoryManager");
    if (current_IEEMM->original_IEEMM == nullptr)
        current_IEEMM->original_IEEMM = original_ICorJitInfo->getMemoryManager();
    return current_IEEMM;
}

// get a block of memory for the code, readonly data, and read-write data
void interceptor_ICJI::allocMem(ULONG              hotCodeSize,   /* IN */
                                ULONG              coldCodeSize,  /* IN */
                                ULONG              roDataSize,    /* IN */
                                ULONG              xcptnsCount,   /* IN */
                                CorJitAllocMemFlag flag,          /* IN */
                                void**             hotCodeBlock,  /* OUT */
                                void**             coldCodeBlock, /* OUT */
                                void**             roDataBlock    /* OUT */
                                )
{
    mc->cr->AddCall("allocMem");
    original_ICorJitInfo->allocMem(hotCodeSize, coldCodeSize, roDataSize, xcptnsCount, flag, hotCodeBlock,
                                   coldCodeBlock, roDataBlock);
    mc->cr->recAllocMem(hotCodeSize, coldCodeSize, roDataSize, xcptnsCount, flag, hotCodeBlock, coldCodeBlock,
                        roDataBlock);
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
void interceptor_ICJI::reserveUnwindInfo(BOOL  isFunclet,  /* IN */
                                         BOOL  isColdCode, /* IN */
                                         ULONG unwindSize  /* IN */
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
void interceptor_ICJI::allocUnwindInfo(BYTE*          pHotCode,     /* IN */
                                       BYTE*          pColdCode,    /* IN */
                                       ULONG          startOffset,  /* IN */
                                       ULONG          endOffset,    /* IN */
                                       ULONG          unwindSize,   /* IN */
                                       BYTE*          pUnwindBlock, /* IN */
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

// only used on x64
void interceptor_ICJI::yieldExecution()
{
    mc->cr->AddCall("yieldExecution"); // Nothing to record
    original_ICorJitInfo->yieldExecution();
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
BOOL interceptor_ICJI::logMsg(unsigned level, const char* fmt, va_list args)
{
    mc->cr->AddCall("logMsg");
    return original_ICorJitInfo->logMsg(level, fmt, args);
}

// do an assert.  will return true if the code should retry (DebugBreak)
// returns false, if the assert should be igored.
int interceptor_ICJI::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    mc->cr->AddCall("doAssert");
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
HRESULT interceptor_ICJI::allocBBProfileBuffer(ULONG           count, // The number of basic blocks that we have
                                               ProfileBuffer** profileBuffer)
{
    mc->cr->AddCall("allocBBProfileBuffer");
    HRESULT result = original_ICorJitInfo->allocBBProfileBuffer(count, profileBuffer);
    mc->cr->recAllocBBProfileBuffer(count, profileBuffer, result);
    return result;
}

// get profile information to be used for optimizing the current method.  The format
// of the buffer is the same as the format the JIT passes to allocBBProfileBuffer.
HRESULT interceptor_ICJI::getBBProfileData(CORINFO_METHOD_HANDLE ftnHnd,
                                           ULONG*                count, // The number of basic blocks that we have
                                           ProfileBuffer**       profileBuffer,
                                           ULONG*                numRuns)
{
    mc->cr->AddCall("getBBProfileData");
    HRESULT temp = original_ICorJitInfo->getBBProfileData(ftnHnd, count, profileBuffer, numRuns);
    mc->recGetBBProfileData(ftnHnd, count, profileBuffer, numRuns, temp);
    return temp;
}

// Associates a native call site, identified by its offset in the native code stream, with
// the signature information and method handle the JIT used to lay out the call site. If
// the call site has no signature information (e.g. a helper call) or has no method handle
// (e.g. a CALLI P/Invoke), then null should be passed instead.
void interceptor_ICJI::recordCallSite(ULONG                 instrOffset, /* IN */
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
void interceptor_ICJI::recordRelocation(void* location,   /* IN  */
                                        void* target,     /* IN  */
                                        WORD  fRelocType, /* IN  */
                                        WORD  slotNum,    /* IN  */
                                        INT32 addlDelta   /* IN  */
                                        )
{
    mc->cr->AddCall("recordRelocation");
    original_ICorJitInfo->recordRelocation(location, target, fRelocType, slotNum, addlDelta);
    mc->cr->recRecordRelocation(location, target, fRelocType, slotNum, addlDelta);
}

WORD interceptor_ICJI::getRelocTypeHint(void* target)
{
    mc->cr->AddCall("getRelocTypeHint");
    WORD result = original_ICorJitInfo->getRelocTypeHint(target);
    mc->recGetRelocTypeHint(target, result);
    return result;
}

// A callback to identify the range of address known to point to
// compiler-generated native entry points that call back into
// MSIL.
void interceptor_ICJI::getModuleNativeEntryPointRange(void** pStart, /* OUT */
                                                      void** pEnd    /* OUT */
                                                      )
{
    mc->cr->AddCall("getModuleNativeEntryPointRange");
    original_ICorJitInfo->getModuleNativeEntryPointRange(pStart, pEnd);
}

// For what machine does the VM expect the JIT to generate code? The VM
// returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
// is cross-compiling (such as the case for crossgen), it will return a
// different value than if it was compiling for the host architecture.
//
DWORD interceptor_ICJI::getExpectedTargetArchitecture()
{
    return original_ICorJitInfo->getExpectedTargetArchitecture();
}
