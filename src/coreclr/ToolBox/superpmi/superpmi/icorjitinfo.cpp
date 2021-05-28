// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "icorjitinfo.h"
#include "jitdebugger.h"
#include "spmiutil.h"

ICorJitInfo* pICJI = nullptr;

ICorJitInfo* InitICorJitInfo(JitInstance* jitInstance)
{
    MyICJI* icji      = new MyICJI();
    icji->jitInstance = jitInstance;
    pICJI             = icji;
    return icji;
}

// Stuff on ICorStaticInfo
/**********************************************************************************/
//
// ICorMethodInfo
//
/**********************************************************************************/

// Quick check whether the method is a jit intrinsic. Returns the same value as getMethodAttribs(ftn) &
// CORINFO_FLG_JIT_INTRINSIC, except faster.
bool MyICJI::isJitIntrinsic(CORINFO_METHOD_HANDLE ftn)
{
    jitInstance->mc->cr->AddCall("isJitIntrinsic");
    return jitInstance->mc->repIsJitIntrinsic(ftn);
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
uint32_t MyICJI::getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */)
{
    jitInstance->mc->cr->AddCall("getMethodAttribs");
    return jitInstance->mc->repGetMethodAttribs(ftn);
}

// sets private JIT flags, which can be, retrieved using getAttrib.
void MyICJI::setMethodAttribs(CORINFO_METHOD_HANDLE     ftn, /* IN */
                              CorInfoMethodRuntimeFlags attribs /* IN */)
{
    jitInstance->mc->cr->AddCall("setMethodAttribs");
    jitInstance->mc->cr->recSetMethodAttribs(ftn, attribs);
}

// Given a method descriptor ftnHnd, extract signature information into sigInfo
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
void MyICJI::getMethodSig(CORINFO_METHOD_HANDLE ftn,         /* IN  */
                          CORINFO_SIG_INFO*     sig,         /* OUT */
                          CORINFO_CLASS_HANDLE  memberParent /* IN */
                          )
{
    jitInstance->mc->cr->AddCall("getMethodSig");
    jitInstance->mc->repGetMethodSig(ftn, sig, memberParent);
}

/*********************************************************************
* Note the following methods can only be used on functions known
* to be IL.  This includes the method being compiled and any method
* that 'getMethodInfo' returns true for
*********************************************************************/

// return information about a method private to the implementation
//      returns false if method is not IL, or is otherwise unavailable.
//      This method is used to fetch data needed to inline functions
bool MyICJI::getMethodInfo(CORINFO_METHOD_HANDLE ftn, /* IN  */
                           CORINFO_METHOD_INFO*  info /* OUT */
                           )
{
    jitInstance->mc->cr->AddCall("getMethodInfo");
    DWORD exceptionCode = 0;
    bool  value         = jitInstance->mc->repGetMethodInfo(ftn, info, &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
    return value;
}

// Decides if you have any limitations for inlining. If everything's OK, it will return
// INLINE_PASS and will fill out pRestrictions with a mask of restrictions the caller of this
// function must respect. If caller passes pRestrictions = nullptr, if there are any restrictions
// INLINE_FAIL will be returned
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
//
// The inlined method need not be verified

CorInfoInline MyICJI::canInline(CORINFO_METHOD_HANDLE callerHnd,    /* IN  */
                                CORINFO_METHOD_HANDLE calleeHnd,    /* IN  */
                                uint32_t*             pRestrictions /* OUT */
                                )
{
    jitInstance->mc->cr->AddCall("canInline");

    DWORD         exceptionCode = 0;
    CorInfoInline result        = jitInstance->mc->repCanInline(callerHnd, calleeHnd, pRestrictions, &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
    return result;
}

// Reports whether or not a method can be inlined, and why.  canInline is responsible for reporting all
// inlining results when it returns INLINE_FAIL and INLINE_NEVER.  All other results are reported by the
// JIT.
void MyICJI::reportInliningDecision(CORINFO_METHOD_HANDLE inlinerHnd,
                                    CORINFO_METHOD_HANDLE inlineeHnd,
                                    CorInfoInline         inlineResult,
                                    const char*           reason)
{
    jitInstance->mc->cr->AddCall("reportInliningDecision");
    jitInstance->mc->cr->recReportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
}

// Returns false if the call is across security boundaries thus we cannot tailcall
//
// The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
bool MyICJI::canTailCall(CORINFO_METHOD_HANDLE callerHnd,         /* IN */
                         CORINFO_METHOD_HANDLE declaredCalleeHnd, /* IN */
                         CORINFO_METHOD_HANDLE exactCalleeHnd,    /* IN */
                         bool                  fIsTailPrefix      /* IN */
                         )
{
    jitInstance->mc->cr->AddCall("canTailCall");
    return jitInstance->mc->repCanTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
}

// Reports whether or not a method can be tail called, and why.
// canTailCall is responsible for reporting all results when it returns
// false.  All other results are reported by the JIT.
void MyICJI::reportTailCallDecision(CORINFO_METHOD_HANDLE callerHnd,
                                    CORINFO_METHOD_HANDLE calleeHnd,
                                    bool                  fIsTailPrefix,
                                    CorInfoTailCall       tailCallResult,
                                    const char*           reason)
{
    jitInstance->mc->cr->AddCall("reportTailCallDecision");
    jitInstance->mc->cr->recReportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
}

// get individual exception handler
void MyICJI::getEHinfo(CORINFO_METHOD_HANDLE ftn,      /* IN  */
                       unsigned              EHnumber, /* IN */
                       CORINFO_EH_CLAUSE*    clause    /* OUT */
                       )
{
    jitInstance->mc->cr->AddCall("getEHinfo");
    jitInstance->mc->repGetEHinfo(ftn, EHnumber, clause);
}

// return class it belongs to
CORINFO_CLASS_HANDLE MyICJI::getMethodClass(CORINFO_METHOD_HANDLE method)
{
    jitInstance->mc->cr->AddCall("getMethodClass");
    return jitInstance->mc->repGetMethodClass(method);
}

// return module it belongs to
CORINFO_MODULE_HANDLE MyICJI::getMethodModule(CORINFO_METHOD_HANDLE method)
{
    jitInstance->mc->cr->AddCall("getMethodModule");
    return jitInstance->mc->repGetMethodModule(method);
}

// This function returns the offset of the specified method in the
// vtable of it's owning class or interface.
void MyICJI::getMethodVTableOffset(CORINFO_METHOD_HANDLE method,                 /* IN */
                                   unsigned*             offsetOfIndirection,    /* OUT */
                                   unsigned*             offsetAfterIndirection, /* OUT */
                                   bool*                 isRelative              /* OUT */
                                   )
{
    jitInstance->mc->cr->AddCall("getMethodVTableOffset");
    jitInstance->mc->repGetMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
}

bool MyICJI::resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO * info)
{
    jitInstance->mc->cr->AddCall("resolveVirtualMethod");
    bool result = jitInstance->mc->repResolveVirtualMethod(info);
    return result;
}

// Get the unboxed entry point for a method, if possible.
CORINFO_METHOD_HANDLE MyICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg)
{
    jitInstance->mc->cr->AddCall("getUnboxedEntry");
    CORINFO_METHOD_HANDLE result = jitInstance->mc->repGetUnboxedEntry(ftn, requiresInstMethodTableArg);
    return result;
}

// Given T, return the type of the default Comparer<T>.
// Returns null if the type can't be determined exactly.
CORINFO_CLASS_HANDLE MyICJI::getDefaultComparerClass(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getDefaultComparerClass");
    CORINFO_CLASS_HANDLE result = jitInstance->mc->repGetDefaultComparerClass(cls);
    return result;
}

// Given T, return the type of the default EqualityComparer<T>.
// Returns null if the type can't be determined exactly.
CORINFO_CLASS_HANDLE MyICJI::getDefaultEqualityComparerClass(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getDefaultEqualityComparerClass");
    CORINFO_CLASS_HANDLE result = jitInstance->mc->repGetDefaultEqualityComparerClass(cls);
    return result;
}

void MyICJI::expandRawHandleIntrinsic(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_GENERICHANDLE_RESULT* pResult)
{
    jitInstance->mc->cr->AddCall("expandRawHandleIntrinsic");
    LogError("Hit unimplemented expandRawHandleIntrinsic");
    DebugBreakorAV(129);
}

// If a method's attributes have (getMethodAttribs) CORINFO_FLG_INTRINSIC set,
// getIntrinsicID() returns the intrinsic ID.
CorInfoIntrinsics MyICJI::getIntrinsicID(CORINFO_METHOD_HANDLE method, bool* pMustExpand /* OUT */
                                         )
{
    jitInstance->mc->cr->AddCall("getIntrinsicID");
    return jitInstance->mc->repGetIntrinsicID(method, pMustExpand);
}

// Is the given type in System.Private.Corelib and marked with IntrinsicAttribute?
bool MyICJI::isIntrinsicType(CORINFO_CLASS_HANDLE classHnd)
{
    jitInstance->mc->cr->AddCall("isIntrinsicType");
    return jitInstance->mc->repIsIntrinsicType(classHnd) ? true : false;
}

// return the entry point calling convention for any of the following
// - a P/Invoke
// - a method marked with UnmanagedCallersOnly
// - a function pointer with the CORINFO_CALLCONV_UNMANAGED calling convention.
CorInfoCallConvExtension MyICJI::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig, bool* pSuppressGCTransition)
{
    jitInstance->mc->cr->AddCall("getUnmanagedCallConv");
    return jitInstance->mc->repGetUnmanagedCallConv(method, callSiteSig, pSuppressGCTransition);
}

// return if any marshaling is required for PInvoke methods.  Note that
// method == 0 => calli.  The call site sig is only needed for the varargs or calli case
bool MyICJI::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    jitInstance->mc->cr->AddCall("pInvokeMarshalingRequired");
    return jitInstance->mc->repPInvokeMarshalingRequired(method, callSiteSig);
}

// Check constraints on method type arguments (only).
// The parent class should be checked separately using satisfiesClassConstraints(parent).
bool MyICJI::satisfiesMethodConstraints(CORINFO_CLASS_HANDLE  parent, // the exact parent of the method
                                        CORINFO_METHOD_HANDLE method)
{
    jitInstance->mc->cr->AddCall("satisfiesMethodConstraints");
    return jitInstance->mc->repSatisfiesMethodConstraints(parent, method);
}

// Given a delegate target class, a target method parent class,  a  target method,
// a delegate class, check if the method signature is compatible with the Invoke method of the delegate
// (under the typical instantiation of any free type variables in the memberref signatures).
bool MyICJI::isCompatibleDelegate(CORINFO_CLASS_HANDLE  objCls,          /* type of the delegate target, if any */
                                  CORINFO_CLASS_HANDLE  methodParentCls, /* exact parent of the target method, if any */
                                  CORINFO_METHOD_HANDLE method,          /* (representative) target method, if any */
                                  CORINFO_CLASS_HANDLE  delegateCls,     /* exact type of the delegate */
                                  bool*                 pfIsOpenDelegate /* is the delegate open */
                                  )
{
    jitInstance->mc->cr->AddCall("isCompatibleDelegate");
    return jitInstance->mc->repIsCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate);
}

// load and restore the method
void MyICJI::methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE method)
{
    jitInstance->mc->cr->AddCall("methodMustBeLoadedBeforeCodeIsRun");
    jitInstance->mc->cr->recMethodMustBeLoadedBeforeCodeIsRun(method);
}

CORINFO_METHOD_HANDLE MyICJI::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE method)
{
    jitInstance->mc->cr->AddCall("mapMethodDeclToMethodImpl");
    LogError("Hit unimplemented mapMethodDeclToMethodImpl");
    DebugBreakorAV(17);
    return 0;
}

// Returns the global cookie for the /GS unsafe buffer checks
// The cookie might be a constant value (JIT), or a handle to memory location (Ngen)
void MyICJI::getGSCookie(GSCookie*  pCookieVal, // OUT
                         GSCookie** ppCookieVal // OUT
                         )
{
    jitInstance->mc->cr->AddCall("getGSCookie");
    jitInstance->mc->repGetGSCookie(pCookieVal, ppCookieVal);
}

// Provide patchpoint info for the method currently being jitted.
void MyICJI::setPatchpointInfo(PatchpointInfo* patchpointInfo)
{
    jitInstance->mc->cr->AddCall("setPatchpointInfo");
    jitInstance->mc->cr->recSetPatchpointInfo(patchpointInfo);
    freeArray(patchpointInfo); // See note in recSetPatchpointInfo... we own destroying this array
}

// Get OSR info for the method currently being jitted
PatchpointInfo* MyICJI::getOSRInfo(unsigned* ilOffset)
{
    jitInstance->mc->cr->AddCall("getOSRInfo");
    return jitInstance->mc->repGetOSRInfo(ilOffset);
}

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/

// Resolve metadata token into runtime method handles.
void MyICJI::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    DWORD exceptionCode = 0;
    jitInstance->mc->cr->AddCall("resolveToken");
    jitInstance->mc->repResolveToken(pResolvedToken, &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
}

// Resolve metadata token into runtime method handles.
bool MyICJI::tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    jitInstance->mc->cr->AddCall("tryResolveToken");
    return jitInstance->mc->repTryResolveToken(pResolvedToken);
}

// Signature information about the call sig
void MyICJI::findSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                     unsigned               sigTOK,  /* IN */
                     CORINFO_CONTEXT_HANDLE context, /* IN */
                     CORINFO_SIG_INFO*      sig      /* OUT */
                     )
{
    jitInstance->mc->cr->AddCall("findSig");
    jitInstance->mc->repFindSig(module, sigTOK, context, sig);
}

// for Varargs, the signature at the call site may differ from
// the signature at the definition.  Thus we need a way of
// fetching the call site information
void MyICJI::findCallSiteSig(CORINFO_MODULE_HANDLE  module,  /* IN */
                             unsigned               methTOK, /* IN */
                             CORINFO_CONTEXT_HANDLE context, /* IN */
                             CORINFO_SIG_INFO*      sig      /* OUT */
                             )
{
    jitInstance->mc->cr->AddCall("findCallSiteSig");
    jitInstance->mc->repFindCallSiteSig(module, methTOK, context, sig);
}

CORINFO_CLASS_HANDLE MyICJI::getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken /* IN  */)
{
    jitInstance->mc->cr->AddCall("getTokenTypeAsHandle");
    return jitInstance->mc->repGetTokenTypeAsHandle(pResolvedToken);
}

// Checks if the given metadata token is valid
bool MyICJI::isValidToken(CORINFO_MODULE_HANDLE module, /* IN  */
                          unsigned              metaTOK /* IN  */
                          )
{
    jitInstance->mc->cr->AddCall("isValidToken");
    return jitInstance->mc->repIsValidToken(module, metaTOK);
}

// Checks if the given metadata token is valid StringRef
bool MyICJI::isValidStringRef(CORINFO_MODULE_HANDLE module, /* IN  */
                              unsigned              metaTOK /* IN  */
                              )
{
    jitInstance->mc->cr->AddCall("isValidStringRef");
    return jitInstance->mc->repIsValidStringRef(module, metaTOK);
}

const char16_t* MyICJI::getStringLiteral(CORINFO_MODULE_HANDLE module,  /* IN  */
                                         unsigned              metaTOK, /* IN  */
                                         int*                  length   /* OUT */
                                         )
{
    jitInstance->mc->cr->AddCall("getStringLiteral");
    return jitInstance->mc->repGetStringLiteral(module, metaTOK, length);
}

/**********************************************************************************/
//
// ICorClassInfo
//
/**********************************************************************************/

// If the value class 'cls' is isomorphic to a primitive type it will
// return that type, otherwise it will return CORINFO_TYPE_VALUECLASS
CorInfoType MyICJI::asCorInfoType(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("asCorInfoType");
    return jitInstance->mc->repAsCorInfoType(cls);
}

// for completeness
const char* MyICJI::getClassName(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getClassName");
    const char* result = jitInstance->mc->repGetClassName(cls);
    return result;
}

const char* MyICJI::getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
{
    jitInstance->mc->cr->AddCall("getClassNameFromMetadata");
    const char* result = jitInstance->mc->repGetClassNameFromMetadata(cls, namespaceName);
    return result;
}

CORINFO_CLASS_HANDLE MyICJI::getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
{
    jitInstance->mc->cr->AddCall("getTypeInstantiationArgument");
    CORINFO_CLASS_HANDLE result = jitInstance->mc->repGetTypeInstantiationArgument(cls, index);
    return result;
}

// Append a (possibly truncated) representation of the type cls to the preallocated buffer ppBuf of length pnBufLen
// If fNamespace=TRUE, include the namespace/enclosing classes
// If fFullInst=TRUE (regardless of fNamespace and fAssembly), include namespace and assembly for any type parameters
// If fAssembly=TRUE, suffix with a comma and the full assembly qualification
// return size of representation
int MyICJI::appendClassName(__deref_inout_ecount(*pnBufLen) char16_t** ppBuf,
                            int*                                    pnBufLen,
                            CORINFO_CLASS_HANDLE                    cls,
                            bool                                    fNamespace,
                            bool                                    fFullInst,
                            bool                                    fAssembly)
{
    jitInstance->mc->cr->AddCall("appendClassName");
    const WCHAR* result = jitInstance->mc->repAppendClassName(cls, fNamespace, fFullInst, fAssembly);
    int          nLen   = 0;
    if (ppBuf != nullptr && result != nullptr)
    {
        nLen = (int)wcslen(result);
        if (*pnBufLen > nLen)
        {
            wcscpy_s((WCHAR*)*ppBuf, *pnBufLen, result);
            (*ppBuf) += nLen;
            (*pnBufLen) -= nLen;
        }
    }
    return nLen;
}

// Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) &
// CORINFO_FLG_VALUECLASS, except faster.
bool MyICJI::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("isValueClass");
    return jitInstance->mc->repIsValueClass(cls);
}

// Decides how the JIT should do the optimization to inline the check for
//     GetTypeFromHandle(handle) == obj.GetType() (for CORINFO_INLINE_TYPECHECK_SOURCE_VTABLE)
//     GetTypeFromHandle(X) == GetTypeFromHandle(Y) (for CORINFO_INLINE_TYPECHECK_SOURCE_TOKEN)
CorInfoInlineTypeCheck MyICJI::canInlineTypeCheck(CORINFO_CLASS_HANDLE cls, CorInfoInlineTypeCheckSource source)
{
    jitInstance->mc->cr->AddCall("canInlineTypeCheck");
    return jitInstance->mc->repCanInlineTypeCheck(cls, source);
}

// return flags (defined above, CORINFO_FLG_PUBLIC ...)
uint32_t MyICJI::getClassAttribs(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getClassAttribs");
    return jitInstance->mc->repGetClassAttribs(cls);
}

// Returns "TRUE" iff "cls" is a struct type such that return buffers used for returning a value
// of this type must be stack-allocated.  This will generally be true only if the struct
// contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
// an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
// returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
// buffers do not require GC write barriers.
bool MyICJI::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("isStructRequiringStackAllocRetBuf");
    return jitInstance->mc->repIsStructRequiringStackAllocRetBuf(cls);
}

CORINFO_MODULE_HANDLE MyICJI::getClassModule(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getClassModule");
    return jitInstance->mc->repGetClassModule(cls);
}

// Returns the assembly that contains the module "mod".
CORINFO_ASSEMBLY_HANDLE MyICJI::getModuleAssembly(CORINFO_MODULE_HANDLE mod)
{
    jitInstance->mc->cr->AddCall("getModuleAssembly");
    return jitInstance->mc->repGetModuleAssembly(mod);
}

// Returns the name of the assembly "assem".
const char* MyICJI::getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem)
{
    jitInstance->mc->cr->AddCall("getAssemblyName");
    return jitInstance->mc->repGetAssemblyName(assem);
}

// Allocate and delete process-lifetime objects.  Should only be
// referred to from static fields, lest a leak occur.
// Note that "LongLifetimeFree" does not execute destructors, if "obj"
// is an array of a struct type with a destructor.
void* MyICJI::LongLifetimeMalloc(size_t sz)
{
    jitInstance->mc->cr->AddCall("LongLifetimeMalloc");
    LogError("Hit unimplemented LongLifetimeMalloc");
    DebugBreakorAV(32);
    return 0;
}

void MyICJI::LongLifetimeFree(void* obj)
{
    jitInstance->mc->cr->AddCall("LongLifetimeFree");
    LogError("Hit unimplemented LongLifetimeFree");
    DebugBreakorAV(33);
}

size_t MyICJI::getClassModuleIdForStatics(CORINFO_CLASS_HANDLE   cls,
                                          CORINFO_MODULE_HANDLE* pModule,
                                          void**                 ppIndirection)
{
    jitInstance->mc->cr->AddCall("getClassModuleIdForStatics");
    return jitInstance->mc->repGetClassModuleIdForStatics(cls, pModule, ppIndirection);
}

// return the number of bytes needed by an instance of the class
unsigned MyICJI::getClassSize(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getClassSize");
    return jitInstance->mc->repGetClassSize(cls);
}

// return the number of bytes needed by an instance of the class allocated on the heap
unsigned MyICJI::getHeapClassSize(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getHeapClassSize");
    return jitInstance->mc->repGetHeapClassSize(cls);
}

bool MyICJI::canAllocateOnStack(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("canAllocateOnStack");
    return jitInstance->mc->repCanAllocateOnStack(cls);
}

unsigned MyICJI::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, bool fDoubleAlignHint)
{
    jitInstance->mc->cr->AddCall("getClassAlignmentRequirement");
    return jitInstance->mc->repGetClassAlignmentRequirement(cls, fDoubleAlignHint);
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
unsigned MyICJI::getClassGClayout(CORINFO_CLASS_HANDLE cls,   /* IN */
                                  BYTE*                gcPtrs /* OUT */
                                  )
{
    jitInstance->mc->cr->AddCall("getClassGClayout");
    return jitInstance->mc->repGetClassGClayout(cls, gcPtrs);
}

// returns the number of instance fields in a class
unsigned MyICJI::getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls /* IN */
                                           )
{
    jitInstance->mc->cr->AddCall("getClassNumInstanceFields");
    return jitInstance->mc->repGetClassNumInstanceFields(cls);
}

CORINFO_FIELD_HANDLE MyICJI::getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num)
{
    jitInstance->mc->cr->AddCall("getFieldInClass");
    return jitInstance->mc->repGetFieldInClass(clsHnd, num);
}

bool MyICJI::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, bool fOptional)
{
    jitInstance->mc->cr->AddCall("checkMethodModifier");
    bool result = jitInstance->mc->repCheckMethodModifier(hMethod, modifier, fOptional);
    return result;
}

// returns the "NEW" helper optimized for "newCls."
CorInfoHelpFunc MyICJI::getNewHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_METHOD_HANDLE callerHandle, bool * pHasSideEffects)
{
    jitInstance->mc->cr->AddCall("getNewHelper");
    return jitInstance->mc->repGetNewHelper(pResolvedToken, callerHandle, pHasSideEffects);
}

// returns the newArr (1-Dim array) helper optimized for "arrayCls."
CorInfoHelpFunc MyICJI::getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls)
{
    jitInstance->mc->cr->AddCall("getNewArrHelper");
    return jitInstance->mc->repGetNewArrHelper(arrayCls);
}

// returns the optimized "IsInstanceOf" or "ChkCast" helper
CorInfoHelpFunc MyICJI::getCastingHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fThrowing)
{
    jitInstance->mc->cr->AddCall("getCastingHelper");
    return jitInstance->mc->repGetCastingHelper(pResolvedToken, fThrowing);
}

// returns helper to trigger static constructor
CorInfoHelpFunc MyICJI::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    jitInstance->mc->cr->AddCall("getSharedCCtorHelper");
    return jitInstance->mc->repGetSharedCCtorHelper(clsHnd);
}

// This is not pretty.  Boxing nullable<T> actually returns
// a boxed<T> not a boxed Nullable<T>.  This call allows the verifier
// to call back to the EE on the 'box' instruction and get the transformed
// type to use for verification.
CORINFO_CLASS_HANDLE MyICJI::getTypeForBox(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getTypeForBox");
    return jitInstance->mc->repGetTypeForBox(cls);
}

// returns the correct box helper for a particular class.  Note
// that if this returns CORINFO_HELP_BOX, the JIT can assume
// 'standard' boxing (allocate object and copy), and optimize
CorInfoHelpFunc MyICJI::getBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getBoxHelper");
    return jitInstance->mc->repGetBoxHelper(cls);
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

CorInfoHelpFunc MyICJI::getUnBoxHelper(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getUnBoxHelper");
    CorInfoHelpFunc result = jitInstance->mc->repGetUnBoxHelper(cls);
    return result;
}

bool MyICJI::getReadyToRunHelper(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                 CORINFO_LOOKUP_KIND*    pGenericLookupKind,
                                 CorInfoHelpFunc         id,
                                 CORINFO_CONST_LOOKUP*   pLookup)
{
    jitInstance->mc->cr->AddCall("getReadyToRunHelper");
    return jitInstance->mc->repGetReadyToRunHelper(pResolvedToken, pGenericLookupKind, id, pLookup);
}

void MyICJI::getReadyToRunDelegateCtorHelper(CORINFO_RESOLVED_TOKEN* pTargetMethod,
                                             CORINFO_CLASS_HANDLE    delegateType,
                                             CORINFO_LOOKUP*         pLookup)
{
    jitInstance->mc->cr->AddCall("getReadyToRunDelegateCtorHelper");
    jitInstance->mc->repGetReadyToRunDelegateCtorHelper(pTargetMethod, delegateType, pLookup);
}

const char* MyICJI::getHelperName(CorInfoHelpFunc funcNum)
{
    jitInstance->mc->cr->AddCall("getHelperName");
    return jitInstance->mc->repGetHelperName(funcNum);
}

// This function tries to initialize the class (run the class constructor).
// this function returns whether the JIT must insert helper calls before
// accessing static field or method.
//
// See code:ICorClassInfo#ClassConstruction.
CorInfoInitClassResult MyICJI::initClass(CORINFO_FIELD_HANDLE field, // Non-nullptr - inquire about cctor trigger before
                                                                     // static field access nullptr - inquire about
                                                                     // cctor trigger in method prolog
                                         CORINFO_METHOD_HANDLE  method,     // Method referencing the field or prolog
                                         CORINFO_CONTEXT_HANDLE context     // Exact context of method
                                         )
{
    jitInstance->mc->cr->AddCall("initClass");
    return jitInstance->mc->repInitClass(field, method, context);
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
void MyICJI::classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("classMustBeLoadedBeforeCodeIsRun");
    jitInstance->mc->cr->recClassMustBeLoadedBeforeCodeIsRun(cls);
}

// returns the class handle for the special builtin classes
CORINFO_CLASS_HANDLE MyICJI::getBuiltinClass(CorInfoClassId classId)
{
    jitInstance->mc->cr->AddCall("getBuiltinClass");
    return jitInstance->mc->repGetBuiltinClass(classId);
}

// "System.Int32" ==> CORINFO_TYPE_INT..
CorInfoType MyICJI::getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getTypeForPrimitiveValueClass");
    return jitInstance->mc->repGetTypeForPrimitiveValueClass(cls);
}

// "System.Int32" ==> CORINFO_TYPE_INT..
// "System.UInt32" ==> CORINFO_TYPE_UINT..
CorInfoType MyICJI::getTypeForPrimitiveNumericClass(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getTypeForPrimitiveNumericClass");
    return jitInstance->mc->repGetTypeForPrimitiveNumericClass(cls);
}

// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
bool MyICJI::canCast(CORINFO_CLASS_HANDLE child, // subtype (extends parent)
                     CORINFO_CLASS_HANDLE parent // base type
                     )
{
    jitInstance->mc->cr->AddCall("canCast");
    return jitInstance->mc->repCanCast(child, parent);
}

// TRUE if cls1 and cls2 are considered equivalent types.
bool MyICJI::areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    jitInstance->mc->cr->AddCall("areTypesEquivalent");
    return jitInstance->mc->repAreTypesEquivalent(cls1, cls2);
}

// See if a cast from fromClass to toClass will succeed, fail, or needs
// to be resolved at runtime.
TypeCompareState MyICJI::compareTypesForCast(CORINFO_CLASS_HANDLE fromClass, CORINFO_CLASS_HANDLE toClass)
{
    jitInstance->mc->cr->AddCall("compareTypesForCast");
    return jitInstance->mc->repCompareTypesForCast(fromClass, toClass);
}

// See if types represented by cls1 and cls2 compare equal, not
// equal, or the comparison needs to be resolved at runtime.
TypeCompareState MyICJI::compareTypesForEquality(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    jitInstance->mc->cr->AddCall("compareTypesForEquality");
    return jitInstance->mc->repCompareTypesForEquality(cls1, cls2);
}

// returns the intersection of cls1 and cls2.
CORINFO_CLASS_HANDLE MyICJI::mergeClasses(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    jitInstance->mc->cr->AddCall("mergeClasses");
    return jitInstance->mc->repMergeClasses(cls1, cls2);
}

// Returns true if cls2 is known to be a more specific type than cls1
bool MyICJI::isMoreSpecificType(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2)
{
    jitInstance->mc->cr->AddCall("isMoreSpecificType");
    return jitInstance->mc->repIsMoreSpecificType(cls1, cls2);
}

// Given a class handle, returns the Parent type.
// For COMObjectType, it returns Class Handle of System.Object.
// Returns 0 if System.Object is passed in.
CORINFO_CLASS_HANDLE MyICJI::getParentType(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getParentType");
    return jitInstance->mc->repGetParentType(cls);
}

// Returns the CorInfoType of the "child type". If the child type is
// not a primitive type, *clsRet will be set.
// Given an Array of Type Foo, returns Foo.
// Given BYREF Foo, returns Foo
CorInfoType MyICJI::getChildType(CORINFO_CLASS_HANDLE clsHnd, CORINFO_CLASS_HANDLE* clsRet)
{
    jitInstance->mc->cr->AddCall("getChildType");
    return jitInstance->mc->repGetChildType(clsHnd, clsRet);
}

// Check constraints on type arguments of this class and parent classes
bool MyICJI::satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("satisfiesClassConstraints");
    return jitInstance->mc->repSatisfiesClassConstraints(cls);
}

// Check if this is a single dimensional array type
bool MyICJI::isSDArray(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("isSDArray");
    return jitInstance->mc->repIsSDArray(cls);
}

// Get the numbmer of dimensions in an array
unsigned MyICJI::getArrayRank(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("getArrayRank");
    return jitInstance->mc->repGetArrayRank(cls);
}

// Get static field data for an array
void* MyICJI::getArrayInitializationData(CORINFO_FIELD_HANDLE field, uint32_t size)
{
    jitInstance->mc->cr->AddCall("getArrayInitializationData");
    return jitInstance->mc->repGetArrayInitializationData(field, size);
}

// Check Visibility rules.
CorInfoIsAccessAllowedResult MyICJI::canAccessClass(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                                    CORINFO_METHOD_HANDLE   callerHandle,
                                                    CORINFO_HELPER_DESC*    pAccessHelper /* If canAccessMethod returns
                                                                                             something other    than
                                                                                             ALLOWED,
                                                                                             then this is filled in. */
                                                    )
{
    jitInstance->mc->cr->AddCall("canAccessClass");
    return jitInstance->mc->repCanAccessClass(pResolvedToken, callerHandle, pAccessHelper);
}

/**********************************************************************************/
//
// ICorFieldInfo
//
/**********************************************************************************/

// this function is for debugging only.  It returns the field name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* MyICJI::getFieldName(CORINFO_FIELD_HANDLE ftn,       /* IN */
                                 const char**         moduleName /* OUT */
                                 )
{
    jitInstance->mc->cr->AddCall("getFieldName");
    return jitInstance->mc->repGetFieldName(ftn, moduleName);
}

// return class it belongs to
CORINFO_CLASS_HANDLE MyICJI::getFieldClass(CORINFO_FIELD_HANDLE field)
{
    jitInstance->mc->cr->AddCall("getFieldClass");
    return jitInstance->mc->repGetFieldClass(field);
}

// Return the field's type, if it is CORINFO_TYPE_VALUECLASS 'structType' is set
// the field's value class (if 'structType' == 0, then don't bother
// the structure info).
//
// 'memberParent' is typically only set when verifying.  It should be the
// result of calling getMemberParent.
CorInfoType MyICJI::getFieldType(CORINFO_FIELD_HANDLE  field,
                                 CORINFO_CLASS_HANDLE* structType,
                                 CORINFO_CLASS_HANDLE  memberParent /* IN */
                                 )
{
    jitInstance->mc->cr->AddCall("getFieldType");
    return jitInstance->mc->repGetFieldType(field, structType, memberParent);
}

// return the data member's instance offset
unsigned MyICJI::getFieldOffset(CORINFO_FIELD_HANDLE field)
{
    jitInstance->mc->cr->AddCall("getFieldOffset");
    return jitInstance->mc->repGetFieldOffset(field);
}

void MyICJI::getFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                          CORINFO_METHOD_HANDLE   callerHandle,
                          CORINFO_ACCESS_FLAGS    flags,
                          CORINFO_FIELD_INFO*     pResult)
{
    jitInstance->mc->cr->AddCall("getFieldInfo");
    jitInstance->mc->repGetFieldInfo(pResolvedToken, callerHandle, flags, pResult);
}

// Returns true iff "fldHnd" represents a static field.
bool MyICJI::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    jitInstance->mc->cr->AddCall("isFieldStatic");
    return jitInstance->mc->repIsFieldStatic(fldHnd);
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
void MyICJI::getBoundaries(CORINFO_METHOD_HANDLE ftn,                      // [IN] method of interest
                           unsigned int*         cILOffsets,               // [OUT] size of pILOffsets
                           uint32_t**            pILOffsets,               // [OUT] IL offsets of interest
                                                                           //       jit MUST free with freeArray!
                           ICorDebugInfo::BoundaryTypes* implicitBoundaries // [OUT] tell jit, all boundaries of this type
                           )
{
    jitInstance->mc->cr->AddCall("getBoundaries");
    jitInstance->mc->repGetBoundaries(ftn, cILOffsets, pILOffsets, implicitBoundaries);

    // The JIT will want to call freearray on the array we pass back, so move the data into a form that complies with
    // this
    if (*cILOffsets > 0)
    {
        uint32_t* realOffsets = (uint32_t*)allocateArray(*cILOffsets * sizeof(ICorDebugInfo::BoundaryTypes));
        memcpy(realOffsets, *pILOffsets, *cILOffsets * sizeof(ICorDebugInfo::BoundaryTypes));
        *pILOffsets = realOffsets;
    }
    else
        *pILOffsets = 0;
}

// Report back the mapping from IL to native code,
// this map should include all boundaries that 'getBoundaries'
// reported as interesting to the debugger.

// Note that debugger (and profiler) is assuming that all of the
// offsets form a contiguous block of memory, and that the
// OffsetMapping is sorted in order of increasing native offset.
void MyICJI::setBoundaries(CORINFO_METHOD_HANDLE         ftn,  // [IN] method of interest
                           ULONG32                       cMap, // [IN] size of pMap
                           ICorDebugInfo::OffsetMapping* pMap  // [IN] map including all points of interest.
                                                               //      jit allocated with allocateArray, EE frees
                           )
{
    jitInstance->mc->cr->AddCall("setBoundaries");
    jitInstance->mc->cr->recSetBoundaries(ftn, cMap, pMap);

    freeArray(pMap); // see note in recSetBoundaries... we own this array and own destroying it.
}

// Query the EE to find out the scope of local varables.
// normally the JIT would trash variables after last use, but
// under debugging, the JIT needs to keep them live over their
// entire scope so that they can be inspected.
//
// Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
// be used only as a hint and the native compiler should not change its
// code generation.
void MyICJI::getVars(CORINFO_METHOD_HANDLE      ftn,   // [IN]  method of interest
                     ULONG32*                   cVars, // [OUT] size of 'vars'
                     ICorDebugInfo::ILVarInfo** vars,  // [OUT] scopes of variables of interest
                                                       //       jit MUST free with freeArray!
                     bool* extendOthers                // [OUT] it TRUE, then assume the scope
                                                       //       of unmentioned vars is entire method
                     )
{
    jitInstance->mc->cr->AddCall("getVars");
    jitInstance->mc->repGetVars(ftn, cVars, vars, extendOthers);

    // The JIT will want to call freearray on the array we pass back, so move the data into a form that complies with
    // this
    if (*cVars > 0)
    {
        ICorDebugInfo::ILVarInfo* realOffsets =
            (ICorDebugInfo::ILVarInfo*)allocateArray(*cVars * sizeof(ICorDebugInfo::ILVarInfo));
        memcpy(realOffsets, *vars, *cVars * sizeof(ICorDebugInfo::ILVarInfo));
        *vars = realOffsets;
    }
    else
        *vars = nullptr;
}

// Report back to the EE the location of every variable.
// note that the JIT might split lifetimes into different
// locations etc.

void MyICJI::setVars(CORINFO_METHOD_HANDLE         ftn,   // [IN] method of interest
                     ULONG32                       cVars, // [IN] size of 'vars'
                     ICorDebugInfo::NativeVarInfo* vars   // [IN] map telling where local vars are stored at what points
                                                          //      jit allocated with allocateArray, EE frees
                     )
{
    jitInstance->mc->cr->AddCall("setVars");
    jitInstance->mc->cr->recSetVars(ftn, cVars, vars);
    freeArray(vars); // See note in recSetVars... we own destroying this array
}

/*-------------------------- Misc ---------------------------------------*/

// Used to allocate memory that needs to handed to the EE.
// For eg, use this to allocated memory for reporting debug info,
// which will be handed to the EE by setVars() and setBoundaries()
void* MyICJI::allocateArray(size_t cBytes)
{
    return jitInstance->allocateArray(cBytes);
}

// JitCompiler will free arrays passed by the EE using this
// For eg, The EE returns memory in getVars() and getBoundaries()
// to the JitCompiler, which the JitCompiler should release using
// freeArray()
void MyICJI::freeArray(void* array)
{
    jitInstance->freeArray(array);
}

/*********************************************************************************/
//
// ICorArgInfo
//
/*********************************************************************************/

// advance the pointer to the argument list.
// a ptr of 0, is special and always means the first argument
CORINFO_ARG_LIST_HANDLE MyICJI::getArgNext(CORINFO_ARG_LIST_HANDLE args /* IN */
                                           )
{
    jitInstance->mc->cr->AddCall("getArgNext");
    return jitInstance->mc->repGetArgNext(args);
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
CorInfoTypeWithMod MyICJI::getArgType(CORINFO_SIG_INFO*       sig,      /* IN */
                                      CORINFO_ARG_LIST_HANDLE args,     /* IN */
                                      CORINFO_CLASS_HANDLE*   vcTypeRet /* OUT */
                                      )
{
    DWORD exceptionCode = 0;
    jitInstance->mc->cr->AddCall("getArgType");
    CorInfoTypeWithMod value = jitInstance->mc->repGetArgType(sig, args, vcTypeRet, &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
    return value;
}

// If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
CORINFO_CLASS_HANDLE MyICJI::getArgClass(CORINFO_SIG_INFO*       sig, /* IN */
                                         CORINFO_ARG_LIST_HANDLE args /* IN */
                                         )
{
    DWORD exceptionCode = 0;
    jitInstance->mc->cr->AddCall("getArgClass");
    CORINFO_CLASS_HANDLE value = jitInstance->mc->repGetArgClass(sig, args, &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
    return value;
}

// Returns type of HFA for valuetype
CorInfoHFAElemType MyICJI::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    jitInstance->mc->cr->AddCall("getHFAType");
    CorInfoHFAElemType value = jitInstance->mc->repGetHFAType(hClass);
    return value;
}

/*****************************************************************************
* ICorErrorInfo contains methods to deal with SEH exceptions being thrown
* from the corinfo interface.  These methods may be called when an exception
* with code EXCEPTION_COMPLUS is caught.
*****************************************************************************/

// Returns the HRESULT of the current exception
HRESULT MyICJI::GetErrorHRESULT(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    jitInstance->mc->cr->AddCall("GetErrorHRESULT");
    LogError("Hit unimplemented GetErrorHRESULT");
    DebugBreakorAV(76);
    return 0;
}

// Fetches the message of the current exception
// Returns the size of the message (including terminating null). This can be
// greater than bufferLength if the buffer is insufficient.
uint32_t MyICJI::GetErrorMessage(__inout_ecount(bufferLength) char16_t* buffer, uint32_t bufferLength)
{
    jitInstance->mc->cr->AddCall("GetErrorMessage");
    LogError("Hit unimplemented GetErrorMessage");
    DebugBreakorAV(77);
    return 0;
}

// returns EXCEPTION_EXECUTE_HANDLER if it is OK for the compile to handle the
//                        exception, abort some work (like the inlining) and continue compilation
// returns EXCEPTION_CONTINUE_SEARCH if exception must always be handled by the EE
//                    things like ThreadStoppedException ...
// returns EXCEPTION_CONTINUE_EXECUTION if exception is fixed up by the EE

int MyICJI::FilterException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    jitInstance->mc->cr->AddCall("FilterException");
    int result = jitInstance->mc->repFilterException(pExceptionPointers);
    return result;
}

// Cleans up internal EE tracking when an exception is caught.
void MyICJI::HandleException(struct _EXCEPTION_POINTERS* pExceptionPointers)
{
    jitInstance->mc->cr->AddCall("HandleException");
}

void MyICJI::ThrowExceptionForJitResult(HRESULT result)
{
    jitInstance->mc->cr->AddCall("ThrowExceptionForJitResult");
    LogError("Hit unimplemented ThrowExceptionForJitResult");
    DebugBreakorAV(80);
}

// Throws an exception defined by the given throw helper.
void MyICJI::ThrowExceptionForHelper(const CORINFO_HELPER_DESC* throwHelper)
{
    jitInstance->mc->cr->AddCall("ThrowExceptionForHelper");
    LogError("Hit unimplemented ThrowExceptionForHelper");
    DebugBreakorAV(81);
}

/*****************************************************************************
 * ICorStaticInfo contains EE interface methods which return values that are
 * constant from invocation to invocation.  Thus they may be embedded in
 * persisted information like statically generated code. (This is of course
 * assuming that all code versions are identical each time.)
 *****************************************************************************/

// Return details about EE internal data structures
void MyICJI::getEEInfo(CORINFO_EE_INFO* pEEInfoOut)
{
    jitInstance->mc->cr->AddCall("getEEInfo");
    jitInstance->mc->repGetEEInfo(pEEInfoOut);
}

// Returns name of the JIT timer log
const char16_t* MyICJI::getJitTimeLogFilename()
{
    jitInstance->mc->cr->AddCall("getJitTimeLogFilename");
    // we have the ability to replay this, but we treat it in this case as EE context
    //  return jitInstance->eec->jitTimeLogFilename;

    // We want to be able to set COMPLUS_JitTimeLogFile when replaying, to collect JIT
    // statistics. So, just do a getenv() call. This isn't quite as thorough as
    // the normal CLR config value functions (which also check the registry), and we've
    // also hard-coded the variable name here instead of using:
    //      CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitTimeLogFile);
    // like in the VM, but it works for our purposes.
    return (const char16_t*)GetEnvironmentVariableWithDefaultW(W("COMPlus_JitTimeLogFile"));
}

/*********************************************************************************/
//
// Diagnostic methods
//
/*********************************************************************************/

// this function is for debugging only. Returns method token.
// Returns mdMethodDefNil for dynamic methods.
mdMethodDef MyICJI::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
{
    jitInstance->mc->cr->AddCall("getMethodDefFromMethod");
    mdMethodDef result = jitInstance->mc->repGetMethodDefFromMethod(hMethod);
    return result;
}

// this function is for debugging only.  It returns the method name
// and if 'moduleName' is non-null, it sets it to something that will
// says which method (a class name, or a module name)
const char* MyICJI::getMethodName(CORINFO_METHOD_HANDLE ftn,       /* IN */
                                  const char**          moduleName /* OUT */
                                  )
{
    jitInstance->mc->cr->AddCall("getMethodName");
    return jitInstance->mc->repGetMethodName(ftn, moduleName);
}

const char* MyICJI::getMethodNameFromMetadata(CORINFO_METHOD_HANDLE ftn,                /* IN */
                                              const char**          className,          /* OUT */
                                              const char**          namespaceName,      /* OUT */
                                              const char**          enclosingClassName /* OUT */
                                              )
{
    jitInstance->mc->cr->AddCall("getMethodNameFromMetadata");
    return jitInstance->mc->repGetMethodNameFromMetadata(ftn, className, namespaceName, enclosingClassName);
}

// this function is for debugging only.  It returns a value that
// is will always be the same for a given method.  It is used
// to implement the 'jitRange' functionality
unsigned MyICJI::getMethodHash(CORINFO_METHOD_HANDLE ftn /* IN */
                               )
{
    jitInstance->mc->cr->AddCall("getMethodHash");
    return jitInstance->mc->repGetMethodHash(ftn);
}

// this function is for debugging only.
size_t MyICJI::findNameOfToken(CORINFO_MODULE_HANDLE              module,        /* IN  */
                               mdToken                            metaTOK,       /* IN  */
                               __out_ecount(FQNameCapacity) char* szFQName,      /* OUT */
                               size_t                             FQNameCapacity /* IN */
                               )
{
    jitInstance->mc->cr->AddCall("findNameOfToken");
    return jitInstance->mc->repFindNameOfToken(module, metaTOK, szFQName, FQNameCapacity);
}

bool MyICJI::getSystemVAmd64PassStructInRegisterDescriptor(
    /* IN */ CORINFO_CLASS_HANDLE                                  structHnd,
    /* OUT */ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    jitInstance->mc->cr->AddCall("getSystemVAmd64PassStructInRegisterDescriptor");
    return jitInstance->mc->repGetSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
}

// Stuff on ICorDynamicInfo
uint32_t MyICJI::getThreadTLSIndex(void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getThreadTLSIndex");
    return jitInstance->mc->repGetThreadTLSIndex(ppIndirection);
}

const void* MyICJI::getInlinedCallFrameVptr(void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getInlinedCallFrameVptr");
    return jitInstance->mc->repGetInlinedCallFrameVptr(ppIndirection);
}

int32_t* MyICJI::getAddrOfCaptureThreadGlobal(void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getAddrOfCaptureThreadGlobal");
    return jitInstance->mc->repGetAddrOfCaptureThreadGlobal(ppIndirection);
}

// return the native entry point to an EE helper (see CorInfoHelpFunc)
void* MyICJI::getHelperFtn(CorInfoHelpFunc ftnNum, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getHelperFtn");
    return jitInstance->mc->repGetHelperFtn(ftnNum, ppIndirection);
}

// return a callable address of the function (native code). This function
// may return a different value (depending on whether the method has
// been JITed or not.
void MyICJI::getFunctionEntryPoint(CORINFO_METHOD_HANDLE ftn,     /* IN  */
                                   CORINFO_CONST_LOOKUP* pResult, /* OUT */
                                   CORINFO_ACCESS_FLAGS  accessFlags)
{
    jitInstance->mc->cr->AddCall("getFunctionEntryPoint");
    jitInstance->mc->repGetFunctionEntryPoint(ftn, pResult, accessFlags);
}

// return a directly callable address. This can be used similarly to the
// value returned by getFunctionEntryPoint() except that it is
// guaranteed to be multi callable entrypoint.
void MyICJI::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE ftn, CORINFO_CONST_LOOKUP* pResult)
{
    jitInstance->mc->cr->AddCall("getFunctionFixedEntryPoint");
    jitInstance->mc->repGetFunctionFixedEntryPoint(ftn, pResult);
}

// get the synchronization handle that is passed to monXstatic function
void* MyICJI::getMethodSync(CORINFO_METHOD_HANDLE ftn, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getMethodSync");
    return jitInstance->mc->repGetMethodSync(ftn, ppIndirection);
}

// These entry points must be called if a handle is being embedded in
// the code to be passed to a JIT helper function. (as opposed to just
// being passed back into the ICorInfo interface.)

// get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
// Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
CorInfoHelpFunc MyICJI::getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle)
{
    jitInstance->mc->cr->AddCall("getLazyStringLiteralHelper");
    return jitInstance->mc->repGetLazyStringLiteralHelper(handle);
}

CORINFO_MODULE_HANDLE MyICJI::embedModuleHandle(CORINFO_MODULE_HANDLE handle, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("embedModuleHandle");
    return jitInstance->mc->repEmbedModuleHandle(handle, ppIndirection);
}

CORINFO_CLASS_HANDLE MyICJI::embedClassHandle(CORINFO_CLASS_HANDLE handle, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("embedClassHandle");
    return jitInstance->mc->repEmbedClassHandle(handle, ppIndirection);
}

CORINFO_METHOD_HANDLE MyICJI::embedMethodHandle(CORINFO_METHOD_HANDLE handle, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("embedMethodHandle");
    return jitInstance->mc->repEmbedMethodHandle(handle, ppIndirection);
}

CORINFO_FIELD_HANDLE MyICJI::embedFieldHandle(CORINFO_FIELD_HANDLE handle, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("embedFieldHandle");
    return jitInstance->mc->repEmbedFieldHandle(handle, ppIndirection);
}

// Given a module scope (module), a method handle (context) and
// a metadata token (metaTOK), fetch the handle
// (type, field or method) associated with the token.
// If this is not possible at compile-time (because the current method's
// code is shared and the token contains generic parameters)
// then indicate how the handle should be looked up at run-time.
//
void MyICJI::embedGenericHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                bool fEmbedParent, // TRUE - embeds parent type handle of the field/method handle
                                CORINFO_GENERICHANDLE_RESULT* pResult)
{
    jitInstance->mc->cr->AddCall("embedGenericHandle");
    jitInstance->mc->repEmbedGenericHandle(pResolvedToken, fEmbedParent, pResult);
}

// Return information used to locate the exact enclosing type of the current method.
// Used only to invoke .cctor method from code shared across generic instantiations
//   !needsRuntimeLookup       statically known (enclosing type of method itself)
//   needsRuntimeLookup:
//      CORINFO_LOOKUP_THISOBJ     use vtable pointer of 'this' param
//      CORINFO_LOOKUP_CLASSPARAM  use vtable hidden param
//      CORINFO_LOOKUP_METHODPARAM use enclosing type of method-desc hidden param
void MyICJI::getLocationOfThisType(CORINFO_METHOD_HANDLE context, CORINFO_LOOKUP_KIND* pLookupKind)
{
    jitInstance->mc->cr->AddCall("getLocationOfThisType");
    jitInstance->mc->repGetLocationOfThisType(context, pLookupKind);
}

// return address of fixup area for late-bound PInvoke calls.
void MyICJI::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method, CORINFO_CONST_LOOKUP* pLookup)
{
    jitInstance->mc->cr->AddCall("getAddressOfPInvokeTarget");
    jitInstance->mc->repGetAddressOfPInvokeTarget(method, pLookup);
}

// Generate a cookie based on the signature that would needs to be passed
// to CORINFO_HELP_PINVOKE_CALLI
LPVOID MyICJI::GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("GetCookieForPInvokeCalliSig");
    return jitInstance->mc->repGetCookieForPInvokeCalliSig(szMetaSig, ppIndirection);
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool MyICJI::canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
{
    jitInstance->mc->cr->AddCall("canGetCookieForPInvokeCalliSig");
    return jitInstance->mc->repCanGetCookieForPInvokeCalliSig(szMetaSig);
}

// Gets a handle that is checked to see if the current method is
// included in "JustMyCode"
CORINFO_JUST_MY_CODE_HANDLE MyICJI::getJustMyCodeHandle(CORINFO_METHOD_HANDLE         method,
                                                        CORINFO_JUST_MY_CODE_HANDLE** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getJustMyCodeHandle");
    return jitInstance->mc->repGetJustMyCodeHandle(method, ppIndirection);
}

// Gets a method handle that can be used to correlate profiling data.
// This is the IP of a native method, or the address of the descriptor struct
// for IL.  Always guaranteed to be unique per process, and not to move. */
void MyICJI::GetProfilingHandle(bool* pbHookFunction, void** pProfilerHandle, bool* pbIndirectedHandles)
{
    jitInstance->mc->cr->AddCall("GetProfilingHandle");
    jitInstance->mc->repGetProfilingHandle(pbHookFunction, pProfilerHandle, pbIndirectedHandles);
}

// Returns instructions on how to make the call. See code:CORINFO_CALL_INFO for possible return values.
void MyICJI::getCallInfo(
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
    jitInstance->mc->cr->AddCall("getCallInfo");
    DWORD exceptionCode = 0;
    jitInstance->mc->repGetCallInfo(pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult,
                                    &exceptionCode);
    if (exceptionCode != 0)
        ThrowException(exceptionCode);
}

bool MyICJI::canAccessFamily(CORINFO_METHOD_HANDLE hCaller, CORINFO_CLASS_HANDLE hInstanceType)

{
    jitInstance->mc->cr->AddCall("canAccessFamily");
    return jitInstance->mc->repCanAccessFamily(hCaller, hInstanceType);
}
// Returns TRUE if the Class Domain ID is the RID of the class (currently true for every class
// except reflection emitted classes and generics)
bool MyICJI::isRIDClassDomainID(CORINFO_CLASS_HANDLE cls)
{
    jitInstance->mc->cr->AddCall("isRIDClassDomainID");
    LogError("Hit unimplemented isRIDClassDomainID");
    DebugBreakorAV(107);
    return false;
}

// returns the class's domain ID for accessing shared statics
unsigned MyICJI::getClassDomainID(CORINFO_CLASS_HANDLE cls, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getClassDomainID");
    return jitInstance->mc->repGetClassDomainID(cls, ppIndirection);
}

// return the data's address (for static fields only)
void* MyICJI::getFieldAddress(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getFieldAddress");
    return jitInstance->mc->repGetFieldAddress(field, ppIndirection);
}

// return the class handle for the current value of a static field
CORINFO_CLASS_HANDLE MyICJI::getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative)
{
    jitInstance->mc->cr->AddCall("getStaticFieldCurrentClass");
    return jitInstance->mc->repGetStaticFieldCurrentClass(field, pIsSpeculative);
}

// registers a vararg sig & returns a VM cookie for it (which can contain other stuff)
CORINFO_VARARGS_HANDLE MyICJI::getVarArgsHandle(CORINFO_SIG_INFO* pSig, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getVarArgsHandle");
    return jitInstance->mc->repGetVarArgsHandle(pSig, ppIndirection);
}

// returns true if a VM cookie can be generated for it (might be false due to cross-module
// inlining, in which case the inlining should be aborted)
bool MyICJI::canGetVarArgsHandle(CORINFO_SIG_INFO* pSig)
{
    jitInstance->mc->cr->AddCall("canGetVarArgsHandle");
    return jitInstance->mc->repCanGetVarArgsHandle(pSig);
}

// Allocate a string literal on the heap and return a handle to it
InfoAccessType MyICJI::constructStringLiteral(CORINFO_MODULE_HANDLE module, mdToken metaTok, void** ppValue)
{
    jitInstance->mc->cr->AddCall("constructStringLiteral");
    return jitInstance->mc->repConstructStringLiteral(module, metaTok, ppValue);
}

InfoAccessType MyICJI::emptyStringLiteral(void** ppValue)
{
    jitInstance->mc->cr->AddCall("emptyStringLiteral");
    return jitInstance->mc->repEmptyStringLiteral(ppValue);
}

// (static fields only) given that 'field' refers to thread local store,
// return the ID (TLS index), which is used to find the beginning of the
// TLS data area for the particular DLL 'field' is associated with.
uint32_t MyICJI::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE field, void** ppIndirection)
{
    jitInstance->mc->cr->AddCall("getFieldThreadLocalStoreID");
    return jitInstance->mc->repGetFieldThreadLocalStoreID(field, ppIndirection);
}

// Sets another object to intercept calls to "self" and current method being compiled
void MyICJI::setOverride(ICorDynamicInfo* pOverride, CORINFO_METHOD_HANDLE currentMethod)
{
    jitInstance->mc->cr->AddCall("setOverride");
    LogError("Hit unimplemented setOverride");
    DebugBreakorAV(115);
}

// Adds an active dependency from the context method's module to the given module
// This is internal callback for the EE. JIT should not call it directly.
void MyICJI::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo)
{
    jitInstance->mc->cr->AddCall("addActiveDependency");
    LogError("Hit unimplemented addActiveDependency");
    DebugBreakorAV(116);
}

CORINFO_METHOD_HANDLE MyICJI::GetDelegateCtor(CORINFO_METHOD_HANDLE methHnd,
                                              CORINFO_CLASS_HANDLE  clsHnd,
                                              CORINFO_METHOD_HANDLE targetMethodHnd,
                                              DelegateCtorArgs*     pCtorData)
{
    jitInstance->mc->cr->AddCall("GetDelegateCtor");
    return jitInstance->mc->repGetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData);
}

void MyICJI::MethodCompileComplete(CORINFO_METHOD_HANDLE methHnd)
{
    jitInstance->mc->cr->AddCall("MethodCompileComplete");
    LogError("Hit unimplemented MethodCompileComplete");
    DebugBreakorAV(118);
}

bool MyICJI::getTailCallHelpers(
        CORINFO_RESOLVED_TOKEN* callToken,
        CORINFO_SIG_INFO* sig,
        CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
        CORINFO_TAILCALL_HELPERS* pResult)
{
    jitInstance->mc->cr->AddCall("getTailCallHelpers");
    return jitInstance->mc->repGetTailCallHelpers(callToken, sig, flags, pResult);
}

bool MyICJI::convertPInvokeCalliToCall(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool fMustConvert)
{
    jitInstance->mc->cr->AddCall("convertPInvokeCalliToCall");
    return jitInstance->mc->repConvertPInvokeCalliToCall(pResolvedToken, fMustConvert);
}

bool MyICJI::notifyInstructionSetUsage(CORINFO_InstructionSet instructionSet, bool supported)
{
    jitInstance->mc->cr->AddCall("notifyInstructionSetUsage");
    return supported;
}

// Stuff directly on ICorJitInfo

// Returns extended flags for a particular compilation instance.
uint32_t MyICJI::getJitFlags(CORJIT_FLAGS* jitFlags, uint32_t sizeInBytes)
{
    jitInstance->mc->cr->AddCall("getJitFlags");
    uint32_t ret = jitInstance->mc->repGetJitFlags(jitFlags, sizeInBytes);
    if (jitInstance->forceClearAltJitFlag)
    {
        jitFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT);
    }
    else if (jitInstance->forceSetAltJitFlag)
    {
        jitFlags->Set(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT);
    }
    return ret;
}

// Runs the given function with the given parameter under an error trap
// and returns true if the function completes successfully. We fake this
// up a bit for SuperPMI and simply catch all exceptions.
bool MyICJI::runWithErrorTrap(void (*function)(void*), void* param)
{
    return RunWithErrorTrap(function, param);
}

// Ideally we'd just use the copies of this in standardmacros.h
// however, superpmi is missing various other dependencies as well
static size_t ALIGN_UP_SPMI(size_t val, size_t alignment)
{
    return (val + (alignment - 1)) & ~(alignment - 1);
}

static void* ALIGN_UP_SPMI(void* val, size_t alignment)
{
    return (void*)ALIGN_UP_SPMI((size_t)val, alignment);
}

// get a block of memory for the code, readonly data, and read-write data
void MyICJI::allocMem(AllocMemArgs* pArgs)
{
    jitInstance->mc->cr->AddCall("allocMem");

    // TODO-Cleanup: Could hot block size be ever 0?
    size_t codeAlignment      = sizeof(void*);
    size_t hotCodeAlignedSize = static_cast<size_t>(pArgs->hotCodeSize);

    if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_32BYTE_ALIGN) != 0)
    {
         codeAlignment = 32;
    }
    else if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN) != 0)
    {
         codeAlignment = 16;
    }
    hotCodeAlignedSize = ALIGN_UP_SPMI(hotCodeAlignedSize, codeAlignment);
    hotCodeAlignedSize = hotCodeAlignedSize + (codeAlignment - sizeof(void*));
    pArgs->hotCodeBlock      = jitInstance->mc->cr->allocateMemory(hotCodeAlignedSize);
    pArgs->hotCodeBlock      = ALIGN_UP_SPMI(pArgs->hotCodeBlock, codeAlignment);

    if (pArgs->coldCodeSize > 0)
        pArgs->coldCodeBlock = jitInstance->mc->cr->allocateMemory(pArgs->coldCodeSize);
    else
        pArgs->coldCodeBlock = nullptr;

    if (pArgs->roDataSize > 0)
    {
        size_t roDataAlignment   = sizeof(void*);
        size_t roDataAlignedSize = static_cast<size_t>(pArgs->roDataSize);

        if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_RODATA_32BYTE_ALIGN) != 0)
        {
            roDataAlignment = 32;
        }
        else if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN) != 0)
        {
            roDataAlignment = 16;
        }
        else if (pArgs->roDataSize >= 8)
        {
            roDataAlignment = 8;
        }

        // We need to round the roDataSize up to the alignment size and then
        // overallocate by at most alignment - sizeof(void*) to ensure that
        // we can offset roDataBlock to be an aligned address and that the
        // allocation contains at least the originally requested size after

        roDataAlignedSize = ALIGN_UP_SPMI(roDataAlignedSize, roDataAlignment);
        roDataAlignedSize = roDataAlignedSize + (roDataAlignment - sizeof(void*));
        pArgs->roDataBlock = jitInstance->mc->cr->allocateMemory(roDataAlignedSize);
        pArgs->roDataBlock = ALIGN_UP_SPMI(pArgs->roDataBlock, roDataAlignment);
    }
    else
        pArgs->roDataBlock = nullptr;

    pArgs->hotCodeBlockRW = pArgs->hotCodeBlock;
    pArgs->coldCodeBlockRW = pArgs->coldCodeBlock;
    pArgs->roDataBlockRW = pArgs->roDataBlock;

    jitInstance->mc->cr->recAllocMem(pArgs->hotCodeSize, pArgs->coldCodeSize, pArgs->roDataSize, pArgs->xcptnsCount, pArgs->flag, &pArgs->hotCodeBlock,
                                     &pArgs->coldCodeBlock, &pArgs->roDataBlock);
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
void MyICJI::reserveUnwindInfo(bool     isFunclet,  /* IN */
                               bool     isColdCode, /* IN */
                               uint32_t unwindSize  /* IN */
                               )
{
    jitInstance->mc->cr->AddCall("reserveUnwindInfo");
    jitInstance->mc->cr->recReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
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
void MyICJI::allocUnwindInfo(uint8_t*       pHotCode,     /* IN */
                             uint8_t*       pColdCode,    /* IN */
                             uint32_t       startOffset,  /* IN */
                             uint32_t       endOffset,    /* IN */
                             uint32_t       unwindSize,   /* IN */
                             uint8_t*       pUnwindBlock, /* IN */
                             CorJitFuncKind funcKind      /* IN */
                             )
{
    jitInstance->mc->cr->AddCall("allocUnwindInfo");
    jitInstance->mc->cr->recAllocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock,
                                            funcKind);
}

// Get a block of memory needed for the code manager information,
// (the info for enumerating the GC pointers while crawling the
// stack frame).
// Note that allocMem must be called first
void* MyICJI::allocGCInfo(size_t size /* IN */
                          )
{
    jitInstance->mc->cr->AddCall("allocGCInfo");
    void* temp = jitInstance->mc->cr->allocateMemory(size);
    jitInstance->mc->cr->recAllocGCInfo(size, temp);

    return temp;
}

// Indicate how many exception handler blocks are to be returned.
// This is guaranteed to be called before any 'setEHinfo' call.
// Note that allocMem must be called before this method can be called.
void MyICJI::setEHcount(unsigned cEH /* IN */
                        )
{
    jitInstance->mc->cr->AddCall("setEHcount");
    jitInstance->mc->cr->recSetEHcount(cEH);
}

// Set the values for one particular exception handler block.
//
// Handler regions should be lexically contiguous.
// This is because FinallyIsUnwinding() uses lexicality to
// determine if a "finally" clause is executing.
void MyICJI::setEHinfo(unsigned                 EHnumber, /* IN  */
                       const CORINFO_EH_CLAUSE* clause    /* IN */
                       )
{
    jitInstance->mc->cr->AddCall("setEHinfo");
    jitInstance->mc->cr->recSetEHinfo(EHnumber, clause);
}

// Level 1 -> fatalError, Level 2 -> Error, Level 3 -> Warning
// Level 4 means happens 10 times in a run, level 5 means 100, level 6 means 1000 ...
// returns non-zero if the logging succeeded
bool MyICJI::logMsg(unsigned level, const char* fmt, va_list args)
{
    jitInstance->mc->cr->AddCall("logMsg");

    //  if(level<=2)
    //  {
    // jitInstance->mc->cr->recMessageLog(fmt, args);
    // DebugBreakorAV(0x99);
    //}
    jitInstance->mc->cr->recMessageLog(fmt, args);
    return 0;
}

// do an assert.  will return true if the code should retry (DebugBreak)
// returns false, if the assert should be igored.
int MyICJI::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    jitInstance->mc->cr->AddCall("doAssert");
    char buff[16 * 1024];
    sprintf_s(buff, sizeof(buff), "%s (%d) - %s", szFile, iLine, szExpr);

    LogIssue(ISSUE_ASSERT, "%s", buff);
    jitInstance->mc->cr->recMessageLog(buff);

    // Under "/boa", ask the user if they want to attach a debugger. If they do, the debugger will be attached,
    // then we'll call DebugBreakorAV(), which will issue a __debugbreak() and actually cause
    // us to stop in the debugger.
    if (BreakOnDebugBreakorAV())
    {
        DbgBreakCheck(szFile, iLine, szExpr);
    }

    DebugBreakorAV(0x7b);
    return 0;
}

void MyICJI::reportFatalError(CorJitResult result)
{
    jitInstance->mc->cr->AddCall("reportFatalError");
    jitInstance->mc->cr->recReportFatalError(result);
}

// allocate a basic block profile buffer where execution counts will be stored
// for jitted basic blocks.
HRESULT MyICJI::allocPgoInstrumentationBySchema(CORINFO_METHOD_HANDLE ftnHnd,
                                                PgoInstrumentationSchema* pSchema,
                                                uint32_t countSchemaItems,
                                                uint8_t** pInstrumentationData)
{
    jitInstance->mc->cr->AddCall("allocPgoInstrumentationBySchema");
    return jitInstance->mc->repAllocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData);
}

// get profile information to be used for optimizing the current method.  The format
// of the buffer is the same as the format the JIT passes to allocMethodBlockCounts.
HRESULT MyICJI::getPgoInstrumentationResults(CORINFO_METHOD_HANDLE      ftnHnd,
                                             PgoInstrumentationSchema **pSchema,                    // pointer to the schema table which describes the instrumentation results (pointer will not remain valid after jit completes)
                                             uint32_t *                 pCountSchemaItems,          // pointer to the count schema items
                                             uint8_t **                 pInstrumentationData)       // pointer to the actual instrumentation data (pointer will not remain valid after jit completes)
{
    jitInstance->mc->cr->AddCall("getPgoInstrumentationResults");
    return jitInstance->mc->repGetPgoInstrumentationResults(ftnHnd, pSchema, pCountSchemaItems, pInstrumentationData);
}

// Associates a native call site, identified by its offset in the native code stream, with
// the signature information and method handle the JIT used to lay out the call site. If
// the call site has no signature information (e.g. a helper call) or has no method handle
// (e.g. a CALLI P/Invoke), then null should be passed instead.
void MyICJI::recordCallSite(uint32_t              instrOffset, /* IN */
                            CORINFO_SIG_INFO*     callSig,     /* IN */
                            CORINFO_METHOD_HANDLE methodHandle /* IN */
                            )
{
    jitInstance->mc->cr->AddCall("recordCallSite");
    jitInstance->mc->cr->repRecordCallSite(instrOffset, callSig, methodHandle);
}

// A relocation is recorded if we are pre-jitting.
// A jump thunk may be inserted if we are jitting
void MyICJI::recordRelocation(void*    location,   /* IN  */
                              void*    locationRW, /* IN  */
                              void*    target,     /* IN  */
                              uint16_t fRelocType, /* IN  */
                              uint16_t slotNum,    /* IN  */
                              int32_t  addlDelta   /* IN  */
                              )
{
    jitInstance->mc->cr->AddCall("recordRelocation");
    jitInstance->mc->cr->repRecordRelocation(location, target, fRelocType, slotNum, addlDelta);
}

uint16_t MyICJI::getRelocTypeHint(void* target)
{
    jitInstance->mc->cr->AddCall("getRelocTypeHint");
    uint16_t result = jitInstance->mc->repGetRelocTypeHint(target);
    return result;
}

// For what machine does the VM expect the JIT to generate code? The VM
// returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
// is cross-compiling (such as the case for crossgen), it will return a
// different value than if it was compiling for the host architecture.
//
uint32_t MyICJI::getExpectedTargetArchitecture()
{
    jitInstance->mc->cr->AddCall("getExpectedTargetArchitecture");
    DWORD result = jitInstance->mc->repGetExpectedTargetArchitecture();
    return result;
}
