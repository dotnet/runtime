// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define API_ENTER(name) wrapComp->CLR_API_Enter(API_##name);
#define API_LEAVE(name) wrapComp->CLR_API_Leave(API_##name);

/**********************************************************************************/
// clang-format off
/**********************************************************************************/
//
// ICorMethodInfo
//

DWORD WrapICorJitInfo::getMethodAttribs(CORINFO_METHOD_HANDLE ftn /* IN */)
{
    API_ENTER(getMethodAttribs)
    DWORD temp = wrapHnd->getMethodAttribs(ftn);
    API_LEAVE(getMethodAttribs)
    return temp;
}

void WrapICorJitInfo::setMethodAttribs(CORINFO_METHOD_HANDLE ftn,/* IN */
                                       CorInfoMethodRuntimeFlags attribs/* IN */)
{
    API_ENTER(setMethodAttribs);
    wrapHnd->setMethodAttribs(ftn, attribs);
    API_LEAVE(setMethodAttribs);
}

void WrapICorJitInfo::getMethodSig(CORINFO_METHOD_HANDLE      ftn,        /* IN  */
                                   CORINFO_SIG_INFO          *sig,        /* OUT */
                                   CORINFO_CLASS_HANDLE      memberParent/* IN */)
{
    API_ENTER(getMethodSig);
    wrapHnd->getMethodSig(ftn, sig, memberParent);
    API_LEAVE(getMethodSig);
}

bool WrapICorJitInfo::getMethodInfo(
            CORINFO_METHOD_HANDLE   ftn,            /* IN  */
            CORINFO_METHOD_INFO*    info            /* OUT */)
{
    API_ENTER(getMethodInfo);
    bool temp = wrapHnd->getMethodInfo(ftn, info);
    API_LEAVE(getMethodInfo);
    return temp;
}

CorInfoInline WrapICorJitInfo::canInline(
            CORINFO_METHOD_HANDLE       callerHnd,                  /* IN  */
            CORINFO_METHOD_HANDLE       calleeHnd,                  /* IN  */
            DWORD*                      pRestrictions               /* OUT */)
{
    API_ENTER(canInline);
    CorInfoInline temp = wrapHnd->canInline(callerHnd, calleeHnd, pRestrictions);
    API_LEAVE(canInline);
    return temp;
}

void WrapICorJitInfo::reportInliningDecision(CORINFO_METHOD_HANDLE inlinerHnd,
                                                CORINFO_METHOD_HANDLE inlineeHnd,
                                                CorInfoInline inlineResult,
                                                const char * reason)
{
    API_ENTER(reportInliningDecision);
    wrapHnd->reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
    API_LEAVE(reportInliningDecision);
}

bool WrapICorJitInfo::canTailCall(
            CORINFO_METHOD_HANDLE   callerHnd,          /* IN */
            CORINFO_METHOD_HANDLE   declaredCalleeHnd,  /* IN */
            CORINFO_METHOD_HANDLE   exactCalleeHnd,     /* IN */
            bool fIsTailPrefix                          /* IN */)
{
    API_ENTER(canTailCall);
    bool temp = wrapHnd->canTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
    API_LEAVE(canTailCall);
    return temp;
}

void WrapICorJitInfo::reportTailCallDecision(CORINFO_METHOD_HANDLE callerHnd,
                                                   CORINFO_METHOD_HANDLE calleeHnd,
                                                   bool fIsTailPrefix,
                                                   CorInfoTailCall tailCallResult,
                                                   const char * reason)
{
    API_ENTER(reportTailCallDecision);
    wrapHnd->reportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
    API_LEAVE(reportTailCallDecision);
}

void WrapICorJitInfo::getEHinfo(
            CORINFO_METHOD_HANDLE ftn,              /* IN  */
            unsigned          EHnumber,             /* IN */
            CORINFO_EH_CLAUSE* clause               /* OUT */)
{
    API_ENTER(getEHinfo);
    wrapHnd->getEHinfo(ftn, EHnumber, clause);
    API_LEAVE(getEHinfo);
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getMethodClass(
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(getMethodClass);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getMethodClass(method);
    API_LEAVE(getMethodClass);
    return temp;
}

CORINFO_MODULE_HANDLE WrapICorJitInfo::getMethodModule(
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(getMethodModule);
    CORINFO_MODULE_HANDLE temp = wrapHnd->getMethodModule(method);
    API_LEAVE(getMethodModule);
    return temp;
}

void WrapICorJitInfo::getMethodVTableOffset(
            CORINFO_METHOD_HANDLE       method,                 /* IN */
            unsigned*                   offsetOfIndirection,    /* OUT */
            unsigned*                   offsetAfterIndirection, /* OUT */
            bool*                       isRelative              /* OUT */)
{
    API_ENTER(getMethodVTableOffset);
    wrapHnd->getMethodVTableOffset(method, offsetOfIndirection, offsetAfterIndirection, isRelative);
    API_LEAVE(getMethodVTableOffset);
}

CorInfoIntrinsics WrapICorJitInfo::getIntrinsicID(
            CORINFO_METHOD_HANDLE       method,
            bool*                       pMustExpand             /* OUT */)
{
    API_ENTER(getIntrinsicID);
    CorInfoIntrinsics temp = wrapHnd->getIntrinsicID(method, pMustExpand);
    API_LEAVE(getIntrinsicID);
    return temp;
}

bool WrapICorJitInfo::isInSIMDModule(CORINFO_CLASS_HANDLE classHnd)
{
    API_ENTER(isInSIMDModule);
    bool temp = wrapHnd->isInSIMDModule(classHnd);
    API_LEAVE(isInSIMDModule);
    return temp;
}

CorInfoUnmanagedCallConv WrapICorJitInfo::getUnmanagedCallConv(
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(getUnmanagedCallConv);
    CorInfoUnmanagedCallConv temp = wrapHnd->getUnmanagedCallConv(method);
    API_LEAVE(getUnmanagedCallConv);
    return temp;
}

BOOL WrapICorJitInfo::pInvokeMarshalingRequired(
            CORINFO_METHOD_HANDLE       method,
            CORINFO_SIG_INFO*           callSiteSig)
{
    API_ENTER(pInvokeMarshalingRequired);
    BOOL temp = wrapHnd->pInvokeMarshalingRequired(method, callSiteSig);
    API_LEAVE(pInvokeMarshalingRequired);
    return temp;
}

BOOL WrapICorJitInfo::satisfiesMethodConstraints(
            CORINFO_CLASS_HANDLE        parent, // the exact parent of the method
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(satisfiesMethodConstraints);
    BOOL temp = wrapHnd->satisfiesMethodConstraints(parent, method);
    API_LEAVE(satisfiesMethodConstraints);
    return temp;
}

BOOL WrapICorJitInfo::isCompatibleDelegate(
            CORINFO_CLASS_HANDLE        objCls,
            CORINFO_CLASS_HANDLE        methodParentCls,
            CORINFO_METHOD_HANDLE       method,
            CORINFO_CLASS_HANDLE        delegateCls,
            BOOL                        *pfIsOpenDelegate)
{
    API_ENTER(isCompatibleDelegate);
    BOOL temp = wrapHnd->isCompatibleDelegate(objCls, methodParentCls, method, delegateCls, pfIsOpenDelegate);
    API_LEAVE(isCompatibleDelegate);
    return temp;
}

CorInfoInstantiationVerification WrapICorJitInfo::isInstantiationOfVerifiedGeneric(
            CORINFO_METHOD_HANDLE   method /* IN  */)
{
    API_ENTER(isInstantiationOfVerifiedGeneric);
    CorInfoInstantiationVerification temp = wrapHnd->isInstantiationOfVerifiedGeneric(method);
    API_LEAVE(isInstantiationOfVerifiedGeneric);
    return temp;
}

void WrapICorJitInfo::initConstraintsForVerification(
            CORINFO_METHOD_HANDLE   method, /* IN */
            BOOL *pfHasCircularClassConstraints, /* OUT */
            BOOL *pfHasCircularMethodConstraint /* OUT */)
{
    API_ENTER(initConstraintsForVerification);
    wrapHnd->initConstraintsForVerification(method, pfHasCircularClassConstraints, pfHasCircularMethodConstraint);
    API_LEAVE(initConstraintsForVerification);
}

CorInfoCanSkipVerificationResult WrapICorJitInfo::canSkipMethodVerification(
            CORINFO_METHOD_HANDLE       ftnHandle)
{
    API_ENTER(canSkipMethodVerification);
    CorInfoCanSkipVerificationResult temp = wrapHnd->canSkipMethodVerification(ftnHandle);
    API_LEAVE(canSkipMethodVerification);
    return temp;
}

void WrapICorJitInfo::methodMustBeLoadedBeforeCodeIsRun(
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(methodMustBeLoadedBeforeCodeIsRun);
    wrapHnd->methodMustBeLoadedBeforeCodeIsRun(method);
    API_LEAVE(methodMustBeLoadedBeforeCodeIsRun);
}

CORINFO_METHOD_HANDLE WrapICorJitInfo::mapMethodDeclToMethodImpl(
            CORINFO_METHOD_HANDLE       method)
{
    API_ENTER(mapMethodDeclToMethodImpl);
    CORINFO_METHOD_HANDLE temp = wrapHnd->mapMethodDeclToMethodImpl(method);
    API_LEAVE(mapMethodDeclToMethodImpl);
    return temp;
}

void WrapICorJitInfo::getGSCookie(
            GSCookie * pCookieVal,
            GSCookie ** ppCookieVal             )
{
    API_ENTER(getGSCookie);
    wrapHnd->getGSCookie(pCookieVal, ppCookieVal);
    API_LEAVE(getGSCookie);
}

/**********************************************************************************/
//
// ICorModuleInfo
//
/**********************************************************************************/

void WrapICorJitInfo::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    API_ENTER(resolveToken);
    wrapHnd->resolveToken(pResolvedToken);
    API_LEAVE(resolveToken);
}

bool WrapICorJitInfo::tryResolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    API_ENTER(tryResolveToken);
    bool success = wrapHnd->tryResolveToken(pResolvedToken);
    API_LEAVE(tryResolveToken);
    return success;
}

void WrapICorJitInfo::findSig(
            CORINFO_MODULE_HANDLE       module,
            unsigned                    sigTOK,
            CORINFO_CONTEXT_HANDLE      context,
            CORINFO_SIG_INFO           *sig     )
{
    API_ENTER(findSig);
    wrapHnd->findSig(module, sigTOK, context, sig);
    API_LEAVE(findSig);
}

void WrapICorJitInfo::findCallSiteSig(
            CORINFO_MODULE_HANDLE       module,     /* IN */
            unsigned                    methTOK,    /* IN */
            CORINFO_CONTEXT_HANDLE      context,    /* IN */
            CORINFO_SIG_INFO           *sig         /* OUT */)
{
    API_ENTER(findCallSiteSig);
    wrapHnd->findCallSiteSig(module, methTOK, context, sig);
    API_LEAVE(findCallSiteSig);
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getTokenTypeAsHandle(
            CORINFO_RESOLVED_TOKEN *    pResolvedToken /* IN  */)
{
    API_ENTER(getTokenTypeAsHandle);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getTokenTypeAsHandle(pResolvedToken);
    API_LEAVE(getTokenTypeAsHandle);
    return temp;
}

CorInfoCanSkipVerificationResult WrapICorJitInfo::canSkipVerification(
            CORINFO_MODULE_HANDLE       module     /* IN  */)
{
    API_ENTER(canSkipVerification);
    CorInfoCanSkipVerificationResult temp = wrapHnd->canSkipVerification(module);
    API_LEAVE(canSkipVerification);
    return temp;
}

BOOL WrapICorJitInfo::isValidToken(
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            unsigned                    metaTOK     /* IN  */)
{
    API_ENTER(isValidToken);
    BOOL result = wrapHnd->isValidToken(module, metaTOK);
    API_LEAVE(isValidToken);
    return result;
}

BOOL WrapICorJitInfo::isValidStringRef(
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            unsigned                    metaTOK     /* IN  */)
{
    API_ENTER(isValidStringRef);
    BOOL temp = wrapHnd->isValidStringRef(module, metaTOK);
    API_LEAVE(isValidStringRef);
    return temp;
}

BOOL WrapICorJitInfo::shouldEnforceCallvirtRestriction(
            CORINFO_MODULE_HANDLE   scope)
{
    API_ENTER(shouldEnforceCallvirtRestriction);
    BOOL temp = wrapHnd->shouldEnforceCallvirtRestriction(scope);
    API_LEAVE(shouldEnforceCallvirtRestriction);
    return temp;
}

/**********************************************************************************/
//
// ICorClassInfo
//
/**********************************************************************************/

CorInfoType WrapICorJitInfo::asCorInfoType(CORINFO_CLASS_HANDLE    cls)
{
    API_ENTER(asCorInfoType);
    CorInfoType temp = wrapHnd->asCorInfoType(cls);
    API_LEAVE(asCorInfoType);
    return temp;
}

const char* WrapICorJitInfo::getClassName(CORINFO_CLASS_HANDLE    cls)
{
    API_ENTER(getClassName);
    const char* result = wrapHnd->getClassName(cls);
    API_LEAVE(getClassName);
    return result;
}

const char* WrapICorJitInfo::getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
{
    API_ENTER(getClassNameFromMetadata);
    const char* result = wrapHnd->getClassNameFromMetadata(cls, namespaceName);
    API_LEAVE(getClassNameFromMetadata);
    return result;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
{
    API_ENTER(getTypeInstantiationArgument);
    CORINFO_CLASS_HANDLE result = wrapHnd->getTypeInstantiationArgument(cls, index);
    API_LEAVE(getTypeInstantiationArgument);
    return result;
}

int WrapICorJitInfo::appendClassName(
            __deref_inout_ecount(*pnBufLen) WCHAR** ppBuf,
            int* pnBufLen,
            CORINFO_CLASS_HANDLE    cls,
            BOOL fNamespace,
            BOOL fFullInst,
            BOOL fAssembly)
{
    API_ENTER(appendClassName);
    WCHAR* pBuf = *ppBuf;
    int nLen = wrapHnd->appendClassName(ppBuf, pnBufLen, cls, fNamespace, fFullInst, fAssembly);
    API_LEAVE(appendClassName);
    return nLen;
}

BOOL WrapICorJitInfo::isValueClass(CORINFO_CLASS_HANDLE cls)
{
    API_ENTER(isValueClass);
    BOOL temp = wrapHnd->isValueClass(cls);
    API_LEAVE(isValueClass);
    return temp;
}

CorInfoInlineTypeCheck canInlineTypeCheck(CORINFO_CLASS_HANDLE cls, CorInfoInlineTypeCheckSource source)
{
    API_ENTER(canInlineTypeCheck);
    CorInfoInlineTypeCheck temp = wrapHnd->canInlineTypeCheck(cls, source);
    API_LEAVE(canInlineTypeCheck);
    return temp;
}

BOOL WrapICorJitInfo::canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE cls)
{
    API_ENTER(canInlineTypeCheckWithObjectVTable);
    BOOL temp = wrapHnd->canInlineTypeCheckWithObjectVTable(cls);
    API_LEAVE(canInlineTypeCheckWithObjectVTable);
    return temp;
}

DWORD WrapICorJitInfo::getClassAttribs(
            CORINFO_CLASS_HANDLE    cls)
{
    API_ENTER(getClassAttribs);
    DWORD temp = wrapHnd->getClassAttribs(cls);
    API_LEAVE(getClassAttribs);
    return temp;
}

BOOL WrapICorJitInfo::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls)
{
    API_ENTER(isStructRequiringStackAllocRetBuf);
    BOOL temp = wrapHnd->isStructRequiringStackAllocRetBuf(cls);
    API_LEAVE(isStructRequiringStackAllocRetBuf);
    return temp;
}

CORINFO_MODULE_HANDLE WrapICorJitInfo::getClassModule(
            CORINFO_CLASS_HANDLE    cls)
{
    API_ENTER(getClassModule);
    CORINFO_MODULE_HANDLE result = wrapHnd->getClassModule(cls);
    API_LEAVE(getClassModule);
    return result;
}

CORINFO_ASSEMBLY_HANDLE WrapICorJitInfo::getModuleAssembly(
            CORINFO_MODULE_HANDLE   mod)
{
    API_ENTER(getModuleAssembly);
    CORINFO_ASSEMBLY_HANDLE result = wrapHnd->getModuleAssembly(mod);
    API_LEAVE(getModuleAssembly);
    return result;
}

const char* WrapICorJitInfo::getAssemblyName(
            CORINFO_ASSEMBLY_HANDLE assem)
{
    API_ENTER(getAssemblyName);
    const char* result = wrapHnd->getAssemblyName(assem);
    API_LEAVE(getAssemblyName);
    return result;
}

void* WrapICorJitInfo::LongLifetimeMalloc(size_t sz)
{
    API_ENTER(LongLifetimeMalloc);
    void* result = wrapHnd->LongLifetimeMalloc(sz);
    API_LEAVE(LongLifetimeMalloc);
    return result;
}

void WrapICorJitInfo::LongLifetimeFree(void* obj)
{
    API_ENTER(LongLifetimeFree);
    wrapHnd->LongLifetimeFree(obj);
    API_LEAVE(LongLifetimeFree);
}

size_t WrapICorJitInfo::getClassModuleIdForStatics(
        CORINFO_CLASS_HANDLE    cls,
        CORINFO_MODULE_HANDLE *pModule,
        void **ppIndirection)
{
    API_ENTER(getClassModuleIdForStatics);
    size_t temp = wrapHnd->getClassModuleIdForStatics(cls, pModule, ppIndirection);
    API_LEAVE(getClassModuleIdForStatics);
    return temp;
}

unsigned WrapICorJitInfo::getClassSize(CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getClassSize);
    unsigned temp = wrapHnd->getClassSize(cls);
    API_LEAVE(getClassSize);
    return temp;
}

unsigned WrapICorJitInfo::getHeapClassSize(CORINFO_CLASS_HANDLE     cls)
{
    API_ENTER(getHeapClassSize);
    unsigned temp = wrapHnd->getHeapClassSize(cls);
    API_LEAVE(getHeapClassSize);
    return temp;
}

BOOL WrapICorJitInfo::canAllocateOnStack(CORINFO_CLASS_HANDLE    cls)
{
    API_ENTER(canAllocateOnStack);
    BOOL temp = wrapHnd->canAllocateOnStack(cls);
    API_LEAVE(canAllocateOnStack);
    return temp;
}

unsigned WrapICorJitInfo::getClassAlignmentRequirement(
            CORINFO_CLASS_HANDLE        cls,
            BOOL                        fDoubleAlignHint)
{
    API_ENTER(getClassAlignmentRequirement);
    unsigned temp = wrapHnd->getClassAlignmentRequirement(cls, fDoubleAlignHint);
    API_LEAVE(getClassAlignmentRequirement);
    return temp;
}

unsigned WrapICorJitInfo::getClassGClayout(
            CORINFO_CLASS_HANDLE        cls,        /* IN */
            BYTE                       *gcPtrs      /* OUT */)
{
    API_ENTER(getClassGClayout);
    unsigned temp = wrapHnd->getClassGClayout(cls, gcPtrs);
    API_LEAVE(getClassGClayout);
    return temp;
}

unsigned WrapICorJitInfo::getClassNumInstanceFields(
            CORINFO_CLASS_HANDLE        cls        /* IN */)
{
    API_ENTER(getClassNumInstanceFields);
    unsigned temp = wrapHnd->getClassNumInstanceFields(cls);
    API_LEAVE(getClassNumInstanceFields);
    return temp;
}

CORINFO_FIELD_HANDLE WrapICorJitInfo::getFieldInClass(
            CORINFO_CLASS_HANDLE clsHnd,
            INT num)
{
    API_ENTER(getFieldInClass);
    CORINFO_FIELD_HANDLE temp = wrapHnd->getFieldInClass(clsHnd, num);
    API_LEAVE(getFieldInClass);
    return temp;
}

BOOL WrapICorJitInfo::checkMethodModifier(
            CORINFO_METHOD_HANDLE hMethod,
            LPCSTR modifier,
            BOOL fOptional)
{
    API_ENTER(checkMethodModifier);
    BOOL result = wrapHnd->checkMethodModifier(hMethod, modifier, fOptional);
    API_LEAVE(checkMethodModifier);
    return result;
}

CorInfoHelpFunc WrapICorJitInfo::getNewHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_METHOD_HANDLE    callerHandle,
            bool * pHasSideEffects)
{
    API_ENTER(getNewHelper);
    CorInfoHelpFunc temp = wrapHnd->getNewHelper(pResolvedToken, callerHandle, pHasSideEffects);
    API_LEAVE(getNewHelper);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getNewArrHelper(
            CORINFO_CLASS_HANDLE        arrayCls)
{
    API_ENTER(getNewArrHelper);
    CorInfoHelpFunc temp = wrapHnd->getNewArrHelper(arrayCls);
    API_LEAVE(getNewArrHelper);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getCastingHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            bool fThrowing)
{
    API_ENTER(getCastingHelper);
    CorInfoHelpFunc temp = wrapHnd->getCastingHelper(pResolvedToken, fThrowing);
    API_LEAVE(getCastingHelper);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getSharedCCtorHelper(
            CORINFO_CLASS_HANDLE clsHnd)
{
    API_ENTER(getSharedCCtorHelper);
    CorInfoHelpFunc temp = wrapHnd->getSharedCCtorHelper(clsHnd);
    API_LEAVE(getSharedCCtorHelper);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getSecurityPrologHelper(
            CORINFO_METHOD_HANDLE   ftn)
{
    API_ENTER(getSecurityPrologHelper);
    CorInfoHelpFunc temp = wrapHnd->getSecurityPrologHelper(ftn);
    API_LEAVE(getSecurityPrologHelper);
    return temp;
}

CORINFO_CLASS_HANDLE  WrapICorJitInfo::getTypeForBox(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getTypeForBox);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getTypeForBox(cls);
    API_LEAVE(getTypeForBox);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getBoxHelper(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getBoxHelper);
    CorInfoHelpFunc temp = wrapHnd->getBoxHelper(cls);
    API_LEAVE(getBoxHelper);
    return temp;
}

CorInfoHelpFunc WrapICorJitInfo::getUnBoxHelper(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getUnBoxHelper);
    CorInfoHelpFunc temp = wrapHnd->getUnBoxHelper(cls);
    API_LEAVE(getUnBoxHelper);
    return temp;
}

bool WrapICorJitInfo::getReadyToRunHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_LOOKUP_KIND *    pGenericLookupKind,
            CorInfoHelpFunc          id,
            CORINFO_CONST_LOOKUP *   pLookup)
{
    API_ENTER(getReadyToRunHelper);
    bool result = wrapHnd->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, id, pLookup);
    API_LEAVE(getReadyToRunHelper);
    return result;
}

void WrapICorJitInfo::getReadyToRunDelegateCtorHelper(
    CORINFO_RESOLVED_TOKEN * pTargetMethod,
    CORINFO_CLASS_HANDLE     delegateType,
    CORINFO_LOOKUP *   pLookup)
{
    API_ENTER(getReadyToRunDelegateCtorHelper);
    wrapHnd->getReadyToRunDelegateCtorHelper(pTargetMethod, delegateType, pLookup);
    API_LEAVE(getReadyToRunDelegateCtorHelper);
}

const char* WrapICorJitInfo::getHelperName(
            CorInfoHelpFunc funcNum)
{
    API_ENTER(getHelperName);
    const char* temp = wrapHnd->getHelperName(funcNum);
    API_LEAVE(getHelperName);
    return temp;
}

CorInfoInitClassResult WrapICorJitInfo::initClass(
            CORINFO_FIELD_HANDLE    field,

            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONTEXT_HANDLE  context,
            BOOL                    speculative)
{
    API_ENTER(initClass);
    CorInfoInitClassResult temp = wrapHnd->initClass(field, method, context, speculative);
    API_LEAVE(initClass);
    return temp;
}

void WrapICorJitInfo::classMustBeLoadedBeforeCodeIsRun(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(classMustBeLoadedBeforeCodeIsRun);
    wrapHnd->classMustBeLoadedBeforeCodeIsRun(cls);
    API_LEAVE(classMustBeLoadedBeforeCodeIsRun);
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getBuiltinClass(
            CorInfoClassId              classId)
{
    API_ENTER(getBuiltinClass);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getBuiltinClass(classId);
    API_LEAVE(getBuiltinClass);
    return temp;
}

CorInfoType WrapICorJitInfo::getTypeForPrimitiveValueClass(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getTypeForPrimitiveValueClass);
    CorInfoType temp = wrapHnd->getTypeForPrimitiveValueClass(cls);
    API_LEAVE(getTypeForPrimitiveValueClass);
    return temp;
}

CorInfoType WrapICorJitInfo::getTypeForPrimitiveNumericClass(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getTypeForPrimitiveNumericClass);
    CorInfoType temp = wrapHnd->getTypeForPrimitiveNumericClass(cls);
    API_LEAVE(getTypeForPrimitiveNumericClass);
    return temp;
}

BOOL WrapICorJitInfo::canCast(
            CORINFO_CLASS_HANDLE        child,
            CORINFO_CLASS_HANDLE        parent  )
{
    API_ENTER(canCast);
    BOOL temp = wrapHnd->canCast(child, parent);
    API_LEAVE(canCast);
    return temp;
}

BOOL WrapICorJitInfo::areTypesEquivalent(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2)
{
    API_ENTER(areTypesEquivalent);
    BOOL temp = wrapHnd->areTypesEquivalent(cls1, cls2);
    API_LEAVE(areTypesEquivalent);
    return temp;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::mergeClasses(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2)
{
    API_ENTER(mergeClasses);
    CORINFO_CLASS_HANDLE temp = wrapHnd->mergeClasses(cls1, cls2);
    API_LEAVE(mergeClasses);
    return temp;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getParentType(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getParentType);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getParentType(cls);
    API_LEAVE(getParentType);
    return temp;
}

CorInfoType WrapICorJitInfo::getChildType(
            CORINFO_CLASS_HANDLE       clsHnd,
            CORINFO_CLASS_HANDLE       *clsRet)
{
    API_ENTER(getChildType);
    CorInfoType temp = wrapHnd->getChildType(clsHnd, clsRet);
    API_LEAVE(getChildType);
    return temp;
}

BOOL WrapICorJitInfo::satisfiesClassConstraints(
            CORINFO_CLASS_HANDLE cls)
{
    API_ENTER(satisfiesClassConstraints);
    BOOL temp = wrapHnd->satisfiesClassConstraints(cls);
    API_LEAVE(satisfiesClassConstraints);
    return temp;

}

BOOL WrapICorJitInfo::isSDArray(
            CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(isSDArray);
    BOOL temp = wrapHnd->isSDArray(cls);
    API_LEAVE(isSDArray);
    return temp;
}

unsigned WrapICorJitInfo::getArrayRank(
        CORINFO_CLASS_HANDLE        cls)
{
    API_ENTER(getArrayRank);
    unsigned result = wrapHnd->getArrayRank(cls);
    API_LEAVE(getArrayRank);
    return result;
}

void * WrapICorJitInfo::getArrayInitializationData(
        CORINFO_FIELD_HANDLE        field,
        DWORD                       size)
{
    API_ENTER(getArrayInitializationData);
    void *temp = wrapHnd->getArrayInitializationData(field, size);
    API_LEAVE(getArrayInitializationData);
    return temp;
}

CorInfoIsAccessAllowedResult WrapICorJitInfo::canAccessClass(
                    CORINFO_RESOLVED_TOKEN * pResolvedToken,
                    CORINFO_METHOD_HANDLE   callerHandle,
                    CORINFO_HELPER_DESC    *pAccessHelper)
{
    API_ENTER(canAccessClass);
    CorInfoIsAccessAllowedResult temp = wrapHnd->canAccessClass(pResolvedToken, callerHandle, pAccessHelper);
    API_LEAVE(canAccessClass);
    return temp;
}

/**********************************************************************************/
//
// ICorFieldInfo
//
/**********************************************************************************/

const char* WrapICorJitInfo::getFieldName(
                    CORINFO_FIELD_HANDLE        ftn,        /* IN */
                    const char                **moduleName  /* OUT */)
{
    API_ENTER(getFieldName);
    const char* temp = wrapHnd->getFieldName(ftn, moduleName);
    API_LEAVE(getFieldName);
    return temp;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getFieldClass(
                    CORINFO_FIELD_HANDLE    field)
{
    API_ENTER(getFieldClass);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getFieldClass(field);
    API_LEAVE(getFieldClass);
    return temp;
}

CorInfoType WrapICorJitInfo::getFieldType(
                        CORINFO_FIELD_HANDLE    field,
                        CORINFO_CLASS_HANDLE   *structType,
                        CORINFO_CLASS_HANDLE    memberParent/* IN */)
{
    API_ENTER(getFieldType);
    CorInfoType temp = wrapHnd->getFieldType(field, structType, memberParent);
    API_LEAVE(getFieldType);
    return temp;
}

unsigned WrapICorJitInfo::getFieldOffset(
                    CORINFO_FIELD_HANDLE    field)
{
    API_ENTER(getFieldOffset);
    unsigned temp = wrapHnd->getFieldOffset(field);
    API_LEAVE(getFieldOffset);
    return temp;
}

bool WrapICorJitInfo::isWriteBarrierHelperRequired(
                    CORINFO_FIELD_HANDLE    field)
{
    API_ENTER(isWriteBarrierHelperRequired);
    bool result = wrapHnd->isWriteBarrierHelperRequired(field);
    API_LEAVE(isWriteBarrierHelperRequired);
    return result;
}

void WrapICorJitInfo::getFieldInfo(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            CORINFO_METHOD_HANDLE  callerHandle,
                            CORINFO_ACCESS_FLAGS   flags,
                            CORINFO_FIELD_INFO    *pResult)
{
    API_ENTER(getFieldInfo);
    wrapHnd->getFieldInfo(pResolvedToken, callerHandle, flags, pResult);
    API_LEAVE(getFieldInfo);
}

bool WrapICorJitInfo::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    API_ENTER(isFieldStatic);
    bool result = wrapHnd->isFieldStatic(fldHnd);
    API_LEAVE(isFieldStatic);
    return result;
}

/*********************************************************************************/
//
// ICorDebugInfo
//
/*********************************************************************************/

void WrapICorJitInfo::getBoundaries(
            CORINFO_METHOD_HANDLE   ftn,
            unsigned int           *cILOffsets,
            DWORD                 **pILOffsets,

            ICorDebugInfo::BoundaryTypes *implictBoundaries)
{
    API_ENTER(getBoundaries);
    wrapHnd->getBoundaries(ftn, cILOffsets, pILOffsets, implictBoundaries);
    API_LEAVE(getBoundaries);
}

void WrapICorJitInfo::setBoundaries(
            CORINFO_METHOD_HANDLE   ftn,
            ULONG32                 cMap,
            ICorDebugInfo::OffsetMapping *pMap)
{
    API_ENTER(setBoundaries);
    wrapHnd->setBoundaries(ftn, cMap, pMap);
    API_LEAVE(setBoundaries);
}

void WrapICorJitInfo::getVars(
        CORINFO_METHOD_HANDLE           ftn,
        ULONG32                        *cVars,
        ICorDebugInfo::ILVarInfo       **vars,
        bool                           *extendOthers)

{
    API_ENTER(getVars);
    wrapHnd->getVars(ftn, cVars, vars, extendOthers);
    API_LEAVE(getVars);
}

void WrapICorJitInfo::setVars(
        CORINFO_METHOD_HANDLE           ftn,
        ULONG32                         cVars,
        ICorDebugInfo::NativeVarInfo   *vars)

{
    API_ENTER(setVars);
    wrapHnd->setVars(ftn, cVars, vars);
    API_LEAVE(setVars);
}

void * WrapICorJitInfo::allocateArray(
                    ULONG              cBytes)
{
    API_ENTER(allocateArray);
    void *temp = wrapHnd->allocateArray(cBytes);
    API_LEAVE(allocateArray);
    return temp;
}

void WrapICorJitInfo::freeArray(
        void               *array)
{
    API_ENTER(freeArray);
    wrapHnd->freeArray(array);
    API_LEAVE(freeArray);
}

/*********************************************************************************/
//
// ICorArgInfo
//
/*********************************************************************************/

CORINFO_ARG_LIST_HANDLE WrapICorJitInfo::getArgNext(
        CORINFO_ARG_LIST_HANDLE     args            /* IN */)
{
    API_ENTER(getArgNext);
    CORINFO_ARG_LIST_HANDLE temp = wrapHnd->getArgNext(args);
    API_LEAVE(getArgNext);
    return temp;
}

CorInfoTypeWithMod WrapICorJitInfo::getArgType(
        CORINFO_SIG_INFO*           sig,            /* IN */
        CORINFO_ARG_LIST_HANDLE     args,           /* IN */
        CORINFO_CLASS_HANDLE       *vcTypeRet       /* OUT */)
{
    API_ENTER(getArgType);
    CorInfoTypeWithMod temp = wrapHnd->getArgType(sig, args, vcTypeRet);
    API_LEAVE(getArgType);
    return temp;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getArgClass(
        CORINFO_SIG_INFO*           sig,            /* IN */
        CORINFO_ARG_LIST_HANDLE     args            /* IN */)
{
    API_ENTER(getArgClass);
    CORINFO_CLASS_HANDLE temp = wrapHnd->getArgClass(sig, args);
    API_LEAVE(getArgClass);
    return temp;
}

CorInfoType WrapICorJitInfo::getHFAType(
        CORINFO_CLASS_HANDLE hClass)
{
    API_ENTER(getHFAType);
    CorInfoType temp = wrapHnd->getHFAType(hClass);
    API_LEAVE(getHFAType);
    return temp;
}

HRESULT WrapICorJitInfo::GetErrorHRESULT(
        struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    API_ENTER(GetErrorHRESULT);
    HRESULT temp = wrapHnd->GetErrorHRESULT(pExceptionPointers);
    API_LEAVE(GetErrorHRESULT);
    return temp;
}

ULONG WrapICorJitInfo::GetErrorMessage(
        __inout_ecount(bufferLength) LPWSTR buffer,
        ULONG bufferLength)
{
    API_ENTER(GetErrorMessage);
    ULONG temp = wrapHnd->GetErrorMessage(buffer, bufferLength);
    API_LEAVE(GetErrorMessage);
    return temp;
}

int WrapICorJitInfo::FilterException(
        struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    API_ENTER(FilterException);
    int temp = wrapHnd->FilterException(pExceptionPointers);
    API_LEAVE(FilterException);
    return temp;
}

void WrapICorJitInfo::HandleException(
        struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    API_ENTER(HandleException);
    wrapHnd->HandleException(pExceptionPointers);
    API_LEAVE(HandleException);
}

void WrapICorJitInfo::ThrowExceptionForJitResult(
        HRESULT result)
{
    API_ENTER(ThrowExceptionForJitResult);
    wrapHnd->ThrowExceptionForJitResult(result);
    API_LEAVE(ThrowExceptionForJitResult);
}

void WrapICorJitInfo::ThrowExceptionForHelper(
        const CORINFO_HELPER_DESC * throwHelper)
{
    API_ENTER(ThrowExceptionForHelper);
    wrapHnd->ThrowExceptionForHelper(throwHelper);
    API_LEAVE(ThrowExceptionForHelper);
}

void WrapICorJitInfo::getEEInfo(
            CORINFO_EE_INFO            *pEEInfoOut)
{
    API_ENTER(getEEInfo);
    wrapHnd->getEEInfo(pEEInfoOut);
    API_LEAVE(getEEInfo);
}

LPCWSTR WrapICorJitInfo::getJitTimeLogFilename()
{
    API_ENTER(getJitTimeLogFilename);
    LPCWSTR temp = wrapHnd->getJitTimeLogFilename();
    API_LEAVE(getJitTimeLogFilename);
    return temp;
}

mdMethodDef WrapICorJitInfo::getMethodDefFromMethod(
        CORINFO_METHOD_HANDLE hMethod)
{
    API_ENTER(getMethodDefFromMethod);
    mdMethodDef result = wrapHnd->getMethodDefFromMethod(hMethod);
    API_LEAVE(getMethodDefFromMethod);
    return result;
}

const char* WrapICorJitInfo::getMethodName(
        CORINFO_METHOD_HANDLE       ftn,        /* IN */
        const char                **moduleName  /* OUT */)
{
    API_ENTER(getMethodName);
    const char* temp = wrapHnd->getMethodName(ftn, moduleName);
    API_LEAVE(getMethodName);
    return temp;
}

const char* WrapICorJitInfo::getMethodNameFromMetadata(
        CORINFO_METHOD_HANDLE       ftn,                 /* IN */
        const char                **className,           /* OUT */
        const char                **namespaceName,       /* OUT */
        const char                **enclosingClassName  /* OUT */)
{
    API_ENTER(getMethodNameFromMetadata);
    const char* temp = wrapHnd->getMethodNameFromMetaData(ftn, className, namespaceName, enclosingClassName);
    API_LEAVE(getMethodNameFromMetadata);
    return temp;
}

unsigned WrapICorJitInfo::getMethodHash(
        CORINFO_METHOD_HANDLE       ftn         /* IN */)
{
    API_ENTER(getMethodHash);
    unsigned temp = wrapHnd->getMethodHash(ftn);
    API_LEAVE(getMethodHash);
    return temp;
}

size_t WrapICorJitInfo::findNameOfToken(
        CORINFO_MODULE_HANDLE       module,     /* IN  */
        mdToken                     metaTOK,     /* IN  */
        __out_ecount(FQNameCapacity) char * szFQName, /* OUT */
        size_t FQNameCapacity  /* IN */)
{
    API_ENTER(findNameOfToken);
    size_t result = wrapHnd->findNameOfToken(module, metaTOK, szFQName, FQNameCapacity);
    API_LEAVE(findNameOfToken);
    return result;
}

bool WrapICorJitInfo::getSystemVAmd64PassStructInRegisterDescriptor(
        /* IN */    CORINFO_CLASS_HANDLE        structHnd,
        /* OUT */   SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    API_ENTER(getSystemVAmd64PassStructInRegisterDescriptor);
    bool result = wrapHnd->getSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
    API_LEAVE(getSystemVAmd64PassStructInRegisterDescriptor);
    return result;
}

DWORD WrapICorJitInfo::getThreadTLSIndex(
                void                  **ppIndirection)
{
    API_ENTER(getThreadTLSIndex);
    DWORD temp = wrapHnd->getThreadTLSIndex(ppIndirection);
    API_LEAVE(getThreadTLSIndex);
    return temp;
}

const void * WrapICorJitInfo::getInlinedCallFrameVptr(
                void                  **ppIndirection)
{
    API_ENTER(getInlinedCallFrameVptr);
    const void* temp = wrapHnd->getInlinedCallFrameVptr(ppIndirection);
    API_LEAVE(getInlinedCallFrameVptr);
    return temp;
}

LONG * WrapICorJitInfo::getAddrOfCaptureThreadGlobal(
                void                  **ppIndirection)
{
    API_ENTER(getAddrOfCaptureThreadGlobal);
    LONG * temp = wrapHnd->getAddrOfCaptureThreadGlobal(ppIndirection);
    API_LEAVE(getAddrOfCaptureThreadGlobal);
    return temp;
}

void* WrapICorJitInfo::getHelperFtn(
                CorInfoHelpFunc         ftnNum,
                void                  **ppIndirection)
{
    API_ENTER(getHelperFtn);
    void *temp = wrapHnd->getHelperFtn(ftnNum, ppIndirection);
    API_LEAVE(getHelperFtn);
    return temp;
}

void WrapICorJitInfo::getFunctionEntryPoint(
                            CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
                            CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
                            CORINFO_ACCESS_FLAGS    accessFlags)
{
    API_ENTER(getFunctionEntryPoint);
    wrapHnd->getFunctionEntryPoint(ftn, pResult, accessFlags);
    API_LEAVE(getFunctionEntryPoint);
}

void WrapICorJitInfo::getFunctionFixedEntryPoint(
                            CORINFO_METHOD_HANDLE   ftn,
                            CORINFO_CONST_LOOKUP *  pResult)
{
    API_ENTER(getFunctionFixedEntryPoint);
    wrapHnd->getFunctionFixedEntryPoint(ftn, pResult);
    API_LEAVE(getFunctionFixedEntryPoint);
}

void* WrapICorJitInfo::getMethodSync(
                CORINFO_METHOD_HANDLE               ftn,
                void                  **ppIndirection)
{
    API_ENTER(getMethodSync);
    void *temp = wrapHnd->getMethodSync(ftn, ppIndirection);
    API_LEAVE(getMethodSync);
    return temp;
}


CorInfoHelpFunc WrapICorJitInfo::getLazyStringLiteralHelper(
    CORINFO_MODULE_HANDLE   handle)
{
    API_ENTER(getLazyStringLiteralHelper);
    CorInfoHelpFunc temp = wrapHnd->getLazyStringLiteralHelper(handle);
    API_LEAVE(getLazyStringLiteralHelper);
    return temp;
}

CORINFO_MODULE_HANDLE WrapICorJitInfo::embedModuleHandle(
                CORINFO_MODULE_HANDLE   handle,
                void                  **ppIndirection)
{
    API_ENTER(embedModuleHandle);
    CORINFO_MODULE_HANDLE temp = wrapHnd->embedModuleHandle(handle, ppIndirection);
    API_LEAVE(embedModuleHandle);
    return temp;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::embedClassHandle(
                CORINFO_CLASS_HANDLE    handle,
                void                  **ppIndirection)
{
    API_ENTER(embedClassHandle);
    CORINFO_CLASS_HANDLE temp = wrapHnd->embedClassHandle(handle, ppIndirection);
    API_LEAVE(embedClassHandle);
    return temp;
}

CORINFO_METHOD_HANDLE WrapICorJitInfo::embedMethodHandle(
                CORINFO_METHOD_HANDLE   handle,
                void                  **ppIndirection)
{
    API_ENTER(embedMethodHandle);
    CORINFO_METHOD_HANDLE temp = wrapHnd->embedMethodHandle(handle, ppIndirection);
    API_LEAVE(embedMethodHandle);
    return temp;
}

CORINFO_FIELD_HANDLE WrapICorJitInfo::embedFieldHandle(
                CORINFO_FIELD_HANDLE    handle,
                void                  **ppIndirection)
{
    API_ENTER(embedFieldHandle);
    CORINFO_FIELD_HANDLE temp = wrapHnd->embedFieldHandle(handle, ppIndirection);
    API_LEAVE(embedFieldHandle);
    return temp;
}

void WrapICorJitInfo::embedGenericHandle(
                    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
                    BOOL                            fEmbedParent,
                    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    API_ENTER(embedGenericHandle);
    wrapHnd->embedGenericHandle(pResolvedToken, fEmbedParent, pResult);
    API_LEAVE(embedGenericHandle);
}

CORINFO_LOOKUP_KIND WrapICorJitInfo::getLocationOfThisType(
                CORINFO_METHOD_HANDLE context)
{
    API_ENTER(getLocationOfThisType);
    CORINFO_LOOKUP_KIND temp = wrapHnd->getLocationOfThisType(context);
    API_LEAVE(getLocationOfThisType);
    return temp;
}

void* WrapICorJitInfo::getPInvokeUnmanagedTarget(
                CORINFO_METHOD_HANDLE   method,
                void                  **ppIndirection)
{
    API_ENTER(getPInvokeUnmanagedTarget);
    void *result = wrapHnd->getPInvokeUnmanagedTarget(method, ppIndirection);
    API_LEAVE(getPInvokeUnmanagedTarget);
    return result;
}

void* WrapICorJitInfo::getAddressOfPInvokeFixup(
                CORINFO_METHOD_HANDLE   method,
                void                  **ppIndirection)
{
    API_ENTER(getAddressOfPInvokeFixup);
    void *temp = wrapHnd->getAddressOfPInvokeFixup(method, ppIndirection);
    API_LEAVE(getAddressOfPInvokeFixup);
    return temp;
}

void WrapICorJitInfo::getAddressOfPInvokeTarget(
                CORINFO_METHOD_HANDLE   method,
                CORINFO_CONST_LOOKUP   *pLookup)
{
    API_ENTER(getAddressOfPInvokeTarget);
    wrapHnd->getAddressOfPInvokeTarget(method, pLookup);
    API_LEAVE(getAddressOfPInvokeTarget);
}

LPVOID WrapICorJitInfo::GetCookieForPInvokeCalliSig(
        CORINFO_SIG_INFO* szMetaSig,
        void           ** ppIndirection)
{
    API_ENTER(GetCookieForPInvokeCalliSig);
    LPVOID temp = wrapHnd->GetCookieForPInvokeCalliSig(szMetaSig, ppIndirection);
    API_LEAVE(GetCookieForPInvokeCalliSig);
    return temp;
}

bool WrapICorJitInfo::canGetCookieForPInvokeCalliSig(
                CORINFO_SIG_INFO* szMetaSig)
{
    API_ENTER(canGetCookieForPInvokeCalliSig);
    bool temp = wrapHnd->canGetCookieForPInvokeCalliSig(szMetaSig);
    API_LEAVE(canGetCookieForPInvokeCalliSig);
    return temp;
}

CORINFO_JUST_MY_CODE_HANDLE WrapICorJitInfo::getJustMyCodeHandle(
                CORINFO_METHOD_HANDLE       method,
                CORINFO_JUST_MY_CODE_HANDLE**ppIndirection)
{
    API_ENTER(getJustMyCodeHandle);
    CORINFO_JUST_MY_CODE_HANDLE temp = wrapHnd->getJustMyCodeHandle(method, ppIndirection);
    API_LEAVE(getJustMyCodeHandle);
    return temp;
}

void WrapICorJitInfo::GetProfilingHandle(
                    BOOL                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    BOOL                      *pbIndirectedHandles)
{
    API_ENTER(GetProfilingHandle);
    wrapHnd->GetProfilingHandle(pbHookFunction, pProfilerHandle, pbIndirectedHandles);
    API_LEAVE(GetProfilingHandle);
}

void WrapICorJitInfo::getCallInfo(
                    CORINFO_RESOLVED_TOKEN * pResolvedToken,
                    CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                    CORINFO_METHOD_HANDLE   callerHandle,
                    CORINFO_CALLINFO_FLAGS  flags,
                    CORINFO_CALL_INFO       *pResult)
{
    API_ENTER(getCallInfo);
    wrapHnd->getCallInfo(pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult);
    API_LEAVE(getCallInfo);
}

BOOL WrapICorJitInfo::canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                                        CORINFO_CLASS_HANDLE hInstanceType)
{
    API_ENTER(canAccessFamily);
    BOOL temp = wrapHnd->canAccessFamily(hCaller, hInstanceType);
    API_LEAVE(canAccessFamily);
    return temp;
}

BOOL WrapICorJitInfo::isRIDClassDomainID(CORINFO_CLASS_HANDLE cls)
{
    API_ENTER(isRIDClassDomainID);
    BOOL result = wrapHnd->isRIDClassDomainID(cls);
    API_LEAVE(isRIDClassDomainID);
    return result;
}

unsigned WrapICorJitInfo::getClassDomainID(
                CORINFO_CLASS_HANDLE    cls,
                void                  **ppIndirection)
{
    API_ENTER(getClassDomainID);
    unsigned temp = wrapHnd->getClassDomainID(cls, ppIndirection);
    API_LEAVE(getClassDomainID);
    return temp;
}

void* WrapICorJitInfo::getFieldAddress(
                CORINFO_FIELD_HANDLE    field,
                void                  **ppIndirection)
{
    API_ENTER(getFieldAddress);
    void *temp = wrapHnd->getFieldAddress(field, ppIndirection);
    API_LEAVE(getFieldAddress);
    return temp;
}

CORINFO_VARARGS_HANDLE WrapICorJitInfo::getVarArgsHandle(
                CORINFO_SIG_INFO       *pSig,
                void                  **ppIndirection)
{
    API_ENTER(getVarArgsHandle);
    CORINFO_VARARGS_HANDLE temp = wrapHnd->getVarArgsHandle(pSig, ppIndirection);
    API_LEAVE(getVarArgsHandle);
    return temp;
}

bool WrapICorJitInfo::canGetVarArgsHandle(
                CORINFO_SIG_INFO       *pSig)
{
    API_ENTER(canGetVarArgsHandle);
    bool temp = wrapHnd->canGetVarArgsHandle(pSig);
    API_LEAVE(canGetVarArgsHandle);
    return temp;
}

InfoAccessType WrapICorJitInfo::constructStringLiteral(
                CORINFO_MODULE_HANDLE   module,
                mdToken                 metaTok,
                void                  **ppValue)
{
    API_ENTER(constructStringLiteral);
    InfoAccessType temp = wrapHnd->constructStringLiteral(module, metaTok, ppValue);
    API_LEAVE(constructStringLiteral);
    return temp;
}

InfoAccessType WrapICorJitInfo::emptyStringLiteral(void **ppValue)
{
    API_ENTER(emptyStringLiteral);
    InfoAccessType temp = wrapHnd->emptyStringLiteral(ppValue);
    API_LEAVE(emptyStringLiteral);
    return temp;
}

DWORD WrapICorJitInfo::getFieldThreadLocalStoreID(
                CORINFO_FIELD_HANDLE    field,
                void                  **ppIndirection)
{
    API_ENTER(getFieldThreadLocalStoreID);
    DWORD temp = wrapHnd->getFieldThreadLocalStoreID(field, ppIndirection);
    API_LEAVE(getFieldThreadLocalStoreID);
    return temp;
}

void WrapICorJitInfo::setOverride(
            ICorDynamicInfo             *pOverride,
            CORINFO_METHOD_HANDLE       currentMethod)
{
    API_ENTER(setOverride);
    wrapHnd->setOverride(pOverride, currentMethod);
    API_LEAVE(setOverride);
}

void WrapICorJitInfo::addActiveDependency(
            CORINFO_MODULE_HANDLE       moduleFrom,
            CORINFO_MODULE_HANDLE       moduleTo)
{
    API_ENTER(addActiveDependency);
    wrapHnd->addActiveDependency(moduleFrom, moduleTo);
    API_LEAVE(addActiveDependency);
}

CORINFO_METHOD_HANDLE WrapICorJitInfo::GetDelegateCtor(
        CORINFO_METHOD_HANDLE  methHnd,
        CORINFO_CLASS_HANDLE   clsHnd,
        CORINFO_METHOD_HANDLE  targetMethodHnd,
        DelegateCtorArgs *     pCtorData)
{
    API_ENTER(GetDelegateCtor);
    CORINFO_METHOD_HANDLE temp = wrapHnd->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, pCtorData);
    API_LEAVE(GetDelegateCtor);
    return temp;
}

void WrapICorJitInfo::MethodCompileComplete(
            CORINFO_METHOD_HANDLE methHnd)
{
    API_ENTER(MethodCompileComplete);
    wrapHnd->MethodCompileComplete(methHnd);
    API_LEAVE(MethodCompileComplete);
}

void* WrapICorJitInfo::getTailCallCopyArgsThunk(
                CORINFO_SIG_INFO       *pSig,
                CorInfoHelperTailCallSpecialHandling flags)
{
    API_ENTER(getTailCallCopyArgsThunk);
    void *result = wrapHnd->getTailCallCopyArgsThunk(pSig, flags);
    API_LEAVE(getTailCallCopyArgsThunk);
    return result;
}

/*********************************************************************************/
//
// ICorJitInfo
//
/*********************************************************************************/

DWORD WrapICorJitInfo::getJitFlags(CORJIT_FLAGS *jitFlags, DWORD sizeInBytes)
{
    API_ENTER(getJitFlags);
    DWORD result = wrapHnd->getJitFlags(jitFlags, sizeInBytes);
    API_LEAVE(getJitFlags);
    return result;
}

bool WrapICorJitInfo::runWithErrorTrap(void(*function)(void*), void *param)
{
    return wrapHnd->runWithErrorTrap(function, param);
}

IEEMemoryManager* WrapICorJitInfo::getMemoryManager()
{
    API_ENTER(getMemoryManager);
    IEEMemoryManager * temp = wrapHnd->getMemoryManager();
    API_LEAVE(getMemoryManager);
    return temp;
}

void WrapICorJitInfo::allocMem(
        ULONG               hotCodeSize,    /* IN */
        ULONG               coldCodeSize,   /* IN */
        ULONG               roDataSize,     /* IN */
        ULONG               xcptnsCount,    /* IN */
        CorJitAllocMemFlag  flag,           /* IN */
        void **             hotCodeBlock,   /* OUT */
        void **             coldCodeBlock,  /* OUT */
        void **             roDataBlock     /* OUT */)
{
    API_ENTER(allocMem);
    wrapHnd->allocMem(hotCodeSize, coldCodeSize, roDataSize, xcptnsCount, flag, hotCodeBlock, coldCodeBlock, roDataBlock);
    API_LEAVE(allocMem);
}

void WrapICorJitInfo::reserveUnwindInfo(
        BOOL                isFunclet,             /* IN */
        BOOL                isColdCode,            /* IN */
        ULONG               unwindSize             /* IN */)
{
    API_ENTER(reserveUnwindInfo);
    wrapHnd->reserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    API_LEAVE(reserveUnwindInfo);
}

void WrapICorJitInfo::allocUnwindInfo(
        BYTE *              pHotCode,              /* IN */
        BYTE *              pColdCode,             /* IN */
        ULONG               startOffset,           /* IN */
        ULONG               endOffset,             /* IN */
        ULONG               unwindSize,            /* IN */
        BYTE *              pUnwindBlock,          /* IN */
        CorJitFuncKind      funcKind               /* IN */)
{
    API_ENTER(allocUnwindInfo);
    wrapHnd->allocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
    API_LEAVE(allocUnwindInfo);
}

void *WrapICorJitInfo::allocGCInfo(size_t size /* IN */)
{
    API_ENTER(allocGCInfo);
    void *temp = wrapHnd->allocGCInfo(size);
    API_LEAVE(allocGCInfo);
    return temp;
}

void WrapICorJitInfo::yieldExecution()
{
    API_ENTER(yieldExecution); //Nothing to record
    wrapHnd->yieldExecution();
    API_LEAVE(yieldExecution); //Nothing to recor)
}

void WrapICorJitInfo::setEHcount(unsigned cEH /* IN */)
{
    API_ENTER(setEHcount);
    wrapHnd->setEHcount(cEH);
    API_LEAVE(setEHcount);
}

void WrapICorJitInfo::setEHinfo(
        unsigned EHnumber, /* IN  */
        const CORINFO_EH_CLAUSE *clause /* IN */)
{
    API_ENTER(setEHinfo);
    wrapHnd->setEHinfo(EHnumber, clause);
    API_LEAVE(setEHinfo);
}

BOOL WrapICorJitInfo::logMsg(unsigned level, const char* fmt, va_list args)
{
    API_ENTER(logMsg);
    BOOL result = wrapHnd->logMsg(level, fmt, args);
    API_LEAVE(logMsg);
    return result;
}

int WrapICorJitInfo::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    API_ENTER(doAssert);
    int result = wrapHnd->doAssert(szFile, iLine, szExpr);
    API_LEAVE(doAssert);
    return result;
}

void WrapICorJitInfo::reportFatalError(CorJitResult result)
{
    API_ENTER(reportFatalError);
    wrapHnd->reportFatalError(result);
    API_LEAVE(reportFatalError);
}

HRESULT WrapICorJitInfo::allocMethodBlockCounts(
        UINT32 count,
        BlockCounts **pBlockCounts)
{
    API_ENTER(allocMethodBlockCounts);
    HRESULT result = wrapHnd->allocMethodBlockCounts(count, pBlockCounts);
    API_LEAVE(allocMethodBlockCounts);
    return result;
}

HRESULT WrapICorJitInfo::getMethodBlockCounts(
        CORINFO_METHOD_HANDLE ftnHnd,
        UINT32 *pCount,
        BlockCounts **pBlockCounts,
        UINT32 *pNumRuns)
{
    API_ENTER(getMethodBlockCounts);
    HRESULT temp = wrapHnd->getMethodBlockCounts(ftnHnd, pCount, pBlockCounts, pNumRuns);
    API_LEAVE(getMethodBlockCounts);
    return temp;
}

void WrapICorJitInfo::recordCallSite(
    ULONG                 instrOffset,  /* IN */
    CORINFO_SIG_INFO *    callSig,      /* IN */
    CORINFO_METHOD_HANDLE methodHandle  /* IN */)
{
    API_ENTER(recordCallSite);
    wrapHnd->recordCallSite(instrOffset, callSig, methodHandle);
    API_LEAVE(recordCallSite);
}

void WrapICorJitInfo::recordRelocation(
        void *location, /* IN  */
        void *target, /* IN  */
        WORD fRelocType, /* IN  */
        WORD slotNum, /* IN  */
        INT32 addlDelta /* IN  */)
{
    API_ENTER(recordRelocation);
    wrapHnd->recordRelocation(location, target, fRelocType, slotNum, addlDelta);
    API_LEAVE(recordRelocation);
}

WORD WrapICorJitInfo::getRelocTypeHint(void *target)
{
    API_ENTER(getRelocTypeHint);
    WORD result = wrapHnd->getRelocTypeHint(target);
    API_LEAVE(getRelocTypeHint);
    return result;
}

void WrapICorJitInfo::getModuleNativeEntryPointRange(
            void **pStart, /* OUT */
            void **pEnd    /* OUT */)
{
    API_ENTER(getModuleNativeEntryPointRange);
    wrapHnd->getModuleNativeEntryPointRange(pStart, pEnd);
    API_LEAVE(getModuleNativeEntryPointRange);
}

DWORD WrapICorJitInfo::getExpectedTargetArchitecture()
{
    API_ENTER(getExpectedTargetArchitecture);
    DWORD result = wrapHnd->getExpectedTargetArchitecture();
    API_LEAVE(getExpectedTargetArchitecture);
    return result;
}

CORINFO_METHOD_HANDLE WrapICorJitInfo::resolveVirtualMethod(
    CORINFO_METHOD_HANDLE       virtualMethod,          /* IN */
    CORINFO_CLASS_HANDLE        implementingClass,      /* IN */
    CORINFO_CONTEXT_HANDLE      ownerType = NULL        /* IN */
)
{
    API_ENTER(resolveVirtualMethod);
    CORINFO_METHOD_HANDLE result = wrapHnd->resolveVirtualMethod(virtualMethod, implementingClass, ownerType);
    API_LEAVE(resolveVirtualMethod);
    return result;
}

CORINFO_METHOD_HANDLE WrapICorJitInfo::getUnboxedEntry(
    CORINFO_METHOD_HANDLE       ftn,          /* IN */
    bool* requiresInstMethodTableArg          /* OUT */
)
{
    API_ENTER(getUnboxedEntry);
    CORINFO_METHOD_HANDLE result = wrapHnd->getUnboxedEntry(ftn, requiresInstMethodTableArg);
    API_LEAVE(getUnboxedEntry);
    return result;
}

CORINFO_CLASS_HANDLE WrapICorJitInfo::getDefaultEqualityComparerClass(
    CORINFO_CLASS_HANDLE elemType)
{
    API_ENTER(getDefaultEqualityComparerClass);
    CORINFO_CLASS_HANDLE result = wrapHnd->getDefaultEqualityComparerClass(elemType);
    API_LEAVE(getDefaultEqualityComparerClass);
    return result;
}


void WrapICorJitInfo::expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    API_ENTER(expandRawHandleIntrinsic);
    wrapHnd->expandRawHandleIntrinsic(pResolvedToken, pResult);
    API_LEAVE(expandRawHandleIntrinsic);
}

/**********************************************************************************/
// clang-format on
/**********************************************************************************/
