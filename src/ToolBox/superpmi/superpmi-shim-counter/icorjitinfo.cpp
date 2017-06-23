//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "icorjitinfo.h"
#include "superpmi-shim-counter.h"
#include "ieememorymanager.h"
#include "icorjitcompiler.h"
#include "spmiutil.h"

// Stuff on ICorStaticInfo
/**********************************************************************************/
//
// ICorMethodInfo
//
/**********************************************************************************/
// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD interceptor_ICJI::getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */)
{
    mcs->AddCall("getMethodAttribs");
    return original_ICorJitInfo->getMethodAttribs(ftn);
}

// sets private JIT flags, which can be, retrieved using getAttrib.
void interceptor_ICJI::setMethodAttribs(CORINFO_METHOD_HANDLE     ftn, /* IN */
                                        CorInfoMethodRuntimeFlags attribs /* IN */)
{
    mcs->AddCall("setMethodAttribs");
    original_ICorJitInfo->setMethodAttribs(ftn, attribs);
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
    mcs->AddCall("getMethodSig");
    original_ICorJitInfo->getMethodSig(ftn, sig, memberParent);
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
    mcs->AddCall("getMethodInfo");
    return original_ICorJitInfo->getMethodInfo(ftn, info);
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
    mcs->AddCall("canInline");
    return original_ICorJitInfo->canInline(callerHnd, calleeHnd, pRestrictions);
}

// Reports whether or not a method can be inlined, and why.  canInline is responsible for reporting all
// inlining results when it returns INLINE_FAIL and INLINE_NEVER.  All other results are reported by the
// JIT.
void interceptor_ICJI::reportInliningDecision(CORINFO_METHOD_HANDLE inlinerHnd,
                                              CORINFO_METHOD_HANDLE inlineeHnd,
                                              CorInfoInline         inlineResult,
                                              const char*           reason)
{
    mcs->AddCall("reportInliningDecision");
    original_ICorJitInfo->reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
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
    mcs->AddCall("canTailCall");
    return original_ICorJitInfo->canTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
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
    mcs->AddCall("reportTailCallDecision");
    original_ICorJitInfo->reportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
}

// get individual exception handler
void interceptor_ICJI::getEHinfo(CORINFO_METHOD_HANDLE ftn,      /* IN  */
                                 unsigned              EHnumber, /* IN */
                                 CORINFO_EH_CLAUSE*    clause    /* OUT */
                                 )
{
    mcs->AddCall("getEHinfo");
    original_ICorJitInfo->getEHinfo(ftn, EHnumber, clause);
}

// return class it belongs to
CORINFO_CLASS_HANDLE interceptor_ICJI::getMethodClass(CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("getMethodClass");
    return original_ICorJitInfo->getMethodClass(method);
}

// return module it belongs to
CORINFO_MODULE_HANDLE interceptor_ICJI::getMethodModule(CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("getMethodModule");
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
    mcs->AddCall("getMethodVTableOffset");
    original_ICorJitInfo->getMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
}

// Find the virtual method in implementingClass that overrides virtualMethod.
// Return null if devirtualization is not possible.
CORINFO_METHOD_HANDLE interceptor_ICJI::resolveVirtualMethod(CORINFO_METHOD_HANDLE  virtualMethod,
                                                             CORINFO_CLASS_HANDLE   implementingClass,
                                                             CORINFO_CONTEXT_HANDLE ownerType)
{
    mcs->AddCall("resolveVirtualMethod");
    return original_ICorJitInfo->resolveVirtualMethod(virtualMethod, implementingClass, ownerType);
}

void interceptor_ICJI::expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    mcs->AddCall("expandRawHandleIntrinsic");
    original_ICorJitInfo->expandRawHandleIntrinsic(pResolvedToken, pResult);
}

// If a method's attributes have (getMethodAttribs) CORINFO_FLG_INTRINSIC set,
// getIntrinsicID() returns the intrinsic ID.
CorInfoIntrinsics interceptor_ICJI::getIntrinsicID(CORINFO_METHOD_HANDLE method, bool* pMustExpand /* OUT */
                                                   )
{
    mcs->AddCall("getIntrinsicID");
    return original_ICorJitInfo->getIntrinsicID(method, pMustExpand);
}

// Is the given module the System.Numerics.Vectors module?
bool interceptor_ICJI::isInSIMDModule(CORINFO_CLASS_HANDLE classHnd)
{
    mcs->AddCall("isInSIMDModule");
    return original_ICorJitInfo->isInSIMDModule(classHnd);
}

// return the unmanaged calling convention for a PInvoke
CorInfoUnmanagedCallConv interceptor_ICJI::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("getUnmanagedCallConv");
    return original_ICorJitInfo->getUnmanagedCallConv(method);
}

// return if any marshaling is required for PInvoke methods.  Note that
// method == 0 => calli.  The call site sig is only needed for the varargs or calli case
BOOL interceptor_ICJI::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    mcs->AddCall("pInvokeMarshalingRequired");
    return original_ICorJitInfo->pInvokeMarshalingRequired(method, callSiteSig);
}

// Check constraints on method type arguments (only).
// The parent class should be checked separately using satisfiesClassConstraints(parent).
BOOL interceptor_ICJI::satisfiesMethodConstraints(CORINFO_CLASS_HANDLE  parent, // the exact parent of the method
                                                  CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("satisfiesMethodConstraints");
    return original_ICorJitInfo->satisfiesMethodConstraints(parent, method);
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
    mcs->AddCall("isCompatibleDelegate");
    return original_ICorJitInfo->isCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate);
}

// Indicates if the method is an instance of the generic
// method that passes (or has passed) verification
CorInfoInstantiationVerification interceptor_ICJI::isInstantiationOfVerifiedGeneric(CORINFO_METHOD_HANDLE method /* IN
                                                                                                                  */
                                                                                    )
{
    mcs->AddCall("isInstantiationOfVerifiedGeneric");
    return original_ICorJitInfo->isInstantiationOfVerifiedGeneric(method);
}

// Loads the constraints on a typical method definition, detecting cycles;
// for use in verification.
void interceptor_ICJI::initConstraintsForVerification(CORINFO_METHOD_HANDLE method,                        /* IN */
                                                      BOOL*                 pfHasCircularClassConstraints, /* OUT */
                                                      BOOL*                 pfHasCircularMethodConstraint  /* OUT */
                                                      )
{
    mcs->AddCall("initConstraintsForVerification");
    original_ICorJitInfo->initConstraintsForVerification(method, pfHasCircularClassConstraints,
                                                         pfHasCircularMethodConstraint);
}

// Returns enum whether the method does not require verification
// Also see ICorModuleInfo::canSkipVerification
CorInfoCanSkipVerificationResult interceptor_ICJI::canSkipMethodVerification(CORINFO_METHOD_HANDLE ftnHandle)
{
    mcs->AddCall("canSkipMethodVerification");
    return original_ICorJitInfo->canSkipMethodVerification(ftnHandle);
}

// load and restore the method
void interceptor_ICJI::methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("methodMustBeLoadedBeforeCodeIsRun");
    original_ICorJitInfo->methodMustBeLoadedBeforeCodeIsRun(method);
}

CORINFO_METHOD_HANDLE interceptor_ICJI::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE method)
{
    mcs->AddCall("mapMethodDeclToMethodImpl");
    return original_ICorJitInfo->mapMethodDeclToMethodImpl(method);
}

// Returns the global cookie for the /GS unsafe buffer checks
// The cookie might be a constant value (JIT), or a handle to memory location (Ngen)
void interceptor_ICJI::getGSCookie(GSCookie*  pCookieVal, // OUT
                                   GSCookie** ppCookieVal // OUT
                                   )
{
    mcs->AddCall("getGSCookie");
    original_ICorJitInfo->getGSCookie(pCookieVal, ppCookieVal);
}

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/

// Resolve metadata token into runtime method handles.
void interceptor_ICJI::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    mcs->AddCall("resolveToken");
    original_ICorJitInfo->resolveToken(pResolvedToken);
}

bool interceptor_ICJI::tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    mcs->AddCall("tryResolveToken");
    return original_ICorJitInfo->tryResolveToken(pResolvedToken);
}

// Signature information about the call sig
void interceptor_ICJI::findSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                               unsigned               sigTOK,  /* IN */
                               CORINFO_CONTEXT_HANDLE context, /* IN */
                               CORINFO_SIG_INFO*      sig      /* OUT */
                               )
{
    mcs->AddCall("findSig");
    original_ICorJitInfo->findSig(module, sigTOK, context, sig);
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
    mcs->AddCall("findCallSiteSig");
    original_ICorJitInfo->findCallSiteSig(module, methTOK, context, sig);
}

CORINFO_CLASS_HANDLE interceptor_ICJI::getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken /* IN  */)
{
    mcs->AddCall("getTokenTypeAsHandle");
    return original_ICorJitInfo->getTokenTypeAsHandle(pResolvedToken);
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
    mcs->AddCall("canSkipVerification");
    return original_ICorJitInfo->canSkipVerification(module);
}

// Checks if the given metadata token is valid
BOOL interceptor_ICJI::isValidToken(CORINFO_MODULE_HANDLE module, /* IN  */
                                    unsigned              metaTOK /* IN  */
                                    )
{
    mcs->AddCall("isValidToken");
    return original_ICorJitInfo->isValidToken(module, metaTOK);
}

// Checks if the given metadata token is valid StringRef
BOOL interceptor_ICJI::isValidStringRef(CORINFO_MODULE_HANDLE module, /* IN  */
                                        unsigned              metaTOK /* IN  */
                                        )
{
    mcs->AddCall("isValidStringRef");
    return original_ICorJitInfo->isValidStringRef(module, metaTOK);
}

BOOL interceptor_ICJI::shouldEnforceCallvirtRestriction(CORINFO_MODULE_HANDLE scope)
{
    mcs->AddCall("shouldEnforceCallvirtRestriction");
    return original_ICorJitInfo->shouldEnforceCallvirtRestriction(scope);
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
    mcs->AddCall("asCorInfoType");
    return original_ICorJitInfo->asCorInfoType(cls);
}

// for completeness
const char* interceptor_ICJI::getClassName(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getClassName");
    return original_ICorJitInfo->getClassName(cls);
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
    mcs->AddCall("appendClassName");
    return original_ICorJitInfo->appendClassName(ppBuf, pnBufLen, cls, fNamespace, fFullInst, fAssembly);
}

// Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) &
// CORINFO_FLG_VALUECLASS, except faster.
BOOL interceptor_ICJI::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("isValueClass");
    return original_ICorJitInfo->isValueClass(cls);
}

// If this method returns true, JIT will do optimization to inline the check for
//     GetTypeFromHandle(handle) == obj.GetType()
BOOL interceptor_ICJI::canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("canInlineTypeCheckWithObjectVTable");
    return original_ICorJitInfo->canInlineTypeCheckWithObjectVTable(cls);
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
DWORD interceptor_ICJI::getClassAttribs(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getClassAttribs");
    return original_ICorJitInfo->getClassAttribs(cls);
}

// Returns "TRUE" iff "cls" is a struct type such that return buffers used for returning a value
// of this type must be stack-allocated.  This will generally be true only if the struct
// contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
// an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
// returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
// buffers do not require GC write barriers.
BOOL interceptor_ICJI::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("isStructRequiringStackAllocRetBuf");
    return original_ICorJitInfo->isStructRequiringStackAllocRetBuf(cls);
}

CORINFO_MODULE_HANDLE interceptor_ICJI::getClassModule(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getClassModule");
    return original_ICorJitInfo->getClassModule(cls);
}

// Returns the assembly that contains the module "mod".
CORINFO_ASSEMBLY_HANDLE interceptor_ICJI::getModuleAssembly(CORINFO_MODULE_HANDLE mod)
{
    mcs->AddCall("getModuleAssembly");
    return original_ICorJitInfo->getModuleAssembly(mod);
}

// Returns the name of the assembly "assem".
const char* interceptor_ICJI::getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem)
{
    mcs->AddCall("getAssemblyName");
    return original_ICorJitInfo->getAssemblyName(assem);
}

// Allocate and delete process-lifetime objects.  Should only be
// referred to from static fields, lest a leak occur.
// Note that "LongLifetimeFree" does not execute destructors, if "obj"
// is an array of a struct type with a destructor.
void* interceptor_ICJI::LongLifetimeMalloc(size_t sz)
{
    mcs->AddCall("LongLifetimeMalloc");
    return original_ICorJitInfo->LongLifetimeMalloc(sz);
}

void interceptor_ICJI::LongLifetimeFree(void* obj)
{
    mcs->AddCall("LongLifetimeFree");
    original_ICorJitInfo->LongLifetimeFree(obj);
}

size_t interceptor_ICJI::getClassModuleIdForStatics(CORINFO_CLASS_HANDLE   cls,
                                                    CORINFO_MODULE_HANDLE* pModule,
                                                    void**                 ppIndirection)
{
    mcs->AddCall("getClassModuleIdForStatics");
    return original_ICorJitInfo->getClassModuleIdForStatics(cls, pModule, ppIndirection);
}

// return the number of bytes needed by an instance of the class
unsigned interceptor_ICJI::getClassSize(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getClassSize");
    return original_ICorJitInfo->getClassSize(cls);
}

unsigned interceptor_ICJI::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint)
{
    mcs->AddCall("getClassAlignmentRequirement");
    return original_ICorJitInfo->getClassAlignmentRequirement(cls, fDoubleAlignHint);
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
    mcs->AddCall("getClassGClayout");
    return original_ICorJitInfo->getClassGClayout(cls, gcPtrs);
}

// returns the number of instance fields in a class
unsigned interceptor_ICJI::getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls /* IN */
                                                     )
{
    mcs->AddCall("getClassNumInstanceFields");
    return original_ICorJitInfo->getClassNumInstanceFields(cls);
}

CORINFO_FIELD_HANDLE interceptor_ICJI::getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num)
{
    mcs->AddCall("getFieldInClass");
    return original_ICorJitInfo->getFieldInClass(clsHnd, num);
}

BOOL interceptor_ICJI::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional)
{
    mcs->AddCall("checkMethodModifier");
    return original_ICorJitInfo->checkMethodModifier(hMethod, modifier, fOptional);
}

// returns the "NEW" helper optimized for "newCls."
CorInfoHelpFunc interceptor_ICJI::getNewHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                               CORINFO_METHOD_HANDLE   callerHandle)
{
    mcs->AddCall("getNewHelper");
    return original_ICorJitInfo->getNewHelper(pResolvedToken, callerHandle);
}

// returns the newArr (1-Dim array) helper optimized for "arrayCls."
CorInfoHelpFunc interceptor_ICJI::getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls)
{
    mcs->AddCall("getNewArrHelper");
    return original_ICorJitInfo->getNewArrHelper(arrayCls);
}

// returns the optimized "IsInstanceOf" or "ChkCast" helper
CorInfoHelpFunc interceptor_ICJI::getCastingHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fThrowing)
{
    mcs->AddCall("getCastingHelper");
    return original_ICorJitInfo->getCastingHelper(pResolvedToken, fThrowing);
}

// returns helper to trigger static constructor
CorInfoHelpFunc interceptor_ICJI::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    mcs->AddCall("getSharedCCtorHelper");
    return original_ICorJitInfo->getSharedCCtorHelper(clsHnd);
}

CorInfoHelpFunc interceptor_ICJI::getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn)
{
    mcs->AddCall("getSecurityPrologHelper");
    return original_ICorJitInfo->getSecurityPrologHelper(ftn);
}

// This is not pretty.  Boxing nullable<T> actually returns
// a boxed<T> not a boxed Nullable<T>.  This call allows the verifier
// to call back to the EE on the 'box' instruction and get the transformed
// type to use for verification.
CORINFO_CLASS_HANDLE interceptor_ICJI::getTypeForBox(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getTypeForBox");
    return original_ICorJitInfo->getTypeForBox(cls);
}

// returns the correct box helper for a particular class.  Note
// that if this returns CORINFO_HELP_BOX, the JIT can assume
// 'standard' boxing (allocate object and copy), and optimize
CorInfoHelpFunc interceptor_ICJI::getBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getBoxHelper");
    return original_ICorJitInfo->getBoxHelper(cls);
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
    mcs->AddCall("getUnBoxHelper");
    return original_ICorJitInfo->getUnBoxHelper(cls);
}

bool interceptor_ICJI::getReadyToRunHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                           CORINFO_LOOKUP_KIND*    pGenericLookupKind,
                                           CorInfoHelpFunc         id,
                                           CORINFO_CONST_LOOKUP*   pLookup)
{
    mcs->AddCall("getReadyToRunHelper");
    return original_ICorJitInfo->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, id, pLookup);
}

void interceptor_ICJI::getReadyToRunDelegateCtorHelper(CORINFO_RESOLVED_TOKEN* pTargetMethod,
                                                       CORINFO_CLASS_HANDLE    delegateType,
                                                       CORINFO_LOOKUP*         pLookup)
{
    mcs->AddCall("getReadyToRunDelegateCtorHelper");
    original_ICorJitInfo->getReadyToRunDelegateCtorHelper(pTargetMethod, delegateType, pLookup);
}

const char* interceptor_ICJI::getHelperName(CorInfoHelpFunc funcNum)
{
    mcs->AddCall("getHelperName");
    return original_ICorJitInfo->getHelperName(funcNum);
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
    mcs->AddCall("initClass");
    return original_ICorJitInfo->initClass(field, method, context, speculative);
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
    mcs->AddCall("classMustBeLoadedBeforeCodeIsRun");
    original_ICorJitInfo->classMustBeLoadedBeforeCodeIsRun(cls);
}

// returns the class handle for the special builtin classes
CORINFO_CLASS_HANDLE interceptor_ICJI::getBuiltinClass(CorInfoClassId classId)
{
    mcs->AddCall("getBuiltinClass");
    return original_ICorJitInfo->getBuiltinClass(classId);
}

// "System.Int32" ==> CORINFO_TYPE_INT..
CorInfoType interceptor_ICJI::getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getTypeForPrimitiveValueClass");
    return original_ICorJitInfo->getTypeForPrimitiveValueClass(cls);
}

// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
BOOL interceptor_ICJI::canCast(CORINFO_CLASS_HANDLE child, // subtype (extends parent)
                               CORINFO_CLASS_HANDLE parent // base type
                               )
{
    mcs->AddCall("canCast");
    return original_ICorJitInfo->canCast(child, parent);
}

// TRUE if cls1 and cls2 are considered equivalent types.
BOOL interceptor_ICJI::areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mcs->AddCall("areTypesEquivalent");
    return original_ICorJitInfo->areTypesEquivalent(cls1, cls2);
}

// returns is the intersection of cls1 and cls2.
CORINFO_CLASS_HANDLE interceptor_ICJI::mergeClasses(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    mcs->AddCall("mergeClasses");
    return original_ICorJitInfo->mergeClasses(cls1, cls2);
}

// Given a class handle, returns the Parent type.
// For COMObjectType, it returns Class Handle of System.Object.
// Returns 0 if System.Object is passed in.
CORINFO_CLASS_HANDLE interceptor_ICJI::getParentType(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getParentType");
    return original_ICorJitInfo->getParentType(cls);
}

// Returns the CorInfoType of the "child type". If the child type is
// not a primitive type, *clsRet will be set.
// Given an Array of Type Foo, returns Foo.
// Given BYREF Foo, returns Foo
CorInfoType interceptor_ICJI::getChildType(CORINFO_CLASS_HANDLE clsHnd, CORINFO_CLASS_HANDLE* clsRet)
{
    mcs->AddCall("getChildType");
    return original_ICorJitInfo->getChildType(clsHnd, clsRet);
}

// Check constraints on type arguments of this class and parent classes
BOOL interceptor_ICJI::satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("satisfiesClassConstraints");
    return original_ICorJitInfo->satisfiesClassConstraints(cls);
}

// Check if this is a single dimensional array type
BOOL interceptor_ICJI::isSDArray(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("isSDArray");
    return original_ICorJitInfo->isSDArray(cls);
}

// Get the numbmer of dimensions in an array
unsigned interceptor_ICJI::getArrayRank(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("getArrayRank");
    return original_ICorJitInfo->getArrayRank(cls);
}

// Get static field data for an array
void* interceptor_ICJI::getArrayInitializationData(CORINFO_FIELD_HANDLE field, DWORD size)
{
    mcs->AddCall("getArrayInitializationData");
    return original_ICorJitInfo->getArrayInitializationData(field, size);
}

// Check Visibility rules.
CorInfoIsAccessAllowedResult interceptor_ICJI::canAccessClass(
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    CORINFO_METHOD_HANDLE   callerHandle,
    CORINFO_HELPER_DESC*    pAccessHelper /* If canAccessMethod returns something other
                                                than ALLOWED, then this is filled in. */
    )
{
    mcs->AddCall("canAccessClass");
    return original_ICorJitInfo->canAccessClass(pResolvedToken, callerHandle, pAccessHelper);
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
    mcs->AddCall("getFieldName");
    return original_ICorJitInfo->getFieldName(ftn, moduleName);
}

// return class it belongs to
CORINFO_CLASS_HANDLE interceptor_ICJI::getFieldClass(CORINFO_FIELD_HANDLE field)
{
    mcs->AddCall("getFieldClass");
    return original_ICorJitInfo->getFieldClass(field);
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
    mcs->AddCall("getFieldType");
    return original_ICorJitInfo->getFieldType(field, structType, memberParent);
}

// return the data member's instance offset
unsigned interceptor_ICJI::getFieldOffset(CORINFO_FIELD_HANDLE field)
{
    mcs->AddCall("getFieldOffset");
    return original_ICorJitInfo->getFieldOffset(field);
}

// TODO: jit64 should be switched to the same plan as the i386 jits - use
// getClassGClayout to figure out the need for writebarrier helper, and inline the copying.
// The interpretted value class copy is slow. Once this happens, USE_WRITE_BARRIER_HELPERS
bool interceptor_ICJI::isWriteBarrierHelperRequired(CORINFO_FIELD_HANDLE field)
{
    mcs->AddCall("isWriteBarrierHelperRequired");
    return original_ICorJitInfo->isWriteBarrierHelperRequired(field);
}

void interceptor_ICJI::getFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_METHOD_HANDLE   callerHandle,
                                    CORINFO_ACCESS_FLAGS    flags,
                                    CORINFO_FIELD_INFO*     pResult)
{
    mcs->AddCall("getFieldInfo");
    original_ICorJitInfo->getFieldInfo(pResolvedToken, callerHandle, flags, pResult);
}

// Returns true iff "fldHnd" represents a static field.
bool interceptor_ICJI::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    mcs->AddCall("isFieldStatic");
    return original_ICorJitInfo->isFieldStatic(fldHnd);
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
    mcs->AddCall("getBoundaries");
    original_ICorJitInfo->getBoundaries(ftn, cILOffsets, pILOffsets, implictBoundaries);
}

// Report back the mapping from IL to native code,
// this map should include all boundaries that 'getBoundaries'
// reported as interesting to the debugger.

// Note that debugger (and profiler) is assuming that all of the
// offsets form a contiguous block of memory, and that the
// OffsetMapping is sorted in order of increasing native offset.
void interceptor_ICJI::setBoundaries(CORINFO_METHOD_HANDLE         ftn,  // [IN] method of interest
                                     ULONG32                       cMap, // [IN] size of pMap
                                     ICorDebugInfo::OffsetMapping* pMap  // [IN] map including all points of interest.
                                                                         //      jit allocated with allocateArray, EE
                                                                         //      frees
                                     )
{
    mcs->AddCall("setBoundaries");
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
    mcs->AddCall("getVars");
    original_ICorJitInfo->getVars(ftn, cVars, vars, extendOthers);
}

// Report back to the EE the location of every variable.
// note that the JIT might split lifetimes into different
// locations etc.

void interceptor_ICJI::setVars(CORINFO_METHOD_HANDLE         ftn,   // [IN] method of interest
                               ULONG32                       cVars, // [IN] size of 'vars'
                               ICorDebugInfo::NativeVarInfo* vars   // [IN] map telling where local vars are stored at
                                                                    // what points
                                                                    //      jit allocated with allocateArray, EE frees
                               )
{
    mcs->AddCall("setVars");
    original_ICorJitInfo->setVars(ftn, cVars, vars);
}

/*-------------------------- Misc ---------------------------------------*/

// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* interceptor_ICJI::allocateArray(ULONG cBytes)
{
    mcs->AddCall("allocateArray");
    return original_ICorJitInfo->allocateArray(cBytes);
}

// JitCompiler will free arrays passed by the EE using this
// For eg, The EE returns memory in getVars() and getBoundaries()
// to the JitCompiler, which the JitCompiler should release using
// freeArray()
void interceptor_ICJI::freeArray(void* array)
{
    mcs->AddCall("freeArray");
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
    mcs->AddCall("getArgNext");
    return original_ICorJitInfo->getArgNext(args);
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
    mcs->AddCall("getArgType");
    return original_ICorJitInfo->getArgType(sig, args, vcTypeRet);
}

// If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
CORINFO_CLASS_HANDLE interceptor_ICJI::getArgClass(CORINFO_SIG_INFO*       sig, /* IN */
                                                   CORINFO_ARG_LIST_HANDLE args /* IN */
                                                   )
{
    mcs->AddCall("getArgClass");
    return original_ICorJitInfo->getArgClass(sig, args);
}

// Returns type of HFA for valuetype
CorInfoType interceptor_ICJI::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    mcs->AddCall("getHFAType");
    return original_ICorJitInfo->getHFAType(hClass);
}

/*****************************************************************************
* ICorErrorInfo contains methods to deal with SEH exceptions being thrown
* from the corinfo interface.  These methods may be called when an exception
* with code EXCEPTION_COMPLUS is caught.
*****************************************************************************/

// Returns the HRESULT of the current exception
HRESULT interceptor_ICJI::GetErrorHRESULT(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    mcs->AddCall("GetErrorHRESULT");
    return original_ICorJitInfo->GetErrorHRESULT(pExceptionPointers);
}

// Fetches the message of the current exception
// Returns the size of the message (including terminating null). This can be
// greater than bufferLength if the buffer is insufficient.
ULONG interceptor_ICJI::GetErrorMessage(__inout_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength)
{
    mcs->AddCall("GetErrorMessage");
    return original_ICorJitInfo->GetErrorMessage(buffer, bufferLength);
}

// returns EXCEPTION_EXECUTE_HANDLER if it is OK for the compile to handle the
//                        exception, abort some work (like the inlining) and continue compilation
// returns EXCEPTION_CONTINUE_SEARCH if exception must always be handled by the EE
//                    things like ThreadStoppedException ...
// returns EXCEPTION_CONTINUE_EXECUTION if exception is fixed up by the EE

int interceptor_ICJI::FilterException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    mcs->AddCall("FilterException");
    return original_ICorJitInfo->FilterException(pExceptionPointers);
}

// Cleans up internal EE tracking when an exception is caught.
void interceptor_ICJI::HandleException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    mcs->AddCall("HandleException");
    original_ICorJitInfo->HandleException(pExceptionPointers);
}

void interceptor_ICJI::ThrowExceptionForJitResult(HRESULT result)
{
    mcs->AddCall("ThrowExceptionForJitResult");
    original_ICorJitInfo->ThrowExceptionForJitResult(result);
}

// Throws an exception defined by the given throw helper.
void interceptor_ICJI::ThrowExceptionForHelper(const CORINFO_HELPER_DESC* throwHelper)
{
    mcs->AddCall("ThrowExceptionForHelper");
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
    mcs->AddCall("getEEInfo");
    original_ICorJitInfo->getEEInfo(pEEInfoOut);
}

// Returns name of the JIT timer log
LPCWSTR interceptor_ICJI::getJitTimeLogFilename()
{
    mcs->AddCall("getJitTimeLogFilename");
    return original_ICorJitInfo->getJitTimeLogFilename();
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
    mcs->AddCall("getMethodDefFromMethod");
    return original_ICorJitInfo->getMethodDefFromMethod(hMethod);
}

// this function is for debugging only.  It returns the method name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* interceptor_ICJI::getMethodName(CORINFO_METHOD_HANDLE ftn,       /* IN */
                                            const char**          moduleName /* OUT */
                                            )
{
    mcs->AddCall("getMethodName");
    return original_ICorJitInfo->getMethodName(ftn, moduleName);
}

// this function is for debugging only.  It returns a value that
// is will always be the same for a given method.  It is used
// to implement the 'jitRange' functionality
unsigned interceptor_ICJI::getMethodHash(CORINFO_METHOD_HANDLE ftn /* IN */
                                         )
{
    mcs->AddCall("getMethodHash");
    return original_ICorJitInfo->getMethodHash(ftn);
}

// this function is for debugging only.
size_t interceptor_ICJI::findNameOfToken(CORINFO_MODULE_HANDLE              module,        /* IN  */
                                         mdToken                            metaTOK,       /* IN  */
                                         __out_ecount(FQNameCapacity) char* szFQName,      /* OUT */
                                         size_t                             FQNameCapacity /* IN */
                                         )
{
    mcs->AddCall("findNameOfToken");
    return original_ICorJitInfo->findNameOfToken(module, metaTOK, szFQName, FQNameCapacity);
}

bool interceptor_ICJI::getSystemVAmd64PassStructInRegisterDescriptor(
    /* IN */ CORINFO_CLASS_HANDLE                                  structHnd,
    /* OUT */ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    mcs->AddCall("getSystemVAmd64PassStructInRegisterDescriptor");
    return original_ICorJitInfo->getSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
}

// Stuff on ICorDynamicInfo
DWORD interceptor_ICJI::getThreadTLSIndex(void** ppIndirection)
{
    mcs->AddCall("getThreadTLSIndex");
    return original_ICorJitInfo->getThreadTLSIndex(ppIndirection);
}

const void* interceptor_ICJI::getInlinedCallFrameVptr(void** ppIndirection)
{
    mcs->AddCall("getInlinedCallFrameVptr");
    return original_ICorJitInfo->getInlinedCallFrameVptr(ppIndirection);
}

LONG* interceptor_ICJI::getAddrOfCaptureThreadGlobal(void** ppIndirection)
{
    mcs->AddCall("getAddrOfCaptureThreadGlobal");
    return original_ICorJitInfo->getAddrOfCaptureThreadGlobal(ppIndirection);
}

// return the native entry point to an EE helper (see CorInfoHelpFunc)
void* interceptor_ICJI::getHelperFtn(CorInfoHelpFunc ftnNum, void** ppIndirection)
{
    mcs->AddCall("getHelperFtn");
    return original_ICorJitInfo->getHelperFtn(ftnNum, ppIndirection);
}

// return a callable address of the function (native code). This function
// may return a different value (depending on whether the method has
// been JITed or not.
void interceptor_ICJI::getFunctionEntryPoint(CORINFO_METHOD_HANDLE ftn,     /* IN  */
                                             CORINFO_CONST_LOOKUP* pResult, /* OUT */
                                             CORINFO_ACCESS_FLAGS  accessFlags)
{
    mcs->AddCall("getFunctionEntryPoint");
    original_ICorJitInfo->getFunctionEntryPoint(ftn, pResult, accessFlags);
}

// return a directly callable address. This can be used similarly to the
// value returned by getFunctionEntryPoint() except that it is
// guaranteed to be multi callable entrypoint.
void interceptor_ICJI::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE ftn, CORINFO_CONST_LOOKUP* pResult)
{
    mcs->AddCall("getFunctionFixedEntryPoint");
    original_ICorJitInfo->getFunctionFixedEntryPoint(ftn, pResult);
}

// get the synchronization handle that is passed to monXstatic function
void* interceptor_ICJI::getMethodSync(CORINFO_METHOD_HANDLE ftn, void** ppIndirection)
{
    mcs->AddCall("getMethodSync");
    return original_ICorJitInfo->getMethodSync(ftn, ppIndirection);
}

// These entry points must be called if a handle is being embedded in
// the code to be passed to a JIT helper function. (as opposed to just
// being passed back into the ICorInfo interface.)

// get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
// Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
CorInfoHelpFunc interceptor_ICJI::getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle)
{
    mcs->AddCall("getLazyStringLiteralHelper");
    return original_ICorJitInfo->getLazyStringLiteralHelper(handle);
}

CORINFO_MODULE_HANDLE interceptor_ICJI::embedModuleHandle(CORINFO_MODULE_HANDLE handle, void** ppIndirection)
{
    mcs->AddCall("embedModuleHandle");
    return original_ICorJitInfo->embedModuleHandle(handle, ppIndirection);
}

CORINFO_CLASS_HANDLE interceptor_ICJI::embedClassHandle(CORINFO_CLASS_HANDLE handle, void** ppIndirection)
{
    mcs->AddCall("embedClassHandle");
    return original_ICorJitInfo->embedClassHandle(handle, ppIndirection);
}

CORINFO_METHOD_HANDLE interceptor_ICJI::embedMethodHandle(CORINFO_METHOD_HANDLE handle, void** ppIndirection)
{
    mcs->AddCall("embedMethodHandle");
    return original_ICorJitInfo->embedMethodHandle(handle, ppIndirection);
}

CORINFO_FIELD_HANDLE interceptor_ICJI::embedFieldHandle(CORINFO_FIELD_HANDLE handle, void** ppIndirection)
{
    mcs->AddCall("embedFieldHandle");
    return original_ICorJitInfo->embedFieldHandle(handle, ppIndirection);
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
    mcs->AddCall("embedGenericHandle");
    original_ICorJitInfo->embedGenericHandle(pResolvedToken, fEmbedParent, pResult);
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
    mcs->AddCall("getLocationOfThisType");
    return original_ICorJitInfo->getLocationOfThisType(context);
}

// return the unmanaged target *if method has already been prelinked.*
void* interceptor_ICJI::getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method, void** ppIndirection)
{
    mcs->AddCall("getPInvokeUnmanagedTarget");
    return original_ICorJitInfo->getPInvokeUnmanagedTarget(method, ppIndirection);
}

// return address of fixup area for late-bound PInvoke calls.
void* interceptor_ICJI::getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method, void** ppIndirection)
{
    mcs->AddCall("getAddressOfPInvokeFixup");
    return original_ICorJitInfo->getAddressOfPInvokeFixup(method, ppIndirection);
}

// return address of fixup area for late-bound PInvoke calls.
void interceptor_ICJI::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP* pLookup)
{
    mcs->AddCall("getAddressOfPInvokeTarget");
    original_ICorJitInfo->getAddressOfPInvokeTarget(method, pLookup);
}

// Generate a cookie based on the signature that would needs to be passed
// to CORINFO_HELP_PINVOKE_CALLI
LPVOID interceptor_ICJI::GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig, void** ppIndirection)
{
    mcs->AddCall("GetCookieForPInvokeCalliSig");
    return original_ICorJitInfo->GetCookieForPInvokeCalliSig(szMetaSig, ppIndirection);
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool interceptor_ICJI::canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
{
    mcs->AddCall("canGetCookieForPInvokeCalliSig");
    return original_ICorJitInfo->canGetCookieForPInvokeCalliSig(szMetaSig);
}

// Gets a handle that is checked to see if the current method is
// included in "JustMyCode"
CORINFO_JUST_MY_CODE_HANDLE interceptor_ICJI::getJustMyCodeHandle(CORINFO_METHOD_HANDLE         method,
                                                                  CORINFO_JUST_MY_CODE_HANDLE** ppIndirection)
{
    mcs->AddCall("getJustMyCodeHandle");
    return original_ICorJitInfo->getJustMyCodeHandle(method, ppIndirection);
}

// Gets a method handle that can be used to correlate profiling data.
// This is the IP of a native method, or the address of the descriptor struct
// for IL.  Always guaranteed to be unique per process, and not to move. */
void interceptor_ICJI::GetProfilingHandle(BOOL* pbHookFunction, void** pProfilerHandle, BOOL* pbIndirectedHandles)
{
    mcs->AddCall("GetProfilingHandle");
    original_ICorJitInfo->GetProfilingHandle(pbHookFunction, pProfilerHandle, pbIndirectedHandles);
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
    mcs->AddCall("getCallInfo");
    original_ICorJitInfo->getCallInfo(pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult);
}

BOOL interceptor_ICJI::canAccessFamily(CORINFO_METHOD_HANDLE hCaller, CORINFO_CLASS_HANDLE hInstanceType)

{
    mcs->AddCall("canAccessFamily");
    return original_ICorJitInfo->canAccessFamily(hCaller, hInstanceType);
}
// Returns TRUE if the Class Domain ID is the RID of the class (currently true for every class
// except reflection emitted classes and generics)
BOOL interceptor_ICJI::isRIDClassDomainID(CORINFO_CLASS_HANDLE cls)
{
    mcs->AddCall("isRIDClassDomainID");
    return original_ICorJitInfo->isRIDClassDomainID(cls);
}

// returns the class's domain ID for accessing shared statics
unsigned interceptor_ICJI::getClassDomainID(CORINFO_CLASS_HANDLE cls, void** ppIndirection)
{
    mcs->AddCall("getClassDomainID");
    return original_ICorJitInfo->getClassDomainID(cls, ppIndirection);
}

// return the data's address (for static fields only)
void* interceptor_ICJI::getFieldAddress(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    mcs->AddCall("getFieldAddress");
    return original_ICorJitInfo->getFieldAddress(field, ppIndirection);
}

// registers a vararg sig & returns a VM cookie for it (which can contain other stuff)
CORINFO_VARARGS_HANDLE interceptor_ICJI::getVarArgsHandle(CORINFO_SIG_INFO* pSig, void** ppIndirection)
{
    mcs->AddCall("getVarArgsHandle");
    return original_ICorJitInfo->getVarArgsHandle(pSig, ppIndirection);
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool interceptor_ICJI::canGetVarArgsHandle(CORINFO_SIG_INFO* pSig)
{
    mcs->AddCall("canGetVarArgsHandle");
    return original_ICorJitInfo->canGetVarArgsHandle(pSig);
}

// Allocate a string literal on the heap and return a handle to it
InfoAccessType interceptor_ICJI::constructStringLiteral(CORINFO_MODULE_HANDLE module, mdToken metaTok, void** ppValue)
{
    mcs->AddCall("constructStringLiteral");
    return original_ICorJitInfo->constructStringLiteral(module, metaTok, ppValue);
}

InfoAccessType interceptor_ICJI::emptyStringLiteral(void** ppValue)
{
    mcs->AddCall("emptyStringLiteral");
    return original_ICorJitInfo->emptyStringLiteral(ppValue);
}

// (static fields only) given that 'field' refers to thread local store,
// return the ID (TLS index), which is used to find the begining of the
// TLS data area for the particular DLL 'field' is associated with.
DWORD interceptor_ICJI::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    mcs->AddCall("getFieldThreadLocalStoreID");
    return original_ICorJitInfo->getFieldThreadLocalStoreID(field, ppIndirection);
}

// Sets another object to intercept calls to "self" and current method being compiled
void interceptor_ICJI::setOverride(ICorDynamicInfo* pOverride, CORINFO_METHOD_HANDLE currentMethod)
{
    mcs->AddCall("setOverride");
    original_ICorJitInfo->setOverride(pOverride, currentMethod);
}

// Adds an active dependency from the context method's module to the given module
// This is internal callback for the EE. JIT should not call it directly.
void interceptor_ICJI::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo)
{
    mcs->AddCall("addActiveDependency");
    original_ICorJitInfo->addActiveDependency(moduleFrom, moduleTo);
}

CORINFO_METHOD_HANDLE interceptor_ICJI::GetDelegateCtor(CORINFO_METHOD_HANDLE methHnd,
                                                        CORINFO_CLASS_HANDLE  clsHnd,
                                                        CORINFO_METHOD_HANDLE targetMethodHnd,
                                                        DelegateCtorArgs*     pCtorData)
{
    mcs->AddCall("GetDelegateCtor");
    return original_ICorJitInfo->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData);
}

void interceptor_ICJI::MethodCompileComplete(CORINFO_METHOD_HANDLE methHnd)
{
    mcs->AddCall("MethodCompileComplete");
    original_ICorJitInfo->MethodCompileComplete(methHnd);
}

// return a thunk that will copy the arguments for the given signature.
void* interceptor_ICJI::getTailCallCopyArgsThunk(CORINFO_SIG_INFO* pSig, CorInfoHelperTailCallSpecialHandling flags)
{
    mcs->AddCall("getTailCallCopyArgsThunk");
    return original_ICorJitInfo->getTailCallCopyArgsThunk(pSig, flags);
}

// Stuff directly on ICorJitInfo

// Returns extended flags for a particular compilation instance.
DWORD interceptor_ICJI::getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes)
{
    mcs->AddCall("getJitFlags");
    return original_ICorJitInfo->getJitFlags(jitFlags, sizeInBytes);
}

// Runs the given function with the given parameter under an error trap
// and returns true if the function completes successfully. We don't
// record the results of the call: when this call gets played back,
// its result will depend on whether or not `function` calls something
// that throws at playback time rather than at capture time.
bool interceptor_ICJI::runWithErrorTrap(void (*function)(void*), void* param)
{
    mcs->AddCall("runWithErrorTrap");
    return original_ICorJitInfo->runWithErrorTrap(function, param);
}

// return memory manager that the JIT can use to allocate a regular memory
IEEMemoryManager* interceptor_ICJI::getMemoryManager()
{
    mcs->AddCall("getMemoryManager");
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
    mcs->AddCall("allocMem");
    return original_ICorJitInfo->allocMem(hotCodeSize, coldCodeSize, roDataSize, xcptnsCount, flag, hotCodeBlock,
                                          coldCodeBlock, roDataBlock);
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
    mcs->AddCall("reserveUnwindInfo");
    original_ICorJitInfo->reserveUnwindInfo(isFunclet, isColdCode, unwindSize);
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
    mcs->AddCall("allocUnwindInfo");
    original_ICorJitInfo->allocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock,
                                          funcKind);
}

// Get a block of memory needed for the code manager information,
// (the info for enumerating the GC pointers while crawling the
// stack frame).
// Note that allocMem must be called first
void* interceptor_ICJI::allocGCInfo(size_t size /* IN */
                                    )
{
    mcs->AddCall("allocGCInfo");
    return original_ICorJitInfo->allocGCInfo(size);
}

// only used on x64
void interceptor_ICJI::yieldExecution()
{
    mcs->AddCall("yieldExecution");
    original_ICorJitInfo->yieldExecution();
}

// Indicate how many exception handler blocks are to be returned.
// This is guaranteed to be called before any 'setEHinfo' call.
// Note that allocMem must be called before this method can be called.
void interceptor_ICJI::setEHcount(unsigned cEH /* IN */
                                  )
{
    mcs->AddCall("setEHcount");
    original_ICorJitInfo->setEHcount(cEH);
}

// Set the values for one particular exception handler block.
//
// Handler regions should be lexically contiguous.
// This is because FinallyIsUnwinding() uses lexicality to
// determine if a "finally" clause is executing.
void interceptor_ICJI::setEHinfo(unsigned                 EHnumber, /* IN  */
                                 const CORINFO_EH_CLAUSE* clause    /* IN */
                                 )
{
    mcs->AddCall("setEHinfo");
    original_ICorJitInfo->setEHinfo(EHnumber, clause);
}

// Level 1 -> fatalError, Level 2 -> Error, Level 3 -> Warning
// Level 4 means happens 10 times in a run, level 5 means 100, level 6 means 1000 ...
// returns non-zero if the logging succeeded
BOOL interceptor_ICJI::logMsg(unsigned level, const char* fmt, va_list args)
{
    mcs->AddCall("logMsg");
    return original_ICorJitInfo->logMsg(level, fmt, args);
}

// do an assert.  will return true if the code should retry (DebugBreak)
// returns false, if the assert should be igored.
int interceptor_ICJI::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    mcs->AddCall("doAssert");
    return original_ICorJitInfo->doAssert(szFile, iLine, szExpr);
}

void interceptor_ICJI::reportFatalError(CorJitResult result)
{
    mcs->AddCall("reportFatalError");
    original_ICorJitInfo->reportFatalError(result);
}

/*
struct ProfileBuffer  // Also defined here: code:CORBBTPROF_BLOCK_DATA
{
    ULONG ILOffset;
    ULONG ExecutionCount;
};
*/

// allocate a basic block profile buffer where execution counts will be stored
// for jitted basic blocks.
HRESULT interceptor_ICJI::allocBBProfileBuffer(ULONG           count, // The number of basic blocks that we have
                                               ProfileBuffer** profileBuffer)
{
    mcs->AddCall("allocBBProfileBuffer");
    return original_ICorJitInfo->allocBBProfileBuffer(count, profileBuffer);
}

// get profile information to be used for optimizing the current method.  The format
// of the buffer is the same as the format the JIT passes to allocBBProfileBuffer.
HRESULT interceptor_ICJI::getBBProfileData(CORINFO_METHOD_HANDLE ftnHnd,
                                           ULONG*                count, // The number of basic blocks that we have
                                           ProfileBuffer**       profileBuffer,
                                           ULONG*                numRuns)
{
    mcs->AddCall("getBBProfileData");
    return original_ICorJitInfo->getBBProfileData(ftnHnd, count, profileBuffer, numRuns);
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
    mcs->AddCall("recordCallSite");
    return original_ICorJitInfo->recordCallSite(instrOffset, callSig, methodHandle);
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
    mcs->AddCall("recordRelocation");
    original_ICorJitInfo->recordRelocation(location, target, fRelocType, slotNum, addlDelta);
}

WORD interceptor_ICJI::getRelocTypeHint(void* target)
{
    mcs->AddCall("getRelocTypeHint");
    return original_ICorJitInfo->getRelocTypeHint(target);
}

// A callback to identify the range of address known to point to
// compiler-generated native entry points that call back into
// MSIL.
void interceptor_ICJI::getModuleNativeEntryPointRange(void** pStart, /* OUT */
                                                      void** pEnd    /* OUT */
                                                      )
{
    mcs->AddCall("getModuleNativeEntryPointRange");
    original_ICorJitInfo->getModuleNativeEntryPointRange(pStart, pEnd);
}

// For what machine does the VM expect the JIT to generate code? The VM
// returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
// is cross-compiling (such as the case for crossgen), it will return a
// different value than if it was compiling for the host architecture.
//
DWORD interceptor_ICJI::getExpectedTargetArchitecture()
{
    mcs->AddCall("getExpectedTargetArchitecture");
    return original_ICorJitInfo->getExpectedTargetArchitecture();
}
